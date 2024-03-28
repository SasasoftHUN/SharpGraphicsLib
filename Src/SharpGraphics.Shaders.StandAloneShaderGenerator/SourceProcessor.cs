using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpGraphics.Shaders;
using SharpGraphics.Shaders.Generator;
using SharpGraphics.Shaders.Reporters;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Validators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using SharpGraphics.Shaders.Builders;
using System.Threading;

namespace SharpGraphics.Shaders.StandAloneShaderGenerator
{
    internal static class SourceProcessor
    {

        private class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
        {
            public void Report(ProjectLoadProgress loadProgress)
            {
                var projectDisplay = Path.GetFileName(loadProgress.FilePath);
                if (loadProgress.TargetFramework != null)
                {
                    projectDisplay += $" ({loadProgress.TargetFramework})";
                }

                Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
            }
        }

        #region Fields

        private static readonly IEnumerable<MetadataReference> _metadataReferences;

        #endregion

        #region Constructors

        static SourceProcessor()
        {
            string gacPath = GetGACPath();

            _metadataReferences = new[] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ShaderBase).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ShaderGenerator).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(gacPath, "mscorlib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(gacPath, "System.dll")),
                MetadataReference.CreateFromFile(Path.Combine(gacPath, "System.Core.dll")),
                MetadataReference.CreateFromFile(Path.Combine(gacPath, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Combine(gacPath, "System.Numerics.Vectors.dll")),
            };
        }

        #endregion

        #region Private Methods

        private static async IAsyncEnumerable<string> LoadSourceFilesAsync(IEnumerable<string> sourceFilePaths)
        {
            foreach (string sourceFilePath in sourceFilePaths)
                yield return await File.ReadAllTextAsync(sourceFilePath);
            yield break;
        }

        private static IEnumerable<ClassDeclarationSyntax> CollectShaderClassDeclarations(Compilation compilation)
        {
            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
                foreach (ClassDeclarationSyntax classDeclaration in syntaxTree.GetRoot().DescendantNodesAndSelf()
                    .Where(n => n.IsKind(SyntaxKind.ClassDeclaration)))
                    if (classDeclaration.AttributeLists.Any() && classDeclaration.AttributeLists.Any(al => al.Attributes.Any()))
                    {
                        SemanticModel model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                        if (classDeclaration.HasAttribute(ShaderGenerator.ShaderAttributeFullNames, model, default))
                            yield return classDeclaration;
                    }

            yield break;
        }

        private static IEnumerable<GeneratedShader> ProcessSources(IEnumerable<SyntaxTree> syntaxTrees, IEnumerable<string> referenceGACDLLs, IEnumerable<string> referenceNuGetPackages, bool isVerbose,
            IEnumerable<string> targetBackendOverrideNames, ShaderValidators shaderValidators)
        {
            //Prepare additional references
            List<MetadataReference> additionalReferences = new List<MetadataReference>();
            string gacPath = GetGACPath();
            foreach (string referenceGACDLL in referenceGACDLLs)
                additionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(gacPath, referenceGACDLL)));
            IEnumerable<MetadataReference> metadataReferences = additionalReferences.Any() ? _metadataReferences.Concat(additionalReferences) : _metadataReferences;

            CSharpCompilationOptions compOps = new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                platform: Platform.AnyCpu);

            //Compile Parsed Sources
            CSharpCompilation compilation = CSharpCompilation.Create("shaderAssembly",
                syntaxTrees,
                metadataReferences,
                compOps);

            //Find Shader Class Declarations and add abstract ShaderBase implementations where needed
            List<SyntaxTree> shaderPartialClassTrees = new List<SyntaxTree>();
            foreach (ClassDeclarationSyntax shaderClassDeclaration in CollectShaderClassDeclarations(compilation))
            {
                if (ASTAnalyzerHelper.IsPartialClassDeclaration(shaderClassDeclaration))
                {
                    StringBuilder shaderPartialClassSourceBuilder = new StringBuilder("""
                        using System;
                        using System.Collections.Generic;
                            
                        """);

                    SemanticModel model = compilation.GetSemanticModel(shaderClassDeclaration.SyntaxTree, true);
                    if (shaderClassDeclaration.TryGetNamespace(model, CancellationToken.None, out string? ns))
                    {
                        shaderPartialClassSourceBuilder.AppendLine($"namespace {ns}");
                        shaderPartialClassSourceBuilder.AppendLine('{');
                    }

                    shaderPartialClassSourceBuilder.AppendLine($$"""
                        public partial class {{shaderClassDeclaration.Identifier.Text}}
                        {
                            public override bool TryGetSourceText(string backendName, out string source) { source = ""; return false; }
                            public override bool TryGetSourceBinary(string backendName, out ReadOnlyMemory<byte> source) { source = ReadOnlyMemory<byte>.Empty; return false; }
                        }
                        """);

                    if (!string.IsNullOrWhiteSpace(ns))
                    {
                        shaderPartialClassSourceBuilder.AppendLine('}');
                    }

                    shaderPartialClassTrees.Add(CSharpSyntaxTree.ParseText(shaderPartialClassSourceBuilder.ToString()));
                }
            }

            //Recompile with ShaderBase implementations if needed
            if (shaderPartialClassTrees.Any())
                compilation = CSharpCompilation.Create("shaderAssembly",
                    syntaxTrees.Concat(shaderPartialClassTrees),
                    metadataReferences,
                    compOps);


            //Generate Shaders
            foreach (GeneratedShader generatedShader in ProcessCompilation(compilation, targetBackendOverrideNames, new ConsoleDiagnosticReporter() { ReportInfo = isVerbose, ReportWarning = isVerbose }, shaderValidators))
                yield return generatedShader;
        }

        private static async IAsyncEnumerable<GeneratedShader> ProcessProjectAsync(Project project, IEnumerable<string> targetBackendOverrideNames, IDiagnosticReporter diagnosticReporter, bool isVerbose, ShaderValidators shaderValidators)
        {
            if (project.SupportsCompilation)
            {
                if (isVerbose)
                    Console.WriteLine($"Analyzing {project.Name} project.");

                CompilationOptions compOps = new CSharpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    platform: Platform.AnyCpu);

                Compilation? compilation = await project.WithCompilationOptions(compOps).GetCompilationAsync(); //TODO: Parallelize
                if (compilation != null)
                    foreach (GeneratedShader generatedShader in ProcessCompilation(compilation, targetBackendOverrideNames, diagnosticReporter, shaderValidators))
                        yield return generatedShader;
            }

            yield break;
        }
        private static IEnumerable<GeneratedShader> ProcessCompilation(Compilation compilation, IEnumerable<string> targetBackendOverrideNames, IDiagnosticReporter diagnosticReporter, ShaderValidators shaderValidators)
        {
            ImmutableArray<Diagnostic> projectCompilationDiagnostics = compilation.GetDiagnostics();
            bool compilationError = false;
            foreach (Diagnostic diagnostic in projectCompilationDiagnostics)
                if (diagnostic.Severity != DiagnosticSeverity.Hidden)
                {
                    diagnosticReporter.Report(diagnostic);
                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                        compilationError = true;
                }

            if (!compilationError)
            {
                ShaderLanguageMappings mappings = ShaderGenerator.CreateMappings();

                foreach (GeneratedShader generatedShader in ShaderGenerator.GenerateShaders(CollectShaderClassDeclarations(compilation), new Generator.GeneratorConfiguration(mappings, targetBackendOverrideNames), compilation, shaderValidators, diagnosticReporter, default))
                    yield return generatedShader;
            }

            yield break;
        }

        #endregion

        #region Public Methods

        public static string GetGACPath()
        {
            string objectAssemblyLocation = typeof(Object).Assembly.Location;

            string? objectAssemblyLocationFolder = Path.GetDirectoryName(objectAssemblyLocation);
            if (objectAssemblyLocationFolder == null)
                objectAssemblyLocationFolder = Path.GetPathRoot(objectAssemblyLocation) ?? "";

            return objectAssemblyLocationFolder;
        }

        public static async IAsyncEnumerable<GeneratedShader> ProcessSourceAsync(string source, IEnumerable<string> referenceGACDLLs, IEnumerable<string> referenceNuGetPackages, bool isVerbose,
            IEnumerable<string> targetBackendOverrideNames, ShaderValidators shaderValidators)
        {
            //Parse Sources
            SyntaxTree[] syntaxTrees = new SyntaxTree[]
            {
                CSharpSyntaxTree.ParseText(source)
            };

            foreach (GeneratedShader generatedShader in ProcessSources(syntaxTrees, referenceGACDLLs, referenceNuGetPackages, isVerbose, targetBackendOverrideNames, shaderValidators))
                yield return generatedShader;
            yield break;
        }
        public static async IAsyncEnumerable<GeneratedShader> ProcessSourcesAsync(IEnumerable<string> sources, IEnumerable<string> referenceGACDLLs, IEnumerable<string> referenceNuGetPackages, bool isVerbose,
            IEnumerable<string> targetBackendOverrideNames, ShaderValidators shaderValidators)
        {
            //Parse Sources
            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
            foreach (string source in sources)
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(source));

            foreach (GeneratedShader generatedShader in ProcessSources(syntaxTrees, referenceGACDLLs, referenceNuGetPackages, isVerbose, targetBackendOverrideNames, shaderValidators))
                yield return generatedShader;
            yield break;
        }
        public static async IAsyncEnumerable<GeneratedShader> ProcessSourcesAsync(IAsyncEnumerable<string> sources, IEnumerable<string> referenceGACDLLs, IEnumerable<string> referenceNuGetPackages, bool isVerbose,
            IEnumerable<string> targetBackendOverrideNames, ShaderValidators shaderValidators)
        {
            //Parse Sources
            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
            await foreach (string source in sources)
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(source));

            foreach (GeneratedShader generatedShader in ProcessSources(syntaxTrees, referenceGACDLLs, referenceNuGetPackages, isVerbose, targetBackendOverrideNames, shaderValidators))
                yield return generatedShader;
            yield break;
        }
        public static async IAsyncEnumerable<GeneratedShader> ProcessSourceFilesAsync(IEnumerable<string> sourcePaths, IEnumerable<string> referenceGACDLLs, IEnumerable<string> referenceNuGetPackages, bool isVerbose,
            IEnumerable<string> targetBackendOverrideNames, ShaderValidators shaderValidators)
        {
            await foreach (GeneratedShader generatedShader in ProcessSourcesAsync(LoadSourceFilesAsync(sourcePaths), referenceGACDLLs, referenceNuGetPackages, isVerbose, targetBackendOverrideNames, shaderValidators))
                yield return generatedShader;
            yield break;
        }

        public static async IAsyncEnumerable<GeneratedShader> ProcessProjectFileAsync(string projectPath, bool isVerbose, IEnumerable<string> targetBackendOverrideNames, ShaderValidators shaderValidators)
        {
            IDiagnosticReporter diagnosticReporter = new ConsoleDiagnosticReporter() { ReportInfo = isVerbose, ReportWarning = isVerbose };

            using (MSBuildWorkspace workspace = MSBuildWorkspace.Create())
            {
                workspace.LoadMetadataForReferencedProjects = true;
                // Print message for WorkspaceFailed event to help diagnosing project load failures.
                workspace.WorkspaceFailed += (o, e) => diagnosticReporter.Report(Diagnostic.Create(id: "MSBUILD", category: "CSharpShaderGenerator", message: $"{e.Diagnostic.Kind}: {e.Diagnostic.Message}",
                    severity: DiagnosticSeverity.Error, defaultSeverity: DiagnosticSeverity.Error, isEnabledByDefault: true, warningLevel: 0, isSuppressed: false));

                Project project = await workspace.OpenProjectAsync(projectPath, isVerbose ? new ConsoleProgressReporter() : null);

                await foreach (GeneratedShader generatedShader in ProcessProjectAsync(project, targetBackendOverrideNames, diagnosticReporter, isVerbose, shaderValidators))
                    yield return generatedShader;
            }

            yield break;
        }

        public static async IAsyncEnumerable<GeneratedShader> ProcessSolutionFileAsync(string solutionPath, bool isVerbose, IEnumerable<string> targetBackendOverrideNames, ShaderValidators shaderValidators)
        {
            IDiagnosticReporter diagnosticReporter = new ConsoleDiagnosticReporter() { ReportInfo = isVerbose, ReportWarning = isVerbose };

            using (MSBuildWorkspace workspace = MSBuildWorkspace.Create())
            {
                workspace.LoadMetadataForReferencedProjects = true;
                // Print message for WorkspaceFailed event to help diagnosing project load failures.
                workspace.WorkspaceFailed += (o, e) => diagnosticReporter.Report(Diagnostic.Create(id: "MSBUILD", category: "CSharpShaderGenerator", message: $"{e.Diagnostic.Kind}: {e.Diagnostic.Message}",
                    severity: DiagnosticSeverity.Error, defaultSeverity: DiagnosticSeverity.Error, isEnabledByDefault: true, warningLevel: 0, isSuppressed: false));

                if (isVerbose)
                    Console.WriteLine($"Loading solution '{solutionPath}'");

                // Attach progress reporter so we print projects as they are loaded.
                Solution solution = await workspace.OpenSolutionAsync(solutionPath, new ConsoleProgressReporter());
                if (isVerbose)
                    Console.WriteLine($"Finished loading solution '{solutionPath}'");

                // Do analysis on the projects in the loaded solution
                CompilationOptions compOps = new CSharpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    platform: Platform.AnyCpu);

                if (isVerbose)
                    Console.WriteLine($"Begin analization of {solution.Projects.Count()} projects.");
                foreach (Project project in solution.Projects)
                    await foreach (GeneratedShader generatedShader in ProcessProjectAsync(project, targetBackendOverrideNames, diagnosticReporter, isVerbose, shaderValidators))
                        yield return generatedShader;
            }
        }

        #endregion

    }
}
