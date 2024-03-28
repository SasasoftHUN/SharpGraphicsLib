using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Reporters
{
    public sealed class DevNullDiagnosticReporter : IDiagnosticReporter
    {
        public void Report(Diagnostic diagnostic) { }
    }
}
