using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics
{

    public abstract class GraphicsDeviceLimits
    {

        public uint UniformBufferAlignment { get; protected set; }
        public float MaxAnisotropy { get; protected set; }

        protected GraphicsDeviceLimits() { }

    }

    public readonly struct ShaderAPIVersion
    {
        public readonly ShaderSourceType type;
        public readonly string name;
        public ShaderAPIVersion(ShaderSourceType type, string name)
        {
            this.type = type;
            this.name = name;
        }
    }

    public abstract class GraphicsDeviceFeatures
    {

        public abstract IEnumerable<ShaderAPIVersion> ShaderAPIVersions { get; }

        public bool IsBufferPersistentMappingSupported { get; protected set; }
        public bool IsBufferCoherentMappingSupported { get; protected set; }
        public bool IsBufferCachedMappingSupported { get; protected set; }

        public bool IsTextureViewSupported { get; protected set; }

    }

    [Flags]
    public enum GraphicsCommandProcessorType : uint
    {
        Unknown = 0u,
        Graphics = 1u, Compute = 2u,
        Copy = 4u,
    }
    public abstract class GraphicsCommandProcessorGroupInfo
    {

        public GraphicsCommandProcessorType Type { get; }
        public uint Count { get; }

        protected GraphicsCommandProcessorGroupInfo(GraphicsCommandProcessorType type, uint count)
        {
            Type = type;
            Count = count;
        }

        public abstract bool IsViewSupported(IGraphicsView graphicsView);

    }

    public enum GraphicsDeviceType : uint
    {
        Unknown = 0u,
        IntegratedGPU = 1u, DiscreteGPU = 2u,
        VirtualGPU = 16u,
        CPU = 32u,
    }
    public abstract class GraphicsDeviceInfo
    {

        public abstract Version APIVersion { get; }
        public abstract Version DriverVersion { get; }
        public uint VendorID { get; protected set; }
        public uint DeviceID { get; protected set; }
        public GraphicsDeviceType Type { get; protected set; }
        public abstract string Name { get; }

        public abstract ReadOnlySpan<GraphicsCommandProcessorGroupInfo> CommandProcessorGroups { get; }
        public bool AreDetailsAvailable { get; protected set; }
        public bool IsPresentSupported { get; protected set; }
        public abstract GraphicsDeviceLimits Limits { get; }
        public abstract GraphicsDeviceFeatures Features { get; }

        public abstract uint[] GetCommandProcessorGroupIndicesSupportingView(IGraphicsView graphicsView);
        public abstract uint[] GetCommandProcessorGroupIndicesSupportingView(IGraphicsView graphicsView, GraphicsCommandProcessorType requiredType);

    }
}
