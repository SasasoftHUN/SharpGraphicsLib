using Microsoft.Build.Locator;
using SharpGraphics.Shaders.StandAloneShaderGenerator.OutputWriters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGraphics.Shaders.StandAloneShaderGenerator
{
    internal class GeneratorConfiguration
    {

        #region Properties

        public IEnumerable<string> SourcePaths { get; }
        public IEnumerable<string> ProjectPaths { get; }
        public IEnumerable<string> SolutionPaths { get; }
        public bool StandardInput { get; }

        public IEnumerable<string> ReferenceGACDLLs { get; }
        public IEnumerable<string> ReferenceNuGetPackages { get; }
        public VisualStudioInstance? VSInstance { get; }

        public bool IsVerbose { get; }

        public IOutputWriter OutputWriter { get; }

        public IEnumerable<string> TargetBackendNames { get; }

        #endregion

        #region Constructors

        //Compiling source files from Standard Input
        public GeneratorConfiguration(IEnumerable<string> referenceGACDLLs, IEnumerable<string> referenceNuGetPackages,
            bool isVerbose, IOutputWriter outputWriter, IEnumerable<string> targetBackendNames)
        {
            SourcePaths = new string[0];
            ProjectPaths = new string[0];
            SolutionPaths = new string[0];
            StandardInput = true;

            VSInstance = null;
            ReferenceGACDLLs = referenceGACDLLs.ToArray();
            ReferenceNuGetPackages = referenceNuGetPackages.ToArray();

            IsVerbose = isVerbose;

            OutputWriter = outputWriter;
            TargetBackendNames = targetBackendNames;
        }
        //Compiling .cs source files
        public GeneratorConfiguration(IEnumerable<string> sourcePaths, IEnumerable<string> referenceGACDLLs, IEnumerable<string> referenceNuGetPackages,
            bool isVerbose, IOutputWriter outputWriter, IEnumerable<string> targetBackendNames)
        {
            SourcePaths = sourcePaths.ToArray();
            ProjectPaths = new string[0];
            SolutionPaths = new string[0];

            VSInstance = null;
            ReferenceGACDLLs = referenceGACDLLs.ToArray();
            ReferenceNuGetPackages = referenceNuGetPackages.ToArray();

            IsVerbose = isVerbose;

            OutputWriter = outputWriter;
            TargetBackendNames = targetBackendNames;
        }
        //Compiling Projects and Solutions
        public GeneratorConfiguration(IEnumerable<string> projectPaths, IEnumerable<string> solutionPaths, VisualStudioInstance? vsInstance,
            bool isVerbose, IOutputWriter outputWriter, IEnumerable<string> targetBackendNames)
        {
            SourcePaths = new string[0];
            ProjectPaths = projectPaths.ToArray();
            SolutionPaths = solutionPaths.ToArray();

            VSInstance = vsInstance;
            ReferenceGACDLLs = new string[0];
            ReferenceNuGetPackages = new string[0];

            IsVerbose = isVerbose;

            OutputWriter = outputWriter;
            TargetBackendNames = targetBackendNames;
        }

        #endregion

    }
}
