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
    Prebiotic       // Phase pré-biotique: synthèse des acides aminés
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

    [Header("Variables planétaires")]
    [SerializeField] private float internalTemperature = 5000f; // K
    [SerializeField] private float surfaceTemperature = 1800f;  // K
    [SerializeField] private float pressure = 100f;              // atm arbitraire
    [SerializeField] private float waterRatio = 0f;              // 0..1
    [SerializeField] private float tectonicActivity = 0f;        // 0..1

    [Header("Composants de l'Atmosphère (pression partielle en atm)")]
    [SerializeField] private float waterVaporPressure;
    [SerializeField] private float co2Pressure;
    [SerializeField] private float nitrogenPressure = 8f;
    [SerializeField] private float otherGasesPressure;

    [Header("Evenements Meteor")]
    [SerializeField] private float meteorGasesPressure = 0f;
    [SerializeField] private float tsunamiWaterRise = 0f;

    [Header("Etat global")]
    [SerializeField] private PlanetEpoch currentEpoch = PlanetEpoch.Hadean;

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
    public PlanetEpoch CurrentEpoch => currentEpoch;
    public bool IsPaused => isPaused;
    public bool NoPlayerBaseline => noPlayerBaseline;
    public float SessionDurationHours => GetSessionDurationHours();
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

        EnsureAudioManager();
        InitializeCsvLogger();
    }

    private void EnsureAudioManager()
    {
        if (AudioManager.Instance == null && FindAnyObjectByType<AudioManager>() == null)
        {
            GameObject audioMgrObj = new GameObject("AudioManager");
            audioMgrObj.AddComponent<AudioManager>();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        customSessionHours = Mathf.Max(0.25f, customSessionHours);
        baselineSimulationUnits = Mathf.Max(1f, baselineSimulationUnits);
        playerSpeedMultiplier = Mathf.Max(0f, playerSpeedMultiplier);
        csvLogIntervalSimulationUnits = Mathf.Max(1f, csvLogIntervalSimulationUnits);
    }
#endif

    private void Update()
    {
        if (isPaused) return;

        float dt = Time.deltaTime * GetSimulationUnitsPerRealSecond();
        simulationTimeSeconds += dt;
        simulationTimeSeconds = Mathf.Min(simulationTimeSeconds, baselineSimulationUnits);

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
        // Refroidissement interne lent
        internalTemperature -= 0.001f * dt;
        internalTemperature = Mathf.Max(internalTemperature, 1500f);

        // La température de surface suit progressivement la baisse de la température interne.
        // On effectue une interpolation linéaire afin de stabiliser la température de surface
        // à environ 25 °C (298.15 K) en fin de phase de refroidissement de la planète (lorsque l'interne atteint 1500 K).
        float targetSurface = Mathf.Lerp(298.15f, 1472.2f, Mathf.Clamp01((internalTemperature - 1500f) / 3500f));
        surfaceTemperature = Mathf.MoveTowards(surfaceTemperature, targetSurface, 0.05f * dt);
        surfaceTemperature = Mathf.Max(surfaceTemperature, 100f);
    }

    private void SimulatePressure(float dt)
    {
        // La pression atmosphérique globale est constituée d'une part de gaz volcaniques
        // "secs" non-condensables (CO2, N2, etc.) et d'une part importante de vapeur d'eau (vapeur).
        // À mesure que la planète se refroidit, la vapeur d'eau se condense sous forme d'océans liquides (waterRatio augmente),
        // ce qui élimine (scrub) l'eau de la phase gazeuse et fait chuter drastiquement la pression totale de l'atmosphère.

        // Décomposition physique des pressions partielles de l'atmosphère :
        // Pour une atmosphère primitive pré-biotique, même après condensation maximale des océans (waterRatio = 1),
        // une pression résiduelle de vapeur d'eau (environ 0.8 atm, ~2.5% de la pression totale de ~31 atm) subsiste
        // pour maintenir une humidité atmosphérique primitive réaliste non-nulle.
        waterVaporPressure = 0.8f + 399.2f * (1f - waterRatio);
        co2Pressure = 2f + 80f * tectonicActivity;
        nitrogenPressure = 8f; // azote stable d'arrière-plan
        otherGasesPressure = 10f * tectonicActivity + meteorGasesPressure; // gaz d'apport volcanique (SO2, etc.) + gaz libérés par météores

        // La pression totale est la somme de toutes les pressions partielles.
        pressure = waterVaporPressure + co2Pressure + nitrogenPressure + otherGasesPressure;
        pressure = Mathf.Clamp(pressure, 0.01f, 500f);
    }

    private void SimulateWater(float dt)
    {
        // Le seuil de condensation dépend de la pression atmosphérique
        // (température d'ébullition variable selon la loi physique).
        float condenseThreshold = 373.15f * Mathf.Pow(pressure, 0.1f);

        if (surfaceTemperature < condenseThreshold && pressure > 0.2f)
        {
            // Taux de condensation réduit (de 0.00001f à 0.000001f) pour permettre une progression plus lente et réaliste,
            // garantissant que l'époque ProtoOcean (eau entre 5% et 30%) soit pleinement vécue par le joueur.
            waterRatio += 0.000001f * dt;
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

    public void AddVolcanicGases(float co2Amount, float waterVaporAmount, float otherGasesAmount)
    {
        co2Pressure += co2Amount;
        waterVaporPressure += waterVaporAmount;
        otherGasesPressure += otherGasesAmount;
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