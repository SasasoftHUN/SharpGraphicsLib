using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Validators.GLSLangValidator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Builders
{
    internal class GLSL430ShaderBuilder : GLSL440ShaderBuilder
    {

        #region Properties

        public override string BackendName { get => "GLSL430"; }

        #endregion

        #region Constructors

        public GLSL430ShaderBuilder(ShaderLanguageMappings mappings) : base(mappings) { }
        public GLSL430ShaderBuilder(ShaderLanguageMappings mappings, IGLSLangValidator glslangValidator) : base(mappings, glslangValidator) { }

        #endregion

        #region Protected Methods

        protected override void BuildPreprocessorVersion() => _sb.AppendLine("#version 430 core");

        #endregion

    }
}
