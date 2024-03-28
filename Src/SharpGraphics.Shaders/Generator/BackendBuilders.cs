using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using SharpGraphics.Shaders.Builders;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Validators;

namespace SharpGraphics.Shaders.Generator
{
    public static class BackendBuilders
    {

        #region Fields

        private static readonly IEnumerable<string> _defaultBackendNames = new string[]
        {
            //"GLSL330",
            "GLSL420",
            "GLSL430",
            "GLSL440",
            "GLSL450",
            "GLSL460",
            "GLSLES300",
            "GLSLES310",
            "GLSLES320",

            //"GLSL_VK1_0",
            "SPIRV_VK1_0",

            //"HLSL6_0",
        };
        private static readonly IEnumerable<string> _backendNames = new string[]
        {
            "GLSL330",
            "GLSL420",
            "GLSL430",
            "GLSL440",
            "GLSL450",
            "GLSL460",
            "GLSLES300",
            "GLSLES310",
            "GLSLES320",

            "GLSL_VK1_0",
            "SPIRV_VK1_0",

            "HLSL6_0",
        };

        #endregion

        #region Properties

        internal static IEnumerable<string> DefaultBackendNames => _defaultBackendNames;
        public static IEnumerable<string> BackendNames => _backendNames;

        #endregion

        #region Internal Methods

        internal static bool TryGetShaderBuilder(string backendName, ShaderLanguageMappings mappings, ShaderValidators validators, [NotNullWhen(returnValue: true)] out IShaderBuilder? shaderBuilder)
        {
            shaderBuilder = backendName switch
            {
                "GLSL330" => new GLSL330ShaderBuilder(mappings, validators.GLSLangValidator),
                "GLSL420" => new GLSL420ShaderBuilder(mappings, validators.GLSLangValidator),
                "GLSL430" => new GLSL430ShaderBuilder(mappings, validators.GLSLangValidator),
                "GLSL440" => new GLSL440ShaderBuilder(mappings, validators.GLSLangValidator),
                "GLSL450" => new GLSL450ShaderBuilder(mappings, validators.GLSLangValidator),
                "GLSL460" => new GLSL460ShaderBuilder(mappings, validators.GLSLangValidator),
                "GLSLES300" => new GLSLES300ShaderBuilder(mappings, validators.GLSLangValidator),
                "GLSLES310" => new GLSLES310ShaderBuilder(mappings, validators.GLSLangValidator),
                "GLSLES320" => new GLSLES320ShaderBuilder(mappings, validators.GLSLangValidator),

                "GLSL_VK1_0" => new VK1_0GLSLShaderBuilder(mappings, validators.GLSLangValidator),
                "SPIRV_VK1_0" => new VKSPIRV1_0ShaderBuilder(mappings, validators.GLSLangValidator),

                "HLSL6_0" => new HLSL6_0ShaderBuilder(mappings, validators.DirectXShaderCompiler),

                _ => null,
            };
            return shaderBuilder != null;
        }

        #endregion

        #region Public Methods

        public static bool IsBackendNameValid(string backendName) => _backendNames.Any(b => b == backendName);

        #endregion

    }
}
