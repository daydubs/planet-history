using UnityEngine;
using System.Collections.Generic;

public class WindAndCurrentManager : MonoBehaviour
{
    public static WindAndCurrentManager Instance { get; private set; }

    [Header("Erosion & Deposition Settings")]
    [Tooltip("Intensité de l'érosion par le vent sur les terres.")]
    [SerializeField] public float windErosionRate = 0.0001f;
    [Tooltip("Intensité de l'érosion par les courants marins sur les côtes.")]
    [SerializeField] public float coastalErosionRate = 0.0002f;
    [Tooltip("Quantité de matière déposée par rapport à celle érodée (ex: 0.9 = 10% perdue).")]
    [SerializeField, Range(0f, 1f)] public float depositionRatio = 0.95f;

    [Header("Simulation Settings")]
    [Tooltip("Intervalle de temps en années simulées entre chaque mise à jour de l'érosion.")]
    [SerializeField] public float updateIntervalYears = 1000f; // Exécuté tous les 1000 ans simulés

    private float lastUpdateYear = 0f;

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
        if (GameManager.Instance != null)
        {
            lastUpdateYear = GameManager.Instance.SimulatedYears;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || CubeSphereTerrain.Instance == null) return;
        if (GameManager.Instance.IsPaused) return;

        float currentYear = GameManager.Instance.SimulatedYears;
        if (currentYear - lastUpdateYear >= updateIntervalYears)
        {
            float dt = currentYear - lastUpdateYear;
            SimulateErosionAndDeposition(dt);
            lastUpdateYear = currentYear;
        }
    }

    private void SimulateErosionAndDeposition(float dt)
    {
        var terrain = CubeSphereTerrain.Instance;
        if (terrain.ContinentalPieces == null) return;

        // We use dt to scale the erosion rate.
        float windErosionStep = windErosionRate * (dt / 1000f);
        float coastalErosionStep = coastalErosionRate * (dt / 1000f);

        bool terrainModified = false;

        foreach (var piece in terrain.ContinentalPieces)
        {
            if (piece.localHeights == null || piece.localGridSize == 0) continue;

            int gridSize = piece.localGridSize;
            float[] nextHeights = new float[piece.localHeights.Length];
            System.Array.Copy(piece.localHeights, nextHeights, piece.localHeights.Length);

            // X axis in localHeights is Longitude (columns), Y is Latitude (rows).
            for (int y = 0; y < gridSize; y++)
            {
                float v = (float)y / (gridSize - 1);
                float dLatDeg = (v - 0.5f) * 2f * piece.radius;
                float latDeg = piece.baseLatitude + dLatDeg;

                // Determine wind direction based on latitude
                // Equator (-30 to 30) -> East to West (-1 in X)
                // Mid latitudes (30 to 60, -30 to -60) -> West to East (+1 in X)
                // Poles (> 60, < -60) -> East to West (-1 in X)

                int dirX = -1; // Default East to West
                float absLat = Mathf.Abs(latDeg);
                if (absLat > 30f && absLat <= 60f)
                {
                    dirX = 1; // West to East
                }

                for (int x = 0; x < gridSize; x++)
                {
                    int index = y * gridSize + x;
                    float h = piece.localHeights[index];

                    if (h <= 0.0001f) continue; // Under water or exact base level, not eroding

                    // Check neighbors to see if it's coast
                    bool isCoast = false;
                    if (x > 0 && piece.localHeights[y * gridSize + x - 1] <= 0.0001f) isCoast = true;
                    else if (x < gridSize - 1 && piece.localHeights[y * gridSize + x + 1] <= 0.0001f) isCoast = true;
                    else if (y > 0 && piece.localHeights[(y - 1) * gridSize + x] <= 0.0001f) isCoast = true;
                    else if (y < gridSize - 1 && piece.localHeights[(y + 1) * gridSize + x] <= 0.0001f) isCoast = true;

                    // Target neighbor index for deposition
                    int targetX = x + dirX;
                    int targetY = y; // Simplified: wind mostly horizontal

                    float erodedAmount = 0f;

                    if (isCoast)
                    {
                        erodedAmount = Mathf.Min(h, coastalErosionStep);
                    }
                    else
                    {
                        // Inland wind erosion
                        erodedAmount = Mathf.Min(h, windErosionStep);
                    }

                    if (erodedAmount > 0f)
                    {
                        nextHeights[index] -= erodedAmount;

                        // Deposition
                        if (targetX >= 0 && targetX < gridSize)
                        {
                            int targetIndex = targetY * gridSize + targetX;
                            nextHeights[targetIndex] += erodedAmount * depositionRatio;
                        }
                        terrainModified = true;
                    }
                }
            }

            // Apply new heights
            System.Array.Copy(nextHeights, piece.localHeights, nextHeights.Length);
        }

        if (terrainModified)
        {
            terrain.RebuildHeightField();
        }
    }
}
