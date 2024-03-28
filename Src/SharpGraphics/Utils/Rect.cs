using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace SharpGraphics.Utils
{
    public readonly struct Rect
    {

        #region Fields

        public readonly Vector2 bottomLeft; //Vulkan: upperLeft, GL: bottomLeft
        public readonly Vector2 size;

        #endregion

        #region Constructors

        public Rect(Vector2 bottomLeft, Vector2 size)
        {
            this.bottomLeft = bottomLeft;
            this.size = size;
        }

        #endregion

        #region Public Methods

        #region Equals and Equality Operators

        public override bool Equals(object? obj) => obj is Rect rect && Equals(rect);
        public bool Equals(Rect other) => bottomLeft == other.bottomLeft && size == other.size;
        public override int GetHashCode() => HashCode.Combine(bottomLeft, size);

        public static bool operator ==(Rect left, Rect right) => left.Equals(right);
        public static bool operator !=(Rect left, Rect right) => !(left == right);

        #endregion

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder('(');
            sb.Append(bottomLeft);
            sb.Append(';');
            sb.Append(size);
            sb.Append(')');
            return sb.ToString();
        }

        #endregion

    }
}
