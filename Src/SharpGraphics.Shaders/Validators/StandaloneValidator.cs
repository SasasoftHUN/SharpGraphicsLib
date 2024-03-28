using Microsoft.CodeAnalysis;
using SharpGraphics.Shaders.Generator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SharpGraphics.Shaders.Validators
{
    internal abstract class StandaloneValidator : IValidator
    {

        protected readonly struct EmbeddedExecutableSupportResourceName
        {
            public readonly string embeddedResourceName;
            public readonly string embeddedResourceReName;

            public EmbeddedExecutableSupportResourceName(string embeddedResourceName)
            {
                this.embeddedResourceName = embeddedResourceName;
                this.embeddedResourceReName = embeddedResourceName;
            }
            public EmbeddedExecutableSupportResourceName(string embeddedResourceName, string embeddedResourceReName)
            {
                this.embeddedResourceName = embeddedResourceName;
                this.embeddedResourceReName = embeddedResourceReName;
            }
        }
        protected class EmbeddedExecutable
        {

            private readonly EmbeddedExecutableSupportResourceName[]? _supportResources;

            public readonly OSPlatform osPlatform;
            public readonly Architecture? architecture;
            public readonly string embeddedResourceName;
            public ReadOnlySpan<EmbeddedExecutableSupportResourceName> SupportResources => _supportResources;

            public EmbeddedExecutable(OSPlatform osPlatform, string embeddedResourcePath)
            {
                this.osPlatform = osPlatform;
                this.architecture = default(Architecture?);
                this.embeddedResourceName = embeddedResourcePath;
                _supportResources = null;
            }
            public EmbeddedExecutable(OSPlatform osPlatform, string embeddedResourcePath, ReadOnlySpan<EmbeddedExecutableSupportResourceName> supportResources)
            {
                this.osPlatform = osPlatform;
                this.architecture = default(Architecture?);
                this.embeddedResourceName = embeddedResourcePath;
                _supportResources = supportResources.ToArray(); //Copy for Safety
            }
            public EmbeddedExecutable(OSPlatform osPlatform, Architecture architecture, string embeddedResourcePath)
            {
                this.osPlatform = osPlatform;
                this.architecture = architecture;
                this.embeddedResourceName = embeddedResourcePath;
                _supportResources = null;
            }
            public EmbeddedExecutable(OSPlatform osPlatform, Architecture architecture, string embeddedResourcePath, ReadOnlySpan<EmbeddedExecutableSupportResourceName> supportResources)
            {
                this.osPlatform = osPlatform;
                this.architecture = architecture;
                this.embeddedResourceName = embeddedResourcePath;
                _supportResources = supportResources.ToArray(); //Copy for Safety
            }

        }

        #region Fields

        private static readonly object _validatorLock = new object();

        private readonly bool _canExecute;
        private readonly string _validatorExecutablePath;
        private readonly string _sessionFolderPath;
        private readonly OSPlatform _osPlatform;
        private readonly Architecture _architecture;

        #endregion

        #region Properties

        protected abstract IEnumerable<EmbeddedExecutable> EmbeddedExecutables { get; }
        protected abstract string ResourcePath { get; }

        public bool CanExecute => _canExecute;

        #endregion

        #region Constructors

        protected StandaloneValidator(string tempFolderPath, string sessionTempFolderPath)
        {
            _osPlatform = GetOSPlatform();
            _architecture = RuntimeInformation.ProcessArchitecture;

            if (TrySelectExecutable(EmbeddedExecutables, _osPlatform, _architecture, out EmbeddedExecutable? validatorEmbeddedExecutable) &&
                TryPrepareValidatorExecutable(validatorEmbeddedExecutable, ResourcePath, _osPlatform, tempFolderPath, out string? validatorExecutablePath))
            {
                if (!Directory.Exists(sessionTempFolderPath))
                    Directory.CreateDirectory(sessionTempFolderPath);
                _canExecute = true;
                _sessionFolderPath = sessionTempFolderPath;
                _validatorExecutablePath = validatorExecutablePath;
            }
            else
            {
                _canExecute = false;
                _sessionFolderPath = "";
                _validatorExecutablePath = "";
            }
        }

        #endregion

        #region Private Methods

        private static OSPlatform GetOSPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return OSPlatform.Windows;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OSPlatform.OSX;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return OSPlatform.Linux;
            else throw new PlatformNotSupportedException(RuntimeInformation.OSDescription);
        }
        private static bool TrySelectExecutable(IEnumerable<EmbeddedExecutable> embeddedExecutables, OSPlatform osPlatform, Architecture architecture, [NotNullWhen(returnValue: true)] out EmbeddedExecutable? validatorEmbeddedExecutable)
        {
            foreach (EmbeddedExecutable embeddedExecutable in embeddedExecutables)
                if ((!embeddedExecutable.architecture.HasValue || embeddedExecutable.architecture.Value == architecture) && embeddedExecutable.osPlatform == osPlatform)
                {
                    validatorEmbeddedExecutable = embeddedExecutable;
                    return true;
                }

            validatorEmbeddedExecutable = null;
            return false;
        }

        private static bool TryPrepareValidatorExecutable(EmbeddedExecutable validatorEmbeddedExecutable, string resourcePath, OSPlatform osPlatform, string tempFolderPath, [NotNullWhen(returnValue: true)] out string? validatorExecutablePath)
        {
            if (!TryPrepareValidatorFile(validatorEmbeddedExecutable.embeddedResourceName, validatorEmbeddedExecutable.embeddedResourceName, true, resourcePath, osPlatform, tempFolderPath, out validatorExecutablePath))
                return false;

            foreach (EmbeddedExecutableSupportResourceName supportResource in validatorEmbeddedExecutable.SupportResources)
                if (!TryPrepareValidatorFile(supportResource.embeddedResourceName, supportResource.embeddedResourceReName, false, resourcePath, osPlatform, tempFolderPath, out _))
                    return false;

            return true;
        }
        private static bool TryPrepareValidatorFile(string validatorResourceName, string validatorFileName, bool needExecuteRights, string resourcePath, OSPlatform osPlatform, string tempFolderPath, [NotNullWhen(returnValue: true)] out string? validatorExecutablePath)
        {
            bool isValidatorUsable = false;
            validatorExecutablePath = Path.Combine(tempFolderPath, validatorFileName);

            lock (_validatorLock)
            {
                if (!File.Exists(validatorExecutablePath) || DateTime.Now - File.GetLastWriteTime(validatorExecutablePath) > TimeSpan.FromDays(1))
                {
                    bool validatorCopied = false;

                    string validatorResourcePath = $"{resourcePath}.{validatorResourceName}";
                    Stream? sourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(validatorResourcePath);
                    if (sourceStream != null)
                    {
                        FileStream destinationStream = File.Open(validatorExecutablePath, FileMode.Create, FileAccess.Write);
                        sourceStream.CopyTo(destinationStream);
                        validatorCopied = true;
                    }

                    //OSX, Linux give execute rights to file...
                    if (validatorCopied && needExecuteRights && (osPlatform == OSPlatform.OSX || osPlatform == OSPlatform.Linux))
                    {
                        ProcessStartInfo processStartInfo = new ProcessStartInfo()
                        {
                            FileName = "chmod",
                            Arguments = $"+x {validatorExecutablePath}",
                            CreateNoWindow = true,
                            UseShellExecute = true,
                            RedirectStandardOutput = false,
                            RedirectStandardError = false,
                        };

                        using Process? chmodProcess = Process.Start(processStartInfo);
                        if (chmodProcess != null)
                        {
                            //Wait for compilation to finish or terminate if Cancellation requested
                            //.NET 5,6 has WaitForExitAsync(CancellationToken).Wait();
                            chmodProcess.WaitForExit();
                            if (chmodProcess.ExitCode == 0)
                                isValidatorUsable = true;
                        }
                    }
                    else isValidatorUsable = validatorCopied;
                }
                else isValidatorUsable = true;
            }

            return isValidatorUsable;
        }

        #endregion

        #region Protected Methods

        protected string ToWorkingDirectoryFilePath(string fileName) => _osPlatform == OSPlatform.OSX || _osPlatform == OSPlatform.Linux ? Path.Combine(_sessionFolderPath, fileName) : fileName;
        protected string ToSessionFilePath(string fileName) => Path.Combine(_sessionFolderPath, fileName);

        protected void ExecuteValidator(string arguments, string? stdInput, Location shaderClassLocation, string errorCode, string errorMessage, CancellationToken cancellationToken)
        {
            try
            {
                //Prepare validator Process
                ProcessStartInfo processStartInfo = new ProcessStartInfo()
                {
                    FileName = _validatorExecutablePath,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = !string.IsNullOrWhiteSpace(stdInput),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                if (_osPlatform == OSPlatform.OSX || _osPlatform == OSPlatform.Linux)
                {
                    //processStartInfo.UseShellExecute = true;
                    //processStartInfo.RedirectStandardOutput = false; //TODO: UseShellExecute and Redirect are mutual exclusive.
                    //processStartInfo.RedirectStandardError = false;
                    processStartInfo.WorkingDirectory = _sessionFolderPath;
                }
                else
                {
                    processStartInfo.WorkingDirectory = _sessionFolderPath;
                }

                using Process? validatorProcess = Process.Start(processStartInfo);

                if (validatorProcess == null)
                    throw new GenerationException(ShaderGenerationDiagnositcs.CreateError(
                        "5500", $"{errorMessage}: Unable to start Validator", shaderClassLocation));

                if (!string.IsNullOrWhiteSpace(stdInput))
                {
                    validatorProcess.StandardInput.WriteLine(stdInput);
                    validatorProcess.StandardInput.Close();
                }

                //Wait for compilation to finish or terminate if Cancellation requested
                //.NET 5,6 has WaitForExitAsync(CancellationToken).Wait();
                //glslangValidatorProcess.WaitForExit();
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (!validatorProcess.WaitForExit(15)) //what would be a good number here?
                {
                    if (validatorProcess.HasExited)
                        break;
                    else if (cancellationToken.IsCancellationRequested || sw.ElapsedMilliseconds > 5000L)
                    {
                        validatorProcess.Kill();
                        //throw new OperationCanceledException();
                    }
                }

                //Return with error if compilation failed
                if (validatorProcess.ExitCode != 0)
                {
                    string output = processStartInfo.RedirectStandardOutput ? validatorProcess.StandardOutput.ReadToEnd() : "";
                    string error = processStartInfo.RedirectStandardError ? validatorProcess.StandardError.ReadToEnd() : "";
                    //TODO: Parse output for multiple Error and Warning Diagnostics.
                    throw new GenerationException(ShaderGenerationDiagnositcs.CreateError(
                        $"{errorCode}{validatorProcess.ExitCode}", $"{errorMessage}: {output.LinesToListing()} - {error.LinesToListing()}", shaderClassLocation));
                }
            }
            catch (GenerationException) { throw; }
            catch (Exception e) { throw new GenerationException(ShaderGenerationDiagnositcs.CreateError(errorCode, $"{errorMessage}: {e.Message}: {e.StackTrace?.LinesToListing()}", shaderClassLocation)); }
        }

        #endregion

        #region Public Methods

        public static void PrepareTempFolders(out string tempFolderPath, out string sessionTempFolderPath)
        {
            lock (_validatorLock)
            {
                tempFolderPath = Path.Combine(Path.GetTempPath(), "SharpGraphics.Shaders");
                Directory.CreateDirectory(tempFolderPath);

                foreach (string previousTempDir in Directory.GetDirectories(tempFolderPath))
                    try
                    {
                        if (DateTime.Now - Directory.GetCreationTime(previousTempDir) > TimeSpan.FromMinutes(1) &&
                            DateTime.Now - Directory.GetLastWriteTime(previousTempDir) > TimeSpan.FromMinutes(1))
                            Directory.Delete(previousTempDir, true);
                    }
                    catch { }

                sessionTempFolderPath = Path.Combine(tempFolderPath, Path.GetRandomFileName());
                Directory.CreateDirectory(sessionTempFolderPath);
            }
        }

        #endregion

    }
}
