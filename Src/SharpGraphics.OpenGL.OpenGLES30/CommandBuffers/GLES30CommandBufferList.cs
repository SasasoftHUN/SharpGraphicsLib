using SharpGraphics.OpenGL.Commands;
using SharpGraphics.OpenGL.CommandBuffers;
using SharpGraphics.OpenGL.OpenGLES30.Commands;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using OpenTK.Graphics.ES30;
using System.Runtime.CompilerServices;
using System.Linq;

namespace SharpGraphics.OpenGL.OpenGLES30.CommandBuffers
{

    internal sealed class GLES30CommandBufferList : GLCommandBufferList
    {

        private readonly struct BoundVertexBuffer
        {
            public readonly uint binding;
            public readonly IGLES30VertexDataBuffer vertexBuffer;
            public readonly int offset;

            public BoundVertexBuffer(uint binding, IGLES30VertexDataBuffer vertexBuffer, int offset)
            {
                this.binding = binding;
                this.vertexBuffer = vertexBuffer;
                this.offset = offset;
            }
        }

        #region Fields

        private readonly GLES30CommandProcessor _gles30CommandProcessor;

        private GLES30GraphicsPipeline? _activeES30GraphicsPipeline;
#if ANDROID
        private BeginMode _primitiveType;
#else
        private PrimitiveType _primitiveType;
#endif
        private DrawElementsType _indexBufferElementType;

        private bool _boundVertexBuffersChanged;
        private IDataBuffer? _indexBufferToBind;
        private int _indexBufferToBindElementSize;
        private ulong _indexBufferToBindOffset;
        private SortedDictionary<uint, BoundVertexBuffer> _boundVertexBuffers = new SortedDictionary<uint, BoundVertexBuffer>(); //TODO: Optimize with array + no foreach

        #endregion

        #region Constructors

        internal GLES30CommandBufferList(GLES30CommandProcessor commandProcessor): base(commandProcessor)
            => _gles30CommandProcessor = commandProcessor;

        #endregion

        #region Private Methods

#if NETUNIFIED
        [SkipLocalsInit]
#endif
        private void BindVertexBuffers()
        {
            if (_boundVertexBuffers.Count == 1)
            {
                BoundVertexBuffer boundVertexBuffer = _boundVertexBuffers.Values.First();
                _commandList.Add(new GLES30BindVertexBufferCommand(boundVertexBuffer.vertexBuffer, _activeES30GraphicsPipeline!.VertexInputs!.Value, boundVertexBuffer.binding, boundVertexBuffer.offset));
            }
            else
            {
                Span<GLES30VertexBufferBinding> vertexBufferBindings = stackalloc GLES30VertexBufferBinding[_boundVertexBuffers.Count];
                int i = 0;
                foreach (BoundVertexBuffer boundVertexBuffer in _boundVertexBuffers.Values)
                    vertexBufferBindings[i++] = new GLES30VertexBufferBinding(boundVertexBuffer.binding, boundVertexBuffer.vertexBuffer.ID, boundVertexBuffer.offset);
                _commandList.Add(new GLES30BindVertexBuffersCommand(_boundVertexBuffers.Values.First().vertexBuffer, _activeES30GraphicsPipeline!.VertexInputs!.Value, vertexBufferBindings, _memoryAllocator));
            }
            _boundVertexBuffersChanged = false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BindIndexBuffer()
        {
            AddBindIndexBufferCommand(_indexBufferToBind!, _indexBufferToBindElementSize, _indexBufferToBindOffset);
            _indexBufferToBind = null;
            _indexBufferToBindElementSize = 0;
            _indexBufferToBindOffset = 0ul;
        }

        #endregion

        #region Protected Methods

        protected override void AddClearCommand(in Vector2UInt size, in ReadOnlySpan<GLClearColor> clearColors, float? clearDepth, int? clearStencil)
            => _commandList.Add(new GLES30ClearCommand(size, clearColors, clearDepth, clearStencil, _memoryAllocator));

        protected override void AddBindDefaultFrameBufferCommand() => new GLES30BindFrameBufferObjectCommand(0);

        protected override void RestorePipelineBindState()
        {
            if (_activeES30GraphicsPipeline != null)
            {
                base.RestorePipelineBindState();
                _boundVertexBuffersChanged = false;
                _boundVertexBuffers.Clear();
                _commandList.Add(new GLES30RestoreDefaultPipelineStateCommand(_pipelineIsScissorChanged));
                _activeES30GraphicsPipeline = null;
            }
        }

        protected override IGLCommand GetUnBindIndexBufferCommand() => new GLES30UnBindBufferCommand(BufferTarget.ElementArrayBuffer);

        protected override void Flush() => GL.Flush();

        #endregion

        #region Public Methods

        public override void BindPipeline(IGraphicsPipeline pipeline)
        {
            if (_activeFrameBuffer != null)
            {
                RestorePipelineBindState();
                _activeES30GraphicsPipeline = Unsafe.As<GLES30GraphicsPipeline>(pipeline);
                _primitiveType = _activeES30GraphicsPipeline.GeometryType;
                _activeGLGraphicsPipeline = _activeES30GraphicsPipeline;
                _commandList.Add(new GLBindGraphicsPipelineCommand(_activeGLGraphicsPipeline));
                if (!_activeES30GraphicsPipeline.HasVertexInputs)
                    _commandList.Add(new GLES30BindVertexArrayCommand(_gles30CommandProcessor.GLES30Device.EmptyVAOID));
                _commandList.Add(new GLES30ViewportScissorCommand(new RectInt(Vector2Int.Zero, (Vector2Int)_activeFrameBuffer.Resolution), false));
            }
            else throw new Exception("GLCommandBuffer is not in a RenderPass, cannot use BindPipeline");
        }

        public override void BindVertexBuffer(uint binding, IDataBuffer vertexBuffer, ulong offset = 0ul)
        {
            if (_activeES30GraphicsPipeline != null && _activeES30GraphicsPipeline.HasVertexInputs)
            {
                if (vertexBuffer is IGLES30VertexDataBuffer gles30VertexBuffer) //Using pattern matching istead of hardcast because passing a GLES30 buffer that is not a vertexBuffer is a "valid error"
                {
                    _boundVertexBuffersChanged = true;
                    _boundVertexBuffers[binding] = new BoundVertexBuffer(binding, gles30VertexBuffer, (int)offset);
                }
                else throw new ArgumentException("DataBuffer is not a VertexDataBuffer, cannot be used to BindVertexBuffer", "vertexBuffer");
            }
            else throw new Exception("GLCommandBuffer is not using a Pipeline with VertexInputs, cannot use BindVertexBuffer");
        }
        public override void BindVertexBuffers(uint firstBinding, in ReadOnlySpan<IDataBuffer> vertexBuffers)
        {
            if (_activeES30GraphicsPipeline != null && _activeES30GraphicsPipeline.HasVertexInputs)
            {
                for (int i = 0; i < vertexBuffers.Length; i++)
                    if (vertexBuffers[i] is IGLES30VertexDataBuffer gles30VertexBuffer) //Using pattern matching istead of hardcast because passing a GLES30 buffer that is not a vertexBuffer is a "valid error"
                    {
                        uint binding = firstBinding + (uint)i;
                        _boundVertexBuffersChanged = true;
                        _boundVertexBuffers[binding] = new BoundVertexBuffer(binding, gles30VertexBuffer, 0);
                    }
                    else throw new ArgumentException("DataBuffer is not a VertexDataBuffer, cannot be used to BindVertexBuffer", "vertexBuffer");
            }
            else throw new Exception("GLCommandBuffer is not using a Pipeline with VertexInputs, cannot use BindVertexBuffer");
        }
        public override void BindVertexBuffers(in ReadOnlySpan<VertexBufferBinding> bindings)
        {
            if (_activeES30GraphicsPipeline != null && _activeES30GraphicsPipeline.HasVertexInputs)
            {
                for (int i = 0; i < bindings.Length; i++)
                    if (bindings[i].vertexBuffer is IGLES30VertexDataBuffer gles30VertexBuffer) //Using pattern matching istead of hardcast because passing a GLES30 buffer that is not a vertexBuffer is a "valid error"
                    {
                        _boundVertexBuffersChanged = true;
                        _boundVertexBuffers[bindings[i].binding] = new BoundVertexBuffer(bindings[i].binding, gles30VertexBuffer, (int)bindings[i].offset);
                    }
                    else throw new ArgumentException("DataBuffer is not a VertexDataBuffer, cannot be used to BindVertexBuffer", "vertexBuffer");
            }
            else throw new Exception("GLCommandBuffer is not using a Pipeline with VertexInputs, cannot use BindVertexBuffer");
        }
        public override void BindIndexBuffer(IDataBuffer indexBuffer, IndexType type, ulong offset = 0ul)
        {
            //base.BindIndexBuffer(indexBuffer, type, offset); //Bind before draw after VAO bind
            _indexBufferToBind = indexBuffer;
            _indexBufferElementType = type.ToDrawElementsType();
            _indexBufferToBindElementSize = (int)type;
            _indexBufferToBindOffset = offset;
        }

        public override void EndRenderPass()
        {
            base.EndRenderPass();
            _activeGLGraphicsPipeline = null;
        }

        public override void SetViewport(in Vector2UInt size)
        {
            if (_activeFrameBuffer != null)
            {
                _commandList.Add(new GLES30ViewportCommand(new RectInt(Vector2Int.Zero, (Vector2Int)size)));
                if (!_pipelineIsViewportChanged)
                {
                    _pipelineBindStateRestoreList.Add(new GLES30ViewportCommand(new RectInt(Vector2Int.Zero, (Vector2Int)_activeFrameBuffer.Resolution)));
                    _pipelineIsViewportChanged = true;
                }
            }
            else throw new Exception("GLCommandBuffer is not using a Pipeline, cannot use SetViewport");
        }
        public override void SetScissor(in Vector2UInt size)
        {
            if (_activeFrameBuffer != null)
            {
                _commandList.Add(new GLES30ScissorCommand(new RectInt(Vector2Int.Zero, (Vector2Int)size), !_pipelineIsScissorChanged));
                if (!_pipelineIsScissorChanged)
                {
                    _pipelineBindStateRestoreList.Add(new GLES30ScissorCommand(new RectInt(Vector2Int.Zero, (Vector2Int)_activeFrameBuffer.Resolution), false));
                    _pipelineIsScissorChanged = true;
                }
            }
            else throw new Exception("GLCommandBuffer is not using a Pipeline, cannot use SetViewport");
        }

        public override void Draw(uint vertexCount)
        {
            if (_boundVertexBuffersChanged)
                BindVertexBuffers();
            _commandList.Add(new GLES30DrawCommand(_primitiveType, 0, (int)vertexCount));
        }
        public override void Draw(uint vertexCount, uint firstVertex)
        {
            if (_boundVertexBuffersChanged)
                BindVertexBuffers();
            _commandList.Add(new GLES30DrawCommand(_primitiveType, (int)firstVertex, (int)vertexCount));
        }
        public override void DrawIndexed(uint indexCount)
        {
            if (_boundVertexBuffersChanged)
                BindVertexBuffers();

            if (_indexBufferToBind != null)
                BindIndexBuffer();

            _commandList.Add(new GLES30DrawIndexedCommand(_primitiveType, (int)indexCount, _indexBufferElementType, IntPtr.Zero));
        }
        public override void DrawIndexed(uint indexCount, uint firstIndex)
        {
            if (_boundVertexBuffersChanged)
                BindVertexBuffers();

            if (_indexBufferToBind != null)
                BindIndexBuffer();

            _commandList.Add(new GLES30DrawIndexedCommand(_primitiveType, (int)indexCount, _indexBufferElementType, (IntPtr)firstIndex));
        }

        #endregion

    }
}
