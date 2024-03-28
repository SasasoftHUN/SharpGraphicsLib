using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SharpGraphics.Shaders.Reporters
{
    public class ConsoleDiagnosticReporter : IDiagnosticReporter
    {

        public bool ReportHidden { get; set; } = false;
        public bool ReportInfo { get; set; } = true;
        public bool ReportWarning { get; set; } = true;
        public bool ReportError { get; set; } = true;

        public void Report(Diagnostic diagnostic)
        {
            string output = $"{diagnostic.Severity} {diagnostic.Id}: {diagnostic.GetMessage()}\n\t{diagnostic.Location}";
            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Hidden:
                    if (ReportWarning)
                        Console.WriteLine(output);
                    break;
                case DiagnosticSeverity.Info:
                    if (ReportWarning)
                        Console.WriteLine(output);
                    break;
                case DiagnosticSeverity.Warning:
                    if (ReportWarning)
                        Console.WriteLine(output);
                    break;
                case DiagnosticSeverity.Error:
                    if (ReportError)
                        Console.Error.WriteLine(output);
                    break;
            }
        }
    }
}
