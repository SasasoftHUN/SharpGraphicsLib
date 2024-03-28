using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics.Apps.Models
{
    [FragmentShader]
    public partial class NormalsFragmentShader : FragmentShaderBase
    {

        [In] private readonly Vector3 vs_out_pos;
        [In] private readonly Vector3 vs_out_norm;
        [In] private readonly Vector2 vs_out_tex;

        [Out] private Vector4 fs_out_col;

        [Uniform(Set = 0u, Binding = 0u)] private LightData light;
        [Uniform(Set = 0u, Binding = 1u)] private SceneData scene;
        [Uniform(Set = 2u, Binding = 0u, UniqueBinding = 3u)] private MaterialData material;
        [Uniform(Set = 2u, Binding = 1u, UniqueBinding = 4u)] private TextureSampler2D<Vector4> diffuseTextureSampler;
        //[Uniform(Set = 2u, Binding = 2u, UniqueBinding = 5u)] private TextureSampler2D<Vector4> normalTextureSampler;

        public override void Main()
        {
            Vector4 diffuseTextureColor = diffuseTextureSampler.Sample(vs_out_tex);
            if (diffuseTextureColor.W < 0.1f)
            {
                Discard();
                return;
            }

            Vector4 ambient = light.ambientColor * material.ambientColor;

            Vector3 normal = Vector3.Normalize(vs_out_norm);
            Vector3 toLight = -light.direction;
            float diffuseIntensity = MathHelper.Clamp(Vector3.Dot(normal, toLight), 0f, 1f);
            Vector4 diffuse = light.diffuseColor * material.diffuseColor * diffuseIntensity;

            Vector3 reflect = Vector3.Reflect(light.direction, normal);
            Vector3 toCamera = Vector3.Normalize(scene.cameraPosition - vs_out_pos);
            float specularIntensity = MathF.Pow(MathHelper.Clamp(Vector3.Dot(reflect, toCamera), 0f, 1f), material.specularPower);
            Vector4 specular = light.specularColor * material.specularColor * specularIntensity;

            fs_out_col = (ambient + diffuse + specular) * diffuseTextureColor;

            /*float gammaCorrect = 1f / 2.2f;
            fs_out_col = new Vector4(
                MathF.Pow(fs_out_col.X, gammaCorrect),
                MathF.Pow(fs_out_col.Y, gammaCorrect),
                MathF.Pow(fs_out_col.Z, gammaCorrect),
                fs_out_col.W
            );*/
        }

    }
}
