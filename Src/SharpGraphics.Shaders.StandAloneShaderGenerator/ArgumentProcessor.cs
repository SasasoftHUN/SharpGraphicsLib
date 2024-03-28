using Microsoft.Build.Locator;
using SharpGraphics.Shaders.Generator;
using SharpGraphics.Shaders.StandAloneShaderGenerator.OutputWriters;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGraphics.Shaders.StandAloneShaderGenerator
{
    internal static class ArgumentProcessor
    {

        #region Private Methods

        private static bool TryGetNextArgument(string[] args, ref int currentI, [NotNullWhen(returnValue: true)] out string? arg)
        {
            if (args.Length > currentI + 1)
            {
                arg = args[++currentI];
                return true;
            }
            else
            {
                arg = null;
                return false;
            }
        }

        private static void ListSDKs()
        {
            IEnumerable<VisualStudioInstance> instances = MSBuildLocator.QueryVisualStudioInstances();
            Console.WriteLine($"Detected {instances.Count()} .NET SDKs:");
            foreach (VisualStudioInstance instance in instances)
                Console.WriteLine($"\t{instance.Name}\\{instance.Version}");
        }
        private static bool TrySelectSDK(string sdkID, [NotNullWhen(returnValue: true)] out VisualStudioInstance? selectedInstance)
        {
            string[] sdkIDSplit = sdkID.Split('\\');
            if (sdkIDSplit.Length == 2)
            {
                if (Version.TryParse(sdkIDSplit[1], out Version? version))
                {
                    foreach (VisualStudioInstance instance in MSBuildLocator.QueryVisualStudioInstances())
                        if (instance.Name == sdkIDSplit[0] && instance.Version == version)
                        {
                            selectedInstance = instance;
                            return true;
                        }
                    Console.Error.WriteLine($"SDK with name {sdkIDSplit[0]} and version {version} is not found!");
                }
                else Console.Error.WriteLine($"SDK ID {sdkID} does not contain valid version after \\ character!");
            }
            else Console.Error.WriteLine($"SDK ID {sdkID} does not contain \\ character to separate SDK name and version!");

            selectedInstance = null;
            return false;
        }

        private static bool IsGACDLLValid(string gacDLL)
        {
            string gacPath = SourceProcessor.GetGACPath();
            return File.Exists(Path.Combine(gacPath, gacDLL));
        }

        private static void ProcessInputFilePath(string arg, List<string> sourcePaths, List<string> projectPaths, List<string> solutionPaths)
        {
            bool foundFiles = false;

            string? directoryPath = Path.GetDirectoryName(arg);
            if (directoryPath == null)
                directoryPath = Path.GetPathRoot(arg) ?? "";

            IEnumerable<string> filePaths = Directory.EnumerateFiles(directoryPath, Path.GetFileName(arg));
            foreach (string filePath in filePaths)
            {
                if (File.Exists(filePath))
                {
                    foundFiles = true;
                    switch (Path.GetExtension(filePath))
                    {
                        case ".cs": sourcePaths.Add(filePath); break;
                        case ".csproj": projectPaths.Add(filePath); break;
                        case ".sln": solutionPaths.Add(filePath); break;
                        default: Console.Error.WriteLine($"File {arg} cannot be used due to it's unsupported file extension!"); break;
                    }
                }
            }
            
            if (!foundFiles)
                Console.Error.WriteLine($"File {arg} is not found!");
        }

        private static void ListTargetBackends()
        {
            IEnumerable<string> backendNames = BackendBuilders.BackendNames;
            Console.WriteLine($"{backendNames.Count()} supported Target Backends:");
            foreach (string backendName in backendNames)
                Console.WriteLine($"\t{backendName}");
        }

        #endregion

        #region Public Methods

        public static bool Process(string[] args, [NotNullWhen(returnValue: true)] out GeneratorConfiguration? configuration)
        {
            configuration = null;

            bool stdInSource = false;
            List<string> sourcePaths = new List<string>();
            List<string> projectPaths = new List<string>();
            List<string> solutionPaths = new List<string>();
            VisualStudioInstance? vsInstance = null;

            List<string> referenceGACDLLs = new List<string>();
            List<string> referencePackages = new List<string>();

            bool isVerbose = false;

            IOutputWriter outputWriter = new StandardOutputWriter();
            List<string> targetBackendNames = new List<string>();

            //Process Arguments
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();
                switch (arg)
                {
                    case "--list-sdks": ListSDKs(); return false;
                    case "--sdk":
                        {
                            if (TryGetNextArgument(args, ref i, out string? sdkID))
                            {
                                if (TrySelectSDK(sdkID, out VisualStudioInstance? selectedInstance))
                                    vsInstance = selectedInstance;
                                else return false;
                            }
                            else
                            {
                                Console.Error.WriteLine($"SDK ID not provided for {arg} option!");
                                return false;
                            }
                            }
                        break;

                    case "--corlib-path": Console.WriteLine($"Path of mscorlib.dll used in .cs compilation: {Path.GetDirectoryName(typeof(Object).Assembly.Location) ?? ""}"); return false;

                    case "--reference-gac":
                    case "--refgac":
                    case "--gac":
                        {
                            if (TryGetNextArgument(args, ref i, out string? gacDLL))
                            {
                                if (IsGACDLLValid(gacDLL))
                                    referenceGACDLLs.Add(gacDLL);
                                else
                                {
                                    Console.Error.WriteLine($"DLL {gacDLL} not found in GAC: {SourceProcessor.GetGACPath()}!");
                                    return false;
                                }
                            }
                            else
                            {
                                Console.Error.WriteLine($"DLL file name not provided for {arg} option!");
                                return false;
                            }
                        }
                        break;

                    case "--reference-package":
                    case "--reference-nuget":
                    case "--refpck":
                    case "--refnuget":
                    case "--pck":
                    case "--nuget":
                        {
                            if (TryGetNextArgument(args, ref i, out string? nuget))
                            {
                                /*if (IsGACDLLValid(gacDLL))
                                    referenceGACDLLs.Add(gacDLL);
                                else
                                {
                                    Console.Error.WriteLine($"NuGet Package {gacDLL} not found in GAC: {SourceProcessor.GetGACPath()}!");
                                    return false;
                                }*/
                                Console.Error.WriteLine($"NuGet Package references not supported yet!");
                                return false;
                            }
                            else
                            {
                                Console.Error.WriteLine($"NuGet Package name not provided for {arg} option!");
                                return false;
                            }
                        }

                    case "--verbose":
                    case "-v":
                        isVerbose = true;
                        break;


                    case "--out-std": outputWriter = new StandardOutputWriter(); break;

                    case "--out-std-base64":
                    case "--out-std-b64":
                        outputWriter = new StandardOutputWriter(true);
                        break;

                    case "--out-std-prefix":
                        {
                            if (TryGetNextArgument(args, ref i, out string? shaderNamePrefix))
                            {
                                if (TryGetNextArgument(args, ref i, out string? shaderBackendPrefix))
                                    outputWriter = new StandardOutputWriter(false, shaderNamePrefix, shaderBackendPrefix);
                                else
                                {
                                    Console.Error.WriteLine($"Shader Backend Prefix not provided for {arg} option!");
                                    return false;
                                }
                            }
                            else
                            {
                                Console.Error.WriteLine($"Shader Name Prefix not provided for {arg} option!");
                                return false;
                            }
                        }
                        break;
                    case "--out-std-prefix-base64":
                    case "--out-std-prefix-b64":
                    case "--out-std-base64-prefix":
                    case "--out-std-b64-prefix":
                        {
                            if (TryGetNextArgument(args, ref i, out string? shaderNamePrefix))
                            {
                                if (TryGetNextArgument(args, ref i, out string? shaderBackendPrefix))
                                    outputWriter = new StandardOutputWriter(true, shaderNamePrefix, shaderBackendPrefix);
                                else
                                {
                                    Console.Error.WriteLine($"Shader Backend Prefix not provided for {arg} option!");
                                    return false;
                                }
                            }
                            else
                            {
                                Console.Error.WriteLine($"Shader Name Prefix not provided for {arg} option!");
                                return false;
                            }
                        }
                        break;

                    case "--out-file": outputWriter = new FileOutputWriter(); break;


                    case "--list-target-backends":
                    case "--list-backends":
                    case "--list-targets":
                        ListTargetBackends();
                        return false;

                    case "--target-backend":
                    case "--target-backends":
                    case "--target":
                    case "--targets":
                    case "--backend":
                    case "--backends":
                        {
                            if (TryGetNextArgument(args, ref i, out string? targetBackends))
                            {
                                string[] backendNameSplit = targetBackends.Split('.');
                                for (int j = 0; j < backendNameSplit.Length; j++)
                                    if (BackendBuilders.IsBackendNameValid(backendNameSplit[j]))
                                        targetBackendNames.Add(backendNameSplit[j]);
                                    else
                                    {
                                        Console.Error.WriteLine($"Unknown Target Backend Name: {backendNameSplit[j]}!");
                                        return false;
                                    }
                            }
                            else
                            {
                                Console.Error.WriteLine($"Target Backend Name(s) not provided for {arg} option!");
                                return false;
                            }
                        }
                        break;

                    case "--std-in":
                    case "--stdin":
                        stdInSource = true;
                        break;

                    default: ProcessInputFilePath(arg, sourcePaths, projectPaths, solutionPaths); break; //Some input file to compile and generate shader from
                }
            }


            if (stdInSource)
            {
                if (sourcePaths.Any() || projectPaths.Any() || solutionPaths.Any())
                {
                    Console.Error.WriteLine("Should not provide input files of any kind when Standard Input is used for source input!");
                    return false;
                }
                configuration = new GeneratorConfiguration(referenceGACDLLs, referencePackages, isVerbose, outputWriter, targetBackendNames);
                return true;
            }

            //Try Get first VS Instance if needed and not yet selected
            if ((projectPaths.Any() || solutionPaths.Any()))
            {
                if (sourcePaths.Any())
                {
                    Console.Error.WriteLine("Cannot mix project and solution files with C# source files in a single execution!");
                    return false;
                }

                if (vsInstance == null)
                {
                    IEnumerable<VisualStudioInstance> instances = MSBuildLocator.QueryVisualStudioInstances();
                    if (instances.Any())
                        vsInstance = instances.First();
                    else
                    {
                        Console.Error.WriteLine(".NET SDKs not detected!");
                        return false;
                    }
                }

                if (referenceGACDLLs.Any() || referencePackages.Any())
                {
                    Console.Error.WriteLine("Cannot reference GAC DLLs or NuGet Packages when compiling complete Projects or Solutions. Add these references into the .csproj files.");
                    return false;
                }

                configuration = new GeneratorConfiguration(projectPaths, solutionPaths, vsInstance, isVerbose, outputWriter, targetBackendNames);
                return true;
            }
            //If any source is provided, package and return Configuration
            else if (sourcePaths.Any())
            {
                configuration = new GeneratorConfiguration(sourcePaths, referenceGACDLLs, referencePackages, isVerbose, outputWriter, targetBackendNames);
                return true;
            }
            else Console.Error.WriteLine($"No input files provided!");

            return false;
        }

        #endregion

    }
}
