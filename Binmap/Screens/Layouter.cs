using Microsoft.Xna.Framework;
using Binmap.Controls;
using Binmap.Core;
using System;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

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

        public Layouter(int x, int y, int w, int h) : base(x, y, w, h, Main.BackgroundColor)
        {
            newStatusText = usageText;
            
            formatButtons = new List<Button>();
            formatButtons.Add(new Button(80, 26, "HEX", Main.HexColor, typeHexButtonClicked, Bin.Formats.Hex));
            formatButtons.Add(new Button(80, 26, "BIN", Main.BinColor, typeHexButtonClicked, Bin.Formats.Binary));
            formatButtons.Add(new Button(80, 26, "DEC", Main.DecColor, typeHexButtonClicked, Bin.Formats.Decimal));
            formatButtons.Add(new Button(80, 26, "NIB", Main.NibDecColor, typeHexButtonClicked, Bin.Formats.NibblesDecimal));
            formatButtons.Add(new Button(80, 26, "ASCII", Main.AsciiColor, typeHexButtonClicked, Bin.Formats.Ascii));
            foreach (Button btn in formatButtons) AddChild(btn);

            smallButton = new Button(26, 26, "S", Color.White, sizeButtonClicked, 0);
            AddChild(smallButton);

            largeButton = new Button(26, 26, "L", Color.White, sizeButtonClicked, 1);
            AddChild(largeButton);

            writeButton = new Button(80, 26, "WRITE", Color.White, writeButtonClicked);
            AddChild(writeButton);

            saveButton = new Button(80, 26, "SAVE", Color.White, saveButtonClicked);
            AddChild(saveButton);

            valueInput = new TextInput(10, 10, 72, 14);
            valueInput.TextColor = Main.DecColor;
            valueInput.OnChangeCallback = valueChanged;
            AddChild(valueInput);

            list = new BinList(10, 10, 100, 100, itemSelected, showStatus);
            list.ItemSpace = itemSpace;
            AddChild(list);

            Resize(w, h);
            setItemFontSize(1);
        }

        private void writeButtonClicked(Button obj)
        {
            showStatus("Changes written to '" + Path.GetFileName(DataFilename) + "'", 2);

            using (Stream stream = File.Open(DataFilename, FileMode.Open))
            {
                foreach (Bin bin in modifiedBins)
                {
                    stream.Position = bin.Offset;
                    stream.WriteByte(bin.Value);
                }
            }

            modifiedBins.Clear();
        }

        private void valueChanged(IInput input)
        {
            TextInput textInput = input as TextInput;

            byte b = selectedBin.Value;
            if (byte.TryParse(textInput.Text, out b))
            {
                if (!modifiedBins.Contains(selectedBin)) modifiedBins.Add(selectedBin);
                selectedBin.Value = b;
                valueInput.FocusFrameColor = Main.TrackColor;
            }
            else valueInput.FocusFrameColor = Color.Red;
            
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
                valueInput.Text = item.Bin.Value.ToString();
                valueInput.Visible = true;
            }
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
            saveButton.Visible = list.NumItems > 0;
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

        private void showStatus(string text, float time)
        {
            newStatusText = text;
            statusFadeOut = true;
            statusTime = time;
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
            // selection info
            if (selectedBin != null)
            {
                int h = 38;

                Rectangle rect = valueInput.WorldTransform;
                int x = rect.X - 4;
                int y = rect.Y;
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
            if (list.NumItems == 0)
            {
                Point center = new Point(list.Transform.X + list.Transform.Width / 2, list.Transform.Y + list.Transform.Height / 2);

                color = Color.FromNonPremultiplied(255, 255, 255, 40);
                spriteBatch.Draw(Main.Logo, new Rectangle(center.X - Main.Logo.Width, center.Y - Main.Logo.Height * 2 + 50, Main.Logo.Width * 2, Main.Logo.Height * 2), color);

                Vector2 size = Main.FontL.MeasureString(startText);
                spriteBatch.DrawString(Main.FontL, startText, 
                    new Vector2((float)Math.Floor(center.X - size.X / 2f), (float)Math.Floor(center.Y - size.Y / 2f) + 70), 
                    Main.BorderColor);
            }

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

            valueInput.Transform.X = x + 4;
            valueInput.Transform.Y = y;
            y += 50;

            int by = list.Transform.Y + list.Transform.Height - saveButton.Transform.Height;

            saveButton.Transform.X = x;
            saveButton.Transform.Y = by;
            saveButton.Visible = list.NumItems > 0 && saveButton.Transform.Y > y;

            writeButton.Transform.X = x;
        }

        #region button handlers
        private void saveButtonClicked(Button btn)
        {
            saveFile();
        }

        private void sizeButtonClicked(Button btn)
        {
            setItemFontSize((int)btn.Tag);
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

        private void typeHexButtonClicked(Button btn)
        {
            list.SetBinFormat((Bin.Formats)btn.Tag);
        }
        #endregion
    }
}
