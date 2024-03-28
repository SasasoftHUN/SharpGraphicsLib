using SharpGraphics.GraphicsViews;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SharpGraphics
{

    public enum PresentMode
    {
        /// <summary>
        /// Immediate, no VSync
        /// </summary>
        Immediate,
        /// <summary>
        /// Single VSync
        /// </summary>
        VSyncDoubleBuffer,
        /// <summary>
        /// Single VSync when at Target FPS
        /// </summary>
        AdaptiveDoubleBuffer,
        /// <summary>
        /// Single VSync with swapping internal buffers for faster than display rendering
        /// </summary>
        VSyncTripleBuffer,
    }

    public readonly struct SwapChainConstruction : IEquatable<SwapChainConstruction>
    {
        
        public readonly PresentMode mode;
        public readonly DataFormat colorFormat;
        public readonly DataFormat depthStencilFormat;
        //TODO: public readonly ColorSpace colorSpace;?

        public SwapChainConstruction(in SwapChainConstruction construction)
        {
            this.mode = construction.mode;
            this.colorFormat = construction.colorFormat;
            this.depthStencilFormat = construction.depthStencilFormat;
        }
        public SwapChainConstruction(in SwapChainConstruction construction, PresentMode mode)
        {
            this.mode = mode;
            this.colorFormat = construction.colorFormat;
            this.depthStencilFormat = construction.depthStencilFormat;
        }
        public SwapChainConstruction(in SwapChainConstruction construction, DataFormat colorFormat)
        {
            this.mode = construction.mode;
            this.colorFormat = colorFormat;
            this.depthStencilFormat = construction.depthStencilFormat;
        }
        public SwapChainConstruction(PresentMode mode, DataFormat colorFormat, DataFormat depthStencilFormat)
        {
            this.mode = mode;
            this.colorFormat = colorFormat;
            this.depthStencilFormat = depthStencilFormat;
        }

        public override bool Equals(object? obj)
            => obj is SwapChainConstruction construction && Equals(construction);

        public bool Equals(SwapChainConstruction other)
            => mode == other.mode &&
               colorFormat == other.colorFormat &&
               depthStencilFormat == other.depthStencilFormat;

        public override int GetHashCode()
        {
            int hashCode = 1740595764;
            hashCode = hashCode * -1521134295 + mode.GetHashCode();
            hashCode = hashCode * -1521134295 + colorFormat.GetHashCode();
            hashCode = hashCode * -1521134295 + depthStencilFormat.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(SwapChainConstruction left, SwapChainConstruction right) => left.Equals(right);
        public static bool operator !=(SwapChainConstruction left, SwapChainConstruction right) => !(left == right);

    }

    public class SwapChainSizeChangedEventArgs : EventArgs
    {
        public Vector2UInt Size { get; private set; }
        public SwapChainSizeChangedEventArgs(Vector2UInt size) => Size = size;
    }

    public abstract class GraphicsSwapChain : IDisposable
    {

        internal class SwapChainInfoEventArgs : EventArgs
        {
            public uint FrameCount { get; }
            public SwapChainInfoEventArgs(uint frameCount) => FrameCount = frameCount;
        }

        #region Fields

        private bool _isDisposed;

        private SwapChainConstruction _format;
        private IRenderPass? _renderPass;

        protected readonly IGraphicsView _view;

        protected bool _needToRecrate = true;

        #endregion

        #region Properties

        protected internal abstract uint FrameCount { get; }
        public abstract uint CurrentFrameIndex { get; }

        public SwapChainConstruction Format
        {
            get => _format;
            protected set
            {
                if (value != _format)
                {
                    _format = value;
                    NeedToRecrate();
                }
            }
        }
        public bool? IsFallbackFormat { get; private set; } = default(bool?);

        public IRenderPass? RenderPass
        {
            get => _renderPass;
            set
            {
                if (value != _renderPass)
                {
                    _renderPass = value;
                    NeedToRecrate();
                }
            }
        }

        public abstract Vector2UInt Size { get; }

        #endregion

        #region Events

        internal event EventHandler<SwapChainInfoEventArgs>? FramesRecreated;
        internal event EventHandler<SwapChainInfoEventArgs>? NextFrame;

        public event EventHandler<SwapChainSizeChangedEventArgs>? SizeChanged;

        #endregion

        #region Constructors

        protected GraphicsSwapChain(IGraphicsView view)
        {
            _view = view;
            Format = view.SwapChainConstructionRequest;
        }

        // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~GraphicsSwapChain()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        #endregion

        #region Private Methods

        private void NeedToRecrate()
        {
            _needToRecrate = true;
            IsFallbackFormat = default(bool?);
        }

        #endregion

        #region Protected Methods

        protected void CheckForFormatFallback(in SwapChainConstruction finalConstruction)
        {
            if (finalConstruction != Format)
            {
                if ((_format.colorFormat != DataFormat.Undefined && _format.colorFormat != finalConstruction.colorFormat) ||
                    (_format.depthStencilFormat != DataFormat.Undefined && _format.depthStencilFormat != finalConstruction.depthStencilFormat))
                    IsFallbackFormat = true;
                _format = finalConstruction;
            }
            else IsFallbackFormat = false;
        }

        protected void OnFramesRecreated(uint frameCount) => FramesRecreated?.Invoke(this, new SwapChainInfoEventArgs(frameCount));
        protected void OnNextFrame(uint frameIndex) => NextFrame?.Invoke(this, new SwapChainInfoEventArgs(frameIndex));
        protected void OnSizeChanged(Vector2UInt size) => SizeChanged?.Invoke(this, new SwapChainSizeChangedEventArgs(size));

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                _isDisposed = true;
            }
        }

        protected bool CheckAndResetIfNeededToBeRecreated()
        {
            bool result = _needToRecrate;
            _needToRecrate = false;
            return result || !_view.IsViewInitialized;
        }

        #endregion

        #region Public Methods

        public abstract bool TryBeginFrame([NotNullWhen(returnValue: true)] out GraphicsCommandBuffer? commandBuffer, [NotNullWhen(returnValue: true)] out IFrameBuffer<ITexture2D>? frameBuffer); //TODO: Multiview? Stereo support?
        public abstract bool PresentFrame();

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
