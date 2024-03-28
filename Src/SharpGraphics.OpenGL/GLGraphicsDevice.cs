using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using static SharpGraphics.GraphicsSwapChain;
using SharpGraphics.OpenGL.Commands;
using SharpGraphics.Utils;
using System.Diagnostics.CodeAnalysis;
using SharpGraphics.OpenGL.Contexts;
using System.Runtime.CompilerServices;

namespace SharpGraphics.OpenGL
{

    /// <summary>
    /// Represents an OpenGL Context attached to a <see cref="View.IGraphicsView"/>.
    /// Can be <see cref="IComputeDevice"/>, however a <see cref="View.IGraphicsView"/> is still needed for initializing it, so it's basically always an <see cref="IGraphicsComputeDevice"/> (or <see cref="IGraphicsDevice"/> if Compute is not supported).
    /// </summary>
    public abstract class GLGraphicsDevice : GraphicsDevice, IGraphicsComputeDevice
    {

        #region Fields

        private bool _isDisposed;

        private readonly GLCommandProcessor _commandProcessor;
        private readonly GLCommandProcessor[] _commandProcessors;
        private readonly GLGraphicsSwapChain _swapChain;

        private readonly Thread _contextThread;
        private readonly CancellationTokenSource _contextThreadCancellationTokenSource;

        private readonly BlockingCollection<IGLCommand> _commands = new BlockingCollection<IGLCommand>();

        protected readonly IGLContext _context;
        protected readonly GLGraphicsDeviceInfo _glDeviceInfo;
        protected readonly GLGraphicsDeviceFeatures _glFeatures;
        protected readonly GraphicsDeviceLimits _limits;

        #endregion

        #region Properties

        public GLCommandProcessor CommandProcessor => _commandProcessor;
        public override ReadOnlySpan<GraphicsCommandProcessor> CommandProcessors => _commandProcessors;
        public GraphicsSwapChain SwapChain => _swapChain;

        public override GraphicsDeviceInfo DeviceInfo => _glDeviceInfo;
        public override GraphicsDeviceFeatures Features => _glFeatures;
        public override GraphicsDeviceLimits Limits => _limits;

        public IGLContext GLContext => _context;
        public GLGraphicsDeviceInfo GLDeviceInfo => _glDeviceInfo;
        public GLGraphicsDeviceFeatures GLFeatures => _glFeatures;

        #endregion

        #region Constructors

        protected internal GLGraphicsDevice(GraphicsManagement management, IGraphicsView view, IGLContextCreationRequest? contextCreationRequest) : base(management)
        {
            //Context must be created first, but base constructor runs before derived
            _context = CreateContext(view, contextCreationRequest, out _glDeviceInfo);
            CheckErrors("After Context Creation");
            _glFeatures = _glDeviceInfo.GLFeatures;
            _limits = _glDeviceInfo.Limits;

            _commandProcessor = CreateCommandProcessor();
            _commandProcessors = new GLCommandProcessor[] { _commandProcessor };
            _swapChain = CreateSwapChain(view, _commandProcessor);
            CheckErrors("After SwapChain Creation");

            _context.UnBind();

            _contextThreadCancellationTokenSource = new CancellationTokenSource();
            _contextThread = new Thread(() =>
            {
                _context.Bind();
                CheckErrors("After Context Thread Bind");
                InitializeOnThread();
                CheckErrors("After Context Thread Initialization");

                try
                {
                    while (!_contextThreadCancellationTokenSource.IsCancellationRequested && !_isDisposed)
                    {
                        IGLCommand command = _commands.Take(_contextThreadCancellationTokenSource.Token);
                        CheckErrors("Before Command", command);
                        //Debug.WriteLine(command);
                        command.Execute();
                        CheckErrors("After Command", command);
                    }
                }
                catch (OperationCanceledException) { }

                //foreach (IDisposable disposableResource in _disposableResourcesList)
                _context.UnBind();
            });
            _contextThread.Name = "OpenGL Context Thread";
            _contextThread.Start();
        }

        ~GLGraphicsDevice() => Dispose(false);

        #endregion

        #region Protected Methods

        protected abstract IGLContext CreateContext(IGraphicsView view, IGLContextCreationRequest? contextCreationRequest, out GLGraphicsDeviceInfo deviceInfo); //Context must be created first, but base constructor runs before derived
        protected abstract void InitializeOnThread();
        protected abstract GLCommandProcessor CreateCommandProcessor();
        protected abstract void AddGLFinishCommand();
        protected abstract GLGraphicsSwapChain CreateSwapChain(IGraphicsView view, GLCommandProcessor commandProcessor);

        protected abstract IGLDataBuffer<T> CreateDataBuffer<T>(DataBufferType bufferType, MappableMemoryType? memoryType, uint dataCapacity, bool isAligned) where T : unmanaged;

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

                _swapChain?.Dispose();
                _commandProcessor?.Dispose();

                if (_contextThreadCancellationTokenSource != null && !_contextThreadCancellationTokenSource.IsCancellationRequested)
                    _contextThreadCancellationTokenSource.Cancel();
                _commands.CompleteAdding();

                if (_contextThread != null && !_contextThread.Join(1000))
                    _contextThread.Interrupt();

                _contextThreadCancellationTokenSource?.Dispose();

#if DEBUG
                if (_commands.Count > 0)
                    Debug.WriteLine($"Warning: OpenGL Graphics Device is being disposed with {_commands.Count} remaining command(s) in it!");
#endif
                _commands.Dispose();

                _context?.Dispose();

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Internal Methods

        protected internal void SubmitCommand(IGLCommand command) => _commands.Add(command);

        #endregion

        #region Public Methods


        [Conditional("DEBUG")]
        public abstract void CheckErrors(string location);
        [Conditional("DEBUG")]
        public abstract void CheckErrors(string location, IGLCommand command);

        public override void WaitForIdle()
        {
            if (_isDisposed)
                return;

            AddGLFinishCommand();
            while (_commands.Count > 0)
                Thread.Yield(); //TODO: Better waiting for Command completition in OpenGL?
        }
        public void WaitForContextThreadIdle()
        {
            if (_isDisposed)
                return;
            while (_commands.Count > 0)
                Thread.Yield(); //TODO: Better waiting for Command completition in OpenGL?
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InitializeResource(IGLResource resource) => _commands.Add(new GLResourceInitializationCommand(resource));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FreeResource(IGLResource resource) => _commands.Add(new GLResourceFreeCommand(resource));

        public override IDeviceOnlyDataBuffer<T> CreateDeviceOnlyDataBuffer<T>(DataBufferType bufferType, uint dataCapacity = 0u, bool isAligned = true)
            => CreateDataBuffer<T>(bufferType, default(MappableMemoryType?), dataCapacity, isAligned);
        public override IMappableDataBuffer<T> CreateMappableDataBuffer<T>(DataBufferType bufferType, MappableMemoryType memoryType, uint dataCapacity = 0u, bool isAligned = true)
            => CreateDataBuffer<T>(bufferType, memoryType, dataCapacity, isAligned);
        public override IStagingDataBuffer<T> CreateStagingDataBuffer<T>(DataBufferType bufferType, uint dataCapacity = 0u)
            => new GLEmulatedStagingDataBuffer<T>(this, dataCapacity, bufferType);

        public abstract IRenderPass CreateRenderPass(in ReadOnlySpan<RenderPassAttachment> attachments, in ReadOnlySpan<RenderPassStep> steps);

        public abstract IGraphicsShaderProgram CompileShaderProgram(in GraphicsShaderSource shaderSource);

        public abstract IGraphicsPipeline CreatePipeline(in GraphicsPipelineConstuctionParameters constuction);

        public override PipelineResourceLayout CreatePipelineResourceLayout(in PipelineResourceProperties properties) => new GLPipelineResourceLayout(properties);

        #endregion

    }

    public class GLGraphicsDeviceCreationException : GraphicsDeviceCreationException
    {

        public GLGraphicsDeviceCreationException() { }
        public GLGraphicsDeviceCreationException(string message) : base(message) { }

    }

}
