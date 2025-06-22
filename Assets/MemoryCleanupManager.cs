using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace LifeStory.Core
{
    /// <summary>
    /// Gestionnaire de nettoyage mémoire spécialisé pour Unity 6
    /// Corrige les fuites mémoire lors de simulations répétées
    /// </summary>
    public class MemoryCleanupManager : MonoBehaviour
    {
        [Header("🧹 Memory Cleanup Configuration")]
        [SerializeField] private bool enableAggressiveCleanup = true;
        [SerializeField] private bool enableCoroutineCleanup = true;
        [SerializeField] private float cleanupDelay = 0.5f; // Délai entre chaque étape

        [Header("Debug")]
        [SerializeField] private bool showCleanupLogs = true;

        private static MemoryCleanupManager _instance;
        public static MemoryCleanupManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MemoryCleanupManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("MemoryCleanupManager");
                        _instance = go.AddComponent<MemoryCleanupManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Nettoyage mémoire complet pour Unity 6
        /// Méthode principale à appeler à la fin de chaque simulation
        /// </summary>
        public void PerformCompleteMemoryCleanup()
        {
            if (enableCoroutineCleanup)
            {
                StartCoroutine(CompleteMemoryCleanupCoroutine());
            }
            else
            {
                ExecuteImmediateCleanup();
            }
        }

        /// <summary>
        /// Nettoyage immédiat synchrone
        /// </summary>
        private void ExecuteImmediateCleanup()
        {
            LogDebug("🧹 === NETTOYAGE MÉMOIRE IMMÉDIAT ===");

            // 1. Arrêter temps de jeu
            Time.timeScale = 0f;
            LogDebug("⏸️ Time.timeScale = 0");

            // 2. Nettoyer les systèmes spécifiques
            CleanupGameSystems();

            // 3. Nettoyage Unity agressif
            PerformAggressiveUnityCleanup();

            LogDebug("✅ Nettoyage mémoire immédiat terminé");
        }

        /// <summary>
        /// Nettoyage par étapes avec coroutine pour Unity 6
        /// Plus efficace pour éviter les pics de performance
        /// </summary>
        private IEnumerator CompleteMemoryCleanupCoroutine()
        {
            LogDebug("🧹 === DÉBUT NETTOYAGE MÉMOIRE COROUTINE ===");

            // 1. Arrêter temps de jeu
            Time.timeScale = 0f;
            LogDebug("⏸️ Time.timeScale = 0");
            yield return new WaitForSecondsRealtime(cleanupDelay);

            // 2. Nettoyer systèmes volcaniques
            CleanupVolcanicSystem();
            yield return new WaitForSecondsRealtime(cleanupDelay);

            // 3. Nettoyer système terrain (CRITIQUE pour votre fuite)
            CleanupTerrainSystem();
            yield return new WaitForSecondsRealtime(cleanupDelay);

            // 4. Nettoyer autres systèmes
            CleanupOtherGameSystems();
            yield return new WaitForSecondsRealtime(cleanupDelay);

            // 5. Nettoyage Unity agressif
            PerformAggressiveUnityCleanup();
            yield return new WaitForSecondsRealtime(cleanupDelay);

            // 6. Nettoyage final avec multiple passes
            if (enableAggressiveCleanup)
            {
                yield return StartCoroutine(MultiPassCleanupCoroutine());
            }

            LogDebug("✅ === NETTOYAGE MÉMOIRE COROUTINE TERMINÉ ===");
        }

        /// <summary>
        /// Nettoyage multi-passes pour Unity 6
        /// Nécessaire car Unity 6 retarde la libération mémoire
        /// </summary>
        private IEnumerator MultiPassCleanupCoroutine()
        {
            LogDebug("🔄 Début nettoyage multi-passes Unity 6");

            for (int pass = 1; pass <= 3; pass++)
            {
                LogDebug($"🔄 Passe {pass}/3");

                // Forcer libération des textures
                Resources.UnloadUnusedAssets();
                yield return new WaitForSecondsRealtime(0.2f);

                // Garbage collection forcé
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect(); // Double call pour Unity 6

                yield return new WaitForSecondsRealtime(0.3f);
            }

            LogDebug("✅ Nettoyage multi-passes terminé");
        }

        /// <summary>
        /// Nettoie spécifiquement le système volcanique
        /// </summary>
        private void CleanupVolcanicSystem()
        {
            var volcanicSystem = FindObjectOfType<LifeStory.Volcanoes.CleanVolcanicSystem>();
            if (volcanicSystem != null)
            {
                volcanicSystem.CleanupSimulation();
                LogDebug("🌋 Système volcanique nettoyé");
            }
            else
            {
                LogDebug("⚠️ Système volcanique non trouvé");
            }
        }

        /// <summary>
        /// Nettoie spécifiquement le système terrain (CRITIQUE)
        /// </summary>
        private void CleanupTerrainSystem()
        {
            var terrainManager = FindObjectOfType<LifeStory.Terrain.TerrainModificationManager>();
            if (terrainManager != null)
            {
                // IMPORTANT : Appel direct de la méthode critique
                terrainManager.ClearAllTerrainModifications();
                LogDebug("🗺️ TerrainModificationManager nettoyé - MÉMOIRE LIBÉRÉE");

                // Force additional cleanup pour les gros tableaux
                Resources.UnloadUnusedAssets();
                System.GC.Collect();
            }
            else
            {
                LogDebug("⚠️ TerrainModificationManager non trouvé");
            }
        }

        /// <summary>
        /// Nettoie les autres systèmes de jeu
        /// </summary>
        private void CleanupOtherGameSystems()
        {
            // Système rifting si disponible
            //var riftingSystem = FindAnyObjectByType<LifeStory.Geology.ContinentalRiftingSystem>();
            //if (riftingSystem != null)
            //{
            //    // Appel cleanup si méthode existe
            //    LogDebug("🌍 Système rifting détecté");
            //}

            // Autres systèmes à ajouter ici selon besoin
        }

        /// <summary>
        /// Nettoyage de tous les systèmes (méthode rapide)
        /// </summary>
        private void CleanupGameSystems()
        {
            CleanupVolcanicSystem();
            CleanupTerrainSystem();
            CleanupOtherGameSystems();
        }

        /// <summary>
        /// Nettoyage Unity agressif spécialement pour Unity 6
        /// </summary>
        private void PerformAggressiveUnityCleanup()
        {
            LogDebug("🧹 Début nettoyage Unity agressif");

            // 1. Libération des ressources non utilisées
            Resources.UnloadUnusedAssets();

            // 2. Garbage collection multiple pour Unity 6
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect(); // Second call nécessaire Unity 6

            // 3. Forcer flush des buffers graphiques
            GL.Flush();

            LogDebug("✅ Nettoyage Unity agressif terminé");
        }

        /// <summary>
        /// Méthode publique simplifiée pour GameManager
        /// </summary>
        public void CleanupAfterSimulation()
        {
            LogDebug("🎯 Nettoyage post-simulation demandé");
            PerformCompleteMemoryCleanup();
        }

        /// <summary>
        /// Obtient des statistiques mémoire pour diagnostic
        /// </summary>
        [ContextMenu("Show Memory Statistics")]
        public void ShowMemoryStatistics()
        {
            LogDebug("📊 === STATISTIQUES MÉMOIRE ===");
            //LogDebug($"💾 Mémoire Unity allouée: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(0) / 1024 / 1024:F1} MB");
            //LogDebug($"💾 Mémoire Unity réservée: {UnityEngine.Profiling.Profiler.GetTotalReservedMemory(0) / 1024 / 1024:F1} MB");
            LogDebug($"🗑️ GC Heap: {System.GC.GetTotalMemory(false) / 1024 / 1024:F1} MB");
        }

        /// <summary>
        /// Test de nettoyage forcé
        /// </summary>
        [ContextMenu("Force Immediate Cleanup")]
        public void ForceImmediateCleanup()
        {
            ExecuteImmediateCleanup();
        }

        private void LogDebug(string message)
        {
            if (showCleanupLogs)
            {
                Debug.Log($"[MemoryCleanupManager] {message}");
            }
        }
    }
}