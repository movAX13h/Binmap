using Binmap.Core;
using Microsoft.Xna.Framework;
using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Binmap.Controls
{
    class Button : Control, IInput
    {
        public object Tag = null;
        private Action<Button> clickCallback;

        public string Text = "";
        public Color TextColor = Color.White;

        public Color NormalColor = Main.PanelColor;
        public Color OverColor = Main.BackgroundColor;

        public SpriteFont Font = Main.FontL;

        public bool Focused { set { } }
        public Action<IInput> OnChangeCallback { set { } }

        public Button(int w, int h, string text, Color textColor, Action<Button> callback = null, object tag = null) : base(0, 0, w, h, Main.BorderColor)
        {
            MouseEnabled = true;
            clickCallback = callback;
            TextColor = textColor;
            Text = text;
            Tag = tag;
        }

        protected override void OnMouseDown()
        {
            Main.SetFocus(this);
            clickCallback?.Invoke(this);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible) return;

            base.Draw(spriteBatch);

            Rectangle rect = WorldTransform;

            spriteBatch.Draw(Main.WhiteTexture, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2), MouseIsOver ? OverColor : NormalColor);

            Vector2 size = Font.MeasureString(Text);
            rect.X += (int)Math.Floor((Transform.Width - size.X) / 2) + 1;
            rect.Y += (int)Math.Floor((Transform.Height - size.Y) / 2);

            if (Font == Main.FontS)
            {
                rect.X -= 1;
                rect.Y += 3;
            }

            spriteBatch.DrawString(Font, Text, new Vector2(rect.X, rect.Y), TextColor);
        }

        public bool ProcessKey(Keys key)
        {
            throw new NotImplementedException();
        }
    }
}
