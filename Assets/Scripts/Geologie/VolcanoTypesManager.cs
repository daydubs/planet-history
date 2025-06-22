// VolcanoTypesManager.cs - REFACTORISÉ - Gestionnaire des types de volcans
// ÉTAPE 2A : Utilisation de VolcanicConfiguration (Data Only)
using UnityEngine;
using System.Collections.Generic;
using LifeStory.Core;
using LifeStory.Volcanoes;

namespace LifeStory.Geology
{
    /// <summary>
    /// Gestionnaire centralisé des types de volcans - REFACTORISÉ
    /// Responsabilité : Logique de sélection et gestion cache
    /// Configuration : Déléguée à VolcanicConfiguration
    /// </summary>
    public class VolcanoTypesManager : MonoBehaviour
    {
        [Header("🎯 Configuration Source")]
        [SerializeField] private VolcanicConfiguration volcanicConfig;

        [Header("🔧 Paramètres Manager")]
        [SerializeField] private bool autoInitializeTypes = true;
        [SerializeField] private bool enableDebugLogs = true;

        // === ÉTAT SYSTÈME (Local au Manager) ===
        private bool isInitialized = false;
        private Dictionary<VolcanoType, VolcanoTypeData> typeDataCache;

        //bool canAppearAtTemperature = data.CanAppearAtCoreTemperature(temperature);

        // === SINGLETON ===
        public static VolcanoTypesManager Instance { get; private set; }

        // === EVENTS ===
        public static System.Action<VolcanoTypesManager> OnTypesInitialized;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LogDebug("🌋 VolcanoTypesManager refactorisé initialisé");
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (autoInitializeTypes)
            {
                InitializeVolcanoTypes();
            }
        }

        private void Start()
        {
            if (!isInitialized && autoInitializeTypes)
            {
                StartCoroutine(DelayedInitialization());
            }

            // 🔄 Charger collection au démarrage si configuré
            if (volcanicConfig != null && volcanicConfig.UsePresetSystem &&
                volcanicConfig.AutoLoadCollectionOnStart && volcanicConfig.ActiveCollection != null)
            {
                StartCoroutine(DelayedPresetLoading());
            }
        }
       

        private System.Collections.IEnumerator DelayedPresetLoading()
        {
            yield return new WaitForSeconds(0.2f);

            if (volcanicConfig?.ActiveCollection != null)
            {
                LoadPresetCollection(volcanicConfig.ActiveCollection);
                LogDebug("🎮 Collection chargée automatiquement au démarrage");
            }
        }

        private System.Collections.IEnumerator DelayedInitialization()
        {
            yield return new WaitForSeconds(0.1f);

            if (!isInitialized)
            {
                InitializeVolcanoTypes();
            }
        }

        /// <summary>
        /// Initialiser le système de types de volcans
        /// </summary>
        [ContextMenu("Initialize Volcano Types")]
        public void InitializeVolcanoTypes()
        {
            LogDebug("🔧 === INITIALISATION TYPES DE VOLCANS (avec VolcanicConfiguration) ===");

            // 🚨 VÉRIFICATION CONFIGURATION
            if (volcanicConfig == null)
            {
                LogDebug("❌ ERREUR CRITIQUE: VolcanicConfiguration non assignée !");
                return;
            }

            // Validation configuration
            if (!volcanicConfig.ValidateConfiguration(out string errorMessage))
            {
                LogDebug($"❌ Configuration invalide: {errorMessage}");
                return;
            }

            var volcanoTypes = volcanicConfig.GetAllVolcanoTypes();
            if (volcanoTypes == null || volcanoTypes.Length == 0 || AllTypesEmpty(volcanoTypes))
            {
                LogDebug("⚠️ Aucun type de volcan configuré");
                return;
            }

            ValidateConfigurationTypes(volcanoTypes);
            BuildTypeCache(volcanoTypes);

            // === AUTO-ASSIGNMENT CONDITIONNEL ===
            if (volcanicConfig.EnableAutoAssignment)
            {
                AutoAssignPrefabs(volcanoTypes);
            }
            else
            {
                LogDebug("🔧 Auto-assignment désactivé - Configuration manuelle requise");
            }

            // === PHASE 1 : FORCER DÉSACTIVATION TYPES FUTURS ===
            if (volcanicConfig.Phase1Only)
            {
                ForcePhase1Configuration(volcanoTypes);
            }

            isInitialized = true;
            OnTypesInitialized?.Invoke(this);

            LogDebug($"✅ Types initialisés - {GetAvailableTypesCount()}/{volcanoTypes.Length} types disponibles");
        }

        /// <summary>
        /// Valider la configuration et corriger les erreurs
        /// </summary>
        private void ValidateConfigurationTypes(VolcanoTypeData[] volcanoTypes)
        {
            for (int i = 0; i < volcanoTypes.Length; i++)
            {
                var data = volcanoTypes[i];
                if (data == null) continue;

                // ✅ NOUVEAU : Ne pas corriger les températures si minTemperature = 0 (nouveau modèle)
                if (data.minTemperature > 0f && data.minTemperature > data.maxTemperature)
                {
                    float temp = data.minTemperature;
                    data.minTemperature = data.maxTemperature;
                    data.maxTemperature = temp;
                    LogDebug($"⚠️ Températures legacy corrigées pour {data.type}");
                }

                // ✅ NOUVEAU : Validation optimalTemperature selon le modèle utilisé
                if (data.minTemperature <= 0f)
                {
                    // NOUVEAU MODÈLE : optimalTemperature peut être n'importe où
                    LogDebug($"✅ {data.type} utilise le nouveau modèle d'activation (maxTemp: {data.maxTemperature:F0}°C, optimal: {data.optimalTemperature:F0}°C)");
                }
                else
                {
                    // ANCIEN MODÈLE : optimalTemperature doit être dans la plage min-max
                    data.optimalTemperature = Mathf.Clamp(data.optimalTemperature,
                                                         data.minTemperature,
                                                         data.maxTemperature);
                    LogDebug($"⚠️ {data.type} utilise l'ancien modèle - optimalTemperature clamped");
                }

                // Valider les noms d'affichage
                if (string.IsNullOrEmpty(data.displayName))
                {
                    data.displayName = data.type.GetDisplayName();
                }

                if (string.IsNullOrEmpty(data.description))
                {
                    data.description = data.type.GetDescription();
                }
            }
        }

        /// <summary>
        /// Vérifier si ce type peut apparaître à la température donnée du noyau
        /// NOUVELLE MÉTHODE pour le nouveau modèle d'activation
        /// </summary>
        /// 


        //bool canAppearAtTemperature = data.CanAppearAtCoreTemperature(temperature);
        //public bool CanAppearAtCoreTemperature(float coreTemperature)
        //{
        //    // Si minTemperature = 0, utiliser le nouveau modèle
        //    if (volcanicConfig.MinVolcanicTemp <= 0f)
        //    {
        //        return coreTemperature <= volcanicConfig.MaxVolcanicTemp;
        //    }
        //    else
        //    {
        //        // Ancien modèle pour compatibilité
        //        return coreTemperature >= volcanicConfig.MinVolcanicTemp && coreTemperature <= volcanicConfig.MaxVolcanicTemp;
        //    }
        //}

        public bool IsInIntenseActivity(float coreTemperature)
        {
            // Période d'activité intense = proche de optimalTemperature (±100°C)
            float difference = Mathf.Abs(coreTemperature - volcanicConfig.OptimalVolcanicTemp);
            return difference <= 100f;
        }

       



        /// <summary>
        /// Construire le cache pour accès rapide
        /// </summary>
        private void BuildTypeCache(VolcanoTypeData[] volcanoTypes)
        {
            typeDataCache = new Dictionary<VolcanoType, VolcanoTypeData>();

            foreach (var data in volcanoTypes)
            {
                if (data != null)
                {
                    typeDataCache[data.type] = data;
                }
            }

            LogDebug($"🗃️ Cache construit avec {typeDataCache.Count} types");
        }

        /// <summary>
        /// Chercher et assigner automatiquement les prefabs
        /// </summary>
        private void AutoAssignPrefabs(VolcanoTypeData[] volcanoTypes)
        {
            LogDebug("🔍 Recherche automatique des prefabs...");

            int foundPrefabs = 0;

            foreach (var data in volcanoTypes)
            {
                if (data == null) continue;

                // Ne pas écraser les prefabs déjà assignés
                if (data.prefab != null)
                {
                    LogDebug($"✅ {data.type}: Prefab déjà assigné ({data.prefab.name})");
                    foundPrefabs++;
                    continue;
                }

                // Chercher le prefab par nom
                string prefabName = data.type.GetPrefabName();
                if (!string.IsNullOrEmpty(prefabName))
                {
                    GameObject foundPrefab = Resources.Load<GameObject>(prefabName);
                    if (foundPrefab != null)
                    {
                        data.prefab = foundPrefab;
                        foundPrefabs++;
                        LogDebug($"✅ {data.type}: Prefab trouvé ({prefabName})");
                    }
                    else
                    {
                        LogDebug($"❌ {data.type}: Prefab non trouvé ({prefabName}) - Sera ignoré en Phase 1");
                    }
                }
            }

            LogDebug($"🎯 Prefabs trouvés: {foundPrefabs}/{volcanoTypes.Length}");
        }

        private VolcanoTypeData SelectWeightedRandom(List<VolcanoTypeData> candidates, float temperature)
        {
            if (candidates.Count == 1)
                return candidates[0];

            float totalWeight = 0f;
            var weights = new float[candidates.Count];

            LogDebug($"📊 Calcul poids pour {candidates.Count} candidats à {temperature:F0}°C:");

            // Calculer les poids
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];

                // ✅ NOUVEAU : Facteur de proximité à la température optimale (période d'activité intense)
                float tempDifference = Mathf.Abs(temperature - candidate.optimalTemperature);
                float tempFactor = 1f;

                if (volcanicConfig.FavoriteOptimalTemperatures)
                {
                    // Plus proche de optimal = meilleur score (avec plateau de tolérance)
                    float tolerance = 200f; // Tolérance de ±200°C autour de l'optimal
                    if (tempDifference <= tolerance)
                    {
                        tempFactor = 1f; // Score parfait dans la zone de tolérance
                    }
                    else
                    {
                        // Dégradation progressive au-delà de la tolérance
                        tempFactor = Mathf.Max(0.1f, 1f - ((tempDifference - tolerance) / 1000f));
                    }
                }

                // Facteur rareté (rareté faible = plus probable)
                float rarityFactor = 1f - candidate.rarity;

                // ✅ NOUVEAU : Bonus d'activité si proche de la période optimale (2339°C = océans)
                float activityBonus = 1f;
                if (Mathf.Abs(temperature - candidate.optimalTemperature) <= 100f)
                {
                    activityBonus = 2f; // Double les chances pendant la période intense
                    LogDebug($"  🔥 {candidate.type}: PÉRIODE INTENSE activée (temp proche de {candidate.optimalTemperature:F0}°C)");
                }

                // Poids final
                float weight = rarityFactor * tempFactor * activityBonus;
                weights[i] = weight;
                totalWeight += weight;

                LogDebug($"  📊 {candidate.type}: rareté={rarityFactor:F2} × temp={tempFactor:F2} × bonus={activityBonus:F1} = {weight:F3}");
            }

            // Sélection aléatoire pondérée
            float randomValue = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += weights[i];
                if (randomValue <= cumulative)
                {
                    LogDebug($"🎯 Sélectionné: {candidates[i].type} (probabilité: {(weights[i] / totalWeight):P1})");
                    return candidates[i];
                }
            }

            // Fallback (ne devrait jamais arriver)
            LogDebug($"⚠️ Fallback vers premier candidat: {candidates[0].type}");
            return candidates[0];
        }

        private void ForcePhase1Configuration(VolcanoTypeData[] volcanoTypes)
        {
            LogDebug("🎮 FORÇAGE CONFIGURATION PHASE 1");

            int disabledCount = 0;

            foreach (var data in volcanoTypes)
            {
                if (data == null) continue;

                // Garder seulement Shield et Fissure
                if (data.type != VolcanoType.Shield && data.type != VolcanoType.Fissure)
                {
                    data.prefab = null;  // FORCER à null
                    disabledCount++;
                    LogDebug($"🚫 {data.type}: Désactivé pour Phase 1");
                }
            }

            LogDebug($"🎯 Phase 1: {disabledCount} types désactivés, seuls Shield + Fissure actifs");
        }

        /// <summary>
        /// Choisir intelligemment le type de volcan selon les conditions
        /// UTILISE VolcanicConfiguration pour les paramètres de sélection
        /// </summary>
        public VolcanoType ChooseVolcanoType(float temperature, Vector3 position)
        {
            if (!isInitialized)
            {
                LogDebug("⚠️ ChooseVolcanoType appelé avant initialisation");
                return VolcanoType.Shield; // Fallback sûr
            }

            if (volcanicConfig == null)
            {
                LogDebug("❌ VolcanicConfiguration manquante pour sélection");
                return VolcanoType.Shield;
            }

            // Filtrer les types disponibles (avec prefab) et compatibles température
            List<VolcanoTypeData> candidates = GetValidCandidates(temperature);

            if (candidates.Count == 0)
            {
                LogDebug($"⚠️ Aucun type disponible pour {temperature:F0}°C - Fallback Shield");
                return VolcanoType.Shield;
            }

            // Sélection pondérée intelligente
            VolcanoTypeData chosen = SelectWeightedRandom(candidates, temperature);

            LogDebug($"🎯 Type choisi: {chosen.type} pour {temperature:F0}°C (sur {candidates.Count} candidats)");
            return chosen.type;
        }

        /// <summary>
        /// Obtenir les candidats valides pour une température donnée
        /// </summary>
        private List<VolcanoTypeData> GetValidCandidates(float temperature)
        {
            List<VolcanoTypeData> candidates = new List<VolcanoTypeData>();
            var volcanoTypes = volcanicConfig.GetAllVolcanoTypes();

            foreach (var data in volcanoTypes)
            {
                if (data == null) continue;

                // ✅ Déplacer cette ligne ICI, à l'intérieur de la boucle
                bool canAppearAtTemperature = data.CanAppearAtCoreTemperature(temperature);

                bool prefabAvailable = data.prefab != null;

                if (canAppearAtTemperature && prefabAvailable)
                {
                    candidates.Add(data);
                }
            }

            return candidates;
        }


        /// <summary>
        /// Sélection pondérée basée sur température optimale et rareté
        /// UTILISE les paramètres de VolcanicConfiguration
        /// </summary>


        /// <summary>
        /// Obtenir les données d'un type spécifique
        /// </summary>
        public VolcanoTypeData GetVolcanoTypeData(VolcanoType type)
        {
            if (!isInitialized)
            {
                LogDebug("⚠️ GetVolcanoTypeData appelé avant initialisation");
                return null;
            }

            if (typeDataCache != null && typeDataCache.ContainsKey(type))
            {
                return typeDataCache[type];
            }

            LogDebug($"❌ Type {type} non trouvé dans le cache");
            return null;
        }

        /// <summary>
        /// Obtenir le nombre de types disponibles (avec prefabs)
        /// </summary>
        public int GetAvailableTypesCount()
        {
            if (volcanicConfig == null) return 0;
            return volcanicConfig.CountAvailableTypes();
        }

        /// <summary>
        /// Vérifier si tous les types sont vides (pour détection première initialisation)
        /// </summary>
        private bool AllTypesEmpty(VolcanoTypeData[] volcanoTypes)
        {
            foreach (var data in volcanoTypes)
            {
                if (data != null)
                    return false;
            }
            return true;
        }

        // === MÉTHODES PRESET SYSTEM ===

        public void LoadPresetCollection(VolcanicPresetCollection collection)
        {
            if (collection == null)
            {
                LogDebug("❌ Collection nulle - impossible à charger");
                return;
            }

            LogDebug($"🔄 Chargement collection: {collection.collectionName}");

            // ⚠️ ATTENTION: Les presets modifient directement la VolcanicConfiguration
            // Il faudra adapter cette logique dans une prochaine étape
            collection.ApplyToManager(this);

            // Reconstruire le cache
            if (volcanicConfig != null)
            {
                BuildTypeCache(volcanicConfig.GetAllVolcanoTypes());
            }

            LogDebug($"✅ Collection '{collection.collectionName}' chargée avec succès");
        }

        public void SaveToPresetCollection(VolcanicPresetCollection collection)
        {
            if (collection == null)
            {
                LogDebug("❌ Collection nulle - impossible de sauvegarder");
                return;
            }

            collection.SaveFromManager(this);
            LogDebug($"💾 Configuration sauvegardée dans '{collection.collectionName}'");
        }

        // ⚠️ MÉTHODE TEMPORAIRE - À adapter dans prochaine étape
        public void SetVolcanoTypesFromPresets(VolcanoTypeData[] newTypes)
        {
            LogDebug("⚠️ SetVolcanoTypesFromPresets - Méthode temporaire, à refactoriser");

            if (newTypes == null || newTypes.Length == 0)
            {
                LogDebug("❌ Array de types null ou vide");
                return;
            }

            // Reconstruire le cache avec les nouveaux types
            BuildTypeCache(newTypes);
            isInitialized = true;

            LogDebug($"🔄 Types de volcans mis à jour depuis presets: {newTypes.Length} types");
        }

        public VolcanoTypeData[] GetAllVolcanoTypes()
        {
            return volcanicConfig?.GetAllVolcanoTypes();
        }

        // === MÉTHODES DEBUG ===

        [ContextMenu("Show Configuration Status")]
        public void ShowConfigurationStatus()
        {
            LogDebug("🔍 === STATUT CONFIGURATION ===");

            if (volcanicConfig == null)
            {
                LogDebug("❌ VolcanicConfiguration NON ASSIGNÉE");
                return;
            }

            LogDebug($"✅ Configuration assignée");
            LogDebug($"📊 Types configurés: {volcanicConfig.TotalVolcanoTypes}");
            LogDebug($"📊 Types disponibles: {volcanicConfig.CountAvailableTypes()}");
            LogDebug($"🌡️ Plage température: {volcanicConfig.MinVolcanicTemp:F0}-{volcanicConfig.MaxVolcanicTemp:F0}°C");
            LogDebug($"🎮 Système presets: {volcanicConfig.UsePresetSystem}");
            LogDebug($"🔧 Phase 1 only: {volcanicConfig.Phase1Only}");
            LogDebug($"⚡ Max volcans: {volcanicConfig.MaxVolcanoes}");

            if (volcanicConfig.ValidateConfiguration(out string error))
            {
                LogDebug("✅ Configuration valide");
            }
            else
            {
                LogDebug($"❌ Configuration invalide: {error}");
            }
        }

        [ContextMenu("List Available Types")]
        public void ListAvailableTypes()
        {
            LogDebug("=== TYPES DE VOLCANS DISPONIBLES ===");

            if (!isInitialized)
            {
                LogDebug("❌ Système non initialisé");
                return;
            }

            if (volcanicConfig == null)
            {
                LogDebug("❌ VolcanicConfiguration manquante");
                return;
            }

            var volcanoTypes = volcanicConfig.GetAllVolcanoTypes();
            foreach (var data in volcanoTypes)
            {
                if (data == null) continue;

                string status = data.prefab != null ? "✅ DISPONIBLE" : "❌ PREFAB MANQUANT";
                LogDebug($"{status} {data.displayName} ({data.type})");
                LogDebug($"   📝 {data.description}");
                LogDebug($"   🌡️ Température: {data.minTemperature:F0}-{data.maxTemperature:F0}°C (optimal: {data.optimalTemperature:F0}°C)");
                LogDebug($"   💥 Explosivité: {data.explosivity:P0} | Gaz: {data.gasEmission:P0} | Durée: {data.eruptionDuration:F1}x");
                LogDebug($"   🎲 Rareté: {data.rarity:P0}");
            }

            LogDebug($"📊 Total: {GetAvailableTypesCount()}/{volcanoTypes.Length} types prêts pour Phase 1");
        }

        [ContextMenu("Test Type Selection")]
        public void TestTypeSelection()
        {
            if (GameManager.Instance == null)
            {
                LogDebug("❌ GameManager non disponible pour test");
                return;
            }

            if (volcanicConfig == null)
            {
                LogDebug("❌ VolcanicConfiguration manquante pour test");
                return;
            }

            // === UTILISER TEMPÉRATURE NOYAU ===
            float coreTemp = GameManager.Instance.CoreTemperature;
            float surfaceTemp = GameManager.Instance.SurfaceTemperature;

            LogDebug($"🧪 === TEST SÉLECTION ===");
            LogDebug($"🔥 Température NOYAU: {coreTemp:F0}°C");
            LogDebug($"🌡️ Température SURFACE: {surfaceTemp:F0}°C");
            LogDebug($"📊 Test avec température NOYAU...");

            // Vérifier candidats disponibles
            var candidates = GetValidCandidates(coreTemp);
            LogDebug($"🎯 Candidats à {coreTemp:F0}°C: {candidates.Count}");
            foreach (var candidate in candidates)
            {
                LogDebug($"   ✅ {candidate.type} (rareté: {candidate.rarity:P0}, optimal: {candidate.optimalTemperature:F0}°C)");
            }

            if (candidates.Count == 0)
            {
                LogDebug("❌ Aucun candidat disponible - Ajustez les plages de température");
                return;
            }

            // Test 10 sélections
            var results = new Dictionary<VolcanoType, int>();
            for (int i = 0; i < 10; i++)
            {
                VolcanoType chosen = ChooseVolcanoType(coreTemp, Vector3.zero);
                if (results.ContainsKey(chosen))
                    results[chosen]++;
                else
                    results[chosen] = 1;
            }

            // Afficher résultats
            LogDebug($"📊 RÉSULTATS (10 tests à {coreTemp:F0}°C NOYAU):");
            foreach (var kvp in results)
            {
                LogDebug($"   {kvp.Key}: {kvp.Value}/10 ({kvp.Value * 10}%)");
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[VolcanoTypesManager-Refactored] {message}");
            }
        }

        // === GETTERS PUBLICS ===
        public bool IsInitialized => isInitialized;
        public int TotalTypesConfigured => volcanicConfig?.TotalVolcanoTypes ?? 0;
        public VolcanicConfiguration Configuration => volcanicConfig; // ✅ NOUVEAU

        // === CLEANUP ===
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}