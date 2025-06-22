// VolcanoTypeData.cs - Données de configuration pour un type de volcan
// EXTRAIT de VolcanoTypesManager pour éliminer dépendances circulaires
using UnityEngine;

namespace LifeStory.Geology
{
    /// <summary>
    /// Énumération des types de volcans disponibles
    /// </summary>
    public enum VolcanoType
    {
        Shield,         // Volcan bouclier - éruptions douces
        Fissure,        // Volcan de fissure - éruptions linéaires  
        Stratovolcano,  // Stratovolcan - éruptions explosives
        Cinder,         // Cône de scories - éruptions courtes
        Caldera         // Caldeira - éruptions catastrophiques
    }

    /// <summary>
    /// Caractéristiques complètes de chaque type de volcan
    /// Données pures configurables dans l'Inspector Unity
    /// </summary>
    [System.Serializable]
    public class VolcanoTypeData
    {
        [Header("🆕 Déformation Avancée")]
        [Tooltip("Profil de déformation personnalisé (optionnel)")]
        public VolcanicDeformationProfile customDeformationProfile;

        [Tooltip("Utiliser les nouveaux profils configurables")]
        public bool useAdvancedDeformation = true;

        [Header("Identification")]
        public VolcanoType type;
        public string displayName;

        [TextArea(2, 4)]
        public string description;

        [Header("Modèle 3D")]
        public GameObject prefab;              // Modèle Blender
        public Vector3 scaleRange = Vector3.one; // Variation de taille

        [Header("Caractéristiques Géologiques")]
        [Range(0f, 1f)]
        public float explosivity = 0.5f;       // 0=Effusif, 1=Explosif

        [Range(0f, 1f)]
        public float gasEmission = 0.3f;       // Émission de gaz

        [Range(0.1f, 5f)]
        public float eruptionDuration = 1f;    // Durée relative d'éruption

        [Header("Conditions d'Apparition")]
        public float minTemperature = 1000f;    // Température minimale
        public float maxTemperature = 2560f;   // Température maximale
        public float optimalTemperature = 1200f; // Température optimale

        [Range(0f, 1f)]
        public float rarity = 0.3f;            // 0=Très commun, 1=Très rare

        [Header("Effets Visuels")]
        public Color lavaColor = Color.red;
        public float lightIntensity = 2f;
        public bool hasLavaDrops = true;

        [Header("Déformation Terrain")]
        [Range(0.1f, 3f)]
        public float deformationRadius = 1f;   // Multiplicateur du rayon de base

        [Range(0.1f, 2f)]
        public float deformationStrength = 1f; // Multiplicateur de la force

     
    }



    /// <summary>
    /// Extensions pour VolcanoType - Méthodes utilitaires
    /// </summary>
    public static class VolcanoTypeExtensions
    {
        /// <summary>
        /// Obtenir le nom d'affichage par défaut pour un type de volcan
        /// </summary>
        public static string GetDisplayName(this VolcanoType type)
        {
            return type switch
            {
                VolcanoType.Shield => "Volcan Bouclier",
                VolcanoType.Fissure => "Volcan de Fissure",
                VolcanoType.Stratovolcano => "Stratovolcan",
                VolcanoType.Cinder => "Cône de Scories",
                VolcanoType.Caldera => "Caldeira",
                _ => type.ToString()
            };
        }

        public static bool CanAppearAtCoreTemperature(this VolcanoTypeData data, float coreTemperature)
        {
            if (data.minTemperature <= 0f)
            {
                return coreTemperature <= data.maxTemperature;
            }
            else
            {
                return coreTemperature >= data.minTemperature && coreTemperature <= data.maxTemperature;
            }
        }

        public static bool IsInIntenseActivity(this VolcanoTypeData data, float coreTemperature)
        {
            float difference = Mathf.Abs(coreTemperature - data.optimalTemperature);
            return difference <= 100f;
        }

        public static float GetActivityMultiplier(this VolcanoTypeData data, float coreTemperature)
        {
            if (!data.CanAppearAtCoreTemperature(coreTemperature))
            {
                return 0f;
            }
            else if (data.IsInIntenseActivity(coreTemperature))
            {
                return 2f;
            }
            else
            {
                return 1f;
            }
        }

        /// <summary>
        /// Obtenir la description par défaut pour un type de volcan
        /// </summary>
        public static string GetDescription(this VolcanoType type)
        {
            return type switch
            {
                VolcanoType.Shield => "Large volcan, éruptions douces et prolongées",
                VolcanoType.Fissure => "Éruption linéaire, coulées importantes",
                VolcanoType.Stratovolcano => "Volcan conique, éruptions explosives",
                VolcanoType.Cinder => "Petit cône, éruptions courtes et intenses",
                VolcanoType.Caldera => "Dépression volcanique, éruptions catastrophiques",
                _ => "Type de volcan non documenté"
            };
        }

        /// <summary>
        /// Obtenir le nom du prefab par défaut pour un type de volcan
        /// </summary>
        public static string GetPrefabName(this VolcanoType type)
        {
            return type switch
            {
                VolcanoType.Shield => "Prefabs/Volcans/Shield_Volcano",
                VolcanoType.Fissure => "Prefabs/Volcans/Fissure_Volcano",
                VolcanoType.Stratovolcano => "Prefabs/Volcans/Strato_Volcano",
                VolcanoType.Cinder => "Prefabs/Volcans/Cinder_Volcano",
                VolcanoType.Caldera => "Prefabs/Volcans/Caldera_Volcano",
                _ => ""
            };
        }

        /// <summary>
        /// Vérifier si un type de volcan est disponible en Phase 1
        /// </summary>
        public static bool IsAvailableInPhase1(this VolcanoType type)
        {
            return type == VolcanoType.Shield || type == VolcanoType.Fissure;
        }



        /// <summary>
        /// Obtenir la priorité d'implémentation (1 = priorité maximale)
        /// </summary>
        public static int GetImplementationPriority(this VolcanoType type)
        {
            return type switch
            {
                VolcanoType.Shield => 1,      // Phase 1 - Priorité max
                VolcanoType.Fissure => 1,     // Phase 1 - Priorité max
                VolcanoType.Stratovolcano => 2, // Phase 2
                VolcanoType.Cinder => 3,      // Phase 3
                VolcanoType.Caldera => 4,     // Phase 4
                _ => 99
            };
        }

        /// <summary>
        /// Obtenir les caractéristiques par défaut pour un type
        /// </summary>
        public static VolcanoTypeData GetDefaultCharacteristics(this VolcanoType type)
        {
            var data = new VolcanoTypeData
            {
                type = type,
                displayName = type.GetDisplayName(),
                description = type.GetDescription()
            };

            // Caractéristiques spécifiques par type
            switch (type)
            {
                case VolcanoType.Shield:
                    data.explosivity = 0.2f;
                    data.gasEmission = 0.4f;
                    data.eruptionDuration = 2.5f;
                    data.minTemperature = 1000f;
                    data.maxTemperature = 1600f;
                    data.optimalTemperature = 1200f;
                    data.rarity = 0.1f; // Très commun
                    data.deformationRadius = 1.5f;
                    data.deformationStrength = 0.8f;
                    data.lavaColor = new Color(1f, 0.3f, 0f, 1f);
                    break;

                case VolcanoType.Fissure:
                    data.explosivity = 0.1f;
                    data.gasEmission = 0.6f;
                    data.eruptionDuration = 4f;
                    data.minTemperature = 1300f;
                    data.maxTemperature = 2000f;
                    data.optimalTemperature = 1600f;
                    data.rarity = 0.2f; // Modérément commun
                    data.deformationRadius = 1.8f;
                    data.deformationStrength = 1.2f;
                    data.lavaColor = new Color(1f, 0.4f, 0.1f, 1f);
                    break;

                case VolcanoType.Stratovolcano:
                    data.explosivity = 0.8f;
                    data.gasEmission = 0.5f;
                    data.eruptionDuration = 1.2f;
                    data.minTemperature = 1100f;
                    data.maxTemperature = 1400f;
                    data.optimalTemperature = 1250f;
                    data.rarity = 0.4f; // Modérément rare
                    data.deformationRadius = 1.2f;
                    data.deformationStrength = 1.5f;
                    data.lavaColor = new Color(0.9f, 0.2f, 0f, 1f);
                    break;

                case VolcanoType.Cinder:
                    data.explosivity = 0.6f;
                    data.gasEmission = 0.3f;
                    data.eruptionDuration = 0.8f;
                    data.minTemperature = 1050f;
                    data.maxTemperature = 1300f;
                    data.optimalTemperature = 1150f;
                    data.rarity = 0.3f; // Commun
                    data.deformationRadius = 0.8f;
                    data.deformationStrength = 1.0f;
                    data.lavaColor = new Color(1f, 0.5f, 0.2f, 1f);
                    break;

                case VolcanoType.Caldera:
                    data.explosivity = 1.0f;
                    data.gasEmission = 0.9f;
                    data.eruptionDuration = 0.5f;
                    data.minTemperature = 1400f;
                    data.maxTemperature = 1800f;
                    data.optimalTemperature = 1600f;
                    data.rarity = 0.9f; // Très rare
                    data.deformationRadius = 3.0f;
                    data.deformationStrength = 2.0f;
                    data.lavaColor = new Color(1f, 0.1f, 0f, 1f);
                    break;
            }

            return data;
        }
    }
}