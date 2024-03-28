using SharpGraphics.Shaders;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Apps.QuadPlayground
{
    [FragmentShader]
    public partial class KeepRightFragmentShader : FragmentShaderBase
    {

        [In] private readonly Vector4 vs_out_pos;
        [In] private readonly Vector4 vs_out_col;

        [Out] private Vector4 fs_out_col;

        public override void Main()
        {
            if (vs_out_pos.X >= 0f)
                fs_out_col = vs_out_col;
            else Discard();
            //else fs_out_col = new Vector4(0f);
        }

    }
}
