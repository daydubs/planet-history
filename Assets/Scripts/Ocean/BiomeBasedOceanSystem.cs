// BiomeBasedOceanSystem.cs - Océan synchronisé avec l'évolution biologique
using UnityEngine;
using LifeStory.Core;
using LifeStory.Generation;
using LifeStory.Biomes;

namespace LifeStory.Ocean
{
    /// <summary>
    /// Système océan basé sur l'évolution biologique du CleanBiomeSystem
    /// Beaucoup plus logique et stable que l'analyse terrain
    /// </summary>
    public class BiomeBasedOceanSystem : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("Si vide, utilise ce GameObject comme OceanSphere")]
        [SerializeField] private Transform oceanSphere;
        [SerializeField] private PlanetGenerator planetGenerator;
        [SerializeField] private CleanBiomeSystem biomeSystem;

        [Header("Configuration Océan")]
        [SerializeField] private float minOceanLevel = 0.95f;         // Commence à 95% de la planète de base
        [SerializeField] private float maxOceanLevel = 1.05f;         // Finit à mi-hauteur du relief continental
        [SerializeField] private float oceanAppearanceThreshold = 0.05f; // Océan apparaît à 5% d'évolution

        [Header("Évolution Océan")]
        [SerializeField] private AnimationCurve oceanEvolutionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private bool useCoastalInfluence = true;     // Utiliser aussi coastal progress
        [SerializeField] private float coastalInfluenceWeight = 0.3f; // Poids de l'influence côtière

        [Header("Mise à Jour")]
        [SerializeField] private bool autoUpdate = true;
        [SerializeField] private float updateInterval = 1f;          // Plus fréquent car plus simple
        [SerializeField] private float significantChangeThreshold = 0.02f; // 2% de changement minimum

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showEvolutionData = true;

        private float currentOceanLevel;
        private float lastOceanProgress = -1f;
        private float lastCoastalProgress = -1f;
        private bool isInitialized = false;
        private bool oceanIsVisible = false;

        private void Start()
        {
            StartCoroutine(InitializeSystem());
        }

        private System.Collections.IEnumerator InitializeSystem()
        {
            LogDebug("⏳ Initialisation système océan basé biomes...");
            yield return new WaitForSeconds(2f); // Laisser OceanSphere créer la sphère

            int attempts = 0;
            while (attempts < 5 && !FindReferences())
            {
                LogDebug($"🔍 Tentative {attempts + 1}/5 - Recherche références...");
                yield return new WaitForSeconds(1f);
                attempts++;
            }

            if (ValidateReferences())
            {
                // 🔪 CASTRATION IMMÉDIATE : Forcer scale à 0 pour contrer OceanSphere
                ForceOceanReset();

                currentOceanLevel = 0f;

                if (autoUpdate)
                {
                    StartCoroutine(PeriodicUpdate());
                }

                isInitialized = true;
                LogDebug("✅ BiomeBasedOceanSystem initialisé - OceanSphere castré avec succès !");
            }
            else
            {
                LogDebug("❌ Impossible d'initialiser - références manquantes");
                ShowMissingReferences();
            }
        }

        /// <summary>
        /// 🔪 Castration forcée d'OceanSphere - Remet le scale à 0
        /// </summary>
        private void ForceOceanReset()
        {
            if (oceanSphere != null)
            {
                Vector3 originalScale = oceanSphere.localScale;
                oceanSphere.localScale = Vector3.zero;
                oceanIsVisible = false;

                LogDebug($"🔪 CASTRATION RÉUSSIE ! Scale forcé de ({originalScale.x:F1},{originalScale.y:F1},{originalScale.z:F1}) → (0,0,0)");
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

            // 🔧 CORRECTION MAJEURE : Chercher l'enfant sphère si c'est un parent vide
            if (oceanSphere.childCount > 0)
            {
                // Chercher un enfant nommé "OceanSphere" ou avec MeshRenderer
                for (int i = 0; i < oceanSphere.childCount; i++)
                {
                    Transform child = oceanSphere.GetChild(i);
                    if (child.name.Contains("OceanSphere") || child.GetComponent<MeshRenderer>() != null)
                    {
                        oceanSphere = child; // UTILISER L'ENFANT au lieu du parent !
                        LogDebug($"🎯 VRAIE sphère trouvée : {child.name} (enfant)");
                        break;
                    }
                }
            }

            // PlanetGenerator
            if (planetGenerator == null)
                planetGenerator = PlanetGenerator.Instance;

            // CleanBiomeSystem - chercher dans la scène
            if (biomeSystem == null)
            {
                biomeSystem = FindAnyObjectByType<CleanBiomeSystem>();
                if (biomeSystem != null)
                {
                    LogDebug($"🎨 CleanBiomeSystem trouvé: {biomeSystem.gameObject.name}");
                }
            }

            return ValidateReferences();
        }

        private bool ValidateReferences()
        {
            return oceanSphere != null && planetGenerator != null && biomeSystem != null;
        }

        private void ShowMissingReferences()
        {
            LogDebug("❌ RÉFÉRENCES MANQUANTES:");
            LogDebug($"   OceanSphere: {(oceanSphere != null ? "✓" : "❌ NULL")}");
            LogDebug($"   PlanetGenerator: {(planetGenerator != null ? "✓" : "❌ NULL")}");
            LogDebug($"   CleanBiomeSystem: {(biomeSystem != null ? "✓" : "❌ NULL")}");
        }

        private System.Collections.IEnumerator PeriodicUpdate()
        {
            while (true)
            {
                yield return new WaitForSeconds(updateInterval);

                if (HasEvolutionChanged())
                {
                    UpdateOceanBasedOnEvolution();
                }
            }
        }

        private bool HasEvolutionChanged()
        {
            if (biomeSystem == null) return false;

            float currentOceanProgress = biomeSystem.OceanProgress;
            float currentCoastalProgress = biomeSystem.CoastalProgress;

            bool changed = Mathf.Abs(currentOceanProgress - lastOceanProgress) > 0.01f ||
                          Mathf.Abs(currentCoastalProgress - lastCoastalProgress) > 0.01f;

            if (changed)
            {
                LogDebug($"🔄 Évolution détectée - Océan: {lastOceanProgress:P0} → {currentOceanProgress:P0}");
                lastOceanProgress = currentOceanProgress;
                lastCoastalProgress = currentCoastalProgress;
            }

            return changed;
        }

        private void UpdateOceanBasedOnEvolution()
        {
            if (!isInitialized || biomeSystem == null) return;

            float oceanProgress = biomeSystem.OceanProgress;
            float coastalProgress = biomeSystem.CoastalProgress;

            // Calculer l'influence combinée
            float combinedProgress = oceanProgress;
            if (useCoastalInfluence)
            {
                combinedProgress = oceanProgress * (1f - coastalInfluenceWeight) +
                                 coastalProgress * coastalInfluenceWeight;
            }

            // Appliquer la courbe d'évolution
            float curveProgress = oceanEvolutionCurve.Evaluate(combinedProgress);

            // Calculer le niveau d'océan target
            float targetOceanLevel;

            if (combinedProgress < oceanAppearanceThreshold)
            {
                // Pas d'océan encore
                targetOceanLevel = 0f;
            }
            else
            {
                // Interpoler entre min et max selon l'évolution
                float normalizedProgress = (combinedProgress - oceanAppearanceThreshold) /
                                         (1f - oceanAppearanceThreshold);
                targetOceanLevel = Mathf.Lerp(minOceanLevel, maxOceanLevel, curveProgress);
            }

            // Appliquer si changement significatif
            if (Mathf.Abs(targetOceanLevel - currentOceanLevel) > significantChangeThreshold)
            {
                ApplyOceanLevel(targetOceanLevel, oceanProgress, coastalProgress);
            }
        }

        private void ApplyOceanLevel(float newLevel, float oceanProg, float coastalProg)
        {
            currentOceanLevel = newLevel;

            if (newLevel <= 0f)
            {
                // Cacher océan
                if (oceanIsVisible)
                {
                    HideOcean();
                    LogDebug("🌊 Océan caché - évolution insuffisante");
                }
            }
            else
            {
                // Montrer et dimensionner océan
                if (!oceanIsVisible)
                {
                    ShowOcean();
                    LogDebug($"🌊 Océan apparaît ! Évolution: {oceanProg:P0}");
                }

                UpdateOceanSize(newLevel);

                if (showEvolutionData)
                {
                    LogDebug($"🌊 Océan ajusté - Niveau: {newLevel:F3} | Évolution O:{oceanProg:P0} C:{coastalProg:P0}");
                }
            }
        }

        private void UpdateOceanSize(float level)
        {
            if (oceanSphere != null && planetGenerator != null)
            {
                float planetRadius = planetGenerator.PlanetRadius;

                // CORRECTION MAJEURE : Calculer la taille proportionnelle à la planète
                float oceanRadius = planetRadius * level;

                // L'océan doit être légèrement plus petit que la planète
                // Pour un niveau de 0.45 (45%), l'océan fait 4.5 unités de rayon
                Vector3 newScale = Vector3.one * (oceanRadius * 2f); // Diamètre pour Unity sphere

                oceanSphere.localScale = newScale;

                LogDebug($"🌊 OCÉAN CORRIGÉ - Planète: {planetRadius} | Niveau: {level:F3} | Rayon océan: {oceanRadius:F2} | Scale: {newScale.x:F1}");
            }
        }

        private void HideOcean()
        {
            if (oceanSphere != null)
            {
                oceanSphere.localScale = Vector3.zero; // Complètement caché
                oceanIsVisible = false;
                LogDebug("🌊 Océan caché (scale = 0)");
            }
        }

        private void ShowOcean()
        {
            oceanIsVisible = true;
            // La taille sera mise à jour par UpdateOceanSize()
        }

        // === MÉTHODES DEBUG ===

        [ContextMenu("🔍 Show Evolution Status")]
        public void ShowEvolutionStatus()
        {
            if (biomeSystem == null)
            {
                LogDebug("❌ CleanBiomeSystem non disponible");
                return;
            }

            LogDebug("=== ÉTAT ÉVOLUTION BIOLOGIQUE ===");
            LogDebug($"Océan: {biomeSystem.OceanProgress:P1}");
            LogDebug($"Côtier: {biomeSystem.CoastalProgress:P1}");

            float combinedProgress = biomeSystem.OceanProgress;
            if (useCoastalInfluence)
            {
                combinedProgress = biomeSystem.OceanProgress * (1f - coastalInfluenceWeight) +
                                 biomeSystem.CoastalProgress * coastalInfluenceWeight;
            }

            LogDebug($"Progrès combiné: {combinedProgress:P1}");
            LogDebug($"Seuil apparition: {oceanAppearanceThreshold:P1}");
            LogDebug($"Océan visible: {(combinedProgress >= oceanAppearanceThreshold ? "OUI" : "NON")}");

            if (combinedProgress >= oceanAppearanceThreshold)
            {
                float normalizedProgress = (combinedProgress - oceanAppearanceThreshold) /
                                         (1f - oceanAppearanceThreshold);
                float curveProgress = oceanEvolutionCurve.Evaluate(normalizedProgress);
                float targetLevel = Mathf.Lerp(minOceanLevel, maxOceanLevel, curveProgress);

                LogDebug($"Niveau océan calculé: {targetLevel:F3}");
                LogDebug($"Niveau océan actuel: {currentOceanLevel:F3}");
            }
        }

        [ContextMenu("🔧 Force Ocean Update")]
        public void ForceOceanUpdate()
        {
            LogDebug("🔧 Mise à jour océan forcée");
            UpdateOceanBasedOnEvolution();
        }

        [ContextMenu("🌊 Test Ocean Appearance")]
        public void TestOceanAppearance()
        {
            if (!isInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            LogDebug("🧪 Test apparition océan...");

            // Simuler différents niveaux d'évolution
            float[] testLevels = { 0f, 0.05f, 0.25f, 0.5f, 0.75f, 1f };

            foreach (float testLevel in testLevels)
            {
                float targetLevel;

                if (testLevel < oceanAppearanceThreshold)
                {
                    targetLevel = 0f;
                }
                else
                {
                    float normalizedProgress = (testLevel - oceanAppearanceThreshold) /
                                             (1f - oceanAppearanceThreshold);
                    float curveProgress = oceanEvolutionCurve.Evaluate(normalizedProgress);
                    targetLevel = Mathf.Lerp(minOceanLevel, maxOceanLevel, curveProgress);
                }

                LogDebug($"Évolution {testLevel:P0} → Océan niveau {targetLevel:F3}");
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[BiomeOcean] {message}");
        }

        // === PROPRIÉTÉS PUBLIQUES ===

        public bool IsInitialized => isInitialized;
        public float CurrentOceanLevel => currentOceanLevel;
        public bool OceanIsVisible => oceanIsVisible;
        public float OceanProgress => biomeSystem?.OceanProgress ?? 0f;
        public float CoastalProgress => biomeSystem?.CoastalProgress ?? 0f;
    }
}