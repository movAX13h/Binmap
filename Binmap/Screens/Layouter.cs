using Microsoft.Xna.Framework;
using Binmap.Controls;
using Binmap.Core;
using System;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Globalization;

namespace Binmap.Screens
{
    class Layouter : Container
    {
        private BinList list;
        private Point itemSize = new Point(30);
        private Point itemSpace = new Point(2);

        private List<Button> formatButtons;
        private Button smallButton;
        private Button largeButton;
        private Button saveButton;
        private Button writeButton;

        private TextInput valueInput;
        private TextInput searchInput;
        private TextInput gotoInput;

        private Button valueInputTypeButton;
        private Button searchInputTypeButton;
        private Button gotoInputTypeButton;

        private string statusText = string.Empty;
        private string newStatusText = string.Empty;
        private float statusVis = 0;
        private bool statusFadeOut = true;
        private float statusTime = 0;

        private string usageText = "LMB: select/deselect, Shift+LMB: range selection, RMB: clear selection, ENTER: line break, BACKSPACE: remove line break";
        private string startText = "DROP FILE TO START";

        public string DataFilename { get; private set; } = string.Empty;
        public string BinmapFilename { get; private set; } = string.Empty;
        public string BinmapDataFilename { get; private set; } = string.Empty;

        public string LastError { get; private set; } = string.Empty;

        private Bin selectedBin;
        private List<Bin> modifiedBins = new List<Bin>();

        public int NumBytes { get { return list.Bins.Count; } }

        public Layouter(int x, int y, int w, int h) : base(x, y, w, h, Main.BackgroundColor)
        {
            newStatusText = usageText;
            
            formatButtons = new List<Button>();
            formatButtons.Add(new Button(80, 26, "HEX", Main.HexColor, formatButtonClicked, Bin.Formats.Hex));
            formatButtons.Add(new Button(80, 26, "BIN", Main.BinColor, formatButtonClicked, Bin.Formats.Binary));
            formatButtons.Add(new Button(80, 26, "DEC", Main.DecColor, formatButtonClicked, Bin.Formats.Decimal));
            formatButtons.Add(new Button(80, 26, "NIB", Main.NibDecColor, formatButtonClicked, Bin.Formats.NibblesDecimal));
            formatButtons.Add(new Button(80, 26, "ASCII", Main.AsciiColor, formatButtonClicked, Bin.Formats.Ascii));
            foreach (Button btn in formatButtons) AddChild(btn);

            smallButton = new Button(26, 26, "S", Color.White, sizeButtonClicked, 0);
            AddChild(smallButton);

            largeButton = new Button(26, 26, "L", Color.White, sizeButtonClicked, 1);
            AddChild(largeButton);

            writeButton = new Button(80, 26, "WRITE", Color.White, writeButtonClicked);
            AddChild(writeButton);

            saveButton = new Button(80, 26, "SAVE", Color.White, saveButtonClicked);
            AddChild(saveButton);

            // value inputs
            valueInput = new TextInput(10, 10, 56, 14);
            valueInput.TextColor = Main.DecColor;
            valueInput.OnChangeCallback = valueChanged;
            valueInput.Visible = false;
            AddChild(valueInput);

            valueInputTypeButton = new Button(15, 14, "D", Main.DecColor, inputTypeSwitchClicked, valueInput);
            valueInputTypeButton.Font = Main.FontS;
            valueInputTypeButton.Transform.X = 57;
            valueInput.AddChild(valueInputTypeButton);

            // search inputs
            searchInput = new TextInput(10, 10, 56, 14);
            searchInput.TextColor = Main.HexColor;
            searchInput.OnChangeCallback = searchChanged;
            searchInput.OnSubmitCallback = searchCommitted;
            searchInput.Visible = false;
            AddChild(searchInput);

            searchInputTypeButton = new Button(15, 14, "H", Main.HexColor, inputTypeSwitchClicked, searchInput);
            searchInputTypeButton.Font = Main.FontS;
            searchInputTypeButton.Transform.X = 57;
            searchInput.AddChild(searchInputTypeButton);

            // goto inputs
            gotoInput = new TextInput(10, 10, 56, 14);
            gotoInput.TextColor = Main.DecColor;
            gotoInput.OnChangeCallback = gotoInputChanged;
            gotoInput.OnSubmitCallback = gotoCommitted;
            gotoInput.Visible = false;
            AddChild(gotoInput);

            gotoInputTypeButton = new Button(15, 14, "D", Main.DecColor, inputTypeSwitchClicked, gotoInput);
            gotoInputTypeButton.Font = Main.FontS;
            gotoInputTypeButton.Transform.X = 57;
            gotoInput.AddChild(gotoInputTypeButton);

            list = new BinList(10, 10, 100, 100, itemSelected, showStatus);
            list.ItemSpace = itemSpace;
            AddChild(list);

            Resize(w, h);
            setItemFontSize(1);
        }


        public bool LoadFile(string filename)
        {
            // clear some variables
            selectedBin = null;
            modifiedBins.Clear();

            // filename can be any file or a .binmap
            bool isBinmap = false;

            try
            {
                isBinmap = loadBinmapFile(filename);
            }
            catch (Exception e)
            {
                LastError = "Exception while trying to load binmap file: " + e.Message;
                return false;
            }

            if (isBinmap)
            {
                DataFilename = Path.Combine(Path.GetDirectoryName(filename), BinmapDataFilename);
                BinmapFilename = filename;
            }
            else
            {
                DataFilename = filename;
                BinmapFilename = filename + ".binmap";
            }

            if (!File.Exists(DataFilename))
            {
                LastError = "The associated file '" + DataFilename + "' was not found!";
                return false;
            }

            list.Lock();

            try
            {
                byte[] bytes = File.ReadAllBytes(DataFilename);

                list.Clear();

                int offset = 0;

                foreach (byte b in bytes)
                {
                    Bin item = new Bin(b, offset);
                    list.AddItem(item);
                    offset++;
                }
            }
            catch (Exception e)
            {
                LastError = "Failed to load data: " + e.Message;
                return false;
            }

            try
            {
                loadBinmapFile(BinmapFilename, true);
            }
            catch (Exception e)
            {
                LastError = "Exception while trying to load binmap file: " + e.Message;
                return false;
            }

            list.Unlock();
            saveButton.Visible = list.NumVisible > 0;
            gotoInput.Visible = saveButton.Visible;
            searchInput.Visible = saveButton.Visible;
            return true;
        }

        private bool loadBinmapFile(string filename, bool apply = false)
        {
            // look for & load binmap file
            if (!File.Exists(filename)) return false;
            if (Path.GetExtension(filename) != ".binmap") return false;

            using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                string signature = reader.ReadString();
                if (signature != "BINMAP")
                {
                    Debug.WriteLine("Binmap signature not found!");
                    return false;
                }

                string version = reader.ReadString();
                BinmapDataFilename = reader.ReadString();

                if (apply)
                {
                    int numBytes = reader.ReadInt32();
                    if (numBytes != list.Bins.Count)
                    {
                        Debug.WriteLine("Byte count mismatch!");
                        return false;
                    }

                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        int offset = reader.ReadInt32();
                        int format = reader.ReadByte();
                        bool lineBreak = reader.ReadBoolean();
                        string comment = reader.ReadString();

                        Bin bin = list.Bins[offset];
                        bin.Format = (Bin.Formats)format;
                        bin.LineBreak = lineBreak;
                        bin.Comment = comment;

                        if (bin.LineBreak || bin.Offset == 0)
                        {
                            list.AddScrollbarMark(bin.Offset, bin.Color);
                        }
                    }

                    Debug.WriteLine("Binmap loaded.");
                }
            }

            return true;
        }

        private void saveFile()
        {
            if (DataFilename == string.Empty) return;

            using (BinaryWriter writer = new BinaryWriter(File.Open(BinmapFilename, FileMode.Create)))
            {
                writer.Write("BINMAP");
                writer.Write(Main.Version);
                writer.Write(Path.GetFileName(DataFilename));
                writer.Write(list.Bins.Count);
                
                foreach (Bin bin in list.Bins)
                {
                    if (bin.Format > 0 || bin.LineBreak)
                    {
                        writer.Write(bin.Offset);
                        writer.Write((byte)bin.Format);
                        writer.Write(bin.LineBreak);
                        writer.Write(bin.Comment);
                    }
                }
            }

            showStatus("Binmap saved to '" + Path.GetFileName(BinmapFilename) + "'", 4);
        }

        private void writeDataFileChanges()
        {
            using (Stream stream = File.Open(DataFilename, FileMode.Open))
            {
                foreach (Bin bin in modifiedBins)
                {
                    stream.Position = bin.Offset;
                    stream.WriteByte(bin.Value);
                }
            }

            showStatus("Changes written to '" + Path.GetFileName(DataFilename) + "'", 2);
            modifiedBins.Clear();
        }

        private void showStatus(string text, float time)
        {
            newStatusText = text;
            statusFadeOut = true;
            statusTime = time;
        }

        private void itemSelected(BinListItem item)
        {
            if (item == null)
            {
                valueInput.Visible = false;
                selectedBin = null;
            }
            else
            {
                selectedBin = item.Bin;
                valueInput.Text = valueInputTypeButton.Text == "H" ? item.Bin.Value.ToString("X2") : item.Bin.Value.ToString();
                valueInput.Visible = true;
            }
        }

        private void setItemFontSize(int id)
        {
            if (id == 0)
            {
                smallButton.NormalColor = Main.BorderColor;
                largeButton.NormalColor = Main.PanelColor;
            }
            else
            {
                smallButton.NormalColor = Main.PanelColor;
                largeButton.NormalColor = Main.BorderColor;
            }

            SpriteFont[] fonts = new SpriteFont[] { Main.FontS, Main.FontL };

            Main.DefaultFont = fonts[id];
            list.ItemSize = new Point(20, 13 + 2 * id);
        }


        public override void Update(float time, float dTime)
        {
            base.Update(time, dTime);

            if (statusFadeOut)
            {
                statusVis -= dTime * 4;

                if (statusVis < 0)
                {
                    statusVis = 0;
                    statusText = newStatusText;
                    statusFadeOut = false;
                }
            }
            else
            {
                statusVis += dTime * 4;

                if (statusVis > 1)
                {
                    statusVis = 1;
                }
            }

            statusTime -= dTime;

            if (newStatusText != usageText && statusTime < 0)
            {
                newStatusText = usageText;
                statusFadeOut = true;
            }

        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            int h = 38;
            Rectangle rect;
            int x, y;
            
            if (searchInput.Visible)
            {
                rect = searchInput.WorldTransform;
                x = rect.X - 4;
                y = rect.Y;
                spriteBatch.Draw(Main.WhiteTexture, new Rectangle(x, y - 20, 80, h), Main.PanelColor);
                spriteBatch.DrawString(Main.FontS, "SEARCH", new Vector2(x + 4, y - 12), Color.White);

                rect = gotoInput.WorldTransform;
                x = rect.X - 4;
                y = rect.Y;
                spriteBatch.Draw(Main.WhiteTexture, new Rectangle(x, y - 20, 80, h), Main.PanelColor);
                spriteBatch.DrawString(Main.FontS, "GOTO", new Vector2(x + 4, y - 12), Color.White);
            }

            // selection info
            if (selectedBin != null)
            {
                rect = valueInput.WorldTransform;
                x = rect.X - 4;
                y = rect.Y;
                spriteBatch.Draw(Main.WhiteTexture, new Rectangle(x, y - 20, 80, h), Main.PanelColor);
                spriteBatch.DrawString(Main.FontS, "VALUE", new Vector2(x + 4, y - 12), Color.White);

                y += h - 4;

                spriteBatch.DrawString(Main.FontS, "SELECTION", new Vector2(x, y), Color.White);

                string s = list.Selection.Count.ToString() + " byte" + (list.Selection.Count > 1 ? "s" : ""); /* + Environment.NewLine +
                        "first: " + list.Selection.First().Value.Offset.ToString() + Environment.NewLine +
                        "last: " + list.Selection.Last().Value.Offset.ToString(); */

                spriteBatch.DrawString(Main.FontS, s, new Vector2(x + 2, y + 12), Main.TrackColor);
            }

        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            // status text
            byte v = (byte)Math.Round(100f * statusVis);
            Color color = Color.FromNonPremultiplied(255, 255, 255, v);
            spriteBatch.DrawString(Main.FontL, statusText, new Vector2(list.Transform.X, Transform.Height - 24), color);

            // text and logo when nothing is loaded
            if (list.NumVisible == 0)
            {
                Point center = new Point(list.Transform.X + list.Transform.Width / 2, list.Transform.Y + list.Transform.Height / 2);

                color = Color.FromNonPremultiplied(255, 255, 255, 40);
                spriteBatch.Draw(Main.Logo, new Rectangle(center.X - Main.Logo.Width, center.Y - Main.Logo.Height * 2 + 50, Main.Logo.Width * 2, Main.Logo.Height * 2), color);

                Vector2 size = Main.FontL.MeasureString(startText);
                spriteBatch.DrawString(Main.FontL, startText, 
                    new Vector2((float)Math.Floor(center.X - size.X / 2f), (float)Math.Floor(center.Y - size.Y / 2f) + 70), 
                    Main.BorderColor);
            }

            // write button position and visibility
            writeButton.Transform.Y = saveButton.Transform.Y - 30;
            writeButton.Visible = modifiedBins.Count > 0 && writeButton.Transform.Y > valueInput.WorldTransform.Y + 50;
        }

        public override void Resize(int w, int h)
        {
            base.Resize(w, h);
            list.Resize(w - 108, h - 40);

            int x = list.Transform.X + w - 102;
            int y = list.Transform.Y;

            foreach (Button btn in formatButtons)
            {
                btn.Transform.X = x;
                btn.Transform.Y = y;
                y += btn.Transform.Height + 4;
            }

            y += 10;
            smallButton.Transform.X = x + 13;
            smallButton.Transform.Y = y;
            
            largeButton.Transform.X = x + 43;
            largeButton.Transform.Y = y;
            y += 60;

            searchInput.Transform.X = x + 4;
            searchInput.Transform.Y = y;
            y += 40;

            gotoInput.Transform.X = x + 4;
            gotoInput.Transform.Y = y;
            y += 40;

            valueInput.Transform.X = x + 4;
            valueInput.Transform.Y = y;
            //valueInputTypeButton.Transform.X = x + 66;
            //valueInputTypeButton.Transform.Y = y - 16;

            y += 50;

            int by = list.Transform.Y + list.Transform.Height - saveButton.Transform.Height;

            saveButton.Transform.X = x;
            saveButton.Transform.Y = by;
            saveButton.Visible = list.NumVisible > 0 && saveButton.Transform.Y > y;

            writeButton.Transform.X = x;
        }

        #region button handlers

        private int getIntValue(Button btn)
        {
            TextInput input = btn.Tag as TextInput;
            int i = -1;

            if (btn.Text == "H")
            {
                if (!int.TryParse(input.Text, NumberStyles.HexNumber, null, out i)) return -1;
            }
            else
            {
                if (!int.TryParse(input.Text, out i)) return -1;
            }

            return i;
        }

        private void gotoInputChanged(IInput obj)
        {
            int i = getIntValue(gotoInputTypeButton);
            gotoInput.FocusFrameColor = (i >= 0 && i < list.Bins.Count) ? Main.TrackColor : Color.Red;
        }

        private void gotoCommitted(IInput input)
        {
            int i = getIntValue(gotoInputTypeButton);
            if (i >= 0 && i < list.Bins.Count) list.ScrollTo(i);
        }

        private byte[] getSearchQuery()
        {
            string text = searchInput.Text.Trim();

            if (text == string.Empty) return null;

            bool hex = searchInputTypeButton.Text == "H";
            List<byte> bytes = new List<byte>();

            string[] parts = text.Split(' ');
            foreach (string part in parts)
            {
                text = part.Trim();
                if (text.Length == 0) return null;

                int value = -1;
                if (hex)
                {
                    if (!int.TryParse(text, NumberStyles.HexNumber, null, out value) || value > 255) return null;
                    else bytes.Add((byte)value);
                }
                else
                {
                    if (!int.TryParse(text, out value) || value > 255) return null;
                    else bytes.Add((byte)value);
                }
            }

            return bytes.ToArray();
        }

        private void searchChanged(IInput input)
        {
            byte[] query = getSearchQuery();
            if (query == null) searchInput.FocusFrameColor = Color.Red;
            else searchInput.FocusFrameColor = Main.TrackColor;
        }

        private void searchCommitted(IInput input)
        {
            byte[] query = getSearchQuery();
            if (query != null)
            {
                int i = list.Search(query);
                if (i >= 0) list.ScrollTo(i);
                else showStatus("No match found for query '" + searchInput.Text + "'.", 2);
            }
        }        

        private void valueChanged(IInput input)
        {
            TextInput textInput = input as TextInput;
            int i = getIntValue(valueInputTypeButton);

            if (i >= 0 && i < 256)
            {
                if (!modifiedBins.Contains(selectedBin)) modifiedBins.Add(selectedBin);
                selectedBin.Value = (byte)i;
                valueInput.FocusFrameColor = Main.TrackColor;
            }
            else valueInput.FocusFrameColor = Color.Red;
        }

        private void inputTypeSwitchClicked(Button btn)
        {
            TextInput input = btn.Tag as TextInput;

            NumberStyles oldStyle;
            string format = "";

            if (btn.Text == "H")
            {
                btn.TextColor = Main.DecColor;
                oldStyle = NumberStyles.HexNumber;
            }
            else
            {
                btn.TextColor = Main.HexColor;
                oldStyle = NumberStyles.Integer;
                format = "X2";
            }

            if (input.Text != "")
            {
                if (input == searchInput) // special treatment for search input because it can have several values (space-separated)
                {
                    byte[] query = getSearchQuery();
                    if (query != null)
                    {
                        List<string> bytes = new List<string>();
                        foreach(byte b in query) bytes.Add(b.ToString(format));
                        input.Text = string.Join(" ", bytes);
                    }
                }
                else
                {
                    int i = -1;
                    if (int.TryParse(input.Text, oldStyle, null, out i))
                        input.Text = i.ToString(format);
                }
            }

            input.TextColor = btn.TextColor;
            btn.Text = btn.Text == "H" ? "D" : "H"; // do this here and not above because getSearchQuery relies on the text
        }

        private void writeButtonClicked(Button obj)
        {
            writeDataFileChanges();
        }
        
        private void saveButtonClicked(Button btn)
        {
            saveFile();
        }

        private void sizeButtonClicked(Button btn)
        {
            setItemFontSize((int)btn.Tag);
        }

        private void formatButtonClicked(Button btn)
        {
            list.SetBinFormat((Bin.Formats)btn.Tag);
        }
        #endregion
    }
}
