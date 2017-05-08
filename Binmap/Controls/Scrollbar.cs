using System;
using Binmap.Core;

namespace Binmap.Controls
{
    class Scrollbar : Container
    {
        private Button buttonA;
        private Button buttonB;
        private Button thumb;

        public int ScrollPosition { get; private set; } = 0;

        private IScrollbarTarget target;
        private int lastDragY = -1000;
        private int lastScrollWheelValue;
        float lastButtonScrollTime = 0;

        public Scrollbar(IScrollbarTarget target) : base(0, 0, 20, 20, Main.BorderColor)
        {
            this.target = target;

            buttonA = new Button(14, 14, "", Main.BorderColor, buttonAClicked);
            buttonA.NormalColor = Main.BackgroundColor;
            buttonA.OverColor = Main.PanelColor;
            AddChild(buttonA);

            buttonB = new Button(14, 14, "", Main.BorderColor, buttonBClicked);
            buttonB.NormalColor = Main.BackgroundColor;
            buttonB.OverColor = Main.PanelColor;
            AddChild(buttonB);

            thumb = new Button(14, 14, "", Main.BorderColor, thumbClicked);
            thumb.Transform.X = 2;
            thumb.NormalColor = Main.TrackColor;
            AddChild(thumb);

            lastScrollWheelValue = Main.MouseState.ScrollWheelValue;
        }

        private void buttonAClicked(Button btn)
        {
            //scroll(-target.ScrollStepSize);
        }

        private void buttonBClicked(Button btn)
        {
            //scroll(target.ScrollStepSize);
        }

        private void thumbClicked(Button btn)
        {
            lastDragY = Main.MouseState.Position.Y;
        }

        public void Layout()
        {
            int availableHeight = Transform.Height - 2 * buttonA.Transform.Height;
            thumb.Resize(Transform.Width - 4, Math.Max(thumb.Transform.Width, Math.Min(availableHeight, availableHeight * target.NumVisible / target.MaxScrollValue)));
            availableHeight -= thumb.Transform.Height;
            float s = (float)ScrollPosition / (Math.Max(1, target.MaxScrollValue - target.NumVisible));
            thumb.Transform.Y = buttonA.Transform.Height + (int)Math.Round(availableHeight * s);
        }

        private void scrollByDistance(int dy)
        {
            int availableHeight = Transform.Height - 2 * buttonA.Transform.Height - thumb.Transform.Height;
            int max = Math.Max(1, target.MaxScrollValue - target.NumVisible);
            
            int id = ScrollPosition + (int)Math.Round((float)max * dy / availableHeight);
            id = Math.Max(0, Math.Min(id, max));
            ScrollTo(id);
        }

        public void ScrollTo(int targetPosition)
        {
            ScrollPosition = Math.Max(0, Math.Min(target.MaxScrollValue, targetPosition));
            target.OnScroll(ScrollPosition);
            Layout();
        }

        private void scrollPosition(int delta)
        {
            ScrollPosition += delta;
            ScrollPosition = Math.Min(Math.Max(0, target.MaxScrollValue - target.NumVisible), Math.Max(0, ScrollPosition));
            target.OnScroll(ScrollPosition);
            Layout();
        }

        public override void Update(float time, float dTime)
        {
            base.Update(time, dTime);

            // up/down buttons
            if (time > lastButtonScrollTime + 0.1f)
            {
                if (buttonA.MouseIsDown)
                {
                    scrollPosition(-target.ScrollStepSize);
                    lastButtonScrollTime = time;
                }

                if (buttonB.MouseIsDown)
                {
                    scrollPosition(target.ScrollStepSize);
                    lastButtonScrollTime = time;
                }
            }

            // dragging the thumb
            if (lastDragY > -1000)
            {
                thumb.NormalColor = Main.BackgroundColor;
                scrollByDistance(Main.MouseState.Position.Y - lastDragY);
                lastDragY = Main.MouseState.Position.Y;
                if (Main.MouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released) lastDragY = -1000;
            }
            else thumb.NormalColor = Main.TrackColor;

            // scroll wheel
            if (!Parent.MouseIsOver)
            {
                lastScrollWheelValue = Main.MouseState.ScrollWheelValue;
            }
            else if (Main.MouseState.ScrollWheelValue != lastScrollWheelValue)
            {
                scrollPosition(target.ScrollStepSize * Math.Sign(lastScrollWheelValue - Main.MouseState.ScrollWheelValue));
                lastScrollWheelValue = Main.MouseState.ScrollWheelValue;
            }
        }

        public override void Resize(int w, int h)
        {
            base.Resize(w, h);

            Transform.X = target.ScrollRectangle.Width - w;

            buttonA.Resize(w, w);
            buttonB.Resize(w, w);
            buttonB.Transform.Y = h - buttonB.Transform.Height;

            Layout();
        }
    }
}
