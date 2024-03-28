using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Validators.GLSLangValidator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Builders
{
    internal class GLSL450ShaderBuilder : GLSL460ShaderBuilder
    {

        #region Properties

        public override string BackendName { get => "GLSL450"; }

        #endregion

        #region Constructors

        public GLSL450ShaderBuilder(ShaderLanguageMappings mappings) : base(mappings) { }
        public GLSL450ShaderBuilder(ShaderLanguageMappings mappings, IGLSLangValidator glslangValidator) : base(mappings, glslangValidator) { }

        #endregion

        #region Protected Methods

        protected override void BuildPreprocessorVersion() => _sb.AppendLine("#version 450 core");

        #endregion

    }
}
