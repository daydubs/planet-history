using LifeStory.Core;
using LifeStory.Generation;
using System.Collections;
using UnityEngine;

namespace Biome
{
    /// <summary>
    /// Version optimisée de SimplifiedBiomeSystem
    /// Garde uniquement ce qui est nécessaire pour les 2 shaders actuels
    /// </summary>
    public class SimplifiedBiomeSystem : MonoBehaviour
    {
        [Header("🎯 Configuration Essentielle")]
        [SerializeField] private bool enableSystem = true;
        [SerializeField] private bool enableDetailedLogs = false;

        [Header("🌋 Shader Thermique (PlanetLavaShader)")]
        [SerializeField] private Material planetLavaMaterial;

        [Header("🔥 Textures Lave Chaude (≥781°C)")]
        [SerializeField] private Texture2D hotLavaAlbedoTexture;
        [SerializeField] private Texture2D hotLavaNormalTexture;
        [SerializeField] private Texture2D hotLavaEmissionTexture;

        [Header("🌋 Textures Lave Froide (<781°C)")]
        [SerializeField] private Texture2D coldLavaAlbedoTexture;
        [SerializeField] private Texture2D coldLavaNormalTexture;
        [SerializeField] private Texture2D coldLavaEmissionTexture;

        [Header("🔧 Paramètres Shader Thermique")]
        [SerializeField] private Vector2 lavaTiling = new Vector2(3f, 3f);
        [SerializeField] private float blendTemperature = 781f;
        [SerializeField] private float blendRange = 50f;

        [Header("🌍 Shader Biomes (PlanetBiome3textures)")]
        [SerializeField] private Material biomesMaterial;

        [Header("🏔️ Textures Principales Biomes")]
        [SerializeField] private Texture2D oceanTexture;
        [SerializeField] private Texture2D shoreTexture;
        [SerializeField] private Texture2D plainsTexture;
        [SerializeField] private Texture2D hillsTexture;
        [SerializeField] private Texture2D mountainTexture;
        [SerializeField] private Texture2D snowTexture;

        [Header("📐 Textures Normales Biomes")]
        [SerializeField] private Texture2D oceanNormal;
        [SerializeField] private Texture2D shoreNormal;
        [SerializeField] private Texture2D plainsNormal;
        [SerializeField] private Texture2D hillsNormal;
        [SerializeField] private Texture2D mountainNormal;
        [SerializeField] private Texture2D snowNormal;

        [Header("🔧 Paramètres Biomes")]
        [SerializeField] private Vector2 biomeTiling = new Vector2(3f, 3f);

        [Header("🌡️ Paramètres Température")]
        [SerializeField] private float temperatureThreshold = 50f; // Seuil changement shader

        // === RÉFÉRENCES SYSTÈME ===
        private GameManager gameManager;
        private MeshRenderer planetRenderer;
        private MeshFilter planetMeshFilter;
        private Material activeMaterial;
        private bool isInitialized = false;

        // === SINGLETON ===
        public static SimplifiedBiomeSystem Instance { get; private set; }

        // ==================================================================================
        // 🎯 API PUBLIQUE
        // ==================================================================================

        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Point d'entrée principal - met à jour selon la température
        /// </summary>
        public void SetTemperature(float temperature)
        {
            if (!isInitialized || !enableSystem) return;

            if (temperature >= temperatureThreshold)
            {
                ApplyThermalShader(temperature);
            }
            else
            {
                ApplyBiomesShader();
            }
        }

        // ==================================================================================
        // 🔧 SYSTÈME CORE
        // ==================================================================================

        private void Awake()
        {
            // Singleton seulement
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LogDebug("Singleton créé - Démarrage initialisation...");
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Démarrer la coroutine d'initialisation
            StartCoroutine(InitializeSystemCoroutine());
        }

        /// <summary>
        /// Coroutine d'initialisation - attend que tous les systèmes soient prêts
        /// </summary>
        private IEnumerator InitializeSystemCoroutine()
        {
            LogDebug("🔄 Début initialisation coroutine...");

            // Attendre quelques frames pour que tout soit créé
            yield return new WaitForSeconds(2f);

            int attempts = 0;
            const int maxAttempts = 10;

            while (!isInitialized && attempts < maxAttempts)
            {
                attempts++;
                LogDebug($"Tentative d'initialisation {attempts}/{maxAttempts}");

                // Chercher GameManager via son instance
                if (GameManager.Instance != null)
                {
                    gameManager = GameManager.Instance;
                    GameManager.OnSurfaceTemperatureChanged += OnSurfaceTemperatureChanged;
                }

                // Chercher PlanetGenerator via son instance
                PlanetGenerator planetGenerator = PlanetGenerator.Instance;

                // Architecture spécifique : MeshRenderer ET MeshFilter sur PlanetGenerator
                if (planetGenerator != null)
                {
                    yield return new WaitUntil(() => planetGenerator.HeightMap != null);
                    
                    planetRenderer = planetGenerator.GetComponent<MeshRenderer>();
                    planetMeshFilter = planetGenerator.GetComponent<MeshFilter>();
                }

                // Fallback : si pas trouvé via instances, chercher dans la scène
                if (planetRenderer == null)
                {
                    GameObject planetObject = GameObject.Find("Planet");
                    if (planetObject == null)
                        planetObject = GameObject.Find("PlanetSphere");
                    if (planetObject == null)
                        planetObject = GameObject.Find("Sphere");

                    if (planetObject != null)
                    {
                        planetRenderer = planetObject.GetComponent<MeshRenderer>();
                        LogDebug($"Planète trouvée sur: {planetObject.name}");
                    }
                }

                // Vérifier si on a tout
                bool hasGameManager = gameManager != null;
                bool hasRenderer = planetRenderer != null;
                bool hasMeshFilter = planetMeshFilter != null;
                bool hasMaterial = hasRenderer && planetRenderer.material != null;

                LogDebug($"État: GameManager={hasGameManager}, Renderer={hasRenderer}, MeshFilter={hasMeshFilter}, Material={hasMaterial}");

                if (hasGameManager && hasRenderer && hasMeshFilter && hasMaterial)
                {
                    // Succès !
                    activeMaterial = planetRenderer.material;
                    isInitialized = true;
                    LogDebug("✅ Initialisation réussie !");
                    yield break;
                }

                // Attendre avant de réessayer
                yield return new WaitForSeconds(1f);
            }

            if (!isInitialized)
            {
                LogError($"❌ Échec initialisation après {maxAttempts} tentatives");
            }
        }

        // ==================================================================================
        // 🌋 SHADER THERMIQUE (≥50°C)
        // ==================================================================================

        /// <summary>
        /// Applique le shader thermique avec température
        /// </summary>
        private void ApplyThermalShader(float temperature)
        {
            // Vérification préalable AVANT toute comparaison
            if (planetLavaMaterial == null)
            {
                LogError("❌ planetLavaMaterial non assigné dans l'inspector !");
                return;
            }

            // Si pas le bon shader, l'activer et configurer les textures
            if (activeMaterial.shader != planetLavaMaterial.shader)
            {
                // Copier les propriétés du PlanetLavaShader
                activeMaterial.shader = planetLavaMaterial.shader;

                // Assigner les textures hot lava (primaires)
                if (hotLavaAlbedoTexture != null)
                    activeMaterial.SetTexture("_Albedo_Texture", hotLavaAlbedoTexture);

                if (hotLavaNormalTexture != null)
                    activeMaterial.SetTexture("_Normal_Texture", hotLavaNormalTexture);

                if (hotLavaEmissionTexture != null)
                    activeMaterial.SetTexture("_Emission_Texture", hotLavaEmissionTexture);

                // Assigner les textures cold lava (secondaires)
                if (coldLavaAlbedoTexture != null)
                    activeMaterial.SetTexture("_Secondary_Albedo", coldLavaAlbedoTexture);

                if (coldLavaNormalTexture != null)
                    activeMaterial.SetTexture("_Secondary_Normal", coldLavaNormalTexture);

                if (coldLavaEmissionTexture != null)
                    activeMaterial.SetTexture("_Secondary_Emission", coldLavaEmissionTexture);

                // Paramètres de blend et tuilage
                activeMaterial.SetFloat("_Blend_Temperature", blendTemperature);
                activeMaterial.SetFloat("_Blend_Range", blendRange);
                activeMaterial.SetVector("_Tiling", lavaTiling);

                LogDebug("🌋 PlanetLavaShader assigné et configuré");
            }

            // Mettre à jour la température dans le shader
            activeMaterial.SetFloat("_Temperature", temperature);

            //LogDebug($"🌋 Shader thermique - Temp: {temperature:F0}°C");
        }

        /// <summary>
        /// Configure le shader thermique - utilise directement le bon matériau
        /// </summary>
        private void SwitchToThermalShader()
        {
            // Plus besoin de test null ici - déjà fait dans ApplyThermalShader()
            LogDebug($"🔄 Changement vers matériau thermique...");
            LogDebug($"   Matériau actuel: {activeMaterial.shader.name}");
            LogDebug($"   Matériau cible: {planetLavaMaterial.shader.name}");

            // UTILISER DIRECTEMENT LE BON MATÉRIAU au lieu de changer le shader
            activeMaterial = planetLavaMaterial;
            planetRenderer.material = activeMaterial;

            // Configurer les textures sur le bon matériau
            ConfigureThermalTextures();

            LogDebug($"✅ Matériau thermique appliqué: {activeMaterial.shader.name}");
        }

        /// <summary>
        /// Configure les textures sur le matériau thermique
        /// </summary>
        private void ConfigureThermalTextures()
        {
            // SET 1 : Textures lave chaude (primaires)
            if (hotLavaAlbedoTexture != null)
            {
                activeMaterial.SetTexture("_Albedo_Texture", hotLavaAlbedoTexture);
                LogDebug($"✅ _Albedo_Texture assignée: {hotLavaAlbedoTexture.name}");
            }

            if (hotLavaNormalTexture != null)
            {
                activeMaterial.SetTexture("_Normal_Texture", hotLavaNormalTexture);
                LogDebug($"✅ _Normal_Texture assignée: {hotLavaNormalTexture.name}");
            }

            if (hotLavaEmissionTexture != null)
            {
                activeMaterial.SetTexture("_Emission_Texture", hotLavaEmissionTexture);
                LogDebug($"✅ _Emission_Texture assignée: {hotLavaEmissionTexture.name}");
            }

            // SET 2 : Textures lave froide (secondaires)  
            if (coldLavaAlbedoTexture != null)
            {
                activeMaterial.SetTexture("_Secondary_Albedo", coldLavaAlbedoTexture);
                LogDebug($"✅ _Secondary_Albedo assignée: {coldLavaAlbedoTexture.name}");
            }

            if (coldLavaNormalTexture != null)
            {
                activeMaterial.SetTexture("_Secondary_Normal", coldLavaNormalTexture);
                LogDebug($"✅ _Secondary_Normal assignée: {coldLavaNormalTexture.name}");
            }

            if (coldLavaEmissionTexture != null)
            {
                activeMaterial.SetTexture("_Secondary_Emission", coldLavaEmissionTexture);
                LogDebug($"✅ _Secondary_Emission assignée: {coldLavaEmissionTexture.name}");
            }

            // Paramètres de blend entre les deux sets
            activeMaterial.SetFloat("_Blend_Temperature", blendTemperature);
            activeMaterial.SetFloat("_Blend_Range", blendRange);

            // Paramètres de tuilage
            activeMaterial.SetVector("_Tiling", lavaTiling);
        }

        // ==================================================================================
        // 🌍 SHADER BIOMES (<50°C)
        // ==================================================================================

        /// <summary>
        /// Applique le shader biomes avec vertex colors
        /// </summary>
        private void ApplyBiomesShader()
        {
            // Changer vers shader biomes si nécessaire
            if (activeMaterial.shader != biomesMaterial.shader)
            {
                SwitchToBiomesShader();
            }

            // Appliquer les vertex colors selon la hauteur
            ApplyVertexColorsByHeight();

            LogDebug("🌍 Shader biomes appliqué");
        }

        /// <summary>
        /// Configure le shader biomes avec ses textures principales et normales
        /// </summary>
        private void SwitchToBiomesShader()
        {
            // Changer le shader
            activeMaterial.shader = biomesMaterial.shader;

            // Assigner les textures principales des biomes
            if (oceanTexture != null)
                activeMaterial.SetTexture("_Ocean_Texture", oceanTexture);

            if (shoreTexture != null)
                activeMaterial.SetTexture("_Shore_Texture", shoreTexture);

            if (plainsTexture != null)
                activeMaterial.SetTexture("_Plains_Texture", plainsTexture);

            if (hillsTexture != null)
                activeMaterial.SetTexture("_Hills_Texture", hillsTexture);

            if (mountainTexture != null)
                activeMaterial.SetTexture("_Mountain_Texture", mountainTexture);

            if (snowTexture != null)
                activeMaterial.SetTexture("_Snow_Texture", snowTexture);

            // Assigner les textures normales des biomes
            if (oceanNormal != null)
                activeMaterial.SetTexture("_Ocean_Normal", oceanNormal);

            if (shoreNormal != null)
                activeMaterial.SetTexture("_Shore_Normal", shoreNormal);

            if (plainsNormal != null)
                activeMaterial.SetTexture("_Plains_Normal", plainsNormal);

            if (hillsNormal != null)
                activeMaterial.SetTexture("_Hills_Normal", hillsNormal);

            if (mountainNormal != null)
                activeMaterial.SetTexture("_Mountain_Normal", mountainNormal);

            if (snowNormal != null)
                activeMaterial.SetTexture("_Snow_Normal", snowNormal);

            // Paramètres de tuilage  
            activeMaterial.SetVector("_Tiling", biomeTiling);

            LogDebug("🔄 Basculé vers shader biomes avec textures normales");
        }

        /// <summary>
        /// Calcule et applique les vertex colors selon la hauteur
        /// </summary>
        private void ApplyVertexColorsByHeight()
        {
            Mesh mesh = planetMeshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
            Color[] colors = new Color[vertices.Length];

            // Calculer les hauteurs min/max pour normalisation
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;

            foreach (var vertex in vertices)
            {
                float height = vertex.magnitude;
                if (height < minHeight) minHeight = height;
                if (height > maxHeight) maxHeight = height;
            }

            // Appliquer les couleurs selon la hauteur normalisée
            for (int i = 0; i < vertices.Length; i++)
            {
                float height = vertices[i].magnitude;
                float normalizedHeight = Mathf.InverseLerp(minHeight, maxHeight, height);

                colors[i] = GetBiomeColorForHeight(normalizedHeight);
            }

            // Appliquer au mesh
            mesh.colors = colors;
            mesh.UploadMeshData(false);

            LogDebug($"Vertex colors appliqués: {colors.Length} vertices");
        }

        /// <summary>
        /// Retourne la couleur du biome selon la hauteur normalisée
        /// COULEURS EXACTES du système existant
        /// </summary>
        private Color GetBiomeColorForHeight(float normalizedHeight)
        {
            // Couleurs exactes correspondantes aux biomes
            if (normalizedHeight < 0.1f)
                return new Color(0.1f, 0.3f, 0.8f);    // Ocean (bleu océan)
            else if (normalizedHeight < 0.2f)
                return new Color(0.8f, 0.7f, 0.4f);    // Shore (sable)
            else if (normalizedHeight < 0.5f)
                return new Color(0.3f, 0.6f, 0.2f);    // Plains (vert)
            else if (normalizedHeight < 0.7f)
                return new Color(0.5f, 0.4f, 0.2f);    // Hills (brun)
            else if (normalizedHeight < 0.9f)
                return new Color(0.4f, 0.4f, 0.4f);    // Mountain (gris)
            else
                return new Color(0.9f, 0.9f, 1.0f);    // Snow (blanc)
        }

        // ==================================================================================
        // 🎯 EVENT HANDLERS
        // ==================================================================================

        /// <summary>
        /// Appelé automatiquement quand GameManager change la température de surface
        /// </summary>
        private void OnSurfaceTemperatureChanged(float newTemperature)
        {
            if (!isInitialized || !enableSystem) return;

            //LogDebug($"🌡️ Event température reçu: {newTemperature:F0}°C");
            SetTemperature(newTemperature);
        }

        // ==================================================================================
        // 🔧 UTILITAIRES
        // ==================================================================================

        private void LogDebug(string message)
        {
            if (enableDetailedLogs)
            {
                Debug.Log($"[SimplifiedBiomeSystem] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SimplifiedBiomeSystem] {message}");
        }
    }
}