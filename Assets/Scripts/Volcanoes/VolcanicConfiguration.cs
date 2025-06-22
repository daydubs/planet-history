// VolcanicConfiguration.cs - Data Only - Configuration pure pour système volcanique
using UnityEngine;
using System.Collections.Generic;
using LifeStory.Geology;
using LifeStory.Volcanoes;

namespace LifeStory.Volcanoes
{
    /// <summary>
    /// Configuration pure des données volcaniques - AUCUNE LOGIQUE MÉTIER
    /// Responsabilité unique : Stocker et exposer les données configurables
    /// </summary>
    [CreateAssetMenu(fileName = "VolcanicConfiguration", menuName = "Life Story/Volcanic Configuration")]
    public class VolcanicConfiguration : ScriptableObject
    {
        [Header("🌋 Types de Volcans")]
        [SerializeField] private VolcanoTypeData[] volcanoTypes = new VolcanoTypeData[5];

        [Header("🎮 Système de Presets")]
        [SerializeField] private bool usePresetSystem = false;
        [SerializeField] private VolcanicPresetCollection activeCollection;
        [SerializeField] private bool autoLoadCollectionOnStart = false;

        [Header("🎯 Sélection Intelligente")]
        [SerializeField] private AnimationCurve temperatureProbability = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private bool favoriteOptimalTemperatures = true;

        [Header("🔧 Phase de Développement")]
        [SerializeField] private bool phase1Only = true;
        [SerializeField] private bool enableAutoAssignment = false;

        [Header("🌡️ Seuils Température")]
        [SerializeField] private float minVolcanicTemp = 1000f;
        [SerializeField] private float maxVolcanicTemp = 4200f;
        [SerializeField] private float optimalVolcanicTemp = 3500f;

        [Header("⚡ Performance")]
        [SerializeField] private int maxVolcanoes = 15;
        [SerializeField] private float volcanoSpawnRate = 0.8f;
        [SerializeField] private float minDistanceBetweenVolcanoes = 0.2f;

        [Header("🔥 Éruptions")]
        [SerializeField] private int minEruptionsPerVolcano = 2;
        [SerializeField] private int maxEruptionsPerVolcano = 5;
        [SerializeField] private float temperatureDropMin = 250f;
        [SerializeField] private float temperatureDropMax = 400f;

        [Header("🌊 Émissions")]
        [SerializeField] private bool enableGasEmissions = true;
        [SerializeField] private bool enableWaterEmissions = true;
        [SerializeField] private float shieldCO2EmissionBase = 0.002f;
        [SerializeField] private float shieldWaterEmissionBase = 0.003f;
        [SerializeField] private float fissureCH4EmissionRate = 0.0005f;
        [SerializeField] private float fissureWaterEmissionRate = 0.001f;

        [Header("🏔️ Déformation Terrain")]
        [SerializeField] private float baseDeformationRadius = 2f;
        [SerializeField] private float baseDeformationStrength = 0.3f;
        [SerializeField] private AnimationCurve deformationFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("🎨 Visuels")]
        [SerializeField] private GameObject defaultVolcanoPrefab;
        [SerializeField] private Material lavaMaterial;

        [Header("📊 Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showEruptionEvents = true;
        [SerializeField] private bool showWaterEmissionLogs = true;

        // === API LECTURE SEULE ===

        /// <summary>Obtenir tous les types de volcans configurés</summary>
        public VolcanoTypeData[] GetAllVolcanoTypes() => volcanoTypes;

        /// <summary>Obtenir un type spécifique par index</summary>
        public VolcanoTypeData GetVolcanoType(int index)
        {
            if (index >= 0 && index < volcanoTypes.Length)
                return volcanoTypes[index];
            return null;
        }

        /// <summary>Nombre total de types configurés</summary>
        public int TotalVolcanoTypes => volcanoTypes?.Length ?? 0;

        // === PROPRIÉTÉS PRESETS ===
        public bool UsePresetSystem => usePresetSystem;
        public VolcanicPresetCollection ActiveCollection => activeCollection;
        public bool AutoLoadCollectionOnStart => autoLoadCollectionOnStart;

        // === PROPRIÉTÉS SÉLECTION ===
        public AnimationCurve TemperatureProbability => temperatureProbability;
        public bool FavoriteOptimalTemperatures => favoriteOptimalTemperatures;

        // === PROPRIÉTÉS DÉVELOPPEMENT ===
        public bool Phase1Only => phase1Only;
        public bool EnableAutoAssignment => enableAutoAssignment;

        // === PROPRIÉTÉS TEMPÉRATURE ===
        public float MinVolcanicTemp => minVolcanicTemp;
        public float MaxVolcanicTemp => maxVolcanicTemp;
        public float OptimalVolcanicTemp => optimalVolcanicTemp;

        // === PROPRIÉTÉS PERFORMANCE ===
        public int MaxVolcanoes => maxVolcanoes;
        public float VolcanoSpawnRate => volcanoSpawnRate;
        public float MinDistanceBetweenVolcanoes => minDistanceBetweenVolcanoes;

        // === PROPRIÉTÉS ÉRUPTIONS ===
        public int MinEruptionsPerVolcano => minEruptionsPerVolcano;
        public int MaxEruptionsPerVolcano => maxEruptionsPerVolcano;
        public float TemperatureDropMin => temperatureDropMin;
        public float TemperatureDropMax => temperatureDropMax;

        // === PROPRIÉTÉS ÉMISSIONS ===
        public bool EnableGasEmissions => enableGasEmissions;
        public bool EnableWaterEmissions => enableWaterEmissions;
        public float ShieldCO2EmissionBase => shieldCO2EmissionBase;
        public float ShieldWaterEmissionBase => shieldWaterEmissionBase;
        public float FissureCH4EmissionRate => fissureCH4EmissionRate;
        public float FissureWaterEmissionRate => fissureWaterEmissionRate;

        // === PROPRIÉTÉS DÉFORMATION ===
        public float BaseDeformationRadius => baseDeformationRadius;
        public float BaseDeformationStrength => baseDeformationStrength;
        public AnimationCurve DeformationFalloff => deformationFalloff;

        // === PROPRIÉTÉS VISUELS ===
        public GameObject DefaultVolcanoPrefab => defaultVolcanoPrefab;
        public Material LavaMaterial => lavaMaterial;

        // === PROPRIÉTÉS DEBUG ===
        public bool EnableDebugLogs => enableDebugLogs;
        public bool ShowEruptionEvents => showEruptionEvents;
        public bool ShowWaterEmissionLogs => showWaterEmissionLogs;

        // === MÉTHODES UTILITAIRES (LECTURE SEULE) ===

        /// <summary>Vérifier si les températures sont dans une plage volcanique valide</summary>
        public bool IsTemperatureInVolcanicRange(float temperature)
        {
            return temperature >= minVolcanicTemp && temperature <= maxVolcanicTemp;
        }

        /// <summary>Obtenir le facteur d'activité normalisé pour une température</summary>
        public float GetActivityFactor(float temperature)
        {
            if (!IsTemperatureInVolcanicRange(temperature)) return 0f;
            return Mathf.InverseLerp(minVolcanicTemp, maxVolcanicTemp, temperature);
        }

        /// <summary>Compter les types disponibles (avec prefab assigné)</summary>
        public int CountAvailableTypes()
        {
            int count = 0;
            if (volcanoTypes != null)
            {
                foreach (var type in volcanoTypes)
                {
                    if (type?.prefab != null) count++;
                }
            }
            return count;
        }

        /// <summary>Valider la cohérence de la configuration</summary>
        public bool ValidateConfiguration(out string errorMessage)
        {
            errorMessage = "";

            // Vérifier températures
            if (minVolcanicTemp >= maxVolcanicTemp)
            {
                errorMessage = "MinVolcanicTemp doit être inférieur à MaxVolcanicTemp";
                return false;
            }

            if (optimalVolcanicTemp < minVolcanicTemp || optimalVolcanicTemp > maxVolcanicTemp)
            {
                errorMessage = "OptimalVolcanicTemp doit être entre Min et Max";
                return false;
            }

            // Vérifier éruptions
            if (minEruptionsPerVolcano >= maxEruptionsPerVolcano)
            {
                errorMessage = "MinEruptions doit être inférieur à MaxEruptions";
                return false;
            }

            // Vérifier température drops
            if (temperatureDropMin >= temperatureDropMax)
            {
                errorMessage = "TemperatureDropMin doit être inférieur à DropMax";
                return false;
            }

            // Vérifier qu'au moins un type est configuré
            if (CountAvailableTypes() == 0)
            {
                errorMessage = "Aucun type de volcan disponible (prefabs manquants)";
                return false;
            }

            return true;
        }

        // === MÉTHODES EDITOR ONLY ===

#if UNITY_EDITOR
        [ContextMenu("Validate Configuration")]
        private void ValidateInEditor()
        {
            if (ValidateConfiguration(out string error))
            {
                Debug.Log("✅ Configuration volcanique valide");
            }
            else
            {
                Debug.LogError($"❌ Configuration invalide: {error}");
            }
        }

        [ContextMenu("Show Configuration Summary")]
        private void ShowConfigurationSummary()
        {
            Debug.Log("🌋 === RÉSUMÉ CONFIGURATION VOLCANIQUE ===");
            Debug.Log($"Types configurés: {TotalVolcanoTypes}");
            Debug.Log($"Types disponibles: {CountAvailableTypes()}");
            Debug.Log($"Plage température: {minVolcanicTemp:F0}-{maxVolcanicTemp:F0}°C");
            Debug.Log($"Max volcans: {maxVolcanoes}");
            Debug.Log($"Presets activé: {usePresetSystem}");
            Debug.Log($"Phase 1 seulement: {phase1Only}");
        }
#endif
    }
}