using SharpGraphics.Utils;
using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SharpGraphics.OpenGL.Commands;
using System.Runtime.CompilerServices;
using SharpGraphics.OpenGL.CommandBuffers;
using System.Diagnostics.CodeAnalysis;

namespace SharpGraphics.OpenGL
{
    public abstract class GLGraphicsSwapChain : GraphicsSwapChain
    {

        #region Fields

        private bool _isDisposed;

        private readonly GLCommandProcessor _presentProcessor;

        private GLCommandBufferList[] _presentCommandBuffers;
        private GLWaitableCommand[] _renderingSemaphores;
        private uint _semaphoreCount = 3u;
        private uint _currentSemaphoreIndex = 0u;

        private bool _invalidFBO = true;
        private GLFrameBuffer<ITexture2D, IGLTexture2D>? _framebuffer; //TODO: Multiview? Stereo support?

        #endregion

        #region Properties

        protected override uint FrameCount => 3u;

        public override uint CurrentFrameIndex => 0u;
        public override Vector2UInt Size => _view.ViewSize;
        public abstract bool IsDoubleBuffered { get; }

        public uint SemaphoreCount
        {
            get => _semaphoreCount;
            set
            {
                if (_semaphoreCount != value)
                {
                    if (_semaphoreCount == 0u)
                        throw new ArgumentOutOfRangeException("SemaphoreCount", "Must be greater than 0!");
                    _semaphoreCount = value;
                    _invalidFBO = true;
                }
            }
        }

        #endregion

        #region Constructors

        protected internal GLGraphicsSwapChain(IGraphicsView view, GLCommandProcessor presentProcessor) : base(view)
        {
            _presentProcessor = presentProcessor;

            CreateSemaphoresAndCommandBuffers();

            _view.ViewSizeChanged += _view_ViewSizeChanged;
        }

        #endregion

        #region Private Methods

        private void _view_ViewSizeChanged(object? sender, ViewSizeChangedEventArgs e)
        {
            _invalidFBO = true;
            OnSizeChanged(Size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint GetNextSemaphoreIndex() => (_currentSemaphoreIndex + 1u) % _semaphoreCount;

        [MemberNotNull("_renderingSemaphores", "_presentCommandBuffers")]
        private void CreateSemaphoresAndCommandBuffers()
        {
            //Semaphores
            if (_renderingSemaphores != null)
            {
                foreach (GLWaitableCommand semaphore in _renderingSemaphores)
                    if (semaphore != null)
                        semaphore.Dispose();

                if (_renderingSemaphores.Length != _semaphoreCount)
                    _renderingSemaphores = new GLWaitableCommand[_semaphoreCount];
            }
            else _renderingSemaphores = new GLWaitableCommand[_semaphoreCount];

            for (int i = 0; i < _renderingSemaphores.Length; i++)
                _renderingSemaphores[i] = new GLWaitableCommand(true);
            _currentSemaphoreIndex = 0u;


            //Command Buffers
            if (_presentCommandBuffers != null)
            {
                foreach (GraphicsCommandBuffer commandBuffer in _presentCommandBuffers)
                    if (commandBuffer != null)
                        commandBuffer.Dispose();

                if (_presentCommandBuffers.Length != _semaphoreCount)
                    _presentCommandBuffers = new GLCommandBufferList[_semaphoreCount];
            }
            else _presentCommandBuffers = new GLCommandBufferList[_semaphoreCount];

            for (int i = 0; i < _presentCommandBuffers.Length; i++)
                _presentCommandBuffers[i] = Unsafe.As<GLCommandBufferList>(_presentProcessor.CommandBufferFactory.CreateCommandBuffer());
        }
        private bool CheckFBO()
        {
            if (_invalidFBO || CheckAndResetIfNeededToBeRecreated())
            {
                if (_view.IsViewInitialized)
                {
                    _presentProcessor.Device.WaitForIdle();
                    CreateSemaphoresAndCommandBuffers();
                    if (_framebuffer != null)
                        _framebuffer.Dispose();
                    _framebuffer = CreateFrameBuffer();
                    _presentProcessor.Device.InitializeResource(_framebuffer);
                    _invalidFBO = false;
                    //OnFramesRecreated(_semaphoreCount);
                    GC.Collect(2, GCCollectionMode.Forced, false, true);
                    return true;
                }
                else return false;
            }
            else return true;
        }

        #endregion

        #region Protected Methods

        protected abstract GLFrameBuffer<ITexture2D, IGLTexture2D> CreateFrameBuffer();

        protected bool IsDefaultFBOCompatible(in RenderPassStep step, in ReadOnlySpan<RenderPassAttachment> attachments)
        {
            ReadOnlySpan<uint> colorAttachmentIndices = step.ColorAttachmentIndices;
            if (colorAttachmentIndices.Length != 1) //Must have exactly one Color Attachment
                return false;

            SwapChainConstruction format = Format;
            if (attachments[(int)colorAttachmentIndices[0]].format != format.colorFormat)
                return false;
            //TODO: Implement support for MultiSampling

            if (step.DepthStencilAttachmentIndex != -1) //It's okay if it has a DepthStencil Buffer even if the Pass don't need it
            {
                switch (attachments[step.DepthStencilAttachmentIndex].format)
                {
                    case DataFormat.Depth16un:
                        if (format.depthStencilFormat != DataFormat.Depth16un && format.depthStencilFormat != DataFormat.Depth16un_Stencil8ui)
                            return false;
                        break;
                    case DataFormat.Depth24un:
                        if (format.depthStencilFormat != DataFormat.Depth24un && format.depthStencilFormat != DataFormat.Depth24un_Stencil8ui)
                            return false;
                        break;
                    case DataFormat.Depth32f:
                        if (format.depthStencilFormat != DataFormat.Depth32f && format.depthStencilFormat != DataFormat.Depth32f_Stencil8ui)
                            return false;
                        break;

                    case DataFormat.Stencil8ui:
                        if (format.depthStencilFormat != DataFormat.Depth16un_Stencil8ui && format.depthStencilFormat != DataFormat.Depth24un_Stencil8ui && format.depthStencilFormat != DataFormat.Depth32f_Stencil8ui)
                            return false;
                        break;

                    case DataFormat.Depth16un_Stencil8ui:
                    case DataFormat.Depth24un_Stencil8ui:
                    case DataFormat.Depth32f_Stencil8ui:
                        if (format.depthStencilFormat != attachments[step.DepthStencilAttachmentIndex].format)
                            return false;
                        break;
                    
                    default:
                        break;
                }
            }

            //False on any Input Attachment? It just binds them to Samplers...

            return true;
        }

        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).

                    if (_view != null)
                        _view.ViewSizeChanged -= _view_ViewSizeChanged;
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.
                if (!disposing)
                    Debug.WriteLine($"Disposing OpenGL SwapChain from {(disposing ? "Dispose()" : "Finalizer")}...");

                _presentProcessor.WaitForIdle();

                if (_presentCommandBuffers != null)
                    foreach (GraphicsCommandBuffer commandBuffer in _presentCommandBuffers)
                        if (!commandBuffer.IsDisposed)
                            commandBuffer.Dispose();

                if (_renderingSemaphores != null)
                    foreach (GLWaitableCommand semaphore in _renderingSemaphores)
                        if (!semaphore.IsDisposed)
                            semaphore.Dispose();

                if (_framebuffer != null)
                {
                    _framebuffer.Dispose();
                    _framebuffer = null;
                }

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public override bool TryBeginFrame([NotNullWhen(returnValue: true)] out GraphicsCommandBuffer? commandBuffer, [NotNullWhen(returnValue: true)] out IFrameBuffer<ITexture2D>? frameBuffer)
        {
            if (!CheckFBO() || _framebuffer == null)
            {
                commandBuffer = null;
                frameBuffer = null;
                return false;
            }

            _currentSemaphoreIndex = GetNextSemaphoreIndex();
            if (!_renderingSemaphores[_currentSemaphoreIndex].Wait())
            {
                commandBuffer = null;
                frameBuffer = null;
                return false;
            }

            _presentCommandBuffers[_currentSemaphoreIndex].Reset();
            _presentCommandBuffers[_currentSemaphoreIndex].Begin();

            commandBuffer = _presentCommandBuffers[_currentSemaphoreIndex];
            frameBuffer = Unsafe.As<IFrameBuffer<ITexture2D>>(_framebuffer);
            return true;
        }

        public override bool PresentFrame()
        {
            bool result = false;
            if (_view.IsViewInitialized)
            {
                _presentCommandBuffers[_currentSemaphoreIndex].AddCommand(new GLSwapBuffersCommand(_presentProcessor.Device.GLContext));
                result = true;
            }
            _presentCommandBuffers[_currentSemaphoreIndex].End();
            _presentProcessor.Submit(_presentCommandBuffers[_currentSemaphoreIndex]);
            _presentProcessor.Submit(_renderingSemaphores[_currentSemaphoreIndex]);
            return result;
        }

        #endregion

    }
}
