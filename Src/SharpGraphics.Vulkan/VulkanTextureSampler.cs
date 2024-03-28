using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{
    internal sealed class VulkanTextureSampler : TextureSampler
    {

        #region Fields

        private bool _isDisposed;
        private Vk.VkSampler _sampler;

        private readonly VulkanGraphicsDevice _device;

        #endregion

        #region Properties

        internal Vk.VkSampler Sampler => _sampler;

        #endregion

        #region Constructors

        internal VulkanTextureSampler(VulkanGraphicsDevice device, in TextureSamplerConstruction construction)
        {
            _device = device;

            Vk.VkSamplerCreateInfo samplerCreateInfo = new Vk.VkSamplerCreateInfo()
            {
                sType = Vk.VkStructureType.SamplerCreateInfo,
                magFilter = construction.magnifyingFilter.ToVkFilter(),
                minFilter = construction.minifyingFilter.ToVkFilter(),
                mipmapMode = construction.mipmapMode.mode.ToVkSamplerMipmapMode(),

                addressModeU = construction.wrap.u.ToVkSamplerAddressMode(),
                addressModeV = construction.wrap.v.ToVkSamplerAddressMode(),
                addressModeW = construction.wrap.w.ToVkSamplerAddressMode(),

                mipLodBias = construction.mipmapMode.lodBias,
                minLod = construction.mipmapMode.lodRange.start,
                maxLod = construction.mipmapMode.lodRange.count == float.MaxValue ? VK.LodClampNone : construction.mipmapMode.lodRange.End,
                anisotropyEnable = construction.anisotropy > 1f ? Vk.VkBool32.True : Vk.VkBool32.False,
                maxAnisotropy = Math.Min(construction.anisotropy, _device.Limits.MaxAnisotropy),
                compareEnable = Vk.VkBool32.False,
                compareOp = Vk.VkCompareOp.Always,

                borderColor = Vk.VkBorderColor.FloatTransparentBlack,
                unnormalizedCoordinates = Vk.VkBool32.False,
            };
            VK.vkCreateSampler(_device.Device, ref samplerCreateInfo, IntPtr.Zero, out _sampler);
        }

        ~VulkanTextureSampler() => Dispose(false);

        #endregion

        #region Protected Methods

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
                    Debug.WriteLine($"Disposing Vulkan Texture Sampler from {(disposing ? "Dispose()" : "Finalizer")}...");

                if (_sampler != Vk.VkSampler.Null)
                {
                    if (_device != null && !_device.IsDisposed)
                    {
                        VK.vkDestroySampler(_device.Device, _sampler, IntPtr.Zero);
                        _sampler = Vk.VkSampler.Null;
                    }
                    else Debug.WriteLine("Warning: VulkanTextureSampler cannot be disposed properly because parent GraphicsDevice is already Disposed!");
                }

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

    }
}
