using SharpGraphics.Shaders.Generator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Validators.GLSLangValidator
{
    internal interface IGLSLangValidator : IValidator
    {

        public enum SPIRVTargetType
        {
            Environment, SPIRV,
        }
        public readonly struct SPIRVTarget
        {
            public readonly SPIRVTargetType type;
            public readonly string target;

            public SPIRVTarget(SPIRVTargetType type, string target)
            {
                this.type = type;
                this.target = target;
            }
        }

        byte[] CompileGLSLToSPIRV(ShaderClassDeclaration shaderClass, string shaderSource, in SPIRVTarget target);
        bool ValidateShader(ShaderClassDeclaration shaderClass, string shaderSource);

    }
}
