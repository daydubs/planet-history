// VolcanicPreset_Simple.cs - Version simplifiée intégrée à votre système
using UnityEngine;

namespace LifeStory.Geology
{
    /// <summary>
    /// ScriptableObject pour sauvegarder/charger les configurations de volcans
    /// Compatible avec votre VolcanoTypeData existant
    /// </summary>
    [CreateAssetMenu(fileName = "New Volcano Preset", menuName = "Life Story/Volcano Preset", order = 1)]
    public class VolcanicPreset : ScriptableObject
    {
        [Header("🌋 Configuration Volcan")]
        public VolcanoTypeData volcanoConfig;

        [Header("📝 Métadonnées")]
        public string presetName = "Nouveau Preset";
        [TextArea(2, 3)]
        public string description = "Description de ce preset volcanique";
        public string version = "1.0";

        [Header("🎯 Tags & Catégories")]
        public string[] tags = new string[] { "default" };
        public PresetCategory category = PresetCategory.Balanced;

        /// <summary>
        /// Obtient la configuration volcanique
        /// </summary>
        public VolcanoTypeData GetConfig()
        {
            return volcanoConfig;
        }

        /// <summary>
        /// Valide que ce preset est correct
        /// </summary>
        public bool IsValid()
        {
            if (volcanoConfig == null) return false;
            if (volcanoConfig.minTemperature >= volcanoConfig.maxTemperature) return false;
            if (string.IsNullOrEmpty(presetName)) return false;

            return true;
        }

        /// <summary>
        /// Résumé pour debug
        /// </summary>
        public override string ToString()
        {
            if (volcanoConfig == null) return $"{presetName} [INVALID]";

            return $"{presetName} ({volcanoConfig.type}): " +
                   $"{volcanoConfig.minTemperature:F0}-{volcanoConfig.maxTemperature:F0}°C, " +
                   $"Rareté: {volcanoConfig.rarity:F2}";
        }

        /// <summary>
        /// Copie ce preset vers un VolcanoTypeData
        /// </summary>
        public void CopyTo(ref VolcanoTypeData target)
        {
            if (volcanoConfig == null) return;

            target.type = volcanoConfig.type;
            target.displayName = volcanoConfig.displayName;
            target.description = volcanoConfig.description;
            target.prefab = volcanoConfig.prefab;
            target.scaleRange = volcanoConfig.scaleRange;
            target.explosivity = volcanoConfig.explosivity;
            target.gasEmission = volcanoConfig.gasEmission;
            target.eruptionDuration = volcanoConfig.eruptionDuration;
            target.minTemperature = volcanoConfig.minTemperature;
            target.maxTemperature = volcanoConfig.maxTemperature;
            target.optimalTemperature = volcanoConfig.optimalTemperature;
            target.rarity = volcanoConfig.rarity;
            target.lavaColor = volcanoConfig.lavaColor;
            target.lightIntensity = volcanoConfig.lightIntensity;
            target.hasLavaDrops = volcanoConfig.hasLavaDrops;
            target.deformationRadius = volcanoConfig.deformationRadius;
            target.deformationStrength = volcanoConfig.deformationStrength;
        }
    }

    /// <summary>
    /// Collection de presets volcaniques
    /// </summary>
    [CreateAssetMenu(fileName = "Volcano Collection", menuName = "Life Story/Volcano Collection", order = 2)]
    public class VolcanicPresetCollection : ScriptableObject
    {
        [Header("📚 Collection")]
        public string collectionName = "Ma Collection";
        [TextArea(2, 3)]
        public string description = "Collection de presets volcaniques";

        [Header("🌋 Presets")]
        public VolcanicPreset[] presets = new VolcanicPreset[5];

        [Header("🎛️ Modificateurs Globaux")]
        [Range(0.1f, 3f)]
        public float globalRarityMultiplier = 1f;
        [Range(-500f, 500f)]
        public float globalTemperatureOffset = 0f;

        /// <summary>
        /// Obtient tous les presets valides
        /// </summary>
        public VolcanicPreset[] GetValidPresets()
        {
            return System.Array.FindAll(presets, preset => preset != null && preset.IsValid());
        }

        /// <summary>
        /// Obtient un preset par type de volcan
        /// </summary>
        public VolcanicPreset GetPresetByType(VolcanoType type)
        {
            foreach (var preset in presets)
            {
                if (preset != null && preset.volcanoConfig != null && preset.volcanoConfig.type == type)
                    return preset;
            }
            return null;
        }

        /// <summary>
        /// Applique cette collection au VolcanoTypesManager
        /// </summary>
        public void ApplyToManager(VolcanoTypesManager manager)
        {
            var validPresets = GetValidPresets();

            Debug.Log($"🔄 Application collection '{collectionName}' avec {validPresets.Length} presets");

            // Créer un array de VolcanoTypeData
            var configArray = new VolcanoTypeData[5]; // Toujours 5 types

            // Initialiser avec configs par défaut
            for (int i = 0; i < 5; i++)
            {
                configArray[i] = new VolcanoTypeData();
                configArray[i].type = (VolcanoType)i; // Shield=0, Fissure=1, etc.
            }

            // Appliquer les presets
            foreach (var preset in validPresets)
            {
                if (preset.volcanoConfig != null)
                {
                    int typeIndex = (int)preset.volcanoConfig.type;
                    if (typeIndex >= 0 && typeIndex < configArray.Length)
                    {
                        // Copier la config
                        preset.CopyTo(ref configArray[typeIndex]);

                        // Appliquer modificateurs globaux
                        configArray[typeIndex].rarity *= globalRarityMultiplier;
                        configArray[typeIndex].minTemperature += globalTemperatureOffset;
                        configArray[typeIndex].maxTemperature += globalTemperatureOffset;
                        configArray[typeIndex].optimalTemperature += globalTemperatureOffset;

                        Debug.Log($"  ✅ {preset.ToString()}");
                    }
                }
            }

            // Appliquer au manager (nécessite une méthode publique)
            manager.SetVolcanoTypesFromPresets(configArray);
        }

        /// <summary>
        /// Sauvegarde la configuration actuelle du manager dans cette collection
        /// AVEC DEBUG COMPLET
        /// </summary>
        public void SaveFromManager(VolcanoTypesManager manager)
        {
            Debug.Log($"🔍 DÉBUT SaveFromManager pour '{collectionName}'");

            if (manager == null)
            {
                Debug.LogError("❌ Manager est null !");
                return;
            }

            var currentTypes = manager.GetAllVolcanoTypes();
            Debug.Log($"🔍 Types récupérés: {(currentTypes != null ? currentTypes.Length : 0)}");

            if (currentTypes == null)
            {
                Debug.LogError("❌ currentTypes est null !");
                return;
            }

            // S'assurer qu'on a un array de la bonne taille
            if (presets == null || presets.Length != 5)
            {
                Debug.Log($"🔧 Initialisation array presets (était: {(presets?.Length ?? 0)})");
                presets = new VolcanicPreset[5];
            }

            Debug.Log($"🔍 Début boucle - {currentTypes.Length} types à traiter");

            // Créer/mettre à jour les presets
            for (int i = 0; i < currentTypes.Length && i < presets.Length; i++)
            {
                Debug.Log($"🔍 Traitement index {i}");

                if (currentTypes[i] == null)
                {
                    Debug.Log($"⚠️ currentTypes[{i}] est null - ignoré");
                    continue;
                }

                Debug.Log($"🔍 Type {i}: {currentTypes[i].type}");

                try
                {
                    // Créer nouveau preset si nécessaire
                    if (presets[i] == null)
                    {
                        Debug.Log($"🆕 Création nouveau preset pour index {i}");
#if UNITY_EDITOR
                        presets[i] = ScriptableObject.CreateInstance<VolcanicPreset>();
#else
                        presets[i] = ScriptableObject.CreateInstance<VolcanicPreset>();
#endif
                        presets[i].presetName = $"{currentTypes[i].type} Config";
                        presets[i].description = $"Configuration pour {currentTypes[i].displayName}";
                    }

                    // Créer une COPIE de la config actuelle
                    Debug.Log($"🔄 Création nouvelle config pour {currentTypes[i].type}");
                    presets[i].volcanoConfig = new VolcanoTypeData();

                    // Copier les propriétés de base
                    presets[i].volcanoConfig.type = currentTypes[i].type;
                    presets[i].volcanoConfig.displayName = currentTypes[i].displayName ?? "Unknown";
                    presets[i].volcanoConfig.description = currentTypes[i].description ?? "No description";

                    // Copier les températures (CRITIQUES)
                    presets[i].volcanoConfig.minTemperature = currentTypes[i].minTemperature;
                    presets[i].volcanoConfig.maxTemperature = currentTypes[i].maxTemperature;
                    presets[i].volcanoConfig.optimalTemperature = currentTypes[i].optimalTemperature;

                    Debug.Log($"💾 COPIÉ: {currentTypes[i].type} - Temp: {currentTypes[i].minTemperature:F0}-{currentTypes[i].maxTemperature:F0}°C");

                    // Copier le reste des propriétés
                    presets[i].volcanoConfig.prefab = currentTypes[i].prefab;
                    presets[i].volcanoConfig.scaleRange = currentTypes[i].scaleRange;
                    presets[i].volcanoConfig.explosivity = currentTypes[i].explosivity;
                    presets[i].volcanoConfig.gasEmission = currentTypes[i].gasEmission;
                    presets[i].volcanoConfig.eruptionDuration = currentTypes[i].eruptionDuration;
                    presets[i].volcanoConfig.rarity = currentTypes[i].rarity;
                    presets[i].volcanoConfig.lavaColor = currentTypes[i].lavaColor;
                    presets[i].volcanoConfig.lightIntensity = currentTypes[i].lightIntensity;
                    presets[i].volcanoConfig.hasLavaDrops = currentTypes[i].hasLavaDrops;
                    presets[i].volcanoConfig.deformationRadius = currentTypes[i].deformationRadius;
                    presets[i].volcanoConfig.deformationStrength = currentTypes[i].deformationStrength;

                    Debug.Log($"✅ Preset {i} ({currentTypes[i].type}) complètement configuré");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"💥 ERREUR lors de la copie index {i}: {e.Message}");
                }
            }

            // Marquer comme modifié pour Unity
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("🔄 Collection marquée dirty");
#endif

            Debug.Log($"✅ FIN SaveFromManager - {presets.Length} presets traités");
        }

        [ContextMenu("Show Collection Info")]
        public void ShowCollectionInfo()
        {
            Debug.Log($"📋 === COLLECTION: {collectionName} ===");
            Debug.Log($"Description: {description}");
            Debug.Log($"Modificateurs: Rareté ×{globalRarityMultiplier:F1}, Température +{globalTemperatureOffset:F0}°C");

            var validPresets = GetValidPresets();
            Debug.Log($"Presets valides: {validPresets.Length}/{presets.Length}");

            foreach (var preset in validPresets)
            {
                Debug.Log($"  ✅ {preset.ToString()}");
            }
        }
    }

    /// <summary>
    /// Catégories de presets
    /// </summary>
    public enum PresetCategory
    {
        Balanced,       // Équilibré
        Aggressive,     // Plus actif
        Conservative,   // Moins actif
        Realistic,      // Réaliste
        Experimental,   // Test
        Scenario        // Scénario spécifique
    }
}