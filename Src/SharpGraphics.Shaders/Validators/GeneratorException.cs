using SharpGraphics.Shaders.Generator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Validators
{
    internal class GenerationException : Exception
    {
        public ShaderGenerationDiagnositcs Diagnositcs { get; private set; }

        public GenerationException(ShaderGenerationDiagnositcs diagnositcs) : base(diagnositcs.Message)
            => Diagnositcs = diagnositcs;
    }
}
