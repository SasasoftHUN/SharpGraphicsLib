using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Utils
{
    public readonly struct Vector2Int
    {

        #region Fields

        public readonly int x;
        public readonly int y;

        #endregion

        #region Properties

        public static Vector2Int Zero => new Vector2Int(0);

        #endregion

        #region Constructors

        public Vector2Int(int xy)
        {
            x = xy;
            y = xy;
        }
        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        public Vector2Int(in Vector2UInt x)
        {
            this.x = (int)x.x;
            this.y = (int)x.y;
        }
        public Vector2Int(in System.Numerics.Vector2 x)
        {
            this.x = (int)MathF.Round(x.X);
            this.y = (int)MathF.Round(x.Y);
        }

        #endregion

        #region Public Methods

        #region Equals and Equality Operators

        public override bool Equals(object? obj) => obj is Vector2Int vec2Int && Equals(vec2Int);
        public bool Equals(Vector2Int other) => x == other.x && y == other.y;
        public override int GetHashCode() => HashCode.Combine(x, y);

        public static bool operator ==(Vector2Int left, Vector2Int right) => left.Equals(right);
        public static bool operator !=(Vector2Int left, Vector2Int right) => !(left == right);


        //Equality With Vector2UInt
        public bool Equals(Vector2UInt other) => x == other.x && y == other.y;
        public static bool operator ==(Vector2Int left, Vector2UInt right) => left.Equals(right);
        public static bool operator !=(Vector2Int left, Vector2UInt right) => !(left == right);

        #endregion

        public static explicit operator Vector2Int(in Vector2UInt x) => new Vector2Int(x);

        public static Vector2Int operator +(in Vector2Int a, in Vector2Int b) => new Vector2Int(a.x + b.x, a.y + b.y);
        public static Vector2Int operator -(in Vector2Int a, in Vector2Int b) => new Vector2Int(a.x - b.x, a.y - b.y);
        public static Vector2Int operator *(in Vector2Int a, in Vector2Int b) => new Vector2Int(a.x * b.x, a.y * b.y);
        public static Vector2Int operator /(in Vector2Int a, in Vector2Int b) => new Vector2Int(a.x / b.x, a.y / b.y);
        public static Vector2Int operator *(in Vector2Int a, in int b) => new Vector2Int(a.x * b, a.y * b);
        public static Vector2Int operator /(in Vector2Int a, in int b) => new Vector2Int(a.x / b, a.y / b);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder('{');
            sb.Append(x);
            sb.Append(';');
            sb.Append(y);
            sb.Append('}');
            return sb.ToString();
        }

        #endregion

    }
}
