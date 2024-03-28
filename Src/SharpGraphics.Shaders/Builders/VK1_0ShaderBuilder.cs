using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGraphics.Shaders.Generator;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Validators.GLSLangValidator;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SharpGraphics.Shaders.Builders
{

    internal class VKSPIRV1_0ShaderBuilder : SPIRVShaderBuilderBase<VK1_0GLSLShaderBuilder>
    {
        protected override IGLSLangValidator.SPIRVTarget SPIRVTarget => new IGLSLangValidator.SPIRVTarget(IGLSLangValidator.SPIRVTargetType.Environment, "vulkan1.0");
        public override string BackendName => "SPIRV_VK1_0";

        public VKSPIRV1_0ShaderBuilder(ShaderLanguageMappings mappings, IGLSLangValidator glslangValidator) : base(new VK1_0GLSLShaderBuilder(mappings), glslangValidator) { }
    }

    internal class VK1_0GLSLShaderBuilder : GLSL460ShaderBuilder
    {

        #region Properties

        public override string BackendName => "GLSL_VK1_0";

        #endregion

        #region Constructors

        public VK1_0GLSLShaderBuilder(ShaderLanguageMappings mappings) : base(mappings) { }
        public VK1_0GLSLShaderBuilder(ShaderLanguageMappings mappings, IGLSLangValidator glslangValidator) : base(mappings, glslangValidator) { }

        #endregion

        #region Protected Methods

        protected bool TryGetInputAttachmentIndex(ShaderVariableDeclaration variable, out int index)
        {
            if (_compilation != null &&
                variable.Field.TryGetAttributeData(ShaderGenerator.InputAttachmentIndexAttributeFullName, _compilation, _cancellationToken, out AttributeData? attributeData) &&
                attributeData.TryGetAttributePropertyValueInt("Index", out index))
                return true;

            index = 0;
            return false;
        }


        protected override bool TryGetBuiltInTypeName(TextureSamplerType shaderType, [NotNullWhen(returnValue: true)] out string? name)
        {
            string samplerType;
            switch (shaderType.Dimensions)
            {
                case TextureSamplerDimensions.RenderPassInput: samplerType = "subpassInput"; break;
                default: return base.TryGetBuiltInTypeName(shaderType, out name);
            }

            switch (shaderType.Type)
            {
                case PrimitiveShaderType primitiveShaderType:
                    switch (primitiveShaderType.Type)
                    {
                        case PrimitiveShaderTypes.Int: name = $"i{samplerType}"; return true;
                        case PrimitiveShaderTypes.UInt: name = $"u{samplerType}"; return true;
                        case PrimitiveShaderTypes.Float: name = samplerType; return true;
                        default:
                            name = null;
                            return false;
                    }
                case VectorShaderType vectorShaderType:
                    switch (vectorShaderType.ComponentType.Type)
                    {
                        case PrimitiveShaderTypes.Int: name = $"i{samplerType}"; return true;
                        case PrimitiveShaderTypes.UInt: name = $"u{samplerType}"; return true;
                        case PrimitiveShaderTypes.Float: name = samplerType; return true;
                        default:
                            name = null;
                            return false;
                    }
                default:
                    name = null;
                    return false;
            }
        }

        protected override bool TryGetBuiltInVariableName(string name, [NotNullWhen(returnValue: true)] out string? builtInName)
        {
            switch (name)
            {
                case "vID": builtInName = "gl_VertexIndex"; return true;

                default: return base.TryGetBuiltInVariableName(name, out builtInName);
            }
        }
        protected override bool TryGetBuiltInFunctionName(ShaderFunctionExpression function, [NotNullWhen(returnValue: true)] out string? name, out ArgumentRemap[]? remap)
        {
            switch (function.FunctionName)
            {
                case "subpassFetch": name = "subpassLoad"; remap = null; return true;

                default: return base.TryGetBuiltInFunctionName(function, out name, out remap);
            }
            
        }

        protected override void BuildVariableLayout(ShaderVariableDeclaration variable, string type)
        {
            switch (variable)
            {
                case ShaderUniformVariableDeclaration uniformVariable: BuildUniformVariableLocation(uniformVariable, type); break;

                default: base.BuildVariableLayout(variable, type); break;
            }
        }
        protected override void BuildUniformVariableLocation(ShaderUniformVariableDeclaration uniformVariable, string type)
        {
            _sb.Append("layout(");

            switch (type)
            {
                case "sampler2D":
                case "isampler2D":
                case "usampler2D":
                    break;
                case "samplerCube":
                case "isamplerCube":
                case "usamplerCube":
                    break;

                case "subpassInput":
                    if (TryGetInputAttachmentIndex(uniformVariable, out int index))
                        _sb.Append($"input_attachment_index={index}, ");
                    else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3300", $"InputAttachmentIndex Attribute is missing for RenderPassInput variable: {uniformVariable.Field.Declaration.GetText()}", uniformVariable.Field));
                    break;

                default:
                    _sb.Append("std140, ");
                    break;
            }

            _sb.Append($"set={uniformVariable.Set}, binding={uniformVariable.Binding}) ");
        }

        protected override void BuildEntryMethodBodyPostfixStatements()
        {
            if (_shaderClass is VertexShaderClassDeclaration vertexShaderClass && vertexShaderClass.FixVulkanY)
            {
                base.BuildEntryMethodBodyPostfixStatements();

                _sb.AppendLineIndentation("gl_Position.y = -gl_Position.y;", _indentation);
            }
            else base.BuildEntryMethodBodyPostfixStatements();
        }

        #endregion

    }
}
