// BiomeConfiguration.cs - Configuration des biomes
// Placé dans Scripts/Core/Data/BiomeConfiguration.cs

using UnityEngine;
using LifeStory.Core;

namespace LifeStory.Data
{
    /// <summary>
    /// Configuration complète d'un biome avec ses paramètres de hauteur et d'évolution
    /// Classe réutilisable pour différents systèmes de biomes
    /// </summary>
    [System.Serializable]
    public class BiomeConfiguration
    {
        [Header("=== IDENTIFICATION ===")]
        public BiomeType biomeType;

        [Header("=== SEUILS DE HAUTEUR ===")]
        [Tooltip("Hauteur minimale pour ce biome (0-1)")]
        [Range(0f, 1f)]
        public float minHeight = 0f;

        [Tooltip("Hauteur maximale pour ce biome (0-1)")]
        [Range(0f, 1f)]
        public float maxHeight = 1f;

        [Header("=== PROPRIÉTÉS ENVIRONNEMENTALES ===")]
        [Tooltip("Niveau d'humidité du biome")]
        [Range(0f, 1f)]
        public float humidity = 0.5f;

        [Tooltip("Modification de température locale (°C)")]
        [Range(-50f, 50f)]
        public float temperatureModifier = 0f;

        [Tooltip("Ce biome peut-il supporter la vie ?")]
        public bool canSupportLife = true;

        [Tooltip("Ce biome est-il aquatique ?")]
        public bool isWaterBiome = false;

        [Header("=== ÉVOLUTION DE LA VIE ===")]
        [Tooltip("Ce biome évolue-t-il avec les étapes de vie ?")]
        public bool evolvesWithLife = true;

        [Tooltip("Sensibilité aux changements évolutifs")]
        [Range(0f, 1f)]
        public float evolutionSensitivity = 0.5f;

        [Tooltip("Priorité d'apparition de la vie (0=dernier, 1=premier)")]
        [Range(0f, 1f)]
        public float lifePriority = 0.5f;

        [Header("=== PARAMÈTRES ÉVOLUTIFS ===")]
        [Tooltip("Étape de vie minimale pour ce biome")]
        public LifeStage minimumLifeStage = LifeStage.None;

        [Tooltip("Diversité biologique maximale supportée")]
        [Range(0f, 1f)]
        public float maxBiodiversity = 1f;

        [Tooltip("Vitesse d'évolution dans ce biome")]
        [Range(0.1f, 3f)]
        public float evolutionRate = 1f;

        [Header("=== VISUAL & DEBUG ===")]
        [Tooltip("Couleur pour le debug et la visualisation")]
        public Color debugColor = Color.white;

        [Tooltip("Matériau pour le rendu (optionnel)")]
        public Material biomeMaterial;

        [Header("=== RESSOURCES (futur) ===")]
        [Tooltip("Ressources disponibles dans ce biome")]
        [Range(0f, 1f)]
        public float resourceAbundance = 0.5f;

        [Tooltip("Types de ressources dominants")]
        public string[] dominantResources = new string[0];

        /// <summary>
        /// Constructeur avec valeurs par défaut
        /// </summary>
        public BiomeConfiguration()
        {
            // Valeurs par défaut raisonnables
            biomeType = BiomeType.Plains;
            minHeight = 0f;
            maxHeight = 1f;
            humidity = 0.5f;
            temperatureModifier = 0f;
            canSupportLife = true;
            isWaterBiome = false;
            evolvesWithLife = true;
            evolutionSensitivity = 0.5f;
            lifePriority = 0.5f;
            minimumLifeStage = LifeStage.None;
            maxBiodiversity = 1f;
            evolutionRate = 1f;
            debugColor = Color.white;
            resourceAbundance = 0.5f;
            dominantResources = new string[0];
        }

        /// <summary>
        /// Constructeur pour biome simple basé sur hauteur
        /// </summary>
        public BiomeConfiguration(BiomeType type, float minH, float maxH, Color color)
        {
            biomeType = type;
            minHeight = minH;
            maxHeight = maxH;
            debugColor = color;

            // Utiliser les extensions pour définir les propriétés automatiquement
            isWaterBiome = type.IsAquatic();
            canSupportLife = type.CanSupportComplexLife();
            lifePriority = type.GetEvolutionPriority();

            // Valeurs par défaut pour le reste
            humidity = isWaterBiome ? 1f : 0.5f;
            temperatureModifier = CalculateTemperatureModifier();
            evolvesWithLife = canSupportLife;
            evolutionSensitivity = canSupportLife ? 0.7f : 0.1f;
            minimumLifeStage = DetermineMinimumLifeStage();
            maxBiodiversity = CalculateMaxBiodiversity();
            evolutionRate = 1f;
            resourceAbundance = 0.5f;
            dominantResources = new string[0];
        }

        /// <summary>
        /// Vérifie si une hauteur donnée correspond à ce biome
        /// </summary>
        public bool ContainsHeight(float height)
        {
            return height >= minHeight && height <= maxHeight;
        }

        /// <summary>
        /// Calcule la "compatibilité" de ce biome avec une hauteur donnée (0-1)
        /// </summary>
        public float GetHeightCompatibility(float height)
        {
            if (!ContainsHeight(height))
                return 0f;

            // Plus on est près du centre de la plage, plus la compatibilité est élevée
            float center = (minHeight + maxHeight) / 2f;
            float range = maxHeight - minHeight;

            if (range < 0.001f) return 1f; // Éviter division par zéro

            float distanceFromCenter = Mathf.Abs(height - center);
            float normalizedDistance = distanceFromCenter / (range / 2f);

            return 1f - Mathf.Clamp01(normalizedDistance);
        }

        /// <summary>
        /// Obtient le nom d'affichage du biome
        /// </summary>
        public string GetDisplayName()
        {
            return biomeType.GetDisplayName();
        }

        /// <summary>
        /// Vérifie si ce biome peut évoluer vers un autre biome
        /// </summary>
        public bool CanEvolveTo(BiomeType targetBiome, LifeStage currentLifeStage)
        {
            if (!evolvesWithLife) return false;

            // Logique d'évolution : par exemple, Plains peut devenir Forest
            switch (biomeType)
            {
                case BiomeType.Plains:
                    return targetBiome == BiomeType.Forest || targetBiome == BiomeType.Grassland;

                case BiomeType.Lowlands:
                    return targetBiome == BiomeType.Wetlands || targetBiome == BiomeType.Plains;

                case BiomeType.Beach:
                    return targetBiome == BiomeType.Wetlands;

                case BiomeType.Hills:
                    return targetBiome == BiomeType.Forest;

                default:
                    return false;
            }
        }

        // === MÉTHODES PRIVÉES DE CALCUL ===

        private float CalculateTemperatureModifier()
        {
            // Calculer modification de température selon le type et l'altitude
            switch (biomeType)
            {
                case BiomeType.DeepOcean:
                case BiomeType.ShallowOcean:
                    return 0f; // Océans = température stable

                case BiomeType.Beach:
                case BiomeType.Lowlands:
                    return 2f; // Plus chaud (proche niveau mer)

                case BiomeType.Hills:
                    return -2f; // Légèrement plus frais

                case BiomeType.Mountains:
                    return -5f; // Plus frais en altitude

                case BiomeType.HighMountains:
                    return -10f; // Froid en haute altitude

                case BiomeType.Peaks:
                    return -15f; // Très froid aux sommets

                case BiomeType.Volcanic:
                    return 20f; // Très chaud près des volcans

                default:
                    return 0f;
            }
        }

        private LifeStage DetermineMinimumLifeStage()
        {
            // Déterminer l'étape de vie minimale selon le biome
            switch (biomeType)
            {
                case BiomeType.DeepOcean:
                case BiomeType.ShallowOcean:
                    return LifeStage.Microbial; // Premiers à avoir de la vie

                case BiomeType.Beach:
                case BiomeType.Wetlands:
                    return LifeStage.Simple; // Transition terre-mer

                case BiomeType.Lowlands:
                case BiomeType.Plains:
                    return LifeStage.Simple; // Colonisation terrestre

                case BiomeType.Forest:
                case BiomeType.Jungle:
                    return LifeStage.Complex; // Écosystèmes complexes

                case BiomeType.Mountains:
                case BiomeType.Desert:
                    return LifeStage.Complex; // Environnements difficiles

                case BiomeType.HighMountains:
                case BiomeType.Peaks:
                    return LifeStage.Intelligent; // Extrêmes, colonisation tardive

                default:
                    return LifeStage.Simple;
            }
        }

        private float CalculateMaxBiodiversity()
        {
            // Calculer la biodiversité maximale selon le biome
            switch (biomeType)
            {
                case BiomeType.Jungle:
                    return 1f; // Biodiversité maximale

                case BiomeType.Forest:
                case BiomeType.ShallowOcean:
                    return 0.9f; // Très haute biodiversité

                case BiomeType.Plains:
                case BiomeType.Grassland:
                case BiomeType.Wetlands:
                    return 0.8f; // Haute biodiversité

                case BiomeType.Hills:
                case BiomeType.Beach:
                    return 0.6f; // Biodiversité modérée

                case BiomeType.Mountains:
                case BiomeType.Desert:
                case BiomeType.Tundra:
                    return 0.4f; // Biodiversité limitée

                case BiomeType.HighMountains:
                case BiomeType.Volcanic:
                    return 0.2f; // Très peu de biodiversité

                case BiomeType.Peaks:
                case BiomeType.IceCap:
                    return 0.1f; // Biodiversité minimale

                default:
                    return 0.5f;
            }
        }

        /// <summary>
        /// Validation de la configuration
        /// </summary>
        public bool IsValid()
        {
            return minHeight >= 0f &&
                   maxHeight <= 1f &&
                   minHeight <= maxHeight &&
                   humidity >= 0f && humidity <= 1f &&
                   evolutionSensitivity >= 0f && evolutionSensitivity <= 1f;
        }

        /// <summary>
        /// Copie cette configuration
        /// </summary>
        public BiomeConfiguration Clone()
        {
            BiomeConfiguration clone = new BiomeConfiguration();

            clone.biomeType = biomeType;
            clone.minHeight = minHeight;
            clone.maxHeight = maxHeight;
            clone.humidity = humidity;
            clone.temperatureModifier = temperatureModifier;
            clone.canSupportLife = canSupportLife;
            clone.isWaterBiome = isWaterBiome;
            clone.evolvesWithLife = evolvesWithLife;
            clone.evolutionSensitivity = evolutionSensitivity;
            clone.lifePriority = lifePriority;
            clone.minimumLifeStage = minimumLifeStage;
            clone.maxBiodiversity = maxBiodiversity;
            clone.evolutionRate = evolutionRate;
            clone.debugColor = debugColor;
            clone.biomeMaterial = biomeMaterial;
            clone.resourceAbundance = resourceAbundance;
            clone.dominantResources = (string[])dominantResources.Clone();

            return clone;
        }
    }

    /// <summary>
    /// Collection de configurations de biomes pour différents types de planètes
    /// </summary>
    [System.Serializable]
    public class BiomeConfigurationSet
    {
        [Header("Informations")]
        public string setName = "Configuration par défaut";
        public string description = "";

        [Header("Configurations")]
        public BiomeConfiguration[] biomeConfigurations;

        /// <summary>
        /// Obtient la configuration d'un biome spécifique
        /// </summary>
        public BiomeConfiguration GetConfiguration(BiomeType biome)
        {
            foreach (var config in biomeConfigurations)
            {
                if (config.biomeType == biome)
                    return config;
            }
            return null;
        }

        /// <summary>
        /// Vérifie si toutes les configurations sont valides
        /// </summary>
        public bool IsValid()
        {
            if (biomeConfigurations == null || biomeConfigurations.Length == 0)
                return false;

            foreach (var config in biomeConfigurations)
            {
                if (!config.IsValid())
                    return false;
            }

            return true;
        }
    }
}