using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{
    internal sealed class VulkanGraphicsSwapChain : GraphicsSwapChain
    {

        #region Fields

        private bool _isDisposed;

        private readonly VulkanGraphicsDevice _device;
        private readonly VulkanCommandProcessor _presentProcessor;
        private readonly VulkanCommandProcessor? _graphicsProcessor;
        private readonly bool _differentGraphicsAndPresentProcessors;

        private Vk.VkSurfaceKHR _surface;
        private Vk.VkSemaphore[]? _imageAvailableSemaphores;
        private Vk.VkSemaphore[]? _renderingFinishedSemaphores;

        private uint _imageCount;
        private Vk.VkFormat _imageFormat;
        private Vk.VkExtent2D _size;
        private Vk.VkSwapchainKHR _swapChain;

        private VulkanTexture2D[][]? _images;
        private VulkanFrameBuffer<ITexture2D, VulkanTexture2D>[]? _frameBuffers; //TODO: Multiview? Stereo support?

        private VulkanCommandBuffer[]? _presentCommandBuffers;
        private VulkanCommandBuffer[]? _graphicsCommandBuffers;

        private uint _currentImageIndex;
        private uint _currentResourceIndex;

        private Vk.VkImageMemoryBarrier[]? _barriersFromPresentToDraw;
        private Vk.VkImageMemoryBarrier[]? _barriersFromDrawToPresent;

        #endregion

        #region Properties

        protected override uint FrameCount => _imageCount;

        public override uint CurrentFrameIndex => _currentImageIndex;
        //internal Vk.VkImage CurrentImage { get => _images[_currentImageIndex].Image; }
        //internal Vk.VkImageView CurrentImageView { get => _images[_currentImageIndex].ImageView; }

        public override Vector2UInt Size => new Vector2UInt(_size.width, _size.height);

        //public ITexture2D[] CurrentTextures { get => _images[_currentImageIndex]; }

        #endregion

        #region Constructors

        internal VulkanGraphicsSwapChain(VulkanGraphicsDevice device, VulkanCommandProcessor presentProcessor, VulkanCommandProcessor? graphicsProcessor, in Vk.VkSurfaceKHR surface, GraphicsViews.IGraphicsView view) : base(view)
        {
            _device = device;
            _presentProcessor = presentProcessor;
            _graphicsProcessor = graphicsProcessor;
            _differentGraphicsAndPresentProcessors = _graphicsProcessor != null && _graphicsProcessor != _presentProcessor;
            _surface = surface;

            //Need the Surface Format early to create RenderPass
            SwapChainConstruction construction = view.SwapChainConstructionRequest;
            GetSwapChainFormat(device, surface, construction.colorFormat.ToVkFormat(), out _imageFormat, out _);
            CheckForFormatFallback(new SwapChainConstruction(construction, _imageFormat.ToDataFormat()));
        }

        ~VulkanGraphicsSwapChain() => Dispose(false);

        #endregion

        #region Private Methods

        private static void GetSwapChainFormat(VulkanGraphicsDevice device, in Vk.VkSurfaceKHR surface, Vk.VkFormat formatRequest, out Vk.VkFormat format, out Vk.VkColorSpaceKHR colorSpace)
        {
            //Query Formats
            if (VK.vkGetPhysicalDeviceSurfaceFormatsKHR(device.PhysicalDevice, surface, out uint formatCount, IntPtr.Zero) != Vk.VkResult.Success || formatCount == 0)
                throw new Exception("Enumerate Surface Formats == 0");
            Span<Vk.VkSurfaceFormatKHR> surfaceFormats = stackalloc Vk.VkSurfaceFormatKHR[(int)formatCount];
            if (VK.vkGetPhysicalDeviceSurfaceFormatsKHR(device.PhysicalDevice, surface, out formatCount, out surfaceFormats[0]) != Vk.VkResult.Success)
                throw new Exception("Enumerating Surface Formats");

            //Select Format
            Vk.VkSurfaceFormatKHR surfaceFormat = surfaceFormats[0];

            // If the list contains only one entry with undefined format it means that there are no preferred surface formats and any can be chosen
            if (formatCount == 1 && surfaceFormats[0].format == Vk.VkFormat.Undefined)
                surfaceFormat = new Vk.VkSurfaceFormatKHR() { format = formatRequest != Vk.VkFormat.Undefined ? formatRequest : Vk.VkFormat.R8g8b8a8Unorm, colorSpace = Vk.VkColorSpaceKHR.SrgbNonlinearKHR };
            else if (formatRequest != Vk.VkFormat.Undefined)
            {
                for (int i = 0; i < formatCount; i++)
                    if (surfaceFormats[i].format == formatRequest)
                    {
                        surfaceFormat = surfaceFormats[i];
                        break;
                    }
            }

            format = surfaceFormat.format;
            colorSpace = surfaceFormat.colorSpace;
        }

        private static bool TryCreateVKSwapChain(VulkanGraphicsDevice device, in Vk.VkSurfaceKHR surface, in Vk.VkSwapchainKHR oldSwapChain, IRenderPass renderPass, in SwapChainConstruction constructionRequest,
                                                            out uint imageCount, out Vk.VkFormat format, out Vk.VkExtent2D extent, out Vk.VkSwapchainKHR swapChain, out SwapChainConstruction finalConstruction, [NotNullWhen(returnValue: true)] out VulkanTexture2D[][]? images)
        {
            imageCount = 0;
            format = Vk.VkFormat.Undefined;
            extent = Vk.VkExtent2D.Zero;
            swapChain = Vk.VkSwapchainKHR.Null;
            finalConstruction = new SwapChainConstruction();
            images = null;

            //Get Supported Surface stuff
            Vk.VkResult result = VK.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(device.PhysicalDevice, surface, out Vk.VkSurfaceCapabilitiesKHR surfaceCapabilities);
            if (result != Vk.VkResult.Success)
            {
                if (result == Vk.VkResult.ErrorSurfaceLostKHR)
                    return false;
                throw new Exception($"Could not check presentation surface capabilities: {result}!");
            }

            result = VK.vkGetPhysicalDeviceSurfacePresentModesKHR(device.PhysicalDevice, surface, out uint presentModeCount, IntPtr.Zero);
            if (result != Vk.VkResult.Success || presentModeCount == 0)
                throw new Exception($"Enumerate Present Modes error: {(result != Vk.VkResult.Success ? result.ToString() : "count == 0")}!");

            Span<Vk.VkPresentModeKHR> presentModes = stackalloc Vk.VkPresentModeKHR[(int)presentModeCount];
            result = VK.vkGetPhysicalDeviceSurfacePresentModesKHR(device.PhysicalDevice, surface, out presentModeCount, out presentModes[0]);
            if (result != Vk.VkResult.Success)
                throw new Exception($"Enumerating Present Modes error: {result}!");

            //Get SwapChain image count
            imageCount = surfaceCapabilities.minImageCount + 1;
            if (surfaceCapabilities.maxImageCount > 0 && imageCount > surfaceCapabilities.maxImageCount)
                imageCount = surfaceCapabilities.maxImageCount;

            //Select Format
            GetSwapChainFormat(device, surface, constructionRequest.colorFormat.ToVkFormat(), out format, out Vk.VkColorSpaceKHR colorSpace);

            //Select Size
            //A special value of "-1" indicates that the application's window size will be determined by the swap chain size, so we can choose whatever dimension we want.
            /*if (surfaceCapabilities.currentExtent.width == -1)
            {
                swapChainExtent = sizeRequest;
                if (swapChainExtent.width < surfaceCapabilities.minImageExtent.width)
                    swapChainExtent.width = surfaceCapabilities.minImageExtent.width;
                if (swapChainExtent.height < surfaceCapabilities.minImageExtent.height)
                    swapChainExtent.height = surfaceCapabilities.minImageExtent.height;
                if (swapChainExtent.width > surfaceCapabilities.maxImageExtent.width)
                    swapChainExtent.width = surfaceCapabilities.maxImageExtent.width;
                if (swapChainExtent.height > surfaceCapabilities.maxImageExtent.height)
                    swapChainExtent.height = surfaceCapabilities.maxImageExtent.height;
            }
            else */
            extent = surfaceCapabilities.currentExtent;

            //Enable SwapChain usage flags
            //Color attachment flag must always be supported. We can define other usage flags but we always need to check if they are supported
            Vk.VkImageUsageFlags usageFlags = Vk.VkImageUsageFlags.ColorAttachment;
            if (surfaceCapabilities.supportedUsageFlags.HasFlag(Vk.VkImageUsageFlags.TransferDst))
                usageFlags |= Vk.VkImageUsageFlags.TransferDst;

            //Set transforms (could be used for rotating images on tablets/phones)
            Vk.VkSurfaceTransformFlagsKHR transformFlags =
                surfaceCapabilities.supportedTransforms.HasFlag(Vk.VkSurfaceTransformFlagsKHR.IdentityKHR) ?
                Vk.VkSurfaceTransformFlagsKHR.IdentityKHR :
                surfaceCapabilities.currentTransform;

            //Set Presentation Mode
            Vk.VkPresentModeKHR presentationModeRequest = constructionRequest.mode.ToVkPresentModeKHR();
            Vk.VkPresentModeKHR presentationMode = Vk.VkPresentModeKHR.ImmediateKHR;
            for (int i = 0; i < presentModes.Length; i++)
                if (presentModes[i] == presentationModeRequest)
                {
                    presentationMode = presentModes[i];
                    break;
                }
            if (presentationMode == Vk.VkPresentModeKHR.ImmediateKHR && constructionRequest.mode != PresentMode.Immediate)
            {
                for (int i = 0; i < presentModeCount; i++)
                    if (presentModes[i] == Vk.VkPresentModeKHR.MailboxKHR)
                    {
                        presentationMode = Vk.VkPresentModeKHR.MailboxKHR;
                        break;
                    }
                if (presentationMode == Vk.VkPresentModeKHR.ImmediateKHR)
                    for (int i = 0; i < presentModeCount; i++)
                        if (presentModes[i] == Vk.VkPresentModeKHR.FifoKHR)
                        {
                            presentationMode = Vk.VkPresentModeKHR.FifoKHR;
                            break;
                        }
            }

            //Creating the SwapChain
            Vk.VkSwapchainCreateInfoKHR swapchainCreateInfo = new Vk.VkSwapchainCreateInfoKHR()
            {
                sType = Vk.VkStructureType.SwapchainCreateInfoKHR,
                surface = surface,

                minImageCount = imageCount,
                imageFormat = format,
                imageColorSpace = colorSpace,
                imageExtent = extent,

                imageArrayLayers = 1u, //Defines the number of layers in a swap chain images (that is, views); typically this value will be one but if we want to create multiview or stereo (stereoscopic 3D) images, we can set it to some higher value.

                imageUsage = usageFlags,
                imageSharingMode = Vk.VkSharingMode.Exclusive,

                preTransform = transformFlags,
                compositeAlpha = Vk.VkCompositeAlphaFlagsKHR.OpaqueKHR,

                presentMode = presentationMode,
                clipped = Vk.VkBool32.True, // Connected with ownership of pixels; in general it should be set to VK_TRUE if application doesn't want to read from swap chain images (like ReadPixels()) as it will allow some platforms to use more optimal presentation methods

                oldSwapchain = oldSwapChain,
            };

            //TODO: Concurrent SharingMode when GraphicsFamily != PresentFamily?
            result = VK.vkCreateSwapchainKHR(device.Device, ref swapchainCreateInfo, IntPtr.Zero, out swapChain);
            if (result != Vk.VkResult.Success)
                throw new Exception($"VkSwapChain creation error: {result}!");

            //Get SwapChain Image Count
            result = VK.vkGetSwapchainImagesKHR(device.Device, swapChain, out imageCount, IntPtr.Zero);
            if (result != Vk.VkResult.Success || imageCount == 0)
                throw new Exception($"Enumerate SwapChain Images error: {(result != Vk.VkResult.Success ? result.ToString() : "count == 0")}!");

            //Get SwapChain Images
            Vk.VkImage[] swapChainImages = new Vk.VkImage[imageCount];
            result = VK.vkGetSwapchainImagesKHR(device.Device, swapChain, out imageCount, out swapChainImages[0]);
            if (result != Vk.VkResult.Success)
                throw new Exception($"Enumerating SwapChain Images error: {result}!");

            ReadOnlySpan<RenderPassAttachment> attachments = renderPass.Attachments;
            RenderPassStep lastStep = renderPass.Steps[renderPass.Steps.Length - 1];
            images = new VulkanTexture2D[swapChainImages.Length][];
            for (int i = 0; i < swapChainImages.Length; i++)
            {
                images[i] = new VulkanTexture2D[attachments.Length];

                for (int j = 0; j < attachments.Length; j++)
                    if (lastStep.IsUsingColorAttachment((uint)j))
                        images[i][j] = new VulkanTexture2D(device, swapChainImages[i], format, extent, Vk.VkImageLayout.PresentSrcKHR, TextureType.ColorAttachment, Vk.VkPipelineStageFlags.BottomOfPipe, 1u); //TODO: Pipeline State?
                    else images[i][j] = new VulkanTexture2D(device, attachments[j].format.ToVkFormat(), extent,
                        Vk.VkImageLayout.Undefined,
                        attachments[j].type.ToTextureType(),
                        Vk.VkMemoryPropertyFlags.DeviceLocal, Vk.VkPipelineStageFlags.BottomOfPipe, 1u); //TODO: Pipeline State?
            }

            finalConstruction = new SwapChainConstruction(constructionRequest, swapchainCreateInfo.imageFormat.ToDataFormat());

            return true;
        }
        private static bool TryCreateVKSwapChain(VulkanGraphicsDevice device, in Vk.VkSurfaceKHR surface, in Vk.VkSwapchainKHR oldSwapChain, IRenderPass renderPass, in SwapChainConstruction constructionRequest,
                                                            out uint imageCount, out Vk.VkFormat format, out Vk.VkExtent2D extent, out Vk.VkSwapchainKHR swapChain, out SwapChainConstruction finalConstruction, [NotNullWhen(returnValue: true)] out VulkanTexture2D[][]? images,
                                                            [NotNullWhen(returnValue: true)] out Vk.VkImageMemoryBarrier[]? barriersFromPresentToDraw, [NotNullWhen(returnValue: true)] out Vk.VkImageMemoryBarrier[]? barriersFromDrawToPresent,
                                                            uint presentQueueFamily, uint graphicsQueueFamily)
        {
            if (TryCreateVKSwapChain(device, surface, oldSwapChain, renderPass, constructionRequest, 
                out imageCount, out format, out extent, out swapChain, out finalConstruction, out images))
            {
                Vk.VkImageSubresourceRange imageSubresourceRange = new Vk.VkImageSubresourceRange()
                {
                    aspectMask = Vk.VkImageAspectFlags.Color,
                    baseMipLevel = 0u,
                    levelCount = 1u,
                    baseArrayLayer = 0u,
                    layerCount = 1u,
                };

                barriersFromPresentToDraw = new Vk.VkImageMemoryBarrier[imageCount];
                barriersFromDrawToPresent = new Vk.VkImageMemoryBarrier[imageCount];
                for (int i = 0; i < imageCount; i++)
                {
                    barriersFromPresentToDraw[i] = new Vk.VkImageMemoryBarrier()
                    {
                        sType = Vk.VkStructureType.ImageMemoryBarrier,

                        srcAccessMask = Vk.VkAccessFlags.MemoryRead,
                        dstAccessMask = Vk.VkAccessFlags.ColorAttachmentWrite,

                        oldLayout = Vk.VkImageLayout.PresentSrcKHR,
                        newLayout = Vk.VkImageLayout.PresentSrcKHR,

                        srcQueueFamilyIndex = presentQueueFamily,
                        dstQueueFamilyIndex = graphicsQueueFamily,

                        image = images[i][0].VkImage,
                        subresourceRange = imageSubresourceRange,
                    };
                    barriersFromDrawToPresent[i] = new Vk.VkImageMemoryBarrier()
                    {
                        sType = Vk.VkStructureType.ImageMemoryBarrier,

                        srcAccessMask = Vk.VkAccessFlags.ColorAttachmentWrite,
                        dstAccessMask = Vk.VkAccessFlags.MemoryRead,

                        oldLayout = Vk.VkImageLayout.PresentSrcKHR,
                        newLayout = Vk.VkImageLayout.PresentSrcKHR,

                        srcQueueFamilyIndex = graphicsQueueFamily,
                        dstQueueFamilyIndex = presentQueueFamily,

                        image = images[i][0].VkImage,
                        subresourceRange = imageSubresourceRange,
                    };
                }

                return true;
            }
            else
            {
                barriersFromPresentToDraw = null;
                barriersFromDrawToPresent = null;
                return false;
            }
        }

        private void RecreateSurface()
        {
            if (_surface != Vk.VkSurfaceKHR.Null)
                VK.vkDestroySurfaceKHR(_device.GraphicsManagement.Instance, _surface, IntPtr.Zero);

            _device.GraphicsManagement.CreateSurface(_view, out _surface);
        }
        private void Recreate()
        {
            Clear();

            if (RenderPass == null)
                throw new NullReferenceException("VulkanGraphicsSwapChain has no RenderPass");

            //Create SwapChain
            _device.WaitForIdle();

            SwapChainConstruction finalConstruction;
            if (_view.IsViewInitialized)
            {
                if (_graphicsProcessor != null && _presentProcessor != _graphicsProcessor)
                {
                    if (!TryCreateVKSwapChain(_device, _surface, Vk.VkSwapchainKHR.Null, RenderPass, Format,
                        out _imageCount, out _imageFormat, out _size, out _swapChain, out finalConstruction, out _images,
                        out _barriersFromPresentToDraw, out _barriersFromDrawToPresent,
                        _presentProcessor.FamilyIndex, _graphicsProcessor.FamilyIndex))
                    {
                        RecreateSurface();
                        Recreate();
                        return;
                    }
                }
                else
                {
                    if (!TryCreateVKSwapChain(_device, _surface, Vk.VkSwapchainKHR.Null, RenderPass, Format,
                        out _imageCount, out _imageFormat, out _size, out _swapChain, out finalConstruction, out _images))
                    {
                        RecreateSurface();
                        Recreate();
                        return;
                    }
                }
            }
            else return;

            CheckForFormatFallback(finalConstruction);

            OnFramesRecreated(_imageCount);
            GC.Collect(2, GCCollectionMode.Forced, false, true);
            OnSizeChanged(Size);

            //Create FrameBuffers
            if (_frameBuffers == null)
                _frameBuffers = new VulkanFrameBuffer<ITexture2D, VulkanTexture2D>[(int)_imageCount];
            else
            {
                ClearFramebuffers();
                if (_frameBuffers.Length != _imageCount)
                    _frameBuffers = new VulkanFrameBuffer<ITexture2D, VulkanTexture2D>[(int)_imageCount];
            }

            for (int i = 0; i < _frameBuffers.Length; i++)
                _frameBuffers[i] = new VulkanFrameBuffer<ITexture2D, VulkanTexture2D>(_device, _images[i], RenderPass, false);

            //Creating Semaphores
            _device.CreateSemaphores(_imageCount, out _imageAvailableSemaphores);
            _device.CreateSemaphores(_imageCount, out _renderingFinishedSemaphores);

            //Allocating Command Buffers
            _presentCommandBuffers = _presentProcessor.CommandBufferFactory.CreateCommandBuffers(_imageCount).Cast<VulkanCommandBuffer>().ToArray();
            if (_differentGraphicsAndPresentProcessors)
                _graphicsCommandBuffers = _graphicsProcessor!.CommandBufferFactory.CreateCommandBuffers(_imageCount).Cast<VulkanCommandBuffer>().ToArray();

            _currentResourceIndex = 0u;
        }

        private void Clear()
        {
            VK.vkDeviceWaitIdle(_device.Device);

            ClearFramebuffers();

            if (_images != null)
                foreach (VulkanTexture2D[] images in _images)
                    if (images != null)
                        foreach (VulkanTexture2D image in images)
                            if (image != null)
                                image.Dispose();
            _images = null;

            if (_presentCommandBuffers != null && _presentCommandBuffers.Length > 0)
                VulkanCommandBufferFactory.DeleteCommandBuffersOnSamePool(_presentCommandBuffers);
            _presentCommandBuffers = null;

            if (_differentGraphicsAndPresentProcessors)
            {
                if (_graphicsCommandBuffers != null && _graphicsCommandBuffers.Length > 0)
                    VulkanCommandBufferFactory.DeleteCommandBuffersOnSamePool(_graphicsCommandBuffers);
            }
            _graphicsCommandBuffers = null;

            if (_imageAvailableSemaphores != null && _imageAvailableSemaphores.Length > 0)
                _device.DestroySemaphores(_imageAvailableSemaphores);
            _imageAvailableSemaphores = null;
            if (_renderingFinishedSemaphores != null && _renderingFinishedSemaphores.Length > 0)
                _device.DestroySemaphores(_renderingFinishedSemaphores);
            _renderingFinishedSemaphores = null;

            if (_swapChain != Vk.VkSwapchainKHR.Null)
            {
                VK.vkDestroySwapchainKHR(_device.Device, _swapChain, IntPtr.Zero);
                _swapChain = Vk.VkSwapchainKHR.Null;
            }
        }
        private void ClearFramebuffers()
        {
            if (_frameBuffers != null)
                for (int i = 0; i < _frameBuffers.Length; i++)
                    if (!_frameBuffers[i].IsDisposed)
                        _frameBuffers[i].Dispose();
        }



        private bool GetNextImage([NotNullWhen(returnValue: true)] out VulkanCommandBuffer? vulkanCommandBuffer)
        {
            if (CheckAndResetIfNeededToBeRecreated() ||
                _presentCommandBuffers == null ||
                _imageAvailableSemaphores == null ||
                (_differentGraphicsAndPresentProcessors && (_barriersFromPresentToDraw == null || _graphicsCommandBuffers == null)))
            {
                if (_view.IsViewInitialized)
                    Recreate();
                else _needToRecrate = true;
                vulkanCommandBuffer = null;
                return false;
            }

            _currentResourceIndex = GetNextResourceIndex();

            Vk.VkFence fence = (_differentGraphicsAndPresentProcessors ? _graphicsCommandBuffers![_currentResourceIndex] : _presentCommandBuffers[_currentResourceIndex]).Fence;
            if (fence != Vk.VkFence.Null)
            {
                VK.vkWaitForFences(_device.Device, 1u, ref fence, Vk.VkBool32.False, ulong.MaxValue);
                VK.vkResetFences(_device.Device, 1u, ref fence);
            }

            Vk.VkResult acquireResult = VK.vkAcquireNextImageKHR(_device.Device, _swapChain, ulong.MaxValue, _imageAvailableSemaphores[_currentResourceIndex], Vk.VkFence.Null, out _currentImageIndex);
            OnNextFrame(_currentImageIndex);

            bool successful;
            switch (acquireResult)
            {
                case Vk.VkResult.Success:
                case Vk.VkResult.SuboptimalKHR:
                    successful = true;
                    break;

                case Vk.VkResult.ErrorOutOfDateKHR:
                    Recreate();
                    successful = false;
                    break;

                default:
                    successful = false;
                    break;
            }

            if (successful)
            {
                vulkanCommandBuffer = _differentGraphicsAndPresentProcessors ? _graphicsCommandBuffers![_currentResourceIndex] : _presentCommandBuffers[_currentResourceIndex];

                vulkanCommandBuffer.Reset();
                vulkanCommandBuffer.Begin();

                if (_differentGraphicsAndPresentProcessors)
                    vulkanCommandBuffer.PipelineBarrier(Vk.VkPipelineStageFlags.ColorAttachmentOutput, Vk.VkPipelineStageFlags.ColorAttachmentOutput, ref _barriersFromPresentToDraw![_currentImageIndex]);

                return true;
            }
            else
            {
                vulkanCommandBuffer = null;
                return false;
            }
        }

        private bool Present()
        {
            if (_presentCommandBuffers == null ||
                _imageAvailableSemaphores == null || _renderingFinishedSemaphores == null ||
                (_differentGraphicsAndPresentProcessors && (_barriersFromDrawToPresent == null || _graphicsCommandBuffers == null)))
            {
                Recreate();
                return false;
            }

            VulkanCommandBuffer frameCommandBuffer = _differentGraphicsAndPresentProcessors ? _graphicsCommandBuffers![_currentResourceIndex] : _presentCommandBuffers[_currentResourceIndex];

            if (_differentGraphicsAndPresentProcessors)
                frameCommandBuffer.PipelineBarrier(Vk.VkPipelineStageFlags.ColorAttachmentOutput, Vk.VkPipelineStageFlags.BottomOfPipe, ref _barriersFromDrawToPresent![_currentImageIndex]);

            frameCommandBuffer.End();
            frameCommandBuffer.Submit(_imageAvailableSemaphores[_currentResourceIndex], Vk.VkPipelineStageFlags.ColorAttachmentOutput, _renderingFinishedSemaphores[_currentResourceIndex]);

            using PinnedObjectReference<Vk.VkSemaphore> pinnedWaitSemaphore = new PinnedObjectReference<Vk.VkSemaphore>(ref _renderingFinishedSemaphores[_currentResourceIndex]);
            using PinnedObjectReference<Vk.VkSwapchainKHR> pinnedSwapChain = new PinnedObjectReference<Vk.VkSwapchainKHR>(ref _swapChain);
            using PinnedObjectReference<uint> pinnedCurrentImageIndex = new PinnedObjectReference<uint>(ref _currentImageIndex);

            Vk.VkPresentInfoKHR presentInfo = new Vk.VkPresentInfoKHR()
            {
                sType = Vk.VkStructureType.PresentInfoKHR,

                waitSemaphoreCount = 1u,
                pWaitSemaphores = pinnedWaitSemaphore.pointer,

                swapchainCount = 1u,
                pSwapchains = pinnedSwapChain.pointer,

                pImageIndices = pinnedCurrentImageIndex.pointer,
            };

            switch (VK.vkQueuePresentKHR(_presentProcessor.Queue, ref presentInfo))
            {
                case Vk.VkResult.Success:
                    return true;

                case Vk.VkResult.ErrorOutOfDateKHR:
                case Vk.VkResult.SuboptimalKHR:
                    Recreate();

                    return false;

                default:
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint GetNextResourceIndex() => (_currentResourceIndex + 1u) % _imageCount;

        #endregion

        #region Protected Methods

        // Protected implementation of Dispose pattern.
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
                    Debug.WriteLine($"Disposing Vulkan SwapChain from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_device != null && !_device.IsDisposed)
                {
                    Clear();

                    if (_surface != Vk.VkSurfaceKHR.Null)
                    {
                        VK.vkDestroySurfaceKHR(_device.GraphicsManagement.Instance, _surface, IntPtr.Zero);
                        _surface = Vk.VkSurfaceKHR.Null;
                    }
                }
                else Debug.WriteLine("Warning: VulkanSwapChain cannot be disposed properly because parent GraphicsDevice is already Disposed!");

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Internal Methods


        #endregion

        #region Public Methods

        public override bool TryBeginFrame([NotNullWhen(returnValue: true)] out GraphicsCommandBuffer? commandBuffer, [NotNullWhen(returnValue: true)] out IFrameBuffer<ITexture2D>? frameBuffer)
        {
            if (GetNextImage(out VulkanCommandBuffer? vulkanCommandBuffer))
            {
                commandBuffer = vulkanCommandBuffer;
                if (_frameBuffers == null || _frameBuffers.Length <= _currentImageIndex || _frameBuffers[_currentImageIndex] == null || Unsafe.As<IVulkanFrameBuffer>(_frameBuffers[_currentImageIndex]).VkSize != _size)
                {
                    frameBuffer = null;
                    return false; //Technically, this shouldn't happen
                }
                frameBuffer = Unsafe.As<IFrameBuffer<ITexture2D>>(_frameBuffers[_currentImageIndex]);
                return true;
            }
            else
            {
                commandBuffer = null;
                frameBuffer = null;
                return false;
            }
        }
        public override bool PresentFrame() => Present();

        #endregion

    }
}
