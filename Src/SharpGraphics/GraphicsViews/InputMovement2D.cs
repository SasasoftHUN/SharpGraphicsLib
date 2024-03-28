using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.GraphicsViews
{
    public readonly struct InputMovement2D
    {

        public readonly Vector2 Position;
        public readonly Vector2 Difference;

        public InputMovement2D(Vector2 position)
        {
            Position = position;
            Difference = Vector2.Zero;
        }
        public InputMovement2D(int x, int y)
        {
            Position = new Vector2(x, y);
            Difference = Vector2.Zero;
        }
        public InputMovement2D(float x, float y)
        {
            Position = new Vector2(x, y);
            Difference = Vector2.Zero;
        }
        public InputMovement2D(double x, double y)
        {
            Position = new Vector2(Convert.ToSingle(x), Convert.ToSingle(y));
            Difference = Vector2.Zero;
        }

        public InputMovement2D(Vector2 position, Vector2 difference)
        {
            Position = position;
            Difference = difference;
        }
        public InputMovement2D(int x, int y, int xDiff, int yDiff)
        {
            Position = new Vector2(x, y);
            Difference = new Vector2(xDiff, yDiff);
        }
        public InputMovement2D(float x, float y, float xDiff, float yDiff)
        {
            Position = new Vector2(x, y);
            Difference = new Vector2(xDiff, yDiff);
        }
        public InputMovement2D(double x, double y, double xDiff, double yDiff)
        {
            Position = new Vector2(Convert.ToSingle(x), Convert.ToSingle(y));
            Difference = new Vector2(Convert.ToSingle(xDiff), Convert.ToSingle(yDiff));
        }

    }
}
