// GeologicalEvents.cs - Types partagés pour tous les systèmes géologiques
using UnityEngine;

namespace LifeStory.Geology
{
    // === ÉNUMÉRATIONS PARTAGÉES ===

    public enum FractureType
    {
        Coastal,        // Faille le long des côtes
        Rift,          // Rift qui divise le continent
        Transform,     // Glissement latéral
        Hotspot,       // Point chaud isolé
        Impact,        // Causé par impact de météore
        Volcanic,      // Causé par activité volcanique
        Tidal          // Causé par forces de marée
    }

    public enum EarthquakeSource
    {
        Tectonic,      // Mouvement naturel des plaques
        Volcanic,      // Éruption volcanique
        Impact,        // Impact de météore/astéroïde
        Artificial,    // Causé par l'activité humaine (plus tard)
        Unknown        // Source indéterminée
    }

    public enum GeologicalEventSeverity
    {
        Minor,         // 1-3 : Très localisé
        Moderate,      // 4-5 : Impact régional
        Major,         // 6-7 : Impact continental
        Catastrophic   // 8-10 : Impact planétaire
    }

    // === STRUCTURES PARTAGÉES ===

    [System.Serializable]
    public struct VulnerableZone
    {
        public Vector2Int mapPosition;
        public FractureType fractureType;
        public float stressLevel;          // 0-1, plus élevé = plus susceptible
        public float coastalDistance;     // Distance à la côte
        public bool isProcessed;           // Déjà fracturé
        public EarthquakeSource lastTriggerSource; // Dernière cause
        public Color debugColor;

        // Métadonnées pour autres systèmes
        public float lastEventTime;       // Timestamp du dernier événement
        public int eventCount;             // Nombre d'événements subis
        public float cumulativeDamage;     // Dommages accumulés
    }

    [System.Serializable]
    public struct EarthquakeEvent
    {
        public Vector2Int epicenter;
        public FractureType type;
        public EarthquakeSource source;    // QUI a causé le tremblement
        public float magnitude;            // 1-10 (échelle modifiée pour le jeu)
        public float fractureRadius;      // Rayon d'impact
        public float fractureDepth;       // Profondeur de la fracture
        public bool successful;           // Fracture créée avec succès
        public float timestamp;           // Quand c'est arrivé
        public string sourceDescription;  // Description de la source

        // Données pour autres systèmes
        public bool triggersVolcanicActivity; // Peut déclencher volcans
        public bool triggersTsunami;          // Peut déclencher tsunami
        public Vector3 worldPosition;         // Position 3D sur la planète
    }

    [System.Serializable]
    public struct GeologicalImpact
    {
        public Vector2Int impactCenter;
        public float impactRadius;
        public float impactForce;          // Force de l'impact
        public EarthquakeSource sourceType;
        public GeologicalEventSeverity severity;
        public bool createsCrater;         // Crée un cratère
        public bool triggersEarthquakes;   // Déclenche des tremblements
        public bool triggersVolcanicActivity; // Déclenche activité volcanique

        // Spécifique aux météores
        public float meteoriteSize;        // Taille du météore (si applicable)
        public float meteoriteSpeed;       // Vitesse d'impact
        public float meteoriteAngle;       // Angle d'impact
    }

    // === ÉVÉNEMENTS SYSTÈME (pour communication inter-systèmes) ===

    public static class GeologicalEventManager
    {
        // Événements pour communication entre systèmes
        public static System.Action<EarthquakeEvent> OnEarthquakeTriggered;
        public static System.Action<GeologicalImpact> OnGeologicalImpact;
        public static System.Action<VulnerableZone> OnVulnerableZoneIdentified;
        public static System.Action<Vector2Int, float> OnTerrainDeformation;

        // Méthodes utilitaires pour autres systèmes
        public static void TriggerEarthquakeFromExternal(EarthquakeSource source, Vector3 worldPos, float magnitude, string description)
        {
            // Permet aux autres systèmes de déclencher des tremblements de terre
            var earthquake = new EarthquakeEvent
            {
                source = source,
                magnitude = magnitude,
                sourceDescription = description,
                worldPosition = worldPos,
                timestamp = Time.time,
                // Les autres champs seront remplis par EarthquakeSystem
            };

            OnEarthquakeTriggered?.Invoke(earthquake);
        }

        public static void RegisterGeologicalImpact(GeologicalImpact impact)
        {
            // Permet aux autres systèmes d'enregistrer des impacts
            OnGeologicalImpact?.Invoke(impact);
        }

        public static GeologicalEventSeverity CalculateSeverity(float magnitude)
        {
            if (magnitude < 3f) return GeologicalEventSeverity.Minor;
            if (magnitude < 5f) return GeologicalEventSeverity.Moderate;
            if (magnitude < 7f) return GeologicalEventSeverity.Major;
            return GeologicalEventSeverity.Catastrophic;
        }

        public static Color GetSeverityColor(GeologicalEventSeverity severity)
        {
            switch (severity)
            {
                case GeologicalEventSeverity.Minor: return Color.green;
                case GeologicalEventSeverity.Moderate: return Color.yellow;
                case GeologicalEventSeverity.Major: return Color.red;
                case GeologicalEventSeverity.Catastrophic: return Color.magenta;
                default: return Color.white;
            }
        }

        public static Color GetFractureTypeColor(FractureType type)
        {
            switch (type)
            {
                case FractureType.Coastal: return Color.red;
                case FractureType.Rift: return Color.yellow;
                case FractureType.Transform: return Color.blue;
                case FractureType.Hotspot: return Color.magenta;
                case FractureType.Impact: return Color.cyan;
                case FractureType.Volcanic: return new Color(1f, 0.5f, 0f); // Orange
                case FractureType.Tidal: return new Color(0f, 0.8f, 0.8f);  // Cyan foncé
                default: return Color.white;
            }
        }
    }

    // === INTERFACES POUR SYSTÈMES EXTERNES ===

    public interface IGeologicalEventTrigger
    {
        // Interface que doivent implémenter les systèmes qui peuvent déclencher des événements géologiques
        void TriggerGeologicalEvent(GeologicalImpact impact);
        bool CanTriggerEarthquakes { get; }
        EarthquakeSource GetEventSource();
    }

    public interface IGeologicalEventReceiver
    {
        // Interface que doivent implémenter les systèmes qui réagissent aux événements géologiques
        void OnEarthquakeReceived(EarthquakeEvent earthquake);
        void OnGeologicalImpactReceived(GeologicalImpact impact);
        bool IsAffectedByGeologicalEvents { get; }
    }

    // === CONFIGURATIONS PRÉDÉFINIES ===

    [System.Serializable]
    public static class GeologicalEventPresets
    {
        // Presets pour différents types d'événements

        public static GeologicalImpact CreateMeteorImpact(Vector3 worldPos, float meteorSize)
        {
            float impactForce = meteorSize * meteorSize * 100f; // Force selon taille
            float radius = meteorSize * 5f;

            return new GeologicalImpact
            {
                sourceType = EarthquakeSource.Impact,
                severity = GeologicalEventManager.CalculateSeverity(impactForce / 100f),
                impactRadius = radius,
                impactForce = impactForce,
                createsCrater = meteorSize > 0.5f,
                triggersEarthquakes = meteorSize > 0.3f,
                triggersVolcanicActivity = meteorSize > 1f,
                meteoriteSize = meteorSize
            };
        }

        public static EarthquakeEvent CreateVolcanicEarthquake(Vector3 volcanoPos, float eruptionIntensity)
        {
            return new EarthquakeEvent
            {
                source = EarthquakeSource.Volcanic,
                magnitude = 2f + eruptionIntensity * 3f, // 2-5 pour volcanique
                sourceDescription = $"Éruption volcanique (intensité: {eruptionIntensity:F2})",
                worldPosition = volcanoPos,
                timestamp = Time.time,
                triggersVolcanicActivity = false, // N'en déclenche pas d'autres
                triggersTsunami = eruptionIntensity > 0.7f
            };
        }

        public static EarthquakeEvent CreateTectonicEarthquake(Vector2Int platePosition, FractureType fractureType)
        {
            float baseMagnitude = fractureType switch
            {
                FractureType.Coastal => 3f,
                FractureType.Rift => 5f,
                FractureType.Transform => 4f,
                FractureType.Hotspot => 3.5f,
                _ => 3f
            };

            return new EarthquakeEvent
            {
                source = EarthquakeSource.Tectonic,
                type = fractureType,
                magnitude = baseMagnitude + Random.Range(-0.5f, 1f),
                sourceDescription = $"Activité tectonique {fractureType}",
                timestamp = Time.time,
                triggersVolcanicActivity = fractureType == FractureType.Rift,
                triggersTsunami = fractureType == FractureType.Coastal
            };
        }
    }
}