using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Apps.SimpleRaytrace
{

    public static class RayTraceHelpers
    {

		public readonly struct Ray
		{
			public readonly Vector3 origin;
			public readonly Vector3 direction;

			public Ray(Vector3 origin, Vector3 direction)
			{
				this.origin = origin;
				this.direction = direction;
			}
		}

		// intersect the [idx]-th spheres with the ray p(t) = r+t*d - return: smallest positive root, or -1 if it does not exist
		public static float IntersectSphere(this in Ray ray, in Vector3 center, in float r)
		{
			Vector3 rayOrig = (new Vector4(ray.origin, 1f)).XYZ();
			Vector3 rayDir = (new Vector4(ray.direction, 0f)).XYZ();

			// coeffs of quadratic equation
			float A = Vector3.Dot(rayDir, rayDir);
			float B = 2.0f * Vector3.Dot(rayDir, rayOrig - center);
			float C = Vector3.Dot(rayOrig - center, rayOrig - center) - r * r;

			// solve it too
			float discr = B * B - 4.0f * A * C;

			// if only complex root => return -1
			if (discr < 0.0f)
				return -1.0f;

			// else calculate the 2 roots
			Vector2 ts = -new Vector2(B / (2.0f * A)) + new Vector2(-1.0f, 1.0f) * MathF.Sqrt(discr) / (2.0f * A);

			float t = ts.X;
			if (t < 0.0f)
				t = ts.Y;

			// if the intersection point if behind them, drop the fragment
			if (t < 0.0f)
				return -1.0f;
			else
				return t;
		}

	}
}
