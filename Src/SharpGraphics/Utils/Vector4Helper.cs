using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics.Utils
{
    public static class Vector4Helper
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 XY(this Vector4 a) => new Vector2(a.X, a.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XYZ(this Vector4 a) => new Vector3(a.X, a.Y, a.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 XYWW(this Vector4 a) => new Vector4(a.X, a.Y, a.W, a.W);

    }
}
