using SharpGraphics.Shaders;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Apps.VertexAttributes
{
    [VertexShader]
    public partial class PassthroughVertexShader : VertexShaderBase
    {

        [In] private readonly Vector4 vs_in_pos;
        [In] private readonly Vector4 vs_in_col;

        [StageOut(Name = "vPosition")] private Vector4 vertexPosition;
        [Out] private Vector4 vs_out_col;

        public override void Main()
        {
            vertexPosition = vs_in_pos;
            vs_out_col = vs_in_col;
        }


    }
}
