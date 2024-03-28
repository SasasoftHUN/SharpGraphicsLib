using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using SharpGraphics.Shaders;

namespace SharpGraphics.Apps.HelloTriangle
{
    [VertexShader]
    public partial class HelloVertexShader : VertexShaderBase
    {

        [StageIn] private uint vID;

        [StageOut] private Vector4 vPosition;

        public override void Main()
        {
            Vector2[] positions = new Vector2[] { new Vector2(-0.7f, 0.7f), new Vector2(0f, -0.7f), new Vector2(0.7f, 0.7f) };
            Vector2 pos = positions[vID];
            vPosition = new Vector4(pos, 0.0f, 1.0f);
        }
    }
}
