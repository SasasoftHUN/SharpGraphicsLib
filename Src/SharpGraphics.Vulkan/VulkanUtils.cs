using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{
    internal static class VulkanUtils
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetVKFunction<T>() where T : Delegate
            => GetVKFunction<T>(typeof(T).Name);
        public static T GetVKFunction<T>(string functionName) where T : Delegate
        {
            using RawString functionNameStr = new RawString(functionName);
            IntPtr functionPtr = VK.vkGetInstanceProcAddr(Vk.VkInstance.Null, functionNameStr);
            if (functionPtr != IntPtr.Zero)
                return Marshal.GetDelegateForFunctionPointer<T>(functionPtr);
            else throw new EntryPointNotFoundException($"vkGetInstanceProcAddr has not found instance independent function {functionName}!");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetVKFunction<T>(in Vk.VkInstance instance) where T : Delegate
            => GetVKFunction<T>(typeof(T).Name, instance);
        public static T GetVKFunction<T>(string functionName, in Vk.VkInstance instance) where T : Delegate
        {
            using RawString functionNameStr = new RawString(functionName);
            IntPtr functionPtr = VK.vkGetInstanceProcAddr(instance, functionNameStr);
            if (functionPtr != IntPtr.Zero)
                return Marshal.GetDelegateForFunctionPointer<T>(functionPtr);
            else throw new EntryPointNotFoundException($"vkGetInstanceProcAddr has not found instance function {functionName}!");
        }


        public static IEnumerable<string> GetAvailableExtensions(this in ReadOnlySpan<Vk.VkExtensionProperties> supportedExtensions, IEnumerable<VulkanGraphicsManagement.ExtensionRequest> requestedExtensions, out bool isPresentSupported)
        {
            List<string> extensions = new List<string>(requestedExtensions.Count());
            isPresentSupported = true;
            bool presentExtensionFound = false;

            foreach (VulkanGraphicsManagement.ExtensionRequest extension in requestedExtensions)
            {
                if (extension.mandatory == VulkanGraphicsManagement.FeatureRequestType.MandatoryOnlyForPresent)
                    presentExtensionFound = true;

                if (supportedExtensions.CheckExtensionAvailability(extension.name))
                    extensions.Add(extension.name);
                else if (extension.mandatory == VulkanGraphicsManagement.FeatureRequestType.Mandatory)
                    throw new Exception("Vulkan Mandatory Extension Unsupported: " + extension.name);
                else if (extension.mandatory == VulkanGraphicsManagement.FeatureRequestType.MandatoryOnlyForPresent)
                    isPresentSupported = false;
            }

            if (!presentExtensionFound)
                isPresentSupported = false;

            return extensions;
        }
        public static bool CheckExtensionAvailability(this in ReadOnlySpan<Vk.VkExtensionProperties> supportedExtensions, IEnumerable<string> requestedExtensions)
        {
            foreach (string extension in requestedExtensions)
                if (!supportedExtensions.CheckExtensionAvailability(extension))
                    return false;

            return true;
        }
        public static bool CheckExtensionAvailability(this in ReadOnlySpan<Vk.VkExtensionProperties> supportedExtensions, string requestedExtension)
        {
            for (int i = 0; i < supportedExtensions.Length; i++)
                if (supportedExtensions[i].extensionName.Trim('\0').StartsWith(requestedExtension)) //== fails on Android for VK_KHR_swapchain
                    return true;
                /*unsafe
                {
                    fixed (byte* pName = supportedExtensions[i].extensionName)
                        if (ParseByteString(pName) == requestedExtension)
                            return true;
                }*/

            Debug.WriteLine("Vulkan Extension Unsupported: " + requestedExtension);
            return false;
        }

        public static IEnumerable<string> GetAvailableLayers(this in ReadOnlySpan<Vk.VkLayerProperties> supportedLayers, IEnumerable<VulkanGraphicsManagement.LayerRequest> requestedLayers)
        {
            List<string> layers = new List<string>(requestedLayers.Count());

            foreach (VulkanGraphicsManagement.LayerRequest layer in requestedLayers)
                if (supportedLayers.CheckLayerAvailability(layer.name))
                    layers.Add(layer.name);
                else if (layer.mandatory)
                    throw new Exception("Vulkan Mandatory Layer Unsupported: " + layer.name);

            return layers;
        }
        public static bool CheckLayerAvailability(this in ReadOnlySpan<Vk.VkLayerProperties> supportedLayers, IEnumerable<string> requestedLayers)
        {
            foreach (string extension in requestedLayers)
                if (!supportedLayers.CheckLayerAvailability(extension))
                    return false;

            return true;
        }
        public static bool CheckLayerAvailability(this in ReadOnlySpan<Vk.VkLayerProperties> supportedLayers, string requestedLayer)
        {
            for (int i = 0; i < supportedLayers.Length; i++)
                if (supportedLayers[i].layerName.Trim('\0').StartsWith(requestedLayer))
                    return true;
                    /*unsafe
                    {
                        fixed (byte* pName = supportedLayers[i].layerName)
                            if (ParseByteString(pName) == requestedLayer)
                                return true;
                    }*/

            Debug.WriteLine("Vulkan Layer Unsupported: " + requestedLayer);
            return false;
        }


        public static GraphicsDeviceType ToGraphicsDeviceType(this Vk.VkPhysicalDeviceType type)
            => type switch
            {
                Vk.VkPhysicalDeviceType.IntegratedGpu => GraphicsDeviceType.IntegratedGPU,
                Vk.VkPhysicalDeviceType.DiscreteGpu => GraphicsDeviceType.DiscreteGPU,
                Vk.VkPhysicalDeviceType.VirtualGpu => GraphicsDeviceType.VirtualGPU,
                Vk.VkPhysicalDeviceType.Cpu => GraphicsDeviceType.CPU,
                _ => GraphicsDeviceType.Unknown,
            };
        public static Vk.VkQueueFlags ToVkQueueFlags(this GraphicsCommandProcessorType type)
        {
            Vk.VkQueueFlags result = 0u;

            if (type.HasFlag(GraphicsCommandProcessorType.Graphics)) result |= Vk.VkQueueFlags.Graphics;
            if (type.HasFlag(GraphicsCommandProcessorType.Compute)) result |= Vk.VkQueueFlags.Compute;
            if (type.HasFlag(GraphicsCommandProcessorType.Copy)) result |= Vk.VkQueueFlags.Transfer;

            return result;
        }
        public static GraphicsCommandProcessorType ToGraphicsCommandProcessorType(this Vk.VkQueueFlags queueFlags)
        {
            GraphicsCommandProcessorType result = 0u;

            if (queueFlags.HasFlag(Vk.VkQueueFlags.Graphics)) result |= GraphicsCommandProcessorType.Graphics;
            if (queueFlags.HasFlag(Vk.VkQueueFlags.Compute)) result |= GraphicsCommandProcessorType.Compute;
            if (queueFlags.HasFlag(Vk.VkQueueFlags.Transfer)) result |= GraphicsCommandProcessorType.Copy;

            return result;
        }


        public static Vk.VkPresentModeKHR ToVkPresentModeKHR(this PresentMode mode)
            => mode switch
            {
                PresentMode.Immediate => Vk.VkPresentModeKHR.ImmediateKHR,
                PresentMode.VSyncDoubleBuffer => Vk.VkPresentModeKHR.FifoKHR,
                PresentMode.AdaptiveDoubleBuffer => Vk.VkPresentModeKHR.FifoRelaxedKHR,
                PresentMode.VSyncTripleBuffer => Vk.VkPresentModeKHR.MailboxKHR,
                _ => Vk.VkPresentModeKHR.FifoKHR,
            };
        public static PresentMode ToPresentMode(this Vk.VkPresentModeKHR mode)
            => mode switch
            {
                Vk.VkPresentModeKHR.ImmediateKHR => PresentMode.Immediate,
                Vk.VkPresentModeKHR.MailboxKHR => PresentMode.VSyncTripleBuffer,
                Vk.VkPresentModeKHR.FifoKHR => PresentMode.VSyncDoubleBuffer,
                Vk.VkPresentModeKHR.FifoRelaxedKHR => PresentMode.AdaptiveDoubleBuffer,
                _ => PresentMode.Immediate,
            };

        public static Vk.VkMemoryPropertyFlags ToMemoryPropertyFlags(this MappableMemoryType type)
        {
            if (type == MappableMemoryType.DontCare)
                return Vk.VkMemoryPropertyFlags.HostVisible;
            else
            {
                Vk.VkMemoryPropertyFlags result = Vk.VkMemoryPropertyFlags.HostVisible;

                if (type.HasFlag(MappableMemoryType.DeviceLocal)) result |= Vk.VkMemoryPropertyFlags.DeviceLocal;
                if (type.HasFlag(MappableMemoryType.Coherent)) result |= Vk.VkMemoryPropertyFlags.HostCoherent;
                if (type.HasFlag(MappableMemoryType.Cached)) result |= Vk.VkMemoryPropertyFlags.HostCached;

                return result;
            }
        }

        public static Vk.VkAccessFlags GetEarliestUsageAccessFlags(this Vk.VkPipelineStageFlags earliestUsage)
            => earliestUsage switch
            {
                Vk.VkPipelineStageFlags.TopOfPipe => Vk.VkAccessFlags.IndirectCommandRead,
                Vk.VkPipelineStageFlags.DrawIndirect => Vk.VkAccessFlags.IndirectCommandRead,

                Vk.VkPipelineStageFlags.VertexInput => Vk.VkAccessFlags.VertexAttributeRead,


                Vk.VkPipelineStageFlags.VertexShader => Vk.VkAccessFlags.ShaderRead,
                Vk.VkPipelineStageFlags.TessellationControlShader => Vk.VkAccessFlags.ShaderRead,
                Vk.VkPipelineStageFlags.TessellationEvaluationShader => Vk.VkAccessFlags.ShaderRead,
                Vk.VkPipelineStageFlags.GeometryShader => Vk.VkAccessFlags.ShaderRead,
                Vk.VkPipelineStageFlags.FragmentShader => Vk.VkAccessFlags.ShaderRead,
                Vk.VkPipelineStageFlags.ComputeShader => Vk.VkAccessFlags.ShaderRead,
                Vk.VkPipelineStageFlags.RayTracingShaderNV => Vk.VkAccessFlags.ShaderRead,
                Vk.VkPipelineStageFlags.TaskShaderNV => Vk.VkAccessFlags.ShaderRead,
                Vk.VkPipelineStageFlags.MeshShaderNV => Vk.VkAccessFlags.ShaderRead,

                Vk.VkPipelineStageFlags.EarlyFragmentTests => Vk.VkAccessFlags.ShaderRead,
                Vk.VkPipelineStageFlags.LateFragmentTests => Vk.VkAccessFlags.ShaderRead,
                Vk.VkPipelineStageFlags.ColorAttachmentOutput => Vk.VkAccessFlags.ShaderRead,


                Vk.VkPipelineStageFlags.Transfer => Vk.VkAccessFlags.MemoryWrite,

                /*Vk.VkPipelineStageFlags.Host =>
                Vk.VkPipelineStageFlags.Reserved27KHR =>
                Vk.VkPipelineStageFlags.Reserved26KHR =>
                Vk.VkPipelineStageFlags.TransformFeedbackEXT =>
                Vk.VkPipelineStageFlags.ConditionalRenderingEXT =>
                Vk.VkPipelineStageFlags.CommandProcessNVX =>
                Vk.VkPipelineStageFlags.ShadingRateImageNV =>
                Vk.VkPipelineStageFlags.AccelerationStructureBuildNV =>
                Vk.VkPipelineStageFlags.FragmentDensityProcessEXT =>*/

                Vk.VkPipelineStageFlags.BottomOfPipe => 0u,

                Vk.VkPipelineStageFlags.AllGraphics => Vk.VkAccessFlags.IndirectCommandRead,
                Vk.VkPipelineStageFlags.AllCommands => Vk.VkAccessFlags.IndirectCommandRead,
                _ => Vk.VkAccessFlags.IndirectCommandRead,
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkCommandPoolCreateFlags ToVkCommandPoolCreateFlags(this CommandBufferFactoryProperties properties)
        {
            Vk.VkCommandPoolCreateFlags result = 0u;

            if (properties.HasFlag(CommandBufferFactoryProperties.ResettableBuffers)) result |= Vk.VkCommandPoolCreateFlags.ResetCommandBuffer;
            if (properties.HasFlag(CommandBufferFactoryProperties.ShortLivedBuffers)) result |= Vk.VkCommandPoolCreateFlags.Transient;

            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkCommandBufferLevel ToVkCommandBufferLevel(this CommandBufferLevel level)
            => level switch
            {
                CommandBufferLevel.Primary => Vk.VkCommandBufferLevel.Primary,
                CommandBufferLevel.Secondary => Vk.VkCommandBufferLevel.Secondary,
                _ => Vk.VkCommandBufferLevel.Primary,
            };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkCommandBufferResetFlags ToVkCommandBufferResetFlags(this GraphicsCommandBuffer.ResetOptions options)
        {
            Vk.VkCommandBufferResetFlags result = 0u;

            if (options.HasFlag(GraphicsCommandBuffer.ResetOptions.ReleaseResources)) result |= Vk.VkCommandBufferResetFlags.ReleaseResources;

            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkCommandBufferUsageFlags ToVkCommandBufferUsageFlags(this GraphicsCommandBuffer.BeginOptions options)
        {
            Vk.VkCommandBufferUsageFlags result = 0u;

            if (options.HasFlag(GraphicsCommandBuffer.BeginOptions.OneTimeSubmit)) result |= Vk.VkCommandBufferUsageFlags.OneTimeSubmit;
            if (options.HasFlag(GraphicsCommandBuffer.BeginOptions.SimultaneousMultiSubmit)) result |= Vk.VkCommandBufferUsageFlags.SimultaneousUse;

            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkSubpassContents ToVkSubpassContents(this CommandBufferLevel level)
            => level switch
            {
                CommandBufferLevel.Primary => Vk.VkSubpassContents.Inline,
                CommandBufferLevel.Secondary => Vk.VkSubpassContents.SecondaryCommandBuffers,
                _ => Vk.VkSubpassContents.Inline,
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkRect2D CreateRectFromSize(this Vk.VkExtent2D size)
            => new Vk.VkRect2D(new Vk.VkOffset2D(0, 0), size);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkViewport CreateViewportFromSize(this Vk.VkExtent2D size)
            => new Vk.VkViewport()
            {
                x = 0f,
                y = 0f, //Upper-Left Corner is Origin
                width = size.width,
                height = size.height,
                minDepth = 0f,
                maxDepth = 1f,
            };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkViewport CreateViewportFromRect(this Vk.VkRect2D rect)
            => new Vk.VkViewport()
            {
                x = rect.offset.x,
                y = rect.offset.y, //Upper-Left Corner is Origin
                width = rect.extent.width,
                height = rect.extent.height,
                minDepth = 0f,
                maxDepth = 1f,
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkViewport CreateViewportFromRect(this Rect rect)
            => new Vk.VkViewport()
            {
                x = rect.bottomLeft.X,
                y = rect.bottomLeft.Y - rect.size.Y, //Upper-Left Corner is Origin
                width = rect.size.X,
                height = rect.size.Y,
                minDepth = 0f,
                maxDepth = 1f,
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkRect2D ToVkRect2D(this Rect rect)
            => new Vk.VkRect2D(rect.bottomLeft.X.RoundToInt(), rect.bottomLeft.Y.RoundToInt(), rect.size.X.RoundToInt(), rect.size.Y.RoundToInt());


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkExtent2D ToVkExtent2D(this Vector2UInt vec2ui) => new Vk.VkExtent2D(vec2ui.x, vec2ui.y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2UInt ToVector2UInt(this Vk.VkExtent2D extent2D) => new Vector2UInt(extent2D.width, extent2D.height);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkExtent3D ToVkExtent3D(this Vector3UInt vec3ui) => new Vk.VkExtent3D() { width = vec3ui.x, height = vec3ui.y, depth = vec3ui.z };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3UInt ToVector3UInt(this Vk.VkExtent3D extent3D) => new Vector3UInt(extent3D.width, extent3D.height, extent3D.depth);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkOffset2D ToVkOffset2D(this Vector2UInt vec2ui) => new Vk.VkOffset2D() { x = (int)vec2ui.x, y = (int)vec2ui.y };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2UInt ToVector2UInt(this Vk.VkOffset2D offset2D) => new Vector2UInt((uint)offset2D.x, (uint)offset2D.y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkOffset3D ToVkOffset3D(this Vector3UInt vec3ui) => new Vk.VkOffset3D() { x = (int)vec3ui.x, y = (int)vec3ui.y, z = (int)vec3ui.z };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3UInt ToVector3UInt(this Vk.VkOffset3D offset3D) => new Vector3UInt((uint)offset3D.x, (uint)offset3D.y, (uint)offset3D.z);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkMemoryPropertyFlags ToVkMemoryPropertyFlags(this MemoryType type)
            => type.IsDeviceOnly ? Vk.VkMemoryPropertyFlags.DeviceLocal : type.MappableType.ToMemoryPropertyFlags();

        public static Vk.VkImageLayout ToVkImageLayout(this TextureLayout layout)
            => layout switch
            {
                TextureLayout.Undefined => Vk.VkImageLayout.Undefined,
                TextureLayout.General => Vk.VkImageLayout.General,
                TextureLayout.ColorAttachment => Vk.VkImageLayout.ColorAttachmentOptimal,
                TextureLayout.DepthStencilAttachment => Vk.VkImageLayout.DepthStencilAttachmentOptimal,
                TextureLayout.DepthStencilReadOnly => Vk.VkImageLayout.DepthStencilReadOnlyOptimal,
                TextureLayout.ShaderReadOnly => Vk.VkImageLayout.ShaderReadOnlyOptimal,
                TextureLayout.DepthAttachment => Vk.VkImageLayout.DepthAttachmentOptimal,
                TextureLayout.StencilAttachment => Vk.VkImageLayout.StencilAttachmentOptimal, //TODO: Seperate Depth-Stencil support?
                TextureLayout.PresentSrc => Vk.VkImageLayout.PresentSrcKHR,
                _ => Vk.VkImageLayout.Undefined,
            };
        public static Vk.VkImageLayout ToVkImageLayout(this AttachmentType type, bool seperateDepthStencilSupported)
        {
            if (type.HasFlag(AttachmentType.Present)) return Vk.VkImageLayout.PresentSrcKHR;
            if (type.HasFlag(AttachmentType.Color)) return Vk.VkImageLayout.ColorAttachmentOptimal;

            if (seperateDepthStencilSupported)
            {
                if (type.HasFlag(AttachmentType.Depth))
                    return type.HasFlag(AttachmentType.Stencil) ? Vk.VkImageLayout.DepthStencilAttachmentOptimal : Vk.VkImageLayout.DepthAttachmentOptimal;
                else if (type.HasFlag(AttachmentType.Stencil)) return Vk.VkImageLayout.StencilAttachmentOptimal;
            }
            else if (type.HasFlag(AttachmentType.Depth) || type.HasFlag(AttachmentType.Stencil))
                return Vk.VkImageLayout.DepthStencilAttachmentOptimal;

            if (type.HasFlag(AttachmentType.ShaderInput)) return Vk.VkImageLayout.ShaderReadOnlyOptimal;

            return Vk.VkImageLayout.Undefined;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkBufferImageCopy ToVkBufferImageCopy(this VulkanTexture destination, in CopyBufferTextureRange range)
            => new Vk.VkBufferImageCopy()
            {
                bufferOffset = range.bufferOffset,
                bufferRowLength = 0u,
                bufferImageHeight = 0u,

                imageSubresource = new Vk.VkImageSubresourceLayers()
                {
                    aspectMask = destination.VkUsage.ToVkImageAspectFlags(),
                    mipLevel = range.mipLevel,
                    baseArrayLayer = range.layers.start,
                    layerCount = range.layers.count != uint.MaxValue ? range.layers.count : (destination.Layers - range.layers.start),
                },
                imageOffset = range.textureOffset.ToVkOffset3D(),
                imageExtent = range.extent.ToVkExtent3D(),
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkSampleCountFlags ToVkSampleCount(this uint samples)
            => samples switch
            {
                1u => Vk.VkSampleCountFlags.SampleCount1,
                2u => Vk.VkSampleCountFlags.SampleCount2,
                4u => Vk.VkSampleCountFlags.SampleCount4,
                8u => Vk.VkSampleCountFlags.SampleCount8,
                16u => Vk.VkSampleCountFlags.SampleCount16,
                32u => Vk.VkSampleCountFlags.SampleCount32,
                64u => Vk.VkSampleCountFlags.SampleCount64,
                _ => Vk.VkSampleCountFlags.SampleCount1,
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkAttachmentLoadOp ToVkLoadOperation(this AttachmentLoadOperation loadOperation)
            => loadOperation switch
            {
                AttachmentLoadOperation.Undefined => Vk.VkAttachmentLoadOp.DontCare,
                AttachmentLoadOperation.Clear => Vk.VkAttachmentLoadOp.Clear,
                AttachmentLoadOperation.Load => Vk.VkAttachmentLoadOp.Load,
                _ => Vk.VkAttachmentLoadOp.DontCare,
            };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkAttachmentStoreOp ToVkStoreOperation(this AttachmentStoreOperation storeOperation)
            => storeOperation switch
            {
                AttachmentStoreOperation.Undefined => Vk.VkAttachmentStoreOp.DontCare,
                AttachmentStoreOperation.Store => Vk.VkAttachmentStoreOp.Store,
                _ => Vk.VkAttachmentStoreOp.DontCare,
            };

        public static Vk.VkBlendFactor ToVkBlendFactor(this BlendFactor factor)
            => factor switch
            {
                BlendFactor.Zero => Vk.VkBlendFactor.Zero,
                BlendFactor.One => Vk.VkBlendFactor.One,
                BlendFactor.SourceColor => Vk.VkBlendFactor.SrcColor,
                BlendFactor.OneMinusSourceColor => Vk.VkBlendFactor.OneMinusSrcColor,
                BlendFactor.DestinationColor => Vk.VkBlendFactor.DstColor,
                BlendFactor.OneMinusDestinationColor => Vk.VkBlendFactor.OneMinusDstColor,
                BlendFactor.SourceAlpha => Vk.VkBlendFactor.SrcAlpha,
                BlendFactor.OneMinusSourceAlpha => Vk.VkBlendFactor.OneMinusSrcAlpha,
                BlendFactor.DestinationAlpha => Vk.VkBlendFactor.DstAlpha,
                BlendFactor.OneMinusDestinationAlpha => Vk.VkBlendFactor.OneMinusDstAlpha,
                BlendFactor.ConstantColor => Vk.VkBlendFactor.ConstantColor,
                BlendFactor.OneMinusConstantColor => Vk.VkBlendFactor.OneMinusConstantColor,
                BlendFactor.ConstantAlpha => Vk.VkBlendFactor.ConstantAlpha,
                BlendFactor.OneMinusConstantAlpha => Vk.VkBlendFactor.OneMinusConstantAlpha,
                BlendFactor.SourceAlphaSaturate => Vk.VkBlendFactor.SrcAlphaSaturate,
                BlendFactor.DualSourceColor => Vk.VkBlendFactor.Src1Color,
                BlendFactor.OneMinusDualSourceColor => Vk.VkBlendFactor.OneMinusSrc1Color,
                BlendFactor.DualSourceAlpha => Vk.VkBlendFactor.Src1Alpha,
                BlendFactor.OneMinusDualSourceAlpha => Vk.VkBlendFactor.OneMinusSrc1Alpha,
                _ => Vk.VkBlendFactor.Zero,
            };
        public static Vk.VkBlendOp ToVkBlendOp(this BlendOperation operation)
            => operation switch
            {
                BlendOperation.Add => Vk.VkBlendOp.Add,
                BlendOperation.Subtract => Vk.VkBlendOp.Subtract,
                BlendOperation.ReverseSubtract => Vk.VkBlendOp.ReverseSubtract,
                BlendOperation.Min => Vk.VkBlendOp.Min,
                BlendOperation.Max => Vk.VkBlendOp.Max,
                _ => Vk.VkBlendOp.Add,
            };
        public static Vk.VkColorComponentFlags ToVkColorComponentFlags(this ColorComponents colorComponents)
        {
            Vk.VkColorComponentFlags result = 0u;

            if (colorComponents.HasFlag(ColorComponents.Red)) result |= Vk.VkColorComponentFlags.R;
            if (colorComponents.HasFlag(ColorComponents.Green)) result |= Vk.VkColorComponentFlags.G;
            if (colorComponents.HasFlag(ColorComponents.Blue)) result |= Vk.VkColorComponentFlags.B;
            if (colorComponents.HasFlag(ColorComponents.Alpha)) result |= Vk.VkColorComponentFlags.A;

            return result;
        }
        public static Vk.VkPipelineColorBlendAttachmentState ToVkPipelineColorBlendAttachmentState(this in ColorAttachmentUsage usage)
        {
            if (usage.blend.HasValue)
            {
                BlendAttachment blend = usage.blend.Value;
                return new Vk.VkPipelineColorBlendAttachmentState()
                {
                    blendEnable = Vk.VkBool32.True,
                    srcColorBlendFactor = blend.sourceColorBlendFactor.ToVkBlendFactor(),
                    dstColorBlendFactor = blend.destinationColorBlendFactor.ToVkBlendFactor(),
                    colorBlendOp = blend.colorBlendOperation.ToVkBlendOp(),
                    srcAlphaBlendFactor = blend.sourceAlphaBlendFactor.ToVkBlendFactor(),
                    dstAlphaBlendFactor = blend.destinationAlphaBlendFactor.ToVkBlendFactor(),
                    alphaBlendOp = blend.alphaBlendOperation.ToVkBlendOp(),
                    colorWriteMask = usage.colorWriteMask.ToVkColorComponentFlags(),
                };
            }
            else return new Vk.VkPipelineColorBlendAttachmentState()
            {
                blendEnable = Vk.VkBool32.False,
                srcColorBlendFactor = Vk.VkBlendFactor.One,
                dstColorBlendFactor = Vk.VkBlendFactor.Zero,
                colorBlendOp = Vk.VkBlendOp.Add,
                srcAlphaBlendFactor = Vk.VkBlendFactor.One,
                dstAlphaBlendFactor = Vk.VkBlendFactor.Zero,
                alphaBlendOp = Vk.VkBlendOp.Add,
                colorWriteMask = usage.colorWriteMask.ToVkColorComponentFlags(),
            };
        }

        public static DataFormat ToDataFormat(this Vk.VkFormat format)
            => format switch
            {
                Vk.VkFormat.Undefined => DataFormat.Undefined,

                Vk.VkFormat.R8Unorm => DataFormat.R8un,
                Vk.VkFormat.R8Snorm => DataFormat.R8n,
                Vk.VkFormat.R8Uscaled => DataFormat.R8us,
                Vk.VkFormat.R8Sscaled => DataFormat.R8s,
                Vk.VkFormat.R8Uint => DataFormat.R8ui,
                Vk.VkFormat.R8Sint => DataFormat.R8i,
                Vk.VkFormat.R8Srgb => DataFormat.R8srgb,

                Vk.VkFormat.R8g8Unorm => DataFormat.RG8un,
                Vk.VkFormat.R8g8Snorm => DataFormat.RG8n,
                Vk.VkFormat.R8g8Uscaled => DataFormat.RG8us,
                Vk.VkFormat.R8g8Sscaled => DataFormat.RG8s,
                Vk.VkFormat.R8g8Uint => DataFormat.RG8ui,
                Vk.VkFormat.R8g8Sint => DataFormat.RG8i,
                Vk.VkFormat.R8g8Srgb => DataFormat.RG8srgb,

                Vk.VkFormat.R8g8b8Unorm => DataFormat.RGB8un,
                Vk.VkFormat.R8g8b8Snorm => DataFormat.RGB8n,
                Vk.VkFormat.R8g8b8Uscaled => DataFormat.RGB8us,
                Vk.VkFormat.R8g8b8Sscaled => DataFormat.RGB8s,
                Vk.VkFormat.R8g8b8Uint => DataFormat.RGB8ui,
                Vk.VkFormat.R8g8b8Sint => DataFormat.RGB8i,
                Vk.VkFormat.R8g8b8Srgb => DataFormat.RGB8srgb,
                Vk.VkFormat.B8g8r8Unorm => DataFormat.BGR8un,
                Vk.VkFormat.B8g8r8Snorm => DataFormat.BGR8n,
                Vk.VkFormat.B8g8r8Uscaled => DataFormat.BGR8us,
                Vk.VkFormat.B8g8r8Sscaled => DataFormat.BGR8s,
                Vk.VkFormat.B8g8r8Uint => DataFormat.BGR8ui,
                Vk.VkFormat.B8g8r8Sint => DataFormat.BGR8i,
                Vk.VkFormat.B8g8r8Srgb => DataFormat.BGR8srgb,

                Vk.VkFormat.R8g8b8a8Unorm => DataFormat.RGBA8un,
                Vk.VkFormat.R8g8b8a8Snorm => DataFormat.RGBA8n,
                Vk.VkFormat.R8g8b8a8Uscaled => DataFormat.RGBA8us,
                Vk.VkFormat.R8g8b8a8Sscaled => DataFormat.RGBA8s,
                Vk.VkFormat.R8g8b8a8Uint => DataFormat.RGBA8ui,
                Vk.VkFormat.R8g8b8a8Sint => DataFormat.RGBA8i,
                Vk.VkFormat.R8g8b8a8Srgb => DataFormat.RGBA8srgb,
                Vk.VkFormat.B8g8r8a8Unorm => DataFormat.BGRA8un,
                Vk.VkFormat.B8g8r8a8Snorm => DataFormat.BGRA8n,
                Vk.VkFormat.B8g8r8a8Uscaled => DataFormat.BGRA8us,
                Vk.VkFormat.B8g8r8a8Sscaled => DataFormat.BGRA8s,
                Vk.VkFormat.B8g8r8a8Uint => DataFormat.BGRA8ui,
                Vk.VkFormat.B8g8r8a8Sint => DataFormat.BGRA8i,
                Vk.VkFormat.B8g8r8a8Srgb => DataFormat.BGRA8srgb,


                Vk.VkFormat.R16Unorm => DataFormat.R16un,
                Vk.VkFormat.R16Snorm => DataFormat.R16n,
                Vk.VkFormat.R16Uscaled => DataFormat.R16us,
                Vk.VkFormat.R16Sscaled => DataFormat.R16s,
                Vk.VkFormat.R16Uint => DataFormat.R16ui,
                Vk.VkFormat.R16Sint => DataFormat.R16i,
                Vk.VkFormat.R16Sfloat => DataFormat.R16f,

                Vk.VkFormat.R16g16Unorm => DataFormat.RG16un,
                Vk.VkFormat.R16g16Snorm => DataFormat.RG16n,
                Vk.VkFormat.R16g16Uscaled => DataFormat.RG16us,
                Vk.VkFormat.R16g16Sscaled => DataFormat.RG16s,
                Vk.VkFormat.R16g16Uint => DataFormat.RG16ui,
                Vk.VkFormat.R16g16Sint => DataFormat.RG16i,
                Vk.VkFormat.R16g16Sfloat => DataFormat.RG16f,

                Vk.VkFormat.R16g16b16Unorm => DataFormat.RGB16un,
                Vk.VkFormat.R16g16b16Snorm => DataFormat.RGB16n,
                Vk.VkFormat.R16g16b16Uscaled => DataFormat.RGB16us,
                Vk.VkFormat.R16g16b16Sscaled => DataFormat.RGB16s,
                Vk.VkFormat.R16g16b16Uint => DataFormat.RGB16ui,
                Vk.VkFormat.R16g16b16Sint => DataFormat.RGB16i,
                Vk.VkFormat.R16g16b16Sfloat => DataFormat.RGB16f,

                Vk.VkFormat.R16g16b16a16Unorm => DataFormat.RGBA16un,
                Vk.VkFormat.R16g16b16a16Snorm => DataFormat.RGBA16n,
                Vk.VkFormat.R16g16b16a16Uscaled => DataFormat.RGBA16us,
                Vk.VkFormat.R16g16b16a16Sscaled => DataFormat.RGBA16s,
                Vk.VkFormat.R16g16b16a16Uint => DataFormat.RGBA16ui,
                Vk.VkFormat.R16g16b16a16Sint => DataFormat.RGBA16i,
                Vk.VkFormat.R16g16b16a16Sfloat => DataFormat.RGBA16f,


                Vk.VkFormat.R32Uint => DataFormat.R32ui,
                Vk.VkFormat.R32Sint => DataFormat.R32i,
                Vk.VkFormat.R32Sfloat => DataFormat.R32f,

                Vk.VkFormat.R32g32Uint => DataFormat.RG32ui,
                Vk.VkFormat.R32g32Sint => DataFormat.RG32i,
                Vk.VkFormat.R32g32Sfloat => DataFormat.RG32f,

                Vk.VkFormat.R32g32b32Uint => DataFormat.RGB32ui,
                Vk.VkFormat.R32g32b32Sint => DataFormat.RGB32i,
                Vk.VkFormat.R32g32b32Sfloat => DataFormat.RGB32f,

                Vk.VkFormat.R32g32b32a32Uint => DataFormat.RGBA32ui,
                Vk.VkFormat.R32g32b32a32Sint => DataFormat.RGBA32i,
                Vk.VkFormat.R32g32b32a32Sfloat => DataFormat.RGBA32f,


                Vk.VkFormat.R64Uint => DataFormat.R64ui,
                Vk.VkFormat.R64Sint => DataFormat.R64i,
                Vk.VkFormat.R64Sfloat => DataFormat.R64f,

                Vk.VkFormat.R64g64Uint => DataFormat.RG64ui,
                Vk.VkFormat.R64g64Sint => DataFormat.RG64i,
                Vk.VkFormat.R64g64Sfloat => DataFormat.RG64f,

                Vk.VkFormat.R64g64b64Uint => DataFormat.RGB64ui,
                Vk.VkFormat.R64g64b64Sint => DataFormat.RGB64i,
                Vk.VkFormat.R64g64b64Sfloat => DataFormat.RGB64f,

                Vk.VkFormat.R64g64b64a64Uint => DataFormat.RGBA64ui,
                Vk.VkFormat.R64g64b64a64Sint => DataFormat.RGBA64i,
                Vk.VkFormat.R64g64b64a64Sfloat => DataFormat.RGBA64f,

                Vk.VkFormat.D16Unorm => DataFormat.Depth16un,
                Vk.VkFormat.X8D24UnormPack32 => DataFormat.Depth24un,
                Vk.VkFormat.D32Sfloat => DataFormat.Depth32f,
                Vk.VkFormat.S8Uint => DataFormat.Stencil8ui,
                Vk.VkFormat.D16UnormS8Uint => DataFormat.Depth16un_Stencil8ui,
                Vk.VkFormat.D24UnormS8Uint => DataFormat.Depth24un_Stencil8ui,
                Vk.VkFormat.D32SfloatS8Uint => DataFormat.Depth32f_Stencil8ui,

                _ => DataFormat.Undefined,
            };
        public static Vk.VkFormat ToVkFormat(this DataFormat format)
            => format switch
            {
                DataFormat.Undefined => Vk.VkFormat.Undefined,

                DataFormat.R8un => Vk.VkFormat.R8Unorm,
                DataFormat.R8n => Vk.VkFormat.R8Snorm,
                DataFormat.R8us => Vk.VkFormat.R8Uscaled,
                DataFormat.R8s => Vk.VkFormat.R8Sscaled,
                DataFormat.R8ui => Vk.VkFormat.R8Uint,
                DataFormat.R8i => Vk.VkFormat.R8Sint,
                DataFormat.R8srgb => Vk.VkFormat.R8Srgb,

                DataFormat.RG8un => Vk.VkFormat.R8g8Unorm,
                DataFormat.RG8n => Vk.VkFormat.R8g8Snorm,
                DataFormat.RG8us => Vk.VkFormat.R8g8Uscaled,
                DataFormat.RG8s => Vk.VkFormat.R8g8Sscaled,
                DataFormat.RG8ui => Vk.VkFormat.R8g8Uint,
                DataFormat.RG8i => Vk.VkFormat.R8g8Sint,
                DataFormat.RG8srgb => Vk.VkFormat.R8g8Srgb,

                DataFormat.RGB8un => Vk.VkFormat.R8g8b8Unorm,
                DataFormat.RGB8n => Vk.VkFormat.R8g8b8Snorm,
                DataFormat.RGB8us => Vk.VkFormat.R8g8b8Uscaled,
                DataFormat.RGB8s => Vk.VkFormat.R8g8b8Sscaled,
                DataFormat.RGB8ui => Vk.VkFormat.R8g8b8Uint,
                DataFormat.RGB8i => Vk.VkFormat.R8g8b8Sint,
                DataFormat.RGB8srgb => Vk.VkFormat.R8g8b8Srgb,
                DataFormat.BGR8un => Vk.VkFormat.B8g8r8Unorm,
                DataFormat.BGR8n => Vk.VkFormat.B8g8r8Snorm,
                DataFormat.BGR8us => Vk.VkFormat.B8g8r8Uscaled,
                DataFormat.BGR8s => Vk.VkFormat.B8g8r8Sscaled,
                DataFormat.BGR8ui => Vk.VkFormat.B8g8r8Uint,
                DataFormat.BGR8i => Vk.VkFormat.B8g8r8Sint,
                DataFormat.BGR8srgb => Vk.VkFormat.B8g8r8Srgb,

                DataFormat.RGBA8un => Vk.VkFormat.R8g8b8a8Unorm,
                DataFormat.RGBA8n => Vk.VkFormat.R8g8b8a8Snorm,
                DataFormat.RGBA8us => Vk.VkFormat.R8g8b8a8Uscaled,
                DataFormat.RGBA8s => Vk.VkFormat.R8g8b8a8Sscaled,
                DataFormat.RGBA8ui => Vk.VkFormat.R8g8b8a8Uint,
                DataFormat.RGBA8i => Vk.VkFormat.R8g8b8a8Sint,
                DataFormat.RGBA8srgb => Vk.VkFormat.R8g8b8a8Srgb,
                DataFormat.BGRA8un => Vk.VkFormat.B8g8r8a8Unorm,
                DataFormat.BGRA8n => Vk.VkFormat.B8g8r8a8Snorm,
                DataFormat.BGRA8us => Vk.VkFormat.B8g8r8a8Uscaled,
                DataFormat.BGRA8s => Vk.VkFormat.B8g8r8a8Sscaled,
                DataFormat.BGRA8ui => Vk.VkFormat.B8g8r8a8Uint,
                DataFormat.BGRA8i => Vk.VkFormat.B8g8r8a8Sint,
                DataFormat.BGRA8srgb => Vk.VkFormat.B8g8r8a8Srgb,


                DataFormat.R16un => Vk.VkFormat.R16Unorm,
                DataFormat.R16n => Vk.VkFormat.R16Snorm,
                DataFormat.R16us => Vk.VkFormat.R16Uscaled,
                DataFormat.R16s => Vk.VkFormat.R16Sscaled,
                DataFormat.R16ui => Vk.VkFormat.R16Uint,
                DataFormat.R16i => Vk.VkFormat.R16Sint,
                DataFormat.R16f => Vk.VkFormat.R16Sfloat,

                DataFormat.RG16un => Vk.VkFormat.R16g16Unorm,
                DataFormat.RG16n => Vk.VkFormat.R16g16Snorm,
                DataFormat.RG16us => Vk.VkFormat.R16g16Uscaled,
                DataFormat.RG16s => Vk.VkFormat.R16g16Sscaled,
                DataFormat.RG16ui => Vk.VkFormat.R16g16Uint,
                DataFormat.RG16i => Vk.VkFormat.R16g16Sint,
                DataFormat.RG16f => Vk.VkFormat.R16g16Sfloat,

                DataFormat.RGB16un => Vk.VkFormat.R16g16b16Unorm,
                DataFormat.RGB16n => Vk.VkFormat.R16g16b16Snorm,
                DataFormat.RGB16us => Vk.VkFormat.R16g16b16Uscaled,
                DataFormat.RGB16s => Vk.VkFormat.R16g16b16Sscaled,
                DataFormat.RGB16ui => Vk.VkFormat.R16g16b16Uint,
                DataFormat.RGB16i => Vk.VkFormat.R16g16b16Sint,
                DataFormat.RGB16f => Vk.VkFormat.R16g16b16Sfloat,

                DataFormat.RGBA16un => Vk.VkFormat.R16g16b16a16Unorm,
                DataFormat.RGBA16n => Vk.VkFormat.R16g16b16a16Snorm,
                DataFormat.RGBA16us => Vk.VkFormat.R16g16b16a16Uscaled,
                DataFormat.RGBA16s => Vk.VkFormat.R16g16b16a16Sscaled,
                DataFormat.RGBA16ui => Vk.VkFormat.R16g16b16a16Uint,
                DataFormat.RGBA16i => Vk.VkFormat.R16g16b16a16Sint,
                DataFormat.RGBA16f => Vk.VkFormat.R16g16b16a16Sfloat,


                DataFormat.R32ui => Vk.VkFormat.R32Uint,
                DataFormat.R32i => Vk.VkFormat.R32Sint,
                DataFormat.R32f => Vk.VkFormat.R32Sfloat,

                DataFormat.RG32ui => Vk.VkFormat.R32g32Uint,
                DataFormat.RG32i => Vk.VkFormat.R32g32Sint,
                DataFormat.RG32f => Vk.VkFormat.R32g32Sfloat,

                DataFormat.RGB32ui => Vk.VkFormat.R32g32b32Uint,
                DataFormat.RGB32i => Vk.VkFormat.R32g32b32Sint,
                DataFormat.RGB32f => Vk.VkFormat.R32g32b32Sfloat,

                DataFormat.RGBA32ui => Vk.VkFormat.R32g32b32a32Uint,
                DataFormat.RGBA32i => Vk.VkFormat.R32g32b32a32Sint,
                DataFormat.RGBA32f => Vk.VkFormat.R32g32b32a32Sfloat,


                DataFormat.R64ui => Vk.VkFormat.R64Uint,
                DataFormat.R64i => Vk.VkFormat.R64Sint,
                DataFormat.R64f => Vk.VkFormat.R64Sfloat,

                DataFormat.RG64ui => Vk.VkFormat.R64g64Uint,
                DataFormat.RG64i => Vk.VkFormat.R64g64Sint,
                DataFormat.RG64f => Vk.VkFormat.R64g64Sfloat,

                DataFormat.RGB64ui => Vk.VkFormat.R64g64b64Uint,
                DataFormat.RGB64i => Vk.VkFormat.R64g64b64Sint,
                DataFormat.RGB64f => Vk.VkFormat.R64g64b64Sfloat,

                DataFormat.RGBA64ui => Vk.VkFormat.R64g64b64a64Uint,
                DataFormat.RGBA64i => Vk.VkFormat.R64g64b64a64Sint,
                DataFormat.RGBA64f => Vk.VkFormat.R64g64b64a64Sfloat,

                DataFormat.Depth16un => Vk.VkFormat.D16Unorm,
                DataFormat.Depth24un => Vk.VkFormat.X8D24UnormPack32,
                DataFormat.Depth32f => Vk.VkFormat.D32Sfloat,
                DataFormat.Stencil8ui => Vk.VkFormat.S8Uint,
                DataFormat.Depth16un_Stencil8ui => Vk.VkFormat.D16UnormS8Uint,
                DataFormat.Depth24un_Stencil8ui => Vk.VkFormat.D24UnormS8Uint,
                DataFormat.Depth32f_Stencil8ui => Vk.VkFormat.D32SfloatS8Uint,

                _ => Vk.VkFormat.Undefined,
            };



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkShaderStageFlags ToVkShaderStageFlags(this GraphicsShaderStages shader)
            => shader switch
	        {
                GraphicsShaderStages.Vertex => Vk.VkShaderStageFlags.Vertex,
                //TODO: Implement for Geometry Shader
                //GraphicsShaderStages.Geometry => Vk.VkShaderStageFlags.Geometry,
                //TODO: Implement for Tessellation Shader
                //GraphicsShaderStages.TessellationControl => Vk.VkShaderStageFlags.TessellationControl,
                //GraphicsShaderStages.TessellationEvaluation => Vk.VkShaderStageFlags.TessellationEvaluation,
                GraphicsShaderStages.Fragment => Vk.VkShaderStageFlags.Fragment,
                _ => Vk.VkShaderStageFlags.AllGraphics,
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkPrimitiveTopology ToVkPrimitiveTopology(this GeometryType type)
            => type switch
            {
                GeometryType.Points => Vk.VkPrimitiveTopology.PointList,

                GeometryType.Lines => Vk.VkPrimitiveTopology.LineList,
                GeometryType.LineStrip => Vk.VkPrimitiveTopology.LineStrip,

                GeometryType.Triangles => Vk.VkPrimitiveTopology.TriangleList,
                GeometryType.TriangleStrip => Vk.VkPrimitiveTopology.TriangleStrip,
                GeometryType.TriangleFan => Vk.VkPrimitiveTopology.TriangleFan,

                //TODO: Implement for Geometry Shader
                //GeometryType.LinesAdjacency => Vk.VkPrimitiveTopology.LineListWithAdjacency,
                //GeometryType.LineStripAdjacency => Vk.VkPrimitiveTopology.LineStripWithAdjacency,
                //GeometryType.TrianglesAdjacency => Vk.VkPrimitiveTopology.TriangleListWithAdjacency,
                //GeometryType.TriangleStripAdjacency => Vk.VkPrimitiveTopology.TriangleStripWithAdjacency,

                //TODO: Implement for Tessellation Shader
                //GeometryType.Patches => Vk.VkPrimitiveTopology.PatchList,

                _ => Vk.VkPrimitiveTopology.TriangleList,
            };
        public static Vk.VkPolygonMode ToVkPolygonMode(this PolygonMode polygonMode)
            => polygonMode switch
            {
                PolygonMode.Fill => Vk.VkPolygonMode.Fill,
                PolygonMode.Line => Vk.VkPolygonMode.Line,
                PolygonMode.Point => Vk.VkPolygonMode.Point,
                _ => Vk.VkPolygonMode.Fill,
            };
        public static Vk.VkCullModeFlags ToVkCullModeFlags(this CullMode cullMode)
        {
            Vk.VkCullModeFlags result = 0u;

            if (cullMode.HasFlag(CullMode.Front)) result |= Vk.VkCullModeFlags.Front;
            if (cullMode.HasFlag(CullMode.Back)) result |= Vk.VkCullModeFlags.Back;

            return result;
        }
        public static Vk.VkFrontFace ToVkFrontFace(this WindingOrder windingOrder)
            => windingOrder switch
            {
                WindingOrder.CounterClockwise => Vk.VkFrontFace.CounterClockwise,
                WindingOrder.Clockwise => Vk.VkFrontFace.Clockwise,
                _ => Vk.VkFrontFace.CounterClockwise,
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkIndexType ToVkIndexType(this GraphicsCommandBuffer.IndexType indexType)
            => indexType switch
            {
                GraphicsCommandBuffer.IndexType.UnsignedShort => Vk.VkIndexType.Uint16,
                GraphicsCommandBuffer.IndexType.UnsignedInt => Vk.VkIndexType.Uint32,
                _ => Vk.VkIndexType.Uint16,
            };

        public static Vk.VkBufferUsageFlags ToVkBufferUsageFlags(this DataBufferType type, bool isDeviceOnly)
        {
            Vk.VkBufferUsageFlags result = 0u;

            if (isDeviceOnly)
            {
                if (type.HasFlag(DataBufferType.Read)) result |= Vk.VkBufferUsageFlags.TransferSrc;
                if (type.HasFlag(DataBufferType.Store)) result |= Vk.VkBufferUsageFlags.TransferDst;
            }

            if (type.HasFlag(DataBufferType.CopySource)) result |= Vk.VkBufferUsageFlags.TransferSrc;
            if (type.HasFlag(DataBufferType.CopyDestination)) result |= Vk.VkBufferUsageFlags.TransferDst;

            if (type.HasFlag(DataBufferType.VertexData)) result |= Vk.VkBufferUsageFlags.VertexBuffer;
            if (type.HasFlag(DataBufferType.IndexData)) result |= Vk.VkBufferUsageFlags.IndexBuffer;

            if (type.HasFlag(DataBufferType.UniformData)) result |= Vk.VkBufferUsageFlags.UniformBuffer;

            return result;
        }
        public static Vk.VkPipelineStageFlags ToPipelineStageFlags(this DataBufferType type)
        {
            Vk.VkPipelineStageFlags result = 0u;

            if (type.HasFlag(DataBufferType.CopySource) || type.HasFlag(DataBufferType.CopyDestination)) result |= Vk.VkPipelineStageFlags.Transfer;

            if (type.HasFlag(DataBufferType.VertexData) || type.HasFlag(DataBufferType.IndexData)) result |= Vk.VkPipelineStageFlags.VertexInput;

            if (type.HasFlag(DataBufferType.UniformData)) result |= Vk.VkPipelineStageFlags.VertexShader; //TODO: Additional hints to have better sync with shader stages

            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkVertexInputRate ToVkVertexInputRate(this VertexInputRate rate)
            => rate switch
            {
                VertexInputRate.Vertex => Vk.VkVertexInputRate.Vertex,
                VertexInputRate.Instance => Vk.VkVertexInputRate.Instance,

                _ => Vk.VkVertexInputRate.Vertex,
            };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkVertexInputBindingDescription ToVkVertexInputBindingDescription(this in VertexInputBinding binding, uint index)
            => new Vk.VkVertexInputBindingDescription(index, binding.stride, binding.rate.ToVkVertexInputRate());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkVertexInputAttributeDescription ToVertexInputAttributeDescription(this in VertexInputAttribute attribute)
            => new Vk.VkVertexInputAttributeDescription(attribute.binding, attribute.location, attribute.format.ToVkFormat(), attribute.offset);

        public static Vk.VkDescriptorType ToVkDescriptorType(this PipelineResourceType type)
            => type switch
            {
                PipelineResourceType.CombinedTextureSampler => Vk.VkDescriptorType.CombinedImageSampler,
                PipelineResourceType.UniformBuffer => Vk.VkDescriptorType.UniformBuffer,
                PipelineResourceType.UniformBufferDynamic => Vk.VkDescriptorType.UniformBufferDynamic,
                PipelineResourceType.InputAttachment => Vk.VkDescriptorType.InputAttachment,

                _ => Vk.VkDescriptorType.Sampler,
            };

        public static Vk.VkCompareOp ToVkCompareOp(this ComparisonType comparison)
            => comparison switch
            {
                ComparisonType.Never => Vk.VkCompareOp.Never,
                ComparisonType.Less => Vk.VkCompareOp.Less,
                ComparisonType.Equal => Vk.VkCompareOp.Equal,
                ComparisonType.LessOrEqual => Vk.VkCompareOp.LessOrEqual,
                ComparisonType.Greater => Vk.VkCompareOp.Greater,
                ComparisonType.NotEqual => Vk.VkCompareOp.NotEqual,
                ComparisonType.GreaterOrEqual => Vk.VkCompareOp.GreaterOrEqual,
                ComparisonType.Always => Vk.VkCompareOp.Always,
                _ => Vk.VkCompareOp.Never,
            };

        public static Vk.VkImageUsageFlags ToVkImageUsageFlags(this TextureType type, bool isDeviceOnly)
        {
            Vk.VkImageUsageFlags usage = 0;

            if (isDeviceOnly)
            {
                if (type.HasFlag(TextureType.Read)) usage |= Vk.VkImageUsageFlags.TransferSrc;
                if (type.HasFlag(TextureType.Store)) usage |= Vk.VkImageUsageFlags.TransferDst;
            }

            if (type.HasFlag(TextureType.CopySource)) usage |= Vk.VkImageUsageFlags.TransferSrc;
            if (type.HasFlag(TextureType.CopyDestination)) usage |= Vk.VkImageUsageFlags.TransferDst;

            if (type.HasFlag(TextureType.ShaderSample)) usage |= Vk.VkImageUsageFlags.Sampled;
            if (type.HasFlag(TextureType.ShaderStorage)) usage |= Vk.VkImageUsageFlags.Storage;

            if (type.HasFlag(TextureType.ColorAttachment)) usage |= Vk.VkImageUsageFlags.ColorAttachment;
            if (type.HasFlag(TextureType.DepthStencilAttachment)) usage |= Vk.VkImageUsageFlags.DepthStencilAttachment;
            if (type.HasFlag(TextureType.InputAttachment)) usage |= Vk.VkImageUsageFlags.InputAttachment;

            return usage;
        }
        public static Vk.VkImageAspectFlags ToVkImageAspectFlags(this Vk.VkImageUsageFlags usage)
        {
            Vk.VkImageAspectFlags aspectMask = 0;

            if (usage.HasFlag(Vk.VkImageUsageFlags.ColorAttachment) || usage.HasFlag(Vk.VkImageUsageFlags.Sampled) || usage.HasFlag(Vk.VkImageUsageFlags.Storage)) aspectMask |= Vk.VkImageAspectFlags.Color;
            //if (usage.HasFlag(Vk.VkImageUsageFlags.DepthStencilAttachment)) aspectMask |= Vk.VkImageAspectFlags.Depth | Vk.VkImageAspectFlags.Stencil; //TODO: Separate?
            if (usage.HasFlag(Vk.VkImageUsageFlags.DepthStencilAttachment)) aspectMask |= Vk.VkImageAspectFlags.Depth;// | Vk.VkImageAspectFlags.Stencil; //TODO: Both should be specified here!

            //The aspectMask field of VkImageSubresourceLayers contains the aspect or aspects that are the destination of the image copy.
            //Usually, this will be a single bit from the VkImageAspectFlagBits enumeration. If the target image is a color image, then this should simply be set to VK_IMAGE_ASPECT_COLOR_BIT.
            //If the image is a depth-only image, it should be VK_IMAGE_ASPECT_DEPTH_BIT, and if the image is a stencil-only image, it should be VK_IMAGE_ASPECT_STENCIL_BIT.
            //If the image is a combined depth-stencil image, then you can copy data into both the depth and stencil aspects simultaneously by specifying both VK_IMAGE_ASPECT_DEPTH_BIT and VK_IMAGE_ASPECT_STENCIL_BIT.
            //Sellers, Graham; Kessenich, John.Vulkan Programming Guide(OpenGL)(pp. 129 - 130).Pearson Education.Kindle Edition. 

            return aspectMask;
        }
        public static Vk.VkAccessFlags ToVkAccessFlags(this Vk.VkImageUsageFlags usage)
        {
            Vk.VkAccessFlags accessFlags = 0u;

            if (usage.HasFlag(Vk.VkImageUsageFlags.ColorAttachment)) accessFlags |= Vk.VkAccessFlags.ColorAttachmentRead;
            if (usage.HasFlag(Vk.VkImageUsageFlags.DepthStencilAttachment)) accessFlags |= Vk.VkAccessFlags.DepthStencilAttachmentRead;
            if (usage.HasFlag(Vk.VkImageUsageFlags.TransientAttachment) || usage.HasFlag(Vk.VkImageUsageFlags.InputAttachment)) accessFlags |= Vk.VkAccessFlags.InputAttachmentRead;
            if (usage.HasFlag(Vk.VkImageUsageFlags.Sampled) || usage.HasFlag(Vk.VkImageUsageFlags.Storage)) accessFlags |= Vk.VkAccessFlags.ShaderRead;

            return accessFlags;
        }
        public static Vk.VkAccessFlags ToVkAccessFlags(this Vk.VkImageUsageFlags usage, Vk.VkPipelineStageFlags earliestUsage)
        {
            Vk.VkAccessFlags accessFlags = usage.ToVkAccessFlags();
            return accessFlags == 0u ? earliestUsage.GetEarliestUsageAccessFlags() : accessFlags;
        }

        public static Vk.VkComponentSwizzle ToVkComponentSwizzle(this TextureSwizzleType type)
            => type switch
            {
                TextureSwizzleType.Original => Vk.VkComponentSwizzle.Identity,
                TextureSwizzleType.Zero => Vk.VkComponentSwizzle.Zero,
                TextureSwizzleType.One => Vk.VkComponentSwizzle.One,
                TextureSwizzleType.Red => Vk.VkComponentSwizzle.R,
                TextureSwizzleType.Green => Vk.VkComponentSwizzle.G,
                TextureSwizzleType.Blue => Vk.VkComponentSwizzle.B,
                TextureSwizzleType.Alpha => Vk.VkComponentSwizzle.A,
                _ => Vk.VkComponentSwizzle.Identity,
            };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkComponentMapping ToVkComponentMapping(this in TextureSwizzle swizzle)
            => new Vk.VkComponentMapping()
            {
                r = swizzle.red.ToVkComponentSwizzle(),
                g = swizzle.green.ToVkComponentSwizzle(),
                b = swizzle.blue.ToVkComponentSwizzle(),
                a = swizzle.alpha.ToVkComponentSwizzle(),
            };

        public static TextureSwizzleType ToTextureSwizzleType(this Vk.VkComponentSwizzle swizzle)
            => swizzle switch
            {
                Vk.VkComponentSwizzle.Identity => TextureSwizzleType.Original,
                Vk.VkComponentSwizzle.Zero => TextureSwizzleType.Zero,
                Vk.VkComponentSwizzle.One => TextureSwizzleType.One,
                Vk.VkComponentSwizzle.R => TextureSwizzleType.Red,
                Vk.VkComponentSwizzle.G => TextureSwizzleType.Green,
                Vk.VkComponentSwizzle.B => TextureSwizzleType.Blue,
                Vk.VkComponentSwizzle.A => TextureSwizzleType.Alpha,
                _ => TextureSwizzleType.Original,
            };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TextureSwizzle ToTextureSwizzle(this in Vk.VkComponentMapping mapping)
            => new TextureSwizzle(mapping.r.ToTextureSwizzleType(), mapping.g.ToTextureSwizzleType(), mapping.b.ToTextureSwizzleType(), mapping.a.ToTextureSwizzleType());



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkFilter ToVkFilter(this TextureFilterType filter)
            => filter switch
            {
                TextureFilterType.Nearest => Vk.VkFilter.Nearest,
                TextureFilterType.Linear => Vk.VkFilter.Linear,
                _ => Vk.VkFilter.Nearest,
            };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vk.VkSamplerMipmapMode ToVkSamplerMipmapMode(this TextureMipMapType mipmap)
            => mipmap switch
            {
                TextureMipMapType.Nearest => Vk.VkSamplerMipmapMode.Nearest,
                TextureMipMapType.Linear => Vk.VkSamplerMipmapMode.Linear,
                _ => Vk.VkSamplerMipmapMode.Nearest,
            };
        public static Vk.VkSamplerAddressMode ToVkSamplerAddressMode(this TextureWrapType wrap)
            => wrap switch
            {
                TextureWrapType.Repeat => Vk.VkSamplerAddressMode.Repeat,
                TextureWrapType.MirrorRepeat => Vk.VkSamplerAddressMode.MirroredRepeat,
                TextureWrapType.ClampToEdge => Vk.VkSamplerAddressMode.ClampToEdge,
                TextureWrapType.ClampToBorder => Vk.VkSamplerAddressMode.ClampToBorder,
                TextureWrapType.MirrorClampToEdge => Vk.VkSamplerAddressMode.MirrorClampToEdge,
                _ => Vk.VkSamplerAddressMode.Repeat,
            };

    }
}
