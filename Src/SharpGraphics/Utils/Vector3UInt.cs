using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Utils
{
    public readonly struct Vector3UInt : IEquatable<Vector3UInt>
    {

        #region Fields

        public readonly uint x;
        public readonly uint y;
        public readonly uint z;

        #endregion

        #region Constructors

        public Vector3UInt(uint xyz)
        {
            x = xyz;
            y = xyz;
            z = xyz;
        }
        public Vector3UInt(uint x, uint y)
        {
            this.x = x;
            this.y = y;
            this.z = 0u;
        }
        public Vector3UInt(uint x, uint y, uint z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Vector3UInt(in Vector3Int x)
        {
            this.x = (uint)x.x;
            this.y = (uint)x.y;
            this.z = (uint)x.z;
        }
        public Vector3UInt(in System.Numerics.Vector3 x)
        {
            this.x = (uint)MathF.Round(x.X);
            this.y = (uint)MathF.Round(x.Y);
            this.z = (uint)MathF.Round(x.Z);
        }

        #endregion

        #region Public Methods

        public static Vector3UInt Zero() => new Vector3UInt(0u);

        #region Equals and Equality Operators

        public override bool Equals(object? obj) => obj is Vector3UInt vec3UInt && Equals(vec3UInt);
        public bool Equals(Vector3UInt other) => x == other.x && y == other.y && z == other.z;
        public override int GetHashCode() => HashCode.Combine(x, y, z);

        public static bool operator ==(Vector3UInt left, Vector3UInt right) => left.Equals(right);
        public static bool operator !=(Vector3UInt left, Vector3UInt right) => !(left == right);

        //Equality With Vector3Int
        public bool Equals(Vector3Int other) => x == other.x && y == other.y && z == other.z;
        public static bool operator ==(Vector3UInt left, Vector3Int right) => left.Equals(right);
        public static bool operator !=(Vector3UInt left, Vector3Int right) => !(left == right);

        #endregion

        public static explicit operator Vector3UInt(in Vector3Int x) => new Vector3UInt(x);

        public static Vector3UInt operator +(in Vector3UInt a, in Vector3UInt b) => new Vector3UInt(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3UInt operator -(in Vector3UInt a, in Vector3UInt b) => new Vector3UInt(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3UInt operator *(in Vector3UInt a, in Vector3UInt b) => new Vector3UInt(a.x * b.x, a.y * b.y, a.z * b.z);
        public static Vector3UInt operator /(in Vector3UInt a, in Vector3UInt b) => new Vector3UInt(a.x / b.x, a.y / b.y, a.z / b.z);
        public static Vector3UInt operator *(in Vector3UInt a, in uint b) => new Vector3UInt(a.x * b, a.y * b, a.z * b);
        public static Vector3UInt operator /(in Vector3UInt a, in uint b) => new Vector3UInt(a.x / b, a.y / b, a.z / b);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('{');
            sb.Append(x);
            sb.Append(';');
            sb.Append(y);
            sb.Append(';');
            sb.Append(z);
            sb.Append('}');
            return sb.ToString();
        }

        #endregion

    }
}
