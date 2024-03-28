using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGraphics.Shaders.Reporters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SharpGraphics.Shaders.Generator
{
    internal abstract class ShaderVariableDeclaration
    {
        public FieldDeclarationSyntax Field { get; }
        public ShaderStageVariable? StageVariable { get; }

        protected ShaderVariableDeclaration(FieldDeclarationSyntax field) => Field = field;
        protected ShaderVariableDeclaration(FieldDeclarationSyntax field, ShaderStageVariable stageVariable)
        {
            Field = field;
            StageVariable = stageVariable;
        }
        protected ShaderVariableDeclaration(FieldDeclarationSyntax field, IDiagnosticReporter diagnosticReporter, Compilation compilation, SemanticModel model, CancellationToken cancellationToken)
        {
            Field = field;

            if (field.HasAttribute(ShaderGenerator.StageInputVariableAttributeFullName, model, cancellationToken))
                StageVariable = new ShaderStageInVariable(field, diagnosticReporter, compilation, cancellationToken);
            else if (field.HasAttribute(ShaderGenerator.StageOutputVariableAttributeFullName, model, cancellationToken))
                StageVariable = new ShaderStageOutVariable(field, diagnosticReporter, compilation, cancellationToken);

            if (Field.CountAttributes(ShaderGenerator.StorageQualifierAttributes, model, cancellationToken) > 1)
                diagnosticReporter.ReportError("1501", "Shader class variables can only have a single Storage Qualifier Attribute.", Field.AttributeLists.Last().GetLocation());

            if (Field.CountAttributes(ShaderGenerator.StageVariableAttributes, model, cancellationToken) > 1)
                diagnosticReporter.ReportError("1502", "Shader class variables can only have a single Stage Variable Attribute.", Field.AttributeLists.Last().GetLocation());
        }

        public static void CheckMultipleDeclarations(FieldDeclarationSyntax field, IDiagnosticReporter diagnosticReporter)
        {
            if (field.Declaration.Variables.Count > 1)
                diagnosticReporter.ReportError("1500", "Shader class variables with Storage Qualifier Attributes can have only one variable per declaration.", field.Declaration.GetLocation());
        }

    }

    internal class ShaderInVariableDeclaration : ShaderVariableDeclaration
    {
        internal ShaderInVariableDeclaration(FieldDeclarationSyntax field) : base(field) { }
        internal ShaderInVariableDeclaration(FieldDeclarationSyntax field, ShaderStageVariable stageVariable) : base(field, stageVariable) { }
        public ShaderInVariableDeclaration(FieldDeclarationSyntax field, IDiagnosticReporter diagnosticReporter, Compilation compilation, SemanticModel model, CancellationToken cancellationToken) : base(field, diagnosticReporter, compilation, model, cancellationToken)
        {
            CheckMultipleDeclarations(field, diagnosticReporter);
            if (StageVariable != null)
                diagnosticReporter.ReportError("1503", "Shader In variables cannot be Shader Stage Input variables.", Field.Declaration.GetLocation());
        }
    }
    internal class ShaderOutVariableDeclaration : ShaderVariableDeclaration
    {
        internal ShaderOutVariableDeclaration(FieldDeclarationSyntax field) : base(field) { }
        internal ShaderOutVariableDeclaration(FieldDeclarationSyntax field, ShaderStageVariable stageVariable) : base(field, stageVariable) { }
        public ShaderOutVariableDeclaration(FieldDeclarationSyntax field, IDiagnosticReporter diagnosticReporter, Compilation compilation, SemanticModel model, CancellationToken cancellationToken) : base(field, diagnosticReporter, compilation, model, cancellationToken)
        {
            CheckMultipleDeclarations(field, diagnosticReporter);
        }
    }
    internal class ShaderLocalVariableDeclaration : ShaderVariableDeclaration
    {
        internal ShaderLocalVariableDeclaration(FieldDeclarationSyntax field) : base(field) { }
        internal ShaderLocalVariableDeclaration(FieldDeclarationSyntax field, ShaderStageVariable stageVariable) : base(field, stageVariable) { }
        public ShaderLocalVariableDeclaration(FieldDeclarationSyntax field, IDiagnosticReporter diagnosticReporter, Compilation compilation, SemanticModel model, CancellationToken cancellationToken) : base(field, diagnosticReporter, compilation, model, cancellationToken) { }
    }
    internal class ShaderMemberVariableDeclaration : ShaderVariableDeclaration
    {
        internal ShaderMemberVariableDeclaration(FieldDeclarationSyntax field) : base(field) { }
        internal ShaderMemberVariableDeclaration(FieldDeclarationSyntax field, ShaderStageVariable stageVariable) : base(field, stageVariable) { }
    }

    internal class ShaderUniformVariableDeclaration : ShaderVariableDeclaration
    {
        public uint Set { get; }
        public uint Binding { get;}
        public uint UniqueBinding { get; }

        public ShaderUniformVariableDeclaration(FieldDeclarationSyntax field, IDiagnosticReporter diagnosticReporter, Compilation compilation, SemanticModel model, CancellationToken cancellationToken): base(field, diagnosticReporter, compilation, model, cancellationToken)
        {
            CheckMultipleDeclarations(field, diagnosticReporter);
            if (StageVariable != null)
                diagnosticReporter.ReportError("1504", "Shader Uniform variables cannot be Shader Stage Input variables.", Field.Declaration.GetLocation());

            if (field.TryGetAttributeData(ShaderGenerator.UniformAttributeFullName, compilation, cancellationToken, out AttributeData? attributeData))
            {
                if (attributeData.TryGetAttributePropertyValueInt("Set", out int set))
                    Set = (uint)set;
                else diagnosticReporter.ReportError("1550", "Uniform variable's Set must be explicitly specified.", field.GetLocation());

                if (attributeData.TryGetAttributePropertyValueInt("Binding", out int binding))
                    Binding = (uint)binding;
                else diagnosticReporter.ReportError("1551", "Uniform variable's Binding must be explicitly specified.", field.GetLocation());

                if (attributeData.TryGetAttributePropertyValueInt("UniqueBinding", out int uniqueBinding))
                    UniqueBinding = (uint)uniqueBinding;
                else
                {
                    if (Set == 0u)
                        UniqueBinding = Binding;
                    else diagnosticReporter.ReportError("1552", "Uniform variable's Unique Binding must be explicitly specified when using multiple Sets.", field.GetLocation());
                }
            }
        }

    }


    internal abstract class ShaderStageVariable
    {

        public FieldDeclarationSyntax Field { get; }
        public string Name { get; }

        protected ShaderStageVariable(FieldDeclarationSyntax field, IDiagnosticReporter diagnosticReporter, Compilation compilation, CancellationToken cancellationToken)
        {
            Field = field;
            if (field.TryGetAttributeData(ShaderGenerator.StageVariableAttributes, compilation, cancellationToken, out AttributeData? attributeData))
            {
                if (attributeData.TryGetAttributePropertyValue("Name", out string? name))
                    Name = name;
                else Name = field.Declaration.Variables[0].Identifier.Text;
            }
            else Name = "";

            ShaderVariableDeclaration.CheckMultipleDeclarations(field, diagnosticReporter);
        }

    }
    internal class ShaderStageInVariable : ShaderStageVariable
    {
        public ShaderStageInVariable(FieldDeclarationSyntax field, IDiagnosticReporter diagnosticReporter, Compilation compilation, CancellationToken cancellationToken) : base(field, diagnosticReporter, compilation, cancellationToken)
        {
        }
    }
    internal class ShaderStageOutVariable : ShaderStageVariable
    {
        public ShaderStageOutVariable(FieldDeclarationSyntax field, IDiagnosticReporter diagnosticReporter, Compilation compilation, CancellationToken cancellationToken) : base(field, diagnosticReporter, compilation, cancellationToken)
        {
        }
    }
}
