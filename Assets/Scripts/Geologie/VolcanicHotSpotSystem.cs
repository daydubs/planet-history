// VolcanicHotSpotSystem_Optimized.cs - Version optimisée performance
using LifeStory.Core;
using LifeStory.Generation;
using LifeStory.Volcanoes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LifeStory.Volcanoes.CleanVolcanicSystem;

namespace LifeStory.Geology
{
    /// <summary>
    /// Données d'un hot-spot volcanique (optimisé)
    /// </summary>
    [System.Serializable]
    public class VolcanicHotSpot
    {
        [Header("Localisation")]
        public Vector3 worldPosition;
        public Vector2Int mapCoordinates;

        [Header("Caractéristiques Thermiques")]
        public float coreTemperature;
        public float influenceRadius;
        public float thermalIntensity;

        [Header("Données Volcaniques")]
        public VolcanoType sourceType;
        public VolcanicState currentState;
        public float activityLevel;
        public int eruptionsCount;

        [Header("Évolution Temporelle")]
        public float ageInMillions;            // Âge en millions d'années
        public float coolingRate;              // Vitesse de refroidissement
        public bool isActive;                  // 🆕 AJOUTÉ : Encore actif

        [Header("Optimisation")]
        public bool needsUpdate;               // Flag pour savoir si recalcul nécessaire
        public float lastUpdateTime;          // Dernière mise à jour
        public float significantChangeThreshold = 0.1f; // Seuil de changement significatif

        // Constructeur optimisé
        public VolcanicHotSpot(SimpleVolcano volcano, float baseRadius)
        {
            worldPosition = volcano.worldPosition;
            mapCoordinates = volcano.heightMapCoords;
            sourceType = volcano.type;
            currentState = volcano.currentState;
            eruptionsCount = volcano.eruptionsCompleted;

            CalculateThermalProperties(volcano, baseRadius);

            // 🆕 AJOUTÉ : Initialisation propriétés manquantes
            ageInMillions = 0f;
            isActive = true;
            coolingRate = GetCoolingRateForType(sourceType);

            needsUpdate = true; // Nouveau hot-spot = besoin calcul initial
            lastUpdateTime = Time.time;
        }

        private void CalculateThermalProperties(SimpleVolcano volcano, float baseRadius)
        {
            // Calculs identiques mais optimisés
            switch (sourceType)
            {
                case VolcanoType.Shield:
                    coreTemperature = 800f + (volcano.intensity * 400f);
                    influenceRadius = baseRadius * 3f;
                    thermalIntensity = 0.6f;
                    break;

                case VolcanoType.Fissure:
                    coreTemperature = 1000f + (volcano.intensity * 600f);
                    influenceRadius = baseRadius * 4f;
                    thermalIntensity = 0.8f;
                    break;

                default:
                    coreTemperature = 600f + (volcano.intensity * 300f);
                    influenceRadius = baseRadius * 2f;
                    thermalIntensity = 0.5f;
                    break;
            }

            float eruptionBonus = eruptionsCount * 0.1f;
            coreTemperature += eruptionBonus * 100f;
            thermalIntensity = Mathf.Min(1f, thermalIntensity + eruptionBonus);

            activityLevel = currentState == VolcanicState.Extinct ? 0.2f : thermalIntensity;
        }

        /// <summary>
        /// 🆕 AJOUTÉ : Obtient le taux de refroidissement selon le type
        /// </summary>
        private float GetCoolingRateForType(VolcanoType type)
        {
            switch (type)
            {
                case VolcanoType.Shield:
                    return 0.1f; // Refroidit lentement
                case VolcanoType.Fissure:
                    return 0.05f; // Refroidit très lentement
                default:
                    return 0.2f; // Refroidissement standard
            }
        }

        /// <summary>
        /// Met à jour le hot-spot lors d'une éruption (event-driven)
        /// </summary>
        public bool UpdateFromEruption(SimpleVolcano volcano)
        {
            float oldTemp = coreTemperature;
            float oldIntensity = thermalIntensity;

            // Recalculer les propriétés
            eruptionsCount = volcano.eruptionsCompleted;
            currentState = volcano.currentState;
            CalculateThermalProperties(volcano, influenceRadius / 3f); // Estimer baseRadius

            // Vérifier si changement significatif
            float tempChange = Mathf.Abs(coreTemperature - oldTemp) / oldTemp;
            float intensityChange = Mathf.Abs(thermalIntensity - oldIntensity) / oldIntensity;

            bool significantChange = tempChange > significantChangeThreshold ||
                                   intensityChange > significantChangeThreshold;

            if (significantChange)
            {
                needsUpdate = true;
                lastUpdateTime = Time.time;
            }

            return significantChange;
        }
    }

    /// <summary>
    /// Gestionnaire optimisé du système de hot-spots volcaniques
    /// </summary>
    public class VolcanicHotSpotSystem : MonoBehaviour
    {
        [Header("🔥 Configuration Hot-Spots")]
        [SerializeField] private bool enableHotSpots = true;
        [SerializeField] private float baseInfluenceRadius = 50f;

        [Header("⚡ Optimisation Performance")]
        [SerializeField] private float mapUpdateInterval = 5f; // Secondes entre mises à jour
        [SerializeField] private int maxHotSpotsPerFrame = 5;  // Limite traitement par frame
        [SerializeField] private bool onlyUpdateOnEvents = true; // Seulement sur éruptions/changements

        [Header("📊 Cartes d'Influence")]
        [SerializeField] private bool generateThermalMap = true;
        [SerializeField] private bool generateWeaknessMap = true;
        [SerializeField] private bool generateSeismicMap = true;
        [SerializeField] private int mapResolution = 512;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool enablePerformanceLogs = false;

        // Données du système
        private List<VolcanicHotSpot> hotSpots = new List<VolcanicHotSpot>();
        private float[,] thermalInfluenceMap;
        private float[,] crustalWeaknessMap;
        private float[,] seismicActivityMap;

        // Optimisation
        private float lastMapUpdate = 0f;
        private bool mapNeedsRegeneration = false;
        private Queue<VolcanicHotSpot> hotSpotsToUpdate = new Queue<VolcanicHotSpot>();

        // Références système
        private CleanVolcanicSystem volcanicSystem;
        private GameManager gameManager;
        private PlanetGenerator planetGenerator;

        // Propriétés publiques
        public IReadOnlyList<VolcanicHotSpot> HotSpots => hotSpots.AsReadOnly();
        public float[,] ThermalMap => thermalInfluenceMap;
        public float[,] WeaknessMap => crustalWeaknessMap;
        public float[,] SeismicMap => seismicActivityMap;

        void Start()
        {
            InitializeSystem();
        }

        void Update()
        {
            if (!enableHotSpots) return;

            // ⚡ OPTIMISATION : Traitement par petits lots
            ProcessHotSpotUpdates();

            // ⚡ OPTIMISATION : Régénération conditionnelle et espacée
            if (ShouldRegenerateMap())
            {
                RegenerateInfluenceMaps();
            }
        }

        private void InitializeSystem()
        {
            volcanicSystem = FindAnyObjectByType<CleanVolcanicSystem>();
            gameManager = FindAnyObjectByType<GameManager>();
            planetGenerator = FindAnyObjectByType<PlanetGenerator>();

            if (volcanicSystem == null)
            {
                LogDebug("❌ CleanVolcanicSystem non trouvé");
                enabled = false;
                return;
            }

            InitializeInfluenceMaps();
            LogDebug("🔥 Système Hot-Spots optimisé initialisé");
        }

        private void InitializeInfluenceMaps()
        {
            if (generateThermalMap)
                thermalInfluenceMap = new float[mapResolution, mapResolution];
            if (generateWeaknessMap)
                crustalWeaknessMap = new float[mapResolution, mapResolution];
            if (generateSeismicMap)
                seismicActivityMap = new float[mapResolution, mapResolution];
        }

        /// <summary>
        /// ⚡ OPTIMISÉ : Enregistrement event-driven
        /// </summary>
        public void RegisterVolcanicHotSpot(SimpleVolcano volcano)
        {
            if (!enableHotSpots) return;

            var hotSpot = new VolcanicHotSpot(volcano, baseInfluenceRadius);
            hotSpots.Add(hotSpot);

            // ⚡ Marquer pour mise à jour au lieu de régénérer immédiatement
            mapNeedsRegeneration = true;

            LogDebug($"🔥 Hot-spot enregistré: {volcano.type} - Régénération programmée");

            if (enablePerformanceLogs)
                LogDebug($"⚡ Performance: {hotSpots.Count} hot-spots total");
        }

        /// <summary>
        /// ⚡ NOUVEAU : Notification d'éruption (event-driven)
        /// </summary>
        public void OnVolcanoEruption(SimpleVolcano volcano)
        {
            if (!enableHotSpots) return;

            // Trouver le hot-spot correspondant
            var hotSpot = hotSpots.Find(hs =>
                Vector2Int.Distance(hs.mapCoordinates, volcano.heightMapCoords) < 2);

            if (hotSpot != null)
            {
                bool significantChange = hotSpot.UpdateFromEruption(volcano);

                if (significantChange)
                {
                    mapNeedsRegeneration = true;
                    LogDebug($"🔥 Hot-spot mis à jour après éruption: {volcano.type}");

                    if (enablePerformanceLogs)
                        LogDebug($"⚡ Changement significatif détecté - Régénération programmée");
                }
            }
        }

        /// <summary>
        /// ⚡ OPTIMISÉ : Traitement par petits lots
        /// </summary>
        private void ProcessHotSpotUpdates()
        {
            if (onlyUpdateOnEvents) return; // Pas de mise à jour automatique

            int processed = 0;
            while (hotSpotsToUpdate.Count > 0 && processed < maxHotSpotsPerFrame)
            {
                var hotSpot = hotSpotsToUpdate.Dequeue();

                // Traitement minimal - juste vieillissement
                if (Time.time - hotSpot.lastUpdateTime > 60f) // 1 minute
                {
                    float oldActivity = hotSpot.activityLevel;
                    hotSpot.activityLevel *= 0.999f; // Déclin très lent

                    if (Mathf.Abs(hotSpot.activityLevel - oldActivity) > hotSpot.significantChangeThreshold)
                    {
                        hotSpot.needsUpdate = true;
                        mapNeedsRegeneration = true;
                    }

                    hotSpot.lastUpdateTime = Time.time;
                }

                processed++;
            }
        }

        /// <summary>
        /// ⚡ OPTIMISÉ : Régénération conditionnelle
        /// </summary>
        private bool ShouldRegenerateMap()
        {
            if (!mapNeedsRegeneration) return false;

            // Espacer les régénérations
            if (Time.time - lastMapUpdate < mapUpdateInterval) return false;

            return true;
        }

        /// <summary>
        /// ⚡ OPTIMISÉ : Régénération avec mesure performance
        /// </summary>
        private void RegenerateInfluenceMaps()
        {
            var startTime = System.Diagnostics.Stopwatch.StartNew();

            // Réinitialiser les cartes
            if (generateThermalMap)
                System.Array.Clear(thermalInfluenceMap, 0, thermalInfluenceMap.Length);
            if (generateWeaknessMap)
                System.Array.Clear(crustalWeaknessMap, 0, crustalWeaknessMap.Length);
            if (generateSeismicMap)
                System.Array.Clear(seismicActivityMap, 0, seismicActivityMap.Length);

            // Calculer seulement les hot-spots qui ont changé
            int processedCount = 0;
            foreach (var hotSpot in hotSpots)
            {
                if (hotSpot.activityLevel < 0.05f) continue; // Ignorer les très faibles

                ApplyHotSpotInfluence(hotSpot);
                hotSpot.needsUpdate = false; // Marquer comme traité
                processedCount++;
            }

            lastMapUpdate = Time.time;
            mapNeedsRegeneration = false;

            startTime.Stop();

            if (enablePerformanceLogs)
            {
                LogDebug($"⚡ Performance: Cartes régénérées en {startTime.ElapsedMilliseconds}ms " +
                        $"({processedCount}/{hotSpots.Count} hot-spots traités)");
            }
            else
            {
                LogDebug($"🗺️ Cartes d'influence mises à jour - {processedCount} hot-spots actifs");
            }
        }

        private void ApplyHotSpotInfluence(VolcanicHotSpot hotSpot)
        {
            Vector2Int center = hotSpot.mapCoordinates;
            float planetRadius = planetGenerator?.PlanetRadius ?? 100f;
            int radiusPixels = Mathf.CeilToInt((hotSpot.influenceRadius / planetRadius) * mapResolution);

            // ⚡ OPTIMISATION : Pré-calculer les valeurs
            float activityFactor = hotSpot.activityLevel;
            float thermalBase = activityFactor * (hotSpot.coreTemperature / 1600f);
            float weaknessBase = activityFactor * 0.8f;
            float seismicBase = activityFactor * 0.6f;

            for (int x = center.x - radiusPixels; x <= center.x + radiusPixels; x++)
            {
                for (int y = center.y - radiusPixels; y <= center.y + radiusPixels; y++)
                {
                    if (x >= 0 && x < mapResolution && y >= 0 && y < mapResolution)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center.x, center.y));

                        if (distance <= radiusPixels)
                        {
                            float normalizedDistance = distance / radiusPixels;
                            float baseInfluence = 1f - normalizedDistance;

                            // Profil selon type (pré-calculé)
                            float influence = ApplyTypeSpecificInfluence(baseInfluence, normalizedDistance, hotSpot.sourceType);

                            // Application optimisée
                            if (generateThermalMap)
                                thermalInfluenceMap[x, y] = Mathf.Max(thermalInfluenceMap[x, y], influence * thermalBase);
                            if (generateWeaknessMap)
                                crustalWeaknessMap[x, y] = Mathf.Max(crustalWeaknessMap[x, y], influence * weaknessBase);
                            if (generateSeismicMap)
                                seismicActivityMap[x, y] = Mathf.Max(seismicActivityMap[x, y], influence * seismicBase);
                        }
                    }
                }
            }
        }

        private float ApplyTypeSpecificInfluence(float baseInfluence, float distance, VolcanoType type)
        {
            // ⚡ OPTIMISATION : Calculs simplifiés
            switch (type)
            {
                case VolcanoType.Shield:
                    return baseInfluence * (1f - distance * 0.5f);
                case VolcanoType.Fissure:
                    return baseInfluence * (1f - distance * 0.3f);
                default:
                    return baseInfluence * (1f - distance);
            }
        }

        // Méthodes d'accès inchangées mais avec vérification performance
        public float GetLocalTemperature(Vector2Int mapPosition)
        {
            if (!generateThermalMap || thermalInfluenceMap == null) return 0f;
            if (mapPosition.x < 0 || mapPosition.x >= mapResolution ||
                mapPosition.y < 0 || mapPosition.y >= mapResolution) return 0f;

            return thermalInfluenceMap[mapPosition.x, mapPosition.y];
        }

        public float GetCrustalWeakness(Vector2Int mapPosition)
        {
            if (!generateWeaknessMap || crustalWeaknessMap == null) return 0f;
            if (mapPosition.x < 0 || mapPosition.x >= mapResolution ||
                mapPosition.y < 0 || mapPosition.y >= mapResolution) return 0f;

            return crustalWeaknessMap[mapPosition.x, mapPosition.y];
        }

        public float GetSeismicActivity(Vector2Int mapPosition)
        {
            if (!generateSeismicMap || seismicActivityMap == null) return 0f;
            if (mapPosition.x < 0 || mapPosition.x >= mapResolution ||
                mapPosition.y < 0 || mapPosition.y >= mapResolution) return 0f;

            return seismicActivityMap[mapPosition.x, mapPosition.y];
        }

        // Debug optimisé
        [ContextMenu("Show Performance Stats")]
        public void ShowPerformanceStats()
        {
            LogDebug("⚡ === STATISTIQUES PERFORMANCE HOT-SPOTS ===");
            LogDebug($"Total hot-spots: {hotSpots.Count}");
            LogDebug($"Dernière mise à jour: {Time.time - lastMapUpdate:F1}s");
            LogDebug($"Régénération nécessaire: {mapNeedsRegeneration}");
            LogDebug($"Mode event-driven: {onlyUpdateOnEvents}");

            int activeCount = hotSpots.Count(hs => hs.activityLevel > 0.05f);
            int needUpdateCount = hotSpots.Count(hs => hs.needsUpdate);

            LogDebug($"Hot-spots actifs: {activeCount}/{hotSpots.Count}");
            LogDebug($"En attente mise à jour: {needUpdateCount}");
        }

        /// <summary>
        /// 🆕 AJOUTÉ : Méthode manquante pour le debug
        /// </summary>
        [ContextMenu("Show Hot-Spots Status")]
        public void ShowHotSpotsStatus()
        {
            LogDebug($"🔥 === STATUT {hotSpots.Count} HOT-SPOTS ===");

            int activeCount = 0, inactiveCount = 0;
            float totalThermal = 0f, totalActivity = 0f;

            foreach (var hotSpot in hotSpots)
            {
                if (hotSpot.isActive) activeCount++;
                else inactiveCount++;

                totalThermal += hotSpot.coreTemperature;
                totalActivity += hotSpot.activityLevel;

                string status = hotSpot.isActive ? "🔥 ACTIF" : "🧊 INACTIF";
                LogDebug($"{status} {hotSpot.sourceType} - Temp: {hotSpot.coreTemperature:F0}°C, " +
                        $"Âge: {hotSpot.ageInMillions:F1}Ma, Activité: {hotSpot.activityLevel:F2}");
            }

            if (hotSpots.Count > 0)
            {
                LogDebug($"📊 Résumé: {activeCount} actifs, {inactiveCount} inactifs");
                LogDebug($"📊 Température moyenne: {(totalThermal / hotSpots.Count):F0}°C");
                LogDebug($"📊 Activité moyenne: {(totalActivity / hotSpots.Count):F2}");
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[HotSpots] {message}");
        }

        internal void RemoveVolcanicHotSpot(SimpleVolcano extinct)
        {
            throw new NotImplementedException();
        }
    }
}