using SharpGraphics.Shaders;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ShaderTypeMappingAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class ShaderTypeMappingExternalAttribute : ShaderTypeMappingAttribute
    {

        public Type Type { get; }

        public ShaderTypeMappingExternalAttribute(Type type)
        {
            Type = type;
        }

    }

}
