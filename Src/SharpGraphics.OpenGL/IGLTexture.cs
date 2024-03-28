using SharpGraphics.Allocator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL
{
    public interface IGLTexture : ITexture, IGLResource
    {

        int ID { get; }

        void GLBind(int binding, int textureUnit);
        void GLUnBind(int textureUnit);

        void GLStoreData(IntPtr data, in CopyBufferTextureRange range);
        void GLStoreData(IntPtr data, in ReadOnlySpan<CopyBufferTextureRange> ranges);
        void GLReadData(IntPtr data, in CopyBufferTextureRange range);
        void GLReadData(IntPtr data, in ReadOnlySpan<CopyBufferTextureRange> ranges);

        void GLBindToFrameBuffer(int frameBufferID, AttachmentType attachmentType, int attachmentTypeIndex);

        void GLGenerateMipMaps();

    }

    public interface IGLTexture2D : IGLTexture, ITexture2D
    {
        
    }

    public interface IGLTextureCube : IGLTexture, ITextureCube
    {


    }

}
