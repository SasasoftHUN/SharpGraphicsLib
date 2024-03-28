using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.GraphicsViews
{
    public class KeyboardEventArgs : UserInputEventArgs
    {

        public KeyboardKey Key { get; }
        public bool IsPressed { get; }

        public KeyboardEventArgs(KeyboardKey key, bool isPressed)
        {
            Key = key;
            IsPressed = isPressed;
        }

    }
}
