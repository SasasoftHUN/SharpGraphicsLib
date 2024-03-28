using SharpGraphics.GraphicsViews;
using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SharpGraphics.Apps.PushValues
{


    [StructLayout(LayoutKind.Sequential)]
    public struct Transform
    {
        public Matrix4x4 mvp;
    }

    public class PushValuesApp : GraphicsApplicationBase
    {

        [StructLayout(LayoutKind.Sequential)]
        struct Vertex
        {
            public Vector3 position;
            public Vector3 color;
        }

        struct FrameResources : IDisposable
        {
            public readonly IMappableDataBuffer<Transform> transformUBO;
            public readonly PipelineResource pipelineResource;

            public FrameResources(IMappableDataBuffer<Transform> transformUBO, PipelineResource pipelineResource)
            {
                this.transformUBO = transformUBO;
                this.pipelineResource = pipelineResource;
                pipelineResource.BindUniformBuffer(0u, transformUBO);
            }

            public void Dispose()
            {
                pipelineResource?.Dispose();
                transformUBO?.Dispose();
            }
        }

        #region Fields

        private bool _isDisposed;

        private IRenderPass _renderPass;
        private PipelineResourceLayout _pipelineResourceLayout;
        private IGraphicsPipeline _pipeline;

        private IDeviceOnlyDataBuffer<Vertex> _vertexBuffer;

        private Vector3 _eyePos;
        private Matrix4x4 _matWorld;
        private Matrix4x4 _matView;
        private Matrix4x4 _matProj;

        private Transform _transform;

        private FrameResource<FrameResources> _frameResources;

        #endregion

        #region Constructors

        public PushValuesApp(GraphicsManagement graphicsManagement, IGraphicsView view) : base(graphicsManagement, view) { }

        ~PushValuesApp() => Dispose(false);

        #endregion

        #region Graphics Event Handlers

        private void SwapChain_SizeChanged(object sender, SwapChainSizeChangedEventArgs e)
        {
            _matProj = Matrix4x4.CreatePerspectiveFieldOfView(3.14f / 4.0f, e.Size.x / (float)e.Size.y, 1f, 1000f);
        }

        #endregion

        #region Protected Methods

        protected override void Initialize()
        {
            base.Initialize();

            _graphicsDevice.SwapChain.SizeChanged += SwapChain_SizeChanged;
            SwapChain_SizeChanged(this, new SwapChainSizeChangedEventArgs(_graphicsDevice.SwapChain.Size));

            _renderPass = _graphicsDevice.CreateRenderPass(
                    stackalloc RenderPassAttachment[] { new RenderPassAttachment(AttachmentType.Color | AttachmentType.Present, _graphicsDevice.SwapChain.Format.colorFormat) },
                    new RenderPassStep[] { new RenderPassStep(0u) });
            _graphicsDevice.SwapChain.RenderPass = _renderPass;

            _pipelineResourceLayout = _graphicsDevice.CreatePipelineResourceLayout(new PipelineResourceProperties(stackalloc PipelineResourceProperty[]
            {
                new PipelineResourceProperty(0u, PipelineResourceType.UniformBuffer, GraphicsShaderStages.Vertex),
            }));

            using (GraphicsShaderPrograms shaderPrograms = _graphicsDevice.CompileShaderPrograms<TransformVertexShader, PassthroughFragmentShader>())
                _pipeline = _graphicsDevice.CreatePipeline(new GraphicsPipelineConstuctionParameters(
                    shaders: shaderPrograms.ShaderPrograms,
                    geometryType: GeometryType.TriangleStrip,
                    renderPass: _renderPass,
                    vertexInputs: new VertexInputs(
                        stackalloc VertexInputBinding[] { VertexInputBinding.Create<Vertex>() },
                        stackalloc VertexInputAttribute[] { new VertexInputAttribute(0u, 0u, DataFormat.RGB32f), new VertexInputAttribute(1u, (uint)Marshal.SizeOf<Vector3>(), DataFormat.RGB32f) }),
                    resourceLayouts: new PipelineResourceLayout[] { _pipelineResourceLayout },
                    depthUsage: DepthUsage.Disabled()
                ));

            Vertex[] vertices = new Vertex[]
            {
                new Vertex()
                {
                    position = new Vector3(-0.7f, -0.7f, 0f),
                    color = new Vector3(1f, 0f, 0f),
                },
                new Vertex()
                {
                    position = new Vector3(-0.7f, 0.7f, 0f),
                    color = new Vector3(0f, 1f, 0f),
                },
                new Vertex()
                {
                    position = new Vector3(0.7f, -0.7f, 0f),
                    color = new Vector3(0f, 0f, 1f),
                },
                new Vertex()
                {
                    position = new Vector3(0.7f, 0.7f, 0f),
                    color = new Vector3(0.3f, 0.3f, 0.3f),
                },
            };


            using GraphicsCommandBuffer copyCommandBuffer = _graphicsDevice.CommandProcessors[0].CommandBufferFactory.CreateCommandBuffer();
            copyCommandBuffer.Begin();

            _vertexBuffer = _graphicsDevice.CreateDeviceOnlyDataBuffer<Vertex>(DataBufferType.VertexData | DataBufferType.Store, vertices.Length);
            copyCommandBuffer.StoreData(_vertexBuffer, vertices);

            copyCommandBuffer.End();
            copyCommandBuffer.Submit();

            _frameResources = new BatchCreatedFrameResource<FrameResources>(_graphicsDevice.SwapChain, (uint count) =>
            {
                FrameResources[] resources = new FrameResources[count];
                PipelineResource[] pipelineResources = _pipelineResourceLayout.CreateResources(count);
                for (int i = 0; i < count; i++)
                    resources[i] = new FrameResources(
                        _graphicsDevice.CreateMappableDataBuffer<Transform>(DataBufferType.UniformData | DataBufferType.Store, MappableMemoryType.DeviceLocal, 1u),
                        pipelineResources[i]);
                return resources;
            });

            _graphicsDevice.WaitForIdle();
        }

        protected override void Update()
        {
            float t = _timers.TimeSeconds;

            _eyePos = new Vector3((float)(Math.Cos(t) * 5d), 5f, (float)(Math.Sin(t) * 5d));
            _matView = Matrix4x4.CreateLookAt(
                _eyePos,
                new Vector3(0, 0, 0),
                new Vector3(0, 1, 0)
                );

            _matWorld = MathUtils.CreateIdentityMatrix();

            _transform.mvp = _matWorld * _matView * _matProj;
        }

        protected override void Render()
        {
            if (_graphicsDevice.SwapChain.TryBeginFrame(out GraphicsCommandBuffer commandBuffer, out IFrameBuffer<ITexture2D> frameBuffer))
            {
                ref FrameResources fr = ref _frameResources.Resource;

                commandBuffer.StoreData(fr.transformUBO, ref _transform);
                
                Vector4 clearColor = new Vector4(1f, 0.7f, 0f, 0f) * (MathF.Sin(_timers.TimeSeconds) * 0.5f + 0.5f);

                commandBuffer.BeginRenderPass(_renderPass, frameBuffer, clearColor);

                commandBuffer.BindPipeline(_pipeline);
                commandBuffer.BindVertexBuffer(_vertexBuffer);
                commandBuffer.BindResource(0u, fr.pipelineResource);

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

                _graphicsDevice.WaitForIdle();

                _vertexBuffer?.Dispose();
                _pipeline?.Dispose();
                _renderPass?.Dispose();
                _frameResources?.Dispose();
                _pipelineResourceLayout?.Dispose();

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

    }
}
