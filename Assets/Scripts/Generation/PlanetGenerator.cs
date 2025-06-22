using UnityEngine;
using Unity.Mathematics;
using LifeStory.Core;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;
#endif


public enum PlanetSize
{
    Small,   // Rayon 5
    Medium,  // Rayon 10  
    Large    // Rayon 20
}

namespace LifeStory.Generation
{

    public class PlanetGenerator : MonoBehaviour
    {
        [Header("Sphere Settings")]
        [SerializeField] private bool useUnitySphere = false;
        [SerializeField] private bool useBlenderSphere = true; // ← NOUVEAU
        [SerializeField] private Mesh blenderSphereMesh; // ← NOUVEAU - glissez votre sphère ici
        [SerializeField] private int sphereSubdivisions = 2; // 0=20 faces, 1=80 faces, 2=320 faces, etc.
        [SerializeField] private float sphereRadius = 5f; // ← AJOUTER CETTE LIGNE


        [Header("Planet Settings")]
        [SerializeField] private float planetRadius = 5f;
        [SerializeField] private int planetResolution = 512;
        [SerializeField] private float heightMultiplier = 2f;
        [SerializeField] private AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Relief Settings - Auto-calculated")]
        [SerializeField] private float baseReliefPercentage = 20f;  // % du rayon pour le relief
        [SerializeField] private float terrainModPercentage = 30f;  // % du rayon pour modifications terrain
        [SerializeField] private bool autoCalculateMultipliers = true;


        [Header("Noise Configuration")]
        [SerializeField] private NoiseSettings continentNoise = new NoiseSettings { scale = 20f, octaves = 3 };
        [SerializeField] private NoiseSettings mountainNoise = new NoiseSettings { scale = 100f, octaves = 6 };
        [SerializeField] private NoiseSettings detailNoise = new NoiseSettings { scale = 200f, octaves = 4 };

        [Header("Biome Settings")]
        [SerializeField] private BiomeSettings biomes = new BiomeSettings();

        [Header("Generation Controls")]
        [SerializeField] private bool autoGenerate = true;
        [SerializeField] private int seed = 12345;

        [Header("Relief Settings")]
        [SerializeField] private bool useSmoothPlanet = true;        // Pour basculer plus tard
        [SerializeField] private float smoothReliefMultiplier = 0.25f;  // Relief réduit
        [SerializeField] private float normalReliefMultiplier = 1.0f;   // Relief normal
        [SerializeField] private float terrainModificationMultiplier = 0.2f; // ✅ NOUVEAU - Spécifique aux modifications terrain

        [Header("Volcanic Preservation")]
        [SerializeField] private bool preserveVolcanicModifications = true;
        private bool hasVolcanicModifications = false;

        // Composants
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;

        // Données générées
        private float[,] heightMap;
        private TerrainType[,] biomeMap;
        private Mesh planetMesh;
        private float calculatedHeightMultiplier;
        private float calculatedTerrainMultiplier;

        // Events
        public static System.Action<PlanetGenerator> OnPlanetGenerated;

        // État du système multi-matériaux
        private bool useMultiMaterialMode = false;


        /* // Dans votre UI ou menu
            public void OnPlanetSizeChanged(int sizeIndex)
            {
                 PlanetSize size = (PlanetSize)sizeIndex;
                 planetGenerator.SetPlanetSize(size);
            }  */

        internal void SetHeightMapInternal(float[,] newHeightMap, string source = "Unknown")
        {
            if (_heightMap != null && newHeightMap != null)
            {
                LogDebug($"🔒 Modification HeightMap autorisée depuis: {source}");

                // Optionnel : Validation de la source
                if (IsAuthorizedSource(source))
                {
                    _heightMap = newHeightMap;
                    LogDebug($"✅ HeightMap mise à jour par source autorisée: {source}");
                }
                else
                {
                    LogDebug($"❌ Source non autorisée pour modification HeightMap: {source}");
                }
            }
            else
            {
                _heightMap = newHeightMap;
            }
        }

        private bool IsAuthorizedSource(string source)
        {
            // Liste blanche des sources autorisées
            string[] authorizedSources = {
        "TerrainModificationManager",
        "PlanetGenerator",
        "InitialGeneration",
        "SystemMigration"
        };

            foreach (string authorized in authorizedSources)
            {
                if (source.Contains(authorized))
                {
                    return true;
                }
            }

            return false;
        }

        internal void ModifyHeightMapCell(int x, int y, float value, string source = "TerrainModificationManager")
        {
            if (_heightMap == null) return;

            if (x >= 0 && x < _heightMap.GetLength(0) && y >= 0 && y < _heightMap.GetLength(1))
            {
                if (IsAuthorizedSource(source))
                {
                    _heightMap[x, y] = value;
                }
                else
                {
                    LogDebug($"❌ Tentative modification cellule par source non autorisée: {source}");
                }
            }
        }

        internal void ModifyHeightMapRegion(int startX, int startY, float[,] regionData, string source = "TerrainModificationManager")
        {
            if (_heightMap == null || regionData == null) return;

            if (!IsAuthorizedSource(source))
            {
                LogDebug($"❌ Tentative modification région par source non autorisée: {source}");
                return;
            }

            int regionWidth = regionData.GetLength(0);
            int regionHeight = regionData.GetLength(1);

            for (int x = 0; x < regionWidth; x++)
            {
                for (int y = 0; y < regionHeight; y++)
                {
                    int mapX = startX + x;
                    int mapY = startY + y;

                    if (mapX >= 0 && mapX < _heightMap.GetLength(0) &&
                        mapY >= 0 && mapY < _heightMap.GetLength(1))
                    {
                        _heightMap[mapX, mapY] = regionData[x, y];
                    }
                }
            }

            LogDebug($"✅ Région HeightMap modifiée: {regionWidth}x{regionHeight} par {source}");
        }
        public void SetMultiMaterialMode(bool enabled)
        {
            useMultiMaterialMode = enabled;
        }

        private void LogGenerationCall(string methodName, string reason = "")
        {
            //var debugger = FindAnyObjectByType<LifeStory.Debugging.PlanetModificationDebugger>();
            //if (debugger != null)
            //{
            //    if (methodName == "GenerateInitialMesh")
            //    {
            //        debugger.OnGenerateInitialMeshCalled($"PlanetGenerator.{methodName} - {reason}");
            //    }
            //    else if (methodName == "GeneratePlanet")
            //    {
            //        debugger.OnGeneratePlanetCalled($"PlanetGenerator.{methodName} - {reason}");
            //    }
            //}

            //Debug.Log($"🌍 [PlanetGenerator] {methodName}() appelé - Raison: {reason}");
        }

        public static PlanetGenerator Instance { get; private set; }

        //private void LogToEvolutionDebugger(string methodName, string description = "")
        //{
        //    var debugger = LifeStory.Debugging.EvolutionTransitionDebugger.Instance;
        //    if (debugger != null)
        //    {
        //        debugger.LogTransitionEvent("PlanetGenerator", methodName, description);
        //    }
        //}

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeComponents();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (autoCalculateMultipliers)
            {
                CalculateAutoMultipliers();
            }

            if (autoGenerate)
            {
                StartCoroutine(DelayedGeneration());
            }
        }

        private void CalculateAutoMultipliers()
        {
            // Calcul basé sur le rayon de la planète
            calculatedHeightMultiplier = planetRadius * (baseReliefPercentage / 100f);
            calculatedTerrainMultiplier = planetRadius * (terrainModPercentage / 100f);

            LogDebug($"🔧 Multiplicateurs auto-calculés pour rayon {planetRadius}:");
            LogDebug($"   Relief de base: {calculatedHeightMultiplier:F1} ({baseReliefPercentage}% du rayon)");
            LogDebug($"   Modifications terrain: {calculatedTerrainMultiplier:F1} ({terrainModPercentage}% du rayon)");
        }


        private void GenerateBlenderSphere()
        {
            if (blenderSphereMesh == null)
            {
                Debug.LogWarning("❌ Blender sphere mesh non assigné");
                GenerateUnitySphere();
                return;
            }

            planetMesh = Instantiate(blenderSphereMesh);
            planetMesh.name = "Generated Planet from Blender";

            // NOUVEAU : Corriger l'orientation
            CorrectSphereOrientation(planetMesh);

            // Appliquer la taille
            ScaleMeshVertices(planetMesh, sphereRadius);

            // Appliquer
            meshFilter.mesh = planetMesh;
            if (meshCollider != null)
                meshCollider.sharedMesh = planetMesh;

            Debug.Log("✅ Sphère Blender générée avec orientation corrigée");
        }

        private void CorrectSphereOrientation(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;

            // Rotation de 90° sur l'axe X pour redresser la sphère
            Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = rotation * vertices[i];
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Debug.Log("🔄 Orientation de la sphère corrigée");
        }

        [ContextMenu("Test New Blender Sphere")]
        public void TestNewBlenderSphere()
        {
            if (blenderSphereMesh == null)
            {
                Debug.LogError("❌ Assignez votre nouvelle sphère Blender");
                return;
            }

            Debug.Log("🧪 TEST NOUVELLE SPHERE BLENDER");

            // Test basique sans modification
            planetMesh = Instantiate(blenderSphereMesh);
            planetMesh.name = "Test_Blender_Sphere";

            // Appliquer directement
            meshFilter.mesh = planetMesh;
            if (meshCollider != null)
                meshCollider.sharedMesh = planetMesh;

            // Matériau simple pour voir le résultat
            if (meshRenderer != null)
            {
                meshRenderer.material = new Material(Shader.Find("Standard"));
                meshRenderer.material.color = Color.white;
            }

            Debug.Log($"✅ Test appliqué - Vertices: {planetMesh.vertices.Length}");
        }

        private void GenerateSphericalUV(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = new Vector2[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i].normalized;

                // Coordonnées sphériques pour UV
                float longitude = Mathf.Atan2(vertex.x, vertex.z);
                float latitude = Mathf.Asin(vertex.y);

                float u = (longitude + Mathf.PI) / (2 * Mathf.PI);
                float v = (latitude + Mathf.PI / 2) / Mathf.PI;

                uvs[i] = new Vector2(u, v);
            }

            mesh.uv = uvs;
            Debug.Log("🗺️ UV sphériques générées automatiquement");
        }

        [ContextMenu("Fix UV Sphere for Unity")]
        public void FixUVSphereForUnity()
        {
            if (blenderSphereMesh == null)
            {
                Debug.LogError("❌ Assignez d'abord votre UV Sphere Blender dans l'inspector");
                return;
            }

            Debug.Log("🔧 Correction UV Sphere pour Unity...");

            // Corriger seulement les UV, garder la géométrie UV Sphere
            Mesh correctedMesh = Instantiate(blenderSphereMesh);
            correctedMesh.name = "UV_Sphere_Fixed";

            GenerateSphericalUVsComplete(correctedMesh);

            // Remplacer le mesh
            blenderSphereMesh = correctedMesh;

            Debug.Log("✅ UV Sphere corrigée - Prête pour terraforming");
        }

        private void GenerateSphericalUVsComplete(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = new Vector2[vertices.Length];

            Debug.Log($"🗺️ Génération UV pour {vertices.Length} vertices...");

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i].normalized;

                // Coordonnées sphériques pour UV équirectangulaires
                float longitude = Mathf.Atan2(vertex.x, vertex.z);
                float latitude = Mathf.Acos(vertex.y);

                // Conversion en UV [0,1]
                float u = (longitude + Mathf.PI) / (2 * Mathf.PI);
                float v = latitude / Mathf.PI;

                // Correction pour éviter les coutures aux bords
                if (u > 0.99f) u = 0.99f;
                if (u < 0.01f) u = 0.01f;

                uvs[i] = new Vector2(u, v);
            }

            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Debug.Log($"✅ {uvs.Length} coordonnées UV sphériques générées");
        }

        private void GenerateUnitySphere()
        {
            // Créer une sphère Unity de base
            GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh baseMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;

            // Copier le mesh pour le modifier
            planetMesh = new Mesh();
            planetMesh.name = "Generated Planet Sphere";

            // Copier les données de base
            planetMesh.vertices = baseMesh.vertices;
            planetMesh.triangles = baseMesh.triangles;
            planetMesh.normals = baseMesh.normals;
            planetMesh.uv = baseMesh.uv;

            // Subdiviser si demandé
            for (int i = 0; i < sphereSubdivisions; i++)
            {
                SubdivideMesh(planetMesh);
            }

            // Redimensionner selon planetRadius
            ScaleMeshVertices(planetMesh, sphereRadius);

            // Appliquer
            meshFilter.mesh = planetMesh;
            meshCollider.sharedMesh = planetMesh;

            // Nettoyer
            DestroyImmediate(tempSphere);

            //LogDebug($"✅ Sphère Unity générée avec {sphereSubdivisions} subdivisions");
        }

        private void SubdivideMesh(Mesh mesh)
        {
            Vector3[] oldVertices = mesh.vertices;
            int[] oldTriangles = mesh.triangles;

            List<Vector3> newVertices = new List<Vector3>(oldVertices);
            List<int> newTriangles = new List<int>();

            Dictionary<string, int> edgeVertices = new Dictionary<string, int>();

            // Subdiviser chaque triangle
            for (int i = 0; i < oldTriangles.Length; i += 3)
            {
                int v1 = oldTriangles[i];
                int v2 = oldTriangles[i + 1];
                int v3 = oldTriangles[i + 2];

                // Créer points milieux des arêtes
                int m1 = GetOrCreateEdgeVertex(v1, v2, oldVertices, newVertices, edgeVertices);
                int m2 = GetOrCreateEdgeVertex(v2, v3, oldVertices, newVertices, edgeVertices);
                int m3 = GetOrCreateEdgeVertex(v3, v1, oldVertices, newVertices, edgeVertices);

                // 4 nouveaux triangles
                newTriangles.AddRange(new int[] { v1, m1, m3 });
                newTriangles.AddRange(new int[] { m1, v2, m2 });
                newTriangles.AddRange(new int[] { m3, m2, v3 });
                newTriangles.AddRange(new int[] { m1, m2, m3 });
            }

            mesh.vertices = newVertices.ToArray();
            mesh.triangles = newTriangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        private int GetOrCreateEdgeVertex(int v1, int v2, Vector3[] oldVertices, List<Vector3> newVertices, Dictionary<string, int> edgeVertices)
        {
            string edgeKey = v1 < v2 ? $"{v1}-{v2}" : $"{v2}-{v1}";

            if (edgeVertices.ContainsKey(edgeKey))
            {
                return edgeVertices[edgeKey];
            }

            Vector3 midpoint = (oldVertices[v1] + oldVertices[v2]) * 0.5f;
            midpoint = midpoint.normalized; // Projeter sur la sphère

            newVertices.Add(midpoint);
            int newIndex = newVertices.Count - 1;
            edgeVertices[edgeKey] = newIndex;

            return newIndex;
        }

        private void ScaleMeshVertices(Mesh mesh, float scale)
        {
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] *= scale;
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }

        private System.Collections.IEnumerator DelayedGeneration()
        {
            yield return null; // Attendre un frame
            GeneratePlanet();
        }

        private void InitializeComponents()
        {
            // Ajouter les composants nécessaires
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();

            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();

            meshCollider = GetComponent<MeshCollider>();
            if (meshCollider == null)
                meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        private void LogDebug(string message)
        {
            
            {
                Debug.Log($"[PlanetGenerator] {message}");
            }
        }

        [ContextMenu("Generate Planet")]
        public void GeneratePlanet()
        {
            LogDebug("🌍 AVANT GeneratePlanet - vérification HeightMap");
            CheckHeightMapRangeIfExists("AVANT GeneratePlanet");

            //LogToEvolutionDebugger("GeneratePlanet", $"Préservation volcanique: {preserveVolcanicModifications}, HasModifications: {hasVolcanicModifications}");
            LogGenerationCall("GeneratePlanet", "Manuel ou auto-generate");

            // 🔥 NOUVEAU : Vérifier s'il y a des modifications volcaniques à préserver
            if (preserveVolcanicModifications && hasVolcanicModifications)
            {
                Debug.LogWarning("⚠️ [PlanetGenerator] MODIFICATIONS VOLCANIQUES DÉTECTÉES");
                Debug.LogWarning("   Utilisation de GeneratePlanetPreservingVolcanic() au lieu de régénération complète");

                GeneratePlanetPreservingVolcanic();
                return;
            }

            ////Debug.Log("Génération de la planète...");

            // Initialiser le générateur de nombres aléatoires
            UnityEngine.Random.InitState(seed);

            // Étapes de génération NORMALES (seulement si pas de modifications volcaniques)
            GenerateHeightMap();
            LogDebug("🌍 APRÈS GenerateHeightMap - vérification HeightMap");
            CheckHeightMapRangeIfExists("APRÈS GenerateHeightMap");
            GenerateBiomeMap();
            if (useBlenderSphere)
            {
                GenerateBlenderSphere();
            }
            else if (useUnitySphere)
            {
                GenerateUnitySphere();
            }
            else
            {
                GenerGenerateInitialMesh_ORIGINAL_SHOULD_ONLY_BE_CALLED_AT_STARTateInitialMesh();
            }
            ApplyMaterials();

            OnPlanetGenerated?.Invoke(this);
            ////Debug.Log("Planète générée avec succès !");
        }



        private void GeneratePlanetPreservingVolcanic()
        {
            Debug.Log("🌋 [PlanetGenerator] Génération préservant les modifications volcaniques");

            // Sauvegarder la HeightMap actuelle (avec modifications volcaniques)
            float[,] modifiedHeightMap = SaveCurrentHeightMap();

            // Régénérer seulement les biomes selon la nouvelle HeightMap
            GenerateBiomeMap();

            // Utiliser UpdatePlanetMesh() au lieu de GenerateInitialMesh()
            UpdatePlanetMesh();

            // Appliquer les matériaux sans régénération
            ApplyMaterialsWithoutRegeneration();

            OnPlanetGenerated?.Invoke(this);
            Debug.Log("✅ [PlanetGenerator] Génération préservante terminée");
        }

        // 4. NOUVELLE MÉTHODE : Sauvegarder la HeightMap actuelle
        private float[,] SaveCurrentHeightMap()
        {
            if (_heightMap == null) return null;

            int resolution = _heightMap.GetLength(0);
            float[,] saved = new float[resolution, resolution];

            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    saved[x, y] = _heightMap[x, y];
                }
            }

            Debug.Log("💾 [PlanetGenerator] HeightMap avec modifications volcaniques sauvegardée");
            return saved;
        }

        // 5. NOUVELLE MÉTHODE : Application matériaux sans régénération
        private void ApplyMaterialsWithoutRegeneration()
        {
            LogGenerationCall("ApplyMaterialsWithoutRegeneration", "Matériaux sans régénération mesh");

            // Chercher le système de matériaux pour le mode simple
            //PlanetMaterialSystem materialSystem = PlanetMaterialSystem.Instance;

            //if (materialSystem != null)
            //{
            //    // Utiliser le matériau de la phase actuelle SANS regénérer le mesh
            //    Material currentMaterial = materialSystem.GetCurrentMaterial();
            //    if (currentMaterial != null && meshRenderer != null)
            //    {
            //        meshRenderer.material = currentMaterial;
            //        Debug.Log($"✅ [PlanetGenerator] Matériau appliqué sans régénération: {currentMaterial.name}");
            //        return;
            //    }
            //}

            // Fallback : utiliser le matériau par défaut si le système n'est pas disponible
            if (meshRenderer != null && meshRenderer.material == null)
            {
                meshRenderer.material = new Material(Shader.Find("Standard"));
                meshRenderer.material.name = "Planet Material";
                Debug.Log("✅ [PlanetGenerator] Matériau fallback appliqué sans régénération");
            }
        }

        // 6. MÉTHODE PUBLIQUE : Marquer qu'il y a des modifications volcaniques
        public void MarkVolcanicModificationsPresent()
        {
            hasVolcanicModifications = true;
            Debug.Log("🌋 [PlanetGenerator] Modifications volcaniques marquées comme présentes");
        }

        // 7. MÉTHODE PUBLIQUE : Effacer le marquage des modifications volcaniques
        public void ClearVolcanicModificationsFlag()
        {
            hasVolcanicModifications = false;
            Debug.Log("🧹 [PlanetGenerator] Flag modifications volcaniques effacé");
        }

        // 8. MÉTHODE PUBLIQUE : Forcer la régénération complète (reset)
        [ContextMenu("Force Complete Regeneration")]
        public void ForceCompleteRegeneration()
        {
            Debug.LogWarning("🔄 [PlanetGenerator] RÉGÉNÉRATION COMPLÈTE FORCÉE - Modifications volcaniques perdues");

            hasVolcanicModifications = false;
            preserveVolcanicModifications = false;

            // Appel normal à GeneratePlanet
            GeneratePlanet();

            preserveVolcanicModifications = true;
        }


        private void GenerateHeightMap()
        {
            _heightMap = new float[PlanetResolution, PlanetResolution]; 

            // Choisir le niveau de relief selon le mode
            float reliefMultiplier = useSmoothPlanet ? smoothReliefMultiplier : normalReliefMultiplier;

            for (int x = 0; x < PlanetResolution; x++)
            {
                for (int y = 0; y < PlanetResolution; y++)
                {
                    // Coordonnées normalisées (0-1)
                    float xCoord = (float)x / PlanetResolution;
                    float yCoord = (float)y / PlanetResolution;

                    // Génération multi-couches de bruit
                    float continentHeight = GenerateNoise(xCoord, yCoord, continentNoise);
                    float mountainHeight = GenerateNoise(xCoord, yCoord, mountainNoise);
                    float detailHeight = GenerateNoise(xCoord, yCoord, detailNoise);

                    // Combinaison avec facteur de relief ajustable
                    float finalHeight = (continentHeight * 0.6f + mountainHeight * 0.3f + detailHeight * 0.1f) * reliefMultiplier;

                    // Appliquer la courbe de hauteur
                    finalHeight = heightCurve.Evaluate(finalHeight);

                    // Effet sphérique
                    float distanceFromCenter = Vector2.Distance(new Vector2(xCoord, yCoord), Vector2.one * 0.5f);
                    float sphereEffect = 1f - Mathf.Clamp01(distanceFromCenter * 2f);
                    finalHeight *= sphereEffect;

                    _heightMap[x, y] = finalHeight;
                }

            }
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;

            // Trouver min/max
            for (int x = 0; x < PlanetResolution; x++)
            {
                for (int y = 0; y < PlanetResolution; y++)
                {
                    if (_heightMap[x, y] < minHeight) minHeight = _heightMap[x, y];
                    if (_heightMap[x, y] > maxHeight) maxHeight = _heightMap[x, y];
                }
            }

            // Normaliser entre 0-1
            float range = maxHeight - minHeight;
            if (range > 0)
            {
                for (int x = 0; x < PlanetResolution; x++)
                {
                    for (int y = 0; y < PlanetResolution; y++)
                    {
                        _heightMap[x, y] = (_heightMap[x, y] - minHeight) / range;
                    }
                }
            }

            Debug.Log($"HeightMap normalisée: {minHeight}-{maxHeight} → 0-1");
        }

        private float GenerateNoise(float x, float y, NoiseSettings settings)
        {
            return 0;
            float value = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0f; 

            for (int i = 0; i < settings.octaves; i++)
            {
                float sampleX = (x + settings.offset.x) * settings.scale * frequency;
                float sampleY = (y + settings.offset.y) * settings.scale * frequency;

                float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
                value += noiseValue * amplitude;

                maxValue += amplitude;
                amplitude *= settings.persistence;
                frequency *= settings.lacunarity;
            }

            return value / maxValue;
        }

        private void GenerateBiomeMap()
        {
            biomeMap = new TerrainType[PlanetResolution, PlanetResolution];

            for (int x = 0; x < PlanetResolution; x++)
            {
                for (int y = 0; y < PlanetResolution; y++)
                {
                    float height = _heightMap[x, y];
                    biomeMap[x, y] = GetTerrainType(height);
                }
            }
        }

        private TerrainType GetTerrainType(float height)
        {
            if (height < biomes.oceanLevel)
                return TerrainType.Ocean;
            else if (height < biomes.shoreLevel)
                return TerrainType.Beach;
            else if (height < biomes.plainLevel)
                return TerrainType.Plains;
            else if (height < biomes.hillLevel)
                return TerrainType.Hills;
            else if (height < biomes.mountainLevel)
                return TerrainType.Mountains;
            else if (height < biomes.snowLevel)
                return TerrainType.Tundra;
            else
                return TerrainType.Ice;
        }

        private void GenerateInitialHeightMap()
        {
            Debug.Log("🌍 Génération HeightMap initiale (planète en fusion - surface lisse)");

            _heightMap = new float[PlanetResolution, PlanetResolution];

            // Planète initiale : complètement lisse (boule de lave en fusion)
            for (int x = 0; x < PlanetResolution; x++)
            {
                for (int y = 0; y < PlanetResolution; y++)
                {
                    // Hauteur uniforme = planète parfaitement sphérique
                    _heightMap[x, y] = 0f; // Pas de relief initial
                }
            }

            Debug.Log("✅ HeightMap initiale générée - planète parfaitement lisse");
        }



        private void GenerGenerateInitialMesh_ORIGINAL_SHOULD_ONLY_BE_CALLED_AT_STARTateInitialMesh()
        {
            //LogToEvolutionDebugger("GenerateInitialMesh", "ATTENTION: Ceci remet la planète à l'état lisse!");
            LogGenerationCall("GenerateInitialMesh", "Génération mesh initial");
            Debug.Log("🔮 Génération mesh initial (sphère parfaite)");

            // Créer les vertices
            Vector3[] vertices = new Vector3[PlanetResolution * PlanetResolution];
            Vector2[] uv = new Vector2[PlanetResolution * PlanetResolution];
            Color[] colors = new Color[PlanetResolution * PlanetResolution];

            for (int x = 0; x < PlanetResolution; x++)
            {
                for (int y = 0; y < PlanetResolution; y++)
                {
                    int index = x * PlanetResolution + y;

                    // Position sphérique
                    float u = (float)x / (PlanetResolution - 1);
                    float v = (float)y / (PlanetResolution - 1);

                    // Coordonnées sphériques
                    float theta = u * 2 * Mathf.PI; // Longitude
                    float phi = v * Mathf.PI;       // Latitude

                    // ✅ PLANÈTE INITIALE : Rayon constant (sphère parfaite)
                    float baseRadius = planetRadius;
                    float height = _heightMap[x, y] * heightMultiplier; // Sera 0 initially
                    float finalRadius = baseRadius + height;

                    // Conversion en coordonnées cartésiennes
                    float xPos = finalRadius * Mathf.Sin(phi) * Mathf.Cos(theta);
                    float yPos = finalRadius * Mathf.Cos(phi);
                    float zPos = finalRadius * Mathf.Sin(phi) * Mathf.Sin(theta);

                    vertices[index] = new Vector3(xPos, yPos, zPos);
                    uv[index] = new Vector2(u, v);
                    colors[index] = GetBiomeColor(biomeMap[x, y]);
                }
            }

            // Appliquer le mesh
            ApplyMeshToComponents(vertices, uv, colors);

            Debug.Log("✅ Mesh initial généré - sphère parfaite");
        }

        public void UpdatePlanetMesh()
        {
            LogDebug("🔄 AVANT UpdatePlanetMesh - vérification HeightMap");
            CheckHeightMapRangeIfExists("AVANT UpdatePlanetMesh");

            //LogToEvolutionDebugger("UpdatePlanetMesh", "Mise à jour avec HeightMap modifiée - PRÉSERVE le relief");
            LogGenerationCall("UpdatePlanetMesh", "Mise à jour avec HeightMap modifiée");
            Debug.Log("🌋 MISE À JOUR mesh planète avec HeightMap modifiée");

            if (_heightMap == null)
            {
                Debug.LogError("❌ Impossible de mettre à jour : HeightMap null");
                return;
            }

            hasVolcanicModifications = true;

            // ✅ CORRECTION : MODIFIER LE MESH EXISTANT AU LIEU DE LE RECRÉER
            if (useBlenderSphere && blenderSphereMesh != null)
            {
                // ✅ RÉUTILISER LE MESH EXISTANT
                if (planetMesh == null)
                {
                    // Seulement créer si pas encore de mesh
                    planetMesh = Instantiate(blenderSphereMesh);
                    planetMesh.name = "Planet Mesh (Reusable)";
                    CorrectSphereOrientation(planetMesh);
                    meshFilter.mesh = planetMesh;
                }

                // ✅ MODIFIER LES VERTICES DU MESH EXISTANT
                UpdateExistingMeshVertices();
            }
            else
            {
                // Fallback pour autres types de sphères
                UpdateFallbackMesh();
            }

            Debug.Log("✅ Mesh planète mis à jour SANS RECREATION !");
            LogDebug("🔄 APRÈS UpdatePlanetMesh - vérification HeightMap");
            CheckHeightMapRangeIfExists("APRÈS UpdatePlanetMesh");
        }

        private void UpdateExistingMeshVertices()
        {
            if (planetMesh == null) return;

            Vector3[] vertices = planetMesh.vertices;

            LogDebug($"🔧 AVANT modification - Vertices: {vertices.Length}");
            LogDebug($"🔧 HeightMap disponible: {(_heightMap != null)}");
            LogDebug($"🔧 Résolution HeightMap: {(_heightMap != null ? $"{_heightMap.GetLength(0)}x{_heightMap.GetLength(1)}" : "null")}");

            int verticesModified = 0;
            float minRadius = float.MaxValue, maxRadius = float.MinValue;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                Vector3 direction = vertex.normalized;

                // ✅ CORRECTION : Utiliser la méthode corrigée
                float heightMapValue = SampleHeightMapFromDirection(direction);

                // ✅ UTILISER planetRadius + relief proportionnel
                float newRadius = planetRadius + (heightMapValue * heightMultiplier);
                vertices[i] = direction * newRadius;

                // Statistiques
                verticesModified++;
                if (newRadius < minRadius) minRadius = newRadius;
                if (newRadius > maxRadius) maxRadius = newRadius;
            }

            LogDebug($"🔧 Vertices modifiés: {verticesModified}");
            LogDebug($"🔧 Rayon range: {minRadius:F3} → {maxRadius:F3} (relief: {maxRadius - minRadius:F3})");

            // ✅ MISE À JOUR MESH
            planetMesh.vertices = vertices;
            meshFilter.mesh = planetMesh;
            planetMesh.RecalculateNormals();
            planetMesh.RecalculateBounds();
            planetMesh.RecalculateTangents();

            LogDebug("✅ Mesh vertices mis à jour avec relief HeightMap");
        }

        public void SetPlanetSize(PlanetSize size)
        {
            switch (size)
            {
                case PlanetSize.Small:
                    planetRadius = 5f;
                    break;
                case PlanetSize.Medium:
                    planetRadius = 10f;
                    break;
                case PlanetSize.Large:
                    planetRadius = 20f;
                    break;
            }

            // Recalculer automatiquement
            if (autoCalculateMultipliers)
            {
                CalculateAutoMultipliers();
            }

            LogDebug($"🌍 Taille planète changée: {size} (rayon: {planetRadius})");
        }



        // ✅ NOUVELLE MÉTHODE : Fallback pour mesh non-Blender
        private void UpdateFallbackMesh()
        {
            if (planetMesh == null)
            {
                // Créer mesh de base si nécessaire
                GenerateUnitySphere();
                return;
            }

            // Modifier le mesh existant pour fallback aussi
            Vector3[] vertices = planetMesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                Vector3 direction = vertex.normalized;

                float heightMapValue = SampleHeightMapFromDirection(direction);
                vertices[i] = direction * (planetRadius + heightMapValue * heightMultiplier);
            }

            planetMesh.vertices = vertices;
            planetMesh.RecalculateNormals();
            planetMesh.RecalculateBounds();
        }

        private void CheckHeightMapRangeIfExists(string context)
        {
            if (_heightMap == null) return;
    
            float min = float.MaxValue, max = float.MinValue;
    
            for (int x = 0; x < PlanetResolution; x++)
            {
                for (int y = 0; y < PlanetResolution; y++)
                {
                    float value = _heightMap[x, y];
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
            }
    
            LogDebug($"📊 {context}: Plage [{min:F3}, {max:F3}]");
    
            if (max > 1.001f)
            {
                LogDebug($"⚠️ DÉPASSEMENT PLANÈTE DÉTECTÉ dans {context} ! Max: {max:F6}");
            }
        }

        private float GetHeightFromCoordinates(Vector3 direction)
        {
            // Conversion simple pour test
            float u = (Mathf.Atan2(direction.x, direction.z) + Mathf.PI) / (2 * Mathf.PI);
            float v = (Mathf.Asin(direction.y) + Mathf.PI / 2) / Mathf.PI;

            int x = Mathf.Clamp(Mathf.RoundToInt(u * (PlanetResolution - 1)), 0, PlanetResolution - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (PlanetResolution - 1)), 0, PlanetResolution - 1);

            return _heightMap[x, y];
        }

        private void ApplyHeightMapToExistingMesh()
        {
            if (planetMesh == null) return;

            Vector3[] vertices = planetMesh.vertices;

            // Appliquer les modifications de HeightMap aux vertices existants
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];

                // Convertir la position 3D vers coordonnées HeightMap (approximation)
                Vector3 direction = vertex.normalized;
                float heightMapValue = SampleHeightMapFromDirection(direction);

                // Appliquer la modification
                float heightModification = heightMapValue * heightMultiplier;
                vertices[i] = direction * (10.9f + heightModification);
            }

            planetMesh.vertices = vertices;
            planetMesh.RecalculateNormals();
            planetMesh.RecalculateBounds();
            planetMesh.RecalculateTangents();

            meshFilter.mesh = planetMesh;
            meshCollider.sharedMesh = planetMesh;
        }

        private float SampleHeightMapFromDirection(Vector3 direction)
        {
            if (_heightMap == null)
            {
                LogDebug("❌ HeightMap null dans SampleHeightMapFromDirection");
                return 0f;
            }

            // Convertir direction 3D vers coordonnées UV de HeightMap
            float longitude = Mathf.Atan2(direction.x, direction.z);
            float latitude = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f));

            float u = (longitude + Mathf.PI) / (2 * Mathf.PI);
            float v = (latitude + Mathf.PI / 2) / Mathf.PI;

            int mapResolution = _heightMap.GetLength(0);
            int x = Mathf.Clamp(Mathf.RoundToInt(u * (mapResolution - 1)), 0, mapResolution - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (mapResolution - 1)), 0, mapResolution - 1);

            float heightValue = _heightMap[x, y];

            // Debug pour premiers échantillons
            if (UnityEngine.Random.value < 0.001f) // 0.1% des échantillons
            {
                LogDebug($"🔍 Sample: Direction={direction} → UV=({u:F3},{v:F3}) → Map=({x},{y}) → Height={heightValue:F3}");
            }

            return heightValue;
        }

        // 10. GETTER public pour vérifier l'état
        public bool HasVolcanicModifications => hasVolcanicModifications;
        public bool PreservesVolcanicModifications => preserveVolcanicModifications;

        // MÉTHODE UTILITAIRE : Application mesh aux composants
        private void ApplyMeshToComponents(Vector3[] vertices, Vector2[] uv, Color[] colors)
        {
            // Créer les triangles (logique existante)
            if (useMultiMaterialMode)
            {
                CreateSubmeshes(vertices, uv, colors);
            }
            else
            {
                CreateSingleMesh(vertices, uv, colors);
            }
        }


        private void CreateSingleMesh(Vector3[] vertices, Vector2[] uv, Color[] colors)
        {
            // Créer les triangles (comme avant)
            int[] triangles = new int[(PlanetResolution - 1) * (PlanetResolution - 1) * 6];
            int triangleIndex = 0;

            for (int x = 0; x < PlanetResolution - 1; x++)
            {
                for (int y = 0; y < PlanetResolution - 1; y++)
                {
                    int topLeft = x * PlanetResolution + y;
                    int topRight = topLeft + 1;
                    int bottomLeft = (x + 1) * PlanetResolution + y;
                    int bottomRight = bottomLeft + 1;

                    triangles[triangleIndex] = topLeft;
                    triangles[triangleIndex + 1] = bottomLeft;
                    triangles[triangleIndex + 2] = topRight;

                    triangles[triangleIndex + 3] = topRight;
                    triangles[triangleIndex + 4] = bottomLeft;
                    triangles[triangleIndex + 5] = bottomRight;

                    triangleIndex += 6;
                }
            }

            // Créer le mesh simple
            if (planetMesh == null)
                planetMesh = new Mesh();
            else
                planetMesh.Clear();

            planetMesh.name = "Generated Planet";
            planetMesh.vertices = vertices;
            planetMesh.triangles = triangles;
            planetMesh.uv = uv;
            planetMesh.colors = colors;
            planetMesh.RecalculateNormals();
            planetMesh.RecalculateBounds();
            planetMesh.RecalculateTangents();

            meshFilter.mesh = planetMesh;
            meshCollider.sharedMesh = planetMesh;
        }

        private void CreateSubmeshes(Vector3[] vertices, Vector2[] uv, Color[] colors)
        {
            // Créer des listes de triangles par biome
            var biomeTriangles = new Dictionary<TerrainType, List<int>>();

            // Initialiser les listes
            foreach (TerrainType biome in System.Enum.GetValues(typeof(TerrainType)))
            {
                biomeTriangles[biome] = new List<int>();
            }

            // Assigner les triangles aux biomes
            for (int x = 0; x < PlanetResolution - 1; x++)
            {
                for (int y = 0; y < PlanetResolution - 1; y++)
                {
                    int topLeft = x * PlanetResolution + y;
                    int topRight = topLeft + 1;
                    int bottomLeft = (x + 1) * PlanetResolution + y;
                    int bottomRight = bottomLeft + 1;

                    // Déterminer le biome dominant de ce quad
                    TerrainType biome = biomeMap[x, y];

                    // Ajouter les triangles à ce biome
                    var triangleList = biomeTriangles[biome];

                    // Premier triangle
                    triangleList.Add(topLeft);
                    triangleList.Add(bottomLeft);
                    triangleList.Add(topRight);

                    // Deuxième triangle
                    triangleList.Add(topRight);
                    triangleList.Add(bottomLeft);
                    triangleList.Add(bottomRight);
                }
            }

            // Créer le mesh avec submeshes
            if (planetMesh == null)
                planetMesh = new Mesh();
            else
                planetMesh.Clear();

            planetMesh.name = "Generated Planet (Multi-Material)";
            planetMesh.vertices = vertices;
            planetMesh.uv = uv;
            planetMesh.colors = colors;

            // Définir le nombre de submeshes
            planetMesh.subMeshCount = biomeTriangles.Count;

            // Créer chaque submesh
            int submeshIndex = 0;
            foreach (var kvp in biomeTriangles)
            {
                if (kvp.Value.Count > 0)
                {
                    planetMesh.SetTriangles(kvp.Value.ToArray(), submeshIndex);
                    ////Debug.Log($"Submesh {submeshIndex} ({kvp.Key}): {kvp.Value.Count / 3} triangles");
                }
                submeshIndex++;
            }

            planetMesh.RecalculateNormals();
            planetMesh.RecalculateBounds();
            planetMesh.RecalculateTangents();

            meshFilter.mesh = planetMesh;
            meshCollider.sharedMesh = planetMesh;

            ////Debug.Log($"Created {planetMesh.subMeshCount} submeshes for biomes");
        }

        private Color GetBiomeColor(TerrainType terrainType)
        {
            // Utiliser le système de matériaux pour obtenir les bonnes couleurs selon la phase
            //PlanetMaterialSystem materialSystem = PlanetMaterialSystem.Instance;

            //if (materialSystem != null)
            //{
            //    // Obtenir la couleur selon la phase actuelle du jeu
            //    return materialSystem.GetBiomeColor(terrainType);
            //}

            // Fallback : utiliser les couleurs par défaut (phase Evolution)
            switch (terrainType)
            {
                case TerrainType.Ocean: return new Color(0.2f, 0.5f, 1f, 1f);    // Bleu océan
                case TerrainType.Beach: return new Color(1f, 0.9f, 0.7f, 1f);    // Beige sable
                case TerrainType.Plains: return new Color(0.4f, 0.8f, 0.3f, 1f);  // Vert plaines
                case TerrainType.Hills: return new Color(0.5f, 0.7f, 0.4f, 1f);  // Vert collines
                case TerrainType.Mountains: return new Color(0.6f, 0.5f, 0.4f, 1f);  // Brun montagnes
                case TerrainType.Tundra: return new Color(0.7f, 0.7f, 0.6f, 1f);  // Gris toundra
                case TerrainType.Ice: return new Color(1f, 1f, 1f, 1f);        // Blanc glace
                default: return Color.white;
            }
        }

        private void ApplyMaterials()
        {
            LogGenerationCall("ApplyMaterials", "Application matériaux");
            // Si on est en mode multi-matériaux, ne pas écraser les matériaux déjà assignés
            if (useMultiMaterialMode && meshRenderer.materials.Length > 1)
            {
                ////Debug.Log($"Mode multi-matériaux détecté - conservation des {meshRenderer.materials.Length} matériaux existants");
                return;
            }

            // Chercher le système de matériaux pour le mode simple
            //PlanetMaterialSystem materialSystem = PlanetMaterialSystem.Instance;

            //if (materialSystem != null)
            //{
            //    // Utiliser le matériau de la phase actuelle
            //    Material currentMaterial = materialSystem.GetCurrentMaterial();
            //    if (currentMaterial != null)
            //    {
            //        meshRenderer.material = currentMaterial;
            //        ////Debug.Log($"Applied single material: {currentMaterial.name}");
            //        return;
            //    }
            //}

            // Fallback : utiliser le matériau par défaut si le système n'est pas disponible
            if (meshRenderer.material == null)
            {
                meshRenderer.material = new Material(Shader.Find("Standard"));
                meshRenderer.material.name = "Planet Material";
                ////Debug.Log("Applied fallback material");
            }
        }

        // MODIFICATION UNIQUE À AJOUTER DANS PlanetGenerator.cs
        // Ajouter cette méthode publique seulement :

        /// <summary>
        /// Regénère le mesh en préservant la HeightMap actuelle (pour le terraforming volcanique)
        /// </summary>
        public void RegenerateMeshWithPreservedHeightMap()
        {
            //LogToEvolutionDebugger("RegenerateMeshWithPreservedHeightMap", "Regénération avec préservation HeightMap");
            LogGenerationCall("RegenerateMeshWithPreservedHeightMap", "Regénération avec préservation");
            Debug.Log("🔄 Regénération avec HeightMap préservée → UpdatePlanetMesh()");

            //if (heightMap == null)
            //{
            //    Debug.LogWarning("⚠️ Aucune HeightMap à préserver - génération initiale");
            //    GenerateInitialMesh();
            //    return;
            //}

            // ✅ NOUVEAU : Utiliser la méthode dédiée à la mise à jour
            UpdatePlanetMesh();

            // Régénérer les biomes selon le nouveau relief
            GenerateBiomeMap();

            // Appliquer les matériaux
            ApplyMaterials();

            // Notifier
            OnPlanetGenerated?.Invoke(this);

            Debug.Log("✅ Regénération terminée avec UpdatePlanetMesh()");
        }

        [ContextMenu("Diagnose Blender Mesh")]
        public void DiagnoseBlenderMesh()
        {
            if (blenderSphereMesh == null) return;

            Vector3[] vertices = blenderSphereMesh.vertices;

            Debug.Log("🔍 DIAGNOSTIC MESH BLENDER:");
            Debug.Log($"   Total vertices: {vertices.Length}");

            // Vérifier la répartition spatiale
            int posX = 0, negX = 0, posY = 0, negY = 0, posZ = 0, negZ = 0;

            foreach (Vector3 v in vertices)
            {
                if (v.x > 0.1f) posX++;
                if (v.x < -0.1f) negX++;
                if (v.y > 0.1f) posY++;
                if (v.y < -0.1f) negY++;
                if (v.z > 0.1f) posZ++;
                if (v.z < -0.1f) negZ++;
            }

            Debug.Log($"   Répartition X: +{posX} -{negX}");
            Debug.Log($"   Répartition Y: +{posY} -{negY}");
            Debug.Log($"   Répartition Z: +{posZ} -{negZ}");

            // Si une direction a très peu de vertices = problème
            if (negX < vertices.Length / 10 || negY < vertices.Length / 10 || negZ < vertices.Length / 10)
            {
                Debug.LogError("❌ MESH INCOMPLET - Il manque une partie de la sphère !");
            }
            else
            {
                Debug.Log("✅ Mesh semble complet spatialement");
            }
        }

        [ContextMenu("Check Normals")]
        public void CheckNormals()
        {
            if (planetMesh == null) return;

            Vector3[] normals = planetMesh.normals;
            Vector3[] vertices = planetMesh.vertices;

            Debug.Log("🔍 DIAGNOSTIC NORMALES:");
            Debug.Log($"   Normales présentes: {normals?.Length > 0}");

            if (normals != null && normals.Length > 0)
            {
                // Vérifier quelques normales
                for (int i = 0; i < Mathf.Min(5, normals.Length); i++)
                {
                    Vector3 vertex = vertices[i];
                    Vector3 normal = normals[i];
                    float dot = Vector3.Dot(vertex.normalized, normal);

                    Debug.Log($"   Normal {i}: {normal} (Dot avec vertex: {dot:F2})");
                }
            }
        }


        // Propriétés publiques pour accès externe
        private float[,] _heightMap;
        public float[,] HeightMap => _heightMap;

        public TerrainType[,] BiomeMap => biomeMap;
        public float PlanetRadius => planetRadius;
        public int Resolution => PlanetResolution;

        public MeshFilter MeshFilter { get => meshFilter; set => meshFilter = value; }
        public MeshRenderer MeshRenderer { get => meshRenderer; set => meshRenderer = value; }
        public float HeightMultiplier { get => heightMultiplier; set => heightMultiplier = value; }
        public int PlanetResolution { get => planetResolution; set => planetResolution = value; }

        // Méthodes utilitaires
        public float GetHeightAtPosition(Vector3 worldPosition)
        {
            // Convertir position mondiale en coordonnées de heightmap
            // TODO: Implémenter la conversion sphérique inverse
            return 0f;
        }

        public TerrainType GetBiomeAtPosition(Vector3 worldPosition)
        {
            // Convertir position mondiale en coordonnées de biomemap
            // TODO: Implémenter la conversion sphérique inverse
            return TerrainType.Plains;
        }

        [ContextMenu("Debug Mesh Update")]
        public void DebugMeshUpdate()
        {
            LogDebug("🔍 === DIAGNOSTIC MISE À JOUR MESH ===");

            if (_heightMap != null)
            {
                float min = float.MaxValue, max = float.MinValue;
                for (int x = 0; x < _heightMap.GetLength(0); x++)
                {
                    for (int y = 0; y < _heightMap.GetLength(1); y++)
                    {
                        float val = _heightMap[x, y];
                        if (val < min) min = val;
                        if (val > max) max = val;
                    }
                }
                LogDebug($"   HeightMap: {_heightMap.GetLength(0)}x{_heightMap.GetLength(1)}, range [{min:F3}, {max:F3}]");
            }
            else
            {
                LogDebug("   ❌ HeightMap est null !");
            }

            if (planetMesh != null)
            {
                LogDebug($"   Mesh: {planetMesh.vertexCount} vertices, bounds: {planetMesh.bounds.size}");
            }
            else
            {
                LogDebug("   ❌ planetMesh est null !");
            }

            LogDebug($"   planetRadius: {planetRadius}, heightMultiplier: {heightMultiplier}");
            LogDebug($"   hasVolcanicModifications: {hasVolcanicModifications}");

            // Forcer une mise à jour
            LogDebug("🔄 Force UpdatePlanetMesh...");
            UpdatePlanetMesh();
        }


        private void OnGUI()
        {
            return;
            GUI.Box(new Rect(Screen.width - 250, 480, 240, 80), "");  // Y changé pour éviter superposition
            GUI.Label(new Rect(Screen.width - 240, 495, 220, 20), "Génération de Planète");
            GUI.Label(new Rect(Screen.width - 240, 515, 220, 20), $"Résolution: {PlanetResolution}x{PlanetResolution}");
            if (GUI.Button(new Rect(Screen.width - 240, 535, 100, 20), "Regénérer"))
            {
                seed = UnityEngine.Random.Range(0, 100000);
                GeneratePlanet();
            }

        }
    }
}