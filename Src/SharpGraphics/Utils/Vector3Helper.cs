using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics.Utils
{
    public static class Vector3Helper
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 XX(this Vector3 a) => new Vector2(a.X, a.X);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 XY(this Vector3 a) => new Vector2(a.X, a.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 YY(this Vector3 a) => new Vector2(a.Y, a.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 YX(this Vector3 a) => new Vector2(a.Y, a.X);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 XZ(this Vector3 a) => new Vector2(a.X, a.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ZZ(this Vector3 a) => new Vector2(a.Z, a.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ZX(this Vector3 a) => new Vector2(a.Z, a.X);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 YZ(this Vector3 a) => new Vector2(a.Y, a.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ZY(this Vector3 a) => new Vector2(a.Z, a.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XXX(this Vector3 a) => new Vector3(a.X, a.X, a.X);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YYY(this Vector3 a) => new Vector3(a.Y, a.Y, a.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ZZZ(this Vector3 a) => new Vector3(a.Z, a.Z, a.Z);

    }
}
