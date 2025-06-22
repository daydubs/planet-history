// VolcanicDeformationProfiles.cs - Système de déformation paramétrable et extensible
using UnityEngine;
using static LifeStory.Volcanoes.CleanVolcanicSystem;

namespace LifeStory.Geology
{
    /// <summary>
    /// Profil de déformation volcanique configurable
    /// Permet de créer des morphologies très différentes avec des paramètres
    /// </summary>
    [System.Serializable]
    public class VolcanicDeformationProfile
    {
        [Header("📏 Géométrie")]
        [Tooltip("Multiplicateur du rayon de base")]
        [Range(0.5f, 5f)]
        public float radiusMultiplier = 1f;

        [Tooltip("Ratio largeur/longueur (1 = circulaire, <1 = allongé)")]
        [Range(0.1f, 1f)]
        public float aspectRatio = 1f;

        [Tooltip("Rotation aléatoire possible")]
        public bool randomOrientation = false;

        [Header("💪 Intensité")]
        [Tooltip("Multiplicateur de force de base")]
        [Range(0.1f, 3f)]
        public float strengthMultiplier = 1f;

        [Tooltip("Évolution de la force avec les éruptions")]
        [Range(0f, 1f)]
        public float accumulationRate = 0.25f;

        [Header("📈 Profil de Hauteur")]
        [Tooltip("Courbe de déformation radiale (0=centre, 1=bord)")]
        public AnimationCurve heightProfile = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Tooltip("Zone de plateau central (0-1)")]
        [Range(0f, 0.8f)]
        public float plateauSize = 0f;

        [Tooltip("Intensité du plateau")]
        [Range(0.5f, 1f)]
        public float plateauIntensity = 1f;

        [Header("🎯 Zones d'Influence")]
        [Tooltip("Zone principale (multiplicateur rayon)")]
        [Range(0.3f, 1f)]
        public float primaryZone = 1f;

        [Tooltip("Zone secondaire étendue")]
        [Range(1f, 2f)]
        public float secondaryZone = 1f;

        [Tooltip("Force de la zone secondaire")]
        [Range(0f, 0.5f)]
        public float secondaryStrength = 0f;

        [Header("🌋 Spécialisation")]
        [Tooltip("Déformation linéaire au lieu de radiale")]
        public bool linearDeformation = false;

        [Tooltip("Crée des fissures multiples")]
        public bool multipleFissures = false;

        [Range(1, 5)]
        public int fissureCount = 1;

        [Tooltip("Variation d'angle entre fissures")]
        [Range(0f, 45f)]
        public float fissureAngleVariation = 0f;
    }

    /// <summary>
    /// Extension de VolcanoTypeData pour inclure le profil de déformation
    /// </summary>
    public static class VolcanoTypeDataExtensions
    {
        /// <summary>
        /// Obtient le profil de déformation pour un type de volcan
        /// Si pas défini, génère un profil par défaut basé sur les paramètres actuels
        /// </summary>
        public static VolcanicDeformationProfile GetDeformationProfile(this VolcanoTypeData typeData)
        {
            // Si un profil personnalisé existe, l'utiliser
            if (typeData.customDeformationProfile != null)
                return typeData.customDeformationProfile;

            // Sinon, générer un profil basé sur les paramètres existants
            return GenerateDefaultProfile(typeData);
        }

        private static VolcanicDeformationProfile GenerateDefaultProfile(VolcanoTypeData typeData)
        {
            var profile = new VolcanicDeformationProfile();

            // Mapper les anciens paramètres vers le nouveau système
            profile.radiusMultiplier = typeData.deformationRadius;
            profile.strengthMultiplier = typeData.deformationStrength;

            // Heuristiques basées sur le type
            switch (typeData.type)
            {
                case VolcanoType.Shield:
                    profile.aspectRatio = 1f; // Circulaire
                    profile.plateauSize = 0.3f; // Large plateau
                    profile.heightProfile = AnimationCurve.EaseInOut(0, 1, 1, 0.1f); // Pente douce
                    profile.accumulationRate = 0.25f; // Accumulation modérée
                    break;

                case VolcanoType.Fissure:
                    profile.aspectRatio = 0.3f; // Très allongé
                    profile.randomOrientation = true;
                    profile.linearDeformation = true;
                    profile.heightProfile = AnimationCurve.EaseInOut(0, 1, 1, 0); // Plus abrupt
                    profile.accumulationRate = 0.4f; // Accumulation rapide
                    break;

                default:
                    // Profil neutre pour types inconnus
                    break;
            }

            return profile;
        }
    }

    /// <summary>
    /// Moteur de déformation générique configurable
    /// </summary>
    public static class ConfigurableVolcanicDeformation
    {
        /// <summary>
        /// Applique la déformation selon le profil configuré
        /// </summary>
        public static float[,] ApplyDeformation(Vector2Int center, SimpleVolcano volcano,
                                              int mapResolution, float baseRadius, float baseStrength,
                                              float planetRadius, VolcanicDeformationProfile profile)
        {
            float[,] deformationLayer = new float[mapResolution, mapResolution];

            // === CALCULS DE BASE ===
            float actualRadius = baseRadius * profile.radiusMultiplier;
            float actualStrength = baseStrength * profile.strengthMultiplier;

            // Évolution avec les éruptions
            float evolutionFactor = 1f + (volcano.eruptionsCompleted * profile.accumulationRate);
            actualStrength *= evolutionFactor;

            // === GESTION ORIENTATION ===
            float orientation = 0f;
            if (profile.randomOrientation)
                orientation = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            // === DÉFORMATION SELON LE TYPE ===
            if (profile.linearDeformation)
            {
                ApplyLinearDeformation(deformationLayer, center, mapResolution, actualRadius,
                                     actualStrength, volcano.intensity, profile, orientation, planetRadius);
            }
            else
            {
                ApplyRadialDeformation(deformationLayer, center, mapResolution, actualRadius,
                                     actualStrength, volcano.intensity, profile, planetRadius);
            }

            return deformationLayer;
        }

        private static void ApplyRadialDeformation(float[,] layer, Vector2Int center, int mapResolution,
                                                 float radius, float strength, float intensity,
                                                 VolcanicDeformationProfile profile, float planetRadius)
        {
            int radiusInt = Mathf.CeilToInt((radius / planetRadius) * mapResolution);

            // Zone secondaire élargie
            int secondaryRadiusInt = Mathf.CeilToInt(radiusInt * profile.secondaryZone);

            for (int x = center.x - secondaryRadiusInt; x <= center.x + secondaryRadiusInt; x++)
            {
                for (int y = center.y - secondaryRadiusInt; y <= center.y + secondaryRadiusInt; y++)
                {
                    if (x >= 0 && x < mapResolution && y >= 0 && y < mapResolution)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center.x, center.y));

                        // Zone primaire
                        if (distance <= radiusInt * profile.primaryZone)
                        {
                            float normalizedDist = distance / (radiusInt * profile.primaryZone);
                            float heightFactor = GetHeightFactor(normalizedDist, profile);
                            layer[x, y] = strength * heightFactor * intensity;
                        }
                        // Zone secondaire
                        else if (distance <= secondaryRadiusInt && profile.secondaryStrength > 0)
                        {
                            float secondaryNormalizedDist = (distance - radiusInt) / (secondaryRadiusInt - radiusInt);
                            float secondaryFactor = (1f - secondaryNormalizedDist) * profile.secondaryStrength;
                            layer[x, y] = strength * secondaryFactor * intensity;
                        }
                    }
                }
            }
        }

        private static void ApplyLinearDeformation(float[,] layer, Vector2Int center, int mapResolution,
                                                 float radius, float strength, float intensity,
                                                 VolcanicDeformationProfile profile, float orientation,
                                                 float planetRadius)
        {
            // Géométrie linéaire
            float length = radius;
            float width = radius * profile.aspectRatio;

            Vector2 directionAlong = new Vector2(Mathf.Cos(orientation), Mathf.Sin(orientation));
            Vector2 directionAcross = new Vector2(-directionAlong.y, directionAlong.x);

            int lengthInt = Mathf.CeilToInt((length / planetRadius) * mapResolution);
            int widthInt = Mathf.CeilToInt((width / planetRadius) * mapResolution);

            // Fissures multiples
            int fissureCount = profile.multipleFissures ? profile.fissureCount : 1;

            for (int f = 0; f < fissureCount; f++)
            {
                float fissureAngle = orientation;
                if (profile.multipleFissures && fissureCount > 1)
                {
                    float angleOffset = ((float)f / (fissureCount - 1) - 0.5f) * profile.fissureAngleVariation * Mathf.Deg2Rad;
                    fissureAngle += angleOffset;

                    directionAlong = new Vector2(Mathf.Cos(fissureAngle), Mathf.Sin(fissureAngle));
                    directionAcross = new Vector2(-directionAlong.y, directionAlong.x);
                }

                int maxRadius = Mathf.Max(lengthInt, widthInt);

                for (int x = center.x - maxRadius; x <= center.x + maxRadius; x++)
                {
                    for (int y = center.y - maxRadius; y <= center.y + maxRadius; y++)
                    {
                        if (x >= 0 && x < mapResolution && y >= 0 && y < mapResolution)
                        {
                            Vector2 fromCenter = new Vector2(x, y) - new Vector2(center.x, center.y);

                            float alongDistance = Mathf.Abs(Vector2.Dot(fromCenter, directionAlong));
                            float acrossDistance = Mathf.Abs(Vector2.Dot(fromCenter, directionAcross));

                            if (alongDistance <= lengthInt && acrossDistance <= widthInt)
                            {
                                float alongFactor = 1f - (alongDistance / lengthInt);
                                float acrossFactor = 1f - (acrossDistance / widthInt);

                                float heightFactor = GetHeightFactor(1f - (alongFactor * acrossFactor), profile);
                                float fissureStrength = strength * (profile.multipleFissures ? 0.7f : 1f); // Réduction si multiples

                                layer[x, y] = Mathf.Max(layer[x, y], fissureStrength * heightFactor * intensity);
                            }
                        }
                    }
                }
            }
        }

        private static float GetHeightFactor(float normalizedDistance, VolcanicDeformationProfile profile)
        {
            // Gestion du plateau central
            if (normalizedDistance <= profile.plateauSize)
            {
                return profile.plateauIntensity;
            }

            // Remapper la distance en excluant le plateau
            float adjustedDistance = (normalizedDistance - profile.plateauSize) / (1f - profile.plateauSize);

            // Appliquer la courbe de profil
            return profile.heightProfile.Evaluate(adjustedDistance);
        }
    }
}