using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Reporters
{
    internal class SourceProductionContextDiagnosticReporter : IDiagnosticReporter
    {

        private readonly SourceProductionContext _context;

        public SourceProductionContextDiagnosticReporter(in SourceProductionContext context)
            => _context = context;

        public void Report(Diagnostic diagnostic) => _context.ReportDiagnostic(diagnostic);

    }
}
