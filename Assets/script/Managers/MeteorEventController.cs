using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum MeteorSizeTier
{
    Small,
    Medium,
    Large,
    Massive
}

[System.Serializable]
public struct MeteorSizeData
{
    public MeteorSizeTier tier;
    public string displayName;
    public float scaleFactor;
    public float radiusDegrees;
    public float depth;
    public float rimHeight;
    public float gasRelease;
    public float tsunamiRise;
}

public class MeteorEventController : MonoBehaviour
{
    [Header("Meteor Settings")]
    [SerializeField] private GameObject meteorPrefab;
    [SerializeField] private float flightDuration = 2.0f;

    public Button MeteorButton { get; private set; }

    private CubeSphereTerrain terrain;

    private void Start()
    {
        terrain = FindAnyObjectByType<CubeSphereTerrain>();
        CreateMeteorUI();
    }

    private void Update()
    {
        if (MeteorButton != null)
        {
            bool isPrebiotic = GameManager.Instance != null && GameManager.Instance.CurrentEpoch == PlanetEpoch.Prebiotic;
            MeteorButton.interactable = isPrebiotic;
        }
    }

    private void CreateMeteorUI()
    {
        RectTransform hudRoot = transform as RectTransform;
        if (hudRoot == null)
        {
            Debug.LogWarning("[MeteorEventController] Aucun RectTransform sur ce GameObject.");
            return;
        }

        // Create a new Row GameObject
        GameObject rowGo = new GameObject("MeteorRow", typeof(RectTransform));
        rowGo.transform.SetParent(hudRoot, false);

        RectTransform rowRect = rowGo.GetComponent<RectTransform>();
        rowRect.localScale = Vector3.one;

        // Add LayoutElement to row
        LayoutElement rowLayout = rowGo.AddComponent<LayoutElement>();
        rowLayout.minHeight = 44f;
        rowLayout.preferredHeight = 44f;
        rowLayout.flexibleHeight = 0f;
        rowLayout.flexibleWidth = 1f;

        // Add HorizontalLayoutGroup to row
        HorizontalLayoutGroup horizontal = rowGo.AddComponent<HorizontalLayoutGroup>();
        horizontal.childAlignment = TextAnchor.MiddleLeft;
        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = true;
        horizontal.spacing = 10f;

        // Create Label
        GameObject labelGo = new GameObject("MeteorLabel", typeof(RectTransform));
        labelGo.transform.SetParent(rowRect, false);
        TextMeshProUGUI labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.text = "Meteor Event :";
        labelText.fontSize = 22;
        labelText.fontStyle = FontStyles.Normal;
        labelText.color = new Color(0.83f, 0.86f, 0.90f, 1f); // Subtle color
        labelText.alignment = TextAlignmentOptions.Left;

        LayoutElement labelLayout = labelGo.AddComponent<LayoutElement>();
        labelLayout.minWidth = 120f;
        labelLayout.flexibleWidth = 1f;

        // Create Button
        GameObject buttonGo = new GameObject("MeteorButton", typeof(RectTransform));
        buttonGo.transform.SetParent(rowRect, false);

        // Add Image for button background
        Image buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = new Color(0.82f, 0.28f, 0.35f, 1f); // Nice red/crimson button

        // Add Button component
        Button button = buttonGo.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        MeteorButton = button;

        // Color block for button states
        ColorBlock cb = button.colors;
        cb.normalColor = new Color(0.82f, 0.28f, 0.35f, 1f);
        cb.highlightedColor = new Color(0.92f, 0.38f, 0.45f, 1f);
        cb.pressedColor = new Color(0.62f, 0.18f, 0.25f, 1f);
        cb.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
        button.colors = cb;

        // Create Button Text
        GameObject buttonTextGo = new GameObject("Text", typeof(RectTransform));
        buttonTextGo.transform.SetParent(buttonGo.transform, false);
        TextMeshProUGUI buttonText = buttonTextGo.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Déclencher";
        buttonText.enableAutoSizing = true;
        buttonText.fontSizeMin = 12;
        buttonText.fontSizeMax = 18;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;

        // Fit text to button
        RectTransform textRect = buttonTextGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        LayoutElement buttonLayout = buttonGo.AddComponent<LayoutElement>();
        buttonLayout.minWidth = 140f;
        buttonLayout.preferredWidth = 160f;
        buttonLayout.flexibleWidth = 0f;
        buttonLayout.minHeight = 32f;
        buttonLayout.preferredHeight = 32f;

        // Bind click event
        button.onClick.AddListener(TriggerMeteor);

        // Rebuild layout
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(hudRoot);
    }

    public static MeteorSizeData GetMeteorSizeData(MeteorSizeTier tier)
    {
        MeteorSizeData data = new MeteorSizeData();
        data.tier = tier;

        switch (tier)
        {
            case MeteorSizeTier.Small:
                data.displayName = "Petit Météore";
                data.scaleFactor = Random.Range(0.5f, 0.7f);
                data.radiusDegrees = Random.Range(0.25f, 0.40f);
                data.depth = Random.Range(0.08f, 0.15f);
                data.rimHeight = Random.Range(0.04f, 0.08f);
                data.gasRelease = Random.Range(2.5f, 5.5f);
                data.tsunamiRise = Random.Range(0.04f, 0.09f);
                break;

            case MeteorSizeTier.Medium:
                data.displayName = "Météore Standard";
                data.scaleFactor = Random.Range(0.9f, 1.15f);
                data.radiusDegrees = Random.Range(0.45f, 0.70f);
                data.depth = Random.Range(0.20f, 0.35f);
                data.rimHeight = Random.Range(0.12f, 0.22f);
                data.gasRelease = Random.Range(8.0f, 15.0f);
                data.tsunamiRise = Random.Range(0.12f, 0.22f);
                break;

            case MeteorSizeTier.Large:
                data.displayName = "Grand Météore";
                data.scaleFactor = Random.Range(1.6f, 2.1f);
                data.radiusDegrees = Random.Range(0.75f, 1.10f);
                data.depth = Random.Range(0.42f, 0.60f);
                data.rimHeight = Random.Range(0.25f, 0.38f);
                data.gasRelease = Random.Range(20.0f, 32.0f);
                data.tsunamiRise = Random.Range(0.25f, 0.38f);
                break;

            case MeteorSizeTier.Massive:
                data.displayName = "Météore Cataclysmique";
                data.scaleFactor = Random.Range(2.7f, 3.4f);
                data.radiusDegrees = Random.Range(1.20f, 1.50f);
                data.depth = Random.Range(0.70f, 0.95f);
                data.rimHeight = Random.Range(0.45f, 0.65f);
                data.gasRelease = Random.Range(45.0f, 75.0f);
                data.tsunamiRise = Random.Range(0.40f, 0.50f);
                break;
        }

        return data;
    }

    public static MeteorSizeTier GetRandomMeteorSizeTier()
    {
        float roll = Random.value;
        if (roll < 0.35f) return MeteorSizeTier.Small;      // 35% chance
        if (roll < 0.70f) return MeteorSizeTier.Medium;     // 35% chance
        if (roll < 0.92f) return MeteorSizeTier.Large;      // 22% chance
        return MeteorSizeTier.Massive;                     // 8% chance
    }

    public void TriggerMeteor()
    {
        TriggerMeteor(GetRandomMeteorSizeTier());
    }

    public void TriggerMeteor(MeteorSizeTier sizeTier)
    {
        if (terrain == null)
        {
            terrain = FindAnyObjectByType<CubeSphereTerrain>();
        }

        if (terrain == null)
        {
            Debug.LogWarning("[MeteorEventController] Impossible de declencher le meteor, aucun CubeSphereTerrain trouve.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[MeteorEventController] GameManager absent.");
            return;
        }

        MeteorSizeData sizeData = GetMeteorSizeData(sizeTier);

        // 1. Pick a random impact position on the planet sphere (latitude restricted to avoid polar crunch)
        float lon = Random.Range(0f, 360f);
        float lat = Random.Range(-60f, 60f);

        PlanetEpoch epoch = GameManager.Instance.CurrentEpoch;
        GameManager.Instance.LogEvent("Meteor Strike Triggered", $"Type: {sizeData.displayName} (Scale: {sizeData.scaleFactor:F2}x), Coordinates: ({lon:F1} deg E, {lat:F1} deg N), Radius: {sizeData.radiusDegrees:F1} deg, Epoch: {epoch}");

        float currentHeight = terrain.GetHeightAtDegrees(lon, lat);
        float waterLevel = 0.03f * GameManager.Instance.RawWaterRatio; // Compare with actual water level, not temporary rise

        bool isOceanic = (currentHeight < waterLevel) && (epoch >= PlanetEpoch.ProtoOcean);

        Debug.Log($"[MeteorEventController] Target impact: {sizeData.displayName} (Scale: {sizeData.scaleFactor:F2}x) at {lon:F2} lon, {lat:F2} lat. Height: {currentHeight:F4}. Oceanic: {isOceanic}");

        // 2. Compute 3D target coordinates on the surface
        Vector3 localImpactPos = GetLocalSurfacePosition(terrain, lon, lat);
        Vector3 localImpactDir = DegreesToLocalDirection(lon, lat);

        // 3. Compute Slingshot trajectory (p0 = deep space, p1 = control point arc)
        CalculateSlingshotTrajectory(localImpactDir, terrain.transform.position, out Vector3 p0, out Vector3 p1);

        // 4. Instantiate Meteor Object with visual scale factor
        GameObject meteorObj = SpawnMeteorObject(p0, sizeData.scaleFactor);

        // Play meteor flight sound effect
        if (AudioManager.Instance != null && meteorObj != null)
        {
            AudioManager.Instance.PlayMeteorFlight(meteorObj);
        }

        // 5. Start animation coroutine (deferred impact execution)
        StartCoroutine(AnimateMeteorFlight(meteorObj, p0, p1, localImpactPos, lon, lat, sizeData, epoch, isOceanic));
    }

    private GameObject SpawnMeteorObject(Vector3 startPosition, float scaleFactor)
    {
        GameObject meteorObj = null;

        if (meteorPrefab == null)
        {
            meteorPrefab = Resources.Load<GameObject>("meteorPrefab");
        }

        if (meteorPrefab != null)
        {
            meteorObj = Instantiate(meteorPrefab, startPosition, Quaternion.identity);
            meteorObj.transform.localScale = meteorObj.transform.localScale * scaleFactor;
        }
        else
        {
            meteorObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            meteorObj.name = "MeteorFallback";
            meteorObj.transform.position = startPosition;
            meteorObj.transform.localScale = Vector3.one * 2.5f * scaleFactor;

            Collider col = meteorObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            MeshRenderer mr = meteorObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material.color = new Color(0.9f, 0.4f, 0.1f);
            }
        }

        AttachFlightTrail(meteorObj, scaleFactor);

        return meteorObj;
    }

    private void AttachFlightTrail(GameObject meteorObj, float scaleFactor)
    {
        if (meteorObj == null) return;

        GameObject trailGo = new GameObject("MeteorTrail");
        trailGo.transform.SetParent(meteorObj.transform, false);

        ParticleSystem ps = trailGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f * scaleFactor, 0.7f * scaleFactor);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * scaleFactor, 2.0f * scaleFactor);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f * scaleFactor, 2.0f * scaleFactor);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 80f * scaleFactor;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f * scaleFactor;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.7f, 0.2f), 0f), new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 0.7f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var texture = Resources.Load<Texture2D>("Textures/particle_spark_old");
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.mainTexture = texture;

        ps.Play();
    }

    private System.Collections.IEnumerator AnimateMeteorFlight(
        GameObject meteorObj,
        Vector3 p0,
        Vector3 p1,
        Vector3 localImpactPos,
        float lon,
        float lat,
        MeteorSizeData sizeData,
        PlanetEpoch epoch,
        bool isOceanic)
    {
        float elapsed = 0f;
        Vector3 localImpactDir = localImpactPos.normalized;

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightDuration);
            // Gravity acceleration effect
            float tAccel = t * t;

            // Dynamically re-evaluate world impact position every frame to track planet rotation
            Vector3 p2 = terrain.transform.TransformPoint(localImpactPos);

            Vector3 currentPos = EvaluateQuadraticBezier(p0, p1, p2, tAccel);
            if (meteorObj != null)
            {
                meteorObj.transform.position = currentPos;

                Vector3 nextPos = EvaluateQuadraticBezier(p0, p1, p2, Mathf.Clamp01(tAccel + 0.02f));
                Vector3 moveDir = (nextPos - currentPos).normalized;
                if (moveDir != Vector3.zero)
                {
                    meteorObj.transform.rotation = Quaternion.LookRotation(moveDir);
                }
                meteorObj.transform.Rotate(Vector3.forward, 360f * Time.deltaTime, Space.Self);
            }

            yield return null;
        }

        // --- IMPACT MOMENT ---
        Vector3 finalImpactPos = terrain.transform.TransformPoint(localImpactPos);
        Vector3 worldNormal = terrain.transform.TransformDirection(localImpactDir).normalized;

        // Play meteor impact sound effect
        if (AudioManager.Instance != null)
        {
            float pitch = Mathf.Clamp(1.2f - sizeData.scaleFactor * 0.2f, 0.6f, 1.4f);
            AudioManager.Instance.PlayMeteorImpact(finalImpactPos, sizeData.scaleFactor, pitch);
        }

        // 1. Spawn Impact Particle System with scale factor
        SpawnImpactParticleSystem(finalImpactPos, worldNormal, sizeData.scaleFactor);

        // 2. Apply Deferred Epoch Terrain Deformations & Game Events
        ApplyImpactEffects(lon, lat, sizeData, epoch, isOceanic);

        // 3. Destroy meteor object
        if (meteorObj != null)
        {
            Destroy(meteorObj);
        }
    }

    private void ApplyImpactEffects(float lon, float lat, MeteorSizeData sizeData, PlanetEpoch epoch, bool isOceanic)
    {
        // 1. Always release atmospheric volatile gases proportional to meteor size
        if (sizeData.gasRelease > 0f)
        {
            GameManager.Instance?.AddMeteorGases(sizeData.gasRelease);
        }

        // 2. Epoch-tailored terrain deformation and aquatic effects
        float radius = sizeData.radiusDegrees;
        float depth = sizeData.depth;
        float rim = sizeData.rimHeight;

        if (epoch == PlanetEpoch.Hadean)
        {
            // Transient craters on magma surface
            terrain.AddCraterDegrees(lon, lat, radius, depth, rim, targetFadeVal: 0f, fadeSpeedVal: 0.015f);
        }
        else if (epoch == PlanetEpoch.CrustFormation)
        {
            float attenuation = Random.Range(0.25f, 0.45f);
            float targetFade = 1.0f - attenuation;

            terrain.AddCraterDegrees(lon, lat, radius, depth, rim, targetFadeVal: targetFade, fadeSpeedVal: 0.015f);
        }
        else if (epoch == PlanetEpoch.VolcanicAge)
        {
            float attenuation = Random.Range(0.25f, 0.45f);
            float targetFade = 1.0f - attenuation;

            terrain.AddCraterDegrees(lon, lat, radius, depth, rim, targetFadeVal: targetFade, fadeSpeedVal: 0.012f);

            // Number and size of induced volcanic fractures scales with meteor size
            int numVolcanoes = sizeData.tier switch
            {
                MeteorSizeTier.Small => 1,
                MeteorSizeTier.Medium => Random.Range(1, 3),
                MeteorSizeTier.Large => Random.Range(2, 4),
                MeteorSizeTier.Massive => Random.Range(3, 6),
                _ => 1
            };

            for (int i = 0; i < numVolcanoes; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = Random.Range(0.5f, 1.5f) * sizeData.scaleFactor;
                float volLon = Mathf.Repeat(lon + dist * Mathf.Cos(angle), 360f);
                float volLat = Mathf.Clamp(lat + dist * Mathf.Sin(angle), -85f, 85f);
                float volRad = Random.Range(0.4f, 1.0f) * sizeData.scaleFactor;
                float volHeight = Random.Range(0.35f, 0.65f) * sizeData.scaleFactor;

                terrain.AddTemporaryVolcanoDegrees(volLon, volLat, volRad, volHeight, fadeSpeedVal: 0.008f);
            }
        }
        else if (epoch == PlanetEpoch.ProtoOcean || epoch == PlanetEpoch.TectonicDrift)
        {
            if (isOceanic)
            {
                GameManager.Instance?.TriggerTsunami(sizeData.tsunamiRise);

                terrain.AddCraterDegrees(lon, lat, radius, depth * 0.7f, rim * 0.6f, targetFadeVal: 0.5f, fadeSpeedVal: 0.02f);
            }
            else
            {
                float attenuation = Random.Range(0.25f, 0.45f);
                float targetFade = 1.0f - attenuation;

                terrain.AddCraterDegrees(lon, lat, radius, depth, rim, targetFadeVal: targetFade, fadeSpeedVal: 0.015f);
            }
        }

        terrain.RebuildHeightField();
    }

    private void SpawnImpactParticleSystem(Vector3 position, Vector3 normal, float scaleFactor)
    {
        GameObject impactGo = new GameObject("MeteorImpactParticles");
        impactGo.transform.position = position;
        impactGo.transform.rotation = Quaternion.LookRotation(normal);

        // 1. Debris & Fire Explosion Burst
        ParticleSystem ps = impactGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1.0f * Mathf.Clamp(scaleFactor, 0.8f, 2.5f);
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f * scaleFactor, 1.3f * scaleFactor);
        main.startSpeed = new ParticleSystem.MinMaxCurve(12f * scaleFactor, 28f * scaleFactor);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f * scaleFactor, 1.8f * scaleFactor);
        main.gravityModifier = 0.35f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        short burstCount = (short)Mathf.Clamp(Mathf.RoundToInt(180 * scaleFactor), 50, 1000);
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, burstCount) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 55f;
        shape.radius = 0.8f * scaleFactor;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0.05f), 0.3f),
                new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 0.7f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.4f);
        curve.AddKey(0.2f, 1.2f);
        curve.AddKey(1f, 0.1f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var texture = Resources.Load<Texture2D>("Textures/particle_spark");
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.mainTexture = texture;

        // 2. Shockwave Ring (Child)
        GameObject ringGo = new GameObject("ShockwaveRing");
        ringGo.transform.SetParent(impactGo.transform, false);
        ParticleSystem ringPs = ringGo.AddComponent<ParticleSystem>();
        ringPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var rMain = ringPs.main;
        rMain.duration = 0.8f * Mathf.Clamp(scaleFactor, 0.8f, 2.5f);
        rMain.loop = false;
        rMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f * scaleFactor, 0.8f * scaleFactor);
        rMain.startSpeed = new ParticleSystem.MinMaxCurve(15f * scaleFactor, 35f * scaleFactor);
        rMain.startSize = new ParticleSystem.MinMaxCurve(0.6f * scaleFactor, 1.5f * scaleFactor);
        rMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var rEmission = ringPs.emission;
        rEmission.rateOverTime = 0f;
        short ringBurstCount = (short)Mathf.Clamp(Mathf.RoundToInt(120 * scaleFactor), 30, 800);
        rEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, ringBurstCount) });

        var rShape = ringPs.shape;
        rShape.shapeType = ParticleSystemShapeType.Donut;
        rShape.radius = 1.0f * scaleFactor;
        rShape.donutRadius = 0.2f * scaleFactor;

        var rColorOverLifetime = ringPs.colorOverLifetime;
        rColorOverLifetime.enabled = true;
        rColorOverLifetime.color = grad;

        ps.Play();
        ringPs.Play();

        Destroy(impactGo, 4.5f);
    }

    /// <summary>
    /// Converts (lon, lat) degrees into a normalized 3D local direction vector.
    /// </summary>
    public static Vector3 DegreesToLocalDirection(float lonDeg, float latDeg)
    {
        float lonRad = lonDeg * Mathf.Deg2Rad;
        float latRad = latDeg * Mathf.Deg2Rad;
        float y = Mathf.Sin(latRad);
        float cosLat = Mathf.Cos(latRad);
        float x = cosLat * Mathf.Cos(lonRad);
        float z = cosLat * Mathf.Sin(lonRad);
        return new Vector3(x, y, z).normalized;
    }

    /// <summary>
    /// Gets local position of terrain surface at given (lon, lat) degrees.
    /// </summary>
    private Vector3 GetLocalSurfacePosition(CubeSphereTerrain terrainRef, float lonDeg, float latDeg)
    {
        Vector3 localDir = DegreesToLocalDirection(lonDeg, latDeg);
        float currentHeight = terrainRef.GetHeightAtDegrees(lonDeg, latDeg);
        return localDir * (terrainRef.BaseRadius + currentHeight * terrainRef.HeightScale);
    }

    /// <summary>
    /// Evaluates Quadratic Bézier curve B(t) = (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
    /// </summary>
    public static Vector3 EvaluateQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    /// <summary>
    /// Computes start position (deep space far outside FOV) and control point for slingshot trajectory.
    /// </summary>
    private void CalculateSlingshotTrajectory(Vector3 localImpactDir, Vector3 planetWorldCenter, out Vector3 p0, out Vector3 p1)
    {
        Vector3 worldImpactDir = terrain.transform.TransformDirection(localImpactDir).normalized;

        Vector3 randomPerp = Vector3.Cross(worldImpactDir, Random.onUnitSphere).normalized;
        if (randomPerp == Vector3.zero) randomPerp = Vector3.right;

        Vector3 spawnDir = (worldImpactDir + randomPerp * Random.Range(0.8f, 1.4f)).normalized;
        float spawnDistance = Random.Range(65f, 85f);
        p0 = planetWorldCenter + spawnDir * spawnDistance;

        Vector3 midPoint = (p0 + (planetWorldCenter + worldImpactDir * 5f)) * 0.5f;
        Vector3 arcOffset = Vector3.Cross(p0 - planetWorldCenter, randomPerp).normalized * Random.Range(20f, 35f);
        p1 = midPoint + arcOffset;
    }
}
