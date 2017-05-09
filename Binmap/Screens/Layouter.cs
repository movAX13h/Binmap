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

        private string statusText = string.Empty;
        private string newStatusText = string.Empty;
        private float statusVis = 0;
        private bool statusFadeOut = true;

        private string usageText = "LMB: select/deselect, Shift+LMB: range selection, RMB: clear selection, ENTER: line break, BACKSPACE: remove line break";
        private string startText = "DROP FILE TO START";

        public string DataFilename { get; private set; } = string.Empty;
        public string BinmapFilename { get; private set; } = string.Empty;
        public string BinmapDataFilename { get; private set; } = string.Empty;

        public string LastError { get; private set; } = string.Empty;

        public Layouter(int x, int y, int w, int h) : base(x, y, w, h, Main.BackgroundColor)
        {
            newStatusText = usageText;
            
            formatButtons = new List<Button>();
            formatButtons.Add(new Button(60, 26, "HEX", Main.HexColor, typeHexButtonClicked, Bin.Formats.Hex));
            formatButtons.Add(new Button(60, 26, "BIN", Main.BinColor, typeHexButtonClicked, Bin.Formats.Binary));
            formatButtons.Add(new Button(60, 26, "DEC", Main.DecColor, typeHexButtonClicked, Bin.Formats.Decimal));
            formatButtons.Add(new Button(60, 26, "NIB", Main.NibDecColor, typeHexButtonClicked, Bin.Formats.NibblesDecimal));
            formatButtons.Add(new Button(60, 26, "ASCII", Main.AsciiColor, typeHexButtonClicked, Bin.Formats.Ascii));
            foreach (Button btn in formatButtons) AddChild(btn);

            smallButton = new Button(26, 26, "S", Color.White, sizeButtonClicked, 0);
            AddChild(smallButton);

            largeButton = new Button(26, 26, "L", Color.White, sizeButtonClicked, 1);
            AddChild(largeButton);

            saveButton = new Button(60, 26, "SAVE", Color.White, saveButtonClicked);
            AddChild(saveButton);

            list = new BinList(10, 10, 100, 100, showStatus);
            list.ItemSpace = itemSpace;
            AddChild(list);

            Resize(w, h);
            setItemFontSize(1);
        }

        public bool LoadFile(string filename)
        {
            if (loadBinmapFile(filename))
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

            byte[] bytes = File.ReadAllBytes(DataFilename);

            list.Lock();
            list.Clear();

            int offset = 0;

            foreach (byte b in bytes)
            {
                Bin item = new Bin(b, offset);
                list.AddItem(item);
                offset++;
            }

            loadBinmapFile(BinmapFilename, true);

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

            showStatus("Binmap saved to " + Path.GetFileName(BinmapFilename), 4);
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
            list.ItemSize = new Point(20, 13 + 2*id);
        }

        private void typeHexButtonClicked(Button btn)
        {
            list.SetBinFormat((Bin.Formats)btn.Tag);
        }
        #endregion


        private float statusTime = 0;

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
        }

        public override void Resize(int w, int h)
        {
            base.Resize(w, h);
            list.Resize(w - 96, h - 40);

            int x = list.Transform.X + w - 90;
            int y = list.Transform.Y;

            foreach (Button btn in formatButtons)
            {
                btn.Transform.X = x;
                btn.Transform.Y = y;
                y += btn.Transform.Height + 4;
            }

            y += 10;
            smallButton.Transform.X = x;
            smallButton.Transform.Y = y;
            y += 30;
            largeButton.Transform.X = x;
            largeButton.Transform.Y = y;
            y += 30;

            saveButton.Transform.X = x;
            saveButton.Transform.Y = list.Transform.Y + list.Transform.Height - saveButton.Transform.Height;

            saveButton.Visible = list.NumItems > 0 && saveButton.Transform.Y > y;
        }

    }
}
