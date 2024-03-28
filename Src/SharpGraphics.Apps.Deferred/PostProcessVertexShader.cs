using SharpGraphics.Shaders;
using SharpGraphics.Shaders.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Apps.Deferred
{
    [VertexShader]
    public partial class PostProcessVertexShader : VertexShaderBase
    {

        private Vector4[] positions = new Vector4[]
        {
            new Vector4(-1f, -1f, 0f, 1f),
            new Vector4( 1f, -1f, 0f, 1f),
            new Vector4(-1f,  1f, 0f, 1f),
            new Vector4( 1f,  1f, 0f, 1f),
        };

        [StageIn] private uint vID;

        [StageOut] private Vector4 vPosition;

        public override void Main()
        {
            vPosition = positions[vID];
        }

    }
}
