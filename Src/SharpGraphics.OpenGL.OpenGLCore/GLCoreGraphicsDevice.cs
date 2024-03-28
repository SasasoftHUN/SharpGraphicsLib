using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using SharpGraphics.GraphicsViews;
using SharpGraphics.OpenGL.Commands;
using SharpGraphics.OpenGL.Contexts;
using SharpGraphics.OpenGL.OpenGLCore.Contexts;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using static SharpGraphics.GraphicsSwapChain;

namespace SharpGraphics.OpenGL.OpenGLCore
{

    internal sealed class GLCoreGraphicsDevice : GLGraphicsDevice
    {

        #region Constructors

        internal GLCoreGraphicsDevice(GLCoreGraphicsManagement management, IGraphicsView view, IGLContextCreationRequest? contextCreationRequest) : base(management, view, contextCreationRequest) { }

        #endregion

        #region Private Methods

        private void DebugMessage(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            switch (severity)
            {
                case DebugSeverity.DebugSeverityHigh:
                    if (_management.DebugLevel == DebugLevel.None)
                        return;
                    break;

                case DebugSeverity.DebugSeverityMedium:
                    if (_management.DebugLevel == DebugLevel.None || _management.DebugLevel == DebugLevel.Errors)
                        return;
                    break;

                case DebugSeverity.DontCare:
                case DebugSeverity.DebugSeverityNotification:
                case DebugSeverity.DebugSeverityLow:
                default:
                    if (_management.DebugLevel != DebugLevel.Everything)
                        return;
                    break;
            }

            StringBuilder errorBuilder = new StringBuilder();
            errorBuilder.Append(source.ToString());
            errorBuilder.Append(" - ");
            errorBuilder.Append(type.ToString());
            errorBuilder.Append(" (");
            errorBuilder.Append(severity.ToString());
            errorBuilder.Append(") <");
            errorBuilder.Append(id);
            errorBuilder.Append("> --- ");

            errorBuilder.Append(Marshal.PtrToStringAnsi(message, length));

            switch (severity)
            {
                case DebugSeverity.DebugSeverityHigh:
                    Debug.Fail(errorBuilder.ToString());
                    break;

                case DebugSeverity.DontCare:
                case DebugSeverity.DebugSeverityNotification:
                case DebugSeverity.DebugSeverityLow:
                case DebugSeverity.DebugSeverityMedium:
                default:
                    Debug.WriteLine(errorBuilder.ToString());
                    break;
            }
        }

        #endregion

        #region Protected Methods

        protected override IGLContext CreateContext(IGraphicsView view, IGLContextCreationRequest? contextCreationRequest, out GLGraphicsDeviceInfo deviceInfo)
        {
            //TODO: Implement support for MultiSampling
            Span<GLContextVersion> versionRequests = stackalloc GLContextVersion[]
{
                new GLContextVersion(4, 6),
                new GLContextVersion(4, 5),
                new GLContextVersion(4, 4),
                new GLContextVersion(4, 3),
                new GLContextVersion(4, 2),
            };

            IGLContext context;
            switch (_management.OperatingSystem)
            {
                case OperatingSystem.Windows:
                    context = new WGLCoreContext(view, versionRequests, _management.DebugLevel != DebugLevel.None, contextCreationRequest as WGLContextCreationRequest);
                    break;

#if OPENTK4 //TODO: Use for all OpenTK versions when implemented properly
                case OperatingSystem.Linux:
                    context = new LinuxCoreContextFactory().CreateContext(view, versionRequests, _management.DebugLevel != DebugLevel.None);
                    break;
#endif

                /*case OperatingSystem.Android:
                case OperatingSystem.MacOS:
                case OperatingSystem.UWP:*/
                default:
#if OPENTK4
                    throw new PlatformNotSupportedException(_management.OperatingSystem.ToString());
#else
                    context = new GLCoreLegacyContext(view, _management.OperatingSystem);
                    break;
#endif
            }


            try
            {
                GLCoreGraphicsDeviceFeatures features = new GLCoreGraphicsDeviceFeatures();
                deviceInfo = new GLCoreGraphicsDeviceInfo(features, new GLCoreGraphicsDeviceLimits(features));
            }
            catch
            {
                context.Dispose();
                throw;
            }


            return context;
        }

        protected override void InitializeOnThread()
        {
            GL.GetInteger(GetPName.ContextFlags, out int contextFlagsInt);

            //Initialize Debug if needed
            /*
#if OPENTK4
            ContextFlags contextFlags = (ContextFlags)contextFlagsInt;
            if (contextFlags.HasFlag(ContextFlags.Debug))
#else
            GraphicsContextFlags contextFlags = (GraphicsContextFlags)contextFlagsInt;
            if (contextFlags.HasFlag(GraphicsContextFlags.Debug))
#endif
            {
                GL.Enable(EnableCap.DebugOutput);
                GL.Enable(EnableCap.DebugOutputSynchronous);
                GL.DebugMessageCallback(DebugMessage, IntPtr.Zero);
                GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DontCare, 0, new int[0], true);
            }*/

            GL.Enable(EnableCap.TextureCubeMapSeamless);
        }

        protected override void AddGLFinishCommand() => SubmitCommand(new Commands.GLCoreFinishCommand());

        protected override GLCommandProcessor CreateCommandProcessor() => new GLCoreCommandProcessor(this);
        protected override GLGraphicsSwapChain CreateSwapChain(IGraphicsView view, GLCommandProcessor commandProcessor)
            => new GLCoreGraphicsSwapChain(this, view, commandProcessor);

        protected override IGLDataBuffer<T> CreateDataBuffer<T>(DataBufferType bufferType, MappableMemoryType? memoryType, uint dataCapacity, bool isAligned)
        {
            GLCoreDataBuffer<T> dataBuffer = new GLCoreDataBuffer<T>(this, dataCapacity, bufferType, isAligned ? bufferType : DataBufferType.Unknown, memoryType);
            InitializeResource(dataBuffer);
            return dataBuffer;
        }

        #endregion

        #region Public Methods

        public override void CheckErrors(string location)
        {
            if (_management.DebugLevel != DebugLevel.None)
                GLCoreUtils.DebugWriteGLErrors(location);
        }
        public override void CheckErrors(string location, IGLCommand command)
        {
            if (_management.DebugLevel != DebugLevel.None)
                GLCoreUtils.DebugWriteGLErrors(location, command);
        }

        public override IRenderPass CreateRenderPass(in ReadOnlySpan<RenderPassAttachment> attachments, in ReadOnlySpan<RenderPassStep> steps) => new GLCoreRenderPass(this, attachments, steps);

        public override IGraphicsShaderProgram CompileShaderProgram(in GraphicsShaderSource shaderSource)
        {
            if (shaderSource.shaderSource is ShaderSourceText textShaderSource)
            {
                GLCoreGraphicsShaderProgram shaderProgram = new GLCoreGraphicsShaderProgram(this, textShaderSource, shaderSource.stage);
                InitializeResource(shaderProgram);
                return shaderProgram;
            }
            else throw new ArgumentException("GLCoreGraphicsDevice can only use Text Shader Sources!", "shaderSource.shaderSource");
        }

        public override IGraphicsPipeline CreatePipeline(in GraphicsPipelineConstuctionParameters constuction)
        {
            GLCoreGraphicsPipeline pipeline = new GLCoreGraphicsPipeline(this, constuction);
            InitializeResource(pipeline);
            return pipeline;
        }

        public override IStagingDataBuffer<T> CreateStagingDataBuffer<T>(DataBufferType bufferType, uint dataCapacity = 0)
        {
            if (GLFeatures.IsBufferStorageSupported)
            {
                GLCoreStagingDataBufferStorage<T> stagingBuffer = new GLCoreStagingDataBufferStorage<T>(this, bufferType, dataCapacity);
                InitializeResource(stagingBuffer);
                return stagingBuffer;
            }
            else return base.CreateStagingDataBuffer<T>(bufferType, dataCapacity);
        }

        public override TextureSampler CreateTextureSampler(in TextureSamplerConstruction construction)
        {
            GLCoreTextureSampler sampler = new GLCoreTextureSampler(this, construction);
            InitializeResource(sampler);
            return sampler;
        }

        public override ITexture2D CreateTexture2D(DataFormat format, Vector2UInt resolution, TextureType textureType, in MemoryType memoryType, uint mipLevels = 0u)
        {
            IGLTexture2D texture = format.IsRenderBuffer(textureType, memoryType, mipLevels) ?
                new GLCoreRenderBuffer(this, resolution, textureType, format, 1u, 1u) :
                new GLCoreTexture2D(this, resolution, textureType, format, mipLevels.CalculateMipLevels(resolution));
            InitializeResource(texture);
            return texture;
        }
        public override ITextureCube CreateTextureCube(DataFormat format, Vector2UInt resolution, TextureType textureType, in MemoryType memoryType, uint mipLevels = 0u)
        {
            GLCoreTextureCube texture = new GLCoreTextureCube(this, resolution, textureType, format, mipLevels.CalculateMipLevels(resolution));
            InitializeResource(texture);
            return texture;
        }

        public override PipelineResourceLayout CreatePipelineResourceLayout(in PipelineResourceProperties properties) => GLFeatures.IsMultiBindSupported ? new GLCorePipelineResourceLayout(properties) : base.CreatePipelineResourceLayout(properties);

        #endregion

    }
}
