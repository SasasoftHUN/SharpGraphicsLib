using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using SharpGraphics.Shaders;
using System.Threading;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Reporters;
using SharpGraphics.Shaders.Validators;
using SharpGraphics.Shaders.Builders;

namespace SharpGraphics.Shaders.Generator
{
    internal static class ShaderGeneratorClassAnalyzer
    {

        private static GeneratedShader? AnalyzeGraphicsShaderClass(ClassDeclarationSyntax classDeclaration, GeneratorConfiguration configuration, Compilation compilation, SemanticModel model, ShaderValidators validators, IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
        {
            GraphicsShaderClassDeclaration? graphicsShaderClassDeclaration = null;

            //Get Shader Attribute
            if (classDeclaration.TryGetAttribute(ShaderGenerator.VertexShaderAttributeFullName, model, cancellationToken, out _))
            {
                if (classDeclaration.TryGetAttribute(ShaderGenerator.FragmentShaderAttributeFullName, model, cancellationToken, out _))
                {
                    diagnosticReporter.ReportError("1002", classDeclaration.Identifier.ValueText + " Graphics Shader has multiple Shader Stage Attributes.", Location.Create(classDeclaration.SyntaxTree, classDeclaration.Span));
                    return null;
                }
                else graphicsShaderClassDeclaration = new VertexShaderClassDeclaration(classDeclaration, configuration.Mappings, diagnosticReporter, compilation, model, cancellationToken);
            }
            else if (classDeclaration.TryGetAttribute(ShaderGenerator.FragmentShaderAttributeFullName, model, cancellationToken, out _))
            {
                graphicsShaderClassDeclaration = new FragmentShaderClassDeclaration(classDeclaration, configuration.Mappings, diagnosticReporter, compilation, model, cancellationToken);
            }

            if (graphicsShaderClassDeclaration == null)
            {
                diagnosticReporter.ReportError("1000", classDeclaration.Identifier.ValueText + " Graphics Shader class does not have a GraphicsShader Attribute.", Location.Create(classDeclaration.SyntaxTree, classDeclaration.Span));
                return null;
            }

            if (!graphicsShaderClassDeclaration.IsValidForGeneration)
                return null;

            //Get Shader Class' Namespace!
            classDeclaration.TryGetNamespace(model, cancellationToken, out string? ns);

            // --- GENERATE SHADER SOURCES ---
            GeneratedShader generatedShader = new GeneratedShader(ns, classDeclaration.Identifier.ValueText, graphicsShaderClassDeclaration.Stage);

            IDiagnosticReporter builderDiagnosticReporter = new DistinctDiagnosticReporter(diagnosticReporter);

            IEnumerable<string> targetBackendNames = configuration.IsTargetBackendOverriden ? configuration.TargetBackendOverrideNames : graphicsShaderClassDeclaration.TargetBackendNames;
            foreach (string targetBackendName in targetBackendNames)
                if (BackendBuilders.TryGetShaderBuilder(targetBackendName, configuration.Mappings, validators, out IShaderBuilder? shaderBuilder))
                {
                    if (!shaderBuilder.BuildShaderSource(generatedShader, graphicsShaderClassDeclaration, builderDiagnosticReporter, cancellationToken))
                        return generatedShader;
                }
                else
                {
                    diagnosticReporter.ReportError("1800", $"Unknown Backend Name: {targetBackendName}", classDeclaration.Identifier.GetLocation());
                    return generatedShader;
                }

            return generatedShader;
        }

        private static bool BuildShaderSource(this Builders.IShaderBuilder builder, GeneratedShader generatedShader, ShaderClassDeclaration shaderClassDeclaration, IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
                builder.BuildGraphics(shaderClassDeclaration).ReportDiagnostics(diagnosticReporter);

            //if (builder.IsBuildSuccessful && !context.CancellationToken.IsCancellationRequested) //doesn't emit shader string if it's failed to build. Good for release, bad for debug
            if (!cancellationToken.IsCancellationRequested)
            {
                generatedShader.AddShaderSource(builder.GetShaderSource());
                return true;
            }
            else
            {
                generatedShader.AddShaderSource(builder.GetShaderSourceEmpty());
                return false;
            }
        }

        public static GeneratedShader? AnalyzeShaderClass(ClassDeclarationSyntax classDeclaration, GeneratorConfiguration configuration, Compilation compilation, ShaderValidators validators, IDiagnosticReporter diagnosticReporter, CancellationToken cancellationToken)
        {
            SemanticModel model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);

            bool isComputeShader = classDeclaration.HasAttribute(ShaderGenerator.ComputeShaderAttributeFullName, model, cancellationToken);
            bool isGraphicsShader = classDeclaration.HasAttribute(ShaderGenerator.GraphicsShaderAttributeFullNames, model, cancellationToken);

            if (isGraphicsShader && isComputeShader)
            {
                diagnosticReporter.ReportError("0001", classDeclaration.Identifier.ValueText + " Shader class cannot have multiple Shader Attributes.", Location.Create(classDeclaration.SyntaxTree, classDeclaration.AttributeLists.Span));
                return null;
            }

            if (isComputeShader)
            {
                diagnosticReporter.ReportError("0002", "Compute Shaders are not supported yet.", Location.Create(classDeclaration.SyntaxTree, classDeclaration.AttributeLists.Span));
                return null;
            }

            return isGraphicsShader ? AnalyzeGraphicsShaderClass(classDeclaration, configuration, compilation, model, validators, diagnosticReporter, cancellationToken) : null;
        }

    }
}
