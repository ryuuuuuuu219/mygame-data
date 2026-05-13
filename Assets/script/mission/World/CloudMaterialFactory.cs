using UnityEngine;

public static class CloudMaterialFactory
{
    private const int CloudRenderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 100;

    public static Material CreateVoxelCloudMaterial(
        Color baseColor,
        float angleFadePower = 2f,
        float minAlphaFactor = 0.12f)
    {
        Shader shader = Shader.Find("Custom/CloudViewAngleTransparent");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = "GeneratedVoxelCloudMaterial";
        material.color = baseColor;
        material.SetColor("_BaseColor", baseColor);
        material.SetColor("_Color", baseColor);
        material.SetFloat("_Smoothness", 0.12f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_AngleFadePower", angleFadePower);
        material.SetFloat("_MinAlphaFactor", minAlphaFactor);
        ConfigureTransparent(material);
        return material;
    }

    private static void ConfigureTransparent(Material material)
    {
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_Mode", 2f);
        material.SetFloat("_AlphaClip", 0f);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = CloudRenderQueue;
    }
}
