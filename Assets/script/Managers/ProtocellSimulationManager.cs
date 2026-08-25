using System;
using System.Collections.Generic;
using UnityEngine;

public enum PrebioticZoneType
{
    HydrothermalVent,
    ChemicalTidePool
}

[System.Serializable]
public class Protocell
{
    public string id;
    public float radius;               // Size of vesicle (microns / relative unit)
    public float membraneStability;    // 0..1 (stability under current pH & temp)
    public float permeability;         // 0..1 (ideal balance ~0.4 - 0.6)
    public float rnaReplicationRate;   // Base rate of self-replication
    public float radiationResistance;  // Resistance to solar UV / ambient radiation
    public float energyEfficiency;     // Efficiency of chemiosmotic gradient consumption
    public float energy;               // Internal energy pool
    public float rnaContent;           // Accumulated RNA chains
    public float age;                  // Age in simulation seconds
    public Color mutationColor;        // Color hint based on evolved dominant trait

    public Protocell CloneWithMutation(float mutationRate = 0.15f)
    {
        Protocell child = new Protocell
        {
            id = Guid.NewGuid().ToString().Substring(0, 6),
            radius = Mathf.Clamp(radius + UnityEngine.Random.Range(-0.05f, 0.05f), 0.5f, 2.5f),
            membraneStability = Mathf.Clamp01(membraneStability + UnityEngine.Random.Range(-mutationRate, mutationRate)),
            permeability = Mathf.Clamp01(permeability + UnityEngine.Random.Range(-mutationRate, mutationRate)),
            rnaReplicationRate = Mathf.Clamp(rnaReplicationRate + UnityEngine.Random.Range(-mutationRate * 0.5f, mutationRate * 0.5f), 0.1f, 2.5f),
            radiationResistance = Mathf.Clamp01(radiationResistance + UnityEngine.Random.Range(-mutationRate, mutationRate)),
            energyEfficiency = Mathf.Clamp(energyEfficiency + UnityEngine.Random.Range(-mutationRate * 0.5f, mutationRate * 0.5f), 0.1f, 2.0f),
            energy = energy * 0.45f,
            rnaContent = 10f,
            age = 0f
        };

        // Determine dominant phenotype trait to update color
        Color c = new Color(0.3f, 0.8f, 0.5f, 0.85f); // Base teal/green
        if (child.energyEfficiency > 1.3f) c = Color.Lerp(c, new Color(0.95f, 0.75f, 0.2f, 0.9f), 0.6f); // Yellow (high energy)
        if (child.radiationResistance > 0.7f) c = Color.Lerp(c, new Color(0.4f, 0.3f, 0.9f, 0.9f), 0.6f); // Purple (radiation resistant)
        if (child.rnaReplicationRate > 1.4f) c = Color.Lerp(c, new Color(0.9f, 0.3f, 0.4f, 0.9f), 0.6f); // Red/pink (fast replicator)
        if (child.permeability > 0.7f) c = Color.Lerp(c, new Color(0.2f, 0.7f, 0.9f, 0.9f), 0.6f); // Cyan (high permeability)

        child.mutationColor = c;
        return child;
    }
}

[System.Serializable]
public class PrebioticZone
{
    public string name;
    public PrebioticZoneType zoneType;
    public Vector3 spherePosition;     // Normalized position on unit sphere
    public float latitudeDeg;
    public float longitudeDeg;

    [Header("Local Environment")]
    public float localTemperature;     // °C (ideal ~40-70 °C depending on type)
    public float localPh;              // pH 1..14 (ideal ~7.0-8.2)
    public float lipidConcentration;   // 0..100%
    public float aminoAcidConcentration; // 0..100%
    public float chemicalGradientStrength; // 0..1 (hydrothermal gradient or tidal fluctuation)

    [Header("Population State")]
    public List<Protocell> protocells = new List<Protocell>();
    public int maxCapacity = 150;
    public float totalLipidMicelles;   // Micelle count before vesicle formation

    [Header("Historical Metrics")]
    public Queue<float> populationHistory = new Queue<float>();
    public Queue<float> diversityHistory = new Queue<float>();

    public float GeneticDiversity
    {
        get
        {
            if (protocells.Count < 2) return 0f;
            float sumVariance = 0f;
            float avgPerm = 0f, avgEff = 0f, avgRad = 0f;
            foreach (var cell in protocells)
            {
                avgPerm += cell.permeability;
                avgEff += cell.energyEfficiency;
                avgRad += cell.radiationResistance;
            }
            avgPerm /= protocells.Count;
            avgEff /= protocells.Count;
            avgRad /= protocells.Count;

            foreach (var cell in protocells)
            {
                sumVariance += Mathf.Abs(cell.permeability - avgPerm) +
                               Mathf.Abs(cell.energyEfficiency - avgEff) +
                               Mathf.Abs(cell.radiationResistance - avgRad);
            }
            return Mathf.Clamp01(sumVariance / (protocells.Count * 3f));
        }
    }

    public float MeanPermeability
    {
        get
        {
            if (protocells.Count == 0) return 0f;
            float sum = 0f;
            foreach (var c in protocells) sum += c.permeability;
            return sum / protocells.Count;
        }
    }

    public float MeanEnergyEfficiency
    {
        get
        {
            if (protocells.Count == 0) return 0f;
            float sum = 0f;
            foreach (var c in protocells) sum += c.energyEfficiency;
            return sum / protocells.Count;
        }
    }
}

public class ProtocellSimulationManager : MonoBehaviour
{
    public static ProtocellSimulationManager Instance { get; private set; }

    [Header("Zones Prebiotiques")]
    [SerializeField] private List<PrebioticZone> activeZones = new List<PrebioticZone>();

    [Header("Paramètres Global Simulation")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private bool autoSimulateInPrebioticEpoch = true;

    private float updateTimer;
    private int selectedZoneIndex = 0;

    public event Action OnSimulationUpdated;
    public event Action<PrebioticZone> OnZoneSelected;

    public List<PrebioticZone> ActiveZones => activeZones;
    public PrebioticZone SelectedZone => (activeZones.Count > 0 && selectedZoneIndex >= 0 && selectedZoneIndex < activeZones.Count) ? activeZones[selectedZoneIndex] : null;
    public int SelectedZoneIndex => selectedZoneIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeDefaultZones();
    }

    private void InitializeDefaultZones()
    {
        activeZones.Clear();

        // Zone 1: Sources Hydrothermales Submarines ("Fumeurs Noirs")
        PrebioticZone ventZone = new PrebioticZone
        {
            name = "Sources Hydrothermales",
            zoneType = PrebioticZoneType.HydrothermalVent,
            latitudeDeg = -15f,
            longitudeDeg = 45f,
            spherePosition = SphericalToVector3(-15f, 45f),
            localTemperature = 65f, // 65°C ideal thermal gradient
            localPh = 7.8f,        // Slightly alkaline
            lipidConcentration = 45f,
            aminoAcidConcentration = 50f,
            chemicalGradientStrength = 0.85f,
            maxCapacity = 150
        };

        // Zone 2: Marées Chimiques & Marégraphes Prebiotiques
        PrebioticZone tideZone = new PrebioticZone
        {
            name = "Marées Chimiques Littorales",
            zoneType = PrebioticZoneType.ChemicalTidePool,
            latitudeDeg = 25f,
            longitudeDeg = -80f,
            spherePosition = SphericalToVector3(25f, -80f),
            localTemperature = 42f, // 42°C warm tide pool
            localPh = 7.2f,        // Near neutral
            lipidConcentration = 60f,
            aminoAcidConcentration = 60f,
            chemicalGradientStrength = 0.60f,
            maxCapacity = 150
        };

        // Seed initial micelles and primitive vesicles in both zones
        SeedInitialProtocells(ventZone, 12);
        SeedInitialProtocells(tideZone, 18);

        activeZones.Add(ventZone);
        activeZones.Add(tideZone);
    }

    private static Vector3 SphericalToVector3(float latDeg, float lonDeg)
    {
        float latRad = latDeg * Mathf.Deg2Rad;
        float lonRad = lonDeg * Mathf.Deg2Rad;
        return new Vector3(
            Mathf.Cos(latRad) * Mathf.Sin(lonRad),
            Mathf.Sin(latRad),
            Mathf.Cos(latRad) * Mathf.Cos(lonRad)
        );
    }

    private void SeedInitialProtocells(PrebioticZone zone, int count)
    {
        zone.protocells.Clear();
        for (int i = 0; i < count; i++)
        {
            Protocell cell = new Protocell
            {
                id = Guid.NewGuid().ToString().Substring(0, 6),
                radius = UnityEngine.Random.Range(0.8f, 1.4f),
                membraneStability = UnityEngine.Random.Range(0.6f, 0.9f),
                permeability = UnityEngine.Random.Range(0.35f, 0.65f),
                rnaReplicationRate = UnityEngine.Random.Range(0.8f, 1.2f),
                radiationResistance = UnityEngine.Random.Range(0.4f, 0.8f),
                energyEfficiency = UnityEngine.Random.Range(0.8f, 1.2f),
                energy = UnityEngine.Random.Range(30f, 60f),
                rnaContent = UnityEngine.Random.Range(10f, 25f),
                age = UnityEngine.Random.Range(0f, 20f),
                mutationColor = new Color(0.3f, 0.8f, 0.5f, 0.85f)
            };
            zone.protocells.Add(cell);
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        bool isPrebiotic = GameManager.Instance == null || GameManager.Instance.CurrentEpoch == PlanetEpoch.Prebiotic;
        if (autoSimulateInPrebioticEpoch && !isPrebiotic) return;

        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            SimulateStep(updateInterval);
        }
    }

    public void SimulateStep(float dt)
    {
        foreach (var zone in activeZones)
        {
            SimulateZone(zone, dt);
        }

        OnSimulationUpdated?.Invoke();
    }

    private void SimulateZone(PrebioticZone zone, float dt)
    {
        // 1. Environmental Stability Check (pH stability around 7.0-8.5, Temp stability 30-80°C)
        float tempPenalty = Mathf.Clamp01(Mathf.Abs(zone.localTemperature - 50f) / 45f); // ideal 50°C
        float phPenalty = Mathf.Clamp01(Mathf.Abs(zone.localPh - 7.5f) / 4.5f);        // ideal 7.5 pH
        float stabilityFactor = Mathf.Clamp01(1f - (tempPenalty * 0.6f + phPenalty * 0.4f));

        // 2. Membrane Self-Assembly Logic (Lipids -> Micelles -> Bilipid Vesicles)
        // If lipid concentration is sufficient (> 20%) and environmental stability > 0.3
        if (zone.lipidConcentration >= 20f && stabilityFactor > 0.3f)
        {
            zone.totalLipidMicelles = zone.lipidConcentration * 15f;

            // Spontaneous vesicle formation if population under capacity
            if (zone.protocells.Count < zone.maxCapacity && UnityEngine.Random.value < (zone.lipidConcentration * 0.002f * stabilityFactor))
            {
                Protocell newVesicle = new Protocell
                {
                    id = Guid.NewGuid().ToString().Substring(0, 6),
                    radius = UnityEngine.Random.Range(0.7f, 1.2f),
                    membraneStability = stabilityFactor,
                    permeability = UnityEngine.Random.Range(0.3f, 0.7f),
                    rnaReplicationRate = 1.0f,
                    radiationResistance = 0.5f,
                    energyEfficiency = 1.0f,
                    energy = 25f,
                    rnaContent = 5f,
                    age = 0f,
                    mutationColor = new Color(0.3f, 0.8f, 0.5f, 0.85f)
                };
                zone.protocells.Add(newVesicle);
                zone.lipidConcentration = Mathf.Max(0f, zone.lipidConcentration - 0.5f);
            }
        }
        else
        {
            zone.totalLipidMicelles = Mathf.Max(0f, zone.totalLipidMicelles - 10f * dt);
        }

        // 3. Protocell Metabolism, Replication, and Natural Selection
        List<Protocell> newOffspring = new List<Protocell>();
        List<Protocell> deadCells = new List<Protocell>();

        foreach (var cell in zone.protocells)
        {
            cell.age += dt;

            float availableNutrients = (zone.aminoAcidConcentration / 100f) * 1.5f;

            // Permeability challenge: must be optimal (around 0.4 - 0.6)
            // Too low: cannot absorb nutrients. Too high: leaks internal RNA & energy.
            float nutrientAbsorption = Mathf.Sin(cell.permeability * Mathf.PI) * availableNutrients * 12f * dt;

            // Consommer les nutriments de la zone (Compétition)
            // On s'assure que la consommation est dépendante de dt pour ne pas être liée au framerate.
            float consumedPercentage = nutrientAbsorption * 0.1f;
            zone.aminoAcidConcentration = Mathf.Max(0f, zone.aminoAcidConcentration - consumedPercentage);

            float energyLeak = Mathf.Max(0f, cell.permeability - 0.65f) * 18f * dt;

            // Primitive Chemiosmotic Metabolism (Gradient consumption vs passive diffusion)
            float gradientHarvest = zone.chemicalGradientStrength * cell.energyEfficiency * 15f * dt;

            // Energy balance
            cell.energy += nutrientAbsorption + gradientHarvest - energyLeak - (8f * dt);

            // Radiation damage (if tide pool and low radiation resistance)
            if (zone.zoneType == PrebioticZoneType.ChemicalTidePool)
            {
                float radDamage = (1f - cell.radiationResistance) * 8f * dt;
                cell.energy -= radDamage;
            }

            // RNA synthesis & replication cycle inside vesicle
            if (cell.energy > 20f)
            {
                cell.rnaContent += cell.rnaReplicationRate * 2.5f * dt;
            }

            // Vesicle Division (Reproduction)
            if (cell.rnaContent >= 50f && cell.energy >= 70f && zone.protocells.Count + newOffspring.Count < zone.maxCapacity)
            {
                cell.rnaContent *= 0.5f;
                cell.energy *= 0.5f;
                Protocell child = cell.CloneWithMutation(0.12f);
                newOffspring.Add(child);
            }

            // Natural Selection Death Conditions
            if (cell.energy <= 0f || cell.membraneStability < 0.15f || (stabilityFactor < 0.2f && UnityEngine.Random.value < 0.25f))
            {
                deadCells.Add(cell);
            }

            // Émergence de la complexité: débloquer la Photosynthèse
            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentEpoch == PlanetEpoch.Prebiotic &&
                !GameManager.Instance.IsPhotosynthesisUnlocked)
            {
                if (cell.energyEfficiency >= 1.5f && cell.radiationResistance >= 0.7f && cell.permeability >= 0.5f)
                {
                    GameManager.Instance.UnlockPhotosynthesis();
                    GameManager.Instance.LogEvent("Photosynthesis Unlocked", "Une protocellule a atteint un niveau métabolique élevé !");
                }
            }
        }

        // Remove dead protocells & add newly divided ones
        foreach (var dead in deadCells)
        {
            zone.protocells.Remove(dead);
            // Recovers lipids into local pool
            zone.lipidConcentration = Mathf.Min(100f, zone.lipidConcentration + 0.15f);
        }

        zone.protocells.AddRange(newOffspring);

        // Track History
        if (zone.populationHistory.Count >= 30) zone.populationHistory.Dequeue();
        zone.populationHistory.Enqueue(zone.protocells.Count);

        if (zone.diversityHistory.Count >= 30) zone.diversityHistory.Dequeue();
        zone.diversityHistory.Enqueue(zone.GeneticDiversity);
    }

    public void SelectZone(int index)
    {
        if (index >= 0 && index < activeZones.Count)
        {
            selectedZoneIndex = index;
            OnZoneSelected?.Invoke(SelectedZone);
        }
    }

    public void ModifySelectedZoneEnvironment(float deltaTemp, float deltaPh)
    {
        PrebioticZone zone = SelectedZone;
        if (zone == null) return;

        zone.localTemperature = Mathf.Clamp(zone.localTemperature + deltaTemp, 0f, 120f);
        zone.localPh = Mathf.Clamp(zone.localPh + deltaPh, 1.0f, 14.0f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LogPlayerAction("Prebiotic Env Tweak", $"Zone {zone.name} : Temp={zone.localTemperature:F1}°C, pH={zone.localPh:F1}");
        }

        OnSimulationUpdated?.Invoke();
    }

    public void AddNutrientsToSelectedZone(float amount)
    {
        PrebioticZone zone = SelectedZone;
        if (zone == null) return;

        zone.aminoAcidConcentration = Mathf.Min(100f, zone.aminoAcidConcentration + amount);
        zone.lipidConcentration = Mathf.Min(100f, zone.lipidConcentration + amount * 0.8f);

        OnSimulationUpdated?.Invoke();
    }
}
