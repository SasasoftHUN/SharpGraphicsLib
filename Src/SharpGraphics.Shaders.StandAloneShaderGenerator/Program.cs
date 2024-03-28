using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpGraphics.Shaders;
using SharpGraphics.Shaders.Generator;
using System.Collections.Immutable;
using System.Runtime.Loader;
using SharpGraphics.Shaders.Reporters;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Validators;

namespace SharpGraphics.Shaders.StandAloneShaderGenerator
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            if (ArgumentProcessor.Process(args, out GeneratorConfiguration? configuration))
            {
                //Prepare Shader Validators
                ShaderValidators shaderValidators = new ShaderValidators();

                // NOTE: Be sure to register an instance with the MSBuildLocator 
                //       before calling MSBuildWorkspace.Create()
                //       otherwise, MSBuildWorkspace won't MEF compose.
                if (configuration.VSInstance != null)
                {
                    MSBuildLocator.RegisterInstance(configuration.VSInstance);
                    if (configuration.IsVerbose)
                        Console.WriteLine($"Using .NET SDK at '{configuration.VSInstance.MSBuildPath}' to load projects.");
                }

                IAsyncEnumerable<GeneratedShader>? generatedShaders = null;

                if (configuration.StandardInput)
                    generatedShaders = SourceProcessor.ProcessSourceAsync(await Console.In.ReadToEndAsync(), configuration.ReferenceGACDLLs, configuration.ReferenceNuGetPackages, configuration.IsVerbose, configuration.TargetBackendNames, shaderValidators);
                else if (configuration.SourcePaths.Any())
                    generatedShaders = SourceProcessor.ProcessSourceFilesAsync(configuration.SourcePaths, configuration.ReferenceGACDLLs, configuration.ReferenceNuGetPackages, configuration.IsVerbose, configuration.TargetBackendNames, shaderValidators);
                else
                {
                    foreach (string projectPath in configuration.ProjectPaths)
                        generatedShaders = SourceProcessor.ProcessProjectFileAsync(projectPath, configuration.IsVerbose, configuration.TargetBackendNames, shaderValidators);
                    foreach (string solutionPath in configuration.SolutionPaths)
                        generatedShaders = SourceProcessor.ProcessSolutionFileAsync(solutionPath, configuration.IsVerbose, configuration.TargetBackendNames, shaderValidators);
                }

                if (generatedShaders != null)
                    await foreach (GeneratedShader generatedShader in generatedShaders)
                        configuration.OutputWriter.OutShader(generatedShader);
            }
        }

    }
}
