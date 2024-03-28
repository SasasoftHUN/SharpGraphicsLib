using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGraphics.Shaders.Generator;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Validators.GLSLangValidator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Builders
{
    internal class GLSL420ShaderBuilder : GLSL430ShaderBuilder
    {

        #region Properties

        public override string BackendName { get => "GLSL420"; }

        #endregion

        #region Constructors

        public GLSL420ShaderBuilder(ShaderLanguageMappings mappings) : base(mappings) { }
        public GLSL420ShaderBuilder(ShaderLanguageMappings mappings, IGLSLangValidator glslangValidator) : base(mappings, glslangValidator) { }

        #endregion

        #region Protected Methods

        protected override void BuildPreprocessorVersion() => _sb.AppendLine("#version 420 core");
        protected override void BuildExtensions()
        {
            base.BuildExtensions();
            _sb.AppendLine("#extension GL_ARB_explicit_uniform_location: enable");
        }

        protected override void BuildUniformVariableLocation(ShaderUniformVariableDeclaration uniformVariable, string type)
        {
            switch (type)
            {
                case "sampler2D":
                case "isampler2D":
                case "usampler2D":
                case "samplerCube":
                case "isamplerCube":
                case "usamplerCube":
                    _sb.AppendLine("#ifdef GL_ARB_explicit_uniform_location");
                    _sb.AppendLine($"layout(location={uniformVariable.UniqueBinding})");
                    _sb.AppendLine("#endif //GL_ARB_explicit_uniform_location");
                    break;

                default:
                    _sb.AppendLine("layout(std140");
                    _sb.AppendLine("#ifdef GL_ARB_explicit_uniform_location");
                    _sb.AppendLine($", binding={uniformVariable.UniqueBinding}");
                    _sb.AppendLine("#endif //GL_ARB_explicit_uniform_location");
                    _sb.Append(") ");
                    break;
            }
        }
        protected override bool TryBuildVariableDeclaration(ShaderVariableDeclaration variable, TypeSyntax typeSyntax, string type, string name)
        {
            string nameOverride = name;

            switch (variable)
            {
                case ShaderUniformVariableDeclaration uniformVariable:
                    switch (type)
                    {
                        case "sampler2D":
                        case "isampler2D":
                        case "usampler2D":
                        case "samplerCube":
                        case "isamplerCube":
                        case "usamplerCube":
                            nameOverride = $"uniformsampler_{uniformVariable.UniqueBinding}"; break;

                        default: nameOverride = $"uniformblock_{uniformVariable.UniqueBinding}"; break;
                    }
                    AddVariableNameOverride(name, nameOverride);
                    /*if (typeSyntax.IsKind(SyntaxKind.ArrayType) && TryGetArrayCount(variable, out int arrayCount)) //This is for Array of Uniform Buffers
                        _uniformArrays.Add(new UniformArrayData(type, nameOverride, name, arrayCount));*/
                    break;
            }

            return base.TryBuildVariableDeclaration(variable, typeSyntax, type, nameOverride);
        }

        #endregion

    }
}
