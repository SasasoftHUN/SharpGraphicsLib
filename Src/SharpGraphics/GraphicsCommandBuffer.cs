using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics
{

    public enum CommandBufferLevel { Primary, Secondary }

    public abstract class GraphicsCommandBuffer : IDisposable
    {

        [Flags]
        public enum ResetOptions : uint
        {
            Nothing = 0u,
            ReleaseResources = 1u,
        }

        [Flags]
        public enum BeginOptions : uint
        {
            Nothing = 0u,
            OneTimeSubmit = 1u,
            SimultaneousMultiSubmit = 2u,
        }

        public readonly struct VertexBufferBinding
        {
            public readonly uint binding;
            public readonly IDataBuffer vertexBuffer;
            public readonly ulong offset;

            public VertexBufferBinding(uint binding, IDataBuffer vertexBuffer, ulong offset = 0ul)
            {
                this.binding = binding;
                this.vertexBuffer = vertexBuffer;
                this.offset = offset;
            }
        }

        public enum IndexType : uint
        {
            UnsignedShort = sizeof(ushort),
            UnsignedInt = sizeof(uint),
        }

        #region Fields

        private bool _isDisposed;

        private readonly GraphicsCommandProcessor _commandProcessor;

        #endregion

        #region Properties

        public bool IsDisposed => _isDisposed;

        #endregion

        #region Constructors

        protected GraphicsCommandBuffer(GraphicsCommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        /*~GraphicsCommandBuffer()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }*/

        #endregion

        #region Protected Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void BindResource(IPipeline pipeline, uint set, PipelineResource resource) => resource.BindResource(pipeline, set, this);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void BindResource(IPipeline pipeline, uint set, PipelineResource resource, int dataIndex) => resource.BindResource(pipeline, set, this, dataIndex);

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

        public abstract void Reset(ResetOptions options = ResetOptions.Nothing);
        public abstract void Begin(BeginOptions options = BeginOptions.OneTimeSubmit);
        public abstract void BeginAndContinue(GraphicsCommandBuffer commandBuffer, BeginOptions options = BeginOptions.OneTimeSubmit);
        public abstract void End();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StoreData(IDataBuffer buffer, IntPtr data, ulong size, ulong offset = 0ul)
            => buffer.StoreData(this, data, size, offset);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StoreData<T>(IDataBuffer<T> buffer, T data, uint elementIndexOffset = 0u) where T : unmanaged
            => buffer.StoreData(this, ref data, elementIndexOffset);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StoreData<T>(IDataBuffer<T> buffer, ref T data, uint elementIndexOffset = 0u) where T : unmanaged
            => buffer.StoreData(this, ref data, elementIndexOffset);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StoreData<T>(IDataBuffer<T> buffer, T[] data, uint elementIndexOffset = 0u) where T : unmanaged
            => buffer.StoreData(this, new ReadOnlySpan<T>(data), elementIndexOffset);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StoreData<T>(IDataBuffer<T> buffer, in ReadOnlySpan<T> data, uint elementIndexOffset = 0u) where T : unmanaged
            => buffer.StoreData(this, data, elementIndexOffset);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StoreData<T>(IDataBuffer<T> buffer, in ReadOnlyMemory<T> data, uint elementIndexOffset = 0u) where T : unmanaged
            => buffer.StoreData(this, data, elementIndexOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void CopyTo(IDataBuffer source, IDataBuffer destination, ulong size, ulong sourceOffset = 0ul, ulong destinationOffset = 0ul)
            => source.CopyTo(this, destination, size, sourceOffset, destinationOffset);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void CopyElementsTo<T>(IDataBuffer<T> source, IDataBuffer<T> destination, uint elementCount, uint sourceElementIndexOffset = 0u, ulong destinationElementIndexOffset = 0u) where T : unmanaged
            => source.CopyElementsTo(this, destination, elementCount, sourceElementIndexOffset, destinationElementIndexOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void CopyTo(IDataBuffer source, ITexture destination, in CopyBufferTextureRange range)
            => source.CopyTo(this, destination, range);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void GenerateTextureMipmaps(ITexture texture) => texture.GenerateMipmaps(this);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StoreTextureData<T>(ITexture2D texture, T[] data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => texture.StoreData(this, new ReadOnlySpan<T>(data), layout, mipLevels);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StoreTextureData<T>(ITexture2D texture, in ReadOnlySpan<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => texture.StoreData(this, data, layout, mipLevels);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StoreTextureData<T>(ITexture2D texture, in ReadOnlyMemory<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => texture.StoreData(this, data, layout, mipLevels);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StoreTextureDataAllFaces<T>(ITextureCube texture, T[] data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => texture.StoreDataAllFaces(this, new ReadOnlySpan<T>(data), layout, mipLevels);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StoreTextureDataAllFaces<T>(ITextureCube texture, in ReadOnlySpan<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => texture.StoreDataAllFaces(this, data, layout, mipLevels);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StoreTextureDataAllFaces<T>(ITextureCube texture, in ReadOnlyMemory<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged
            => texture.StoreDataAllFaces(this, data, layout, mipLevels);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StoreTextureData<T>(ITextureCube texture, T[] data, TextureLayout layout, CubeFace face, in TextureRange mipLevels) where T : unmanaged
            => texture.StoreData(this, new ReadOnlySpan<T>(data), layout, face, mipLevels);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StoreTextureData<T>(ITextureCube texture, in ReadOnlySpan<T> data, TextureLayout layout, CubeFace face, in TextureRange mipLevels) where T : unmanaged
            => texture.StoreData(this, data, layout, face, mipLevels);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void StoreTextureData<T>(ITextureCube texture, in ReadOnlyMemory<T> data, TextureLayout layout, CubeFace face, in TextureRange mipLevels) where T : unmanaged
            => texture.StoreData(this, data, layout, face, mipLevels);

        public abstract void BeginRenderPass(IRenderPass renderPass, IFrameBuffer frameBuffer, CommandBufferLevel executionLevel = CommandBufferLevel.Primary);
        public abstract void BeginRenderPass(IRenderPass renderPass, IFrameBuffer frameBuffer, Vector4 clearColor, CommandBufferLevel executionLevel = CommandBufferLevel.Primary);
        public abstract void BeginRenderPass(IRenderPass renderPass, IFrameBuffer frameBuffer, in ReadOnlySpan<Vector4> clearValues, CommandBufferLevel executionLevel = CommandBufferLevel.Primary);
        public abstract void NextRenderPassStep(CommandBufferLevel executionLevel = CommandBufferLevel.Primary);
        public abstract void NextRenderPassStep(PipelineResource inputAttachmentResource, CommandBufferLevel executionLevel = CommandBufferLevel.Primary);
        public abstract void EndRenderPass();

        public abstract void BindPipeline(IGraphicsPipeline pipeline);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SetViewportAndScissor(in Vector2UInt size)
        {
            SetViewport(size);
            SetScissor(size);
        }
        public abstract void SetViewport(in Vector2UInt size);
        public abstract void SetScissor(in Vector2UInt size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindVertexBuffer(IDataBuffer vertexBuffer) => BindVertexBuffer(0u, vertexBuffer);
        public abstract void BindVertexBuffer(uint binding, IDataBuffer vertexBuffer, ulong offset = 0ul);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindVertexBuffer(in VertexBufferBinding binding) => BindVertexBuffer(binding.binding, binding.vertexBuffer, binding.offset);
        public abstract void BindVertexBuffers(uint firstBinding, in ReadOnlySpan<IDataBuffer> vertexBuffers);
        public abstract void BindVertexBuffers(in ReadOnlySpan<VertexBufferBinding> bindings);
        public abstract void BindIndexBuffer(IDataBuffer indexBuffer, IndexType type, ulong offset = 0ul);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindIndexBuffer(IDataBuffer<ushort> indexBuffer, ulong offset = 0ul) => BindIndexBuffer(indexBuffer, IndexType.UnsignedShort, offset);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindIndexBuffer(IDataBuffer<uint> indexBuffer, ulong offset = 0ul) => BindIndexBuffer(indexBuffer, IndexType.UnsignedInt, offset);

        public abstract void BindResource(uint set, PipelineResource resource);
        public abstract void BindResource(uint set, PipelineResource resource, int dataIndex);

        public abstract void Draw(uint vertexCount);
        public abstract void Draw(uint vertexCount, uint firstVertex);
        //public abstract void DrawInstanced(uint vertexCount, uint instanceCount);
        //public abstract void DrawInstanced(uint vertexCount, uint firstVertex, uint instanceCount);
        //public abstract void DrawInstanced(uint vertexCount, uint firstVertex, uint instanceCount, uint firstInstance);

        public abstract void DrawIndexed(uint indexCount);
        public abstract void DrawIndexed(uint indexCount, uint firstIndex);
        //public abstract void DrawIndexedInstanced(uint indexCount, uint instanceCount);
        //public abstract void DrawIndexedInstanced(uint indexCount, uint firstIndex, uint instanceCount);
        //public abstract void DrawIndexedInstanced(uint indexCount, uint firstIndex, uint instanceCount, uint firstInstance);


        public abstract void ExecuteSecondaryCommandBuffers(in ReadOnlySpan<GraphicsCommandBuffer> commandBuffers);

        public virtual void Submit() => _commandProcessor.Submit(this);

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
