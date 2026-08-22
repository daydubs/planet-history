using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Sphère lisse (cube-sphere) dont le relief n'est PAS géométrique:
/// il est stocké dans un <see cref="PlanetHeightField"/> équirectangulaire et rendu
/// par le shader PlanetHistory/PlanetSurface (normales + couleur dérivées du heightmap).
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CubeSphereTerrain : MonoBehaviour
{
    [System.Serializable]
    public class ContinentalPiece
    {
        public string name;
        public float baseLongitude; // in degrees
        public float baseLatitude;  // in degrees
        public float radius;        // in degrees
        public float height;        // target height
        public float driftSpeedLon; // degrees per simulation unit
        public float driftSpeedLat; // degrees per simulation unit

        [HideInInspector] public float currentLongitude;
        [HideInInspector] public float currentLatitude;
        [HideInInspector] public float currentHeight;

        // Sector fracture parameters
        [HideInInspector] public float sectorStartAngle; // Radians relative to supercontinent center
        [HideInInspector] public float sectorEndAngle;   // Radians relative to supercontinent center
        [HideInInspector] public float supercontinentCenterLon; // degrees
        [HideInInspector] public float supercontinentCenterLat; // degrees

        // Pre-baked local heightmap grid
        [HideInInspector] public float[] localHeights;
        [HideInInspector] public int localGridSize;
    }

    [System.Serializable]
    public class VolcanoStamp
    {
        public float longitudeDegrees;
        public float latitudeDegrees;
        public float radiusDegrees;
        public float peakHeight;
        public float rate;

        // Fading / Temporary volcano support
        public bool isTemporary;
        public float maxPeakHeight;
        public float currentFade = 1f;
        public float fadeSpeed = 0.05f; // per simulation unit step

        // Tectonic drift attachment
        public ContinentalPiece parentPiece;
        public float offsetLonFromParent;
        public float offsetLatFromParent;
    }

    [System.Serializable]
    public class CraterStamp
    {
        public string name;
        public float longitudeDegrees;
        public float latitudeDegrees;
        public float radiusDegrees;
        public float maxDepth;
        public float maxRimHeight;
        public float currentFade = 1f;
        public float targetFade = 0f;
        public float fadeSpeed = 0.05f; // per simulation unit step

        // Tectonic drift attachment
        public ContinentalPiece parentPiece;
        public float offsetLonFromParent;
        public float offsetLatFromParent;
    }

    private static readonly int HeightTexId = Shader.PropertyToID("_HeightTex");
    private static readonly int DisplaceScaleId = Shader.PropertyToID("_DisplaceScale");
    private static readonly int SurfaceTemperatureId = Shader.PropertyToID("_SurfaceTemperature");
    private static readonly int WaterRatioId = Shader.PropertyToID("_WaterRatio");

    [Header("Shape")]
    [SerializeField, Range(4, 128)] private int resolution = 32;
    [SerializeField] private float baseRadius = 5f;
    [SerializeField] private float heightScale = 1.0f;

    [Header("Height Field")]
    [SerializeField, Range(64, 4096)] private int heightFieldWidth = 1024;
    [SerializeField, Min(0)] private int poleFlatRows = 2;
    [SerializeField] private float poleHeight;

    [Header("Debug Volcano")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool addTestVolcanoOnStart;
    [SerializeField] private float testLongitudeDegrees = 45f;
    [SerializeField] private float testLatitudeDegrees = 15f;
    [SerializeField] private float testRadiusDegrees = 12f;
    [SerializeField] private float testPeak = 0.6f;

    [Header("Supercontinent Configuration")]
    [SerializeField] private ContinentalPiece[] continentalPieces;

    [Header("Randomization Configuration")]
    [SerializeField] private bool randomizeOnStart = true;
    [SerializeField] private int seed = 0;

    [Header("Performance Optimization")]
    [SerializeField, Range(0.01f, 0.2f)] private float rebuildInterval = 0.066f;

    private System.Collections.Generic.List<VolcanoStamp> activeVolcanoes = new System.Collections.Generic.List<VolcanoStamp>();
    private System.Collections.Generic.List<CraterStamp> activeCraters = new System.Collections.Generic.List<CraterStamp>();
    private float lastSimTime;
    private float lastRebuildTime;
    private bool pendingRebuild;
    private Vector3 activeNoiseOffset = Vector3.zero;
    private bool hasRandomized = false;

    private Mesh mesh;
    private PlanetHeightField field;
    private MeshRenderer meshRenderer;
    private Material instantiatedMaterial;

    private Vector3[] baseDirs; // direction sphérique de chaque vertex
    private Vector3[] vertices;
    private Vector3[] normals;
    private Vector2[] uvs;
    private int[] triangles;

    public PlanetHeightField Field => field;
    public float BaseRadius => baseRadius;
    public float HeightScale => heightScale;
    public ContinentalPiece[] ContinentalPieces => continentalPieces;

    private void Start()
    {
        InitializeContinentalPieces();

        if (!generateOnStart) return;

        Build();

        if (addTestVolcanoOnStart)
        {
            AddVolcanoDegrees(testLongitudeDegrees, testLatitudeDegrees, testRadiusDegrees, testPeak);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSimulationStep += HandleSimulationStep;
            lastSimTime = GameManager.Instance.SimulationTimeSeconds;

            // Match current epoch state instantly on start (useful if loading/starting in mid-epoch)
            UpdateTerrainSimulation(0f);
        }
    }

    private void Update()
    {
        if (Application.isPlaying && GameManager.Instance != null && meshRenderer != null)
        {
            if (instantiatedMaterial == null)
            {
                instantiatedMaterial = meshRenderer.material;
            }

            if (instantiatedMaterial != null)
            {
                if (instantiatedMaterial.HasProperty(SurfaceTemperatureId))
                {
                    instantiatedMaterial.SetFloat(SurfaceTemperatureId, GameManager.Instance.SurfaceTemperature);
                }
                if (instantiatedMaterial.HasProperty(WaterRatioId))
                {
                    instantiatedMaterial.SetFloat(WaterRatioId, GameManager.Instance.WaterRatio);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSimulationStep -= HandleSimulationStep;
        }
        if (field != null)
        {
            field.OnCleared -= HandleFieldCleared;
            field.Dispose();
        }
        field = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        resolution = Mathf.Max(4, resolution);
        baseRadius = Mathf.Max(0.01f, baseRadius);
        heightScale = Mathf.Max(0f, heightScale);
        heightFieldWidth = Mathf.Max(64, heightFieldWidth);
        testRadiusDegrees = Mathf.Max(0.1f, testRadiusDegrees);

        if (!Application.isPlaying && generateOnStart)
        {
            Build();
        }
    }
#endif

    [ContextMenu("Build CubeSphere")]
    public void Build()
    {
        resolution = Mathf.Max(4, resolution);

        InitializeContinentalPieces();

        if (field != null)
        {
            field.OnCleared -= HandleFieldCleared;
            field.Dispose();
        }
        field = new PlanetHeightField(heightFieldWidth, heightFieldWidth / 2, poleFlatRows, poleHeight);
        field.OnCleared += HandleFieldCleared;

        field.NoiseOffset = activeNoiseOffset;

        CreateMeshData();
        BindHeightFieldToMaterial();

        RebuildHeightField();
    }

    /// <summary>Assigne le heightmap au matériau (le relief est entièrement rendu par le shader).</summary>
    public void BindHeightFieldToMaterial()
    {
        if (field == null) return;

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (meshRenderer == null) return;

        if (Application.isPlaying && instantiatedMaterial == null)
        {
            instantiatedMaterial = meshRenderer.material;
        }
        Material material = Application.isPlaying ? instantiatedMaterial : meshRenderer.sharedMaterial;
        if (material == null) return;

        material.SetTexture(HeightTexId, field.HeightTex);

        if (material.HasProperty(DisplaceScaleId))
        {
            material.SetFloat(DisplaceScaleId, heightScale);
        }
    }

    /// <summary>Convertit des coordonnées de longitude et latitude (en degrés) en un vecteur directionnel 3D unitaire.</summary>
    public static Vector3 LatLonToVector3(float lonDeg, float latDeg)
    {
        float lonRad = lonDeg * Mathf.Deg2Rad;
        float latRad = latDeg * Mathf.Deg2Rad;
        float cosLat = Mathf.Cos(latRad);
        return new Vector3(
            cosLat * Mathf.Cos(lonRad),
            Mathf.Sin(latRad),
            cosLat * Mathf.Sin(lonRad)
        );
    }

    /// <summary>Calcul de la distance angulaire en degrés entre deux coordonnées géographiques.</summary>
    public static float AngularDistanceDegrees(float lon1Deg, float lat1Deg, float lon2Deg, float lat2Deg)
    {
        float lon1Rad = lon1Deg * Mathf.Deg2Rad;
        float lat1Rad = lat1Deg * Mathf.Deg2Rad;
        float lon2Rad = lon2Deg * Mathf.Deg2Rad;
        float lat2Rad = lat2Deg * Mathf.Deg2Rad;

        float cosD = Mathf.Sin(lat1Rad) * Mathf.Sin(lat2Rad) + Mathf.Cos(lat1Rad) * Mathf.Cos(lat2Rad) * Mathf.Cos(lon1Rad - lon2Rad);
        return Mathf.Acos(Mathf.Clamp(cosD, -1f, 1f)) * Mathf.Rad2Deg;
    }

    /// <summary>Trouve le morceau continental parent chevauchant les coordonnées données.</summary>
    public ContinentalPiece FindParentPiece(float longitudeDegrees, float latitudeDegrees)
    {
        if (continentalPieces == null || continentalPieces.Length == 0) return null;

        ContinentalPiece closestPiece = null;
        float minDistance = float.MaxValue;

        foreach (var piece in continentalPieces)
        {
            float dist = AngularDistanceDegrees(piece.currentLongitude, piece.currentLatitude, longitudeDegrees, latitudeDegrees);
            if (dist <= piece.radius * 1.15f)
            {
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestPiece = piece;
                }
            }
        }

        if (closestPiece != null) return closestPiece;

        // Fallback: always return nearest continental piece so every volcano/crater is anchored to a plate
        foreach (var piece in continentalPieces)
        {
            float dist = AngularDistanceDegrees(piece.currentLongitude, piece.currentLatitude, longitudeDegrees, latitudeDegrees);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestPiece = piece;
            }
        }

        return closestPiece;
    }

    /// <summary>Calcule l'écart en longitude empaqueté dans [-180, 180] degrés.</summary>
    public static float DeltaLongitudeDegrees(float lonA, float lonB)
    {
        float dLon = lonA - lonB;
        while (dLon > 180f) dLon -= 360f;
        while (dLon < -180f) dLon += 360f;
        return dLon;
    }

    /// <summary>Ajoute un volcan en coordonnées géographiques (degrés).</summary>
    public void AddVolcanoDegrees(float longitudeDegrees, float latitudeDegrees, float radiusDegrees, float peakHeight, float rate = 1f)
    {
        var parent = FindParentPiece(longitudeDegrees, latitudeDegrees);
        activeVolcanoes.Add(new VolcanoStamp
        {
            longitudeDegrees = longitudeDegrees,
            latitudeDegrees = latitudeDegrees,
            radiusDegrees = radiusDegrees,
            peakHeight = peakHeight,
            rate = rate,
            parentPiece = parent,
            offsetLonFromParent = parent != null ? DeltaLongitudeDegrees(longitudeDegrees, parent.currentLongitude) : 0f,
            offsetLatFromParent = parent != null ? latitudeDegrees - parent.currentLatitude : 0f
        });

        field?.AddVolcano(
            longitudeDegrees * Mathf.Deg2Rad,
            latitudeDegrees * Mathf.Deg2Rad,
            radiusDegrees * Mathf.Deg2Rad,
            peakHeight,
            rate);
    }

    /// <summary>Synchronise la liste complète des volcans actifs sans ajouter de doublons.</summary>
    public void SyncVolcanoes(System.Collections.Generic.IEnumerable<VolcanoInstance> volcanoes)
    {
        // Conserve uniquement les volcans temporaires (provenant des météores)
        activeVolcanoes.RemoveAll(v => !v.isTemporary);

        if (volcanoes != null)
        {
            foreach (var vol in volcanoes)
            {
                activeVolcanoes.Add(new VolcanoStamp
                {
                    longitudeDegrees = vol.longitudeDegrees,
                    latitudeDegrees = vol.latitudeDegrees,
                    radiusDegrees = vol.currentRadiusDegrees,
                    peakHeight = vol.currentPeakHeight,
                    rate = 1f,
                    parentPiece = vol.parentPiece,
                    offsetLonFromParent = vol.offsetLonFromParent,
                    offsetLatFromParent = vol.offsetLatFromParent
                });
            }
        }

        RequestRebuild();
    }

    /// <summary>Signale qu'une reconstruction du heightmap est nécessaire (soumise au rebuildInterval).</summary>
    public void RequestRebuild()
    {
        pendingRebuild = true;
    }

    /// <summary>Ajoute un volcan temporaire en coordonnées géographiques (degrés).</summary>
    public void AddTemporaryVolcanoDegrees(float longitudeDegrees, float latitudeDegrees, float radiusDegrees, float peakHeight, float fadeSpeedVal = 0.015f)
    {
        var parent = FindParentPiece(longitudeDegrees, latitudeDegrees);
        activeVolcanoes.Add(new VolcanoStamp
        {
            longitudeDegrees = longitudeDegrees,
            latitudeDegrees = latitudeDegrees,
            radiusDegrees = radiusDegrees,
            peakHeight = peakHeight,
            rate = 1f,
            isTemporary = true,
            maxPeakHeight = peakHeight,
            currentFade = 1f,
            fadeSpeed = fadeSpeedVal,
            parentPiece = parent,
            offsetLonFromParent = parent != null ? DeltaLongitudeDegrees(longitudeDegrees, parent.currentLongitude) : 0f,
            offsetLatFromParent = parent != null ? latitudeDegrees - parent.currentLatitude : 0f
        });

        field?.AddVolcano(
            longitudeDegrees * Mathf.Deg2Rad,
            latitudeDegrees * Mathf.Deg2Rad,
            radiusDegrees * Mathf.Deg2Rad,
            peakHeight,
            1f);
    }

    /// <summary>Ajoute un cratère d'impact en coordonnées géographiques (degrés).</summary>
    public void AddCraterDegrees(float longitudeDegrees, float latitudeDegrees, float radiusDegrees, float depth, float rimHeight, float targetFadeVal = 0f, float fadeSpeedVal = 0.02f)
    {
        var parent = FindParentPiece(longitudeDegrees, latitudeDegrees);
        activeCraters.Add(new CraterStamp
        {
            name = $"Crater at ({longitudeDegrees:F1}, {latitudeDegrees:F1})",
            longitudeDegrees = longitudeDegrees,
            latitudeDegrees = latitudeDegrees,
            radiusDegrees = radiusDegrees,
            maxDepth = depth,
            maxRimHeight = rimHeight,
            currentFade = 1f,
            targetFade = targetFadeVal,
            fadeSpeed = fadeSpeedVal,
            parentPiece = parent,
            offsetLonFromParent = parent != null ? DeltaLongitudeDegrees(longitudeDegrees, parent.currentLongitude) : 0f,
            offsetLatFromParent = parent != null ? latitudeDegrees - parent.currentLatitude : 0f
        });

        field?.AddCrater(
            longitudeDegrees * Mathf.Deg2Rad,
            latitudeDegrees * Mathf.Deg2Rad,
            radiusDegrees * Mathf.Deg2Rad,
            depth,
            rimHeight);
    }

    /// <summary>Retourne la hauteur actuelle du terrain aux coordonnées de degrés données.</summary>
    public float GetHeightAtDegrees(float longitudeDegrees, float latitudeDegrees)
    {
        if (field == null) return 0f;
        float lonRad = Mathf.Repeat(longitudeDegrees, 360f) * Mathf.Deg2Rad;
        float latRad = Mathf.Clamp(latitudeDegrees, -90f, 90f) * Mathf.Deg2Rad;
        float u = Mathf.Repeat(lonRad / (2f * Mathf.PI) + 0.5f, 1f);
        int x = Mathf.RoundToInt(u * field.Width);
        int y = Mathf.RoundToInt((latRad / Mathf.PI + 0.5f) * field.Height);
        return field.GetCurrent(x, y);
    }

    /// <summary>Ajoute un continent (relief large, croissance lente) en degrés.</summary>
    public void AddContinentDegrees(float longitudeDegrees, float latitudeDegrees, float radiusDegrees, float plateauHeight, float rate = 0.1f)
    {
        field?.AddContinent(
            longitudeDegrees * Mathf.Deg2Rad,
            latitudeDegrees * Mathf.Deg2Rad,
            radiusDegrees * Mathf.Deg2Rad,
            plateauHeight,
            rate);
    }

    private void ApplyRandomization()
    {
        if (hasRandomized) return;
        hasRandomized = true;

        if (!randomizeOnStart)
        {
            activeNoiseOffset = Vector3.zero;
            return;
        }

        int activeSeed = seed != 0 ? seed : (System.Environment.TickCount ^ System.Guid.NewGuid().GetHashCode()) & 0x7FFFFFFF;
        if (activeSeed == 0) activeSeed = 1;
        System.Random prng = new System.Random(activeSeed);
        Debug.Log($"[CubeSphereTerrain] Applied Randomization with seed: {activeSeed}");

        // 1. Generate random noise offset for PlanetHeightField
        activeNoiseOffset = new Vector3(
            (float)(prng.NextDouble() * 2000.0 - 1000.0),
            (float)(prng.NextDouble() * 2000.0 - 1000.0),
            (float)(prng.NextDouble() * 2000.0 - 1000.0)
        );

        // 2. Select a single shared supercontinent center in safe equatorial/temperate zone
        float centerLon = (float)(prng.NextDouble() * 360.0);
        float centerLat = (float)((prng.NextDouble() * 60.0) - 30.0); // Safe latitude range (-30° to +30°) to avoid polar ice caps

        // Supercontinent global radius
        float supercontinentRadius = (float)(40.0 + prng.NextDouble() * 15.0); // 40° - 55°
        float supercontinentHeight = (float)(0.6 + prng.NextDouble() * 0.15); // 0.6 - 0.75

        // 3. Choose number of fractured pieces (3 to 5)
        int numPieces = prng.Next(3, 6);
        continentalPieces = new ContinentalPiece[numPieces];

        // Sector angles partitioning 2*PI radians around center
        float[] sectorAngles = new float[numPieces + 1];
        sectorAngles[0] = 0f;
        sectorAngles[numPieces] = Mathf.PI * 2f;

        // Divide 2*PI into random sectors
        float remainingAngle = Mathf.PI * 2f;
        float currentAngle = 0f;
        for (int i = 0; i < numPieces - 1; i++)
        {
            float minShare = (Mathf.PI * 2f / numPieces) * 0.5f;
            float maxShare = (remainingAngle - (numPieces - 1 - i) * minShare);
            float share = minShare + (float)(prng.NextDouble() * (maxShare - minShare));
            currentAngle += share;
            sectorAngles[i + 1] = currentAngle;
            remainingAngle -= share;
        }

        float centerLonRad = centerLon * Mathf.Deg2Rad;
        float centerLatRad = centerLat * Mathf.Deg2Rad;
        float cosCenterLat = Mathf.Max(Mathf.Cos(centerLatRad), 0.2f);
        float sinCenterLat = Mathf.Sin(centerLatRad);

        Vector3 supercenterDir = new Vector3(
            cosCenterLat * Mathf.Cos(centerLonRad),
            sinCenterLat,
            cosCenterLat * Mathf.Sin(centerLonRad)
        );

        Vector3 eastCenter = new Vector3(-Mathf.Sin(centerLonRad), 0f, Mathf.Cos(centerLonRad));
        Vector3 northCenter = Vector3.Cross(supercenterDir, eastCenter).normalized;

        for (int i = 0; i < numPieces; i++)
        {
            var piece = new ContinentalPiece();
            piece.name = $"Fragment {i + 1}";

            piece.supercontinentCenterLon = centerLon;
            piece.supercontinentCenterLat = centerLat;

            piece.sectorStartAngle = sectorAngles[i];
            piece.sectorEndAngle = sectorAngles[i + 1];

            // Sector mid angle relative to supercontinent tangent frame
            float midAngle = (piece.sectorStartAngle + piece.sectorEndAngle) * 0.5f;

            // Calculate sector centroid location on the sphere so fragments start at their natural piece centers
            // (preventing dist = 0 overlap and avoiding initial instant teleportation)
            float centroidOffsetRad = (supercontinentRadius * 0.35f) * Mathf.Deg2Rad;
            Vector3 radialDir = (Mathf.Cos(midAngle) * eastCenter + Mathf.Sin(midAngle) * northCenter).normalized;
            Vector3 centroidPos = (supercenterDir + radialDir * Mathf.Sin(centroidOffsetRad)).normalized;

            float baseLat = Mathf.Asin(Mathf.Clamp(centroidPos.y, -1f, 1f)) * Mathf.Rad2Deg;
            float baseLon = Mathf.Atan2(centroidPos.z, centroidPos.x) * Mathf.Rad2Deg;

            piece.baseLongitude = Mathf.Repeat(baseLon, 360f);
            piece.baseLatitude = Mathf.Clamp(baseLat, -45f, 45f);
            piece.radius = supercontinentRadius * 0.9f;
            piece.height = supercontinentHeight;

            // Pure outward radial drift ("explosion" behavior away from supercontinent center)
            float driftMagnitude = (float)(2.5e-5 + prng.NextDouble() * 2.5e-5); // ~ 2.5e-5 to 5.0e-5
            Vector3 piecePos = LatLonToVector3(piece.baseLongitude, piece.baseLatitude);
            float pieceLatRad = piece.baseLatitude * Mathf.Deg2Rad;
            float pieceLonRad = piece.baseLongitude * Mathf.Deg2Rad;
            float cosPieceLat = Mathf.Max(Mathf.Cos(pieceLatRad), 0.2f);

            Vector3 eastPiece = new Vector3(-Mathf.Sin(pieceLonRad), 0f, Mathf.Cos(pieceLonRad));
            Vector3 northPiece = Vector3.Cross(eastPiece, piecePos).normalized;

            // Outward vector pointing radially away from supercontinent center
            Vector3 outwardDir = (piecePos - supercenterDir * Vector3.Dot(supercenterDir, piecePos)).normalized;

            float vLon = Vector3.Dot(outwardDir, eastPiece);
            float vLat = Vector3.Dot(outwardDir, northPiece);

            float latScale = 0.18f; // Weak North-South derivation factor
            piece.driftSpeedLon = (vLon * driftMagnitude) / cosPieceLat;
            piece.driftSpeedLat = vLat * driftMagnitude * latScale;

            // Pre-bake heightmap for this sector piece
            BakePieceHeightGrid(piece);

            continentalPieces[i] = piece;
        }
    }

    private void BakePieceHeightGrid(ContinentalPiece piece)
    {
        int gridSize = 64;
        piece.localGridSize = gridSize;
        piece.localHeights = new float[gridSize * gridSize];

        float centerLonRad = piece.supercontinentCenterLon * Mathf.Deg2Rad;
        float centerLatRad = piece.supercontinentCenterLat * Mathf.Deg2Rad;
        float cosPieceLat = Mathf.Max(Mathf.Cos(piece.baseLatitude * Mathf.Deg2Rad), 0.2f);
        float cosLat = Mathf.Max(Mathf.Cos(centerLatRad), 0.2f);
        float sinLat = Mathf.Sin(centerLatRad);

        Vector3 supercenterDir = new Vector3(
            cosLat * Mathf.Cos(centerLonRad),
            sinLat,
            cosLat * Mathf.Sin(centerLonRad)
        );

        float pieceRadiusRad = piece.radius * Mathf.Deg2Rad;
        float warpStrength = pieceRadiusRad * 0.45f;

        for (int y = 0; y < gridSize; y++)
        {
            float v = (float)y / (gridSize - 1);
            float dLatDeg = (v - 0.5f) * 2f * piece.radius;
            float latDeg = piece.baseLatitude + dLatDeg;
            float latRad = latDeg * Mathf.Deg2Rad;

            float cosRowLat = Mathf.Cos(latRad);
            float sinRowLat = Mathf.Sin(latRad);

            for (int x = 0; x < gridSize; x++)
            {
                float u = (float)x / (gridSize - 1);
                float dLonDeg = (u - 0.5f) * 2f * piece.radius / cosPieceLat;
                float lonDeg = piece.baseLongitude + dLonDeg;
                float lonRad = lonDeg * Mathf.Deg2Rad;

                float xDir = cosRowLat * Mathf.Cos(lonRad);
                float yDir = sinRowLat;
                float zDir = cosRowLat * Mathf.Sin(lonRad);

                Vector3 pos = new Vector3(xDir, yDir, zDir);

                // Check distance from supercontinent center
                float cosDCenter = Vector3.Dot(pos, supercenterDir);
                float distFromCenter = Mathf.Acos(Mathf.Clamp(cosDCenter, -1f, 1f));

                // Domain warping
                float wx = PlanetHeightField.Fbm3D((pos.x + activeNoiseOffset.x + 13.5f) * 2.2f, (pos.y + activeNoiseOffset.y + 27.1f) * 2.2f, (pos.z + activeNoiseOffset.z + 41.8f) * 2.2f, 3);
                float wy = PlanetHeightField.Fbm3D((pos.x + activeNoiseOffset.x + 52.3f) * 2.2f, (pos.y + activeNoiseOffset.y + 68.9f) * 2.2f, (pos.z + activeNoiseOffset.z + 84.2f) * 2.2f, 3);
                float wz = PlanetHeightField.Fbm3D((pos.x + activeNoiseOffset.x + 91.7f) * 2.2f, (pos.y + activeNoiseOffset.y + 14.3f) * 2.2f, (pos.z + activeNoiseOffset.z + 36.6f) * 2.2f, 3);

                Vector3 warpedPos = (pos + new Vector3(wx, wy, wz) * warpStrength).normalized;

                // Sector angle check relative to supercontinent center tangent frame
                // Calculate local angle around supercenterDir
                Vector3 eastDir = new Vector3(-Mathf.Sin(centerLonRad), 0f, Mathf.Cos(centerLonRad));
                Vector3 northDir = Vector3.Cross(supercenterDir, eastDir).normalized;

                Vector3 deltaVec = pos - supercenterDir;
                float projEast = Vector3.Dot(deltaVec, eastDir);
                float projNorth = Vector3.Dot(deltaVec, northDir);

                float pointAngle = Mathf.Atan2(projNorth, projEast);
                if (pointAngle < 0f) pointAngle += Mathf.PI * 2f;

                // Sector fracture boundary check with multi-frequency noise jitter for organic fracture lines
                float fractureJitter = PlanetHeightField.Fbm3D(
                    (pos.x + activeNoiseOffset.x + 12.3f) * 4.5f,
                    (pos.y + activeNoiseOffset.y + 45.6f) * 4.5f,
                    (pos.z + activeNoiseOffset.z + 78.9f) * 4.5f, 4) * 0.35f;
                float adjustedAngle = Mathf.Repeat(pointAngle + fractureJitter, Mathf.PI * 2f);

                bool inSector = (piece.sectorStartAngle <= piece.sectorEndAngle)
                    ? (adjustedAngle >= piece.sectorStartAngle && adjustedAngle <= piece.sectorEndAngle)
                    : (adjustedAngle >= piece.sectorStartAngle || adjustedAngle <= piece.sectorEndAngle);

                if (!inSector) continue;

                // Supercontinent shape masking
                float superRadiusRad = (piece.radius / 0.9f) * Mathf.Deg2Rad;
                float coastlineNoise = PlanetHeightField.Fbm3D((pos.x + activeNoiseOffset.x) * 8.0f, (pos.y + activeNoiseOffset.y) * 8.0f, (pos.z + activeNoiseOffset.z) * 8.0f, 4);
                float perturbedRadius = superRadiusRad * (1.0f + coastlineNoise * 0.35f);

                if (distFromCenter > perturbedRadius) continue;

                float t = 1f - distFromCenter / perturbedRadius;
                float heightFactor = Mathf.SmoothStep(0f, 1f, t);

                float internalNoise = PlanetHeightField.Fbm3D((pos.x + activeNoiseOffset.x) * 12.0f, (pos.y + activeNoiseOffset.y) * 12.0f, (pos.z + activeNoiseOffset.z) * 12.0f, 4);
                float finalHeight = heightFactor * (1.0f + internalNoise * 0.25f);

                // Smooth edge falloff to avoid rectangular grid edge cutoff
                float uNorm = (u - 0.5f) * 2f;
                float vNorm = (v - 0.5f) * 2f;
                float gridDist = Mathf.Sqrt(uNorm * uNorm + vNorm * vNorm);
                float gridFade = Mathf.SmoothStep(1.0f, 0.85f, gridDist);
                finalHeight *= gridFade;

                piece.localHeights[x + y * gridSize] = finalHeight;
            }
        }
    }

    public void InitializeContinentalPieces()
    {
        if (continentalPieces == null || continentalPieces.Length == 0)
        {
            continentalPieces = new ContinentalPiece[]
            {
                new ContinentalPiece
                {
                    name = "Fragment 1",
                    baseLongitude = 160f,
                    baseLatitude = 15f,
                    radius = 35f,
                    height = 0.65f,
                    driftSpeedLon = -0.00003f,
                    driftSpeedLat = 0.000005f
                },
                new ContinentalPiece
                {
                    name = "Fragment 2",
                    baseLongitude = 200f,
                    baseLatitude = 20f,
                    radius = 30f,
                    height = 0.55f,
                    driftSpeedLon = 0.000035f,
                    driftSpeedLat = 0.000004f
                },
                new ContinentalPiece
                {
                    name = "Fragment 3",
                    baseLongitude = 150f,
                    baseLatitude = -15f,
                    radius = 38f,
                    height = 0.7f,
                    driftSpeedLon = -0.000025f,
                    driftSpeedLat = -0.000005f
                }
            };
        }

        if (Application.isPlaying)
        {
            ApplyRandomization();
        }

        ResetContinentalPieces();
    }

    public void ResetContinentalPieces()
    {
        if (continentalPieces == null) return;
        foreach (var piece in continentalPieces)
        {
            piece.currentLongitude = piece.baseLongitude;
            piece.currentLatitude = piece.baseLatitude;
            piece.currentHeight = Application.isPlaying ? 0f : piece.height;
        }
    }

    public void RebuildHeightField()
    {
        if (field == null) return;

        lastRebuildTime = Time.unscaledTime;
        pendingRebuild = false;

        field.OnCleared -= HandleFieldCleared;
        field.Clear(0f);
        field.OnCleared += HandleFieldCleared;

        // 1. Stamp continental pieces (using pre-baked fast grid if available)
        foreach (var piece in continentalPieces)
        {
            if (piece.currentHeight > 0f)
            {
                if (piece.localHeights != null && piece.localHeights.Length > 0)
                {
                    field.StampPrebakedPiece(
                        piece.currentLongitude * Mathf.Deg2Rad,
                        piece.currentLatitude * Mathf.Deg2Rad,
                        piece.radius * Mathf.Deg2Rad,
                        piece.localHeights,
                        piece.localGridSize,
                        piece.localGridSize,
                        piece.currentHeight
                    );
                }
                else
                {
                    field.AddContinent(
                        piece.currentLongitude * Mathf.Deg2Rad,
                        piece.currentLatitude * Mathf.Deg2Rad,
                        piece.radius * Mathf.Deg2Rad,
                        piece.currentHeight,
                        1.0f
                    );
                }
            }
        }

        // 2. Stamp volcanoes
        foreach (var vol in activeVolcanoes)
        {
            field.AddVolcano(
                vol.longitudeDegrees * Mathf.Deg2Rad,
                vol.latitudeDegrees * Mathf.Deg2Rad,
                vol.radiusDegrees * Mathf.Deg2Rad,
                vol.peakHeight,
                vol.rate
            );
        }

        // 3. Stamp craters
        foreach (var crater in activeCraters)
        {
            if (crater.currentFade > 0.001f)
            {
                field.AddCrater(
                    crater.longitudeDegrees * Mathf.Deg2Rad,
                    crater.latitudeDegrees * Mathf.Deg2Rad,
                    crater.radiusDegrees * Mathf.Deg2Rad,
                    crater.maxDepth * crater.currentFade,
                    crater.maxRimHeight * crater.currentFade,
                    1.0f
                );
            }
        }

        field.SnapToTarget();
    }

    private void HandleFieldCleared()
    {
        activeVolcanoes.Clear();
        activeCraters.Clear();
    }

    private void HandleSimulationStep()
    {
        if (GameManager.Instance == null) return;

        float currentSimTime = GameManager.Instance.SimulationTimeSeconds;
        float simDt = currentSimTime - lastSimTime;
        lastSimTime = currentSimTime;

        UpdateTerrainSimulation(simDt);
    }

    public void UpdateTerrainSimulation(float simDt)
    {
        if (GameManager.Instance == null) return;

        PlanetEpoch epoch = GameManager.Instance.CurrentEpoch;
        bool needsRebuild = false;

        // 1. Update active fading craters
        for (int i = activeCraters.Count - 1; i >= 0; i--)
        {
            var crater = activeCraters[i];
            if (crater.currentFade > crater.targetFade)
            {
                crater.currentFade -= crater.fadeSpeed * simDt;
                if (crater.currentFade < crater.targetFade)
                {
                    crater.currentFade = crater.targetFade;
                }
                needsRebuild = true;
            }

            // If completely faded out and target is 0, remove it
            if (crater.currentFade <= 0f && crater.targetFade <= 0f)
            {
                activeCraters.RemoveAt(i);
                needsRebuild = true;
            }
        }

        // 2. Update active temporary volcanoes
        for (int i = activeVolcanoes.Count - 1; i >= 0; i--)
        {
            var vol = activeVolcanoes[i];
            if (vol.isTemporary)
            {
                vol.currentFade -= vol.fadeSpeed * simDt;
                if (vol.currentFade <= 0f)
                {
                    activeVolcanoes.RemoveAt(i);
                    needsRebuild = true;
                }
                else
                {
                    float newPeak = vol.maxPeakHeight * vol.currentFade;
                    if (!Mathf.Approximately(vol.peakHeight, newPeak))
                    {
                        vol.peakHeight = newPeak;
                        needsRebuild = true;
                    }
                }
            }
        }

        // 3. Handle progressive crust growth during CrustFormation
        if (epoch == PlanetEpoch.CrustFormation)
        {
            float temp = GameManager.Instance.SurfaceTemperature;
            // Map 1400K -> 0, 1000K -> 1
            float progress = Mathf.Clamp01((1400f - temp) / (1400f - 1000f));

            foreach (var piece in continentalPieces)
            {
                float newHeight = piece.height * progress;
                if (!Mathf.Approximately(piece.currentHeight, newHeight))
                {
                    piece.currentHeight = newHeight;
                    needsRebuild = true;
                }
            }
        }
        else if (epoch > PlanetEpoch.CrustFormation)
        {
            // Ensure they are at full height in later epochs
            foreach (var piece in continentalPieces)
            {
                if (!Mathf.Approximately(piece.currentHeight, piece.height))
                {
                    piece.currentHeight = piece.height;
                    needsRebuild = true;
                }
            }
        }
        else
        {
            // Hadean or earlier: no crust yet
            foreach (var piece in continentalPieces)
            {
                if (piece.currentHeight > 0f)
                {
                    piece.currentHeight = 0f;
                    needsRebuild = true;
                }
            }
        }

        // 2. Handle drift during TectonicDrift and subsequent epochs (e.g., Prebiotic)
        if (epoch >= PlanetEpoch.TectonicDrift && simDt > 0f)
        {
            // Speed factor: full rush speed during TectonicDrift epoch (which spans waterRatio 0.10 to 1.00 for a 3x extended rapid drift duration before slowing down in Prebiotic epoch)
            float tectonicFactor = GameManager.Instance != null ? Mathf.Clamp01(GameManager.Instance.TectonicActivity / 0.23f) : 1f;
            float speedFactor = (epoch == PlanetEpoch.TectonicDrift) ? 1.0f : (0.20f * tectonicFactor);

            // First perform continental piece collision detection
            if (continentalPieces != null && continentalPieces.Length > 1)
            {
                for (int i = 0; i < continentalPieces.Length; i++)
                {
                    var pieceA = continentalPieces[i];
                    for (int j = i + 1; j < continentalPieces.Length; j++)
                    {
                        var pieceB = continentalPieces[j];

                        // Skip check if both pieces are already completely stopped
                        if (pieceA.driftSpeedLon == 0f && pieceA.driftSpeedLat == 0f &&
                            pieceB.driftSpeedLon == 0f && pieceB.driftSpeedLat == 0f)
                        {
                            continue;
                        }

                        float dist = AngularDistanceDegrees(pieceA.currentLongitude, pieceA.currentLatitude, pieceB.currentLongitude, pieceB.currentLatitude);
                        // Collision threshold at boundary contact (85% of radius sum to account for pie sector shapes)
                        float collisionDistance = (pieceA.radius + pieceB.radius) * 0.85f;

                        if (dist < collisionDistance)
                        {
                            Vector3 posA = LatLonToVector3(pieceA.currentLongitude, pieceA.currentLatitude);
                            Vector3 posB = LatLonToVector3(pieceB.currentLongitude, pieceB.currentLatitude);

                            Vector3 dirAtoB = (posB - posA * Vector3.Dot(posA, posB)).normalized;
                            Vector3 dirBtoA = (posA - posB * Vector3.Dot(posA, posB)).normalized;

                            float latRadA = pieceA.currentLatitude * Mathf.Deg2Rad;
                            float lonRadA = pieceA.currentLongitude * Mathf.Deg2Rad;
                            float cosLatA = Mathf.Max(Mathf.Cos(latRadA), 0.1f);

                            float latRadB = pieceB.currentLatitude * Mathf.Deg2Rad;
                            float lonRadB = pieceB.currentLongitude * Mathf.Deg2Rad;
                            float cosLatB = Mathf.Max(Mathf.Cos(latRadB), 0.1f);

                            Vector3 eastA = new Vector3(-Mathf.Sin(lonRadA), 0f, Mathf.Cos(lonRadA));
                            Vector3 northA = Vector3.Cross(eastA, posA).normalized;
                            Vector3 velA = (eastA * (pieceA.driftSpeedLon * Mathf.Deg2Rad * cosLatA) + northA * (pieceA.driftSpeedLat * Mathf.Deg2Rad)) * speedFactor;

                            Vector3 eastB = new Vector3(-Mathf.Sin(lonRadB), 0f, Mathf.Cos(lonRadB));
                            Vector3 northB = Vector3.Cross(eastB, posB).normalized;
                            Vector3 velB = (eastB * (pieceB.driftSpeedLon * Mathf.Deg2Rad * cosLatB) + northB * (pieceB.driftSpeedLat * Mathf.Deg2Rad)) * speedFactor;

                            float vNormalA = Vector3.Dot(velA, dirAtoB);
                            float vNormalB = Vector3.Dot(velB, dirBtoA);

                            // Eliminate normal convergence velocity so plates touch without interpenetration/overlap
                            if (vNormalA > 0f)
                            {
                                Vector3 velA_deflected = velA - vNormalA * dirAtoB;
                                float safeSpeedFactor = Mathf.Max(speedFactor, 1e-5f);
                                pieceA.driftSpeedLon = (Vector3.Dot(velA_deflected, eastA) / safeSpeedFactor) / (Mathf.Deg2Rad * cosLatA);
                                pieceA.driftSpeedLat = (Vector3.Dot(velA_deflected, northA) / safeSpeedFactor) / Mathf.Deg2Rad;
                                float maxLatSpeedA = Mathf.Abs(pieceA.driftSpeedLon) * 0.25f + 1e-6f;
                                pieceA.driftSpeedLat = Mathf.Clamp(pieceA.driftSpeedLat, -maxLatSpeedA, maxLatSpeedA);
                            }

                            if (vNormalB > 0f)
                            {
                                Vector3 velB_deflected = velB - vNormalB * dirBtoA;
                                float safeSpeedFactor = Mathf.Max(speedFactor, 1e-5f);
                                pieceB.driftSpeedLon = (Vector3.Dot(velB_deflected, eastB) / safeSpeedFactor) / (Mathf.Deg2Rad * cosLatB);
                                pieceB.driftSpeedLat = (Vector3.Dot(velB_deflected, northB) / safeSpeedFactor) / Mathf.Deg2Rad;
                                float maxLatSpeedB = Mathf.Abs(pieceB.driftSpeedLon) * 0.25f + 1e-6f;
                                pieceB.driftSpeedLat = Mathf.Clamp(pieceB.driftSpeedLat, -maxLatSpeedB, maxLatSpeedB);
                            }

                            // Positional correction ONLY when plates are converging towards each other (vNormal > 0),
                            // preventing artificial push teleportation during initial divergent rifting.
                            float minAllowedDist = collisionDistance * 0.95f;
                            if ((vNormalA > 0f || vNormalB > 0f) && dist < minAllowedDist && dist > 0.01f)
                            {
                                float overlapDeg = minAllowedDist - dist;
                                float maxPushDeg = 0.2f; // Max degree push per simulation step
                                float overlapRad = Mathf.Min(overlapDeg, maxPushDeg) * Mathf.Deg2Rad;

                                // Push pieceA away along dirBtoA and pieceB along dirAtoB
                                Vector3 sepA = dirBtoA * (overlapRad * 0.5f);
                                Vector3 newPosA = (posA + sepA).normalized;
                                float newLatA = Mathf.Asin(Mathf.Clamp(newPosA.y, -1f, 1f)) * Mathf.Rad2Deg;
                                float newLonA = Mathf.Atan2(newPosA.z, newPosA.x) * Mathf.Rad2Deg;
                                pieceA.currentLongitude = Mathf.Repeat(newLonA, 360f);
                                pieceA.currentLatitude = Mathf.Clamp(newLatA, -50f, 50f);

                                Vector3 sepB = dirAtoB * (overlapRad * 0.5f);
                                Vector3 newPosB = (posB + sepB).normalized;
                                float newLatB = Mathf.Asin(Mathf.Clamp(newPosB.y, -1f, 1f)) * Mathf.Rad2Deg;
                                float newLonB = Mathf.Atan2(newPosB.z, newPosB.x) * Mathf.Rad2Deg;
                                pieceB.currentLongitude = Mathf.Repeat(newLonB, 360f);
                                pieceB.currentLatitude = Mathf.Clamp(newLatB, -50f, 50f);
                            }
                        }
                    }
                }
            }

            foreach (var piece in continentalPieces)
            {
                if (piece.driftSpeedLon != 0f || piece.driftSpeedLat != 0f)
                {
                    float deltaLon = piece.driftSpeedLon * speedFactor * simDt;
                    float deltaLat = piece.driftSpeedLat * speedFactor * simDt;

                    // Smoothly damp North-South drift as piece latitude approaches +/- 50° to prevent entering polar ice caps
                    float currentAbsLat = Mathf.Abs(piece.currentLatitude);
                    float latDamping = Mathf.Clamp01((50f - currentAbsLat) / 15f);
                    deltaLat *= latDamping;

                    if (Mathf.Abs(deltaLon) > 1e-6f || Mathf.Abs(deltaLat) > 1e-6f)
                    {
                        piece.currentLongitude = Mathf.Repeat(piece.currentLongitude + deltaLon, 360f);
                        piece.currentLatitude = Mathf.Clamp(piece.currentLatitude + deltaLat, -50f, 50f);
                        needsRebuild = true;
                    }

                    // Update attached craters and volcanoes to remain strictly locked to parent piece crust
                    foreach (var crater in activeCraters)
                    {
                        if (crater.parentPiece != null)
                        {
                            crater.longitudeDegrees = Mathf.Repeat(crater.parentPiece.currentLongitude + crater.offsetLonFromParent, 360f);
                            crater.latitudeDegrees = Mathf.Clamp(crater.parentPiece.currentLatitude + crater.offsetLatFromParent, -85f, 85f);
                        }
                    }

                    foreach (var vol in activeVolcanoes)
                    {
                        if (vol.parentPiece != null)
                        {
                            vol.longitudeDegrees = Mathf.Repeat(vol.parentPiece.currentLongitude + vol.offsetLonFromParent, 360f);
                            vol.latitudeDegrees = Mathf.Clamp(vol.parentPiece.currentLatitude + vol.offsetLatFromParent, -85f, 85f);
                        }
                    }
                }
            }

            // Check collision / overlap for unattached craters & volcanoes with drifting continental pieces
            foreach (var crater in activeCraters)
            {
                if (crater.parentPiece == null)
                {
                    var hitPiece = FindParentPiece(crater.longitudeDegrees, crater.latitudeDegrees);
                    if (hitPiece != null)
                    {
                        // Amalgamation: Attach crater to colliding continent and deformation
                        crater.parentPiece = hitPiece;
                        crater.offsetLonFromParent = DeltaLongitudeDegrees(crater.longitudeDegrees, hitPiece.currentLongitude);
                        crater.offsetLatFromParent = crater.latitudeDegrees - hitPiece.currentLatitude;

                        // Earthquake & deformation: create local uplift ridge at contact point
                        field?.AddContinent(
                            crater.longitudeDegrees * Mathf.Deg2Rad,
                            crater.latitudeDegrees * Mathf.Deg2Rad,
                            Mathf.Max(0.5f, crater.radiusDegrees * 0.8f) * Mathf.Deg2Rad,
                            0.15f,
                            1.0f);

                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.LogEvent("Tectonic Collision", $"Crater amalgamated with continental piece [{hitPiece.name}]. Earthquake deformation generated.");
                        }
                    }
                }
            }

            foreach (var vol in activeVolcanoes)
            {
                if (vol.parentPiece == null)
                {
                    var hitPiece = FindParentPiece(vol.longitudeDegrees, vol.latitudeDegrees);
                    if (hitPiece != null)
                    {
                        // Amalgamation: Attach volcano to colliding continent and deformation
                        vol.parentPiece = hitPiece;
                        vol.offsetLonFromParent = DeltaLongitudeDegrees(vol.longitudeDegrees, hitPiece.currentLongitude);
                        vol.offsetLatFromParent = vol.latitudeDegrees - hitPiece.currentLatitude;

                        // Earthquake & deformation: create local uplift ridge at contact point
                        field?.AddContinent(
                            vol.longitudeDegrees * Mathf.Deg2Rad,
                            vol.latitudeDegrees * Mathf.Deg2Rad,
                            Mathf.Max(4f, vol.radiusDegrees * 0.9f) * Mathf.Deg2Rad,
                            0.20f,
                            1.0f);

                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.LogEvent("Tectonic Collision", $"Volcano amalgamated with continental piece [{hitPiece.name}]. Earthquake deformation generated.");
                        }
                    }
                }
            }
        }

        if (needsRebuild)
        {
            pendingRebuild = true;
        }

        if (pendingRebuild)
        {
            float currentTime = Time.unscaledTime;
            if (currentTime - lastRebuildTime >= rebuildInterval)
            {
                RebuildHeightField();
            }
        }
    }

    [ContextMenu("Add Random Test Volcano")]
    public void AddRandomTestVolcano()
    {
        if (field == null) return;

        AddVolcanoDegrees(
            Random.Range(0f, 360f),
            Random.Range(-70f, 70f),
            Random.Range(6f, 18f),
            Random.Range(0.2f, 0.8f));
    }

    [ContextMenu("Save Heightmap to PNG")]
    public void SaveHeightmapToPng()
    {
        if (field == null)
        {
            Debug.LogWarning("[CubeSphereTerrain] Aucun PlanetHeightField à sauvegarder.");
            return;
        }

        int width = field.Width;
        int height = field.Height;
        Texture2D pngTex = new Texture2D(width, height, TextureFormat.RGB24, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float h = field.GetCurrent(x, y);
                // On mappe [0..1] sur [0..1] en niveaux de gris
                float val = Mathf.Clamp01(h);
                pngTex.SetPixel(x, y, new Color(val, val, val));
            }
        }
        pngTex.Apply();

        byte[] bytes = pngTex.EncodeToPNG();
        if (Application.isPlaying)
        {
            Destroy(pngTex);
        }
        else
        {
            DestroyImmediate(pngTex);
        }

        string path = System.IO.Path.Combine(Application.dataPath, "Planet_Heightmap_Debug.png");
        System.IO.File.WriteAllBytes(path, bytes);
        Debug.Log($"[CubeSphereTerrain] Heightmap sauvegardée avec succès sous : {path}");
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    private void CreateMeshData()
    {
        if (mesh == null)
        {
            mesh = new Mesh { name = "CubeSphereTerrain" };
            mesh.indexFormat = IndexFormat.UInt32;
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
        else
        {
            mesh.Clear();
            mesh.indexFormat = IndexFormat.UInt32;
        }

        Vector3[] faceNormals =
        {
            Vector3.up, Vector3.down,
            Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back
        };

        int vertsPerFace = resolution * resolution;
        int trisPerFace = (resolution - 1) * (resolution - 1) * 6;

        vertices = new Vector3[vertsPerFace * 6];
        normals = new Vector3[vertsPerFace * 6];
        uvs = new Vector2[vertsPerFace * 6];
        triangles = new int[trisPerFace * 6];

        baseDirs = new Vector3[vertices.Length];

        int vOffset = 0;
        int tOffset = 0;

        for (int f = 0; f < 6; f++)
        {
            BuildFace(faceNormals[f], ref vOffset, ref tOffset);
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void BuildFace(Vector3 localUp, ref int vOffset, ref int tOffset)
    {
        Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        Vector3 axisB = Vector3.Cross(localUp, axisA);

        int faceStart = vOffset;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = faceStart + x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1f);

                Vector3 pointOnCube =
                    localUp +
                    (percent.x - 0.5f) * 2f * axisA +
                    (percent.y - 0.5f) * 2f * axisB;

                Vector3 dir = pointOnCube.normalized;

                baseDirs[i] = dir;
                vertices[i] = dir * baseRadius;
                normals[i] = dir;
                uvs[i] = DirectionToEquirectUv(dir);
            }
        }

        for (int y = 0; y < resolution - 1; y++)
        {
            for (int x = 0; x < resolution - 1; x++)
            {
                int i = faceStart + x + y * resolution;

                triangles[tOffset++] = i;
                triangles[tOffset++] = i + resolution + 1;
                triangles[tOffset++] = i + resolution;

                triangles[tOffset++] = i;
                triangles[tOffset++] = i + 1;
                triangles[tOffset++] = i + resolution + 1;
            }
        }

        vOffset += resolution * resolution;
    }

    /// <summary>
    /// UV équirectangulaire d'une direction sphérique.
    /// Note couture: u saute de 1 à 0 au méridien arrière. Ces UV ne servent qu'au
    /// displacement optionnel (_DISPLACE), où l'échantillonnage est par-vertex et le wrap
    /// Repeat rend les deux valeurs équivalentes. Pour l'ombrage, le shader recalcule l'UV
    /// par pixel depuis la direction, ce qui supprime totalement l'artefact de couture.
    /// </summary>
    public static Vector2 DirectionToEquirectUv(Vector3 dir)
    {
        float u = Mathf.Atan2(dir.z, dir.x) / (2f * Mathf.PI) + 0.5f;
        float v = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) / Mathf.PI + 0.5f;
        return new Vector2(u, v);
    }
}
