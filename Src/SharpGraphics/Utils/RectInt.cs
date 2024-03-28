using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Utils
{
    public readonly struct RectInt
    {

        #region Fields

        public readonly Vector2Int bottomLeft;  //Vulkan: upperLeft, GL: bottomLeft
        public readonly Vector2Int size;

        #endregion

        #region Constructors

        public RectInt(Vector2Int bottomLeft, Vector2Int size)
        {
            this.bottomLeft = bottomLeft;
            this.size = size;
        }

        #endregion

        #region Public Methods

        #region Equals and Equality Operators

        public override bool Equals(object? obj) => obj is Rect rect && Equals(rect);
        public bool Equals(RectInt other) => bottomLeft == other.bottomLeft && size == other.size;
        public override int GetHashCode() => HashCode.Combine(bottomLeft, size);

        public static bool operator ==(RectInt left, RectInt right) => left.Equals(right);
        public static bool operator !=(RectInt left, RectInt right) => !(left == right);

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
