using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Shaders
{
    public readonly struct TextureSampler2D<T> where T : unmanaged
    {

        public Vector2Int GetSize(int lod) => Vector2Int.Zero;
        //public Vector2 QueryLod(Vector2 uv) => Vector2.Zero; //GLSL ES do not support it, neither QueryLevels or textureSamplers...

        public T Sample(Vector2 uv) => default(T);
        //public T SampleProjected(Vector3 projection) => default(T);
        //public T SampleProjected(Vector4 projection) => default(T);
        public T Fetch(Vector2Int coordinates, int lod) => default(T);

    }
    public readonly struct TextureSamplerCube<T> where T : unmanaged
    {
        public Vector2Int GetSize(int lod) => Vector2Int.Zero;
        //public Vector2 QueryLod(Vector3 direction) => Vector2.Zero; //GLSL ES do not support it

        public T Sample(Vector3 direction) => default(T);

    }
}
