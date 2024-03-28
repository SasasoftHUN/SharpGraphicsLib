using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Generator
{
    public class ShaderGenerationDiagnositcs
    {

        public string ID { get; private set; }
        public string Message { get; private set; }
        public Location? Location { get; private set; }
        public DiagnosticSeverity Severity { get; private set; }

        private ShaderGenerationDiagnositcs(string id, string message, DiagnosticSeverity severity)
        {
            ID = id;
            Message = message;
            Severity = severity;
        }
        private ShaderGenerationDiagnositcs(string id, string message, Location? location, DiagnosticSeverity severity)
        {
            ID = id;
            Message = message;
            Location = location;
            Severity = severity;
        }

        public static ShaderGenerationDiagnositcs CreateError(string id, string message, ISymbol? symbol) => symbol != null && symbol.Locations.Length > 0 ? CreateError(id, message, symbol.Locations[0]) : CreateError(id, message);
        public static ShaderGenerationDiagnositcs CreateError(string id, string message, in SyntaxToken? syntaxToken) => CreateError(id, message, syntaxToken?.GetLocation());
        public static ShaderGenerationDiagnositcs CreateError(string id, string message, SyntaxNode? syntaxNode) => CreateError(id, message, syntaxNode?.GetLocation());
        public static ShaderGenerationDiagnositcs CreateError(string id, string message, Location? location) => new ShaderGenerationDiagnositcs(id, message, location, DiagnosticSeverity.Error);
        public static ShaderGenerationDiagnositcs CreateError(string id, string message) => new ShaderGenerationDiagnositcs(id, message, DiagnosticSeverity.Error);
        public static ShaderGenerationDiagnositcs CreateWarning(string id, string message, ISymbol? symbol) => symbol != null && symbol.Locations.Length > 0 ? CreateWarning(id, message, symbol.Locations[0]) : CreateWarning(id, message);
        public static ShaderGenerationDiagnositcs CreateWarning(string id, string message, in SyntaxToken? syntaxToken) => CreateWarning(id, message, syntaxToken?.GetLocation());
        public static ShaderGenerationDiagnositcs CreateWarning(string id, string message, SyntaxNode? syntaxNode) => CreateWarning(id, message, syntaxNode?.GetLocation());
        public static ShaderGenerationDiagnositcs CreateWarning(string id, string message, Location? location) => new ShaderGenerationDiagnositcs(id, message, location, DiagnosticSeverity.Warning);
        public static ShaderGenerationDiagnositcs CreateWarning(string id, string message) => new ShaderGenerationDiagnositcs(id, message, DiagnosticSeverity.Warning);
        public static ShaderGenerationDiagnositcs CreateInfo(string id, string message, Location? location) => new ShaderGenerationDiagnositcs(id, message, location, DiagnosticSeverity.Info);

    }
}
