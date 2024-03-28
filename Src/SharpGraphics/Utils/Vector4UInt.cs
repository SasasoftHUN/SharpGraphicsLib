using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Utils
{
    public readonly struct Vector4UInt
    {

        #region Fields

        public readonly uint x;
        public readonly uint y;
        public readonly uint z;
        public readonly uint w;

        #endregion

        #region Properties

        public static Vector4UInt Zero => new Vector4UInt(0u);

        #endregion

        #region Constructors

        public Vector4UInt(uint xyzw)
        {
            x = xyzw;
            y = xyzw;
            z = xyzw;
            w = xyzw;
        }
        public Vector4UInt(uint x, uint y, uint z, uint w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        public Vector4UInt(System.Numerics.Vector4 floatVector)
        {
            this.x = (uint)MathF.Round(floatVector.X);
            this.y = (uint)MathF.Round(floatVector.Y);
            this.z = (uint)MathF.Round(floatVector.Z);
            this.w = (uint)MathF.Round(floatVector.W);
        }

        #endregion

        #region Public Methods

        #region Equals and Equality Operators

        public override bool Equals(object? obj) => obj is Vector4UInt vec4UInt && Equals(vec4UInt);
        public bool Equals(Vector4UInt other) => x == other.x && y == other.y && z == other.z && w == other.w;
        public override int GetHashCode() => HashCode.Combine(x, y, z, w);

        public static bool operator ==(Vector4UInt left, Vector4UInt right) => left.Equals(right);
        public static bool operator !=(Vector4UInt left, Vector4UInt right) => !(left == right);


        //Equality With Vector4Int
        public bool Equals(Vector4Int other) => x == other.x && y == other.y && z == other.z && w == other.w;
        public static bool operator ==(Vector4UInt left, Vector4Int right) => left.Equals(right);
        public static bool operator !=(Vector4UInt left, Vector4Int right) => !(left == right);

        #endregion

        public Vector4Int ToVector4Int() => new Vector4Int((int)x, (int)y, (int)z, (int)w);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder('{');
            sb.Append(x);
            sb.Append(';');
            sb.Append(y);
            sb.Append(';');
            sb.Append(z);
            sb.Append(';');
            sb.Append(w);
            sb.Append('}');
            return sb.ToString();
        }

        #endregion


    }
}
