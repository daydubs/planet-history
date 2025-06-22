// ContinentalRiftSystem.cs - Système de séparation continental graduelle
// Sépare le supercontinent en 2-4 continents distincts basé sur température noyau
// Utilise les volcans comme zones de faiblesse naturelles pour lignes de rift organiques

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LifeStory.Core;
using LifeStory.Generation;
using LifeStory.Terrain;
using LifeStory.Geology;
using LifeStory.Volcanoes;

namespace LifeStory.Geology
{
    /// <summary>
    /// État du processus de rift continental
    /// </summary>
    public enum RiftState
    {
        Inactive,           // Pas encore de rift (température > 1400°C)
        InitialWeakening,   // Début des fissures (1400°C - 1300°C)
        ActiveRifting,      // Séparation en cours (1300°C - 1100°C)
        FinalSeparation,    // Finalisation (1100°C - 1000°C)
        Completed           // Continents séparés (< 1000°C)
    }

    /// <summary>
    /// Ligne de rift avec points volcaniques
    /// </summary>
    [System.Serializable]
    public class RiftLine
    {
        public List<Vector2Int> points;              // Points de la ligne de rift
        public List<Vector2Int> volcanicAnchors;     // Volcans qui ancrent la ligne
        public float intensity;                      // Intensité du creusement
        public bool isActive;                        // Ligne en cours de formation
        public float currentDepth;                   // Profondeur actuelle
        public float targetDepth;                    // Profondeur cible
        public int riftID;                          // ID unique de la ligne
    }

    /// <summary>
    /// Nouveau continent séparé
    /// </summary>
    [System.Serializable]
    public class SeparatedContinent
    {
        public int plateID;                          // Nouvel ID de plaque (2, 3, 4...)
        public Vector3 center;                       // Centre de masse calculé
        public List<Vector2Int> cells;               // Cellules appartenant au continent
        public Color debugColor;                     // Couleur de debug
        public float totalArea;                      // Surface totale
        public bool isMainlandContinent;             // Plus gros fragment
    }

    /// <summary>
    /// Système de rift continental - Sépare le supercontinent progressivement
    /// Déclenché par température noyau et guidé par zones volcaniques
    /// </summary>
    public class ContinentalRiftSystem : MonoBehaviour
    {
        [Header("🌡️ Déclenchement Température Noyau")]
        [SerializeField] private float riftStartTemperature = 1400f;     // Début du processus
        [SerializeField] private float riftEndTemperature = 1000f;       // Fin du processus
        [SerializeField] private float criticalRiftTemperature = 1200f;  // Intensification

        [Header("🗺️ Configuration Lignes de Rift")]
        [SerializeField] private int targetRiftLines = 3;                // Nombre de lignes de rift
        [SerializeField] private float minRiftLength = 0.3f;             // Longueur minimum (fraction rayon planète)
        [SerializeField] private float maxRiftLength = 0.8f;             // Longueur maximum
        [SerializeField] private float riftWidth = 15f;                  // AUGMENTÉ : Largeur zone affectée

        [Header("🌋 Influence Volcanique")]
        [SerializeField] private bool useVolcanicAnchors = true;         // Utiliser volcans comme ancres
        [SerializeField] private float volcanicInfluenceRadius = 0.1f;   // Rayon d'influence volcanique
        [SerializeField] private float volcanicAttractionStrength = 2f;  // Force attraction vers volcans

        [Header("⛰️ Déformation Terrain")]
        [SerializeField] private float maxRiftDepth = -0.5f;            // AUGMENTÉ : Profondeur maximum des rifts
        [SerializeField] private AnimationCurve riftDepthCurve;         // Progression profondeur
        [SerializeField] private AnimationCurve riftWidthFalloff;       // Atténuation largeur

        [Header("🧩 Séparation Continents")]
        [SerializeField] private int targetContinentCount = 3;          // Nombre final de continents (2-4)
        [SerializeField] private float minContinentSize = 0.05f;        // Taille minimum continent viable

        [Header("🎨 Visualisation Continents")]
        [SerializeField] private bool enableContinentMarking = true;     // Activer marquage couleurs
        [SerializeField] private bool showContinentBorders = true;       // Afficher bordures
        [SerializeField] private float markingIntensity = 0.3f;          // Intensité couleurs

        [Header("⚡ Optimisation Performance")]
        [SerializeField] private bool enablePerformanceOptimization = true;  // Optimisations actives
        [SerializeField] private float updateThrottlingInterval = 0.1f;       // Max 10 updates/seconde
        [SerializeField] private int maxCellsPerFrame = 1000;                 // Limite cellules/frame
        [SerializeField] private bool enableAsyncDeformation = true;          // Déformation asynchrone

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showRiftVisualization = true;
        [SerializeField] private bool enableProgressiveUpdates = true;

        // === DONNÉES SYSTÈME ===
        private RiftState currentRiftState = RiftState.Inactive;
        private List<RiftLine> activeRiftLines = new List<RiftLine>();
        private List<SeparatedContinent> separatedContinents = new List<SeparatedContinent>();
        private float[,] riftDeformationLayer;
        private int nextPlateID = 2; // Commence après océanique (0) et continental (1)

        // Références système
        private SimpleTwoPlateGenerator plateGenerator;
        private CleanVolcanicSystem volcanicSystem;
        private VolcanicHotSpotSystem hotSpotSystem;
        private TerrainModificationManager terrainManager;
        private GameManager gameManager;
        private PlanetGenerator planetGenerator;

        private int mapResolution;
        private bool isInitialized = false;
        private bool riftProcessStarted = false;

        // Performance tracking
        private float lastUpdateTime = 0f;
        private int totalCellsProcessedThisFrame = 0;
        private bool isAsyncDeformationRunning = false;

        // Constantes
        private const string RIFT_LAYER_NAME = "ContinentalRifts";
        private const string MARKING_LAYER_NAME = "ContinentMarking";

        public static ContinentalRiftSystem Instance { get; private set; }

        // === LIFECYCLE ===

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LogDebug("🗻 Continental Rift System initialisé");
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
            yield return new WaitForSeconds(3f); // Attendre que les autres systèmes s'initialisent

            // Récupérer les références
            plateGenerator = SimpleTwoPlateGenerator.Instance;
            volcanicSystem = CleanVolcanicSystem.Instance;
            hotSpotSystem = FindAnyObjectByType<VolcanicHotSpotSystem>();
            terrainManager = TerrainModificationManager.Instance;
            gameManager = GameManager.Instance;
            planetGenerator = PlanetGenerator.Instance;

            if (plateGenerator == null)
            {
                LogDebug("❌ SimpleTwoPlateGenerator non trouvé");
                yield break;
            }

            if (terrainManager == null)
            {
                LogDebug("❌ TerrainModificationManager non trouvé");
                yield break;
            }

            mapResolution = planetGenerator.Resolution;

            // Attendre que les systèmes soient prêts
            yield return new WaitUntil(() => plateGenerator.IsInitialized);
            yield return new WaitUntil(() => terrainManager.IsInitialized);
            yield return new WaitUntil(() => gameManager != null);

            // Initialiser la couche de déformation
            riftDeformationLayer = new float[mapResolution, mapResolution];

            // S'abonner aux changements de température
            if (gameManager != null)
            {
                GameManager.OnCoreTemperatureChanged += OnCoreTemperatureChanged;
            }

            isInitialized = true;
            LogDebug("✅ Continental Rift System prêt - En attente température noyau");
        }

        private void OnDestroy()
        {
            if (GameManager.OnCoreTemperatureChanged != null)
            {
                GameManager.OnCoreTemperatureChanged -= OnCoreTemperatureChanged;
            }
        }

        // === GESTION TEMPÉRATURE NOYAU ===

        private void OnCoreTemperatureChanged(float newTemperature)
        {
            if (!isInitialized) return;

            UpdateRiftState(newTemperature);

            if (enableProgressiveUpdates && currentRiftState != RiftState.Inactive && currentRiftState != RiftState.Completed)
            {
                UpdateRiftProgression(newTemperature);
            }
        }

        private void UpdateRiftState(float coreTemperature)
        {
            RiftState previousState = currentRiftState;

            if (coreTemperature >= riftStartTemperature)
            {
                currentRiftState = RiftState.Inactive;
            }
            else if (coreTemperature >= 1300f)
            {
                currentRiftState = RiftState.InitialWeakening;
            }
            else if (coreTemperature >= 1100f)
            {
                currentRiftState = RiftState.ActiveRifting;
            }
            else if (coreTemperature >= riftEndTemperature)
            {
                currentRiftState = RiftState.FinalSeparation;
            }
            else
            {
                currentRiftState = RiftState.Completed;
            }

            // Déclencheur de démarrage
            if (previousState == RiftState.Inactive && currentRiftState == RiftState.InitialWeakening)
            {
                StartRiftProcess();
            }

            // Finalisation
            if (previousState != RiftState.Completed && currentRiftState == RiftState.Completed)
            {
                CompleteRiftProcess();
            }

            if (previousState != currentRiftState)
            {
                LogDebug($"🌡️ Température noyau: {coreTemperature:F0}°C → État rift: {currentRiftState}");
            }
        }

        // === PROCESSUS PRINCIPAL ===

        private void StartRiftProcess()
        {
            if (riftProcessStarted) return;

            LogDebug("🗻 === DÉBUT PROCESSUS RIFT CONTINENTAL ===");
            riftProcessStarted = true;

            // 1. Analyser les volcans existants pour identifier les zones de faiblesse
            AnalyzeVolcanicWeaknessZones();

            // 2. Planifier les lignes de rift
            PlanRiftLines();

            // 3. Commencer le creusement progressif
            InitializeRiftDeformation();

            LogDebug($"✅ {activeRiftLines.Count} lignes de rift planifiées pour séparation en {targetContinentCount} continents");
        }

        private void CompleteRiftProcess()
        {
            LogDebug("🗻 === FINALISATION RIFT CONTINENTAL ===");

            // Séparer le continent en fragments distincts
            AnalyzeContinentalFragments();

            // Créer les nouvelles plaques
            CreateSeparatedPlates();

            LogDebug($"✅ Séparation terminée : {separatedContinents.Count} nouveaux continents créés");
        }

        // === ANALYSE VOLCANIQUE ===

        private void AnalyzeVolcanicWeaknessZones()
        {
            if (volcanicSystem == null || !useVolcanicAnchors)
            {
                LogDebug("⚠️ Analyse volcanique ignorée - système non disponible ou désactivé");
                return;
            }

            var volcanoes = volcanicSystem.Volcanoes;
            if (volcanoes == null || volcanoes.Count == 0)
            {
                LogDebug("⚠️ Aucun volcan trouvé pour ancrer les rifts - génération aléatoire");
                return;
            }

            LogDebug($"🌋 Analyse {volcanoes.Count} volcans pour zones de faiblesse...");

            // Filtrer volcans continentaux uniquement
            var continentalVolcanoes = volcanoes.Where(v =>
                plateGenerator.IsContinentalCell(v.heightMapCoords.x, v.heightMapCoords.y)
            ).ToList();

            LogDebug($"🏔️ {continentalVolcanoes.Count} volcans continentaux identifiés");
        }

        private bool IsNearVolcano(Vector2Int point)
        {
            if (volcanicSystem == null) return false;

            var volcanoes = volcanicSystem.Volcanoes;
            if (volcanoes == null) return false;

            float radiusPixels = volcanicInfluenceRadius * mapResolution;

            foreach (var volcano in volcanoes)
            {
                float distance = Vector2.Distance(point, volcano.heightMapCoords);
                if (distance <= radiusPixels)
                {
                    return true;
                }
            }

            return false;
        }

        // === PLANIFICATION LIGNES DE RIFT ===

        private void PlanRiftLines()
        {
            LogDebug("📐 Planification lignes de rift...");

            activeRiftLines.Clear();

            var continentCells = GetContinentalCells();
            var continentCenter = CalculateContinentCenter(continentCells);

            // Générer des lignes de rift qui traversent le continent
            for (int i = 0; i < targetRiftLines; i++)
            {
                RiftLine riftLine = GenerateRiftLine(i, continentCenter, continentCells);
                if (riftLine != null && riftLine.points.Count > 0)
                {
                    activeRiftLines.Add(riftLine);
                    LogDebug($"  Ligne {i}: {riftLine.points.Count} points, volcans: {riftLine.volcanicAnchors.Count}");
                }
            }

            LogDebug($"✅ {activeRiftLines.Count} lignes de rift planifiées");
        }

        private RiftLine GenerateRiftLine(int lineIndex, Vector2Int continentCenter, List<Vector2Int> continentCells)
        {
            // Angle de base pour cette ligne (diviser le cercle)
            float baseAngle = (float)lineIndex / targetRiftLines * 2f * Mathf.PI;

            // Ajouter variation aléatoire
            float angleVariation = Random.Range(-0.5f, 0.5f);
            float actualAngle = baseAngle + angleVariation;

            // Direction de la ligne
            Vector2 direction = new Vector2(Mathf.Cos(actualAngle), Mathf.Sin(actualAngle));

            // Longueur de la ligne
            float lineLength = Random.Range(minRiftLength, maxRiftLength) * mapResolution * 0.5f;

            // Générer les points de la ligne
            var riftPoints = new List<Vector2Int>();
            var volcanicAnchors = new List<Vector2Int>();

            // Point de départ (légèrement excentré du centre)
            Vector2 startOffset = direction * Random.Range(-lineLength * 0.3f, lineLength * 0.3f);
            Vector2Int startPoint = continentCenter + Vector2Int.RoundToInt(startOffset);

            // Générer points le long de la ligne avec distorsion naturelle
            int pointCount = Mathf.RoundToInt(lineLength / 3f); // Un point tous les 3 pixels

            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / (pointCount - 1);

                // Position de base le long de la ligne
                Vector2 basePosition = Vector2.Lerp(
                    startPoint - direction * lineLength * 0.5f,
                    startPoint + direction * lineLength * 0.5f,
                    t
                );

                // Ajouter distorsion organique (perpendiculaire à la direction)
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                float distortion = Mathf.PerlinNoise(t * 5f + lineIndex * 100f, lineIndex * 50f) - 0.5f;
                basePosition += perpendicular * distortion * riftWidth;

                Vector2Int riftPoint = Vector2Int.RoundToInt(basePosition);

                // Vérifier que le point est dans le continent
                if (riftPoint.x >= 0 && riftPoint.x < mapResolution &&
                    riftPoint.y >= 0 && riftPoint.y < mapResolution &&
                    plateGenerator.IsContinentalCell(riftPoint.x, riftPoint.y))
                {
                    riftPoints.Add(riftPoint);

                    // Vérifier proximité volcanique
                    if (useVolcanicAnchors && IsNearVolcano(riftPoint))
                    {
                        volcanicAnchors.Add(riftPoint);
                    }
                }
            }

            if (riftPoints.Count < 10) // Ligne trop courte
            {
                LogDebug($"⚠️ Ligne de rift {lineIndex} trop courte ({riftPoints.Count} points) - ignorée");
                return null;
            }

            return new RiftLine
            {
                points = riftPoints,
                volcanicAnchors = volcanicAnchors,
                intensity = Random.Range(0.8f, 1.2f),
                isActive = true,
                currentDepth = 0f,
                targetDepth = maxRiftDepth,
                riftID = lineIndex
            };
        }

        // === UTILITAIRES CONTINENT ===

        private List<Vector2Int> GetContinentalCells()
        {
            var cells = new List<Vector2Int>();

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    if (plateGenerator.IsContinentalCell(x, y))
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            return cells;
        }

        private Vector2Int CalculateContinentCenter(List<Vector2Int> continentCells)
        {
            if (continentCells.Count == 0) return Vector2Int.zero;

            float avgX = (float)continentCells.Average(c => (double)c.x);
            float avgY = (float)continentCells.Average(c => (double)c.y);

            return new Vector2Int(Mathf.RoundToInt(avgX), Mathf.RoundToInt(avgY));
        }

        // === DÉFORMATION TERRAIN ===

        private void InitializeRiftDeformation()
        {
            LogDebug("⛰️ Initialisation déformation terrain rift...");

            // Nettoyer la couche existante
            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    riftDeformationLayer[x, y] = 0f;
                }
            }

            // Enregistrer la couche dans le TerrainModificationManager
            try
            {
                terrainManager.RegisterModificationLayer(RIFT_LAYER_NAME, riftDeformationLayer, "ContinentalRiftSystem");
                LogDebug("✅ Couche rift enregistrée dans TerrainModificationManager");
            }
            catch (System.Exception e)
            {
                LogDebug($"❌ Erreur enregistrement couche rift: {e.Message}");
                LogDebug("⚠️ Vérifiez que TerrainModificationManager.RegisterModificationLayer existe");
            }
        }

        private void UpdateRiftProgression(float coreTemperature)
        {
            if (activeRiftLines.Count == 0) return;

            // Calculer le facteur de progression basé sur la température
            float progressionFactor = CalculateProgressionFactor(coreTemperature);

            bool layerModified = false;

            foreach (var riftLine in activeRiftLines)
            {
                if (!riftLine.isActive) continue;

                // Calculer la profondeur cible pour cette température
                float targetDepthForTemp = riftLine.targetDepth * progressionFactor;

                // Progression graduelle vers la profondeur cible
                if (riftLine.currentDepth > targetDepthForTemp)
                {
                    float depthChange = Mathf.Max(-0.001f, (targetDepthForTemp - riftLine.currentDepth) * 0.1f);
                    riftLine.currentDepth += depthChange;

                    // Appliquer la déformation
                    ApplyRiftDeformation(riftLine);
                    layerModified = true;
                }
            }

            // Mettre à jour la couche si modifiée
            if (layerModified)
            {
                // Vérifier si la méthode UpdateModificationLayer existe
                if (terrainManager != null)
                {
                    try
                    {
                        // Essayer d'utiliser UpdateModificationLayer si elle existe
                        var updateMethod = terrainManager.GetType().GetMethod("UpdateModificationLayer");
                        if (updateMethod != null)
                        {
                            updateMethod.Invoke(terrainManager, new object[] { RIFT_LAYER_NAME, riftDeformationLayer, "ContinentalRiftSystem" });
                        }
                        else
                        {
                            // Fallback : ré-enregistrer la couche
                            terrainManager.RegisterModificationLayer(RIFT_LAYER_NAME, riftDeformationLayer, "ContinentalRiftSystem");
                        }
                    }
                    catch (System.Exception e)
                    {
                        LogDebug($"⚠️ Erreur mise à jour couche rift: {e.Message}");
                        // Fallback : ré-enregistrer la couche
                        terrainManager.RegisterModificationLayer(RIFT_LAYER_NAME, riftDeformationLayer, "ContinentalRiftSystem");
                    }
                }
            }
        }

        private float CalculateProgressionFactor(float coreTemperature)
        {
            // Progression non-linéaire : lent au début, rapide au milieu, ralenti à la fin
            float normalizedTemp = Mathf.InverseLerp(riftStartTemperature, riftEndTemperature, coreTemperature);
            normalizedTemp = 1f - normalizedTemp; // Inverser (plus froid = plus de progression)

            return riftDepthCurve != null ? riftDepthCurve.Evaluate(normalizedTemp) : normalizedTemp;
        }

        private void ApplyRiftDeformation(RiftLine riftLine)
        {
            LogDebug($"🔧 Application déformation rift {riftLine.riftID} - {riftLine.points.Count} points");

            int cellsModified = 0;
            float totalDeformationApplied = 0f;

            foreach (var point in riftLine.points)
            {
                // Zone d'influence autour du point
                int radius = Mathf.RoundToInt(riftWidth);

                for (int x = point.x - radius; x <= point.x + radius; x++)
                {
                    for (int y = point.y - radius; y <= point.y + radius; y++)
                    {
                        if (x >= 0 && x < mapResolution && y >= 0 && y < mapResolution)
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(point.x, point.y));
                            if (distance <= riftWidth)
                            {
                                // Facteur d'atténuation basé sur la distance
                                float falloff = riftWidthFalloff != null ?
                                    riftWidthFalloff.Evaluate(distance / riftWidth) :
                                    (1f - distance / riftWidth);

                                // Déformation finale - CORRECTION : valeur négative pour creuser
                                float deformation = riftLine.currentDepth * riftLine.intensity * falloff;

                                // CORRECTION : Appliquer la déformation la plus profonde (valeur la plus négative)
                                float currentValue = riftDeformationLayer[x, y];
                                if (deformation < currentValue) // Plus négatif = plus profond
                                {
                                    riftDeformationLayer[x, y] = deformation;
                                    cellsModified++;
                                    totalDeformationApplied += Mathf.Abs(deformation);
                                }
                            }
                        }
                    }
                }
            }

            LogDebug($"  Cellules modifiées: {cellsModified}, déformation totale: {totalDeformationApplied:F6}");
            LogDebug($"  Profondeur rift: {riftLine.currentDepth:F3}, intensité: {riftLine.intensity:F2}");
        }

        // === FRAGMENTATION CONTINENTALE ===

        private void AnalyzeContinentalFragments()
        {
            LogDebug("🧩 Analyse fragmentation continentale...");

            var continentCells = GetContinentalCells();
            var visited = new bool[mapResolution, mapResolution];
            separatedContinents.Clear();

            foreach (var cell in continentCells)
            {
                if (visited[cell.x, cell.y]) continue;

                // Flood fill pour identifier un fragment
                var fragment = FloodFillContinent(cell, visited);

                if (fragment.Count >= minContinentSize * mapResolution * mapResolution)
                {
                    var continent = new SeparatedContinent
                    {
                        plateID = nextPlateID++,
                        cells = fragment,
                        center = Vector3.zero, // Calculé après
                        totalArea = fragment.Count,
                        debugColor = GenerateRandomColor(),
                        isMainlandContinent = false
                    };

                    // Calculer le centre en Vector3
                    Vector2Int center2D = CalculateContinentCenter(fragment);
                    continent.center = MapCoordsToWorldPosition(center2D);

                    separatedContinents.Add(continent);
                }
            }

            // Identifier le continent principal (le plus grand)
            if (separatedContinents.Count > 0)
            {
                var mainContinent = separatedContinents.OrderByDescending(c => c.totalArea).First();
                mainContinent.isMainlandContinent = true;
            }

            LogDebug($"✅ {separatedContinents.Count} fragments continentaux identifiés");
        }

        private List<Vector2Int> FloodFillContinent(Vector2Int startCell, bool[,] visited)
        {
            var fragment = new List<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(startCell);
            visited[startCell.x, startCell.y] = true;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                fragment.Add(current);

                // Vérifier les 4 voisins
                var neighbors = new Vector2Int[]
                {
                    new Vector2Int(current.x + 1, current.y),
                    new Vector2Int(current.x - 1, current.y),
                    new Vector2Int(current.x, current.y + 1),
                    new Vector2Int(current.x, current.y - 1)
                };

                foreach (var neighbor in neighbors)
                {
                    if (neighbor.x >= 0 && neighbor.x < mapResolution &&
                        neighbor.y >= 0 && neighbor.y < mapResolution &&
                        !visited[neighbor.x, neighbor.y] &&
                        plateGenerator.IsContinentalCell(neighbor.x, neighbor.y) &&
                        !IsInRiftZone(neighbor))
                    {
                        visited[neighbor.x, neighbor.y] = true;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return fragment;
        }

        private bool IsInRiftZone(Vector2Int cell)
        {
            // Vérifier si la cellule est dans une zone de rift profonde
            return riftDeformationLayer[cell.x, cell.y] < maxRiftDepth * 0.7f;
        }

        private void CreateSeparatedPlates()
        {
            LogDebug("🗺️ Création nouvelles plaques tectoniques...");

            // Créer le marquage visuel des continents
            if (enableContinentMarking)
            {
                ApplyContinentColorMarking();
            }

            foreach (var continent in separatedContinents)
            {
                LogDebug($"  Nouveau continent (ID:{continent.plateID}): {continent.totalArea} cellules, centre: {continent.center}");
            }

            // TODO: Intégrer avec SimpleTwoPlateGenerator pour étendre le système de plaques
            // Pour l'instant, on log les informations pour validation
        }

        private void ApplyContinentColorMarking()
        {
            LogDebug("🎨 Application marquage couleur continents...");

            // Créer une couche de marquage visuel
            float[,] continentMarkingLayer = new float[mapResolution, mapResolution];

            for (int i = 0; i < separatedContinents.Count; i++)
            {
                var continent = separatedContinents[i];

                // Valeur de marquage basée sur l'ID du continent (pour différenciation visuelle)
                float markingValue = continent.isMainlandContinent ? 1.0f : 0.3f + (i * 0.2f);

                foreach (var cell in continent.cells)
                {
                    if (cell.x >= 0 && cell.x < mapResolution && cell.y >= 0 && cell.y < mapResolution)
                    {
                        continentMarkingLayer[cell.x, cell.y] = markingValue * markingIntensity;
                    }
                }

                LogDebug($"  Continent {i + 1} marqué avec valeur {markingValue:F2} ({continent.totalArea} cellules)");
            }

            // Enregistrer la couche de marquage
            try
            {
                terrainManager.RegisterModificationLayer("ContinentMarking", continentMarkingLayer, "ContinentalRiftSystem");
                LogDebug("✅ Couche marquage continents enregistrée");
            }
            catch (System.Exception e)
            {
                LogDebug($"❌ Erreur marquage continents: {e.Message}");
            }
        }

        private Color GenerateRandomColor()
        {
            return new Color(
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f),
                1f
            );
        }

        // === MÉTHODES PUBLIQUES D'ACCÈS ===

        public RiftState CurrentRiftState => currentRiftState;
        public bool IsRiftProcessActive => riftProcessStarted && currentRiftState != RiftState.Completed;
        public int ActiveRiftLineCount => activeRiftLines.Count;
        public int SeparatedContinentCount => separatedContinents.Count;
        public bool IsInitialized => isInitialized;

        public List<RiftLine> GetActiveRiftLines() => new List<RiftLine>(activeRiftLines);
        public List<SeparatedContinent> GetSeparatedContinents() => new List<SeparatedContinent>(separatedContinents);

        // === MÉTHODES DE TEST ===

        [ContextMenu("Force Start Rift Process")]
        public void ForceStartRiftProcess()
        {
            if (!isInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            LogDebug("🧪 FORCE DÉMARRAGE RIFT (TEST)");
            StartRiftProcess();
        }

        [ContextMenu("Simulate Temperature Drop")]
        public void SimulateTemperatureDrop()
        {
            if (gameManager == null) return;

            LogDebug("🧪 SIMULATION CHUTE TEMPÉRATURE");
            float currentTemp = gameManager.CoreTemperature;
            float newTemp = Mathf.Max(riftEndTemperature - 100f, currentTemp - 200f);

            LogDebug($"Température simulée: {currentTemp:F0}°C → {newTemp:F0}°C");
            OnCoreTemperatureChanged(newTemp);
        }

        [ContextMenu("Analyze Current State")]
        public void AnalyzeCurrentState()
        {
            if (!isInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            LogDebug("📊 === ANALYSE ÉTAT RIFT CONTINENTAL ===");
            LogDebug($"  État actuel: {currentRiftState}");
            LogDebug($"  Processus démarré: {riftProcessStarted}");
            LogDebug($"  Lignes de rift actives: {activeRiftLines.Count}");
            LogDebug($"  Continents séparés: {separatedContinents.Count}");

            if (gameManager != null)
            {
                float coreTemp = gameManager.CoreTemperature;
                LogDebug($"  Température noyau: {coreTemp:F0}°C");
                LogDebug($"  Progression: {CalculateProgressionFactor(coreTemp):P1}");
            }

            // Analyser chaque ligne de rift
            for (int i = 0; i < activeRiftLines.Count; i++)
            {
                var rift = activeRiftLines[i];
                LogDebug($"  Rift {i}: {rift.points.Count} points, profondeur {rift.currentDepth:F3}m, volcans: {rift.volcanicAnchors.Count}");
            }

            // Analyser les continents séparés
            for (int i = 0; i < separatedContinents.Count; i++)
            {
                var continent = separatedContinents[i];
                string type = continent.isMainlandContinent ? "PRINCIPAL" : "secondaire";
                LogDebug($"  Continent {i} (ID:{continent.plateID}): {continent.totalArea} cellules [{type}]");
            }
        }

        [ContextMenu("Visualize Rift Lines")]
        public void VisualizeRiftLines()
        {
            if (!showRiftVisualization || activeRiftLines.Count == 0)
            {
                LogDebug("❌ Visualisation désactivée ou aucune ligne de rift");
                return;
            }

            LogDebug("🎨 Visualisation lignes de rift:");

            foreach (var rift in activeRiftLines)
            {
                LogDebug($"  Ligne {rift.riftID}: {rift.points.Count} points");
                LogDebug($"    Profondeur: {rift.currentDepth:F3}m / {rift.targetDepth:F3}m");
                LogDebug($"    Volcans: {rift.volcanicAnchors.Count} ancres");
                LogDebug($"    Active: {rift.isActive}");
            }
        }

        [ContextMenu("Toggle Continent Marking")]
        public void ToggleContinentMarking()
        {
            enableContinentMarking = !enableContinentMarking;

            if (enableContinentMarking && separatedContinents.Count > 0)
            {
                ApplyContinentColorMarking();
                LogDebug("🎨 Marquage continents activé");
            }
            else
            {
                // Supprimer la couche de marquage
                try
                {
                    float[,] emptyLayer = new float[mapResolution, mapResolution];
                    terrainManager.RegisterModificationLayer(MARKING_LAYER_NAME, emptyLayer, "ContinentalRiftSystem");
                    LogDebug("🎨 Marquage continents désactivé");
                }
                catch (System.Exception e)
                {
                    LogDebug($"⚠️ Erreur suppression marquage: {e.Message}");
                }
            }
        }
        public void ValidateContinentSeparation()
        {
            if (!isInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            LogDebug("🧪 VALIDATION SÉPARATION CONTINENTS:");

            // Calculer la taille totale du continent original
            var originalContinentCells = GetContinentalCells();
            float totalContinentalArea = originalContinentCells.Count;

            LogDebug($"  Superficie continentale totale: {totalContinentalArea:F0} cellules");

            // Analyser les fragments actuels
            if (separatedContinents.Count > 0)
            {
                LogDebug($"  Fragments identifiés: {separatedContinents.Count}");

                float totalFragmentArea = 0f;
                for (int i = 0; i < separatedContinents.Count; i++)
                {
                    var continent = separatedContinents[i];
                    float percentage = (continent.totalArea / totalContinentalArea) * 100f;
                    string status = continent.isMainlandContinent ? "PRINCIPAL" : "secondaire";

                    LogDebug($"    Fragment {i + 1}: {continent.totalArea:F0} cellules ({percentage:F1}%) [{status}]");
                    totalFragmentArea += continent.totalArea;
                }

                float recoveredPercentage = (totalFragmentArea / totalContinentalArea) * 100f;
                LogDebug($"  Superficie récupérée: {recoveredPercentage:F1}% du continent original");

                // Validation des critères
                bool hasValidFragmentCount = separatedContinents.Count >= 2 && separatedContinents.Count <= 4;
                bool hasReasonableSizes = separatedContinents.All(c => c.totalArea >= minContinentSize * mapResolution * mapResolution);
                bool hasMainlandContinent = separatedContinents.Any(c => c.isMainlandContinent);

                LogDebug($"  ✅ Critères de validation:");
                LogDebug($"    Nombre fragments (2-4): {hasValidFragmentCount} ({separatedContinents.Count})");
                LogDebug($"    Tailles viables: {hasReasonableSizes}");
                LogDebug($"    Continent principal: {hasMainlandContinent}");

                if (hasValidFragmentCount && hasReasonableSizes && hasMainlandContinent)
                {
                    LogDebug($"  🎉 SÉPARATION RÉUSSIE ! Paramètres optimaux atteints.");
                }
                else
                {
                    LogDebug($"  ⚠️ Ajustements recommandés:");
                    if (!hasValidFragmentCount)
                    {
                        if (separatedContinents.Count < 2)
                            LogDebug($"    - Augmenter minRiftLength ou targetRiftLines");
                        else if (separatedContinents.Count > 4)
                            LogDebug($"    - Réduire targetRiftLines ou augmenter minContinentSize");
                    }
                    if (!hasReasonableSizes)
                        LogDebug($"    - Réduire minContinentSize ou ajuster riftWidth");
                }
            }
            else
            {
                LogDebug($"  ❌ Aucun fragment détecté - Rifts trop faibles ou mal positionnés");
                LogDebug($"  💡 Suggestions:");
                LogDebug($"    - Augmenter maxRiftDepth (actuellement: {maxRiftDepth:F3})");
                LogDebug($"    - Augmenter riftWidth (actuellement: {riftWidth:F1})");
                LogDebug($"    - Vérifier que les rifts traversent le continent");
            }
        }
        public void TestExtremeDeformation()
        {
            if (!isInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            LogDebug("🧪 TEST DÉFORMATION EXTRÊME");

            // Nettoyer la couche
            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    riftDeformationLayer[x, y] = 0f;
                }
            }

            // Appliquer une déformation MASSIVE et SIMPLE pour test
            var continentCells = GetContinentalCells();
            var center = CalculateContinentCenter(continentCells);

            LogDebug($"Centre continent: ({center.x}, {center.y})");

            // Créer une ligne de rift SIMPLE et MASSIVE
            int testRadius = 50; // 50 pixels de rayon
            float testDepth = -1.0f; // TRÈS profond
            int cellsModified = 0;

            // Ligne horizontale à travers le centre
            for (int x = center.x - testRadius; x <= center.x + testRadius; x++)
            {
                for (int y = center.y - 10; y <= center.y + 10; y++) // Largeur 20 pixels
                {
                    if (x >= 0 && x < mapResolution && y >= 0 && y < mapResolution)
                    {
                        // Vérifier si continental
                        if (plateGenerator.IsContinentalCell(x, y))
                        {
                            riftDeformationLayer[x, y] = testDepth;
                            cellsModified++;
                        }
                    }
                }
            }

            LogDebug($"✅ Déformation extrême appliquée: {cellsModified} cellules à {testDepth:F3}");

            // Forcer la mise à jour
            try
            {
                terrainManager.RegisterModificationLayer(RIFT_LAYER_NAME, riftDeformationLayer, "ContinentalRiftSystem");
                LogDebug("✅ Couche test enregistrée");
            }
            catch (System.Exception e)
            {
                LogDebug($"❌ Erreur couche test: {e.Message}");
            }

            // Vérifier immédiatement
            DebugRiftLayer();
        }
        public void DebugRiftPoints()
        {
            if (!isInitialized || activeRiftLines.Count == 0)
            {
                LogDebug("❌ Aucune ligne de rift disponible");
                return;
            }

            LogDebug("🔍 DEBUG POINTS DE RIFT:");

            for (int i = 0; i < activeRiftLines.Count; i++)
            {
                var rift = activeRiftLines[i];
                LogDebug($"  Ligne {i}:");
                LogDebug($"    Points totaux: {rift.points.Count}");
                LogDebug($"    Profondeur actuelle: {rift.currentDepth:F3}");
                LogDebug($"    Profondeur cible: {rift.targetDepth:F3}");
                LogDebug($"    Intensité: {rift.intensity:F2}");
                LogDebug($"    Active: {rift.isActive}");

                // Vérifier quelques points
                int continentalPoints = 0;
                int validPoints = 0;

                for (int p = 0; p < rift.points.Count && p < 10; p++) // Tester les 10 premiers points
                {
                    var point = rift.points[p];

                    // Vérifier si dans les limites
                    bool inBounds = point.x >= 0 && point.x < mapResolution &&
                                   point.y >= 0 && point.y < mapResolution;

                    if (inBounds)
                    {
                        validPoints++;

                        // Vérifier si continental
                        if (plateGenerator.IsContinentalCell(point.x, point.y))
                        {
                            continentalPoints++;
                        }
                    }

                    if (p < 3) // Log les 3 premiers points
                    {
                        LogDebug($"      Point {p}: ({point.x}, {point.y}) - Bounds: {inBounds}, Continental: {(inBounds ? plateGenerator.IsContinentalCell(point.x, point.y) : false)}");
                    }
                }

                LogDebug($"    Points valides (échantillon 10): {validPoints}/10");
                LogDebug($"    Points continentaux (échantillon 10): {continentalPoints}/10");

                if (continentalPoints == 0)
                {
                    LogDebug($"    ⚠️ PROBLÈME: Aucun point continental détecté !");
                }
            }
        }
        public void DebugRiftLayer()
        {
            if (!isInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            LogDebug("🔍 DEBUG COUCHE RIFT:");

            int modifiedCells = 0;
            float minValue = 0f;
            float maxValue = 0f;
            float totalDeformation = 0f;

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    float value = riftDeformationLayer[x, y];
                    if (Mathf.Abs(value) > 0.001f)
                    {
                        modifiedCells++;
                        totalDeformation += Mathf.Abs(value);

                        if (value < minValue) minValue = value;
                        if (value > maxValue) maxValue = value;
                    }
                }
            }

            LogDebug($"  Cellules modifiées: {modifiedCells} / {mapResolution * mapResolution}");
            LogDebug($"  Valeur min: {minValue:F6}");
            LogDebug($"  Valeur max: {maxValue:F6}");
            LogDebug($"  Déformation totale: {totalDeformation:F6}");
            LogDebug($"  Profondeur cible: {maxRiftDepth:F3}");

            if (modifiedCells == 0)
            {
                LogDebug("⚠️ Aucune déformation détectée - vérifier ApplyRiftDeformation");
            }
            else if (minValue > -0.01f)
            {
                LogDebug("⚠️ Déformations trop faibles - augmenter maxRiftDepth");
            }
        }
        public void TestVolcanicIntegration()
        {
            if (volcanicSystem == null)
            {
                LogDebug("❌ CleanVolcanicSystem non disponible");
                return;
            }

            LogDebug("🌋 TEST INTÉGRATION VOLCANIQUE:");

            var volcanoes = volcanicSystem.Volcanoes;
            if (volcanoes == null || volcanoes.Count == 0)
            {
                LogDebug("  ❌ Aucun volcan disponible");
                return;
            }

            int continentalVolcanoes = 0;
            int volcanicAnchors = 0;

            foreach (var volcano in volcanoes)
            {
                bool isContinental = plateGenerator.IsContinentalCell(volcano.heightMapCoords.x, volcano.heightMapCoords.y);
                if (isContinental)
                {
                    continentalVolcanoes++;

                    // Test si peut servir d'ancre
                    Vector2Int volcanoPos = volcano.heightMapCoords;
                    if (IsNearVolcano(volcanoPos))
                    {
                        volcanicAnchors++;
                    }
                }
            }

            LogDebug($"  Total volcans: {volcanoes.Count}");
            LogDebug($"  Volcans continentaux: {continentalVolcanoes}");
            LogDebug($"  Ancres potentielles: {volcanicAnchors}");
            LogDebug($"  Influence radius: {volcanicInfluenceRadius:F3}");
        }

        [ContextMenu("Force Complete Rift")]
        public void ForceCompleteRift()
        {
            if (!isInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            LogDebug("🧪 FORCE FINALISATION RIFT (TEST)");

            // Démarrer le processus si pas encore fait
            if (!riftProcessStarted)
            {
                StartRiftProcess();
            }

            // Forcer l'état final
            currentRiftState = RiftState.Completed;

            // CORRECTION : Forcer les valeurs de déformation explicitement
            foreach (var rift in activeRiftLines)
            {
                // CORRECTION : S'assurer que currentDepth est bien négatif
                rift.currentDepth = maxRiftDepth; // -0.5f
                rift.targetDepth = maxRiftDepth;  // -0.5f
                rift.intensity = 2.0f; // Doubler l'intensité pour test

                LogDebug($"🔧 Rift {rift.riftID} - AVANT déformation:");
                LogDebug($"  currentDepth: {rift.currentDepth:F3}");
                LogDebug($"  targetDepth: {rift.targetDepth:F3}");
                LogDebug($"  intensity: {rift.intensity:F2}");

                ApplyRiftDeformation(rift);
            }

            // Forcer la mise à jour de la couche
            if (terrainManager != null)
            {
                try
                {
                    var updateMethod = terrainManager.GetType().GetMethod("UpdateModificationLayer");
                    if (updateMethod != null)
                    {
                        updateMethod.Invoke(terrainManager, new object[] { RIFT_LAYER_NAME, riftDeformationLayer, "ContinentalRiftSystem" });
                    }
                    else
                    {
                        terrainManager.RegisterModificationLayer(RIFT_LAYER_NAME, riftDeformationLayer, "ContinentalRiftSystem");
                    }
                    LogDebug("✅ Couche rift forcée dans TerrainModificationManager");
                }
                catch (System.Exception e)
                {
                    LogDebug($"❌ Erreur finalisation rift: {e.Message}");
                }
            }

            // Finaliser le processus
            CompleteRiftProcess();

            // Re-tester la couche après application
            DebugRiftLayer();

            // Diagnostic final des valeurs appliquées
            LogDebug($"🔧 DIAGNOSTIC FINAL:");
            LogDebug($"  maxRiftDepth configuré: {maxRiftDepth:F3}");
            LogDebug($"  riftWidth configuré: {riftWidth:F1}");
            LogDebug($"  Lignes actives: {activeRiftLines.Count}");

            if (activeRiftLines.Count > 0)
            {
                var firstRift = activeRiftLines[0];
                LogDebug($"  Première ligne APRÈS: {firstRift.points.Count} points, profondeur {firstRift.currentDepth:F3}");
            }
        }

        // === VISUALISATION ET DEBUG ===

        private void OnDrawGizmos()
        {
            if (!showRiftVisualization || !isInitialized || activeRiftLines.Count == 0) return;

            // Dessiner les lignes de rift
            foreach (var rift in activeRiftLines)
            {
                if (!rift.isActive) continue;

                // Couleur selon profondeur
                float depthRatio = Mathf.Abs(rift.currentDepth / rift.targetDepth);
                Gizmos.color = Color.Lerp(Color.yellow, Color.red, depthRatio);

                // Dessiner la ligne
                for (int i = 0; i < rift.points.Count - 1; i++)
                {
                    Vector3 start = MapCoordsToWorldPosition(rift.points[i]);
                    Vector3 end = MapCoordsToWorldPosition(rift.points[i + 1]);
                    Gizmos.DrawLine(start, end);
                }

                // Dessiner les ancres volcaniques
                Gizmos.color = Color.green;
                foreach (var anchor in rift.volcanicAnchors)
                {
                    Vector3 anchorPos = MapCoordsToWorldPosition(anchor);
                    Gizmos.DrawWireSphere(anchorPos, planetGenerator.PlanetRadius * 0.02f);
                }
            }

            // Dessiner les centres des continents séparés
            Gizmos.color = Color.cyan;
            foreach (var continent in separatedContinents)
            {
                Vector3 centerPos = continent.center; // Déjà en Vector3
                float radius = continent.isMainlandContinent ?
                    planetGenerator.PlanetRadius * 0.05f :
                    planetGenerator.PlanetRadius * 0.03f;
                Gizmos.DrawWireSphere(centerPos, radius);
            }
        }

        private Vector3 MapCoordsToWorldPosition(Vector2Int mapCoords)
        {
            float u = (float)mapCoords.x / (mapResolution - 1);
            float v = (float)mapCoords.y / (mapResolution - 1);

            float longitude = u * 2 * Mathf.PI - Mathf.PI;
            float latitude = v * Mathf.PI - Mathf.PI / 2;

            float x = Mathf.Cos(latitude) * Mathf.Cos(longitude);
            float y = Mathf.Sin(latitude);
            float z = Mathf.Cos(latitude) * Mathf.Sin(longitude);

            return new Vector3(x, y, z) * planetGenerator.PlanetRadius;
        }

        // === GUI DEBUG ===

        private void OnGUI()
        {
            if (!enableDebugLogs) return;

            GUI.Box(new Rect(10, 700, 400, 160), "");
            GUI.Label(new Rect(20, 715, 380, 20), "=== CONTINENTAL RIFT SYSTEM ===");

            if (isInitialized)
            {
                // État actuel
                GUI.color = GetStateColor(currentRiftState);
                GUI.Label(new Rect(20, 735, 380, 20), $"État: {currentRiftState}");
                GUI.color = Color.white;

                // Température noyau
                if (gameManager != null)
                {
                    float coreTemp = gameManager.CoreTemperature;
                    float progression = CalculateProgressionFactor(coreTemp);
                    GUI.Label(new Rect(20, 755, 380, 20), $"Noyau: {coreTemp:F0}°C | Progression: {progression:P1}");
                }

                // Lignes de rift
                GUI.Label(new Rect(20, 775, 380, 20), $"Lignes rift: {activeRiftLines.Count} | Continents: {separatedContinents.Count}");

                // Processus
                GUI.color = riftProcessStarted ? Color.green : Color.gray;
                string processStatus = riftProcessStarted ? "ACTIF" : "EN ATTENTE";
                GUI.Label(new Rect(20, 795, 380, 20), $"Processus: {processStatus}");
                GUI.color = Color.white;

                // Boutons de test
                if (GUI.Button(new Rect(20, 815, 80, 20), "Démarrer"))
                {
                    ForceStartRiftProcess();
                }

                if (GUI.Button(new Rect(110, 815, 80, 20), "Simuler"))
                {
                    SimulateTemperatureDrop();
                }

                if (GUI.Button(new Rect(200, 815, 80, 20), "Finaliser"))
                {
                    ForceCompleteRift();
                }

                if (GUI.Button(new Rect(290, 815, 80, 20), "Analyser"))
                {
                    AnalyzeCurrentState();
                }

                // Détails avancés
                if (activeRiftLines.Count > 0)
                {
                    var firstRift = activeRiftLines[0];
                    GUI.Label(new Rect(20, 835, 380, 20), $"Rift 0: {firstRift.currentDepth:F3}m / {firstRift.targetDepth:F3}m");
                }
            }
            else
            {
                GUI.Label(new Rect(20, 735, 380, 20), "❌ Système non initialisé");
            }
        }

        private Color GetStateColor(RiftState state)
        {
            return state switch
            {
                RiftState.Inactive => Color.gray,
                RiftState.InitialWeakening => Color.yellow,
                RiftState.ActiveRifting => Color.cyan,
                RiftState.FinalSeparation => Color.red,
                RiftState.Completed => Color.green,
                _ => Color.white
            };
        }

        // === UTILITAIRES ===

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[ContinentalRift] {message}");
            }
        }
    }
}