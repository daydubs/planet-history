// BiomeTextureSetup.cs - Générateur de textures procédurales pour biomes
using UnityEngine;
using System.IO;

namespace LifeStory.Biomes
{
    /// <summary>
    /// Outil pour générer les textures de biomes procéduralement
    /// </summary>
    public class BiomeTextureSetup : MonoBehaviour
    {
        [Header("Texture Generation Settings")]
        [SerializeField] private int textureResolution = 512;
        [SerializeField] private bool generateOnAwake = false;
        [SerializeField] private string outputFolder = "Generated/BiomeTextures";

        [Header("Ocean Textures")]
        [SerializeField]
        private EvolvingOceanSettings deepOcean = new EvolvingOceanSettings
        {
            biomeName = "DeepOcean",
            sterilColor = new Color(0.15f, 0.1f, 0.05f),  // Lave refroidie
            matureColor = new Color(0.1f, 0.2f, 0.8f),    // Océan bleu profond
            waveScale = 20f,
            waveStrength = 0.3f,
            normalStrength = 0.5f
        };

        [SerializeField]
        private EvolvingOceanSettings shallowOcean = new EvolvingOceanSettings
        {
            biomeName = "ShallowOcean",
            sterilColor = new Color(0.25f, 0.15f, 0.1f),  // Lave plus claire
            matureColor = new Color(0.2f, 0.4f, 0.9f),    // Océan bleu clair
            waveScale = 15f,
            waveStrength = 0.2f,
            normalStrength = 0.3f
        };

        [Header("Land Textures")]
        [SerializeField]
        private EvolvingLandSettings beach = new EvolvingLandSettings
        {
            biomeName = "Beach",
            sterilColor = new Color(0.4f, 0.3f, 0.2f),    // Roche volcanique
            matureColor = new Color(0.9f, 0.8f, 0.6f),    // Sable doré
            noiseScale = 50f,
            grainSize = 0.8f,
            normalStrength = 0.2f
        };

        [SerializeField]
        private EvolvingLandSettings plains = new EvolvingLandSettings
        {
            biomeName = "Plains",
            sterilColor = new Color(0.3f, 0.25f, 0.2f),   // Sol stérile
            matureColor = new Color(0.4f, 0.7f, 0.2f),    // Plaines verdoyantes
            noiseScale = 30f,
            grainSize = 0.5f,
            normalStrength = 0.4f
        };

        [SerializeField]
        private EvolvingLandSettings hills = new EvolvingLandSettings
        {
            biomeName = "Hills",
            sterilColor = new Color(0.35f, 0.3f, 0.25f),  // Collines rocheuses
            matureColor = new Color(0.5f, 0.6f, 0.3f),    // Collines végétalisées
            noiseScale = 25f,
            grainSize = 0.3f,
            normalStrength = 0.6f
        };

        [SerializeField]
        private EvolvingLandSettings mountains = new EvolvingLandSettings
        {
            biomeName = "Mountains",
            sterilColor = new Color(0.4f, 0.35f, 0.3f),   // Montagnes nues
            matureColor = new Color(0.6f, 0.5f, 0.4f),    // Montagnes altérées
            noiseScale = 15f,
            grainSize = 0.2f,
            normalStrength = 0.8f
        };

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        [System.Serializable]
        public class EvolvingOceanSettings
        {
            public string biomeName;
            [Header("Evolution Colors")]
            public Color sterilColor;  // État stérile (lave/roche)
            public Color matureColor;  // État avec vie (océan bleu)
            [Header("Texture Generation")]
            public float waveScale = 20f;
            public float waveStrength = 0.3f;
            public float normalStrength = 0.5f;
        }

        [System.Serializable]
        public class EvolvingLandSettings
        {
            public string biomeName;
            [Header("Evolution Colors")]
            public Color sterilColor;  // État stérile (roche nue)
            public Color matureColor;  // État avec vie (végétation)
            [Header("Texture Generation")]
            public float noiseScale = 30f;
            public float grainSize = 0.5f;
            public float normalStrength = 0.4f;
        }

        public static BiomeTextureSetup Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LogDebug("🎨 Biome Texture Setup initialisé");

                if (generateOnAwake)
                {
                    GenerateAllBiomeTextures();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        [ContextMenu("Generate All Biome Textures")]
        public void GenerateAllBiomeTextures()
        {
            LogDebug("🎨 === GÉNÉRATION TEXTURES BIOMES ===");

            EnsureOutputFolderExists();

            // Générer textures océan
            GenerateOceanTextures(deepOcean);
            GenerateOceanTextures(shallowOcean);

            // Générer textures terrestres
            GenerateLandTextures(beach);
            GenerateLandTextures(plains);
            GenerateLandTextures(hills);
            GenerateLandTextures(mountains);

            LogDebug("✅ Toutes les textures de biomes générées");

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        private void GenerateOceanTextures(EvolvingOceanSettings settings)
        {
            LogDebug($"🌊 Génération textures océan évolutives: {settings.biomeName}");

            // Générer paire de diffuse (stérile + mature)
            Texture2D sterileDiffuse = GenerateOceanDiffuse(settings, true);
            Texture2D matureDiffuse = GenerateOceanDiffuse(settings, false);

            SaveTexture(sterileDiffuse, $"{settings.biomeName}_Sterile_Diffuse");
            SaveTexture(matureDiffuse, $"{settings.biomeName}_Mature_Diffuse");

            // Générer normal maps (partagées entre stérile/mature)
            Texture2D normal = GenerateOceanNormal(settings);
            SaveTexture(normal, $"{settings.biomeName}_Normal", true);

            LogDebug($"✅ {settings.biomeName} textures évolutives créées");
        }

        private void GenerateLandTextures(EvolvingLandSettings settings)
        {
            LogDebug($"🏔️ Génération textures terrestres évolutives: {settings.biomeName}");

            // Générer paire de diffuse (stérile + mature)
            Texture2D sterileDiffuse = GenerateLandDiffuse(settings, true);
            Texture2D matureDiffuse = GenerateLandDiffuse(settings, false);

            SaveTexture(sterileDiffuse, $"{settings.biomeName}_Sterile_Diffuse");
            SaveTexture(matureDiffuse, $"{settings.biomeName}_Mature_Diffuse");

            // Générer normal maps (partagées entre stérile/mature)
            Texture2D normal = GenerateLandNormal(settings);
            SaveTexture(normal, $"{settings.biomeName}_Normal", true);

            LogDebug($"✅ {settings.biomeName} textures évolutives créées");
        }

        private Texture2D GenerateOceanDiffuse(EvolvingOceanSettings settings, bool isSterile)
        {
            Texture2D texture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGB24, true);
            Color[] pixels = new Color[textureResolution * textureResolution];

            Color baseColor = isSterile ? settings.sterilColor : settings.matureColor;

            for (int y = 0; y < textureResolution; y++)
            {
                for (int x = 0; x < textureResolution; x++)
                {
                    float u = (float)x / textureResolution;
                    float v = (float)y / textureResolution;

                    // Générer vagues avec bruit Perlin
                    float wave1 = Mathf.PerlinNoise(u * settings.waveScale, v * settings.waveScale);
                    float wave2 = Mathf.PerlinNoise(u * settings.waveScale * 2f, v * settings.waveScale * 2f) * 0.5f;
                    float wavePattern = (wave1 + wave2) * settings.waveStrength;

                    // Variation de couleur basée sur les vagues
                    Color finalColor = Color.Lerp(
                        baseColor * 0.8f, // Plus sombre
                        baseColor * 1.2f, // Plus clair
                        wavePattern
                    );

                    // Pour stérile : ajouter aspect plus chaotique/volcanique
                    if (isSterile)
                    {
                        float chaos = Mathf.PerlinNoise(u * settings.waveScale * 4f, v * settings.waveScale * 4f) * 0.3f;
                        finalColor = Color.Lerp(finalColor, Color.red * 0.5f, chaos * 0.2f);
                    }

                    int index = y * textureResolution + x;
                    pixels[index] = finalColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private Texture2D GenerateOceanNormal(EvolvingOceanSettings settings)
        {
            Texture2D texture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGB24, true);
            Color[] pixels = new Color[textureResolution * textureResolution];

            for (int y = 0; y < textureResolution; y++)
            {
                for (int x = 0; x < textureResolution; x++)
                {
                    float u = (float)x / textureResolution;
                    float v = (float)y / textureResolution;

                    // Calculer gradients pour normal map
                    float height = Mathf.PerlinNoise(u * settings.waveScale, v * settings.waveScale);

                    float heightL = Mathf.PerlinNoise((u - 1f / textureResolution) * settings.waveScale, v * settings.waveScale);
                    float heightR = Mathf.PerlinNoise((u + 1f / textureResolution) * settings.waveScale, v * settings.waveScale);
                    float heightD = Mathf.PerlinNoise(u * settings.waveScale, (v - 1f / textureResolution) * settings.waveScale);
                    float heightU = Mathf.PerlinNoise(u * settings.waveScale, (v + 1f / textureResolution) * settings.waveScale);

                    // Calculer normale
                    Vector3 normal = new Vector3(
                        (heightL - heightR) * settings.normalStrength,
                        (heightD - heightU) * settings.normalStrength,
                        1f
                    ).normalized;

                    // Convertir vers espace normal map (0-1)
                    Color normalColor = new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z * 0.5f + 0.5f
                    );

                    int index = y * textureResolution + x;
                    pixels[index] = normalColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private Texture2D GenerateLandDiffuse(EvolvingLandSettings settings, bool isSterile)
        {
            Texture2D texture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGB24, true);
            Color[] pixels = new Color[textureResolution * textureResolution];

            Color baseColor = isSterile ? settings.sterilColor : settings.matureColor;

            for (int y = 0; y < textureResolution; y++)
            {
                for (int x = 0; x < textureResolution; x++)
                {
                    float u = (float)x / textureResolution;
                    float v = (float)y / textureResolution;

                    // Bruit principal pour variation de couleur
                    float noise1 = Mathf.PerlinNoise(u * settings.noiseScale, v * settings.noiseScale);
                    float noise2 = Mathf.PerlinNoise(u * settings.noiseScale * 2f, v * settings.noiseScale * 2f) * 0.5f;
                    float grainNoise = Mathf.PerlinNoise(u * settings.noiseScale * 8f, v * settings.noiseScale * 8f) * settings.grainSize;

                    float combinedNoise = (noise1 + noise2 + grainNoise) / 3f;

                    // Variation de couleur
                    Color finalColor = Color.Lerp(
                        baseColor * 0.7f, // Plus sombre
                        baseColor * 1.3f, // Plus clair
                        combinedNoise
                    );

                    // Pour stérile : ajouter aspect plus rocheux/désolé
                    if (isSterile)
                    {
                        float rockiness = Mathf.PerlinNoise(u * settings.noiseScale * 3f, v * settings.noiseScale * 3f);
                        finalColor = Color.Lerp(finalColor, Color.gray * 0.6f, rockiness * 0.3f);
                    }
                    // Pour mature : ajouter variation organique
                    else
                    {
                        float organicVariation = Mathf.PerlinNoise(u * settings.noiseScale * 0.5f, v * settings.noiseScale * 0.5f);
                        Color organicTint = baseColor * 1.1f;
                        finalColor = Color.Lerp(finalColor, organicTint, organicVariation * 0.2f);
                    }

                    int index = y * textureResolution + x;
                    pixels[index] = finalColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private Texture2D GenerateLandNormal(EvolvingLandSettings settings)
        {
            Texture2D texture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGB24, true);
            Color[] pixels = new Color[textureResolution * textureResolution];

            for (int y = 0; y < textureResolution; y++)
            {
                for (int x = 0; x < textureResolution; x++)
                {
                    float u = (float)x / textureResolution;
                    float v = (float)y / textureResolution;

                    // Calculer hauteur pour normal map
                    float height = Mathf.PerlinNoise(u * settings.noiseScale, v * settings.noiseScale);

                    // Échantillonner pixels voisins
                    float heightL = Mathf.PerlinNoise((u - 1f / textureResolution) * settings.noiseScale, v * settings.noiseScale);
                    float heightR = Mathf.PerlinNoise((u + 1f / textureResolution) * settings.noiseScale, v * settings.noiseScale);
                    float heightD = Mathf.PerlinNoise(u * settings.noiseScale, (v - 1f / textureResolution) * settings.noiseScale);
                    float heightU = Mathf.PerlinNoise(u * settings.noiseScale, (v + 1f / textureResolution) * settings.noiseScale);

                    // Calculer normale
                    Vector3 normal = new Vector3(
                        (heightL - heightR) * settings.normalStrength,
                        (heightD - heightU) * settings.normalStrength,
                        1f
                    ).normalized;

                    // Convertir vers espace normal map
                    Color normalColor = new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z * 0.5f + 0.5f
                    );

                    int index = y * textureResolution + x;
                    pixels[index] = normalColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void EnsureOutputFolderExists()
        {
            string fullPath = Path.Combine(Application.dataPath, outputFolder);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                LogDebug($"📁 Dossier créé: {fullPath}");
            }
        }

        private void SaveTexture(Texture2D texture, string fileName, bool isNormalMap = false)
        {
            try
            {
                byte[] pngData = texture.EncodeToPNG();
                string fullPath = Path.Combine(Application.dataPath, outputFolder, fileName + ".png");

                File.WriteAllBytes(fullPath, pngData);
                LogDebug($"💾 Texture sauvée: {fileName}.png");

#if UNITY_EDITOR
                // Configurer import settings
                string assetPath = "Assets/" + outputFolder + "/" + fileName + ".png";
                UnityEditor.AssetDatabase.ImportAsset(assetPath);

                if (isNormalMap)
                {
                    SetupNormalMapImport(assetPath);
                }
#endif
            }
            catch (System.Exception e)
            {
                LogDebug($"❌ Erreur sauvegarde {fileName}: {e.Message}");
            }
        }

#if UNITY_EDITOR
        private void SetupNormalMapImport(string assetPath)
        {
            UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
            if (importer != null)
            {
                importer.textureType = UnityEditor.TextureImporterType.NormalMap;
                importer.SaveAndReimport();
                LogDebug($"🔧 Normal map configurée: {Path.GetFileName(assetPath)}");
            }
        }
#endif

        [ContextMenu("Test Single Ocean")]
        public void TestSingleOcean()
        {
            EnsureOutputFolderExists();
            GenerateOceanTextures(deepOcean);
            LogDebug("🧪 Test océan terminé");
        }

        [ContextMenu("Test Single Land")]
        public void TestSingleLand()
        {
            EnsureOutputFolderExists();
            GenerateLandTextures(plains);
            LogDebug("🧪 Test terrain terminé");
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[BiomeTextureSetup] {message}");
            }
        }

        // === GETTERS POUR INTÉGRATION ===
        public int TextureResolution => textureResolution;
        public string OutputFolder => outputFolder;

        // Accès aux settings pour CleanBiomeSystem
        public EvolvingOceanSettings DeepOceanSettings => deepOcean;
        public EvolvingOceanSettings ShallowOceanSettings => shallowOcean;
        public EvolvingLandSettings BeachSettings => beach;
        public EvolvingLandSettings PlainsSettings => plains;
        public EvolvingLandSettings HillsSettings => hills;
        public EvolvingLandSettings MountainsSettings => mountains;
    }
}