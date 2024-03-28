using System;
using System.Collections.Generic;
using System.Text;
using SharpGraphics.Shaders;
using System.Numerics;

namespace SharpGraphics.Apps.Models
{
    [FragmentShader]
    public partial class SkyboxFragmentShader : FragmentShaderBase
    {

        [In] private readonly Vector3 vs_out_pos;

        [Out] private Vector4 fs_out_pos;

        [Uniform(Set = 1u, Binding = 0u, UniqueBinding = 4u)] private TextureSamplerCube<Vector4> skyboxTexture;

        public override void Main()
        {
            fs_out_pos = skyboxTexture.Sample(vs_out_pos);
        }

    }
}
