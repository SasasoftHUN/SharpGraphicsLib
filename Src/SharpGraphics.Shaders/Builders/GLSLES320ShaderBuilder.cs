using SharpGraphics.Shaders.Generator;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Validators.GLSLangValidator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Builders
{
    internal class GLSLES320ShaderBuilder : GLSL460ShaderBuilder
    {

        #region Properties

        public override string BackendName { get => "GLSLES320"; }

        #endregion

        #region Constructors

        public GLSLES320ShaderBuilder(ShaderLanguageMappings mappings) : base(mappings) { }
        public GLSLES320ShaderBuilder(ShaderLanguageMappings mappings, IGLSLangValidator glslangValidator) : base(mappings, glslangValidator) { }

        #endregion

        #region Protected Methods

        protected override void BuildPreprocessorVersion() => _sb.AppendLine("#version 320 es");
        protected override void BuildExtensions()
        {
            if (_shaderClass is FragmentShaderClassDeclaration fragmentShaderClass)
            {
                _sb.AppendLine(@"#ifdef GL_FRAGMENT_PRECISION_HIGH 
    precision highp float;
    precision highp int;
    precision highp sampler2D;
    precision highp samplerCube;
    precision highp isampler2D;
    precision highp isamplerCube;
    precision highp usampler2D;
    precision highp usamplerCube;
#else                             
    precision mediump float;
    precision mediump int;
    precision mediump sampler2D;
    precision mediump samplerCube;
    precision mediump isampler2D;
    precision mediump isamplerCube;
    precision mediump usampler2D;
    precision mediump usamplerCube;
#endif");
            }
        }

        #endregion

    }
}
