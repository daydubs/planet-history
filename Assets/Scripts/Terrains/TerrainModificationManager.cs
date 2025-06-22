using UnityEngine;
using System.Collections.Generic;
using LifeStory.Core;
using LifeStory.Generation;

namespace LifeStory.Terrain
{
    public class TerrainModificationManager : MonoBehaviour
    {
        [Header("🧹 Memory Management")]
        [SerializeField] private bool enableAutoLayerCleanup = true;
        [SerializeField] private int maxLayersBeforeCleanup = 100;

        [Header("Terrain Management")]
        [SerializeField] private bool enableAutoNormalization = true;
        [SerializeField] private float normalizationInterval = 2f;

        [Header("Mesh Update Throttling")]
        [SerializeField] private float minimumMeshUpdateInterval = 0.5f;
        [SerializeField] private float batchingDelay = 0.2f;
        [SerializeField] private int maxMeshUpdatesPerSecond = 2;
        [SerializeField] private bool enableMeshUpdateThrottling = true;

        [Header("Performance Monitoring")]
        [SerializeField] private bool trackUpdateSources = true;
        [SerializeField] private bool showPerformanceStats = true;

        [Header("Zone-Based Optimization")]
        [SerializeField] private bool enableZonalRecomposition = true;
        [SerializeField] private int zonalUpdateChunkSize = 32;
        [SerializeField] private bool showZonalStats = true;

        [Header("Mesh Update Optimization")]
        [SerializeField] private bool enableZonalMeshUpdate = true;
        [SerializeField] private bool trackMeshUpdatePerformance = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // Système de couches de modifications
        private Dictionary<string, float[,]> modificationLayers = new Dictionary<string, float[,]>();
        private float[,] baseHeightMap;
        private int mapResolution;

        // Système de zones modifiées
        private HashSet<Vector2Int> modifiedZones = new HashSet<Vector2Int>();
        private Dictionary<string, HashSet<Vector2Int>> layerModifiedZones = new Dictionary<string, HashSet<Vector2Int>>();

        // Throttling mesh updates
        private float lastMeshUpdateTime = 0f;
        private float pendingMeshUpdateTime = 0f;
        private bool hasPendingMeshUpdate = false;
        private bool isMeshUpdating = false;
        private Queue<string> pendingUpdateSources = new Queue<string>();

        // Statistiques performance
        private int totalMeshUpdatesExecuted = 0;
        private int totalMeshUpdatesBlocked = 0;
        private int meshUpdatesThisSecond = 0;
        private float lastSecondCheck = 0f;
        private Dictionary<string, int> updateSources = new Dictionary<string, int>();

        // Références
        private PlanetGenerator planetGenerator;
        private bool isInitialized = false;

        // Couches prédéfinies
        public const string VOLCANIC_LAYER = "Volcanic";
        public const string RIFT_LAYER = "Rifts";
        public const string EARTHQUAKE_LAYER = "Earthquakes";
        public const string ASTEROID_LAYER = "Asteroids";

        public static TerrainModificationManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LogDebug("🌍 Terrain Modification Manager initialisé avec optimisations zonales");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            StartCoroutine(DelayedInitialization());
        }

        private void Update()
        {
            UpdateMeshThrottling();
        }

        private void UpdateMeshThrottling()
        {
            // Réinitialiser compteur par seconde
            if (Time.time - lastSecondCheck >= 1f)
            {
                meshUpdatesThisSecond = 0;
                lastSecondCheck = Time.time;
            }

            // Traiter les mises à jour mesh en attente
            if (hasPendingMeshUpdate && !isMeshUpdating && enableMeshUpdateThrottling)
            {
                float timeSinceLastUpdate = Time.time - lastMeshUpdateTime;
                float timeSincePending = Time.time - pendingMeshUpdateTime;

                bool canUpdate = timeSinceLastUpdate >= minimumMeshUpdateInterval &&
                               timeSincePending >= batchingDelay &&
                               meshUpdatesThisSecond < maxMeshUpdatesPerSecond;

                if (canUpdate)
                {
                    ExecutePendingMeshUpdate();
                }
            }
        }

        private System.Collections.IEnumerator DelayedInitialization()
        {
            yield return new WaitForSeconds(1f);

            planetGenerator = PlanetGenerator.Instance;
            if (planetGenerator == null)
            {
                LogDebug("❌ PlanetGenerator non trouvé");
                yield break;
            }

            yield return new WaitUntil(() => planetGenerator.HeightMap != null);

            mapResolution = planetGenerator.Resolution;
            InitializeImmutableBase();

            if (enableAutoNormalization)
            {
                StartCoroutine(AutoNormalizationLoop());
            }

            isInitialized = true;
            LogDebug($"✅ Terrain Manager initialisé avec optimisations - Résolution: {mapResolution}x{mapResolution}");
        }

        private void InitializeImmutableBase()
        {
            baseHeightMap = new float[mapResolution, mapResolution];
            var currentHeightMap = planetGenerator.HeightMap;

            LogDebug("💾 === SAUVEGARDE BASE IMMUABLE ===");
            float min = float.MaxValue, max = float.MinValue;

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    float value = currentHeightMap[x, y];
                    baseHeightMap[x, y] = value;

                    if (value < min) min = value;
                    if (value > max) max = value;
                }
            }

            LogDebug($"✅ Base immuable sauvegardée: [{min:F3},{max:F3}]");
        }

        // === API PUBLIQUE ===

        public void RegisterModificationLayer(string layerName, float[,] modifications, string updateSource = null)
        {
            if (!isInitialized) return;

            modificationLayers[layerName] = modifications;
            LogDebug($"📝 Couche '{layerName}' enregistrée");

            if (enableZonalRecomposition)
            {
                HashSet<Vector2Int> changedZones = CalculateModifiedZones(modifications, layerName);

                foreach (var zone in changedZones)
                {
                    modifiedZones.Add(zone);
                }

                LogDebug($"🔄 Zones modifiées détectées: {changedZones.Count} pour couche '{layerName}'");
                RecomposeModifiedZonesOnly();
            }
            else
            {
                RecomposeHeightMapFromLayers();
            }

            string source = updateSource ?? $"Layer_{layerName}";
            RequestMeshUpdate(source);
        }

        public void RequestMeshUpdate(string source = "Unknown")
        {
            if (!enableMeshUpdateThrottling)
            {
                ExecuteImmediateMeshUpdate(source);
                return;
            }

            if (trackUpdateSources)
            {
                if (!updateSources.ContainsKey(source))
                    updateSources[source] = 0;
                updateSources[source]++;
            }

            float timeSinceLastUpdate = Time.time - lastMeshUpdateTime;

            if (!isMeshUpdating &&
                timeSinceLastUpdate >= minimumMeshUpdateInterval &&
                meshUpdatesThisSecond < maxMeshUpdatesPerSecond)
            {
                ExecuteImmediateMeshUpdate(source);
            }
            else
            {
                SchedulePendingMeshUpdate(source);
            }
        }

        public void ForceImmediateMeshUpdate(string source = "Force")
        {
            ExecuteImmediateMeshUpdate(source);
        }

        // === OPTIMISATIONS ZONALES ===

        private HashSet<Vector2Int> CalculateModifiedZones(float[,] layerData, string layerName)
        {
            var zones = new HashSet<Vector2Int>();

            HashSet<Vector2Int> previousZones = null;
            if (layerModifiedZones.ContainsKey(layerName))
            {
                previousZones = layerModifiedZones[layerName];
            }

            var currentZones = new HashSet<Vector2Int>();

            for (int x = 0; x < mapResolution; x += zonalUpdateChunkSize)
            {
                for (int y = 0; y < mapResolution; y += zonalUpdateChunkSize)
                {
                    bool hasModifications = false;

                    for (int chunkX = x; chunkX < Mathf.Min(x + zonalUpdateChunkSize, mapResolution); chunkX++)
                    {
                        for (int chunkY = y; chunkY < Mathf.Min(y + zonalUpdateChunkSize, mapResolution); chunkY++)
                        {
                            if (Mathf.Abs(layerData[chunkX, chunkY]) > 0.001f)
                            {
                                hasModifications = true;
                                break;
                            }
                        }
                        if (hasModifications) break;
                    }

                    if (hasModifications)
                    {
                        Vector2Int zoneCoord = new Vector2Int(x / zonalUpdateChunkSize, y / zonalUpdateChunkSize);
                        currentZones.Add(zoneCoord);
                        zones.Add(zoneCoord);
                    }
                }
            }

            if (previousZones != null)
            {
                foreach (var previousZone in previousZones)
                {
                    if (!currentZones.Contains(previousZone))
                    {
                        zones.Add(previousZone);
                    }
                }
            }

            layerModifiedZones[layerName] = currentZones;
            return zones;
        }

        public float GetComposedHeightAt(int x, int y)
        {
            if (!isInitialized)
            {
                LogDebug("⚠️ GetComposedHeightAt appelé avant initialisation");
                return 0f;
            }

            if (!IsValidCoordinate(x, y))
            {
                LogDebug($"⚠️ Coordonnées invalides dans GetComposedHeightAt: ({x},{y}) - Résolution: {mapResolution}");
                return 0f;
            }

            // ✅ CALCUL IDENTIQUE À RecomposeModifiedZonesOnly() et RecomposeHeightMapFromLayers()
            float composedHeight = baseHeightMap[x, y];

            // Ajouter toutes les couches de modification
            foreach (var layer in modificationLayers.Values)
            {
                if (layer != null)
                {
                    composedHeight += layer[x, y];
                }
            }

            return composedHeight;
        }

        /// <summary>
        /// Version surchargée avec Vector2Int pour plus de commodité
        /// </summary>
        /// <param name="coordinates">Coordonnées HeightMap en Vector2Int</param>
        /// <returns>Hauteur composée finale</returns>
        public float GetComposedHeightAt(Vector2Int coordinates)
        {
            return GetComposedHeightAt(coordinates.x, coordinates.y);
        }

        public float GetComposedHeightAtWithDebug(int x, int y, bool debugInfo = false)
        {
            if (!IsValidCoordinate(x, y)) return 0f;

            float baseHeight = baseHeightMap[x, y];
            float totalModification = 0f;
            int activeLayersCount = 0;

            foreach (var kvp in modificationLayers)
            {
                if (kvp.Value != null)
                {
                    float layerValue = kvp.Value[x, y];
                    totalModification += layerValue;
                    activeLayersCount++;

                    if (debugInfo && Mathf.Abs(layerValue) > 0.001f)
                    {
                        LogDebug($"🔍 Couche '{kvp.Key}' à ({x},{y}): {layerValue:F6}");
                    }
                }
            }

            float finalHeight = baseHeight + totalModification;

            if (debugInfo)
            {
                LogDebug($"📊 GetComposedHeightAt({x},{y}):");
                LogDebug($"   Base: {baseHeight:F6}");
                LogDebug($"   Modifications: {totalModification:F6} ({activeLayersCount} couches)");
                LogDebug($"   Final: {finalHeight:F6}");
            }

            return finalHeight;
        }


        private void RecomposeModifiedZonesOnly()
        {
            if (modifiedZones.Count == 0) return;

            LogDebug($"🔄 === RECOMPOSITION ZONALE - {modifiedZones.Count} zones ===");

            int cellsRecomposed = 0;

            foreach (var zone in modifiedZones)
            {
                int startX = zone.x * zonalUpdateChunkSize;
                int startY = zone.y * zonalUpdateChunkSize;
                int endX = Mathf.Min(startX + zonalUpdateChunkSize, mapResolution);
                int endY = Mathf.Min(startY + zonalUpdateChunkSize, mapResolution);

                for (int x = startX; x < endX; x++)
                {
                    for (int y = startY; y < endY; y++)
                    {
                        float composedHeight = baseHeightMap[x, y];

                        foreach (var layer in modificationLayers.Values)
                        {
                            composedHeight += layer[x, y];
                        }

                        float normalizedHeight = Mathf.Clamp01(composedHeight);
                        planetGenerator.ModifyHeightMapCell(x, y, normalizedHeight, "TerrainModificationManager");
                        cellsRecomposed++;
                    }
                }
            }

            LogDebug($"✅ Recomposition zonale terminée: {cellsRecomposed} cellules recomposées (au lieu de {mapResolution * mapResolution})");

            float optimizationPercentage = (1f - (float)cellsRecomposed / (mapResolution * mapResolution)) * 100f;
            LogDebug($"🚀 Optimisation: {optimizationPercentage:F1}% de cellules évitées");

            NormalizeModifiedZones();
            modifiedZones.Clear();
        }

        private void NormalizeModifiedZones()
        {
            var heightMap = planetGenerator.HeightMap;
            float min = float.MaxValue, max = float.MinValue;
            bool needsNormalization = false;

            foreach (var zone in modifiedZones)
            {
                int startX = zone.x * zonalUpdateChunkSize;
                int startY = zone.y * zonalUpdateChunkSize;
                int endX = Mathf.Min(startX + zonalUpdateChunkSize, mapResolution);
                int endY = Mathf.Min(startY + zonalUpdateChunkSize, mapResolution);

                for (int x = startX; x < endX; x++)
                {
                    for (int y = startY; y < endY; y++)
                    {
                        float value = heightMap[x, y];
                        if (value < min) min = value;
                        if (value > max) max = value;

                        if (value > 1.001f || value < -0.001f)
                        {
                            needsNormalization = true;
                        }
                    }
                }
            }

            if (needsNormalization)
            {
                LogDebug($"🔧 Normalisation nécessaire après modification zonale: [{min:F3},{max:F3}]");
                NormalizeHeightMap();
            }
        }

        // === MESH UPDATE OPTIMISÉ ===

        private void ExecuteImmediateMeshUpdate(string source)
        {
            if (planetGenerator == null) return;

            isMeshUpdating = true;

            try
            {
                var meshBefore = planetGenerator.MeshFilter?.mesh;
                int instanceIDBefore = meshBefore?.GetInstanceID() ?? 0;

                UpdatePlanetMeshAutonomous();

                var meshAfter = planetGenerator.MeshFilter?.mesh;
                int instanceIDAfter = meshAfter?.GetInstanceID() ?? 0;

                if (meshBefore != null && instanceIDBefore != instanceIDAfter)
                {
                    LogDebug($"⚠️ RECREATION MESH DÉTECTÉE ! {instanceIDBefore} → {instanceIDAfter}");
                }
                else
                {
                    LogDebug($"✅ Mesh modifié sans recreation - Source: {source}");
                }

                lastMeshUpdateTime = Time.time;
                meshUpdatesThisSecond++;
                totalMeshUpdatesExecuted++;

            }
            catch (System.Exception e)
            {
                LogDebug($"❌ Erreur mesh update: {e.Message}");
            }
            finally
            {
                isMeshUpdating = false;
            }
        }

        private void UpdatePlanetMeshAutonomous()
        {
            LogDebug("🔄 === DÉBUT UPDATE MESH AUTONOME ===");

            if (planetGenerator?.MeshFilter?.mesh == null)
            {
                LogDebug("❌ Mesh ou MeshFilter non disponible");
                return;
            }

            var mesh = planetGenerator.MeshFilter.mesh;
            var heightMap = planetGenerator.HeightMap;

            if (heightMap == null)
            {
                LogDebug("❌ HeightMap non disponible");
                return;
            }

            planetGenerator.MarkVolcanicModificationsPresent();

            if (enableZonalMeshUpdate && modifiedZones.Count > 0)
            {
                UpdateMeshVerticesZonal(mesh, heightMap);
            }
            else
            {
                UpdateMeshVerticesFromHeightMap(mesh, heightMap);
            }

            LogDebug("✅ Update mesh autonome terminé");
        }

        private void UpdateMeshVerticesZonal(Mesh mesh, float[,] heightMap)
        {
            Vector3[] vertices = mesh.vertices;

            LogDebug($"🔧 Déformation zonale {modifiedZones.Count} zones...");

            int verticesModified = 0;
            float startTime = Time.realtimeSinceStartup;

            HashSet<Vector2Int> modifiedCells = new HashSet<Vector2Int>();

            foreach (var zone in modifiedZones)
            {
                int startX = zone.x * zonalUpdateChunkSize;
                int startY = zone.y * zonalUpdateChunkSize;
                int endX = Mathf.Min(startX + zonalUpdateChunkSize, mapResolution);
                int endY = Mathf.Min(startY + zonalUpdateChunkSize, mapResolution);

                for (int x = startX; x < endX; x++)
                {
                    for (int y = startY; y < endY; y++)
                    {
                        modifiedCells.Add(new Vector2Int(x, y));
                    }
                }
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                Vector3 direction = vertex.normalized;

                Vector2Int mapCoords = DirectionToMapCoordinates(direction, heightMap.GetLength(0));

                if (modifiedCells.Contains(mapCoords))
                {
                    float heightMapValue = SampleHeightMapFromDirection(direction, heightMap);
                    float newRadius = planetGenerator.PlanetRadius + (heightMapValue * planetGenerator.HeightMultiplier);
                    vertices[i] = direction * newRadius;
                    verticesModified++;
                }
            }

            float updateTime = (Time.realtimeSinceStartup - startTime) * 1000f;

            LogDebug($"🔧 Vertices modifiés: {verticesModified}/{vertices.Length} en {updateTime:F1}ms");
            LogDebug($"🚀 Optimisation mesh: {(1f - (float)verticesModified / vertices.Length) * 100f:F1}% vertices évités");

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            planetGenerator.MeshFilter.mesh = mesh;

            LogDebug("✅ Mesh vertices mis à jour avec optimisation zonale");
        }

        private Vector2Int DirectionToMapCoordinates(Vector3 direction, int resolution)
        {
            float longitude = Mathf.Atan2(direction.x, direction.z);
            float latitude = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f));

            float u = (longitude + Mathf.PI) / (2 * Mathf.PI);
            float v = (latitude + Mathf.PI / 2) / Mathf.PI;

            int x = Mathf.Clamp(Mathf.RoundToInt(u * (resolution - 1)), 0, resolution - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (resolution - 1)), 0, resolution - 1);

            return new Vector2Int(x, y);
        }

        private void UpdateMeshVerticesFromHeightMap(Mesh mesh, float[,] heightMap)
        {
            Vector3[] vertices = mesh.vertices;

            // ✅ NOUVEAU - Sauvegarder les couleurs existantes AVANT modification
            Color[] existingColors = mesh.colors;
            bool hasColors = existingColors != null && existingColors.Length == vertices.Length;

            LogDebug($"🔧 Déformation {vertices.Length} vertices selon HeightMap...");
            if (hasColors)
            {
                LogDebug($"💾 Préservation de {existingColors.Length} couleurs vertex existantes");
            }

            int verticesModified = 0;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                Vector3 direction = vertex.normalized;

                float heightMapValue = SampleHeightMapFromDirection(direction, heightMap);
                float newRadius = planetGenerator.PlanetRadius + (heightMapValue * planetGenerator.HeightMultiplier);
                vertices[i] = direction * newRadius;
                verticesModified++;
            }

            LogDebug($"🔧 Vertices modifiés: {verticesModified}");

            mesh.vertices = vertices;

            // ✅ NOUVEAU - Restaurer les couleurs après modification vertices
            if (hasColors)
            {
                mesh.colors = existingColors;
                LogDebug("🎨 Couleurs vertex restaurées après modification terrain");
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            planetGenerator.MeshFilter.mesh = mesh;

            LogDebug("✅ Mesh vertices mis à jour avec relief HeightMap");
        }

        private float SampleHeightMapFromDirection(Vector3 direction, float[,] heightMap)
        {
            if (heightMap == null) return 0f;

            float longitude = Mathf.Atan2(direction.x, direction.z);
            float latitude = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f));

            float u = (longitude + Mathf.PI) / (2 * Mathf.PI);
            float v = (latitude + Mathf.PI / 2) / Mathf.PI;

            int heightMapResolution = heightMap.GetLength(0);
            int x = Mathf.Clamp(Mathf.RoundToInt(u * (heightMapResolution - 1)), 0, heightMapResolution - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (heightMapResolution - 1)), 0, heightMapResolution - 1);

            return heightMap[x, y];
        }

        // === MÉTHODES THROTTLING ===

        private void SchedulePendingMeshUpdate(string source)
        {
            if (!hasPendingMeshUpdate)
            {
                pendingMeshUpdateTime = Time.time;
                hasPendingMeshUpdate = true;
                pendingUpdateSources.Clear();
            }

            pendingUpdateSources.Enqueue(source);
            totalMeshUpdatesBlocked++;

            LogDebug($"⏳ Mesh update programmé - Source: {source} | Bloqués: {totalMeshUpdatesBlocked}");
        }

        private void ExecutePendingMeshUpdate()
        {
            if (planetGenerator == null)
            {
                hasPendingMeshUpdate = false;
                return;
            }

            isMeshUpdating = true;
            hasPendingMeshUpdate = false;

            string groupedSources = string.Join(", ", pendingUpdateSources.ToArray());

            try
            {
                UpdatePlanetMeshAutonomous();

                lastMeshUpdateTime = Time.time;
                meshUpdatesThisSecond++;
                totalMeshUpdatesExecuted++;

                LogDebug($"🔄 Mesh update groupé exécuté - Sources: {groupedSources}");
            }
            catch (System.Exception e)
            {
                LogDebug($"❌ Erreur mesh update groupé: {e.Message}");
            }
            finally
            {
                isMeshUpdating = false;
                pendingUpdateSources.Clear();
            }
        }

        // === MÉTHODES LEGACY ===

        public void ApplyAllModifications(bool updateMesh = true)
        {
            if (enableZonalRecomposition)
            {
                RecomposeModifiedZonesOnly();
            }
            else
            {
                RecomposeHeightMapFromLayers();
            }

            if (updateMesh)
            {
                RequestMeshUpdate("ApplyAllModifications");
            }
        }

        private void RecomposeHeightMapFromLayers()
        {
            if (!isInitialized) return;

            LogDebug("🔄 === RECOMPOSITION COMPLÈTE (Mode Legacy) ===");

            float[,] composedHeightMap = new float[mapResolution, mapResolution];

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    float composedHeight = baseHeightMap[x, y];

                    foreach (var layer in modificationLayers.Values)
                    {
                        composedHeight += layer[x, y];
                        composedHeightMap[x, y] = Mathf.Clamp01(composedHeight);
                    }

                    composedHeightMap[x, y] = composedHeight;
                }
            }

            planetGenerator.SetHeightMapInternal(composedHeightMap, "TerrainModificationManager");

            LogDebug($"✅ HeightMap recomposée COMPLÈTE avec {modificationLayers.Count} couches");
            NormalizeHeightMap();
        }

        public void ModifyPoint(string layerName, int x, int y, float value)
        {
            if (!isInitialized) return;

            if (!modificationLayers.ContainsKey(layerName))
            {
                modificationLayers[layerName] = new float[mapResolution, mapResolution];
            }

            modificationLayers[layerName][x, y] = value;

            if (enableZonalRecomposition)
            {
                Vector2Int zone = new Vector2Int(x / zonalUpdateChunkSize, y / zonalUpdateChunkSize);
                modifiedZones.Add(zone);
                RecomposeModifiedZonesOnly();
            }
            else
            {
                RecomposeHeightMapFromLayers();
            }

            RequestMeshUpdate($"ModifyPoint_{layerName}");
        }

        public void ModifyPoints(string layerName, List<Vector2Int> points, float value)
        {
            if (!isInitialized) return;

            if (!modificationLayers.ContainsKey(layerName))
            {
                modificationLayers[layerName] = new float[mapResolution, mapResolution];
            }

            var layer = modificationLayers[layerName];
            foreach (var point in points)
            {
                if (IsValidCoordinate(point.x, point.y))
                {
                    layer[point.x, point.y] = value;

                    if (enableZonalRecomposition)
                    {
                        Vector2Int zone = new Vector2Int(point.x / zonalUpdateChunkSize, point.y / zonalUpdateChunkSize);
                        modifiedZones.Add(zone);
                    }
                }
            }

            LogDebug($"📝 {points.Count} points modifiés dans couche '{layerName}'");

            if (enableZonalRecomposition)
            {
                RecomposeModifiedZonesOnly();
            }
            else
            {
                RecomposeHeightMapFromLayers();
            }

            RequestMeshUpdate($"ModifyPoints_{layerName}");
        }

        public void RemoveLayer(string layerName)
        {
            if (modificationLayers.ContainsKey(layerName))
            {
                modificationLayers.Remove(layerName);
                LogDebug($"🗑️ Couche '{layerName}' supprimée");

                if (enableZonalRecomposition)
                {
                    RecomposeModifiedZonesOnly();
                }
                else
                {
                    RecomposeHeightMapFromLayers();
                }

                RequestMeshUpdate($"RemoveLayer_{layerName}");
            }
        }

        private void NormalizeHeightMap()
        {
            var heightMap = planetGenerator.HeightMap;

            float min = float.MaxValue, max = float.MinValue;
            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    float value = heightMap[x, y];
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
            }

            if (max > 1.001f || min < -0.001f)
            {
                float range = max - min;
                if (range > 0.001f)
                {
                    for (int x = 0; x < mapResolution; x++)
                    {
                        for (int y = 0; y < mapResolution; y++)
                        {
                            heightMap[x, y] = (heightMap[x, y] - min) / range;
                        }
                    }
                    LogDebug($"🔧 HeightMap normalisée: [{min:F3},{max:F3}] → [0,1]");
                }
            }
        }

        private System.Collections.IEnumerator AutoNormalizationLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(normalizationInterval);

                if (modificationLayers.Count > 0)
                {
                    ApplyAllModifications(true);
                }
            }
        }

        private bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < mapResolution && y >= 0 && y < mapResolution;
        }

        private void LogValueExceeded(float originalValue, Vector2Int position, string layerInfo = "")
        {
            if (originalValue > 1.001f || originalValue < -0.001f)
            {
                LogDebug($"⚠️ Valeur normalisée: {originalValue:F3} → [0,1] à ({position.x},{position.y}) {layerInfo}");
            }
        }

        // === MÉTHODES DEBUG ===

        [ContextMenu("🧹 Clear All Terrain Modifications")]
        public void ClearAllTerrainModifications()
        {
            LogDebug("🧹 === NETTOYAGE COMPLET TERRAIN ===");

            int layersCleared = modificationLayers.Count;
            int zonesCleared = modifiedZones.Count;
            int layerZonesCleared = layerModifiedZones.Count;

            // 1. Vider toutes les couches de modification
            modificationLayers.Clear();
            LogDebug($"✅ {layersCleared} couches de modification supprimées");

            // 2. Vider les zones modifiées
            modifiedZones.Clear();
            LogDebug($"✅ {zonesCleared} zones modifiées effacées");

            // 3. Vider le tracking des zones par couche
            layerModifiedZones.Clear();
            LogDebug($"✅ {layerZonesCleared} mappings couche-zones supprimés");

            // 4. Réinitialiser les statistiques
            totalMeshUpdatesExecuted = 0;
            totalMeshUpdatesBlocked = 0;
            updateSources.Clear();
            pendingUpdateSources.Clear();
            hasPendingMeshUpdate = false;
            isMeshUpdating = false;
            LogDebug("✅ Statistiques réinitialisées");

            // 5. Restaurer heightmap de base
            if (baseHeightMap != null && planetGenerator?.HeightMap != null)
            {
                var heightMap = planetGenerator.HeightMap;
                for (int x = 0; x < mapResolution; x++)
                {
                    for (int y = 0; y < mapResolution; y++)
                    {
                        heightMap[x, y] = baseHeightMap[x, y];
                    }
                }
                LogDebug("✅ HeightMap restaurée à l'état de base");
            }

            // 6. Forcer mise à jour mesh
            if (planetGenerator != null)
            {
                UpdatePlanetMeshAutonomous();
                LogDebug("✅ Mesh planète mise à jour");
            }

            // 7. Forcer garbage collection
            System.GC.Collect();

            LogDebug($"🎯 NETTOYAGE TERMINÉ - Libéré: {layersCleared} couches, {zonesCleared} zones");
        }


        [ContextMenu("🧹 Clear Volcanic Layers Only")]
        public void ClearVolcanicLayersOnly()
        {
            LogDebug("🌋 Nettoyage couches volcaniques uniquement");

            var keysToRemove = new List<string>();
            foreach (var key in modificationLayers.Keys)
            {
                if (key.Contains("Volcano") || key.Contains("VOLCANIC"))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                modificationLayers.Remove(key);
                if (layerModifiedZones.ContainsKey(key))
                {
                    layerModifiedZones.Remove(key);
                }
            }

            LogDebug($"🌋 {keysToRemove.Count} couches volcaniques supprimées");

            // Recomposer sans les couches volcaniques
            RecomposeHeightMapFromLayers();
            RequestMeshUpdate("ClearVolcanicLayers");
        }


        [ContextMenu("Show Performance Stats")]
        public void ShowPerformanceStats()
        {
            LogDebug("📊 === STATISTIQUES PERFORMANCE TERRAIN ===");
            LogDebug($"   Mesh updates exécutés: {totalMeshUpdatesExecuted}");
            LogDebug($"   Mesh updates bloqués: {totalMeshUpdatesBlocked}");
            LogDebug($"   Dernière update: {Time.time - lastMeshUpdateTime:F1}s ago");
            LogDebug($"   Updates/seconde: {meshUpdatesThisSecond}/{maxMeshUpdatesPerSecond}");
            LogDebug($"   En cours: {(isMeshUpdating ? "OUI" : "NON")}");
            LogDebug($"   En attente: {(hasPendingMeshUpdate ? "OUI" : "NON")}");
            LogDebug($"   Couches actives: {modificationLayers.Count}");

            if (trackUpdateSources && updateSources.Count > 0)
            {
                LogDebug("📈 SOURCES DES DEMANDES:");
                foreach (var kvp in updateSources)
                {
                    LogDebug($"   {kvp.Key}: {kvp.Value} demandes");
                }
            }

            float efficiency = totalMeshUpdatesExecuted > 0 ?
                (float)totalMeshUpdatesExecuted / (totalMeshUpdatesExecuted + totalMeshUpdatesBlocked) * 100f : 100f;
            LogDebug($"💡 Efficacité: {efficiency:F1}%");
        }

        public void RegisterModificationLayer(string layerName, float[,] modificationData)
        {
            if (!isInitialized)
            {
                LogDebug($"⚠️ Tentative d'enregistrement de couche '{layerName}' avant initialisation");
                return;
            }

            if (modificationData == null)
            {
                LogDebug($"❌ Données de modification nulles pour couche '{layerName}'");
                return;
            }

            // === NETTOYAGE AUTOMATIQUE ===
            if (enableAutoLayerCleanup && modificationLayers.Count >= maxLayersBeforeCleanup)
            {
                LogDebug($"⚠️ Auto-nettoyage: {modificationLayers.Count} couches → seuil {maxLayersBeforeCleanup}");
                ClearOldestLayers(modificationLayers.Count - maxLayersBeforeCleanup + 1);
            }

            // === ENREGISTREMENT COUCHE (LOGIQUE ORIGINALE) ===
            if (modificationData.GetLength(0) != mapResolution || modificationData.GetLength(1) != mapResolution)
            {
                LogDebug($"❌ Taille incorrecte pour couche '{layerName}': {modificationData.GetLength(0)}x{modificationData.GetLength(1)} (attendu: {mapResolution}x{mapResolution})");
                return;
            }

            // Enregistrer la couche
            modificationLayers[layerName] = modificationData;

            // Tracking des zones modifiées si activé
            if (enableZonalRecomposition)
            {
                TrackModifiedZonesForLayer(layerName, modificationData);
            }

            LogDebug($"📝 Couche '{layerName}' enregistrée ({modificationLayers.Count} couches actives)");

            // Recomposition
            if (enableZonalRecomposition)
            {
                RecomposeModifiedZonesOnly();
            }
            else
            {
                RecomposeHeightMapFromLayers();
            }

            // Demander mise à jour mesh
            RequestMeshUpdate($"RegisterLayer_{layerName}");
        }

        private void TrackModifiedZonesForLayer(string layerName, float[,] modificationData)
        {
            if (!layerModifiedZones.ContainsKey(layerName))
            {
                layerModifiedZones[layerName] = new HashSet<Vector2Int>();
            }

            var layerZones = layerModifiedZones[layerName];
            layerZones.Clear();

            // Parcourir la couche pour trouver les zones modifiées
            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    if (Mathf.Abs(modificationData[x, y]) > 0.001f) // Seuil de modification
                    {
                        Vector2Int zone = new Vector2Int(x / zonalUpdateChunkSize, y / zonalUpdateChunkSize);
                        layerZones.Add(zone);
                        modifiedZones.Add(zone);
                    }
                }
            }

            LogDebug($"   📍 Couche '{layerName}': {layerZones.Count} zones modifiées détectées");
        }


        private void ClearOldestLayers(int layersToRemove)
        {
            var layersList = new List<string>(modificationLayers.Keys);

            // Prendre les premières (plus anciennes) de la liste
            for (int i = 0; i < Mathf.Min(layersToRemove, layersList.Count); i++)
            {
                string layerToRemove = layersList[i];
                modificationLayers.Remove(layerToRemove);

                if (layerModifiedZones.ContainsKey(layerToRemove))
                {
                    layerModifiedZones.Remove(layerToRemove);
                }

                LogDebug($"🗑️ Couche ancienne supprimée: {layerToRemove}");
            }

            LogDebug($"🧹 {layersToRemove} couches anciennes supprimées automatiquement");
        }

        [ContextMenu("📊 Show Memory Usage")]
        public void ShowMemoryUsage()
        {
            LogDebug("📊 === UTILISATION MÉMOIRE TERRAIN ===");

            // Calculer mémoire des couches
            int bytesPerFloat = sizeof(float);
            long totalMemory = 0;

            foreach (var layer in modificationLayers)
            {
                long layerMemory = (long)mapResolution * mapResolution * bytesPerFloat;
                totalMemory += layerMemory;
            }

            LogDebug($"📏 Résolution heightmap: {mapResolution}x{mapResolution}");
            LogDebug($"📊 Couches actives: {modificationLayers.Count}");
            LogDebug($"💾 Mémoire par couche: {(mapResolution * mapResolution * bytesPerFloat) / 1024 / 1024:F1} MB");
            LogDebug($"💾 Mémoire totale couches: {totalMemory / 1024 / 1024:F1} MB");
            LogDebug($"🗺️ Zones modifiées: {modifiedZones.Count}");
            LogDebug($"📍 Zones trackées par couche: {layerModifiedZones.Count}");

            // Suggestions
            if (totalMemory > 100 * 1024 * 1024) // > 100MB
            {
                LogDebug("⚠️ RECOMMANDATION: Mémoire élevée, considérer nettoyage");
            }
        }



        [ContextMenu("Show Zonal Statistics")]
        public void ShowZonalStatistics()
        {
            LogDebug("📊 === STATISTIQUES ZONALES ===");
            LogDebug($"   Mode zonal activé: {enableZonalRecomposition}");
            LogDebug($"   Mesh zonal activé: {enableZonalMeshUpdate}");
            LogDebug($"   Taille chunk: {zonalUpdateChunkSize}x{zonalUpdateChunkSize}");
            LogDebug($"   Zones modifiées en attente: {modifiedZones.Count}");
            LogDebug($"   Couches avec zones trackées: {layerModifiedZones.Count}");

            foreach (var kvp in layerModifiedZones)
            {
                LogDebug($"   Couche '{kvp.Key}': {kvp.Value.Count} zones modifiées");
            }

            int totalChunksX = Mathf.CeilToInt((float)mapResolution / zonalUpdateChunkSize);
            int totalChunksY = Mathf.CeilToInt((float)mapResolution / zonalUpdateChunkSize);
            int totalChunks = totalChunksX * totalChunksY;

            LogDebug($"   Chunks totaux possibles: {totalChunks} ({totalChunksX}x{totalChunksY})");

            if (modifiedZones.Count > 0)
            {
                float modificationPercentage = (float)modifiedZones.Count / totalChunks * 100f;
                LogDebug($"   Pourcentage terrain modifié: {modificationPercentage:F1}%");
            }
        }

        [ContextMenu("Toggle Zonal Recomposition")]
        public void ToggleZonalRecomposition()
        {
            enableZonalRecomposition = !enableZonalRecomposition;
            LogDebug($"🔄 Recomposition zonale: {(enableZonalRecomposition ? "✅ ACTIVÉE" : "❌ DÉSACTIVÉE")}");
        }

        [ContextMenu("Toggle Zonal Mesh Update")]
        public void ToggleZonalMeshUpdate()
        {
            enableZonalMeshUpdate = !enableZonalMeshUpdate;
            LogDebug($"🔄 Mesh update zonal: {(enableZonalMeshUpdate ? "✅ ACTIVÉ" : "❌ DÉSACTIVÉ")}");
        }

        [ContextMenu("Clear Modified Zones")]
        public void ClearModifiedZones()
        {
            int zonesToClear = modifiedZones.Count;
            modifiedZones.Clear();
            layerModifiedZones.Clear();
            LogDebug($"🧹 {zonesToClear} zones modifiées effacées");
        }

        [ContextMenu("Force Full Recomposition")]
        public void ForceFullRecomposition()
        {
            LogDebug("🔄 Force recomposition complète...");
            bool originalZonal = enableZonalRecomposition;
            enableZonalRecomposition = false;
            RecomposeHeightMapFromLayers();
            enableZonalRecomposition = originalZonal;
            LogDebug("✅ Recomposition complète forcée terminée");
        }

        [ContextMenu("Show Mesh Update Performance")]
        public void ShowMeshUpdatePerformance()
        {
            LogDebug("📊 === PERFORMANCE MESH UPDATE ===");
            LogDebug($"   Mode zonal mesh: {enableZonalMeshUpdate}");
            LogDebug($"   Zones en attente: {modifiedZones.Count}");

            if (planetGenerator?.MeshFilter?.mesh != null)
            {
                int totalVertices = planetGenerator.MeshFilter.mesh.vertexCount;
                LogDebug($"   Total vertices mesh: {totalVertices:N0}");

                if (enableZonalMeshUpdate && modifiedZones.Count > 0)
                {
                    int estimatedVertices = modifiedZones.Count * zonalUpdateChunkSize * zonalUpdateChunkSize;
                    float percentage = (float)estimatedVertices / totalVertices * 100f;
                    LogDebug($"   Vertices estimés à modifier: {estimatedVertices:N0} ({percentage:F1}%)");
                }
            }
        }

        [ContextMenu("📊 Show Layer Count")]
        public void ShowLayerCount()
        {
            // Adapter selon votre implémentation
            Debug.Log($"📊 Couches de modification actives: [À IMPLÉMENTER]");
            // Si vous avez un Dictionary<string, float[,]>, afficher .Count
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[TerrainManager] {message}");
            }
        }

        // === GETTERS ===
        public bool IsInitialized => isInitialized;
        public List<string> GetActiveLayers() => new List<string>(modificationLayers.Keys);
        public bool IsMeshUpdating => isMeshUpdating;
        public bool HasPendingMeshUpdate => hasPendingMeshUpdate;
        public int TotalMeshUpdatesExecuted => totalMeshUpdatesExecuted;
        public int TotalMeshUpdatesBlocked => totalMeshUpdatesBlocked;

        // === GUI DEBUG ===
        private void OnGUI()
        {
            if (!showPerformanceStats) return;

            GUI.Box(new Rect(10, 1280, 400, 140), "");
            GUI.Label(new Rect(20, 1295, 380, 20), "=== TERRAIN PERFORMANCE (Optimisé) ===");

            GUI.Label(new Rect(20, 1315, 380, 20), $"Mesh Updates: {totalMeshUpdatesExecuted} | Bloqués: {totalMeshUpdatesBlocked}");
            GUI.Label(new Rect(20, 1335, 380, 20), $"Couches: {modificationLayers.Count} | Updates/sec: {meshUpdatesThisSecond}/{maxMeshUpdatesPerSecond}");

            GUI.color = isMeshUpdating ? Color.red : (hasPendingMeshUpdate ? Color.yellow : Color.green);
            string status = isMeshUpdating ? "MESH UPDATE" : (hasPendingMeshUpdate ? "EN ATTENTE" : "PRÊT");
            GUI.Label(new Rect(20, 1355, 380, 20), $"Statut: {status}");
            GUI.color = Color.white;

            GUI.Label(new Rect(20, 1375, 380, 20), $"Zonal: {(enableZonalRecomposition ? "ON" : "OFF")} | Mesh: {(enableZonalMeshUpdate ? "ON" : "OFF")} | Zones: {modifiedZones.Count}");

            if (GUI.Button(new Rect(20, 1395, 50, 20), "Stats"))
            {
                ShowPerformanceStats();
            }

            if (GUI.Button(new Rect(80, 1395, 50, 20), "Zonal"))
            {
                ShowZonalStatistics();
            }

            if (GUI.Button(new Rect(140, 1395, 60, 20), "Zone ON/OFF"))
            {
                ToggleZonalRecomposition();
            }

            if (GUI.Button(new Rect(210, 1395, 60, 20), "Mesh ON/OFF"))
            {
                ToggleZonalMeshUpdate();
            }

            if (GUI.Button(new Rect(280, 1395, 50, 20), "Clear"))
            {
                ClearModifiedZones();
            }
        }
    }
}