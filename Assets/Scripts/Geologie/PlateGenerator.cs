// PlateGenerator.cs - Génération SIMPLE de plaques tectoniques (SANS événements)
using UnityEngine;
using System.Collections.Generic;
using LifeStory.Core;
using LifeStory.Generation;

namespace LifeStory.Geology
{
    //public enum PlateType
    //{
    //    Continental,    // Plaque continentale → élevée
    //    Oceanic        // Plaque océanique → basse
    //}

    [System.Serializable]
    public struct TectonicPlate
    {
        public int plateID;
        public Vector3 center;              // Centre de la plaque sur la sphère
        public PlateType type;
        public float baseElevation;         // Élévation de base
        public List<Vector2Int> cells;      // Cellules de cette plaque
        public Color debugColor;            // Couleur pour debug
    }

    public class PlateGenerator : MonoBehaviour
    {
        [Header("Plate Configuration")]
        [SerializeField] private int numberOfPlates = 8;            // Nombre de plaques
        [SerializeField] private float continentalRatio = 0.4f;     // % plaques continentales

        [Header("Elevation Settings")]
        [SerializeField] private float continentalElevation = 0.3f; // Hauteur continents
        [SerializeField] private float oceanicElevation = -0.1f;    // Hauteur océans

        [Header("Generation Controls")]
        [SerializeField] private bool autoGenerateOnStart = true;
        [SerializeField] private bool regeneratePlates = false;     // Bouton Inspector

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showPlateColors = true;
        [SerializeField] private bool showPlateCenters = true;

        // Données des plaques
        private TectonicPlate[] plates;
        private int[,] plateMap;            // Quelle plaque pour chaque cellule heightmap
        private bool[,] plateBoundaries;    // Frontières entre plaques

        // Références système
        private PlanetGenerator planetGenerator;
        private int mapResolution;
        private bool isInitialized = false;

        public static PlateGenerator Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LogDebug("🗺️ Plate Generator initialisé");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (autoGenerateOnStart)
            {
                StartCoroutine(DelayedInitialization());
            }
        }

        private System.Collections.IEnumerator DelayedInitialization()
        {
            yield return new WaitForSeconds(1f);

            planetGenerator = PlanetGenerator.Instance;
            if (planetGenerator == null)
            {
                LogDebug("❌ PlanetGenerator non trouvé");
                yield break;
            }

            yield return new WaitUntil(() => planetGenerator.HeightMap != null);

            mapResolution = planetGenerator.Resolution;
            GeneratePlatesComplete();

            isInitialized = true;
            LogDebug($"✅ Génération terminée - {numberOfPlates} plaques créées");
        }

        private void Update()
        {
            // Bouton regeneration dans l'Inspector
            if (regeneratePlates)
            {
                regeneratePlates = false;
                if (isInitialized)
                {
                    GeneratePlatesComplete();
                }
            }
        }

        // === GÉNÉRATION COMPLÈTE DES PLAQUES ===
        [ContextMenu("Generate Plates")]
        public void GeneratePlatesComplete()
        {
            LogDebug("🌍 === DÉBUT GÉNÉRATION PLAQUES ===");

            // Étape 1: Créer les centres de plaques
            CreatePlateSeeds();

            // Étape 2: Assigner chaque cellule à une plaque (Voronoi)
            AssignCellsToPlates();

            // Étape 3: Calculer les frontières
            CalculatePlateBoundaries();

            // Étape 4: Appliquer les élévations
            ApplyPlateElevations();

            LogDebug("✅ === GÉNÉRATION TERMINÉE ===");
        }

        // === ÉTAPE 1: CRÉER LES CENTRES DE PLAQUES ===
        private void CreatePlateSeeds()
        {
            LogDebug($"🎯 Création de {numberOfPlates} centres de plaques...");

            plates = new TectonicPlate[numberOfPlates];

            for (int i = 0; i < numberOfPlates; i++)
            {
                // Position aléatoire sur la sphère
                Vector3 randomDirection = Random.onUnitSphere;
                Vector3 plateCenter = randomDirection * planetGenerator.PlanetRadius;

                // Type de plaque
                PlateType plateType = Random.value < continentalRatio ? PlateType.Continental : PlateType.Oceanic;

                // Élévation selon le type
                float elevation = plateType == PlateType.Continental ? continentalElevation : oceanicElevation;

                plates[i] = new TectonicPlate
                {
                    plateID = i,
                    center = plateCenter,
                    type = plateType,
                    baseElevation = elevation,
                    cells = new List<Vector2Int>(),
                    debugColor = GenerateRandomColor()
                };

                LogDebug($"  Plaque {i}: {plateType} à {plateCenter} (élévation: {elevation:F3})");
            }

            LogDebug($"✅ {numberOfPlates} centres créés");
        }

        // === ÉTAPE 2: ATTRIBUTION CELLULES (VORONOI) ===
        private void AssignCellsToPlates()
        {
            LogDebug("📍 Attribution des cellules aux plaques (Voronoi)...");

            // Initialiser les arrays
            plateMap = new int[mapResolution, mapResolution];

            // Vider les listes de cellules
            for (int i = 0; i < numberOfPlates; i++)
            {
                plates[i].cells.Clear();
            }

            // Pour chaque cellule, trouver la plaque la plus proche
            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    Vector3 cellWorldPos = MapCoordinatesToWorldPosition(new Vector2Int(x, y));
                    int closestPlateID = FindClosestPlate(cellWorldPos);

                    plateMap[x, y] = closestPlateID;
                    plates[closestPlateID].cells.Add(new Vector2Int(x, y));
                }
            }

            // Afficher statistiques
            for (int i = 0; i < numberOfPlates; i++)
            {
                float percentage = (float)plates[i].cells.Count / (mapResolution * mapResolution) * 100f;
                LogDebug($"  Plaque {i} ({plates[i].type}): {plates[i].cells.Count} cellules ({percentage:F1}%)");
            }

            LogDebug("✅ Attribution terminée");
        }

        private int FindClosestPlate(Vector3 worldPosition)
        {
            int closestPlate = 0;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < numberOfPlates; i++)
            {
                float distance = Vector3.Distance(worldPosition, plates[i].center);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlate = i;
                }
            }

            return closestPlate;
        }

        // === ÉTAPE 3: CALCULER LES FRONTIÈRES ===
        private void CalculatePlateBoundaries()
        {
            LogDebug("🔗 Calcul des frontières entre plaques...");

            plateBoundaries = new bool[mapResolution, mapResolution];
            int boundaryCount = 0;

            for (int x = 1; x < mapResolution - 1; x++)
            {
                for (int y = 1; y < mapResolution - 1; y++)
                {
                    int currentPlate = plateMap[x, y];

                    // Vérifier si un voisin appartient à une plaque différente
                    bool isBoundary = false;

                    if (plateMap[x + 1, y] != currentPlate ||
                        plateMap[x - 1, y] != currentPlate ||
                        plateMap[x, y + 1] != currentPlate ||
                        plateMap[x, y - 1] != currentPlate)
                    {
                        isBoundary = true;
                        boundaryCount++;
                    }

                    plateBoundaries[x, y] = isBoundary;
                }
            }

            LogDebug($"✅ {boundaryCount} cellules de frontière calculées");
        }

        // === ÉTAPE 4: APPLIQUER LES ÉLÉVATIONS ===
        private void ApplyPlateElevations()
        {
            LogDebug("🏔️ Application des élévations de plaques...");

            var heightMap = planetGenerator.HeightMap;
            float totalChange = 0f;
            int cellsChanged = 0;

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    int plateID = plateMap[x, y];
                    float plateElevation = plates[plateID].baseElevation;

                    heightMap[x, y] += plateElevation;
                    totalChange += Mathf.Abs(plateElevation);
                    cellsChanged++;
                }
            }

            // Mettre à jour le mesh de la planète
            UpdatePlanetMesh();

            LogDebug($"✅ {cellsChanged} cellules modifiées, changement total: {totalChange:F4}");
        }

        private void UpdatePlanetMesh()
        {
            if (planetGenerator != null)
            {
                planetGenerator.MarkVolcanicModificationsPresent();
                planetGenerator.UpdatePlanetMesh();
                LogDebug("🔄 Mesh planète mis à jour");
            }
        }

        // === MÉTHODES UTILITAIRES ===
        private Vector3 MapCoordinatesToWorldPosition(Vector2Int mapCoords)
        {
            // Convertir coordonnées heightmap vers position 3D sur sphère
            float u = (float)mapCoords.x / (mapResolution - 1);
            float v = (float)mapCoords.y / (mapResolution - 1);

            float longitude = u * 2 * Mathf.PI - Mathf.PI;
            float latitude = v * Mathf.PI - Mathf.PI / 2;

            float x = Mathf.Cos(latitude) * Mathf.Cos(longitude);
            float y = Mathf.Sin(latitude);
            float z = Mathf.Cos(latitude) * Mathf.Sin(longitude);

            return new Vector3(x, y, z) * planetGenerator.PlanetRadius;
        }

        private Color GenerateRandomColor()
        {
            return new Color(
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f),
                0.8f
            );
        }

        // === MÉTHODES DE TEST ===
        [ContextMenu("Test Plate Distribution")]
        public void TestPlateDistribution()
        {
            if (!isInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            LogDebug("📊 TEST DISTRIBUTION DES PLAQUES:");

            int continentalCount = 0;
            int oceanicCount = 0;
            int totalCells = 0;

            for (int i = 0; i < numberOfPlates; i++)
            {
                if (plates[i].type == PlateType.Continental)
                    continentalCount++;
                else
                    oceanicCount++;

                totalCells += plates[i].cells.Count;

                LogDebug($"  Plaque {i}: {plates[i].type}, {plates[i].cells.Count} cellules");
            }

            float continentalPercent = (float)continentalCount / numberOfPlates * 100f;
            LogDebug($"Répartition: {continentalCount} continentales ({continentalPercent:F1}%), {oceanicCount} océaniques");
            LogDebug($"Total cellules: {totalCells} / {mapResolution * mapResolution}");
        }

        [ContextMenu("Show Plate Centers")]
        public void ShowPlateCenters()
        {
            if (!isInitialized) return;

            LogDebug("📍 CENTRES DES PLAQUES:");
            for (int i = 0; i < numberOfPlates; i++)
            {
                LogDebug($"  Plaque {i}: {plates[i].center} ({plates[i].type})");
            }
        }

        // === GETTERS PUBLICS ===
        public TectonicPlate[] Plates => plates;
        public int[,] PlateMap => plateMap;
        public bool[,] PlateBoundaries => plateBoundaries;
        public int NumberOfPlates => numberOfPlates;
        public bool IsInitialized => isInitialized;

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[PlateGenerator] {message}");
            }
        }

        // === GIZMOS POUR VISUALISATION ===
        private void OnDrawGizmos()
        {
            if (!showPlateCenters || !isInitialized || plates == null) return;

            // Dessiner les centres des plaques
            for (int i = 0; i < numberOfPlates; i++)
            {
                Gizmos.color = plates[i].debugColor;
                Gizmos.DrawSphere(plates[i].center, 0.3f);

                // Étiquette
                if (showPlateColors)
                {
                    Gizmos.color = plates[i].type == PlateType.Continental ? Color.green : Color.blue;
                    Gizmos.DrawWireSphere(plates[i].center, 0.5f);
                }
            }
        }

        // === GUI DEBUG ===
        private void OnGUI()
        {
            if (!enableDebugLogs) return;

            GUI.Box(new Rect(10, 1150, 350, 80), "");
            GUI.Label(new Rect(20, 1165, 330, 20), "=== PLATE GENERATOR ===");

            if (isInitialized)
            {
                GUI.Label(new Rect(20, 1185, 330, 20), $"Plaques: {numberOfPlates} | Résolution: {mapResolution}x{mapResolution}");

                int continental = 0;
                for (int i = 0; i < numberOfPlates; i++)
                    if (plates[i].type == PlateType.Continental) continental++;

                GUI.Label(new Rect(20, 1205, 330, 20), $"Continentales: {continental} | Océaniques: {numberOfPlates - continental}");
            }
            else
            {
                GUI.Label(new Rect(20, 1185, 330, 20), "❌ Système non initialisé");
            }

            if (GUI.Button(new Rect(370, 1150, 100, 25), "Regénérer"))
            {
                GeneratePlatesComplete();
            }
        }
    }
}