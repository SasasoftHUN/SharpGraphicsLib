using SharpGraphics.Shaders.Generator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Validators.DirectXShaderCompiler
{
    internal class DirectXShaderCompilerDummy : IDirectXShaderCompiler
    {
        public bool CanExecute => false;
        public bool ValidateShader(ShaderClassDeclaration shaderClass, string shaderSource, string targetShaderModel) => false;
    }
}
