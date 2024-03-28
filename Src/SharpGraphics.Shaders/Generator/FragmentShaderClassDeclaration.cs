using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Reporters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SharpGraphics.Shaders.Generator
{
    internal class FragmentShaderClassDeclaration : GraphicsShaderClassDeclaration
    {

        #region Constructors

        public FragmentShaderClassDeclaration(ClassDeclarationSyntax classDeclaration, ShaderLanguageMappings mappings, IDiagnosticReporter diagnosticReporter, Compilation compilation, SemanticModel model, CancellationToken cancellationToken)
            : base(classDeclaration, mappings, diagnosticReporter, compilation, model, cancellationToken)
        {
            Stage = ShaderStage.Fragment;
            GraphicsStage = GraphicsShaderStages.Fragment;
        }

        #endregion

        #region Protected Methods

        protected override bool CheckStageVariable(ShaderLanguageMappings mappings, IDiagnosticReporter diagnosticReporter, ShaderStageVariable stageVariable, StageVariableMapping stageVariableMapping)
        {
            if (stageVariableMapping.Stage != ShaderStage.Fragment)
            {
                diagnosticReporter.ReportError("1508", $"Shader Stage variable cannot be used in Fragment Shader Stage.", stageVariable.Field.GetLocation());
                return false;
            }

            return base.CheckStageVariable(mappings, diagnosticReporter, stageVariable, stageVariableMapping);
        }

        #endregion

    }
}
