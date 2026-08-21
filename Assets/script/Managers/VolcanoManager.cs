using System;
using System.Collections.Generic;
using UnityEngine;

public enum VolcanoState
{
    Growth,
    Eruptive,
    Dormant
}

public class VolcanoInstance
{
    public string id;
    public float longitudeDegrees;
    public float latitudeDegrees;

    // Radius and Peak Height targets
    public float targetRadiusDegrees;
    public float currentRadiusDegrees;
    public float targetPeakHeight;
    public float currentPeakHeight;

    // Growth rates
    public float growthSpeed = 0.05f; // progress per simulation unit

    // Lifecycle state
    public VolcanoState state = VolcanoState.Growth;
    public float stateTimer = 0f;
    public float currentPhaseDuration = 5f;

    // Dormancy properties
    public bool isPermanentlyDormant = false;

    // Gas emission rates per simulation step when erupting
    public float co2EmissionRate;
    public float waterVaporEmissionRate;
    public float otherGasesEmissionRate;

    // Attached continental piece for tectonic drift
    public CubeSphereTerrain.ContinentalPiece parentPiece;
    public float offsetLonFromParent;
    public float offsetLatFromParent;

    // Associated Visual Particle System
    public GameObject particleSystemObject;
    public ParticleSystem particleSystemRef;
}

public class VolcanoManager : MonoBehaviour
{
    public static VolcanoManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CubeSphereTerrain terrain;

    [Header("Volcanic Epoch Settings")]
    [SerializeField] private int minEpochVolcanoes = 6;
    [SerializeField] private int maxEpochVolcanoes = 12;

    [Header("Supercontinent Benchmark Reference")]
    // Average radius of supercontinent in degrees (~45 degrees => ~90 degrees diameter)
    [SerializeField] private float supercontinentRadiusDegrees = 45f;

    [Header("ParticlePack Volcanic Effects")]
    [SerializeField] private GameObject volcanoFlamePrefab;
    [SerializeField] private GameObject volcanoSmokePrefab;
    [SerializeField] private GameObject volcanoExplosionPrefab;

    private List<VolcanoInstance> volcanoes = new List<VolcanoInstance>();
    private bool volcanicEpochTriggered = false;
    private Material sharedVolcanoParticleMaterial;

    public IReadOnlyList<VolcanoInstance> Volcanoes => volcanoes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (terrain == null)
        {
            terrain = FindAnyObjectByType<CubeSphereTerrain>();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEpochChanged += HandleEpochChanged;
            GameManager.Instance.OnSimulationStep += HandleSimulationStep;

            // Check if starting directly in VolcanicAge
            if (GameManager.Instance.CurrentEpoch == PlanetEpoch.VolcanicAge)
            {
                TriggerVolcanicEpoch();
            }
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEpochChanged -= HandleEpochChanged;
            GameManager.Instance.OnSimulationStep -= HandleSimulationStep;
        }
    }

    private void HandleEpochChanged(PlanetEpoch newEpoch)
    {
        if (newEpoch == PlanetEpoch.VolcanicAge && !volcanicEpochTriggered)
        {
            TriggerVolcanicEpoch();
        }
    }

    public void TriggerVolcanicEpoch()
    {
        if (volcanicEpochTriggered) return;
        volcanicEpochTriggered = true;

        int count = UnityEngine.Random.Range(minEpochVolcanoes, maxEpochVolcanoes + 1);
        Debug.Log($"[VolcanoManager] Volcanic Age epoch started! Spawning {count} volcanoes across the planet.");

        for (int i = 0; i < count; i++)
        {
            SpawnRandomVolcano();
        }
    }

    /// <summary>
    /// Calculates target radius based on supercontinent size (1/100 to 1/10 ratio).
    /// Smallest and largest combined make up 10% of volcanoes (5% smallest, 5% largest, 90% medium).
    /// </summary>
    public float GenerateVolcanoRadius()
    {
        // 1/100 of supercontinent radius to 1/10 of supercontinent radius
        float minRadius = supercontinentRadiusDegrees * 0.01f; // ~0.45 deg radius
        float maxRadius = supercontinentRadiusDegrees * 0.10f; // ~4.5 deg radius

        float roll = UnityEngine.Random.value;
        float radius;

        if (roll < 0.05f)
        {
            // 5% Smallest volcanoes (1/100 to 1/60 of supercontinent)
            radius = UnityEngine.Random.Range(minRadius, minRadius * 1.6f);
        }
        else if (roll > 0.95f)
        {
            // 5% Largest volcanoes (1/12 to 1/10 of supercontinent)
            radius = UnityEngine.Random.Range(maxRadius * 0.8f, maxRadius);
        }
        else
        {
            // 90% Standard medium volcanoes (1/50 to 1/15 of supercontinent)
            radius = UnityEngine.Random.Range(minRadius * 2.0f, maxRadius * 0.7f);
        }

        return radius;
    }

    /// <summary>
    /// Spawns a new volcano at random spherical coordinates or custom coordinates.
    /// Spawns directly on solid continental crust so volcanoes are clipped to tectonic plates.
    /// </summary>
    public VolcanoInstance SpawnRandomVolcano()
    {
        if (terrain != null && terrain.ContinentalPieces != null && terrain.ContinentalPieces.Length > 0)
        {
            var piece = terrain.ContinentalPieces[UnityEngine.Random.Range(0, terrain.ContinentalPieces.Length)];
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float r = UnityEngine.Random.Range(0f, piece.radius * 0.75f);
            float cosLat = Mathf.Max(Mathf.Cos(piece.currentLatitude * Mathf.Deg2Rad), 0.2f);
            float lon = Mathf.Repeat(piece.currentLongitude + (r * Mathf.Cos(angle)) / cosLat, 360f);
            float lat = Mathf.Clamp(piece.currentLatitude + r * Mathf.Sin(angle), -65f, 65f);
            return SpawnVolcano(lon, lat);
        }

        float fallbackLon = UnityEngine.Random.Range(0f, 360f);
        float fallbackLat = UnityEngine.Random.Range(-65f, 65f);
        return SpawnVolcano(fallbackLon, fallbackLat);
    }

    public VolcanoInstance SpawnVolcano(float lonDeg, float latDeg)
    {
        if (terrain == null)
        {
            terrain = FindAnyObjectByType<CubeSphereTerrain>();
        }

        float targetRadius = GenerateVolcanoRadius();
        // Peak height scales with volcano radius
        float targetHeight = UnityEngine.Random.Range(0.3f, 0.8f) * (targetRadius / (supercontinentRadiusDegrees * 0.1f));
        targetHeight = Mathf.Clamp(targetHeight, 0.15f, 0.9f);

        var parentPiece = terrain != null ? terrain.FindParentPiece(lonDeg, latDeg) : null;

        VolcanoInstance volcano = new VolcanoInstance
        {
            id = Guid.NewGuid().ToString().Substring(0, 8),
            longitudeDegrees = lonDeg,
            latitudeDegrees = latDeg,
            targetRadiusDegrees = targetRadius,
            currentRadiusDegrees = targetRadius * 0.1f, // starts small
            targetPeakHeight = targetHeight,
            currentPeakHeight = targetHeight * 0.1f,
            growthSpeed = UnityEngine.Random.Range(0.01f, 0.03f),
            state = VolcanoState.Growth,
            stateTimer = 0f,
            currentPhaseDuration = UnityEngine.Random.Range(100f, 300f), // simulation units

            // Atmospheric emission rates scale with volcano size
            co2EmissionRate = UnityEngine.Random.Range(0.0001f, 0.0005f) * (targetRadius / 2f),
            waterVaporEmissionRate = UnityEngine.Random.Range(0.0002f, 0.0008f) * (targetRadius / 2f),
            otherGasesEmissionRate = UnityEngine.Random.Range(0.0001f, 0.0003f) * (targetRadius / 2f),

            parentPiece = parentPiece,
            offsetLonFromParent = parentPiece != null ? CubeSphereTerrain.DeltaLongitudeDegrees(lonDeg, parentPiece.currentLongitude) : 0f,
            offsetLatFromParent = parentPiece != null ? latDeg - parentPiece.currentLatitude : 0f
        };

        volcanoes.Add(volcano);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LogEvent("Volcano Created", $"Volcano [{volcano.id}] at ({lonDeg:F1} deg E, {latDeg:F1} deg N) with radius {targetRadius:F2} deg.");
        }

        RebuildTerrainVolcanoes();
        return volcano;
    }

    private void HandleSimulationStep()
    {
        if (GameManager.Instance == null) return;
        float simDt = Time.deltaTime * 10f; // estimated simulation step delta

        UpdateVolcanoesSimulation(simDt);
    }

    public void UpdateVolcanoesSimulation(float simDt)
    {
        bool terrainNeedsRebuild = false;

        for (int i = volcanoes.Count - 1; i >= 0; i--)
        {
            VolcanoInstance vol = volcanoes[i];
            vol.stateTimer += simDt;

            // 1. Tectonic Drift Update: maintain fixed offset on solid crust
            if (vol.parentPiece != null)
            {
                vol.longitudeDegrees = Mathf.Repeat(vol.parentPiece.currentLongitude + vol.offsetLonFromParent, 360f);
                vol.latitudeDegrees = Mathf.Clamp(vol.parentPiece.currentLatitude + vol.offsetLatFromParent, -85f, 85f);
            }
            else if (terrain != null)
            {
                var parent = terrain.FindParentPiece(vol.longitudeDegrees, vol.latitudeDegrees);
                if (parent != null)
                {
                    vol.parentPiece = parent;
                    vol.offsetLonFromParent = CubeSphereTerrain.DeltaLongitudeDegrees(vol.longitudeDegrees, parent.currentLongitude);
                    vol.offsetLatFromParent = vol.latitudeDegrees - parent.currentLatitude;
                }
            }

            // 2. State Machine Logic
            switch (vol.state)
            {
                case VolcanoState.Growth:
                    // Grow radius and peak height
                    vol.currentRadiusDegrees = Mathf.MoveTowards(vol.currentRadiusDegrees, vol.targetRadiusDegrees, vol.growthSpeed * simDt);
                    vol.currentPeakHeight = Mathf.MoveTowards(vol.currentPeakHeight, vol.targetPeakHeight, vol.growthSpeed * 0.5f * simDt);
                    terrainNeedsRebuild = true;

                    // Transition to Eruptive once grown or after timer
                    if (Mathf.Approximately(vol.currentRadiusDegrees, vol.targetRadiusDegrees) || vol.stateTimer >= vol.currentPhaseDuration)
                    {
                        TransitionToState(vol, VolcanoState.Eruptive);
                    }
                    break;

                case VolcanoState.Eruptive:
                    // Emit gases into atmosphere
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddVolcanicGases(
                            vol.co2EmissionRate * simDt,
                            vol.waterVaporEmissionRate * simDt,
                            vol.otherGasesEmissionRate * simDt
                        );
                    }

                    // Transition to Dormant after eruption phase
                    if (vol.stateTimer >= vol.currentPhaseDuration)
                    {
                        TransitionToState(vol, VolcanoState.Dormant);
                    }
                    break;

                case VolcanoState.Dormant:
                    // Check if temporary or permanent dormancy
                    if (!vol.isPermanentlyDormant && vol.stateTimer >= vol.currentPhaseDuration)
                    {
                        // 60% chance to re-erupt, 40% chance to become permanently dormant
                        if (UnityEngine.Random.value < 0.6f)
                        {
                            TransitionToState(vol, VolcanoState.Eruptive);
                        }
                        else
                        {
                            vol.isPermanentlyDormant = true;
                            Debug.Log($"[VolcanoManager] Volcano [{vol.id}] is now permanently dormant.");
                        }
                    }
                    break;
            }

            // Update particle system position & status
            UpdateVolcanoVisuals(vol);
        }

        if (terrainNeedsRebuild && terrain != null)
        {
            RebuildTerrainVolcanoes();
        }
    }

    private void TransitionToState(VolcanoInstance vol, VolcanoState newState)
    {
        vol.state = newState;
        vol.stateTimer = 0f;

        switch (newState)
        {
            case VolcanoState.Eruptive:
                vol.currentPhaseDuration = UnityEngine.Random.Range(150f, 400f);

                if (vol.particleSystemObject != null)
                {
                    ParticleSystem[] particleSystems = vol.particleSystemObject.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in particleSystems)
                    {
                        ps.Play(true);
                    }
                }
                else
                {
                    CreateVolcanoParticleSystem(vol);
                }

                if (terrain != null)
                {
                    Vector3 localDir = MeteorEventController.DegreesToLocalDirection(vol.longitudeDegrees, vol.latitudeDegrees);
                    float h = terrain.GetHeightAtDegrees(vol.longitudeDegrees, vol.latitudeDegrees);
                    Vector3 localPos = localDir * (terrain.BaseRadius + h * terrain.HeightScale);
                    Vector3 volPos = terrain.transform.TransformPoint(localPos);
                    Vector3 volNormal = terrain.transform.TransformDirection(localDir).normalized;

                    TriggerEruptionBurstEffect(volPos, volNormal, vol.currentRadiusDegrees);

                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayVolcanoEruption(volPos, vol.currentRadiusDegrees);
                        AudioManager.Instance.PlayVolcanicExplosion(volPos, vol.currentRadiusDegrees, UnityEngine.Random.Range(0.85f, 1.15f));
                    }
                }

                Debug.Log($"[VolcanoManager] Volcano [{vol.id}] entering ERUPTIVE phase!");
                break;

            case VolcanoState.Dormant:
                vol.currentPhaseDuration = UnityEngine.Random.Range(200f, 600f);
                if (vol.particleSystemObject != null)
                {
                    ParticleSystem[] particleSystems = vol.particleSystemObject.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in particleSystems)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    }
                }
                Debug.Log($"[VolcanoManager] Volcano [{vol.id}] entering DORMANT phase.");
                break;
        }
    }

    private void TriggerEruptionBurstEffect(Vector3 position, Vector3 normal, float radiusDegrees)
    {
        GameObject prefabToUse = volcanoExplosionPrefab;
        if (prefabToUse == null)
        {
            prefabToUse = Resources.Load<GameObject>("ParticlePack/DustExplosion")
                       ?? Resources.Load<GameObject>("ParticlePack/BigExplosion");
        }

        if (prefabToUse != null)
        {
            float scale = Mathf.Clamp(radiusDegrees / (supercontinentRadiusDegrees * 0.05f), 0.5f, 3f);
            GameObject burstInst = Instantiate(prefabToUse, position, Quaternion.LookRotation(normal));
            burstInst.name = "VolcanoEruptionBurst";
            burstInst.transform.localScale = Vector3.one * scale;

            ParticleSystem[] pss = burstInst.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in pss)
            {
                ps.Play(true);
            }

            Destroy(burstInst, 5.0f);
        }
    }

    private void UpdateVolcanoVisuals(VolcanoInstance vol)
    {
        if (terrain == null) return;

        if (vol.particleSystemObject == null && vol.state == VolcanoState.Eruptive)
        {
            CreateVolcanoParticleSystem(vol);
        }

        if (vol.particleSystemObject != null)
        {
            // Position on surface
            Vector3 localDir = MeteorEventController.DegreesToLocalDirection(vol.longitudeDegrees, vol.latitudeDegrees);
            float h = terrain.GetHeightAtDegrees(vol.longitudeDegrees, vol.latitudeDegrees);
            Vector3 localPos = localDir * (terrain.BaseRadius + h * terrain.HeightScale);
            Vector3 worldPos = terrain.transform.TransformPoint(localPos);
            Vector3 worldNormal = terrain.transform.TransformDirection(localDir).normalized;

            vol.particleSystemObject.transform.position = worldPos;
            vol.particleSystemObject.transform.rotation = Quaternion.LookRotation(worldNormal);

            float scale = Mathf.Clamp(vol.currentRadiusDegrees / (supercontinentRadiusDegrees * 0.05f), 0.4f, 2.5f);
            vol.particleSystemObject.transform.localScale = Vector3.one * scale;
        }
    }

    private void CreateVolcanoParticleSystem(VolcanoInstance vol)
    {
        GameObject pObj = new GameObject($"VolcanoEruption_{vol.id}");
        float scale = Mathf.Clamp(vol.currentRadiusDegrees / (supercontinentRadiusDegrees * 0.05f), 0.4f, 2.5f);
        pObj.transform.localScale = Vector3.one * scale;

        // Try loading ParticlePack Flame Stream and Smoke prefabs
        GameObject flamePrefabToUse = volcanoFlamePrefab;
        if (flamePrefabToUse == null)
        {
            flamePrefabToUse = Resources.Load<GameObject>("ParticlePack/FlameStream")
                            ?? Resources.Load<GameObject>("ParticlePack/LargeFlames");
        }

        GameObject smokePrefabToUse = volcanoSmokePrefab;
        if (smokePrefabToUse == null)
        {
            smokePrefabToUse = Resources.Load<GameObject>("ParticlePack/SmokeEffect");
        }

        bool attachedParticlePack = false;

        if (flamePrefabToUse != null)
        {
            GameObject flameInst = Instantiate(flamePrefabToUse, pObj.transform);
            flameInst.name = "FlameStream";
            flameInst.transform.localPosition = Vector3.zero;
            flameInst.transform.localRotation = Quaternion.identity;
            flameInst.transform.localScale = Vector3.one;
            attachedParticlePack = true;
        }

        if (smokePrefabToUse != null)
        {
            GameObject smokeInst = Instantiate(smokePrefabToUse, pObj.transform);
            smokeInst.name = "SmokeColumn";
            smokeInst.transform.localPosition = Vector3.up * 0.2f;
            smokeInst.transform.localRotation = Quaternion.identity;
            smokeInst.transform.localScale = Vector3.one * 1.2f;
            attachedParticlePack = true;
        }

        // Fallback procedural particle system if no prefabs loaded
        if (!attachedParticlePack)
        {
            ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 2.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.45f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 25f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.15f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.4f, 0.05f), 0f),   // Bright lava fire
                    new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 0.4f), // Dark ash smoke
                    new GradientColorKey(new Color(0.1f, 0.1f, 0.1f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.6f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = grad;

            if (sharedVolcanoParticleMaterial == null)
            {
                var texture = Resources.Load<Texture2D>("Textures/particle_spark")
                           ?? Resources.Load<Texture2D>("Textures/particle_spark_old");
                Shader particleShader = Shader.Find("Particles/Standard Unlit")
                                     ?? Shader.Find("Mobile/Particles/Additive")
                                     ?? Shader.Find("Sprites/Default");
                sharedVolcanoParticleMaterial = new Material(particleShader);
                if (texture != null)
                {
                    sharedVolcanoParticleMaterial.mainTexture = texture;
                }
            }

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (sharedVolcanoParticleMaterial != null)
            {
                renderer.sharedMaterial = sharedVolcanoParticleMaterial;
            }

            vol.particleSystemRef = ps;
        }
        else
        {
            vol.particleSystemRef = pObj.GetComponentInChildren<ParticleSystem>();
        }

        vol.particleSystemObject = pObj;

        // Start emitting if in Eruptive state
        if (vol.state == VolcanoState.Eruptive)
        {
            ParticleSystem[] particleSystems = pObj.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                ps.Play(true);
            }
        }
    }

    public void RebuildTerrainVolcanoes()
    {
        if (terrain == null) return;

        // Sync with CubeSphereTerrain
        terrain.SyncVolcanoes(volcanoes);
    }
}
