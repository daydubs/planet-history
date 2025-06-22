// RiftSystemMigration.cs - Script utilitaire pour migrer de l'ancien système au nouveau
using LifeStory.Generation;
using LifeStory.Tectonics;
using LifeStory.Terrain;
using UnityEngine;

namespace LifeStory.Migration
{
    [System.Serializable]
    public class RiftSystemMigration : MonoBehaviour
    {
        [Header("Migration Configuration")]
        [SerializeField] private bool autoMigrateOnStart = true;
        [SerializeField] private bool preserveOldSystemBackup = true;
        [SerializeField] private bool enableMigrationLogs = true;

        [Header("Component References")]
        [SerializeField] private GameObject newSystemPrefab; // Si vous utilisez un prefab

        [Header("Migration Actions")]
        [Space]
        [SerializeField] private bool stepByStepMigration = false;

        private TerrainModificationManager terrainManager;
        private ContinentalSeparationSystem newSeparationSystem;

        private void Start()
        {
            if (autoMigrateOnStart)
            {
                StartCoroutine(AutoMigration());
            }
        }

        private System.Collections.IEnumerator AutoMigration()
        {
            yield return new WaitForSeconds(1f); // Laisser les systèmes s'initialiser

            LogMigration("🔄 === DÉBUT MIGRATION AUTOMATIQUE ===");

            if (!stepByStepMigration)
            {
                PerformFullMigration();
            }
            else
            {
                yield return PerformStepByStepMigration();
            }

            LogMigration("✅ === MIGRATION TERMINÉE ===");
        }

        [ContextMenu("🔄 Perform Full Migration")]
        public void PerformFullMigration()
        {
            LogMigration("🔄 MIGRATION COMPLÈTE EN COURS...");

            // 1. Trouver le TerrainManager
            FindTerrainManager();

            // 2. Désactiver l'ancien système
            DisableOldRiftingSystem();

            // 3. Nettoyer les anciennes couches
            CleanupOldRiftLayers();

            // 4. Configurer le nouveau système
            SetupNewSeparationSystem();

            // 5. Valider la migration
            ValidateMigration();

            LogMigration("✅ Migration complète terminée");
        }

        private System.Collections.IEnumerator PerformStepByStepMigration()
        {
            LogMigration("📋 MIGRATION ÉTAPE PAR ÉTAPE...");

            LogMigration("Étape 1/5: Recherche TerrainManager...");
            FindTerrainManager();
            yield return new WaitForSeconds(0.5f);

            LogMigration("Étape 2/5: Désactivation ancien système...");
            DisableOldRiftingSystem();
            yield return new WaitForSeconds(0.5f);

            LogMigration("Étape 3/5: Nettoyage anciennes couches...");
            CleanupOldRiftLayers();
            yield return new WaitForSeconds(0.5f);

            LogMigration("Étape 4/5: Configuration nouveau système...");
            SetupNewSeparationSystem();
            yield return new WaitForSeconds(0.5f);

            LogMigration("Étape 5/5: Validation...");
            ValidateMigration();
            yield return new WaitForSeconds(0.5f);

            LogMigration("✅ Migration étape par étape terminée");
        }

        private void FindTerrainManager()
        {
            terrainManager = TerrainModificationManager.Instance;
            if (terrainManager != null)
            {
                LogMigration("✅ TerrainModificationManager trouvé");
            }
            else
            {
                LogMigration("❌ TerrainModificationManager introuvable");
            }
        }

        private void DisableOldRiftingSystem()
        {
            // Chercher tous les anciens systèmes de rifting
            var oldSystems = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            int disabledCount = 0;

            foreach (var component in oldSystems)
            {
                if (component.GetType().Name == "ContinentalRiftingSystem")
                {
                    if (preserveOldSystemBackup)
                    {
                        // Désactiver seulement
                        component.enabled = false;
                        LogMigration($"🔒 Ancien système désactivé (sauvegardé): {component.name}");
                    }
                    else
                    {
                        // Détruire complètement
                        DestroyImmediate(component);
                        LogMigration($"🗑️ Ancien système détruit: {component.name}");
                    }
                    disabledCount++;
                }
            }

            LogMigration($"✅ {disabledCount} ancien(s) système(s) traité(s)");
        }

        private void CleanupOldRiftLayers()
        {
            if (terrainManager == null) return;

            LogMigration("🧹 Nettoyage couches anciennes...");

            try
            {
                // Créer une couche vide pour la couche "Rifts"
                var planetGenerator = FindAnyObjectByType<PlanetGenerator>();
                if (planetGenerator != null)
                {
                    int resolution = planetGenerator.Resolution;
                    float[,] emptyLayer = new float[resolution, resolution];

                    // Nettoyer la couche RIFT_LAYER
                    terrainManager.RegisterModificationLayer(TerrainModificationManager.RIFT_LAYER, emptyLayer, "MigrationCleanup");

                    LogMigration("✅ Couche rifts nettoyée");
                }
                else
                {
                    LogMigration("⚠️ PlanetGenerator introuvable - nettoyage partiel");
                }
            }
            catch (System.Exception e)
            {
                LogMigration($"❌ Erreur nettoyage couches: {e.Message}");
            }
        }

        private void SetupNewSeparationSystem()
        {
            // Chercher le nouveau système dans la scène
            newSeparationSystem = FindAnyObjectByType<ContinentalSeparationSystem>();

            if (newSeparationSystem == null)
            {
                LogMigration("⚠️ ContinentalSeparationSystem non trouvé dans la scène");

                if (newSystemPrefab != null)
                {
                    LogMigration("🔧 Tentative d'instanciation depuis prefab...");
                    GameObject newSystemObject = Instantiate(newSystemPrefab);
                    newSeparationSystem = newSystemObject.GetComponent<ContinentalSeparationSystem>();
                }
                else
                {
                    LogMigration("🔧 Création d'un nouveau ContinentalSeparationSystem...");
                    GameObject newSystemObject = new GameObject("ContinentalSeparationSystem");
                    newSeparationSystem = newSystemObject.AddComponent<ContinentalSeparationSystem>();
                }
            }

            if (newSeparationSystem != null)
            {
                // Configurer le nouveau système
                newSeparationSystem.enabled = true;
                LogMigration("✅ Nouveau système configuré et activé");
            }
            else
            {
                LogMigration("❌ Impossible de configurer le nouveau système");
            }
        }

        private void ValidateMigration()
        {
            LogMigration("🔍 VALIDATION MIGRATION:");

            // Vérifier que l'ancien système est bien désactivé
            var oldSystems = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            int activeOldSystems = 0;

            foreach (var component in oldSystems)
            {
                if (component.GetType().Name == "ContinentalRiftingSystem" && component.enabled)
                {
                    activeOldSystems++;
                }
            }

            LogMigration($"   Anciens systèmes actifs: {activeOldSystems} (devrait être 0)");

            // Vérifier le nouveau système
            bool newSystemReady = newSeparationSystem != null && newSeparationSystem.enabled;
            LogMigration($"   Nouveau système prêt: {(newSystemReady ? "✅" : "❌")}");

            // Vérifier TerrainManager
            bool terrainManagerReady = terrainManager != null && terrainManager.IsInitialized;
            LogMigration($"   TerrainManager prêt: {(terrainManagerReady ? "✅" : "❌")}");

            // Résultat final
            bool migrationSuccessful = activeOldSystems == 0 && newSystemReady && terrainManagerReady;
            LogMigration($"   RÉSULTAT: {(migrationSuccessful ? "✅ SUCCÈS" : "❌ ÉCHEC")}");

            if (!migrationSuccessful)
            {
                LogMigration("⚠️ Des problèmes ont été détectés. Vérifiez manuellement la configuration.");
            }
        }

        // === MÉTHODES MANUELLES POUR DEBUG ===
        [ContextMenu("🔍 Check Current State")]
        public void CheckCurrentState()
        {
            LogMigration("📊 === ÉTAT ACTUEL DES SYSTÈMES ===");

            // Anciens systèmes
            var oldSystems = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            int oldSystemCount = 0;
            foreach (var component in oldSystems)
            {
                if (component.GetType().Name == "ContinentalRiftingSystem")
                {
                    LogMigration($"   Ancien système: {component.name} - {(component.enabled ? "ACTIF" : "DÉSACTIVÉ")}");
                    oldSystemCount++;
                }
            }
            LogMigration($"   Total anciens systèmes: {oldSystemCount}");

            // Nouveau système
            var newSystem = FindAnyObjectByType<ContinentalSeparationSystem>();
            if (newSystem != null)
            {
                LogMigration($"   Nouveau système: {newSystem.name} - {(newSystem.enabled ? "ACTIF" : "DÉSACTIVÉ")}");
            }
            else
            {
                LogMigration("   Nouveau système: NON TROUVÉ");
            }

            // TerrainManager
            var terrain = TerrainModificationManager.Instance;
            if (terrain != null)
            {
                LogMigration($"   TerrainManager: {(terrain.IsInitialized ? "INITIALISÉ" : "NON INITIALISÉ")}");
            }
            else
            {
                LogMigration("   TerrainManager: NON TROUVÉ");
            }
        }

        [ContextMenu("🗑️ Force Remove Old Systems")]
        public void ForceRemoveOldSystems()
        {
            var oldSystems = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            int removedCount = 0;

            foreach (var component in oldSystems)
            {
                if (component.GetType().Name == "ContinentalRiftingSystem")
                {
                    DestroyImmediate(component);
                    removedCount++;
                    LogMigration($"🗑️ Ancien système détruit: {component.name}");
                }
            }

            LogMigration($"✅ {removedCount} ancien(s) système(s) supprimé(s)");
        }

        [ContextMenu("🔧 Force Create New System")]
        public void ForceCreateNewSystem()
        {
            var existing = FindAnyObjectByType<ContinentalSeparationSystem>();
            if (existing != null)
            {
                LogMigration("⚠️ Un nouveau système existe déjà");
                return;
            }

            GameObject newSystemObject = new GameObject("ContinentalSeparationSystem");
            var newSystem = newSystemObject.AddComponent<ContinentalSeparationSystem>();

            LogMigration("✅ Nouveau système créé manuellement");
        }

        private void LogMigration(string message)
        {
            if (enableMigrationLogs)
            {
                Debug.Log($"[RiftMigration] {message}");
            }
        }
    }
}