using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Apps.QuadPlayground
{
    [FragmentShader]
    public partial class MandelbrotFragmentShader : FragmentShaderBase
    {

        [In] private readonly Vector4 vs_out_pos;
        [In] private readonly Vector4 vs_out_col;

        [Out] private Vector4 fs_out_col;

        private Vector2 ComplexMultiplication(Vector2 u, Vector2 v)
            => new Vector2(
                u.X * v.X - u.Y * v.Y,
                u.X * v.Y + u.Y * v.X);


        public override void Main()
        {
            Vector2 z = vs_out_pos.XY();
            Vector2 c = z;

            for (int i = 0; i < 30; i++)
            {
                z = ComplexMultiplication(z, z) + c;
                //z = new Vector2(z.X * z.X - z.Y * z.Y, z.X * z.Y + z.Y * z.X) + c;

                if (z.Length() >= 2f)
                    fs_out_col = new Vector4((float)i / 30f, 0f, 0f, 0f);
            }

            if (z.Length() < 2f)
                fs_out_col = new Vector4(1f, 0f, 0f, 0f);
        }

    }
}
