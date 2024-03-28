using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SharpGraphics.Shaders.Generator;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace SharpGraphics.Shaders.Suppressors
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1036:Specify analyzer banned API enforcement setting", Justification = "System.IO is needed for glsLangValidator")]
    public class ShaderVariableDiagnosticSuppressor : DiagnosticSuppressor
    {

        private static readonly SuppressionDescriptor s_suppressUnusedFieldsRule = new SuppressionDescriptor(
            id: "CSSG1001",
            suppressedDiagnosticId: "CS0649",
            justification: "Field symbols marked with ShaderVariableAttribute are implicitly assigned by the GPU Pipeline and hence do not have its default value");

        private static readonly SuppressionDescriptor s_suppressMakeReadonlyRule = new SuppressionDescriptor(
            id: "CSSG1002",
            suppressedDiagnosticId: "IDE0044",
            justification: "Field symbols marked with ShaderVariableAttribute are implicitly assigned by the GPU Pipeline and hence should not be readonly");

        private static readonly SuppressionDescriptor s_suppressAssignedUnusedFieldsRule = new SuppressionDescriptor(
            id: "CSSG1003",
            suppressedDiagnosticId: "IDE0052",
            justification: "Field symbols marked with OutShaderVariableAttribute are implicitly used by the GPU Pipeline and hence should not be removed");

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(s_suppressUnusedFieldsRule, s_suppressMakeReadonlyRule);

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
                if (diagnostic.Location.SourceTree != null)
                {
                    SyntaxNode node = diagnostic.Location.SourceTree.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan);
                    if (node != null)
                    {
                        SemanticModel model = context.GetSemanticModel(node.SyntaxTree);
                        ISymbol? declaredSymbol = model.GetDeclaredSymbol(node, context.CancellationToken);
                        if (declaredSymbol?.Kind == SymbolKind.Field)
                        {
                            if (declaredSymbol.HasAttribute(ShaderGenerator.StorageQualifierAttributes, model, context.CancellationToken) ||
                                declaredSymbol.HasAttribute(ShaderGenerator.StageVariableAttributes, model, context.CancellationToken))
                            {
                                if (diagnostic.Id == s_suppressUnusedFieldsRule.SuppressedDiagnosticId)
                                    context.ReportSuppression(Suppression.Create(s_suppressUnusedFieldsRule, diagnostic));
                                else if (diagnostic.Id == s_suppressMakeReadonlyRule.SuppressedDiagnosticId)
                                    context.ReportSuppression(Suppression.Create(s_suppressMakeReadonlyRule, diagnostic));
                            }
                            if (declaredSymbol.HasAttribute(ShaderGenerator.OutAttributeFullName, model, context.CancellationToken) ||
                                declaredSymbol.HasAttribute(ShaderGenerator.StageOutputVariableAttributeFullName, model, context.CancellationToken))
                                context.ReportSuppression(Suppression.Create(s_suppressAssignedUnusedFieldsRule, diagnostic));
                        }
                    }
                }
        }

    }
}
