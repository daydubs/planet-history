// BiomeMaterialEnhancer.cs - Améliorer la visibilité des biomes
using UnityEngine;
using LifeStory.Generation;
using LifeStory.Core;

public class BiomeMaterialEnhancer : MonoBehaviour
{
    [Header("Enhanced Biome Colors")]
    [SerializeField] private Color oceanColor = new Color(0.1f, 0.4f, 0.8f, 1f);      // Bleu océan
    [SerializeField] private Color shoreColor = new Color(0.9f, 0.8f, 0.6f, 1f);      // Beige sable
    [SerializeField] private Color plainsColor = new Color(0.3f, 0.7f, 0.2f, 1f);     // Vert prairie
    [SerializeField] private Color hillsColor = new Color(0.5f, 0.6f, 0.3f, 1f);      // Vert-brun collines
    [SerializeField] private Color mountainColor = new Color(0.6f, 0.5f, 0.4f, 1f);   // Brun montagne
    [SerializeField] private Color tundraColor = new Color(0.7f, 0.7f, 0.6f, 1f);     // Gris toundra
    [SerializeField] private Color iceColor = new Color(0.9f, 0.95f, 1f, 1f);         // Blanc-bleu glace

    [Header("Material Properties")]
    [SerializeField] private float oceanSmoothness = 0.9f;      // Eau lisse
    [SerializeField] private float landSmoothness = 0.2f;       // Terre rugueuse
    [SerializeField] private float mountainSmoothness = 0.1f;   // Roche rugueuse
    [SerializeField] private float iceSmoothness = 0.8f;        // Glace lisse

    [Header("Enhanced Lighting")]
    [SerializeField] private bool enableEmission = true;
    [SerializeField] private Color volcanicEmission = new Color(1f, 0.3f, 0f, 1f);

    private PlanetGenerator planetGenerator;
    private Material[] enhancedMaterials;

    private void Start()
    {
        StartCoroutine(DelayedEnhancement());
    }

    private System.Collections.IEnumerator DelayedEnhancement()
    {
        yield return new WaitForSeconds(1f);

        planetGenerator = PlanetGenerator.Instance;
        if (planetGenerator != null)
        {
            CreateEnhancedMaterials();
            ApplyEnhancedMaterials();
        }
    }

    [ContextMenu("Create Enhanced Materials")]
    public void CreateEnhancedMaterials()
    {
        Debug.Log("🎨 Création des matériaux améliorés...");

        // Créer 7 matériaux pour les 7 biomes
        enhancedMaterials = new Material[7];

        enhancedMaterials[0] = CreateBiomeMaterial("Enhanced_Ocean", oceanColor, oceanSmoothness);
        enhancedMaterials[1] = CreateBiomeMaterial("Enhanced_Shore", shoreColor, landSmoothness);
        enhancedMaterials[2] = CreateBiomeMaterial("Enhanced_Plains", plainsColor, landSmoothness);
        enhancedMaterials[3] = CreateBiomeMaterial("Enhanced_Hills", hillsColor, landSmoothness);
        enhancedMaterials[4] = CreateBiomeMaterial("Enhanced_Mountains", mountainColor, mountainSmoothness);
        enhancedMaterials[5] = CreateBiomeMaterial("Enhanced_Tundra", tundraColor, landSmoothness);
        enhancedMaterials[6] = CreateBiomeMaterial("Enhanced_Ice", iceColor, iceSmoothness);

        Debug.Log($"✅ {enhancedMaterials.Length} matériaux créés");
    }

    private Material CreateBiomeMaterial(string name, Color color, float smoothness)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = name;

        // Couleur de base
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color); // Compatibilité

        // Propriétés physiques
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", 0f); // Pas métallique

        // Amélioration visuelle pour l'océan
        if (name.Contains("Ocean"))
        {
            mat.SetFloat("_Metallic", 0.3f); // Légèrement métallique pour reflets

            // Optionnel : Alpha pour transparence
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.8f));
        }

        // Émission pour zones volcaniques (montagnes)
        if (enableEmission && name.Contains("Mountains"))
        {
            mat.SetColor("_EmissionColor", volcanicEmission * 0.1f);
            mat.EnableKeyword("_EMISSION");
        }

        return mat;
    }

    [ContextMenu("Apply Enhanced Materials")]
    public void ApplyEnhancedMaterials()
    {
        if (planetGenerator == null)
        {
            Debug.LogError("❌ PlanetGenerator non trouvé");
            return;
        }

        var renderer = planetGenerator.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            Debug.LogError("❌ MeshRenderer non trouvé");
            return;
        }

        if (enhancedMaterials == null || enhancedMaterials.Length == 0)
        {
            CreateEnhancedMaterials();
        }

        // Appliquer les matériaux
        renderer.materials = enhancedMaterials;

        // Forcer le mode multi-matériaux
        planetGenerator.SetMultiMaterialMode(true);

        // Régénérer la planète avec les nouveaux matériaux
        planetGenerator.UpdatePlanetMesh();

        Debug.Log("✅ Matériaux améliorés appliqués - Planète régénérée");
    }

    [ContextMenu("Test Extreme Contrast")]
    public void TestExtremeContrast()
    {
        Debug.Log("🧪 Test contraste extrême...");

        // Couleurs très contrastées pour test
        Color[] testColors = {
            Color.blue,      // Océan
            Color.yellow,    // Rivage
            Color.green,     // Plaines
            Color.red,       // Collines
            Color.black,     // Montagnes
            Color.gray,      // Toundra
            Color.white      // Glace
        };

        for (int i = 0; i < enhancedMaterials.Length && i < testColors.Length; i++)
        {
            if (enhancedMaterials[i] != null)
            {
                enhancedMaterials[i].SetColor("_BaseColor", testColors[i]);
            }
        }

        ApplyEnhancedMaterials();
        Debug.Log("🌈 Contraste extrême appliqué");
    }

    [ContextMenu("Show Current Biome Distribution")]
    public void ShowBiomeDistribution()
    {
        if (planetGenerator?.BiomeMap == null) return;

        int resolution = planetGenerator.Resolution;
        int[] biomeCounts = new int[7]; // 7 biomes
        int totalCells = resolution * resolution;

        // Compter chaque biome
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                TerrainType biome = planetGenerator.BiomeMap[x, y];
                if ((int)biome < biomeCounts.Length)
                {
                    biomeCounts[(int)biome]++;
                }
            }
        }

        Debug.Log("📊 DISTRIBUTION DES BIOMES:");
        string[] biomeNames = { "Ocean", "Beach", "Plains", "Hills", "Mountains", "Tundra", "Ice" };

        for (int i = 0; i < biomeCounts.Length; i++)
        {
            float percentage = (float)biomeCounts[i] / totalCells * 100f;
            Debug.Log($"  {biomeNames[i]}: {percentage:F1}% ({biomeCounts[i]} cellules)");
        }
    }

    [ContextMenu("Fix Ocean Problem")]
    public void FixOceanProblem()
    {
        Debug.Log("🔧 Correction du problème océanique...");

        // Accéder aux BiomeSettings
        if (planetGenerator != null)
        {
            var biomesField = planetGenerator.GetType().GetField("biomes",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (biomesField != null)
            {
                var biomes = biomesField.GetValue(planetGenerator);
                var biomeType = biomes.GetType();

                // Réajuster tous les seuils
                SetBiomeThreshold(biomes, biomeType, "oceanLevel", 0.05f);
                SetBiomeThreshold(biomes, biomeType, "shoreLevel", 0.1f);
                SetBiomeThreshold(biomes, biomeType, "plainLevel", 0.3f);
                SetBiomeThreshold(biomes, biomeType, "hillLevel", 0.6f);
                SetBiomeThreshold(biomes, biomeType, "mountainLevel", 0.8f);
                SetBiomeThreshold(biomes, biomeType, "snowLevel", 0.95f);

                Debug.Log("✅ Seuils de biomes réajustés");

                // Régénérer avec nouveaux seuils
                ApplyEnhancedMaterials();
            }
        }
    }

    private void SetBiomeThreshold(object biomes, System.Type biomeType, string fieldName, float value)
    {
        var field = biomeType.GetField(fieldName);
        if (field != null)
        {
            float oldValue = (float)field.GetValue(biomes);
            field.SetValue(biomes, value);
            Debug.Log($"  {fieldName}: {oldValue:F2} → {value:F2}");
        }
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(Screen.width - 220, 200, 200, 180), "");
        GUI.Label(new Rect(Screen.width - 210, 215, 180, 20), "=== BIOME ENHANCER ===");

        if (GUI.Button(new Rect(Screen.width - 210, 235, 180, 25), "Create Enhanced Materials"))
        {
            CreateEnhancedMaterials();
        }

        if (GUI.Button(new Rect(Screen.width - 210, 265, 180, 25), "Apply Materials"))
        {
            ApplyEnhancedMaterials();
        }

        if (GUI.Button(new Rect(Screen.width - 210, 295, 180, 25), "Test Extreme Contrast"))
        {
            TestExtremeContrast();
        }

        if (GUI.Button(new Rect(Screen.width - 210, 325, 180, 25), "Fix Ocean Problem"))
        {
            FixOceanProblem();
        }

        if (GUI.Button(new Rect(Screen.width - 210, 355, 180, 25), "Show Distribution"))
        {
            ShowBiomeDistribution();
        }
    }
}