using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders
{
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class ShaderAttribute : Attribute { }


    [AttributeUsage(AttributeTargets.Class)]
    public class ComputeShaderAttribute : ShaderAttribute { }


    [AttributeUsage(AttributeTargets.Class)]
    public abstract class GraphicsShaderAttribute : ShaderAttribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class VertexShaderAttribute : GraphicsShaderAttribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class FragmentShaderAttribute : GraphicsShaderAttribute { }


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ShaderBackendTarget : Attribute
    {
        public IEnumerable<string> BackendNames { get; }
        public ShaderBackendTarget(params string[] backendNames) => BackendNames = backendNames;
    }



    [AttributeUsage(AttributeTargets.Field)]
    public abstract class ShaderVariableAttribute : Attribute { }


    [AttributeUsage(AttributeTargets.Field)]
    public class InAttribute : ShaderVariableAttribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class OutAttribute : ShaderVariableAttribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class UniformAttribute : ShaderVariableAttribute
    {
        public uint Set { get; set; }
        public uint Binding { get; set; }
        public uint UniqueBinding { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ArraySizeAttribute : Attribute
    {
        public uint Count { get; set; }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class InputAttachmentIndexAttribute : Attribute
    {
        public uint Index { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public abstract class StageAttribute : ShaderVariableAttribute
    {
        public string? Name { get; set; }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class StageInAttribute : StageAttribute
    {
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class StageOutAttribute : StageAttribute
    {
    }

}
