// PlanetaryGravitySystem.cs - Système principal de gravité planétaire
using UnityEngine;
using System.Collections.Generic;

namespace LifeStory.Physics
{
    public class PlanetaryGravitySystem : MonoBehaviour
    {
        [Header("Configuration")]
        public PlanetaryGravitySettings settings = new PlanetaryGravitySettings();

        [Header("Références")]
        public Transform planetTransform;              // Ta planète
        public ParticleSystem[] particleSystemsToAffect; // Systèmes à affecter par la gravité

        // Variables internes pour optimisation
        private Dictionary<ParticleSystem, ParticleSystem.Particle[]> particleBuffers;
        private Vector3 lastPlanetCenter;

        public static PlanetaryGravitySystem Instance { get; private set; }

        void Awake()
        {
            // Singleton simple
            if (Instance == null)
            {
                Instance = this;
                particleBuffers = new Dictionary<ParticleSystem, ParticleSystem.Particle[]>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            InitializeSystem();
        }

        void InitializeSystem()
        {
            // Trouver automatiquement la planète si pas assignée
            if (planetTransform == null)
            {
                var planetGenerator = FindAnyObjectByType<LifeStory.Generation.PlanetGenerator>();
                if (planetGenerator != null)
                {
                    planetTransform = planetGenerator.transform;
                    settings.planetRadius = planetGenerator.PlanetRadius;
                    //Debug.Log($"Planète trouvée automatiquement : {planetGenerator.name}");
                }
            }

            // Mettre à jour le centre
            if (planetTransform != null)
            {
                settings.planetCenter = planetTransform.position;
                lastPlanetCenter = settings.planetCenter;
            }

            // Trouver automatiquement les systèmes de particules volcaniques
            if (particleSystemsToAffect == null || particleSystemsToAffect.Length == 0)
            {
                FindVolcanicParticleSystems();
            }

            // Préparer les buffers pour chaque système
            PrepareParticleBuffers();

            //Debug.Log($"Gravité planétaire initialisée - {particleSystemsToAffect?.Length} systèmes trouvés");
        }

        void FindVolcanicParticleSystems()
        {
            var allParticleSystems = FindObjectsOfType<ParticleSystem>();
            var volcanicSystems = new List<ParticleSystem>();

            foreach (var ps in allParticleSystems)
            {
                // Chercher les systèmes de gouttes de lave
                if (ps.name.Contains("Lava Drops") || ps.name.Contains("Drops 3D"))
                {
                    volcanicSystems.Add(ps);

                   
                }
            }

            particleSystemsToAffect = volcanicSystems.ToArray();
        }

        void PrepareParticleBuffers()
        {
            particleBuffers.Clear();

            foreach (var ps in particleSystemsToAffect)
            {
                if (ps != null)
                {
                    int maxParticles = ps.main.maxParticles;
                    particleBuffers[ps] = new ParticleSystem.Particle[maxParticles];
                }
            }
        }

        void Update()
        {
            // Vérifier si la planète a bougé
            if (planetTransform != null && planetTransform.position != lastPlanetCenter)
            {
                settings.planetCenter = planetTransform.position;
                lastPlanetCenter = settings.planetCenter;
            }

            // Appliquer la gravité à tous les systèmes
            ApplyGravityToAllSystems();
        }

        void ApplyGravityToAllSystems()
        {
            foreach (var particleSystem in particleSystemsToAffect)
            {
                if (particleSystem != null && particleSystem.isPlaying)
                {
                    ApplyGravityToParticleSystem(particleSystem);
                }
            }
        }

        void ApplyGravityToParticleSystem(ParticleSystem ps)
        {
            // Récupérer le buffer pour ce système
            if (!particleBuffers.ContainsKey(ps))
                return;

            var particles = particleBuffers[ps];
            int particleCount = ps.GetParticles(particles);

            if (particleCount == 0)
                return;

            bool hasChanges = false;

            // Traiter chaque particule
            for (int i = 0; i < particleCount; i++)
            {
                if (ProcessSingleParticle(ref particles[i], ps))
                {
                    hasChanges = true;
                }
            }

            // Remettre les particules modifiées dans le système
            if (hasChanges)
            {
                ps.SetParticles(particles, particleCount);
            }
        }

        bool ProcessSingleParticle(ref ParticleSystem.Particle particle, ParticleSystem ps)
        {
            // Convertir position locale en position mondiale
            Vector3 worldPosition = ps.transform.TransformPoint(particle.position);
            float distanceFromCenter = Vector3.Distance(worldPosition, settings.planetCenter);

            // Ignorer si trop loin (optimisation)
            if (distanceFromCenter > settings.maxSimulationDistance)
                return false;

            // Calculer la direction de gravité (vers le centre)
            Vector3 gravityDirection = (settings.planetCenter - worldPosition).normalized;

            // Calculer la force selon la distance
            float gravityForce = settings.GetGravityStrengthAtDistance(distanceFromCenter);

            // Appliquer la gravité
            Vector3 gravityAcceleration = gravityDirection * gravityForce * Time.deltaTime;
            Vector3 localGravityAcceleration = ps.transform.InverseTransformDirection(gravityAcceleration);
            particle.velocity += localGravityAcceleration;

            // Appliquer la résistance de l'air
            if (settings.airResistance > 0f)
            {
                particle.velocity *= (1f - settings.airResistance * Time.deltaTime);
            }

            // Vérifier collision avec la surface
            if (settings.enableCollisionWithSurface)
            {
                HandleSurfaceCollision(ref particle, ps, worldPosition, distanceFromCenter);
            }

            return true; // Particule modifiée
        }

        void HandleSurfaceCollision(ref ParticleSystem.Particle particle, ParticleSystem ps, Vector3 worldPos, float distanceFromCenter)
        {
            // Vérifier si la particule touche la surface
            if (distanceFromCenter <= settings.planetRadius + 0.1f)
            {
                // Calculer la normale de surface
                Vector3 surfaceNormal = settings.GetSurfaceNormal(worldPos);

                // Calculer la vitesse mondiale
                Vector3 worldVelocity = ps.transform.TransformDirection(particle.velocity);

                // Composante de vitesse vers la surface
                float velocityTowardsSurface = Vector3.Dot(worldVelocity, -surfaceNormal);

                if (velocityTowardsSurface > 0) // Se dirige vers la surface
                {
                    // Calculer le rebond
                    Vector3 reflectedVelocity = worldVelocity + 2f * velocityTowardsSurface * surfaceNormal;
                    reflectedVelocity *= settings.bounciness;

                    // Reconvertir en vitesse locale
                    particle.velocity = ps.transform.InverseTransformDirection(reflectedVelocity);

                    // Repositionner au-dessus de la surface
                    Vector3 correctedWorldPos = settings.planetCenter + surfaceNormal * (settings.planetRadius + 0.2f);
                    particle.position = ps.transform.InverseTransformPoint(correctedWorldPos);

                    // Réduire la durée de vie
                    particle.remainingLifetime *= 0.8f;

                    
                }
            }
        }

        // Méthode publique pour orienter un volcan
        public void OrientVolcanoToSurface(Transform volcanoTransform, Vector3 volcanoWorldPosition)
        {
            Vector3 surfaceNormal = settings.GetSurfaceNormal(volcanoWorldPosition);
            volcanoTransform.up = surfaceNormal;

            
        }

        // Méthode pour ajouter un nouveau système de particules
        public void AddParticleSystem(ParticleSystem ps)
        {
            if (ps == null) return;

            // Ajouter au tableau
            var newArray = new ParticleSystem[particleSystemsToAffect.Length + 1];
            particleSystemsToAffect.CopyTo(newArray, 0);
            newArray[particleSystemsToAffect.Length] = ps;
            particleSystemsToAffect = newArray;

            // Préparer le buffer
            particleBuffers[ps] = new ParticleSystem.Particle[ps.main.maxParticles];

            //Debug.Log($"Système de particules ajouté à la gravité : {ps.name}");
        }

        // Méthodes de debug et test
        [ContextMenu("Test Gravité")]
        public void TestGravity()
        {
            //Debug.Log($"=== TEST GRAVITÉ PLANÉTAIRE ===");
            //Debug.Log($"Centre planète : {settings.planetCenter}");
            //Debug.Log($"Rayon planète : {settings.planetRadius}");
            //Debug.Log($"Force gravité : {settings.gravityStrength}");
            //Debug.Log($"Systèmes affectés : {particleSystemsToAffect?.Length}");
        }

        [ContextMenu("Rechercher Systèmes Volcaniques")]
        public void RefreshVolcanicSystems()
        {
            FindVolcanicParticleSystems();
            PrepareParticleBuffers();
            //Debug.Log("Systèmes volcaniques rafraîchis");
        }

        void OnDrawGizmos()
        {
            if (!settings.showDebugGizmos) return;

            // Dessiner la planète
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(settings.planetCenter, settings.planetRadius);

            // Dessiner la zone de simulation
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(settings.planetCenter, settings.maxSimulationDistance);

            // Dessiner les directions de gravité pour chaque système
            if (particleSystemsToAffect != null)
            {
                Gizmos.color = Color.red;
                foreach (var ps in particleSystemsToAffect)
                {
                    if (ps != null)
                    {
                        Vector3 psPos = ps.transform.position;
                        Vector3 gravityDir = (settings.planetCenter - psPos).normalized;
                        Gizmos.DrawLine(psPos, psPos + gravityDir * 2f);
                    }
                }
            }
        }
    }
}