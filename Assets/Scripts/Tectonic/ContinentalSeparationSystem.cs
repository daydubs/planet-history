// ContinentalSeparationSystem.cs - Nouveau système pour séparer efficacement le supercontinent
using LifeStory.Core;
using LifeStory.Generation;
using LifeStory.Geology;
using LifeStory.Terrain;
using LifeStory.Volcanoes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LifeStory.Volcanoes.CleanVolcanicSystem;

namespace LifeStory.Tectonics
{
    public class ContinentalSeparationSystem : MonoBehaviour
    {
        [Header("Continental Separation Configuration")]
        [SerializeField] private float separationTemperatureThreshold = 2560f; // Changé de 800f à 2560f
        [SerializeField] private float separationEndTemperature = 1200f; // NOUVEAU - Fin de la séparation
        [SerializeField] private int targetContinentalMasses = 4; // 2-6 continents désirés
        [SerializeField] private bool enableSeparation = true;

        [Header("Rift Valley Configuration")]
        [SerializeField] private float riftValleyWidth = 80f; // Largeur en cellules heightmap
        [SerializeField] private float riftValleyDepth = -0.4f; // Profondeur océanique
        [SerializeField] private float riftValleyLength = 0.8f; // Fraction du continent (0-1)

        [Header("Volcanic Interaction")]
        [SerializeField] private bool integrateExistingFissures = true;
        [SerializeField] private float fissureInfluenceRadius = 25f;
        [SerializeField] private float fissureWeakeningFactor = 1.5f; // Amplification par les fissures
        [SerializeField] private bool createNewFissuresAtRifts = true;

        [Header("Geological Realism")]
        [SerializeField] private float riftPathDeviation = 20f;           // Déviation max en degrés à chaque étape
        [SerializeField] private float geologicalFollowing = 0.6f;       // Tendance à suivre la géologie (0-1)
        [SerializeField] private float momentumPreservation = 0.7f;      // Conservation de direction (0-1)
        [SerializeField] private float noiseScale = 0.08f;               // Échelle du bruit géologique
        [SerializeField] private int pathSmoothingSteps = 4;             // Lissage du chemin final
        [SerializeField] private bool useOrganicRifts = true;            // Activer rifts organiques
        [SerializeField] private AnimationCurve riftWidthProfile = AnimationCurve.EaseInOut(0, 0.3f, 1, 1f); // Profile largeur

        [Header("Separation Pattern")]
        [SerializeField] private SeparationPattern separationPattern = SeparationPattern.RadialFromCenter;
        [SerializeField] private float patternVariation = 0.2f; // Variation aléatoire
        [SerializeField] private float minimumSeparationAngle = 45f; // Angle min entre rifts

        [Header("Progressive Evolution")]
        [SerializeField] private bool enableProgressiveEvolution = true;
        [SerializeField] private float evolutionRate = 0.02f; // Vitesse d'évolution par frame
        [SerializeField] private float maxEvolutionPerUpdate = 0.1f; // Limite par update
        [SerializeField] private float terrainUpdateThrottling = 2.0f; // NOUVEAU - Throttling terrain updates

        [Header("Quality Control")]
        [SerializeField] private float minimumContinentSize = 0.15f; // Taille min des continents (fraction)
        [SerializeField] private bool preventTinyIslands = true;
        [SerializeField] private float coastalSmoothingRadius = 5f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showSeparationGizmos = false;
        [SerializeField] private bool testSeparationNow = false;

        [Header("Migration from Old System")]
        [SerializeField] private bool replaceOldRiftingSystem = true;
        [SerializeField] private bool cleanupOldRiftLayers = true;

        public enum SeparationPattern
        {
            RadialFromCenter,    // Rifts radiaux depuis le centre
            CrossPattern,        // Croix ou X
            YPattern,           // Pattern en Y 
            ParallelLines,      // Lignes parallèles
            CustomAngles        // Angles définis manuellement
        }

        [System.Serializable]
        public class SeparationRift
        {
            public List<Vector2Int> points = new List<Vector2Int>();
            public float currentDepth;
            public float targetDepth;
            public float width;
            public Vector2 direction;
            public float age;
            public bool isActive;
            public bool influencedByFissures;
            public List<SimpleVolcano> associatedFissures = new List<SimpleVolcano>();
        }

        // Données du système
        private List<SeparationRift> separationRifts = new List<SeparationRift>();
        private Vector2Int continentCenter;
        private bool systemInitialized = false;
        private bool separationActive = false;
        private float[,] separationInfluenceMap;
        private float lastTerrainUpdate = 0f; // NOUVEAU - Throttling terrain updates
        private bool hasPendingTerrainUpdate = false; // NOUVEAU - Flag pour updates en attente
        private bool needsInitialTrigger = true;
        private float lastKnownCoreTemp = -1f;
        private float lastTempCheckTime = 0f;
        private float tempCheckInterval = 2f; // Vérifier toutes les 2 secondes

        // Références système
        private PlanetGenerator planetGenerator;
        private GameManager gameManager;
        private SimpleTwoPlateGenerator plateGenerator;
        private TerrainModificationManager terrainManager;
        private CleanVolcanicSystem volcanicSystem;
        private int mapResolution;

        // Couche pour le TerrainManager
        private const string SEPARATION_LAYER = "ContinentalSeparation";

        public static ContinentalSeparationSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LogDebug("🌍 Continental Separation System initialisé");
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
            yield return new WaitForSeconds(3f); // Attendre que tous les systèmes soient prêts

            // Trouver les références
            planetGenerator = PlanetGenerator.Instance;
            gameManager = GameManager.Instance;
            plateGenerator = SimpleTwoPlateGenerator.Instance;
            terrainManager = TerrainModificationManager.Instance;
            volcanicSystem = CleanVolcanicSystem.Instance;

            if (planetGenerator == null || gameManager == null || plateGenerator == null || terrainManager == null)
            {
                LogDebug("❌ Systèmes requis manquants");
                yield break;
            }

            // Attendre que tous les systèmes soient initialisés
            yield return new WaitUntil(() => planetGenerator.HeightMap != null);
            yield return new WaitUntil(() => plateGenerator.IsInitialized);
            yield return new WaitUntil(() => terrainManager.IsInitialized);

            mapResolution = planetGenerator.Resolution;
            separationInfluenceMap = new float[mapResolution, mapResolution];

            // ✅ MIGRATION : Nettoyer l'ancien système
            if (replaceOldRiftingSystem)
            {
                CleanupOldRiftingSystem();
            }

            systemInitialized = true;
            LogDebug("✅ Continental Separation System initialisé et prêt");

            // ✅ CONTOURNEMENT BUG UNITY : Retarder l'abonnement aux événements
            yield return new WaitForSeconds(0.5f); // Délai de sécurité comme dans OceanSphere

            // S'abonner aux changements de température CORE (pas surface) APRÈS le délai
            if (gameManager != null)
            {
                //GameManager.OnCoreTemperatureChanged += OnTemperatureChanged;
                //LogDebug("🔗 Écoute OnCoreTemperatureChanged activée (post-délai)");
            }


            // ✅ CORRECTION : Ne PAS déclencher automatiquement au début
            // Test automatique si configuré ET température CORE suffisante
            if (testSeparationNow && gameManager != null && gameManager.CoreTemperature >= separationTemperatureThreshold)
            {
                yield return new WaitForSeconds(2f);
                InitiateContinentalSeparation();
            }
            else if (testSeparationNow)
            {
                LogDebug($"⚠️ Test séparation demandé mais température CORE insuffisante: {gameManager?.CoreTemperature:F0}°C < {separationTemperatureThreshold:F0}°C");
            }
        }

        private void Update()
        {
            if (needsInitialTrigger && systemInitialized && gameManager != null)
            {
                if (Time.time > 5f) // Après 5 secondes de jeu
                {
                    GameManager.OnCoreTemperatureChanged += OnTemperatureChanged;
                    OnTemperatureChanged(gameManager.CoreTemperature);
                    LogDebug("🔗 Écoute OnCoreTemperatureChanged activée (post-délai)");
                    needsInitialTrigger = false;
                }
            }
            if (systemInitialized && gameManager != null && Time.time - lastTempCheckTime >= tempCheckInterval)
            {
                CheckTemperatureManually();
                lastTempCheckTime = Time.time;
            }
            if (enableProgressiveEvolution && separationActive && separationRifts.Count > 0)
            {
                EvolveSeparationRifts();
            }

            // ✅ NOUVEAU : Gérer les mises à jour terrain throttlées
            //if (hasPendingTerrainUpdate && Time.time - lastTerrainUpdate >= terrainUpdateThrottling)
            //{
            //    ApplySeparationRiftsToTerrain();
            //    hasPendingTerrainUpdate = false;
            //    lastTerrainUpdate = Time.time;
            //}
        }

        private void CheckTemperatureManually()
        {
            if (!enableSeparation) return;

            float currentCoreTemp = gameManager.CoreTemperature;

            // Vérifier si température a changé significativement
            if (Mathf.Abs(currentCoreTemp - lastKnownCoreTemp) > 1f)
            {
                LogDebug($"🌡️ Auto-check: Température core {lastKnownCoreTemp:F0}°C → {currentCoreTemp:F0}°C");
                OnTemperatureChanged(currentCoreTemp);
                lastKnownCoreTemp = currentCoreTemp;
            }
        }

        private void OnTemperatureChanged(float newCoreTemperature)
        {
            if (!systemInitialized || !enableSeparation) return;

            // ✅ PROTECTION PHASE : Comme CleanVolcanicSystem
            if (gameManager.CurrentPhase != GamePhase.Geological) return;

            // ✅ PROTECTION PLAGE TEMPÉRATURE : Comme IsVolcanismPossible
            if (!IsSeparationPossible) return;

            // ✅ CORRECTION : Utiliser CoreTemperature au lieu de SurfaceTemperature
            if (newCoreTemperature <= separationTemperatureThreshold && !separationActive)
            {
                LogDebug($"🌡️ Température CORE de séparation atteinte: {newCoreTemperature:F0}°C <= {separationTemperatureThreshold:F0}°C");
                InitiateContinentalSeparation();
            }
            else if (newCoreTemperature > separationTemperatureThreshold && enableDebugLogs)
            {
                LogDebug($"🌡️ Température CORE actuelle: {newCoreTemperature:F0}°C (seuil: {separationTemperatureThreshold:F0}°C)");
            }
        }

        [ContextMenu("Initiate Continental Separation")]
        public void InitiateContinentalSeparation()
        {
            if (!systemInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            LogDebug("🌍 === DÉBUT SÉPARATION CONTINENTALE ===");

            // 1. Identifier le centre du supercontinent
            IdentifyContinentCenter();

            // 2. Analyser l'influence volcanique existante
            if (integrateExistingFissures)
            {
                AnalyzeVolcanicInfluence();
            }

            // 3. Générer le pattern de séparation
            GenerateSeparationPattern();

            // 4. Créer les rifts de séparation
            CreateSeparationRifts();

            // 5. Activer l'évolution progressive
            separationActive = true;

            LogDebug($"✅ Séparation initiée: {separationRifts.Count} rifts créés");
        }

        private void IdentifyContinentCenter()
        {
            LogDebug("📍 Identification du centre continental...");

            int totalContinentalCells = 0;
            Vector2 centroid = Vector2.zero;

            // Calculer le centroïde du supercontinent
            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    if (plateGenerator.IsContinentalCell(x, y))
                    {
                        centroid += new Vector2(x, y);
                        totalContinentalCells++;
                    }
                }
            }

            if (totalContinentalCells > 0)
            {
                centroid /= totalContinentalCells;
                continentCenter = new Vector2Int(Mathf.RoundToInt(centroid.x), Mathf.RoundToInt(centroid.y));
                LogDebug($"📍 Centre continental: {continentCenter} ({totalContinentalCells} cellules)");
            }
            else
            {
                LogDebug("❌ Aucune cellule continentale trouvée");
            }
        }

        private void AnalyzeVolcanicInfluence()
        {
            LogDebug("🌋 Analyse de l'influence volcanique...");

            if (volcanicSystem?.Volcanoes == null)
            {
                LogDebug("⚠️ Système volcanique non disponible");
                return;
            }

            // Réinitialiser la carte d'influence
            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    separationInfluenceMap[x, y] = 1.0f; // Influence de base
                }
            }

            int fissuresFound = 0;

            // Analyser chaque volcan fissural
            foreach (var volcano in volcanicSystem.Volcanoes)
            {
                if (volcano.type == VolcanoType.Fissure)
                {
                    Vector2Int volcanoPos = volcano.heightMapCoords;

                    // Appliquer l'influence de faiblesse autour de la fissure
                    int influenceRadius = Mathf.RoundToInt(fissureInfluenceRadius);

                    for (int dx = -influenceRadius; dx <= influenceRadius; dx++)
                    {
                        for (int dy = -influenceRadius; dy <= influenceRadius; dy++)
                        {
                            int x = volcanoPos.x + dx;
                            int y = volcanoPos.y + dy;

                            if (IsValidMapCoordinate(x, y) && plateGenerator.IsContinentalCell(x, y))
                            {
                                float distance = Vector2.Distance(new Vector2(dx, dy), Vector2.zero);
                                if (distance <= influenceRadius)
                                {
                                    float falloff = 1f - (distance / influenceRadius);
                                    separationInfluenceMap[x, y] *= (1f + fissureWeakeningFactor * falloff);
                                }
                            }
                        }
                    }

                    fissuresFound++;
                }
            }

            LogDebug($"🌋 Influence volcanique analysée: {fissuresFound} fissures trouvées");
        }

        private void GenerateSeparationPattern()
        {
            LogDebug($"📐 Génération pattern de séparation: {separationPattern}");

            List<Vector2> separationDirections = new List<Vector2>();

            switch (separationPattern)
            {
                case SeparationPattern.RadialFromCenter:
                    GenerateRadialPattern(separationDirections);
                    break;
                case SeparationPattern.CrossPattern:
                    GenerateCrossPattern(separationDirections);
                    break;
                case SeparationPattern.YPattern:
                    GenerateYPattern(separationDirections);
                    break;
                case SeparationPattern.ParallelLines:
                    GenerateParallelPattern(separationDirections);
                    break;
                case SeparationPattern.CustomAngles:
                    GenerateCustomPattern(separationDirections);
                    break;
            }

            LogDebug($"📐 Pattern généré: {separationDirections.Count} directions");
        }

        private void GenerateRadialPattern(List<Vector2> directions)
        {
            // ✅ TEST : Angles fixes parfaits pour diagnostic
            float[] fixedAngles = { 0f, 90f, 180f, 270f }; // Croix parfaite

            LogDebug($"🔍 TEST ANGLES FIXES: {targetContinentalMasses} directions");

            for (int i = 0; i < targetContinentalMasses && i < fixedAngles.Length; i++)
            {
                float angle = fixedAngles[i];

                Vector2 direction = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                directions.Add(direction);
                LogDebug($"   Direction {i}: angle={angle}°, vector=({direction.x:F2}, {direction.y:F2})");
            }

            LogDebug($"✅ Total directions TEST: {directions.Count}");
        }

        private void GenerateCrossPattern(List<Vector2> directions)
        {
            // Pattern en croix simple
            directions.Add(Vector2.right);
            directions.Add(Vector2.up);

            if (targetContinentalMasses > 4)
            {
                // Ajouter diagonales
                directions.Add(new Vector2(1, 1).normalized);
                directions.Add(new Vector2(-1, 1).normalized);
            }
        }

        private void GenerateYPattern(List<Vector2> directions)
        {
            // Pattern en Y (120° entre chaque branche)
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f + Random.Range(-15f, 15f); // Variation
                directions.Add(new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                ));
            }
        }

        private void GenerateParallelPattern(List<Vector2> directions)
        {
            // Lignes parallèles verticales ou horizontales
            bool vertical = Random.value > 0.5f;
            Vector2 baseDirection = vertical ? Vector2.up : Vector2.right;

            directions.Add(baseDirection);
            if (targetContinentalMasses > 2)
            {
                directions.Add(-baseDirection);
            }
        }

        private void GenerateCustomPattern(List<Vector2> directions)
        {
            // Pattern personnalisable - à étendre selon besoins
            GenerateRadialPattern(directions); // Fallback
        }

        private void CreateSeparationRifts()
        {
            LogDebug("🔨 Création des rifts de séparation...");

            separationRifts.Clear();

            var directions = GetGeneratedDirections();
            LogDebug($"🎯 Directions récupérées: {directions.Count}");

            int riftIndex = 0;
            foreach (Vector2 direction in directions)
            {
                LogDebug($"🔨 Création rift {riftIndex}: direction ({direction.x:F2}, {direction.y:F2})");

                SeparationRift rift = CreateRiftInDirection(direction);

                LogDebug($"   Rift créé avec {rift.points.Count} points");

                if (rift.points.Count > 0)
                {
                    separationRifts.Add(rift);
                    LogDebug($"✅ Rift {riftIndex} ajouté au système");
                }
                else
                {
                    LogDebug($"❌ Rift {riftIndex} vide - non ajouté");
                }

                riftIndex++;
            }

            LogDebug($"🎯 RÉSULTAT FINAL: {separationRifts.Count} rifts dans le système");

            // Appliquer les rifts au terrain
            ApplySeparationRiftsToTerrain();
        }

        private List<Vector2> GetGeneratedDirections()
        {
            List<Vector2> directions = new List<Vector2>();

            // ✅ CORRECTION : Utiliser le pattern réellement sélectionné
            switch (separationPattern)
            {
                case SeparationPattern.RadialFromCenter:
                    GenerateRadialPattern(directions);
                    break;
                case SeparationPattern.CrossPattern:
                    GenerateCrossPattern(directions);
                    break;
                case SeparationPattern.YPattern:
                    GenerateYPattern(directions);
                    break;
                case SeparationPattern.ParallelLines:
                    GenerateParallelPattern(directions);
                    break;
                case SeparationPattern.CustomAngles:
                    GenerateCustomPattern(directions);
                    break;
            }

            return directions;
        }

        private SeparationRift CreateRiftInDirection(Vector2 direction)
        {
            SeparationRift rift = new SeparationRift
            {
                direction = direction,
                currentDepth = -0.4f,
                targetDepth = riftValleyDepth,
                width = riftValleyWidth,
                age = 0f,
                isActive = true
            };

            if (useOrganicRifts)
            {
                // ✅ NOUVEAU : Créer un chemin géologique réaliste
                rift.points = CreateOrganicRiftPath(direction);
            }
            else
            {
                // ✅ ANCIEN : Méthode originale pour comparaison
                rift.points = CreateStraightRiftPath(direction);
            }

            return rift;
        }

        private List<Vector2Int> CreateOrganicRiftPath(Vector2 initialDirection)
        {
            List<Vector2Int> path = new List<Vector2Int>();

            // === ÉTAPE 1: TRACER DANS UNE DIRECTION ===
            List<Vector2Int> forwardPath = TraceOrganicPathFromCenter(initialDirection, false);

            // === ÉTAPE 2: TRACER DANS L'AUTRE DIRECTION ===
            List<Vector2Int> backwardPath = TraceOrganicPathFromCenter(-initialDirection, true);

            // === ÉTAPE 3: COMBINER LES CHEMINS ===
            // Ajouter le chemin arrière (inversé pour ordre correct)
            backwardPath.Reverse();
            path.AddRange(backwardPath);

            // Ajouter le centre (éviter doublon)
            if (!path.Contains(continentCenter))
            {
                path.Add(continentCenter);
            }

            // Ajouter le chemin avant
            path.AddRange(forwardPath);

            // === ÉTAPE 4: LISSER LE CHEMIN FINAL ===
            return SmoothGeologicalPath(path);
        }


        private void ApplySeparationRiftsToTerrain()
        {
            // ✅ PROTECTION : Ne pas appliquer si déjà fait
            if (separationRifts.Count == 0)
            {
                LogDebug("⚠️ Aucun rift à appliquer");
                return;
            }

            // ✅ PROTECTION : Éviter double application
            if (Time.time - lastTerrainUpdate < 0.1f) // Minimum 100ms entre applications
            {
                LogDebug("⚠️ Application trop récente - ignorée");
                return;
            }

            LogDebug("🗺️ Application des rifts au terrain...");

            float[,] separationLayer = new float[mapResolution, mapResolution];
            int totalModifiedCells = 0;

            // ✅ CORRECTION : Traiter TOUS les rifts dans UNE SEULE couche
            foreach (var rift in separationRifts)
            {
                LogDebug($"   Traitement rift avec {rift.points.Count} points...");

                foreach (var point in rift.points)
                {
                    int halfWidth = Mathf.RoundToInt(rift.width * 0.5f);

                    for (int dx = -halfWidth; dx <= halfWidth; dx++)
                    {
                        for (int dy = -halfWidth; dy <= halfWidth; dy++)
                        {
                            int x = point.x + dx;
                            int y = point.y + dy;

                            if (IsValidMapCoordinate(x, y))
                            {
                                float distance = Vector2.Distance(new Vector2(dx, dy), Vector2.zero);
                                if (distance <= halfWidth)
                                {
                                    float depthFactor = 1f - (distance / halfWidth);
                                    float depth = rift.currentDepth * depthFactor;

                                    if (integrateExistingFissures && separationInfluenceMap != null)
                                    {
                                        depth *= separationInfluenceMap[x, y];
                                    }

                                    // ✅ CORRECTION : Prendre la plus profonde valeur (ou additionner si souhaité)
                                    if (separationLayer[x, y] == 0f || depth < separationLayer[x, y])
                                    {
                                        separationLayer[x, y] = depth;
                                    }
                                    totalModifiedCells++;
                                }
                            }
                        }
                    }
                }
            }

            // ✅ UN SEUL APPEL pour enregistrer TOUS les rifts ensemble
            terrainManager.RegisterModificationLayer(SEPARATION_LAYER, separationLayer, "ContinentalSeparation_All");

            lastTerrainUpdate = Time.time;
            LogDebug($"✅ TOUS LES RIFTS appliqués en une couche: {totalModifiedCells} cellules");
        }

        private List<Vector2Int> TraceOrganicPathFromCenter(Vector2 initialDirection, bool isBackward)
        {
            List<Vector2Int> path = new List<Vector2Int>();

            Vector2 currentPos = new Vector2(continentCenter.x, continentCenter.y);
            Vector2 currentDirection = initialDirection.normalized;

            float maxLength = mapResolution * riftValleyLength * 0.5f;
            int maxSteps = Mathf.RoundToInt(maxLength);

            for (int step = 0; step < maxSteps; step++)
            {
                // === CALCULER NOUVELLE DIRECTION GÉOLOGIQUE ===
                Vector2 newDirection = CalculateGeologicalDirection(
                    currentPos,
                    currentDirection,
                    step,
                    maxSteps,
                    isBackward
                );

                // === AVANCER AVEC LA NOUVELLE DIRECTION ===
                currentPos += newDirection;
                currentDirection = newDirection.normalized;

                Vector2Int mapPos = new Vector2Int(
                    Mathf.RoundToInt(currentPos.x),
                    Mathf.RoundToInt(currentPos.y)
                );

                // === CONDITIONS D'ARRÊT ===
                if (!IsValidMapCoordinate(mapPos.x, mapPos.y)) break;
                if (!plateGenerator.IsContinentalCell(mapPos.x, mapPos.y)) break;

                // Éviter doublons
                if (!path.Contains(mapPos))
                {
                    path.Add(mapPos);
                }
            }

            return path;
        }

        private Vector2 CalculateGeologicalDirection(Vector2 currentPos, Vector2 currentDirection, int step, int maxSteps, bool isBackward)
        {
            // ✅ COMPOSANTE 1: MOMENTUM - Conserver direction générale
            Vector2 momentumComponent = currentDirection * momentumPreservation;

            // ✅ COMPOSANTE 2: GÉOLOGIE - Suivre les caractéristiques géologiques
            Vector2 geologicalComponent = CalculateGeologicalInfluence(currentPos) * geologicalFollowing;

            // ✅ COMPOSANTE 3: DÉVIATION NATURELLE - Variation réaliste
            float deviationAmount = riftPathDeviation * (1f - momentumPreservation - geologicalFollowing);
            float randomAngle = Random.Range(-deviationAmount, deviationAmount) * Mathf.Deg2Rad;

            Vector2 deviatedDirection = new Vector2(
                currentDirection.x * Mathf.Cos(randomAngle) - currentDirection.y * Mathf.Sin(randomAngle),
                currentDirection.x * Mathf.Sin(randomAngle) + currentDirection.y * Mathf.Cos(randomAngle)
            );

            Vector2 deviationComponent = deviatedDirection * (1f - momentumPreservation - geologicalFollowing);

            // ✅ COMPOSANTE 4: PROFIL DE LARGEUR - Variation selon position
            float pathProgress = (float)step / maxSteps;
            float widthFactor = riftWidthProfile.Evaluate(pathProgress);

            // === COMBINER TOUTES LES COMPOSANTES ===
            Vector2 combinedDirection = momentumComponent + geologicalComponent + deviationComponent;

            // Normaliser et appliquer vitesse adaptative
            float stepSize = 0.8f + (widthFactor * 0.4f); // Vitesse variable
            return combinedDirection.normalized * stepSize;
        }

        private Vector2 CalculateGeologicalInfluence(Vector2 currentPos)
        {
            Vector2 influence = Vector2.zero;

            // === INFLUENCE 1: BRUIT GÉOLOGIQUE GÉNÉRAL ===
            float geologicalNoise = Mathf.PerlinNoise(
                currentPos.x * noiseScale + 1000f,
                currentPos.y * noiseScale + 1000f
            );

            // Convertir bruit en direction d'influence
            float noiseAngle = geologicalNoise * 2f * Mathf.PI;
            Vector2 noiseDirection = new Vector2(Mathf.Cos(noiseAngle), Mathf.Sin(noiseAngle));
            influence += noiseDirection * 0.3f;

            // === INFLUENCE 2: INFLUENCE VOLCANIQUE ===
            if (integrateExistingFissures && separationInfluenceMap != null)
            {
                int x = Mathf.RoundToInt(currentPos.x);
                int y = Mathf.RoundToInt(currentPos.y);

                if (IsValidMapCoordinate(x, y))
                {
                    float volcanicInfluence = separationInfluenceMap[x, y];

                    // Plus l'influence volcanique est forte, plus on suit cette direction
                    if (volcanicInfluence > 1.1f) // Zone affaiblie
                    {
                        // Calculer gradient vers zone plus faible
                        Vector2 gradient = CalculateVolcanicGradient(currentPos);
                        influence += gradient * 0.4f;
                    }
                }
            }

            // === INFLUENCE 3: ÉVITEMENT DES BORDS ===
            Vector2 centerAttraction = CalculateCenterAttraction(currentPos);
            influence += centerAttraction * 0.2f;

            return influence;
        }

        private Vector2 CalculateVolcanicGradient(Vector2 currentPos)
        {
            Vector2 gradient = Vector2.zero;
            float searchRadius = 3f;

            // Chercher dans un petit rayon pour trouver direction de plus forte influence
            for (int dx = -3; dx <= 3; dx++)
            {
                for (int dy = -3; dy <= 3; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int x = Mathf.RoundToInt(currentPos.x) + dx;
                    int y = Mathf.RoundToInt(currentPos.y) + dy;

                    if (IsValidMapCoordinate(x, y))
                    {
                        float neighborInfluence = separationInfluenceMap[x, y];

                        // Plus l'influence est forte, plus on est attiré
                        if (neighborInfluence > 1.0f)
                        {
                            Vector2 direction = new Vector2(dx, dy).normalized;
                            gradient += direction * (neighborInfluence - 1.0f);
                        }
                    }
                }
            }

            return gradient.normalized;
        }

        private Vector2 CalculateCenterAttraction(Vector2 currentPos)
        {
            // Légère attraction vers le centre pour éviter que le rift sorte trop du continent
            Vector2 toCenter = new Vector2(continentCenter.x, continentCenter.y) - currentPos;
            float distanceFromCenter = toCenter.magnitude;

            // Plus on s'éloigne, plus l'attraction est forte
            float maxDistance = mapResolution * 0.3f;
            if (distanceFromCenter > maxDistance)
            {
                float attractionStrength = (distanceFromCenter - maxDistance) / maxDistance;
                return toCenter.normalized * Mathf.Clamp01(attractionStrength);
            }

            return Vector2.zero;
        }

        private List<Vector2Int> SmoothGeologicalPath(List<Vector2Int> rawPath)
        {
            if (rawPath.Count < pathSmoothingSteps * 2) return rawPath;

            List<Vector2Int> smoothedPath = new List<Vector2Int>();

            // Garder les premiers points
            for (int i = 0; i < pathSmoothingSteps && i < rawPath.Count; i++)
            {
                smoothedPath.Add(rawPath[i]);
            }

            // Lisser les points intermédiaires
            for (int i = pathSmoothingSteps; i < rawPath.Count - pathSmoothingSteps; i++)
            {
                Vector2 averagePos = Vector2.zero;
                int count = 0;

                // Moyenne des points environnants
                for (int j = -pathSmoothingSteps; j <= pathSmoothingSteps; j++)
                {
                    if (i + j >= 0 && i + j < rawPath.Count)
                    {
                        averagePos += new Vector2(rawPath[i + j].x, rawPath[i + j].y);
                        count++;
                    }
                }

                if (count > 0)
                {
                    averagePos /= count;
                    Vector2Int smoothedPoint = new Vector2Int(
                        Mathf.RoundToInt(averagePos.x),
                        Mathf.RoundToInt(averagePos.y)
                    );

                    // Vérifier validité du point lissé
                    if (IsValidMapCoordinate(smoothedPoint.x, smoothedPoint.y) &&
                        plateGenerator.IsContinentalCell(smoothedPoint.x, smoothedPoint.y))
                    {
                        smoothedPath.Add(smoothedPoint);
                    }
                    else
                    {
                        smoothedPath.Add(rawPath[i]); // Garder l'original si problème
                    }
                }
            }

            // Garder les derniers points
            for (int i = Mathf.Max(pathSmoothingSteps, rawPath.Count - pathSmoothingSteps); i < rawPath.Count; i++)
            {
                smoothedPath.Add(rawPath[i]);
            }

            return smoothedPath;
        }

        private List<Vector2Int> CreateStraightRiftPath(Vector2 direction)
        {
            // Ancienne méthode pour comparaison
            List<Vector2Int> points = new List<Vector2Int>();

            // Direction positive
            Vector2 currentPos = new Vector2(continentCenter.x, continentCenter.y);
            Vector2 step = direction.normalized;
            float maxLength = mapResolution * riftValleyLength * 0.5f;
            int steps = Mathf.RoundToInt(maxLength);

            for (int i = 0; i < steps; i++)
            {
                Vector2Int mapPos = new Vector2Int(
                    Mathf.RoundToInt(currentPos.x),
                    Mathf.RoundToInt(currentPos.y)
                );

                if (!IsValidMapCoordinate(mapPos.x, mapPos.y)) break;
                if (!plateGenerator.IsContinentalCell(mapPos.x, mapPos.y)) break;

                points.Add(mapPos);
                currentPos += step;
            }

            // Direction négative
            currentPos = new Vector2(continentCenter.x, continentCenter.y);
            step = -direction.normalized;

            for (int i = 1; i < steps; i++)
            {
                Vector2Int mapPos = new Vector2Int(
                    Mathf.RoundToInt(currentPos.x),
                    Mathf.RoundToInt(currentPos.y)
                );

                if (!IsValidMapCoordinate(mapPos.x, mapPos.y)) break;
                if (!plateGenerator.IsContinentalCell(mapPos.x, mapPos.y)) break;

                points.Insert(0, mapPos);
                currentPos += step;
            }

            return points;
        }


        private void EvolveSeparationRifts()
        {
            bool hasEvolved = false;
            float evolutionThisFrame = 0f;

            foreach (var rift in separationRifts)
            {
                if (rift.isActive && rift.currentDepth > rift.targetDepth)
                {
                    float evolution = evolutionRate * Time.deltaTime;
                    evolution = Mathf.Min(evolution, maxEvolutionPerUpdate - evolutionThisFrame);

                    if (evolution > 0f)
                    {
                        rift.currentDepth = Mathf.Max(rift.targetDepth, rift.currentDepth - evolution);
                        rift.age += Time.deltaTime;
                        evolutionThisFrame += evolution;
                        hasEvolved = true;

                        if (evolutionThisFrame >= maxEvolutionPerUpdate) break;
                    }
                }
            }

            // ✅ OPTIMISATION : Marquer pour mise à jour throttlée au lieu d'appliquer immédiatement
            if (hasEvolved)
            {
                hasPendingTerrainUpdate = true;
            }
        }

        // === MÉTHODES UTILITAIRES ===
        private bool IsValidMapCoordinate(int x, int y)
        {
            return x >= 0 && x < mapResolution && y >= 0 && y < mapResolution;
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[ContinentalSeparation] {message}");
            }
        }

        // === MÉTHODES PUBLIQUES ===
        public List<SeparationRift> GetSeparationRifts() => separationRifts;
        public bool IsSeparationActive => separationActive;
        public Vector2Int ContinentCenter => continentCenter;

        // ✅ NOUVEAU - Getters comme CleanVolcanicSystem
        public float CurrentCoreTemperature => gameManager?.CoreTemperature ?? 0f;
        public float SeparationStartTemp => separationTemperatureThreshold;
        public float SeparationEndTemp => separationEndTemperature;
        public bool IsSeparationPossible => CurrentCoreTemperature <= separationTemperatureThreshold &&
                                           CurrentCoreTemperature >= separationEndTemperature;

        // === CLEANUP ===
        private void OnDestroy()
        {
            if (GameManager.OnCoreTemperatureChanged != null)
            {
                GameManager.OnCoreTemperatureChanged -= OnTemperatureChanged;
            }
        }

        // === MÉTHODES DE DEBUG ===
        [ContextMenu("Test Separation Now")]
        public void TestSeparationNow()
        {
            if (!systemInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            // ✅ CORRECTION : Forcer le test seulement si vraiment demandé
            LogDebug($"🧪 TEST FORCÉ - Température CORE actuelle: {gameManager?.CoreTemperature:F0}°C");
            InitiateContinentalSeparation();
        }

        [ContextMenu("Clear Separation")]
        public void ClearSeparation()
        {
            separationRifts.Clear();
            separationActive = false;

            if (terrainManager != null)
            {
                // Créer une couche vide pour effacer
                float[,] emptyLayer = new float[mapResolution, mapResolution];
                terrainManager.RegisterModificationLayer(SEPARATION_LAYER, emptyLayer, "ClearSeparation");
            }

            LogDebug("🧹 Séparation continentale effacée");
        }

        [ContextMenu("Show Separation Status")]
        public void ShowSeparationStatus()
        {
            LogDebug("📊 === STATUT SÉPARATION CONTINENTALE ===");
            LogDebug($"   🌡️ Température CORE actuelle: {gameManager?.CoreTemperature:F0}°C");
            LogDebug($"   🌡️ Plage séparation: {separationEndTemperature:F0}°C - {separationTemperatureThreshold:F0}°C");
            LogDebug($"   ✅ Séparation possible: {IsSeparationPossible}");
            LogDebug($"   ⚡ Séparation active: {separationActive}");
            LogDebug($"   📏 Rifts créés: {separationRifts.Count}");
            LogDebug($"   🎯 Continents cibles: {targetContinentalMasses}");
            LogDebug($"   📐 Pattern: {separationPattern}");

            if (separationRifts.Count > 0)
            {
                foreach (var rift in separationRifts)
                {
                    LogDebug($"     - Points: {rift.points.Count}, Profondeur: {rift.currentDepth:F3}/{rift.targetDepth:F3}");
                }
            }
        }

        // === MIGRATION DEPUIS L'ANCIEN SYSTÈME ===
        private void CleanupOldRiftingSystem()
        {
            LogDebug("🔄 === NETTOYAGE ANCIEN SYSTÈME RIFTING ===");

            // 1. Chercher l'ancien ContinentalRiftingSystem
            //var oldRiftingSystem = FindAnyObjectByType<ContinentalRiftingSystem>();
            //if (oldRiftingSystem != null)
            //{
            //    LogDebug("🗑️ Désactivation ancien ContinentalRiftingSystem...");
            //    oldRiftingSystem.enabled = false;
            //    // Ne pas détruire pour éviter les erreurs de références
            //}

            // 2. Nettoyer les couches rifts dans le TerrainManager
            if (cleanupOldRiftLayers && terrainManager != null)
            {
                LogDebug("🧹 Nettoyage couches rifts anciennes...");

                // Effacer la couche "Rifts" de l'ancien système
                float[,] emptyRiftLayer = new float[mapResolution, mapResolution];
                terrainManager.RegisterModificationLayer(TerrainModificationManager.RIFT_LAYER, emptyRiftLayer, "CleanupOldRifts");

                LogDebug("✅ Anciennes couches rifts nettoyées");
            }

            LogDebug("✅ Nettoyage ancien système terminé");
        }

        [ContextMenu("Force Cleanup Old System")]
        public void ForceCleanupOldSystem()
        {
            if (systemInitialized)
            {
                CleanupOldRiftingSystem();
            }
            else
            {
                LogDebug("⚠️ Système non initialisé - nettoyage reporté");
            }
        }

        [ContextMenu("Test Geological Parameters")]
        public void TestGeologicalParameters()
        {
            LogDebug("🧪 PARAMÈTRES GÉOLOGIQUES ACTUELS:");
            LogDebug($"   Déviation chemin: {riftPathDeviation}°");
            LogDebug($"   Suivi géologie: {geologicalFollowing:P0}");
            LogDebug($"   Conservation momentum: {momentumPreservation:P0}");
            LogDebug($"   Échelle bruit: {noiseScale}");
            LogDebug($"   Lissage: {pathSmoothingSteps} étapes");
            LogDebug($"   Mode organique: {useOrganicRifts}");
        }

        [ContextMenu("Test Single Rift Only")]
        public void TestSingleRiftOnly()
        {
            LogDebug("🧪 TEST: UN SEUL RIFT pour validation");

            // Vider complètement
            separationRifts.Clear();

            // Créer UN SEUL rift manuellement
            SeparationRift singleRift = new SeparationRift
            {
                direction = Vector2.right, // Direction Est uniquement
                currentDepth = riftValleyDepth, // -0.4f
                targetDepth = riftValleyDepth,
                width = 20f, // Largeur réduite pour test
                isActive = true,
                points = new List<Vector2Int>()
            };

            // Ajouter quelques points manuellement
            for (int i = 0; i < 50; i++)
            {
                singleRift.points.Add(new Vector2Int(continentCenter.x + i, continentCenter.y));
            }

            separationRifts.Add(singleRift);

            LogDebug($"✅ Test rift unique: {singleRift.points.Count} points");

            // Appliquer
            ApplySeparationRiftsToTerrain();
        }
    }
}