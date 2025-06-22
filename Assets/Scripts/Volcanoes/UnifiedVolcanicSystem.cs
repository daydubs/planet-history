//// UnifiedVolcanicSystem.cs - VERSION PROPRE SANS MODIFICATIONS D'ANCRAGE/ROTATION
//using UnityEngine;
//using System.Collections.Generic;
//using LifeStory.Core;
//using LifeStory.Generation;
//using LifeStory.Terrain;
//using LifeStory.Geology;

//namespace LifeStory.Volcanoes
//{
//    /// <summary>
//    /// Système volcanique unifié - VERSION PROPRE
//    /// </summary>
//    public class UnifiedVolcanicSystem : MonoBehaviour
//    {
//        [Header("Deferred Positioning System")]
//        [SerializeField] private float maxWaitTimeForTerrain = 2f;
//        [SerializeField] private float positionCheckInterval = 0.1f;
//        [SerializeField] private bool enableDeferredPositioning = true;

//        [Header("Volcanic Activity")]
//        [SerializeField] private int maxVolcanoes = 15;
//        [SerializeField] private float volcanoSpawnRate = 0.05f;
//        [SerializeField] private float minDistanceBetweenVolcanoes = 3f;

//        [Header("Temperature Control")]
//        [SerializeField] private float minVolcanicTemp = 800f;
//        [SerializeField] private float maxVolcanicTemp = 2000f;

//        [Header("Terrain Deformation - Base Settings")]
//        [SerializeField] private float baseDeformationRadius = 2f;
//        [SerializeField] private float baseDeformationStrength = 0.3f;
//        [SerializeField] private AnimationCurve deformationFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

//        [Header("Visual Settings")]
//        [SerializeField] private GameObject fallbackVolcanoPrefab;
//        [SerializeField] private Material lavaMaterial;
//        [SerializeField] private Color volcanoGlow = Color.red;

//        [Header("Type System Integration")]
//        [SerializeField] private bool useVolcanoTypes = true;
//        [SerializeField] private bool logTypeSelection = true;

//        [Header("Debug")]
//        [SerializeField] private bool enableDebugLogs = true;
//        [SerializeField] private bool showVolcanoGizmos = false;

//        // === DONNÉES UNIFIÉES ===
//        private List<UnifiedVolcano> volcanoes = new List<UnifiedVolcano>();
//        private float[,] volcanicModifications;

//        // Références
//        private PlanetGenerator planetGenerator;
//        private GameManager gameManager;
//        private TerrainModificationManager terrainManager;
//        private VolcanoTypesManager volcanoTypesManager;
//        private bool isInitialized = false;
//        private int mapResolution;

//        // État du système différé
//        private Queue<UnifiedVolcano> pendingVisualsQueue = new Queue<UnifiedVolcano>();
//        private bool isDeferredPositioningActive = false;


//        // === CLASSE VOLCANO SIMPLE ===
//        [System.Serializable]
//        public class UnifiedVolcano
//        {
//            [Header("Position et Visuel")]
//            public Vector3 worldPosition;
//            public GameObject visualObject;

//            [Header("Type et Caractéristiques")]
//            public VolcanoType type;
//            public VolcanoTypeData typeData;

//            [Header("État Volcanique")]
//            public float intensity;
//            public float age;
//            public bool isActive;
//            public VolcanoState state;

//            [Header("Éruptions")]
//            public float lastEruptionTime;
//            public float nextEruptionTime;
//            public bool isCurrentlyErupting;
//            public float eruptionStartTime;
//            public Vector2Int coordinates;  // Coordonnées HeightMap source

//            [Header("Impact Terrain")]
//            public List<Vector2Int> affectedTerrainCells;

//            // ✅ CONSTRUCTEUR PAR DÉFAUT
//            public UnifiedVolcano()
//            {
//                worldPosition = Vector3.zero;
//                type = VolcanoType.Shield;
//                typeData = null;
//                intensity = 0.5f;
//                age = 0f;
//                isActive = false;
//                state = VolcanoState.Dormant;
//                affectedTerrainCells = new List<Vector2Int>();
//                lastEruptionTime = 0f;
//                nextEruptionTime = 0f;
//                isCurrentlyErupting = false;
//                eruptionStartTime = 0f;
//            }

//            public UnifiedVolcano(Vector3 position, VolcanoType volcanoType, VolcanoTypeData typeData) : this()
//            {
//                worldPosition = position;
//                type = volcanoType;
//                this.typeData = typeData;
//                intensity = typeData?.explosivity ?? 0.5f;
//                nextEruptionTime = CalculateNextEruptionTime();
//            }

//            public UnifiedVolcano(Vector3 position, float intensityValue) : this()
//            {
//                worldPosition = position;
//                intensity = intensityValue;
//                nextEruptionTime = CalculateNextEruptionTime();
//            }

//            private float CalculateNextEruptionTime()
//            {
//                if (typeData == null)
//                {
//                    return UnityEngine.Random.Range(50f, 200f);
//                }

//                float baseInterval = Mathf.Lerp(30f, 300f, typeData.explosivity);
//                float randomVariation = UnityEngine.Random.Range(0.5f, 1.5f);
//                return baseInterval * randomVariation;
//            }

//            public void InitializeWithType(VolcanoType volcanoType, VolcanoTypeData volcanoTypeData)
//            {
//                type = volcanoType;
//                typeData = volcanoTypeData;
//                intensity = volcanoTypeData?.explosivity ?? 0.5f;
//                nextEruptionTime = CalculateNextEruptionTime();
//            }

//            public void InitializeBasic(float intensityValue)
//            {
//                intensity = intensityValue;
//                nextEruptionTime = CalculateNextEruptionTime();
//            }

//            // Propriétés calculées
//            public float EffectiveDeformationRadius
//            {
//                get { return typeData?.deformationRadius ?? 1f; }
//            }

//            public float EffectiveDeformationStrength
//            {
//                get { return typeData?.deformationStrength ?? 1f; }
//            }

//            public float EffectiveEruptionDuration
//            {
//                get { return typeData?.eruptionDuration ?? 1f; }
//            }

//            public Color EffectiveLavaColor
//            {
//                get { return typeData?.lavaColor ?? Color.red; }
//            }

//            public bool ShouldStartEruption(float currentTime)
//            {
//                return !isCurrentlyErupting && isActive && currentTime >= nextEruptionTime;
//            }

//            public bool ShouldEndEruption(float currentTime)
//            {
//                if (!isCurrentlyErupting) return false;
//                float eruptionDuration = EffectiveEruptionDuration * 10f;
//                return (currentTime - eruptionStartTime) >= eruptionDuration;
//            }

//            public void StartEruption(float currentTime)
//            {
//                isCurrentlyErupting = true;
//                eruptionStartTime = currentTime;
//                state = VolcanoState.Erupting;
//                lastEruptionTime = currentTime;
//            }

//            public void EndEruption(float currentTime)
//            {
//                isCurrentlyErupting = false;
//                state = isActive ? VolcanoState.Active : VolcanoState.Dormant;
//                nextEruptionTime = currentTime + CalculateNextEruptionTime();
//            }
//        }

//        public enum VolcanoState
//        {
//            Dormant,
//            Active,
//            Erupting
//        }

//        public static UnifiedVolcanicSystem Instance { get; private set; }

//        // === LIFECYCLE ===
//        private void Awake()
//        {
//            if (Instance == null)
//            {
//                Instance = this;
//                LogDebug("🌋 Unified Volcanic System initialisé");
//            }
//            else
//            {
//                Destroy(gameObject);
//            }
//        }

//        private void Start()
//        {
//            StartCoroutine(DelayedInitialization());
//        }

//        private System.Collections.IEnumerator DelayedInitialization()
//        {
//            yield return new WaitForSeconds(1f);

//            planetGenerator = PlanetGenerator.Instance;
//            gameManager = GameManager.Instance;
//            terrainManager = TerrainModificationManager.Instance;
//            volcanoTypesManager = VolcanoTypesManager.Instance;

//            if (planetGenerator == null || gameManager == null)
//            {
//                LogDebug("❌ Références système manquantes");
//                yield break;
//            }

//            if (terrainManager == null)
//            {
//                LogDebug("❌ TerrainModificationManager non trouvé");
//                yield break;
//            }

//            if (useVolcanoTypes)
//            {
//                if (volcanoTypesManager == null)
//                {
//                    LogDebug("⚠️ VolcanoTypesManager non trouvé - Mode types désactivé");
//                    useVolcanoTypes = false;
//                }
//                else
//                {
//                    yield return new WaitUntil(() => volcanoTypesManager.IsInitialized);
//                    LogDebug($"✅ VolcanoTypesManager connecté - {volcanoTypesManager.GetAvailableTypesCount()} types disponibles");
//                }
//            }

//            yield return new WaitUntil(() => planetGenerator.HeightMap != null);
//            yield return new WaitUntil(() => terrainManager.IsInitialized);

//            mapResolution = planetGenerator.Resolution;
//            InitializeVolcanicLayer();

//            isInitialized = true;
//            LogDebug($"✅ Système initialisé - Résolution: {mapResolution}");
//        }

//        private void InitializeVolcanicLayer()
//        {
//            volcanicModifications = new float[mapResolution, mapResolution];

//            for (int x = 0; x < mapResolution; x++)
//            {
//                for (int y = 0; y < mapResolution; y++)
//                {
//                    volcanicModifications[x, y] = 0f;
//                }
//            }

//            LogDebug("🔥 Couche volcanique initialisée");
//        }

//        private void Update()
//        {
//            if (!isInitialized || gameManager.CurrentPhase != GamePhase.Geological) return;

//            if (ShouldCreateVolcano())
//            {
//                CreateNewVolcanoWithFixedCoordinates();
//            }

//            UpdateExistingVolcanoes();
//        }

//        // === CRÉATION DE VOLCANS ===
//        private bool ShouldCreateVolcano()
//        {
//            if (volcanoes.Count >= maxVolcanoes) return false;

//            float temp = gameManager.PlanetTemperature;
//            if (temp < minVolcanicTemp || temp > maxVolcanicTemp) return false;

//            float activity = Mathf.InverseLerp(minVolcanicTemp, maxVolcanicTemp, temp);
//            float spawnChance = volcanoSpawnRate * activity * Time.deltaTime;

//            return Random.value < spawnChance;
//        }

//        private void CreateNewVolcano()
//        {
//            Vector3 position = FindValidVolcanoPosition();
//            if (position == Vector3.zero) return;

//            UnifiedVolcano newVolcano = null;

//            if (useVolcanoTypes && volcanoTypesManager != null)
//            {
//                float temperature = gameManager.PlanetTemperature;
//                VolcanoType selectedType = volcanoTypesManager.ChooseVolcanoType(temperature, position);
//                VolcanoTypeData typeData = volcanoTypesManager.GetVolcanoTypeData(selectedType);

//                if (typeData == null)
//                {
//                    var (explosivity, _, _) = selectedType.GetBasicCharacteristics();
//                    newVolcano = new UnifiedVolcano(position, explosivity);
//                    newVolcano.type = selectedType;
//                }
//                else
//                {
//                    newVolcano = new UnifiedVolcano(position, selectedType, typeData);
//                }

//                if (logTypeSelection)
//                {
//                    LogDebug($"🎯 Type sélectionné: {selectedType} à {temperature:F0}°C");
//                }
//            }
//            else
//            {
//                float intensity = Random.Range(0.5f, 1f);
//                newVolcano = new UnifiedVolcano(position, intensity);
//            }

//            if (newVolcano.intensity <= 0.001f)
//            {
//                newVolcano.intensity = 0.5f;
//            }

//            LogDebug($"🌋 === CRÉATION VOLCAN {newVolcano.type} ===");
//            LogDebug($"   Position fixe: {newVolcano.worldPosition}");

//            // ✅ Appliquer déformation AVANT création visuelle
//            ApplyTerrainDeformationViaTerrainManager(newVolcano);
//            volcanoes.Add(newVolcano);

//            // ✅ Création visuelle SANS recalcul de position
//            if (enableDeferredPositioning)
//            {
//                ScheduleDeferredVisualsCreation(newVolcano);
//            }
//            else
//            {
//                // ✅ FORCER mise à jour mesh immédiate
//                terrainManager.ForceImmediateMeshUpdate($"ImmediateVolcano_{newVolcano.type}");

//                // ✅ Attendre que le mesh soit réellement mis à jour
//                StartCoroutine(WaitForMeshUpdateAndPositionVolcano(newVolcano));
//            }

//            LogDebug($"✅ Volcan créé - Position finale: {newVolcano.worldPosition}");
//        }

//        private Vector2Int FindValidVolcanoCoordinates()
//        {
//            int attempts = 0;

//            while (attempts < 50)
//            {
//                // ✅ COMMENCER par des coordonnées HeightMap valides
//                int x = Random.Range(0, mapResolution);
//                int y = Random.Range(0, mapResolution);
//                Vector2Int coords = new Vector2Int(x, y);

//                // Vérifier distance avec volcans existants
//                bool tooClose = false;
//                foreach (var volcano in volcanoes)
//                {
//                    Vector2Int volcanoCoords = WorldToMapCoordinates(volcano.worldPosition);
//                    float distance = Vector2.Distance(coords, volcanoCoords);

//                    // Distance minimale en coordonnées HeightMap
//                    float minDistanceInMapCoords = (minDistanceBetweenVolcanoes / planetGenerator.PlanetRadius) * mapResolution;

//                    if (distance < minDistanceInMapCoords)
//                    {
//                        tooClose = true;
//                        break;
//                    }
//                }

//                if (!tooClose) return coords;
//                attempts++;
//            }

//            // Fallback : coordonnées aléatoires
//            return new Vector2Int(Random.Range(0, mapResolution), Random.Range(0, mapResolution));
//        }

//        private Vector3 MapCoordinatesToWorldPosition(Vector2Int coords)
//        {
//            // Conversion inverse : HeightMap → Position 3D
//            float u = (float)coords.x / (mapResolution - 1);
//            float v = (float)coords.y / (mapResolution - 1);

//            // UV → Angles sphériques
//            float longitude = u * 2 * Mathf.PI - Mathf.PI;     // [0,1] → [-π, π]
//            float latitude = v * Mathf.PI - Mathf.PI / 2;      // [0,1] → [-π/2, π/2]

//            // Angles → Direction 3D
//            float x = Mathf.Cos(latitude) * Mathf.Sin(longitude);
//            float y = Mathf.Sin(latitude);
//            float z = Mathf.Cos(latitude) * Mathf.Cos(longitude);

//            Vector3 direction = new Vector3(x, y, z).normalized;

//            // Position à la surface de base
//            Vector3 basePosition = direction * planetGenerator.PlanetRadius;

//            LogDebug($"🔄 Conversion coords→3D:");
//            LogDebug($"   Coords: ({coords.x}, {coords.y})");
//            LogDebug($"   UV: ({u:F3}, {v:F3})");
//            LogDebug($"   Angles: lon={longitude:F3}, lat={latitude:F3}");
//            LogDebug($"   Direction: {direction}");
//            LogDebug($"   Position: {basePosition}");

//            return basePosition;
//        }


//        private System.Collections.IEnumerator WaitForMeshUpdateAndPositionVolcano(UnifiedVolcano volcano)
//        {
//            // Attendre 1 frame pour que le mesh soit mis à jour
//            yield return null;

//            // Vérifier que le mesh n'est plus en cours de mise à jour
//            while (terrainManager.IsMeshUpdating)
//            {
//                yield return new WaitForSeconds(0.1f);
//            }

//            // Maintenant créer le visuel avec la position correcte
//            CreateVolcanoVisualWithCorrectPosition(volcano);
//        }

//        private Vector3 FindValidVolcanoPosition()
//        {
//            int attempts = 0;
//            float planetRadius = planetGenerator.PlanetRadius;

//            while (attempts < 20)
//            {
//                Vector3 direction = Random.onUnitSphere;
//                Vector3 position = direction * planetRadius;

//                bool tooClose = false;
//                foreach (var volcano in volcanoes)
//                {
//                    if (Vector3.Distance(position, volcano.worldPosition) < minDistanceBetweenVolcanoes)
//                    {
//                        tooClose = true;
//                        break;
//                    }
//                }

//                if (!tooClose) return position;
//                attempts++;
//            }

//            return Vector3.zero;
//        }

//        private void CreateNewVolcanoWithFixedCoordinates()
//        {
//            // ✅ ÉTAPE 1: Choisir coordonnées HeightMap d'abord
//            Vector2Int volcanoCoords = FindValidVolcanoCoordinates();

//            // ✅ ÉTAPE 2: Calculer position 3D exacte depuis ces coordonnées
//            Vector3 basePosition = MapCoordinatesToWorldPosition(volcanoCoords);

//            // ✅ ÉTAPE 3: Créer volcan avec position cohérente
//            var newVolcano = new UnifiedVolcano
//            {
//                worldPosition = basePosition,  // Position AVANT déformation
//                coordinates = volcanoCoords,   // Stocker les coordonnées source
//                type = VolcanoType.Shield,     // Temporaire
//                intensity = Random.Range(0.3f, 1.0f),
//                age = 0f,
//                state = VolcanoState.Dormant,
//                isActive = true,
//                affectedTerrainCells = new List<Vector2Int>()
//            };


//            LogDebug($"🌋 === CRÉATION VOLCAN COORDONNÉES FIXES ===");
//            LogDebug($"   Coordonnées HeightMap: ({volcanoCoords.x}, {volcanoCoords.y})");
//            LogDebug($"   Position 3D base: {basePosition}");

//            // ✅ ÉTAPE 4: Appliquer déformation à CES coordonnées précises
//            ApplyTerrainDeformationAtCoordinates(newVolcano, volcanoCoords);
//            volcanoes.Add(newVolcano);

//            // ✅ ÉTAPE 5: Positionner visuel à la surface déformée
//            CreateVolcanoVisualAtDeformedSurface(newVolcano);

//            LogDebug($"✅ Volcan créé avec alignement garanti");
//        }

//        private void ApplyTerrainDeformationAtCoordinates(UnifiedVolcano volcano, Vector2Int centerCoords)
//        {
//            float effectiveRadius = baseDeformationRadius * volcano.EffectiveDeformationRadius;
//            float effectiveStrength = baseDeformationStrength * volcano.EffectiveDeformationStrength;

//            int radiusInt = Mathf.RoundToInt(effectiveRadius * mapResolution / planetGenerator.PlanetRadius);

//            volcano.affectedTerrainCells.Clear();
//            int affectedCells = 0;

//            // ✅ Utiliser les coordonnées exactes passées en paramètre
//            for (int x = centerCoords.x - radiusInt; x <= centerCoords.x + radiusInt; x++)
//            {
//                for (int y = centerCoords.y - radiusInt; y <= centerCoords.y + radiusInt; y++)
//                {
//                    if (!IsValidCoordinate(x, y)) continue;

//                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerCoords.x, centerCoords.y));
//                    if (distance <= radiusInt)
//                    {
//                        Vector2Int cellCoord = new Vector2Int(x, y);
//                        float falloff = deformationFalloff.Evaluate(distance / radiusInt);
//                        float deformation = effectiveStrength * volcano.intensity * falloff;

//                        volcanicModifications[x, y] += deformation;
//                        volcano.affectedTerrainCells.Add(cellCoord);
//                        affectedCells++;
//                    }
//                }
//            }

//            terrainManager.RegisterModificationLayer(
//                TerrainModificationManager.VOLCANIC_LAYER,
//                volcanicModifications,
//                $"VolcanicDeformation_{volcano.type}_Coords_{centerCoords.x}_{centerCoords.y}"
//            );

//            LogDebug($"🏔️ Déformation appliquée aux coordonnées ({centerCoords.x}, {centerCoords.y}): {affectedCells} cellules");
//        }

//        private void CreateVolcanoVisualAtDeformedSurface(UnifiedVolcano volcano)
//        {
//            // Attendre que le mesh soit mis à jour
//            StartCoroutine(CreateVisualAfterMeshUpdate(volcano));
//        }

//        private System.Collections.IEnumerator CreateVisualAfterMeshUpdate(UnifiedVolcano volcano)
//        {
//            // Forcer mise à jour mesh
//            terrainManager.ForceImmediateMeshUpdate($"VolcanoVisual_{volcano.coordinates.x}_{volcano.coordinates.y}");

//            // Attendre mise à jour
//            while (terrainManager.IsMeshUpdating)
//            {
//                yield return new WaitForSeconds(0.1f);
//            }

//            // Calculer position finale sur surface déformée
//            float heightValue = terrainManager.GetComposedHeightAt(volcano.coordinates.x, volcano.coordinates.y);
//            Vector3 direction = volcano.worldPosition.normalized;
//            float realRadius = planetGenerator.PlanetRadius + (heightValue * planetGenerator.HeightMultiplier);
//            Vector3 finalPosition = direction * realRadius;

//            // Mettre à jour position volcan
//            volcano.worldPosition = finalPosition;

//            LogDebug($"🎯 Position finale volcan:");
//            LogDebug($"   Coordonnées: ({volcano.coordinates.x}, {volcano.coordinates.y})");
//            LogDebug($"   Hauteur HeightMap: {heightValue:F6}");
//            LogDebug($"   Position finale: {finalPosition}");

//            // Créer visuel
//            CreateVolcanoVisual(volcano);
//        }





//        // === DÉFORMATION TERRAIN ===
//        private void ApplyTerrainDeformationViaTerrainManager(UnifiedVolcano volcano)
//        {
//            Vector2Int centerCoords = WorldToMapCoordinates(volcano.worldPosition);

//            float effectiveRadius = baseDeformationRadius * volcano.EffectiveDeformationRadius;
//            float effectiveStrength = baseDeformationStrength * volcano.EffectiveDeformationStrength;

//            int radiusInt = Mathf.RoundToInt(effectiveRadius * mapResolution / planetGenerator.PlanetRadius);

//            volcano.affectedTerrainCells.Clear();
//            int affectedCells = 0;

//            for (int x = centerCoords.x - radiusInt; x <= centerCoords.x + radiusInt; x++)
//            {
//                for (int y = centerCoords.y - radiusInt; y <= centerCoords.y + radiusInt; y++)
//                {
//                    if (!IsValidCoordinate(x, y)) continue;

//                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerCoords.x, centerCoords.y));
//                    if (distance <= radiusInt)
//                    {
//                        Vector2Int cellCoord = new Vector2Int(x, y);
//                        float falloff = deformationFalloff.Evaluate(distance / radiusInt);
//                        float deformation = effectiveStrength * volcano.intensity * falloff;

//                        volcanicModifications[x, y] += deformation;
//                        volcano.affectedTerrainCells.Add(cellCoord);
//                        affectedCells++;
//                    }
//                }
//            }

//            terrainManager.RegisterModificationLayer(
//                TerrainModificationManager.VOLCANIC_LAYER,
//                volcanicModifications,
//                $"VolcanicDeformation_{volcano.type}"
//            );

//            LogDebug($"🏔️ Volcan {volcano.type} appliqué: {affectedCells} cellules");
//        }

//        // === SYSTÈME DIFFÉRÉ ===
//        private void ScheduleDeferredVisualsCreation(UnifiedVolcano volcano)
//        {
//            LogDebug($"⏳ Programmation création visuelle différée pour {volcano.type}");
//            pendingVisualsQueue.Enqueue(volcano);

//            if (!isDeferredPositioningActive)
//            {
//                StartCoroutine(ProcessDeferredVisualsCreation());
//            }
//        }

//        private System.Collections.IEnumerator ProcessDeferredVisualsCreation()
//        {
//            isDeferredPositioningActive = true;

//            while (pendingVisualsQueue.Count > 0)
//            {
//                UnifiedVolcano volcano = pendingVisualsQueue.Dequeue();
//                yield return StartCoroutine(WaitForTerrainModificationComplete(volcano));
//                CreateVolcanoVisualWithCorrectPosition(volcano);
//            }

//            isDeferredPositioningActive = false;
//        }

//        private System.Collections.IEnumerator WaitForTerrainModificationComplete(UnifiedVolcano volcano)
//        {
//            float startTime = Time.time;
//            int stableChecks = 0;
//            const int requiredStableChecks = 3;

//            while (Time.time - startTime < maxWaitTimeForTerrain)
//            {
//                yield return new WaitForSeconds(positionCheckInterval);

//                if (terrainManager.IsMeshUpdating || terrainManager.HasPendingMeshUpdate)
//                {
//                    stableChecks = 0;
//                    continue;
//                }

//                stableChecks++;
//                if (stableChecks >= requiredStableChecks)
//                {
//                    yield break;
//                }
//            }
//        }

//        private void CreateVolcanoVisualWithCorrectPosition(UnifiedVolcano volcano)
//        {
//            LogDebug($"🎨 Création visuel pour {volcano.type}");

//            // ✅ CRÉER VISUEL D'ABORD avec position originale
//            CreateVolcanoVisual(volcano);

//            // ✅ PUIS ajuster position du visuel créé
//            AdjustVolcanoHeightOnDeformedTerrain(volcano);

//            // ✅ METTRE À JOUR position du visuel
//            if (volcano.visualObject != null)
//            {
//                volcano.visualObject.transform.position = volcano.worldPosition;

//                // Réorienter
//                Vector3 upDirection = volcano.worldPosition.normalized;
//                volcano.visualObject.transform.up = upDirection;
//            }

//            LogDebug($"✅ Visuel créé - Position finale: {volcano.worldPosition}");
//        }

//        private Vector3 GetRealSurfacePosition(Vector3 approximatePosition)
//        {
//            // ✅ MÉTHODE SIMPLE ET PRÉCISE : Utiliser directement la HeightMap
//            // Au lieu de chercher dans le mesh, utiliser la HeightMap qui EST la vérité

//            Vector3 direction = approximatePosition.normalized;
//            Vector2Int coords = WorldToMapCoordinates(approximatePosition);

//            // Obtenir la hauteur réelle depuis TerrainManager (source de vérité)
//            float heightValue = terrainManager.GetComposedHeightAt(coords.x, coords.y);

//            // Calculer position exacte
//            float realRadius = planetGenerator.PlanetRadius + (heightValue * planetGenerator.HeightMultiplier);
//            Vector3 realPosition = direction * realRadius;

//            LogDebug($"🎯 Position précise calculée:");
//            LogDebug($"   Direction: {direction}");
//            LogDebug($"   Coords HeightMap: ({coords.x}, {coords.y})");
//            LogDebug($"   Hauteur HeightMap: {heightValue:F6}");
//            LogDebug($"   Rayon final: {realRadius:F3}");
//            LogDebug($"   Position finale: {realPosition}");

//            return realPosition;
//        }

     

//        private void AdjustVolcanoHeightOnDeformedTerrain(UnifiedVolcano volcano)
//        {
//            if (terrainManager == null || !terrainManager.IsInitialized)
//            {
//                LogDebug("❌ TerrainModificationManager non disponible");
//                return;
//            }

//            // Garder la direction originale (position horizontale fixe)
//            Vector3 originalDirection = volcano.worldPosition.normalized;

//            // Convertir position vers coordonnées HeightMap
//            Vector2Int volcanoMapCoords = WorldToMapCoordinates(volcano.worldPosition);

//            LogDebug($"🔍 Ajustement hauteur volcan {volcano.type}:");
//            LogDebug($"   Position originale: {volcano.worldPosition}");
//            LogDebug($"   Direction originale: {originalDirection}");
//            LogDebug($"   Coordonnées HeightMap: ({volcanoMapCoords.x}, {volcanoMapCoords.y})");

//            // Obtenir hauteur du terrain déformé à ces coordonnées
//            float composedHeight = terrainManager.GetComposedHeightAt(volcanoMapCoords.x, volcanoMapCoords.y);

//            // Calculer nouveau rayon (hauteur ajustée)
//            float newRadius = planetGenerator.PlanetRadius + (composedHeight * planetGenerator.HeightMultiplier);

//            // ✅ CRUCIAL : Position finale = Direction ORIGINALE × Nouveau rayon
//            Vector3 adjustedPosition = originalDirection * newRadius;

//            Vector3 oldPosition = volcano.worldPosition;
//            volcano.worldPosition = adjustedPosition;

//            float heightAdjustment = Vector3.Distance(oldPosition, adjustedPosition);

//            LogDebug($"   Hauteur terrain: {composedHeight:F6}");
//            LogDebug($"   Nouveau rayon: {newRadius:F3}");
//            LogDebug($"   Position ajustée: {adjustedPosition}");
//            LogDebug($"   Ajustement hauteur: {heightAdjustment:F3} unités");

//            // Vérifier que direction n'a pas changé
//            Vector3 newDirection = adjustedPosition.normalized;
//            float angularError = Vector3.Angle(originalDirection, newDirection);

//            if (angularError < 0.01f)
//            {
//                LogDebug($"✅ Direction préservée (erreur: {angularError:F4}°)");
//            }
//            else
//            {
//                LogDebug($"⚠️ Dérive direction détectée: {angularError:F3}°");
//            }
//        }

//        [ContextMenu("Vérifier Positions Volcans vs Déformations")]
//        public void VerifyVolcanoPositionsVsDeformations()
//        {
//            LogDebug("🔍 === VÉRIFICATION POSITIONS VOLCANS VS DÉFORMATIONS ===");

//            foreach (var volcano in volcanoes)
//            {
//                // Position volcan visuel
//                Vector3 visualPos = volcano.visualObject?.transform.position ?? volcano.worldPosition;

//                // Coordonnées HeightMap du volcan
//                Vector2Int volcanoCoords = WorldToMapCoordinates(volcano.worldPosition);

//                // Valeur déformation à cette position
//                float heightMapValue = terrainManager.GetComposedHeightAt(volcanoCoords.x, volcanoCoords.y);

//                // Position attendue selon mesh déformé
//                Vector3 expectedPos = GetRealSurfacePosition(volcano.worldPosition);

//                float distance = Vector3.Distance(visualPos, expectedPos);

//                LogDebug($"🌋 VOLCAN {volcano.type}:");
//                LogDebug($"   Coords HeightMap: ({volcanoCoords.x}, {volcanoCoords.y})");
//                LogDebug($"   Valeur déformation: {heightMapValue:F6}");
//                LogDebug($"   Position visuelle: {visualPos}");
//                LogDebug($"   Position attendue: {expectedPos}");
//                LogDebug($"   Distance: {distance:F3}");

//                if (distance < 0.5f)
//                {
//                    LogDebug($"   ✅ CORRECTEMENT POSITIONNÉ");
//                }
//                else
//                {
//                    LogDebug($"   ❌ MAL POSITIONNÉ - Correction nécessaire");
//                }
//            }

//            LogDebug("🔍 === FIN VÉRIFICATION ===");
//        }

//        // === CORRECTION IMMÉDIATE POUR VOLCANS EXISTANTS ===
//        [ContextMenu("Corriger Positions Volcans Existants")]
//        public void FixExistingVolcanoPositions()
//        {
//            LogDebug("🔧 === CORRECTION POSITIONS VOLCANS EXISTANTS ===");

//            int volcansCorrigés = 0;

//            foreach (var volcano in volcanoes)
//            {
//                if (volcano.visualObject != null)
//                {
//                    Vector3 oldPosition = volcano.visualObject.transform.position;
//                    Vector3 newPosition = GetRealSurfacePosition(volcano.worldPosition);

//                    // Mettre à jour position visuelle
//                    volcano.visualObject.transform.position = newPosition;
//                    volcano.worldPosition = newPosition;

//                    // Réorienter
//                    Vector3 upDirection = newPosition.normalized;
//                    volcano.visualObject.transform.up = upDirection;

//                    float correction = Vector3.Distance(oldPosition, newPosition);
//                    LogDebug($"🌋 {volcano.type} corrigé - Déplacement: {correction:F3}");

//                    volcansCorrigés++;
//                }
//            }

//            LogDebug($"✅ {volcansCorrigés} volcans repositionnés correctement");
//        }

//        [ContextMenu("Vérifier Alignement Volcans-Déformations")]
//        public void VerifyVolcanoDeformationAlignment()
//        {
//            LogDebug("🔍 === VÉRIFICATION ALIGNEMENT VOLCANS-DÉFORMATIONS ===");

//            foreach (var volcano in volcanoes)
//            {
//                Vector3 volcanoPos = volcano.worldPosition;
//                Vector2Int volcanoCoords = WorldToMapCoordinates(volcanoPos);

//                LogDebug($"🌋 VOLCAN {volcano.type}:");
//                LogDebug($"   Position 3D: {volcanoPos}");
//                LogDebug($"   Coords HeightMap: ({volcanoCoords.x}, {volcanoCoords.y})");

//                // Vérifier si ces coordonnées correspondent aux cellules affectées
//                bool isInAffectedCells = volcano.affectedTerrainCells.Contains(volcanoCoords);

//                if (isInAffectedCells)
//                {
//                    LogDebug($"   ✅ ALIGNÉ - Volcan au centre de sa déformation");
//                }
//                else
//                {
//                    // Trouver la cellule affectée la plus proche
//                    Vector2Int closestCell = Vector2Int.zero;
//                    float minDistance = float.MaxValue;

//                    foreach (var cell in volcano.affectedTerrainCells)
//                    {
//                        float distance = Vector2.Distance(volcanoCoords, cell);
//                        if (distance < minDistance)
//                        {
//                            minDistance = distance;
//                            closestCell = cell;
//                        }
//                    }

//                    LogDebug($"   ❌ DÉCALÉ - Cellule la plus proche: ({closestCell.x}, {closestCell.y}) | Distance: {minDistance:F1}");
//                }

//                // Calculer centre de masse des cellules affectées pour diagnostic
//                Vector2 centerOfMass = Vector2.zero;
//                foreach (var cell in volcano.affectedTerrainCells)
//                {
//                    centerOfMass += new Vector2(cell.x, cell.y);
//                }
//                centerOfMass /= volcano.affectedTerrainCells.Count;

//                float centerDistance = Vector2.Distance(volcanoCoords, centerOfMass);
//                LogDebug($"   Centre déformation: ({centerOfMass.x:F1}, {centerOfMass.y:F1}) | Distance: {centerDistance:F2}");
//            }

//            LogDebug("🔍 === FIN VÉRIFICATION ===");
//        }

//        // === MÉTHODE DE CORRECTION : Réaligner tous les volcans ===
//        [ContextMenu("Réaligner Tous Les Volcans")]
//        public void RealignAllVolcanoes()
//        {
//            LogDebug("🔧 === RÉALIGNEMENT TOUS LES VOLCANS ===");

//            int volcanoesRealigned = 0;

//            foreach (var volcano in volcanoes)
//            {
//                Vector3 oldPosition = volcano.worldPosition;

//                // Appliquer ajustement hauteur seulement
//                AdjustVolcanoHeightOnDeformedTerrain(volcano);

//                // Mettre à jour visuel si existe
//                if (volcano.visualObject != null)
//                {
//                    volcano.visualObject.transform.position = volcano.worldPosition;

//                    // Réorienter selon la nouvelle position
//                    Vector3 upDirection = volcano.worldPosition.normalized;
//                    volcano.visualObject.transform.up = upDirection;
//                }

//                float adjustment = Vector3.Distance(oldPosition, volcano.worldPosition);
//                LogDebug($"🌋 {volcano.type} réaligné - Ajustement: {adjustment:F3}");

//                volcanoesRealigned++;
//            }

//            LogDebug($"✅ {volcanoesRealigned} volcans réalignés");
//        }

//        // === MÉTHODE DE DIAGNOSTIC : Analyser précision coordonnées ===
//        [ContextMenu("Diagnostiquer Précision Coordonnées")]
//        public void DiagnoseCoordinatePrecision()
//        {
//            LogDebug("🔍 === DIAGNOSTIC PRÉCISION COORDONNÉES ===");

//            // Test avec positions connues
//            Vector3[] testPositions = {
//        new Vector3(planetGenerator.PlanetRadius, 0, 0),      // X+ axis
//        new Vector3(0, planetGenerator.PlanetRadius, 0),      // Y+ axis  
//        new Vector3(0, 0, planetGenerator.PlanetRadius),      // Z+ axis
//        new Vector3(-planetGenerator.PlanetRadius, 0, 0),     // X- axis
//    };

//            foreach (var testPos in testPositions)
//            {
//                Vector2Int coords = WorldToMapCoordinates(testPos);
//                Vector3 reconstructed = CalculateWorldPositionFromHeightMapValue(testPos, 0f);

//                float error = Vector3.Distance(testPos, reconstructed);

//                LogDebug($"Test: {testPos} → ({coords.x},{coords.y}) → {reconstructed}");
//                LogDebug($"   Erreur: {error:F4}");
//            }
//        }


//        [ContextMenu("DIAGNOSTIC GLOBAL - HeightMap vs Mesh vs Volcans")]
//        public void GlobalCoherenceDiagnostic()
//        {
//            LogDebug("🔍 === DIAGNOSTIC GLOBAL COHÉRENCE SYSTÈME ===");

//            if (volcanoes.Count == 0)
//            {
//                LogDebug("❌ Aucun volcan pour diagnostic");
//                return;
//            }

//            LogDebug($"📊 Analysing {volcanoes.Count} volcans...");

//            foreach (var volcano in volcanoes)
//            {
//                DiagnoseVolcanoCoherence(volcano);
//                LogDebug("---");
//            }

//            LogDebug("🔍 === FIN DIAGNOSTIC GLOBAL ===");
//        }

//        private void DiagnoseVolcanoCoherence(UnifiedVolcano volcano)
//        {
//            LogDebug($"🌋 DIAGNOSTIC VOLCAN {volcano.type}:");
//            LogDebug($"   Position 3D: {volcano.worldPosition}");

//            // === TEST 1: CONVERSION COORDINATES MULTIPLE MÉTHODES ===
//            Vector3 volcanoPos = volcano.worldPosition;
//            Vector3 direction = volcanoPos.normalized;

//            // Méthode 1: UnifiedVolcanicSystem
//            Vector2Int coords1 = WorldToMapCoordinates(volcanoPos);

//            // Méthode 2: PlanetGenerator style
//            Vector2Int coords2 = ConvertLikePlanetGenerator(direction);

//            // Méthode 3: TerrainModificationManager style  
//            Vector2Int coords3 = ConvertLikeTerrainManager(direction);

//            LogDebug($"   🔄 CONVERSIONS COORDONNÉES:");
//            LogDebug($"      UnifiedVolcanic: ({coords1.x}, {coords1.y})");
//            LogDebug($"      PlanetGenerator: ({coords2.x}, {coords2.y})");
//            LogDebug($"      TerrainManager:  ({coords3.x}, {coords3.y})");

//            // Calculer écarts
//            Vector2Int diff12 = coords2 - coords1;
//            Vector2Int diff13 = coords3 - coords1;
//            Vector2Int diff23 = coords3 - coords2;

//            LogDebug($"      Écart 1-2: ({diff12.x}, {diff12.y})");
//            LogDebug($"      Écart 1-3: ({diff13.x}, {diff13.y})");
//            LogDebug($"      Écart 2-3: ({diff23.x}, {diff23.y})");

//            // === TEST 2: HAUTEUR HEIGHTMAP VS MESH ===
//            float heightMapValue = terrainManager.GetComposedHeightAt(coords1.x, coords1.y);

//            // Calculer hauteur attendue du mesh à cette position
//            float expectedMeshRadius = planetGenerator.PlanetRadius + (heightMapValue * planetGenerator.HeightMultiplier);
//            Vector3 expectedMeshPos = direction * expectedMeshRadius;

//            // Obtenir hauteur réelle du mesh via raycast
//            float actualMeshRadius = GetActualMeshRadiusAtDirection(direction);

//            LogDebug($"   📏 HAUTEURS:");
//            LogDebug($"      HeightMap valeur: {heightMapValue:F6}");
//            LogDebug($"      Rayon attendu mesh: {expectedMeshRadius:F3}");
//            LogDebug($"      Rayon réel mesh: {actualMeshRadius:F3}");
//            LogDebug($"      Écart mesh: {Mathf.Abs(expectedMeshRadius - actualMeshRadius):F3}");

//            // === TEST 3: CENTRE DÉFORMATION ===
//            Vector2 centerDeformation = CalculateCenterOfDeformation(volcano);
//            Vector2 volcanoCoords2D = new Vector2(coords1.x, coords1.y);

//            float distanceToCenter = Vector2.Distance(volcanoCoords2D, centerDeformation);

//            LogDebug($"   🎯 DÉFORMATION:");
//            LogDebug($"      Centre déformation: ({centerDeformation.x:F1}, {centerDeformation.y:F1})");
//            LogDebug($"      Coords volcan: ({volcanoCoords2D.x}, {volcanoCoords2D.y})");
//            LogDebug($"      Distance au centre: {distanceToCenter:F2}");

//            // === VERDICT ===
//            bool coordsConsistent = (diff12.magnitude <= 1 && diff13.magnitude <= 1);
//            bool meshConsistent = Mathf.Abs(expectedMeshRadius - actualMeshRadius) < 0.1f;
//            bool deformationCentered = distanceToCenter < 2f;

//            LogDebug($"   ✅ VERDICT:");
//            LogDebug($"      Coords cohérentes: {coordsConsistent}");
//            LogDebug($"      Mesh cohérent: {meshConsistent}");
//            LogDebug($"      Déformation centrée: {deformationCentered}");

//            if (coordsConsistent && meshConsistent && deformationCentered)
//            {
//                LogDebug($"      🎉 VOLCAN PARFAITEMENT ALIGNÉ");
//            }
//            else
//            {
//                LogDebug($"      ⚠️ PROBLÈMES DÉTECTÉS");
//            }
//        }

//        // === MÉTHODES DE CONVERSION ALTERNATIVES ===
//        private Vector2Int ConvertLikePlanetGenerator(Vector3 direction)
//        {
//            float longitude = Mathf.Atan2(direction.x, direction.z);
//            float latitude = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f));

//            float u = (longitude + Mathf.PI) / (2 * Mathf.PI);
//            float v = (latitude + Mathf.PI / 2) / Mathf.PI;

//            int mapResolution = planetGenerator.Resolution;
//            int x = Mathf.Clamp(Mathf.RoundToInt(u * (mapResolution - 1)), 0, mapResolution - 1);
//            int y = Mathf.Clamp(Mathf.RoundToInt(v * (mapResolution - 1)), 0, mapResolution - 1);

//            return new Vector2Int(x, y);
//        }

//        private Vector2Int ConvertLikeTerrainManager(Vector3 direction)
//        {
//            float longitude = Mathf.Atan2(direction.x, direction.z);
//            float latitude = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f));

//            float u = (longitude + Mathf.PI) / (2 * Mathf.PI);
//            float v = (latitude + Mathf.PI / 2) / Mathf.PI;

//            int resolution = mapResolution;
//            int x = Mathf.Clamp(Mathf.RoundToInt(u * (resolution - 1)), 0, resolution - 1);
//            int y = Mathf.Clamp(Mathf.RoundToInt(v * (resolution - 1)), 0, resolution - 1);

//            return new Vector2Int(x, y);
//        }

//        // === OBTENIR RAYON RÉEL DU MESH ===
//        private float GetActualMeshRadiusAtDirection(Vector3 direction)
//        {
//            // Raycast depuis le centre vers l'extérieur
//            RaycastHit hit;
//            Ray ray = new Ray(Vector3.zero, direction);

//            if (UnityEngine.Physics.Raycast(ray, out hit, planetGenerator.PlanetRadius * 2f))
//            {
//                return hit.distance;
//            }

//            // Fallback: approximation par vertex le plus proche
//            return GetClosestVertexRadius(direction);
//        }

//        private float GetClosestVertexRadius(Vector3 direction)
//        {
//            var mesh = planetGenerator.MeshFilter.mesh;
//            if (mesh == null) return planetGenerator.PlanetRadius;

//            Vector3[] vertices = mesh.vertices;
//            float closestDistance = float.MaxValue;
//            float closestRadius = planetGenerator.PlanetRadius;

//            foreach (var vertex in vertices)
//            {
//                Vector3 worldVertex = planetGenerator.transform.TransformPoint(vertex);
//                Vector3 vertexDirection = worldVertex.normalized;

//                float angle = Vector3.Angle(direction, vertexDirection);
//                if (angle < closestDistance)
//                {
//                    closestDistance = angle;
//                    closestRadius = worldVertex.magnitude;
//                }
//            }

//            return closestRadius;
//        }

//        // === CALCULER CENTRE DE DÉFORMATION ===
//        private Vector2 CalculateCenterOfDeformation(UnifiedVolcano volcano)
//        {
//            if (volcano.affectedTerrainCells.Count == 0)
//                return Vector2.zero;

//            Vector2 center = Vector2.zero;
//            foreach (var cell in volcano.affectedTerrainCells)
//            {
//                center += new Vector2(cell.x, cell.y);
//            }
//            center /= volcano.affectedTerrainCells.Count;

//            return center;
//        }

//        // === MÉTHODE DE CORRECTION UNIFIÉE ===
//        [ContextMenu("Unifier Méthodes Conversion")]
//        public void UnifyConversionMethods()
//        {
//            LogDebug("🔧 === UNIFICATION MÉTHODES CONVERSION ===");

//            // Analyser quelle méthode donne les meilleurs résultats
//            float[] scores = new float[3];

//            foreach (var volcano in volcanoes)
//            {
//                Vector3 direction = volcano.worldPosition.normalized;

//                Vector2Int coords1 = WorldToMapCoordinates(volcano.worldPosition);
//                Vector2Int coords2 = ConvertLikePlanetGenerator(direction);
//                Vector2Int coords3 = ConvertLikeTerrainManager(direction);

//                Vector2 centerDeformation = CalculateCenterOfDeformation(volcano);

//                // Score basé sur distance au centre de déformation
//                scores[0] += Vector2.Distance(coords1, centerDeformation);
//                scores[1] += Vector2.Distance(coords2, centerDeformation);
//                scores[2] += Vector2.Distance(coords3, centerDeformation);
//            }

//            LogDebug($"📊 SCORES (plus bas = meilleur):");
//            LogDebug($"   Méthode UnifiedVolcanic: {scores[0]:F2}");
//            LogDebug($"   Méthode PlanetGenerator: {scores[1]:F2}");
//            LogDebug($"   Méthode TerrainManager: {scores[2]:F2}");

//            int bestMethod = 0;
//            float bestScore = scores[0];

//            for (int i = 1; i < 3; i++)
//            {
//                if (scores[i] < bestScore)
//                {
//                    bestScore = scores[i];
//                    bestMethod = i;
//                }
//            }

//            string[] methodNames = { "UnifiedVolcanic", "PlanetGenerator", "TerrainManager" };
//            LogDebug($"🏆 MEILLEURE MÉTHODE: {methodNames[bestMethod]} (score: {bestScore:F2})");

//            LogDebug("💡 RECOMMANDATION: Utilisez cette méthode pour toutes les conversions");
//        }

//        // === VISUALISATION HEIGHTMAP ===
//        [ContextMenu("Visualiser HeightMap Volcans")]
//        public void VisualizeVolcanoHeightMap()
//        {
//            LogDebug("🗺️ === VISUALISATION HEIGHTMAP VOLCANS ===");

//            foreach (var volcano in volcanoes)
//            {
//                Vector2Int coords = WorldToMapCoordinates(volcano.worldPosition);
//                float height = terrainManager.GetComposedHeightAt(coords.x, coords.y);

//                LogDebug($"🌋 {volcano.type} à ({coords.x}, {coords.y}): Hauteur = {height:F6}");

//                // Échantillonner zone autour du volcan
//                for (int dx = -2; dx <= 2; dx++)
//                {
//                    string line = "";
//                    for (int dy = -2; dy <= 2; dy++)
//                    {
//                        int x = coords.x + dx;
//                        int y = coords.y + dy;

//                        if (IsValidCoordinate(x, y))
//                        {
//                            float h = terrainManager.GetComposedHeightAt(x, y);
//                            line += $"{h:F2} ";
//                        }
//                        else
//                        {
//                            line += "---- ";
//                        }
//                    }
//                    LogDebug($"      {line}");
//                }
//                LogDebug("---");
//            }
//        }

//        private void CreateVolcanoVisualImmediately(UnifiedVolcano volcano)
//        {
//            terrainManager.ForceImmediateMeshUpdate($"ImmediateVolcano_{volcano.type}");
//            CreateVolcanoVisualWithCorrectPosition(volcano);
//        }

//        private void RecalculateVolcanoPositionOnDeformedTerrain(UnifiedVolcano volcano)
//        {
//            // ✅ REDIRECTION vers la nouvelle méthode d'ajustement
//            AdjustVolcanoHeightOnDeformedTerrain(volcano);
//        }

//        private Vector3 CalculateWorldPositionFromHeightMapValue(Vector3 originalPosition, float heightMapValue)
//        {
//            Vector3 direction = originalPosition.normalized;
//            float newRadius = planetGenerator.PlanetRadius + (heightMapValue * planetGenerator.HeightMultiplier);
//            return direction * newRadius;
//        }

//        // === CRÉATION VISUELLE ===
//        private void CreateVolcanoVisual(UnifiedVolcano volcano)
//        {
//            GameObject visual = null;

//            if (volcano.typeData?.prefab != null)
//            {
//                visual = Instantiate(volcano.typeData.prefab);
//            }
//            else if (fallbackVolcanoPrefab != null)
//            {
//                visual = Instantiate(fallbackVolcanoPrefab);
//            }
//            else
//            {
//                visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
//            }

//            visual.name = $"Volcano_{volcano.type}_{volcanoes.Count}";
//            visual.transform.position = volcano.worldPosition;
//            visual.transform.SetParent(transform);

//            Vector3 upDirection = volcano.worldPosition.normalized;
//            visual.transform.up = upDirection;

//            Vector3 scale = Vector3.one;
//            if (volcano.typeData != null)
//            {
//                scale = volcano.typeData.scaleRange;
//                scale += Random.Range(-0.1f, 0.1f) * Vector3.one;
//            }
//            visual.transform.localScale = scale;

//            var renderer = visual.GetComponent<MeshRenderer>();
//            if (renderer != null && lavaMaterial != null)
//            {
//                renderer.material = lavaMaterial;
//                renderer.material.color = volcano.EffectiveLavaColor;
//            }

//            volcano.visualObject = visual;
//        }

//        // === MISE À JOUR VOLCANS ===
//        private void UpdateExistingVolcanoes()
//        {
//            float currentTime = Time.time;

//            foreach (var volcano in volcanoes)
//            {
//                volcano.age += Time.deltaTime;
//                UpdateVolcanoState(volcano, currentTime);
//                UpdateVolcanoEruptions(volcano, currentTime);
//            }
//        }

//        private void UpdateVolcanoState(UnifiedVolcano volcano, float currentTime)
//        {
//            if (volcano.state == VolcanoState.Dormant)
//            {
//                float activationChance = volcano.intensity * volcano.age * 0.001f * Time.deltaTime;
//                if (Random.value < activationChance)
//                {
//                    volcano.state = VolcanoState.Active;
//                    volcano.isActive = true;
//                }
//            }
//        }

//        private void UpdateVolcanoEruptions(UnifiedVolcano volcano, float currentTime)
//        {
//            if (volcano.ShouldStartEruption(currentTime))
//            {
//                volcano.StartEruption(currentTime);
//                StartEruptionViaTerrainManager(volcano);
//            }

//            if (volcano.ShouldEndEruption(currentTime))
//            {
//                volcano.EndEruption(currentTime);
//            }
//        }

//        private void StartEruptionViaTerrainManager(UnifiedVolcano volcano)
//        {
//            float eruptionBonus = volcano.EffectiveDeformationStrength * 0.1f;

//            foreach (var cellCoord in volcano.affectedTerrainCells)
//            {
//                if (IsValidCoordinate(cellCoord.x, cellCoord.y))
//                {
//                    volcanicModifications[cellCoord.x, cellCoord.y] += eruptionBonus;
//                }
//            }

//            terrainManager.RegisterModificationLayer(
//                TerrainModificationManager.VOLCANIC_LAYER,
//                volcanicModifications,
//                $"VolcanicEruption_{volcano.type}"
//            );
//        }



//        // === UTILITAIRES ===
//        private Vector2Int WorldToMapCoordinates(Vector3 worldPos)
//        {
//            Vector3 direction = worldPos.normalized;
//            float longitude = Mathf.Atan2(direction.x, direction.z);
//            float latitude = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f));

//            float u = (longitude + Mathf.PI) / (2 * Mathf.PI);
//            float v = (latitude + Mathf.PI / 2) / Mathf.PI;

//            u = Mathf.Clamp01(u);
//            v = Mathf.Clamp01(v);

//            int x = Mathf.RoundToInt(u * (mapResolution - 1));
//            int y = Mathf.RoundToInt(v * (mapResolution - 1));

//            x = Mathf.Clamp(x, 0, mapResolution - 1);
//            y = Mathf.Clamp(y, 0, mapResolution - 1);

//            return new Vector2Int(x, y);
//        }

//        private bool IsValidCoordinate(int x, int y)
//        {
//            return x >= 0 && x < mapResolution && y >= 0 && y < mapResolution;
//        }

//        // === MÉTHODES PUBLIQUES ===
//        public void PreserveVolcanoesForPhaseChange()
//        {
//            LogDebug($"💾 Préservation de {volcanoes.Count} volcans");
//        }

//        // === DEBUG ===
//        private void LogDebug(string message)
//        {
//            if (enableDebugLogs)
//            {
//                Debug.Log($"[UnifiedVolcanic] {message}");
//            }
//        }

//        // === GETTERS ===
//        public int VolcanoCount => volcanoes.Count;
//        public List<UnifiedVolcano> Volcanoes => new List<UnifiedVolcano>(volcanoes);
//        public bool UseVolcanoTypes => useVolcanoTypes;

//        // === GIZMOS ===
//        private void OnDrawGizmos()
//        {
//            if (!showVolcanoGizmos || volcanoes == null) return;

//            foreach (var volcano in volcanoes)
//            {
//                if (volcano.isCurrentlyErupting)
//                    Gizmos.color = Color.white;
//                else if (volcano.isActive)
//                    Gizmos.color = Color.red;
//                else
//                    Gizmos.color = Color.yellow;

//                float radius = baseDeformationRadius * volcano.EffectiveDeformationRadius;
//                Gizmos.DrawWireSphere(volcano.worldPosition, radius);

//#if UNITY_EDITOR
//                UnityEditor.Handles.Label(volcano.worldPosition + Vector3.up * 0.5f, volcano.type.ToString());
//#endif
//            }
//        }
//    }
//}