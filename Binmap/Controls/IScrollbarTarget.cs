using Microsoft.Xna.Framework;

namespace Binmap.Controls
{
    interface IScrollbarTarget
    {
        Rectangle ScrollRectangle { get; }
        int ScrollStepSize { get; }
        int MaxScrollValue { get; }
        int NumVisible { get; }

        void OnScroll(int scrollPosition);
    }
}
