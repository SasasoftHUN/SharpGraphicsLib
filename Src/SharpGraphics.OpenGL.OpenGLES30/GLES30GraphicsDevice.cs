using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;
using OpenTK.Platform;
using SharpGraphics.GraphicsViews;
using SharpGraphics.OpenGL.Commands;
using SharpGraphics.OpenGL.Contexts;
using SharpGraphics.OpenGL.OpenGLES30.Contexts;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using static SharpGraphics.GraphicsSwapChain;

namespace SharpGraphics.OpenGL.OpenGLES30
{

    internal sealed class GLES30GraphicsDevice : GLGraphicsDevice
    {

        #region Fields

        private bool _isDisposed;

        private int _emptyVAOID; //VAO with no attributes, used for pipelines with no VertexInputs

        #endregion

        #region Properties

        internal int EmptyVAOID => _emptyVAOID;

        #endregion

        #region Constructors

        internal GLES30GraphicsDevice(GLES30GraphicsManagement management, IGraphicsView view, IGLContextCreationRequest? contextCreationRequest) : base(management, view, contextCreationRequest) { }

        ~GLES30GraphicsDevice() => Dispose(false);

        #endregion

        #region Protected Methods

        protected override IGLContext CreateContext(IGraphicsView view, IGLContextCreationRequest? contextCreationRequest, out GLGraphicsDeviceInfo deviceInfo)
        {
            //TODO: Implement support for MultiSampling
            Span<GLContextVersion> versionRequests = stackalloc GLContextVersion[] { new GLContextVersion(3, 0) };

            IGLContext context;
            switch (_management.OperatingSystem)
            {
                case OperatingSystem.Windows:
                    context = new WGLES30Context(view, versionRequests, _management.DebugLevel != DebugLevel.None, contextCreationRequest as WGLContextCreationRequest);
                    break;

                case OperatingSystem.Android:
#if OPENTK4
                    throw new PlatformNotSupportedException(_management.OperatingSystem.ToString());
#else
    #if ANDROID
                    context = new GLES30LegacyAndroidContext(view, _management.DebugLevel);
                    break;
    #else
                    throw new PlatformNotSupportedException($"The Library is being used for {_management.OperatingSystem} which is not supported in the current build!");
    #endif
#endif

                /*case OperatingSystem.Linux:
                case OperatingSystem.MacOS:
                case OperatingSystem.UWP:*/
                default:
#if OPENTK4
                    throw new PlatformNotSupportedException(_management.OperatingSystem.ToString());
#else
    #if ANDROID
                    throw new PlatformNotSupportedException($"The Library has been built for Android and being used for {_management.OperatingSystem} which is not supported!");
    #else
                    context = new GLES30LegacyDesktopContext(view, _management.OperatingSystem, _management.DebugLevel);
                    break;
    #endif
#endif
            }


            try
            {
                GLES30GraphicsDeviceFeatures features = new GLES30GraphicsDeviceFeatures();
                deviceInfo = new GLES30GraphicsDeviceInfo(features, new GLES30GraphicsDeviceLimits(features));
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
            GL.GenVertexArrays(1, out _emptyVAOID);
        }

        protected override void AddGLFinishCommand() => SubmitCommand(new Commands.GLES30FinishCommand());

        protected override GLCommandProcessor CreateCommandProcessor() => new GLES30CommandProcessor(this);
        protected override GLGraphicsSwapChain CreateSwapChain(IGraphicsView view, GLCommandProcessor commandProcessor)
            => new GLES30GraphicsSwapChain(this, view, commandProcessor);

        protected override IGLDataBuffer<T> CreateDataBuffer<T>(DataBufferType bufferType, MappableMemoryType? memoryType, uint dataCapacity, bool isAligned)
        {
            GLES30DataBuffer<T> dataBuffer;

            if (bufferType.HasFlag(DataBufferType.VertexData))
                dataBuffer = new GLES30VertexDataBuffer<T>(this, bufferType, dataCapacity, memoryType, isAligned);
            else dataBuffer = new GLES30DataBuffer<T>(this, dataCapacity, bufferType, isAligned ? bufferType : DataBufferType.Unknown, memoryType);

            InitializeResource(dataBuffer);
            return dataBuffer;
        }


        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                /*if (disposing)
                {
                    // Dispose managed state (managed objects).
                }*/

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.
                if (!disposing)
                    Debug.WriteLine($"Disposing OpenGL Context from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_emptyVAOID > 0)
                {
                    GL.DeleteVertexArrays(1, ref _emptyVAOID);
                    _emptyVAOID = 0;
                }

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public override void CheckErrors(string location)
        {
            if (_management.DebugLevel != DebugLevel.None)
                GLES30Utils.DebugWriteGLErrors(location);
        }
        public override void CheckErrors(string location, IGLCommand command)
        {
            if (_management.DebugLevel != DebugLevel.None)
                GLES30Utils.DebugWriteGLErrors(location, command);
        }

        public override IRenderPass CreateRenderPass(in ReadOnlySpan<RenderPassAttachment> attachments, in ReadOnlySpan<RenderPassStep> steps) => new GLES30RenderPass(this, attachments, steps);

        public override IGraphicsShaderProgram CompileShaderProgram(in GraphicsShaderSource shaderSource)
        {
            if (shaderSource.shaderSource is ShaderSourceText textShaderSource)
            {
                GLES30GraphicsShaderProgram shaderProgram = new GLES30GraphicsShaderProgram(this, textShaderSource, shaderSource.stage);
                InitializeResource(shaderProgram);
                return shaderProgram;
            }
            else throw new ArgumentException("GLES30GraphicsDevice can only use Text Shader Sources!", "shaderSource.shaderSource");
        }

        public override IGraphicsPipeline CreatePipeline(in GraphicsPipelineConstuctionParameters constuction)
        {
            GLES30GraphicsPipeline pipeline = new GLES30GraphicsPipeline(this, constuction);
            InitializeResource(pipeline);
            return pipeline;
        }

        public override TextureSampler CreateTextureSampler(in TextureSamplerConstruction construction)
        {
            GLES30TextureSampler sampler = new GLES30TextureSampler(this, construction);
            InitializeResource(sampler);
            return sampler;
        }

        public override ITexture2D CreateTexture2D(DataFormat format, Vector2UInt resolution, TextureType textureType, in MemoryType memoryType, uint mipLevels = 0u)
        {
            IGLTexture2D texture = format.IsRenderBuffer(textureType, memoryType, mipLevels) ?
                new GLES30RenderBuffer(this, resolution, textureType, format, 1u, 1u) :
                new GLES30Texture2D(this, resolution, textureType, format, mipLevels.CalculateMipLevels(resolution));
            InitializeResource(texture);
            return texture;
        }
        public override ITextureCube CreateTextureCube(DataFormat format, Vector2UInt resolution, TextureType textureType, in MemoryType memoryType, uint mipLevels = 0u)
        {
            GLES30TextureCube texture = new GLES30TextureCube(this, resolution, textureType, format, mipLevels.CalculateMipLevels(resolution));
            InitializeResource(texture);
            return texture;
        }

        #endregion

    }
}
