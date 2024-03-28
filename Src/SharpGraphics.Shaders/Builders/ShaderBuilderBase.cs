using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SharpGraphics.Shaders.Generator;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using SharpGraphics.Shaders.Mappings;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Immutable;
using SharpGraphics.Shaders.Builders;
using SharpGraphics.Shaders.Reporters;
using System.Linq.Expressions;
using System.Data.Common;
using System.Reflection;

namespace SharpGraphics.Shaders.Builders
{
    internal abstract class ShaderBuilderBase : IShaderBuilder
    {

        protected readonly struct ArgumentRemap
        {
            public readonly int mappedIndex;
            public readonly string? customArgument; //TODO: what if binary format? Generic? Add binary field too?

            public ArgumentRemap(int mappedIndex)
            {
                this.mappedIndex = mappedIndex;
                this.customArgument = null;
            }
            public ArgumentRemap(string customArgument)
            {
                this.mappedIndex = -1;
                this.customArgument = customArgument;
            }
        }

        protected readonly struct GeneratedFunction
        {
            public readonly IMethodSymbol methodSymbol;
            public readonly string name;

            public GeneratedFunction(IMethodSymbol methodSymbol, string name)
            {
                this.methodSymbol = methodSymbol;
                this.name = name;
            }
        }

        #region Fields

        private readonly List<ShaderGenerationDiagnositcs> _diagnostics = new List<ShaderGenerationDiagnositcs>();
        private bool _isFailed = false;

        private readonly Dictionary<ITypeSymbol, UserStructShaderType> _generatedStructs = new Dictionary<ITypeSymbol, UserStructShaderType>(SymbolEqualityComparer.IncludeNullability);
        private readonly Dictionary<IMethodSymbol, GeneratedFunction> _generatedFunctions = new Dictionary<IMethodSymbol, GeneratedFunction>(SymbolEqualityComparer.IncludeNullability);
        private readonly Stack<string> _functionNameGenerationStack = new Stack<string>();

        private readonly ShaderLanguageMappings _mappings;

        private readonly StringBuilder _tmpBuilder = new StringBuilder();

        protected ShaderClassDeclaration? _shaderClass;

        protected Compilation? _compilation;
        protected CancellationToken _cancellationToken;

        #endregion

        #region Properties

        public abstract string BackendName { get; }

        public virtual bool IsBuildSuccessful => !_isFailed;
        public IEnumerable<ShaderGenerationDiagnositcs> Diagnositcs => _diagnostics;

        #endregion

        #region Constructors

        public ShaderBuilderBase(ShaderLanguageMappings mappings)
        {
            _mappings = mappings;
        }

        #endregion

        #region Protected Methods

        protected void AddDiagnostics(ShaderGenerationDiagnositcs diagnositcs) => _diagnostics.Add(diagnositcs);
        protected void AddDiagnosticsAndFail(ShaderGenerationDiagnositcs diagnositcs)
        {
            AddDiagnostics(diagnositcs);
            _isFailed = true;
        }

        protected string ReplaceInvalidCharsInName(string name)
        {
            _tmpBuilder.Clear();

            foreach (char c in name)
                switch (c)
                {
                    case '.':
                    case ',':
                        _tmpBuilder.Append('_'); break;

                    case '<':
                    case '>':
                        _tmpBuilder.Append('_'); _tmpBuilder.Append('_'); break;

                    case '(':
                    case ')':
                        return _tmpBuilder.ToString();

                    default: _tmpBuilder.Append(c); break;
                }

            return _tmpBuilder.ToString();
        }

        
        protected bool TryReorderMappedArguments(in ShaderArgumentMappings mapping, ExpressionSyntax? invoker, IEnumerable<ArgumentSyntax>? arguments, SyntaxNode argumentListNode, [NotNullWhen(returnValue: true)] out IReadOnlyList<ExpressionSyntax>? reorderedArguments)
            => TryReorderMappedArguments(mapping, invoker, arguments != null ? arguments.Select(a => a.Expression) : null, argumentListNode, out reorderedArguments);
        protected bool TryReorderMappedArguments(in ShaderArgumentMappings mapping, ExpressionSyntax? invoker, IEnumerable<ExpressionSyntax>? arguments, SyntaxNode argumentListNode, [NotNullWhen(returnValue: true)] out IReadOnlyList<ExpressionSyntax>? reorderedArguments)
        {
            int mappedArgumentCount = 0;
            bool needToReorder = false;

            if (mapping.invokerMappedToArgumentIndex >= 0) //Check if invoker is mapped into argument
            {
                if (invoker == null) //invoker is mapped, but no invoker expression, fail
                {
                    AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3601", $"Incorrect Argument mapping index during Argument Reordering: {argumentListNode}", argumentListNode));
                    reorderedArguments = null;
                    return false;
                }
                ++mappedArgumentCount;
                needToReorder = true;
            }

            if (arguments != null) //Check if arguments are provided
            {
                ReadOnlySpan<ShaderArgumentMapping> argumentMappings = mapping.Arguments;
                if (arguments.Count() != argumentMappings.Length) //Must have same number of arguments
                {
                    AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3601", $"Incorrect Argument mapping index during Argument Reordering: {argumentListNode}", argumentListNode));
                    reorderedArguments = null;
                    return false;
                }

                int argumentCount = argumentMappings.Length;
                if (mapping.invokerMappedToArgumentIndex >= 0) //When invoker is mapped, mappedIndices can be greater than the number of arguments provided
                    ++argumentCount;

                for (int i = 0; i < argumentMappings.Length; i++) //Count how many arguments should be mapped
                    if (argumentMappings[i].mappedIndex >= 0)
                    {
                        ++mappedArgumentCount;
                        if (argumentCount <= argumentMappings[i].mappedIndex)
                        {
                            AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3601", $"Incorrect Argument mapping index during Argument Reordering: {argumentListNode}", argumentListNode));
                            reorderedArguments = null;
                            return false;
                        }
                        if (i != argumentMappings[i].mappedIndex)
                            needToReorder = true;
                    }
            }
            else //No arguments, check if mapping really doesn't need any
            {
                ReadOnlySpan<ShaderArgumentMapping> argumentMappings = mapping.Arguments;
                for (int i = 0; i < argumentMappings.Length; i++)
                    if (argumentMappings[i].mappedIndex >= 0)
                    {
                        AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3601", $"Incorrect Argument mapping index during Argument Reordering: {argumentListNode}", argumentListNode));
                        reorderedArguments = null;
                        return false;
                    }
            }

            if (needToReorder)
            {
                ExpressionSyntax[] reordered = new ExpressionSyntax[mappedArgumentCount];

                if (mapping.invokerMappedToArgumentIndex >= 0) //Invoker into argument
                    reordered[mapping.invokerMappedToArgumentIndex] = invoker!; //Tested outside, also will be tested later

                //Reorder Arguments
                ReadOnlySpan<ShaderArgumentMapping> argumentMappings = mapping.Arguments;
                for (int i = 0; i < argumentMappings.Length; i++)
                    if (argumentMappings[i].mappedIndex >= 0)
                    {
                        if (reordered[argumentMappings[i].mappedIndex] != null)
                        {
                            AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3601", $"Incorrect Argument mapping index during Argument Reordering: {argumentListNode}", argumentListNode));
                            reorderedArguments = null;
                            return false;
                        }
                        reordered[argumentMappings[i].mappedIndex] = arguments!.ElementAt(i); //Tested outside
                    }

                //Check if any index is missed
                for (int i = 0; i < reordered.Length; i++)
                    if (reordered[i] == null)
                    {
                        AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3601", $"Incorrect Argument mapping index during Argument Reordering: {argumentListNode}", argumentListNode));
                        reorderedArguments = null;
                        return false;
                    }

                reorderedArguments = reordered;
                return true;
            }
            else
            {
                reorderedArguments = arguments != null ? arguments.ToList() : new List<ExpressionSyntax>();
                return true;
            }
        }

        protected bool TryGetSymbol(SyntaxNode node, [NotNullWhen(returnValue: true)] out ISymbol? symbol)
        {
            if (_compilation != null && ASTAnalyzerHelper.TryGetSymbol(_compilation, node, _cancellationToken, out symbol))
                return true;

            AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3013", $"Unknown Symbol at Incovation: {node.GetText()}", node));
            symbol = null;
            return false;
        }
        protected bool TryGetTypeSymbol(SyntaxNode node, [NotNullWhen(returnValue: true)] out ITypeSymbol? resultTypeSymbol)
        {
            if (_compilation != null && ASTAnalyzerHelper.TryGetTypeSymbol(_compilation, node, _cancellationToken, out resultTypeSymbol))
                return true;

            AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3000", $"Unknown Type: {node}", node));
            resultTypeSymbol = null;
            return false;
        }
        protected bool TryGetTypeSymbol(TypeSyntax type, [NotNullWhen(returnValue: true)] out ITypeSymbol? resultTypeSymbol)
        {
            if (_compilation != null && ASTAnalyzerHelper.TryGetTypeSymbol(_compilation, type, _cancellationToken, out resultTypeSymbol))
                return true;

            AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3000", $"Unknown Type: {type}", type));
            resultTypeSymbol = null;
            return false;
        }
        protected bool TryGetTypeName(TypeSyntax type, [NotNullWhen(returnValue: true)] out string? name)
        {
            if (_compilation != null && ASTAnalyzerHelper.TryGetTypeSymbol(_compilation, type, _cancellationToken, out ITypeSymbol? typeSymbol))
                return TryGetTypeName(typeSymbol, out name);
            else
            {
                name = null;
                return false;
            }
        }
        protected bool TryGetTypeName(ITypeSymbol namedType, [NotNullWhen(returnValue: true)] out string? name)
        {
            if (namedType is IArrayTypeSymbol arrayTypeSymbol)
                namedType = arrayTypeSymbol.ElementType;

            if (namedType.TypeKind == TypeKind.Struct && namedType.IsValueType)
            {
                if (TryGetBuiltInTypeName(namedType, out name))
                    return true;
                else if (TryGenerateStruct(namedType, out UserStructShaderType? generatedStruct))
                {
                    name = generatedStruct.TypeName;
                    return true;
                }
                else
                {
                    AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3000", $"Unknown Type: {namedType.Name}", namedType));
                    name = null;
                    return false;
                }
            }
            else
            {
                AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3001", $"Unsupported Type Kind: {namedType.TypeKind}", namedType));
                name = null;
                return false;
            }
        }
        protected bool TryGetBuiltInTypeName(ITypeSymbol namedType, [NotNullWhen(returnValue: true)] out string? name)
        {
            if (TryGetShaderType(namedType, out ShaderType? shaderType))
                return TryGetBuiltInTypeName(shaderType, out name);
            else
            {
                name = null;
                return false;
            }
        }

        protected bool TryGetShaderTypeMapping(TypeSyntax type, [NotNullWhen(returnValue: true)] out ShaderTypeMapping? mapping)
        {
            if (_compilation != null && _shaderClass != null && _shaderClass.ClassTypeSymbol != null &&
                ASTAnalyzerHelper.TryGetShaderTypeMapping(_compilation, _mappings, _shaderClass.ClassTypeSymbol, type, _cancellationToken, out mapping))
                return true;
            else
            {
                mapping = null;
                return false;
            }
        }
        protected bool TryGetShaderTypeMapping(ITypeSymbol type, [NotNullWhen(returnValue: true)] out ShaderTypeMapping? mapping)
        {
            if (_shaderClass != null && _shaderClass.ClassTypeSymbol != null && ASTAnalyzerHelper.TryGetShaderTypeMapping(_mappings, _shaderClass.ClassTypeSymbol, type, out mapping))
                return true;
            else
            {
                mapping = null;
                return false;
            }
        }
        protected bool TryGetShaderType(TypeSyntax type, [NotNullWhen(returnValue: true)] out ShaderType? shaderType)
        {
            if (TryGetTypeSymbol(type, out ITypeSymbol? typeSymbol))
                return TryGetShaderType(typeSymbol, out shaderType);
            else
            {
                shaderType = null;
                return false;
            }
        }
        protected bool TryGetShaderType(ITypeSymbol type, [NotNullWhen(returnValue: true)] out ShaderType? shaderType)
        {
            if (TryGetShaderTypeMapping(type, out ShaderTypeMapping? mapping) && mapping.TypeMapping != null)
            {
                shaderType = mapping.TypeMapping;
                return true;
            }
            else if (type is INamedTypeSymbol namedType && _generatedStructs.TryGetValue(namedType, out UserStructShaderType? generatedStruct))
            {
                shaderType = generatedStruct;
                return true;
            }
            {
                shaderType = null;
                return false;
            }
        }
        protected bool TryGetShaderTypes(ArgumentListSyntax? argumentList, [NotNullWhen(returnValue: true)] out IReadOnlyList<ShaderType>? result)
        {
            if (argumentList != null)
                return TryGetShaderTypes(argumentList.Arguments.Select(a => a.Expression), out result);
            else
            {
                result = new List<ShaderType>();
                return true;
            }
        }
        protected bool TryGetShaderTypes(IEnumerable<ExpressionSyntax> expressions, [NotNullWhen(returnValue: true)] out IReadOnlyList<ShaderType>? result)
        {
            ShaderType[] shaderTypes = new ShaderType[expressions.Count()];
            int index = 0;
            foreach (ExpressionSyntax expression in expressions)
            {
                if (TryGetTypeSymbol(expression, out ITypeSymbol? argumentTypeSymbol) &&
                    TryGetShaderType(argumentTypeSymbol, out ShaderType? shaderType))
                    shaderTypes[index++] = shaderType;
                else
                {
                    result = null;
                    return false;
                }
            }

            result = shaderTypes;
            return true;
        }
        protected bool TryGetShaderTypes(IEnumerable<FieldDeclarationSyntax> fieldDeclarations, [NotNullWhen(returnValue: true)] out IReadOnlyList<ShaderType>? result)
            => TryGetShaderTypes(fieldDeclarations.Select(f => f.Declaration), out result);
        protected bool TryGetShaderTypes(IEnumerable<VariableDeclarationSyntax> variableDeclarations, [NotNullWhen(returnValue: true)] out IReadOnlyList<ShaderType>? result)
        {
            List<ShaderType> shaderTypes = new List<ShaderType>(variableDeclarations.Count());
            foreach (VariableDeclarationSyntax declaration in variableDeclarations)
            {
                if (TryGetTypeSymbol(declaration.Type, out ITypeSymbol? argumentTypeSymbol) &&
                    TryGetShaderType(argumentTypeSymbol, out ShaderType? shaderType))
                {
                    foreach (VariableDeclaratorSyntax declarator in declaration.Variables)
                        shaderTypes.Add(shaderType);
                }
                else
                {
                    result = null;
                    return false;
                }
            }

            result = shaderTypes;
            return true;
        }

        protected virtual bool TryGetBuiltInTypeName(ShaderType shaderType, [NotNullWhen(returnValue: true)] out string? name)
        {
            switch (shaderType)
            {
                case PrimitiveShaderType primitiveShaderType: return TryGetBuiltInTypeName(primitiveShaderType, out name);
                case VectorShaderType vectorShaderType: return TryGetBuiltInTypeName(vectorShaderType, out name);
                case MatrixShaderType matrixShaderType: return TryGetBuiltInTypeName(matrixShaderType, out name);
                case TextureSamplerType textureSamplerType: return TryGetBuiltInTypeName(textureSamplerType, out name);
                case UserStructShaderType userStructShaderType: name = userStructShaderType.TypeName; return true;
                default:
                    name = null;
                    return false;
            }
        }
        protected abstract bool TryGetBuiltInTypeName(PrimitiveShaderType shaderType, [NotNullWhen(returnValue: true)] out string? name);
        protected abstract bool TryGetBuiltInTypeName(VectorShaderType shaderType, [NotNullWhen(returnValue: true)] out string? name);
        protected abstract bool TryGetBuiltInTypeName(MatrixShaderType shaderType, [NotNullWhen(returnValue: true)] out string? name);
        protected abstract bool TryGetBuiltInTypeName(TextureSamplerType shaderType, [NotNullWhen(returnValue: true)] out string? name);

        protected bool TryGenerateStruct(ITypeSymbol namedType, [NotNullWhen(returnValue: true)] out UserStructShaderType? generatedStruct)
        {
            if (_generatedStructs.TryGetValue(namedType, out generatedStruct))
                return true;
            else return TryGenerateNewStruct(namedType, ReplaceInvalidCharsInName(namedType.ToDisplayString()), out generatedStruct);
        }
        protected bool TryGenerateNewStruct(ITypeSymbol structSymbol, string structFullName, [NotNullWhen(returnValue: true)] out UserStructShaderType? generatedStruct)
        {
            if (structSymbol.TypeKind == TypeKind.Struct && structSymbol.IsValueType && !TryGetShaderTypeMapping(structSymbol, out _))
            {
                if (TryFindStructDeclaration(structSymbol, out StructDeclarationSyntax? structDeclaration) && TryBuildNewStruct(structSymbol, structFullName, structDeclaration))
                {
                    generatedStruct = new UserStructShaderType(structFullName, structDeclaration, structSymbol);
                    _generatedStructs[structSymbol] = generatedStruct;

                    if (TryGetShaderTypes(structDeclaration.Members.OfType<FieldDeclarationSyntax>(), out IReadOnlyList<ShaderType>? constructorArguments))
                        generatedStruct.AddConstructor(new ShaderConstructorExpression(generatedStruct, constructorArguments.ToArray()));

                    return true;
                }
                else
                {
                    generatedStruct = null;
                    return false;
                }
            }
            else
            {
                AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3203", $"Struct generation is not supported for this type: {structSymbol.Name}", structSymbol));
                generatedStruct = null;
                return false;
            }
        }
        protected abstract bool TryBuildNewStruct(ITypeSymbol structSymbol, string structFullName, StructDeclarationSyntax structDeclaration);
        protected bool TryFindStructDeclaration(ITypeSymbol structSymbol, [NotNullWhen(returnValue: true)] out StructDeclarationSyntax? structDeclaration)
        {
            if (_compilation != null)
            {
                structDeclaration = null;
                if (structSymbol.DeclaringSyntaxReferences.Count() == 0)
                {
                    AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3200", $"Struct Declaration not found: {structSymbol.Name}", structSymbol));
                    return false;
                }

                foreach (SyntaxReference declarationReference in structSymbol.DeclaringSyntaxReferences)
                    if (declarationReference.GetSyntax(_cancellationToken) is StructDeclarationSyntax structDeclarationReference && structDeclarationReference != null)
                    {
                        if (structDeclaration == null)
                            structDeclaration = structDeclarationReference;
                        else
                        {
                            structDeclaration = null;
                            AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3202", $"Multiple Struct Declarations found for the same struct ({structSymbol.Name}). Shader Generator does not support this.", structSymbol));
                            return false;
                        }
                    }

                return structDeclaration != null;
            }
            else
            {
                structDeclaration = null;
                return false;
            }
        }

        protected virtual string GetVariableName(string varName, TypeSyntax varType) => varName;

        protected abstract bool IsConstructorRemapped(ShaderConstructorExpression constructor, [NotNullWhen(returnValue: true)] out ArgumentRemap[]? remap);
        protected abstract bool TryGetBuiltInFunctionName(ShaderFunctionExpression function, [NotNullWhen(returnValue: true)] out string? name, out ArgumentRemap[]? remap);
        protected abstract bool TryGetBuiltInStatementName(ShaderStatementExpression statement, [NotNullWhen(returnValue: true)] out string? name);

        protected bool IsFunctionGenerationSupported(IMethodSymbol method, [NotNullWhen(returnValue: true)] out string? name)
        {
            if (method.IsStatic)
            {
                name = ParseFullFunctionName(method);
                return true;
            }

            if (method.IsExtensionMethod)
            {
                name = ParseFullFunctionName(method);
                return true;
            }

            if (_shaderClass != null && method.ContainingSymbol != null && method.ContainingSymbol.DeclaringSyntaxReferences.Any(dsr => dsr.GetSyntax(_cancellationToken) == _shaderClass.ClassDeclaration))
            {
                name = method.Name;
                return true;
            }

            name = null;
            return false;
        }
        protected string ParseFullFunctionName(IMethodSymbol method) => ReplaceInvalidCharsInName(method.ToDisplayString());
        protected bool TryGenerateFunction(IMethodSymbol methodSymbol, Location locationWhenError, out GeneratedFunction generatedFunction)
        {
            if (IsFunctionGenerationSupported(methodSymbol, out string? functionName))
            {
                if (_generatedFunctions.TryGetValue(methodSymbol, out generatedFunction))
                    return true;
                else return TryGenerateNewFunction(methodSymbol, locationWhenError, functionName, out generatedFunction);
            }
            else
            {
                AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3010", $"Method is not a member of this Shader Class and not static: {methodSymbol.Name}{(methodSymbol.ContainingSymbol != null ? $" in: {methodSymbol.ContainingSymbol.Name}" : "")}", methodSymbol));
                generatedFunction = new GeneratedFunction();
                return false;
            }
        }
        protected virtual bool TryGenerateNewFunction(IMethodSymbol methodSymbol, Location locationWhenError, string name, out GeneratedFunction generatedFunction)
        {
            if (TryFindFunctionDeclaration(methodSymbol, locationWhenError, out MethodDeclarationSyntax? methodDeclaration))
            {
                if (_functionNameGenerationStack.Contains(name))
                {
                    AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3011", $"Recursive Method call detected ({methodSymbol.Name}), this is not supported.", methodSymbol));
                    generatedFunction = new GeneratedFunction();
                    return false;
                }
                
                try
                {
                    _functionNameGenerationStack.Push(name);
                    if (TryBuildNewFunction(methodSymbol, name, methodDeclaration))
                    {
                        generatedFunction = new GeneratedFunction(methodSymbol, name);
                        _generatedFunctions[methodSymbol] = generatedFunction;
                        return true;
                    }
                    else
                    {
                        generatedFunction = new GeneratedFunction();
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3007", $"Unexpected error during Method Generation ({methodSymbol.Name}): {ex.Message.LinesToListing()}", methodDeclaration.Identifier));
                    generatedFunction = new GeneratedFunction();
                    return false;
                }
                finally
                {
                    _functionNameGenerationStack.Pop();
                }
            }
            else
            {
                generatedFunction = new GeneratedFunction();
                return false;
            }
        }
        protected abstract bool TryBuildNewFunction(IMethodSymbol methodSymbol, string name, MethodDeclarationSyntax methodDeclaration);
        protected bool TryFindFunctionDeclaration(IMethodSymbol methodSymbol, Location locationWhenError, [NotNullWhen(returnValue: true)] out MethodDeclarationSyntax? methodDeclaration)
        {
            if (_compilation != null)
            {
                methodDeclaration = null;
                if (methodSymbol.DeclaringSyntaxReferences.Count() == 0)
                {
                    AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3500", $"Method Declaration not found: {methodSymbol.Name}", locationWhenError));
                    return false;
                }

                foreach (SyntaxReference declarationReference in methodSymbol.DeclaringSyntaxReferences)
                    if (declarationReference.GetSyntax(_cancellationToken) is MethodDeclarationSyntax methodDeclarationReference && methodDeclarationReference != null)
                    {
                        if (methodDeclaration == null)
                            methodDeclaration = methodDeclarationReference;
                        else
                        {
                            methodDeclaration = null;
                            AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3501", $"Multiple Method Declarations found for the same method ({methodSymbol.Name}). Shader Generator does not support this.", methodSymbol));
                            return false;
                        }
                    }

                return methodDeclaration != null;
            }
            else
            {
                methodDeclaration = null;
                return false;
            }
        }
        protected IReadOnlyList<ExpressionSyntax> GetFunctionCallArguments(in GeneratedFunction generatedFunction, ExpressionSyntax? invoker, IEnumerable<ArgumentSyntax>? arguments)
            => GetFunctionCallArguments(generatedFunction, invoker, arguments != null ? arguments.Select(a => a.Expression) : null);
        protected IReadOnlyList<ExpressionSyntax> GetFunctionCallArguments(in GeneratedFunction generatedFunction, ExpressionSyntax? invoker, IEnumerable<ExpressionSyntax>? arguments)
        {
            List<ExpressionSyntax> functionArguments = new List<ExpressionSyntax>();

            if (generatedFunction.methodSymbol.IsExtensionMethod)
            {
                if (invoker != null)
                    functionArguments.Add(invoker);
            }
            //TODO: Invoker as argument for member functions

            if (arguments != null)
                functionArguments.AddRange(arguments);

            return functionArguments;
        }

        protected bool TryGetMemberSymbol(MemberAccessExpressionSyntax memberAccess, [NotNullWhen(returnValue: true)] out ITypeSymbol? ownerType, [NotNullWhen(returnValue: true)] out ISymbol? memberSymbol)
        {
            if (_compilation != null && TryGetTypeSymbol(memberAccess.Expression, out ownerType))
            {
                SemanticModel model = _compilation.GetSemanticModel(memberAccess.SyntaxTree);
                SymbolInfo memberSymbolInfo = model.GetSymbolInfo(memberAccess.Name, _cancellationToken);
                if (memberSymbolInfo.Symbol != null)
                {
                    memberSymbol = memberSymbolInfo.Symbol;
                    return true;
                }
                else
                {
                    AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3008",
                        $"Unknown Symbol at Member Access: {memberAccess.GetText().ToString().LinesToListing()} - Owner Type: {ownerType}", memberAccess));
                    ownerType = null;
                    memberSymbol = null;
                    return false;
                }
            }

            AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3008", $"Unknown Symbol at Member Access: {memberAccess.GetText().ToString().LinesToListing()}", memberAccess));
            ownerType = null;
            memberSymbol = null;
            return false;
        }


        protected abstract void BuildReadabilityLineSeparator();
        protected abstract void RegisterStructDeclarationInsertIndex();
        protected abstract void RegisterFunctionDeclarationInsertIndex();

        protected virtual void BuildPreprocessorBegin() { }
        protected virtual void BuildVariables(ShaderClassDeclaration shaderClass)
        {
            BuildVariables(shaderClass.InVars, false);
            BuildVariables(shaderClass.OutVars, false);

            BuildVariables(shaderClass.Uniforms, false);

            BuildVariables(shaderClass.Locals, true);
        }
        protected virtual void BuildVariables(IEnumerable<ShaderVariableDeclaration> variables, bool canBeInitialized)
        {
            if (variables.Any())
            {
                foreach (ShaderVariableDeclaration variable in variables)
                {
                    if (_cancellationToken.IsCancellationRequested)
                        return;

                    if (TryGetTypeName(variable.Field.Declaration.Type, out string? typeName))
                        foreach (VariableDeclaratorSyntax varDec in variable.Field.Declaration.Variables)
                        {
                            if (varDec.Initializer != null && !canBeInitialized)
                                AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2200", "Variable cannot be initialized.", varDec.Initializer));

                            BuildVariableDeclaration(variable, variable.Field.Declaration.Type, typeName, GetVariableName(varDec.Identifier.ValueText, variable.Field.Declaration.Type), varDec.Initializer);
                        }
                    else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3000", $"Unknown Type: {variable.Field.Declaration.Type}", variable.Field.Declaration));
                }

                BuildReadabilityLineSeparator();
            }
        }
        protected abstract void BuildVariableDeclaration(ShaderVariableDeclaration variable, TypeSyntax typeSyntax, string type, string name, EqualsValueClauseSyntax? initializer);

        protected virtual void BuildMethod(MethodDeclarationSyntax method, string name)
        {
            if (method.Body != null || method.ExpressionBody != null && method.ExpressionBody.Expression != null)
            {
                if (TryGetTypeName(method.ReturnType, out string? returnType))
                {
                    BuildMethodInterface(returnType, name, method.ParameterList);

                    if (method.Body != null)
                        BuildMethodBody(method.Identifier.Text, method.Body.Statements);
                    else if (method.ExpressionBody != null && method.ExpressionBody.Expression != null)
                        BuildMethodBody(method.Identifier.Text, returnType != "void", method.ExpressionBody.Expression);
                }
                else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3000", $"Unknown Type: {method.ReturnType}", method));
            }
        }
        protected abstract void BuildMethodInterface(string type, string name, ParameterListSyntax parameters);
        protected virtual void BuildMethodBody(string name, IEnumerable<StatementSyntax> statements)
        {
            BuildMethodBodyBegin();

            bool isMainMethod = name.ToLower() == "main";
            if (isMainMethod)
                BuildEntryMethodBodyPrefixStatements();

            foreach (StatementSyntax statement in statements)
            {
                if (_cancellationToken.IsCancellationRequested)
                    return;
                BuildMethodBodyStatement(statement);
            }

            if (isMainMethod) //TODO: If there was a return then this will not be executed...
                BuildEntryMethodBodyPostfixStatements();

            BuildMethodBodyEnd();
        }
        protected virtual void BuildMethodBody(string name, bool isReturn, ExpressionSyntax expression)
        {
            BuildMethodBodyBegin();

            bool isMainMethod = name.ToLower() == "main";
            if (isMainMethod)
                BuildEntryMethodBodyPrefixStatements();

            BuildMethodBodySingleStatementExpression(expression, isReturn);

            /*if (isMainMethod) //TODO: If there was a return then this will not be executed...
                BuildMainMethodBodyPostfixStatements();*/

            BuildMethodBodyEnd();
        }
        protected abstract void BuildMethodBodyBegin();
        protected virtual void BuildEntryMethodBodyPrefixStatements() { }
        protected virtual void BuildEntryMethodBodyPostfixStatements() { }
        protected abstract void BuildMethodBodyEnd();

        protected virtual void BuildMethodBodyStatement(StatementSyntax statement)
        {
            if (_cancellationToken.IsCancellationRequested)
                return;

            try
            {
                switch (statement)
                {
                    case ExpressionStatementSyntax expression: BuildStatement(expression); break;
                    case LocalDeclarationStatementSyntax localDeclaration: BuildStatement(localDeclaration); break;
                    case ReturnStatementSyntax returnStatement: BuildStatement(returnStatement); break;
                    case BreakStatementSyntax breakStatement: BuildStatement(breakStatement); break;
                    case ContinueStatementSyntax continueStatement: BuildStatement(continueStatement); break;
                    case IfStatementSyntax ifStatement: BuildStatement(ifStatement); break;
                    case ForStatementSyntax forStatement: BuildStatement(forStatement); break;
                    case WhileStatementSyntax whileStatement: BuildStatement(whileStatement); break;
                    case DoStatementSyntax doStatement: BuildStatement(doStatement); break;
                    case BlockSyntax block: BuildStatement(block); break;
                    default: AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2000", $"Unknown Statement ({statement.GetType().Name}): {statement}", statement)); break;
                }
            }
            catch (Exception ex)
            {
                AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2004", $"Unexpected error during Statement Generation ({statement.GetText().ToString().LinesToListing()}): {ex.Message}", statement));
            }

        }
        protected abstract void BuildMethodBodySingleStatementExpression(ExpressionSyntax expression, bool isReturn);
        protected abstract void BuildStatement(ExpressionStatementSyntax expression);
        protected abstract void BuildStatement(LocalDeclarationStatementSyntax localDeclaration);
        protected abstract void BuildLocalDeclarationStatement(TypeSyntax type, string typeName, string variableName);
        protected abstract void BuildLocalDeclarationStatement(TypeSyntax type, string variableName);
        protected abstract void BuildStatement(ReturnStatementSyntax returnStatement);
        protected abstract void BuildStatement(BreakStatementSyntax breakStatement);
        protected abstract void BuildStatement(ContinueStatementSyntax continueStatement);
        protected abstract void BuildStatement(IfStatementSyntax ifStatement);
        protected abstract void BuildStatement(ForStatementSyntax forStatement);
        protected abstract void BuildStatement(WhileStatementSyntax whileStatement);
        protected abstract void BuildStatement(DoStatementSyntax doStatement);
        protected abstract void BuildStatement(BlockSyntax block);


        protected virtual void BuildExpression(ExpressionSyntax expression)
        {
            try
            {
                switch (expression)
                {
                    case AssignmentExpressionSyntax assignment: BuildExpression(assignment); break;
                    case ArrayCreationExpressionSyntax arrayCreation: BuildExpression(arrayCreation); break;
                    case OmittedArraySizeExpressionSyntax omittedArraySize: break; //Found no use for it in the generation so far, but needs to handle it
                    case InitializerExpressionSyntax initializer: BuildExpression(initializer); break;
                    case ObjectCreationExpressionSyntax objectCreation: BuildExpression(objectCreation); break;
                    case PrefixUnaryExpressionSyntax prefixUnaryExpression: BuildExpression(prefixUnaryExpression); break;
                    case PostfixUnaryExpressionSyntax postfixUnaryExpression: BuildExpression(postfixUnaryExpression); break;
                    case LiteralExpressionSyntax literalExpression: BuildExpression(literalExpression); break;
                    case SimpleNameSyntax simpleName: BuildExpression(simpleName); break; //Base of IdentifierName
                    case ElementAccessExpressionSyntax elementAccess: BuildExpression(elementAccess); break;
                    case InvocationExpressionSyntax invocation: BuildExpression(invocation); break;
                    case MemberAccessExpressionSyntax memberAccess: BuildExpression(memberAccess); break;
                    case BinaryExpressionSyntax binaryExpression: BuildExpression(binaryExpression); break;
                    case CastExpressionSyntax castExpression: BuildExpression(castExpression); break;
                    case ParenthesizedExpressionSyntax parenthesizedExpression: BuildExpression(parenthesizedExpression); break;
                    default: AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2001", $"Unknown Expression ({expression.GetType().Name}): {expression}", expression)); break;
                }
            }
            catch (Exception ex)
            {
                AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2005", $"Unexpected error during Expression Generation ({expression.GetText().ToString().LinesToListing()}): {ex.Message}", expression));
            }
        }

        protected abstract void BuildExpression(AssignmentExpressionSyntax assignment);
        protected abstract void BuildExpression(ArrayCreationExpressionSyntax arrayCreation);
        protected abstract void BuildExpression(InitializerExpressionSyntax initializer);

        protected virtual void BuildExpression(ObjectCreationExpressionSyntax objectCreation)
        {
            if (TryGetTypeSymbol(objectCreation.Type, out ITypeSymbol? typeSymbol) && TryGetShaderTypes(objectCreation.ArgumentList, out IReadOnlyList<ShaderType>? constructorArguments))
            {
                if (TryGetShaderTypeMapping(typeSymbol, out ShaderTypeMapping? mapping))
                {
                    //Find Mapping for this constructor
                    foreach (ShaderTypeConstructorMapping constructorMapping in mapping.ConstructorMappings)
                    {
                        if (ASTAnalyzerHelper.IsSameMapping(null, constructorMapping.Arguments, constructorArguments))
                        {
                            if (TryReorderMappedArguments(constructorMapping.Arguments, null, objectCreation.ArgumentList?.Arguments, objectCreation, out IReadOnlyList<ExpressionSyntax>? reorderedArguments))
                                BuildShaderExpression(objectCreation, constructorMapping.ExpressionMapping, null, reorderedArguments);
                            return;
                        }
                    }

                    AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2100", $"Unsupported Initializer: {objectCreation.GetText()}", objectCreation));
                }
                else //User struct
                {
                    if (TryGenerateStruct(typeSymbol, out UserStructShaderType? generatedStruct))
                    {
                        if (objectCreation.ArgumentList != null && objectCreation.ArgumentList.Arguments.Count > 0)
                            BuildShaderExpression(objectCreation, generatedStruct.Constructors.Values.First(), objectCreation.ArgumentList.Arguments.Select(a => a.Expression).ToList());
                        else BuildShaderExpression(objectCreation, generatedStruct.Constructors.Values.First());
                    }
                    else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3000", $"Unknown Type: {typeSymbol.Name}", typeSymbol));
                }
            }
        }

        protected virtual void BuildExpression(PrefixUnaryExpressionSyntax prefixUnary) => BuildExpression(prefixUnary, prefixUnary.Operand, prefixUnary.OperatorToken.Text, true);
        protected virtual void BuildExpression(PostfixUnaryExpressionSyntax postfixUnary) => BuildExpression(postfixUnary, postfixUnary.Operand, postfixUnary.OperatorToken.Text, false);
        protected virtual void BuildExpression(ExpressionSyntax unaryOperator, ExpressionSyntax operand, string op, bool isPrefix)
        {
            if (TryGetTypeSymbol(operand, out ITypeSymbol? operandTypeSymbol) && TryGetShaderTypeMapping(operandTypeSymbol, out ShaderTypeMapping? mapping))
            {
                if (mapping.UnaryOperatorMappings.TryGetValue(op, out ShaderTypeUnaryOperatorMapping? operatorMapping) && ((isPrefix && operatorMapping.IsPrefix) || (!isPrefix && operatorMapping.IsPostfix)))
                    BuildShaderExpression(unaryOperator, operatorMapping.ExpressionMapping, null, new ExpressionSyntax[] { operand });
                else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3610", $"Unknown operator, operator ({(isPrefix ? $"{op}{operandTypeSymbol.Name}" : $"{operandTypeSymbol.Name}{op}")}) is not mapped: {unaryOperator.GetText()}", unaryOperator));
            }
        }
        protected virtual void BuildExpression(BinaryExpressionSyntax binaryExpression)
        {
            if (TryGetTypeSymbol(binaryExpression.Left, out ITypeSymbol? leftOperandTypeSymbol) && TryGetShaderTypeMapping(leftOperandTypeSymbol, out ShaderTypeMapping? leftMapping) &&
                TryGetTypeSymbol(binaryExpression.Right, out ITypeSymbol? rightOperandTypeSymbol) && TryGetShaderTypeMapping(rightOperandTypeSymbol, out ShaderTypeMapping? rightMapping) &&
                leftMapping.TypeMapping != null && rightMapping.TypeMapping != null)
            {
                if (ASTAnalyzerHelper.TryGetOperatorMapping(leftMapping.BinaryOperatorMappings, binaryExpression.OperatorToken.Text, leftMapping.TypeMapping, rightMapping.TypeMapping, out ShaderTypeBinaryOperatorMapping? operatorMapping))
                    BuildShaderExpression(binaryExpression, operatorMapping.ExpressionMapping, null, operatorMapping.SwapOperands ? new ExpressionSyntax[] { binaryExpression.Right, binaryExpression.Left } : new ExpressionSyntax[] { binaryExpression.Left, binaryExpression.Right });
                else if (ASTAnalyzerHelper.TryGetOperatorMapping(rightMapping.BinaryOperatorMappings, binaryExpression.OperatorToken.Text, leftMapping.TypeMapping, rightMapping.TypeMapping, out operatorMapping))
                    BuildShaderExpression(binaryExpression, operatorMapping.ExpressionMapping, null, operatorMapping.SwapOperands ? new ExpressionSyntax[] { binaryExpression.Right, binaryExpression.Left } : new ExpressionSyntax[] { binaryExpression.Left, binaryExpression.Right });
                else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3610", $"Unknown operator, operator ({leftMapping.TypeMapping}{binaryExpression.OperatorToken.Text}{rightMapping.TypeMapping}) is not mapped: {binaryExpression.GetText()}", binaryExpression));
            }
            //TODO: else custom operator
        }

        protected virtual void BuildExpression(LiteralExpressionSyntax literal)
        {
            if (literal.Token.Value != null)
            {
                switch (literal.Kind())
                {
                    case SyntaxKind.TrueLiteralExpression: BuildBoolLiteral(true); break;
                    case SyntaxKind.FalseLiteralExpression: BuildBoolLiteral(false); break;

                    case SyntaxKind.NumericLiteralExpression:
                        switch (literal.Token.Value)
                        {
                            case float single: BuildFloatLiteral(single); break;
                            case int int32: BuildIntLiteral(int32); break;
                            case double d: BuildDoubleLiteral(d); break;

                            //Support for other literals? Converting them to another type?
                            //case Decimal dec: break;
                            //case DateTime dateTime: break;
                            default: AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3026", $"Unsupported Literal: {literal.Token.Value.GetType().Name}", literal)); break;
                        }
                        break;

                    //TODO: Implement support for DefaultLiteralExpression
                    //case SyntaxKind.DefaultLiteralExpression:

                    case SyntaxKind.CharacterLiteralExpression:
                    case SyntaxKind.StringLiteralExpression:
                    case SyntaxKind.NullLiteralExpression:
                        AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3026", $"Unsupported Literal: {literal.Kind()}", literal)); break;

                    default: AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3025", $"Unknown Literal: {literal.Kind()}", literal)); break;
                }
            }
            else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3025", "Unknown Literal: " + literal.Kind(), literal));
        }
        protected abstract void BuildBoolLiteral(bool literal);
        protected abstract void BuildDoubleLiteral(double literal);
        protected abstract void BuildFloatLiteral(float literal);
        protected abstract void BuildIntLiteral(int literal);

        protected virtual void BuildExpression(SimpleNameSyntax simpleName) //Base of IdentifierName
        {
            if (TryGetSymbol(simpleName, out ISymbol? symbol))
            {
                switch (symbol)
                {
                    case IFieldSymbol:
                    case IMethodSymbol:
                        if (symbol.ContainingType != null) //Is this a member access invocation? //TODO: it will return the type where the function/field is used, not where it is declared...
                            BuildMemberAccessExpression(simpleName, symbol.ContainingType, symbol, null, null);
                        //TODO: else?
                        else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("TODO", $"Unknown TODO ({simpleName.Identifier.Text}).", simpleName.Identifier));
                        break;

                    case ILocalSymbol localSymbol: BuildShaderIdentifierNameExpression(simpleName, localSymbol); break;
                    case IParameterSymbol parameterSymbol: BuildShaderIdentifierNameExpression(simpleName, parameterSymbol); break;

                    default: AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2003", $"Unsupported Symbol Kind ({symbol.Kind}) in Identifier Name: {simpleName.Identifier.Text}", simpleName.Identifier)); break;
                }
            }
        }

        protected abstract void BuildExpression(ElementAccessExpressionSyntax elementAccess);

        protected virtual void BuildExpression(InvocationExpressionSyntax invocation)
        {
            switch (invocation.Expression)
            {
                case MemberAccessExpressionSyntax memberAccess: BuildInvocationExpression(memberAccess, invocation.ArgumentList); break;
                case IdentifierNameSyntax identifierName: BuildInvocationExpression(identifierName, invocation.ArgumentList); break;
                default: AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2001", $"Unknown Expression at invocation ({invocation.Expression.GetType().Name}): {invocation.Expression}", invocation.Expression)); break;
            }
        }
        protected virtual void BuildInvocationExpression(MemberAccessExpressionSyntax memberAccess, ArgumentListSyntax? argumentList)
        {
            if (TryGetMemberSymbol(memberAccess, out ITypeSymbol? ownerTypeSymbol, out ISymbol? memberSymbol))
                BuildMemberAccessExpression(memberAccess, ownerTypeSymbol, memberSymbol, memberAccess.Expression, argumentList);
        }
        protected virtual void BuildInvocationExpression(IdentifierNameSyntax identifierName, ArgumentListSyntax? argumentList)
        {
            if (TryGetSymbol(identifierName, out ISymbol? symbol))
            {
                if (symbol.ContainingType != null) //Is this a member access invocation?
                    BuildMemberAccessExpression(identifierName, symbol.ContainingType, symbol, null, argumentList);
                //TODO: else?
                else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("TODO", $"Unknown TODO ({identifierName.Identifier.Text}) at Invocation.", identifierName.Identifier));
            }
        }

        protected virtual void BuildExpression(MemberAccessExpressionSyntax memberAccess)
        {
            if (TryGetMemberSymbol(memberAccess, out ITypeSymbol? ownerTypeSymbol, out ISymbol? memberSymbol))
            {
                if (TryGetShaderTypeMapping(ownerTypeSymbol, out ShaderTypeMapping? mapping)) //Mapped Expression
                {
                    switch (memberSymbol)
                    {
                        case IFieldSymbol fieldSymbol: //Member is a Field
                            if (mapping.FieldMappings.TryGetValue(memberSymbol.Name, out ShaderTypeFieldMapping? fieldMapping))
                            {
                                BuildShaderExpression(memberAccess, fieldMapping.ExpressionMapping, memberAccess.Expression);
                                return;
                            }
                            else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3005", $"Unknown Member Field ({memberAccess.Name}) at Member Access: {memberAccess.GetText().ToString().Trim().LinesToListing()}", memberAccess));
                            break;

                        default:
                            AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3009", $"Unsupported Member Access Kind at Member Access: {memberSymbol.Kind}", memberAccess));
                            return;
                    }
                }
                else //Custom struct member access
                {
                    switch (memberSymbol)
                    {
                        case IFieldSymbol memberFieldSymbol: //Member is a Field
                            BuildMemberFieldAccessExpression(memberAccess.Expression, memberFieldSymbol.Name);
                            break;

                        default:
                            AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3009", $"Unsupported Member Access Kind at Member Access: {memberSymbol.Kind}", memberAccess));
                            break;
                    }
                }
            }
            else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3004", $"Unknown Member Access: {memberAccess.GetText().ToString().LinesToListing()}", memberAccess));
        }
        protected abstract void BuildMemberFieldAccessExpression(ExpressionSyntax ownerExpression, string fieldName);
        protected virtual void BuildMemberAccessExpression(ExpressionSyntax originalExpression, ITypeSymbol ownerTypeSymbol, ISymbol memberSymbol, ExpressionSyntax? invoker, ArgumentListSyntax? argumentList)
        {
            if (TryGetShaderTypes(argumentList, out IReadOnlyList<ShaderType>? argumentTypes))
            {
                if (TryGetShaderTypeMapping(ownerTypeSymbol, out ShaderTypeMapping? mapping)) //Mapped Expression
                {
                    switch (memberSymbol)
                    {
                        case IMethodSymbol memberMethodSymbol: //Member is a Method
                            foreach (ShaderTypeMethodMapping methodMapping in mapping.MethodMappings) //Mapped Method
                            {
                                if (methodMapping.MethodName == memberSymbol.Name && ASTAnalyzerHelper.IsSameMapping(mapping.TypeMapping, methodMapping.Arguments, argumentTypes))
                                {
                                    if (TryReorderMappedArguments(methodMapping.Arguments, invoker, argumentList?.Arguments, originalExpression, out IReadOnlyList<ExpressionSyntax>? reorderedArguments))
                                    {
                                        BuildShaderExpression(originalExpression, methodMapping.ExpressionMapping, invoker, reorderedArguments);
                                        return;
                                    }
                                }
                            }
                            if (TryGenerateFunction(memberMethodSymbol, originalExpression.GetLocation(), out GeneratedFunction generatedFunction)) //Custom function in Shader Class
                                BuildShaderFunctionCallExpression(generatedFunction, GetFunctionCallArguments(generatedFunction, invoker, argumentList?.Arguments));
                            break;

                        case IFieldSymbol memberFieldSymbol: //Member is a Field
                            if (mapping.FieldMappings.TryGetValue(memberSymbol.Name, out ShaderTypeFieldMapping? fieldMapping)) //Mapped Field
                                BuildShaderExpression(originalExpression, fieldMapping.ExpressionMapping, invoker);
                            else if (mapping == _mappings.ShaderClassMapping) //Field in Shader Class
                                BuildShaderIdentifierNameExpression(originalExpression, memberFieldSymbol);
                            //TODO: else? is it possible/supported?
                            else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3005", $"Unknown Member Field ({memberSymbol.Name}) at Member Access: {originalExpression.GetText().ToString().Trim().LinesToListing()}", originalExpression));
                            break;

                        default: //Member Kind is unsupported
                            AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3009", $"Unsupported Member Access Kind at Member Access: {memberSymbol.Kind}", originalExpression));
                            return;
                    }
                }
                else //User struct method or others
                {
                    switch (memberSymbol)
                    {
                        case IMethodSymbol memberMethodSymbol:
                            if (TryGenerateFunction(memberMethodSymbol, originalExpression.GetLocation(), out GeneratedFunction generatedFunction)) //Custom function of custom struct
                                BuildShaderFunctionCallExpression(generatedFunction, GetFunctionCallArguments(generatedFunction, invoker, argumentList?.Arguments));
                            break;

                        default:
                            AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3005", $"Unknown Member ({memberSymbol.Name}) at Member Access: {originalExpression.GetText().ToString().Trim().LinesToListing()}", originalExpression));
                            return;
                    }
                }
            }
        }

        protected abstract void BuildExpression(CastExpressionSyntax castExpression);
        protected abstract void BuildExpression(ParenthesizedExpressionSyntax parenthesized);


        protected virtual void BuildShaderExpression(ExpressionSyntax originalExpression, IShaderExpression shaderExpression, ExpressionSyntax? invoker)
        {
            switch (shaderExpression)
            {
                case ShaderConstructorExpression constructorExpression: BuildShaderExpression(originalExpression, constructorExpression); break;
                case ShaderUnaryOperatorExpression unaryOperatorExpression: AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3611", $"Incorrect Operator mapping, Unary operator supports only a single Operand: {originalExpression.GetText()}", originalExpression)); break;
                case ShaderBinaryOperatorExpression binaryOperatorExpression: AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3612", $"Incorrect Operator mapping, Binary operator supports only two Operands: {originalExpression.GetText()}", originalExpression)); break;
                case ShaderMemberAccessExpression memberAccessExpression:
                    if (invoker != null)
                        BuildShaderExpression(originalExpression, memberAccessExpression, invoker);
                    else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3606", $"Incorrect Expression mapping, only Invocation is supported: {originalExpression.GetText()}", originalExpression));
                    break;

                case ShaderFunctionExpression shaderFunctionExpression: BuildShaderExpression(originalExpression, shaderFunctionExpression); break;
                case ShaderStatementExpression shaderStatementExpression: BuildShaderExpression(originalExpression, shaderStatementExpression); break;
                default:
                    AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2001", $"Unknown Expression {originalExpression.GetText()}", originalExpression));
                    break;
            }
        }
        protected virtual void BuildShaderExpression(ExpressionSyntax originalExpression, IShaderExpression shaderExpression, ExpressionSyntax? invoker, IReadOnlyList<ExpressionSyntax> argumentExpressions)
        {
            switch (shaderExpression)
            {
                case ShaderConstructorExpression constructorExpression: BuildShaderExpression(originalExpression, constructorExpression, argumentExpressions); break;
                case ShaderUnaryOperatorExpression unaryOperatorExpression:
                    if (argumentExpressions.Count == 1)
                        BuildShaderExpression(originalExpression, unaryOperatorExpression, argumentExpressions[0]);
                    else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3611", $"Incorrect Operator mapping, Unary operator supports only a single Operand: {originalExpression.GetText()}", originalExpression));
                    break;

                case ShaderBinaryOperatorExpression binaryOperatorExpression:
                    if (argumentExpressions.Count == 2)
                        BuildShaderExpression(originalExpression, binaryOperatorExpression, argumentExpressions[0], argumentExpressions[1]);
                    else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3612", $"Incorrect Operator mapping, Binary operator supports only two Operands: {originalExpression.GetText()}", originalExpression));
                    break;

                case ShaderMemberAccessExpression memberAccessExpression:
                    if (invoker != null)
                    {
                        if (argumentExpressions.Count == 0)
                            BuildShaderExpression(originalExpression, memberAccessExpression, invoker);
                        else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3620", $"Incorrect Member Access mapping, Member Access does not support arguments: {originalExpression.GetText()}", originalExpression));
                    }
                    else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3606", $"Incorrect Expression mapping, only Invocation is supported: {originalExpression.GetText()}", originalExpression));
                    break;

                case ShaderFunctionExpression shaderFunctionExpression: BuildShaderExpression(originalExpression, shaderFunctionExpression, argumentExpressions); break;
                case ShaderStatementExpression shaderStatementExpression:
                    if (argumentExpressions.Count == 0)
                        BuildShaderExpression(originalExpression, shaderStatementExpression);
                    else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3640", $"Incorrect Statement mapping, Statements do not support arguments: {originalExpression.GetText()}", originalExpression));
                    break;

                default:
                    AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2001", $"Unknown Expression {originalExpression.GetText()}", originalExpression));
                    break;
            }
        }

        protected abstract void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderConstructorExpression shaderExpression);
        protected abstract void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderConstructorExpression shaderExpression, IReadOnlyList<ExpressionSyntax> argumentExpressions);

        protected abstract void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderUnaryOperatorExpression shaderExpression, ExpressionSyntax expression);
        protected abstract void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderBinaryOperatorExpression shaderExpression, ExpressionSyntax leftExpression, ExpressionSyntax rightExpression);

        protected abstract void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderMemberAccessExpression shaderExpression, ExpressionSyntax invoker);

        protected abstract void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderFunctionExpression shaderExpression);
        protected abstract void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderFunctionExpression shaderExpression, IReadOnlyList<ExpressionSyntax> argumentExpressions);

        protected abstract void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderStatementExpression shaderExpression);

        protected abstract void BuildShaderIdentifierNameExpression(ExpressionSyntax originalExpression, ISymbol symbol);
        protected abstract void BuildShaderFunctionCallExpression(in GeneratedFunction generatedFunction, IReadOnlyList<ExpressionSyntax> arguments);

        #endregion

        #region Public Methods

        public virtual IShaderBuilder BuildGraphics(ShaderClassDeclaration shaderClass)
        {
            _shaderClass = shaderClass;

            _compilation = shaderClass.Compilation;
            _cancellationToken = shaderClass.CancellationToken;

            try
            {
                BuildPreprocessorBegin();
                BuildReadabilityLineSeparator();

                RegisterStructDeclarationInsertIndex();
                BuildReadabilityLineSeparator();

                if (!_cancellationToken.IsCancellationRequested)
                    BuildVariables(shaderClass);
                BuildReadabilityLineSeparator();

                RegisterFunctionDeclarationInsertIndex();
                BuildReadabilityLineSeparator();

                if (shaderClass.EntryMethod != null && !_cancellationToken.IsCancellationRequested)
                    BuildMethod(shaderClass.EntryMethod, "main");
                BuildReadabilityLineSeparator();
            }
            catch (Exception e)
            {
                string message = e.Message.LinesToListing();
                if (e.StackTrace != null)
                    message += " --- " + e.StackTrace.ToString().LinesToListing();
                AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("1900", $"Internal error during Shader Generation: {message}", shaderClass.ClassDeclaration.Identifier));
            }

            if (_cancellationToken.IsCancellationRequested)
                _isFailed = true;
            return this;
        }
        public void ReportDiagnostics(IDiagnosticReporter diagnosticReporter)
        {
            foreach (ShaderGenerationDiagnositcs diagnositcs in _diagnostics)
                diagnosticReporter.Report(diagnositcs, $"({BackendName}) ");
        }

        public abstract IGeneratedShaderSource GetShaderSource();
        public abstract IGeneratedShaderSource GetShaderSourceEmpty();

        #endregion

    }
}
