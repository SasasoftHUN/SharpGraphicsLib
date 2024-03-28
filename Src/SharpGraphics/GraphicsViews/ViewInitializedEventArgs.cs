using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.GraphicsViews
{
    public class ViewInitializedEventArgs : ViewSizeChangedEventArgs
    {

        public IntPtr Handle { get; private set; }

        public ViewInitializedEventArgs(in Vector2UInt size, in IntPtr handle) : base(size) => Handle = handle;

    }
}
