using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static SharpGraphics.Apps.SimpleRaytrace.RayTraceHelpers;

namespace SharpGraphics.Apps.SimpleRaytrace
{
    [FragmentShader]
    public partial class RaytraceFragmentShader : FragmentShaderBase
	{

		[In] private Vector4 vs_out_pos;

		[Out] private Vector4 fs_out_col;

		[Uniform(Set = 0u, Binding = 0u)] [ArraySize(Count = 16)] private SphereData[] spheres;
		[Uniform(Set = 0u, Binding = 1u)] private LightData light;
		[Uniform(Set = 0u, Binding = 2u)] private SceneData scene;
		[Uniform(Set = 0u, Binding = 3u)] private TextureSamplerCube<Vector4> skyboxSampler;

		private void ClosestIntersect(in Ray ray, out float t_min, out int i_isc)
		{
			float tmp_min = 1000.0f;
			int tmp_idx = -1;

			for (int idx = 0; idx < scene.numberOfSpheres; idx++)
			{
				float t = ray.IntersectSphere(spheres[idx].position, spheres[idx].radius);
				if ((t > 0.001f && t < tmp_min))
				{
					tmp_min = t;
					tmp_idx = idx;
				}
			}

			t_min = tmp_min;
			i_isc = tmp_idx;
		}

		private Vector3 Trace(in Ray ray, ref int i_isc, ref float t_param, ref float l_param, out Vector3 newNormal)
		{
			Vector3 acc_col = new Vector3(0f);

			float t_min = -1.0f;
			i_isc = -1;

			ClosestIntersect(ray, out t_min, out i_isc);

			if (i_isc != -1)
			{
				// calculate the base color with lighting
				Vector3 ipt = ray.origin + t_min * ray.direction;
				Vector3 n = Vector3.Normalize(ipt - spheres[i_isc].position);
				newNormal = n;
				t_param = t_min;

				// check if there is something between us and the light source
				Vector3 new_orig = ipt;
				Vector3 new_dir = Vector3.Normalize(light.position - ipt);

				if (Vector3.Dot(n, new_dir) > 0.0f)
				{
					// may be intersection, the light source is not behind the object
					float closest_dist = 0f;
					int closest_idx;
					ClosestIntersect(new Ray(new_orig, new_dir), out closest_dist, out closest_idx);

					if (closest_dist > 0.0001f && closest_dist < (light.position - ipt).Length())
					{
						l_param *= 0.05f;
						return spheres[i_isc].color * l_param;
					}

					float intensity = MathHelper.Clamp(Vector3.Dot(n, Vector3.Normalize(light.position - ipt)), 0.05f, 1f);
					l_param *= intensity;
					return spheres[i_isc].color * l_param;
				}
				else
                {
					l_param *= 0.05f;
					return spheres[i_isc].color * l_param; // we are in shadow - only the light emitted by us is visible here
				}
			}
			else
            {
				newNormal = new Vector3(0f);
				//return new Vector3(0.1f, 0.2f, 0.4f) / 2f; // "background color"
				return skyboxSampler.Sample(ray.direction).XYZ() * l_param;
			}
		}



		private void GetRay(in Vector3 inVec, out Ray ray)
		{
			// coordinates of the point mapped to the click on the near clipping plane in world coordinate system
			Vector4 nearPt = Vector4.Transform(new Vector4(inVec.XY(), -1, 1), scene.viewProjI);
			// coordinates of the point mapped to the click on the far clipping plane in world coordinate system
			Vector4 farPt = Vector4.Transform(new Vector4(inVec.XY(), 1, 1), scene.viewProjI);

			// let the ray start from the near clipping plane
			Vector3 rayOrig = nearPt.XYZ() / nearPt.W;

			// the direction of it from here is trivial
			Vector3 rayEnd = farPt.XYZ() / farPt.W;
			ray = new Ray(rayOrig, Vector3.Normalize(rayEnd - rayOrig));
		}


		public override void Main()
        {
			Ray ray;

			GetRay(vs_out_pos.XYZ(), out ray);

			float t = -1.0f;
			Vector3 n = new Vector3(0f, 1f, 0f);
			int idx = 0;
			float l = 1f;

			fs_out_col = new Vector4(0f);

			for (int i = 0; i < scene.maxTrace; ++i)
			{
				fs_out_col += new Vector4(Trace(ray, ref idx, ref t, ref l, out n) / MathF.Pow(2.0f, (float)i), 1.0f);
				if (t > 0.0f)
				{
					ray = new Ray(
						ray.origin + t * ray.direction + 0.01f * n,
						Vector3.Reflect(ray.direction, n));
					if (Vector3.Dot(ray.direction, n) < 0.0f)
						break;
				}
				//else Discard();
				else fs_out_col = skyboxSampler.Sample(ray.direction) * l;
			}
		}

    }
}
