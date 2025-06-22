// BiomeType.cs - Énumération des types de biomes
// Placé dans Scripts/Core/Enums/BiomeType.cs

namespace LifeStory.Core
{
    /// <summary>
    /// Types de biomes selon la hauteur et l'évolution
    /// Utilisé par tous les systèmes : biomes, vie, évolution, climat
    /// </summary>
    public enum BiomeType
    {
        // === BIOMES AQUATIQUES ===
        DeepOcean,      // Océan profond - Vie marine primitive
        ShallowOcean,   // Océan peu profond - Berceau de la vie

        // === BIOMES DE TRANSITION ===
        Beach,          // Plage/rivage - Zone de transition terre/mer
        Wetlands,       // Zones humides - Marécages (ajout futur)

        // === BIOMES TERRESTRES BAS ===
        Lowlands,       // Plaines basses - Premières colonisations terrestres
        Plains,         // Plaines moyennes - Expansion de la vie

        // === BIOMES TERRESTRES ÉLEVÉS ===
        Hills,          // Collines - Diversification
        Mountains,      // Montagnes - Adaptation altitude
        HighMountains,  // Hautes montagnes - Environnements extrêmes
        Peaks,          // Sommets - Limite de la vie

        // === BIOMES SPÉCIAUX (pour évolution future) ===
        Volcanic,       // Zones volcaniques actives
        Desert,         // Déserts - Adaptation aridité
        Forest,         // Forêts - Écosystèmes complexes
        Tundra,         // Toundra - Environnements froids
        IceCap,         // Calottes glaciaires

        // === BIOMES ÉVOLUTIFS (ajouts futurs) ===
        // Ces biomes apparaîtront avec l'évolution de la vie
        Grassland,      // Prairies - Avec l'évolution des herbivores
        Jungle,         // Jungle - Biodiversité maximale
        Swamp,          // Marécages - Écosystèmes amphibies

        // === VALEUR PAR DÉFAUT ===
        Unknown         // Type inconnu ou non défini
    }

    /// <summary>
    /// Extensions utilitaires pour BiomeType
    /// </summary>
    public static class BiomeTypeExtensions
    {
        /// <summary>
        /// Vérifie si le biome est aquatique
        /// </summary>
        public static bool IsAquatic(this BiomeType biome)
        {
            return biome == BiomeType.DeepOcean ||
                   biome == BiomeType.ShallowOcean ||
                   biome == BiomeType.Wetlands ||
                   biome == BiomeType.Swamp;
        }

        /// <summary>
        /// Vérifie si le biome peut supporter la vie complexe
        /// </summary>
        public static bool CanSupportComplexLife(this BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.HighMountains:
                case BiomeType.Peaks:
                case BiomeType.IceCap:
                case BiomeType.Volcanic:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Obtient la priorité d'évolution du biome (0 = dernier, 1 = premier)
        /// </summary>
        public static float GetEvolutionPriority(this BiomeType biome)
        {
            switch (biome)
            {
                // Océans - Berceau de la vie
                case BiomeType.ShallowOcean: return 1.0f;
                case BiomeType.DeepOcean: return 0.9f;

                // Zones de transition - Colonisation
                case BiomeType.Beach: return 0.8f;
                case BiomeType.Wetlands: return 0.7f;

                // Terres basses - Expansion
                case BiomeType.Lowlands: return 0.6f;
                case BiomeType.Plains: return 0.5f;

                // Terres moyennes - Diversification
                case BiomeType.Hills: return 0.4f;
                case BiomeType.Forest: return 0.4f;
                case BiomeType.Grassland: return 0.4f;

                // Environnements spécialisés
                case BiomeType.Desert: return 0.3f;
                case BiomeType.Jungle: return 0.3f;
                case BiomeType.Swamp: return 0.3f;

                // Montagnes - Adaptation
                case BiomeType.Mountains: return 0.2f;
                case BiomeType.Tundra: return 0.2f;

                // Extrêmes - Dernier
                case BiomeType.HighMountains: return 0.1f;
                case BiomeType.Volcanic: return 0.1f;
                case BiomeType.Peaks: return 0.05f;
                case BiomeType.IceCap: return 0.05f;

                default: return 0.5f;
            }
        }

        /// <summary>
        /// Obtient une description humaine du biome
        /// </summary>
        public static string GetDisplayName(this BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.DeepOcean: return "Océan Profond";
                case BiomeType.ShallowOcean: return "Océan Peu Profond";
                case BiomeType.Beach: return "Plage";
                case BiomeType.Wetlands: return "Zones Humides";
                case BiomeType.Lowlands: return "Plaines Basses";
                case BiomeType.Plains: return "Plaines";
                case BiomeType.Hills: return "Collines";
                case BiomeType.Mountains: return "Montagnes";
                case BiomeType.HighMountains: return "Hautes Montagnes";
                case BiomeType.Peaks: return "Sommets";
                case BiomeType.Volcanic: return "Zone Volcanique";
                case BiomeType.Desert: return "Désert";
                case BiomeType.Forest: return "Forêt";
                case BiomeType.Tundra: return "Toundra";
                case BiomeType.IceCap: return "Calotte Glaciaire";
                case BiomeType.Grassland: return "Prairie";
                case BiomeType.Jungle: return "Jungle";
                case BiomeType.Swamp: return "Marécage";
                default: return biome.ToString();
            }
        }

        /// <summary>
        /// Obtient la couleur de base du biome pour le debug
        /// </summary>
        public static UnityEngine.Color GetDebugColor(this BiomeType biome)
        {
            switch (biome)
            {
                // Océans - Bleus
                case BiomeType.DeepOcean: return new UnityEngine.Color(0.1f, 0.2f, 0.8f, 1f);
                case BiomeType.ShallowOcean: return new UnityEngine.Color(0.2f, 0.4f, 0.9f, 1f);

                // Transition - Beiges/bruns
                case BiomeType.Beach: return new UnityEngine.Color(0.9f, 0.8f, 0.6f, 1f);
                case BiomeType.Wetlands: return new UnityEngine.Color(0.5f, 0.7f, 0.5f, 1f);

                // Plaines - Verts
                case BiomeType.Lowlands: return new UnityEngine.Color(0.6f, 0.8f, 0.4f, 1f);
                case BiomeType.Plains: return new UnityEngine.Color(0.4f, 0.7f, 0.2f, 1f);
                case BiomeType.Grassland: return new UnityEngine.Color(0.5f, 0.8f, 0.3f, 1f);

                // Reliefs - Bruns/verts foncés
                case BiomeType.Hills: return new UnityEngine.Color(0.5f, 0.6f, 0.3f, 1f);
                case BiomeType.Mountains: return new UnityEngine.Color(0.6f, 0.5f, 0.4f, 1f);
                case BiomeType.HighMountains: return new UnityEngine.Color(0.7f, 0.7f, 0.6f, 1f);
                case BiomeType.Peaks: return new UnityEngine.Color(0.9f, 0.95f, 1f, 1f);

                // Forêts - Verts foncés
                case BiomeType.Forest: return new UnityEngine.Color(0.2f, 0.6f, 0.2f, 1f);
                case BiomeType.Jungle: return new UnityEngine.Color(0.1f, 0.5f, 0.1f, 1f);

                // Spéciaux
                case BiomeType.Desert: return new UnityEngine.Color(0.9f, 0.7f, 0.4f, 1f);
                case BiomeType.Volcanic: return new UnityEngine.Color(0.8f, 0.2f, 0.1f, 1f);
                case BiomeType.Tundra: return new UnityEngine.Color(0.8f, 0.8f, 0.7f, 1f);
                case BiomeType.IceCap: return new UnityEngine.Color(0.95f, 0.98f, 1f, 1f);
                case BiomeType.Swamp: return new UnityEngine.Color(0.4f, 0.5f, 0.3f, 1f);

                default: return UnityEngine.Color.gray;
            }
        }
    }
}