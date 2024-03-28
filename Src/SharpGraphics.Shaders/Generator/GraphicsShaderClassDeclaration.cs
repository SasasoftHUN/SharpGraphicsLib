using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGraphics.Shaders.Generator;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Reporters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SharpGraphics.Shaders.Generator
{
    internal abstract class GraphicsShaderClassDeclaration : ShaderClassDeclaration
    {

        #region Properties

        public ShaderStage Stage { get; protected set; }
        public GraphicsShaderStages GraphicsStage { get; protected set; }

        #endregion

        #region Constructors

        public GraphicsShaderClassDeclaration(ClassDeclarationSyntax classDeclaration, ShaderLanguageMappings mappings, IDiagnosticReporter diagnosticReporter, Compilation compilation, SemanticModel model, CancellationToken cancellationToken) : base(classDeclaration, mappings, diagnosticReporter, compilation, model, cancellationToken)
        {

        }

        #endregion

    }
}
