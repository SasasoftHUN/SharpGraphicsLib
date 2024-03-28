using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Utils
{
    public readonly struct Vector2UInt : IEquatable<Vector2UInt>
    {

        #region Fields

        public readonly uint x;
        public readonly uint y;

        #endregion

        #region Constructors

        public Vector2UInt(uint xy)
        {
            x = xy;
            y = xy;
        }
        public Vector2UInt(uint x, uint y)
        {
            this.x = x;
            this.y = y;
        }
        public Vector2UInt(in Vector2Int x)
        {
            this.x = (uint)x.x;
            this.y = (uint)x.y;
        }
        public Vector2UInt(in System.Numerics.Vector2 x)
        {
            this.x = (uint)MathF.Round(x.X);
            this.y = (uint)MathF.Round(x.Y);
        }

        #endregion

        #region Public Methods

        public static Vector2UInt Zero() => new Vector2UInt(0u);

        #region Equals and Equality Operators

        public override bool Equals(object? obj) => obj is Vector2UInt vec2UInt && Equals(vec2UInt);
        public bool Equals(Vector2UInt other) => x == other.x && y == other.y;
        public override int GetHashCode() =>  HashCode.Combine(x, y);

        public static bool operator ==(Vector2UInt left, Vector2UInt right) => left.Equals(right);
        public static bool operator !=(Vector2UInt left, Vector2UInt right) => !(left == right);

        //Equality With Vector2Int
        public bool Equals(Vector2Int other) => x == other.x && y == other.y;
        public static bool operator ==(Vector2UInt left, Vector2Int right) => left.Equals(right);
        public static bool operator !=(Vector2UInt left, Vector2Int right) => !(left == right);

        #endregion

        public static explicit operator Vector2UInt(in Vector2Int x) => new Vector2UInt(x);

        public static Vector2UInt operator +(in Vector2UInt a, in Vector2UInt b) => new Vector2UInt(a.x + b.x, a.y + b.y);
        public static Vector2UInt operator -(in Vector2UInt a, in Vector2UInt b) => new Vector2UInt(a.x - b.x, a.y - b.y);
        public static Vector2UInt operator *(in Vector2UInt a, in Vector2UInt b) => new Vector2UInt(a.x * b.x, a.y * b.y);
        public static Vector2UInt operator /(in Vector2UInt a, in Vector2UInt b) => new Vector2UInt(a.x / b.x, a.y / b.y);
        public static Vector2UInt operator *(in Vector2UInt a, in uint b) => new Vector2UInt(a.x * b, a.y * b);
        public static Vector2UInt operator /(in Vector2UInt a, in uint b) => new Vector2UInt(a.x / b, a.y / b);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('{');
            sb.Append(x);
            sb.Append(';');
            sb.Append(y);
            sb.Append('}');
            return sb.ToString();
        }

        #endregion

    }
}
