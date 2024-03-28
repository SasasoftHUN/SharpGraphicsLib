using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Shaders
{
    //https://www.saschawillems.de/blog/2018/07/19/vulkan-input-attachments-and-sub-passes/
    public readonly struct RenderPassInput<T> where T : unmanaged
    {

        public T Load() => default(T);

    }
}
