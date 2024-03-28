using Microsoft.CodeAnalysis;
using SharpGraphics.Shaders.Generator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SharpGraphics.Shaders.Validators.DirectXShaderCompiler
{
    internal class DirectXShaderCompilerStandalone : StandaloneValidator, IDirectXShaderCompiler
    {

        #region Properties

        protected override string ResourcePath => "SharpGraphics.Shaders.Validators.DirectXShaderCompiler";
        protected override IEnumerable<EmbeddedExecutable> EmbeddedExecutables =>
            new EmbeddedExecutable[]
            {
                new EmbeddedExecutable(OSPlatform.Windows, Architecture.X86, "dxc_winx86.exe", new [] { new EmbeddedExecutableSupportResourceName("dxil_winx86.dll", "dxil.dll") }),
                new EmbeddedExecutable(OSPlatform.Windows, Architecture.X64, "dxc_winx64.exe", new [] { new EmbeddedExecutableSupportResourceName("dxil_winx64.dll", "dxil.dll") }),
                new EmbeddedExecutable(OSPlatform.Windows, Architecture.Arm64, "dxc_winarm64.exe", new [] { new EmbeddedExecutableSupportResourceName("dxil_winarm64.dll", "dxil.dll") }),
            };

        #endregion

        #region Constructors

        public DirectXShaderCompilerStandalone(string tempFolderPath, string sessionTempFolderPath) : base(tempFolderPath, sessionTempFolderPath)
        {
        }

        #endregion

        #region Private Methods

        private bool ValidateShader(string shaderName, string shaderSource, string targetProfile, Location shaderClassLocation, CancellationToken cancellationToken)
        {
            try
            {
                //Create Shader Filename
                string shaderFileName = $"{shaderName}_{targetProfile}.hlsl";
                string shaderSourceToCompilePath = ToSessionFilePath(shaderFileName);

                //Create Source Shader file for Compilation
                try
                {
                    using (FileStream shaderStream = File.Open(shaderSourceToCompilePath, FileMode.Create, FileAccess.Write))
                    using (StreamWriter shaderStringStream = new StreamWriter(shaderStream))
                        shaderStringStream.Write(shaderSource);
                }
                catch (Exception ex) { throw new GenerationException(ShaderGenerationDiagnositcs.CreateError("5006", "Unexpected error while preparing shader source for DirectXShaderCompiler: " + ex.Message, shaderClassLocation)); }

                //Start GLSLangValidator to compile
                ExecuteValidator($"{shaderSourceToCompilePath} -T {targetProfile} -E main", null, shaderClassLocation, "5101", "DirectXShaderCompiler error", cancellationToken);
                //ExecuteValidator($"-T {targetProfile} -E main", shaderSource, shaderClassLocation, "5101", "DirectXShaderCompiler error", cancellationToken);
                return true;
            }
            catch (GenerationException) { throw; }
            catch (Exception e) { throw new GenerationException(ShaderGenerationDiagnositcs.CreateError("5100", "Unexpected DirectXShaderCompiler error: " + e.Message, shaderClassLocation)); }
        }

        #endregion

        #region Public Methods

        public bool ValidateShader(ShaderClassDeclaration shaderClass, string shaderSource, string targetShaderModel)
        {
            //Create Shader Filenames
            string targetStage = shaderClass switch
            {
                GraphicsShaderClassDeclaration gs => gs.GraphicsStage switch
                {
                    GraphicsShaderStages.Vertex => "vs",
                    //GraphicsShaderStages.Geometry => "gs",
                    //GraphicsShaderStages.TessellationControl => "hs",
                    //GraphicsShaderStages.TessellationEvaluation => "ds",
                    GraphicsShaderStages.Fragment => "ps",
                    _ => "",
                },
                //TODO: Implement for Compute Shaders: ComputeShaderClassDeclaration cs => "cs",
                _ => "",
            };

            Location shaderClassLocation = shaderClass.ClassDeclaration.Identifier.GetLocation();
            if (string.IsNullOrWhiteSpace(targetStage))
                throw new GenerationException(ShaderGenerationDiagnositcs.CreateError("5102", "Unknown DirectXShaderCompiler shader stage.", shaderClassLocation));

            return ValidateShader(shaderClass.Name.Replace('.', '_'), shaderSource, $"{targetStage}_{targetShaderModel}", shaderClassLocation, shaderClass.CancellationToken);
        }

        #endregion

    }
}
