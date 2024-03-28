using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGraphics.Shaders.Reporters;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

namespace SharpGraphics.Shaders.Generator
{
    public static class GeneratorUtils
    {

        #region Fields

        private static readonly SymbolDisplayFormat TYPE_NAME_FORMAT = new SymbolDisplayFormat(
            SymbolDisplayGlobalNamespaceStyle.Omitted,
            SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        #endregion

        #region Private Methods

        private static int GetDiagnosticsWarningLevel(DiagnosticSeverity severity)
            => severity switch
            {
                DiagnosticSeverity.Error => 0,
                DiagnosticSeverity.Warning => 1,
                DiagnosticSeverity.Info => 2,
                _ => 3,
            };

        private static bool TryGetAttributeDataFromNode(this SyntaxNode source, string attributeFullName, Compilation compilation, CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out AttributeData? result)
        {
            INamedTypeSymbol? attributeTypeName = compilation.GetTypeByMetadataName(attributeFullName);
            if (attributeTypeName != null)
                return TryGetAttributeDataFromNode(source, attributeTypeName, compilation, cancellationToken, out result);
            else
            {
                result = null;
                return false;
            }
        }
        private static bool TryGetAttributeDataFromNode(this SyntaxNode source, INamedTypeSymbol attributeNameSymbol, Compilation compilation, CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out AttributeData? result)
        {
            SemanticModel model = compilation.GetSemanticModel(source.SyntaxTree);
            ISymbol? sourceSymbol = model.GetDeclaredSymbol(source, cancellationToken);
            if (sourceSymbol != null)
            {
                foreach (AttributeData attributeData in sourceSymbol.GetAttributes())
                    if (attributeNameSymbol.Equals(attributeData.AttributeClass, SymbolEqualityComparer.Default))
                    {
                        result = attributeData;
                        return true;
                    }
            }

            result = null;
            return false;
        }
        private static IReadOnlyList<AttributeData> GetAttributeDatasFromNode(this SyntaxNode source, string attributeFullName, Compilation compilation, CancellationToken cancellationToken)
        {
            INamedTypeSymbol? attributeTypeName = compilation.GetTypeByMetadataName(attributeFullName);
            if (attributeTypeName != null)
                return GetAttributeDatasFromNode(source, attributeTypeName, compilation, cancellationToken);
            else return new List<AttributeData>();
        }
        private static IReadOnlyList<AttributeData> GetAttributeDatasFromNode(this SyntaxNode source, INamedTypeSymbol attributeNameSymbol, Compilation compilation, CancellationToken cancellationToken)
        {
            List<AttributeData> result = new List<AttributeData>();

            SemanticModel model = compilation.GetSemanticModel(source.SyntaxTree);
            ISymbol? sourceSymbol = model.GetDeclaredSymbol(source, cancellationToken);
            if (sourceSymbol != null)
            {
                foreach (AttributeData attributeData in sourceSymbol.GetAttributes())
                    if (attributeNameSymbol.Equals(attributeData.AttributeClass, SymbolEqualityComparer.Default))
                        result.Add(attributeData);
            }

            return result;
        }

        #endregion

        #region Public Methods

        public static string GetIndentation(int indentation) => new string('\t', indentation);
        public static StringBuilder AppendLine(this StringBuilder sb, char c) => sb.AppendLine(c.ToString());
        public static StringBuilder AppendIndentation(this StringBuilder sb, int indentation) => sb.Append('\t', indentation);
        public static StringBuilder AppendLineIndentation(this StringBuilder sb, int indentation) => sb.AppendIndentation(indentation).AppendLine();
        public static StringBuilder AppendIndentation(this StringBuilder sb, bool b, int indentation) => sb.AppendIndentation(indentation).Append(b);
        public static StringBuilder AppendIndentation(this StringBuilder sb, char c, int indentation) => sb.AppendIndentation(indentation).Append(c);
        public static StringBuilder AppendIndentation(this StringBuilder sb, byte b, int indentation) => sb.AppendIndentation(indentation).Append(b);
        public static StringBuilder AppendIndentation(this StringBuilder sb, int i, int indentation) => sb.AppendIndentation(indentation).Append(i);
        public static StringBuilder AppendIndentation(this StringBuilder sb, float f, int indentation) => sb.AppendIndentation(indentation).Append(f);
        public static StringBuilder AppendIndentation(this StringBuilder sb, string text, int indentation) => sb.AppendIndentation(indentation).Append(text);
        public static StringBuilder AppendLineIndentation(this StringBuilder sb, bool b, int indentation) => sb.AppendIndentation(indentation).Append(b).AppendLine();
        public static StringBuilder AppendLineIndentation(this StringBuilder sb, char c, int indentation) => sb.AppendIndentation(indentation).Append(c).AppendLine();
        public static StringBuilder AppendLineIndentation(this StringBuilder sb, byte b, int indentation) => sb.AppendIndentation(indentation).Append(b).AppendLine();
        public static StringBuilder AppendLineIndentation(this StringBuilder sb, int i, int indentation) => sb.AppendIndentation(indentation).Append(i).AppendLine();
        public static StringBuilder AppendLineIndentation(this StringBuilder sb, float f, int indentation) => sb.AppendIndentation(indentation).Append(f).AppendLine();
        public static StringBuilder AppendLineIndentation(this StringBuilder sb, string text, int indentation) => sb.AppendIndentation(indentation).AppendLine(text);




        public static bool HasAttribute(this ClassDeclarationSyntax source, string attributeFullName, SemanticModel model, CancellationToken cancellationToken)
        {
            foreach (AttributeListSyntax attributeList in source.AttributeLists)
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    TypeInfo typeInfo = model.GetTypeInfo(attribute, cancellationToken);
                    if (typeInfo.Type != null)
                    {
                        string typeName = typeInfo.Type.ToDisplayString(TYPE_NAME_FORMAT);
                        if (string.Equals(typeName, attributeFullName, StringComparison.Ordinal))
                            return true;
                    }
                }
            return false;
        }
        public static bool HasAttribute(this ClassDeclarationSyntax source, string[] attributeFullNames, SemanticModel model, CancellationToken cancellationToken)
        {
            foreach (AttributeListSyntax attributeList in source.AttributeLists)
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    TypeInfo typeInfo = model.GetTypeInfo(attribute, cancellationToken);
                    if (typeInfo.Type != null)
                    {
                        string typeName = typeInfo.Type.ToDisplayString(TYPE_NAME_FORMAT);
                        foreach (string fullName in attributeFullNames)
                            if (string.Equals(typeName, fullName, StringComparison.Ordinal))
                                return true;
                    }
                }
            return false;
        }
        public static bool HasAttribute(this MemberDeclarationSyntax source, string attributeFullName, SemanticModel model, CancellationToken cancellationToken)
        {
            foreach (AttributeListSyntax attributeList in source.AttributeLists)
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    TypeInfo typeInfo = model.GetTypeInfo(attribute, cancellationToken);
                    if (typeInfo.Type != null)
                    {
                        string typeName = typeInfo.Type.ToDisplayString(TYPE_NAME_FORMAT);
                        if (string.Equals(typeName, attributeFullName, StringComparison.Ordinal))
                            return true;
                    }
                }
            return false;
        }
        public static bool HasAttribute(this ISymbol symbol, string attributeFullName, SemanticModel model, CancellationToken cancellationToken)
        {
            foreach (AttributeData attributeData in symbol.GetAttributes())
                if (attributeData.AttributeClass != null)
                {
                    string typeName = attributeData.AttributeClass.ToDisplayString(TYPE_NAME_FORMAT);
                    if (string.Equals(typeName, attributeFullName, StringComparison.Ordinal))
                        return true;
                }
            return false;
        }
        public static bool HasAttribute(this MemberDeclarationSyntax source, string[] attributeFullNames, SemanticModel model, CancellationToken cancellationToken)
        {
            foreach (AttributeListSyntax attributeList in source.AttributeLists)
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    TypeInfo typeInfo = model.GetTypeInfo(attribute, cancellationToken);
                    if (typeInfo.Type != null)
                    {
                        string typeName = typeInfo.Type.ToDisplayString(TYPE_NAME_FORMAT);
                        foreach (string fullName in attributeFullNames)
                            if (string.Equals(typeName, fullName, StringComparison.Ordinal))
                                return true;
                    }
                }
            return false;
        }
        public static bool HasAttribute(this ISymbol symbol, string[] attributeFullNames, SemanticModel model, CancellationToken cancellationToken)
        {
            foreach (AttributeData attributeData in symbol.GetAttributes())
                if (attributeData.AttributeClass != null)
                {
                    string typeName = attributeData.AttributeClass.ToDisplayString(TYPE_NAME_FORMAT);
                    foreach (string fullName in attributeFullNames)
                        if (string.Equals(typeName, fullName, StringComparison.Ordinal))
                            return true;
                }
            return false;
        }
        public static int CountAttributes(this MemberDeclarationSyntax source, string[] attributeFullNames, SemanticModel model, CancellationToken cancellationToken)
        {
            int count = 0;
            foreach (AttributeListSyntax attributeList in source.AttributeLists)
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    TypeInfo typeInfo = model.GetTypeInfo(attribute, cancellationToken);
                    if (typeInfo.Type != null)
                    {
                        string typeName = typeInfo.Type.ToDisplayString(TYPE_NAME_FORMAT);
                        foreach (string fullName in attributeFullNames)
                            if (string.Equals(typeName, fullName, StringComparison.Ordinal))
                                ++count;
                    }
                }
            return count;
        }

        public static bool TryGetAttribute(this MemberDeclarationSyntax source, string attributeFullName, SemanticModel model, CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out AttributeSyntax? result)
        {
            foreach (AttributeListSyntax attributeList in source.AttributeLists)
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    TypeInfo typeInfo = model.GetTypeInfo(attribute, cancellationToken);
                    if (typeInfo.Type != null)
                    {
                        string typeName = typeInfo.Type.ToDisplayString(TYPE_NAME_FORMAT);
                        if (string.Equals(typeName, attributeFullName, StringComparison.Ordinal))
                        {
                            result = attribute;
                            return true;
                        }
                    }
                }

            result = null;
            return false;
        }
        public static IReadOnlyList<AttributeSyntax> GetAttributesMultiple(this MemberDeclarationSyntax source, string attributeFullName, SemanticModel model, CancellationToken cancellationToken)
        {
            List<AttributeSyntax> result = new List<AttributeSyntax>();

            foreach (AttributeListSyntax attributeList in source.AttributeLists)
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    TypeInfo typeInfo = model.GetTypeInfo(attribute, cancellationToken);
                    if (typeInfo.Type != null)
                    {
                        string typeName = typeInfo.Type.ToDisplayString(TYPE_NAME_FORMAT);
                        if (string.Equals(typeName, attributeFullName, StringComparison.Ordinal))
                            result.Add(attribute);
                    }
                }

            return result;
        }

        public static bool TryGetAttributeData(this FieldDeclarationSyntax source, IEnumerable<string> attributeFullNames, Compilation compilation, CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out AttributeData? result)
        {
            foreach (string attributeFullName in attributeFullNames)
                if (TryGetAttributeData(source, attributeFullName, compilation, cancellationToken, out result))
                    return true;
            
            result = null;
            return false;
        }
        public static bool TryGetAttributeData(this FieldDeclarationSyntax source, string attributeFullName, Compilation compilation, CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out AttributeData? result)
            => TryGetAttributeData(source.Declaration, attributeFullName, compilation, cancellationToken, out result);
        public static bool TryGetAttributeData(this VariableDeclarationSyntax source, string attributeFullName, Compilation compilation, CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out AttributeData? result)
        {
            if (source.Variables.Count > 0)
                return TryGetAttributeData(source.Variables[0], attributeFullName, compilation, cancellationToken, out result);
            else
            {
                result = null;
                return false;
            }
        }
        public static bool TryGetAttributeData(this VariableDeclaratorSyntax source, string attributeFullName, Compilation compilation, CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out AttributeData? result)
            => TryGetAttributeDataFromNode(source, attributeFullName, compilation, cancellationToken, out result);

        public static bool TryGetAttributeData(this FieldDeclarationSyntax source, INamedTypeSymbol attributeNameSymbol, Compilation compilation, CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out AttributeData? result)
            => TryGetAttributeData(source.Declaration, attributeNameSymbol, compilation, cancellationToken, out result);
        public static bool TryGetAttributeData(this VariableDeclarationSyntax source, INamedTypeSymbol attributeNameSymbol, Compilation compilation, CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out AttributeData? result)
        {
            if (source.Variables.Count > 0)
                return TryGetAttributeData(source.Variables[0], attributeNameSymbol, compilation, cancellationToken, out result);
            else
            {
                result = null;
                return false;
            }
        }
        public static bool TryGetAttributeData(this VariableDeclaratorSyntax source, INamedTypeSymbol attributeNameSymbol, Compilation compilation, CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out AttributeData? result)
            => TryGetAttributeDataFromNode(source, attributeNameSymbol, compilation, cancellationToken, out result);

        public static IReadOnlyList<AttributeData> GetAttributesData(this ClassDeclarationSyntax source, string attributeFullName, Compilation compilation, CancellationToken cancellationToken)
            => GetAttributeDatasFromNode(source, attributeFullName, compilation, cancellationToken);

        public static bool TryGetAttributePropertyValue<T>(this AttributeData? attribute, string argumentName, [NotNullWhen(returnValue: true)] out T? value)
        {
            if (attribute != null)
                foreach (KeyValuePair<string, TypedConstant> namedArgument in attribute.NamedArguments)
                    if (string.Equals(namedArgument.Key, argumentName, StringComparison.Ordinal) && namedArgument.Value.Value != null && namedArgument.Value.Value is T result)
                    {
                        value = result;
                        return true;
                    }

            value = default(T);
            return false;
        }
        public static bool TryGetAttributePropertyValueInt(this AttributeData? attribute, string argumentName, out int value)
        {
            if (attribute != null)
            {
                if (TryGetAttributePropertyValue(attribute, argumentName, out value))
                    return true;
                else if (TryGetAttributePropertyValue(attribute, argumentName, out uint uintValue))
                {
                    value = (int)uintValue;
                    return true;
                }
                else
                {
                    value = 0;
                    return false;
                }
            }
            else
            {
                value = 0;
                return false;
            }
        }

        public static bool TryGetAttributeConstructorValue<T>(this AttributeData? attribute, int constructorParameterIndex, [NotNullWhen(returnValue: true)] out T? value)
        {
            if (attribute != null && attribute.ConstructorArguments.Length > constructorParameterIndex)
            {
                switch (attribute.ConstructorArguments[constructorParameterIndex].Kind)
                {
                    case TypedConstantKind.Primitive:
                    case TypedConstantKind.Enum:
                    case TypedConstantKind.Type:
                        {
                            if (attribute.ConstructorArguments[constructorParameterIndex].Value != null && attribute.ConstructorArguments[constructorParameterIndex].Value is T result)
                            {
                                value = result;
                                return true;
                            }
                        }
                        break;

                    //case TypedConstantKind.Array:
                    //case TypedConstantKind.Error:
                    default:
                        value = default(T);
                        return false;
                }

            }

            value = default(T);
            return false;
        }
        public static bool TryGetAttributeConstructorValues<T>(this AttributeData? attribute, int constructorParameterIndex, [NotNullWhen(returnValue: true)] out T[]? value)
        {
            if (attribute != null && attribute.ConstructorArguments.Length > constructorParameterIndex)
            {
                switch (attribute.ConstructorArguments[constructorParameterIndex].Kind)
                {
                    case TypedConstantKind.Array:
                        {
                            if (attribute.ConstructorArguments[constructorParameterIndex].Values != null)
                            {
                                value = new T[attribute.ConstructorArguments[constructorParameterIndex].Values.Length];
                                for (int i = 0; i < value.Length; i++)
                                {
                                    if (attribute.ConstructorArguments[constructorParameterIndex].Values[i].Value != null && attribute.ConstructorArguments[constructorParameterIndex].Values[i].Value is T element)
                                        value[i] = element;
                                    else
                                    {
                                        value = null;
                                        return false;
                                    }
                                }
                                return true;
                            }
                        }
                        break;

                    //case TypedConstantKind.Primitive:
                    //case TypedConstantKind.Enum:
                    //case TypedConstantKind.Type:
                    //case TypedConstantKind.Error:
                    default:
                        value = null;
                        return false;
                }

            }

            value = null;
            return false;
        }

        public static bool TryGetNamespace(this TypeDeclarationSyntax declarationSyntax, SemanticModel model, CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out string? ns)
        {
            ISymbol? classSymbol = model.GetDeclaredSymbol(declarationSyntax, cancellationToken);
            if (classSymbol != null && classSymbol.ContainingNamespace != null && !string.IsNullOrWhiteSpace(classSymbol.ContainingNamespace.Name))
            {
                ns = classSymbol.ContainingNamespace.ToDisplayString();
                if (!string.IsNullOrWhiteSpace(ns))
                    return true;
            }

            ns = null;
            return false;
        }


        public static void ReportError(this IDiagnosticReporter diagnosticReporter, string id, string message, Location location)
            => diagnosticReporter.Report(
                    Diagnostic.Create(id: "CSSG" + id, category: "CSharpShaderGenerator", message: message,
                    severity: DiagnosticSeverity.Error, defaultSeverity: DiagnosticSeverity.Error, isEnabledByDefault: true, warningLevel: 0, isSuppressed: false,
                    location: location));
        public static void ReportWarning(this IDiagnosticReporter diagnosticReporter, string id, string message, Location location)
            => diagnosticReporter.Report(
                    Diagnostic.Create(id: "CSSG" + id, category: "CSharpShaderGenerator", message: message,
                    severity: DiagnosticSeverity.Warning, defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true, warningLevel: 1, isSuppressed: false,
                    location: location));
        public static void ReportInfo(this IDiagnosticReporter diagnosticReporter, string id, string message, Location location)
            => diagnosticReporter.Report(
                    Diagnostic.Create(id: "CSSG" + id, category: "CSharpShaderGenerator", message: message,
                    severity: DiagnosticSeverity.Info, defaultSeverity: DiagnosticSeverity.Info, isEnabledByDefault: true, warningLevel: 2, isSuppressed: false,
                    location: location));

        public static void Report(this IDiagnosticReporter diagnosticReporter, ShaderGenerationDiagnositcs diagnositcs)
            => diagnosticReporter.Report(
                    Diagnostic.Create(id: "CSSG" + diagnositcs.ID, category: "CSharpShaderGenerator", message: diagnositcs.Message,
                    severity: diagnositcs.Severity, defaultSeverity: DiagnosticSeverity.Error, isEnabledByDefault: true, warningLevel: GetDiagnosticsWarningLevel(diagnositcs.Severity), isSuppressed: false,
                    location: diagnositcs.Location));
        public static void Report(this IDiagnosticReporter diagnosticReporter, ShaderGenerationDiagnositcs diagnositcs, string messagePrefix)
            => diagnosticReporter.Report(
                    Diagnostic.Create(id: "CSSG" + diagnositcs.ID, category: "CSharpShaderGenerator", message: messagePrefix + diagnositcs.Message,
                    severity: diagnositcs.Severity, defaultSeverity: DiagnosticSeverity.Error, isEnabledByDefault: true, warningLevel: GetDiagnosticsWarningLevel(diagnositcs.Severity), isSuppressed: false,
                    location: diagnositcs.Location));


        public static string LinesToListing(this string text, string separator = ", ")
        {
            IEnumerable<string> lines = text.Split('\n', '\r').Where(l => !string.IsNullOrWhiteSpace(l));
            return lines.Any() ? lines.Aggregate((a, b) => a + separator + b) : "";
        }

        #endregion

    }
}
