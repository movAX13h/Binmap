using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Binmap.Core
{
    class Control
    {
        public Rectangle Transform;
        public Container Parent;
        public Color Color;

        private bool visible = true;
        public bool Visible
        {
            get { return visible; }
            set { visible = value; OnVisibilityChanged(); }
        }

        public bool MouseEnabled = false;
        public bool MouseIsOver { get; protected set; } = false;
        public bool MouseIsDown { get; protected set; } = false;

        public Rectangle WorldTransform
        {
            get
            {
                Rectangle pos = Transform;
                Control p = Parent;

                while (p != null)
                {
                    pos.X += p.Transform.X;
                    pos.Y += p.Transform.Y;
                    p = p.Parent;
                }

                return pos;
            }
        }

        public Control(int x, int y, int w, int h, Color color)
        {
            Transform = new Rectangle(x, y, w, h);
            Color = color;
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible) return;
            spriteBatch.Draw(Main.WhiteTexture, WorldTransform, Color);
        }

        public virtual void Update(float time, float dTime)
        {
            if (!Visible) return;

            if (MouseEnabled)
            {
                Rectangle r = new Rectangle(Main.MouseState.Position, new Point(1, 1));
                MouseIsOver = WorldTransform.Intersects(r);

                if (MouseIsOver && Main.MouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                {
                    if (!MouseIsDown)
                    {
                        OnMouseDown();
                    }
                    MouseIsDown = true;
                }
                else MouseIsDown = false;
            }
        }

        protected virtual void OnMouseDown()
        {

        }

        protected virtual void OnVisibilityChanged()
        { }

        public virtual void Resize(int w, int h)
        {
            Transform.Width = w;
            Transform.Height = h;
        }
    }
}
