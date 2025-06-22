// SeparationDiagnostic.cs - Diagnostic complet du système de séparation
using UnityEngine;
using LifeStory.Core;
using LifeStory.Tectonics;

namespace LifeStory.Diagnostics  // Changé de Debug à Diagnostics
{
    public class SeparationDiagnostic : MonoBehaviour
    {
        [Header("Diagnostic Configuration")]
        [SerializeField] private bool enableContinuousLogging = true;
        [SerializeField] private float loggingInterval = 5f;
        [SerializeField] private bool logEveryTemperatureChange = true;

        private GameManager gameManager;
        private ContinentalSeparationSystem separationSystem;
        private float lastLogTime = 0f;
        private float lastKnownCoreTemp = -1f;
        private GamePhase lastKnownPhase = GamePhase.Geological;
        private int temperatureChangeCount = 0;

        private void Start()
        {
            // Trouver les références
            gameManager = GameManager.Instance;
            separationSystem = FindAnyObjectByType<ContinentalSeparationSystem>();

            if (gameManager == null)
            {
                Debug.LogError("❌ GameManager non trouvé pour diagnostic");
                return;
            }

            if (separationSystem == null)
            {
                Debug.LogError("❌ ContinentalSeparationSystem non trouvé pour diagnostic");
                return;
            }

            // S'abonner DIRECTEMENT aux événements pour traquer
            GameManager.OnCoreTemperatureChanged += OnDiagnosticCoreTemperatureChanged;

            Debug.Log("🔍 === DIAGNOSTIC SÉPARATION CONTINENTALE ACTIVÉ ===");
            ShowInitialStatus();
        }

        private void Update()
        {
            if (enableContinuousLogging && Time.time - lastLogTime >= loggingInterval)
            {
                ShowCurrentStatus();
                lastLogTime = Time.time;
            }
        }

        private void OnDiagnosticCoreTemperatureChanged(float newCoreTemp)
        {
            temperatureChangeCount++;

            if (logEveryTemperatureChange)
            {
                Debug.Log($"🌡️ [DIAGNOSTIC] Changement température #{temperatureChangeCount}: {lastKnownCoreTemp:F0}°C → {newCoreTemp:F0}°C");
                Debug.Log($"    Phase: {gameManager.CurrentPhase}");
                Debug.Log($"    Système initialisé: {(separationSystem != null ? separationSystem.GetType().GetProperty("IsInitialized")?.GetValue(separationSystem) : "UNKNOWN")}");
                Debug.Log($"    Séparation possible: {(separationSystem != null ? separationSystem.IsSeparationPossible : false)}");
                Debug.Log($"    Séparation active: {(separationSystem != null ? separationSystem.IsSeparationActive : false)}");
            }

            lastKnownCoreTemp = newCoreTemp;
        }

        [ContextMenu("🔍 Show Complete Status")]
        public void ShowInitialStatus()
        {
            Debug.Log("🔍 === STATUT INITIAL DIAGNOSTIC ===");

            if (gameManager != null)
            {
                Debug.Log($"   GameManager: ✅ Trouvé");
                Debug.Log($"   Température Core: {gameManager.CoreTemperature:F0}°C");
                Debug.Log($"   Phase actuelle: {gameManager.CurrentPhase}");
                Debug.Log($"   Âge planète: {gameManager.PlanetAge:F1} millions d'années");
            }
            else
            {
                Debug.Log($"   GameManager: ❌ NULL");
            }

            if (separationSystem != null)
            {
                Debug.Log($"   ContinentalSeparationSystem: ✅ Trouvé");
                Debug.Log($"   Seuil début: {separationSystem.SeparationStartTemp:F0}°C");
                Debug.Log($"   Seuil fin: {separationSystem.SeparationEndTemp:F0}°C");
                Debug.Log($"   Séparation possible: {separationSystem.IsSeparationPossible}");
                Debug.Log($"   Séparation active: {separationSystem.IsSeparationActive}");

                // Utiliser réflexion pour accéder aux champs privés
                var systemInitField = separationSystem.GetType().GetField("systemInitialized",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                bool systemInit = systemInitField?.GetValue(separationSystem) is bool value ? value : false;
                Debug.Log($"   Système initialisé: {systemInit}");

                var enableSeparationField = separationSystem.GetType().GetField("enableSeparation",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                bool enableSep = enableSeparationField?.GetValue(separationSystem) is bool value2 ? value2 : false;
                Debug.Log($"   Séparation activée: {enableSep}");
            }
            else
            {
                Debug.Log($"   ContinentalSeparationSystem: ❌ NULL");
            }

            // Vérifier abonnement aux événements
            CheckEventSubscription();
        }

        public void ShowCurrentStatus()
        {
            if (gameManager == null || separationSystem == null) return;

            float currentTemp = gameManager.CoreTemperature;
            GamePhase currentPhase = gameManager.CurrentPhase;

            // Seulement logger si changement significatif
            if (Mathf.Abs(currentTemp - lastKnownCoreTemp) > 10f || currentPhase != lastKnownPhase)
            {
                Debug.Log($"🔍 [CONTINU] Temp: {currentTemp:F0}°C, Phase: {currentPhase}, Séparation: {(separationSystem.IsSeparationActive ? "ACTIVE" : "INACTIVE")}");
                lastKnownCoreTemp = currentTemp;
                lastKnownPhase = currentPhase;
            }
        }

        private void CheckEventSubscription()
        {
            Debug.Log("🔍 === VÉRIFICATION ABONNEMENTS ÉVÉNEMENTS ===");

            try
            {
                // Utiliser réflexion pour vérifier les abonnements
                var eventField = typeof(GameManager).GetField("OnCoreTemperatureChanged",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (eventField != null)
                {
                    var eventValue = eventField.GetValue(null) as System.MulticastDelegate;
                    if (eventValue != null)
                    {
                        var invocationList = eventValue.GetInvocationList();
                        Debug.Log($"   OnCoreTemperatureChanged: {invocationList.Length} abonnés");

                        foreach (var method in invocationList)
                        {
                            Debug.Log($"     - {method.Target?.GetType().Name ?? "Static"}.{method.Method.Name}");
                        }
                    }
                    else
                    {
                        Debug.Log($"   OnCoreTemperatureChanged: ❌ Aucun abonné");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.Log($"   Erreur vérification abonnements: {e.Message}");
            }
        }

        [ContextMenu("🧪 Force Temperature Event")]
        public void ForceTemperatureEvent()
        {
            if (gameManager != null)
            {
                float currentTemp = gameManager.CoreTemperature;
                Debug.Log($"🧪 FORCE: Simulation événement température à {currentTemp:F0}°C");

                // Déclencher manuellement l'événement
                GameManager.OnCoreTemperatureChanged?.Invoke(currentTemp);

                Debug.Log($"🧪 Événement forcé envoyé - Vérifiez les logs ContinentalSeparation");
            }
        }

        [ContextMenu("🎯 Test Separation Conditions")]
        public void TestSeparationConditions()
        {
            Debug.Log("🎯 === TEST CONDITIONS SÉPARATION ===");

            if (gameManager == null || separationSystem == null)
            {
                Debug.Log("❌ Références manquantes");
                return;
            }

            float coreTemp = gameManager.CoreTemperature;
            GamePhase phase = gameManager.CurrentPhase;

            Debug.Log($"   Température: {coreTemp:F0}°C");
            Debug.Log($"   Seuils: {separationSystem.SeparationEndTemp:F0}°C - {separationSystem.SeparationStartTemp:F0}°C");
            Debug.Log($"   Dans plage: {(coreTemp >= separationSystem.SeparationEndTemp && coreTemp <= separationSystem.SeparationStartTemp)}");
            Debug.Log($"   Phase: {phase} (besoin: Geological)");
            Debug.Log($"   Phase OK: {(phase == GamePhase.Geological)}");
            Debug.Log($"   IsSeparationPossible: {separationSystem.IsSeparationPossible}");
            Debug.Log($"   IsSeparationActive: {separationSystem.IsSeparationActive}");

            // Test manuel de déclenchement
            if (phase == GamePhase.Geological && separationSystem.IsSeparationPossible)
            {
                Debug.Log("✅ Conditions réunies - Test manuel possible");
            }
            else
            {
                Debug.Log("❌ Conditions non réunies");
            }
        }

        [ContextMenu("📊 Show Event Statistics")]
        public void ShowEventStatistics()
        {
            Debug.Log("📊 === STATISTIQUES ÉVÉNEMENTS ===");
            Debug.Log($"   Changements température détectés: {temperatureChangeCount}");
            Debug.Log($"   Dernière température: {lastKnownCoreTemp:F0}°C");
            Debug.Log($"   Diagnostic actif depuis: {Time.time:F1}s");
        }

        private void OnDestroy()
        {
            if (GameManager.OnCoreTemperatureChanged != null)
            {
                GameManager.OnCoreTemperatureChanged -= OnDiagnosticCoreTemperatureChanged;
            }
        }
    }
}