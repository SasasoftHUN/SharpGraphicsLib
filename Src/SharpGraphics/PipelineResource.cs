using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics
{
    public abstract class PipelineResource : IDisposable
    {

        public readonly struct BufferBinding
        {
            public readonly uint binding;
            public readonly IDataBuffer buffer;

            public BufferBinding(uint binding, IDataBuffer buffer)
            {
                this.binding = binding;
                this.buffer = buffer;
            }
        }

        #region Fields

        private bool _isDisposed;

        #endregion

        #region Constructors

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~PipelineResource()
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
                    // dispose managed state (managed objects)
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Internal Methods

        protected internal abstract void BindResource(IPipeline pipeline, uint set, GraphicsCommandBuffer commandBuffer);
        protected internal abstract void BindResource(IPipeline pipeline, uint set, GraphicsCommandBuffer commandBuffer, int index);

        #endregion

        #region Public Methods

        public void BindUniformBuffer<R>(uint binding, FrameResource<R> buffer) where R : IDataBuffer
            => BindUniformBuffer(binding, buffer.Resource);
        public void BindUniformBufferDynamic<R>(uint binding, FrameResource<R> buffer, uint elementOffset) where R : IDataBuffer
            => BindUniformBufferDynamic(binding, buffer.Resource, elementOffset);

        //TODO: Refactor...
        public void BindUniformBufferDynamic<T>(uint binding, FrameResource<IDataBuffer<T>> buffer) where T : unmanaged
            => BindUniformBufferDynamic(binding, buffer.Resource, buffer.Resource.ElementOffset);
        public void BindUniformBufferDynamic<T>(uint binding, FrameResource<IMappableDataBuffer<T>> buffer) where T : unmanaged
            => BindUniformBufferDynamic(binding, buffer.Resource, buffer.Resource.ElementOffset);
        public void BindUniformBufferDynamic<T>(uint binding, FrameResource<IDeviceOnlyDataBuffer<T>> buffer) where T : unmanaged
            => BindUniformBufferDynamic(binding, buffer.Resource, buffer.Resource.ElementOffset);


        public abstract void BindUniformBuffer(uint binding, IDataBuffer buffer);
        public abstract void BindUniformBufferDynamic(uint binding, IDataBuffer buffer, uint elementOffset);
        public void BindUniformBufferDynamic<T>(uint binding, IDataBuffer<T> buffer) where T : unmanaged
            => BindUniformBufferDynamic(binding, buffer, buffer.ElementOffset);
        //TODO: public abstract void BindBuffer(in ReadOnlySpan<BufferBinding> bindings);

        public abstract void BindTexture(uint binding, TextureSampler sampler, ITexture texture);

        public abstract void BindInputAttachments(uint binding, ITexture attachment);
        //TODO: public abstract void BindInputAttachments(in ReadOnlySpan<InputAttachmentBinding> bindings);
        public abstract void BindInputAttachments(in RenderPassStep step, IFrameBuffer frameBuffer);

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
