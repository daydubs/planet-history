// ContinentalPlateIdentifier.cs - Système d'identification et marquage des plaques continentales
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using LifeStory.Core;
using LifeStory.Generation;
using LifeStory.Geology;
using LifeStory.Tectonics;

namespace LifeStory.Geology
{
    public enum PlateType
    {
        Continental,    // Plaque continentale pure
        Oceanic,       // Plaque océanique pure  
        Mixed,         // Plaque mixte (continent + océan)
        Undefined      // Non encore analysée
    }

    [System.Serializable]
    public struct IdentifiedPlate
    {
        public int plateID;
        public PlateType type;
        public List<Vector2Int> cells;
        public Vector2Int centroid;
        public float area;
        public float continentalRatio; // 0=100% océan, 1=100% continent
        public Color debugColor;       // Couleur pour visualisation
        public bool isValid;           // Plaque valide (taille suffisante)

        // Statistiques géologiques
        public int continentalCells;
        public int oceanicCells;
        public float averageElevation;
        public List<Vector2Int> coastalCells; // Cellules côtières
    }

    public class ContinentalPlateIdentifier : MonoBehaviour
    {
        [Header("Identification Configuration")]
        [SerializeField] private bool enablePlateIdentification = true;
        [SerializeField] private float identificationDelay = 3f; // Délai après fin des rifts
        [SerializeField] private bool autoIdentifyAfterSeparation = true;

        [Header("Plate Analysis Settings")]
        [SerializeField] private int minimumPlateSize = 500; // Taille minimum en cellules
        [SerializeField] private float continentalThreshold = 0.7f; // 70% continent = plaque continentale
        [SerializeField] private float oceanicThreshold = 0.3f; // 30% continent = plaque océanique

        [Header("Coastal Detection")]
        [SerializeField] private bool identifyCoastalCells = true;
        [SerializeField] private int coastalSearchRadius = 3;

        [Header("Visualization")]
        [SerializeField] private bool enableVisualization = true;
        [SerializeField] private bool showPlateIDs = true;
        [SerializeField] private bool showPlateColors = false;
        [SerializeField] private float visualizationOpacity = 0.6f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool logDetailedAnalysis = false;

        // Données du système
        private int[,] plateMap; // ID de plaque pour chaque cellule (-1 = non assigné)
        private List<IdentifiedPlate> identifiedPlates = new List<IdentifiedPlate>();
        private bool systemInitialized = false;
        private bool identificationCompleted = false;

        // Références système
        private PlanetGenerator planetGenerator;
        private SimpleTwoPlateGenerator twoPlateGenerator;
        private ContinentalSeparationSystem separationSystem;
        private int mapResolution;

        // Couleurs de debug prédéfinies
        private Color[] debugColors = {
            Color.red, Color.green, Color.blue, Color.yellow,
            Color.magenta, Color.cyan, new Color(1f, 0.5f, 0f), // Orange
            new Color(0.5f, 0f, 1f), // Violet
            new Color(0f, 1f, 0.5f), // Vert-cyan
            new Color(1f, 0f, 0.5f)  // Rose
        };

        public static ContinentalPlateIdentifier Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LogDebug("🗺️ Continental Plate Identifier initialisé");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            StartCoroutine(DelayedInitialization());
        }

        private System.Collections.IEnumerator DelayedInitialization()
        {
            yield return new WaitForSeconds(2f);

            // Trouver les références
            planetGenerator = PlanetGenerator.Instance;
            twoPlateGenerator = SimpleTwoPlateGenerator.Instance;
            separationSystem = ContinentalSeparationSystem.Instance;

            if (planetGenerator == null || twoPlateGenerator == null)
            {
                LogDebug("❌ Systèmes requis manquants");
                yield break;
            }

            // Attendre que les systèmes soient prêts
            yield return new WaitUntil(() => planetGenerator.HeightMap != null);
            yield return new WaitUntil(() => twoPlateGenerator.IsInitialized);

            mapResolution = planetGenerator.Resolution;
            plateMap = new int[mapResolution, mapResolution];

            // Initialiser la carte des plaques
            InitializePlateMap();

            systemInitialized = true;
            LogDebug("✅ Continental Plate Identifier prêt");

            // Auto-identification si séparation déjà terminée
            if (autoIdentifyAfterSeparation && separationSystem != null)
            {
                yield return StartCoroutine(WaitForSeparationCompletion());
            }
        }

        private void InitializePlateMap()
        {
            // ✅ UTILISER les données existantes de SimpleTwoPlateGenerator
            var existingPlateMap = twoPlateGenerator.PlateMap;

            if (existingPlateMap != null)
            {
                // Copier la carte existante
                for (int x = 0; x < mapResolution; x++)
                {
                    for (int y = 0; y < mapResolution; y++)
                    {
                        plateMap[x, y] = existingPlateMap[x, y];
                    }
                }
                LogDebug("📋 Carte des plaques importée depuis SimpleTwoPlateGenerator");
            }
            else
            {
                // Fallback : initialiser comme non-assigné
                for (int x = 0; x < mapResolution; x++)
                {
                    for (int y = 0; y < mapResolution; y++)
                    {
                        plateMap[x, y] = -1;
                    }
                }
                LogDebug("⚠️ Carte des plaques initialisée par défaut");
            }
        }

        private System.Collections.IEnumerator WaitForSeparationCompletion()
        {
            if (separationSystem == null) yield break;

            LogDebug("⏳ Attente fin de la séparation continentale...");

            // Attendre que la séparation soit active
            yield return new WaitUntil(() => separationSystem.IsSeparationActive);

            // Attendre que tous les rifts atteignent leur profondeur cible
            bool separationComplete = false;
            while (!separationComplete)
            {
                yield return new WaitForSeconds(1f);

                var rifts = separationSystem.GetSeparationRifts();
                separationComplete = rifts.All(rift =>
                    Mathf.Abs(rift.currentDepth - rift.targetDepth) < 0.05f);
            }

            // Délai supplémentaire pour stabilisation
            yield return new WaitForSeconds(identificationDelay);

            LogDebug("🎯 Séparation terminée - Début identification des plaques");
            IdentifyPlates();
        }

        [ContextMenu("🗺️ Identify Plates Now")]
        public void IdentifyPlates()
        {
            if (!systemInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            LogDebug("🗺️ === DÉBUT SUBDIVISION PLAQUES CONTINENTALES ===");

            // 1. Réinitialiser les données identifiées (garder les données océaniques)
            identifiedPlates.Clear();

            // 2. Ajouter la plaque océanique existante (ID=0)
            AddOceanicPlate();

            // 3. Subdiviser seulement les cellules continentales (ID=1) séparées par les rifts
            int newPlateID = 2; // Commencer après océan(0) et ancien supercontinent(1)

            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    // Chercher les cellules continentales non encore réassignées ET non dans un rift
                    if (plateMap[x, y] == 1 && IsContinentalCellAccessible(x, y))
                    {
                        var plate = FloodFillContinentalMass(x, y, newPlateID);
                        if (plate.cells.Count >= minimumPlateSize)
                        {
                            identifiedPlates.Add(plate);
                            newPlateID++;
                        }
                        else
                        {
                            // Masse trop petite - garder comme partie de l'océan
                            MarkCellsAsOceanic(plate.cells);
                        }
                    }
                }
            }

            // 4. Analyser chaque nouvelle plaque continentale
            AnalyzePlates();

            // 5. Identifier les cellules côtières
            if (identifyCoastalCells)
            {
                IdentifyCoastalCells();
            }

            identificationCompleted = true;
            LogDebug($"✅ Subdivision terminée: {identifiedPlates.Count} plaques totales (1 océan + {identifiedPlates.Count - 1} continents)");

            ShowIdentificationResults();
        }

        private void AddOceanicPlate()
        {
            // Récupérer la plaque océanique existante de SimpleTwoPlateGenerator
            var oceanicPlate = new IdentifiedPlate
            {
                plateID = 0, // OCEANIC_PLATE_ID
                type = PlateType.Oceanic,
                cells = new List<Vector2Int>(),
                coastalCells = new List<Vector2Int>(),
                debugColor = Color.blue,
                isValid = true,
                continentalRatio = 0f
            };

            // Collecter toutes les cellules océaniques
            for (int x = 0; x < mapResolution; x++)
            {
                for (int y = 0; y < mapResolution; y++)
                {
                    if (plateMap[x, y] == 0) // OCEANIC_PLATE_ID
                    {
                        oceanicPlate.cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            oceanicPlate.oceanicCells = oceanicPlate.cells.Count;
            oceanicPlate.area = oceanicPlate.cells.Count;

            if (oceanicPlate.cells.Count > 0)
            {
                CalculatePlateCentroid(ref oceanicPlate);
                identifiedPlates.Add(oceanicPlate);
                LogDebug($"🌊 Plaque océanique: {oceanicPlate.cells.Count} cellules");
            }
        }

        private bool IsContinentalCellAccessible(int x, int y)
        {
            // Vérifier si cette cellule continentale n'est pas coupée par les rifts
            if (!twoPlateGenerator.IsContinentalCell(x, y)) return false;

            // ✅ NOUVEAU : Vérifier si la cellule fait partie d'un rift
            return !IsCellInRiftZone(x, y);
        }

        private bool IsCellInRiftZone(int x, int y)
        {
            if (separationSystem == null) return false;

            var rifts = separationSystem.GetSeparationRifts();
            foreach (var rift in rifts)
            {
                // ✅ OPTIMISATION : Vérification rapide des limites
                float halfWidth = rift.width * 0.5f;

                // Approximation rapide - vérifier seulement quelques points clés du rift
                int stepSize = Mathf.Max(1, rift.points.Count / 20); // Max 20 vérifications par rift

                for (int i = 0; i < rift.points.Count; i += stepSize)
                {
                    var riftPoint = rift.points[i];
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(riftPoint.x, riftPoint.y));
                    if (distance <= halfWidth)
                    {
                        return true; // Dans un rift
                    }
                }
            }
            return false;
        }

        private IdentifiedPlate FloodFillContinentalMass(int startX, int startY, int newPlateID)
        {
            var plate = new IdentifiedPlate
            {
                plateID = newPlateID,
                cells = new List<Vector2Int>(),
                coastalCells = new List<Vector2Int>(),
                debugColor = debugColors[newPlateID % debugColors.Length],
                isValid = false,
                type = PlateType.Continental
            };

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startX, startY));
            plateMap[startX, startY] = newPlateID;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                plate.cells.Add(current);

                // Vérifier les cellules adjacentes (4-connectivité)
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (Mathf.Abs(dx) + Mathf.Abs(dy) > 1) continue; // 4-connectivité

                        int nx = current.x + dx;
                        int ny = current.y + dy;

                        if (IsValidCoordinate(nx, ny) &&
                            plateMap[nx, ny] == 1 && // Encore marqué comme ancien supercontinent
                            IsContinentalCellAccessible(nx, ny)) // ✅ Vérifier rifts
                        {
                            plateMap[nx, ny] = newPlateID;
                            queue.Enqueue(new Vector2Int(nx, ny));
                        }
                    }
                }
            }

            return plate;
        }

        private void MarkCellsAsOceanic(List<Vector2Int> cells)
        {
            foreach (var cell in cells)
            {
                plateMap[cell.x, cell.y] = 0; // Marquer comme océan
            }
        }

        private void AnalyzePlates()
        {
            LogDebug("🔍 Analyse détaillée des plaques...");

            for (int i = 0; i < identifiedPlates.Count; i++)
            {
                var plate = identifiedPlates[i];

                // Calculer les statistiques
                AnalyzePlateComposition(ref plate);
                CalculatePlateCentroid(ref plate);
                DeterminePlateType(ref plate);

                plate.isValid = plate.cells.Count >= minimumPlateSize;
                plate.area = plate.cells.Count; // En cellules pour le moment

                identifiedPlates[i] = plate;

                if (logDetailedAnalysis)
                {
                    LogDebug($"📊 Plaque {plate.plateID}: {plate.cells.Count} cellules, " +
                            $"Type: {plate.type}, Ratio continent: {plate.continentalRatio:P0}");
                }
            }
        }

        private void AnalyzePlateComposition(ref IdentifiedPlate plate)
        {
            plate.continentalCells = 0;
            plate.oceanicCells = 0;
            float totalElevation = 0f;

            var heightMap = planetGenerator.HeightMap;

            foreach (var cell in plate.cells)
            {
                if (twoPlateGenerator.IsContinentalCell(cell.x, cell.y))
                {
                    plate.continentalCells++;
                }
                else
                {
                    plate.oceanicCells++;
                }

                totalElevation += heightMap[cell.x, cell.y];
            }

            plate.continentalRatio = (float)plate.continentalCells / plate.cells.Count;
            plate.averageElevation = totalElevation / plate.cells.Count;
        }

        private void CalculatePlateCentroid(ref IdentifiedPlate plate)
        {
            if (plate.cells.Count == 0) return;

            float sumX = 0f, sumY = 0f;
            foreach (var cell in plate.cells)
            {
                sumX += cell.x;
                sumY += cell.y;
            }

            plate.centroid = new Vector2Int(
                Mathf.RoundToInt(sumX / plate.cells.Count),
                Mathf.RoundToInt(sumY / plate.cells.Count)
            );
        }

        private void DeterminePlateType(ref IdentifiedPlate plate)
        {
            if (plate.continentalRatio >= continentalThreshold)
            {
                plate.type = PlateType.Continental;
            }
            else if (plate.continentalRatio <= oceanicThreshold)
            {
                plate.type = PlateType.Oceanic;
            }
            else
            {
                plate.type = PlateType.Mixed;
            }
        }

        private void IdentifyCoastalCells()
        {
            LogDebug("🏖️ Identification des cellules côtières...");

            for (int i = 0; i < identifiedPlates.Count; i++)
            {
                var plate = identifiedPlates[i];
                plate.coastalCells.Clear();

                foreach (var cell in plate.cells)
                {
                    if (IsCoastalCell(cell))
                    {
                        plate.coastalCells.Add(cell);
                    }
                }

                identifiedPlates[i] = plate;
            }
        }

        private bool IsCoastalCell(Vector2Int cell)
        {
            // Vérifier si une cellule continentale a de l'océan dans son voisinage
            if (!twoPlateGenerator.IsContinentalCell(cell.x, cell.y)) return false;

            for (int dx = -coastalSearchRadius; dx <= coastalSearchRadius; dx++)
            {
                for (int dy = -coastalSearchRadius; dy <= coastalSearchRadius; dy++)
                {
                    int nx = cell.x + dx;
                    int ny = cell.y + dy;

                    if (IsValidCoordinate(nx, ny) &&
                        !twoPlateGenerator.IsContinentalCell(nx, ny))
                    {
                        return true; // Océan trouvé dans le voisinage
                    }
                }
            }

            return false;
        }

        private void ShowIdentificationResults()
        {
            LogDebug("📊 === RÉSULTATS IDENTIFICATION PLAQUES ===");
            LogDebug($"   Plaques continentales détectées: {identifiedPlates.Count}");

            foreach (var plate in identifiedPlates)
            {
                LogDebug($"   🗺️ Plaque {plate.plateID}: {plate.type}");
                LogDebug($"      Cellules: {plate.cells.Count} | Centroïde: {plate.centroid}");
                LogDebug($"      Ratio continental: {plate.continentalRatio:P0}");
                LogDebug($"      Élévation moyenne: {plate.averageElevation:F3}");
                if (identifyCoastalCells)
                {
                    LogDebug($"      Cellules côtières: {plate.coastalCells.Count}");
                }
            }
        }

        private bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < mapResolution && y >= 0 && y < mapResolution;
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[PlateIdentifier] {message}");
            }
        }

        // === MÉTHODES PUBLIQUES ===
        public List<IdentifiedPlate> GetIdentifiedPlates() => new List<IdentifiedPlate>(identifiedPlates);
        public int[,] GetPlateMap() => plateMap;
        public bool IsIdentificationCompleted => identificationCompleted;
        public int PlateCount => identifiedPlates.Count;

        public IdentifiedPlate? GetPlateAtPosition(int x, int y)
        {
            if (!IsValidCoordinate(x, y) || plateMap[x, y] < 0) return null;

            int plateID = plateMap[x, y];
            return identifiedPlates.FirstOrDefault(p => p.plateID == plateID);
        }

        public IdentifiedPlate? GetPlateByID(int plateID)
        {
            return identifiedPlates.FirstOrDefault(p => p.plateID == plateID);
        }

        // === MÉTHODES DE DEBUG ===
        [ContextMenu("📊 Show Plate Statistics")]
        public void ShowPlateStatistics()
        {
            if (!identificationCompleted)
            {
                LogDebug("❌ Identification non terminée");
                return;
            }

            ShowIdentificationResults();

            // Statistiques globales
            int totalContinentalCells = identifiedPlates.Sum(p => p.continentalCells);
            int totalOceanicCells = identifiedPlates.Sum(p => p.oceanicCells);

            LogDebug("📈 === STATISTIQUES GLOBALES ===");
            LogDebug($"   Total cellules continentales: {totalContinentalCells}");
            LogDebug($"   Total cellules océaniques: {totalOceanicCells}");
            LogDebug($"   Plaque la plus grande: {identifiedPlates.Max(p => p.cells.Count)} cellules");
            LogDebug($"   Plaque la plus petite: {identifiedPlates.Min(p => p.cells.Count)} cellules");
        }

        [ContextMenu("🎨 Toggle Visualization")]
        public void ToggleVisualization()
        {
            enableVisualization = !enableVisualization;
            LogDebug($"Visualisation: {(enableVisualization ? "ON" : "OFF")}");
        }

        [ContextMenu("🧹 Clear Identification")]
        public void ClearIdentification()
        {
            identifiedPlates.Clear();
            InitializePlateMap();
            identificationCompleted = false;
            LogDebug("🧹 Identification effacée");
        }

        // === VISUALISATION DEBUG ===
        private void OnGUI()
        {
            if (!enableVisualization || !identificationCompleted) return;

            GUI.Box(new Rect(10, 300, 300, 120), "");
            GUI.Label(new Rect(20, 315, 280, 20), "=== PLAQUES CONTINENTALES ===");
            GUI.Label(new Rect(20, 335, 280, 20), $"Plaques détectées: {identifiedPlates.Count}");

            int yOffset = 355;
            for (int i = 0; i < Mathf.Min(3, identifiedPlates.Count); i++)
            {
                var plate = identifiedPlates[i];
                GUI.color = plate.debugColor;
                GUI.Label(new Rect(20, yOffset, 280, 20),
                    $"P{plate.plateID}: {plate.type} ({plate.cells.Count} cellules)");
                yOffset += 20;
            }
            GUI.color = Color.white;

            if (identifiedPlates.Count > 3)
            {
                GUI.Label(new Rect(20, yOffset, 280, 20), $"... et {identifiedPlates.Count - 3} autres");
            }
        }
    }
}