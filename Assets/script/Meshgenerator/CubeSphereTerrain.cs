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

    private System.Collections.Generic.List<VolcanoStamp> activeVolcanoes = new System.Collections.Generic.List<VolcanoStamp>();
    private System.Collections.Generic.List<CraterStamp> activeCraters = new System.Collections.Generic.List<CraterStamp>();
    private float lastSimTime;
    private Vector3 activeNoiseOffset = Vector3.zero;
    private bool hasRandomized = false;

    private Mesh mesh;
    private PlanetHeightField field;
    private MeshRenderer meshRenderer;

    private Vector3[] baseDirs; // direction sphérique de chaque vertex
    private Vector3[] vertices;
    private Vector3[] normals;
    private Vector2[] uvs;
    private int[] triangles;

    public PlanetHeightField Field => field;
    public float BaseRadius => baseRadius;
    public float HeightScale => heightScale;

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
            Material material = meshRenderer.material;
            if (material != null)
            {
                if (material.HasProperty(SurfaceTemperatureId))
                {
                    material.SetFloat(SurfaceTemperatureId, GameManager.Instance.SurfaceTemperature);
                }
                if (material.HasProperty(WaterRatioId))
                {
                    material.SetFloat(WaterRatioId, GameManager.Instance.WaterRatio);
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

        Material material = Application.isPlaying ? meshRenderer.material : meshRenderer.sharedMaterial;
        if (material == null) return;

        material.SetTexture(HeightTexId, field.HeightTex);

        if (material.HasProperty(DisplaceScaleId))
        {
            material.SetFloat(DisplaceScaleId, heightScale);
        }
    }

    /// <summary>Ajoute un volcan en coordonnées géographiques (degrés).</summary>
    public void AddVolcanoDegrees(float longitudeDegrees, float latitudeDegrees, float radiusDegrees, float peakHeight, float rate = 1f)
    {
        activeVolcanoes.Add(new VolcanoStamp
        {
            longitudeDegrees = longitudeDegrees,
            latitudeDegrees = latitudeDegrees,
            radiusDegrees = radiusDegrees,
            peakHeight = peakHeight,
            rate = rate
        });

        field?.AddVolcano(
            longitudeDegrees * Mathf.Deg2Rad,
            latitudeDegrees * Mathf.Deg2Rad,
            radiusDegrees * Mathf.Deg2Rad,
            peakHeight,
            rate);
    }

    /// <summary>Ajoute un volcan temporaire en coordonnées géographiques (degrés).</summary>
    public void AddTemporaryVolcanoDegrees(float longitudeDegrees, float latitudeDegrees, float radiusDegrees, float peakHeight, float fadeSpeedVal = 0.015f)
    {
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
            fadeSpeed = fadeSpeedVal
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
            fadeSpeed = fadeSpeedVal
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
        int x = Mathf.RoundToInt(lonRad / (2f * Mathf.PI) * field.Width);
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

        int activeSeed = seed != 0 ? seed : Random.Range(1, 999999);
        System.Random prng = new System.Random(activeSeed);
        Debug.Log($"[CubeSphereTerrain] Applied Randomization with seed: {activeSeed}");

        // 1. Generate random noise offset for PlanetHeightField
        activeNoiseOffset = new Vector3(
            (float)(prng.NextDouble() * 2000.0 - 1000.0),
            (float)(prng.NextDouble() * 2000.0 - 1000.0),
            (float)(prng.NextDouble() * 2000.0 - 1000.0)
        );

        // 2. Choose number of pieces between 1 and 5 to represent the fracturing supercontinent
        int numPieces = prng.Next(1, 6);
        continentalPieces = new ContinentalPiece[numPieces];

        // 3. Select a single shared supercontinent center
        float centerLon = (float)(prng.NextDouble() * 360.0);
        float centerLat = (float)((prng.NextDouble() * 80.0) - 40.0); // Safe latitude range to avoid polar start

        for (int i = 0; i < numPieces; i++)
        {
            var piece = new ContinentalPiece();
            piece.name = $"Continent Segment {i + 1}";

            // Place them closely around the shared center so they overlap to form a single supercontinent
            float offsetLon = (numPieces == 1) ? 0f : (float)((prng.NextDouble() * 30.0) - 15.0);
            float offsetLat = (numPieces == 1) ? 0f : (float)((prng.NextDouble() * 20.0) - 10.0);

            piece.baseLongitude = Mathf.Repeat(centerLon + offsetLon, 360f);
            piece.baseLatitude = Mathf.Clamp(centerLat + offsetLat, -60f, 60f);

            // Radius: single continent is larger, multiple are medium-sized
            float baseRadiusVal = (numPieces == 1) ? 40f : 25f;
            float radiusScale = (float)(0.8 + prng.NextDouble() * 0.4); // 0.8 to 1.2
            piece.radius = baseRadiusVal * radiusScale;

            // Height
            float heightScale = (float)(0.85 + prng.NextDouble() * 0.3); // 0.85 to 1.15
            piece.height = Mathf.Clamp01(0.6f * heightScale);

            // Separate drift speeds with larger East-West speed to simulate fracturing
            float randomSignLon = prng.Next(0, 2) == 0 ? -1f : 1f;
            float randomSignLat = prng.Next(0, 2) == 0 ? -1f : 1f;

            piece.driftSpeedLon = randomSignLon * (float)(2.0e-5 + prng.NextDouble() * 3.0e-5);
            piece.driftSpeedLat = randomSignLat * (float)(0.2e-5 + prng.NextDouble() * 1.3e-5);

            continentalPieces[i] = piece;
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
                    name = "Laurasia West",
                    baseLongitude = 160f,
                    baseLatitude = 15f,
                    radius = 35f,
                    height = 0.65f,
                    driftSpeedLon = -0.00003f,
                    driftSpeedLat = 0.000015f
                },
                new ContinentalPiece
                {
                    name = "Laurasia East",
                    baseLongitude = 200f,
                    baseLatitude = 20f,
                    radius = 30f,
                    height = 0.55f,
                    driftSpeedLon = 0.000035f,
                    driftSpeedLat = 0.00001f
                },
                new ContinentalPiece
                {
                    name = "Gondwana West",
                    baseLongitude = 150f,
                    baseLatitude = -15f,
                    radius = 38f,
                    height = 0.7f,
                    driftSpeedLon = -0.000025f,
                    driftSpeedLat = -0.00002f
                },
                new ContinentalPiece
                {
                    name = "Gondwana East",
                    baseLongitude = 210f,
                    baseLatitude = -10f,
                    radius = 32f,
                    height = 0.6f,
                    driftSpeedLon = 0.00004f,
                    driftSpeedLat = -0.000015f
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

        field.OnCleared -= HandleFieldCleared;
        field.Clear(0f);
        field.OnCleared += HandleFieldCleared;

        // 1. Stamp continental pieces
        foreach (var piece in continentalPieces)
        {
            if (piece.currentHeight > 0f)
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

        // 2. Handle drift during TectonicDrift
        if (epoch == PlanetEpoch.TectonicDrift && simDt > 0f)
        {
            foreach (var piece in continentalPieces)
            {
                if (piece.driftSpeedLon != 0f || piece.driftSpeedLat != 0f)
                {
                    piece.currentLongitude += piece.driftSpeedLon * simDt;
                    piece.currentLatitude += piece.driftSpeedLat * simDt;

                    piece.currentLongitude = Mathf.Repeat(piece.currentLongitude, 360f);
                    piece.currentLatitude = Mathf.Clamp(piece.currentLatitude, -85f, 85f);
                    needsRebuild = true;
                }
            }
        }

        if (needsRebuild)
        {
            RebuildHeightField();
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
