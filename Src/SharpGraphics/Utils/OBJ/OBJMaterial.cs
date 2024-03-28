using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Utils.OBJ
{
    public class OBJMaterial
    {

        public string? Name { get; internal set; }

        public Vector3 Ka { get; internal set; } = Vector3.One;
        public Vector3 Kd { get; internal set; } = Vector3.One;
        public Vector3 Ks { get; internal set; } = Vector3.One;
        public float Ns { get; internal set; } = 16f; //Specular Exponent
        
        public string? MapKa { get; internal set; }
        public string? MapKd { get; internal set; }
        public string? MapKs { get; internal set; }
        public string? MapDisp { get; internal set; }
        public string? MapD { get; internal set; }

        public bool IsBlended => !string.IsNullOrWhiteSpace(MapD);

    }
}
