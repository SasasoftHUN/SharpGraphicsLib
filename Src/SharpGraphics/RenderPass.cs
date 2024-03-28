using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics
{

    [Flags]
    public enum AttachmentType : uint
    {
        Undefined = 0u,
        Color = 1u,
        Depth = 2u,
        Stencil = 4u,
        DepthStencil = Depth | Stencil,
        ShaderInput = 8u,
        Present = 16u,
    }
    public enum AttachmentLoadOperation : uint { Undefined = 0, Clear = 1, Load = 2, }
    public enum AttachmentStoreOperation : uint { Undefined = 0, Store = 2, }

    //https://www.khronos.org/registry/vulkan/specs/1.3-extensions/man/html/VkAttachmentDescription.html
    public readonly struct RenderPassAttachment
    {

        public readonly AttachmentType type;
        public readonly DataFormat format;
        public readonly uint samples;

        public readonly AttachmentLoadOperation loadOperation; //loadOp and stencilLoadOp define the load operations that execute as part of the first subpass that uses the attachment.
        public readonly AttachmentStoreOperation storeOperation; //storeOp and stencilStoreOp define the store operations that execute as part of the last subpass that uses the attachment.
        public readonly AttachmentLoadOperation stencilLoadOperation;
        public readonly AttachmentStoreOperation stencilStoreOperation;

        public RenderPassAttachment(AttachmentType type, DataFormat format)
        {
            this.type = type;
            this.format = format;
            this.samples = 1u;
            this.loadOperation = AttachmentLoadOperation.Clear;
            this.storeOperation = AttachmentStoreOperation.Store;
            this.stencilLoadOperation = AttachmentLoadOperation.Undefined;
            this.stencilStoreOperation = AttachmentStoreOperation.Undefined;
        }
        public RenderPassAttachment(AttachmentType type, DataFormat format, AttachmentLoadOperation load, AttachmentStoreOperation store)
        {
            this.type = type;
            this.format = format;
            this.samples = 1u;
            this.loadOperation = load;
            this.storeOperation = store;
            this.stencilLoadOperation = load;
            this.stencilStoreOperation = store;
        }
        public RenderPassAttachment(AttachmentType type, DataFormat format, AttachmentLoadOperation loadDepth, AttachmentStoreOperation storeDepth, AttachmentLoadOperation loadStencil, AttachmentStoreOperation storeStencil)
        {
            this.type = type;
            this.format = format;
            this.samples = 1u;
            this.loadOperation = loadDepth;
            this.storeOperation = storeDepth;
            this.stencilLoadOperation = loadStencil;
            this.stencilStoreOperation = storeStencil;
        }
    }

    public readonly struct RenderPassStep
    {

        private readonly uint[]? _colorAttachmentIndices; //Null is valid for optimization (not allocating empty array)
        private readonly int _depthStencilAttachmentIndex;
        private readonly uint[]? _inputAttachmentIndices; //Null is valid for optimization (not allocating empty array)

        public ReadOnlySpan<uint> ColorAttachmentIndices => _colorAttachmentIndices;
        public int DepthStencilAttachmentIndex => _depthStencilAttachmentIndex;
        public bool HasDepthStencilAttachment => _depthStencilAttachmentIndex >= 0;
        public ReadOnlySpan<uint> InputAttachmentIndices => _inputAttachmentIndices;

        public RenderPassStep(uint colorAttachmentIndex)
        {
            this._colorAttachmentIndices = new uint[] { colorAttachmentIndex };
            this._depthStencilAttachmentIndex = -1;
            this._inputAttachmentIndices = null;
        }
        public RenderPassStep(in ReadOnlySpan<uint> colorAttachmentIndices)
        {
            this._colorAttachmentIndices = colorAttachmentIndices.Length > 0 ? colorAttachmentIndices.ToArray() : null; //Copy for safety
            this._depthStencilAttachmentIndex = -1;
            this._inputAttachmentIndices = null;
        }
        public RenderPassStep(uint colorAttachmentIndex, uint depthAttachmentIndex)
        {
            this._colorAttachmentIndices = new uint[] { colorAttachmentIndex };
            this._depthStencilAttachmentIndex = (int)depthAttachmentIndex;
            this._inputAttachmentIndices = null;
        }
        public RenderPassStep(in ReadOnlySpan<uint> colorAttachmentIndices, uint depthAttachmentIndex)
        {
            this._colorAttachmentIndices = colorAttachmentIndices.Length > 0 ? colorAttachmentIndices.ToArray() : null; //Copy for safety
            this._depthStencilAttachmentIndex = (int)depthAttachmentIndex;
            this._inputAttachmentIndices = null;
        }

        public RenderPassStep(uint colorAttachmentIndex, in ReadOnlySpan<uint> inputAttachmentIndices)
        {
            this._colorAttachmentIndices = new uint[] { colorAttachmentIndex };
            this._depthStencilAttachmentIndex = -1;
            this._inputAttachmentIndices = inputAttachmentIndices.Length > 0 ? inputAttachmentIndices.ToArray() : null; //Copy for safety
        }
        public RenderPassStep(in ReadOnlySpan<uint> colorAttachmentIndices, in ReadOnlySpan<uint> inputAttachmentIndices)
        {
            this._colorAttachmentIndices = colorAttachmentIndices.Length > 0 ? colorAttachmentIndices.ToArray() : null; //Copy for safety
            this._depthStencilAttachmentIndex = -1;
            this._inputAttachmentIndices = inputAttachmentIndices.Length > 0 ? inputAttachmentIndices.ToArray() : null; //Copy for safety
        }
        public RenderPassStep(uint colorAttachmentIndex, uint depthAttachmentIndex, in ReadOnlySpan<uint> inputAttachmentIndices)
        {
            this._colorAttachmentIndices = new uint[] { colorAttachmentIndex };
            this._depthStencilAttachmentIndex = (int)depthAttachmentIndex;
            this._inputAttachmentIndices = inputAttachmentIndices.Length > 0 ? inputAttachmentIndices.ToArray() : null; //Copy for safety
        }
        public RenderPassStep(in ReadOnlySpan<uint> colorAttachmentIndices, uint depthAttachmentIndex, in ReadOnlySpan<uint> inputAttachmentIndices)
        {
            this._colorAttachmentIndices = colorAttachmentIndices.Length > 0 ? colorAttachmentIndices.ToArray() : null; //Copy for safety
            this._depthStencilAttachmentIndex = (int)depthAttachmentIndex;
            this._inputAttachmentIndices = inputAttachmentIndices.Length > 0 ? inputAttachmentIndices.ToArray() : null; //Copy for safety
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUsingAttachment(uint index)
            => IsUsingColorAttachment(index) || IsUsingDepthStencilAttachment(index) || IsUsingInputAttachment(index);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsWritingAttachment(uint index)
            => IsUsingColorAttachment(index) || IsUsingDepthStencilAttachment(index);
        public bool IsUsingColorAttachment(uint index)
        {
            if (_colorAttachmentIndices != null)
            {
                foreach (uint colorAttachmentIndex in _colorAttachmentIndices)
                    if (colorAttachmentIndex == index)
                        return true;
            }
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUsingDepthStencilAttachment(uint index) => _depthStencilAttachmentIndex == index;
        public bool IsUsingInputAttachment(uint index)
        {
            if (_inputAttachmentIndices != null)
            {
                foreach (uint inputAttachmentIndex in _inputAttachmentIndices)
                    if (inputAttachmentIndex == index)
                        return true;
            }
            return false;
        }
        public bool IsUsing(in RenderPassStep step)
        {
            if (_inputAttachmentIndices != null)
                foreach (uint index in _inputAttachmentIndices)
                    if (step.IsWritingAttachment(index))
                        return true;
            return false;
        }
    }

    public abstract class RenderPass : IRenderPass
    {

        #region Fields

        private bool _isDisposed;

        protected readonly RenderPassAttachment[] _attachments;
        protected readonly RenderPassStep[] _steps;

        #endregion

        #region Properties

        public bool IsDisposed => _isDisposed;

        public ReadOnlySpan<RenderPassAttachment> Attachments => _attachments;
        public ReadOnlySpan<RenderPassStep> Steps => _steps;

        #endregion

        #region Constructors

        protected RenderPass(in ReadOnlySpan<RenderPassAttachment> attachments, in ReadOnlySpan<RenderPassStep> steps)
        {
            if (attachments.Length == 0)
                throw new ArgumentOutOfRangeException("attachments.Length");
            _attachments = attachments.ToArray(); //Copy for safety

            if (steps.Length == 0)
                throw new ArgumentOutOfRangeException("steps.Length");
            _steps = steps.ToArray(); //Copy for safety

            //TODO: Check steps' indices exists in attachments
            //TODO: Check attachments used in steps have correct AttachmentType for their roles
            //Automatic dependency determined in implementation subclasses (VulkanRenderPass only, OpenGL doesn't care)
        }

        // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RenderPass()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        #endregion

        #region Protected Methods

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

        #endregion

        #region Public Methods

        public abstract IFrameBuffer<ITexture2D> CreateFrameBuffer2D(in Vector2UInt resolution);
        public abstract IFrameBuffer<ITexture2D> CreateFrameBuffer2D(in ReadOnlySpan<ITexture2D> textures);

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
