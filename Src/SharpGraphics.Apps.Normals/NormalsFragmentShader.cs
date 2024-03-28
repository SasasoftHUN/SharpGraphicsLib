using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static SharpGraphics.Apps.Normals.NormalsApp;

namespace SharpGraphics.Apps.Normals
{
    [FragmentShader]
    public partial class NormalsFragmentShader : FragmentShaderBase
    {

        [In] private readonly Vector3 vs_out_pos;
        [In] private readonly Vector3 vs_out_norm;
        [In] private readonly Vector2 vs_out_tex;

        [Out] private Vector4 fs_out_col;

        [Uniform(Set = 0u, Binding = 1u)] private MaterialData material;
        [Uniform(Set = 0u, Binding = 2u)] private LightData light;
        [Uniform(Set = 0u, Binding = 3u)] private SceneData scene;
        [Uniform(Set = 0u, Binding = 4u)] private TextureSampler2D<Vector4> textureSampler;

        public override void Main()
        {
            Vector4 ambient = light.ambientColor * material.ambientColor;

            Vector3 normal = Vector3.Normalize(vs_out_norm);
            Vector3 toLight = -light.direction;
            float diffuseIntensity = MathHelper.Clamp(Vector3.Dot(normal, toLight), 0f, 1f);
            Vector4 diffuse = light.diffuseColor * material.diffuseColor * diffuseIntensity;

            Vector3 reflect = Vector3.Reflect(light.direction, normal);
            Vector3 toCamera = Vector3.Normalize(scene.cameraPosition - vs_out_pos);
            float specularIntensity = MathF.Pow(MathHelper.Clamp(Vector3.Dot(reflect, toCamera), 0f, 1f), light.specularPower);
            Vector4 specular = light.specularColor * material.specularColor * specularIntensity;

            Vector4 textureColor = textureSampler.Sample(vs_out_tex);

            fs_out_col = (ambient + diffuse + specular) * textureColor;
        }

    }
}
