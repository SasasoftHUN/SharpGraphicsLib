using SharpGraphics.Shaders;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Apps.QuadPlayground
{
    [VertexShader]
    public partial class PassthroughVertexShader : VertexShaderBase
    {

        [In] private readonly Vector4 vs_in_pos;
        [In] private readonly Vector4 vs_in_col;

        [Out][StageOut(Name = "vPosition")] private Vector4 vs_out_pos;
        [Out] private Vector4 vs_out_col;

        public override void Main()
        {
            vs_out_pos = vs_in_pos;
            vs_out_col = vs_in_col;
        }


    }
}
