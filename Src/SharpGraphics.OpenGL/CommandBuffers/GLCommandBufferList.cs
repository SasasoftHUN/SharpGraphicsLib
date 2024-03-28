using SharpGraphics.Allocator;
using SharpGraphics.OpenGL.Commands;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.OpenGL.CommandBuffers
{

    public struct GLClearColor
    {

        public readonly static int sizeOfStruct;

        public readonly int buffer;
        public Vector4 color;

        static GLClearColor() => sizeOfStruct = Marshal.SizeOf<GLClearColor>();

        public GLClearColor(int buffer, in Vector4 color)
        {
            this.buffer = buffer;
            this.color = color;
        }
    }

    public abstract class GLCommandBufferList : GraphicsCommandBuffer, IGLCommandBuffer
    {

        #region Fields

        private bool _isDisposed;

        private Vector4[]? _activeClearColors;
        private int _activeClearColorCount = 0;
        private bool _restoreRenderPassOnEnd = true;

        protected readonly GLGraphicsDevice _glDevice;
        protected readonly GLCommandProcessor _glCommandProcessor;
        protected IMemoryAllocator _memoryAllocator = new BankMemoryAllocator();
        protected GLGraphicsPipeline? _activeGLGraphicsPipeline;
        protected IRenderPass? _activeRenderPass;
        protected int _activeRenderPassStep = -1;
        protected IGLFrameBuffer? _activeFrameBuffer;

        protected List<IGLCommand> _dataTransferCommandList = new List<IGLCommand>();
        protected List<IGLCommand> _commandList = new List<IGLCommand>();
        protected List<IGLCommand> _renderPassStateRestoreList = new List<IGLCommand>();
        protected List<IGLCommand> _pipelineBindStateRestoreList = new List<IGLCommand>();
        protected Dictionary<uint, List<IGLCommand>> _pipelineBindResourceStateRestoreList = new Dictionary<uint, List<IGLCommand>>();
        protected bool _pipelineIsViewportChanged = false;
        protected bool _pipelineIsScissorChanged = false;

        protected int _boundIndexBufferElementSize = 0;
        protected int _boundIndexBufferOffset = 0;

        #endregion

        #region Properties

        public IMemoryAllocator MemoryAllocator => _memoryAllocator;

        #endregion

        #region Constructors

        protected internal GLCommandBufferList(GLCommandProcessor commandProcessor): base(commandProcessor)
        {
            _glDevice = commandProcessor.Device;
            _glCommandProcessor = commandProcessor;
        }
        ~GLCommandBufferList() => Dispose(false);

        #endregion

        #region Private Methods

#if NETUNIFIED
        [SkipLocalsInit]
#endif
        private void AddClearCommand(in Vector2UInt resolution, in ReadOnlySpan<Vector4> clearColors, in RenderPassStep step, in ReadOnlySpan<RenderPassAttachment> attachments)
        {
            ReadOnlySpan<uint> colorAttachmentIndices = step.ColorAttachmentIndices;
            Span<GLClearColor> colors = stackalloc GLClearColor[colorAttachmentIndices.Length];
            int clearColorCount = 0;
            float? clearDepth = default(float?);
            int? clearStencil = default(int?);

            if (colors.Length > 0)
                for (int i = 0; i < colors.Length; i++)
                {
                    int attachmentIndex = (int)colorAttachmentIndices[i];
                    if (attachments[attachmentIndex].loadOperation == AttachmentLoadOperation.Clear)
                        colors[clearColorCount++] = new GLClearColor(attachmentIndex, clearColors[attachmentIndex]);
                }

            if (step.DepthStencilAttachmentIndex != -1)
            {
                if (attachments[step.DepthStencilAttachmentIndex].loadOperation == AttachmentLoadOperation.Clear)
                    clearDepth = clearColors[step.DepthStencilAttachmentIndex].X;
                if (attachments[step.DepthStencilAttachmentIndex].stencilLoadOperation == AttachmentLoadOperation.Clear)
                    clearStencil = (int)clearColors[step.DepthStencilAttachmentIndex].Y;
            }

            if (clearColorCount > 0 || clearDepth.HasValue || clearStencil.HasValue)
                AddClearCommand(resolution, colors, clearDepth, clearStencil);
        }

        #endregion

        #region Protected Methods

        protected abstract void AddClearCommand(in Vector2UInt size, in ReadOnlySpan<GLClearColor> clearColors, float? clearDepth, int? clearStencil);

        protected abstract void AddBindDefaultFrameBufferCommand();

        protected virtual void AddBindIndexBufferCommand(IDataBuffer indexBuffer, int elementSize, ulong offset)
        {
            Debug.Assert(elementSize != 0, "GLCommandBuffer internal error, binding IndexBuffer with 0 element size.");

            _commandList.Add(new GLBindIndexBufferCommand(Unsafe.As<IGLDataBuffer>(indexBuffer)));
            _boundIndexBufferOffset = (int)offset;

            if (_boundIndexBufferElementSize == 0)
            {
                _pipelineBindStateRestoreList.Add(GetUnBindIndexBufferCommand());
                _boundIndexBufferElementSize = elementSize;
            }
        }
        protected abstract IGLCommand GetUnBindIndexBufferCommand();

        protected abstract void Flush();

        protected void RestoreAllState()
        {
            RestoreRenderPassState();
        }
        protected void RestoreRenderPassState()
        {
            RestorePipelineBindState();
            if (_renderPassStateRestoreList.Count > 0)
            {
                if (_restoreRenderPassOnEnd)
                    _commandList.AddRange(_renderPassStateRestoreList);
                _renderPassStateRestoreList.Clear();
            }
        }
        protected virtual void RestorePipelineBindState()
        {
            RestoreAllPipelineResourceState();
            if (_pipelineBindStateRestoreList.Count > 0)
            {
                _commandList.AddRange(_pipelineBindStateRestoreList);
                _pipelineBindStateRestoreList.Clear();
                _pipelineIsViewportChanged = false;
                _pipelineIsScissorChanged = false;
                _boundIndexBufferElementSize = 0;
                _boundIndexBufferOffset = 0;
            }
        }
        protected void RestoreAllPipelineResourceState()
        {
            foreach (List<IGLCommand> commands in _pipelineBindResourceStateRestoreList.Values)
                if (commands.Count > 0)
                {
                    _commandList.AddRange(commands);
                    commands.Clear();
                }
        }
        protected void RestorePipelineResourceState(uint set)
        {
            if (_pipelineBindResourceStateRestoreList.TryGetValue(set, out List<IGLCommand>? commands) && commands.Count > 0)
            {
                _commandList.AddRange(commands);
                commands.Clear();
            }
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
                    Debug.WriteLine($"Disposing OpenGL CommandBuffer from {(disposing ? "Dispose()" : "Finalizer")}...");

                _memoryAllocator.Dispose();
                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public override void Reset(ResetOptions options = ResetOptions.Nothing)
        {
            _dataTransferCommandList.Clear();
            _commandList.Clear();
            _memoryAllocator.ReleaseAll(); //TODO: Recreate if ResetOptions.ReleaseResources?
            _renderPassStateRestoreList.Clear();
            _restoreRenderPassOnEnd = true;
            _pipelineBindStateRestoreList.Clear();
        }
        public override void Begin(BeginOptions options = BeginOptions.OneTimeSubmit)
        {
            Debug.Assert(!_dataTransferCommandList.Any() || !_commandList.Any(), "GLCommandBuffer has not been Reset before Begin!");
        }
        public override void BeginAndContinue(GraphicsCommandBuffer commandBuffer, BeginOptions options = BeginOptions.OneTimeSubmit)
        {
            Begin(options);
            GLCommandBufferList glCommandBuffer = Unsafe.As<GLCommandBufferList>(commandBuffer);
            if (glCommandBuffer._activeRenderPass == null || glCommandBuffer._activeRenderPassStep < 0 || glCommandBuffer._activeFrameBuffer == null)
                throw new ArgumentException("Primary CommandBuffer has no active RenderPass when starting the SecondaryCommandBuffer", "commandBuffer");

            _activeRenderPass = glCommandBuffer._activeRenderPass;
            _activeFrameBuffer = glCommandBuffer._activeFrameBuffer;
            _activeRenderPassStep = glCommandBuffer._activeRenderPassStep;
            _restoreRenderPassOnEnd = false;
        }

        public void UseMemoryAllocator<T>() where T : IMemoryAllocator, new()
        {
            _memoryAllocator.Dispose();
            _memoryAllocator = new T();
        }

        public void AddDataTransferCommand(IGLCommand command) => _dataTransferCommandList.Add(command);
        public void AddCommand(IGLCommand command) => _commandList.Add(command);
        public void AddRenderPassStateRestoreCommand(IGLCommand command) => _renderPassStateRestoreList.Add(command);
        public void AddResourceStateRestoreCommand(uint set, IGLCommand command)
        {
            if (_pipelineBindResourceStateRestoreList.TryGetValue(set, out List<IGLCommand>? commands))
                commands.Add(command);
            else _pipelineBindResourceStateRestoreList[set] = new List<IGLCommand>() { command };
        }
        public void AddBindStateRestoreCommand(IGLCommand command) => _pipelineBindStateRestoreList.Add(command);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void StoreData<T>(IDataBuffer<T> buffer, T[] data, uint elementIndexOffset = 0u)
            => buffer.StoreData(this, new ReadOnlyMemory<T>(data), elementIndexOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void StoreTextureData<T>(ITexture2D texture, T[] data, TextureLayout layout, in TextureRange mipLevels)
            => texture.StoreData(this, new ReadOnlyMemory<T>(data), layout, mipLevels);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void StoreTextureDataAllFaces<T>(ITextureCube texture, T[] data, TextureLayout layout, in TextureRange mipLevels)
            => texture.StoreDataAllFaces(this, new ReadOnlyMemory<T>(data), layout, mipLevels);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void StoreTextureData<T>(ITextureCube texture, T[] data, TextureLayout layout, CubeFace face, in TextureRange mipLevels)
            => texture.StoreData(this, new ReadOnlyMemory<T>(data), layout, face, mipLevels);


        public override void BeginRenderPass(IRenderPass renderPass, IFrameBuffer frameBuffer, CommandBufferLevel executionLevel = CommandBufferLevel.Primary)
        {
            _activeFrameBuffer = Unsafe.As<IGLFrameBuffer>(frameBuffer);
            _activeRenderPass = renderPass;
            _activeRenderPassStep = 0;
            _activeClearColorCount = 0;

            if (_activeFrameBuffer.MustBlitFromDefaultOnStart)
                _commandList.Add(new GLBlitFrameBufferFromDefault(_activeFrameBuffer));

            _commandList.Add(new GLBindFrameBufferCommand(_activeFrameBuffer, 0));
        }
        public override void BeginRenderPass(IRenderPass renderPass, IFrameBuffer frameBuffer, Vector4 clearColor, CommandBufferLevel executionLevel = CommandBufferLevel.Primary)
        {
            ReadOnlySpan<RenderPassAttachment> attachments = renderPass.Attachments;
#if DEBUG
            if (attachments.Length != 1)
                throw new ArgumentException($"The RenderPass has {renderPass.Attachments.Length} attachments instead of 1.", "clearColor");
#endif

            BeginRenderPass(renderPass, frameBuffer, executionLevel);

            if (_activeClearColors == null || _activeClearColors.Length < 1)
                _activeClearColors = new Vector4[] { clearColor };
            else _activeClearColors[0] = clearColor;
            _activeClearColorCount = 1;

            AddClearCommand(frameBuffer.Resolution, new ReadOnlySpan<Vector4>(_activeClearColors, 0, _activeClearColorCount), renderPass.Steps[0], attachments);
        }
        public override void BeginRenderPass(IRenderPass renderPass, IFrameBuffer frameBuffer, in ReadOnlySpan<Vector4> clearValues, CommandBufferLevel executionLevel = CommandBufferLevel.Primary)
        {
            ReadOnlySpan<RenderPassAttachment> attachments = renderPass.Attachments;
#if DEBUG
            if (attachments.Length != clearValues.Length)
                throw new ArgumentException("Count of ClearValues is different than the attachments in the RenderPass", "clearValues");
#endif

            BeginRenderPass(renderPass, frameBuffer, executionLevel);

            if (_activeClearColors == null || _activeClearColors.Length < clearValues.Length)
                _activeClearColors = clearValues.ToArray();
            else for (int i = 0; i < clearValues.Length; i++)
                _activeClearColors[i] = clearValues[i];
            _activeClearColorCount = clearValues.Length;

            AddClearCommand(frameBuffer.Resolution, new ReadOnlySpan<Vector4>(_activeClearColors, 0, _activeClearColorCount), renderPass.Steps[0], attachments);
        }
        public override void NextRenderPassStep(CommandBufferLevel executionLevel = CommandBufferLevel.Primary)
        {
            if (_activeRenderPass != null && _activeFrameBuffer != null)
            {
                RestoreRenderPassState();
                _commandList.Add(new GLBindFrameBufferCommand(_activeFrameBuffer, ++_activeRenderPassStep));
                if (_activeClearColorCount > 0)
                    AddClearCommand(_activeFrameBuffer.Resolution, new ReadOnlySpan<Vector4>(_activeClearColors, 0, _activeClearColorCount), _activeRenderPass.Steps[_activeRenderPassStep], _activeRenderPass.Attachments);
            }
            else throw new Exception("GLCommandBuffer is not in a RenderPass, cannot use NextRenderPassStep");
        }
        public override void NextRenderPassStep(PipelineResource inputAttachmentResource, CommandBufferLevel executionLevel = CommandBufferLevel.Primary)
        {
            if (_activeRenderPass != null && _activeFrameBuffer != null)
            {
                NextRenderPassStep(executionLevel);
                inputAttachmentResource.BindInputAttachments(_activeRenderPass.Steps[_activeRenderPassStep], _activeFrameBuffer);
            }
            else throw new Exception("GLCommandBuffer is not in a RenderPass, cannot use NextRenderPassStep");
        }
        public override void EndRenderPass()
        {
            if (_activeFrameBuffer != null)
            {
                if (_activeFrameBuffer.MustBlitToDefaultOnEnd)
                    _commandList.Add(new GLBlitFrameBufferToDefault(_activeFrameBuffer));

                _activeFrameBuffer = null;
                _activeRenderPass = null;
                _activeRenderPassStep = -1;
                _activeClearColorCount = 0;
                RestoreAllState();
                AddBindDefaultFrameBufferCommand();
            }
            else throw new Exception("GLCommandBuffer is not in a RenderPass, cannot use EndRenderPass");
        }

        public override void BindIndexBuffer(IDataBuffer indexBuffer, IndexType type, ulong offset = 0ul) => AddBindIndexBufferCommand(indexBuffer, (int)type, offset);

        public override void BindResource(uint set, PipelineResource resource)
        {
            if (_activeGLGraphicsPipeline != null)
            {
                RestorePipelineResourceState(set);
                BindResource(_activeGLGraphicsPipeline, set, resource);
            }
            else throw new Exception("GLCommandBuffer is not using a Pipeline, cannot use BindResource");
        }
        public override void BindResource(uint set, PipelineResource resource, int dataIndex)
        {
            if (_activeGLGraphicsPipeline != null)
            {
                RestorePipelineResourceState(set);
                BindResource(_activeGLGraphicsPipeline, set, resource, dataIndex);
            }
            else throw new Exception("GLCommandBuffer is not using a Pipeline, cannot use BindResource");
        }

        //TODO: Implement Instanced Draw
        //public override void DrawInstanced(uint vertexCount, uint instanceCount) => throw new NotImplementedException();
        //public override void DrawInstanced(uint vertexCount, uint firstVertex, uint instanceCount) => throw new NotImplementedException();
        //public override void DrawInstanced(uint vertexCount, uint firstVertex, uint instanceCount, uint firstInstance) => throw new NotImplementedException();

        //TODO: Implement Instanced Draw
        //public override void DrawIndexedInstanced(uint indexCount, uint instanceCount) => throw new NotImplementedException();
        //public override void DrawIndexedInstanced(uint indexCount, uint firstIndex, uint instanceCount) => throw new NotImplementedException();
        //public override void DrawIndexedInstanced(uint indexCount, uint firstIndex, uint instanceCount, uint firstInstance) => throw new NotImplementedException();


        public override void ExecuteSecondaryCommandBuffers(in ReadOnlySpan<GraphicsCommandBuffer> commandBuffers)
        {
            foreach (GraphicsCommandBuffer commandBuffer in commandBuffers)
                _commandList.AddRange(Unsafe.As<GLCommandBufferList>(commandBuffer)._commandList);
        }


        public override void End()
        {
            RestorePipelineBindState();
            _activeGLGraphicsPipeline = null;
        }

        public void Execute()
        {
            for (int i = 0; i < _dataTransferCommandList.Count; i++)
            {
                _glDevice.CheckErrors("Before Command", _dataTransferCommandList[i]);
                //Debug.WriteLine(_dataTransferCommandList[i]);
                _dataTransferCommandList[i].Execute();
                _glDevice.CheckErrors("After Command", _dataTransferCommandList[i]);
            }

            for (int i = 0; i < _commandList.Count; i++)
            {
                _glDevice.CheckErrors("Before Command", _commandList[i]);
                //Debug.WriteLine(_commandList[i]);
                _commandList[i].Execute();
                _glDevice.CheckErrors("After Command", _commandList[i]);
            }

            _glDevice.CheckErrors("Before CommandBuffer Flush");
            Flush();
            _glDevice.CheckErrors("After CommandBuffer Flush");
        }

        public override string ToString() => $"Command Buffer (Data Transfer Commands: {_dataTransferCommandList.Count}, Commands: {_commandList.Count})";

        #endregion

    }
}
