// SimpleTwoPlateGenerator.cs - Version simplifiée pour 2 plaques seulement
using UnityEngine;
using System.Collections.Generic;
using LifeStory.Core;
using LifeStory.Generation;
using LifeStory.Terrain;

namespace LifeStory.Geology
{
    public class SimpleTwoPlateGenerator : MonoBehaviour
    {
        [Header("Two Plate Configuration")]
        [SerializeField] private float supercontinentSize = 0.85f;      // Taille du supercontinent (0-1)
        [SerializeField] private bool randomizePosition = true;        // Position aléatoire complète
        [SerializeField] private Vector3 manualContinentCenter = Vector3.up; // Centre manuel si randomize = false

        [Header("Organic Shape Settings")]
        [SerializeField] private float baseDistortion = 0.25f;          // Distorsion de base (plus fort)
        [SerializeField] private float noiseScale1 = 0.08f;           // Échelle noise principal
        [SerializeField] private float noiseScale2 = 0.04f;           // Échelle noise détail
        [SerializeField] private float noiseScale3 = 0.15f;            // Échelle noise micro-détail
        [SerializeField] private AnimationCurve distortionFalloff = AnimationCurve.EaseInOut(0, 0.1f, 1, 1f); // Falloff de distorsion

        [Header("Elevation Settings")]
        [SerializeField] private float continentalElevation = 0.3f;    // Hauteur supercontinent
        [SerializeField] private float oceanicElevation = -0.1f;       // Hauteur océan global

        [Header("Advanced Shape Settings")]
        [SerializeField] private float peninsulaIntensity = 1.2f;
        [SerializeField] private float coastalComplexity = 0.8f;
        [SerializeField] private float islandDensity = 0.15f;
        [SerializeField] private int geologicalLobes = 3;
        [SerializeField] private bool enableArchipelagos = true;

        [Header("Volcanic Preservation")]
        [SerializeField] private bool preserveVolcanicModifications = true;
        [SerializeField] private float volcanicPreservationThreshold = 0.05f;  // Seuil pour détecter un volcan

        [Header("Plate Marking")]
        [SerializeField] private bool markPlatesInHeightMap = false;    // Marquer dans la HeightMap
        [SerializeField] private float oceanicMarker = 0.0f;           // Valeur pour océan (noir)
        [SerializeField] private float continentalMarker = 1.0f;       // Valeur pour continent (blanc)

        [Header("Fracture Zones Preparation")]
        [SerializeField] private bool createWeaknessZones = true;           // NOUVEAU - zones de faiblesse prédéfinies
        [SerializeField] private float weaknessIntensity = 0.3f;            // Intensité des zones faibles
        [SerializeField] private int numberOfWeaknessLines = 3;             // Lignes de faiblesse à travers le continent

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showPlateVisualization = true;

        // Données des 2 plaques
        private TectonicPlate[] plates = new TectonicPlate[2];
        private int[,] plateMap;            // 0 = Océanique, 1 = Continentale
        private bool[,] plateBoundaries;    // Frontières continent/océan

        // Références système
        private PlanetGenerator planetGenerator;
        private TerrainModificationManager terrainManager;
        private int mapResolution;
        private bool isInitialized = false;

        // IDs fixes pour les 2 plaques
        private const int OCEANIC_PLATE_ID = 0;
        private const int CONTINENTAL_PLATE_ID = 1;

        public static SimpleTwoPlateGenerator Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LogDebug("🗺️ Simple Two Plate Generator initialisé");
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
            yield return new WaitForSeconds(2f);

            planetGenerator = PlanetGenerator.Instance;
            terrainManager = TerrainModificationManager.Instance;
            if (planetGenerator == null)
            {
                LogDebug("❌ PlanetGenerator non trouvé");
                yield break;
            }
            
            mapResolution = planetGenerator.Resolution;
            
            
            yield return new WaitUntil(() => planetGenerator.HeightMap != null);
            yield return new WaitUntil(() => terrainManager?.IsInitialized == true);
            GenerateTwoPlates();

            isInitialized = true;
            LogDebug("✅ Génération 2 plaques terminée");
        }

        [ContextMenu("Generate Two Plates")]
        public void GenerateTwoPlates()
        {
            LogDebug("🌍 === GÉNÉRATION 2 PLAQUES (OCÉAN + SUPERCONTINENT) ===");

            // Étape 1: Initialiser les plaques
            InitializeTwoPlates();

            // Étape 2: Créer le supercontinent
            CreateSupercontinent();

            // Étape 3: Calculer les frontières
            CalculatePlateBoundaries();

            // Étape 4: Appliquer les élévations
            ApplyPlateElevations();

            // Étape 5: Marquer les plaques (optionnel)
            if (markPlatesInHeightMap)
            {
                MarkPlatesInHeightMap();
            }

            LogDebug("✅ === GÉNÉRATION TERMINÉE ===");
        }

        // Variables pour position aléatoire
        private Vector3 actualContinentCenter;

        private void InitializeTwoPlates()
        {
            LogDebug("🎯 Initialisation des 2 plaques...");

            // Position du supercontinent : aléatoire ou manuelle
            if (randomizePosition)
            {
                actualContinentCenter = Random.onUnitSphere;
                LogDebug($"🎲 Position aléatoire générée: {actualContinentCenter}");
            }
            else
            {
                actualContinentCenter = manualContinentCenter.normalized;
                LogDebug($"📍 Position manuelle: {actualContinentCenter}");
            }

            // Plaque océanique globale
            plates[OCEANIC_PLATE_ID] = new TectonicPlate
            {
                plateID = OCEANIC_PLATE_ID,
                center = Vector3.zero,              // Centre global
                type = PlateType.Oceanic,
                baseElevation = oceanicElevation,
                cells = new List<Vector2Int>(),
                debugColor = Color.blue
            };

            // Supercontinent
            plates[CONTINENTAL_PLATE_ID] = new TectonicPlate
            {
                plateID = CONTINENTAL_PLATE_ID,
                center = actualContinentCenter * planetGenerator.PlanetRadius,
                type = PlateType.Continental,
                baseElevation = continentalElevation,
                cells = new List<Vector2Int>(),
                debugColor = Color.green
            };

            // Initialiser les arrays
            plateMap = new int[mapResolution, mapResolution];

            LogDebug($"✅ Plaque océanique: {oceanicElevation:F3}m");
            LogDebug($"✅ Supercontinent: {continentalElevation:F3}m au centre {actualContinentCenter}");
        }

        private void CreateSupercontinent()
        {
            LogDebug("🏔️ Création du supercontinent...");

            int continentalCells = 0;
            int oceanicCells = 0;

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    Vector3 cellWorldPos = MapCoordinatesToWorldPosition(new Vector2Int(x, y));
                    bool isContinental = IsPositionInSupercontinent(cellWorldPos);

                    if (isContinental)
                    {
                        plateMap[x, y] = CONTINENTAL_PLATE_ID;
                        plates[CONTINENTAL_PLATE_ID].cells.Add(new Vector2Int(x, y));
                        continentalCells++;
                    }
                    else
                    {
                        plateMap[x, y] = OCEANIC_PLATE_ID;
                        plates[OCEANIC_PLATE_ID].cells.Add(new Vector2Int(x, y));
                        oceanicCells++;
                    }
                }
            }

            float continentalPercent = (float)continentalCells / (mapResolution * mapResolution) * 100f;
            LogDebug($"✅ Supercontinent: {continentalCells} cellules ({continentalPercent:F1}%)");
            LogDebug($"✅ Océan global: {oceanicCells} cellules ({100f - continentalPercent:F1}%)");
        }

        private bool IsPositionInSupercontinent(Vector3 worldPosition)
        {
            // Distance du centre du supercontinent
            Vector3 continentCenterWorld = actualContinentCenter * planetGenerator.PlanetRadius;
            float distanceFromCenter = Vector3.Distance(worldPosition, continentCenterWorld);

            // Rayon de base du supercontinent
            float baseRadius = supercontinentSize * planetGenerator.PlanetRadius;

            // === NOUVEAU SYSTÈME DE DISTORSION GÉOLOGIQUE ===

            // 1. DISTORSION PRINCIPALE - Formes majeures (péninsules, golfes)
            Vector3 normalizedPos = worldPosition.normalized;
            float mainDistortion = GetMajorGeologicalFeatures(normalizedPos, worldPosition);

            // 2. DISTORSION CÔTIÈRE - Détails des côtes
            float coastalDistortion = GetCoastalComplexity(worldPosition);

            // 3. DISTORSION TECTONIQUE - Simulate les failles et rifts
            float tectonicDistortion = GetTectonicInfluence(worldPosition, continentCenterWorld);

            // 4. VARIATION RADIALE - Force différentielle selon l'angle
            float radialVariation = GetRadialVariation(worldPosition, continentCenterWorld);

            // === COMBINAISON PONDÉRÉE ===
            float totalDistortion = (mainDistortion * 0.4f) +
                                   (coastalDistortion * 0.3f) +
                                   (tectonicDistortion * 0.2f) +
                                   (radialVariation * 0.1f);

            // === NOUVEAU FALLOFF PLUS AGRESSIF ===
            float normalizedDistance = distanceFromCenter / baseRadius;

            // Falloff différentiel : plus de distorsion aux bords
            float edgeEnhancement = Mathf.Pow(normalizedDistance, 0.7f);
            float distortionIntensity = baseDistortion * (1.0f + edgeEnhancement * 2.0f);

            // Rayon final avec distorsion
            float finalRadius = baseRadius + (totalDistortion * distortionIntensity * baseRadius);

            // === SYSTÈME DE SEUILS MULTIPLES POUR ARCHIPELS ===
            bool isInMainContinent = distanceFromCenter <= finalRadius;

            // Îles et archipels satellites
            if (!isInMainContinent && distanceFromCenter <= finalRadius * 1.3f)
            {
                float islandChance = GetIslandProbability(worldPosition, finalRadius);
                isInMainContinent = Random.value < islandChance;
            }

            return isInMainContinent;
        }

        private float GetMajorGeologicalFeatures(Vector3 normalizedPos, Vector3 worldPos)
        {
            // Utilise coordonnées sphériques pour des formes cohérentes
            float longitude = Mathf.Atan2(normalizedPos.x, normalizedPos.z);
            float latitude = Mathf.Asin(normalizedPos.y);

            // Grandes formes géologiques (échelle continentale)
            float feature1 = Mathf.PerlinNoise(longitude * 2.0f + 1000f, latitude * 2.0f + 1000f);
            float feature2 = Mathf.PerlinNoise(longitude * 1.5f + 2000f, latitude * 1.5f + 2000f);

            // Combiner avec biais vers formes allongées
            float combined = (feature1 * 0.7f + feature2 * 0.3f - 0.5f) * 2.0f;

            // Enhance pour créer des péninsules marquées
            return Mathf.Sign(combined) * Mathf.Pow(Mathf.Abs(combined), 0.8f);
        }

        private float GetCoastalComplexity(Vector3 worldPosition)
        {
            // Détails fins des côtes (baies, caps, fjords)
            float scale1 = noiseScale2 * 2.0f; // Plus dense que l'original
            float scale2 = noiseScale3 * 3.0f;

            float coastal1 = Mathf.PerlinNoise(
                worldPosition.x * scale1 + 3000f,
                worldPosition.z * scale1 + 3000f
            );

            float coastal2 = Mathf.PerlinNoise(
                worldPosition.y * scale2 + 4000f,
                worldPosition.x * scale2 + 4000f
            );

            // Variation côtière avec tendance aux découpures
            float coastalDetail = (coastal1 * 0.6f + coastal2 * 0.4f - 0.5f) * 2.0f;

            // Accentuer les variations pour côtes plus découpées
            return coastalDetail * Mathf.Abs(coastalDetail);
        }

        private float GetTectonicInfluence(Vector3 worldPosition, Vector3 continentCenter)
        {
            // Simule l'influence des lignes de faille tectonique
            Vector3 toCenter = (worldPosition - continentCenter).normalized;

            // Créer des "lignes de faiblesse" radiales
            float angle = Mathf.Atan2(toCenter.x, toCenter.z);
            float radialPattern = Mathf.Sin(angle * numberOfWeaknessLines) * 0.5f + 0.5f;

            // Bruit tectonique le long de ces lignes
            float tectonicNoise = Mathf.PerlinNoise(
                worldPosition.x * noiseScale1 * 0.5f + 5000f,
                worldPosition.z * noiseScale1 * 0.5f + 5000f
            );

            // Combiner pattern radial et bruit
            return (radialPattern * tectonicNoise - 0.5f) * 2.0f * weaknessIntensity;
        }

        private float GetRadialVariation(Vector3 worldPosition, Vector3 continentCenter)
        {
            // Force différentielle selon l'angle depuis le centre
            Vector3 direction = (worldPosition - continentCenter).normalized;
            float angle = Mathf.Atan2(direction.x, direction.z);

            // Créer des lobes géologiques (ex: extension vers certaines directions)
            float lobeFactor = Mathf.Cos(angle * 3.0f) * 0.3f + Mathf.Sin(angle * 2.0f) * 0.2f;

            return lobeFactor;
        }

        private float GetIslandProbability(Vector3 worldPosition, float mainContinentRadius)
        {
            // Probabilité d'îles basée sur distance et patterns géologiques
            float distanceFromMain = Vector3.Distance(worldPosition, actualContinentCenter * planetGenerator.PlanetRadius);
            float distanceFactor = 1.0f - Mathf.Clamp01((distanceFromMain - mainContinentRadius) / (mainContinentRadius * 0.3f));

            // Pattern d'îles avec clustering
            float islandNoise = Mathf.PerlinNoise(
                worldPosition.x * noiseScale3 * 0.5f + 6000f,
                worldPosition.z * noiseScale3 * 0.5f + 6000f
            );

            // Probabilité finale combinant distance et géologie
            return distanceFactor * (islandNoise * 0.3f + 0.1f);
        }


        private void CalculatePlateBoundaries()
        {
            LogDebug("🔗 Calcul frontières océan/continent...");

            plateBoundaries = new bool[mapResolution, mapResolution];
            int boundaryCount = 0;

            for (int x = 1; x < mapResolution - 1; x++)
            {
                for (int y = 1; y < mapResolution - 1; y++)
                {
                    int currentPlate = plateMap[x, y];

                    // Vérifier si adjacent à l'autre plaque
                    bool isBoundary = false;

                    if (plateMap[x + 1, y] != currentPlate ||
                        plateMap[x - 1, y] != currentPlate ||
                        plateMap[x, y + 1] != currentPlate ||
                        plateMap[x, y - 1] != currentPlate)
                    {
                        isBoundary = true;
                        boundaryCount++;
                    }

                    plateBoundaries[x, y] = isBoundary;
                }
            }

            LogDebug($"✅ {boundaryCount} cellules de côte calculées");
        }

        private void ApplyPlateElevations()
        {
            LogDebug("🏔️ Création supercontinent via TerrainManager...");

            // ✅ CORRECTION : Utiliser le nouveau système avec source spécifiée
            float[,] continentLayer = new float[mapResolution, mapResolution];

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    if (IsContinentalCell(x, y))
                    {
                        continentLayer[x, y] = 0.2f; // ← AUGMENTER la valeur pour plus de visibilité
                    }
                    else
                    {
                        continentLayer[x, y] = 0.0f; // Océan à niveau 0
                    }
                }
            }

            // ✅ CORRECTION : Utiliser la nouvelle API avec source
            terrainManager.RegisterModificationLayer("Supercontinent", continentLayer, "SimpleTwoPlateGenerator");

            LogDebug("✅ Supercontinent enregistré comme couche de modification");
        }

        private bool IsVolcanicModification(float currentHeight, float expectedBaseHeight)
        {
            if (!preserveVolcanicModifications) return false;

            // Un volcan est détecté si la hauteur dépasse significativement la base continentale
            float heightDifference = currentHeight - expectedBaseHeight;

            // Vérifications multiples pour robustesse
            bool significantlyHigher = heightDifference > volcanicPreservationThreshold;
            bool withinReasonableBounds = currentHeight < 2.0f; // Éviter les valeurs aberrantes
            bool aboveMinimum = currentHeight > 0.1f; // Éviter les valeurs négatives/nulles

            return significantlyHigher && withinReasonableBounds && aboveMinimum;
        }


        private void NormalizeHeightMapSafely()
        {
            var heightMap = planetGenerator.HeightMap;

            // Trouver min/max actuels
            float min = float.MaxValue, max = float.MinValue;

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    float height = heightMap[x, y];
                    if (height < min) min = height;
                    if (height > max) max = height;
                }
            }

            // Si les valeurs sortent de [0,1], normaliser
            if (min < 0f || max > 1f)
            {
                LogDebug($"⚠️ Normalisation nécessaire: {min:F3} → {max:F3}");

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
                    LogDebug("✅ HeightMap normalisée vers [0,1]");
                }
            }
        }

        private void MarkPlatesInHeightMap()
        {
            LogDebug("🏷️ Marquage des plaques dans HeightMap...");

            var heightMap = planetGenerator.HeightMap;

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    if (plateMap[x, y] == OCEANIC_PLATE_ID)
                    {
                        heightMap[x, y] = oceanicMarker;     // Noir (0.0)
                    }
                    else
                    {
                        heightMap[x, y] = continentalMarker; // Blanc (1.0)
                    }
                }
            }

            // Mettre à jour le mesh avec le marquage
            UpdatePlanetMesh();

            LogDebug("✅ Plaques marquées: Océan=NOIR, Continent=BLANC");
        }

        private void UpdatePlanetMesh()
        {
            if (planetGenerator != null)
            {
                planetGenerator.MarkVolcanicModificationsPresent();
                planetGenerator.UpdatePlanetMesh();
                LogDebug("🔄 Mesh planète mis à jour");
            }
        }

        // === MÉTHODES UTILITAIRES ===
        private Vector3 MapCoordinatesToWorldPosition(Vector2Int mapCoords)
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

        // === GETTERS PUBLICS ===
        public TectonicPlate[] Plates => plates;
        public int[,] PlateMap => plateMap;
        public bool[,] PlateBoundaries => plateBoundaries;
        public bool IsInitialized => isInitialized;

        // Getters spécifiques pour les 2 plaques
        public TectonicPlate OceanicPlate => plates[OCEANIC_PLATE_ID];
        public TectonicPlate ContinentalPlate => plates[CONTINENTAL_PLATE_ID];

        // Méthodes de test pour les tremblements de terre futurs
        public bool IsContinentalCell(int x, int y) => plateMap[x, y] == CONTINENTAL_PLATE_ID;
        public bool IsOceanicCell(int x, int y) => plateMap[x, y] == OCEANIC_PLATE_ID;
        public bool IsCoastalCell(int x, int y) => plateBoundaries[x, y];

        // === MÉTHODES DE TEST AMÉLIORÉES ===
        [ContextMenu("Generate Random Continent")]
        public void GenerateRandomContinent()
        {
            randomizePosition = true;
            GenerateTwoPlates();
            LogDebug("🎲 Nouveau continent généré aléatoirement");
        }

        [ContextMenu("Test Multiple Shapes")]
        public void TestMultipleShapes()
        {
            LogDebug("🧪 TEST GÉNÉRATION MULTIPLE FORMES...");

            for (int i = 0; i < 5; i++)
            {
                GenerateRandomContinent();
                LogDebug($"  Forme {i + 1} générée");

                // Pause pour voir les résultats
                System.Threading.Thread.Sleep(500);
            }
        }

        [ContextMenu("Toggle Position Mode")]
        public void TogglePositionMode()
        {
            randomizePosition = !randomizePosition;
            LogDebug($"Mode position: {(randomizePosition ? "🎲 ALÉATOIRE" : "📍 MANUEL")}");
            GenerateTwoPlates();
        }
        [ContextMenu("Test Plate Distribution")]
        public void TestPlateDistribution()
        {
            if (!isInitialized) return;

            LogDebug("📊 DISTRIBUTION DES 2 PLAQUES:");

            float continentalPercent = (float)plates[CONTINENTAL_PLATE_ID].cells.Count / (mapResolution * mapResolution) * 100f;
            float oceanicPercent = 100f - continentalPercent;

            LogDebug($"🌊 Plaque océanique: {plates[OCEANIC_PLATE_ID].cells.Count} cellules ({oceanicPercent:F1}%)");
            LogDebug($"🏔️ Supercontinent: {plates[CONTINENTAL_PLATE_ID].cells.Count} cellules ({continentalPercent:F1}%)");

            // Frontières
            int coastalCells = 0;
            for (int x = 0; x < mapResolution; x++)
                for (int y = 0; y < mapResolution; y++)
                    if (plateBoundaries[x, y]) coastalCells++;

            LogDebug($"🏖️ Cellules côtières: {coastalCells}");
        }

        [ContextMenu("Toggle Plate Marking")]
        public void TogglePlateMarking()
        {
            markPlatesInHeightMap = !markPlatesInHeightMap;

            if (markPlatesInHeightMap)
            {
                MarkPlatesInHeightMap();
                LogDebug("🏷️ Marquage activé");
            }
            else
            {
                ApplyPlateElevations(); // Restaurer les élévations normales
                LogDebug("🏷️ Marquage désactivé");
            }
        }

        [ContextMenu("Configure Optimal Elevations")]
        public void ConfigureOptimalElevations()
        {
            LogDebug("⚙️ CONFIGURATION ÉLÉVATIONS OPTIMISÉES");

            // Élévations calculées pour coordination avec autres systèmes
            // Océan : Assez bas pour contraste mais pas négatif
            oceanicElevation = 0.0f;        // ← Plus d'élévations négatives

            // Continent : Assez haut pour absorber les fractures sismiques
            continentalElevation = 0.0f;    // ← On utilise le remplacement direct maintenant

            // Marquage : optionnel selon besoins visuels
            if (markPlatesInHeightMap)
            {
                LogDebug("🏷️ Mode marquage : Océan=0.0, Continent=1.0");
                oceanicMarker = 0.0f;
                continentalMarker = 1.0f;
            }
            else
            {
                LogDebug("🌍 Mode réaliste : Élévations géologiques");
            }

            LogDebug("✅ Configuration optimisée pour coordination système");
        }

        [ContextMenu("Analyze Volcanic Preservation")]
        public void AnalyzeVolcanicPreservation()
        {
            if (!isInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            LogDebug("🔍 ANALYSE PRÉSERVATION VOLCANIQUE:");

            var heightMap = planetGenerator.HeightMap;
            float baseContinentLevel = 0.7f;

            int totalContinentalCells = 0;
            int potentialVolcanoes = 0;
            int preservedVolcanoes = 0;
            float maxVolcanicHeight = 0f;

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    if (IsContinentalCell(x, y))
                    {
                        totalContinentalCells++;
                        float height = heightMap[x, y];

                        if (IsVolcanicModification(height, baseContinentLevel))
                        {
                            potentialVolcanoes++;
                            if (height > maxVolcanicHeight) maxVolcanicHeight = height;

                            // Vérifier si ce serait préservé lors de la prochaine application
                            if (preserveVolcanicModifications)
                            {
                                preservedVolcanoes++;
                            }
                        }
                    }
                }
            }

            LogDebug($"  Cellules continentales: {totalContinentalCells}");
            LogDebug($"  Volcans potentiels détectés: {potentialVolcanoes}");
            LogDebug($"  Volcans qui seraient préservés: {preservedVolcanoes}");
            LogDebug($"  Hauteur volcanique max: {maxVolcanicHeight:F3}");
            LogDebug($"  Seuil de préservation: {volcanicPreservationThreshold:F3}");
            LogDebug($"  Préservation activée: {preserveVolcanicModifications}");

            float preservationRate = totalContinentalCells > 0 ? (float)preservedVolcanoes / potentialVolcanoes * 100f : 0f;
            LogDebug($"  Taux de préservation: {preservationRate:F1}%");

            if (potentialVolcanoes == 0)
            {
                LogDebug("⚠️ Aucun volcan détecté - vérifier le système volcanique");
            }
            else if (preservationRate > 80f)
            {
                LogDebug("✅ Excellent taux de préservation volcanique");
            }
            else if (preservationRate > 50f)
            {
                LogDebug("⚠️ Préservation partielle - ajuster le seuil ?");
            }
            else
            {
                LogDebug("❌ Faible préservation - problème de détection");
            }
        }

        // 🎛️ MÉTHODES DE CONFIGURATION
        [ContextMenu("Enable Volcanic Preservation")]
        public void EnableVolcanicPreservation()
        {
            preserveVolcanicModifications = true;
            LogDebug("🌋 Préservation volcanique ACTIVÉE");
        }

        [ContextMenu("Disable Volcanic Preservation")]
        public void DisableVolcanicPreservation()
        {
            preserveVolcanicModifications = false;
            LogDebug("🌋 Préservation volcanique DÉSACTIVÉE");
        }

        [ContextMenu("Test Preservation with Current Heights")]
        public void TestPreservationWithCurrentHeights()
        {
            LogDebug("🧪 TEST PRÉSERVATION avec hauteurs actuelles:");

            AnalyzeVolcanicPreservation();

            // Simuler une nouvelle application sans réellement modifier
            LogDebug("🔄 Simulation application élévations...");

            var heightMap = planetGenerator.HeightMap;
            float baseContinentLevel = 0.7f;
            int wouldBePreserved = 0;
            int wouldBeOverwritten = 0;

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    if (IsContinentalCell(x, y))
                    {
                        float currentHeight = heightMap[x, y];

                        if (IsVolcanicModification(currentHeight, baseContinentLevel))
                        {
                            wouldBePreserved++;
                        }
                        else
                        {
                            wouldBeOverwritten++;
                        }
                    }
                }
            }

            LogDebug($"📊 Résultat simulation:");
            LogDebug($"   Cellules préservées: {wouldBePreserved}");
            LogDebug($"   Cellules écrasées: {wouldBeOverwritten}");

            if (wouldBePreserved > 0)
            {
                LogDebug("✅ La préservation fonctionnerait");
            }
            else
            {
                LogDebug("❌ Aucune préservation - vérifier seuil et volcans");
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[TwoPlateGenerator] {message}");
            }
        }

        // === GUI DEBUG ===
        private void OnGUI()
        {
            if (!enableDebugLogs) return;

            GUI.Box(new Rect(10, 1150, 400, 100), "");
            GUI.Label(new Rect(20, 1165, 380, 20), "=== TWO PLATE SYSTEM ===");

            if (isInitialized)
            {
                float continentalPercent = (float)plates[CONTINENTAL_PLATE_ID].cells.Count / (mapResolution * mapResolution) * 100f;

                GUI.Label(new Rect(20, 1185, 380, 20), $"🌊 Océan: {100f - continentalPercent:F1}% | 🏔️ Continent: {continentalPercent:F1}%");
                GUI.Label(new Rect(20, 1205, 380, 20), $"Marquage: {(markPlatesInHeightMap ? "✅ ON" : "❌ OFF")}");
            }
            else
            {
                GUI.Label(new Rect(20, 1185, 380, 20), "❌ Système non initialisé");
            }

            if (GUI.Button(new Rect(20, 1225, 80, 20), "Regénérer"))
            {
                GenerateTwoPlates();
            }

            if (GUI.Button(new Rect(110, 1225, 80, 20), "Aléatoire"))
            {
                GenerateRandomContinent();
            }

            if (GUI.Button(new Rect(200, 1225, 80, 20), "Marquage"))
            {
                TogglePlateMarking();
            }

            if (GUI.Button(new Rect(290, 1225, 80, 20), "Position"))
            {
                TogglePositionMode();
            }
        }
    }
}