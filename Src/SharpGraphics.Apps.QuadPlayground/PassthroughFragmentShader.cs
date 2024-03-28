using SharpGraphics.Shaders;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Apps.QuadPlayground
{
    [FragmentShader]
    public partial class PassthroughFragmentShader : FragmentShaderBase
    {

        [In] private readonly Vector4 vs_in_pos;
        [In] private readonly Vector4 vs_out_col;

        [Out] private Vector4 fs_out_col;

        public override void Main()
        {
            fs_out_col = vs_out_col;
        }

    }
}
