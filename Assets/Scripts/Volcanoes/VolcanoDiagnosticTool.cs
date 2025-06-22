//// VolcanoDiagnosticTool.cs - Diagnostic précis du problème d'alignement
//using UnityEngine;
//using LifeStory.Volcanoes;
//using LifeStory.Terrain;

//namespace LifeStory.Debugging
//{
//    /// <summary>
//    /// Outil de diagnostic pour identifier précisément si le problème vient :
//    /// A) Du volcan qui se déplace après création
//    /// B) De la déformation mal placée dès le départ
//    /// </summary>
//    public class VolcanoDiagnosticTool : MonoBehaviour
//    {
//        [Header("Configuration")]
//        [SerializeField] private bool enableContinuousTracking = true;
//        [SerializeField] private float trackingInterval = 0.5f;
//        [SerializeField] private bool createVisualMarkers = true;

//        [Header("Debug")]
//        [SerializeField] private bool enableDebugLogs = true;

//        // Références
//        private CleanVolcanicSystem volcanicSystem;
//        private TerrainModificationManager terrainManager;

//        // Données de tracking
//        private class VolcanoTrackingData
//        {
//            public GameObject volcanoMesh;
//            public Vector3 initialPosition;
//            public Vector3 currentPosition;
//            public Vector2Int expectedHeightMapCoords;
//            public Vector2Int actualDeformationCenter;
//            public GameObject positionMarker;
//            public GameObject deformationMarker;
//            public float trackingStartTime;
//        }

//        private VolcanoTrackingData currentTracking;

//        private void Start()
//        {
//            volcanicSystem = CleanVolcanicSystem.Instance;
//            terrainManager = TerrainModificationManager.Instance;

//            if (volcanicSystem == null || terrainManager == null)
//            {
//                LogDebug("❌ Systèmes requis non trouvés");
//                return;
//            }

//            LogDebug("🔍 Outil de diagnostic volcanique initialisé");
//        }

//        private void Update()
//        {
//            if (enableContinuousTracking && currentTracking != null)
//            {
//                TrackVolcanoMovement();
//            }
//        }

//        /// <summary>
//        /// MÉTHODE PRINCIPALE : Commencer diagnostic sur le prochain volcan créé
//        /// </summary>
//        [ContextMenu("Démarrer Diagnostic Volcan")]
//        public void StartVolcanoDiagnostic()
//        {
//            LogDebug("🧪 === DÉBUT DIAGNOSTIC VOLCAN ===");

//            // Nettoyer diagnostic précédent
//            CleanupPreviousDiagnostic();

//            // Forcer création d'un volcan pour diagnostic
//            if (volcanicSystem != null && volcanicSystem.IsInitialized)
//            {
//                // Hook pour capturer le prochain volcan créé
//                StartCoroutine(WaitForVolcanoCreation());
//            }
//            else
//            {
//                LogDebug("❌ Système volcanique non prêt");
//            }
//        }

//        /// <summary>
//        /// Attendre qu'un volcan soit créé et commencer le tracking
//        /// </summary>
//        private System.Collections.IEnumerator WaitForVolcanoCreation()
//        {
//            int initialCount = volcanicSystem.VolcanoCount;
//            LogDebug($"📊 Volcans actuels: {initialCount}");

//            // Déclencher création volcan
//            volcanicSystem.CreateTestVolcano();

//            // Attendre qu'un nouveau volcan apparaisse
//            yield return new WaitUntil(() => volcanicSystem.VolcanoCount > initialCount);

//            // Capturer le dernier volcan créé
//            var volcanoes = volcanicSystem.Volcanoes;
//            if (volcanoes.Count > 0)
//            {
//                var lastVolcano = volcanoes[volcanoes.Count - 1];
//                StartTrackingVolcano(lastVolcano);
//            }
//        }

//        /// <summary>
//        /// Commencer tracking d'un volcan spécifique
//        /// </summary>
//        private void StartTrackingVolcano(CleanVolcanicSystem.SimpleVolcano volcano)
//        {
//            LogDebug($"🎯 === DÉBUT TRACKING VOLCAN {volcano.type} ===");

//            currentTracking = new VolcanoTrackingData
//            {
//                volcanoMesh = volcano.visualObject,
//                initialPosition = volcano.visualObject.transform.position,
//                currentPosition = volcano.visualObject.transform.position,
//                expectedHeightMapCoords = volcano.heightMapCoords,
//                trackingStartTime = Time.time
//            };

//            LogDebug($"📍 Position initiale mesh: {currentTracking.initialPosition}");
//            LogDebug($"🗺️ Coordonnées HeightMap attendues: ({currentTracking.expectedHeightMapCoords.x}, {currentTracking.expectedHeightMapCoords.y})");

//            // Attendre un frame pour que la déformation soit appliquée
//            StartCoroutine(AnalyzeDeformationAfterCreation());

//            // Créer marqueurs visuels
//            if (createVisualMarkers)
//            {
//                CreateVisualMarkers();
//            }
//        }

//        /// <summary>
//        /// Analyser où est réellement la déformation après création
//        /// </summary>
//        private System.Collections.IEnumerator AnalyzeDeformationAfterCreation()
//        {
//            yield return new WaitForSeconds(0.5f); // Laisser temps à la déformation

//            LogDebug("🔍 === ANALYSE DÉFORMATION RÉELLE ===");

//            // Trouver le centre réel de la déformation
//            Vector2Int realDeformationCenter = FindRealDeformationCenter();
//            currentTracking.actualDeformationCenter = realDeformationCenter;

//            // Convertir position mesh vers coordonnées HeightMap
//            Vector3 currentMeshPos = currentTracking.volcanoMesh.transform.position;
//            Vector2Int currentMeshCoords = WorldToHeightMapCoords(currentMeshPos);

//            LogDebug($"📍 Position mesh ACTUELLE: {currentMeshPos}");
//            LogDebug($"🗺️ Coordonnées mesh ACTUELLES: ({currentMeshCoords.x}, {currentMeshCoords.y})");
//            LogDebug($"🗺️ Coordonnées déformation RÉELLE: ({realDeformationCenter.x}, {realDeformationCenter.y})");

//            // Calculer distances
//            int meshExpectedDistance = Mathf.RoundToInt(Vector2Int.Distance(currentMeshCoords, currentTracking.expectedHeightMapCoords));
//            int deformationExpectedDistance = Mathf.RoundToInt(Vector2Int.Distance(realDeformationCenter, currentTracking.expectedHeightMapCoords));
//            int meshDeformationDistance = Mathf.RoundToInt(Vector2Int.Distance(currentMeshCoords, realDeformationCenter));

//            LogDebug($"📏 === DIAGNOSTIC DISTANCES ===");
//            LogDebug($"   Mesh ↔ Position attendue: {meshExpectedDistance} cellules");
//            LogDebug($"   Déformation ↔ Position attendue: {deformationExpectedDistance} cellules");
//            LogDebug($"   Mesh ↔ Déformation: {meshDeformationDistance} cellules");

//            // DIAGNOSTIC PRINCIPAL
//            if (meshExpectedDistance <= 2 && deformationExpectedDistance > 10)
//            {
//                LogDebug("🎯 === DIAGNOSTIC: DÉFORMATION MAL PLACÉE ===");
//                LogDebug("✅ Le mesh est au bon endroit");
//                LogDebug("❌ La déformation est ailleurs → Problème de conversion coordonnées");
//            }
//            else if (meshExpectedDistance > 10 && deformationExpectedDistance <= 2)
//            {
//                LogDebug("🎯 === DIAGNOSTIC: MESH DÉPLACÉ ===");
//                LogDebug("❌ Le mesh a bougé après création");
//                LogDebug("✅ La déformation est au bon endroit");
//            }
//            else if (meshDeformationDistance <= 2)
//            {
//                LogDebug("🎯 === DIAGNOSTIC: ALIGNEMENT CORRECT ===");
//                LogDebug("✅ Mesh et déformation sont alignés");
//                LogDebug("❓ Problème peut-être visuel ou d'échelle");
//            }
//            else
//            {
//                LogDebug("🎯 === DIAGNOSTIC: PROBLÈME COMPLEXE ===");
//                LogDebug("❌ Ni mesh ni déformation au bon endroit");
//                LogDebug("💡 Vérifier tout le pipeline de création");
//            }

//            // Continuer tracking si demandé
//            if (enableContinuousTracking)
//            {
//                InvokeRepeating(nameof(TrackVolcanoMovement), trackingInterval, trackingInterval);
//            }
//        }

//        /// <summary>
//        /// Trouver le vrai centre de déformation en scannant la HeightMap
//        /// </summary>
//        private Vector2Int FindRealDeformationCenter()
//        {
//            Vector2Int bestCenter = Vector2Int.zero;
//            float maxHeight = 0f;
//            int mapResolution = 512; // À ajuster selon votre résolution

//            // Scanner toute la HeightMap pour trouver le pic
//            for (int x = 0; x < mapResolution; x++)
//            {
//                for (int y = 0; y < mapResolution; y++)
//                {
//                    float height = terrainManager.GetComposedHeightAt(x, y);
//                    if (height > maxHeight)
//                    {
//                        maxHeight = height;
//                        bestCenter = new Vector2Int(x, y);
//                    }
//                }
//            }

//            LogDebug($"🏔️ Pic de déformation trouvé: ({bestCenter.x}, {bestCenter.y}) = {maxHeight:F6}");
//            return bestCenter;
//        }

//        /// <summary>
//        /// Conversion position 3D → HeightMap (copie de la logique volcanique)
//        /// </summary>
//        private Vector2Int WorldToHeightMapCoords(Vector3 worldPosition)
//        {
//            Vector3 direction = worldPosition.normalized;
//            float longitude = Mathf.Atan2(direction.x, direction.z);
//            float latitude = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f));
//            float u = (longitude + Mathf.PI) / (2 * Mathf.PI);
//            float v = (latitude + Mathf.PI / 2) / Mathf.PI;
//            int mapResolution = 512;
//            int x = Mathf.Clamp(Mathf.RoundToInt(u * (mapResolution - 1)), 0, mapResolution - 1);
//            int y = Mathf.Clamp(Mathf.RoundToInt(v * (mapResolution - 1)), 0, mapResolution - 1);
//            return new Vector2Int(x, y);
//        }

//        /// <summary>
//        /// Tracking continu du mouvement du volcan
//        /// </summary>
//        private void TrackVolcanoMovement()
//        {
//            if (currentTracking?.volcanoMesh == null) return;

//            Vector3 newPosition = currentTracking.volcanoMesh.transform.position;
//            float movementDistance = Vector3.Distance(currentTracking.currentPosition, newPosition);

//            if (movementDistance > 0.01f) // Seuil de détection mouvement
//            {
//                LogDebug($"🚨 MOUVEMENT DÉTECTÉ !");
//                LogDebug($"   Temps: {Time.time - currentTracking.trackingStartTime:F1}s");
//                LogDebug($"   Ancienne position: {currentTracking.currentPosition}");
//                LogDebug($"   Nouvelle position: {newPosition}");
//                LogDebug($"   Distance: {movementDistance:F3}");

//                currentTracking.currentPosition = newPosition;

//                // Mettre à jour marqueur visuel
//                if (currentTracking.positionMarker != null)
//                {
//                    currentTracking.positionMarker.transform.position = newPosition;
//                }
//            }
//        }

//        /// <summary>
//        /// Créer marqueurs visuels pour debug
//        /// </summary>
//        private void CreateVisualMarkers()
//        {
//            // Marqueur position mesh
//            currentTracking.positionMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//            currentTracking.positionMarker.name = "VolcanoPositionMarker";
//            currentTracking.positionMarker.transform.position = currentTracking.initialPosition;
//            currentTracking.positionMarker.transform.localScale = Vector3.one * 0.5f;
//            currentTracking.positionMarker.GetComponent<MeshRenderer>().material.color = Color.green;

//            LogDebug("🟢 Marqueur vert = Position mesh volcan");
//        }

//        /// <summary>
//        /// Nettoyer diagnostic précédent
//        /// </summary>
//        private void CleanupPreviousDiagnostic()
//        {
//            if (currentTracking != null)
//            {
//                if (currentTracking.positionMarker != null)
//                    DestroyImmediate(currentTracking.positionMarker);
//                if (currentTracking.deformationMarker != null)
//                    DestroyImmediate(currentTracking.deformationMarker);

//                CancelInvoke(nameof(TrackVolcanoMovement));
//            }
//            currentTracking = null;
//        }

//        /// <summary>
//        /// Afficher résumé diagnostic
//        /// </summary>
//        [ContextMenu("Afficher Résumé Diagnostic")]
//        public void ShowDiagnosticSummary()
//        {
//            if (currentTracking == null)
//            {
//                LogDebug("❌ Aucun diagnostic en cours");
//                return;
//            }

//            LogDebug("📋 === RÉSUMÉ DIAGNOSTIC ===");
//            LogDebug($"   Volcan tracké: {currentTracking.volcanoMesh?.name}");
//            LogDebug($"   Position initiale: {currentTracking.initialPosition}");
//            LogDebug($"   Position actuelle: {currentTracking.currentPosition}");
//            LogDebug($"   A bougé: {Vector3.Distance(currentTracking.initialPosition, currentTracking.currentPosition) > 0.01f}");
//            LogDebug($"   Coordonnées attendues: {currentTracking.expectedHeightMapCoords}");
//            LogDebug($"   Déformation réelle: {currentTracking.actualDeformationCenter}");
//        }

//        private void LogDebug(string message)
//        {
//            if (enableDebugLogs)
//            {
//                Debug.Log($"[VolcanoDiagnostic] {message}");
//            }
//        }

//        private void OnDestroy()
//        {
//            CleanupPreviousDiagnostic();
//        }
//    }
//}

//// INSTRUCTIONS D'UTILISATION :
//// 1. Ajouter ce script à un GameObject vide dans la scène
//// 2. Cliquer "Démarrer Diagnostic Volcan" dans le menu contextuel
//// 3. Observer les logs pour identifier la cause précise du problème
//// 4. Le diagnostic dira clairement si c'est le mesh qui bouge ou la déformation mal placée