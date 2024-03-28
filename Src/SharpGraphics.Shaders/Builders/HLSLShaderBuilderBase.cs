using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGraphics.Shaders.Builders;
using SharpGraphics.Shaders.Generator;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Validators;
using SharpGraphics.Shaders.Validators.DirectXShaderCompiler;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SharpGraphics.Shaders.Builders
{
    internal abstract class HLSLShaderBuilderBase : TextShaderBuilderBase
    {

        #region Fields

        protected readonly IDirectXShaderCompiler? _dxc;

        protected string _entryReturnType = "void";
        protected string? _entryParameterType = null;

        protected int _inVariableLocation = 0;
        protected int _outVariableLocation = 0;

        #endregion

        #region Properties

        protected abstract string TargetShaderModel { get; }

        #endregion

        #region Constructors

        public HLSLShaderBuilderBase(ShaderLanguageMappings mappings) : base(mappings) { }
        public HLSLShaderBuilderBase(ShaderLanguageMappings mappings, IDirectXShaderCompiler dxc) : base(mappings)
            => _dxc = dxc;

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
                        case ShaderTypePrecisions.Bits16: name = "half"; return true;
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
            if (shaderType.ComponentCount >= 1 && shaderType.ComponentCount <= 4 &&
                TryGetBuiltInTypeName(shaderType.ComponentType, out string? componentTypeName))
            {
                name = $"{componentTypeName}{shaderType.ComponentCount}";
                return true;
            }
            else
            {
                name = null;
                return false;
            }
        }
        protected override bool TryGetBuiltInTypeName(MatrixShaderType shaderType, [NotNullWhen(returnValue: true)] out string? name)
        {
            if (shaderType.RowCount >= 1 && shaderType.RowCount <= 4 &&
                shaderType.ColumnType.ComponentCount >= 1 && shaderType.ColumnType.ComponentCount <= 4 &&
                TryGetBuiltInTypeName(shaderType.ColumnType.ComponentType, out string? componentTypeName))
            {
                name = $"{componentTypeName}{shaderType.RowCount}x{shaderType.ColumnType.ComponentCount}";
                return true;
            }
            else
            {
                name = null;
                return false;
            }
        }
        protected override bool TryGetBuiltInTypeName(TextureSamplerType shaderType, [NotNullWhen(returnValue: true)] out string? name)
        {
            switch (shaderType.Dimensions)
            {
                //case TextureSamplerDimensions.Dimensions1: name = "sampler1D"; return true;
                //case TextureSamplerDimensions.Dimensions2: name = "sampler2D"; return true;
                //case TextureSamplerDimensions.Dimensions3: name = "sampler3D"; return true;
                //case TextureSamplerDimensions.Cube: name = "samplerCUBE"; return true;
                //case TextureSamplerDimensions.RenderPassInput: samplerType = "sampler2D"; break; //TODO: HLSL RenderPassInput
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
                //case "texelSample": name = "texture"; remap = null; return true;
                //case "subpassFetch": name = "texelFetch"; remap = new[] { new ArgumentRemap(0), new ArgumentRemap("ivec2(gl_FragCoord.xy)"), new ArgumentRemap("0") }; return true;

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
        protected virtual bool TryGetVariableSemanticName(string name, [NotNullWhen(returnValue: true)] out string? semanticName)
        {
            //https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-semantics
            //https://learn.microsoft.com/en-us/windows/uwp/gaming/glsl-to-hlsl-reference
            switch (name)
            {
                //Vertex Shader Inputs
                case "vID": semanticName = "SV_VertexID"; return true;
                //Vertex Shader Outputs
                case "vPosition": semanticName = "SV_Position"; return true;
                case "vPointSize": semanticName = "PSIZE"; return true;
                case "vClipDistance": semanticName = "SV_ClipDistance"; return true;

                //Fragment Shader Inputs
                case "fCoord": semanticName = "SV_Position"; return true;
                case "fIsFrontFace": semanticName = "SV_IsFrontFace"; return true;
                //TODO: HLSL fPointCoord case "fPointCoord": semanticName = "SV_Position"; return true;
                case "fClipDistance": semanticName = "SV_ClipDistance"; return true;
                //Fragment Shader Outputs
                case "fDepth": semanticName = "SV_Depth"; return true;

                default:
                    semanticName = null;
                    return false;
            }
        }


        protected override bool TryBuildVariableDeclaration(ShaderVariableDeclaration variable, TypeSyntax typeSyntax, string type, string name)
        {
            //bool canBeArrayOfBuffers = true;

            switch (variable)
            {
                case ShaderMemberVariableDeclaration memberVariable:
                    _sb.Append(type);
                    _sb.Append(' ');
                    _sb.Append(name);
                    break;

                case ShaderLocalVariableDeclaration localVariable:
                    _sb.Append("static ");
                    _sb.Append(type);
                    _sb.Append(' ');
                    _sb.Append(name);
                    break;

                case ShaderInVariableDeclaration inVariable:
                    _sb.AppendIndentation(type, _indentation);
                    _sb.Append(' ');
                    _sb.Append(name);
                    _sb.Append(" : ");
                    if (inVariable.StageVariable != null)
                    {
                        if (TryGetVariableSemanticName(inVariable.StageVariable.Name, out string? semanticName))
                            _sb.Append(semanticName);
                        else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("1505", $"Unknown Shader Stage Input variable Name: {inVariable.StageVariable.Name}"));
                    }
                    else
                    {
                        _sb.Append("SHADER_IN_");
                        _sb.Append(_inVariableLocation++);
                    }
                    break;

                case ShaderOutVariableDeclaration outVariable:
                    _sb.AppendIndentation(type, _indentation);
                    _sb.Append(' ');
                    _sb.Append(name);
                    _sb.Append(" : ");
                    if (outVariable.StageVariable != null)
                    {
                        if (TryGetVariableSemanticName(outVariable.StageVariable.Name, out string? semanticName))
                            _sb.Append(semanticName);
                        else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("1506", $"Unknown Shader Stage Output variable Name: {outVariable.StageVariable.Name}"));
                    }
                    else if (_shaderClass != null)
                    {
                        if (_shaderClass is FragmentShaderClassDeclaration)
                            _sb.Append("SV_Target");
                        else _sb.Append("SHADER_OUT_");
                        _sb.Append(_outVariableLocation++);
                    }
                    break;

                case ShaderUniformVariableDeclaration uniformVariable:
                    if (TryGetTypeSymbol(typeSyntax, out ITypeSymbol? typeSymbol) && TryGenerateStruct(typeSymbol, out UserStructShaderType? generatedStruct))
                    {
                        _sb.Append("ConstantBuffer<");
                        _sb.Append(generatedStruct.TypeName);
                        _sb.Append("> ");
                        _sb.Append(name);
                        _sb.Append(" : register(b");
                        _sb.Append(uniformVariable.UniqueBinding);
                        _sb.Append(")");
                    }
                    else _sb.Append(type); //Error...
                    break;
            }

            /*if (canBeArrayOfBuffers && typeSyntax.IsKind(SyntaxKind.ArrayType))
            {
                if (TryGetArrayCount(variable, out int arrayCount))
                    _sb.Append($"[{arrayCount}]"); //OpenGL expects array length after name: vec2 pos[3] = vec2[3]( vec2(...), ... );
                else _sb.Append("[]");
            }*/

            return true;
        }


        protected override void BuildVariables(ShaderClassDeclaration shaderClass)
        {
            //base.BuildVariables(shaderClass);

            IEnumerable<ShaderInVariableDeclaration> inVars = shaderClass.InVars.Concat(shaderClass.StageInputs.Values.Where(s => !shaderClass.InVars.Any(o => o.Field == s.Field)).Select(s => new ShaderInVariableDeclaration(s.Field, s.StageVariable!)));
            if (inVars.Count() > 0)
            {
                _sb.Append("struct ");
                BuildStructDeclaration("ShaderIn", inVars);
                _sb.AppendLine(';');
                _entryParameterType = "ShaderIn";
                foreach (ShaderInVariableDeclaration shaderInVariable in inVars)
                    foreach (VariableDeclaratorSyntax varDec in shaderInVariable.Field.Declaration.Variables)
                        AddVariableNameOverride(varDec.Identifier.Text, $"shaderIn.{varDec.Identifier.Text}");
            }

            IEnumerable<ShaderOutVariableDeclaration> outVars = shaderClass.OutVars.Concat(shaderClass.StageOutputs.Values.Where(s => !shaderClass.OutVars.Any(o => o.Field == s.Field)).Select(s => new ShaderOutVariableDeclaration(s.Field, s.StageVariable!)));
            if (outVars.Count() > 0)
            {
                _sb.Append("struct ");
                BuildStructDeclaration("ShaderOut", outVars);
                _sb.AppendLine(';');
                _entryReturnType = "ShaderOut";
                foreach (ShaderOutVariableDeclaration shaderInVariable in outVars)
                    foreach (VariableDeclaratorSyntax varDec in shaderInVariable.Field.Declaration.Variables)
                        AddVariableNameOverride(varDec.Identifier.Text, $"shaderOut.{varDec.Identifier.Text}");
            }

            BuildVariables(shaderClass.Uniforms, false);

            BuildVariables(shaderClass.Locals.Where(l => l.StageVariable == null), true);
        }
        protected override void BuildVariables(IEnumerable<ShaderVariableDeclaration> variables, bool canBeInitialized)
        {
            base.BuildVariables(variables, canBeInitialized);
        }


        protected override void BuildMethodInterface(string type, string name, ParameterListSyntax parameters)
        {
            if (name.ToLower() == "main")
            {
                BuildMethodInterfaceBegin(_entryReturnType, name);
                if (_entryParameterType != null)
                {
                    _sb.Append(_entryParameterType);
                    _sb.Append(" shaderIn");
                }
                BuildMethodInterfaceEnd();
            }
            else base.BuildMethodInterface(type, name, parameters);
        }
        protected override void BuildEntryMethodBodyPrefixStatements()
        {
            base.BuildEntryMethodBodyPrefixStatements();

            if (_entryReturnType != "void")
            {
                _sb.AppendLineIndentation("ShaderOut shaderOut;", _indentation);
            }
        }
        protected override void BuildEntryMethodBodyPostfixStatements()
        {
            base.BuildEntryMethodBodyPostfixStatements();

            if (_entryReturnType != "void")
            {
                _sb.AppendLineIndentation("return shaderOut;", _indentation);
            }
        }

        protected override void BuildArrayInitializationExpression(ArrayCreationExpressionSyntax arrayCreation)
        {
            if (TryGetTypeName(arrayCreation.Type.ElementType, out string? elementType))
            {
                if (arrayCreation.Initializer != null)
                {
                    _sb.Append('{');
                    BuildExpression(arrayCreation.Initializer);
                    _sb.Append('}');
                }
            }
            else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3000", $"Unknown Type: {arrayCreation.Type}", arrayCreation));
        }


        protected override void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderBinaryOperatorExpression shaderExpression, ExpressionSyntax leftExpression, ExpressionSyntax rightExpression)
        {
            if (shaderExpression.Left is MatrixShaderType || shaderExpression.Right is MatrixShaderType)
            {
                switch (shaderExpression.Operator)
                {
                    case "*": BuildShaderExpression(originalExpression, new ShaderFunctionExpression("mul", shaderExpression.Left, shaderExpression.Right), new[] { leftExpression, rightExpression }); break;

                    default: AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2001", $"Unknown Expression {originalExpression.GetText()}", originalExpression)); break;
                }
            }
            else base.BuildShaderExpression(originalExpression, shaderExpression, leftExpression, rightExpression);
        }


        protected virtual void ExecuteDirectXShaderCompiler()
        {
            if (_dxc != null && _dxc.CanExecute)
            {
                string glslSource = _sb.ToString();

                try
                {
                    if (base.IsBuildSuccessful && _shaderClass != null && !_cancellationToken.IsCancellationRequested && !_dxc.ValidateShader(_shaderClass, glslSource, TargetShaderModel))
                        AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("5101", "DirectXShaderCompiler error.", _shaderClass.ClassDeclaration.Identifier));
                }
                catch (GenerationException e)
                {
                    AddDiagnosticsAndFail(e.Diagnositcs);
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
                    ExecuteDirectXShaderCompiler();
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
