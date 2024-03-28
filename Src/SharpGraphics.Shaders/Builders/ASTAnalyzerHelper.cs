using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using SharpGraphics.Shaders.Mappings;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using SharpGraphics.Shaders.Generator;

namespace SharpGraphics.Shaders.Builders
{
    public static class ASTAnalyzerHelper
    {

        public static bool IsSameMapping(ShaderType? invoker, in ShaderArgumentMappings mapping, IReadOnlyList<ShaderType> arguments)
        {
            //Is invoker required and provided?
            if (mapping.invokerMappedToArgumentIndex >= 0)
            {
                if (invoker == null)
                    return false;
            }

            //Are all arguments compatible?
            ReadOnlySpan<ShaderArgumentMapping> argumentMappings = mapping.Arguments;
            if (arguments.Count != argumentMappings.Length)
                return false;

            for (int i = 0; i < arguments.Count; i++)
                if (!argumentMappings[i].type.IsImplicitConvertableTo(arguments[i]))
                    return false;

            return true;
        }

        public static bool TryGetOperatorMapping(IEnumerable<ShaderTypeBinaryOperatorMapping> mappings, string op, ShaderType left, ShaderType right, [NotNullWhen(returnValue: true)] out ShaderTypeBinaryOperatorMapping? result)
        {
            foreach (ShaderTypeBinaryOperatorMapping operatorMapping in mappings)
                if (operatorMapping.Operator == op &&
                    ((operatorMapping.Left.IsImplicitConvertableTo(left) && operatorMapping.Right.IsImplicitConvertableTo(right)) ||
                     (operatorMapping.IsAssociative && operatorMapping.Left.IsImplicitConvertableTo(right) && operatorMapping.Right.IsImplicitConvertableTo(left))))
                {
                    result = operatorMapping;
                    return true;
                }

            result = null;
            return false;
        }


        public static bool TryGetSymbol(Compilation compilation, SyntaxNode node, CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out ISymbol? symbol)
        {
            SemanticModel model = compilation.GetSemanticModel(node.SyntaxTree);
            SymbolInfo symbolInfo = model.GetSymbolInfo(node, cancellationToken);
            if (symbolInfo.Symbol != null)
            {
                symbol = symbolInfo.Symbol;
                return true;
            }

            symbol = null;
            return false;
        }
        public static bool TryGetTypeSymbol(Compilation compilation, SyntaxNode node, CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out ITypeSymbol? resultTypeSymbol)
        {
            SemanticModel model = compilation.GetSemanticModel(node.SyntaxTree);

            ITypeSymbol? typeSymbol = model.GetTypeInfo(node, cancellationToken).Type;
            if (typeSymbol != null)
            {
                resultTypeSymbol = typeSymbol;
                return true;
            }
            else
            {
                SymbolInfo typeSymbolInfo = model.GetSymbolInfo(node, cancellationToken);
                if (typeSymbolInfo.Symbol != null)
                    switch (typeSymbolInfo.Symbol)
                    {
                        case IAliasSymbol alias: resultTypeSymbol = alias.Target as ITypeSymbol; return resultTypeSymbol != null;
                        //TODO: case IArrayTypeSymbol array: resultTypeSymbol = array.ElementType; return true;
                        //TODO: case IDiscardSymbol assembly: resultTypeSymbol = null; return false;
                        case IDynamicTypeSymbol dynamic: resultTypeSymbol = dynamic.OriginalDefinition; return true;
                        case IFieldSymbol field: resultTypeSymbol = field.Type; return true;
                        case ILocalSymbol local: resultTypeSymbol = local.Type; return true;
                        case IMethodSymbol method: resultTypeSymbol = method.MethodKind == MethodKind.Constructor ? method.ReceiverType : method.ReturnType; return resultTypeSymbol != null;
                        case INamedTypeSymbol namedType: resultTypeSymbol = namedType; return true;
                        case INamespaceOrTypeSymbol typeOrNamespace: resultTypeSymbol = typeOrNamespace as ITypeSymbol; return resultTypeSymbol != null;
                        case IParameterSymbol parameter: resultTypeSymbol = parameter.Type; return true;
                        case IPropertySymbol property: resultTypeSymbol = property.Type; return true;
                    }
            }

            resultTypeSymbol = null;
            return false;
        }
        public static bool TryGetTypeSymbol(Compilation compilation, TypeSyntax type, CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out ITypeSymbol? resultTypeSymbol)
        {
            SemanticModel model = compilation.GetSemanticModel(type.SyntaxTree);

            ITypeSymbol? typeSymbol = model.GetTypeInfo(type, cancellationToken).Type;
            if (typeSymbol != null)
            {
                resultTypeSymbol = typeSymbol;
                return true;
            }
            else
            {
                SymbolInfo typeSymbolInfo = model.GetSymbolInfo(type, cancellationToken);
                if (typeSymbolInfo.Symbol != null && typeSymbolInfo.Symbol is ITypeSymbol typeSymbolFallback)
                {
                    resultTypeSymbol = typeSymbolFallback;
                    return true;
                }
            }

            resultTypeSymbol = null;
            return false;
        }

        public static bool IsPartialClassDeclaration(ClassDeclarationSyntax classDeclarationSyntax)
            => classDeclarationSyntax.Modifiers.Any(m => m.Text.ToLower() == "partial");

        public static bool IsInheritedFrom(ITypeSymbol derived, ITypeSymbol b)
        {
            if (SymbolEqualityComparer.IncludeNullability.Equals(derived, b))
                return true;
            else if (derived.BaseType != null)
                return IsInheritedFrom(derived.BaseType, b);
            else return false;
        }
        public static bool TryGetShaderTypeMapping(Compilation compilation, ShaderLanguageMappings mappings, ITypeSymbol shaderClassTypeSymbol, TypeSyntax type, CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out ShaderTypeMapping? mapping)
        {
            if (TryGetTypeSymbol(compilation, type, cancellationToken, out ITypeSymbol? typeSymbol))
                return TryGetShaderTypeMapping(mappings, shaderClassTypeSymbol, typeSymbol, out mapping);
            else
            {
                mapping = null;
                return false;
            }
        }
        public static bool TryGetShaderTypeMapping(ShaderLanguageMappings mappings, ITypeSymbol shaderClassTypeSymbol, ITypeSymbol type, [NotNullWhen(returnValue: true)] out ShaderTypeMapping? mapping)
        {
            if (IsInheritedFrom(shaderClassTypeSymbol, type))
            {
                mapping = mappings.ShaderClassMapping;
                return true;
            }
            else if (mappings.SpecialTypeMappings.TryGetValue(type.SpecialType, out mapping))
                return true;
            else if (mappings.TypeMappings.TryGetValue(type.ToDisplayString().Split('<')[0], out mapping))
                return true;
            else
            {
                mapping = null;
                return false;
            }
        }

    }
}
