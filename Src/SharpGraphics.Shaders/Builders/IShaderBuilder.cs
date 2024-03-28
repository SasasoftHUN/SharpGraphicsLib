using Microsoft.CodeAnalysis;
using SharpGraphics.Shaders.Generator;
using SharpGraphics.Shaders.Reporters;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Builders
{
    internal interface IShaderBuilder
    {

        bool IsBuildSuccessful { get; }
        string BackendName { get; }
        IEnumerable<ShaderGenerationDiagnositcs> Diagnositcs { get; }

        public IShaderBuilder BuildGraphics(ShaderClassDeclaration shaderClass);
        public IGeneratedShaderSource GetShaderSource();
        public IGeneratedShaderSource GetShaderSourceEmpty();
        public void ReportDiagnostics(IDiagnosticReporter diagnosticReporter);

    }
}
