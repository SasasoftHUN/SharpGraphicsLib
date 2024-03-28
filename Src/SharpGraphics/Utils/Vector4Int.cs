using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Utils
{
    public readonly struct Vector4Int
    {

        #region Fields

        public readonly int x;
        public readonly int y;
        public readonly int z;
        public readonly int w;

        #endregion

        #region Properties

        public static Vector4Int Zero => new Vector4Int(0);

        #endregion

        #region Constructors

        public Vector4Int(int xyzw)
        {
            x = xyzw;
            y = xyzw;
            z = xyzw;
            w = xyzw;
        }
        public Vector4Int(int x, int y, int z, int w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        public Vector4Int(System.Numerics.Vector4 floatVector)
        {
            this.x = (int)MathF.Round(floatVector.X);
            this.y = (int)MathF.Round(floatVector.Y);
            this.z = (int)MathF.Round(floatVector.Z);
            this.w = (int)MathF.Round(floatVector.W);
        }

        #endregion

        #region Public Methods

        #region Equals and Equality Operators

        public override bool Equals(object? obj) => obj is Vector4Int vec4Int && Equals(vec4Int);
        public bool Equals(Vector4Int other) => x == other.x && y == other.y && z == other.z && w == other.w;
        public override int GetHashCode() => HashCode.Combine(x, y, z, w);

        public static bool operator ==(Vector4Int left, Vector4Int right) => left.Equals(right);
        public static bool operator !=(Vector4Int left, Vector4Int right) => !(left == right);


        //Equality With Vector4UInt
        public bool Equals(Vector4UInt other) => x == other.x && y == other.y && z == other.z && w == other.w;
        public static bool operator ==(Vector4Int left, Vector4UInt right) => left.Equals(right);
        public static bool operator !=(Vector4Int left, Vector4UInt right) => !(left == right);

        #endregion

        public Vector4UInt ToVector4UInt() => new Vector4UInt((uint)x, (uint)y, (uint)z, (uint)w);

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
