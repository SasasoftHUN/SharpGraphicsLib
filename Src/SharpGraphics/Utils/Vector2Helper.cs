using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics.Utils
{
    public static class Vector2Helper
    {
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 XX(this Vector2 a) => new Vector2(a.X, a.X);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 YY(this Vector2 a) => new Vector2(a.Y, a.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 YX(this Vector2 a) => new Vector2(a.Y, a.X);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XXX(this Vector2 a) => new Vector3(a.X, a.X, a.X);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XXY(this Vector2 a) => new Vector3(a.X, a.X, a.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XYY(this Vector2 a) => new Vector3(a.X, a.Y, a.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XYX(this Vector2 a) => new Vector3(a.X, a.Y, a.X);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YYY(this Vector2 a) => new Vector3(a.Y, a.Y, a.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YYX(this Vector2 a) => new Vector3(a.Y, a.Y, a.X);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YXX(this Vector2 a) => new Vector3(a.Y, a.X, a.X);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YXY(this Vector2 a) => new Vector3(a.Y, a.X, a.Y);

    }
}
