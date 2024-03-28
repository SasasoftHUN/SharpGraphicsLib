using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Reporters
{
    internal class DistinctDiagnosticReporter : IDiagnosticReporter
    {

        private IDiagnosticReporter _reporter;
        private List<Diagnostic> _reportedDiagnostics = new List<Diagnostic>();

        internal DistinctDiagnosticReporter(IDiagnosticReporter reporter) =>_reporter = reporter;

        public void Report(Diagnostic diagnostic)
        {
            foreach (Diagnostic reportedDiagnostic in _reportedDiagnostics)
                if (reportedDiagnostic.Id == diagnostic.Id && reportedDiagnostic.Location == diagnostic.Location)
                    return;

            _reporter.Report(diagnostic);
            _reportedDiagnostics.Add(diagnostic);
        }

    }
}
