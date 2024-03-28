using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.GraphicsViews
{
    public abstract class TouchEventArgs : UserInputEventArgs
    {
        public int ID { get; }
        public TouchEventArgs(int id) => ID = id;

    }

    public class TouchDetectEventArgs : TouchEventArgs
    {
        public bool IsPressed { get; }
        public TouchDetectEventArgs(int id, bool isPressed) : base(id) => IsPressed = isPressed;
    }

    public class TouchMoveEventArgs : TouchEventArgs
    {
        
        public InputMovement2D Movement { get; }
        public InputMovement2D StartMovement { get; }

        public TouchMoveEventArgs(int id, in InputMovement2D movement, in InputMovement2D startMovement) : base(id)
        {
            Movement = movement;
            StartMovement = startMovement;
        }

    }
}
