using SharpGraphics.GraphicsViews;
using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using static SharpGraphics.Utils.ParametricSurfaces;

//[assembly: ShaderTypeMappingExternal(typeof(System.Numerics.Vector3))]

namespace SharpGraphics.Apps.Deferred
{

    public class DeferredApp : GraphicsModuleBase
    {

        [StructLayout(LayoutKind.Sequential)]
        public struct Transform
        {
            public Matrix4x4 mvp;
            public Matrix4x4 world;
            public Matrix4x4 worldIT;

            public Transform(in Matrix4x4 world, in Matrix4x4 vp)
            {
                this.mvp = world * vp;
                this.world = world;
                Matrix4x4.Invert(Matrix4x4.Transpose(world), out this.worldIT);
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct LightData
        {
            [FieldOffset(0)]
            public readonly Vector3 direction;

            [FieldOffset(16)]
            public readonly Vector4 ambientColor;
            [FieldOffset(32)]
            public readonly Vector4 diffuseColor;
            [FieldOffset(48)]
            public readonly Vector4 specularColor;

            public LightData(Vector3 direction, Vector4 ambientColor, Vector4 diffuseColor, Vector4 specularColor)
            {
                this.direction = direction;
                
                this.ambientColor = ambientColor;
                this.diffuseColor = diffuseColor;
                this.specularColor = specularColor;
            }

        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MaterialData
        {
            public readonly Vector4 ambientColor;
            public readonly Vector4 diffuseColor;
            public readonly Vector4 specularColor;
            public readonly float specularPower;

            public MaterialData(Vector4 color, float specularPower)
            {
                ambientColor = color;
                diffuseColor = color;
                specularColor = color;
                this.specularPower = specularPower;
            }
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct SceneData
        {
            [FieldOffset(0)]
            public readonly Vector3 cameraPosition;

            public SceneData(Vector3 cameraPosition)
            {
                this.cameraPosition = cameraPosition;
            }
        }

        #region Fields

        private bool _isDisposed;

        private IRenderPass _renderPass;
        private PipelineResourceLayout _gPassPipelineResourceLayout;
        private PipelineResourceLayout _deferredPassPipelineResourceLayout;
        private PipelineResourceLayout _deferredPassInputPipelineResourceLayout;
        private IGraphicsPipeline _gPassPipeline;
        private IGraphicsPipeline _deferredPassPipeline;

        private IDeviceOnlyDataBuffer<Vertex> _vertexBuffer;
        private IDeviceOnlyDataBuffer<ushort> _indexBuffer;

        private FlyingCamera _camera = new FlyingCamera(new Vector3(5f, 5f ,0f), Vector3.Zero, Vector3.UnitY, 3.14f / 4f, 1f, 1f, 1000f);

        private List<Vector3> _spherePositions = new List<Vector3>();
        private Transform[] _transforms;
        private MaterialData[] _materials;

        private FrameResource<IMappableDataBuffer<Transform>> _transformUBO;
        private IDeviceOnlyDataBuffer<LightData> _lightDataUBO;
        private FrameResource<IDeviceOnlyDataBuffer<MaterialData>> _materialUBO;
        private FrameResource<IDeviceOnlyDataBuffer<SceneData>> _sceneDataUBO;
        private FrameResource<PipelineResource> _gPassPipelineResource;
        private FrameResource<PipelineResource> _deferredPassPipelineResource;
        private FrameResource<PipelineResource> _deferredPassInputPipelineResource;

        private ITexture2D _texture;
        private TextureSampler _sampler;

        #endregion

        #region Constructors

        public DeferredApp(in GraphicsModuleContext context) : base(context)
        {
            if (_device.SwapChain == null)
                throw new NullReferenceException("Device SwapChain is null");

            _camera.UserInput = context.view.UserInputSource;

            _spherePositions.Add(Vector3.Zero);
            _spherePositions.Add(new Vector3(0f, 2f, 0f));
            _spherePositions.Add(new Vector3(2f, 0f, 0f));

            _transforms = new Transform[_spherePositions.Count];
            _materials = new MaterialData[_spherePositions.Count];
            UpdateSpheres();

            _device.SwapChain.SizeChanged += SwapChain_SizeChanged;
            SwapChain_SizeChanged(this, new SwapChainSizeChangedEventArgs(_device.SwapChain.Size));

            _renderPass = _device.CreateRenderPass(
                stackalloc RenderPassAttachment[]
                {
                    new RenderPassAttachment(AttachmentType.Color | AttachmentType.ShaderInput, DataFormat.RGBA32f, AttachmentLoadOperation.Clear, AttachmentStoreOperation.Undefined), //Deferred Position Buffer
                    new RenderPassAttachment(AttachmentType.Color | AttachmentType.ShaderInput, DataFormat.RGBA32f, AttachmentLoadOperation.Clear, AttachmentStoreOperation.Undefined), //Deferred Diffuse Buffer
                    new RenderPassAttachment(AttachmentType.Color | AttachmentType.ShaderInput, DataFormat.RGBA32f, AttachmentLoadOperation.Clear, AttachmentStoreOperation.Undefined), //Deferred Normal Buffer
                    new RenderPassAttachment(AttachmentType.Depth, DataFormat.Depth24un, AttachmentLoadOperation.Clear, AttachmentStoreOperation.Undefined),
                    new RenderPassAttachment(AttachmentType.Color | AttachmentType.Present, _device.SwapChain.Format.colorFormat, AttachmentLoadOperation.Undefined, AttachmentStoreOperation.Store),
                },
                new RenderPassStep[]
                {
                    new RenderPassStep(stackalloc uint[] { 0u, 1u, 2u }, 3u),
                    new RenderPassStep(4u, stackalloc uint[] { 0u, 1u, 2u }),
                });
            _device.SwapChain.RenderPass = _renderPass;

            //G-Pass
            _gPassPipelineResourceLayout = _device.CreatePipelineResourceLayout(new PipelineResourceProperties(stackalloc PipelineResourceProperty[]
            {
                new PipelineResourceProperty(0u, PipelineResourceType.UniformBufferDynamic, GraphicsShaderStages.Vertex),
                new PipelineResourceProperty(1u, PipelineResourceType.CombinedTextureSampler, GraphicsShaderStages.Fragment),
            }));

            using (GraphicsShaderPrograms shaderPrograms = _device.CompileShaderPrograms<TransformVertexShader, GPassFragmentShader>())
                _gPassPipeline = _device.CreatePipeline(new GraphicsPipelineConstuctionParameters(
                    shaders: shaderPrograms,
                    geometryType: GeometryType.Triangles,
                    renderPass: _renderPass,
                    vertexInputs: new VertexInputs(
                        stackalloc VertexInputBinding[] { VertexInputBinding.Create<Vertex>() },
                        stackalloc VertexInputAttribute[]
                        {
                            new VertexInputAttribute(0u, 0u, DataFormat.RGB32f),
                            new VertexInputAttribute(1u, (uint)Marshal.SizeOf<Vector3>(), DataFormat.RGB32f),
                            new VertexInputAttribute(2u, (uint)Marshal.SizeOf<Vector3>() * 2u, DataFormat.RG32f),
                        }),
                    resourceLayouts: new PipelineResourceLayout[] { _gPassPipelineResourceLayout },
                    depthUsage: DepthUsage.Enabled()
                ));

            //Deferred Pass
            _deferredPassInputPipelineResourceLayout = _device.CreatePipelineResourceLayoutForInputAttachments(_renderPass.Steps[1], 0u);
            _deferredPassPipelineResourceLayout = _device.CreatePipelineResourceLayout(new PipelineResourceProperties(stackalloc PipelineResourceProperty[]
{
                new PipelineResourceProperty(0u, 3u, PipelineResourceType.UniformBuffer, GraphicsShaderStages.Fragment),
                new PipelineResourceProperty(1u, 4u, PipelineResourceType.UniformBuffer, GraphicsShaderStages.Fragment),
            }));

            using (GraphicsShaderPrograms shaderPrograms = _device.CompileShaderPrograms<PostProcessVertexShader, DeferredFragmentShader>())
                _deferredPassPipeline = _device.CreatePipeline(new GraphicsPipelineConstuctionParameters(
                    shaders: shaderPrograms.ShaderPrograms,
                    geometryType: GeometryType.TriangleStrip,
                    renderPass: _renderPass,
                    renderPassStep: 1u,
                    resourceLayouts: new PipelineResourceLayout[] { _deferredPassInputPipelineResourceLayout, _deferredPassPipelineResourceLayout }
                ));


            GeometryData sphere = ParametricSurfaces.Generate(ParametricSurfaces.Sphere);
            using GraphicsCommandBuffer copyCommandBuffer = _device.CommandProcessors[0].CommandBufferFactory.CreateCommandBuffer();
            copyCommandBuffer.Begin();

            _vertexBuffer = _device.CreateDeviceOnlyDataBuffer<Vertex>(DataBufferType.VertexData | DataBufferType.Store, sphere.vertices.Length);
            copyCommandBuffer.StoreData(_vertexBuffer, sphere.vertices);

            _indexBuffer = _device.CreateDeviceOnlyDataBuffer<ushort>(DataBufferType.IndexData | DataBufferType.Store, sphere.indices.Length);
            copyCommandBuffer.StoreData(_indexBuffer, sphere.indices);

            _lightDataUBO = _device.CreateDeviceOnlyDataBuffer<LightData>(DataBufferType.UniformData | DataBufferType.Store, 1u);
            copyCommandBuffer.StoreData(_lightDataUBO, new LightData(Vector3.Normalize(new Vector3(-1f, -1f, 1f)),
                new Vector4(0.1f, 0.1f, 0.1f, 1f), new Vector4(0.7f, 0.7f, 0.7f, 1f), new Vector4(0.4f, 0.4f, 0.4f, 1f)));


            DataFormat colorFormat = _device.SwapChain.Format.colorFormat.IsSRGB() ? DataFormat.RGBA8srgb : DataFormat.RGBA8un;
            Configuration.Default.PreferContiguousImageBuffers = true;
            _texture = _device.CreateTexture2D(colorFormat, new Vector2UInt(512u), TextureType.Store | TextureType.CopySource | TextureType.ShaderSample, MemoryType.DeviceOnly);
            using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("DeferredResources.earth.png"))
                if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                    copyCommandBuffer.StoreTextureData<Rgba32>(_texture, pixels, TextureLayout.ShaderReadOnly, 0u);
            _texture.GenerateMipmaps(copyCommandBuffer);

            copyCommandBuffer.End();
            copyCommandBuffer.Submit();

            _transformUBO = new UniversalFrameResource<IMappableDataBuffer<Transform>>(_device.SwapChain, () => _device.CreateMappableDataBuffer<Transform>(DataBufferType.UniformData | DataBufferType.Store, MappableMemoryType.DeviceLocal, _transforms.Length));
            _materialUBO = new UniversalFrameResource<IDeviceOnlyDataBuffer<MaterialData>>(_device.SwapChain, () => _device.CreateDeviceOnlyDataBuffer<MaterialData>(DataBufferType.UniformData | DataBufferType.Store, _materials.Length));
            _sceneDataUBO = new UniversalFrameResource<IDeviceOnlyDataBuffer<SceneData>>(_device.SwapChain, () => _device.CreateDeviceOnlyDataBuffer<SceneData>(DataBufferType.UniformData | DataBufferType.Store, 1u));
            _gPassPipelineResource = new BatchCreatedFrameResource<PipelineResource>(_device.SwapChain, (uint count) => _gPassPipelineResourceLayout.CreateResources(count));
            _deferredPassPipelineResource = new BatchCreatedFrameResource<PipelineResource>(_device.SwapChain, (uint count) => _deferredPassPipelineResourceLayout.CreateResources(count));
            _deferredPassInputPipelineResource = new BatchCreatedFrameResource<PipelineResource>(_device.SwapChain, (uint count) => _deferredPassInputPipelineResourceLayout.CreateResources(count));

            _sampler = _device.CreateTextureSampler(new TextureSamplerConstruction(TextureFilterType.Linear, TextureMipMapType.Linear, 16f));

            _device.WaitForIdle();
        }

        ~DeferredApp() => Dispose(false);

        #endregion

        #region Graphics Event Handlers

        private void SwapChain_SizeChanged(object? sender, SwapChainSizeChangedEventArgs e)
        {
            _camera.SetProjection(3.14f / 4.0f, e.Size.x / (float)e.Size.y, 1f, 1000f);
        }

        #endregion

        #region Private Methods

        private void UpdateSpheres()
        {
            if (_transforms == null || _transforms.Length != _spherePositions.Count)
                _transforms = new Transform[_spherePositions.Count];
            for (int i = 0; i < _transforms.Length; i++)
                _transforms[i] = new Transform(Matrix4x4.CreateTranslation(_spherePositions[i]), _camera.ViewProjectionMatrix);

            if (_materials == null || _materials.Length != _spherePositions.Count)
                _materials = new MaterialData[_spherePositions.Count];
            for (int i = 0; i < _materials.Length; i++)
                _materials[i] = new MaterialData(Vector4.One, 16f);
        }

        #endregion

        #region Protected Methods

        protected override void Update()
        {
            float t = _timers.TimeSeconds;

            //_camera.Eye = new Vector3((float)(Math.Cos(t) * 5d), 5f, (float)(Math.Sin(t) * 5d));
            _camera.Update(_timers.DeltaTime);

            UpdateSpheres();
        }

        protected override void Render()
        {
            if (_device.SwapChain != null && _device.SwapChain.TryBeginFrame(out GraphicsCommandBuffer? commandBuffer, out IFrameBuffer<ITexture2D>? frameBuffer))
            {
                _gPassPipelineResource.Resource.BindUniformBufferDynamic(0u, _transformUBO);
                _gPassPipelineResource.Resource.BindTexture(1u, _sampler, _texture);
                commandBuffer.StoreData(_transformUBO.Resource, _transforms);

                _deferredPassPipelineResource.Resource.BindUniformBuffer(0u, _lightDataUBO);
                _deferredPassPipelineResource.Resource.BindUniformBuffer(1u, _sceneDataUBO);
                commandBuffer.StoreData(_sceneDataUBO.Resource, new SceneData(_camera.Eye));

                Vector4 clearColor = new Vector4(1f, 0.7f, 0f, 0f) * (MathF.Sin(_timers.TimeSeconds) * 0.5f + 0.5f);

                //G-Pass
                commandBuffer.BeginRenderPass(_renderPass, frameBuffer, stackalloc Vector4[] { Vector4.Zero, Vector4.Zero, Vector4.Zero, Vector4.One, clearColor });

                commandBuffer.BindPipeline(_gPassPipeline);

                commandBuffer.BindVertexBuffer(_vertexBuffer);
                commandBuffer.BindIndexBuffer(_indexBuffer);

                for (int i = 0; i < _transforms.Length; i++)
                {
                    commandBuffer.BindResource(0u, _gPassPipelineResource, i);

                    commandBuffer.DrawIndexed(_indexBuffer.Capacity);
                }

                //Deferred-Pass
                commandBuffer.NextRenderPassStep(_deferredPassInputPipelineResource);

                commandBuffer.BindPipeline(_deferredPassPipeline);
                commandBuffer.BindResource(0u, _deferredPassInputPipelineResource);
                commandBuffer.BindResource(1u, _deferredPassPipelineResource);

                commandBuffer.Draw(4u);

                commandBuffer.EndRenderPass();

                _device.SwapChain.PresentFrame();
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

                _device.WaitForIdle();

                _sampler?.Dispose();
                _texture?.Dispose();
                _indexBuffer?.Dispose();
                _vertexBuffer?.Dispose();
                _renderPass?.Dispose();
                _sceneDataUBO?.Dispose();
                _lightDataUBO?.Dispose();
                _materialUBO?.Dispose();
                _transformUBO?.Dispose();
                _gPassPipelineResource?.Dispose();
                _gPassPipelineResourceLayout?.Dispose();
                _deferredPassPipelineResource?.Dispose();
                _deferredPassPipelineResourceLayout?.Dispose();
                _deferredPassInputPipelineResource?.Dispose();
                _deferredPassInputPipelineResourceLayout?.Dispose();
                _gPassPipeline?.Dispose();
                _deferredPassPipeline?.Dispose();

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public static DeferredApp Factory(in GraphicsModuleContext context) => new DeferredApp(context);

        #endregion

    }
}