using UnityEngine;

public class PlanetTextureManager : MonoBehaviour
{
    [Header("Matériaux & Shaders")]
    [SerializeField] private Material planetMaterial;

    [Header("Configuration des Hauteurs")]
    [SerializeField] private int textureResolution = 256;

    [Header("Rampe de Couleurs (Gradient)")]
    // Ce champ va afficher une magnifique barre de dégradé éditable dans l'inspecteur Unity !
    [SerializeField] private Gradient planetColorGradient;
    [SerializeField] private int rampResolution = 256; // Nombre de nuances de couleurs

    private Cubemap heightCubemap;
    private Texture2D rampTexture;

    private void Start()
    {
        GenerateAndApplyColorRamp();
    }

    // Permet de mettre à jour la rampe en temps réel dans l'éditeur lors de modifications
    private void OnValidate()
    {
        if (planetMaterial != null && planetColorGradient != null)
        {
            GenerateAndApplyColorRamp();
        }
    }

    /// <summary>
    /// Génère une texture 1D à partir du dégradé de l'inspecteur et l'envoie au shader.
    /// </summary>
    public void GenerateAndApplyColorRamp()
    {
        if (planetMaterial == null || planetColorGradient == null) return;

        // 1. Création de la texture si nécessaire
        if (rampTexture == null || rampTexture.width != rampResolution)
        {
            rampTexture = new Texture2D(rampResolution, 1, TextureFormat.RGBA32, false);
            rampTexture.wrapMode = TextureWrapMode.Clamp;
            rampTexture.filterMode = FilterMode.Bilinear; // Lissage entre les couleurs
        }

        // 2. Conversion du Gradient en pixels colorés
        Color[] colors = new Color[rampResolution];
        for (int i = 0; i < rampResolution; i++)
        {
            float t = (float)i / (rampResolution - 1);
            // Échantillonnage de la couleur du dégradé à la position t (entre 0 et 1)
            colors[i] = planetColorGradient.Evaluate(t);
        }

        // 3. Application et envoi au shader
        rampTexture.SetPixels(colors);
        rampTexture.Apply();

        planetMaterial.SetTexture("_ColorRamp", rampTexture);
    }

    /// <summary>
    /// Met à jour la Cubemap d'altitudes (appelée lors des modifications de terrain)
    /// </summary>
    public void UpdateShaderWithGridData(PlanetGridData grid)
    {
        if (planetMaterial == null || grid == null) return;

        if (heightCubemap == null)
        {
            heightCubemap = new Cubemap(textureResolution, TextureFormat.RFloat, false);
            heightCubemap.filterMode = FilterMode.Bilinear;
            heightCubemap.wrapMode = TextureWrapMode.Clamp;
        }

        CubemapFace[] faces = new CubemapFace[]
        {
            CubemapFace.PositiveY, // Face 0
            CubemapFace.NegativeY, // Face 1
            CubemapFace.NegativeX, // Face 2
            CubemapFace.PositiveX, // Face 3
            CubemapFace.PositiveZ, // Face 4
            CubemapFace.NegativeZ  // Face 5
        };

        for (int f = 0; f < 6; f++)
        {
            Color[] pixels = new Color[textureResolution * textureResolution];

            for (int y = 0; y < textureResolution; y++)
            {
                for (int x = 0; x < textureResolution; x++)
                {
                    float gridX = ((float)x / (textureResolution - 1)) * (grid.Resolution - 1);
                    float gridY = ((float)y / (textureResolution - 1)) * (grid.Resolution - 1);

                    float height = grid.GetHeight(f, Mathf.RoundToInt(gridX), Mathf.RoundToInt(gridY));
                    pixels[x + y * textureResolution] = new Color(height, 0f, 0f, 1f);
                }
            }

            heightCubemap.SetPixels(pixels, faces[f]);
        }

        heightCubemap.Apply();
        planetMaterial.SetTexture("_HeightCubemap", heightCubemap);
    }
}