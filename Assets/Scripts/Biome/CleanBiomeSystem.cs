// CleanBiomeSystem.cs - VERSION PROPRE RECONSTITUÉE
using UnityEngine;
using LifeStory.Core;
using LifeStory.Generation;
using LifeStory.Terrain;

namespace LifeStory.Biomes
{
    /// <summary>
    /// Système unifié de biomes thermiques et biologiques - Version propre
    /// </summary>
    public class CleanBiomeSystem : MonoBehaviour
    {
        [Header("System Configuration")]
        [SerializeField] private bool enableSystem = true;
        [SerializeField] private Material planetBiomeMaterial;

        [Header("Thermal Biomes")]
        [SerializeField] private bool enableThermalBiomes = true;
        [SerializeField] private float thermalUpdateInterval = 0.5f;
        [SerializeField] private bool enableContinuousEmission = true;  // ✅ NOUVEAU
        [SerializeField] private float emissionUpdateInterval = 0.2f;   // ✅ NOUVEAU - Plus fréquent que thermal

        [Header("Thermal Temperature Thresholds - ORDRE LOGIQUE")]
        [SerializeField] private float moltenLavaThreshold = 781f;    // ✅ TEMPÉRATURE MINIMUM pour MoltenLava
        [SerializeField] private float coolingLavaThreshold = 113f;   // ✅ TEMPÉRATURE MINIMUM pour CoolingLava  
        [SerializeField] private float sterilRockThreshold = 80f;     // ✅ TEMPÉRATURE MINIMUM pour SterilRock
        [SerializeField] private float biologicalThreshold = 50f;     // ✅ TEMPÉRATURE MINIMUM pour Biological

        [Header("Evolution Timing")]
        [SerializeField] private float evolutionStartAge = 1000f;
        [SerializeField] private float oceanFormationDuration = 200f;
        [SerializeField] private float terrestrialLifeStartAge = 150f;
        [SerializeField] private float terrestrialLifeDuration = 300f;
        [SerializeField] private float updateInterval = 5f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showEvolutionProgress = true;

        // === ENUMS ===
        public enum ThermalBiomeState
        {
            MoltenLava,
            CoolingLava,
            SterilRock,
            BiologicalLife
        }

        public enum BiomeEvolutionType
        {
            None,
            Ocean,
            Coastal,
            Terrestrial,
            Thermal
        }

        [System.Serializable]
        public class EvolvingBiome
        {
            public string name;
            public float minHeight;
            public float maxHeight;
            public Color sterilColor;
            public Color matureColor;
            public BiomeEvolutionType evolutionType;

            public bool ContainsHeight(float height)
            {
                return height >= minHeight && height <= maxHeight;
            }

            public Color GetCurrentColor(float oceanProgress, float terrestrialProgress, float coastalProgress)
            {
                float progress = 0f;
                switch (evolutionType)
                {
                    case BiomeEvolutionType.Ocean:
                        progress = oceanProgress;
                        break;
                    case BiomeEvolutionType.Terrestrial:
                        progress = terrestrialProgress;
                        break;
                    case BiomeEvolutionType.Coastal:
                        progress = coastalProgress;
                        break;
                    case BiomeEvolutionType.None:
                    case BiomeEvolutionType.Thermal:
                        return matureColor;
                }

                return Color.Lerp(sterilColor, matureColor, progress);
            }
        }

        // === CONFIGURATION BIOMES ===
        [Header("Thermal Biomes Configuration")]
        [SerializeField]
        private EvolvingBiome moltenLava = new EvolvingBiome
        {
            name = "MoltenLava",
            minHeight = 0f,
            maxHeight = 1f,
            sterilColor = new Color(1f, 0.2f, 0f, 1f),
            matureColor = new Color(1f, 0.4f, 0.1f, 1f),
            evolutionType = BiomeEvolutionType.Thermal
        };

        [SerializeField]
        private EvolvingBiome coolingLava = new EvolvingBiome
        {
            name = "CoolingLava",
            minHeight = 0f,
            maxHeight = 1f,
            sterilColor = new Color(0.3f, 0.1f, 0f, 1f),
            matureColor = new Color(0.5f, 0.2f, 0.1f, 1f),
            evolutionType = BiomeEvolutionType.Thermal
        };

        [SerializeField]
        private EvolvingBiome sterilRock = new EvolvingBiome
        {
            name = "SterilRock",
            minHeight = 0f,
            maxHeight = 1f,
            sterilColor = new Color(0.25f, 0.2f, 0.15f, 1f),
            matureColor = new Color(0.4f, 0.35f, 0.3f, 1f),
            evolutionType = BiomeEvolutionType.Thermal
        };

        [Header("Biological Biomes Configuration")]
        [SerializeField]
        private EvolvingBiome[] biologicalBiomes = new EvolvingBiome[]
        {
            new EvolvingBiome
            {
                name = "DeepOcean",
                minHeight = 0.0f,
                maxHeight = 0.3f,
                sterilColor = new Color(0.15f, 0.1f, 0.05f),
                matureColor = new Color(0.1f, 0.2f, 0.8f),
                evolutionType = BiomeEvolutionType.Ocean
            },
            new EvolvingBiome
            {
                name = "ShallowOcean",
                minHeight = 0.3f,
                maxHeight = 0.5f,
                sterilColor = new Color(0.25f, 0.15f, 0.1f),
                matureColor = new Color(0.2f, 0.4f, 0.9f),
                evolutionType = BiomeEvolutionType.Ocean
            },
            new EvolvingBiome
            {
                name = "Beach",
                minHeight = 0.5f,
                maxHeight = 0.65f,
                sterilColor = new Color(0.4f, 0.3f, 0.2f),
                matureColor = new Color(0.9f, 0.8f, 0.6f),
                evolutionType = BiomeEvolutionType.Coastal
            },
            new EvolvingBiome
            {
                name = "Plains",
                minHeight = 0.65f,
                maxHeight = 0.8f,
                sterilColor = new Color(0.3f, 0.25f, 0.2f),
                matureColor = new Color(0.4f, 0.7f, 0.2f),
                evolutionType = BiomeEvolutionType.Terrestrial
            },
            new EvolvingBiome
            {
                name = "Hills",
                minHeight = 0.8f,
                maxHeight = 0.9f,
                sterilColor = new Color(0.35f, 0.3f, 0.25f),
                matureColor = new Color(0.5f, 0.6f, 0.3f),
                evolutionType = BiomeEvolutionType.Terrestrial
            },
            new EvolvingBiome
            {
                name = "Mountains",
                minHeight = 0.9f,
                maxHeight = 1.0f,
                sterilColor = new Color(0.4f, 0.35f, 0.3f),
                matureColor = new Color(0.6f, 0.5f, 0.4f),
                evolutionType = BiomeEvolutionType.None
            }
        };

        [Header("Dual Quality System")]
        [SerializeField] private bool useHighQualityShaders = true;
        [SerializeField] private Material thermalEmissionMaterial; // PlanetBiomeHighQuality (pour MoltenLava + CoolingLava)
        [SerializeField] private Material standardBiomeMaterial;   // PlanetVertexColor (pour SterilRock + biomes)

        [Header("Thermal HQ Textures - Emission Phases")]
        [SerializeField] private Texture2D moltenLavaMainTex;
        [SerializeField] private Texture2D moltenLavaEmissionMap;
        [SerializeField] private Texture2D moltenLavaNormalMap;

        [SerializeField] private Texture2D coolingLavaMainTex;
        [SerializeField] private Texture2D coolingLavaEmissionMap;
        [SerializeField] private Texture2D coolingLavaNormalMap;

        [Header("Standard Textures - Non-Emission Phases")]
        [SerializeField] private Texture2D sterilRockAlbedo;   // lavalcold albedotransparency
        [SerializeField] private Texture2D sterilRockNormal;  // lavacoldnormal

        // === VARIABLES D'ÉTAT ===
        // Références système
        private PlanetGenerator planetGenerator;
        private GameManager gameManager;
        private MeshRenderer planetRenderer;
        private MeshFilter planetMeshFilter;

        // État général
        private bool isUsingEmissionShader = false;
        private Material currentActiveMaterial;
        private bool isInitialized = false;
        private int mapResolution;

        // État thermal
        private ThermalBiomeState currentThermalState = ThermalBiomeState.MoltenLava;
        private bool isInThermalMode = true;
        private float lastThermalUpdate = 0f;
        private float lastEmissionUpdate = 0f;  // ✅ NOUVEAU - Séparé du thermal

        // État biologique
        private bool isInEvolutionPhase = false;
        private float evolutionPhaseStartTime = 0f;
        private float currentOceanProgress = 0f;
        private float currentTerrestrialProgress = 0f;
        private float currentCoastalProgress = 0f;
        private float[,] normalizedHeightMap;

        private float lastUpdateTime = 0f;

        public static CleanBiomeSystem Instance { get; private set; }

        // === LIFECYCLE ===
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            StartCoroutine(InitializeSystem());
        }

        private void Update()
        {
            if (!enableSystem || !isInitialized) return;

            // Système biomes thermiques
            if (enableThermalBiomes && isInThermalMode &&
                Time.time - lastThermalUpdate >= thermalUpdateInterval)
            {
                UpdateThermalBiomeState();
                lastThermalUpdate = Time.time;
            }

            // Évolution biomes biologiques
            if (isInEvolutionPhase && Time.time - lastUpdateTime >= updateInterval)
            {
                bool evolutionChanged = UpdateEvolutionProgress();
                if (evolutionChanged)
                {
                    ApplyBiologicalBiomes();
                }
                lastUpdateTime = Time.time;
            }
        }

        // === INITIALISATION ===
        private System.Collections.IEnumerator InitializeSystem()
        {
            LogDebug("🌋 Initialisation Système Biomes");

            // ===== RETOUR DÉLAI ORIGINAL POUR STABILITÉ =====
            yield return new WaitForSeconds(1f); // ✅ REMIS à 1f pour stabilité

            // Trouver références
            planetGenerator = PlanetGenerator.Instance;
            gameManager = GameManager.Instance;

            if (planetGenerator == null || gameManager == null)
            {
                LogDebug("❌ Références système manquantes");
                yield break;
            }

            planetRenderer = planetGenerator.GetComponent<MeshRenderer>();
            planetMeshFilter = planetGenerator.GetComponent<MeshFilter>();

            if (planetRenderer == null || planetMeshFilter == null)
            {
                LogDebug("❌ Composants planète manquants");
                yield break;
            }

            // S'abonner aux événements
            GameManager.OnPhaseChanged += OnPhaseChanged;
            GameManager.OnSurfaceTemperatureChanged += OnSurfaceTemperatureChanged;

            yield return new WaitUntil(() => planetGenerator.HeightMap != null);

            mapResolution = planetGenerator.Resolution;
            isInitialized = true;

            LogDebug("✅ Système biomes initialisé");

            // ===== RETOUR DÉLAI ORIGINAL =====
            if (enableThermalBiomes)
            {
                // ✅ REMIS à 2f pour éviter conflits avec autres systèmes
                yield return new WaitForSeconds(2f);

                LogDebug($"🚀 Démarrage thermique stable - Température actuelle: {gameManager.SurfaceTemperature:F0}°C");
                StartThermalBiomeSystem();
            }
            else if (gameManager.CurrentPhase == GamePhase.Evolution)
            {
                StartEvolutionPhase();
            }
        }

        // === SYSTÈME THERMIQUE ===
        private void StartThermalBiomeSystem()
        {
            LogDebug("🌋 === SYSTÈME BIOMES THERMIQUES ACTIVÉ (APRÈS INITIALISATION) ===");

            isInThermalMode = true;
            isInEvolutionPhase = false;

            ApplyBiomeMaterial();
            UpdateThermalBiomeState();
            ApplyThermalBiomes();

            // ===== SUPPRIMÉ : enableContinuousEmission ConfigureShaderProperties =====
            // L'émission sera gérée par ApplyEmissionThermalBiome() UNIQUEMENT

            // Surveillance pour détecter écrasement couleurs
            StartCoroutine(MonitorColorOverride());

            LogDebug($"🌡️ État initial appliqué: {currentThermalState} ({gameManager.SurfaceTemperature:F0}°C)");
        }

        // ===== NOUVELLE MÉTHODE : SURVEILLER ÉCRASEMENT COULEURS =====
        private System.Collections.IEnumerator MonitorColorOverride()
        {
            yield return new WaitForSeconds(1f); // Attendre que tout soit stable

            Color[] expectedColors = null;
            if (planetMeshFilter?.mesh?.colors != null)
            {
                expectedColors = new Color[planetMeshFilter.mesh.colors.Length];
                System.Array.Copy(planetMeshFilter.mesh.colors, expectedColors, expectedColors.Length);
                LogDebug($"🔍 Surveillance couleurs activée - {expectedColors.Length} couleurs mémorisées");
            }

            for (int i = 0; i < 10; i++) // Surveiller pendant 10 secondes
            {
                yield return new WaitForSeconds(1f);

                if (planetMeshFilter?.mesh?.colors == null)
                {
                    LogDebug("🚨 DÉTECTION: Couleurs complètement supprimées !");
                    ApplyThermalBiomes(); // Réappliquer
                }
                else if (expectedColors != null)
                {
                    Color[] currentColors = planetMeshFilter.mesh.colors;
                    if (currentColors.Length != expectedColors.Length)
                    {
                        LogDebug("🚨 DÉTECTION: Nombre de couleurs changé !");
                        ApplyThermalBiomes(); // Réappliquer
                    }
                    else
                    {
                        // Vérifier si les couleurs ont été écrasées (beaucoup de blanc/gris)
                        int whiteCount = 0;
                        for (int j = 0; j < currentColors.Length; j++)
                        {
                            if (currentColors[j].r > 0.9f && currentColors[j].g > 0.9f && currentColors[j].b > 0.9f)
                                whiteCount++;
                        }

                        float whitePercentage = (float)whiteCount / currentColors.Length;
                        if (whitePercentage > 0.8f) // Plus de 80% blanc = problème
                        {
                            LogDebug($"🚨 DÉTECTION: {whitePercentage:P0} couleurs blanches - Réapplication biomes");
                            ApplyThermalBiomes();
                            break; // Arrêter la surveillance après correction
                        }
                    }
                }
            }

            LogDebug("🔍 Surveillance couleurs terminée");
        }

        private void UpdateThermalBiomeState()
        {
            LogDebug($"🔥 UpdateThermalBiomeState() APPELÉE - Temp: {gameManager?.SurfaceTemperature:F0}°C");

            if (!enableThermalBiomes || gameManager == null)
            {
                LogDebug("❌ Sortie prématurée - enableThermalBiomes ou gameManager NULL");
                return;
            }

            float temperature = gameManager.SurfaceTemperature;
            ThermalBiomeState newState;

            // ===== DÉTERMINER LE NOUVEL ÉTAT =====
            if (temperature >= moltenLavaThreshold)
                newState = ThermalBiomeState.MoltenLava;
            else if (temperature >= coolingLavaThreshold)
                newState = ThermalBiomeState.CoolingLava;
            else if (temperature >= sterilRockThreshold)
                newState = ThermalBiomeState.SterilRock;
            else
                newState = ThermalBiomeState.BiologicalLife;

            LogDebug($"🔄 État calculé: {currentThermalState} → {newState}");

            // ===== VÉRIFIER SI CHANGEMENT D'ÉTAT NÉCESSAIRE =====
            if (newState != currentThermalState)
            {
                LogDebug($"🌡️ TRANSITION CONFIRMÉE: {currentThermalState} → {newState} ({temperature:F0}°C)");
                currentThermalState = newState;

                if (newState == ThermalBiomeState.BiologicalLife)
                {
                    LogDebug("🌱 Appel TransitionToBiologicalBiomes()");
                    TransitionToBiologicalBiomes();
                }
                else
                {
                    LogDebug("🌋 Appel ApplyThermalBiomes()");
                    ApplyThermalBiomes();
                }
            }
            else
            {
                LogDebug($"⏸️ Aucun changement d'état nécessaire (déjà {currentThermalState})");
            }

            // ===== MISE À JOUR CONTINUE - SÉPARÉE ET CONDITIONNELLE =====
            if (isInThermalMode && (currentThermalState == ThermalBiomeState.MoltenLava || currentThermalState == ThermalBiomeState.CoolingLava))
            {
                // ===== NOUVEAU : MISE À JOUR CONDITIONNELLE SELON LE TYPE DE SHADER =====
                if (isUsingEmissionShader)
                {
                    // Mode EMISSION : Mettre à jour seulement l'émission, pas les vertex colors
                    UpdateEmissionOnly(temperature);
                }
                else
                {
                    // Mode STANDARD : Mettre à jour les vertex colors (qui incluent l'émission)
                    EvolvingBiome thermalBiome = GetCurrentThermalBiome();
                    if (thermalBiome != null)
                    {
                        LogDebug($"🎨 Application vertex colors (mode standard): {thermalBiome.name}");
                        ApplyThermalBiomeVertexColors(thermalBiome);

                        // En mode standard, on peut appliquer l'émission après les vertex colors
                        UpdateEmissionOnly(temperature);
                    }
                }
            }

            LogDebug($"✅ UpdateThermalBiomeState() TERMINÉE");
        }

        private void UpdateEmissionOnly(float temperature)
        {
            if (currentActiveMaterial == null) return;

            float emissionIntensity = CalculateEmission(temperature);
            Color finalEmissionColor = Color.white * emissionIntensity;  // TOUJOURS BLANC

            LogDebug($"🔥 UpdateEmissionOnly - Temp: {temperature:F0}°C, Intensité: {emissionIntensity:F3}");

            // Appliquer l'émission BLANCHE
            currentActiveMaterial.SetColor("_EmisionColor", finalEmissionColor);

            // Gestion des keywords
            if (emissionIntensity > 0.01f)
            {
                currentActiveMaterial.EnableKeyword("_EMISSION");
                currentActiveMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                currentActiveMaterial.DisableKeyword("_EMISSION");
                currentActiveMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            // Animation temps si MoltenLava
            if (currentThermalState == ThermalBiomeState.MoltenLava && emissionIntensity > 0.1f)
            {
                float timeOffset = Time.time * 0.5f;
                if (currentActiveMaterial.HasProperty("_TimeOffset"))
                {
                    currentActiveMaterial.SetFloat("_TimeOffset", timeOffset);
                }
            }

            LogDebug($"🌟 Émission mise à jour - R={finalEmissionColor.r:F3} G={finalEmissionColor.g:F3} B={finalEmissionColor.b:F3}");
        }

        private void ApplyThermalBiomes()
        {
            if (!isInThermalMode) return;

            float currentTemp = gameManager?.SurfaceTemperature ?? 0f;

            // Déterminer si on a besoin du shader avec émission
            bool needsEmissionShader = RequiresEmissionShader();

            // Changer de shader si nécessaire
            if (isUsingEmissionShader != needsEmissionShader)
            {
                SwitchToAppropriateShader(needsEmissionShader);
            }

            // Appliquer le biome selon le shader actif
            if (isUsingEmissionShader)
            {
                ApplyEmissionThermalBiome(currentTemp);
            }
            else
            {
                ApplyStandardThermalBiome();
            }

            LogDebug($"🌋 Biome thermique appliqué: {currentThermalState} | Shader: {(isUsingEmissionShader ? "EMISSION" : "STANDARD")} | Temp: {currentTemp:F0}°C");
        }

        private bool RequiresEmissionShader()
        {
            // Si utilisateur a désactivé la HQ, toujours standard
            if (!useHighQualityShaders) return false;

            // Seulement MoltenLava et CoolingLava utilisent l'émission
            return currentThermalState == ThermalBiomeState.MoltenLava ||
                   currentThermalState == ThermalBiomeState.CoolingLava;
        }

        // ===== CHANGER DE SHADER =====
        private void SwitchToAppropriateShader(bool useEmissionShader)
        {
            if (planetRenderer?.material == null) return;

            Material targetMaterial = useEmissionShader ? thermalEmissionMaterial : standardBiomeMaterial;

            if (targetMaterial == null)
            {
                LogDebug($"❌ Matériau {(useEmissionShader ? "EMISSION" : "STANDARD")} non assigné !");
                return;
            }

            // Créer une instance du matériau pour éviter de modifier l'original
            if (currentActiveMaterial != null)
            {
                DestroyImmediate(currentActiveMaterial);
            }

            currentActiveMaterial = new Material(targetMaterial);
            currentActiveMaterial.name = $"Planet_{(useEmissionShader ? "Emission" : "Standard")}_{currentThermalState}";

            planetRenderer.material = currentActiveMaterial;
            isUsingEmissionShader = useEmissionShader;

            LogDebug($"🔄 Shader changé vers: {(useEmissionShader ? "EMISSION" : "STANDARD")} - {currentActiveMaterial.name}");
        }

        // ===== APPLIQUER BIOME AVEC SHADER D'ÉMISSION =====
        private void ApplyEmissionThermalBiome(float temperature)
        {
            if (currentActiveMaterial == null)
            {
                LogDebug("❌ currentActiveMaterial NULL dans ApplyEmissionThermalBiome");
                return;
            }

            LogDebug($"🎨 ApplyEmissionThermalBiome() - État: {currentThermalState}, Temp: {temperature:F0}°C");

            // ... [Code textures existant - gardez-le] ...

            // ===== ÉMISSION CORRIGÉE : TOUJOURS BLANC × INTENSITÉ =====
            float emissionIntensity = CalculateEmission(temperature);
            Color finalEmissionColor = Color.white * emissionIntensity;  // TOUJOURS BLANC !

            LogDebug($"🔥 CALCUL ÉMISSION - Intensité: {emissionIntensity:F3} × Blanc = {finalEmissionColor}");

            // Application directe
            currentActiveMaterial.SetColor("_EmisionColor", finalEmissionColor);

            // Gestion des keywords
            if (emissionIntensity > 0.01f)
            {
                currentActiveMaterial.EnableKeyword("_EMISSION");
                currentActiveMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                currentActiveMaterial.DisableKeyword("_EMISSION");
                currentActiveMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            // Animation temps
            if (currentThermalState == ThermalBiomeState.MoltenLava && emissionIntensity > 0.1f)
            {
                float timeOffset = Time.time * 0.5f;
                if (currentActiveMaterial.HasProperty("_TimeOffset"))
                {
                    currentActiveMaterial.SetFloat("_TimeOffset", timeOffset);
                }
            }

            LogDebug($"🔥 ÉMISSION FINALE APPLIQUÉE - R={finalEmissionColor.r:F3} G={finalEmissionColor.g:F3} B={finalEmissionColor.b:F3}");
        }



        private bool TryKeywordUpdate()
        {
            try
            {
                LogDebug("🔑 Tentative mise à jour par keywords...");

                // Désactiver puis réactiver les keywords principaux pour forcer refresh
                currentActiveMaterial.DisableKeyword("_EMISSION");
                currentActiveMaterial.DisableKeyword("_NORMALMAP");

                // Réactiver immédiatement
                currentActiveMaterial.EnableKeyword("_EMISSION");
                if (currentActiveMaterial.GetTexture("_Bump") != null)
                    currentActiveMaterial.EnableKeyword("_NORMALMAP");

                // Attendre une frame pour que Unity traite
                StartCoroutine(VerifyKeywordUpdate());

                LogDebug("✅ Keywords mis à jour");
                return true;
            }
            catch (System.Exception e)
            {
                LogDebug($"❌ Erreur keywords: {e.Message}");
                return false;
            }
        }

        private System.Collections.IEnumerator VerifyKeywordUpdate()
        {
            yield return null; // Attendre une frame

            // Vérifier si les textures sont bien appliquées
            string currentMainTex = currentActiveMaterial.GetTexture("_MainTex")?.name ?? "NULL";
            string expectedMainTex = (currentThermalState == ThermalBiomeState.CoolingLava) ?
                                    coolingLavaMainTex?.name ?? "NULL" :
                                    moltenLavaMainTex?.name ?? "NULL";

            if (currentMainTex != expectedMainTex)
            {
                LogDebug($"⚠️ Keywords insuffisants - Textures non mises à jour. Fallback...");
                ForceNewMaterialUpdate();
            }
            else
            {
                LogDebug($"✅ Keywords réussis - Textures correctement mises à jour");
            }
        }

        private void ForceNewMaterialUpdate()
        {
            LogDebug("🔄 FALLBACK - Création nouveau matériau...");

            // Méthode actuelle qui fonctionne
            Material updatedMaterial = new Material(currentActiveMaterial);
            if (planetRenderer != null)
            {
                planetRenderer.material = updatedMaterial;

                // Détruire l'ancien matériau pour éviter les fuites mémoire
                if (currentActiveMaterial != updatedMaterial)
                {
                    DestroyImmediate(currentActiveMaterial);
                }

                currentActiveMaterial = updatedMaterial;
                LogDebug("🔄 FALLBACK terminé - Nouveau matériau assigné");
            }
        }


        private void ApplyStandardThermalBiome()
        {
            // Pour SterilRock, appliquer les textures si en mode HQ
            if (currentThermalState == ThermalBiomeState.SterilRock && useHighQualityShaders)
            {
                if (currentActiveMaterial != null)
                {
                    if (sterilRockAlbedo != null)
                        currentActiveMaterial.SetTexture("_MainTex", sterilRockAlbedo);

                    if (sterilRockNormal != null)
                        currentActiveMaterial.SetTexture("_Bump", sterilRockNormal);
                }

                LogDebug($"🪨 STANDARD HQ Textures appliquées: SterilRock - Albedo: {sterilRockAlbedo?.name}");
            }
            else
            {
                // Utiliser le système existant de couleurs vertices pour LQ ou biomes biologiques
                EvolvingBiome thermalBiome = GetCurrentThermalBiome();
                if (thermalBiome == null) return;

                ApplyThermalBiomeVertexColors(thermalBiome);
                LogDebug($"🎨 STANDARD Vertex colors appliquées: {thermalBiome.name}");
            }
        }




        private EvolvingBiome GetCurrentThermalBiome()
        {
            switch (currentThermalState)
            {
                case ThermalBiomeState.MoltenLava: return moltenLava;
                case ThermalBiomeState.CoolingLava: return coolingLava;
                case ThermalBiomeState.SterilRock: return sterilRock;
                default: return null;
            }
        }

        private void ApplyThermalBiomeVertexColors(EvolvingBiome thermalBiome)
        {
            var mesh = planetMeshFilter?.mesh;
            if (mesh == null) return;

            Vector3[] vertices = mesh.vertices;
            Color[] colors = new Color[vertices.Length];

            float currentTemp = gameManager.SurfaceTemperature;
            LogDebug($"🎨 ApplyThermalBiomeVertexColors - Temperature: {currentTemp:F0}°C (SANS émission)");

            float time = Time.time;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];

                float surfaceNoise = Mathf.PerlinNoise(
                    vertex.x * 0.3f + time * 0.2f,
                    vertex.z * 0.3f + time * 0.2f
                );

                Color baseColor = Color.Lerp(
                    thermalBiome.sterilColor,
                    thermalBiome.matureColor,
                    surfaceNoise
                );

                colors[i] = baseColor;
            }

            mesh.colors = colors;

            // ===== SUPPRIMÉ : ConfigureShaderProperties =====
            // L'émission sera gérée par ApplyEmissionThermalBiome() UNIQUEMENT

            LogDebug($"🎨 Couleurs vertex appliquées - {colors.Length} vertex (ÉMISSION NON TOUCHÉE)");
        }




        // ===== NOUVELLE MÉTHODE - CONFIGURER PROPRIÉTÉS SHADER =====
        private void ConfigureShaderProperties(float temperature, float emissionIntensity, Color emissionColor)
        {
            return;
            if (planetRenderer?.material == null) return;

            Material material = planetRenderer.material;

            LogDebug($"🎨 ConfigureShaderProperties - Temp: {temperature:F0}°C, Intensité: {emissionIntensity:F3}");

            // ===== LOGIQUE CORRIGÉE : BLANC × INTENSITÉ POUR HDR =====
            // Utiliser blanc × intensité pour obtenir les valeurs HDR correctes
            Color finalEmissionColor = Color.white * emissionIntensity;

            // ===== APPLICATION DES PROPRIÉTÉS =====
            material.SetColor("_EmisionColor", finalEmissionColor);

            // ===== DIAGNOSTIC HDR =====
            LogDebug($"🌟 Couleur HDR finale - R:{finalEmissionColor.r:F3} G:{finalEmissionColor.g:F3} B:{finalEmissionColor.b:F3}");
            LogDebug($"🌟 Intensité HDR max component: {finalEmissionColor.maxColorComponent:F3}");

            // ===== GESTION DES KEYWORDS D'ÉMISSION =====
            if (emissionIntensity > 0.01f)
            {
                // Activer l'émission
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

                LogDebug($"✅ Émission activée - Intensité finale: {emissionIntensity:F3}");
            }
            else
            {
                // Désactiver l'émission
                material.DisableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                LogDebug("🔒 Émission désactivée");
            }

            // ===== ANIMATION TEMPS (pour lave en fusion) =====
            if (currentThermalState == ThermalBiomeState.MoltenLava && emissionIntensity > 0.1f)
            {
                float animationSpeed = Mathf.Lerp(0.2f, 1.0f, emissionIntensity);
                float timeOffset = Time.time * animationSpeed;

                if (material.HasProperty("_TimeOffset"))
                {
                    material.SetFloat("_TimeOffset", timeOffset);
                    LogDebug($"🔄 Animation temps activée - Vitesse: {animationSpeed:F2}");
                }
            }
            else
            {
                if (material.HasProperty("_TimeOffset"))
                {
                    material.SetFloat("_TimeOffset", 0f);
                }
            }

            // ===== PROPRIÉTÉS ÉVOLUTION (valeurs par défaut pour mode thermique) =====
            if (material.HasProperty("_OceanProgress"))
                material.SetFloat("_OceanProgress", 0f);

            if (material.HasProperty("_TerrestrialProgress"))
                material.SetFloat("_TerrestrialProgress", 0f);

            if (material.HasProperty("_CoastalProgress"))
                material.SetFloat("_CoastalProgress", 0f);

            LogDebug($"🎨 Shader configuré - Émission: {(emissionIntensity > 0.01f ? "ON" : "OFF")} | Valeur HDR: {finalEmissionColor.maxColorComponent:F3}");
        }

        private float CalculateEmission(float temperature)
        {
            LogDebug($"🧮 CalculateEmission appelée - Temp: {temperature:F0}°C");

            if (temperature >= 2000f)
            {
                LogDebug($"   → Température max (≥2000°C) → Intensité: 4.0");
                return 4.0f;
            }

            if (temperature <= 125f)  // ✅ SEUIL COHÉRENT
            {
                LogDebug($"   → Température min (≤125°C) → Intensité: 0.0");
                return 0f;
            }

            // ===== CORRECTION : 125f au lieu de 100f =====
            float progress = Mathf.InverseLerp(125f, 2000f, temperature);
            float intensity = progress * 4.0f;

            LogDebug($"   → Progress: {progress:F3} → Intensité: {intensity:F3}");

            return Mathf.Clamp(intensity, 0f, 4.0f);
        }

        private Color CalculateEmissionColor(float temperature)
        {
            // ===== TOUJOURS BLANC POUR ÉMISSION PROPRE =====
            // Le blanc permet à la texture d'émission de garder ses vraies couleurs
            // L'intensité sera appliquée via la multiplication

            LogDebug($"🎨 CalculateEmissionColor - Blanc stable à {temperature:F0}°C");

            return Color.white; // Toujours blanc, peu importe la température
        }

        // === TRANSITION VERS BIOLOGIQUE ===
        private void TransitionToBiologicalBiomes()
        {
            LogDebug("🌱 === TRANSITION VERS BIOMES BIOLOGIQUES ===");

            // S'assurer qu'on utilise le shader standard pour les biomes
            if (isUsingEmissionShader)
            {
                SwitchToAppropriateShader(false);
            }

            isInThermalMode = false;
            isInEvolutionPhase = true;

            // Appliquer le matériau biome existant (qui devrait être le même que standardBiomeMaterial)
            if (!ApplyBiomeMaterial())
            {
                LogDebug("❌ Échec application matériau biomes");
                return;
            }

            StartEvolutionPhase();
        }

        private System.Collections.IEnumerator DelayedBiologicalTransition()
        {
            yield return new WaitForSeconds(1f);

            NormalizeHeightMap();
            ApplyBiologicalBiomes();

            LogDebug("✅ Transition biologique terminée");
        }

        private void StartEvolutionPhase()
        {
            LogDebug("🌱 === DÉBUT PHASE ÉVOLUTION ===");

            isInEvolutionPhase = true;
            isInThermalMode = false;
            evolutionPhaseStartTime = gameManager.PlanetAge;

            currentOceanProgress = 0f;
            currentTerrestrialProgress = 0f;
            currentCoastalProgress = 0f;

            lastUpdateTime = Time.time;

            StartCoroutine(DelayedInitialApply());
        }

        private System.Collections.IEnumerator DelayedInitialApply()
        {
            yield return new WaitForSeconds(2f);

            NormalizeHeightMap();
            ApplyBiomeMaterial();
            ApplyBiologicalBiomes();

            LogDebug("✅ Biomes biologiques appliqués");
        }

        // === SYSTÈME BIOLOGIQUE ===
        private bool UpdateEvolutionProgress()
        {
            if (!isInEvolutionPhase) return false;

            float currentAge = gameManager.PlanetAge;
            float timeInEvolution = currentAge - evolutionPhaseStartTime;

            float oldOceanProgress = currentOceanProgress;
            float oldCoastalProgress = currentCoastalProgress;
            float oldTerrestrialProgress = currentTerrestrialProgress;

            // Calculer progression océan
            float oceanProgress = Mathf.Clamp01(timeInEvolution / oceanFormationDuration);
            currentOceanProgress = Mathf.SmoothStep(0f, 1f, oceanProgress);

            // Calculer progression côtière
            float coastalStartTime = oceanFormationDuration * 0.5f;
            float coastalProgress = 0f;
            if (timeInEvolution > coastalStartTime)
            {
                coastalProgress = (timeInEvolution - coastalStartTime) / (oceanFormationDuration * 0.5f);
            }
            currentCoastalProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(coastalProgress));

            // Calculer progression terrestre
            if (timeInEvolution > terrestrialLifeStartAge)
            {
                float terrestrialTime = timeInEvolution - terrestrialLifeStartAge;
                float terrestrialProgress = terrestrialTime / terrestrialLifeDuration;
                currentTerrestrialProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(terrestrialProgress));
            }

            bool hasChanged =
                Mathf.Abs(currentOceanProgress - oldOceanProgress) > 0.01f ||
                Mathf.Abs(currentCoastalProgress - oldCoastalProgress) > 0.01f ||
                Mathf.Abs(currentTerrestrialProgress - oldTerrestrialProgress) > 0.01f;

            if (hasChanged && showEvolutionProgress)
            {
                LogDebug($"🔄 Évolution - Océan: {currentOceanProgress:P0} | Côte: {currentCoastalProgress:P0} | Terrestre: {currentTerrestrialProgress:P0}");
            }

            return hasChanged;
        }

        [ContextMenu("Debug - Verify No Emission Conflicts")]
        public void DebugVerifyNoEmissionConflicts()
        {
            LogDebug("🔍 === VÉRIFICATION CONFLITS ÉMISSION ===");

            if (currentActiveMaterial == null)
            {
                LogDebug("❌ Pas de matériau actif");
                return;
            }

            float currentTemp = gameManager?.SurfaceTemperature ?? 2000f;

            // 1. Calculer l'émission attendue
            float expectedIntensity = CalculateEmission(currentTemp);
            Color expectedFinal = Color.white * expectedIntensity;

            LogDebug($"📊 ATTENDU - Temp: {currentTemp:F0}°C → Intensité: {expectedIntensity:F3} → Couleur: {expectedFinal}");

            // 2. Lire l'émission actuelle du matériau
            Color actualColor = currentActiveMaterial.GetColor("_EmisionColor");

            LogDebug($"📊 RÉEL - Couleur matériau: {actualColor}");

            // 3. Vérifier la cohérence
            bool isWhite = Mathf.Approximately(actualColor.r, actualColor.g) &&
                           Mathf.Approximately(actualColor.g, actualColor.b);

            LogDebug($"📊 ANALYSE - Est blanc: {(isWhite ? "✅" : "❌")} | Intensité réelle: {actualColor.maxColorComponent:F3}");

            if (!isWhite)
            {
                LogDebug($"🚨 PROBLÈME: Couleur non-blanche détectée ! Quelque chose écrase encore l'émission.");
            }
            else if (Mathf.Abs(actualColor.maxColorComponent - expectedIntensity) > 0.1f)
            {
                LogDebug($"🚨 PROBLÈME: Intensité incorrecte ! Attendu: {expectedIntensity:F3}, Réel: {actualColor.maxColorComponent:F3}");
            }
            else
            {
                LogDebug($"✅ ÉMISSION CORRECTE - Aucun conflit détecté");
            }
        }

        [ContextMenu("Apply Biological Biomes")]
        public void ApplyBiologicalBiomes()
        {
            if (!enableSystem || !isInitialized || !isInEvolutionPhase)
            {
                return;
            }

            if (normalizedHeightMap == null)
            {
                if (!NormalizeHeightMap())
                {
                    return;
                }
            }

            if (!ApplyBiomeMaterial())
            {
                return;
            }

            ApplyEvolvingVertexColors();
        }

        private bool NormalizeHeightMap()
        {
            var heightMap = planetGenerator.HeightMap;
            if (heightMap == null) return false;

            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    float height = heightMap[x, y];
                    if (height < minHeight) minHeight = height;
                    if (height > maxHeight) maxHeight = height;
                }
            }

            float range = maxHeight - minHeight;
            if (range < 0.001f) return false;

            normalizedHeightMap = new float[mapResolution, mapResolution];

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    normalizedHeightMap[x, y] = (heightMap[x, y] - minHeight) / range;
                }
            }

            return true;
        }

        private bool ApplyBiomeMaterial()
        {
            if (standardBiomeMaterial == null || planetRenderer == null)
            {
                LogDebug("❌ standardBiomeMaterial non disponible");
                return false;
            }

            // Utiliser standardBiomeMaterial au lieu de planetBiomeMaterial pour cohérence
            if (currentActiveMaterial != null)
            {
                DestroyImmediate(currentActiveMaterial);
            }

            currentActiveMaterial = new Material(standardBiomeMaterial);
            currentActiveMaterial.name = "Planet_BiologicalEvolution";

            planetRenderer.material = currentActiveMaterial;
            isUsingEmissionShader = false;

            LogDebug("🎨 Matériau biome évolution appliqué");
            return true;
        }

        private bool ApplyEvolvingVertexColors()
        {
            var mesh = planetMeshFilter.mesh;
            if (mesh == null || normalizedHeightMap == null) return false;

            Vector3[] vertices = mesh.vertices;
            Color[] colors = new Color[vertices.Length];

            // ===== CONFIGURER PROPRIÉTÉS SHADER POUR ÉVOLUTION =====
            ConfigureEvolutionShaderProperties();

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                Vector3 direction = vertex.normalized;
                Vector2Int mapCoords = WorldToHeightMapCoordinates(direction);

                if (IsValidCoordinate(mapCoords))
                {
                    float height = normalizedHeightMap[mapCoords.x, mapCoords.y];
                    EvolvingBiome biome = GetBiomeForHeight(height);

                    if (biome != null)
                    {
                        Color evolvedColor = biome.GetCurrentColor(
                            currentOceanProgress,
                            currentTerrestrialProgress,
                            currentCoastalProgress
                        );
                        colors[i] = evolvedColor;
                    }
                    else
                    {
                        colors[i] = Color.gray;
                    }
                }
                else
                {
                    colors[i] = Color.gray;
                }
            }

            mesh.colors = colors;
            return true;
        }

        // ===== NOUVELLE MÉTHODE - CONFIGURER PROPRIÉTÉS SHADER POUR ÉVOLUTION =====
        private void ConfigureEvolutionShaderProperties()
        {
            if (planetRenderer?.material == null) return;

            Material material = planetRenderer.material;

            // ===== PROPRIÉTÉS ÉVOLUTION =====
            material.SetFloat("_OceanProgress", currentOceanProgress);
            material.SetFloat("_TerrestrialProgress", currentTerrestrialProgress);
            material.SetFloat("_CoastalProgress", currentCoastalProgress);

            // ===== PROPRIÉTÉS D'ÉMISSION (désactivées en mode biologique) =====
            material.SetFloat("_EmissionIntensity", 0f);
            material.SetColor("_EmisionColor", Color.black);
            material.SetFloat("_TimeOffset", 0f);

            LogDebug($"🌱 Propriétés shader évolution - Océan:{currentOceanProgress:P0} Terrestre:{currentTerrestrialProgress:P0} Côte:{currentCoastalProgress:P0}");
        }

        private EvolvingBiome GetBiomeForHeight(float height)
        {
            foreach (var biome in biologicalBiomes)
            {
                if (biome.ContainsHeight(height))
                {
                    return biome;
                }
            }
            return null;
        }

        // === ÉVÉNEMENTS ===
        private void OnPhaseChanged(GamePhase newPhase)
        {
            if (!enableSystem || !isInitialized) return;

            if (newPhase == GamePhase.Evolution && !isInEvolutionPhase)
            {
                if (enableThermalBiomes && isInThermalMode)
                {
                    // Laisser la transition thermique naturelle se faire
                    return;
                }
                StartEvolutionPhase();
            }
        }

        private void OnSurfaceTemperatureChanged(float newSurfaceTemperature)
        {
            if (!enableThermalBiomes || !isInitialized || !isInThermalMode) return;

            LogDebug($"🌡️ Température SURFACE changée: {newSurfaceTemperature:F0}°C");

            // Vérifier changement d'état thermique
            UpdateThermalBiomeState();
        }

        // === UTILITAIRES ===
        private Vector2Int WorldToHeightMapCoordinates(Vector3 direction)
        {
            float longitude = Mathf.Atan2(direction.x, direction.z);
            float latitude = Mathf.Asin(direction.y);

            float u = (longitude + Mathf.PI) / (2 * Mathf.PI);
            float v = (latitude + Mathf.PI / 2) / Mathf.PI;

            int x = Mathf.Clamp(Mathf.RoundToInt(u * (mapResolution - 1)), 0, mapResolution - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (mapResolution - 1)), 0, mapResolution - 1);

            return new Vector2Int(x, y);
        }

        private bool IsValidCoordinate(Vector2Int coords)
        {
            return coords.x >= 0 && coords.x < mapResolution &&
                   coords.y >= 0 && coords.y < mapResolution;
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[CleanBiomeSystem] {message}");
            }
        }

        [ContextMenu("Toggle Shader Quality")]
        public void ToggleShaderQuality()
        {
            useHighQualityShaders = !useHighQualityShaders;
            LogDebug($"🔄 Qualité shader toggleée: {(useHighQualityShaders ? "HIGH" : "LOW")}");

            // Réappliquer immédiatement
            if (isInThermalMode)
            {
                ApplyThermalBiomes();
            }
            else if (isInEvolutionPhase)
            {
                ApplyBiologicalBiomes();
            }
        }

        // === MÉTHODES DE TEST ===

     
        public void ForceBiologicalTransition()
        {
            currentThermalState = ThermalBiomeState.BiologicalLife;
            TransitionToBiologicalBiomes();
            LogDebug("🌱 Transition biomes biologiques forcée");
        }

        [ContextMenu("Diagnostic - Test Emission Values")]
        public void DiagnosticTestEmissionValues()
        {
            LogDebug("🔍 === TEST VALEURS ÉMISSION ===");

            // Tester les températures clés
            float[] testTemperatures = { 2000f, 1500f, 1000f, 500f, 125f, 100f };

            foreach (float temp in testTemperatures)
            {
                float intensity = CalculateEmission(temp);
                Color color = CalculateEmissionColor(temp);
                Color final = color * intensity;

                LogDebug($"🌡️ {temp:F0}°C → Intensité: {intensity:F3} | Couleur finale: R={final.r:F3} G={final.g:F3} B={final.b:F3}");
            }
        }

        [ContextMenu("Test - Verify Emission Calculation")]
        public void TestVerifyEmissionCalculation()
        {
            LogDebug("🧮 === VÉRIFICATION CALCUL ÉMISSION ===");

            // Tester les températures de vos images
            float[] testTemps = { 1974f, 1084f, 440f, 125f, 100f, 2000f };

            foreach (float temp in testTemps)
            {
                float intensity = CalculateEmission(temp);
                float progress = Mathf.InverseLerp(125f, 2000f, temp);

                LogDebug($"🌡️ {temp:F0}°C → Progress: {progress:F3} → Intensité: {intensity:F3}");

                // Vérifications attendues
                if (temp >= 2000f && !Mathf.Approximately(intensity, 4.0f))
                    LogDebug($"❌ ERREUR: {temp}°C devrait donner 4.0, pas {intensity:F3}");
                if (temp <= 125f && !Mathf.Approximately(intensity, 0.0f))
                    LogDebug($"❌ ERREUR: {temp}°C devrait donner 0.0, pas {intensity:F3}");
            }

            LogDebug("✅ Vérification terminée");
        }

        private bool ColorApproximately(Color a, Color b, float threshold = 0.01f)
        {
            return Mathf.Abs(a.r - b.r) < threshold &&
                   Mathf.Abs(a.g - b.g) < threshold &&
                   Mathf.Abs(a.b - b.b) < threshold &&
                   Mathf.Abs(a.a - b.a) < threshold;
        }

        // === GETTERS PUBLICS ===
        public bool IsSystemInitialized => isInitialized;
        public bool IsInThermalMode => isInThermalMode;
        public bool IsInEvolutionPhase => isInEvolutionPhase;
        public ThermalBiomeState CurrentThermalState => currentThermalState;
        public float OceanProgress => currentOceanProgress;
        public float CoastalProgress => currentCoastalProgress;
        public float TerrestrialProgress => currentTerrestrialProgress;

        // === CLEANUP ===
        private void OnDestroy()
        {
            if (currentActiveMaterial != null)
            {
                DestroyImmediate(currentActiveMaterial);
            }
        }
    }
}