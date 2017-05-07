using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Binmap.Core;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace Binmap.Controls
{
    class BinList : Container, IScrollbarTarget, IInput
    {
        public List<Bin> Bins { get { return bins; } }
        private List<Bin> bins;
        private List<BinListItem> items;
        private SortedList<int,Bin> selection;
        private BinListItem lastSelectedItem;

        public Point itemSize { get; private set; } = new Point(20);
        public Point ItemSize
        {
            set
            {
                if (itemSize.X == value.X && itemSize.Y == value.Y) return;
                itemSize = value;
                dirty = true;
                if (!layoutLocked) Layout();
            }
        }

        private Point itemSpace;
        public Point ItemSpace
        {
            set
            {
                if (itemSpace.X == value.X && itemSpace.Y == value.Y) return;
                itemSpace = value;
                dirty = true;
                if (!layoutLocked) Layout();
            }
        }

        public int NumItems
        {
            get
            {
                return items.Count;
            }
        }

        #region Scrollbar target implementations
        Rectangle IScrollbarTarget.ScrollRectangle { get { return Transform; } }
        public int ScrollStepSize { get { return 100; } }
        public int MaxScrollValue { get { return bins.Count - 1; } }
        public int NumVisible { get { return items.Count; } }

        public bool Focused
        {
            set
            {
                
            }
        }

        public Action<IInput> OnChangeCallback
        {
            set
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        private bool dirty = false;
        private bool layoutLocked = false;

        private int startIndex = 0;
        private int commentColumnWidth = 160;
        
        private Scrollbar scrollbar;

        public BinList(int x, int y, int w, int h) : base(x, y, w, h, Main.BorderColor)
        {
            bins = new List<Bin>();
            items = new List<BinListItem>();
            selection = new SortedList<int, Bin>();

            scrollbar = new Scrollbar(this);
            AddChild(scrollbar);

            MouseEnabled = true;
        }

        protected override void OnMouseDown()
        {
            Main.SetFocus(this);
        }

        public void SetBinFormat(Bin.Formats format)
        {
            foreach (Bin bin in selection.Values) bin.Format = format;
            if (selection.Count > 0) Layout();
        }

        public void Layout()
        {
            scrollbar.Visible = bins.Count > 0;
            if (bins.Count == 0)
            {
                while (items.Count > 0)
                {
                    RemoveChild(items[0]);
                    items.RemoveAt(0);
                }
                return;
            }

            dirty = false;

            int x = 100;
            int y = 2;

            int ctr = 0;

            BinListItem lineStartItem = null;
            BinListItem lineEndItem = null;

            for(int i = startIndex; i < bins.Count && y < Transform.Height - itemSize.Y - itemSpace.Y; i++)
            {
                Bin bin = bins[i];

                BinListItem item;
                if (ctr >= items.Count)
                {
                    item = new BinListItem(itemClicked);
                    item.CommentColumnWidth = commentColumnWidth;
                    items.Add(item);
                    AddChild(item);
                }
                else item = items[ctr];

                if (lineStartItem == null) lineStartItem = item;

                item.Transform.Height = itemSize.Y;
                item.LineEnd = null;
                item.Bin = bin;
                item.ID = ctr;
                item.Selected = selection.ContainsKey(item.Bin.Offset);

                if (x + item.Transform.Width + itemSpace.X > Transform.Width - commentColumnWidth || item.Bin.LineBreak)
                {
                    lineStartItem.LineEnd = lineEndItem;

                    x = 100;
                    y += itemSize.Y + itemSpace.Y;

                    lineStartItem = item;
                    lineEndItem = item;
                }
                else lineEndItem = item;

                item.Transform.X = x;
                item.Transform.Y = y;

                x += item.Transform.Width + itemSpace.X;

                ctr++;
            }

            if (lineStartItem != null) lineStartItem.LineEnd = lineEndItem;

            if (ctr < bins.Count && ctr > 0) ctr--;

            while (items.Count > ctr)
            {
                RemoveChild(items[items.Count - 1]);
                items.RemoveAt(items.Count - 1);
            }

            scrollbar.Visible = items.Count != bins.Count;
            scrollbar.Layout();
        }

        private void itemClicked(BinListItem item)
        {
            Main.SetFocus(this);

            if (lastSelectedItem != null && Main.KeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift))
            {
                int start = Math.Min(lastSelectedItem.ID, item.ID);
                int end =   Math.Max(lastSelectedItem.ID, item.ID);

                for (int i = start; i <= end; i++)
                {
                    BinListItem rangeItem = items[i];
                    if (!selection.ContainsKey(rangeItem.Bin.Offset)) selection.Add(rangeItem.Bin.Offset, rangeItem.Bin);
                }

                if (end - start > 0) Layout();
            }

            lastSelectedItem = item;
        }


        public bool ProcessKey(Keys key)
        {
            // add line break
            if (key == Keys.Enter)
            {
                if (selection.Count > 0 && selection.First().Value.Offset > 0)
                {
                    selection.First().Value.LineBreak = true;
                    selection.First().Value.Comment = "";
                    Layout();
                }

                return true;
            }

            // remove line break
            if (key == Keys.Back)
            {
                if (selection.Count > 0 && selection.First().Value.LineBreak)
                {
                    selection.First().Value.LineBreak = false;
                    selection.First().Value.Comment = "";
                    Layout();
                }

                return true;
            }

            return false;
        }

        public override void Update(float time, float dTime)
        {
            base.Update(time, dTime);
            
            // clear selection
            if (MouseIsOver && Main.MouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                lastSelectedItem = null;
                selection.Clear();
                Layout();
            }

            // update selections
            foreach(BinListItem item in items)
            {
                if (item.Selected && !selection.ContainsKey(item.Bin.Offset)) selection.Add(item.Bin.Offset, item.Bin);
                else if (!item.Selected && selection.ContainsKey(item.Bin.Offset))
                {
                    selection.Remove(item.Bin.Offset);
                    lastSelectedItem = null;
                }
            }
        }

        public void Lock()
        {
            layoutLocked = true;
        }

        public void Unlock()
        {
            layoutLocked = false;
            if (dirty) Layout();
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Rectangle rect = WorldTransform;
            spriteBatch.Draw(Main.WhiteTexture, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2), Main.PanelColor);
            if (bins.Count > 0) spriteBatch.Draw(Main.WhiteTexture, new Rectangle(rect.X + rect.Width - commentColumnWidth, rect.Y + 2, 1, rect.Height - 4), Main.BorderColor);
        }


        public override void Resize(int w, int h)
        {
            base.Resize(w, h);
            scrollbar.Resize(14, h);
            if (!layoutLocked) Layout();
        }

        public void AddItem(Bin item)
        {
            bins.Add(item);
            dirty = true;
            if (!layoutLocked) Layout();
        }

        public void RemoveItem(Bin item)
        {
            bins.Remove(item);
            dirty = true;
            if (!layoutLocked) Layout();
        }

        public void Clear()
        {
            bins.Clear();
            dirty = true;
            lastSelectedItem = null;
            selection.Clear();
            scrollbar.ScrollTo(0);
        }

        public void OnScroll(int scrollPosition)
        {
            startIndex = scrollPosition;
            Layout();
        }

    }
}
