using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics
{

    public enum CubeFace : uint { XPositive = 0u, XNegative, YPositive, YNegative, ZPositive, ZNegative = 5u }

    public interface ITextureCube : ITexture
    {

        void StoreDataAllFaces<T>(GraphicsCommandBuffer commandBuffer, T[] data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged;
        void StoreDataAllFaces<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged;
        void StoreDataAllFaces<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, TextureLayout layout, in TextureRange mipLevels) where T : unmanaged;
        void StoreData<T>(GraphicsCommandBuffer commandBuffer, T[] data, TextureLayout layout, CubeFace face, in TextureRange mipLevels) where T : unmanaged;
        void StoreData<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlySpan<T> data, TextureLayout layout, CubeFace face, in TextureRange mipLevels) where T : unmanaged;
        void StoreData<T>(GraphicsCommandBuffer commandBuffer, in ReadOnlyMemory<T> data, TextureLayout layout, CubeFace face, in TextureRange mipLevels) where T : unmanaged;

        ITextureCube CreateView(DataFormat? dataFormat = default(DataFormat?), in TextureSwizzle? swizzle = default(TextureSwizzle?), in TextureRange? levels = default(TextureRange?));
        ITexture2D CreateView(CubeFace face, DataFormat? dataFormat = default(DataFormat?), in TextureSwizzle? swizzle = default(TextureSwizzle?), in TextureRange? levels = default(TextureRange?));

        void UseStagingBuffer(IStagingDataBuffer stagingBuffer, in TextureRange mipLevels, CubeFace face, uint elementIndexOffset = 0u);

    }
}
