using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static SharpGraphics.Apps.Deferred.DeferredApp;

namespace SharpGraphics.Apps.Deferred
{
    [FragmentShader]
    public partial class GPassFragmentShader : FragmentShaderBase
    {

        [In] private readonly Vector4 vs_out_pos;
        [In] private readonly Vector3 vs_out_norm;
        [In] private readonly Vector2 vs_out_tex;

        [Out] private Vector4 fs_out_position;
        [Out] private Vector4 fs_out_diffuse;
        [Out] private Vector4 fs_out_normal;

        [Uniform(Set = 0u, Binding = 1u)] private TextureSampler2D<Vector4> textureSampler;

        public override void Main()
        {
            fs_out_position = new Vector4(vs_out_pos.XYZ() / vs_out_pos.W, 1f);
            fs_out_diffuse = new Vector4(textureSampler.Sample(vs_out_tex).XYZ(), 1f);
            fs_out_normal = new Vector4(Vector3.Normalize(vs_out_norm), 0f);
        }

    }
}
