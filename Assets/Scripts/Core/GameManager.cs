using UnityEngine;
using Unity.Mathematics;

namespace LifeStory.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("🎚️ Seuils Ajustables Runtime")]
        [SerializeField] private bool enableRuntimeThresholdAdjustment = true;

        
        [Header("🔧 Simulation Control")]
        [SerializeField]
        private bool showSimulationControls = true;

        [Header("⏱️ Time Scales - 3 Phases")]
        [SerializeField] private float infernalTimeScale = 1000f;
        [SerializeField] private float geologicalTimeScale = 200f;  // 1 seconde = 1000 ans
        [SerializeField] private float evolutionTimeScale = 50f;    // 1 seconde = 100 ans
        [SerializeField] private float currentTimeScale = 1f;

        [Header("Player Time Control")]
        [SerializeField] private float playerTimeMultiplier = 1f; // Multiplicateur du joueur (1x par défaut)

        [Header("Game State")]
        [SerializeField] private GamePhase currentPhase = GamePhase.Geological;
        [SerializeField] private float planetAge = 0f;              // Âge en millions d'années
        [SerializeField] private bool isPaused = false;

        [Header("Phase Transition")]
        [SerializeField] private float lifeEmergenceThreshold = 1000f; // Âge requis pour la vie (millions d'années)
        [SerializeField] private bool autoTransition = true;
        [SerializeField] private bool enableDebugLogs = true;

        [Header("🌡️ SURFACE Temperature System")]
        [SerializeField] private float surfaceTemperature = 2000f;     // Température surface en °C
        [SerializeField] private float minSurfaceTemperature = -50f;   // Température minimale surface
        [SerializeField] private float maxSurfaceTemperature = 2000f;  // Température maximale surface
        [SerializeField] private ClimateState currentClimate = ClimateState.Hellish;

        [Header("🔥 CORE Temperature System - NOUVEAU")]
        [SerializeField] private float coreTemperature = 4000f;        // Température noyau en °C  
        [SerializeField] private float minCoreTemperature = 2000f;     // Température finale noyau
        [SerializeField] private float maxCoreTemperature = 4000f;     // Température initiale noyau

        [Header("🌋 Formation Croûte - Équilibre Thermique")]
        [SerializeField] private float crustFormationThreshold = 900f;           // Surface max pour croûte
        [SerializeField] private float minThermalDifferential = 1200f;           // Différentiel min noyau-surface pour croûte
        [SerializeField] private float optimalCrustFormationCore = 2800f;        // Noyau optimal pour formation croûte
        [SerializeField] private bool enableThermalBalance = true;               // Activer équilibre thermique
        [SerializeField] private float biologicalLifeThreshold = 50f;            // Seuil vie biologique

        [Header("🌋 Seuils Volcaniques Calibrés")]
        [SerializeField] private float volcanicMinCoreTemp = 2200f;              // Seuil extinction volcans (noyau)
        [SerializeField] private float volcanicMaxCoreTemp = 4200f;              // Seuil maximum volcans (noyau)
        [SerializeField] private float tectonicMinCoreTemp = 2400f;              // Seuil minimum tectonique (noyau)

                       // Phase infernale: rapide

        [Header("Surface Cooling Configuration")]
        [SerializeField] private AnimationCurve surfaceCoolingCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private float surfaceCoolingDuration = 1500f;  // Durée refroidissement surface (Ma)
        [SerializeField] private float surfaceStabilizationTemp = 50f; // Température finale surface

        [Header("Core Cooling Configuration")]
        [SerializeField] private AnimationCurve coreCoolingCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private float coreCoolingDuration = 1800f;    // 4x plus lent que surface (Ma)

        [Header("Volcanic Gas Integration")]
        [SerializeField] private bool enableVolcanicGasIntegration = true;
        [SerializeField] private float volcanicCO2Multiplier = 1.0f;        // Amplificateur CO₂ volcanique
        [SerializeField] private float volcanicCH4Multiplier = 1.5f;        // Amplificateur CH₄ volcanique
        [SerializeField] private bool showVolcanicEmissionLogs = true;

        [Header("Water System")]
        [SerializeField] private float waterLevel = 0f;              // Niveau d'eau liquide (0-1)
        [SerializeField] private float vaporLevel = 0f;             // Niveau vapeur d'eau (0-1) - COMMENCE À 0
        [SerializeField] private float iceLevel = 0f;               // Niveau de glace (0-1)
        [SerializeField] private WaterState currentWaterState = WaterState.Vapor;

        [Header("🌊 Water Release - Two Phase System")]
        [SerializeField] private float earlyDegassingTemp = 200f;        // Début dégazage atmosphérique précoce
        [SerializeField] private float earlyDegassingRate = 0.002f;      // Débit lent atmosphérique (par seconde)
        [SerializeField] private float volcanicWaterMultiplier = 8f;     // Accélération par volcans
        [SerializeField] private bool enableEarlyDegassing = true;       // Activer dégazage précoce
        [SerializeField] private bool showWaterSourceLogs = true;        // Debug sources eau

        [Header("💧 Distribution Eau Réaliste")]
        [SerializeField] private float maxLiquidConversionRatio = 0.4f;     // 40% max devient liquide
        [SerializeField] private float minVaporRetention = 0.6f;            // 60% min reste vapeur
        [SerializeField] private float condensationStartTemp = 100f;        // Début condensation
        [SerializeField] private float maxCondensationTemp = 50f;           // Condensation maximale
        [SerializeField] private bool enableWaterDistributionLogs = true;   // Debug distribution

        [Header("Complete Atmospheric System")]
        [SerializeField] private float nitrogenLevel = 0f;      // N₂ - Gaz majoritaire stable
        [SerializeField] private float methaneLevel = 0f;       // CH₄ - Gaz primitif réducteur
        [SerializeField] private float co2Level = 0f;           // CO₂ - Gaz à effet de serre
        [SerializeField] private float oxygenLevel = 0f;        // O₂ - Gaz biologique (phase Evolution)

        [Header("Atmospheric Evolution Settings")]
        [SerializeField] private float maxNitrogenRelease = 0.78f;      // 78% comme sur Terre
        [SerializeField] private float maxMethaneRelease = 0.15f;       // 15% initial primitif
        [SerializeField] private float maxCO2Release = 0.20f;          // 20% initial
        [SerializeField] private float methaneToC02Rate = 0.1f;        // Vitesse conversion CH₄→CO₂
        [SerializeField] private float co2ToOxygenRate = 0.05f;        // Vitesse conversion CO₂→O₂ (Evolution)

        [Header("Atmospheric Transition Temperatures")]
        [SerializeField] private float methaneStabilityTemp = 400f;    // Température limite CH₄
        [SerializeField] private float co2ConversionStartTemp = 300f;  // Début conversion CH₄→CO₂
        [SerializeField] private AtmosphereComposition currentAtmosphere = AtmosphereComposition.None;

        [Header("Atmospheric Water Release")]
        [SerializeField] private float waterReleaseStartTemp = 150f;    // Température de début libération (changement texture)
        [SerializeField] private float waterReleaseEndTemp = 25f;     // Température de fin libération
        [SerializeField] private float maxWaterInAtmosphere = 1f;      // Maximum d'eau libérable
        [SerializeField]
        private AnimationCurve waterReleaseCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f),    // Début lent
            new Keyframe(1f, 1f, 0f, 0f)     // Fin rapide (EaseOut)
        );

        // ✅ NOUVEAUX EVENTS - Système Thermique Dual
        public static System.Action<GamePhase> OnPhaseChanged;
        public static System.Action<float> OnTimeScaleChanged;
        public static System.Action<float> OnPlanetAgeChanged;
        public static System.Action<float> OnSurfaceTemperatureChanged;  // ← RENOMMÉ
        public static System.Action<float> OnCoreTemperatureChanged;     // ← NOUVEAU
        public static System.Action<ClimateState> OnClimateChanged;
        public static System.Action<float> OnWaterLevelChanged;
        public static System.Action<WaterState> OnWaterStateChanged;
        public static System.Action<AtmosphereComposition> OnAtmosphereChanged;
        public static System.Action<float, float, float, float> OnGasLevelsChanged; // N₂, CH₄, CO₂, O₂
        public static System.Action<float> OnVolcanicWaterEmission;
        public bool ShowSimulationControls => showSimulationControls;


        // ✅ PROPRIÉTÉS PUBLIQUES - Système Thermique Dual
        public float SurfaceTemperature => surfaceTemperature;  // ← RENOMMÉ
        public float CoreTemperature => coreTemperature;        // ← NOUVEAU

        private float lastKnownTimeScale = -1f;
        public ClimateState CurrentClimate => currentClimate;
        public float WaterLevel => waterLevel;
        public float VaporLevel => vaporLevel;
        public float IceLevel => iceLevel;
        public float NitrogenLevel => nitrogenLevel;
        public float MethaneLevel => methaneLevel;
        public float CO2Level => co2Level;
        public float OxygenLevel => oxygenLevel;
        private float totalVolcanicWaterContribution = 0f;
        private float totalAtmosphericWaterRelease = 0f;
        private bool earlyDegassingActive = false;
        public float ThermalDifferential => coreTemperature - surfaceTemperature;
        public bool HasStableCrust => CanCrustForm();
        public float CrustFormationThreshold => crustFormationThreshold;
        public float VolcanicMinCoreTemp => volcanicMinCoreTemp;
        public float VolcanicMaxCoreTemp => volcanicMaxCoreTemp;
        public float TectonicMinCoreTemp => tectonicMinCoreTemp;
        public float BiologicalLifeThreshold => biologicalLifeThreshold;
        public float SurfaceCoolingDuration => surfaceCoolingDuration;
        public float CoreCoolingDuration => coreCoolingDuration;






        public float CrustStabilityFactor
        {
            get
            {
                if (!CanCrustForm()) return 0f; // Pas de croûte = 0% stabilité

                // Plus le différentiel est élevé, plus c'est stable
                float excess = ThermalDifferential - minThermalDifferential;
                float maxExcess = minThermalDifferential; // Range de calcul

                return Mathf.Clamp01(excess / maxExcess);
            }
        }
        //public float CrustFormationThreshold => crustFormationThreshold;



        public WaterState CurrentWaterState => currentWaterState;

        
        public AtmosphereComposition CurrentAtmosphere => currentAtmosphere;

        private float totalVolcanicCO2 = 0f;
        private float totalVolcanicCH4 = 0f;

        // Singleton pattern pour accès global
        public static GameManager Instance { get; private set; }

        public void DebugTransitionStatus()
        {
            LogDebug("=== DEBUG TRANSITION DÉTAILLÉ ===");
            LogDebug($"Surface: {surfaceTemperature:F0}°C (seuil croûte: {crustFormationThreshold:F0}°C)");
            LogDebug($"Noyau: {coreTemperature:F0}°C");
            LogDebug($"Différentiel: {ThermalDifferential:F0}°C (min: {minThermalDifferential:F0}°C)");
            LogDebug($"CanCrustForm(): {CanCrustForm()}");
            LogDebug($"HasStableCrust: {HasStableCrust}");
            LogDebug($"Phase actuelle: {currentPhase}");
            LogDebug($"Phase déterminée: {DetermineCurrentPhase()}");
            LogDebug($"CrustStabilityFactor: {CrustStabilityFactor:P1}");
        }

        private void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // ✅ NOUVEAU - S'abonner aux émissions volcaniques
            if (enableVolcanicGasIntegration)
            {
                LifeStory.Volcanoes.CleanVolcanicSystem.OnVolcanicGasEmission += OnVolcanicEmission;
                LogDebug("🌋 Abonnement aux émissions volcaniques activé");
            }
            if (enableVolcanicGasIntegration)
            {
                LifeStory.Volcanoes.CleanVolcanicSystem.OnVolcanicGasEmission += OnVolcanicEmission;
                LifeStory.Volcanoes.CleanVolcanicSystem.OnVolcanicWaterEmission += OnVolcanicWaterEmission; // NOUVEAU
                LogDebug("🌋 Abonnement aux émissions volcaniques (gaz + eau) activé");
            }
        }

        public void SetCrustFormationThreshold(float value)
        {
            if (!enableRuntimeThresholdAdjustment) return;
            crustFormationThreshold = value;
            LogDebug($"🎚️ Seuil croûte modifié: {value:F0}°C");
        }

        public void SetVolcanicMinCoreTemp(float value)
        {
            if (!enableRuntimeThresholdAdjustment) return;
            volcanicMinCoreTemp = value;
            LogDebug($"🎚️ Volcans min modifié: {value:F0}°C");
        }

        public void SetVolcanicMaxCoreTemp(float value)
        {
            if (!enableRuntimeThresholdAdjustment) return;
            volcanicMaxCoreTemp = value;
            LogDebug($"🎚️ Volcans max modifié: {value:F0}°C");
        }

        public void SetTectonicMinCoreTemp(float value)
        {
            if (!enableRuntimeThresholdAdjustment) return;
            tectonicMinCoreTemp = value;
            LogDebug($"🎚️ Tectonique min modifié: {value:F0}°C");
        }

        public void SetBiologicalLifeThreshold(float value)
        {
            if (!enableRuntimeThresholdAdjustment) return;
            biologicalLifeThreshold = value;
            LogDebug($"🎚️ Seuil vie modifié: {value:F0}°C");
        }

        public void SetSurfaceCoolingDuration(float value)
        {
            if (!enableRuntimeThresholdAdjustment) return;
            surfaceCoolingDuration = value;
            LogDebug($"🎚️ Durée surface modifiée: {value:F0} Ma");
        }

        public void SetCoreCoolingDuration(float value)
        {
            if (!enableRuntimeThresholdAdjustment) return;
            coreCoolingDuration = value;
            LogDebug($"🎚️ Durée noyau modifiée: {value:F0} Ma");
        }


        private bool CanCrustForm()
        {
            if (!enableThermalBalance)
            {
                bool simpleResult = surfaceTemperature < crustFormationThreshold;
                if (enableDebugLogs && Time.frameCount % 300 == 0) // Toutes les 5 secondes
                {
                    LogDebug($"🔍 Mode simple - Surface {surfaceTemperature:F0}°C < {crustFormationThreshold:F0}°C = {simpleResult}");
                }
                return simpleResult;
            }

            // Mode équilibre thermique
            bool surfaceCoolEnough = surfaceTemperature < crustFormationThreshold;
            float thermalDifferential = coreTemperature - surfaceTemperature;
            bool thermalBalanceOK = thermalDifferential >= minThermalDifferential;

            bool result = surfaceCoolEnough && thermalBalanceOK;

            if (enableDebugLogs && Time.frameCount % 300 == 0) // Log toutes les 5 secondes
            {
                //LogDebug($"🔍 Mode équilibre - Surface: {surfaceCoolEnough}, Différentiel: {thermalBalanceOK} → {result}");
                LogDebug($"   Surface: {surfaceTemperature:F0}°C < {crustFormationThreshold:F0}°C");
                LogDebug($"   Différentiel: {thermalDifferential:F0}°C ≥ {minThermalDifferential:F0}°C");
            }

            return result;
        }

        private GamePhase DetermineCurrentPhase()
        {
            // Vérifications formation → géologique
            bool crustFormed = CanCrustForm();
            bool atmosphereStable = (nitrogenLevel + methaneLevel + co2Level) > 0.15f; // 15% atmosphère min
            bool liquidWaterExists = waterLevel > 0.05f; // 5% eau liquide min
            bool coreActive = coreTemperature > tectonicMinCoreTemp; // Noyau assez chaud

            if (!crustFormed || !atmosphereStable || !liquidWaterExists || !coreActive)
            {
                return GamePhase.Infernal; // = Formation
            }
            else if (surfaceTemperature >= biologicalLifeThreshold)
            {
                return GamePhase.Geological; // Géologie active, pas encore de vie
            }
            else
            {
                return GamePhase.Evolution; // Géologie + Vie simultanées
            }
        }

        public bool IsVolcanicActivityPossible()
        {
            bool coreHotEnough = coreTemperature >= volcanicMinCoreTemp && coreTemperature <= volcanicMaxCoreTemp;
            bool hasCrust = HasStableCrust;

            return coreHotEnough && hasCrust;
        }

        public bool IsTectonicActivityPossible()
        {
            bool coreVeryHot = coreTemperature >= tectonicMinCoreTemp;
            bool hasCrust = HasStableCrust;

            return coreVeryHot && hasCrust;
        }

        private float GetBaseTimeScaleForPhase(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.Infernal => infernalTimeScale,      // 2000x - Formation rapide
                GamePhase.Geological => geologicalTimeScale,  // 500x - Activité géologique
                GamePhase.Evolution => evolutionTimeScale,    // 100x - Évolution détaillée
                GamePhase.Paused => 0f,
                _ => 1f
            };
        }

       

        private void DisableAllGeologicalSystems()
        {
            // Désactiver volcans
            var volcanicSystem = FindAnyObjectByType<LifeStory.Volcanoes.CleanVolcanicSystem>();
            if (volcanicSystem != null)
            {
                volcanicSystem.enabled = false;
                LogDebug("🚫 Volcans désactivés (pas de croûte)");
            }

            // Désactiver SimpleTwoPlateGenerator (système de plaques)
            var plateSystem = FindAnyObjectByType<LifeStory.Geology.SimpleTwoPlateGenerator>();
            if (plateSystem != null)
            {
                plateSystem.enabled = false;
                LogDebug("🚫 Système de plaques désactivé (pas de croûte)");
            }

            // Désactiver ContinentalRiftSystem (système de rifts)
            var riftSystem = FindAnyObjectByType<LifeStory.Geology.ContinentalRiftSystem>();
            if (riftSystem != null)
            {
                riftSystem.enabled = false;
                LogDebug("🚫 Système de rifts désactivé (pas de croûte)");
            }

            LogDebug("🚫 Phase infernale: Seul refroidissement actif");
        }

        private void EnableGeologicalSystemsWithThermalCheck()
        {
            // Volcans : vérifier seuil noyau ET croûte
            var volcanicSystem = FindAnyObjectByType<LifeStory.Volcanoes.CleanVolcanicSystem>();
            if (volcanicSystem != null)
            {
                bool shouldEnable = IsVolcanicActivityPossible();
                volcanicSystem.enabled = shouldEnable;

                LogDebug($"{(shouldEnable ? "✅" : "🚫")} Volcans: Noyau {coreTemperature:F0}°C, Seuil {volcanicMinCoreTemp:F0}°C, Croûte {CanCrustForm()}");
            }

            // Système de plaques : vérifier seuil noyau ET croûte
            var plateSystem = FindAnyObjectByType<LifeStory.Geology.SimpleTwoPlateGenerator>();
            if (plateSystem != null)
            {
                bool shouldEnable = IsTectonicActivityPossible();
                plateSystem.enabled = shouldEnable;

                LogDebug($"{(shouldEnable ? "✅" : "🚫")} Plaques tectoniques: Noyau {coreTemperature:F0}°C, Seuil {tectonicMinCoreTemp:F0}°C, Croûte {CanCrustForm()}");
            }

            // Système de rifts continentaux : vérifier seuil noyau ET croûte
            var riftSystem = FindAnyObjectByType<LifeStory.Geology.ContinentalRiftSystem>();
            if (riftSystem != null)
            {
                bool shouldEnable = IsTectonicActivityPossible();
                riftSystem.enabled = shouldEnable;

                LogDebug($"{(shouldEnable ? "✅" : "🚫")} Rifts continentaux: Noyau {coreTemperature:F0}°C, Seuil {tectonicMinCoreTemp:F0}°C, Croûte {CanCrustForm()}");
            }

            // Atmosphère : toujours active en phase géologique
            LogDebug("✅ Atmosphère: Active (dégazage volcanique)");

            // Océans : selon condensation eau
            var oceanSystem = FindAnyObjectByType<LifeStory.Ocean.HydricOceanSystem>();
            if (oceanSystem != null)
            {
                LogDebug("✅ Océans: Système de condensation actif");
            }
        }

        private void ForceInterfaceUpdate()
        {
            var guiSystem = FindAnyObjectByType<GUITabSystem>();
            if (guiSystem != null)
            {
                // L'interface se mettra à jour au prochain frame automatiquement
                LogDebug("🖥️ Interface GUI mise à jour forcée");
            }
        }

        private void EnableBiologicalSystems()
        {
            // Transition vers biomes biologiques
            var biomeSystem = FindAnyObjectByType<Biome.SimplifiedBiomeSystem>();
            if (biomeSystem != null)
            {
                LogDebug("✅ Biomes biologiques activés (vie possible)");
            }
        }

        private float CalculateTimeToTemperature(float targetTemp)
        {
            if (surfaceTemperature <= targetTemp) return 0f;

            float progress = (maxSurfaceTemperature - surfaceTemperature) / (maxSurfaceTemperature - targetTemp);
            float remainingProgress = 1f - progress;

            return remainingProgress * surfaceCoolingDuration;
        }

        private string GetNextTransitionInfo()
        {
            switch (currentPhase)
            {
                case GamePhase.Infernal:
                    float timeToGeological = CalculateTimeToTemperature(crustFormationThreshold);
                    return $"⏳ Croûte dans ~{timeToGeological:F0} Ma";

                case GamePhase.Geological:
                    float timeToEvolution = CalculateTimeToTemperature(biologicalLifeThreshold);
                    return $"⏳ Évolution dans ~{timeToEvolution:F0} Ma";

                case GamePhase.Evolution:
                    return "🌱 Phase finale - Évolution active";

                default:
                    return "";
            }
        }



        private void OnVolcanicEmission(LifeStory.Geology.VolcanoType volcanoType, float co2Amount, float ch4Amount)
        {
            if (!enableVolcanicGasIntegration) return;

            // Ajouter aux totaux
            totalVolcanicCO2 += co2Amount;
            totalVolcanicCH4 += ch4Amount;

            // Ajouter aux niveaux atmosphériques
            if (co2Amount > 0f)
            {
                float adjustedCO2 = co2Amount * volcanicCO2Multiplier;
                co2Level += adjustedCO2;
                co2Level = Mathf.Clamp01(co2Level);

                if (showVolcanicEmissionLogs)
                {
                    LogDebug($"🌋 {volcanoType} → CO₂: +{adjustedCO2:F6} (total: {co2Level:F4})");
                }
            }

            if (ch4Amount > 0f)
            {
                float adjustedCH4 = ch4Amount * volcanicCH4Multiplier;
                methaneLevel += adjustedCH4;
                methaneLevel = Mathf.Clamp01(methaneLevel);

                if (showVolcanicEmissionLogs)
                {
                    LogDebug($"🌋 {volcanoType} → CH₄: +{adjustedCH4:F6} (total: {methaneLevel:F4})");
                }
            }

            // Notifier changements atmosphériques
            OnGasLevelsChanged?.Invoke(nitrogenLevel, methaneLevel, co2Level, oxygenLevel);
        }

        private void Update()
        {
            if (!isPaused)
            {
                UpdateGameTime();
                UpdateSurfaceTemperature();
                UpdateCoreTemperature();
                UpdateAtmosphericSystem();

                // ✅ NOUVEAU : Activation indépendante des systèmes selon seuils
                CheckSystemActivations();

                // Garder seulement pour l'interface
                UpdatePhaseForUI();
            }
        }

        private void UpdatePhaseForUI()
        {
            GamePhase uiPhase = DetermineUIPhase();

            if (currentPhase != uiPhase)
            {
                LogDebug($"🖥️ Interface - Phase UI: {currentPhase} → {uiPhase}");
                currentPhase = uiPhase;

                // Time scale selon phase UI (optionnel)
                float baseTimeScale = GetBaseTimeScaleForPhase(uiPhase);
                currentTimeScale = baseTimeScale * playerTimeMultiplier;

                // Notifier interface
                OnPhaseChanged?.Invoke(uiPhase);
                OnTimeScaleChanged?.Invoke(currentTimeScale);
            }
        }

        private GamePhase DetermineUIPhase()
        {
            if (!HasStableCrust)
            {
                return GamePhase.Infernal; // Formation planète
            }
            else if (!CanLifeEmerge())
            {
                return GamePhase.Geological; // Géologie active, pas encore de vie
            }
            else
            {
                return GamePhase.Evolution; // Géologie + Vie coexistent
            }
        }
        private void CheckSystemActivations()
        {
            CheckVolcanicSystemActivation();
            CheckTectonicSystemActivation();
            CheckAtmosphericSystemActivation();
            CheckOceanSystemActivation();
            CheckBiologicalSystemActivation();
        }

        private void CheckVolcanicSystemActivation()
        {
            bool shouldBeActive = IsVolcanicActivityPossible();
            var volcanicSystem = FindAnyObjectByType<LifeStory.Volcanoes.CleanVolcanicSystem>();

            if (volcanicSystem != null && volcanicSystem.enabled != shouldBeActive)
            {
                volcanicSystem.enabled = shouldBeActive;
                LogDebug($"🌋 Volcans: {(shouldBeActive ? "ACTIVÉS" : "DÉSACTIVÉS")} - Noyau: {coreTemperature:F0}°C, Croûte: {HasStableCrust}");
            }
        }
        private void CheckTectonicSystemActivation()
        {
            bool shouldBeActive = IsTectonicActivityPossible();

            // Plaques tectoniques
            var plateSystem = FindAnyObjectByType<LifeStory.Geology.SimpleTwoPlateGenerator>();
            if (plateSystem != null && plateSystem.enabled != shouldBeActive)
            {
                plateSystem.enabled = shouldBeActive;
                LogDebug($"🏔️ Plaques: {(shouldBeActive ? "ACTIVÉES" : "DÉSACTIVÉES")} - Noyau: {coreTemperature:F0}°C");
            }

            // Rifts continentaux
            var riftSystem = FindAnyObjectByType<LifeStory.Geology.ContinentalRiftSystem>();
            if (riftSystem != null && riftSystem.enabled != shouldBeActive)
            {
                riftSystem.enabled = shouldBeActive;
                LogDebug($"🌊 Rifts: {(shouldBeActive ? "ACTIVÉS" : "DÉSACTIVÉS")} - Noyau: {coreTemperature:F0}°C");
            }
        }
        private void CheckAtmosphericSystemActivation()
        {
            // L'atmosphère est gérée dans UpdateAtmosphericSystem()
            // Pas besoin d'activation/désactivation de composant
            // Juste s'assurer que ça fonctionne selon température
        }

        private void CheckOceanSystemActivation()
        {
            bool hasLiquidWater = waterLevel > 0.01f; // 1% eau liquide minimum
            var oceanSystem = FindAnyObjectByType<LifeStory.Ocean.HydricOceanSystem>();

            if (oceanSystem != null)
            {
                // Les océans se gèrent automatiquement selon waterLevel
                // Pas besoin d'activation/désactivation manuelle
            }
        }

        private void CheckBiologicalSystemActivation()
        {
            bool canLifeExist = CanLifeEmerge();

            // Activer systèmes biologiques si conditions favorables
            var biomeSystem = FindAnyObjectByType<Biome.SimplifiedBiomeSystem>();
            if (biomeSystem != null)
            {
                // SimplifiedBiomeSystem gère déjà selon température
                // Pas besoin de désactivation forcée
            }

            var cleanBiomeSystem = FindAnyObjectByType<LifeStory.Biomes.CleanBiomeSystem>();
            if (cleanBiomeSystem != null)
            {
                // Laisser CleanBiomeSystem gérer selon ses propres seuils
            }
        }



        private void InitializeGame()
        {
            // ✅ NOUVEAU - Démarrer en phase appropriée selon température initiale
            GamePhase initialPhase = DetermineCurrentPhase();
            SetPhase(initialPhase);

            LogDebug($"🌍 Life Story initialisé - Phase initiale: {initialPhase}");
            LogDebug($"   Surface: {surfaceTemperature:F0}°C, Noyau: {coreTemperature:F0}°C");
        }

        //private void OnVolcanicWaterEmission(float volcanicWaterAmount)
        //{
        //    if (!enableVolcanicGasIntegration) return;

        //    // Accélération massive par volcans
        //    float adjustedWaterAmount = volcanicWaterAmount * volcanicWaterMultiplier;
        //    vaporLevel += adjustedWaterAmount;
        //    totalVolcanicWaterContribution += adjustedWaterAmount;

        //    if (showWaterSourceLogs)
        //    {
        //        LogDebug($"🌋 Émission volcanique eau: +{adjustedWaterAmount:F6} (brut: {volcanicWaterAmount:F6}) [Total volcanique: {totalVolcanicWaterContribution:F4}]");
        //    }

        //    // Notifier changements niveau eau
        //    OnWaterLevelChanged?.Invoke(waterLevel + vaporLevel + iceLevel);
        //}



        private void UpdateGameTime()
        {
            float deltaTime = Time.deltaTime * (currentTimeScale / 1000f);
            planetAge += deltaTime;

            // Notifier les systèmes de l'avancement du temps
            OnPlanetAgeChanged?.Invoke(planetAge);
        }

        // ✅ NOUVEAU - Mise à jour température surface (basé sur ancien système)
        private void UpdateSurfaceTemperature()
        {
            if (planetAge < surfaceCoolingDuration)
            {
                // Calculer la température surface selon l'âge
                float coolingProgress = planetAge / surfaceCoolingDuration;
                float coolingFactor = surfaceCoolingCurve.Evaluate(coolingProgress);

                // Température instantanée selon l'âge
                float newSurfaceTemperature = Mathf.Lerp(surfaceStabilizationTemp, maxSurfaceTemperature, coolingFactor);

                // Vérifier changement de climat seulement si changement significatif
                //if (Mathf.Abs(newSurfaceTemperature - surfaceTemperature) > 0.5f)
                {
                    //LogDebug($"🔥 Mise à jour température surface: {surfaceTemperature:F0}°C → {newSurfaceTemperature:F0}°C (Âge: {planetAge:F1}Ma)");
                    ClimateState oldClimate = currentClimate;
                    surfaceTemperature = newSurfaceTemperature;

                    ClimateState newClimate = GetClimateFromTemperature(surfaceTemperature);
                    if (newClimate != currentClimate)
                    {
                        currentClimate = newClimate;
                        OnClimateChanged?.Invoke(newClimate);
                        LogDebug($"Changement climatique: {oldClimate} → {newClimate} (Surface: {surfaceTemperature:F0}°C, Âge: {planetAge:F1}Ma)");
                    }
                    //Debug.Log($"🔥 AVANT event: {OnSurfaceTemperatureChanged != null}");
                    OnSurfaceTemperatureChanged?.Invoke(surfaceTemperature);
                    //Debug.Log($"🔥 APRÈS event déclenché: {surfaceTemperature}°C");
                }
                //else
                {
                    surfaceTemperature = newSurfaceTemperature;
                }
            }
        }

        // ✅ NOUVEAU - Mise à jour température noyau (système indépendant)
        private void UpdateCoreTemperature()
        {
            if (planetAge < coreCoolingDuration)
            {
                // Calculer la température noyau selon l'âge (courbe indépendante)
                float coreCoolingProgress = planetAge / coreCoolingDuration;
                float coreCoolingFactor = coreCoolingCurve.Evaluate(coreCoolingProgress);

                // Température noyau instantanée selon l'âge
                float newCoreTemperature = Mathf.Lerp(minCoreTemperature, maxCoreTemperature, coreCoolingFactor);
                float oldCoreTemperature = coreTemperature;
                coreTemperature = newCoreTemperature;
                // Vérifier changement significatif
                if (Mathf.Abs(newCoreTemperature - oldCoreTemperature) > 0.1f)
                {
                    OnCoreTemperatureChanged?.Invoke(coreTemperature);

                    // Log seulement pour changements significatifs
                    if (Mathf.Abs(newCoreTemperature - oldCoreTemperature) > 10f)
                    {
                        LogDebug($"🔥 Core: {coreTemperature:F0}°C | Surface: {surfaceTemperature:F0}°C (Âge: {planetAge:F1}Ma)");
                    }
                }
            }   //LogDebug($"🔥 Core: {coreTemperature}"); ///// verification de la temperature du core
        }

        private ClimateState GetClimateFromTemperature(float temperature)
        {
            if (temperature >= 1000f)
                return ClimateState.Hellish;    // Lave partout
            else if (temperature >= 200f)
                return ClimateState.Hot;        // Très chaud, vapeur d'eau seulement
            else if (temperature >= 100f)
                return ClimateState.Warm;       // Chaud, eau bout encore
            else if (temperature >= 50f)
                return ClimateState.Temperate;  // Tempéré, eau liquide possible
            else if (temperature >= 0f)
                return ClimateState.Cold;       // Froid mais eau liquide
            else
                return ClimateState.Frozen;     // Gelé, eau solide seulement
        }

        private void UpdateAtmosphericSystem()
        {
            // ✅ NOUVEAU - Vérifier que nous ne sommes pas en phase infernale
            if (currentPhase == GamePhase.Infernal)
            {
                // En phase infernale, pas d'atmosphère stable possible
                return;
            }

            if (currentPhase == GamePhase.Geological)
            {
                // ÉTAPE 1: Libération progressive des gaz (incluant vapeur d'eau)
                UpdateGasRelease();

                // ÉTAPE 2: Évolution chimique de l'atmosphère
                UpdateAtmosphericChemistry();

                // ÉTAPE 3: Répartition vapeur/liquide/glace pour l'eau
                UpdateWaterStatesConfigurable();

                // ÉTAPE 4: Déterminer composition atmosphérique globale
                UpdateAtmosphereComposition();
            }
            else if (currentPhase == GamePhase.Evolution)
            {
                // Phase évolution : début production d'oxygène
                UpdateBiologicalOxygenProduction();
                UpdateAtmosphereComposition();
            }
        }

        private void UpdateGasRelease()
        {
            // === PHASE 1: DÉGAZAGE ATMOSPHÉRIQUE PRÉCOCE ===
            if (enableEarlyDegassing && surfaceTemperature <= earlyDegassingTemp && surfaceTemperature >= waterReleaseEndTemp)
            {
                Debug.Log($"🌫️ Dégazage atmosphérique setting: enable: {enableEarlyDegassing:F0}, surface temperature: {surfaceTemperature} °C, water relese end temperatur {waterReleaseEndTemp} °C");
                if (!earlyDegassingActive)
                {
                    earlyDegassingActive = true;
                    LogDebug($"🌫️ DÉBUT dégazage atmosphérique précoce à {surfaceTemperature:F0}°C");
                }

                // Dégazage atmosphérique LENT mais constant
                float atmosphericWaterRelease = earlyDegassingRate * Time.deltaTime;
                Debug.Log($"🌫️ Dégazage atmosphérique: +{atmosphericWaterRelease:F6}/s (température: {surfaceTemperature:F0}°C)");
                vaporLevel += atmosphericWaterRelease;
                vaporLevel = Mathf.Clamp01(vaporLevel); // Assurer que la vapeur ne dépasse pas 1
                Debug.Log($"🌫️ Dégazage atmosphérique: +{atmosphericWaterRelease:F6} (vapeur actuelle: {vaporLevel:F4})");
                totalAtmosphericWaterRelease += atmosphericWaterRelease;

                // Dégazage gaz aussi (existant, mais réduit)
                float releaseSpeed = Time.deltaTime * 0.03f; // Plus lent que l'original

                float releaseProgress = Mathf.InverseLerp(earlyDegassingTemp, waterReleaseEndTemp, surfaceTemperature);
                releaseProgress = waterReleaseCurve.Evaluate(releaseProgress);

                // Libération d'azote (majoritaire et stable)
                float targetNitrogen = releaseProgress * maxNitrogenRelease * 0.5f; // Commence à 50%
                nitrogenLevel = Mathf.MoveTowards(nitrogenLevel, targetNitrogen, releaseSpeed);

                // Libération de méthane (important au début)
                float targetMethane = releaseProgress * maxMethaneRelease * 0.3f; // Commence plus bas
                methaneLevel = Mathf.MoveTowards(methaneLevel, targetMethane, releaseSpeed);

                // Libération de CO₂ (modérée initialement)
                float targetCO2 = releaseProgress * maxCO2Release * 0.2f; // Commence très bas
                co2Level = Mathf.MoveTowards(co2Level, targetCO2, releaseSpeed);

                OnGasLevelsChanged?.Invoke(nitrogenLevel, methaneLevel, co2Level, oxygenLevel);

                if (showWaterSourceLogs && Time.frameCount % 300 == 0) // Log toutes les 5s environ
                {
                    LogDebug($"💧 Dégazage atmosphérique: +{atmosphericWaterRelease:F6}/s (total: {totalAtmosphericWaterRelease:F4})");
                }
            }
            else if (earlyDegassingActive && surfaceTemperature > earlyDegassingTemp)
            {
                earlyDegassingActive = false;
                LogDebug($"🔥 FIN dégazage atmosphérique - température trop élevée: {surfaceTemperature:F0}°C");
            }

            // === PHASE 2: ACCÉLÉRATION VOLCANIQUE ===
            // (Gérée par OnVolcanicWaterEmission dans la méthode séparée ci-dessous)

            // === DÉGAZAGE ORIGINAL (température plus basse) ===
            if (surfaceTemperature <= waterReleaseStartTemp && surfaceTemperature >= waterReleaseEndTemp && surfaceTemperature < earlyDegassingTemp)
            {
                // Système original pour finaliser la libération
                float releaseProgress = Mathf.InverseLerp(waterReleaseStartTemp, waterReleaseEndTemp, surfaceTemperature);
                releaseProgress = waterReleaseCurve.Evaluate(releaseProgress);

                float releaseSpeed = Time.deltaTime * 0.08f;

                // Finaliser libération gaz
                float targetNitrogen = releaseProgress * maxNitrogenRelease;
                nitrogenLevel = Mathf.MoveTowards(nitrogenLevel, targetNitrogen, releaseSpeed);

                float targetMethane = releaseProgress * maxMethaneRelease;
                methaneLevel = Mathf.MoveTowards(methaneLevel, targetMethane, releaseSpeed);

                float targetCO2 = releaseProgress * maxCO2Release * 0.3f;
                co2Level = Mathf.MoveTowards(co2Level, targetCO2, releaseSpeed);

                OnGasLevelsChanged?.Invoke(nitrogenLevel, methaneLevel, co2Level, oxygenLevel);
            }
            else if (surfaceTemperature > earlyDegassingTemp)
            {
                // Trop chaud : pas de gaz libérés
                nitrogenLevel = 0f;
                methaneLevel = 0f;
                co2Level = 0f;
                oxygenLevel = 0f;
                vaporLevel = 0f;
                waterLevel = 0f;
                iceLevel = 0f;
            }
        }



        private void UpdateAtmosphericChemistry()
        {
            float conversionSpeed = Time.deltaTime * methaneToC02Rate;

            // ✅ BASÉ SUR TEMPÉRATURE SURFACE (chimie atmosphérique)
            if (surfaceTemperature < methaneStabilityTemp && methaneLevel > 0.01f)
            {
                float methaneToConvert = Mathf.Min(methaneLevel * conversionSpeed, methaneLevel);

                // Facteur de conversion selon température (plus froid = plus rapide)
                float tempFactor = Mathf.InverseLerp(methaneStabilityTemp, co2ConversionStartTemp, surfaceTemperature);
                methaneToConvert *= tempFactor;

                if (methaneToConvert > 0.001f)
                {
                    methaneLevel -= methaneToConvert;
                    co2Level += methaneToConvert * 0.8f; // Légère perte dans la conversion

                    //Debug.Log($"Conversion atmosphérique: CH₄({methaneLevel:P1}) → CO₂({co2Level:P1}) à {surfaceTemperature:F0}°C");
                }
            }
        }

        // NOUVELLE MÉTHODE : Production biologique d'oxygène (phase Evolution)
        private void UpdateBiologicalOxygenProduction()
        {
            if (currentPhase == GamePhase.Evolution && co2Level > 0.01f)
            {
                float oxygenProduction = co2Level * co2ToOxygenRate * Time.deltaTime;

                co2Level -= oxygenProduction;
                oxygenLevel += oxygenProduction * 0.6f; // Efficacité photosynthèse primitive

                // S'assurer que CO₂ ne devient pas négatif
                co2Level = Mathf.Max(co2Level, 0.02f); // Minimum résiduel

                OnGasLevelsChanged?.Invoke(nitrogenLevel, methaneLevel, co2Level, oxygenLevel);
            }
        }

        // NOUVELLE MÉTHODE : Déterminer composition atmosphérique
        private void UpdateAtmosphereComposition()
        {
            AtmosphereComposition newComposition;

            float totalGas = nitrogenLevel + methaneLevel + co2Level + oxygenLevel;

            if (totalGas < 0.05f)
            {
                newComposition = AtmosphereComposition.None;
            }
            else if (methaneLevel > 0.05f && oxygenLevel < 0.01f)
            {
                newComposition = AtmosphereComposition.Primitive; // CH₄ dominant
            }
            else if (co2Level > 0.10f && oxygenLevel < 0.05f)
            {
                newComposition = AtmosphereComposition.Reducing; // CO₂ dominant
            }
            else if (oxygenLevel > 0.05f && oxygenLevel < 0.18f)
            {
                newComposition = AtmosphereComposition.Oxidizing; // O₂ émergent
            }
            else if (oxygenLevel >= 0.18f)
            {
                newComposition = AtmosphereComposition.Balanced; // Atmosphère terrestre
            }
            else
            {
                newComposition = AtmosphereComposition.Reducing;
            }

            if (newComposition != currentAtmosphere)
            {
                AtmosphereComposition oldAtmosphere = currentAtmosphere;
                currentAtmosphere = newComposition;
                OnAtmosphereChanged?.Invoke(newComposition);
                //Debug.Log($"Évolution atmosphérique: {oldAtmosphere} → {newComposition}");
            }
        }

        private void UpdateWaterStatesConfigurable()
        {
            float totalWater = vaporLevel + waterLevel + iceLevel;

            if (totalWater <= 0.001f)
            {
                vaporLevel = 0f;
                waterLevel = 0f;
                iceLevel = 0f;
                return;
            }

            float transitionSpeed = Time.deltaTime * 0.5f;

            if (surfaceTemperature > condensationStartTemp)
            {
                // Trop chaud : tout en vapeur
                float targetVapor = totalWater;
                vaporLevel = Mathf.MoveTowards(vaporLevel, targetVapor, transitionSpeed);
                waterLevel = Mathf.MoveTowards(waterLevel, 0f, transitionSpeed);
                iceLevel = Mathf.MoveTowards(iceLevel, 0f, transitionSpeed);
            }
            else if (surfaceTemperature > 0f)
            {
                // Distribution configurable vapeur/liquide
                float coolnessFactor = Mathf.InverseLerp(condensationStartTemp, maxCondensationTemp, surfaceTemperature);

                // Calculer ratios selon paramètres configurables
                float liquidRatio = coolnessFactor * maxLiquidConversionRatio;
                float vaporRatio = Mathf.Max(minVaporRetention, 1f - liquidRatio);

                // Ajuster si total > 1 (sécurité)
                float totalRatio = liquidRatio + vaporRatio;
                if (totalRatio > 1f)
                {
                    liquidRatio /= totalRatio;
                    vaporRatio /= totalRatio;
                }

                float targetWater = totalWater * liquidRatio;
                float targetVapor = totalWater * vaporRatio;

                waterLevel = Mathf.MoveTowards(waterLevel, targetWater, transitionSpeed);
                vaporLevel = Mathf.MoveTowards(vaporLevel, targetVapor, transitionSpeed);
                iceLevel = Mathf.MoveTowards(iceLevel, 0f, transitionSpeed);

                // Debug configurable
                if (enableWaterDistributionLogs && Time.frameCount % 600 == 0)
                {
                    LogDebug($"💧 Distribution configurable - Total: {totalWater:P1}");
                    LogDebug($"   Liquide: {targetWater:P1} ({liquidRatio:P1}) | Vapeur: {targetVapor:P1} ({vaporRatio:P1})");
                    LogDebug($"   Température: {surfaceTemperature:F0}°C | Coolness: {coolnessFactor:F2}");
                }
            }
            else
            {
                // Trop froid : tout gelé
                float targetIce = totalWater;
                iceLevel = Mathf.MoveTowards(iceLevel, targetIce, transitionSpeed);
                waterLevel = Mathf.MoveTowards(waterLevel, 0f, transitionSpeed);
                vaporLevel = Mathf.MoveTowards(vaporLevel, 0f, transitionSpeed);
            }

            OnWaterLevelChanged?.Invoke(waterLevel);
        }

        private WaterState GetWaterStateFromTemperature(float temperature)
        {
            float totalWater = vaporLevel + waterLevel + iceLevel;

            if (totalWater < 0.01f)
                return WaterState.Vapor; // Pas encore d'eau libérée
            else if (temperature > 150f)
                return WaterState.Vapor;
            else if (temperature < -10f)
                return WaterState.Ice;
            else if (waterLevel > 0.3f)
                return WaterState.Liquid;
            else
                return WaterState.Mixed;
        }

        private void CheckPhaseTransition()
        {
            if (!autoTransition) return;

            GamePhase determinedPhase = DetermineCurrentPhase();

            if (determinedPhase != currentPhase)
            {
                LogDebug($"🚨 TRANSITION DÉTECTÉE: {currentPhase} → {determinedPhase}");
                LogDebug($"   Âge: {planetAge:F0} Ma");
                LogDebug($"   Surface: {surfaceTemperature:F0}°C | Noyau: {coreTemperature:F0}°C");
                LogDebug($"   Différentiel: {ThermalDifferential:F0}°C");
                LogDebug($"   Croûte possible: {CanCrustForm()}");

                SetPhase(determinedPhase);
            }
        }

        // Dans GameManager.cs - Correction du bug condition eau
        private bool CanLifeEmerge()
        {
            bool temperatureOK = surfaceTemperature >= 5f && surfaceTemperature <= 80f;
            bool waterOK = waterLevel >= 0.3f; // 30% eau liquide
            float totalAtmosphere = nitrogenLevel + methaneLevel + co2Level;
            bool atmosphereOK = totalAtmosphere > 0.20f; // 20% atmosphère
            bool stable = surfaceTemperature < 200f; // Pas de volatilité extreme

            return temperatureOK && waterOK && atmosphereOK && stable;
        }



        public void SetPhase(GamePhase newPhase)
        {
            if (currentPhase != newPhase)
            {
                LogDebug($"🔄 Changement phase: {currentPhase} → {newPhase}");
                GamePhase oldPhase = currentPhase;
                currentPhase = newPhase;

                // Calcul échelle de temps selon la nouvelle phase
                float baseTimeScale = GetBaseTimeScaleForPhase(newPhase);

                if (newPhase != GamePhase.Paused)
                {
                    float oldTimeScale = currentTimeScale;
                    currentTimeScale = baseTimeScale * playerTimeMultiplier;
                    LogDebug($"🕐 TimeScale: {oldTimeScale:F0} → {currentTimeScale:F0} (Base:{baseTimeScale:F0} × Mult:{playerTimeMultiplier:F1})");
                }
                else
                {
                    currentTimeScale = 0f;
                }

                // Notifier tous les systèmes du changement
                OnPhaseChanged?.Invoke(newPhase);
                OnTimeScaleChanged?.Invoke(currentTimeScale);

                // Configurer systèmes selon la phase
                //ConfigureSystemsForPhase(newPhase);

                // ✅ NOUVEAU - Forcer mise à jour interface
                ForceInterfaceUpdate();
            }
        }

        public void TransitionToEvolution()
        {
            //LogToEvolutionDebugger("TransitionToEvolution", "TRANSITION CRITIQUE vers Evolution");
            ////Debug.Log($"Conditions favorables atteintes ! Transition vers la phase d'évolution à {planetAge:F1} millions d'années");
            SetPhase(GamePhase.Evolution);
        }

        public void PauseGame()
        {
            isPaused = true;
            SetPhase(GamePhase.Paused);
        }

        public void SetTimeScale(float multiplier)
        {
            // Utiliser EXACTEMENT la même logique que TestSetPlayerMultiplier qui fonctionne !
            //Debug.Log($"🎮 SetTimeScale: Changement multiplicateur {playerTimeMultiplier} → {multiplier}");

            playerTimeMultiplier = Mathf.Clamp(multiplier, 0.1f, 10f);

            if (currentPhase != GamePhase.Paused)
            {
                float baseTimeScale = currentPhase == GamePhase.Geological ? geologicalTimeScale : evolutionTimeScale;
                float oldTimeScale = currentTimeScale;
                currentTimeScale = baseTimeScale * playerTimeMultiplier;

                //Debug.Log($"🎮 SetTimeScale résultat: {oldTimeScale} → {currentTimeScale} (Base:{baseTimeScale} × Mult:{playerTimeMultiplier})");
                OnTimeScaleChanged?.Invoke(currentTimeScale);
            }
            else
            {
                //Debug.Log("⏸️ SetTimeScale: Jeu en pause - changement ignoré");
            }
        }

        private System.Collections.IEnumerator RestartAfterCleanup()
        {
            yield return new WaitForSecondsRealtime(1f); // Attendre nettoyage

            // Réinitialiser les valeurs initiales
            InitializeGame();
            Time.timeScale = 1f;

            Debug.Log("🔄 Simulation redémarrée proprement");
        }

        private void OnApplicationQuit()
        {
            CloseSimulation();
        }



        public void ResumeGame()
        {
            isPaused = false;

            // ✅ NOUVEAU - Déterminer la phase appropriée selon température actuelle
            GamePhase appropriatePhase = DetermineCurrentPhase();
            SetPhase(appropriatePhase);

            LogDebug($"🎮 Jeu repris - Phase: {appropriatePhase} - Multiplicateur: ×{playerTimeMultiplier}");
        }

        public float GetPlayerTimeMultiplier()
        {
            return playerTimeMultiplier;
        }

        public float GetBaseTimeScale()
        {
            return GetBaseTimeScaleForPhase(currentPhase);
        }

        // ✅ PROPRIÉTÉS PUBLIQUES - Système Thermique Dual
        public GamePhase CurrentPhase => currentPhase;
        public float PlanetAge => planetAge;
        public float CurrentTimeScale => currentTimeScale;
        public bool IsPaused => isPaused;

        // ✅ PROPRIÉTÉ LEGACY POUR COMPATIBILITÉ TEMPORAIRE (À SUPPRIMER PLUS TARD)
        [System.Obsolete("Utiliser SurfaceTemperature à la place")]
        public float PlanetTemperature => surfaceTemperature;  // ← TEMPORAIRE pour éviter erreurs compilation

        // Méthodes utilitaires
        public string GetFormattedAge()
        {
            if (planetAge < 1f)
                return $"{planetAge * 1000f:F0} mille ans";
            else if (planetAge < 1000f)
                return $"{planetAge:F1} millions d'années";
            else
                return $"{planetAge / 1000f:F2} milliards d'années";
        }

        private void LogDebug(string message)
        {
            Debug.Log($"[GameManager] {message}");
        }

        // ✅ MÉTHODES DE TEST - Système Thermique Dual

        [ContextMenu("🚨 Debug Transition Complet")]
        public void DebugTransitionComplet()
        {
            DebugTransitionStatus();
            LogDebug($"--- État systèmes ---");
            LogDebug($"Volcans possibles: {IsVolcanicActivityPossible()}");
            LogDebug($"Tectonique possible: {IsTectonicActivityPossible()}");
            LogDebug($"Time scale actuel: {currentTimeScale:F0}x");
        }


        [ContextMenu("🚪 Close Simulation")]
        public void CloseSimulation()
        {
            Debug.Log("🚪 === FERMETURE SIMULATION ===");

            // REMPLACEZ tout le contenu par :
            MemoryCleanupManager.Instance.CleanupAfterSimulation();

            // Réinitialiser phase
            currentPhase = GamePhase.Geological;

            Debug.Log("✅ === SIMULATION FERMÉE ===");
        }

        [ContextMenu("🔍 Test Formation Croûte")]
        public void TestCrustFormation()
        {
            LogDebug("=== TEST FORMATION CROÛTE ===");
            LogDebug($"Surface: {surfaceTemperature:F0}°C (seuil: {crustFormationThreshold:F0}°C)");
            LogDebug($"Noyau: {coreTemperature:F0}°C");
            LogDebug($"Différentiel: {ThermalDifferential:F0}°C (min: {minThermalDifferential:F0}°C)");
            LogDebug($"Croûte possible: {CanCrustForm()}");
            LogDebug($"Volcans possibles: {IsVolcanicActivityPossible()}");
            LogDebug($"Tectonique possible: {IsTectonicActivityPossible()}");
            LogDebug($"Phase déterminée: {DetermineCurrentPhase()}");
        }

        [ContextMenu("🔍 Test Tous Seuils")]
        public void TestAllThresholds()
        {
            LogDebug("=== TEST TOUS SEUILS ===");
            LogDebug($"Surface: {surfaceTemperature:F0}°C | Noyau: {coreTemperature:F0}°C");
            LogDebug($"Croûte formée: {HasStableCrust} (seuil: {crustFormationThreshold:F0}°C)");
            LogDebug($"Volcans possibles: {IsVolcanicActivityPossible()} (noyau: {volcanicMinCoreTemp:F0}-{volcanicMaxCoreTemp:F0}°C)");
            LogDebug($"Tectonique possible: {IsTectonicActivityPossible()} (noyau: ≥{tectonicMinCoreTemp:F0}°C)");

            // Vérifier si CanLifeEmerge existe, sinon utiliser une condition simple
            bool lifeConditions = surfaceTemperature < biologicalLifeThreshold && waterLevel > 0.3f;
            LogDebug($"Vie possible: {lifeConditions} (surface: ≤{biologicalLifeThreshold:F0}°C)");
        }

        [ContextMenu("💾 Sauver Seuils")]
        public void SaveCurrentThresholds()
        {
            PlayerPrefs.SetFloat("CrustThreshold", crustFormationThreshold);
            PlayerPrefs.SetFloat("VolcanicMinCore", volcanicMinCoreTemp);
            PlayerPrefs.SetFloat("VolcanicMaxCore", volcanicMaxCoreTemp);
            PlayerPrefs.SetFloat("TectonicMinCore", tectonicMinCoreTemp);
            PlayerPrefs.SetFloat("BiologicalThreshold", biologicalLifeThreshold);
            PlayerPrefs.SetFloat("SurfaceDuration", surfaceCoolingDuration);
            PlayerPrefs.SetFloat("CoreDuration", coreCoolingDuration);
            PlayerPrefs.Save();
            LogDebug("💾 Seuils sauvegardés");
        }

        [ContextMenu("📁 Charger Seuils")]
        public void LoadSavedThresholds()
        {
            if (PlayerPrefs.HasKey("CrustThreshold"))
            {
                crustFormationThreshold = PlayerPrefs.GetFloat("CrustThreshold");
                volcanicMinCoreTemp = PlayerPrefs.GetFloat("VolcanicMinCore");
                volcanicMaxCoreTemp = PlayerPrefs.GetFloat("VolcanicMaxCore");
                tectonicMinCoreTemp = PlayerPrefs.GetFloat("TectonicMinCore");
                biologicalLifeThreshold = PlayerPrefs.GetFloat("BiologicalThreshold");
                surfaceCoolingDuration = PlayerPrefs.GetFloat("SurfaceDuration");
                coreCoolingDuration = PlayerPrefs.GetFloat("CoreDuration");
                LogDebug("📁 Seuils chargés");
            }
            else
            {
                LogDebug("❌ Aucune sauvegarde trouvée");
            }
        }

        [ContextMenu("🔄 Reset Seuils Défaut")]
        public void ResetThresholdsToDefault()
        {
            crustFormationThreshold = 800f;
            volcanicMinCoreTemp = 2200f;
            volcanicMaxCoreTemp = 4200f;
            tectonicMinCoreTemp = 2400f;
            biologicalLifeThreshold = 25f;
            surfaceCoolingDuration = 900f;
            coreCoolingDuration = 1200f;
            LogDebug("🔄 Seuils remis aux valeurs par défaut");
        }

        [ContextMenu("⚙️ Ajuster Seuils Automatiquement")]
        public void AutoCalibrateThermalThresholds()
        {
            // Formation croûte : quand différentiel devient raisonnable
            float targetCrustAge = surfaceCoolingDuration * 0.4f; // 40% du refroidissement surface
            float progress = targetCrustAge / surfaceCoolingDuration;
            float surfaceAtCrust = Mathf.Lerp(surfaceStabilizationTemp, maxSurfaceTemperature, surfaceCoolingCurve.Evaluate(1f - progress));

            // Noyau correspondant
            float coreProgress = targetCrustAge / coreCoolingDuration;
            float coreAtCrust = Mathf.Lerp(minCoreTemperature, maxCoreTemperature, coreCoolingCurve.Evaluate(1f - coreProgress));

            // Calculer différentiel optimal
            float optimalDifferential = coreAtCrust - surfaceAtCrust;

            LogDebug("=== CALIBRAGE AUTOMATIQUE ===");
            LogDebug($"Âge formation croûte: {targetCrustAge:F0} Ma");
            LogDebug($"Surface à {targetCrustAge:F0} Ma: {surfaceAtCrust:F0}°C");
            LogDebug($"Noyau à {targetCrustAge:F0} Ma: {coreAtCrust:F0}°C");
            LogDebug($"Différentiel optimal: {optimalDifferential:F0}°C");

            // Suggestions
            LogDebug($"SUGGESTION - crustFormationThreshold: {surfaceAtCrust:F0}°C");
            LogDebug($"SUGGESTION - minThermalDifferential: {optimalDifferential:F0}°C");
            LogDebug($"SUGGESTION - volcanicMinCoreTemp: {(coreAtCrust - 200f):F0}°C");
        }

        private void ShowThermalSystemStatus()
        {
            LogDebug("=== STATUT SYSTÈME THERMIQUE ===");
            LogDebug($"Phase actuelle: {currentPhase}");
            LogDebug($"Surface: {surfaceTemperature:F0}°C | Noyau: {coreTemperature:F0}°C");
            LogDebug($"Différentiel thermique: {ThermalDifferential:F0}°C");
            LogDebug($"Croûte stable: {HasStableCrust}");
            LogDebug($"Facteur stabilité: {CrustStabilityFactor:P1}");
            LogDebug($"Volcans possibles: {IsVolcanicActivityPossible()}");
            LogDebug($"Tectonique possible: {IsTectonicActivityPossible()}");
            LogDebug($"Time scale actuel: {currentTimeScale:F0}x");
            LogDebug($"Phase déterminée automatiquement: {DetermineCurrentPhase()}");
        }




        private void OnDestroy()
        {
            if (LifeStory.Volcanoes.CleanVolcanicSystem.OnVolcanicGasEmission != null)
            {
                LifeStory.Volcanoes.CleanVolcanicSystem.OnVolcanicGasEmission -= OnVolcanicEmission;
            }
            if (LifeStory.Volcanoes.CleanVolcanicSystem.OnVolcanicWaterEmission != null)
            {
                LifeStory.Volcanoes.CleanVolcanicSystem.OnVolcanicWaterEmission -= OnVolcanicWaterEmission;
            }
        }
    }
}