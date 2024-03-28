using SharpGraphics.Utils;
using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SharpGraphics.Apps.QuadPlayground
{
    public class QuadPlaygroundApp : GraphicsApplicationBase
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
        private List<IGraphicsPipeline> _pipelines = new List<IGraphicsPipeline>();
        private int _pipelineIndex = 0;

        private IDeviceOnlyDataBuffer<Vertex> _vertexBuffer;

        #endregion

        #region Constructors

        public QuadPlaygroundApp(GraphicsManagement graphicsManagement, IGraphicsView view) : base(graphicsManagement, view)
        {
            if (view.UserInputSource != null)
                view.UserInputSource.UserInput += View_UserInput;
        }

        ~QuadPlaygroundApp() => Dispose(false);

        #endregion

        #region View Event Handlers

        private void View_UserInput(object sender, UserInputEventArgs e)
        {
            switch (e)
            {
                case KeyboardEventArgs keyboardEvent:
                    {
                        int pipelineIndex = keyboardEvent.Key switch
                        {
                            KeyboardKey.Digit0 => 0,
                            KeyboardKey.Digit1 => 1,
                            KeyboardKey.Digit2 => 2,
                            KeyboardKey.Digit3 => 3,
                            KeyboardKey.Digit4 => 4,
                            KeyboardKey.Digit5 => 5,
                            KeyboardKey.Digit6 => 6,
                            KeyboardKey.Digit7 => 7,
                            KeyboardKey.Digit8 => 8,
                            KeyboardKey.Digit9 => 9,
                            _ => 0,
                        };
                        if (pipelineIndex >= 0 && pipelineIndex < _pipelines.Count)
                            _pipelineIndex = pipelineIndex;
                    }
                    break;
            }
        }

        #endregion

        #region Protected Methods

        protected override void Initialize()
        {
            base.Initialize();

            if (_graphicsDevice == null || _graphicsDevice.SwapChain == null)
                throw new NullReferenceException("Graphics Device or SwapChain not initialized!");

            _renderPass = _graphicsDevice.CreateRenderPass(
                    stackalloc RenderPassAttachment[] { new RenderPassAttachment(AttachmentType.Color | AttachmentType.Present, _graphicsDevice.SwapChain.Format.colorFormat) },
                    new RenderPassStep[] { new RenderPassStep(0u) });
            _graphicsDevice.SwapChain.RenderPass = _renderPass;

            //PIPELINES
            VertexInputs vertexInputs = new VertexInputs(
                    stackalloc VertexInputBinding[] { VertexInputBinding.Create<Vertex>() },
                    stackalloc VertexInputAttribute[] { new VertexInputAttribute(0u, 0u, DataFormat.RGBA32f), new VertexInputAttribute(1u, (uint)Marshal.SizeOf<Vector4>(), DataFormat.RGBA32f) });


            IShaderSource vertexShaderSource = _graphicsDevice.CreateShaderSource<PassthroughVertexShader>();
            using IGraphicsShaderProgram vertexShader = _graphicsDevice.CompileShaderProgram(new GraphicsShaderSource(vertexShaderSource, GraphicsShaderStages.Vertex));

            //Passthrough Pipeline
            IShaderSource fragmentShaderSource = _graphicsDevice.CreateShaderSource<PassthroughFragmentShader>();
            using (IGraphicsShaderProgram fragmentShader = _graphicsDevice.CompileShaderProgram(new GraphicsShaderSource(fragmentShaderSource, GraphicsShaderStages.Fragment)))
                _pipelines.Add(_graphicsDevice.CreatePipeline(new GraphicsPipelineConstuctionParameters(
                    shaders: new IGraphicsShaderProgram[] { vertexShader, fragmentShader },
                    geometryType: GeometryType.TriangleStrip, renderPass: _renderPass, vertexInputs: vertexInputs
                )));

            //Inverted Pipeline
            fragmentShaderSource = _graphicsDevice.CreateShaderSource<InvertedFragmentShader>();
            using (IGraphicsShaderProgram fragmentShader = _graphicsDevice.CompileShaderProgram(new GraphicsShaderSource(fragmentShaderSource, GraphicsShaderStages.Fragment)))
                _pipelines.Add(_graphicsDevice.CreatePipeline(new GraphicsPipelineConstuctionParameters(
                    shaders: new IGraphicsShaderProgram[] { vertexShader, fragmentShader },
                    geometryType: GeometryType.TriangleStrip, renderPass: _renderPass, vertexInputs: vertexInputs
                )));

            //Keep Right Pipeline
            fragmentShaderSource = _graphicsDevice.CreateShaderSource<KeepRightFragmentShader>();
            using (IGraphicsShaderProgram fragmentShader = _graphicsDevice.CompileShaderProgram(new GraphicsShaderSource(fragmentShaderSource, GraphicsShaderStages.Fragment)))
                _pipelines.Add(_graphicsDevice.CreatePipeline(new GraphicsPipelineConstuctionParameters(
                    shaders: new IGraphicsShaderProgram[] { vertexShader, fragmentShader },
                    geometryType: GeometryType.TriangleStrip, renderPass: _renderPass, vertexInputs: vertexInputs
                )));

            //Mandelbrot Pipeline
            fragmentShaderSource = _graphicsDevice.CreateShaderSource<MandelbrotFragmentShader>();
            using (IGraphicsShaderProgram fragmentShader = _graphicsDevice.CompileShaderProgram(new GraphicsShaderSource(fragmentShaderSource, GraphicsShaderStages.Fragment)))
                _pipelines.Add(_graphicsDevice.CreatePipeline(new GraphicsPipelineConstuctionParameters(
                    shaders: new IGraphicsShaderProgram[] { vertexShader, fragmentShader },
                    geometryType: GeometryType.TriangleStrip, renderPass: _renderPass, vertexInputs: vertexInputs
                )));


            //VERTICES
            using GraphicsCommandBuffer copyCommandBuffer = _graphicsDevice.CommandProcessors[0].CommandBufferFactory.CreateCommandBuffer();
            copyCommandBuffer.Begin();

            Vertex[] vertices = new Vertex[]
            {
                new Vertex()
                {
                    position = new Vector4(-1f, -1f, 0f, 1f),
                    color = new Vector4(1f, 0f, 0f, 0f),
                },
                new Vertex()
                {
                    position = new Vector4(1f, -1f, 0f, 1f),
                    color = new Vector4(0f, 0f, 1f, 0f),
                },
                new Vertex()
                {
                    position = new Vector4(-1f, 1f, 0f, 1f),
                    color = new Vector4(0f, 1f, 0f, 0f),
                },
                new Vertex()
                {
                    position = new Vector4(1f, 1f, 0f, 1f),
                    color = new Vector4(1f, 1f, 1f, 0f),
                },
            };
            _vertexBuffer = _graphicsDevice.CreateDeviceOnlyDataBuffer<Vertex>(DataBufferType.VertexData | DataBufferType.Store, vertices.Length);
            copyCommandBuffer.StoreData(_vertexBuffer, vertices);

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

                IGraphicsPipeline pipeline = _pipelines[_pipelineIndex];
                commandBuffer.BindPipeline(pipeline);
                commandBuffer.BindVertexBuffer(_vertexBuffer);

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
                Debug.WriteLine("Disposing App from " + (disposing ? "Dispose()" : "Finalizer") + "...");

                _graphicsDevice?.WaitForIdle();

                _vertexBuffer?.Dispose();
                foreach (IGraphicsPipeline pipeline in _pipelines)
                    pipeline.Dispose();
                _renderPass?.Dispose();

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

    }
}
