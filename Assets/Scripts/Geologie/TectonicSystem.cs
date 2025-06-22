// TectonicSystem.cs - MIGRÉ vers TerrainModificationManager
using UnityEngine;
using System.Collections.Generic;
using LifeStory.Core;
using LifeStory.Generation;
using LifeStory.Terrain; // ✅ AJOUTÉ pour TerrainModificationManager

namespace LifeStory.Geology
{
    public enum FaultType
    {
        Collision,      // Collision de plaques → montagnes
        Separation,     // Séparation → rifts/vallées
        Transform       // Glissement → déformations latérales
    }

    [System.Serializable]
    public struct TectonicFault
    {
        public Vector3 startPoint;
        public Vector3 endPoint;
        public float intensity;
        public FaultType type;
        public float age;
        public bool isActive;
    }

    public class TectonicSystem : MonoBehaviour
    {
        [Header("Tectonic Configuration")]
        [SerializeField] private bool enableTectonics = true;
        [SerializeField] private float tectonicMultiplier = 0.02f;
        [SerializeField] private float faultWidth = 25f;
        [SerializeField] private int maxActiveFaults = 8;

        [Header("Fault Generation")]
        [SerializeField] private float faultGenerationRate = 1f;
        [SerializeField] private float minFaultLength = 80f;
        [SerializeField] private float maxFaultLength = 145f;
        [SerializeField] private float faultLifetime = 120f;

        [Header("Temperature Based Activity")]
        [SerializeField] private float maxTectonicTemp = 1800f;
        [SerializeField] private float minTectonicTemp = 600f;
        [SerializeField] private AnimationCurve tectonicActivityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Layer Management")] // ✅ NOUVEAU
        [SerializeField] private bool enableLayerThrottling = true;
        [SerializeField] private float layerUpdateInterval = 1f; // Mise à jour max par seconde

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showFaultGizmos = false;

        // Données tectoniques
        private List<TectonicFault> activeFaults = new List<TectonicFault>();

        // ✅ NOUVEAU : Système de couches pour TerrainModificationManager
        private float[,] tectonicModifications;
        private bool tectonicLayerNeedsUpdate = false;
        private float lastTectonicLayerUpdate = 0f;

        // Références
        private PlanetGenerator planetGenerator;
        private GameManager gameManager;
        private TerrainModificationManager terrainManager; // ✅ NOUVEAU
        private int mapResolution;
        private bool isInitialized = false;

        // État
        private float lastTectonicEvent = 0f;
        private float totalTectonicImpact = 0f;

        public static TectonicSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LogDebug("🌍 Tectonic System initialisé (TerrainManager)");
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

        private System.Collections.IEnumerator DelayedInitialization()
        {
            yield return new WaitForSeconds(1.5f);

            planetGenerator = PlanetGenerator.Instance;
            gameManager = GameManager.Instance;
            terrainManager = TerrainModificationManager.Instance; // ✅ NOUVEAU

            if (planetGenerator == null || gameManager == null)
            {
                LogDebug("❌ Systèmes requis non trouvés");
                yield break;
            }

            // ✅ NOUVEAU : Attendre TerrainModificationManager
            if (terrainManager == null)
            {
                LogDebug("❌ TerrainModificationManager non trouvé");
                yield break;
            }

            yield return new WaitUntil(() => planetGenerator.HeightMap != null);
            yield return new WaitUntil(() => terrainManager.IsInitialized); // ✅ NOUVEAU

            mapResolution = planetGenerator.Resolution;

            // ✅ NOUVEAU : Initialiser la couche tectonique
            InitializeTectonicLayer();

            isInitialized = true;
            LogDebug($"✅ Système tectonique initialisé - Résolution: {mapResolution}x{mapResolution}");
        }

        // ✅ NOUVELLE MÉTHODE : Initialiser la couche tectonique
        private void InitializeTectonicLayer()
        {
            tectonicModifications = new float[mapResolution, mapResolution];

            // Initialiser la couche à zéro
            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    tectonicModifications[x, y] = 0f;
                }
            }

            LogDebug("🌋 Couche tectonique initialisée");
        }

        private void Update()
        {
            if (!isInitialized || !enableTectonics) return;
            if (gameManager.CurrentPhase != GamePhase.Geological) return;

            float currentTime = Time.time;
            float deltaTime = Time.deltaTime;

            // Générer nouvelles failles
            if (ShouldGenerateFault() && activeFaults.Count < maxActiveFaults)
            {
                GenerateRandomFault();
                lastTectonicEvent = currentTime;
            }

            // Traiter failles actives
            ProcessActiveFaults(deltaTime);

            // Nettoyer failles expirées
            CleanupExpiredFaults(currentTime);

            // ✅ NOUVEAU : Mise à jour throttled de la couche tectonique
            UpdateTectonicLayerThrottled();
        }

        // ✅ NOUVELLE MÉTHODE : Mise à jour throttled
        private void UpdateTectonicLayerThrottled()
        {
            if (!tectonicLayerNeedsUpdate) return;
            if (!enableLayerThrottling)
            {
                ApplyTectonicLayerUpdate();
                return;
            }

            float timeSinceLastUpdate = Time.time - lastTectonicLayerUpdate;
            if (timeSinceLastUpdate < layerUpdateInterval) return;

            // Appliquer la mise à jour
            ApplyTectonicLayerUpdate();

            // Reset flags
            tectonicLayerNeedsUpdate = false;
            lastTectonicLayerUpdate = Time.time;

            LogDebug($"🔄 Couche tectonique mise à jour (throttled après {timeSinceLastUpdate:F1}s)");
        }

        // ✅ NOUVELLE MÉTHODE : Appliquer mise à jour couche
        private void ApplyTectonicLayerUpdate()
        {
            if (terrainManager == null) return;

            // Enregistrer la couche dans TerrainModificationManager
            terrainManager.RegisterModificationLayer(
                TerrainModificationManager.EARTHQUAKE_LAYER,
                tectonicModifications,
                "TectonicDeformation"
            );

            int modifiedCells = 0;
            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    if (Mathf.Abs(tectonicModifications[x, y]) > 0.001f) modifiedCells++;
                }
            }

            LogDebug($"✅ Couche tectonique appliquée: {modifiedCells} cellules modifiées, {activeFaults.Count} failles");
        }

        private bool ShouldGenerateFault()
        {
            float generationChance = faultGenerationRate * Time.deltaTime;
            bool shouldGenerate = Random.value < generationChance;

            if (shouldGenerate)
            {
                LogDebug($"🎯 Génération faille - Chance: {generationChance:F6}, Random: {Random.value:F3}");
            }

            return shouldGenerate;
        }

        private float CalculateTectonicActivity(float temperature)
        {
            if (temperature < minTectonicTemp || temperature > maxTectonicTemp)
                return 0f;

            float normalizedTemp = Mathf.InverseLerp(minTectonicTemp, maxTectonicTemp, temperature);
            return tectonicActivityCurve.Evaluate(normalizedTemp);
        }

        private void GenerateRandomFault()
        {
            // Générer ligne de faille aléatoire sur la sphère
            Vector3 startDirection = Random.onUnitSphere;
            float faultLength = Random.Range(minFaultLength, maxFaultLength);

            // Créer direction perpendiculaire pour la faille
            Vector3 tangent = Vector3.Cross(startDirection, Vector3.up).normalized;
            if (tangent.magnitude < 0.1f) // Si parallèle à up, utiliser right
                tangent = Vector3.Cross(startDirection, Vector3.right).normalized;

            float planetRadius = planetGenerator.PlanetRadius;
            Vector3 startPoint = startDirection * planetRadius;

            // Calculer point final en suivant la surface de la sphère
            float angularLength = faultLength / planetRadius; // Longueur en radians
            Vector3 endDirection = Quaternion.AngleAxis(angularLength * Mathf.Rad2Deg, tangent) * startDirection;
            Vector3 endPoint = endDirection * planetRadius;

            // Déterminer type de faille
            FaultType faultType = DetermineFaultType();
            float intensity = Random.Range(0.5f, 1.0f);

            TectonicFault newFault = new TectonicFault
            {
                startPoint = startPoint,
                endPoint = endPoint,
                intensity = intensity,
                type = faultType,
                age = 0f,
                isActive = true
            };

            activeFaults.Add(newFault);
            LogDebug($"🌍 Nouvelle faille {faultType}: {startPoint} → {endPoint} (intensité: {intensity:F2})");

            // ✅ NOUVEAU : Appliquer déformation via couche
            ApplyFaultDeformationToLayer(newFault);
        }

        private FaultType DetermineFaultType()
        {
            float rand = Random.value;

            // Probabilités basées sur la réalité géologique
            if (rand < 0.4f) return FaultType.Collision;     // 40% - Création montagnes
            else if (rand < 0.7f) return FaultType.Separation; // 30% - Création rifts
            else return FaultType.Transform;                   // 30% - Déformation latérale
        }

        private void ProcessActiveFaults(float deltaTime)
        {
            for (int i = 0; i < activeFaults.Count; i++)
            {
                var fault = activeFaults[i];
                fault.age += deltaTime;
                activeFaults[i] = fault;
            }
        }

        // ✅ NOUVELLE MÉTHODE : Appliquer déformation à la couche (au lieu de HeightMap directe)
        private void ApplyFaultDeformationToLayer(TectonicFault fault)
        {
            LogDebug($"🏔️ Application déformation {fault.type} à la couche tectonique");

            // Convertir les points 3D en coordonnées de heightmap
            var startCoords = WorldToMapCoordinates(fault.startPoint);
            var endCoords = WorldToMapCoordinates(fault.endPoint);

            if (!IsValidMapCoordinate(startCoords) || !IsValidMapCoordinate(endCoords))
                return;

            // Calculer tous les points le long de la ligne de faille
            var faultPoints = CalculateFaultLine(startCoords, endCoords);

            int pointsProcessed = 0;
            float totalDeformation = 0f;

            foreach (var point in faultPoints)
            {
                ApplyDeformationAtPointToLayer(point, fault);
                pointsProcessed++;
                totalDeformation += fault.intensity * tectonicMultiplier;
            }

            totalTectonicImpact += totalDeformation;
            LogDebug($"✅ {pointsProcessed} points traités, déformation totale: {totalDeformation:F6}");

            // ✅ MARQUER POUR MISE À JOUR
            tectonicLayerNeedsUpdate = true;
        }

        private List<Vector2Int> CalculateFaultLine(Vector2Int start, Vector2Int end)
        {
            var points = new List<Vector2Int>();

            // Algorithme de ligne de Bresenham pour tracer la ligne
            int x0 = start.x, y0 = start.y;
            int x1 = end.x, y1 = end.y;

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                points.Add(new Vector2Int(x0, y0));

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }

            return points;
        }

        // ✅ NOUVELLE MÉTHODE : Appliquer déformation à la couche (remplace l'ancienne ApplyDeformationAtPoint)
        private void ApplyDeformationAtPointToLayer(Vector2Int center, TectonicFault fault)
        {
            int radius = Mathf.RoundToInt(faultWidth);

            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    if (!IsValidMapCoordinate(x, y)) continue;

                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center.x, center.y));
                    if (distance > faultWidth) continue;

                    float falloff = 1f - (distance / faultWidth);
                    float deformation = CalculateDeformationForFaultType(fault.type, fault.intensity, falloff);

                    // ✅ APPLIQUER À LA COUCHE TECTONIQUE (plus de modification directe HeightMap)
                    tectonicModifications[x, y] += deformation;
                }
            }
        }

        private float CalculateDeformationForFaultType(FaultType type, float intensity, float falloff)
        {
            float baseDeformation = intensity * tectonicMultiplier * falloff;

            switch (type)
            {
                case FaultType.Collision:
                    // Collision → soulèvement (montagnes)
                    return baseDeformation * 20f;

                case FaultType.Separation:
                    // Séparation → affaissement (vallées/rifts)
                    return -baseDeformation * 15f;

                case FaultType.Transform:
                    // Glissement → déformation mixte
                    return baseDeformation * (Random.value > 0.5f ? 50f : -25f);

                default:
                    return baseDeformation * 10;
            }
        }

        private void CleanupExpiredFaults(float currentTime)
        {
            for (int i = activeFaults.Count - 1; i >= 0; i--)
            {
                if (activeFaults[i].age > faultLifetime)
                {
                    LogDebug($"🗑️ Faille expirée supprimée: {activeFaults[i].type}");
                    activeFaults.RemoveAt(i);

                    // ✅ MARQUER POUR MISE À JOUR après suppression
                    tectonicLayerNeedsUpdate = true;
                }
            }
        }

        // === MÉTHODES UTILITAIRES ===
        private Vector2Int WorldToMapCoordinates(Vector3 worldPos)
        {
            Vector3 direction = worldPos.normalized;

            float longitude = Mathf.Atan2(direction.x, direction.z);
            float latitude = Mathf.Asin(direction.y);

            float u = (longitude + Mathf.PI) / (2 * Mathf.PI);
            float v = (latitude + Mathf.PI / 2) / Mathf.PI;

            int x = Mathf.Clamp(Mathf.RoundToInt(u * (mapResolution - 1)), 0, mapResolution - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (mapResolution - 1)), 0, mapResolution - 1);

            return new Vector2Int(x, y);
        }

        private bool IsValidMapCoordinate(Vector2Int coords)
        {
            return coords.x >= 0 && coords.x < mapResolution && coords.y >= 0 && coords.y < mapResolution;
        }

        private bool IsValidMapCoordinate(int x, int y)
        {
            return x >= 0 && x < mapResolution && y >= 0 && y < mapResolution;
        }

        // === MÉTHODES DEBUG ===
        [ContextMenu("Generate Test Fault")]
        public void GenerateTestFault()
        {
            if (!isInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            GenerateRandomFault();
            LogDebug("🧪 Faille de test générée");
        }

        [ContextMenu("Show Tectonic Statistics")]
        public void ShowTectonicStatistics()
        {
            LogDebug("📊 STATISTIQUES TECTONIQUES:");
            LogDebug($"  Failles actives: {activeFaults.Count}/{maxActiveFaults}");
            LogDebug($"  Impact total: {totalTectonicImpact:F6}");
            LogDebug($"  Dernière activité: {Time.time - lastTectonicEvent:F1}s");
            LogDebug($"  Couche tectonique: {(tectonicModifications != null ? "✅ Initialisée" : "❌ NULL")}");

            if (gameManager != null)
            {
                float activity = CalculateTectonicActivity(gameManager.SurfaceTemperature);
                LogDebug($"  Activité actuelle: {activity:P1} (Temp: {gameManager.SurfaceTemperature:F0}°C)");
            }

            // Analyser la couche tectonique
            if (tectonicModifications != null)
            {
                int modifiedCells = 0;
                float totalModification = 0f;

                for (int x = 0; x < mapResolution; x++)
                {
                    for (int y = 0; y < mapResolution; y++)
                    {
                        float value = tectonicModifications[x, y];
                        if (Mathf.Abs(value) > 0.001f)
                        {
                            modifiedCells++;
                            totalModification += Mathf.Abs(value);
                        }
                    }
                }

                LogDebug($"  Cellules modifiées: {modifiedCells}");
                LogDebug($"  Modification totale: {totalModification:F3}");
            }

            // Statistiques par type
            int collisions = 0, separations = 0, transforms = 0;
            foreach (var fault in activeFaults)
            {
                switch (fault.type)
                {
                    case FaultType.Collision: collisions++; break;
                    case FaultType.Separation: separations++; break;
                    case FaultType.Transform: transforms++; break;
                }
            }
            LogDebug($"  Types: Collision={collisions}, Séparation={separations}, Transform={transforms}");
        }

        [ContextMenu("Force Tectonic Layer Update")]
        public void ForceTectonicLayerUpdate()
        {
            tectonicLayerNeedsUpdate = true;
            lastTectonicLayerUpdate = 0f; // Force immediate update
            LogDebug("🔄 Mise à jour couche tectonique forcée");
        }

        [ContextMenu("Clear Tectonic Layer")]
        public void ClearTectonicLayer()
        {
            if (tectonicModifications != null)
            {
                for (int x = 0; x < mapResolution; x++)
                {
                    for (int y = 0; y < mapResolution; y++)
                    {
                        tectonicModifications[x, y] = 0f;
                    }
                }

                ApplyTectonicLayerUpdate();
                LogDebug("🧹 Couche tectonique nettoyée");
            }
        }

        // === GETTERS ===
        public int ActiveFaultCount => activeFaults.Count;
        public float TotalTectonicImpact => totalTectonicImpact;
        public bool IsSystemActive => isInitialized && enableTectonics;

        // ✅ NOUVEAU : Getter pour modifications tectoniques (compatible avec ancien code)
        public float[,] GetTectonicModifications() => tectonicModifications;

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[TectonicSystem] {message}");
            }
        }

        // === GIZMOS ===
        private void OnDrawGizmos()
        {
            if (!showFaultGizmos || activeFaults == null) return;

            foreach (var fault in activeFaults)
            {
                // Couleur selon le type
                switch (fault.type)
                {
                    case FaultType.Collision:
                        Gizmos.color = Color.red;
                        break;
                    case FaultType.Separation:
                        Gizmos.color = Color.blue;
                        break;
                    case FaultType.Transform:
                        Gizmos.color = Color.yellow;
                        break;
                }

                // Intensité affecte l'alpha
                var color = Gizmos.color;
                color.a = fault.intensity;
                Gizmos.color = color;

                // Dessiner la ligne de faille
                Gizmos.DrawLine(fault.startPoint, fault.endPoint);

                // Dessiner la zone d'influence
                Gizmos.DrawWireSphere(fault.startPoint, faultWidth * 0.5f);
                Gizmos.DrawWireSphere(fault.endPoint, faultWidth * 0.5f);
            }
        }

        // === GUI DEBUG ===
        private void OnGUI()
        {
            if (!enableDebugLogs) return;

            GUI.Box(new Rect(10, 920, 400, 120), "");
            GUI.Label(new Rect(20, 935, 380, 20), "=== TECTONIC SYSTEM (TerrainManager) ===");

            if (isInitialized)
            {
                GUI.Label(new Rect(20, 955, 380, 20), $"Failles actives: {activeFaults.Count}/{maxActiveFaults}");
                GUI.Label(new Rect(20, 975, 380, 20), $"Impact total: {totalTectonicImpact:F4}");

                // ✅ NOUVEAU : Afficher état couche
                GUI.color = tectonicLayerNeedsUpdate ? Color.yellow : Color.green;
                string layerStatus = tectonicLayerNeedsUpdate ? "PENDING" : "UP TO DATE";
                GUI.Label(new Rect(20, 995, 380, 20), $"Couche: {layerStatus}");
                GUI.color = Color.white;

                if (gameManager != null)
                {
                    float activity = CalculateTectonicActivity(gameManager.SurfaceTemperature);
                    GUI.Label(new Rect(20, 1015, 380, 20), $"Activité: {activity:P1} (Temp: {gameManager.SurfaceTemperature:F0}°C)");
                }
            }
            else
            {
                GUI.Label(new Rect(20, 955, 380, 20), "❌ Système non initialisé");
            }
        }
    }
}