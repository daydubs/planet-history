using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public enum PlanetEpoch
{
    Hadean,         // Planète très chaude, lave dominante
    CrustFormation, // Formation de la croûte
    VolcanicAge,    // Activité volcanique intense
    ProtoOcean,     // Eau liquide commence à apparaître
    TectonicDrift,  // Dérive continentale active
    Prebiotic,      // Phase pré-biotique: synthèse des acides aminés
    Photosynthesis, // Emergence de la photosynthèse, nouvelle époque
    CambrianExplosion // Explosion Cambrienne, diversification de la vie multicellulaire
}

public enum SessionLengthPreset
{
    OneHour,
    ThreeHours,
    SixHours,
    TwelveHours,
    Custom
}

[Flags]
public enum LogChannel
{
    None = 0,
    Core = 1 << 0,
    Events = 1 << 1,
    PlayerActions = 1 << 2,
    Perf = 1 << 3
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Temps")]
    [SerializeField] private SessionLengthPreset sessionLengthPreset = SessionLengthPreset.ThreeHours;
    [SerializeField, Min(0.25f)] private float customSessionHours = 3f;
    [SerializeField, Min(1f)] private float baselineSimulationUnits = 5_000_000f;
    [SerializeField, Min(0f)] private float playerSpeedMultiplier = 1f;
    [SerializeField] private bool noPlayerBaseline = true;
    [SerializeField] private float simulationTimeSeconds; // temps total simulé
    [SerializeField] private bool isPaused;

    [Header("Courbes d'évolution scientifiques de la Terre")]
    [SerializeField] private AnimationCurve internalTempCurve;
    [SerializeField] private AnimationCurve surfaceTempBaselineCurve;
    [SerializeField, Min(0f)] private float greenhouseSensitivity = 25f;

    [Header("Variables planétaires")]
    [SerializeField] private float internalTemperature = 5000f; // K
    [SerializeField] private float surfaceTemperature = 1800f;  // K
    [SerializeField] private float pressure = 100f;              // atm arbitraire
    [SerializeField] private float waterRatio = 0f;              // 0..1
    [SerializeField] private float tectonicActivity = 0f;        // 0..1
    [SerializeField] private float impactThermalPulse = 0f;      // K (Choc thermique météorite)
    [SerializeField] private float greenhouseDeltaTemp = 0f;      // K (Réchauffement de serre)

    [Header("Composants de l'Atmosphère (pression partielle en atm)")]
    [SerializeField] private float waterVaporPressure;
    [SerializeField] private float co2Pressure;
    [SerializeField] private float nitrogenPressure = 8f;
    [SerializeField] private float otherGasesPressure;
    [SerializeField] private float oxygenPressure;

    [Header("Océan & Fer")]
    [SerializeField] private float dissolvedIronRatio = 1.0f; // Commence à 1, se dissipe lors du GOE

    [Header("Evenements Meteor")]
    [SerializeField] private float meteorGasesPressure = 0f;
    [SerializeField] private float tsunamiWaterRise = 0f;

    [Header("Etat global")]
    [SerializeField] private PlanetEpoch currentEpoch = PlanetEpoch.Hadean;
    [SerializeField] private bool isPhotosynthesisUnlocked = false;
    [SerializeField] private bool isCambrianExplosionUnlocked = false;

    [Header("Baseline CSV Logger")]
    [SerializeField] private bool enableCsvLogger = true;
    [SerializeField] private LogChannel enabledLogChannels = LogChannel.Core;
    [SerializeField, Min(1f)] private float csvLogIntervalSimulationUnits = 10_000f;
    [SerializeField] private bool logOnlyWhenNoPlayerBaseline = true;

    public event Action<PlanetEpoch> OnEpochChanged;
    public event Action OnSimulationStep;

    // Accès en lecture (les autres systèmes lisent ces valeurs)
    public float SimulationTimeSeconds => simulationTimeSeconds;
    public float InternalTemperature => internalTemperature;
    public float SurfaceTemperature => surfaceTemperature;
    public float Pressure => pressure;
    public float WaterRatio => Mathf.Clamp01(waterRatio + tsunamiWaterRise);
    public float RawWaterRatio => waterRatio; // In case any system specifically wants the un-flooded water ratio
    public float TectonicActivity => tectonicActivity;
    public float WaterVaporPressure => waterVaporPressure;
    public float Co2Pressure => co2Pressure;
    public float NitrogenPressure => nitrogenPressure;
    public float OtherGasesPressure => otherGasesPressure;
    public float MeteorGasesPressure => meteorGasesPressure;
    public float TsunamiWaterRise => tsunamiWaterRise;
    public float ImpactThermalPulse => impactThermalPulse;
    public float GreenhouseDeltaTemp => greenhouseDeltaTemp;
    public float OxygenPressure => oxygenPressure;
    public float DissolvedIronRatio => dissolvedIronRatio;
    public PlanetEpoch CurrentEpoch => currentEpoch;
    public bool IsPhotosynthesisUnlocked => isPhotosynthesisUnlocked;
    public bool IsCambrianExplosionUnlocked => isCambrianExplosionUnlocked;
    public bool IsPaused => isPaused;

    public void UnlockPhotosynthesis()
    {
        isPhotosynthesisUnlocked = true;
    }

    public void UnlockCambrianExplosion()
    {
        isCambrianExplosionUnlocked = true;
    }
    public bool NoPlayerBaseline => noPlayerBaseline;
    public float SessionDurationHours => GetSessionDurationHours();
    public float SimulatedYears => simulationTimeSeconds * 300f;
    public float SimulatedYearsPerRealSecond => GetSimulationUnitsPerRealSecond() * 300f;
    public float SessionProgress => Mathf.Clamp01(simulationTimeSeconds / baselineSimulationUnits);
    public float SessionRemainingHoursAtCurrentSpeed
    {
        get
        {
            float speed = GetSimulationUnitsPerRealSecond();
            if (speed <= 0f) return float.PositiveInfinity;
            float remainingUnits = Mathf.Max(0f, baselineSimulationUnits - simulationTimeSeconds);
            return (remainingUnits / speed) / 3600f;
        }
    }

    private readonly Dictionary<LogChannel, StreamWriter> channelWriters = new Dictionary<LogChannel, StreamWriter>();
    private string currentLogTimestamp;
    private float nextCsvLogAtSimulationTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Optionnel: si tu veux garder entre scènes
        // DontDestroyOnLoad(gameObject);

        // Débloquer le plafond de 30 FPS lié au VSync ou à la configuration de la plateforme par défaut
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        // Initial gas state (approximation of Hadean earth initial state before drift simulation starts)
        waterVaporPressure = 250f;
        co2Pressure = 50f;
        nitrogenPressure = 2f;
        otherGasesPressure = 0f;
        oxygenPressure = 0f;
        dissolvedIronRatio = 1.0f;

        // Ensure initial total pressure is set before first simulation update
        pressure = waterVaporPressure + co2Pressure + nitrogenPressure + otherGasesPressure + oxygenPressure;

        EnsureDefaultCurves();
        EnsureAudioManager();
        EnsureAtmosphereManager();
        EnsureMicrobeManager();
        EnsureWindAndCurrentManager();
        InitializeCsvLogger();
    }

    private void EnsureDefaultCurves()
    {
        if (internalTempCurve == null || internalTempCurve.length == 0)
        {
            internalTempCurve = new AnimationCurve(
                new Keyframe(0.00f, 5000f), // Hadéen précoce (manteau/noyau en fusion)
                new Keyframe(0.15f, 3500f), // Formation de la croûte
                new Keyframe(0.35f, 2200f), // Âge volcanique
                new Keyframe(0.60f, 1700f), // Dérive tectonique & proto-océan
                new Keyframe(1.00f, 1500f)  // Stabilisation thermique mantle
            );
        }

        if (surfaceTempBaselineCurve == null || surfaceTempBaselineCurve.length == 0)
        {
            surfaceTempBaselineCurve = new AnimationCurve(
                new Keyframe(0.00f, 1800f),  // Océan de magma Hadéen
                new Keyframe(0.10f, 1200f),  // Refroidissement croûte solide
                new Keyframe(0.25f, 650f),   // Surface volcanique
                new Keyframe(0.45f, 380f),   // Seuil de condensation de l'eau
                new Keyframe(0.70f, 310f),   // Océans liquides
                new Keyframe(1.00f, 298.15f) // 25 °C cible stabilité prébiotique
            );
        }
    }

    private void EnsureAudioManager()
    {
        if (AudioManager.Instance == null && FindAnyObjectByType<AudioManager>() == null)
        {
            GameObject audioMgrObj = new GameObject("AudioManager");
            audioMgrObj.AddComponent<AudioManager>();
        }
    }

    private void EnsureAtmosphereManager()
    {
        if (FindAnyObjectByType<AtmosphereManager>() == null)
        {
            GameObject atmosphereMgrObj = new GameObject("AtmosphereManager");
            atmosphereMgrObj.AddComponent<AtmosphereManager>();
        }
    }

    private void EnsureMicrobeManager()
    {
        if (FindAnyObjectByType<MicrobeManager>() == null)
        {
            GameObject microbeMgrObj = new GameObject("MicrobeManager");
            microbeMgrObj.AddComponent<MicrobeManager>();
        }
    }

    private void EnsureWindAndCurrentManager()
    {
        if (WindAndCurrentManager.Instance == null && GetComponent<WindAndCurrentManager>() == null)
        {
            gameObject.AddComponent<WindAndCurrentManager>();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        customSessionHours = Mathf.Max(0.25f, customSessionHours);
        baselineSimulationUnits = Mathf.Max(1f, baselineSimulationUnits);
        playerSpeedMultiplier = Mathf.Max(0f, playerSpeedMultiplier);
        csvLogIntervalSimulationUnits = Mathf.Max(1f, csvLogIntervalSimulationUnits);
        EnsureDefaultCurves();
    }
#endif

    private void Update()
    {
        if (isPaused) return;

        float dt = Time.deltaTime * GetSimulationUnitsPerRealSecond();
        simulationTimeSeconds += dt;

        SimulateTemperatures(dt);
        SimulatePressure(dt);
        SimulateWater(dt);
        SimulateTectonics(dt);

        // Décroissance des gaz de météore et de la montée des eaux (raz de marée)
        if (meteorGasesPressure > 0f)
        {
            // Décroissance lente
            meteorGasesPressure -= 0.00005f * dt;
            meteorGasesPressure = Mathf.Max(0f, meteorGasesPressure);
        }

        if (tsunamiWaterRise > 0f)
        {
            // Résorption de la montée des eaux du raz de marée
            tsunamiWaterRise -= 0.00008f * dt;
            tsunamiWaterRise = Mathf.Max(0f, tsunamiWaterRise);
        }

        if (impactThermalPulse > 0f)
        {
            // Dissipation progressive du choc thermique de météore
            impactThermalPulse -= 0.02f * dt;
            impactThermalPulse = Mathf.Max(0f, impactThermalPulse);
        }

        UpdateEpochFromState();
        TryLogCsvSample();

        OnSimulationStep?.Invoke();
    }

    private void OnDestroy()
    {
        CloseCsvLogger();
    }

    private void OnApplicationQuit()
    {
        CloseCsvLogger();
    }

    private void SimulateTemperatures(float dt)
    {
        // 1. La température interne suit la courbe d'évolution scientifique du modèle terrestre
        float progress = SessionProgress;
        if (internalTempCurve != null && internalTempCurve.length > 0)
        {
            internalTemperature = internalTempCurve.Evaluate(progress);
        }
        else
        {
            internalTemperature = Mathf.Max(internalTemperature - 0.001f * dt, 1500f);
        }

        // 2. Température de surface baseline (modèle d'évolution scientifique théorique)
        float baselineSurface = surfaceTempBaselineCurve != null && surfaceTempBaselineCurve.length > 0
            ? surfaceTempBaselineCurve.Evaluate(progress)
            : Mathf.Lerp(298.15f, 1472.2f, Mathf.Clamp01((internalTemperature - 1500f) / 3500f));

        // 3. Couplage physique de l'Effet de Serre (Greenhouse Effect) :
        // Le CO2, la vapeur d'eau (H2O) et les gaz volcaniques/météoritiques (SO2, CH4, etc.)
        // piègent le rayonnement infrarouge et augmentent la température de surface.
        // On utilise la loi physique logarithmique de forçage radiatif du réchauffement de serre :
        float greenhouseGasForcing = co2Pressure + 0.35f * waterVaporPressure + 0.75f * otherGasesPressure;
        // Pression de référence de serre à l'équilibre prébiotique (~2.3 atm)
        float baselineForcing = 2.3f;
        float excessForcing = Mathf.Max(0f, greenhouseGasForcing - baselineForcing);

        greenhouseDeltaTemp = greenhouseSensitivity * Mathf.Log(1f + excessForcing / 8f);

        // 4. Température de surface dynamique globale = Baseline + Effet de Serre + Choc Thermique d'impact
        float targetSurface = baselineSurface + greenhouseDeltaTemp + impactThermalPulse;

        surfaceTemperature = Mathf.MoveTowards(surfaceTemperature, targetSurface, 0.05f * dt);
        surfaceTemperature = Mathf.Max(surfaceTemperature, 100f);
    }

    private void SimulatePressure(float dt)
    {
        // La pression atmosphérique globale évolue de manière dynamique.

        // 1. Natural Outgassing (Volcanic/Tectonic contribution)
        // Les volcans relâchent du CO2, de la vapeur d'eau, et d'autres gaz
        if (tectonicActivity > 0f)
        {
            float outgassingRate = tectonicActivity * dt;
            co2Pressure += 0.00005f * outgassingRate;
            waterVaporPressure += 0.0001f * outgassingRate;
            nitrogenPressure += 0.00001f * outgassingRate;
            otherGasesPressure += 0.00002f * outgassingRate;
        }

        // 2. Add temporary meteor gases (already handled in simulate core step via meteorGasesPressure decay)
        // Meteor gases decaying logic is handled in Update, but its direct contribution to pressure is added here
        float dynamicOtherGases = otherGasesPressure + meteorGasesPressure;

        // 3. Environmental Sinks
        // Les océans absorbent le CO2 (Carbonate-silicate cycle et altération des roches)
        // La séquestration du carbone augmente avec le niveau d'eau. On veut cibler une quantité
        // de trace pour éviter la disparition totale de la vie (target de ~0.01 à 0.1 atm au final)
        float targetCo2 = 0.01f;
        if (waterRatio > 0.01f && co2Pressure > targetCo2)
        {
            float co2Excess = co2Pressure - targetCo2;
            // Une absorption qui s'accélère si la pression est très élevée,
            // mais ralentit lorsqu'on s'approche de la cible
            float co2Absorption = (waterRatio * 0.00005f + co2Excess * 0.00001f) * dt;
            co2Pressure = Mathf.Max(targetCo2, co2Pressure - co2Absorption);
        }

        // Les autres gaz (SO2, NH3, CH4) se dégradent lentement par photolyse/réactions chimiques
        if (otherGasesPressure > 0f)
        {
            otherGasesPressure = Mathf.Max(0f, otherGasesPressure - 0.001f * dt);
        }

        // Puits d'Azote (Fixation dans les sols / Océans / Echappement)
        // La cible naturelle pour l'azote est ~0.78 atm.
        float targetNitrogen = 0.78f;
        if (nitrogenPressure > targetNitrogen)
        {
            // Plus on s'éloigne de la cible, plus le puits est fort.
            float nitrogenExcess = nitrogenPressure - targetNitrogen;
            // Un taux de perte léger qui stabilise N2 contre le dégazage tectonique
            float nitrogenSink = nitrogenExcess * 0.00005f * dt;
            nitrogenPressure = Mathf.Max(targetNitrogen, nitrogenPressure - nitrogenSink);
        }
        else if (nitrogenPressure < targetNitrogen && tectonicActivity == 0f)
        {
            // Si la pression d'azote est trop basse on tend doucement vers l'équilibre
            nitrogenPressure = Mathf.MoveTowards(nitrogenPressure, targetNitrogen, 0.00001f * dt);
        }

        // La condensation massive de la vapeur d'eau est liée au waterRatio
        // La vapeur diminue quand l'eau liquide augmente, mais on garde un plancher résiduel (0.8 atm)
        float condenseThreshold = 373.15f * Mathf.Pow(pressure, 0.1f);
        if (surfaceTemperature < condenseThreshold)
        {
            float targetWaterVapor = 0.8f;
            waterVaporPressure = Mathf.MoveTowards(waterVaporPressure, targetWaterVapor, 0.01f * dt);
        }

        // 4. Puits d'Oxygène (Oxydation des roches & Respiration Biologique)
        // La respiration (et oxydation) consomme de l'O2 et rejette un peu de CO2
        if (oxygenPressure > 0f)
        {
            // Cible d'équilibre pour O2
            float targetOxygen = 0.21f;

            // Perte de base par oxydation des roches (faible)
            float rockOxidation = 0.00001f * dt;

            // Respiration biologique (active une fois que la vie produit de l'O2)
            float biologicalRespiration = 0f;
            if (currentEpoch >= PlanetEpoch.Photosynthesis)
            {
                // La respiration augmente si on dépasse la cible, créant un puits d'équilibre
                float oxygenExcess = Mathf.Max(0f, oxygenPressure - targetOxygen);
                biologicalRespiration = (0.00005f + oxygenExcess * 0.0001f) * dt;

                // Le cycle du carbone : la respiration rejette du CO2
                co2Pressure += biologicalRespiration * 0.5f; // On ne rejette qu'une partie pour garder l'équilibre
            }

            float totalOxygenSink = rockOxidation + biologicalRespiration;
            oxygenPressure = Mathf.Max(0f, oxygenPressure - totalOxygenSink);
        }

        // La pression totale est la somme de toutes les pressions partielles.
        pressure = waterVaporPressure + co2Pressure + nitrogenPressure + dynamicOtherGases + oxygenPressure;
    }

    private void SimulateWater(float dt)
    {
        // Le seuil de condensation dépend de la pression atmosphérique
        // (température d'ébullition variable selon la loi physique).
        float condenseThreshold = 373.15f * Mathf.Pow(pressure, 0.1f);

        if (surfaceTemperature < condenseThreshold && pressure > 0.2f)
        {
            // Taux de condensation réduit à (0.0000005f / 3f) pour allonger 3x la durée du rush initial
            // de la dérive tectonique rapide (époque TectonicDrift) tout en garantissant d'atteindre l'époque Prebiotic.
            waterRatio += (0.0000005f / 3f) * dt;
        }
        else
        {
            waterRatio -= 0.000005f * dt;
        }

        waterRatio = Mathf.Clamp01(waterRatio);
    }

    private void SimulateTectonics(float dt)
    {
        // Activité tectonique augmente après formation de croûte et reste modulée
        float target =
            currentEpoch >= PlanetEpoch.CrustFormation
                ? Mathf.Clamp01((internalTemperature - 800f) / 3000f)
                : 0f;

        tectonicActivity = Mathf.MoveTowards(tectonicActivity, target, 0.0005f * dt);
    }

    private void UpdateEpochFromState()
    {
        PlanetEpoch newEpoch = currentEpoch;

        if (surfaceTemperature > 1400f)
            newEpoch = PlanetEpoch.Hadean;
        else if (surfaceTemperature > 1000f)
            newEpoch = PlanetEpoch.CrustFormation;
        else if (waterRatio < 0.05f)
            newEpoch = PlanetEpoch.VolcanicAge;
        else if (waterRatio < 0.10f)
            newEpoch = PlanetEpoch.ProtoOcean;
        else if (waterRatio < 1.00f)
            newEpoch = PlanetEpoch.TectonicDrift;
        else if (isCambrianExplosionUnlocked)
            newEpoch = PlanetEpoch.CambrianExplosion;
        else if (isPhotosynthesisUnlocked)
            newEpoch = PlanetEpoch.Photosynthesis;
        else
            newEpoch = PlanetEpoch.Prebiotic;

        if (newEpoch != currentEpoch)
        {
            currentEpoch = newEpoch;
            OnEpochChanged?.Invoke(currentEpoch);
        }
    }

    // Contrôles publics
    public void SetPause(bool paused) => isPaused = paused;
    public void SetPlayerSpeedMultiplier(float multiplier) => playerSpeedMultiplier = Mathf.Max(0f, multiplier);
    public void SetNoPlayerBaseline(bool enabled) => noPlayerBaseline = enabled;
    public void SetSessionLengthPreset(SessionLengthPreset preset) => sessionLengthPreset = preset;
    public void SetCustomSessionHours(float hours) => customSessionHours = Mathf.Max(0.25f, hours);

    private float GetSessionDurationHours()
    {
        return sessionLengthPreset switch
        {
            SessionLengthPreset.OneHour => 1f,
            SessionLengthPreset.ThreeHours => 3f,
            SessionLengthPreset.SixHours => 6f,
            SessionLengthPreset.TwelveHours => 12f,
            SessionLengthPreset.Custom => Mathf.Max(0.25f, customSessionHours),
            _ => 3f
        };
    }

    private float GetSimulationUnitsPerRealSecond()
    {
        float durationSeconds = Mathf.Max(1f, GetSessionDurationHours() * 3600f);
        float baseUnitsPerSecond = baselineSimulationUnits / durationSeconds;
        return baseUnitsPerSecond * playerSpeedMultiplier;
    }

    private void InitializeCsvLogger()
    {
        if (!enableCsvLogger) return;
        if (logOnlyWhenNoPlayerBaseline && !noPlayerBaseline) return;

        try
        {
            currentLogTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            OpenChannelWriter(
                LogChannel.Core,
                "baseline_core",
                "simulationTime,sessionProgress,internalTemperature,surfaceTemperature,pressure,waterRatio,tectonicActivity,epoch");
            OpenChannelWriter(
                LogChannel.Events,
                "events",
                "simulationTime,sessionProgress,eventType,details");
            OpenChannelWriter(
                LogChannel.PlayerActions,
                "player_actions",
                "simulationTime,sessionProgress,actionType,details");
            OpenChannelWriter(
                LogChannel.Perf,
                "perf",
                "simulationTime,sessionProgress,deltaTime,simulationUnitsPerSecond");

            nextCsvLogAtSimulationTime = simulationTimeSeconds;
            Debug.Log($"[GameManager] CSV logger actif ({enabledLogChannels}) dans: {Application.persistentDataPath}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameManager] Impossible d'initialiser le logger CSV: {ex.Message}");
            CloseCsvLogger();
        }
    }

    private void TryLogCsvSample()
    {
        if (!TryGetWriter(LogChannel.Core, out StreamWriter coreWriter)) return;
        if (simulationTimeSeconds < nextCsvLogAtSimulationTime) return;

        string coreLine = string.Format(
            CultureInfo.InvariantCulture,
            "{0:F2},{1:F6},{2:F3},{3:F3},{4:F5},{5:F5},{6:F5},{7}",
            simulationTimeSeconds,
            SessionProgress,
            internalTemperature,
            surfaceTemperature,
            pressure,
            waterRatio,
            tectonicActivity,
            currentEpoch);

        coreWriter.WriteLine(coreLine);
        coreWriter.Flush();

        if (TryGetWriter(LogChannel.Perf, out StreamWriter perfWriter))
        {
            string perfLine = string.Format(
                CultureInfo.InvariantCulture,
                "{0:F2},{1:F6},{2:F6},{3:F3}",
                simulationTimeSeconds,
                SessionProgress,
                Time.deltaTime,
                GetSimulationUnitsPerRealSecond());
            perfWriter.WriteLine(perfLine);
            perfWriter.Flush();
        }

        nextCsvLogAtSimulationTime += csvLogIntervalSimulationUnits;
    }

    private void CloseCsvLogger()
    {
        foreach (StreamWriter writer in channelWriters.Values)
        {
            writer.Flush();
            writer.Dispose();
        }

        channelWriters.Clear();
    }

    [ContextMenu("Open Baseline Log Folder")]
    private void OpenBaselineLogFolder()
    {
        string path = Application.persistentDataPath;
        if (Directory.Exists(path))
        {
            Application.OpenURL($"file://{path}");
        }
        else
        {
            Debug.LogWarning($"[GameManager] Dossier introuvable: {path}");
        }
    }

    public void LogEvent(string eventType, string details = "")
    {
        WriteChannelLine(LogChannel.Events, eventType, details);
    }

    public void LogPlayerAction(string actionType, string details = "")
    {
        WriteChannelLine(LogChannel.PlayerActions, actionType, details);
    }

    private void OpenChannelWriter(LogChannel channel, string prefix, string header)
    {
        if (!IsChannelEnabled(channel)) return;

        string fileName = $"{prefix}_{currentLogTimestamp}.csv";
        string path = Path.Combine(Application.persistentDataPath, fileName);
        StreamWriter writer = new StreamWriter(path, false);
        writer.WriteLine(header);
        writer.Flush();
        channelWriters[channel] = writer;
    }

    private bool IsChannelEnabled(LogChannel channel)
    {
        return (enabledLogChannels & channel) != 0;
    }

    private bool TryGetWriter(LogChannel channel, out StreamWriter writer)
    {
        writer = null;
        return channelWriters.TryGetValue(channel, out writer);
    }

    private void WriteChannelLine(LogChannel channel, string type, string details)
    {
        if (!TryGetWriter(channel, out StreamWriter writer)) return;

        string safeType = EscapeCsv(type);
        string safeDetails = EscapeCsv(details);
        string line = string.Format(
            CultureInfo.InvariantCulture,
            "{0:F2},{1:F6},{2},{3}",
            simulationTimeSeconds,
            SessionProgress,
            safeType,
            safeDetails);

        writer.WriteLine(line);
        writer.Flush();
    }

    public void AddMeteorGases(float amount)
    {
        meteorGasesPressure += amount;
        LogEvent("Meteor Gases Released", $"Added {amount:F1} atm of other volcanic gases.");
    }

    public void AddImpactThermalPulse(float amount)
    {
        impactThermalPulse += amount;
        LogEvent("Impact Thermal Shock", $"Surface temperature spiked by +{amount:F1} K.");
    }

    public void AddTectonicActivity(float amount)
    {
        tectonicActivity = Mathf.Clamp01(tectonicActivity + amount);
        LogEvent("Tectonic Activity Spike", $"Tectonic activity surged by +{amount:F2}.");
    }

    public void AddVolcanicGases(float co2Amount, float waterVaporAmount, float otherGasesAmount)
    {
        co2Pressure += co2Amount;
        waterVaporPressure += waterVaporAmount;
        otherGasesPressure += otherGasesAmount;
    }

    public void AddOxygen(float amount)
    {
        float remainingOxygen = amount;

        // 1. Oxydation du fer dans l'océan (Great Oxidation Event)
        if (dissolvedIronRatio > 0f)
        {
            float ironConsumed = Mathf.Min(dissolvedIronRatio, remainingOxygen * 10f); // Le fer consomme beaucoup d'oxygène
            dissolvedIronRatio -= ironConsumed;
            remainingOxygen -= ironConsumed * 0.1f;
        }

        // 2. Oxydation des gaz réduits (Méthane, etc.)
        if (remainingOxygen > 0f && otherGasesPressure > 0f)
        {
            float gasesConsumed = Mathf.Min(otherGasesPressure, remainingOxygen * 2f);
            otherGasesPressure -= gasesConsumed;
            remainingOxygen -= gasesConsumed * 0.5f;
        }

        // 3. Accumulation dans l'atmosphère
        if (remainingOxygen > 0f)
        {
            oxygenPressure += remainingOxygen;
        }
    }

    public void ConsumeGases(float co2Amount, float waterVaporAmount, float otherGasesAmount)
    {
        co2Pressure = Mathf.Max(0f, co2Pressure - co2Amount);
        waterVaporPressure = Mathf.Max(0.8f, waterVaporPressure - waterVaporAmount);
        otherGasesPressure = Mathf.Max(0f, otherGasesPressure - otherGasesAmount);
        LogEvent("Gases Consumed", $"Atmosphere depleted by internal events.");
    }

    public void TriggerTsunami(float amount)
    {
        tsunamiWaterRise += amount;
        tsunamiWaterRise = Mathf.Clamp(tsunamiWaterRise, 0f, 0.5f); // limit max rise to prevent complete overflow above 100%
        LogEvent("Tsunami Triggered", $"Water level temporarily rose by {amount * 100f:F1}%.");
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";

        string escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}