// HeightMapPNGExporter.cs - Version adaptée pour EnhancedVolcanicTerraforming
// CHANGEMENTS MINIMAUX pour compatibilité avec le nouveau système

using UnityEngine;
using System.IO;
//using LifeStory.Terraforming;  // ← CHANGÉ de l'ancien namespace
using LifeStory.Generation;
using LifeStory.Core;

public class HeightMapPNGExporter : MonoBehaviour
{
    [Header("Export Settings")]
    [SerializeField] private bool autoExportOnConsolidation = true;
    [SerializeField] private bool exportSeparateLayers = true;
    [SerializeField] private int textureSize = 512;

    [Header("Visualization")]
    [SerializeField] private bool enhanceContrast = true;
    [SerializeField] private float contrastMultiplier = 5f;
    //[SerializeField] private bool colorCodeVolcanicAreas = true;

    [Header("Paths")]
    [SerializeField] private string exportFolder = "HeightMapExports";

    // Références - MISE À JOUR pour nouveau système
    //private EnhancedVolcanicTerraforming terraformingSystem;  // ← CHANGÉ le type
    private PlanetGenerator planetGenerator;

    // Stats pour diagnostic (inchangé)
    private struct HeightMapStats
    {
        public float min, max, average;
        public int modifiedCells;
        public float totalVolume;
    }

    private void Start()
    {
        // Trouver les références - MISE À JOUR
        //terraformingSystem = FindAnyObjectByType<EnhancedVolcanicTerraforming>();  // ← CHANGÉ
        planetGenerator = FindAnyObjectByType<PlanetGenerator>();

        // S'abonner aux événements (inchangé)
        if (GameManager.Instance != null)
        {
            GameManager.OnPhaseChanged += OnPhaseChanged;
        }

        // Créer le dossier d'export
        CreateExportFolder();

        Debug.Log("🖼️ HeightMapPNGExporter adapté pour EnhancedTerraforming - Exports dans : " + GetExportPath());
    }

    // ✅ MÉTHODE DE TEST RAPIDE - NOUVELLE
    [ContextMenu("Quick Before/After Test")]
    public void QuickBeforeAfterTest()
    {
        Debug.Log("📸 TEST RAPIDE AVANT/APRÈS");

        // Export AVANT
        string timestamp = System.DateTime.Now.ToString("HH-mm-ss");
        ExportHeightMapLayer(planetGenerator.HeightMap, $"BEFORE_Test_{timestamp}");

        // Attendre que l'utilisateur lance le test volcanique
        Debug.Log("🌋 Maintenant lancez 'Test Système' puis cliquez 'Export After Test'");
    }

   

   

    // Reste du code inchangé - juste mise à jour des accès aux données privées
   

    // ✅ MÉTHODE DE COMPARAISON AUTOMATIQUE
  

    private System.Collections.IEnumerator DelayedAfterExport(string timestamp, HeightMapStats statsBefore)
    {
        yield return null; // Attendre un frame

        // Export APRÈS
        var statsAfter = CalculateStats(planetGenerator.HeightMap, false);
        ExportHeightMapLayer(planetGenerator.HeightMap, $"AutoTest_AFTER_{timestamp}");

        Debug.Log($"📊 APRÈS - Min: {statsAfter.min:F6}, Max: {statsAfter.max:F6}, Range: {statsAfter.max - statsAfter.min:F6}");

        // Comparaison
        float rangeDiff = (statsAfter.max - statsAfter.min) - (statsBefore.max - statsBefore.min);
        Debug.Log($"📈 DIFFÉRENCE DE RANGE: {rangeDiff:F6}");

        if (Mathf.Abs(rangeDiff) > 0.000001f)
        {
            Debug.Log("✅ IMPACT DÉTECTÉ dans la HeightMap !");
        }
        else
        {
            Debug.Log("❌ AUCUN IMPACT détecté dans la HeightMap");
        }

        // Export des couches détaillées
       
    }

    // Toutes les autres méthodes restent identiques...
    // (CalculateStats, SampleHeightMap, HeightToColor, etc.)

    // [Le reste du code original reste inchangé]
    private void OnPhaseChanged(GamePhase newPhase)
    {
        if (autoExportOnConsolidation && newPhase == GamePhase.Evolution)
        {
            StartCoroutine(DelayedExportAfterConsolidation());
        }
    }

    private System.Collections.IEnumerator DelayedExportAfterConsolidation()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("📸 Export automatique post-consolidation...");
        ExportAllLayers();
    }

    [ContextMenu("Export All Layers")]
    public void ExportAllLayers()
    {
        if (planetGenerator?.HeightMap == null)
        {
            Debug.LogError("❌ HeightMap non disponible pour export");
            return;
        }

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        ExportHeightMapLayer(planetGenerator.HeightMap, $"Final_HeightMap_{timestamp}");

      

        Debug.Log($"✅ Export terminé dans : {GetExportPath()}");
    }

    // [Toutes les autres méthodes utilitaires restent identiques...]
    private HeightMapStats CalculateStats(float[,] heightMap, bool isVolcanicOnly)
    {
        int resolution = heightMap.GetLength(0);
        HeightMapStats stats = new HeightMapStats();

        stats.min = float.MaxValue;
        stats.max = float.MinValue;
        float sum = 0f;
        int totalCells = resolution * resolution;

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float value = heightMap[x, y];

                if (value < stats.min) stats.min = value;
                if (value > stats.max) stats.max = value;
                sum += value;

                if (isVolcanicOnly && value > 0.001f)
                {
                    stats.modifiedCells++;
                    stats.totalVolume += value;
                }
            }
        }

        stats.average = sum / totalCells;
        return stats;
    }

    private void ExportHeightMapLayer(float[,] heightMap, string filename, bool isVolcanicOnly = false)
    {
        int resolution = heightMap.GetLength(0);
        HeightMapStats stats = CalculateStats(heightMap, isVolcanicOnly);

        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                float heightValue = SampleHeightMap(heightMap, x, y, textureSize, resolution);
                Color pixelColor = HeightToColor(heightValue, stats, isVolcanicOnly);
                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();
        SaveTextureToPNG(texture, filename);
        SaveStatsReport(stats, filename, isVolcanicOnly);
        DestroyImmediate(texture);
    }

    private void ExportVolcanicHeatmap(float[,] volcanicMods, string filename)
    {
        int resolution = volcanicMods.GetLength(0);
        Texture2D heatmap = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);

        float maxIntensity = 0f;
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                if (volcanicMods[x, y] > maxIntensity)
                    maxIntensity = volcanicMods[x, y];
            }
        }

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                float intensity = SampleHeightMap(volcanicMods, x, y, textureSize, resolution);
                float normalizedIntensity = maxIntensity > 0 ? intensity / maxIntensity : 0f;
                Color heatColor = GetHeatmapColor(normalizedIntensity);
                heatmap.SetPixel(x, y, heatColor);
            }
        }

        heatmap.Apply();
        SaveTextureToPNG(heatmap, filename);
        DestroyImmediate(heatmap);

        Debug.Log($"🔥 Heatmap volcanique : Intensité max = {maxIntensity:F6}");
    }

    private float SampleHeightMap(float[,] heightMap, int texX, int texY, int texSize, int mapResolution)
    {
        float mapX = (float)texX / texSize * (mapResolution - 1);
        float mapY = (float)texY / texSize * (mapResolution - 1);

        int x0 = Mathf.FloorToInt(mapX);
        int y0 = Mathf.FloorToInt(mapY);
        int x1 = Mathf.Min(x0 + 1, mapResolution - 1);
        int y1 = Mathf.Min(y0 + 1, mapResolution - 1);

        float fx = mapX - x0;
        float fy = mapY - y0;

        float v00 = heightMap[x0, y0];
        float v10 = heightMap[x1, y0];
        float v01 = heightMap[x0, y1];
        float v11 = heightMap[x1, y1];

        float v0 = Mathf.Lerp(v00, v10, fx);
        float v1 = Mathf.Lerp(v01, v11, fx);

        return Mathf.Lerp(v0, v1, fy);
    }

    private Color HeightToColor(float height, HeightMapStats stats, bool isVolcanicOnly)
    {
        if (isVolcanicOnly)
        {
            if (height < 0.001f) return Color.black;
            float intensity = Mathf.Clamp01(height / stats.max * contrastMultiplier);
            return Color.Lerp(Color.red, Color.yellow, intensity);
        }
        else
        {
            float range = stats.max - stats.min;
            if (range < 0.001f) return Color.gray;

            float normalized = (height - stats.min) / range;
            if (enhanceContrast) normalized = Mathf.Pow(normalized, 1f / contrastMultiplier);

            float gray = Mathf.Clamp01(normalized);
            return new Color(gray, gray, gray, 1f);
        }
    }

    private Color GetHeatmapColor(float intensity)
    {
        if (intensity < 0.001f) return Color.black;

        if (intensity < 0.33f)
            return Color.Lerp(Color.black, Color.red, intensity * 3f);
        else if (intensity < 0.66f)
            return Color.Lerp(Color.red, Color.yellow, (intensity - 0.33f) * 3f);
        else
            return Color.Lerp(Color.yellow, Color.white, (intensity - 0.66f) * 3f);
    }

    private void SaveTextureToPNG(Texture2D texture, string filename)
    {
        byte[] pngData = texture.EncodeToPNG();
        string fullPath = Path.Combine(GetExportPath(), filename + ".png");
        File.WriteAllBytes(fullPath, pngData);
        Debug.Log($"💾 Sauvegardé : {fullPath}");
    }

    private void SaveStatsReport(HeightMapStats stats, string filename, bool isVolcanicOnly)
    {
        string report = $"HeightMap Analysis Report - {filename}\n";
        report += $"Generated: {System.DateTime.Now}\n\n";
        report += $"Statistics:\n";
        report += $"- Min Height: {stats.min:F6}\n";
        report += $"- Max Height: {stats.max:F6}\n";
        report += $"- Average: {stats.average:F6}\n";
        report += $"- Range: {stats.max - stats.min:F6}\n";

        if (isVolcanicOnly)
        {
            report += $"- Modified Cells: {stats.modifiedCells}\n";
            report += $"- Total Volume Added: {stats.totalVolume:F6}\n";
            report += $"- Average Addition per Modified Cell: {(stats.modifiedCells > 0 ? stats.totalVolume / stats.modifiedCells : 0):F6}\n";
        }

        string reportPath = Path.Combine(GetExportPath(), filename + "_stats.txt");
        File.WriteAllText(reportPath, report);
    }

    private void CreateExportFolder()
    {
        string path = GetExportPath();
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private string GetExportPath()
    {
        return Path.Combine(Application.dataPath, "..", exportFolder);
    }

    [ContextMenu("Test Export Current")]
    public void TestExportCurrent()
    {
        ExportHeightMapLayer(planetGenerator.HeightMap, "Test_Current_" + System.DateTime.Now.ToString("HH-mm-ss"));
    }

    [ContextMenu("Open Export Folder")]
    public void OpenExportFolder()
    {
        string path = GetExportPath();
        if (Directory.Exists(path))
        {
            Application.OpenURL("file://" + path);
        }
        else
        {
            Debug.LogWarning("⚠️ Dossier d'export n'existe pas encore");
        }
    }

   

    [ContextMenu("Direct HeightMap Test")]
    public void DirectHeightMapTest()
    {
        Debug.Log("🧪 TEST DIRECT HEIGHTMAP");

        if (planetGenerator?.HeightMap == null)
        {
            Debug.Log("❌ HeightMap est NULL");
            return;
        }

        var heightMap = planetGenerator.HeightMap;
        int resolution = heightMap.GetLength(0);

        Debug.Log($"HeightMap résolution: {resolution}x{heightMap.GetLength(1)}");

        // Test 1: Écriture directe
        heightMap[50, 50] = 0.123f;
        heightMap[51, 51] = 0.456f;
        heightMap[52, 52] = 0.789f;

        Debug.Log($"Après écriture directe [50,50]: {heightMap[50, 50]}");
        Debug.Log($"Après écriture directe [51,51]: {heightMap[51, 51]}");
        Debug.Log($"Après écriture directe [52,52]: {heightMap[52, 52]}");

        // Test 2: Export immédiat
        ExportHeightMapLayer(heightMap, "DIRECT_WRITE_TEST");
    }

   

    private void OnDestroy()
    {
        if (GameManager.OnPhaseChanged != null)
        {
            GameManager.OnPhaseChanged -= OnPhaseChanged;
        }
    }
}