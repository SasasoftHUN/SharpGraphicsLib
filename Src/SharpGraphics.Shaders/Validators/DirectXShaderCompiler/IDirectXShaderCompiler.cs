using SharpGraphics.Shaders.Generator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Validators.DirectXShaderCompiler
{
    internal interface IDirectXShaderCompiler : IValidator
    {
        bool ValidateShader(ShaderClassDeclaration shaderClass, string shaderSource, string targetShaderModel);
    }
}
