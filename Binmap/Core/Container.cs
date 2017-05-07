using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Binmap.Core
{
    class Container : Control
    {
        private List<Control> children = new List<Control>();

        public Container(int x, int y, int w, int h, Color color) : base(x, y, w, h, color)
        {
        }

        public virtual Control AddChild(Control child)
        {
            if (child.Parent != null) child.Parent.RemoveChild(child);
            children.Add(child);
            child.Parent = this;
            return child;
        }

        public virtual Control RemoveChild(Control child)
        {
            if (!children.Contains(child)) return child;
            children.Remove(child);
            child.Parent = null;
            return child;
        }

        public virtual void RemoveAllChildren()
        {
            foreach (Control child in children) child.Parent = null;
            children.Clear();
        }

        public override void Update(float time, float dTime)
        {
            if (!Visible) return;

            base.Update(time, dTime);
            Control[] array = children.ToArray(); // copy to allow modification of the list
            foreach (Control child in array) child.Update(time, dTime);
        }

        public virtual void CustomDraw(SpriteBatch spriteBatch)
        {
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible) return;

            base.Draw(spriteBatch);
            CustomDraw(spriteBatch);
            Control[] array = children.ToArray(); // copy to allow modification of the list
            foreach (Control child in array) child.Draw(spriteBatch);
        }

    }
}
