using SharpGraphics.OpenGL.Commands;
using SharpGraphics.OpenGL.CommandBuffers;
using SharpGraphics.OpenGL.OpenGLCore.Commands;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Linq;

namespace SharpGraphics.OpenGL.OpenGLCore.CommandBuffers
{
    internal sealed class GLCoreCommandBufferList : GLCommandBufferList
    {

        #region Fields

        private GLCoreGraphicsPipeline? _activeCoreGraphicsPipeline;
        private PrimitiveType _geometryType;
        private DrawElementsType _indexBufferElementType;

        //private SortedSet<uint> _pipelineBindRestoreVertexBufferBindings = new SortedSet<uint>();

        #endregion

        #region Constructors

        internal GLCoreCommandBufferList(GLCommandProcessor commandProcessor): base(commandProcessor) { }

        #endregion

        #region Protected Methods

        protected override void AddClearCommand(in Vector2UInt size, in ReadOnlySpan<GLClearColor> clearColors, float? clearDepth, int? clearStencil)
            => _commandList.Add(new GLCoreClearCommand(size, clearColors, clearDepth, clearStencil, _memoryAllocator));

        protected override void AddBindDefaultFrameBufferCommand() => _commandList.Add(new GLCoreBindFrameBufferObjectCommand(0));
        protected override void RestorePipelineBindState()
        {
            if (_activeCoreGraphicsPipeline != null)
            {
                //UnBind Vertex Arrays
                /*if (_pipelineBindRestoreVertexBufferBindings.Count > 0)
                {
                    if (_pipelineBindRestoreVertexBufferBindings.Count == 1)
                        _pipelineBindStateRestoreList.Add(new GLCoreUnBindVertexBufferCommand(_pipelineBindRestoreVertexBufferBindings.First()));
                    else
                    {
                        Span<int> bindings = stackalloc int[_pipelineBindRestoreVertexBufferBindings.Count];
                        int i = 0;
                        foreach (int binding in _pipelineBindRestoreVertexBufferBindings)
                            bindings[i++] = binding;
                        _pipelineBindStateRestoreList.Add(new GLCoreUnBindVertexBuffersCommand(bindings, _memoryAllocator));
                    }
                }*/

                base.RestorePipelineBindState();

                _commandList.Add(new GLCoreRestoreDefaultPipelineStateCommand(_activeCoreGraphicsPipeline, _pipelineIsScissorChanged));
                _activeCoreGraphicsPipeline = null;
            }
            //_pipelineBindRestoreVertexBufferBindings.Clear();
        }

        protected override IGLCommand GetUnBindIndexBufferCommand() => new GLCoreUnBindBufferCommand(BufferTarget.ElementArrayBuffer);

        protected override void Flush() => GL.Flush();

        #endregion

        #region Public Methods

        public override void BindPipeline(IGraphicsPipeline pipeline)
        {
            if (_activeFrameBuffer != null)
            {
                RestorePipelineBindState();
                _activeCoreGraphicsPipeline = Unsafe.As<GLCoreGraphicsPipeline>(pipeline);
                _geometryType = _activeCoreGraphicsPipeline.GeometryType;
                _activeGLGraphicsPipeline = _activeCoreGraphicsPipeline;
                _commandList.Add(new GLBindGraphicsPipelineCommand(_activeGLGraphicsPipeline));
                _commandList.Add(new GLCoreViewportScissorCommand(new RectInt(Vector2Int.Zero, (Vector2Int)_activeFrameBuffer.Resolution), false));
            }
            else throw new Exception("GLCommandBuffer is not in a RenderPass, cannot use BindPipeline");
        }

        public override void BindVertexBuffer(uint binding, IDataBuffer vertexBuffer, ulong offset = 0ul)
        {
            if (_activeCoreGraphicsPipeline != null)
            {
                _commandList.Add(new GLCoreBindVertexBufferCommand(binding, Unsafe.As<IGLDataBuffer>(vertexBuffer).ID, (IntPtr)offset, _activeCoreGraphicsPipeline.GetVertexArrayStride(binding)));
                //_pipelineBindRestoreVertexBufferBindings.Add(binding);
            }
            else throw new Exception("GLCommandBuffer is not using a Pipeline, cannot use BindVertexBuffer");
        }
        public override void BindVertexBuffers(uint firstBinding, in ReadOnlySpan<IDataBuffer> vertexBuffers)
        {
            //TODO: Use "multi-bind"
            for (int i = 0; i < vertexBuffers.Length; i++)
                BindVertexBuffer(firstBinding + (uint)i, vertexBuffers[i]);
        }
        public override void BindVertexBuffers(in ReadOnlySpan<VertexBufferBinding> bindings)
        {
            //TODO: Use "multi-bind"
            for (int i = 0; i <bindings.Length; i++)
                BindVertexBuffer(bindings[i].binding, bindings[i].vertexBuffer, bindings[i].offset);
        }
        public override void BindIndexBuffer(IDataBuffer indexBuffer, IndexType type, ulong offset = 0ul)
        {
            base.BindIndexBuffer(indexBuffer, type, offset);
            _indexBufferElementType = type.ToDrawElementsType();
        }

        public override void SetViewport(in Vector2UInt size)
        {
            if (_activeFrameBuffer != null)
            {
                _commandList.Add(new GLCoreViewportCommand(new RectInt(Vector2Int.Zero, (Vector2Int)size)));
                if (!_pipelineIsViewportChanged)
                {
                    _pipelineBindStateRestoreList.Add(new GLCoreViewportCommand(new RectInt(Vector2Int.Zero, (Vector2Int)_activeFrameBuffer.Resolution)));
                    _pipelineIsViewportChanged = true;
                }
            }
            else throw new Exception("GLCommandBuffer is not using a Pipeline, cannot use SetViewport");
        }
        public override void SetScissor(in Vector2UInt size)
        {
            if (_activeFrameBuffer != null)
            {
                _commandList.Add(new GLCoreScissorCommand(new RectInt(Vector2Int.Zero, (Vector2Int)size), !_pipelineIsScissorChanged));
                if (!_pipelineIsScissorChanged)
                {
                    _pipelineBindStateRestoreList.Add(new GLCoreScissorCommand(new RectInt(Vector2Int.Zero, (Vector2Int)_activeFrameBuffer.Resolution), false));
                    _pipelineIsScissorChanged = true;
                }
            }
            else throw new Exception("GLCommandBuffer is not using a Pipeline, cannot use SetViewport");
        }

        public override void Draw(uint vertexCount) => _commandList.Add(new GLCoreDrawCommand(_geometryType, 0, (int)vertexCount));
        public override void Draw(uint vertexCount, uint firstVertex) => _commandList.Add(new GLCoreDrawCommand(_geometryType, (int)firstVertex, (int)vertexCount));
        public override void DrawIndexed(uint indexCount) => _commandList.Add(new GLCoreDrawIndexedCommand(_geometryType, (int)indexCount, _indexBufferElementType, _boundIndexBufferOffset));
        public override void DrawIndexed(uint indexCount, uint firstIndex) => _commandList.Add(new GLCoreDrawIndexedCommand(_geometryType, (int)indexCount, _indexBufferElementType, (int)firstIndex * _boundIndexBufferElementSize + _boundIndexBufferOffset));

        #endregion

    }
}
