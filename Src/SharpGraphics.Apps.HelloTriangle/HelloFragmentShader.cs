using SharpGraphics.Shaders;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Apps.HelloTriangle
{
    [FragmentShader]
    public partial class HelloFragmentShader : FragmentShaderBase
    {

        [Out] private Vector4 fs_out_col;

        public override void Main()
        {
            fs_out_col = new Vector4(0f, 0.4f, 1f, 1f);
        }

    }
}
