using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGraphics.Shaders.Generator;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Validators.GLSLangValidator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Builders
{
    internal class GLSL440ShaderBuilder : GLSL450ShaderBuilder
    {

        #region Properties

        public override string BackendName { get => "GLSL440"; }

        #endregion

        #region Constructors

        public GLSL440ShaderBuilder(ShaderLanguageMappings mappings) : base(mappings) { }
        public GLSL440ShaderBuilder(ShaderLanguageMappings mappings, IGLSLangValidator glslangValidator) : base(mappings, glslangValidator) { }

        #endregion

        #region Protected Methods

        protected override void BuildPreprocessorVersion() => _sb.AppendLine("#version 440 core");

        #endregion

    }
}
