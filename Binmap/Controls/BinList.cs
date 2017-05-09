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

        public SortedList<int,Bin> Selection { get; private set; }
        private Bin lastSelectedBin = null;

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

        #region IScrollbarTarget and IInput implementations
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
                
            }
        }
        #endregion

        private bool dirty = false;
        private bool layoutLocked = false;

        private int startIndex = 0;
        private int commentColumnWidth = 160;
        
        private Scrollbar scrollbar;
        private Action<string, float> statusCallback;
        private Action<BinListItem> itemSelectedCallback;
        private BinListItem overItem = null;

        public BinList(int x, int y, int w, int h, Action<BinListItem> itemSelectedCallback, Action<string, float> statusCallback) : base(x, y, w, h, Main.BorderColor)
        {
            this.statusCallback = statusCallback;
            this.itemSelectedCallback = itemSelectedCallback;

            bins = new List<Bin>();
            items = new List<BinListItem>();
            Selection = new SortedList<int, Bin>();

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
            foreach (Bin bin in Selection.Values) bin.Format = format;
            if (Selection.Count > 0) Layout();
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
                    item = new BinListItem(itemClick, itemEnter, itemLeave);
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
                item.Selected = Selection.ContainsKey(item.Bin.Offset);

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

        private void itemEnter(BinListItem item)
        {
            overItem = item;
        }

        private void itemLeave(BinListItem item)
        {
            if (overItem == item) overItem = null;
        }

        private void itemClick(BinListItem item)
        {
            Main.SetFocus(this);

            if (lastSelectedBin != null && Main.KeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift))
            {
                int start = Math.Min(lastSelectedBin.Offset, item.Bin.Offset);
                int end =   Math.Max(lastSelectedBin.Offset, item.Bin.Offset);

                if (end - start > 0)
                {
                    for (int i = start; i <= end; i++)
                    {
                        Bin rangeItem = bins[i];
                        if (!Selection.ContainsKey(rangeItem.Offset)) Selection.Add(rangeItem.Offset, rangeItem);
                    }

                    Layout();
                }
            }

            if (lastSelectedBin != null)
            {
                lastSelectedBin.Selected = false;
                itemSelectedCallback(null);
            }

            if (item.Selected)
            {
                lastSelectedBin = item.Bin;
                lastSelectedBin.Selected = true;
                if (!Selection.ContainsKey(item.Bin.Offset)) Selection.Add(item.Bin.Offset, item.Bin);
                itemSelectedCallback(item);
            }
            else
            {
                if (Selection.ContainsKey(item.Bin.Offset)) Selection.Remove(item.Bin.Offset);
                lastSelectedBin = null;
            }
        }

        private void deselectAll()
        {
            if (lastSelectedBin != null) lastSelectedBin.Selected = false;
            lastSelectedBin = null;
            Selection.Clear();
            itemSelectedCallback(null);
        }

        public bool ProcessKey(Keys key)
        {
            if (key == Keys.C && (Main.KeyboardState.IsKeyDown(Keys.LeftControl) || Main.KeyboardState.IsKeyDown(Keys.RightControl)))
            {
                if (Selection.Count > 0)
                {
                    string[] s = new string[Selection.Count];
                    int i = 0;
                    foreach (Bin selBin in Selection.Values)
                    {
                        s[i] = (selBin.LineBreak ? Environment.NewLine : "") + selBin.Text;
                        i++;
                    }
                    System.Windows.Forms.Clipboard.SetText(string.Join(",", s));
                    statusCallback("Copied " + i + " byte" + (i > 1 ? "s" : "") + " to clipboard.", 2);
                }
                else statusCallback("No bytes selected!", 1);

                return true;
            }

            if (Selection.Count == 0) return false;

            Bin bin = Selection.First().Value;

            // add line break
            if (key == Keys.Enter)
            {
                if (bin.Offset > 0)
                {
                    bin.LineBreak = true;
                    bin.Comment = "";
                    Layout();
                }

                return true;
            }

            // remove line break
            if (key == Keys.Back)
            {
                if (bin.LineBreak)
                {
                    bin.LineBreak = false;
                    bin.Comment = "";
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
                deselectAll();
                Layout();
            }            
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Rectangle rect = WorldTransform;

            // background
            spriteBatch.Draw(Main.WhiteTexture, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2), Main.PanelColor);

            // vertical separator line for comments column
            if (bins.Count > 0) spriteBatch.Draw(Main.WhiteTexture, new Rectangle(rect.X + rect.Width - commentColumnWidth, rect.Y + 2, 1, rect.Height - 4), Main.BorderColor);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            #region cursor note
            if (!MouseIsOver || overItem == null) return;

            string text = "Address: 0x" + overItem.Bin.Offset.ToString("X") + " (" + overItem.Bin.Offset.ToString() + ")" + Environment.NewLine +
                          "Value: 0x" + overItem.Bin.Value.ToString("X2") + " (" + overItem.Bin.Value.ToString() + ")";

            if (lastSelectedBin != null && lastSelectedBin != overItem.Bin)
            {
                int dist = Math.Abs(lastSelectedBin.Offset - overItem.Bin.Offset) + 1;
                text += Environment.NewLine +
                        "-------------------------------" + Environment.NewLine +
                        "Range: 0x" + lastSelectedBin.Offset.ToString("X") + " -> 0x" + overItem.Bin.Offset.ToString("X") + " = " + dist.ToString() + "bytes";
            }

            Vector2 textSize = Main.DefaultFont.MeasureString(text);

            textSize.Y -= Main.DefaultFont == Main.FontL ? 6 : 4;
            
            Rectangle rect = new Rectangle(Main.MouseState.Position.X + 10, Main.MouseState.Position.Y + 24, (int)textSize.X + 8, (int)textSize.Y + 8);
            spriteBatch.Draw(Main.WhiteTexture, rect, Color.FromNonPremultiplied(0, 0, 0, 160));

            Vector2 textPos = new Vector2(Main.MouseState.Position.X + 14, Main.MouseState.Position.Y + 24);
            textPos.Y += Main.DefaultFont == Main.FontL ? 1 : 5;

            spriteBatch.DrawString(Main.DefaultFont, text, textPos, Color.Black);

            textPos.X -= 1;
            textPos.Y -= 1;
            spriteBatch.DrawString(Main.DefaultFont, text, textPos, Color.White);
            #endregion
        }

        public override void Resize(int w, int h)
        {
            base.Resize(w, h);
            scrollbar.Resize(14, h);
            if (!layoutLocked) Layout();
        }

        public void OnScroll(int scrollPosition)
        {
            startIndex = scrollPosition;
            Layout();
        }

        #region item management
        public void Lock()
        {
            layoutLocked = true;
        }

        public void Unlock()
        {
            layoutLocked = false;
            if (dirty) Layout();
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
            deselectAll();
            scrollbar.ScrollTo(0);
        }
        #endregion
    }
}
