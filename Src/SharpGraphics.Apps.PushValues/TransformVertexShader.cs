using SharpGraphics.Shaders;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Apps.PushValues
{
    [VertexShader]
    public partial class TransformVertexShader : VertexShaderBase
    {

        [In] private readonly Vector3 vs_in_pos;
        [In] private readonly Vector3 vs_in_col;

        [StageOut] private Vector4 vPosition;
        [Out] private Vector3 vs_out_col;

        [Uniform(Set = 0u, Binding = 0u)] private Transform transform;

        public override void Main()
        {
            vPosition = Vector4.Transform(new Vector4(vs_in_pos, 1f), transform.mvp);
            //vPosition = new Vector4(vs_in_pos, 1f);
            vs_out_col = vs_in_col;
        }


    }
}
