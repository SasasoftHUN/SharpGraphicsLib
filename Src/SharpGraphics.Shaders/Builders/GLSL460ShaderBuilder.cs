using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGraphics.Shaders.Generator;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Validators.GLSLangValidator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Builders
{
    internal class GLSL460ShaderBuilder : GLSLShaderBuilderBase
    {

        #region Fields

        protected int _inLocation = 0;
        protected int _outLocation = 0;

        #endregion

        #region Properties

        public override string BackendName { get => "GLSL460"; }

        #endregion

        #region Constructors

        public GLSL460ShaderBuilder(ShaderLanguageMappings mappings) : base(mappings) { }
        public GLSL460ShaderBuilder(ShaderLanguageMappings mappings, IGLSLangValidator glslangValidator) : base(mappings, glslangValidator) { }

        #endregion

        #region Protected Methods

        protected override void BuildPreprocessorVersion() => _sb.AppendLine("#version 460 core");

        protected virtual void BuildVariableLayout(ShaderVariableDeclaration variable, string type)
        {
            switch (variable)
            {
                case ShaderInVariableDeclaration inVariable: BuildVariableLocation(inVariable, type, ref _inLocation); break;
                case ShaderOutVariableDeclaration outVariable: BuildVariableLocation(outVariable, type, ref _outLocation); break;
                case ShaderUniformVariableDeclaration uniformVariable: BuildUniformVariableLocation(uniformVariable, type); break;
            }
        }
        protected virtual void BuildVariableLocation(ShaderVariableDeclaration variable, string type, ref int location)
        {
            _sb.Append("layout(location = ");
            _sb.Append(location);
            _sb.Append(") ");

            switch (type)
            {
                case "mat4": location += 4; break;
                //TODO: Calculate struct location
                default: ++location; break;
            }
        }
        protected virtual void BuildUniformVariableLocation(ShaderUniformVariableDeclaration uniformVariable, string type)
        {
            switch (type)
            {
                case "sampler2D":
                case "isampler2D":
                case "usampler2D":
                case "samplerCube":
                case "isamplerCube":
                case "usamplerCube":
                    _sb.Append($"layout(location={uniformVariable.UniqueBinding}) ");
                    break;

                default:
                    _sb.Append($"layout(std140, binding={uniformVariable.UniqueBinding}) ");
                    break;
            }
        }

        protected override bool TryBuildVariableDeclaration(ShaderVariableDeclaration variable, TypeSyntax typeSyntax, string type, string name)
        {
            BuildVariableLayout(variable, type);
            return base.TryBuildVariableDeclaration(variable, typeSyntax, type, name);
        }


        protected override void BuildEntryMethodBodyPostfixStatements()
        {
            base.BuildEntryMethodBodyPostfixStatements();

            foreach (KeyValuePair<string, string> copyVariables in _variableCopyAtEnd)
            {
                _sb.AppendIndentation(copyVariables.Value, _indentation);
                _sb.Append(" = ");
                _sb.Append(copyVariables.Key);
                _sb.AppendLine(';');
            }
        }

        #endregion

    }
}
