using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGraphics.Shaders.Builders;
using SharpGraphics.Shaders.Generator;
using SharpGraphics.Shaders.Reporters;
using SharpGraphics.Shaders.Validators;
using SharpGraphics.Shaders.Validators.GLSLangValidator;

namespace SharpGraphics.Shaders.Builders
{
    internal abstract class SPIRVShaderBuilderBase<T> : IShaderBuilder where T : GLSLShaderBuilderBase
    {

        #region Fields

        private readonly List<ShaderGenerationDiagnositcs> _diagnostics = new List<ShaderGenerationDiagnositcs>();

        protected readonly IGLSLangValidator _glslangValidator;
        protected readonly T _glslBuilder;

        protected byte[]? _spirvByteCode;

        #endregion

        #region Properties

        protected abstract IGLSLangValidator.SPIRVTarget SPIRVTarget { get; }

        public abstract string BackendName { get; }

        public ReadOnlySpan<byte> SPIRVByteCode => _spirvByteCode;
        [MemberNotNullWhen(true, "_spirvByteCode")] public bool IsBuildSuccessful => _spirvByteCode != null;

        public IEnumerable<ShaderGenerationDiagnositcs> Diagnositcs => _glslBuilder.Diagnositcs.Concat(_diagnostics);

        #endregion

        #region Constructors

        public SPIRVShaderBuilderBase(T glslShaderBuilder, IGLSLangValidator glslangValidator)
        {
            _glslangValidator = glslangValidator;
            _glslBuilder = glslShaderBuilder;
        }

        #endregion

        #region Public Methods

        public IGeneratedShaderSource GetShaderSource() => new GeneratedBinaryShaderSource(BackendName, IsBuildSuccessful ? _spirvByteCode : new ReadOnlyMemory<byte>());
        public IGeneratedShaderSource GetShaderSourceEmpty() => new GeneratedBinaryShaderSource(BackendName, new ReadOnlyMemory<byte>());

        public IShaderBuilder BuildGraphics(ShaderClassDeclaration shaderClass)
        {
            if (_glslBuilder.BuildGraphics(shaderClass).IsBuildSuccessful)
            {
                try
                {
                    if (!shaderClass.CancellationToken.IsCancellationRequested)
                    {
                        string? glslSource = _glslBuilder.ShaderSourceCode;
                        if (glslSource != null)
                            _spirvByteCode = _glslangValidator.CompileGLSLToSPIRV(shaderClass, glslSource, SPIRVTarget);
                    }
                }
                catch (GenerationException e)
                {
                    _diagnostics.Add(e.Diagnositcs);
                }
                catch (OperationCanceledException) { }
                catch { }
            }

            return this;
        }

        public void ReportDiagnostics(IDiagnosticReporter diagnosticReporter)
        {
            foreach (ShaderGenerationDiagnositcs diagnositcs in _diagnostics)
                diagnosticReporter.Report(diagnositcs, $"({BackendName}) ");
        }

        #endregion

    }
}
