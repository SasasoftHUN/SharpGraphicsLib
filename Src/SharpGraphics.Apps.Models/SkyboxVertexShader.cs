using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Apps.Models
{
    [VertexShader]
    public partial class SkyboxVertexShader : VertexShaderBase
    {

        [In] private readonly Vector3 vs_in_pos;

        [StageOut] private Vector4 vPosition;
        [Out] private Vector3 vs_out_pos;

        [Uniform(Set = 0u, Binding = 0u, UniqueBinding = 2u)] private Transform transform;

        public override void Main()
        {
            vPosition = Vector4.Transform(new Vector4(vs_in_pos, 1f), transform.mvp).XYWW();
            vs_out_pos = vs_in_pos;
        }


    }
}
