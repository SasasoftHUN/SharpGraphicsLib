using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGraphics.Shaders.Builders;
using SharpGraphics.Shaders.Generator;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Validators;
using SharpGraphics.Shaders.Validators.GLSLangValidator;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SharpGraphics.Shaders.Builders
{
    internal abstract class GLSLShaderBuilderBase : TextShaderBuilderBase
    {

        #region Fields

        protected readonly IGLSLangValidator? _glslangValidator;

        protected Dictionary<string, string> _variableCopyAtEnd = new Dictionary<string, string>(); //Copy Keys into Values

        #endregion

        #region Constructors

        public GLSLShaderBuilderBase(ShaderLanguageMappings mappings) : base(mappings) { }
        public GLSLShaderBuilderBase(ShaderLanguageMappings mappings, IGLSLangValidator glslangValidator) : base(mappings)
        {
            if (glslangValidator.CanExecute)
                _glslangValidator = glslangValidator;
            else AddDiagnostics(ShaderGenerationDiagnositcs.CreateWarning("5501", "GLSLangValidator is not initialized, validation will not execute."));
        }

        #endregion

        #region Protected Methods

        protected override bool TryGetBuiltInTypeName(PrimitiveShaderType shaderType, [NotNullWhen(returnValue: true)] out string? name)
        {
            switch (shaderType)
            {
                case VoidShaderType voidType: name = "void"; return true;
                case BoolShaderType boolType: name = "bool"; return true;

                case IntShaderType intType:
                    switch (intType.Precision)
                    {
                        case ShaderTypePrecisions.Bits32: name = "int"; return true;
                        default:
                            name = null;
                            return false;
                    }

                case UIntShaderType uintType:
                    switch (uintType.Precision)
                    {
                        case ShaderTypePrecisions.Bits32: name = "uint"; return true;
                        default:
                            name = null;
                            return false;
                    }

                case FloatShaderType floatType:
                    switch (floatType.Precision)
                    {
                        case ShaderTypePrecisions.Bits32: name = "float"; return true;
                        case ShaderTypePrecisions.Bits64: name = "double"; return true;
                        default:
                            name = null;
                            return false;
                    }

                default:
                    name = null;
                    return false;
            }
        }
        protected override bool TryGetBuiltInTypeName(VectorShaderType shaderType, [NotNullWhen(returnValue: true)] out string? name)
        {
            switch (shaderType.ComponentCount)
            {
                case 2:
                case 3:
                case 4:
                    switch (shaderType.ComponentType.Type)
                    {
                        case PrimitiveShaderTypes.Bool: name = $"bvec{shaderType.ComponentCount}"; return true;

                        case PrimitiveShaderTypes.Int:
                            if (shaderType.ComponentType is NumericShaderType intComponentType)
                                switch (intComponentType.Precision)
                                {
                                    case ShaderTypePrecisions.Bits32: name = $"ivec{shaderType.ComponentCount}"; return true;
                                    default:
                                        name = null;
                                        return false;
                                }
                            else
                            {
                                name = null;
                                return false;
                            }

                        case PrimitiveShaderTypes.UInt:
                            if (shaderType.ComponentType is NumericShaderType uintComponentType)
                                switch (uintComponentType.Precision)
                                {
                                    case ShaderTypePrecisions.Bits32: name = $"uvec{shaderType.ComponentCount}"; return true;
                                    default:
                                        name = null;
                                        return false;
                                }
                            else
                            {
                                name = null;
                                return false;
                            }
                        case PrimitiveShaderTypes.Float:
                            if (shaderType.ComponentType is NumericShaderType floatComponentType)
                                switch (floatComponentType.Precision)
                                {
                                    case ShaderTypePrecisions.Bits32: name = $"vec{shaderType.ComponentCount}"; return true;
                                    case ShaderTypePrecisions.Bits64: name = $"dvec{shaderType.ComponentCount}"; return true;
                                    default:
                                        name = null;
                                        return false;
                                }
                            else
                            {
                                name = null;
                                return false;
                            }
                        default:
                            name = null;
                            return false;
                    }

                default:
                    name = null;
                    return false;
            }
        }
        protected override bool TryGetBuiltInTypeName(MatrixShaderType shaderType, [NotNullWhen(returnValue: true)] out string? name)
        {
            if (shaderType.ColumnType.ComponentType.Type == PrimitiveShaderTypes.Float &&
                shaderType.ColumnType.ComponentType is NumericShaderType componentType &&
                shaderType.ColumnType.ComponentCount >= 2 && shaderType.ColumnType.ComponentCount <= 4 &&
                shaderType.RowCount >= 2 && shaderType.RowCount <= 4)
            {
                switch (componentType.Precision)
                {
                    case ShaderTypePrecisions.Bits32: name = $"mat{shaderType.RowCount}x{shaderType.ColumnType.ComponentCount}"; return true;
                    case ShaderTypePrecisions.Bits64: name = $"dmat{shaderType.RowCount}x{shaderType.ColumnType.ComponentCount}"; return true;
                    default:
                        name = null;
                        return false;
                }
            }
            else
            {
                name = null;
                return false;
            }
        }
        protected override bool TryGetBuiltInTypeName(TextureSamplerType shaderType, [NotNullWhen(returnValue: true)] out string? name)
        {
            string samplerType;
            switch (shaderType.Dimensions)
            {
                case TextureSamplerDimensions.Dimensions1: samplerType = "sampler1D"; break;
                case TextureSamplerDimensions.Dimensions2: samplerType = "sampler2D"; break;
                case TextureSamplerDimensions.Dimensions3: samplerType = "sampler3D"; break;
                case TextureSamplerDimensions.Cube: samplerType = "samplerCube"; break;
                case TextureSamplerDimensions.RenderPassInput: samplerType = "sampler2D"; break;
                default:
                    name = null;
                    return false;
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


        protected override bool IsConstructorRemapped(ShaderConstructorExpression constructor, [NotNullWhen(true)] out ArgumentRemap[]? remap)
        {
            remap = null;
            return false;
        }
        protected override bool TryGetBuiltInFunctionName(ShaderFunctionExpression function, [NotNullWhen(returnValue: true)] out string? name, out ArgumentRemap[]? remap)
        {
            switch (function.FunctionName)
            {
                case "texelSample": name = "texture"; remap = null; return true;
                case "subpassFetch": name = "texelFetch"; remap = new[] { new ArgumentRemap(0), new ArgumentRemap("ivec2(gl_FragCoord.xy)"), new ArgumentRemap("0") }; return true;

                default: name = function.FunctionName; remap = null; return true;
            }
        }
        protected override bool TryGetBuiltInStatementName(ShaderStatementExpression statement, [NotNullWhen(returnValue: true)] out string? name)
        {
            switch (statement.StatementName)
            {
                default: name = statement.StatementName; return true;
            }
        }
        protected virtual bool TryGetBuiltInVariableName(string name, [NotNullWhen(returnValue: true)] out string? builtInName)
        {
            switch (name)
            {
                //Vertex Shader Inputs
                case "vID": builtInName = "gl_VertexID"; return true;
                //Vertex Shader Outputs
                case "vPosition": builtInName = "gl_Position"; return true;
                case "vPointSize": builtInName = "gl_PointSize"; return true;
                case "vClipDistance": builtInName = "gl_ClipDistance"; return true;

                //Fragment Shader Inputs
                case "fCoord": builtInName = "gl_FragCoord"; return true;
                case "fIsFrontFace": builtInName = "gl_FrontFacing"; return true;
                case "fPointCoord": builtInName = "gl_PointCoord"; return true;
                case "fClipDistance": builtInName = "gl_ClipDistance"; return true;
                //Fragment Shader Outputs
                case "fDepth": builtInName = "gl_FragDepth"; return true;

                default:
                    builtInName = null;
                    return false;
            }
        }

        protected bool TryGetArrayCount(ShaderVariableDeclaration variable, out int count)
        {
            if (_compilation != null &&
                variable.Field.TryGetAttributeData(ShaderGenerator.ArraySizeAttributeFullName, _compilation, _cancellationToken, out AttributeData? attributeData) &&
                attributeData.TryGetAttributePropertyValueInt("Count", out count))
                return true;

            count = 0;
            return false;
        }


        protected abstract void BuildPreprocessorVersion();

        protected override void BuildPreprocessorBegin()
        {
            BuildPreprocessorVersion();
            BuildExtensions();
        }
        protected virtual void BuildExtensions() { }

        protected override bool TryBuildVariableDeclaration(ShaderVariableDeclaration variable, TypeSyntax typeSyntax, string type, string name)
        {
            bool canBeArrayOfBuffers = true;

            if (variable.StageVariable != null)
            {
                if (TryGetBuiltInVariableName(variable.StageVariable.Name, out string? builtInName))
                {
                    switch (variable)
                    {
                        //case ShaderMemberVariableDeclaration memberVariable: cannot happen, could only be provided through internal code error

                        case ShaderLocalVariableDeclaration localVariable:
                            AddVariableNameOverride(name, builtInName);
                            return false;

                        //case ShaderInVariableDeclaration inVariable: cannot happen, error outside builder

                        case ShaderOutVariableDeclaration outVariable:
                            _sb.Append("out ");
                            _sb.Append(type);
                            _sb.Append(' ');
                            _sb.Append(name);
                            _variableCopyAtEnd[name] = builtInName;
                            //TODO: Can this be array?
                            return true;

                        //case ShaderUniformVariableDeclaration uniformVariable: cannot happen, error outside builder

                        default: return false;
                    }
                }
                else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3651", $"Unknown Shader Built-In Variable: {variable.StageVariable.Name}", variable.Field));
                return false;
            }
            else
            {
                switch (variable)
                {
                    case ShaderMemberVariableDeclaration memberVariable:
                        _sb.Append(type);
                        break;

                    case ShaderLocalVariableDeclaration localVariable:
                        _sb.Append(type);
                        break;

                    case ShaderInVariableDeclaration inVariable:
                        _sb.Append("in ");
                        _sb.Append(type);
                        break;

                    case ShaderOutVariableDeclaration outVariable:
                        _sb.Append("out ");
                        _sb.Append(type);
                        break;
                    case ShaderUniformVariableDeclaration uniformVariable:
                        _sb.Append("uniform ");
                        if (TryGetTypeSymbol(typeSyntax, out ITypeSymbol? typeSymbol))
                        {
                            string nameOverride = name + "_uniform";
                            switch (typeSymbol)
                            {
                                case IArrayTypeSymbol arrayTypeSymbol:
                                    if (arrayTypeSymbol.ElementType is INamedTypeSymbol elementType)
                                        BuildInterfaceBlockArray(uniformVariable, elementType, type, name, nameOverride, ref canBeArrayOfBuffers);
                                    else
                                    {
                                        AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3000", $"Unknown Type: {type}", arrayTypeSymbol.ElementType));
                                        _sb.Append(type);
                                    }
                                    break;

                                default:
                                    BuildInterfaceBlock(typeSymbol, type, nameOverride);
                                    //AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3001", $"Unsupported Type Kind: {typeSymbol.TypeKind}", typeSyntax));
                                    //_sb.Append(type);
                                    break;
                            }
                        }
                        else _sb.Append(type); //Error...
                        break;
                }

                _sb.Append(' ');
                _sb.Append(name);

                if (canBeArrayOfBuffers && typeSyntax.IsKind(SyntaxKind.ArrayType))
                {
                    if (TryGetArrayCount(variable, out int arrayCount))
                        _sb.Append($"[{arrayCount}]"); //OpenGL expects array length after name: vec2 pos[3] = vec2[3]( vec2(...), ... );
                    else _sb.Append("[]");
                }

                return true;
            }
        }
        protected virtual void BuildInterfaceBlockArray(ShaderVariableDeclaration variable, ITypeSymbol namedType, string type, string name, string nameOverride, ref bool canBeArrayOfBuffers)
        {
            if (TryGetArrayCount(variable, out int arrayCount))
            {
                canBeArrayOfBuffers = false;
                if (!TryGetBuiltInTypeName(namedType, out _))
                    TryGenerateStruct(namedType, out _); //Generate struct
                BuildStructArrayDeclarationForVariable(nameOverride, type, arrayCount);
                AddVariableNameOverride(name, name + ".s");
            }
            else BuildInterfaceBlock(namedType, type, nameOverride);
        }
        protected virtual void BuildInterfaceBlock(ITypeSymbol namedType, string type, string nameOverride)
        {
            if (TryGetShaderType(namedType, out ShaderType? shaderType))
            {
                if (shaderType is UserStructShaderType userStructType)
                    BuildStructDeclaration(nameOverride, userStructType.Declaration);
                else _sb.Append(type);
            }
        }
        protected virtual void BuildStructArrayDeclarationForVariable(string name, string structType, int count)
        {
            _sb.AppendLine(name);
            _sb.AppendLineIndentation('{', _indentation);

            _sb.AppendIndentation(structType, _indentation + 1);
            _sb.Append(" s[");
            _sb.Append(count);
            _sb.AppendLine("];");

            _sb.AppendIndentation("}", _indentation);
        }


        protected virtual void ExecuteGLSLValidator()
        {
            if (_glslangValidator != null && _glslangValidator.CanExecute)
            {
                string glslSource = _sb.ToString();

                try
                {
                    if (base.IsBuildSuccessful && _shaderClass != null && !_cancellationToken.IsCancellationRequested)
                    {
                        if (!_glslangValidator.ValidateShader(_shaderClass, glslSource))
                        {
                            AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("5101", "GLSLangValidator error.", _shaderClass.ClassDeclaration.Identifier));
                            AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("5000", $"Validation error. Shader source code: {glslSource.LinesToListing("\\n")}", _shaderClass.ClassDeclaration.Identifier));
                        }
                    }
                }
                catch (GenerationException e)
                {
                    AddDiagnosticsAndFail(e.Diagnositcs);
                    AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("5000", $"Validation error. Shader source code: {glslSource.LinesToListing("\\n")}", _shaderClass?.ClassDeclaration.Identifier));
                }
                catch (OperationCanceledException) { }
                catch { }
            }
        }

        #endregion

        #region Public Methods

        public override IShaderBuilder BuildGraphics(ShaderClassDeclaration shaderClass)
        {
            base.BuildGraphics(shaderClass);

            if (base.IsBuildSuccessful && !_cancellationToken.IsCancellationRequested)
                try
                {
                    ExecuteGLSLValidator();
                }
                catch (Exception e)
                {
                    string message = e.Message.LinesToListing();
                    if (e.StackTrace != null)
                        message += " --- " + e.StackTrace.ToString().LinesToListing();
                    AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("1900", $"Internal error during Shader Generation: {message}", shaderClass.ClassDeclaration.Identifier));
                }

            return this;
        }

        #endregion

    }
}
