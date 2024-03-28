using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics.Utils
{
    public static class MathHelper
    {

        public const float DEGREES_TO_RADIANS = MathF.PI / 180f;
        public const float RADIANS_TO_DEGREES = 180f / MathF.PI;


        //GLSL - 8.1 Angle and Trigonometry Functions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRadians(this float degrees) => degrees * DEGREES_TO_RADIANS;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToDegrees(this float radians) => radians * RADIANS_TO_DEGREES;


        //GLSL - 8.2 Exponential Functions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Exp2(this float x) => MathF.Pow(2, x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Log2(this float x) => MathF.Log10(x) / MathF.Log10(2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InverseSqrt(this float x) => 1f / MathF.Sqrt(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double InverseSqrt(this double x) => 1f / Math.Sqrt(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sqr(this float x) => x * x;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Sqr(this double x) => x * x;

        //GLSL-8.3 Common Functions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Fract(this float x) => 1f - MathF.Floor(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Fract(this double x) => 1.0 - Math.Floor(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Modulus(this float x, float y) => x - y * MathF.Floor(x / y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Modulus(this double x, double y) => x - y * Math.Floor(x / y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Min(this Vector2 x, float y) => new Vector2(MathF.Min(x.X, y), MathF.Min(x.Y, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Min(this Vector3 x, float y) => new Vector3(MathF.Min(x.X, y), MathF.Min(x.Y, y), MathF.Min(x.Z, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Min(this Vector4 x, float y) => new Vector4(MathF.Min(x.X, y), MathF.Min(x.Y, y), MathF.Min(x.Z, y), MathF.Min(x.W, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Min(this Vector2 x, Vector2 y) => new Vector2(MathF.Min(x.X, y.X), MathF.Min(x.Y, y.Y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Min(this Vector3 x, Vector3 y) => new Vector3(MathF.Min(x.X, y.X), MathF.Min(x.Y, y.Y), MathF.Min(x.Z, y.Z));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Min(this Vector4 x, Vector4 y) => new Vector4(MathF.Min(x.X, y.X), MathF.Min(x.Y, y.Y), MathF.Min(x.Z, y.Z), MathF.Min(x.W, y.W));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Max(this Vector2 x, float y) => new Vector2(MathF.Max(x.X, y), MathF.Max(x.Y, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Max(this Vector3 x, float y) => new Vector3(MathF.Max(x.X, y), MathF.Max(x.Y, y), MathF.Max(x.Z, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Max(this Vector4 x, float y) => new Vector4(MathF.Max(x.X, y), MathF.Max(x.Y, y), MathF.Max(x.Z, y), MathF.Max(x.W, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Max(this Vector2 x, Vector2 y) => new Vector2(MathF.Max(x.X, y.X), MathF.Max(x.Y, y.Y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Max(this Vector3 x, Vector3 y) => new Vector3(MathF.Max(x.X, y.X), MathF.Max(x.Y, y.Y), MathF.Max(x.Z, y.Z));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Max(this Vector4 x, Vector4 y) => new Vector4(MathF.Max(x.X, y.X), MathF.Max(x.Y, y.Y), MathF.Max(x.Z, y.Z), MathF.Max(x.W, y.W));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Abs(this Vector2 x) => new Vector2(MathF.Abs(x.X), MathF.Abs(x.Y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Abs(this Vector3 x) => new Vector3(MathF.Abs(x.X), MathF.Abs(x.Y), MathF.Abs(x.Z));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Abs(this Vector4 x) => new Vector4(MathF.Abs(x.X), MathF.Abs(x.Y), MathF.Abs(x.Z), MathF.Abs(x.W));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(this int x, int min, int max) => x < min ? min : (x > max ? max : x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Clamp(this uint x, uint min, uint max) => x < min ? min : (x > max ? max : x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(this float x, float min, float max) => x < min ? min : (x > max ? max : x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(this double x, double min, double max) => x < min ? min : (x > max ? max : x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Clamp(this Vector2 x, float min, float max) => new Vector2(Clamp(x.X, min, max), Clamp(x.Y, min, max));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Clamp(this Vector3 x, float min, float max) => new Vector3(Clamp(x.X, min, max), Clamp(x.Y, min, max), Clamp(x.Z, min, max));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Clamp(this Vector4 x, float min, float max) => new Vector4(Clamp(x.X, min, max), Clamp(x.Y, min, max), Clamp(x.Z, min, max), Clamp(x.W, min, max));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Clamp(this Vector2 x, Vector2 min, Vector2 max) => new Vector2(Clamp(x.X, min.X, max.X), Clamp(x.Y, min.Y, max.Y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Clamp(this Vector3 x, Vector3 min, Vector3 max) => new Vector3(Clamp(x.X, min.X, max.X), Clamp(x.Y, min.Y, max.Y), Clamp(x.Z, min.Z, max.Z));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Clamp(this Vector4 x, Vector4 min, Vector4 max) => new Vector4(Clamp(x.X, min.X, max.X), Clamp(x.Y, min.Y, max.Y), Clamp(x.Z, min.Z, max.Z), Clamp(x.W, min.W, max.W));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp01(this float x) => x < 0f ? 0f : (x > 1f ? 1f : x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp01(this double x) => x < 0.0 ? 0.0 : (x > 0.0 ? 0.0 : x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Clamp01(this Vector2 x) => new Vector2(Clamp01(x.X), Clamp01(x.Y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Clamp01(this Vector3 x) => new Vector3(Clamp01(x.X), Clamp01(x.Y), Clamp01(x.Z));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Clamp01(this Vector4 x) => new Vector4(Clamp01(x.X), Clamp01(x.Y), Clamp01(x.Z), Clamp01(x.W));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Mix(this float x, float y, float t) => x * (1f - t) + y * t;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Mix(this double x, double y, double t) => x * (1.0 - t) + y * t;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Mix(this Vector2 x, Vector2 y, float t) => x * (1f - t) + y * t;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Mix(this Vector3 x, Vector3 y, float t) => x * (1f - t) + y * t;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Mix(this Vector4 x, Vector4 y, float t) => x * (1f - t) + y * t;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Mix(this Vector2 x, Vector2 y, Vector2 t) => x * (Vector2.One - t) + y * t;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Mix(this Vector3 x, Vector3 y, Vector3 t) => x * (Vector3.One - t) + y * t;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Mix(this Vector4 x, Vector4 y, Vector4 t) => x * (Vector4.One - t) + y * t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Step(this float edge, float x) => x < edge ? 0f : 1f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Step(this double edge, double x) => x < edge ? 0.0 : 1.0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Step(this float edge, Vector2 x) => new Vector2(Step(edge, x.X), Step(edge, x.Y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Step(this float edge, Vector3 x) => new Vector3(Step(edge, x.X), Step(edge, x.Y), Step(edge, x.Z));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Step(this float edge, Vector4 x) => new Vector4(Step(edge, x.X), Step(edge, x.Y), Step(edge, x.Z), Step(edge, x.W));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Step(this Vector2 edge, Vector2 x) => new Vector2(Step(edge.X, x.X), Step(edge.Y, x.Y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Step(this Vector3 edge, Vector3 x) => new Vector3(Step(edge.X, x.X), Step(edge.Y, x.Y), Step(edge.Z, x.Z));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Step(this Vector4 edge, Vector4 x) => new Vector4(Step(edge.X, x.X), Step(edge.Y, x.Y), Step(edge.Z, x.Z), Step(edge.W, x.W));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SmoothStep(double edge0, double edge1, double x)
        {
            double t = Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3.0 - 2.0 * t);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 SmoothStep(float edge0, float edge1, Vector2 x) => new Vector2(SmoothStep(edge0, edge1, x.X), SmoothStep(edge0, edge1, x.Y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 SmoothStep(float edge0, float edge1, Vector3 x) => new Vector3(SmoothStep(edge0, edge1, x.X), SmoothStep(edge0, edge1, x.Y), SmoothStep(edge0, edge1, x.Z));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 SmoothStep(float edge0, float edge1, Vector4 x) => new Vector4(SmoothStep(edge0, edge1, x.X), SmoothStep(edge0, edge1, x.Y), SmoothStep(edge0, edge1, x.Z), SmoothStep(edge0, edge1, x.W));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 SmoothStep(Vector2 edge0, Vector2 edge1, Vector2 x) => new Vector2(SmoothStep(edge0.X, edge1.X, x.X), SmoothStep(edge0.Y, edge1.Y, x.Y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 SmoothStep(Vector3 edge0, Vector3 edge1, Vector3 x) => new Vector3(SmoothStep(edge0.X, edge1.X, x.X), SmoothStep(edge0.Y, edge1.Y, x.Y), SmoothStep(edge0.Z, edge1.Z, x.Z));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 SmoothStep(Vector4 edge0, Vector4 edge1, Vector4 x) => new Vector4(SmoothStep(edge0.X, edge1.X, x.X), SmoothStep(edge0.Y, edge1.Y, x.Y), SmoothStep(edge0.Z, edge1.Z, x.Z), SmoothStep(edge0.W, edge1.W, x.W));


        //GLSL - 8.5 Geometric Functions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 FaceForward(Vector2 n, Vector2 i, Vector2 nRef) => Vector2.Dot(nRef, i) < 0f ? n : -n;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FaceForward(Vector3 n, Vector3 i, Vector3 nRef) => Vector3.Dot(nRef, i) < 0f ? n : -n;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 FaceForward(Vector4 n, Vector4 i, Vector4 nRef) => Vector4.Dot(nRef, i) < 0f ? n : -n;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Refract(Vector2 i, Vector2 n, float eta)
        {
            float dot = Vector2.Dot(n, i);
            float k = 1f - eta * eta * (1f - Sqr(dot));
            return k < 0f ? Vector2.Zero : eta * i - (eta * dot + MathF.Sqrt(k)) * n;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Refract(Vector3 i, Vector3 n, float eta)
        {
            float dot = Vector3.Dot(n, i);
            float k = 1f - eta * eta * (1f - Sqr(dot));
            return k < 0f ? Vector3.Zero : eta * i - (eta * dot + MathF.Sqrt(k)) * n;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Refract(Vector4 i, Vector4 n, float eta)
        {
            float dot = Vector4.Dot(n, i);
            float k = 1f - eta * eta * (1f - Sqr(dot));
            return k < 0f ? Vector4.Zero : eta * i - (eta * dot + MathF.Sqrt(k)) * n;
        }


    }
}
