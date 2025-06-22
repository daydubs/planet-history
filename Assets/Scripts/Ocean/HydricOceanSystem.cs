// HydricOceanSystem.cs - Océan synchronisé avec le système hydrique du GameManager
using UnityEngine;
using LifeStory.Core;
using LifeStory.Generation;

namespace LifeStory.Ocean
{
    /// <summary>
    /// Système océan basé uniquement sur le niveau d'eau liquide du GameManager
    /// Remplace BiomeBasedOceanSystem - Plus simple et logique
    /// </summary>
    public class HydricOceanSystem : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("Si vide, utilise ce GameObject comme OceanSphere")]
        [SerializeField] private Transform oceanSphere;
        [SerializeField] private PlanetGenerator planetGenerator;

        [Header("Configuration Océan Physique")]
        [SerializeField] private float planetRadius = 10f;           // Rayon planète de base
        [SerializeField] private float minOceanDiameter = 20f;       // Diamètre initial (océan minimal visible)
        [SerializeField] private float maxOceanDiameter = 23f;       // Diamètre déluge total (100% eau)
        [SerializeField] private float normalOceanLevel = 0.4f;      // 40% eau liquide = océan stable
        [SerializeField] private float normalOceanDiameter = 20.27f; // Diamètre océan stable (40% eau)
        [SerializeField] private float oceanAppearanceThreshold = 0.01f; // Océan apparaît à 1% d'eau liquide

        [Header("Mise à Jour")]
        [SerializeField] private bool autoUpdate = true;
        [SerializeField] private float updateInterval = 0.5f;        // Mise à jour fréquente
        [SerializeField] private float significantChangeThreshold = 0.005f; // 0.5% de changement minimum

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showWaterLevelData = true;

        // État système
        private GameManager gameManager;
        private float currentOceanDiameter;
        private float lastWaterLevel = -1f;
        private bool isInitialized = false;
        private bool oceanIsVisible = false;
        private bool hasInitializedOnce = false; // ✅ NOUVEAU

        public static System.Action<float> OnOceanDiameterChanged;

        private void Start()
        {
            StartCoroutine(InitializeSystem());
        }

        private System.Collections.IEnumerator InitializeSystem()
        {
            LogDebug("⏳ Initialisation système océan hydrique...");
            yield return new WaitForSeconds(2f); // Laisser les autres systèmes s'initialiser

            int attempts = 0;
            while (attempts < 5 && !FindReferences())
            {
                LogDebug($"🔍 Tentative {attempts + 1}/5 - Recherche références...");
                yield return new WaitForSeconds(1f);
                attempts++;
            }

            if (ValidateReferences())
            {
                // Forcer océan caché au démarrage
                //ForceOceanHidden();

                // S'abonner aux événements du GameManager
                GameManager.OnWaterLevelChanged += OnWaterLevelChanged;

                // Démarrer mise à jour périodique si activée
                if (autoUpdate)
                {
                    StartCoroutine(PeriodicUpdate());
                }

                isInitialized = true;
                LogDebug("✅ HydricOceanSystem initialisé - Connecté au système hydrique !");
            }
            else
            {
                LogDebug("❌ Impossible d'initialiser - références manquantes");
                ShowMissingReferences();
            }

            hasInitializedOnce = true;
            LogDebug("✅ HydricOceanSystem prêt - Événements activés");
        }

        /// <summary>
        /// Force l'océan au minimum au démarrage
        /// </summary>
        private void ForceOceanHidden()
        {
            if (oceanSphere != null)
            {
                Vector3 minScale = Vector3.one * minOceanDiameter;
                oceanSphere.localScale = minScale;
                currentOceanDiameter = minOceanDiameter;
                oceanIsVisible = false;

                LogDebug($"🌊 Océan initialisé au minimum - Diamètre: {minOceanDiameter:F2}");
            }
        }

        private bool FindReferences()
        {
            // OceanSphere - utiliser ce GameObject si pas assigné
            if (oceanSphere == null)
            {
                oceanSphere = this.transform;
                LogDebug($"🌊 Utilisation de ce GameObject comme OceanSphere: {gameObject.name}");
            }

            // Chercher l'enfant sphère si c'est un parent vide
            if (oceanSphere.childCount > 0)
            {
                for (int i = 0; i < oceanSphere.childCount; i++)
                {
                    Transform child = oceanSphere.GetChild(i);
                    if (child.name.Contains("OceanSphere") || child.GetComponent<MeshRenderer>() != null)
                    {
                        oceanSphere = child; // Utiliser l'enfant avec le mesh
                        LogDebug($"🎯 Vraie sphère océan trouvée : {child.name} (enfant)");
                        break;
                    }
                }
            }

            // GameManager
            if (gameManager == null)
                gameManager = GameManager.Instance;

            // PlanetGenerator (pour validation du rayon planète)
            if (planetGenerator == null)
                planetGenerator = PlanetGenerator.Instance;

            return ValidateReferences();
        }

        private bool ValidateReferences()
        {
            return oceanSphere != null && gameManager != null;
        }

        private void ShowMissingReferences()
        {
            LogDebug("❌ RÉFÉRENCES MANQUANTES:");
            LogDebug($"   OceanSphere: {(oceanSphere != null ? "✓" : "❌ NULL")}");
            LogDebug($"   GameManager: {(gameManager != null ? "✓" : "❌ NULL")}");
            LogDebug($"   PlanetGenerator: {(planetGenerator != null ? "✓" : "❌ Optionnel")}");
        }

        /// <summary>
        /// Event handler - Appelé quand le niveau d'eau change dans GameManager
        /// </summary>
        private void OnWaterLevelChanged(float newWaterLevel)
        {
            if (!isInitialized) return;

            if (showWaterLevelData)
            {
                LogDebug($"💧 Eau liquide changée: {lastWaterLevel:F3} → {newWaterLevel:F3}");
            }

            // Mettre à jour océan basé sur nouveau niveau d'eau
            UpdateOceanBasedOnWaterLevel(newWaterLevel);
            lastWaterLevel = newWaterLevel;
        }

        private System.Collections.IEnumerator PeriodicUpdate()
        {
            while (true)
            {
                yield return new WaitForSeconds(updateInterval);

                if (HasWaterLevelChanged())
                {
                    float currentWaterLevel = gameManager?.WaterLevel ?? 0f;
                    UpdateOceanBasedOnWaterLevel(currentWaterLevel);
                }
            }
        }

        private bool HasWaterLevelChanged()
        {
            if (gameManager == null) return false;

            float currentWaterLevel = gameManager.WaterLevel;
            bool changed = Mathf.Abs(currentWaterLevel - lastWaterLevel) > significantChangeThreshold;

            if (changed)
            {
                LogDebug($"🔄 Changement eau détecté - {lastWaterLevel:F3} → {currentWaterLevel:F3}");
                lastWaterLevel = currentWaterLevel;
            }

            return changed;
        }

        /// <summary>
        /// Met à jour la taille de l'océan basé sur le niveau d'eau liquide
        /// </summary>
        private void UpdateOceanBasedOnWaterLevel(float waterLevel)
        {
            if (!isInitialized || oceanSphere == null) return;

            // Déterminer si l'océan doit être visible
            if (waterLevel < oceanAppearanceThreshold)
            {
                // Pas assez d'eau - cacher océan
                if (oceanIsVisible)
                {
                    HideOcean();
                }
                return;
            }

            // Assez d'eau - montrer et dimensionner océan
            if (!oceanIsVisible)
            {
                ShowOcean();
            }

            // Calculer nouveau diamètre basé sur niveau d'eau (0-100%)
            // Progression en deux phases : 0-40% (formation) puis 40-100% (événements/déluge)
            float targetDiameter;

            if (waterLevel <= normalOceanLevel)
            {
                // Phase 1: Formation océan normal (0% → 40%)
                float normalizedLevel = waterLevel / normalOceanLevel;
                targetDiameter = Mathf.Lerp(minOceanDiameter, normalOceanDiameter, normalizedLevel);
            }
            else
            {
                // Phase 2: Événements/Inondations/Déluge (40% → 100%)
                float excessLevel = (waterLevel - normalOceanLevel) / (1f - normalOceanLevel);
                targetDiameter = Mathf.Lerp(normalOceanDiameter, maxOceanDiameter, excessLevel);
            }

            // Appliquer si changement significatif OU si on atteint exactement le niveau stable
            bool significantChange = Mathf.Abs(targetDiameter - currentOceanDiameter) > (significantChangeThreshold * maxOceanDiameter);
            bool reachedStableLevel = Mathf.Approximately(waterLevel, normalOceanLevel) && targetDiameter != currentOceanDiameter;

            if (significantChange || reachedStableLevel)
            {
                ApplyOceanDiameter(targetDiameter, waterLevel);
            }
        }

        private void ApplyOceanDiameter(float newDiameter, float waterLevel)
        {
            currentOceanDiameter = newDiameter;

            // Appliquer scale à Unity (diamètre = scale pour sphere primitive)
            Vector3 newScale = Vector3.one * newDiameter;
            oceanSphere.localScale = newScale;

            if (hasInitializedOnce)
            {
                OnOceanDiameterChanged?.Invoke(newDiameter);
                LogDebug($"📡 Événement envoyé - Diamètre: {newDiameter:F2}");
            }
            else
            {
                LogDebug($"🔇 Initialisation - Pas d'événement envoyé pour diamètre: {newDiameter:F2}");
            }

            //OnOceanDiameterChanged?.Invoke(newDiameter);

            if (showWaterLevelData)
            {
                string phase = waterLevel <= normalOceanLevel ? "Formation" : "Événements";
                LogDebug($"🌊 Océan redimensionné - Eau: {waterLevel:P1} ({phase}) | Diamètre: {newDiameter:F2} | Scale: {newScale.x:F2}");

                if (waterLevel >= 0.8f)
                    LogDebug($"⚠️ ALERTE DÉLUGE - Niveau critique: {waterLevel:P1}");
                else if (waterLevel > normalOceanLevel)
                    LogDebug($"🌊 Inondation en cours - Niveau: {waterLevel:P1}");
            }
        }

        private void HideOcean()
        {
            if (oceanSphere != null)
            {
                Vector3 minScale = Vector3.one * minOceanDiameter;
                oceanSphere.localScale = minScale;
                currentOceanDiameter = minOceanDiameter;
                oceanIsVisible = false;
                LogDebug("🌊 Océan réduit au minimum - Pas assez d'eau liquide");
            }
        }

        private void ShowOcean()
        {
            oceanIsVisible = true;
            LogDebug("🌊 Océan apparaît - Condensation suffisante !");
        }

        // === MÉTHODES DEBUG ===

        [ContextMenu("🔍 Show Water System Status")]
        public void ShowWaterSystemStatus()
        {
            if (gameManager == null)
            {
                LogDebug("❌ GameManager non disponible");
                return;
            }

            LogDebug("=== ÉTAT SYSTÈME HYDRIQUE ===");
            LogDebug($"Eau liquide: {gameManager.WaterLevel:P1}");
            LogDebug($"Vapeur: {gameManager.VaporLevel:P1}");
            LogDebug($"Glace: {gameManager.IceLevel:P1}");
            LogDebug($"État eau: {gameManager.CurrentWaterState}");

            float totalWater = gameManager.WaterLevel + gameManager.VaporLevel + gameManager.IceLevel;
            LogDebug($"Eau totale: {totalWater:P1}");

            LogDebug($"Niveau océan stable: {normalOceanLevel:P1} (diamètre {normalOceanDiameter:F2})");
            LogDebug($"Niveau déluge total: 100% (diamètre {maxOceanDiameter:F2})");
            LogDebug($"Océan visible: {(gameManager.WaterLevel >= oceanAppearanceThreshold ? "OUI" : "NON")}");

            if (gameManager.WaterLevel >= oceanAppearanceThreshold)
            {
                float currentWater = gameManager.WaterLevel;
                float targetDiameter;
                string phase;

                if (currentWater <= normalOceanLevel)
                {
                    float normalizedLevel = currentWater / normalOceanLevel;
                    targetDiameter = Mathf.Lerp(minOceanDiameter, normalOceanDiameter, normalizedLevel);
                    phase = "Formation";
                }
                else
                {
                    float excessLevel = (currentWater - normalOceanLevel) / (1f - normalOceanLevel);
                    targetDiameter = Mathf.Lerp(normalOceanDiameter, maxOceanDiameter, excessLevel);
                    phase = currentWater >= 0.8f ? "DÉLUGE" : "Inondation";
                }

                LogDebug($"Phase: {phase} | Eau: {currentWater:P1}");
                LogDebug($"Diamètre océan calculé: {targetDiameter:F2}");
                LogDebug($"Diamètre océan actuel: {currentOceanDiameter:F2}");
            }
        }

        [ContextMenu("🔧 Force Ocean Update")]
        public void ForceOceanUpdate()
        {
            if (gameManager != null)
            {
                LogDebug("🔧 Mise à jour océan forcée");
                float currentWaterLevel = gameManager.WaterLevel;
                UpdateOceanBasedOnWaterLevel(currentWaterLevel);
            }
            else
            {
                LogDebug("❌ GameManager non disponible pour mise à jour forcée");
            }
        }

        [ContextMenu("🧪 Test Ocean Sizes")]
        public void TestOceanSizes()
        {
            if (!isInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            LogDebug("🧪 Test tailles océan...");

            // Simuler différents niveaux d'eau incluant événements catastrophiques
            float[] testLevels = { 0f, 0.01f, 0.2f, 0.4f, 0.5f, 0.6f, 0.8f, 1f };

            foreach (float testLevel in testLevels)
            {
                float targetDiameter;
                string eventType;

                if (testLevel < oceanAppearanceThreshold)
                {
                    targetDiameter = minOceanDiameter;
                    eventType = "Caché";
                }
                else if (testLevel <= normalOceanLevel)
                {
                    float normalizedLevel = testLevel / normalOceanLevel;
                    targetDiameter = Mathf.Lerp(minOceanDiameter, normalOceanDiameter, normalizedLevel);
                    eventType = "Formation";
                }
                else
                {
                    float excessLevel = (testLevel - normalOceanLevel) / (1f - normalOceanLevel);
                    targetDiameter = Mathf.Lerp(normalOceanDiameter, maxOceanDiameter, excessLevel);

                    if (testLevel >= 1f)
                        eventType = "DÉLUGE TOTAL";
                    else if (testLevel >= 0.8f)
                        eventType = "Déluge majeur";
                    else if (testLevel >= 0.6f)
                        eventType = "Inondation majeure";
                    else
                        eventType = "Inondation légère";
                }

                LogDebug($"Eau {testLevel:P0} → Diamètre {targetDiameter:F2} ({eventType})");
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[HydricOcean] {message}");
        }

        // === CLEANUP ===
        private void OnDestroy()
        {
            // Se désabonner des événements
            if (GameManager.OnWaterLevelChanged != null)
            {
                GameManager.OnWaterLevelChanged -= OnWaterLevelChanged;
            }
        }

        // === PROPRIÉTÉS PUBLIQUES ===

        public bool IsInitialized => isInitialized;
        public float CurrentOceanDiameter => currentOceanDiameter;
        public bool OceanIsVisible => oceanIsVisible;
        public float WaterLevel => gameManager?.WaterLevel ?? 0f;
    }
}