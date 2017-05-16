using Binmap.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Binmap.Controls
{
    class BinListItem : Container
    {
        public int ID = -1;
        private Bin bin;
        public Bin Bin
        {
            get
            {
                return bin;
            }

            set
            {
                bin = value;
                string text = "";

                switch(bin.Format)
                {
                    case Bin.Formats.Hex:
                        text += "88";
                        break;

                    case Bin.Formats.Decimal:
                        text += "888";
                        break;

                    case Bin.Formats.Binary:
                        text += "00000000";
                        break;

                    case Bin.Formats.NibblesDecimal:
                        text += "00 00";
                        break;

                    default:
                        text += bin.Text;
                        break;

                }
                Transform.Width = (int)Math.Ceiling(Main.DefaultFont.MeasureString(text).X) + 8;
            }
        }

        public BinListItem LineEnd;
        public bool Selected;
        public int CommentColumnWidth;

        private Action<BinListItem> clickCallback;
        private Action<BinListItem> mouseEnterCallback;
        private Action<BinListItem> mouseLeaveCallback;
        private TextInput commentInput;

        public BinListItem(Action<BinListItem> clickCallback, Action<BinListItem> mouseEnterCallback, Action<BinListItem> mouseLeaveCallback) : base(0, 0, 10, 10, Color.White)
        {           
            MouseEnabled = true;

            this.clickCallback = clickCallback;
            this.mouseEnterCallback = mouseEnterCallback;
            this.mouseLeaveCallback = mouseLeaveCallback;

            commentInput = new TextInput(0, 0, 143, 20);
            commentInput.OnChangeCallback = commentChanged;
            commentInput.Visible = false;
            AddChild(commentInput);
        }

        private void commentChanged(IInput obj)
        {
            bin.Comment = commentInput.Text;
        }

        protected override void OnMouseDown()
        {
            Selected = !Selected;
            clickCallback(this);
        }

        protected override void OnMouseEnter()
        {
            mouseEnterCallback(this);
        }

        protected override void OnMouseLeave()
        {
            mouseLeaveCallback(this);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Color = Selected ? (Bin.Selected ? Color.Fuchsia : Color.LightGray) : Main.BackgroundColor;

            // comment (only for the first item or items with line break)
            if (Bin.LineBreak || Bin.Offset == 0)
            {
                Rectangle parentRect = Parent.WorldTransform;
                commentInput.Text = Bin.Comment;
                commentInput.TextColor = Bin.Color;
                commentInput.Transform.Width = CommentColumnWidth - 17;
                commentInput.Transform.Height = Transform.Height;
                commentInput.Transform.X = parentRect.Width - CommentColumnWidth - Transform.X + 2;
                commentInput.Transform.Y = 0;
                commentInput.Visible = true;
                commentInput.Font = Main.DefaultFont;
            }
            else commentInput.Visible = false;

            base.Draw(spriteBatch);

            // caption text
            string text = bin.Text;
            Rectangle rect = WorldTransform;
            int x = rect.X;

            if (Selected) spriteBatch.Draw(Main.WhiteTexture, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2), Main.PanelColor);

            Vector2 textSize = Main.DefaultFont.MeasureString(text);
            if (Main.DefaultFont == Main.FontS) textSize.Y -= 2;

            rect.X += (int)Math.Floor((Transform.Width - textSize.X) / 2);
            rect.Y += (int)Math.Round((Transform.Height - textSize.Y) / 2) + 1;
            if (Main.DefaultFont == Main.FontL) rect.Y -= 2;

            spriteBatch.DrawString(Main.DefaultFont, text, new Vector2(rect.X, rect.Y), bin.Color);

            // address range (only for first items in a row)
            if (LineEnd != null)
            {
                // start address
                spriteBatch.DrawString(Main.DefaultFont, bin.Offset.ToString("X4"), new Vector2(x - 90, rect.Y), Main.TrackColor);

                // end address
                if (bin.Offset != LineEnd.Bin.Offset)
                {
                    spriteBatch.DrawString(Main.DefaultFont, " - ", new Vector2(x - 58, rect.Y), Main.BorderColor);
                    spriteBatch.DrawString(Main.DefaultFont, LineEnd.Bin.Offset.ToString("X4"), new Vector2(x - 40, rect.Y), Main.TrackColor);
                }
            }

        }
    }
}
