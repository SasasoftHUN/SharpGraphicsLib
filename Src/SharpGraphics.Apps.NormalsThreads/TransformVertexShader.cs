using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Apps.NormalsThreads
{
    [VertexShader]
    public partial class TransformVertexShader : VertexShaderBase
    {

        [In] private readonly Vector3 vs_in_pos;
        [In] private readonly Vector3 vs_in_norm;
        [In] private readonly Vector2 vs_in_tex;

        [StageOut] private Vector4 vPosition;
        [Out] private Vector3 vs_out_pos;
        [Out] private Vector3 vs_out_norm;
        [Out] private Vector2 vs_out_tex;

        [Uniform(Set = 0u, Binding = 0u)] private Transform transform;

        public override void Main()
        {
            vPosition = Vector4.Transform(new Vector4(vs_in_pos, 1f), transform.mvp);
            vs_out_pos = Vector4.Transform(new Vector4(vs_in_pos, 1f), transform.world).XYZ();
            vs_out_norm = Vector4.Transform(new Vector4(vs_in_norm, 0f), transform.worldIT).XYZ();
            vs_out_tex = vs_in_tex;
        }


    }
}
