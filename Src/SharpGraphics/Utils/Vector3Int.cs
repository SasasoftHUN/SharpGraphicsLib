using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Utils
{
    public readonly struct Vector3Int : IEquatable<Vector3Int>
    {

        #region Fields

        public readonly int x;
        public readonly int y;
        public readonly int z;

        #endregion

        #region Constructors

        public Vector3Int(int xyz)
        {
            x = xyz;
            y = xyz;
            z = xyz;
        }
        public Vector3Int(int x, int y)
        {
            this.x = x;
            this.y = y;
            this.z = 0;
        }
        public Vector3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Vector3Int(in Vector3UInt x)
        {
            this.x = (int)x.x;
            this.y = (int)x.y;
            this.z = (int)x.z;
        }
        public Vector3Int(in System.Numerics.Vector3 x)
        {
            this.x = (int)MathF.Round(x.X);
            this.y = (int)MathF.Round(x.Y);
            this.z = (int)MathF.Round(x.Z);
        }

        #endregion

        #region Public Methods

        public static Vector3Int Zero() => new Vector3Int(0);

        #region Equals and Equality Operators

        public override bool Equals(object? obj) => obj is Vector3Int vec3Int && Equals(vec3Int);
        public bool Equals(Vector3Int other) => x == other.x && y == other.y && z == other.z;
        public override int GetHashCode() => HashCode.Combine(x, y, z);

        public static bool operator ==(Vector3Int left, Vector3Int right) => left.Equals(right);
        public static bool operator !=(Vector3Int left, Vector3Int right) => !(left == right);

        //Equality With Vector3UInt
        public bool Equals(Vector3UInt other) => x == other.x && y == other.y && z == other.z;
        public static bool operator ==(Vector3Int left, Vector3UInt right) => left.Equals(right);
        public static bool operator !=(Vector3Int left, Vector3UInt right) => !(left == right);

        #endregion

        public static explicit operator Vector3Int(in Vector3UInt x) => new Vector3Int(x);

        public static Vector3Int operator +(in Vector3Int a, in Vector3Int b) => new Vector3Int(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3Int operator -(in Vector3Int a, in Vector3Int b) => new Vector3Int(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3Int operator *(in Vector3Int a, in Vector3Int b) => new Vector3Int(a.x * b.x, a.y * b.y, a.z * b.z);
        public static Vector3Int operator /(in Vector3Int a, in Vector3Int b) => new Vector3Int(a.x / b.x, a.y / b.y, a.z / b.z);
        public static Vector3Int operator *(in Vector3Int a, in int b) => new Vector3Int(a.x * b, a.y * b, a.z * b);
        public static Vector3Int operator /(in Vector3Int a, in int b) => new Vector3Int(a.x / b, a.y / b, a.z / b);

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
