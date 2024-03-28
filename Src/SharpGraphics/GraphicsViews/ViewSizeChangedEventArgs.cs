using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.GraphicsViews
{
    public class ViewSizeChangedEventArgs : EventArgs
    {

        public Vector2UInt Size { get; private set; }

        public ViewSizeChangedEventArgs(in Vector2UInt size) => Size = size;

    }
}
