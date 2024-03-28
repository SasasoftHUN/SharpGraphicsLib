using SharpGraphics.GraphicsViews;
using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using static SharpGraphics.Utils.ParametricSurfaces;

namespace SharpGraphics.Apps.Normals
{

    public class NormalsApp : GraphicsModuleBase
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
            public Vector3 direction;

            [FieldOffset(16)]
            public Vector4 ambientColor;
            [FieldOffset(32)]
            public Vector4 diffuseColor;
            [FieldOffset(48)]
            public Vector4 specularColor;

            [FieldOffset(64)]
            public float specularPower;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MaterialData
        {
            public Vector4 ambientColor;
            public Vector4 diffuseColor;
            public Vector4 specularColor;

            public MaterialData(Vector4 color)
            {
                ambientColor = color;
                diffuseColor = color;
                specularColor = color;
            }
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct SceneData
        {
            [FieldOffset(0)]
            public Vector3 cameraPosition;
        }

        struct FrameResources : IDisposable
        {
            public readonly IMappableDataBuffer<Transform> transformUBO;
            public readonly IDeviceOnlyDataBuffer<MaterialData> materialUBO;
            public readonly IDeviceOnlyDataBuffer<SceneData> sceneUBO;
            public readonly PipelineResource pipelineResource;

            public FrameResources(IMappableDataBuffer<Transform> transformUBO, IDeviceOnlyDataBuffer<MaterialData> materialUBO, IDeviceOnlyDataBuffer<SceneData> sceneUBO, PipelineResource pipelineResource)
            {
                this.transformUBO = transformUBO;
                this.materialUBO = materialUBO;
                this.sceneUBO = sceneUBO;
                this.pipelineResource = pipelineResource;

                pipelineResource.BindUniformBufferDynamic(0u, transformUBO);
                pipelineResource.BindUniformBufferDynamic(1u, materialUBO);
                pipelineResource.BindUniformBuffer(3u, sceneUBO);
            }

            public void Dispose()
            {
                pipelineResource?.Dispose();
                sceneUBO?.Dispose();
                materialUBO?.Dispose();
                transformUBO?.Dispose();
            }
        }

        #region Fields

        private bool _isDisposed;

        private IRenderPass _renderPass;
        private PipelineResourceLayout _pipelineResourceLayout;
        private IGraphicsPipeline _pipeline;

        //private IDeviceOnlyDataBuffer<Vertex> _vertexBuffer;
        private IDeviceOnlyDataBuffer<Vector3> _vertexPositionBuffer;
        private IDeviceOnlyDataBuffer<Vector3> _vertexNormalBuffer;
        private IDeviceOnlyDataBuffer<Vector2> _vertexUVBuffer;
        private IDataBuffer[] _vertexBuffers;
        private IDeviceOnlyDataBuffer<ushort> _indexBuffer;

        private FlyingCamera _camera = new FlyingCamera(new Vector3(5f, 5f ,0f), Vector3.Zero, Vector3.UnitY, 3.14f / 4f, 1f, 1f, 1000f);

        private List<Vector3> _spherePositions = new List<Vector3>();
        private Transform[] _transforms;
        private MaterialData[] _materials;

        private IDeviceOnlyDataBuffer<LightData> _lightDataUBO;
        private FrameResource<FrameResources> _frameResources;

        private ITexture2D _texture;
        private ITextureCube _skyboxTexture;
        private TextureSampler _sampler;

        #endregion

        #region Constructors

        public NormalsApp(in GraphicsModuleContext context) : base(context)
        {
            if (_device.SwapChain == null)
                throw new NullReferenceException("Device SwapChain is null");

            _camera.UserInput = context.view.UserInputSource;

            _spherePositions.Add(Vector3.Zero);
            _spherePositions.Add(new Vector3(0f, 2f, 0f));
            _spherePositions.Add(new Vector3(2f, 0f, 0f));

            UpdateSpheres();

            _device.SwapChain.SizeChanged += SwapChain_SizeChanged;
            SwapChain_SizeChanged(this, new SwapChainSizeChangedEventArgs(_device.SwapChain.Size));

            _renderPass = _device.CreateRenderPass(
                stackalloc RenderPassAttachment[]
                {
                    new RenderPassAttachment(AttachmentType.Color | AttachmentType.Present, _device.SwapChain.Format.colorFormat),
                    new RenderPassAttachment(AttachmentType.Depth, DataFormat.Depth24un, AttachmentLoadOperation.Clear, AttachmentStoreOperation.Undefined),
                },
                new RenderPassStep[] { new RenderPassStep(0u, 1u) });
            _device.SwapChain.RenderPass = _renderPass;

            _pipelineResourceLayout = _device.CreatePipelineResourceLayout(new PipelineResourceProperties(stackalloc PipelineResourceProperty[]
            {
                new PipelineResourceProperty(0u, PipelineResourceType.UniformBufferDynamic, GraphicsShaderStages.Vertex),
                new PipelineResourceProperty(1u, PipelineResourceType.UniformBufferDynamic, GraphicsShaderStages.Fragment),
                new PipelineResourceProperty(2u, PipelineResourceType.UniformBuffer, GraphicsShaderStages.Fragment),
                new PipelineResourceProperty(3u, PipelineResourceType.UniformBuffer, GraphicsShaderStages.Fragment),
                new PipelineResourceProperty(4u, PipelineResourceType.CombinedTextureSampler, GraphicsShaderStages.Fragment),
            }));

            using (GraphicsShaderPrograms shaderPrograms = _device.CompileShaderPrograms<TransformVertexShader, NormalsFragmentShader>())
                _pipeline = _device.CreatePipeline(new GraphicsPipelineConstuctionParameters(
                    shaders: shaderPrograms.ShaderPrograms,
                    geometryType: GeometryType.Triangles,
                    renderPass: _renderPass,
                    /*vertexInputs: new VertexInputs(
                        stackalloc VertexInputBinding[] { VertexInputBinding.Create<Vertex>() },
                        stackalloc VertexInputAttribute[]
                        {
                            new VertexInputAttribute(0u, 0u, DataFormat.RGB32f),
                            new VertexInputAttribute(1u, (uint)Marshal.SizeOf<Vector3>(), DataFormat.RGB32f),
                            new VertexInputAttribute(2u, (uint)Marshal.SizeOf<Vector3>() * 2u, DataFormat.RG32f),
                        }),*/
                    vertexInputs: new VertexInputs(
                        stackalloc VertexInputBinding[] { VertexInputBinding.Create<Vector3>(), VertexInputBinding.Create<Vector3>(), VertexInputBinding.Create<Vector2>() },
                        stackalloc VertexInputAttribute[]
                        {
                            new VertexInputAttribute(0u, 0u, 0u, DataFormat.RGB32f),
                            new VertexInputAttribute(1u, 1u, 0u, DataFormat.RGB32f),
                            new VertexInputAttribute(2u, 2u, 0u, DataFormat.RG32f),
                        }),
                    resourceLayouts: new PipelineResourceLayout[] { _pipelineResourceLayout },
                    depthUsage: DepthUsage.Enabled()
                ));


            GeometryData sphere = ParametricSurfaces.Generate(ParametricSurfaces.Sphere);
            using GraphicsCommandBuffer copyCommandBuffer = _device.CommandProcessors[0].CommandBufferFactory.CreateCommandBuffer();
            copyCommandBuffer.Begin();

            /*using IStagingDataBuffer<Vertex> stagingBuffer = _device.CreateStagingDataBuffer<Vertex>(DataBufferType.VertexData, sphere.vertices.Length);
            _vertexBuffer = _device.CreateDeviceOnlyDataBuffer<Vertex>(DataBufferType.VertexData | DataBufferType.Store, sphere.vertices.Length);
            _vertexBuffer.UseStagingBuffer(stagingBuffer);
            copyCommandBuffer.StoreData(_vertexBuffer, sphere.vertices);
            _vertexBuffer.ReleaseStagingBuffer();*/

            Vector3[] positions = new Vector3[sphere.vertices.Length];
            Vector3[] normals = new Vector3[sphere.vertices.Length];
            Vector2[] uvs = new Vector2[sphere.vertices.Length];
            for (int i = 0; i < sphere.vertices.Length; i++)
            {
                Vertex vertex = sphere.vertices[i];
                positions[i] = vertex.position;
                normals[i] = vertex.normal;
                uvs[i] = vertex.textureuv;
            }
            using IStagingDataBuffer stagingBuffer = _device.CreateStagingDataBuffer((ulong)sphere.vertices.Length * (ulong)Marshal.SizeOf<Vertex>());
            _vertexPositionBuffer = _device.CreateDeviceOnlyDataBuffer<Vector3>(DataBufferType.VertexData | DataBufferType.CopyDestination, sphere.vertices.Length);
            _vertexNormalBuffer = _device.CreateDeviceOnlyDataBuffer<Vector3>(DataBufferType.VertexData | DataBufferType.CopyDestination, sphere.vertices.Length);
            _vertexUVBuffer = _device.CreateDeviceOnlyDataBuffer<Vector2>(DataBufferType.VertexData | DataBufferType.CopyDestination, sphere.vertices.Length);
            _vertexBuffers = new IDataBuffer[] { _vertexPositionBuffer, _vertexNormalBuffer, _vertexUVBuffer };
            using PinnedObjectReference<Vector3> pinnedPositions = new PinnedObjectReference<Vector3>(positions);
            using PinnedObjectReference<Vector3> pinnedNormals = new PinnedObjectReference<Vector3>(normals);
            using PinnedObjectReference<Vector2> pinnedUVs = new PinnedObjectReference<Vector2>(uvs);
            ulong sizeofPositions = (ulong)sphere.vertices.Length * (ulong)Marshal.SizeOf<Vector3>();
            ulong sizeofUVs = (ulong)sphere.vertices.Length * (ulong)Marshal.SizeOf<Vector2>();
            copyCommandBuffer.StoreData(stagingBuffer, pinnedPositions.pointer, sizeofPositions);
            copyCommandBuffer.StoreData(stagingBuffer, pinnedNormals.pointer, sizeofPositions, sizeofPositions);
            copyCommandBuffer.StoreData(stagingBuffer, pinnedUVs.pointer, sizeofUVs, sizeofPositions * 2);
            copyCommandBuffer.CopyTo(stagingBuffer, _vertexPositionBuffer, sizeofPositions);
            copyCommandBuffer.CopyTo(stagingBuffer, _vertexNormalBuffer, sizeofPositions, sizeofPositions);
            copyCommandBuffer.CopyTo(stagingBuffer, _vertexUVBuffer, sizeofUVs, sizeofPositions * 2);

            _indexBuffer = _device.CreateDeviceOnlyDataBuffer<ushort>(DataBufferType.IndexData | DataBufferType.Store, sphere.indices.Length);
            copyCommandBuffer.StoreData(_indexBuffer, sphere.indices);

            _lightDataUBO = _device.CreateDeviceOnlyDataBuffer<LightData>(DataBufferType.UniformData | DataBufferType.Store, 1u);
            copyCommandBuffer.StoreData(_lightDataUBO, new LightData()
            {
                direction = Vector3.Normalize(new Vector3(-1f, -1f, 1f)),
                ambientColor = new Vector4(0.1f, 0.1f, 0.1f, 1f),
                diffuseColor = new Vector4(0.7f, 0.7f, 0.7f, 1f),
                specularColor = new Vector4(0.4f, 0.4f, 0.4f, 1f),
                specularPower = 16f,
            });


            DataFormat colorFormat = _device.SwapChain.Format.colorFormat.IsSRGB() ? DataFormat.RGBA8srgb : DataFormat.RGBA8un;
            Configuration.Default.PreferContiguousImageBuffers = true;
            _texture = _device.CreateTexture2D(colorFormat, new Vector2UInt(512u), TextureType.Store | TextureType.CopySource | TextureType.ShaderSample, MemoryType.DeviceOnly);
            using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("NormalsResources.earth.png"))
                if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                    copyCommandBuffer.StoreTextureData<Rgba32>(_texture, pixels, TextureLayout.ShaderReadOnly, 0u);
            _texture.GenerateMipmaps(copyCommandBuffer);

            _skyboxTexture = _device.CreateTextureCube(colorFormat, new Vector2UInt(1024u), TextureType.Store | TextureType.CopySource | TextureType.ShaderSample, MemoryType.DeviceOnly);
            using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("NormalsResources.xpos.png"))
                if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                    copyCommandBuffer.StoreTextureData<Rgba32>(_skyboxTexture, pixels, TextureLayout.ShaderReadOnly, CubeFace.XPositive, 0u);
            using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("NormalsResources.xneg.png"))
                if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                    copyCommandBuffer.StoreTextureData<Rgba32>(_skyboxTexture, pixels, TextureLayout.ShaderReadOnly, CubeFace.XNegative, 0u);
            using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("NormalsResources.ypos.png"))
                if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                    copyCommandBuffer.StoreTextureData<Rgba32>(_skyboxTexture, pixels, TextureLayout.ShaderReadOnly, CubeFace.YPositive, 0u);
            using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("NormalsResources.yneg.png"))
                if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                    copyCommandBuffer.StoreTextureData<Rgba32>(_skyboxTexture, pixels, TextureLayout.ShaderReadOnly, CubeFace.YNegative, 0u);
            using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("NormalsResources.zpos.png"))
                if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                    copyCommandBuffer.StoreTextureData<Rgba32>(_skyboxTexture, pixels, TextureLayout.ShaderReadOnly, CubeFace.ZPositive, 0u);
            using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("NormalsResources.zneg.png"))
                if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                    copyCommandBuffer.StoreTextureData<Rgba32>(_skyboxTexture, pixels, TextureLayout.ShaderReadOnly, CubeFace.ZNegative, 0u);
            _skyboxTexture.GenerateMipmaps(copyCommandBuffer);

            copyCommandBuffer.End();
            copyCommandBuffer.Submit();

            _sampler = _device.CreateTextureSampler(new TextureSamplerConstruction(TextureFilterType.Linear, TextureMipMapType.Linear, 16f));

            _frameResources = new BatchCreatedFrameResource<FrameResources>(_device.SwapChain, (uint count) =>
            {
                FrameResources[] resources = new FrameResources[count];
                PipelineResource[] pipelineResources = _pipelineResourceLayout.CreateResources(count);
                for (int i = 0; i < count; i++)
                {
                    resources[i] = new FrameResources(
                        _device.CreateMappableDataBuffer<Transform>(DataBufferType.UniformData | DataBufferType.Store, MappableMemoryType.DeviceLocal, 1u), //Count is 1 for testing reallocation during rendering
                        _device.CreateDeviceOnlyDataBuffer<MaterialData>(DataBufferType.UniformData | DataBufferType.Store, _materials.Length),
                        _device.CreateDeviceOnlyDataBuffer<SceneData>(DataBufferType.UniformData | DataBufferType.Store, 1u),
                        pipelineResources[i]);

                    pipelineResources[i].BindUniformBuffer(2u, _lightDataUBO);
                    pipelineResources[i].BindTexture(4u, _sampler, _texture);
                }
                return resources;
            });

            _device.WaitForIdle();
        }

        ~NormalsApp() => Dispose(false);

        #endregion

        #region Graphics Event Handlers

        private void SwapChain_SizeChanged(object? sender, SwapChainSizeChangedEventArgs e)
        {
            _camera.SetProjection(3.14f / 4.0f, e.Size.x / (float)e.Size.y, 1f, 1000f);
        }

        #endregion

        #region Private Methods

        [MemberNotNull("_transforms", "_materials")]
        private void UpdateSpheres()
        {
            if (_transforms == null || _transforms.Length != _spherePositions.Count)
                _transforms = new Transform[_spherePositions.Count];
            for (int i = 0; i < _transforms.Length; i++)
                _transforms[i] = new Transform(Matrix4x4.CreateTranslation(_spherePositions[i]), _camera.ViewProjectionMatrix);

            if (_materials == null || _materials.Length != _spherePositions.Count)
                _materials = new MaterialData[_spherePositions.Count];
            for (int i = 0; i < _materials.Length; i++)
                _materials[i] = new MaterialData(Vector4.One);
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
                ref FrameResources fr = ref _frameResources.Resource;

                fr.transformUBO.EnsureCapacity(_transforms.Length);
                commandBuffer.StoreData(fr.transformUBO, _transforms);
                commandBuffer.StoreData(fr.materialUBO, _materials);
                commandBuffer.StoreData(fr.sceneUBO, new SceneData() { cameraPosition = _camera.Eye });

                Vector4 clearColor = new Vector4(1f, 0.7f, 0f, 0f) * (MathF.Sin(_timers.TimeSeconds) * 0.5f + 0.5f);

                commandBuffer.BeginRenderPass(_renderPass, frameBuffer, stackalloc Vector4[] { clearColor, new Vector4(1f), });
                commandBuffer.BindPipeline(_pipeline);

                //commandBuffer.BindVertexBuffer(_vertexBuffer);
                commandBuffer.BindVertexBuffers(0u, _vertexBuffers);
                //commandBuffer.BindVertexBuffer(0u, _vertexPositionBuffer);
                //commandBuffer.BindVertexBuffer(1u, _vertexNormalBuffer);
                //commandBuffer.BindVertexBuffer(2u, _vertexUVBuffer);
                commandBuffer.BindIndexBuffer(_indexBuffer);

                for (int i = 0; i < _transforms.Length; i++)
                {
                    commandBuffer.BindResource(0u, fr.pipelineResource, i);

                    commandBuffer.DrawIndexed(_indexBuffer.Capacity);
                }

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
                Debug.WriteLine("Disposing App from " + (disposing ? "Dispose()" : "Finalizer") + "...");

                _device.WaitForIdle();

                _sampler?.Dispose();
                _skyboxTexture?.Dispose();
                _texture?.Dispose();
                _indexBuffer?.Dispose();
                //_vertexBuffer?.Dispose();
                _vertexPositionBuffer?.Dispose();
                _vertexNormalBuffer?.Dispose();
                _vertexUVBuffer?.Dispose();
                _pipeline?.Dispose();
                _renderPass?.Dispose();
                _lightDataUBO?.Dispose();
                _frameResources?.Dispose();
                _pipelineResourceLayout?.Dispose();

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public static NormalsApp Factory(in GraphicsModuleContext context) => new NormalsApp(context);

        #endregion

    }
}