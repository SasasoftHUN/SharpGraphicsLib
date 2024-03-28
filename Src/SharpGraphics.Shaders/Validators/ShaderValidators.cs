using SharpGraphics.Shaders.Validators.DirectXShaderCompiler;
using SharpGraphics.Shaders.Validators.GLSLangValidator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Validators
{
    public class ShaderValidators
    {

        internal IGLSLangValidator GLSLangValidator { get; }
        internal IDirectXShaderCompiler DirectXShaderCompiler { get; }

        public ShaderValidators()
        {
            StandaloneValidator.PrepareTempFolders(out string tempFolderPath, out string sessionTempFolderPath);
            GLSLangValidator = new GLSLangValidatorStandalone(tempFolderPath, sessionTempFolderPath);
            //DirectXShaderCompiler = new DirectXShaderCompilerStandalone(tempFolderPath, sessionTempFolderPath);
            DirectXShaderCompiler = new DirectXShaderCompilerDummy();
        }

    }
}
