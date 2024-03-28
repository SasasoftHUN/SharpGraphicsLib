using SharpGraphics.Shaders;
using SharpGraphics.GraphicsViews;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SharpGraphics.Apps.VertexAttributes
{
    public class VertexAttributesApp : GraphicsApplicationBase
    {

        [StructLayout(LayoutKind.Sequential)]
        struct Vertex
        {
            public Vector4 position;
            public Vector4 color;
        }

        #region Fields

        private bool _isDisposed;

        private IRenderPass _renderPass;
        private IGraphicsPipeline _pipeline;

        private IDeviceOnlyDataBuffer<Vertex> _vertexBuffer;
        private IDeviceOnlyDataBuffer<Vertex> _vertexBuffer2;

        #endregion

        #region Constructors

        public VertexAttributesApp(GraphicsManagement graphicsManagement, IGraphicsView view) : base(graphicsManagement, view) { }

        ~VertexAttributesApp() => Dispose(false);

        #endregion

        #region Protected Methods

        protected override void Initialize()
        {
            base.Initialize();

            _renderPass = _graphicsDevice.CreateRenderPass(
                    stackalloc RenderPassAttachment[] { new RenderPassAttachment(AttachmentType.Color | AttachmentType.Present, _graphicsDevice.SwapChain.Format.colorFormat) },
                    new RenderPassStep[] { new RenderPassStep(0u) });
            _graphicsDevice.SwapChain.RenderPass = _renderPass;

            using GraphicsShaderPrograms shaderPrograms = _graphicsDevice.CompileShaderPrograms<PassthroughVertexShader, PassthroughFragmentShader>();
            _pipeline = _graphicsDevice.CreatePipeline(new GraphicsPipelineConstuctionParameters(
                shaders: shaderPrograms.ShaderPrograms,
                geometryType: GeometryType.TriangleStrip,
                renderPass: _renderPass,
                vertexInputs: new VertexInputs(
                    stackalloc VertexInputBinding[] { VertexInputBinding.Create<Vertex>() },
                    stackalloc VertexInputAttribute[] { new VertexInputAttribute(0u, 0u, DataFormat.RGBA32f), new VertexInputAttribute(1u, (uint)Marshal.SizeOf<Vector4>(), DataFormat.RGBA32f) })
            ));


            using IStagingDataBuffer<Vertex> stagingBuffer = _graphicsDevice.CreateStagingDataBuffer<Vertex>(DataBufferType.VertexData, 8u);
            using GraphicsCommandBuffer copyCommandBuffer = _graphicsDevice.CommandProcessors[0].CommandBufferFactory.CreateCommandBuffer();
            copyCommandBuffer.Begin();

            Vertex[] vertices = new Vertex[]
            {
                new Vertex()
                {
                    position = new Vector4(-0.7f, -0.7f, 0f, 1f),
                    color = new Vector4(1f, 0f, 0f, 0f),
                },
                new Vertex()
                {
                    position = new Vector4(0.7f, -0.7f, 0f, 1f),
                    color = new Vector4(0f, 0f, 1f, 0f),
                },
                new Vertex()
                {
                    position = new Vector4(-0.7f, 0.7f, 0f, 1f),
                    color = new Vector4(0f, 1f, 0f, 0f),
                },
                new Vertex()
                {
                    position = new Vector4(0.7f, 0.7f, 0f, 1f),
                    color = new Vector4(0.3f, 0.3f, 0.3f, 0f),
                },
            };
            _vertexBuffer = _graphicsDevice.CreateDeviceOnlyDataBuffer<Vertex>(DataBufferType.VertexData | DataBufferType.CopyDestination, vertices.Length);
            _vertexBuffer.UseStagingBuffer(stagingBuffer);
            copyCommandBuffer.StoreData(_vertexBuffer, vertices);
            _vertexBuffer.ReleaseStagingBuffer();

            Vertex[] vertices2 = new Vertex[]
            {
                new Vertex()
                {
                    position = new Vector4(-0.7f, -0.7f, 0f, 1f),
                    color = new Vector4(0f, 0f, 1f, 0f),
                },
                new Vertex()
                {
                    position = new Vector4(0.7f, -0.7f, 0f, 1f),
                    color = new Vector4(1f, 0f, 0f, 0f),
                },
                new Vertex()
                {
                    position = new Vector4(-0.7f, 0.7f, 0f, 1f),
                    color = new Vector4(0f, 1f, 0f, 0f),
                },
                new Vertex()
                {
                    position = new Vector4(0.7f, 0.7f, 0f, 1f),
                    color = new Vector4(0.3f, 0.3f, 0.3f, 0f),
                },
            };
            _vertexBuffer2 = _graphicsDevice.CreateDeviceOnlyDataBuffer<Vertex>(DataBufferType.VertexData | DataBufferType.CopyDestination, vertices.Length);
            _vertexBuffer2.UseStagingBuffer(stagingBuffer, 4u);
            copyCommandBuffer.StoreData(_vertexBuffer2, vertices2);
            _vertexBuffer2.ReleaseStagingBuffer();

            copyCommandBuffer.End();
            copyCommandBuffer.Submit();

            _graphicsDevice.WaitForIdle();
        }

        protected override void Update() { }

        protected override void Render()
        {
            if (_graphicsDevice.SwapChain.TryBeginFrame(out GraphicsCommandBuffer commandBuffer, out IFrameBuffer<ITexture2D> frameBuffer))
            {
                Vector4 clearColor = new Vector4(1f, 0.7f, 0f, 0f) * (MathF.Sin(_timers.TimeSeconds) * 0.5f + 0.5f);

                commandBuffer.BeginRenderPass(_renderPass, frameBuffer, clearColor);

                commandBuffer.BindPipeline(_pipeline);
                commandBuffer.BindVertexBuffer(((int)_timers.TimeSeconds) % 2 == 0 ? _vertexBuffer : _vertexBuffer2);

                commandBuffer.Draw(4u);

                commandBuffer.EndRenderPass();

                _graphicsDevice.SwapChain.PresentFrame();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.
                if (!disposing)
                    Debug.WriteLine($"Disposing App from {(disposing ? "Dispose()" : "Finalizer")}...");

                _graphicsDevice.WaitForIdle();

                _vertexBuffer2.Dispose();
                _vertexBuffer.Dispose();
                _pipeline.Dispose();
                _renderPass.Dispose();

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

    }
}
