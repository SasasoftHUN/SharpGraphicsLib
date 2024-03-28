using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Reporters
{
    public interface IDiagnosticReporter
    {

        void Report(Diagnostic diagnostic);

    }
}
