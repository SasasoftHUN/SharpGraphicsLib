using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics
{
    public interface ITexture2D : ITexture
    {

        void StoreData<T>(GraphicsCommandBuffer commandBuffer, T[] data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged;
        void StoreData<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged;
        void StoreData<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged;

        ITexture2D CreateView(DataFormat? dataFormat = default(DataFormat?), in TextureSwizzle? swizzle = default(TextureSwizzle?), in TextureRange? levels = default(TextureRange?));

    }
}
