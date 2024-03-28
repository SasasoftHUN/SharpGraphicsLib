using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics.Utils
{
    public static class MathUtils
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 CreateIdentityMatrix()
            => new Matrix4x4(
                1f, 0f, 0f, 0f,
                0f, 1f, 0f, 0f,
                0f, 0f, 1f, 0f,
                0f, 0f, 0f, 1f
                );



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] ToArray(this Vector2 v) => new float[] { v.X, v.Y };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] ToArray(this Vector3 v) => new float[] { v.X, v.Y, v.Z };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] ToArray(this Vector4 v) => new float[] { v.X, v.Y, v.Z, v.W };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] ToArray(this Matrix3x2 m)
            => new float[] {
                m.M11, m.M12,
                m.M21, m.M22,
                m.M31, m.M32,
            };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] ToArray(this Matrix4x4 m)
            => new float[] {
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44,
            };


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundToInt(this float x) => (int)MathF.Round(x);

    }
}
