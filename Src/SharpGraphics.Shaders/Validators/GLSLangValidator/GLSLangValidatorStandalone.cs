using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using SharpGraphics.Shaders.Generator;

namespace SharpGraphics.Shaders.Validators.GLSLangValidator
{

    internal class GLSLangValidatorStandalone : StandaloneValidator, IGLSLangValidator
    {

        #region Properties

        protected override string ResourcePath => "SharpGraphics.Shaders.Validators.GLSLangValidator";
        protected override IEnumerable<EmbeddedExecutable> EmbeddedExecutables => new EmbeddedExecutable[]
        {
            new EmbeddedExecutable(OSPlatform.Windows, Architecture.X64, "glslangValidator_winx64.exe"),
            new EmbeddedExecutable(OSPlatform.Linux, "glslangValidator_linux"),
            new EmbeddedExecutable(OSPlatform.OSX, "glslangValidator_mac"),
        };

        #endregion

        #region Constructors

        public GLSLangValidatorStandalone(string tempFolderPath, string sessionTempFolderPath) : base(tempFolderPath, sessionTempFolderPath) { }

        #endregion

        #region Private Methods

        private string GetGLSLFileExtension(ShaderClassDeclaration shaderClass)
            => shaderClass switch
            {
                GraphicsShaderClassDeclaration gs => gs.GraphicsStage switch
                {
                    GraphicsShaderStages.Vertex => "vert",
                    //GraphicsShaderStages.Geometry => "geom",
                    //GraphicsShaderStages.TessellationControl => "tesc",
                    //GraphicsShaderStages.TessellationEvaluation => "tese",
                    GraphicsShaderStages.Fragment => "frag",
                    _ => "",
                },
                //TODO: Implement for Compute Shaders: ComputeShaderClassDeclaration cs => "comp",
                _ => "",
            };

        private byte[] CompileGLSLToSPIRV(string shaderName, string shaderSource, string shaderFileFormat, Location shaderClassLocation, in IGLSLangValidator.SPIRVTarget target, CancellationToken cancellationToken)
        {
            try
            {
                //Create Shader Filenames
                string shaderFileName = ToWorkingDirectoryFilePath($"{shaderName}.{shaderFileFormat}");
                string shaderOutputFileName = ToWorkingDirectoryFilePath($"{shaderFileName}.spv");

                //Create Source Shader file for Compilation
                /*try
                {
                    string shaderSourceToCompilePath = Path.Combine(_sessionFolderPath, shaderFileName);
                    using (FileStream shaderStream = File.Open(shaderSourceToCompilePath, FileMode.Create, FileAccess.Write))
                    using (StreamWriter shaderStringStream = new StreamWriter(shaderStream))
                        shaderStringStream.Write(shaderSource);
                }
                catch (Exception ex) { throw new GenerationException(ShaderGenerationDiagnositcs.CreateError("5005", "Unexpected error while preparing shader source for SPIR-V compilation: " + ex.Message, shaderClassLocation)); }*/

                string spirvTarget = target.type switch
                {
                    IGLSLangValidator.SPIRVTargetType.Environment => $"--target-env {target.target}",
                    IGLSLangValidator.SPIRVTargetType.SPIRV => $"--target-spv {target.target}",
                    _ => "",
                };

                //Start GLSLangValidator to compile
                //Regular File input: ExecuteValidator($" -V -o {shaderOutputFileName} {shaderFileName}", shaderClassLocation, "5001", "SPIR-V Compilation error", cancellationToken);
                ExecuteValidator($" --stdin -S {shaderFileFormat} -V -o {shaderOutputFileName} {spirvTarget}", shaderSource, shaderClassLocation, "5001", "SPIR-V Compilation error", cancellationToken);

                //Open and read compiled shader file
                try
                {
                    string compiledShaderPath = ToSessionFilePath(shaderOutputFileName);
                    using (FileStream compiledStream = File.Open(compiledShaderPath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] compiledData = new byte[compiledStream.Length];
                        compiledStream.Read(compiledData, 0, compiledData.Length);
                        return compiledData;
                    }
                }
                catch (FileNotFoundException ex) { throw new GenerationException(ShaderGenerationDiagnositcs.CreateError("5003", "Compiled SPIR-V source not found: " + ex.Message, shaderClassLocation)); }
                catch (Exception ex) { throw new GenerationException(ShaderGenerationDiagnositcs.CreateError("5004", "Unexpected error while getting compiled SPIR-V source: " + ex.Message, shaderClassLocation)); }
            }
            catch (GenerationException) { throw; }
            catch (Exception e) { throw new GenerationException(ShaderGenerationDiagnositcs.CreateError("5000", "Unexpected SPIR-V Compilation error: " + e.Message, shaderClassLocation)); }
        }

        private bool ValidateShader(string shaderName, string shaderSource, string shaderFileFormat, Location shaderClassLocation, CancellationToken cancellationToken)
        {
            try
            {
                //Create Shader Filename
                //string shaderFileName = $"{shaderName}.{shaderFileFormat}";

                //Create Source Shader file for Compilation
                /*try
                {
                    string shaderSourceToCompilePath = Path.Combine(_sessionFolderPath, shaderFileName);
                    using (FileStream shaderStream = File.Open(shaderSourceToCompilePath, FileMode.Create, FileAccess.Write))
                    using (StreamWriter shaderStringStream = new StreamWriter(shaderStream))
                        shaderStringStream.Write(shaderSource);
                }
                catch (Exception ex) { throw new GenerationException(ShaderGenerationDiagnositcs.CreateError("5006", "Unexpected error while preparing shader source for GLSLangValidator: " + ex.Message, shaderClassLocation)); }*/

                //Start GLSLangValidator to compile
                //Regular File input: ExecuteValidator(shaderFileName, shaderClassLocation, "5101", "GLSLangValidator error", cancellationToken);
                ExecuteValidator($" --stdin -S {shaderFileFormat}", shaderSource, shaderClassLocation, "5101", "GLSLangValidator error", cancellationToken);
                return true;
            }
            catch (GenerationException) { throw; }
            catch (Exception e) { throw new GenerationException(ShaderGenerationDiagnositcs.CreateError("5100", "Unexpected GLSLangValidator error: " + e.Message, shaderClassLocation)); }
        }

        #endregion

        #region Public Methods

        public byte[] CompileGLSLToSPIRV(ShaderClassDeclaration shaderClass, string shaderSource, in IGLSLangValidator.SPIRVTarget target)
        {
            //TODO: Remove when 32 bit glsLangValidator support is added
            if (!CanExecute)
                return new byte[0];

            //Create Shader Filenames
            string shaderFileFormat = GetGLSLFileExtension(shaderClass);

            Location shaderClassLocation = shaderClass.ClassDeclaration.Identifier.GetLocation();
            if (string.IsNullOrWhiteSpace(shaderFileFormat))
                throw new GenerationException(ShaderGenerationDiagnositcs.CreateError("5002", "Unknown SPIR-V shader stage.", shaderClassLocation));

            return CompileGLSLToSPIRV(shaderClass.Name.Replace('.', '_'), shaderSource, shaderFileFormat, shaderClassLocation, target, shaderClass.CancellationToken);
        }

        public bool ValidateShader(ShaderClassDeclaration shaderClass, string shaderSource)
        {
            //TODO: Remove when 32 bit glsLangValidator support is added
            if (!CanExecute)
                return true;

            //Create Shader Filenames
            string shaderFileFormat = GetGLSLFileExtension(shaderClass);

            Location shaderClassLocation = shaderClass.ClassDeclaration.Identifier.GetLocation();
            if (string.IsNullOrWhiteSpace(shaderFileFormat))
                throw new GenerationException(ShaderGenerationDiagnositcs.CreateError("5102", "Unknown GLSLangValidator shader stage.", shaderClassLocation));

            return ValidateShader(shaderClass.Name.Replace('.', '_'), shaderSource, shaderFileFormat, shaderClassLocation, shaderClass.CancellationToken);
        }

        #endregion

    }
}
