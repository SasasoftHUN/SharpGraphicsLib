using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.GraphicsViews
{
    public abstract class UserInputEventArgs : EventArgs
    {
    }

    public sealed class UserInputCloseEventArgs : UserInputEventArgs { }

}
