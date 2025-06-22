// PlanetaryGravitySettings.cs - Configuration simple pour la gravité planétaire
using UnityEngine;

namespace LifeStory.Physics
{
    [System.Serializable]
    public class PlanetaryGravitySettings
    {
        [Header("Gravité de Base")]
        [Range(5f, 50f)]
        public float gravityStrength = 15f;            // Force de gravité (plus fort que Terre)

        [Range(1f, 20f)]
        public float planetRadius = 5f;                // Rayon de ta planète

        public Vector3 planetCenter = Vector3.zero;    // Centre de la planète

        [Header("Effets Physiques")]
        [Range(0f, 1f)]
        public float airResistance = 0.2f;             // Ralentissement dans l'air

        [Range(0f, 1f)]
        public float bounciness = 0.1f;                // Rebond au contact surface

        [Header("Optimisation")]
        [Range(20f, 100f)]
        public float maxSimulationDistance = 20f;      // Distance max de calcul

        public bool enableCollisionWithSurface = true; // Collision avec la planète

        [Header("Debug")]
        public bool showDebugGizmos = false;           // Afficher les guides visuels
        public bool enableDebugLogs = false;           // Messages de debug

        // Méthode utilitaire pour calculer la normale de surface
        public Vector3 GetSurfaceNormal(Vector3 position)
        {
            return (position - planetCenter).normalized;
        }

        // Méthode pour vérifier si une position est sur la surface
        public bool IsOnSurface(Vector3 position, float tolerance = 0.2f)
        {
            float distance = Vector3.Distance(position, planetCenter);
            return Mathf.Abs(distance - planetRadius) <= tolerance;
        }

        // Méthode pour calculer la force de gravité selon la distance
        public float GetGravityStrengthAtDistance(float distanceFromCenter)
        {
            if (distanceFromCenter <= planetRadius)
            {
                return gravityStrength;
            }

            // Gravité décroît avec le carré de la distance (loi physique)
            float distanceFactor = (planetRadius * planetRadius) / (distanceFromCenter * distanceFromCenter);
            return gravityStrength * distanceFactor;
        }
    }
}