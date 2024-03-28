#if CSLANG_7_3 && !CSLANG_8_0
using System;
using System.Runtime.CompilerServices;

namespace SharpGraphics.Utils
{
    public static class MathF
    {

        public const float E = 2.7182818284590451f;
        public const float PI = 3.1415926535897931f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Abs(float value) => Math.Abs(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Acos(float x) => (float)Math.Acos(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Asin(float x) => (float)Math.Asin(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Atan(float x) => (float)Math.Atan(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Ceiling(float x) => (float)Math.Ceiling(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cos(float x) => (float)Math.Cos(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cosh(float value) => (float)Math.Cosh(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Exp(float x) => (float)Math.Exp(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Floor(float x) => (float)Math.Floor(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float IEEERemainder(float x, float y) => (float)Math.IEEERemainder(x, y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Log(float x) => (float)Math.Log(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Log(float a, float newBase) => (float)Math.Log(a, newBase);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Log10(float x) => (float)Math.Log10(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(float val1, float val2) => (float)Math.Max(val1, val2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float val1, float val2) => (float)Math.Min(val1, val2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Pow(float x, float y) => (float)Math.Pow(x, y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Round(float value, MidpointRounding mode) => (float)Math.Round(value, mode);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Round(float value, int digits, MidpointRounding mode) => (float)Math.Round(value, digits, mode);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Round(float value, int digits) => (float)Math.Round(value, digits);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Round(float a) => (float)Math.Round(a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(float value) => Math.Sign(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sin(float a) => (float)Math.Sin(a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sinh(float value) => (float)Math.Sinh(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sqrt(float x) => (float)Math.Sqrt(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Tan(float a) => (float)Math.Tan(a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Tanh(float value) => (float)Math.Tanh(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Truncate(float x) => (float)Math.Truncate(x);

    }
}
#endif