using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGraphics.Shaders.Generator;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Validators.GLSLangValidator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGraphics.Shaders.Builders
{
    internal class GLSLES300ShaderBuilder : GLSL330ShaderBuilder
    {

        #region Properties

        public override string BackendName { get => "GLSLES300"; }

        #endregion

        #region Constructors

        public GLSLES300ShaderBuilder(ShaderLanguageMappings mappings) : base(mappings) { }
        public GLSLES300ShaderBuilder(ShaderLanguageMappings mappings, IGLSLangValidator glslangValidator) : base(mappings, glslangValidator) { }

        #endregion

        #region Protected Methods

        protected override void BuildPreprocessorVersion() => _sb.AppendLine("#version 300 es");

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
