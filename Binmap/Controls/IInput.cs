using Microsoft.Xna.Framework.Input;
using System;

namespace Binmap.Controls
{
    public interface IInput
    {
        bool Focused { set; }
        bool ProcessKey(Keys key);
        Action<IInput> OnChangeCallback { set; }
    }
}
