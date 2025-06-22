//// ContinentalRiftingSystem.cs - Système de division du supercontinent par rifting
//using UnityEngine;
//using System.Collections.Generic;
//using LifeStory.Core;
//using LifeStory.Generation;
//using LifeStory.Geology;
//using LifeStory.Terrain;
//using System.Linq;

//namespace LifeStory.Tectonics
//{
//    public class ContinentalRiftingSystem : MonoBehaviour
//    {
//        [Header("Debug Testing")]
//        [SerializeField] private bool enableDebugTrigger = true;
//        [SerializeField] private float debugTriggerDelay = 20f; // 20 secondes
//        [SerializeField] private float debugTriggerTemp = 25f;  // 25°C

//        [Header("Debug Mountain Test")]
//        [SerializeField] private bool createMountainsInsteadOfRifts = true;
//        //[SerializeField] private float debugMountainHeight = 1.5f; // ← Très haut, impossible à rater

//        [Header("Rifting Configuration")]
//        [SerializeField] private float riftingTemperatureThreshold = 1000f;
//        [SerializeField] private float continentalSeparationTemp = 200f;
//        [SerializeField] private bool enableRifting = true;

//        [Header("Stress Lines Configuration")]
//        [SerializeField] private int mainStressLines = 4;              // 3-4 lignes principales
//        [SerializeField] private int subdivisionLevels = 2;            // Niveaux de subdivision
//        [SerializeField] private float stressAngleVariation = 15f;     // Variation angulaire

//        [Header("Rift Width Settings")]
//        [SerializeField] private int riftWidth = 5; // NOUVEAU - largeur en cellules


//        [Header("Rift Evolution")]
//        [SerializeField] private float initialRiftDepth = 0.0f;       // Fissure initiale
//        [SerializeField] private float matureRiftDepth = -0.1f;        // Rift mature
//        [SerializeField] private float oceanRiftDepth = -0.2f;         // Océan complet
//        [SerializeField] private float riftProgressionRate = 0.01f;    // Vitesse approfondissement

//        [Header("Volcanic Influence")]
//        [SerializeField] private float shieldReinforcementRadius = 15f;
//        [SerializeField] private float fissureWeakeningRadius = 10f;
//        [SerializeField] private float volcanicInfluenceStrength = 0.3f;

//        [Header("Debug")]
//        [SerializeField] private bool enableDebugLogs = true;
//        [SerializeField] private bool showStressLines = false;

//        // Données du système
//        private List<StressLine> permanentRifts = new List<StressLine>();
//        private Vector2Int continentCenter;
//        private float[,] stressMap;
//        private bool isRiftingActive = false;
//        private bool systemInitialized = false;
//        private bool riftLayerNeedsUpdate = false;
//        private float lastRiftLayerUpdate = 0f;
//        private float riftLayerUpdateInterval = 0.5f; // Mise à jour max toutes les 0.5

//        private float systemStartTime = -1f; // NOUVEAU
//        private bool debugTestCompleted = false; // NOUVEAU
//        private TerrainModificationManager terrainManager;

//        // Références système
//        private PlanetGenerator planetGenerator;
//        private GameManager gameManager;
//        //private VolcanicSystem volcanicSystem;
//        private SimpleTwoPlateGenerator plateGenerator;
//        private int mapResolution;
//        private float[,] riftBackup;

//        [System.Serializable]
//        public class StressLine
//        {
//            public List<Vector2Int> points = new List<Vector2Int>();
//            public float currentDepth;
//            public float targetDepth;
//            public Vector2 direction;
//            public float age;
//            public bool isActive;
//            public RiftType type;
//        }

//        public enum RiftType
//        {
//            Initial,        // Fissure initiale
//            Propagating,    // En expansion
//            Mature,         // Rift mature
//            Ocean          // Océan formé
//        }

//        public static ContinentalRiftingSystem Instance { get; private set; }

//        private void Awake()
//        {
//            if (Instance == null)
//            {
//                Instance = this;
//                LogDebug("🌍 Continental Rifting System initialisé");
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
//            yield return new WaitForSeconds(2f);

//            // Trouver les références
//            planetGenerator = PlanetGenerator.Instance;
//            gameManager = GameManager.Instance;
//            //volcanicSystem = VolcanicSystem.Instance;
//            plateGenerator = SimpleTwoPlateGenerator.Instance;
//            terrainManager = TerrainModificationManager.Instance; // ← NOUVEAU

//            if (planetGenerator == null || gameManager == null)
//            {
//                LogDebug("❌ Systèmes requis non trouvés");
//                yield break;
//            }

//            yield return new WaitUntil(() => planetGenerator.HeightMap != null);
//            yield return new WaitUntil(() => plateGenerator?.IsInitialized ?? false);
//            yield return new WaitUntil(() => terrainManager.IsInitialized);

//            mapResolution = planetGenerator.Resolution;
//            stressMap = new float[mapResolution, mapResolution];

//            // S'abonner aux changements de température
//            if (gameManager != null)
//            {
//                GameManager.OnCoreTemperatureChanged += OnCoreTemperatureChanged;
//            }

//            systemInitialized = true;
//            systemStartTime = Time.time; // NOUVEAU - marquer le début
//            LogDebug($"✅ Système initialisé à t={systemStartTime:F1}s");
//            LogDebug($"✅ Système rifting initialisé - Résolution: {mapResolution}x{mapResolution}");

//            // Vérifier si rifting doit démarrer immédiatement
//            CheckRiftingTrigger(gameManager.PlanetTemperature);
//        }

//        private void Update()
//        {
//            if (!systemInitialized || !enableRifting) return;

//            // AJOUTER : Vérification température continue
//            if (!isRiftingActive)
//            {
//                CheckRiftingTrigger(gameManager.CoreTemperature);
//            }

//            // Progression continue des rifts (existant)
//            if (isRiftingActive && permanentRifts.Count > 0)
//            {
//                ProgressRiftEvolution();
//            }
//            UpdateRiftLayerThrottled();
//        }

//        private void UpdateRiftLayerThrottled()
//        {
//            if (!riftLayerNeedsUpdate) return;

//            float timeSinceLastUpdate = Time.time - lastRiftLayerUpdate;
//            if (timeSinceLastUpdate < riftLayerUpdateInterval) return;

//            // Appliquer la mise à jour
//            ApplyRiftProgressToHeightMap();

//            // Reset flags
//            riftLayerNeedsUpdate = false;
//            lastRiftLayerUpdate = Time.time;

//            LogDebug($"🔄 Couche rifts mise à jour (throttled après {timeSinceLastUpdate:F1}s)");
//        }

//        private void OnCoreTemperatureChanged(float newCoreTemperature)
//        {
//            LogDebug($"🔥 Température NOYAU changée: {newCoreTemperature:F0}°C");
//            CheckRiftingTrigger(newCoreTemperature);
//        }

//        private void CheckRiftingTrigger(float coreTemperature)
//        {
//            if (!enableRifting || !systemInitialized) return;

//            // MODE DEBUG (seulement si activé)
//            if (enableDebugTrigger && !debugTestCompleted && systemStartTime > 0)
//            {
//                float elapsedTime = Time.time - systemStartTime;

//                if (elapsedTime >= debugTriggerDelay && coreTemperature <= debugTriggerTemp)
//                {
//                    LogDebug($"🧪 DEBUG: DÉCLENCHEMENT RIFTING à {coreTemperature:F0}°C après {elapsedTime:F1}s");
//                    debugTestCompleted = true;
//                    InitiateRifting();
//                    return;
//                }
//            }

//            // MODE NORMAL (restauré)
//            if (!enableDebugTrigger && !isRiftingActive && coreTemperature <= riftingTemperatureThreshold)
//            {
//                LogDebug($"🔥 DÉCLENCHEMENT RIFTING NORMAL à {coreTemperature:F0}°C (seuil: {riftingTemperatureThreshold:F0}°C)");
//                InitiateRifting();
//            }
//        }

//        [ContextMenu("Initiate Rifting Now")]
//        public void InitiateRifting()
//        {
//            if (!systemInitialized)
//            {
//                LogDebug("❌ Système non initialisé");
//                return;
//            }

//            int maxRetries = 3;
//            for (int attempt = 0; attempt < maxRetries; attempt++)
//            {
//                LogDebug($"🌍 === TENTATIVE RIFTING #{attempt + 1} ===");

//                // Générer le rifting
//                if (TryGenerateRifting())
//                {
//                    LogDebug($"✅ Rifting réussi à la tentative #{attempt + 1}");
//                    isRiftingActive = true;
//                    return;
//                }

//                LogDebug($"⚠️ Tentative #{attempt + 1} échouée - retry...");
//                permanentRifts.Clear(); // Nettoyer avant retry
//            }

//            LogDebug("❌ Échec après toutes les tentatives");
//        }

//        private bool TryGenerateRifting()
//        {
//            IdentifyContinentCenter();
//            CalculateStressMap();
//            GenerateMainStressLines();

//            // Vérifier si au moins une ligne valide
//            int validLines = permanentRifts.Count(r => r.points.Count > 5);

//            if (validLines == 0)
//            {
//                return false; // Échec détecté
//            }

//            SubdivideStressLines();
//            CreateInitialRifts();
//            return true; // Succès
//        }

//        private void IdentifyContinentCenter()
//        {
//            Vector2Int center = Vector2Int.zero;
//            int continentalCells = 0;

//            // Calculer le centroïde des cellules continentales
//            for (int x = 0; x < mapResolution; x++)
//            {
//                for (int y = 0; y < mapResolution; y++)
//                {
//                    if (plateGenerator.IsContinentalCell(x, y))
//                    {
//                        center.x += x;
//                        center.y += y;
//                        continentalCells++;
//                    }
//                }
//            }

//            if (continentalCells > 0)
//            {
//                continentCenter.x = center.x / continentalCells;
//                continentCenter.y = center.y / continentalCells;

//                LogDebug($"🎯 Centre supercontinent: ({continentCenter.x},{continentCenter.y})");
//                LogDebug($"📊 Cellules continentales: {continentalCells}");
//            }
//        }

//        private void CalculateStressMap()
//        {
//            LogDebug("📈 Calcul carte de stress...");

//            // Initialiser la carte de stress
//            for (int x = 0; x < mapResolution; x++)
//            {
//                for (int y = 0; y < mapResolution; y++)
//                {
//                    if (plateGenerator.IsContinentalCell(x, y))
//                    {
//                        // Stress basé sur la distance du centre
//                        Vector2 pos = new Vector2(x, y);
//                        Vector2 center = new Vector2(continentCenter.x, continentCenter.y);
//                        float distanceFromCenter = Vector2.Distance(pos, center);

//                        // CORRECTION : Stress maximal au centre, décroissant vers les bords
//                        float normalizedDistance = distanceFromCenter / (mapResolution * 0.5f);
//                        float baseStress = 1.0f - normalizedDistance; // Inversé : 1.0 au centre, 0.0 aux bords
//                        baseStress = Mathf.Max(0.1f, baseStress); // Stress minimum de 0.1 partout
//                        stressMap[x, y] = baseStress;
//                    }
//                    else
//                    {
//                        stressMap[x, y] = 0f; // Pas de stress dans l'océan
//                    }
//                }
//            }

//            // Appliquer l'influence volcanique
//            ApplyVolcanicInfluence();
//        }

//        private void ApplyVolcanicInfluence()
//        {
            
//            //if (volcanicSystem?.Volcanoes == null) return;

//            //LogDebug("🌋 Application influence volcanique...");

//            //foreach (var volcano in volcanicSystem.Volcanoes)
//            //{
//            //    Vector2Int volcanoMapPos = WorldToMapCoordinates(volcano.worldPosition);

//            //    if (!IsValidMapCoordinate(volcanoMapPos)) continue;

//            //    float radius = volcano.type == VolcanoType.Shield ?
//            //                  shieldReinforcementRadius : fissureWeakeningRadius;

//            //    float influence = volcano.type == VolcanoType.Shield ?
//            //                    -volcanicInfluenceStrength : volcanicInfluenceStrength;

//            //    // Appliquer l'influence dans un rayon
//            //    int radiusInt = Mathf.RoundToInt(radius);
//            //    for (int x = volcanoMapPos.x - radiusInt; x <= volcanoMapPos.x + radiusInt; x++)
//            //    {
//            //        for (int y = volcanoMapPos.y - radiusInt; y <= volcanoMapPos.y + radiusInt; y++)
//            //        {
//            //            if (!IsValidMapCoordinate(x, y)) continue;
//            //            if (!plateGenerator.IsContinentalCell(x, y)) continue;

//            //            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(volcanoMapPos.x, volcanoMapPos.y));
//            //            if (distance <= radius)
//            //            {
//            //                float falloff = 1f - (distance / radius);
//            //                stressMap[x, y] += influence * falloff;
//            //                stressMap[x, y] = Mathf.Max(0f, stressMap[x, y]); // Pas de stress négatif
//            //            }
//            //        }
//            //    }
//            //}
//        }

//        private void GenerateMainStressLines()
//        {
//            LogDebug($"📏 DÉBUT Génération {mainStressLines} lignes principales...");

//            float angleStep = 360f / mainStressLines;
//            LogDebug($"📐 Angle step calculé: {angleStep}°");

//            for (int i = 0; i < mainStressLines; i++)
//            {
//                LogDebug($"🔄 Ligne {i + 1}/{mainStressLines}...");

//                float baseAngle = i * angleStep;
//                float angle = baseAngle + Random.Range(-stressAngleVariation, stressAngleVariation);

//                LogDebug($"   Angle: {angle:F1}°");

//                Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad),
//                                               Mathf.Sin(angle * Mathf.Deg2Rad));

//                LogDebug($"   Direction: {direction}");
//                LogDebug($"   Appel CreateStressLineFromCenter...");

//                StressLine mainLine = CreateStressLineFromCenter(direction, 0.3f);

//                LogDebug($"   Ligne créée avec {mainLine.points.Count} points");

//                if (mainLine.points.Count > 0)
//                {
//                    permanentRifts.Add(mainLine);
//                    LogDebug($"✅ Ligne {i + 1} ajoutée: {mainLine.points.Count} points");
//                }
//                else
//                {
//                    LogDebug($"⚠️ Ligne {i + 1} vide - non ajoutée");
//                }
//            }

//            LogDebug($"✅ FIN GenerateMainStressLines - {permanentRifts.Count} lignes totales");
//        }

//        private StressLine CreateStressLineFromCenter(Vector2 direction, float stressThreshold)
//        {
//            StressLine line = new StressLine
//            {
//                direction = direction,
//                currentDepth = 0f,
//                targetDepth = initialRiftDepth,
//                age = 0f,
//                isActive = true,
//                type = RiftType.Initial,
//                points = new List<Vector2Int>()
//            };

//            Vector2 currentPos = new Vector2(continentCenter.x, continentCenter.y);
//            Vector2 step = direction.normalized * 0.5f; // Pas plus petit pour plus de précision

//            int maxSteps = mapResolution; // Protection contre boucle infinie
//            int stepCount = 0;

//            while (stepCount < maxSteps)
//            {
//                Vector2Int mapPos = new Vector2Int(
//                    Mathf.RoundToInt(currentPos.x),
//                    Mathf.RoundToInt(currentPos.y)
//                );

//                // Conditions d'arrêt
//                if (!IsValidMapCoordinate(mapPos)) break;
//                if (!plateGenerator.IsContinentalCell(mapPos.x, mapPos.y)) break;
//                if (stressMap[mapPos.x, mapPos.y] < stressThreshold) break;

//                // Éviter les doublons
//                if (!line.points.Contains(mapPos))
//                {
//                    line.points.Add(mapPos);
//                }

//                currentPos += step;
//                stepCount++;
//            }

//            return line;
//        }

//        private void SubdivideStressLines()
//        {
//            LogDebug($"🌿 Subdivision des lignes principales ({subdivisionLevels} niveaux)...");

//            try
//            {
//                List<StressLine> subdividedLines = new List<StressLine>();

//                foreach (var mainLine in permanentRifts)
//                {
//                    // Créer des lignes secondaires perpendiculaires
//                    for (int level = 1; level <= subdivisionLevels; level++)
//                    {
//                        int pointStep = Mathf.Max(1, mainLine.points.Count / (level + 1));

//                        for (int i = pointStep; i < mainLine.points.Count; i += pointStep)
//                        {
//                            Vector2Int branchPoint = mainLine.points[i];

//                            // Direction perpendiculaire avec variation
//                            Vector2 perpDir = new Vector2(-mainLine.direction.y, mainLine.direction.x);
//                            float variation = Random.Range(-30f, 30f) * Mathf.Deg2Rad;
//                            perpDir = new Vector2(
//                                perpDir.x * Mathf.Cos(variation) - perpDir.y * Mathf.Sin(variation),
//                                perpDir.x * Mathf.Sin(variation) + perpDir.y * Mathf.Cos(variation)
//                            );

//                            float stressThreshold = 0.5f / level; // Seuil décroissant
//                            StressLine branchLine = CreateStressLineFromPoint(branchPoint, perpDir, stressThreshold);

//                            if (branchLine.points.Count > 5) // Seulement si assez longue
//                            {
//                                subdividedLines.Add(branchLine);
//                            }
//                        }
//                    }
//                }

//                permanentRifts.AddRange(subdividedLines);
//                LogDebug($"✅ {subdividedLines.Count} lignes secondaires créées");
//                LogDebug($"✅ FIN Subdivision - {subdividedLines.Count} lignes secondaires");
//            }
//            catch (System.Exception e)
//            {

//                LogDebug($"❌ ERREUR Subdivision: {e.Message}"); ;
//            }
//        }

//        private StressLine CreateStressLineFromPoint(Vector2Int startPoint, Vector2 direction, float stressThreshold)
//        {
//            StressLine line = new StressLine
//            {
//                direction = direction,
//                currentDepth = 0f,
//                targetDepth = initialRiftDepth,
//                age = 0f,
//                isActive = true,
//                type = RiftType.Initial
//            };

//            Vector2 currentPos = new Vector2(startPoint.x, startPoint.y);
//            Vector2 step = direction.normalized;

//            // Tracer depuis le point de départ
//            while (true)
//            {
//                Vector2Int mapPos = new Vector2Int(Mathf.RoundToInt(currentPos.x),
//                                                  Mathf.RoundToInt(currentPos.y));

//                if (!IsValidMapCoordinate(mapPos) || !plateGenerator.IsContinentalCell(mapPos.x, mapPos.y))
//                    break;

//                if (stressMap[mapPos.x, mapPos.y] < stressThreshold)
//                    break;

//                line.points.Add(mapPos);
//                currentPos += step;
//            }

//            return line;
//        }

//        private void CreateInitialRifts()
//        {
//            LogDebug("🔨 Création rifts via TerrainManager...");

//            // Créer une couche de rifts
//            float[,] riftLayer = new float[mapResolution, mapResolution];
//            int totalRiftCells = 0;

//            foreach (var rift in permanentRifts)
//            {
//                foreach (var point in rift.points)
//                {
//                    if (IsValidMapCoordinate(point))
//                    {
//                        // NOUVEAU : Appliquer riftWidth autour de chaque point de stress
//                        int halfWidth = riftWidth / 2;

//                        for (int dx = -halfWidth; dx <= halfWidth; dx++)
//                        {
//                            for (int dy = -halfWidth; dy <= halfWidth; dy++)
//                            {
//                                int x = point.x + dx;
//                                int y = point.y + dy;

//                                if (IsValidMapCoordinate(x, y))
//                                {
//                                    // Distance du centre du point de stress
//                                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

//                                    // Seulement si dans le rayon défini par riftWidth
//                                    if (distance <= halfWidth)
//                                    {
//                                        riftLayer[x, y] = -0.7f; // Valeur océanique forte
//                                        totalRiftCells++;
//                                    }
//                                }
//                            }
//                        }
//                    }
//                }
//            }

//            // Enregistrer la couche dans le TerrainManager
//            terrainManager.RegisterModificationLayer(TerrainModificationManager.RIFT_LAYER, riftLayer);

//            LogDebug($"✅ {totalRiftCells} rifts enregistrés dans TerrainManager");
//        }

//        private void BackupRiftPositions()
//        {
//            riftBackup = new float[mapResolution, mapResolution];
//            var heightMap = planetGenerator.HeightMap;

//            for (int x = 0; x < mapResolution; x++)
//            {
//                for (int y = 0; y < mapResolution; y++)
//                {
//                    riftBackup[x, y] = heightMap[x, y];
//                }
//            }

//            LogDebug("💾 Positions des rifts sauvegardées");
//        }

//        private System.Collections.IEnumerator ProtectRiftsFromVolcanicOverride()
//        {
//            while (isRiftingActive)
//            {
//                yield return new WaitForSeconds(0.5f); // Vérifier toutes les 0.5s

//                // Vérifier si les rifts ont été écrasés
//                if (RiftsHaveBeenOverridden())
//                {
//                    LogDebug("🔧 Rifts écrasés détectés - Restauration...");
//                    RestoreRifts();
//                }
//            }
//        }

//        private bool RiftsHaveBeenOverridden()
//        {
//            var heightMap = planetGenerator.HeightMap;

//            // Vérifier quelques points de rift
//            foreach (var rift in permanentRifts)
//            {
//                if (rift.points.Count > 0)
//                {
//                    var testPoint = rift.points[0];
//                    if (IsValidMapCoordinate(testPoint))
//                    {
//                        float currentValue = heightMap[testPoint.x, testPoint.y];
//                        if (Mathf.Abs(currentValue - 0.0f) > 0.1f) // Le rift n'est plus océanique
//                        {
//                            return true;
//                        }
//                    }
//                }
//            }
//            return false;
//        }

//        private void RestoreRifts()
//        {
//            var heightMap = planetGenerator.HeightMap;

//            foreach (var rift in permanentRifts)
//            {
//                foreach (var point in rift.points)
//                {
//                    if (IsValidMapCoordinate(point))
//                    {
//                        heightMap[point.x, point.y] = 0.0f; // Restaurer niveau océanique
//                    }
//                }
//            }

//            planetGenerator.MarkVolcanicModificationsPresent();
//            planetGenerator.UpdatePlanetMesh();
//        }

//        private void ProgressRiftEvolution()
//        {
//            float currentTemp = gameManager.SurfaceTemperature;
//            bool hasProgressed = false;

//            foreach (var rift in permanentRifts)
//            {
//                if (!rift.isActive) continue;

//                rift.age += Time.deltaTime;

//                // Calculer la profondeur cible selon la température
//                float tempProgress = Mathf.InverseLerp(riftingTemperatureThreshold, continentalSeparationTemp, currentTemp);
//                float newTargetDepth = Mathf.Lerp(initialRiftDepth, oceanRiftDepth, tempProgress);

//                if (newTargetDepth < rift.targetDepth)
//                {
//                    rift.targetDepth = newTargetDepth;

//                    // Progression graduelle vers la profondeur cible
//                    if (rift.currentDepth > rift.targetDepth)
//                    {
//                        float progressAmount = riftProgressionRate * Time.deltaTime;
//                        rift.currentDepth = Mathf.Max(rift.targetDepth, rift.currentDepth - progressAmount);
//                        hasProgressed = true;

//                        // Mettre à jour le type de rift
//                        UpdateRiftType(rift);
//                    }
//                }
//            }

//            // ✅ OPTIMISATION : Marquer pour mise à jour au lieu d'appliquer immédiatement
//            if (hasProgressed)
//            {
//                riftLayerNeedsUpdate = true;
//            }
//        }


//        private void UpdateRiftType(StressLine rift)
//        {
//            if (rift.currentDepth <= initialRiftDepth * 0.8f)
//                rift.type = RiftType.Initial;
//            else if (rift.currentDepth <= matureRiftDepth * 0.8f)
//                rift.type = RiftType.Propagating;
//            else if (rift.currentDepth <= oceanRiftDepth * 0.8f)
//                rift.type = RiftType.Mature;
//            else
//                rift.type = RiftType.Ocean;
//        }

//        private void ApplyRiftProgressToHeightMap()
//        {
//            if (terrainManager == null) return;

//            LogDebug("🔄 === DÉBUT MISE À JOUR COUCHE RIFTS ===");

//            // Créer la couche de rifts avec les profondeurs actuelles
//            float[,] riftLayer = new float[mapResolution, mapResolution];
//            int riftsApplied = 0;

//            foreach (var rift in permanentRifts)
//            {
//                foreach (var point in rift.points)
//                {
//                    if (IsValidMapCoordinate(point))
//                    {
//                        riftLayer[point.x, point.y] = rift.currentDepth;
//                        riftsApplied++;
//                    }
//                }
//            }

//            // Mettre à jour la couche
//            terrainManager.RegisterModificationLayer(
//                TerrainModificationManager.RIFT_LAYER,
//                riftLayer,
//                "RiftProgression"
//            );

//            LogDebug($"✅ Couche rifts mise à jour: {riftsApplied} cellules, {permanentRifts.Count} rifts");
//        }


//        // === MÉTHODES UTILITAIRES ===
//        private Vector2Int WorldToMapCoordinates(Vector3 worldPosition)
//        {
//            Vector3 direction = worldPosition.normalized;
//            float longitude = Mathf.Atan2(direction.x, direction.z);
//            float latitude = Mathf.Asin(direction.y);

//            float u = (longitude + Mathf.PI) / (2 * Mathf.PI);
//            float v = (latitude + Mathf.PI / 2) / Mathf.PI;

//            int x = Mathf.Clamp(Mathf.RoundToInt(u * (mapResolution - 1)), 0, mapResolution - 1);
//            int y = Mathf.Clamp(Mathf.RoundToInt(v * (mapResolution - 1)), 0, mapResolution - 1);

//            return new Vector2Int(x, y);
//        }

//        private bool IsValidMapCoordinate(Vector2Int coords)
//        {
//            return coords.x >= 0 && coords.x < mapResolution && coords.y >= 0 && coords.y < mapResolution;
//        }

//        private bool IsValidMapCoordinate(int x, int y)
//        {
//            return x >= 0 && x < mapResolution && y >= 0 && y < mapResolution;
//        }

//        // === MÉTHODES PUBLIQUES ===
//        public List<StressLine> GetPermanentRifts() => permanentRifts;
//        public bool IsRiftingActive => isRiftingActive;
//        public Vector2Int ContinentCenter => continentCenter;

//        // Méthode pour EarthquakeSystem
//        public List<Vector2Int> GetActiveRiftLines()
//        {
//            List<Vector2Int> allRiftPoints = new List<Vector2Int>();
//            foreach (var rift in permanentRifts)
//            {
//                if (rift.isActive)
//                {
//                    allRiftPoints.AddRange(rift.points);
//                }
//            }
//            return allRiftPoints;
//        }

//        // === MÉTHODES DE TEST ===
//        [ContextMenu("Show Rifting Status")]
//        public void ShowRiftingStatus()
//        {
//            LogDebug("📊 STATUT RIFTING:");
//            LogDebug($"   🌡️ Température: {gameManager?.SurfaceTemperature:F0}°C");
//            LogDebug($"   ⚡ Rifting actif: {isRiftingActive}");
//            LogDebug($"   🌍 Centre continent: {continentCenter}");
//            LogDebug($"   📏 Rifts permanents: {permanentRifts.Count}");

//            if (permanentRifts.Count > 0)
//            {
//                foreach (var rift in permanentRifts)
//                {
//                    LogDebug($"     - Type: {rift.type}, Points: {rift.points.Count}, Profondeur: {rift.currentDepth:F3}");
//                }
//            }
//        }

//        [ContextMenu("Test Stress Map")]
//        public void TestStressMap()
//        {
//            if (!systemInitialized)
//            {
//                LogDebug("❌ Système non initialisé");
//                return;
//            }

//            IdentifyContinentCenter();
//            CalculateStressMap();

//            // Analyser la carte de stress
//            float minStress = float.MaxValue, maxStress = float.MinValue;
//            int stressedCells = 0;

//            for (int x = 0; x < mapResolution; x++)
//            {
//                for (int y = 0; y < mapResolution; y++)
//                {
//                    float stress = stressMap[x, y];
//                    if (stress > 0f)
//                    {
//                        stressedCells++;
//                        if (stress < minStress) minStress = stress;
//                        if (stress > maxStress) maxStress = stress;
//                    }
//                }
//            }

//            LogDebug($"🧪 ANALYSE STRESS MAP:");
//            LogDebug($"   Cellules avec stress: {stressedCells}");
//            LogDebug($"   Stress min/max: {minStress:F3} / {maxStress:F3}");
//        }

//        // ✅ AJOUTER méthodes de contrôle pour debug
//        [ContextMenu("Force Rift Layer Update")]
//        public void ForceRiftLayerUpdate()
//        {
//            riftLayerNeedsUpdate = true;
//            lastRiftLayerUpdate = 0f; // Force immediate update
//            LogDebug("🔄 Mise à jour couche rifts forcée");
//        }

//        [ContextMenu("Show Rift Update Stats")]
//        public void ShowRiftUpdateStats()
//        {
//            LogDebug("📊 === STATISTIQUES MISE À JOUR RIFTS ===");
//            LogDebug($"   Mise à jour nécessaire: {riftLayerNeedsUpdate}");
//            LogDebug($"   Dernière mise à jour: {Time.time - lastRiftLayerUpdate:F1}s");
//            LogDebug($"   Intervalle: {riftLayerUpdateInterval:F1}s");
//            LogDebug($"   Rifts actifs: {permanentRifts.Count}");

//            int totalRiftCells = 0;
//            foreach (var rift in permanentRifts)
//            {
//                totalRiftCells += rift.points.Count;
//            }
//            LogDebug($"   Cellules rift totales: {totalRiftCells}");
//        }

//        private void LogDebug(string message)
//        {
//            if (enableDebugLogs)
//            {
//                Debug.Log($"[ContinentalRifting-CORE] {message}");
//            }
//        }

//        // === CLEANUP ===
//        private void OnDestroy()
//        {
//            if (GameManager.OnSurfaceTemperatureChanged != null)
//            {
//                GameManager.OnSurfaceTemperatureChanged -= OnCoreTemperatureChanged;
//            }
//        }


//        // ====================Test contextuel====================

//        [ContextMenu("Debug Show Rift Positions")]
//        public void DebugShowRiftPositions()
//        {
//            LogDebug($"=== DEBUG RIFTS ({permanentRifts.Count} rifts) ===");

//            var heightMap = planetGenerator.HeightMap;

//            foreach (var rift in permanentRifts)
//            {
//                LogDebug($"Rift: {rift.points.Count} points, Profondeur: {rift.currentDepth}");

//                // Vérifier quelques points du rift
//                for (int i = 0; i < Mathf.Min(3, rift.points.Count); i++)
//                {
//                    var point = rift.points[i];
//                    float heightValue = heightMap[point.x, point.y];
//                    LogDebug($"  Point [{point.x},{point.y}]: HeightMap = {heightValue:F3}");
//                }
//            }
//        }

//        [ContextMenu("Test Force Visible Rift")]
//        public void TestForceVisibleRift()
//        {
//            if (!systemInitialized) return;

//            LogDebug("🧪 TEST: Création rift forcé visible");

//            var heightMap = planetGenerator.HeightMap;

//            // Créer une ligne horizontale au centre pour test
//            int centerY = mapResolution / 2;
//            int startX = mapResolution / 4;
//            int endX = 3 * mapResolution / 4;

//            int pointsModified = 0;

//            for (int x = startX; x <= endX; x++)
//            {
//                if (plateGenerator.IsContinentalCell(x, centerY))
//                {
//                    heightMap[x, centerY] = initialRiftDepth; // -0.6f
//                    pointsModified++;
//                }
//            }

//            planetGenerator.MarkVolcanicModificationsPresent();
//            planetGenerator.UpdatePlanetMesh();

//            LogDebug($"✅ Rift test: {pointsModified} points modifiés en ligne droite");
//        }

//        [ContextMenu("Debug Rift Locations")]
//        public void DebugRiftLocations()
//        {
//            LogDebug("🔍 DIAGNOSTIC POSITIONS RIFTS");

//            foreach (var rift in permanentRifts)
//            {
//                int continentalPoints = 0;
//                int oceanicPoints = 0;

//                foreach (var point in rift.points)
//                {
//                    if (plateGenerator.IsContinentalCell(point.x, point.y))
//                        continentalPoints++;
//                    else
//                        oceanicPoints++;
//                }

//                LogDebug($"Rift: {rift.points.Count} points - Continental: {continentalPoints}, Océan: {oceanicPoints}");
//            }
//        }

//        [ContextMenu("Test Direct HeightMap Impact")]
//        public void TestDirectHeightMapImpact()
//        {
//            if (!systemInitialized) return;

//            LogDebug("🧪 TEST DIRECT: Impact HeightMap avant/après rifts");

//            var heightMap = planetGenerator.HeightMap;
//            Vector2Int testPoint = new Vector2Int(mapResolution / 2, mapResolution / 2);

//            // AVANT
//            float valueBefore = heightMap[testPoint.x, testPoint.y];
//            LogDebug($"AVANT: HeightMap[{testPoint.x},{testPoint.y}] = {valueBefore:F6}");

//            // MODIFICATION DIRECTE
//            heightMap[testPoint.x, testPoint.y] = 0.05f;

//            // VÉRIFICATION IMMÉDIATE
//            float valueAfter = heightMap[testPoint.x, testPoint.y];
//            LogDebug($"APRÈS: HeightMap[{testPoint.x},{testPoint.y}] = {valueAfter:F6}");

//            // FORCER MISE À JOUR
//            planetGenerator.MarkVolcanicModificationsPresent();
//            planetGenerator.UpdatePlanetMesh();

//            // VÉRIFICATION FINALE
//            float valueFinal = heightMap[testPoint.x, testPoint.y];
//            LogDebug($"FINAL: HeightMap[{testPoint.x},{testPoint.y}] = {valueFinal:F6}");

//            if (valueFinal != valueAfter)
//            {
//                LogDebug("❌ PROBLÈME: La HeightMap a été écrasée après UpdatePlanetMesh()");
//            }
//            else
//            {
//                LogDebug("✅ HeightMap conservée - problème ailleurs");
//            }
//        }

//        [ContextMenu("Analyze HeightMap Range")]
//        public void AnalyzeHeightMapRange()
//        {
//            if (!systemInitialized) return;

//            LogDebug("📊 ANALYSE COMPLÈTE HEIGHTMAP");

//            var heightMap = planetGenerator.HeightMap;

//            float minValue = float.MaxValue;
//            float maxValue = float.MinValue;
//            float oceanSample = 0f;
//            float continentSample = 0f;

//            int oceanCells = 0;
//            int continentCells = 0;
//            int totalCells = 0;

//            for (int x = 0; x < mapResolution; x++)
//            {
//                for (int y = 0; y < mapResolution; y++)
//                {
//                    float value = heightMap[x, y];
//                    totalCells++;

//                    // Min/Max global
//                    if (value < minValue) minValue = value;
//                    if (value > maxValue) maxValue = value;

//                    // Échantillons par type
//                    if (plateGenerator.IsContinentalCell(x, y))
//                    {
//                        continentCells++;
//                        if (continentCells == 1) continentSample = value;
//                    }
//                    else
//                    {
//                        oceanCells++;
//                        if (oceanCells == 1) oceanSample = value;
//                    }
//                }
//            }

//            float range = maxValue - minValue;

//            LogDebug($"=== RÉSULTATS ANALYSE ===");
//            LogDebug($"Valeur MIN (noir): {minValue:F6}");
//            LogDebug($"Valeur MAX (blanc): {maxValue:F6}");
//            LogDebug($"Plage totale: {range:F6}");
//            LogDebug($"Échantillon océan: {oceanSample:F6}");
//            LogDebug($"Échantillon continent: {continentSample:F6}");
//            LogDebug($"Cellules océan: {oceanCells} ({(float)oceanCells / totalCells * 100:F1}%)");
//            LogDebug($"Cellules continent: {continentCells} ({(float)continentCells / totalCells * 100:F1}%)");

//            // Recommandations
//            float suggestedRiftValue = minValue + (range * 0.1f); // 10% au-dessus du min
//            LogDebug($"💡 RIFT recommandé: {suggestedRiftValue:F6}");
//        }

//        [ContextMenu("Track HeightMap Range Changes")]
//        public void TrackHeightMapRangeChanges()
//        {
//            StartCoroutine(MonitorHeightMapRange());
//        }

//        private System.Collections.IEnumerator MonitorHeightMapRange()
//        {
//            float lastMin = 0f, lastMax = 1f;

//            while (true)
//            {
//                var heightMap = planetGenerator.HeightMap;
//                if (heightMap != null)
//                {
//                    float currentMin, currentMax;
//                    AnalyzeCurrentRange(out currentMin, out currentMax);

//                    if (Mathf.Abs(currentMin - lastMin) > 0.001f || Mathf.Abs(currentMax - lastMax) > 0.001f)
//                    {
//                        LogDebug($"⚠️ PLAGE CHANGÉE: {lastMin:F3}-{lastMax:F3} → {currentMin:F3}-{currentMax:F3}");
//                        LogDebug($"   Phase: {gameManager?.CurrentPhase}, Temp: {gameManager?.CoreTemperature:F0}°C");

//                        lastMin = currentMin;
//                        lastMax = currentMax;
//                    }
//                }

//                yield return new WaitForSeconds(1f);
//            }
//        }

//        [ContextMenu("Validate Core Temperature System")]
//        public void ValidateCoreTemperatureSystem()
//        {
//            LogDebug("🔍 === VALIDATION SYSTÈME TEMPÉRATURE NOYAU - RIFTING ===");

//            // Vérifier que le système utilise bien la température noyau
//            LogDebug($"✅ Source température: GameManager.CoreTemperature = {gameManager.CoreTemperature:F0}°C");
//            LogDebug($"ℹ️ Comparaison surface: GameManager.SurfaceTemperature = {gameManager.SurfaceTemperature:F0}°C");
//            LogDebug($"📊 Différentiel: {gameManager.CoreTemperature - gameManager.SurfaceTemperature:F0}°C");

//            // Vérifier event abonnement
//            bool coreEventConnected = GameManager.OnCoreTemperatureChanged != null;
//            LogDebug($"✅ Event OnCoreTemperatureChanged: {(coreEventConnected ? "Abonné" : "❌ Non abonné")}");

//            // Vérifier seuils
//            LogDebug($"✅ Seuil rifting NOYAU: {riftingTemperatureThreshold:F0}°C");

//            // Vérifier déclenchement
//            bool riftingPossible = gameManager.CoreTemperature <= riftingTemperatureThreshold;
//            LogDebug($"✅ Rifting possible: {(riftingPossible ? "OUI" : "NON")} (Core trop {(riftingPossible ? "froid" : "chaud")})");

//            // État actuel
//            LogDebug($"✅ État rifting: {(isRiftingActive ? "ACTIF" : "INACTIF")}");
//            LogDebug($"✅ Rifts permanents: {permanentRifts.Count}");

//            LogDebug($"📊 RÉSUMÉ: Système rifting température NOYAU {(coreEventConnected ? "✅ OPÉRATIONNEL" : "❌ À CORRIGER")}");
//        }

//        [ContextMenu("Show Core vs Surface Impact")]
//        public void ShowCoreVsSurfaceImpact()
//        {
//            LogDebug("=== IMPACT TEMPÉRATURE NOYAU vs SURFACE ===");
//            LogDebug($"🔥 Température NOYAU: {gameManager.CoreTemperature:F0}°C");
//            LogDebug($"🌡️ Température SURFACE: {gameManager.SurfaceTemperature:F0}°C");
//            LogDebug($"📊 Différentiel: {gameManager.CoreTemperature - gameManager.SurfaceTemperature:F0}°C");

//            LogDebug($"🌍 Seuil rifting (NOYAU): {riftingTemperatureThreshold:F0}°C");

//            bool riftingWithCore = gameManager.CoreTemperature <= riftingTemperatureThreshold;
//            bool riftingWithSurface = gameManager.SurfaceTemperature <= riftingTemperatureThreshold;

//            LogDebug($"🎯 Rifting avec NOYAU: {(riftingWithCore ? "✅ POSSIBLE" : "❌ Trop chaud")}");
//            LogDebug($"🎯 Rifting avec SURFACE: {(riftingWithSurface ? "✅ POSSIBLE" : "❌ Trop chaud")}");

//            if (riftingWithCore != riftingWithSurface)
//            {
//                LogDebug($"⚠️ DIFFÉRENCE: Utilisation température NOYAU vs SURFACE donne résultat différent!");
//                LogDebug($"   Recommandation: Utiliser NOYAU pour processus géologiques profonds");
//            }
//            else
//            {
//                LogDebug($"ℹ️ Cohérence: Température NOYAU et SURFACE donnent même résultat pour rifting");
//            }
//        }

//        [ContextMenu("Simulate Core Temperature Drop")]
//        public void SimulateCoreTemperatureDrop()
//        {
//            float newCoreTemp = gameManager.CoreTemperature - 200f;
//            LogDebug($"🧪 SIMULATION: Température NOYAU forcée à {newCoreTemp:F0}°C");
//            OnCoreTemperatureChanged(newCoreTemp);
//        }

//        [ContextMenu("Test Core Temperature Calibration")]
//        public void TestCoreTemperatureCalibration()
//        {
//            LogDebug("🧪 === TEST CALIBRAGE TEMPÉRATURE NOYAU - RIFTING ===");

//            // Tester différents scénarios de température noyau
//            var testTemperatures = new[]
//            {
//        ("Formation planète", 3500f),
//        ("Noyau très chaud", 3000f),
//        ("Seuil rifting", riftingTemperatureThreshold),
//        ("Rifting possible", riftingTemperatureThreshold - 200f),
//        ("Noyau refroidi", 1500f)
//    };

//            foreach (var (scenario, temp) in testTemperatures)
//            {
//                bool riftingPossible = temp <= riftingTemperatureThreshold;

//                LogDebug($"📊 {scenario} ({temp:F0}°C NOYAU):");
//                LogDebug($"   Rifting possible: {(riftingPossible ? "✅ OUI" : "❌ NON")}");
//                LogDebug($"   Marge: {temp - riftingTemperatureThreshold:+0;-0}°C");
//            }
//        }

//        private void AnalyzeCurrentRange(out float min, out float max)
//        {
//            var heightMap = planetGenerator.HeightMap;
//            min = float.MaxValue;
//            max = float.MinValue;

//            for (int x = 0; x < mapResolution; x++)
//            {
//                for (int y = 0; y < mapResolution; y++)
//                {
//                    float value = heightMap[x, y];
//                    if (value < min) min = value;
//                    if (value > max) max = value;
//                }
//            }
//        }

//        // === GUI DEBUG ===
//        private void OnGUI()
//        {
//            return; // Désactiver l'interface GUI pour le moment
//            if (!enableDebugLogs) return;

//            GUI.Box(new Rect(870, 20, 300, 180), "");
//            GUI.Label(new Rect(880, 20, 380, 20), "=== CONTINENTAL RIFTING ===");

//            if (systemInitialized && gameManager != null)
//            {
//                GUI.Label(new Rect(880, 50, 380, 20), $"Temp: {gameManager.SurfaceTemperature:F0}°C | Rifting: {(isRiftingActive ? "✅" : "❌")}");
//                GUI.Label(new Rect(880, 80, 380, 20), $"Centre: ({continentCenter.x},{continentCenter.y}) | Rifts: {permanentRifts.Count}");

//                if (permanentRifts.Count > 0)
//                {
//                    int oceanRifts = permanentRifts.FindAll(r => r.type == RiftType.Ocean).Count;
//                    GUI.Label(new Rect(880, 110, 380, 20), $"État: {oceanRifts} océans formés / {permanentRifts.Count} total");
//                }
//            }
//            else
//            {
//                GUI.Label(new Rect(880, 130, 380, 20), "❌ Système non initialisé");
//            }

//            if (GUI.Button(new Rect(880, 160, 380, 20), "Initier Rifting"))
//            {
//                InitiateRifting();
//            }

//            if (GUI.Button(new Rect(880, 190, 100, 20), "Test Stress"))
//            {
//                TestStressMap();
//            }

//            if (GUI.Button(new Rect(880, 220, 100, 20), "Statut"))
//            {
//                ShowRiftingStatus();
//            }
//        }
//    }
//}