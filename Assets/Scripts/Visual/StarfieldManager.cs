// Nouveau script : StarfieldManager.cs
using UnityEngine;

namespace LifeStory.Visual
{
    public class StarfieldManager : MonoBehaviour
    {
        [Header("Starfield Settings")]
        [SerializeField] private int numberOfStars = 1000;
        [SerializeField] private float starfieldRadius = 100f;
        [SerializeField] private Material starMaterial;
        [SerializeField] private float minStarSize = 0.1f;
        [SerializeField] private float maxStarSize = 0.3f;

        [Header("Star Colors")]
        [SerializeField]
        private Color[] starColors = {
            Color.white,
            new Color(1f, 0.9f, 0.8f, 1f),    // Légèrement orange
            new Color(0.8f, 0.9f, 1f, 1f),    // Légèrement bleu
            new Color(1f, 0.8f, 0.6f, 1f),    // Orange
            new Color(0.9f, 0.9f, 1f, 1f)     // Bleu pâle
        };

        [Header("Animation")]
        [SerializeField] private bool animateStars = true;
        //[SerializeField] private float twinkleSpeed = 2f;
        [SerializeField] private float twinkleAmount = 0.3f;

        private GameObject starfieldParent;
        private ParticleSystem starParticles;

        private void Start()
        {
            CreateStarfield();
        }

        private void CreateStarfield()
        {
            // Créer un parent pour organiser les étoiles
            starfieldParent = new GameObject("Starfield");
            starfieldParent.transform.SetParent(transform);
            starfieldParent.transform.localPosition = Vector3.zero;

            // Méthode 1: Système de particules (plus performant)
            CreateParticleStarfield();

            // Méthode 2: GameObjects individuels (commentée pour performance)
            // CreateGameObjectStarfield();
        }

        private void CreateParticleStarfield()
        {
            // Créer le système de particules
            GameObject particleObj = new GameObject("Star Particles");
            particleObj.transform.SetParent(starfieldParent.transform);
            particleObj.transform.localPosition = Vector3.zero;

            starParticles = particleObj.AddComponent<ParticleSystem>();
            var main = starParticles.main;
            var emission = starParticles.emission;
            var shape = starParticles.shape;
            var velocityOverLifetime = starParticles.velocityOverLifetime;
            var sizeOverLifetime = starParticles.sizeOverLifetime;

            // Configuration principale
            main.startLifetime = Mathf.Infinity; // Étoiles permanentes
            main.startSpeed = 0f; // Pas de mouvement
            main.startSize3D = true;
            main.startSizeX = Random.Range(minStarSize, maxStarSize);
            main.startSizeY = main.startSizeX;
            main.startSizeZ = main.startSizeX;
            main.startColor = starColors[Random.Range(0, starColors.Length)];
            main.maxParticles = numberOfStars;

            // Émission
            emission.rateOverTime = 0f; // Pas d'émission continue
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, numberOfStars)
            });

            // Forme sphérique
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = starfieldRadius;
            shape.radiusThickness = 0f; // Surface seulement

            // Désactiver la vélocité
            velocityOverLifetime.enabled = false;

            // Animation scintillement (optionnel)
            if (animateStars)
            {
                sizeOverLifetime.enabled = true;
                AnimationCurve twinkleCurve = new AnimationCurve();
                twinkleCurve.AddKey(0f, 1f);
                twinkleCurve.AddKey(0.5f, 1f + twinkleAmount);
                twinkleCurve.AddKey(1f, 1f);
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, twinkleCurve);
            }

            // Matériau pour les étoiles
            var renderer = starParticles.GetComponent<ParticleSystemRenderer>();
            if (starMaterial != null)
            {
                renderer.material = starMaterial;
            }
            else
            {
                // Créer un matériau par défaut
                CreateDefaultStarMaterial(renderer);
            }

            //Debug.Log($"Starfield créé avec {numberOfStars} étoiles");
        }

        private void CreateDefaultStarMaterial()
        {
            // Créer un matériau simple pour les étoiles
            starMaterial = new Material(Shader.Find("Sprites/Default"));
            starMaterial.name = "Star Material";
            starMaterial.color = Color.white;

            // Créer une texture simple d'étoile (point blanc)
            Texture2D starTexture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];

            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                    float alpha = Mathf.Clamp01(1f - (distance / 16f));
                    pixels[y * 32 + x] = new Color(1f, 1f, 1f, alpha * alpha);
                }
            }

            starTexture.SetPixels(pixels);
            starTexture.Apply();
            starMaterial.mainTexture = starTexture;
        }

        private void CreateDefaultStarMaterial(ParticleSystemRenderer renderer)
        {
            CreateDefaultStarMaterial();
            renderer.material = starMaterial;
        }

        // Méthode alternative avec GameObjects (plus coûteuse)
        private void CreateGameObjectStarfield()
        {
            for (int i = 0; i < numberOfStars; i++)
            {
                // Position aléatoire sur une sphère
                Vector3 direction = Random.onUnitSphere;
                Vector3 position = direction * starfieldRadius;

                // Créer l'étoile
                GameObject star = GameObject.CreatePrimitive(PrimitiveType.Quad);
                star.name = $"Star_{i}";
                star.transform.SetParent(starfieldParent.transform);
                star.transform.position = position;
                star.transform.LookAt(Vector3.zero); // Regarder vers le centre

                // Taille aléatoire
                float size = Random.Range(minStarSize, maxStarSize);
                star.transform.localScale = Vector3.one * size;

                // Couleur aléatoire
                Color starColor = starColors[Random.Range(0, starColors.Length)];
                var renderer = star.GetComponent<Renderer>();
                if (starMaterial != null)
                {
                    renderer.material = starMaterial;
                    renderer.material.color = starColor;
                }

                // Supprimer le collider
                if (star.GetComponent<Collider>())
                    DestroyImmediate(star.GetComponent<Collider>());
            }
        }

        // Méthodes publiques
        public void RegenerateStarfield()
        {
            if (starfieldParent != null)
            {
                DestroyImmediate(starfieldParent);
            }
            CreateStarfield();
        }

        public void SetStarCount(int count)
        {
            numberOfStars = Mathf.Max(100, count);
            RegenerateStarfield();
        }

        private void OnGUI()
        {
            return;
            // Debug optionnel
            if (starfieldParent != null)
            {
                GUI.Box(new Rect(Screen.width - 200, Screen.height - 160, 180, 40), "");
                GUI.Label(new Rect(Screen.width - 190, Screen.height - 145, 160, 20), $"Étoiles: {numberOfStars}");
            }
        }
    }
}