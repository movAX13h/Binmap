using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Binmap.Core;
using System;
using Microsoft.Xna.Framework.Input;

namespace Binmap.Controls
{
    class TextInput : Container, IInput
    {
        public string Text = "";
        private bool focused = false;
        public bool Focused
        {
            set
            {
                focused = value;
            }
        }

        private Action<IInput> onChangeCallback = null;
        public Action<IInput> OnChangeCallback
        {
            set
            {
                onChangeCallback = value;
            }
        }

        private int caretPosition = 0;
        private float caretTime = 0;

        public TextInput(int x, int y, int w, int h) : base(x, y, w, h, Main.BackgroundColor)
        {
            MouseEnabled = true;
        }

        protected override void OnMouseDown()
        {
            Main.SetFocus(this);
            caretPosition = Text.Length;
        }
        
        public override void Update(float time, float dTime)
        {
            base.Update(time, dTime);
            caretTime += dTime;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible) return;

            Color = focused ? Main.TrackColor : Main.BackgroundColor;

            base.Draw(spriteBatch);

            Rectangle worldRect = WorldTransform;
            if (focused) spriteBatch.Draw(Main.WhiteTexture, new Rectangle(worldRect.X + 1, worldRect.Y + 1, worldRect.Width - 2, worldRect.Height - 2), Main.PanelColor);

            // text
            int x = worldRect.X + 2;
            int y = worldRect.Y - 1;
            if (Main.DefaultFont == Main.FontS) y += 4;
            spriteBatch.DrawString(Main.DefaultFont, Text, new Vector2(x, y), Color.Yellow);

            // caret
            if (focused && caretTime % 0.4f > 0.2f)
            {
                string left = Text.Substring(0, caretPosition);

                Vector2 textSize = Main.DefaultFont.MeasureString(left);
                x += (int)Math.Round(textSize.X);
                if (Main.DefaultFont == Main.FontL) y += 3;
                else y -= 1;

                spriteBatch.Draw(Main.WhiteTexture, new Rectangle(x, y, 1, Transform.Height - 4), Color.White);
            }
        }

        public bool ProcessKey(Keys key)
        {
            string left = Text.Substring(0, caretPosition);
            string right = Text.Substring(caretPosition);
            string s = "";
            int i = 0;

            switch (key)
            {
                case Keys.Left:
                    caretPosition = Math.Min(Text.Length, Math.Max(0, caretPosition - 1));
                    break;

                case Keys.Right:
                    caretPosition = Math.Min(Text.Length, Math.Max(0, caretPosition + 1));
                    break;

                case Keys.A:
                case Keys.B:
                case Keys.C:
                case Keys.D:
                case Keys.E:
                case Keys.F:
                case Keys.G:
                case Keys.H:
                case Keys.I:
                case Keys.J:
                case Keys.K:
                case Keys.L:
                case Keys.M:
                case Keys.N:
                case Keys.O:
                case Keys.P:
                case Keys.Q:
                case Keys.R:
                case Keys.S:
                case Keys.T:
                case Keys.U:
                case Keys.V:
                case Keys.W:
                case Keys.X:
                case Keys.Y:
                case Keys.Z:
                    s = key.ToString();
                    if (!Main.KeyboardState.IsKeyDown(Keys.LeftShift) && !Main.KeyboardState.IsKeyDown(Keys.RightShift)) s = s.ToLower();

                    Text = left + s + right;
                    onChangeCallback?.Invoke(this);
                    caretPosition += 1;
                    break;

                case Keys.Space:
                    Text = left + " " + right;
                    onChangeCallback?.Invoke(this);
                    caretPosition += 1;
                    break;

                case Keys.Add:
                    Text = left + "+" + right;
                    onChangeCallback?.Invoke(this);
                    caretPosition += 1;
                    break;

                case Keys.Subtract:
                    Text = left + "-" + right;
                    onChangeCallback?.Invoke(this);
                    caretPosition += 1;
                    break;

                case Keys.Decimal:
                case Keys.OemComma:
                    Text = left + "," + right;
                    onChangeCallback?.Invoke(this);
                    caretPosition += 1;
                    break;

                case Keys.OemPeriod:
                    Text = left + "." + right;
                    onChangeCallback?.Invoke(this);
                    caretPosition += 1;
                    break;

                case Keys.Multiply:
                    Text = left + "*" + right;
                    onChangeCallback?.Invoke(this);
                    caretPosition += 1;
                    break;

                case Keys.Divide:
                    Text = left + "/" + right;
                    onChangeCallback?.Invoke(this);
                    caretPosition += 1;
                    break;

                case Keys.D1:
                case Keys.D2:
                case Keys.D3:
                case Keys.D4:
                case Keys.D5:
                case Keys.D6:
                case Keys.D7:
                case Keys.D8:
                case Keys.D9:
                case Keys.D0:
                case Keys.NumPad1:
                case Keys.NumPad2:
                case Keys.NumPad3:
                case Keys.NumPad4:
                case Keys.NumPad5:
                case Keys.NumPad6:
                case Keys.NumPad7:
                case Keys.NumPad8:
                case Keys.NumPad9:
                case Keys.NumPad0:
                    s = key.ToString().Substring(key.ToString().Length - 1);
                    i = int.Parse(s);

                    if (Main.KeyboardState.IsKeyDown(Keys.LeftShift) || Main.KeyboardState.IsKeyDown(Keys.RightShift))
                    {
                        switch(i)
                        {
                            case 1: s = "!"; break;
                            case 2: s = "\""; break;
                            //case 3: s = "§"; break;
                            case 4: s = "$"; break;
                            case 5: s = "%"; break;
                            case 6: s = "&"; break;
                            case 7: s = "/"; break;
                            case 8: s = "("; break;
                            case 9: s = ")"; break;
                            case 0: s = "="; break;
                        }
                    }

                    Text = left + s + right;
                    onChangeCallback?.Invoke(this);
                    caretPosition += 1;
                    break;

                case Keys.Back:
                    if (left.Length > 0)
                    {
                        Text = left.Substring(0, left.Length - 1) + right;
                        onChangeCallback?.Invoke(this);
                        caretPosition -= 1;
                    }
                    break;

                case Keys.Enter:
                    Main.SetFocus(null);
                    break;
                
                default:
                    return false;
            }

            return true;
        }
    }
}
