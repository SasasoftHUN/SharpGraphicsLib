using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Apps.SimpleRaytrace
{
    [VertexShader]
    public partial class RaytraceVertexShader : VertexShaderBase
    {

        private Vector4[] positions = new Vector4[]
        {
            new Vector4(-1f, -1f, 0f, 1f),
            new Vector4( 1f, -1f, 0f, 1f),
            new Vector4(-1f,  1f, 0f, 1f),
            new Vector4( 1f,  1f, 0f, 1f),
        };

        [StageIn] private uint vID;

        [Out][StageOut(Name = "vPosition")] private Vector4 vs_out_pos;

        public override void Main()
        {
            vs_out_pos = positions[vID];
        }

    }
}
