using SharpGraphics.Shaders;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Apps.PushValues
{
    [FragmentShader]
    public partial class PassthroughFragmentShader : FragmentShaderBase
    {

        [In] private readonly Vector3 vs_out_col;

        [Out] private Vector4 fs_out_col;

        public override void Main()
        {
            fs_out_col = new Vector4(vs_out_col, 1f);
        }

    }
}
