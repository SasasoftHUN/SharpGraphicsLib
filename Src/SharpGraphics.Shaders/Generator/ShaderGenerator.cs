using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Reporters;
using SharpGraphics.Shaders.Validators;

namespace SharpGraphics.Shaders.Generator
{

    [Generator(LanguageNames.CSharp)]
    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1036:Specify analyzer banned API enforcement setting", Justification = "System.IO is needed for glsLangValidator")]
    public class ShaderGenerator : IIncrementalGenerator
    {

        #region Properties

        public static string ShaderAttributeFullName { get; private set; }
        public static string ComputeShaderAttributeFullName { get; private set; }
        public static string GraphicsShaderAttributeFullName { get; private set; }
        public static string VertexShaderAttributeFullName { get; private set; }
        public static string FragmentShaderAttributeFullName { get; private set; }
        public static string[] ShaderAttributeFullNames { get; private set; }
        public static string[] GraphicsShaderAttributeFullNames { get; private set; }

        public static string ShaderBaseFullName { get; private set; }
        public static string VertexShaderBaseFullName { get; private set; }
        public static string FragmentShaderBaseFullName { get; private set; }

        public static string ShaderBackendTargetFullName { get; private set; }

        public static string[] StorageQualifierAttributes { get; private set; }
        public static string InAttributeFullName { get; private set; }
        public static string OutAttributeFullName { get; private set; }
        public static string UniformAttributeFullName { get; private set; }
        public static string ArraySizeAttributeFullName { get; private set; }
        public static string InputAttachmentIndexAttributeFullName { get; private set; }
        public static string StageInputVariableAttributeFullName { get; private set; }
        public static string StageOutputVariableAttributeFullName { get; private set; }
        public static string[] StageVariableAttributes { get; private set; }

        public static string ShaderTypeMappingAttributeFullName { get; private set; }

        #endregion

        #region Constructors

        static ShaderGenerator()
        {
            ShaderAttributeFullName = typeof(ShaderAttribute)?.FullName ?? "SharpGraphics.Shaders.ShaderAttribute";
            ComputeShaderAttributeFullName = typeof(ComputeShaderAttribute)?.FullName ?? "SharpGraphics.Shaders.ComputeShaderAttribute";
            GraphicsShaderAttributeFullName = typeof(GraphicsShaderAttribute)?.FullName ?? "SharpGraphics.Shaders.GraphicsShaderAttribute";
            VertexShaderAttributeFullName = typeof(VertexShaderAttribute)?.FullName ?? "SharpGraphics.Shaders.VertexShaderAttribute";
            FragmentShaderAttributeFullName = typeof(FragmentShaderAttribute)?.FullName ?? "SharpGraphics.Shaders.FragmentShaderAttribute";

            ShaderAttributeFullNames = new string[]
            {
                ComputeShaderAttributeFullName,
                VertexShaderAttributeFullName,
                FragmentShaderAttributeFullName,
            };
            GraphicsShaderAttributeFullNames = new string[]
            {
                VertexShaderAttributeFullName,
                FragmentShaderAttributeFullName,
            };

            //These classes are in a different assembly which makes them unavailable for typeof in the static constructor...
            ShaderBaseFullName = "SharpGraphics.Shaders.ShaderBase";
            VertexShaderBaseFullName = "SharpGraphics.Shaders.VertexShaderBase";
            FragmentShaderBaseFullName = "SharpGraphics.Shaders.FragmentShaderBase";

            ShaderBackendTargetFullName = typeof(ShaderBackendTarget)?.FullName ?? "SharpGraphics.Shaders.ShaderBackendTarget";

            InAttributeFullName = typeof(InAttribute)?.FullName ?? "SharpGraphics.Shaders.InAttribute";
            OutAttributeFullName = typeof(OutAttribute)?.FullName ?? "SharpGraphics.Shaders.OutAttribute";
            UniformAttributeFullName = typeof(UniformAttribute)?.FullName ?? "SharpGraphics.Shaders.UniformAttribute";
            ArraySizeAttributeFullName = typeof(ArraySizeAttribute)?.FullName ?? "SharpGraphics.Shaders.ArraySizeAttribute";
            InputAttachmentIndexAttributeFullName = typeof(InputAttachmentIndexAttribute)?.FullName ?? "SharpGraphics.Shaders.InputAttachmentIndexAttribute";

            StorageQualifierAttributes = new string[]
            {
                InAttributeFullName,
                OutAttributeFullName,
                UniformAttributeFullName,
            };

            ShaderTypeMappingAttributeFullName = typeof(ShaderTypeMappingAttribute)?.FullName ?? "SharpGraphics.Shaders.ShaderTypeMappingAttribute";

            StageInputVariableAttributeFullName = typeof(StageInAttribute)?.FullName ?? "SharpGraphics.Shaders.StageInputVariableAttribute";
            StageOutputVariableAttributeFullName = typeof(StageOutAttribute)?.FullName ?? "SharpGraphics.Shaders.StageOutputVariableAttribute";
            StageVariableAttributes = new string[]
            {
                StageInputVariableAttributeFullName,
                StageOutputVariableAttributeFullName,
            };
        }

        #endregion

        #region Private Methods

        private static bool CanBeShaderClassDeclaration(SyntaxNode node)
            => node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0;
        private static ClassDeclarationSyntax? GetSemanticTargetForShaderClassDeclaration(GeneratorSyntaxContext context, CancellationToken cancellationToken)
            => context.Node is ClassDeclarationSyntax classDeclarationSyntax && classDeclarationSyntax.HasAttribute(ShaderAttributeFullNames, context.SemanticModel, cancellationToken) ? classDeclarationSyntax : null;

        private static bool CanBeShaderTypeMappingDeclaration(SyntaxNode node)
            => node is TypeDeclarationSyntax m && m.AttributeLists.Count > 0;
        private static TypeDeclarationSyntax? GetSemanticTargetForShaderTypeMappingDeclaration(GeneratorSyntaxContext context, CancellationToken cancellationToken)
            => context.Node is TypeDeclarationSyntax typeDeclarationSyntax && typeDeclarationSyntax.HasAttribute(ShaderTypeMappingAttributeFullName, context.SemanticModel, cancellationToken) ? typeDeclarationSyntax : null;

        private static bool CanBeAssemblyShaderTypeMappingDeclaration(SyntaxNode node)
            => node is AttributeListSyntax m && m.Attributes.Count > 0 && m.Target != null && m.Target.Identifier.Text.ToLower() == "assembly";
        private static AttributeListSyntax? GetSemanticTargetForAssemblyShaderTypeMappingDeclaration(GeneratorSyntaxContext context)
            => context.Node is AttributeListSyntax attributeListSyntax ? attributeListSyntax : null;


        private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> shaderClassDeclarations, ImmutableArray<AttributeListSyntax> shaderTypeMappings, ShaderValidators validators, in SourceProductionContext context)
        {
            if (shaderClassDeclarations.IsDefaultOrEmpty)
                return;

            /*context.ReportDiagnostic(Diagnostic.Create(id: "CSSGMAPPINGS", category: "CSharpShaderGenerator", message: $"Mappings: {(shaderTypeMappings.Count() > 0 ? shaderTypeMappings.Select(t => t.Attributes[0].Name.GetText().ToString()).Aggregate((t1, t2) => $"{t1}, {t2}") : "")}",
                    severity: DiagnosticSeverity.Warning, defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true, warningLevel: 1, isSuppressed: false,
                    location: shaderClassDeclarations.First().GetLocation()));*/

            try
            {
                ShaderLanguageMappings mappings = CreateMappings();
                
                IEnumerable<GeneratedShader> generatedShaders = GenerateShaders(shaderClassDeclarations, new GeneratorConfiguration(mappings), compilation, validators, new SourceProductionContextDiagnosticReporter(context), context.CancellationToken);

                foreach (GeneratedShader generatedShader in generatedShaders)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                        return;
                    AddShaderSource(generatedShader, context);
                }
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create("CSSG", "ShaderGenerator", $"ShaderGenerator: {e.Message}; StackTrace: {e.StackTrace?.LinesToListing()}", DiagnosticSeverity.Error, DiagnosticSeverity.Error, true, 0));
            }
        }

        private static bool TryGetTypeStringForShaderSourceType(ShaderSourceType sourceType, [NotNullWhen(returnValue: true)] out string? typeString)
        {
            switch (sourceType)
            {
                case ShaderSourceType.Text: typeString = "string"; return true;
                case ShaderSourceType.Binary: typeString = "ReadOnlyMemory<byte>"; return true;
                default: typeString = null; return false;
            }
        }

        private static void AddShaderSource(GeneratedShader shaderSource, in SourceProductionContext context)
        {
            bool isInNamespace = shaderSource.Namespace != null && !string.IsNullOrWhiteSpace(shaderSource.Namespace);
            ShaderSourceType[] sourceTypes = Enum.GetValues(typeof(ShaderSourceType)).Cast<ShaderSourceType>().ToArray();

            //Add shader source classes for all backends
            int indentation = 0;
            Dictionary<ShaderSourceType, List<string>> sources = new Dictionary<ShaderSourceType, List<string>>();
            StringBuilder sb = new StringBuilder();
            foreach (IGeneratedShaderSource source in shaderSource.Sources)
                if (TryGetTypeStringForShaderSourceType(source.SourceType, out string? sourceType))
                {
                    indentation = 0;
                    sb.Clear();

                    sb.AppendLineIndentation("using System;", indentation);
                    sb.AppendLineIndentation(indentation);

                    if (isInNamespace)
                        sb.AppendLineIndentation(@$"namespace {shaderSource.Namespace!}
{{", indentation++);

                    sb.AppendLineIndentation($@"public partial class {shaderSource.Name}
    {{
", indentation++);


                    sb.AppendLineIndentation($"private static readonly {sourceType} _source{source.BackendName} = {source.AsToStringLiteral()};", indentation);

                    sb.AppendLineIndentation("}", --indentation);
                    if (isInNamespace)
                        sb.AppendLineIndentation("}", --indentation);

                    SourceText shaderSourceText = SourceText.From(sb.ToString(), Encoding.UTF8);
                    //Debug.WriteLine(shaderSourceText);
                    context.AddSource($"{shaderSource.Name}_{source.BackendName}.g.cs", shaderSourceText);


                    if (!sources.TryGetValue(source.SourceType, out List<string>? sourceNames))
                    {
                        sourceNames = new List<string>();
                        sources[source.SourceType] = sourceNames;
                    }
                    sourceNames.Add(source.BackendName);
            }

            //Add class source for Dictionary
            indentation = 0;
            sb.Clear();
            sb.AppendLineIndentation("using System;", indentation);
            sb.AppendLineIndentation("using System.Collections.Generic;", indentation);
            sb.AppendLineIndentation("using System.Diagnostics.CodeAnalysis;", indentation);
            sb.AppendLineIndentation(indentation);

            if (isInNamespace)
                sb.AppendLineIndentation(@$"namespace {shaderSource.Namespace!}
{{", indentation++);

            sb.AppendLineIndentation($@"public partial class {shaderSource.Name}
    {{
", indentation++);

            //Enforced parameterless constructor
            sb.AppendLine("#if NET5_0_OR_GREATER");
            sb.AppendLineIndentation("[RequiresUnreferencedCode(\"SharpGraphics.GraphicsDevice.CreateShaderSource will call this through Generics\")]", indentation);
            sb.AppendLineIndentation($"public {shaderSource.Name}() : base() {{ }}", indentation);
            sb.AppendLine("#endif");
            sb.AppendLine();

            //Dictionary Fields
            foreach (ShaderSourceType sourceType in sourceTypes)
                if (TryGetTypeStringForShaderSourceType(sourceType, out string? sourceTypeString))
                {
                    sb.AppendLineIndentation($"private static readonly IReadOnlyDictionary<string, {sourceTypeString}> _sources{sourceType} = new Dictionary<string, {sourceTypeString}>", indentation);
                    sb.AppendLineIndentation("{", indentation++);
                    if (sources.TryGetValue(sourceType, out List<string>? typeSources))
                    foreach (string backendName in typeSources)
                        sb.AppendLineIndentation($"{{ \"{backendName}\", _source{backendName} }},", indentation);
                    sb.AppendLineIndentation("};", --indentation);
                }
            sb.AppendLine();

            //Dictionary Properties
            foreach (ShaderSourceType sourceType in sourceTypes)
                if (TryGetTypeStringForShaderSourceType(sourceType, out string? sourceTypeString))
                {
                    sb.AppendLineIndentation($"public override bool TryGetSource{sourceType}(string backendName, out {sourceTypeString} source)", indentation);
                    sb.AppendLineIndentation($"=> _sources{sourceType}.TryGetValue(backendName, out source);", indentation + 1);
                }
            sb.AppendLine();

            sb.AppendLineIndentation("}", --indentation);
            if (isInNamespace)
                sb.AppendLineIndentation("}", --indentation);

            SourceText sourceText = SourceText.From(sb.ToString(), Encoding.UTF8);
            //Debug.WriteLine(sourceText);
            context.AddSource($"{shaderSource.Name}.g.cs", sourceText);
        }

        #endregion

        #region Public Methods

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //Filter for Classes with ShaderAttribute on them
            IncrementalValuesProvider<ClassDeclarationSyntax> shaderClassDeclarations =
                context.SyntaxProvider.CreateSyntaxProvider(
                        static (s, ct) => CanBeShaderClassDeclaration(s),
                        static (ctx, ct) => GetSemanticTargetForShaderClassDeclaration(ctx, ct)).
                    Where(static m => m != null)!;

            //Get Compilations for the Shader Classes
            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationWithShaderClasses =
                context.CompilationProvider.Combine(shaderClassDeclarations.Collect());


            //Filter for Types with ShaderTypeMappingAttributes
            /*IncrementalValuesProvider<TypeDeclarationSyntax> shaderTypeMappingDeclarations =
                context.SyntaxProvider.CreateSyntaxProvider(
                        static (s, ct) => CanBeShaderTypeMappingDeclaration(s),
                        static (ctx, ct) => GetSemanticTargetForShaderTypeMappingDeclaration(ctx, ct)).
                    Where(static m => m != null)!;

            //Get Compilations for Shader Type Mappings
            IncrementalValueProvider<(Compilation, ImmutableArray<TypeDeclarationSyntax>)> compilationWithShaderTypeMappings =
                context.CompilationProvider.Combine(shaderTypeMappingDeclarations.Collect());*/

            //Filter for Types with ShaderTypeMappingAttributes
            IncrementalValuesProvider<AttributeListSyntax> shaderTypeMappingDeclarations =
                context.SyntaxProvider.CreateSyntaxProvider(
                        static (s, ct) => CanBeAssemblyShaderTypeMappingDeclaration(s),
                        static (ctx, ct) => GetSemanticTargetForAssemblyShaderTypeMappingDeclaration(ctx)).
                    Where(static m => m != null)!;

            //Get Compilations for Shader Type Mappings
            IncrementalValueProvider<(Compilation, ImmutableArray<AttributeListSyntax>)> compilationWithShaderTypeMappings =
                context.CompilationProvider.Combine(shaderTypeMappingDeclarations.Collect());

            ShaderValidators validators = new ShaderValidators();
            context.RegisterImplementationSourceOutput(compilationWithShaderClasses.Combine(shaderTypeMappingDeclarations.Collect()), (spc, source) => Execute(source.Item1.Item1, source.Item1.Item2, source.Item2, validators, spc));
        }


        public static IEnumerable<GeneratedShader> GenerateShaders(IEnumerable<ClassDeclarationSyntax> classes, GeneratorConfiguration configuration, Compilation compilation, ShaderValidators validators, IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
        {
            if (!classes.Any())
                yield break;

            //Compile Graphics Shaders
            foreach (ClassDeclarationSyntax c in classes.Distinct())
            {
                SemanticModel model = compilation.GetSemanticModel(c.SyntaxTree);
                if (c.HasAttribute(GraphicsShaderAttributeFullNames, model, cancellationToken))
                {
                    GeneratedShader? generatedShader = ShaderGeneratorClassAnalyzer.AnalyzeShaderClass(c, configuration, compilation, validators, diagnosticReporter, cancellationToken);
                    if (generatedShader != null)
                        yield return generatedShader;
                }
            }

            //TODO: Implement support for Compute Shaders
            /*foreach (ClassDeclarationSyntax c in classes)
            {
                SemanticModel model = compilation.GetSemanticModel(c.SyntaxTree);
                if (c.AttributeLists.HasAttribute(ComputeShaderAttributeFullName, model, context.CancellationToken))
                {
                    GeneratedShader? generatedShader = ShaderGeneratorClassAnalyzer.AnalyzeShaderClass(c, compilation, glslangValidator, diagnosticReporter, cancellationToken);
                    if (generatedShader != null)
                        generatedShaders.Add(generatedShader);
                }
            }*/

            yield break;
        }

        public static ShaderLanguageMappings CreateMappings()
        {
            return new ShaderLanguageMappings();
        }

        #endregion

    }
}
