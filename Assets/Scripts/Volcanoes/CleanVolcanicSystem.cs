using LifeStory.Core;
using LifeStory.Generation;
using LifeStory.Geology;
using LifeStory.Terrain;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LifeStory.Volcanoes
{
    public enum VolcanicState
    {
        Dormant,    // En attente du prochain seuil
        Erupting,   // Éruption en cours
        Extinct     // Tous les seuils consommés - Formation géologique passive
    }

    /// <summary>
    /// Système volcanique avec pool management et nouvelle logique d'activation
    /// </summary>
    public class CleanVolcanicSystem : MonoBehaviour
    {
        [Header("🌋 Pool Management System")]
        [SerializeField] private int maxActiveVolcanoes = 15;  // Max volcans (actifs + dormants)
        [SerializeField] private bool enableExtinctCleanup = true;  // Nettoyer les systèmes des volcans éteints
        [SerializeField] private float cleanupInterval = 30f;  // Intervalle de nettoyage (secondes)
        [SerializeField] private bool showPoolStats = true;   // Afficher stats du pool

        [Header("Configuration")]
        [SerializeField] private float volcanoSpawnRate = 0.8f;
        [SerializeField] private float minDistanceBetweenVolcanoes = 0.2f;
        //[SerializeField] private bool elevateVolcanoToSummit = false;
        [SerializeField] private int maxVolcanoesPerFrame = 1;

        [Header("Positionnement")]
        [SerializeField] private bool elevateVolcanoToSummit = true;
        [SerializeField] private float summitPositioningDelay = 0.2f;
        [SerializeField] private float heightSafetyMargin = 0.1f;

        [Header("🔥 Seuils Température NOYAU")]
        [SerializeField] private float minVolcanicTemp = 1000f;  // ✅ NOUVEAU : Mort complète
        [SerializeField] private float maxVolcanicTemp = 4200f;  // ✅ NOUVEAU : Début possible
        [SerializeField] private int minEruptionsPerVolcano = 2;
        [SerializeField] private int maxEruptionsPerVolcano = 5;
        [SerializeField] private float temperatureDropMin = 250f;
        [SerializeField] private float temperatureDropMax = 400f;

        [Header("Activité Progressive")]
        [SerializeField] private AnimationCurve activityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Émissions Gazeuses")]
        [SerializeField] private bool enableGasEmissions = true;
        [SerializeField] private float shieldCO2EmissionBase = 0.002f;
        [SerializeField] private float fissureCH4EmissionRate = 0.0005f;   // CH₄ par seconde Fissure active
        [SerializeField] private AnimationCurve emissionIntensityCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 1);

        [Header("🌊 Volcanic Water Emissions")]
        [SerializeField] private bool enableWaterEmissions = true;
        [SerializeField] private float shieldWaterEmissionBase = 0.003f;      // H₂O par éruption Shield
        [SerializeField] private float fissureWaterEmissionRate = 0.001f;     // H₂O par seconde Fissure active
        [SerializeField] private float waterEmissionIntensityCurve = 1f;      // Facteur intensité émission eau
        [SerializeField] private bool showWaterEmissionLogs = true;

        [Header("Déformation Terrain")]
        [SerializeField] private float baseDeformationRadius = 2f;
        [SerializeField] private float baseDeformationStrength = 0.3f;
        [SerializeField] private AnimationCurve deformationFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("🎯 Positionnement Surface Précis")]
        [SerializeField] private bool useInterpolatedPositioning = true;
        [SerializeField] private float interpolationSearchRadius = 8f;
        [SerializeField] private int interpolationMinVertices = 4;
        [SerializeField] private float positioningDelay = 0.3f;
        [SerializeField] private bool validatePositioning = true;

        [Header("Visuels")]
        [SerializeField] private GameObject defaultVolcanoPrefab;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showEruptionEvents = true;

        // === DONNÉES VOLCAN ===
        [System.Serializable]
        public class SimpleVolcano
        {
            public Vector3 worldPosition;
            public GameObject visualObject;
            public Vector2Int heightMapCoords;
            public VolcanoType type;
            public VolcanoTypeData typeData;
            public float intensity;
            public string layerName;

            public List<float> temperatureThresholds = new List<float>();
            public VolcanicState currentState = VolcanicState.Dormant;
            public int eruptionsCompleted = 0;
            public int totalPlannedEruptions = 0;
        }

        // === VARIABLES SYSTÈME ===
        private List<SimpleVolcano> volcanoes = new List<SimpleVolcano>();
        private List<SimpleVolcano> activeFissures = new List<SimpleVolcano>();

        // Pool management
        private float lastCleanupTime = 0f;
        private int totalVolcanoesCreated = 0;

        // Frame limiting
        private int volcanosCreatedThisFrame = 0;
        private float lastVolcanoCreationTime = 0f;
        private int lastFrameCount = -1;

        // Émissions continues
        private float lastContinuousEmissionTime = 0f;
        private float continuousEmissionInterval = 1f;

        // Références système
        private PlanetGenerator planetGenerator;
        private GameManager gameManager;
        private TerrainModificationManager terrainManager;
        private VolcanoTypesManager volcanoTypesManager;
        private VolcanicHotSpotSystem hotSpotSystem;

        private bool isInitialized = false;
        private int mapResolution;
        private float lastKnownCoreTemperature = 0f;

        /// <summary>Events pour les émissions gazeuses volcaniques</summary>
        public static System.Action<VolcanoType, float, float> OnVolcanicGasEmission;
        public static System.Action<float> OnVolcanicWaterEmission;

        public static CleanVolcanicSystem Instance { get; private set; }

        // === PROPRIÉTÉS POOL ===

        /// <summary>Nombre de volcans actifs + dormants (excluant les éteints)</summary>
        public int ActiveVolcanoCount => volcanoes.Count(v => v.currentState != VolcanicState.Extinct);

        /// <summary>Nombre de volcans éteints dans le pool</summary>
        public int ExtinctVolcanoCount => volcanoes.Count(v => v.currentState == VolcanicState.Extinct);

        /// <summary>Nombre de volcans dormants</summary>
        public int DormantVolcanoCount => volcanoes.Count(v => v.currentState == VolcanicState.Dormant);

        /// <summary>Nombre de volcans en éruption</summary>
        public int EruptingVolcanoCount => volcanoes.Count(v => v.currentState == VolcanicState.Erupting);

        /// <summary>Vérifier si on peut créer un nouveau volcan</summary>
        public bool CanCreateNewVolcano => ActiveVolcanoCount < maxActiveVolcanoes;

        // === GETTERS EXISTANTS ===
        public int VolcanoCount => volcanoes.Count;
        public List<SimpleVolcano> Volcanoes => new List<SimpleVolcano>(volcanoes);
        public bool IsInitialized => isInitialized;
        public float CurrentCoreTemperature => gameManager?.CoreTemperature ?? 0f;
        public float MinVolcanicCoreTemp => minVolcanicTemp;
        public float MaxVolcanicCoreTemp => maxVolcanicTemp;
        public bool IsVolcanismPossible => CurrentCoreTemperature >= minVolcanicTemp && CurrentCoreTemperature <= maxVolcanicTemp;

        // === LIFECYCLE ===

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LogDebug("🌋 Clean Volcanic System avec Pool Management initialisé");
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
            yield return new WaitForSeconds(1f);

            planetGenerator = PlanetGenerator.Instance;
            gameManager = GameManager.Instance;
            terrainManager = TerrainModificationManager.Instance;
            volcanoTypesManager = VolcanoTypesManager.Instance;

            if (planetGenerator == null || gameManager == null || terrainManager == null)
            {
                LogDebug("❌ Systèmes requis manquants");
                yield break;
            }

            yield return new WaitUntil(() => planetGenerator.HeightMap != null);
            yield return new WaitUntil(() => terrainManager.IsInitialized);

            // Forcer initialisation VolcanoTypesManager
            if (volcanoTypesManager != null && !volcanoTypesManager.IsInitialized)
            {
                LogDebug("🔄 Forçage initialisation VolcanoTypesManager...");
                volcanoTypesManager.InitializeVolcanoTypes();

                float timeout = 3f;
                float elapsed = 0f;
                while (!volcanoTypesManager.IsInitialized && elapsed < timeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }
            }

            mapResolution = planetGenerator.PlanetResolution;
            InitializeHotSpotSystem();

            // S'abonner aux changements de température noyau
            if (GameManager.OnCoreTemperatureChanged != null)
            {
                GameManager.OnCoreTemperatureChanged += OnCoreTemperatureChanged;
            }

            isInitialized = true;
            LogDebug("✅ Système volcanique initialisé avec pool management");
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Maintenance du pool
            ProcessPoolMaintenance();

            // Création de volcans (avec limite de pool)
            if (ShouldCreateVolcano() && CanCreateVolcanoThisFrame())
            {
                CreateVolcano();
                volcanosCreatedThisFrame++;
                lastVolcanoCreationTime = Time.time;
            }

            // Reset compteur par frame
            if (Time.frameCount != lastFrameCount)
            {
                volcanosCreatedThisFrame = 0;
                lastFrameCount = Time.frameCount;
            }

            // Émissions continues
            if (enableGasEmissions && Time.time - lastContinuousEmissionTime >= continuousEmissionInterval)
            {
                ProcessContinuousFissureEmissions();
                lastContinuousEmissionTime = Time.time;
            }

            foreach (var volcano in volcanoes) // Pool actif seulement
            {
                if (volcano.currentState == VolcanicState.Erupting)
                {
                    // Lumière rouge/orange pendant éruption
                    SetVolcanoLight(volcano, Color.red, 2f);
                    LogDebug($"🔥 {volcano.type} en éruption - Lumière activée");
                }
                else
                {
                    // Éteindre lumière si dormant
                    SetVolcanoLight(volcano, Color.black, 0f);
                }
            }
        }

        // === POOL MANAGEMENT ===

        /// <summary>Nettoyage automatique des systèmes actifs des volcans éteints</summary>
        private void ProcessPoolMaintenance()
        {
            if (Time.time - lastCleanupTime >= cleanupInterval)
            {
                CleanupExtinctVolcanoes();
                lastCleanupTime = Time.time;

                if (showPoolStats)
                {
                    ShowPoolStatistics();
                }
            }
        }

        /// <summary>Nettoyer seulement les systèmes actifs des volcans éteints (pas les volcans eux-mêmes)</summary>
        private void CleanupExtinctVolcanoes()
        {
            if (!enableExtinctCleanup) return;

            var extinctVolcanoes = volcanoes.Where(v => v.currentState == VolcanicState.Extinct).ToList();

            if (extinctVolcanoes.Count == 0) return;

            LogDebug($"🧹 Nettoyage systèmes actifs: {extinctVolcanoes.Count} volcans éteints");

            foreach (var extinct in extinctVolcanoes)
            {
                // ✅ GARDER l'objet visuel (la montagne reste)
                // Le volcan éteint reste visible comme formation géologique

                // Retirer seulement des systèmes actifs
                if (activeFissures.Contains(extinct))
                {
                    activeFissures.Remove(extinct);
                    LogDebug($"   🚫 {extinct.type} retiré des fissures actives");
                }

                // Retirer des hot-spots (plus d'activité thermique)
                if (hotSpotSystem != null)
                {
                    hotSpotSystem.RemoveVolcanicHotSpot(extinct);
                }

                LogDebug($"   💤 {extinct.type} désactivé (formation géologique conservée)");
            }

            LogDebug($"✅ Systèmes nettoyés: {ActiveVolcanoCount}/{maxActiveVolcanoes} volcans actifs");
        }

        // === LOGIQUE CRÉATION ===

        /// <summary>Vérifier si on doit créer un nouveau volcan - AVEC POOL MANAGEMENT</summary>
        private bool ShouldCreateVolcano()
        {
            // Vérifier limite du pool AVANT tout le reste
            if (!CanCreateNewVolcano)
            {
                // Si on ne peut pas créer mais qu'il y a des éteints, nettoyer d'abord
                if (ExtinctVolcanoCount > 0 && enableExtinctCleanup)
                {
                    LogDebug($"⚠️ Pool plein ({ActiveVolcanoCount}/{maxActiveVolcanoes}) - Nettoyage forcé");
                    CleanupExtinctVolcanoes();

                    // Re-vérifier après nettoyage
                    if (!CanCreateNewVolcano)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            // Vérifications température noyau
            float coreTemp = gameManager.CoreTemperature;
            if (coreTemp < minVolcanicTemp || coreTemp > maxVolcanicTemp) return false;

            float activity = Mathf.InverseLerp(minVolcanicTemp, maxVolcanicTemp, coreTemp);
            return Random.value < volcanoSpawnRate * activity * Time.deltaTime;
        }

        private void DeformTerrain(SimpleVolcano volcano)
        {
            ApplyEruptiveDeformation(volcano);
        }

        private void ApplyEruptiveDeformation(SimpleVolcano volcano)
        {
            Vector2Int center = volcano.heightMapCoords;

            // === NOUVEAU SYSTÈME CONFIGURABLE ===
            if (volcano.typeData?.useAdvancedDeformation == true)
            {
                // Utiliser le nouveau système de profils
                var profile = volcano.typeData.GetDeformationProfile();
                float[,] deformationLayer = ConfigurableVolcanicDeformation.ApplyDeformation(
                    center, volcano, mapResolution, baseDeformationRadius, baseDeformationStrength,
                    planetGenerator.PlanetRadius, profile);

                terrainManager.RegisterModificationLayer(volcano.layerName, deformationLayer, "AdvancedVolcanicDeformation");

                LogDebug($"🆕 Déformation avancée appliquée pour {volcano.type} - Profil: {profile.radiusMultiplier:F1}x radius, {profile.strengthMultiplier:F1}x strength");
            }
            else
            {
                // === SYSTÈME LEGACY (compatible) ===
                float[,] deformationLayer = new float[mapResolution, mapResolution];

                float radius = baseDeformationRadius * (volcano.typeData?.deformationRadius ?? 1f);
                float strength = baseDeformationStrength * (volcano.typeData?.deformationStrength ?? 1f);

                // Compensation par eruptionDuration (existant)
                float durationFactor = volcano.typeData?.eruptionDuration ?? 1f;
                float durationCompensation = Mathf.Pow(durationFactor, 0.7f);
                strength *= durationCompensation;

                // Augmenter force avec chaque éruption (existant)
                float cycleMultiplier = 1.0f + (volcano.eruptionsCompleted * 0.15f);
                strength *= cycleMultiplier;

                // Application circulaire simple (code existant)
                float mapRadius = (radius / planetGenerator.PlanetRadius) * mapResolution;
                int radiusInt = Mathf.RoundToInt(mapRadius);

                for (int x = center.x - radiusInt; x <= center.x + radiusInt; x++)
                {
                    for (int y = center.y - radiusInt; y <= center.y + radiusInt; y++)
                    {
                        if (x < 0 || x >= mapResolution || y < 0 || y >= mapResolution) continue;

                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center.x, center.y));
                        if (distance <= radiusInt)
                        {
                            float falloff = deformationFalloff.Evaluate(distance / radiusInt);
                            deformationLayer[x, y] = strength * volcano.intensity * falloff;
                        }
                    }
                }

                terrainManager.RegisterModificationLayer(volcano.layerName, deformationLayer, "LegacyVolcanicDeformation");

                LogDebug($"🔧 Déformation legacy appliquée pour {volcano.type} - Force: {strength:F3}");
            }
        }

        private bool CanCreateVolcanoThisFrame()
        {
            return volcanosCreatedThisFrame < maxVolcanoesPerFrame &&
                   Time.time - lastVolcanoCreationTime >= 0.1f;
        }

        /// <summary>Créer un nouveau volcan avec tracking du pool</summary>
        private void CreateVolcano()
        {
            LogDebug($"🌋 === CRÉATION VOLCAN #{totalVolcanoesCreated + 1} (Pool: {ActiveVolcanoCount}/{maxActiveVolcanoes}) ===");

            // Vérifications pré-création
            if (!CanCreateNewVolcano)
            {
                LogDebug($"❌ Cannot create volcano - Pool limit reached ({ActiveVolcanoCount}/{maxActiveVolcanoes})");
                return;
            }

            // 1. Position sur surface
            Vector3 volcanoPosition = GenerateRandomSurfacePosition();
            if (volcanoPosition == Vector3.zero)
            {
                LogDebug($"❌ Cannot find valid position for volcano");
                return;
            }

            // 2. Créer mesh visuel
            SimpleVolcano volcano = CreateVolcanoMesh(volcanoPosition);
            if (volcano == null)
            {
                LogDebug($"❌ Failed to create volcano mesh");
                return;
            }

            // 3. Programmer les seuils température noyau
            ProgramCoreTemperatureThresholds(volcano);

            // 4. Déformation terrain initiale
            DeformTerrain(volcano);

            // 5. Positionner au sommet (optionnel)
            if (elevateVolcanoToSummit)
            {
                StartCoroutine(ElevateToSummit(volcano));
            }

            // 6. Ajouter au pool
            volcanoes.Add(volcano);
            totalVolcanoesCreated++;

            // 7. Enregistrer comme hot-spot
            RegisterVolcanoAsHotSpot(volcano);

            LogDebug($"✅ Volcan {volcano.type} créé avec {volcano.totalPlannedEruptions} éruptions programmées");
            LogDebug($"   Pool status: {ActiveVolcanoCount}/{maxActiveVolcanoes} actifs, {ExtinctVolcanoCount} éteints");
        }

        private System.Collections.IEnumerator ElevateToSummit(SimpleVolcano volcano)
        {
            yield return new WaitForSeconds(summitPositioningDelay);

            Vector2Int coords = volcano.heightMapCoords;
            float terrainHeight = terrainManager.GetComposedHeightAt(coords.x, coords.y);
            Vector3 direction = volcano.worldPosition.normalized;

            float heightMultiplier = planetGenerator.HeightMultiplier;
            if (heightMultiplier < 2f)
                heightMultiplier = planetGenerator.PlanetRadius * 0.1f;

            float worldHeight = terrainHeight * heightMultiplier;
            float summitRadius = planetGenerator.PlanetRadius + worldHeight + heightSafetyMargin;
            Vector3 summitPosition = direction * summitRadius;

            volcano.visualObject.transform.position = summitPosition;
            volcano.worldPosition = summitPosition;
        }

        private void SetVolcanoLight(SimpleVolcano volcano, Color color, float intensity)
        {
            Light volcanoLight = volcano.visualObject?.GetComponent<Light>();
            if (volcanoLight != null)
            {
                volcanoLight.color = color;
                volcanoLight.intensity = intensity;
            }
        }

        private System.Collections.IEnumerator PositionVolcanoOnSurface(SimpleVolcano volcano)
        {
            yield return new WaitForSeconds(positioningDelay);

            Vector3 direction = volcano.worldPosition.normalized;

            if (useInterpolatedPositioning)
            {
                // === MÉTHODE INTERPOLATION (RECOMMANDÉE) ===
                Vector3 surfacePosition = GetInterpolatedSurfacePosition(direction);

                if (surfacePosition != Vector3.zero)
                {
                    // Appliquer safety margin
                    Vector3 finalPosition = surfacePosition + (direction * heightSafetyMargin);

                    volcano.visualObject.transform.position = finalPosition;
                    volcano.worldPosition = finalPosition;

                    LogDebug($"✅ Volcan {volcano.type} positionné par interpolation à {finalPosition.magnitude:F3}");

                    if (validatePositioning)
                    {
                        ValidateVolcanoPosition(volcano, surfacePosition);
                    }
                }
                else
                {
                    LogDebug($"❌ Interpolation échec pour {volcano.type}, fallback vers méthode legacy");
                    yield return StartCoroutine(ElevateToSummit_Legacy(volcano));
                }
            }
            else
            {
                // === FALLBACK VERS MÉTHODE EXISTANTE ===
                yield return StartCoroutine(ElevateToSummit_Legacy(volcano));
            }
        }

        private void ValidateVolcanoPosition(SimpleVolcano volcano, Vector3 targetSurface)
        {
            Vector3 volcanoPos = volcano.visualObject.transform.position;
            Vector3 surfaceDirection = targetSurface.normalized;
            Vector3 volcanoDirection = volcanoPos.normalized;

            float directionError = Vector3.Angle(surfaceDirection, volcanoDirection);
            float heightDifference = Mathf.Abs(volcanoPos.magnitude - targetSurface.magnitude);

            bool positionValid = directionError < 2f && heightDifference < (planetGenerator.PlanetRadius * 0.1f);

            if (!positionValid)
            {
                LogDebug($"⚠️ Position volcan {volcano.type} questionnable:");
                LogDebug($"   Erreur direction: {directionError:F2}°");
                LogDebug($"   Différence hauteur: {heightDifference:F3}");
            }
            else
            {
                LogDebug($"✅ Position volcan {volcano.type} validée");
            }
        }

        private System.Collections.IEnumerator ElevateToSummit_Legacy(SimpleVolcano volcano)
        {
            Vector2Int coords = volcano.heightMapCoords;
            float terrainHeight = terrainManager.GetComposedHeightAt(coords.x, coords.y);
            Vector3 direction = volcano.worldPosition.normalized;

            float heightMultiplier = planetGenerator.HeightMultiplier;
            if (heightMultiplier < 2f)
                heightMultiplier = planetGenerator.PlanetRadius * 0.1f;

            float worldHeight = terrainHeight * heightMultiplier;
            float summitRadius = planetGenerator.PlanetRadius + worldHeight + heightSafetyMargin;
            Vector3 summitPosition = direction * summitRadius;

            volcano.visualObject.transform.position = summitPosition;
            volcano.worldPosition = summitPosition;

            LogDebug($"⚠️ Volcan {volcano.type} positionné par méthode legacy");

            yield return null;
        }


        private SimpleVolcano CreateVolcanoMesh(Vector3 position)
        {
            VolcanoType type = VolcanoType.Shield;
            VolcanoTypeData typeData = null;

            if (volcanoTypesManager?.IsInitialized == true)
            {
                type = volcanoTypesManager.ChooseVolcanoType(gameManager.CoreTemperature, position);
                typeData = volcanoTypesManager.GetVolcanoTypeData(type);
            }

            var volcano = new SimpleVolcano
            {
                worldPosition = position,
                type = type,
                typeData = typeData,
                intensity = Random.Range(0.3f, 1.0f),
                layerName = $"Volcano_{type}_{Time.time:F3}",
                heightMapCoords = WorldToHeightMapCoords(position)
            };

            // Sélection prefab défensive
            GameObject prefab = null;

            if (typeData?.prefab != null)
            {
                prefab = typeData.prefab;
                LogDebug($"🎯 Prefab type utilisé: {prefab.name} pour {type}");
            }
            else if (defaultVolcanoPrefab != null)
            {
                prefab = defaultVolcanoPrefab;
                LogDebug($"⚠️ Prefab default utilisé pour {type}");
            }
            else
            {
                LogDebug($"❌ Aucun prefab disponible pour {type}");
                return null;
            }

            // ✅ CORRECTION 1 : Calculer l'orientation correcte
            Vector3 surfaceNormal = position.normalized; // Normal à la surface sphérique
            Quaternion correctOrientation = Quaternion.LookRotation(Vector3.forward, surfaceNormal);
            // Ou alternativement :
            // Quaternion correctOrientation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);

            // ✅ CORRECTION 2 : Instancier avec la bonne orientation et parent
            volcano.visualObject = Instantiate(prefab, position, correctOrientation);

            // ✅ CORRECTION 3 : Définir le bon parent (UnifiedVolcanicSystem)
            volcano.visualObject.transform.SetParent(this.transform); // CleanVolcanicSystem est child de UnifiedVolcanicSystem

            volcano.visualObject.name = $"Volcano_{type}_{volcano.heightMapCoords.x}_{volcano.heightMapCoords.y}";

            // Échelle aléatoire
            if (typeData != null)
            {
                Vector3 scale = Vector3.one;
                scale.x = Random.Range(typeData.scaleRange.x, typeData.scaleRange.y);
                scale.y = Random.Range(typeData.scaleRange.x, typeData.scaleRange.y);
                scale.z = scale.x;
                volcano.visualObject.transform.localScale = scale;
            }

            LogDebug($"🌋 Volcan {type} créé à {position} avec orientation correcte");

            return volcano;
        }

        // === SYSTÈME ÉRUPTIONS ===

        private void OnCoreTemperatureChanged(float newCoreTemperature)
        {
            CheckTemperatureThresholds(newCoreTemperature);
            lastKnownCoreTemperature = newCoreTemperature;
        }

        private void CheckTemperatureThresholds(float currentCoreTemp)
        {
            foreach (var volcano in volcanoes)
            {
                if (volcano.currentState == VolcanicState.Extinct) continue;

                if (volcano.temperatureThresholds.Count > 0 &&
                    currentCoreTemp <= volcano.temperatureThresholds[0])
                {
                    TriggerEruption(volcano, currentCoreTemp);
                }
            }
        }

        private Vector3 GetInterpolatedSurfacePosition(Vector3 direction)
        {
            Mesh planetMesh = planetGenerator.MeshFilter?.mesh;

            if (planetMesh == null)
            {
                LogDebug("❌ Mesh planète non disponible pour interpolation");
                return Vector3.zero;
            }

            Vector3 surfacePosition = MeshSurfaceInterpolator.GetSurfacePositionFromDirection(
                planetMesh,
                direction,
                interpolationSearchRadius,
                interpolationMinVertices
            );

            return surfacePosition;
        }

        private void TriggerEruption(SimpleVolcano volcano, float triggerCoreTemp)
        {
            volcano.temperatureThresholds.RemoveAt(0);
            volcano.eruptionsCompleted++;
            volcano.currentState = VolcanicState.Erupting;

            float durationFactor = volcano.typeData?.eruptionDuration ?? 1f;
            string durationType = GetDurationDescription(durationFactor);
            float eruptionStrength = volcano.typeData?.explosivity ?? 0.5f;
            float intensity = volcano.intensity; // 0.3f - 1.0f
            float lightIntensity = eruptionStrength * intensity * 3f; // 0.9f - 3.0f
            Color lightColor = Color.Lerp(Color.cyan, Color.red, eruptionStrength);



            LogDebug($"🌋 ÉRUPTION {durationType} - {volcano.type} #{volcano.eruptionsCompleted}/{volcano.totalPlannedEruptions}");
            LogDebug($"   Déclenchée par NOYAU à {triggerCoreTemp:F0}°C (durée factor: {durationFactor:F1}x)");

            ApplyEruptiveDeformation(volcano);

            if (enableGasEmissions)
            {
                HandleVolcanicGasEmission(volcano, triggerCoreTemp);
            }

            // Gestion fissures actives
            if (volcano.type == VolcanoType.Fissure && volcano.currentState == VolcanicState.Erupting)
            {
                if (!activeFissures.Contains(volcano))
                {
                    activeFissures.Add(volcano);
                    LogDebug($"   🔥 Fissure ajoutée aux émissions continues");
                }
            }

            // Marquer comme dormant ou éteint
            if (volcano.temperatureThresholds.Count == 0)
            {
                volcano.currentState = VolcanicState.Extinct;
                volcanoes.Remove(volcano); // ← Retirer du pool
                LogDebug($"🏁 {volcano.type} retiré du pool - Formation géologique permanente");

                if (volcano.type == VolcanoType.Fissure && activeFissures.Contains(volcano))
                {
                    activeFissures.Remove(volcano);
                    LogDebug($"   🏁 Fissure retirée des émissions (éteinte)");
                }

                LogDebug($"   🏁 VOLCAN ÉTEINT - Formation géologique permanente");

            }
            else
            {
                volcano.currentState = VolcanicState.Dormant;

                if (volcano.type == VolcanoType.Fissure && activeFissures.Contains(volcano))
                {
                    activeFissures.Remove(volcano);
                    LogDebug($"   💤 Fissure retirée des émissions (dormante)");
                }

                LogDebug($"   💤 Prochaine éruption: {volcano.temperatureThresholds[0]:F0}°C (NOYAU)");
            }

            NotifyHotSpotEruption(volcano);
            SetVolcanoLight(volcano, lightColor, lightIntensity);
            StartCoroutine(EndEruptionAfterDelay(volcano, durationFactor));

            LogDebug($"🔥 ÉRUPTION {volcano.type}: Force={eruptionStrength:F2}, Durée={durationFactor:F1}x, Intensité={lightIntensity:F1}");

        }

        private IEnumerator EndEruptionAfterDelay(SimpleVolcano volcano, float durationFactor)
        {
            yield return new WaitForSeconds(durationFactor * 2f); // 2-6 secondes visibles

            // Éteindre la lumière après délai
            SetVolcanoLight(volcano, Color.black, 0f);
            LogDebug($"💡 Lumière éteinte pour {volcano.type} après éruption");
        }

        private void HandleVolcanicGasEmission(SimpleVolcano volcano, float currentCoreTemp)
        {
            float gasEmissionFactor = volcano.typeData?.gasEmission ?? 0.3f;
            float temperatureFactor = Mathf.InverseLerp(minVolcanicTemp, maxVolcanicTemp, currentCoreTemp);

            switch (volcano.type)
            {
                case VolcanoType.Shield:
                    EmitShieldCO2(volcano, gasEmissionFactor, temperatureFactor, currentCoreTemp);

                    // NOUVEAU - Émissions eau Shield
                    if (enableWaterEmissions)
                    {
                        EmitShieldWater(volcano, gasEmissionFactor, temperatureFactor, currentCoreTemp);
                    }
                    break;

                case VolcanoType.Fissure:
                    // Fissure : Pas d'émission instantanée, seulement continue (gérée dans Update)
                    LogDebug($"   💨 Fissure: Début émissions CH₄ + H₂O continues (Noyau: {currentCoreTemp:F0}°C)");
                    break;

                default:
                    // Autres types futurs : Émission CO₂ + H₂O par défaut
                    EmitShieldCO2(volcano, gasEmissionFactor * 0.7f, temperatureFactor, currentCoreTemp);

                    if (enableWaterEmissions)
                    {
                        EmitShieldWater(volcano, gasEmissionFactor * 0.7f, temperatureFactor, currentCoreTemp);
                    }
                    break;
            }
        }

        private void EmitShieldWater(SimpleVolcano volcano, float gasEmissionFactor, float temperatureFactor, float coreTemp)
        {
            // Calcul émission H₂O basée sur :
            // - Taille du volcan (intensity)
            // - Capacité d'émission du type (gasEmissionFactor)  
            // - Température noyau actuelle (plus chaud = plus de vapeur)
            // - Cycle d'éruption (plus tard = moins de vapeur disponible)

            float baseEmission = shieldWaterEmissionBase;
            float intensityMultiplier = volcano.intensity;
            float typeMultiplier = gasEmissionFactor;
            float tempMultiplier = temperatureFactor * waterEmissionIntensityCurve;

            // Diminution avec l'âge du volcan (moins de vapeur disponible)
            float ageMultiplier = Mathf.Lerp(1f, 0.5f, (float)volcano.eruptionsCompleted / volcano.totalPlannedEruptions);

            float totalWaterEmission = baseEmission * intensityMultiplier * typeMultiplier * tempMultiplier * ageMultiplier;

            // Notifier GameManager
            OnVolcanicWaterEmission?.Invoke(totalWaterEmission);

            if (showWaterEmissionLogs)
            {
                LogDebug($"   💧 H₂O émis: {totalWaterEmission:F6} (intensité:{intensityMultiplier:F2} × type:{typeMultiplier:F2} × temp:{tempMultiplier:F2} × âge:{ageMultiplier:F2}) [Noyau: {coreTemp:F0}°C]");
            }
        }

        private void EmitShieldCO2(SimpleVolcano volcano, float gasEmissionFactor, float temperatureFactor, float coreTemp)
        {
            // Calcul émission CO₂ basée sur :
            // - Taille du volcan (intensity)
            // - Capacité d'émission du type (gasEmissionFactor)  
            // - Température noyau actuelle (plus chaud = plus de gaz)
            // - Cycle d'éruption (plus tard = moins de gaz)

            float baseEmission = shieldCO2EmissionBase;
            float intensityMultiplier = volcano.intensity;
            float typeMultiplier = gasEmissionFactor;
            float tempMultiplier = emissionIntensityCurve.Evaluate(temperatureFactor);

            // Diminution avec l'âge du volcan (moins de gaz disponible)
            float ageMultiplier = Mathf.Lerp(1f, 0.6f, (float)volcano.eruptionsCompleted / volcano.totalPlannedEruptions);

            float totalCO2Emission = baseEmission * intensityMultiplier * typeMultiplier * tempMultiplier * ageMultiplier;

            // Notifier GameManager
            OnVolcanicGasEmission?.Invoke(VolcanoType.Shield, totalCO2Emission, 0f);

            LogDebug($"   💨 CO₂ émis: {totalCO2Emission:F6} (intensité:{intensityMultiplier:F2} × type:{typeMultiplier:F2} × temp:{tempMultiplier:F2} × âge:{ageMultiplier:F2}) [Noyau: {coreTemp:F0}°C]");
        }

        private void ProcessContinuousFissureEmissions()
        {
            if (activeFissures.Count == 0) return;

            float currentCoreTemp = gameManager.CoreTemperature;
            float temperatureFactor = Mathf.InverseLerp(minVolcanicTemp, maxVolcanicTemp, currentCoreTemp);
            float tempMultiplier = emissionIntensityCurve.Evaluate(temperatureFactor);

            float totalCH4Emission = 0f;
            float totalWaterEmission = 0f; // NOUVEAU

            foreach (var fissure in activeFissures)
            {
                if (fissure.currentState != VolcanicState.Erupting) continue;

                float gasEmissionFactor = fissure.typeData?.gasEmission ?? 0.6f;

                // === ÉMISSIONS CH₄ EXISTANTES ===
                float baseEmission = fissureCH4EmissionRate * continuousEmissionInterval;
                float intensityMultiplier = fissure.intensity;
                float typeMultiplier = gasEmissionFactor;
                float ageMultiplier = Mathf.Lerp(1f, 0.4f, (float)fissure.eruptionsCompleted / fissure.totalPlannedEruptions);

                float fissureCH4 = baseEmission * intensityMultiplier * typeMultiplier * tempMultiplier * ageMultiplier;
                totalCH4Emission += fissureCH4;

                // === NOUVELLES ÉMISSIONS H₂O ===
                if (enableWaterEmissions)
                {
                    float baseWaterEmission = fissureWaterEmissionRate * continuousEmissionInterval;
                    float fissureWater = baseWaterEmission * intensityMultiplier * typeMultiplier * tempMultiplier * ageMultiplier;
                    totalWaterEmission += fissureWater;
                }
            }

            // Notifier émissions CH₄ (existant)
            if (totalCH4Emission > 0.0001f)
            {
                OnVolcanicGasEmission?.Invoke(VolcanoType.Fissure, 0f, totalCH4Emission);

                LogDebug($"💨 CH₄ continu émis: {totalCH4Emission:F6} par {activeFissures.Count} fissures actives (Core: {currentCoreTemp:F0}°C)");
            }

            // NOUVEAU - Notifier émissions H₂O
            if (enableWaterEmissions && totalWaterEmission > 0.0001f)
            {
                OnVolcanicWaterEmission?.Invoke(totalWaterEmission);

                if (showWaterEmissionLogs)
                {
                    LogDebug($"💧 H₂O continu émis: {totalWaterEmission:F6} par {activeFissures.Count} fissures actives (Core: {currentCoreTemp:F0}°C)");
                }
            }
        }
        private void InitializeHotSpotSystem()
        {
            hotSpotSystem = FindAnyObjectByType<VolcanicHotSpotSystem>();
            if (hotSpotSystem == null)
            {
                // Créer automatiquement le système s'il n'existe pas
                GameObject hotSpotGO = new GameObject("VolcanicHotSpotSystem");
                hotSpotSystem = hotSpotGO.AddComponent<VolcanicHotSpotSystem>();
                LogDebug("🔥 Système Hot-Spots créé automatiquement");
            }
            else
            {
                LogDebug("🔥 Système Hot-Spots trouvé et connecté");
            }
        }

        private void RegisterVolcanoAsHotSpot(SimpleVolcano volcano)
        {
            if (hotSpotSystem != null)
            {
                hotSpotSystem.RegisterVolcanicHotSpot(volcano);
                LogDebug($"🔥 Hot-spot enregistré pour {volcano.type} à {volcano.worldPosition}");
            }
        }

        private void NotifyHotSpotEruption(SimpleVolcano volcano)
        {
            if (hotSpotSystem != null)
            {
                hotSpotSystem.OnVolcanoEruption(volcano);
                LogDebug($"🔥 Hot-spot notifié éruption {volcano.type}");
            }
        }

        private void ProgramCoreTemperatureThresholds(SimpleVolcano volcano)
        {
            float currentCoreTemp = gameManager.CoreTemperature;

            int baseEruptions = Random.Range(minEruptionsPerVolcano, maxEruptionsPerVolcano + 1);
            LogDebug($"🌋 Programmation seuils NOYAU pour {volcano.type} - Base: {baseEruptions} éruptions");
            float durationFactor = volcano.typeData?.eruptionDuration ?? 1f;
            int adjustedEruptions = Mathf.RoundToInt(baseEruptions / Mathf.Sqrt(durationFactor));
            adjustedEruptions = Mathf.Max(1, adjustedEruptions);

            volcano.totalPlannedEruptions = adjustedEruptions;
            volcano.temperatureThresholds.Clear();

            LogDebug($"🎯 Programmation {volcano.type} - Duration: {durationFactor:F1}x");
            LogDebug($"   Base: {baseEruptions} → Ajusté: {adjustedEruptions} éruptions");
            LogDebug($"   Température NOYAU actuelle: {currentCoreTemp:F0}°C");

            float tempCursor = currentCoreTemp;
            for (int i = 0; i < adjustedEruptions; i++)
            {
                float baseDropMin = temperatureDropMin * (1f + durationFactor * 0.2f);
                float baseDropMax = temperatureDropMax * (1f + durationFactor * 0.3f);
                float tempDrop = Random.Range(baseDropMin, baseDropMax);
                tempCursor -= tempDrop;

                //if (tempCursor < minVolcanicTemp)
                {
                    tempCursor = Random.Range(minVolcanicTemp, minVolcanicTemp + 300f);
                }

                volcano.temperatureThresholds.Add(tempCursor);
                LogDebug($"   Éruption #{i + 1}: {tempCursor:F0}°C NOYAU (drop: {tempDrop:F0}°C)");
            }

            volcano.temperatureThresholds.Sort((a, b) => b.CompareTo(a));
            LogDebug($"✅ Seuils NOYAU programmés pour {volcano.type}: {string.Join(", ", volcano.temperatureThresholds.ConvertAll(t => t.ToString("F0") + "°C"))}");
        }

        // === MÉTHODES DEBUG ===

        [ContextMenu("Show Pool Statistics")]
        public void ShowPoolStatistics()
        {
            LogDebug("📊 === STATISTIQUES POOL VOLCANIQUE ===");
            LogDebug($"Pool actuel: {ActiveVolcanoCount}/{maxActiveVolcanoes} volcans actifs");
            LogDebug($"   • Dormants: {DormantVolcanoCount}");
            LogDebug($"   • En éruption: {EruptingVolcanoCount}");
            LogDebug($"   • Éteints (formations géologiques): {ExtinctVolcanoCount}");
            LogDebug($"Total volcans (toutes formations): {volcanoes.Count}");
            LogDebug($"Total créés depuis le début: {totalVolcanoesCreated}");
            LogDebug($"Nettoyage systèmes: {(enableExtinctCleanup ? "✅ ACTIVÉ" : "❌ DÉSACTIVÉ")}");
            LogDebug($"Prochain nettoyage dans: {(cleanupInterval - (Time.time - lastCleanupTime)):F1}s");

            var typeStats = volcanoes
                .Where(v => v.currentState != VolcanicState.Extinct)
                .GroupBy(v => v.type)
                .ToDictionary(g => g.Key, g => g.Count());

            LogDebug("📈 Répartition par type (actifs seulement):");
            foreach (var kvp in typeStats)
            {
                LogDebug($"   • {kvp.Key}: {kvp.Value}");
            }
        }

        [ContextMenu("Force Cleanup Extinct Systems")]
        public void ForceCleanupExtinctVolcanoes()
        {
            LogDebug("🧹 Force cleanup extinct volcano systems (keeping geological formations)");
            CleanupExtinctVolcanoes();
            ShowPoolStatistics();
        }

        [ContextMenu("Reset Volcano Pool")]
        public void ResetVolcanoPool()
        {
            LogDebug("🔄 Réinitialisation complète du pool volcanique");

            CleanupSimulation();
            totalVolcanoesCreated = 0;
            lastCleanupTime = Time.time;

            LogDebug("✅ Pool volcanique réinitialisé");
        }

        public void CleanupSimulation()
        {
            Debug.Log("🧹 CleanVolcanicSystem: Début nettoyage");

            foreach (var volcano in volcanoes)
            {
                if (volcano.visualObject != null)
                {
                    DestroyImmediate(volcano.visualObject);
                }
            }

            volcanoes.Clear();
            if (activeFissures != null) activeFissures.Clear();

            Debug.Log($"🌋 {volcanoes.Count} volcans nettoyés");
        }

        // === MÉTHODES UTILITAIRES ===

        private Vector2Int WorldToHeightMapCoords(Vector3 worldPos)
        {
            Vector3 direction = worldPos.normalized;
            float longitude = Mathf.Atan2(direction.x, direction.z);
            float latitude = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f));

            float u = (longitude + Mathf.PI) / (2 * Mathf.PI);
            float v = (latitude + Mathf.PI / 2) / Mathf.PI;

            int x = Mathf.Clamp(Mathf.RoundToInt(u * (mapResolution - 1)), 0, mapResolution - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (mapResolution - 1)), 0, mapResolution - 1);

            return new Vector2Int(x, y);
        }

        private Vector3 GenerateRandomSurfacePosition()
        {
            for (int attempts = 0; attempts < 50; attempts++)
            {
                Vector3 direction = Random.onUnitSphere;
                Vector3 position = direction * planetGenerator.PlanetRadius;

                bool tooClose = false;
                foreach (var existing in volcanoes)
                {
                    if (Vector3.Distance(position, existing.worldPosition) < minDistanceBetweenVolcanoes)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose) return position;
            }
            return Vector3.zero;
        }

        private string GetDurationDescription(float durationFactor)
        {
            if (durationFactor >= 3.5f) return "PROLONGÉE";
            else if (durationFactor >= 2.0f) return "MODÉRÉE";
            else return "BRÈVE";
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[CleanVolcanic-CORE] {message}");
        }

        // === CLEANUP ===
        private void OnDestroy()
        {
            if (GameManager.OnCoreTemperatureChanged != null)
            {
                GameManager.OnCoreTemperatureChanged -= OnCoreTemperatureChanged;
            }
        }

        // Méthodes manquantes (à implémenter selon le besoin)
        //private void InitializeHotSpotSystem() { /* TODO */ }
        //private void RegisterVolcanoAsHotSpot(SimpleVolcano volcano) { /* TODO */ }
        //private void NotifyHotSpotEruption(SimpleVolcano volcano) { /* TODO */ }
        //private void ApplyEruptiveDeformation(SimpleVolcano volcano) { /* TODO */ }
        //private void DeformTerrain(SimpleVolcano volcano) { /* TODO */ }
        //private System.Collections.IEnumerator ElevateToSummit(SimpleVolcano volcano) { yield break; }
        //private void HandleVolcanicGasEmission(SimpleVolcano volcano, float currentCoreTemp) { /* TODO */ }
        //private void ProcessContinuousFissureEmissions() { /* TODO */ }
    }
}