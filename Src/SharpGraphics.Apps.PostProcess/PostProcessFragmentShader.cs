using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Apps.PostProcess
{
    [FragmentShader]
    public partial class PostProcessFragmentShader : FragmentShaderBase
    {

        [Out] private Vector4 fs_out_col;

        [Uniform(Set = 0u, Binding = 0u)][InputAttachmentIndex(Index = 0)] private RenderPassInput<Vector4> colorBuffer;

        public override void Main()
        {
            Vector3 color = colorBuffer.Load().XYZ();
            float bw = (color.X + color.Y + color.Z) * 0.33f;
            fs_out_col = new Vector4(bw, bw, bw, 1f);
        }
    }
}
