using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.Utils
{
    public static class ParametricSurfaces
    {

        [StructLayout(LayoutKind.Sequential)]
        public struct Vertex
        {
            public Vector3 position;
            public Vector3 normal;
            public Vector2 textureuv;
        }

        public readonly struct GeometryData
        {
            public readonly Vertex[] vertices;
            public readonly ushort[] indices;

            public GeometryData(Vertex[] vertices, ushort[] indices)
            {
                this.vertices = vertices;
                this.indices = indices;
            }
        }

        public static Vector3 Sphere(float u, float v, float r = 1f)
        {
            float uu = u * 2 * (float)Math.PI;
            float vv = v * (float)Math.PI;

            float cu = (float)Math.Cos(uu), su = (float)Math.Sin(uu), cv = (float)Math.Cos(vv), sv = (float)Math.Sin(vv);

            return new Vector3(r * cu * sv, r * cv, r * su * sv);
        }

        public static Vector3 Torus(float u, float v, float r = 1f, float R = 2f)
        {
            float uu = u * 2 * (float)Math.PI;
            float vv = v * 2 * (float)Math.PI;

            float cu = (float)Math.Cos(uu), su = (float)Math.Sin(uu), cv = (float)Math.Cos(vv), sv = (float)Math.Sin(vv);

            return new Vector3((R + r * cu) * cv, r * su, (R + r * cu) * sv);
        }

        public static Vector3 Cylinder(float u, float v, float r = 1f, float h = 1f)
        {
            float uu = -u * 2 * (float)Math.PI;
            float vv = v * h;

            float cu = (float)Math.Cos(uu), su = (float)Math.Sin(uu);

            return new Vector3(r * cu, vv, r * su);
        }


        public static Vector3 Normal(float u, float v, Func<float, float, float, Vector3> parametricEquation, float arg1 = 1f)
        {
            return Vector3.Cross(
                parametricEquation(u + 0.01f, v, arg1) - parametricEquation(u - 0.01f, v, arg1),
                parametricEquation(u, v + 0.01f, arg1) - parametricEquation(u, v - 0.01f, arg1));
        }
        public static Vector3 Normal(float u, float v, Func<float, float, float, float, Vector3> parametricEquation, float arg1 = 1f, float arg2 = 1f)
        {
            return Vector3.Cross(
                parametricEquation(u + 0.01f, v, arg1, arg2) - parametricEquation(u - 0.01f, v, arg1, arg2),
                parametricEquation(u, v + 0.01f, arg1, arg2) - parametricEquation(u, v - 0.01f, arg1, arg2));
        }
        public static Vector2 TextureUV(float u, float v) => new Vector2(u, v);


        public static GeometryData Generate(Func<float, float, float, Vector3> parametricEquation, float arg1 = 1f, int n = 20)
        {
            GeometryData result =
                new GeometryData(new Vertex[(n + 1) * (n + 1)], new ushort[3 * 2 * n * n]);

            for (int i = 0; i <= n; i++)
                for (int j = 0; j <= n; j++)
                {
                    float u = i / (float)n;
                    float v = j / (float)n;

                    result.vertices[i + j * (n + 1)].position = parametricEquation(u, v, arg1);
                    result.vertices[i + j * (n + 1)].normal = Normal(u, v, parametricEquation, arg1); //vertices[i + j * (N + 1)].position;
                    result.vertices[i + j * (n + 1)].textureuv = TextureUV(u, v);
                }

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    result.indices[6 * i + j * 3 * 2 * n + 0] = (ushort)((i) + (j) * (n + 1));
                    result.indices[6 * i + j * 3 * 2 * n + 1] = (ushort)((i + 1) + (j) * (n + 1));
                    result.indices[6 * i + j * 3 * 2 * n + 2] = (ushort)((i) + (j + 1) * (n + 1));
                    result.indices[6 * i + j * 3 * 2 * n + 3] = (ushort)((i + 1) + (j) * (n + 1));
                    result.indices[6 * i + j * 3 * 2 * n + 4] = (ushort)((i + 1) + (j + 1) * (n + 1));
                    result.indices[6 * i + j * 3 * 2 * n + 5] = (ushort)((i) + (j + 1) * (n + 1));
                }

            return result;
        }
        public static GeometryData Generate(Func<float, float, float, float, Vector3> parametricEquation, float arg1 = 1f, float arg2 = 1f, int n = 20)
        {
            GeometryData result =
                new GeometryData(new Vertex[(n + 1) * (n + 1)], new ushort[3 * 2 * n * n]);

            for (int i = 0; i <= n; i++)
                for (int j = 0; j <= n; j++)
                {
                    float u = i / (float)n;
                    float v = j / (float)n;

                    result.vertices[i + j * (n + 1)].position = parametricEquation(u, v, arg1, arg2);
                    result.vertices[i + j * (n + 1)].normal = Normal(u, v, parametricEquation, arg1, arg2); //vertices[i + j * (N + 1)].position;
                    result.vertices[i + j * (n + 1)].textureuv = TextureUV(u, v);
                }

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    result.indices[6 * i + j * 3 * 2 * n + 0] = (ushort)((i) + (j) * (n + 1));
                    result.indices[6 * i + j * 3 * 2 * n + 1] = (ushort)((i + 1) + (j) * (n + 1));
                    result.indices[6 * i + j * 3 * 2 * n + 2] = (ushort)((i) + (j + 1) * (n + 1));
                    result.indices[6 * i + j * 3 * 2 * n + 3] = (ushort)((i + 1) + (j) * (n + 1));
                    result.indices[6 * i + j * 3 * 2 * n + 4] = (ushort)((i + 1) + (j + 1) * (n + 1));
                    result.indices[6 * i + j * 3 * 2 * n + 5] = (ushort)((i) + (j + 1) * (n + 1));
                }

            return result;
        }

    }
}
