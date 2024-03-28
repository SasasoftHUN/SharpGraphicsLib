using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.GraphicsViews
{
    
    public abstract class MouseEventArgs : UserInputEventArgs
    {
    }

    public class MouseButtonEventArgs : MouseEventArgs
    {
        public MouseButton Button { get; }
        public bool IsPressed { get; }

        public MouseButtonEventArgs(MouseButton button, bool isPressed)
        {
            Button = button;
            IsPressed = isPressed;
        }
    }

    public class MouseMoveEventArgs : MouseEventArgs
    {
        public InputMovement2D Movement { get; }

        public MouseMoveEventArgs(InputMovement2D movement) => Movement = movement;
    }

}
