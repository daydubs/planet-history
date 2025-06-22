// EarthquakeSystem.cs - Version refactorisée utilisant les types partagés
using UnityEngine;
using System.Collections.Generic;
using LifeStory.Core;
using LifeStory.Generation;

namespace LifeStory.Geology
{
    public class EarthquakeSystem : MonoBehaviour, IGeologicalEventReceiver
    {
        [Header("System References")]
        [SerializeField] private bool autoFindSystems = true;

        [Header("Earthquake Configuration")]
        [SerializeField] private bool enableEarthquakes = true;
        [SerializeField] private float earthquakeFrequency = 2f;        // Secondes entre tentatives
        [SerializeField] private float baseEarthquakeProbability = 0.3f; // Probabilité de base
        [SerializeField] private int maxEarthquakesPerFrame = 1;        // Limite performance

        [Header("Fracture Settings")]
        [SerializeField] private float minFractureDepth = 0.05f;        // Profondeur minimum
        [SerializeField] private float maxFractureDepth = 0.2f;         // Profondeur maximum
        [SerializeField] private float coastalFractureBonus = 2f;       // Bonus pour côtes
        [SerializeField] private bool enableFractureVisualization = true;

        [Header("Vulnerable Zone Detection")]
        [SerializeField] private bool detectVulnerableZones = true;
        [SerializeField] private float coastalThreshold = 3f;           // Distance max de la côte
        [SerializeField] private float stressAccumulationRate = 0.01f;  // Vitesse accumulation stress
        [SerializeField] private float stressReleaseThreshold = 0.8f;   // Seuil déclenchement

        [Header("External Event Response")]
        [SerializeField] private bool respondToExternalEvents = true;   // Réagir aux événements externes
        [SerializeField] private float externalEventMultiplier = 1.5f;  // Multiplicateur pour événements externes
        [SerializeField] private bool enableChainReactions = true;      // Permettre réactions en chaîne

        [Header("Debug & Visualization")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showVulnerableZones = true;
        [SerializeField] private bool showEarthquakeHistory = true;

        // Données système (utilise maintenant les types partagés)
        private List<VulnerableZone> vulnerableZones = new List<VulnerableZone>();
        private List<EarthquakeEvent> earthquakeHistory = new List<EarthquakeEvent>();
        private float[,] stressMap;                    // Accumulation stress par cellule
        private bool[,] fractureMap;                   // Cellules déjà fracturées

        // Références systèmes
        private SimpleTwoPlateGenerator plateGenerator;
        private PlanetGenerator planetGenerator;
        private GameManager gameManager;

        // État
        private int mapResolution;
        private bool isInitialized = false;
        private float lastEarthquakeTime = 0f;

        // Statistiques étendues
        private int totalEarthquakes = 0;
        private int successfulFractures = 0;
        private int externalTriggeredEarthquakes = 0;
        private int chainReactionEarthquakes = 0;
        private float totalContinentalAreaFractured = 0f;

        public static EarthquakeSystem Instance { get; private set; }

        // Interface IGeologicalEventReceiver
        public bool IsAffectedByGeologicalEvents => respondToExternalEvents;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LogDebug("⚡ Earthquake System initialisé (architecture modulaire)");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // S'abonner aux événements géologiques globaux
            SubscribeToGeologicalEvents();

            if (autoFindSystems)
            {
                StartCoroutine(DelayedSystemInitialization());
            }
        }

        private void SubscribeToGeologicalEvents()
        {
            // Écouter les événements externes
            GeologicalEventManager.OnEarthquakeTriggered += OnEarthquakeReceived;
            GeologicalEventManager.OnGeologicalImpact += OnGeologicalImpactReceived;

            LogDebug("📡 Abonné aux événements géologiques globaux");
        }

        private System.Collections.IEnumerator DelayedSystemInitialization()
        {
            yield return new WaitForSeconds(2f); // Attendre les autres systèmes

            // Trouver les références
            plateGenerator = SimpleTwoPlateGenerator.Instance;
            planetGenerator = PlanetGenerator.Instance;
            gameManager = GameManager.Instance;

            if (plateGenerator == null)
            {
                LogDebug("❌ SimpleTwoPlateGenerator non trouvé");
                yield break;
            }

            if (planetGenerator == null)
            {
                LogDebug("❌ PlanetGenerator non trouvé");
                yield break;
            }

            // Attendre que les plaques soient générées
            yield return new WaitUntil(() => plateGenerator.IsInitialized);

            mapResolution = planetGenerator.Resolution;
            InitializeEarthquakeSystem();

            isInitialized = true;
            LogDebug("✅ Earthquake System prêt à fracturer le supercontinent");
        }

        private void Update()
        {
            if (!isInitialized || !enableEarthquakes) return;

            // Accumulation progressive du stress
            if (detectVulnerableZones)
            {
                AccumulateStress();
            }

            // Tentative de tremblement de terre naturel
            if (Time.time - lastEarthquakeTime >= earthquakeFrequency)
            {
                TryTriggerNaturalEarthquake();
                lastEarthquakeTime = Time.time;
            }
        }

        private void InitializeEarthquakeSystem()
        {
            LogDebug("🌍 Initialisation système tremblements de terre...");

            // Initialiser les cartes
            stressMap = new float[mapResolution, mapResolution];
            fractureMap = new bool[mapResolution, mapResolution];

            // Détecter les zones vulnérables initiales
            if (detectVulnerableZones)
            {
                DetectAllVulnerableZones();
            }

            LogDebug($"✅ Système initialisé - {vulnerableZones.Count} zones vulnérables détectées");
        }

        // === INTERFACE IGeologicalEventReceiver ===
        public void OnEarthquakeReceived(EarthquakeEvent earthquake)
        {
            if (!isInitialized || !respondToExternalEvents) return;

            LogDebug($"🌍 Événement sismique externe reçu : {earthquake.source} - Magnitude {earthquake.magnitude:F1}");

            // Convertir position monde vers coordonnées de carte si nécessaire
            Vector2Int mapPosition = earthquake.epicenter;
            if (mapPosition == Vector2Int.zero && earthquake.worldPosition != Vector3.zero)
            {
                mapPosition = WorldToMapCoordinates(earthquake.worldPosition);
            }

            // Traiter l'événement externe
            ProcessExternalEarthquake(earthquake, mapPosition);
            externalTriggeredEarthquakes++;
        }

        public void OnGeologicalImpactReceived(GeologicalImpact impact)
        {
            if (!isInitialized || !respondToExternalEvents) return;

            LogDebug($"💥 Impact géologique reçu : {impact.sourceType} - Force {impact.impactForce:F1}");

            if (impact.triggersEarthquakes)
            {
                // Créer un tremblement de terre causé par l'impact
                var impactEarthquake = CreateEarthquakeFromImpact(impact);
                ProcessExternalEarthquake(impactEarthquake, impact.impactCenter);
            }
        }

        private void ProcessExternalEarthquake(EarthquakeEvent earthquake, Vector2Int mapPosition)
        {
            // Amplifier l'effet des événements externes
            var amplifiedEarthquake = earthquake;
            amplifiedEarthquake.magnitude *= externalEventMultiplier;
            amplifiedEarthquake.epicenter = mapPosition;

            // Recalculer les paramètres selon la nouvelle magnitude
            amplifiedEarthquake.fractureRadius = amplifiedEarthquake.magnitude * 2f;
            amplifiedEarthquake.fractureDepth = Mathf.Lerp(minFractureDepth, maxFractureDepth, amplifiedEarthquake.magnitude / 8f);

            // Appliquer la fracture
            bool success = ApplyFracture(amplifiedEarthquake);
            amplifiedEarthquake.successful = success;

            // Enregistrer dans l'historique
            earthquakeHistory.Add(amplifiedEarthquake);
            totalEarthquakes++;

            if (success)
            {
                successfulFractures++;

                // Réduire le stress dans la zone
                RelieveStressInArea(mapPosition, amplifiedEarthquake.fractureRadius);

                // Déclencher des réactions en chaîne si activé
                if (enableChainReactions && amplifiedEarthquake.magnitude > 4f)
                {
                    TriggerChainReaction(amplifiedEarthquake);
                }
            }

            LogDebug($"✅ Événement externe traité - {(success ? "SUCCÈS" : "ÉCHEC")} - Magnitude finale: {amplifiedEarthquake.magnitude:F1}");
        }

        private EarthquakeEvent CreateEarthquakeFromImpact(GeologicalImpact impact)
        {
            float magnitude = Mathf.Sqrt(impact.impactForce) * 0.5f; // Conversion force → magnitude
            magnitude = Mathf.Clamp(magnitude, 2f, 9f);

            return new EarthquakeEvent
            {
                epicenter = impact.impactCenter,
                source = impact.sourceType,
                type = FractureType.Impact,
                magnitude = magnitude,
                fractureRadius = impact.impactRadius,
                fractureDepth = magnitude * 0.02f,
                sourceDescription = $"Impact {impact.sourceType} (force: {impact.impactForce:F1})",
                timestamp = Time.time,
                triggersVolcanicActivity = impact.triggersVolcanicActivity,
                triggersTsunami = magnitude > 6f
            };
        }

        private void TriggerChainReaction(EarthquakeEvent sourceEarthquake)
        {
            LogDebug($"⛓️ Déclenchement réaction en chaîne depuis magnitude {sourceEarthquake.magnitude:F1}");

            // Chercher zones vulnérables proches
            List<VulnerableZone> nearbyZones = FindNearbyVulnerableZones(sourceEarthquake.epicenter, sourceEarthquake.fractureRadius * 2f);

            foreach (var zone in nearbyZones)
            {
                if (!zone.isProcessed && Random.value < 0.3f) // 30% chance de déclencher
                {
                    // Créer un séisme secondaire plus faible
                    var chainEarthquake = GeologicalEventPresets.CreateTectonicEarthquake(zone.mapPosition, zone.fractureType);
                    chainEarthquake.magnitude *= 0.7f; // Réduire magnitude
                    chainEarthquake.sourceDescription = "Réaction en chaîne";

                    // Traiter avec délai pour effet réaliste
                    StartCoroutine(DelayedChainReaction(chainEarthquake, Random.Range(1f, 3f)));
                    chainReactionEarthquakes++;
                }
            }
        }

        private System.Collections.IEnumerator DelayedChainReaction(EarthquakeEvent earthquake, float delay)
        {
            yield return new WaitForSeconds(delay);

            LogDebug($"⛓️ Réaction en chaîne déclenchée après {delay:F1}s");
            ProcessExternalEarthquake(earthquake, earthquake.epicenter);
        }

        private List<VulnerableZone> FindNearbyVulnerableZones(Vector2Int center, float radius)
        {
            List<VulnerableZone> nearbyZones = new List<VulnerableZone>();

            foreach (var zone in vulnerableZones)
            {
                float distance = Vector2.Distance(zone.mapPosition, center);
                if (distance <= radius)
                {
                    nearbyZones.Add(zone);
                }
            }

            return nearbyZones;
        }

        private Vector2Int WorldToMapCoordinates(Vector3 worldPosition)
        {
            // Convertir position 3D monde vers coordonnées de carte 2D
            Vector3 direction = worldPosition.normalized;

            float longitude = Mathf.Atan2(direction.x, direction.z);
            float latitude = Mathf.Asin(direction.y);

            float u = (longitude + Mathf.PI) / (2 * Mathf.PI);
            float v = (latitude + Mathf.PI / 2) / Mathf.PI;

            int x = Mathf.Clamp(Mathf.RoundToInt(u * (mapResolution - 1)), 0, mapResolution - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (mapResolution - 1)), 0, mapResolution - 1);

            return new Vector2Int(x, y);
        }

        // === DÉTECTION DES ZONES VULNÉRABLES (identique) ===
        private void DetectAllVulnerableZones()
        {
            LogDebug("🔍 Détection des zones vulnérables...");

            vulnerableZones.Clear();
            int coastalZones = 0;
            int inlandZones = 0;

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    // Seulement analyser les cellules continentales
                    if (plateGenerator.IsContinentalCell(x, y))
                    {
                        VulnerableZone zone = AnalyzeCellVulnerability(x, y);

                        if (zone.stressLevel > 0.1f) // Seuil minimum de vulnérabilité
                        {
                            vulnerableZones.Add(zone);

                            if (zone.fractureType == FractureType.Coastal)
                                coastalZones++;
                            else
                                inlandZones++;
                        }
                    }
                }
            }

            LogDebug($"✅ Zones vulnérables : {coastalZones} côtières, {inlandZones} intérieures");
        }

        private VulnerableZone AnalyzeCellVulnerability(int x, int y)
        {
            Vector2Int cellPos = new Vector2Int(x, y);

            // Calculer distance à la côte
            float coastalDistance = CalculateCoastalDistance(x, y);

            // Déterminer le type de fracture
            FractureType fractureType = DetermineFractureType(x, y, coastalDistance);

            // Calculer niveau de stress
            float stressLevel = CalculateBaseStressLevel(x, y, coastalDistance, fractureType);

            return new VulnerableZone
            {
                mapPosition = cellPos,
                fractureType = fractureType,
                stressLevel = stressLevel,
                coastalDistance = coastalDistance,
                isProcessed = false,
                lastTriggerSource = EarthquakeSource.Unknown,
                debugColor = GeologicalEventManager.GetFractureTypeColor(fractureType),
                lastEventTime = 0f,
                eventCount = 0,
                cumulativeDamage = 0f
            };
        }

        private float CalculateCoastalDistance(int x, int y)
        {
            float minDistance = float.MaxValue;

            // Chercher la côte la plus proche
            for (int dx = -10; dx <= 10; dx++)
            {
                for (int dy = -10; dy <= 10; dy++)
                {
                    int checkX = x + dx;
                    int checkY = y + dy;

                    if (checkX >= 0 && checkX < mapResolution &&
                        checkY >= 0 && checkY < mapResolution)
                    {
                        // Si c'est une cellule océanique adjacente à notre continent
                        if (plateGenerator.IsOceanicCell(checkX, checkY))
                        {
                            float distance = Mathf.Sqrt(dx * dx + dy * dy);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                            }
                        }
                    }
                }
            }

            return minDistance == float.MaxValue ? 999f : minDistance;
        }

        private FractureType DetermineFractureType(int x, int y, float coastalDistance)
        {
            if (coastalDistance <= coastalThreshold)
            {
                return FractureType.Coastal;
            }
            else if (IsInContinentCenter(x, y))
            {
                return FractureType.Rift;
            }
            else
            {
                return Random.value < 0.7f ? FractureType.Transform : FractureType.Hotspot;
            }
        }

        private bool IsInContinentCenter(int x, int y)
        {
            // Vérifier si la cellule est dans la partie centrale du continent
            int continentCenterRadius = mapResolution / 8;
            Vector2Int continentCenter = FindContinentCenter();

            float distanceFromCenter = Vector2.Distance(new Vector2(x, y), continentCenter);
            return distanceFromCenter <= continentCenterRadius;
        }

        private Vector2Int FindContinentCenter()
        {
            // Trouver le centre géométrique du continent
            int totalX = 0, totalY = 0, count = 0;

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    if (plateGenerator.IsContinentalCell(x, y))
                    {
                        totalX += x;
                        totalY += y;
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                return new Vector2Int(totalX / count, totalY / count);
            }

            return new Vector2Int(mapResolution / 2, mapResolution / 2);
        }

        private float CalculateBaseStressLevel(int x, int y, float coastalDistance, FractureType type)
        {
            float stress = 0.2f; // Stress de base

            // Bonus selon le type
            switch (type)
            {
                case FractureType.Coastal:
                    stress += 0.5f; // Les côtes sont très vulnérables
                    stress += Mathf.Max(0, (coastalThreshold - coastalDistance) / coastalThreshold * 0.3f);
                    break;

                case FractureType.Rift:
                    stress += 0.4f; // Centre du continent sous pression
                    break;

                case FractureType.Transform:
                    stress += 0.3f;
                    break;

                case FractureType.Hotspot:
                    stress += 0.2f;
                    break;
            }

            // Ajouter du bruit pour variation naturelle
            float noiseVariation = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.2f;
            stress += noiseVariation;

            return Mathf.Clamp01(stress);
        }

        // === ACCUMULATION DE STRESS (identique) ===
        private void AccumulateStress()
        {
            // Augmenter progressivement le stress dans les zones vulnérables
            for (int i = 0; i < vulnerableZones.Count; i++)
            {
                var zone = vulnerableZones[i];

                if (!zone.isProcessed)
                {
                    int x = zone.mapPosition.x;
                    int y = zone.mapPosition.y;

                    stressMap[x, y] += stressAccumulationRate * Time.deltaTime * zone.stressLevel;
                    stressMap[x, y] = Mathf.Clamp01(stressMap[x, y]);
                }
            }
        }

        // === DÉCLENCHEMENT DES TREMBLEMENTS DE TERRE NATURELS ===
        private void TryTriggerNaturalEarthquake()
        {
            if (vulnerableZones.Count == 0) return;

            // Chercher les zones avec stress élevé
            List<VulnerableZone> highStressZones = new List<VulnerableZone>();

            foreach (var zone in vulnerableZones)
            {
                if (!zone.isProcessed)
                {
                    int x = zone.mapPosition.x;
                    int y = zone.mapPosition.y;

                    if (stressMap[x, y] >= stressReleaseThreshold)
                    {
                        highStressZones.Add(zone);
                    }
                }
            }

            if (highStressZones.Count > 0 && Random.value < baseEarthquakeProbability)
            {
                // Choisir une zone au hasard
                VulnerableZone selectedZone = highStressZones[Random.Range(0, highStressZones.Count)];
                TriggerNaturalEarthquake(selectedZone);
            }
        }

        private void TriggerNaturalEarthquake(VulnerableZone zone)
        {
            LogDebug($"⚡ TREMBLEMENT DE TERRE NATUREL ! Type: {zone.fractureType} à ({zone.mapPosition.x}, {zone.mapPosition.y})");

            // Créer l'événement sismique naturel
            EarthquakeEvent earthquake = GeologicalEventPresets.CreateTectonicEarthquake(zone.mapPosition, zone.fractureType);

            // Ajouter stress accumulé à la magnitude
            float stressBonus = stressMap[zone.mapPosition.x, zone.mapPosition.y] * 2f;
            earthquake.magnitude += stressBonus;

            // Traiter comme événement normal
            ProcessExternalEarthquake(earthquake, zone.mapPosition);
        }

        // === APPLICATION DES FRACTURES (identique mais utilise les types partagés) ===
        private bool ApplyFracture(EarthquakeEvent earthquake)
        {
            LogDebug($"🔨 Application fracture - Magnitude: {earthquake.magnitude:F1}, Rayon: {earthquake.fractureRadius:F1}");

            var heightMap = planetGenerator.HeightMap;
            int cellsModified = 0;
            float totalFractureArea = 0f;

            int centerX = earthquake.epicenter.x;
            int centerY = earthquake.epicenter.y;
            int radius = Mathf.RoundToInt(earthquake.fractureRadius);

            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (x >= 0 && x < mapResolution && y >= 0 && y < mapResolution)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));

                        if (distance <= earthquake.fractureRadius && plateGenerator.IsContinentalCell(x, y))
                        {
                            // ✅ NOUVELLE LOGIQUE SÉCURISÉE
                            float currentHeight = heightMap[x, y];

                            // Calculer l'effet de la fracture selon la distance
                            float falloff = 1f - (distance / earthquake.fractureRadius);
                            float rawFractureEffect = earthquake.fractureDepth * falloff;

                            // 🛡️ PROTECTION CONTRE LES VALEURS NÉGATIVES
                            float newHeight = ApplySafeFracture(currentHeight, rawFractureEffect);

                            // Appliquer la nouvelle hauteur
                            heightMap[x, y] = newHeight;

                            // Marquer comme fracturé
                            fractureMap[x, y] = true;

                            cellsModified++;
                            totalFractureArea += (currentHeight - newHeight); // Delta réel appliqué
                        }
                    }
                }
            }

            // Mettre à jour le mesh de la planète
            if (cellsModified > 0)
            {
                UpdatePlanetMesh();
                totalContinentalAreaFractured += totalFractureArea;

                LogDebug($"✅ {cellsModified} cellules fracturées, profondeur totale: {totalFractureArea:F4}");
                return true;
            }

            return false;
        }

        // 🛡️ NOUVELLE MÉTHODE : Protection intelligente contre les valeurs négatives
        private float ApplySafeFracture(float currentHeight, float requestedFractureDepth)
        {
            // Stratégie : Garder un minimum de terrain, éviter complètement le négatif
            float minimumTerrainHeight = 0.05f; // 5% de hauteur minimum

            // Calculer la fracture maximum possible
            float maxAllowedFracture = currentHeight - minimumTerrainHeight;

            // Limiter la fracture à ce qui est sûr
            float safeFractureDepth = Mathf.Min(requestedFractureDepth, maxAllowedFracture);

            // S'assurer qu'on ne va pas en négatif
            safeFractureDepth = Mathf.Max(0f, safeFractureDepth);

            // Calculer la nouvelle hauteur
            float newHeight = currentHeight - safeFractureDepth;

            // Double vérification : s'assurer qu'on reste positif
            newHeight = Mathf.Max(minimumTerrainHeight, newHeight);

            // 🔍 DEBUG : Signaler les cas où on a dû limiter la fracture
            if (safeFractureDepth < requestedFractureDepth * 0.8f)
            {
                LogDebug($"⚠️ Fracture limitée: demandée {requestedFractureDepth:F3}, appliquée {safeFractureDepth:F3} (terrain trop bas)");
            }

            return newHeight;
        }

        private void MarkZoneAsProcessed(VulnerableZone zone)
        {
            for (int i = 0; i < vulnerableZones.Count; i++)
            {
                if (vulnerableZones[i].mapPosition == zone.mapPosition)
                {
                    var updatedZone = vulnerableZones[i];
                    updatedZone.isProcessed = true;
                    updatedZone.lastEventTime = Time.time;
                    updatedZone.eventCount++;
                    vulnerableZones[i] = updatedZone;
                    break;
                }
            }
        }

        private void RelieveStressInArea(Vector2Int center, float radius)
        {
            int radiusInt = Mathf.RoundToInt(radius);

            for (int x = center.x - radiusInt; x <= center.x + radiusInt; x++)
            {
                for (int y = center.y - radiusInt; y <= center.y + radiusInt; y++)
                {
                    if (x >= 0 && x < mapResolution && y >= 0 && y < mapResolution)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), center);
                        if (distance <= radius)
                        {
                            float relief = 1f - (distance / radius);
                            stressMap[x, y] *= (1f - relief * 0.8f); // Réduire le stress
                        }
                    }
                }
            }
        }

        private void UpdatePlanetMesh()
        {
            if (planetGenerator != null)
            {
                planetGenerator.MarkVolcanicModificationsPresent();
                planetGenerator.UpdatePlanetMesh();
            }
        }

        // === MÉTHODES PUBLIQUES ÉTENDUES ===
        [ContextMenu("Detect Vulnerable Zones")]
        public void ForceDetectVulnerableZones()
        {
            if (isInitialized)
            {
                DetectAllVulnerableZones();
            }
        }

        [ContextMenu("Trigger Test Earthquake")]
        public void TriggerTestEarthquake()
        {
            if (vulnerableZones.Count > 0)
            {
                var testZone = vulnerableZones[Random.Range(0, vulnerableZones.Count)];
                TriggerNaturalEarthquake(testZone);
            }
        }

        [ContextMenu("Test External Meteor Impact")]
        public void TestMeteorImpact()
        {
            // Simuler un impact de météore pour tester les événements externes
            Vector3 randomWorldPos = Random.onUnitSphere * planetGenerator.PlanetRadius;

            var meteorImpact = GeologicalEventPresets.CreateMeteorImpact(randomWorldPos, 1.5f);
            meteorImpact.impactCenter = WorldToMapCoordinates(randomWorldPos);

            LogDebug("☄️ TEST: Impact de météore simulé");
            GeologicalEventManager.RegisterGeologicalImpact(meteorImpact);
        }

        [ContextMenu("Show System Statistics")]
        public void ShowSystemStatistics()
        {
            LogDebug("📊 STATISTIQUES EARTHQUAKE SYSTEM (MODULAIRE):");
            LogDebug($"  Zones vulnérables: {vulnerableZones.Count}");
            LogDebug($"  Tremblements totaux: {totalEarthquakes}");
            LogDebug($"  - Naturels: {totalEarthquakes - externalTriggeredEarthquakes}");
            LogDebug($"  - Externes: {externalTriggeredEarthquakes}");
            LogDebug($"  - Réactions chaîne: {chainReactionEarthquakes}");
            LogDebug($"  Fractures réussies: {successfulFractures}");
            LogDebug($"  Aire continentale fracturée: {totalContinentalAreaFractured:F4}");

            if (totalEarthquakes > 0)
            {
                float successRate = (float)successfulFractures / totalEarthquakes * 100f;
                LogDebug($"  Taux de réussite: {successRate:F1}%");
            }
        }

        [ContextMenu("Show Fracture Safety Stats")]
        public void ShowFractureSafetyStats()
        {
            if (!isInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            LogDebug("📊 STATISTIQUES SÉCURITÉ DES FRACTURES:");

            var heightMap = planetGenerator.HeightMap;
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            int negativeValues = 0;
            int nearZeroValues = 0;
            int totalContinentalCells = 0;

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    if (plateGenerator.IsContinentalCell(x, y))
                    {
                        totalContinentalCells++;
                        float height = heightMap[x, y];

                        if (height < minHeight) minHeight = height;
                        if (height > maxHeight) maxHeight = height;

                        if (height < 0f) negativeValues++;
                        if (height < 0.05f) nearZeroValues++;
                    }
                }
            }

            LogDebug($"  Plage continentale: {minHeight:F3} → {maxHeight:F3}");
            LogDebug($"  Valeurs négatives: {negativeValues} sur {totalContinentalCells} ({(float)negativeValues / totalContinentalCells * 100f:F1}%)");
            LogDebug($"  Valeurs près de zéro: {nearZeroValues} sur {totalContinentalCells} ({(float)nearZeroValues / totalContinentalCells * 100f:F1}%)");

            if (negativeValues == 0)
            {
                LogDebug("✅ Aucune valeur négative détectée - Protection efficace !");
            }
            else
            {
                LogDebug("⚠️ Des valeurs négatives persistent - ajuster minimumTerrainHeight");
            }
        }

        // === GETTERS ÉTENDUS ===
        public List<VulnerableZone> VulnerableZones => vulnerableZones;
        public List<EarthquakeEvent> EarthquakeHistory => earthquakeHistory;
        public bool IsInitialized => isInitialized;
        public int TotalEarthquakes => totalEarthquakes;
        public int SuccessfulFractures => successfulFractures;
        public int ExternalTriggeredEarthquakes => externalTriggeredEarthquakes;
        public int ChainReactionEarthquakes => chainReactionEarthquakes;

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[EarthquakeSystem] {message}");
            }
        }

        // === CLEANUP ===
        private void OnDestroy()
        {
            // Se désabonner des événements
            GeologicalEventManager.OnEarthquakeTriggered -= OnEarthquakeReceived;
            GeologicalEventManager.OnGeologicalImpact -= OnGeologicalImpactReceived;
        }

        // === GUI DEBUG ÉTENDU ===
        private void OnGUI()
        {
            if (!enableDebugLogs) return;

            GUI.Box(new Rect(420, 1150, 400, 120), "");
            GUI.Label(new Rect(430, 1165, 380, 20), "=== EARTHQUAKE SYSTEM (MODULAIRE) ===");

            if (isInitialized)
            {
                GUI.Label(new Rect(430, 1185, 380, 20), $"Zones vulnérables: {vulnerableZones.Count}");

                // Statistiques détaillées
                string statsLine1 = $"Total: {totalEarthquakes} | Naturels: {totalEarthquakes - externalTriggeredEarthquakes} | Externes: {externalTriggeredEarthquakes}";
                GUI.Label(new Rect(430, 1205, 380, 20), statsLine1);

                string statsLine2 = $"Fractures: {successfulFractures} | Chaînes: {chainReactionEarthquakes}";
                GUI.Label(new Rect(430, 1225, 380, 20), statsLine2);

                string statusText = enableEarthquakes ? "✅ ACTIF" : "❌ INACTIF";
                if (respondToExternalEvents) statusText += " | 📡 ÉCOUTE";
                if (enableChainReactions) statusText += " | ⛓️ CHAÎNES";

                GUI.Label(new Rect(430, 1245, 380, 20), statusText);
            }
            else
            {
                GUI.Label(new Rect(430, 1185, 380, 20), "❌ En attente d'initialisation...");
            }

            // Boutons de test
            if (GUI.Button(new Rect(830, 1150, 80, 20), "Séisme Test"))
            {
                TriggerTestEarthquake();
            }

            if (GUI.Button(new Rect(830, 1175, 80, 20), "Météore Test"))
            {
                TestMeteorImpact();
            }

            if (GUI.Button(new Rect(830, 1200, 80, 20), "Stats"))
            {
                ShowSystemStatistics();
            }

            if (GUI.Button(new Rect(830, 1225, 80, 20), "Détecter"))
            {
                ForceDetectVulnerableZones();
            }
        }
    }
}