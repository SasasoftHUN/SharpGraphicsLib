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
    internal class GLSL330ShaderBuilder : GLSL420ShaderBuilder
    {

        protected readonly struct UniformArrayData
        {
            public readonly string type;
            public readonly string name;
            public readonly string originalName;
            public readonly int count;

            public UniformArrayData(string type, string name, string originalName, int count)
            {
                this.type = type;
                this.name = name;
                this.originalName = originalName;
                this.count = count;
            }
        }

        #region Fields

        protected int _vertexShaderInLocation = 0;

        //protected List<UniformArrayData> _uniformArrays = new List<UniformArrayData>(); //Array of Uniform Buffers

        #endregion

        #region Properties

        public override string BackendName { get => "GLSL330"; }

        #endregion

        #region Constructors

        public GLSL330ShaderBuilder(ShaderLanguageMappings mappings) : base(mappings) { }
        public GLSL330ShaderBuilder(ShaderLanguageMappings mappings, IGLSLangValidator glslangValidator) : base(mappings, glslangValidator) { }

        #endregion

        #region Protected Methods

        protected override void BuildPreprocessorVersion() => _sb.AppendLine("#version 330 core");

        protected override void BuildExtensions() { }

        protected override void BuildVariableLayout(ShaderVariableDeclaration variable, string type)
        {
            switch (variable)
            {
                case ShaderOutVariableDeclaration outVariable:
                    if (_shaderClass is FragmentShaderClassDeclaration)
                        BuildVariableLocation(outVariable, type, ref _outLocation);
                    break;
                case ShaderUniformVariableDeclaration uniformVariable: BuildUniformVariableLocation(uniformVariable, type); break;
            }
        }
        //protected override void BuildVariableLocation(ShaderVariableDeclaration variable, string type, ref int location) { }
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
                    break;

                default:
                    _sb.Append($"layout(std140) ");
                    break;
            }
        }
        protected override bool TryBuildVariableDeclaration(ShaderVariableDeclaration variable, TypeSyntax typeSyntax, string type, string name)
        {
            string nameOverride = name;

            switch (variable)
            {
                case ShaderInVariableDeclaration inVariable:
                    if (_shaderClass is VertexShaderClassDeclaration vertexShaderClass)
                    {
                        nameOverride = $"vs_in_{_vertexShaderInLocation++}";
                        AddVariableNameOverride(name, nameOverride);
                    }
                    break;
            }

            return base.TryBuildVariableDeclaration(variable, typeSyntax, type, nameOverride);
        }

        /*protected override void BuildVariables(ShaderClassDeclaration shaderClass)
        {
            base.BuildVariables(shaderClass);

            foreach (UniformArrayData uniformArray in _uniformArrays)
                if (TryGenerateStruct(uniformArray.type, out UserStructShaderType? structDeclaration))
                {
                    _sb.AppendIndentation(uniformArray.type, _indentation);
                    _sb.Append(' ');
                    _sb.Append(uniformArray.name);
                    _sb.Append("_local[");
                    _sb.Append(uniformArray.count);
                    _sb.Append("];");
                }

            if (_uniformArrays.Count > 0)
                _sb.AppendLineIndentation(_indentation);
        }

        protected override void BuildMainMethodBodyPrefixStatements()
        {
            foreach (UniformArrayData uniformArray in _uniformArrays) //TODO: If it's not a struct? Just a float[]?
                if (TryGetGeneratedStructDeclaration(uniformArray.type, out StructDeclarationSyntax? structDeclaration) && structDeclaration != null)
                {
                    string nameOverride = uniformArray.name + "_local";
                    _sb.AppendIndentation(nameOverride, _indentation);
                    _sb.Append(" = ");
                    _sb.Append(uniformArray.type);
                    _sb.Append('[');
                    _sb.Append(uniformArray.count);
                    _sb.AppendLine("](");

                    ++_indentation;
                    for (int i = 0; i < uniformArray.count; i++)
                    {
                        _sb.AppendIndentation(uniformArray.type, _indentation);
                        _sb.Append('(');

                        IEnumerable<FieldDeclarationSyntax> fields = structDeclaration.Members.OfType<FieldDeclarationSyntax>();
                        foreach (FieldDeclarationSyntax fieldDeclaration in fields)
                        {
                            bool isLastFieldDec = fieldDeclaration == fields.Last();
                            IEnumerable<VariableDeclaratorSyntax> vars = fieldDeclaration.Declaration.Variables;
                            foreach (VariableDeclaratorSyntax varDec in vars)
                            {
                                _sb.Append(uniformArray.name);
                                _sb.Append('[');
                                _sb.Append(i);
                                _sb.Append("].");
                                _sb.Append(GetVariableName(varDec.Identifier.ValueText, fieldDeclaration.Declaration.Type));

                                if (!isLastFieldDec || varDec != vars.Last())
                                    _sb.Append(", ");
                            }
                        }

                        if (i + 1 < uniformArray.count)
                            _sb.AppendLine("),");
                        else _sb.AppendLine(')');
                    }
                    --_indentation;

                    _sb.AppendLineIndentation(");", _indentation);
                    AddVariableNameOverride(uniformArray.originalName, nameOverride);
                }

            if (_uniformArrays.Count > 0)
                _sb.AppendLineIndentation(_indentation);

            base.BuildMainMethodBodyPrefixStatements();
        }*/

        #endregion

    }
}
