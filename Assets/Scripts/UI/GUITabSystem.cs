// GUITabSystem.cs - Version complète avec système de sous-sections et toggle corrigé
using UnityEngine;
using System.Collections.Generic;
using LifeStory.Core;
using LifeStory.Geology;
using LifeStory.Generation;
using LifeStory.Biomes;

public class GUITabSystem : MonoBehaviour
{
    [System.Serializable]
    public class GUITab
    {
        public string tabName;
        public KeyCode shortcutKey;
        public bool isEnabled = true;
        public System.Action<Rect> drawContent;

        public GUITab(string name, KeyCode key, System.Action<Rect> content)
        {
            tabName = name;
            shortcutKey = key;
            drawContent = content;
        }
    }

    public enum GameSubSection
    {
        General,
        Water,
        Atmosphere,
        Life,
        All
    }

    [Header("Tab System Settings")]
    [SerializeField] private bool enableTabSystem = true;
    [SerializeField] private KeyCode toggleSystemKey = KeyCode.Tab;
    [SerializeField] private int currentTabIndex = 0;

    [Header("Visual Settings")]
    [SerializeField] private float tabHeight = 30f;
    [SerializeField] private float tabWidth = 100f;
    [SerializeField] private Vector2 windowPosition = new Vector2(50, 50);
    [SerializeField] private Vector2 windowSize = new Vector2(300, 400);

    [Header("Per-Tab Window Sizes")]
    [SerializeField] private Vector2 gameTabSize = new Vector2(300, 500);
    [SerializeField] private Vector2 volcanicTabSize = new Vector2(300, 350);
    [SerializeField] private Vector2 defaultTabSize = new Vector2(300, 400);

    // Variables d'état
    private GameSubSection currentGameSubSection = GameSubSection.General;

    // Liste des onglets
    private List<GUITab> tabs = new List<GUITab>();

    // Références aux autres systèmes
    private GameManager gameManager;
    //private VolcanicSystem volcanicSystem;
    //private SimpleTemperatureMaterialSystem materialSystem;
    private CleanBiomeSystem biomeSystem; // AJOUTER cette ligne dans les variables de classe

    public static GUITabSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeTabs();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        gameManager = GameManager.Instance;
                
    }

    private void Update()
    {
        // TOUJOURS vérifier le toggle TAB, même si le système est désactivé
        if (Input.GetKeyDown(toggleSystemKey))
        {
            enableTabSystem = !enableTabSystem;
            Debug.Log($"Système onglets: {(enableTabSystem ? "ACTIVÉ" : "DÉSACTIVÉ")}");
        }

        // Les autres inputs seulement si le système est activé
        if (enableTabSystem)
        {
            HandleOtherInputs();
        }
    }

    private void InitializeTabs()
    {
        tabs.Add(new GUITab("Jeu", KeyCode.Keypad1, DrawGameTab));
        tabs.Add(new GUITab("Temps", KeyCode.Keypad2, DrawTimeTab));
        tabs.Add(new GUITab("Planète", KeyCode.Keypad3, DrawPlanetTab));
        tabs.Add(new GUITab("Seuils", KeyCode.Keypad4, DrawThresholdsTab)); // ✅ NOUVEAU ONGLET
    }

    private void DrawThresholdsTab(Rect contentRect)
    {
        if (gameManager == null)
        {
            GUILayout.BeginArea(contentRect);
            GUILayout.Label("GameManager non trouvé");
            GUILayout.EndArea();
            return;
        }

        GUILayout.BeginArea(contentRect);

        GUILayout.Label("=== CALIBRAGE SEUILS TEMPS RÉEL ===", GUI.skin.box);

        GUILayout.Space(10);

        // === SEUILS FORMATION CROÛTE ===
        GUILayout.Label("🌋 FORMATION CROÛTE", GUI.skin.box);

        float crustThreshold = gameManager.CrustFormationThreshold;
        GUILayout.Label($"Seuil formation croûte: {crustThreshold:F0}°C");
        float newCrustThreshold = GUILayout.HorizontalSlider(crustThreshold, 600f, 1200f);
        if (Mathf.Abs(newCrustThreshold - crustThreshold) > 1f)
        {
            gameManager.SetCrustFormationThreshold(newCrustThreshold);
        }

        GUILayout.Space(10);

        // === SEUILS VOLCANS ===
        GUILayout.Label("🌋 ACTIVITÉ VOLCANIQUE", GUI.skin.box);

        float volcMinCore = gameManager.VolcanicMinCoreTemp;
        GUILayout.Label($"Noyau min volcans: {volcMinCore:F0}°C");
        float newVolcMinCore = GUILayout.HorizontalSlider(volcMinCore, 1500f, 3000f);
        if (Mathf.Abs(newVolcMinCore - volcMinCore) > 10f)
        {
            gameManager.SetVolcanicMinCoreTemp(newVolcMinCore);
        }

        float volcMaxCore = gameManager.VolcanicMaxCoreTemp;
        GUILayout.Label($"Noyau max volcans: {volcMaxCore:F0}°C");
        float newVolcMaxCore = GUILayout.HorizontalSlider(volcMaxCore, 3500f, 5000f);
        if (Mathf.Abs(newVolcMaxCore - volcMaxCore) > 10f)
        {
            gameManager.SetVolcanicMaxCoreTemp(newVolcMaxCore);
        }

        GUILayout.Space(10);

        // === SEUILS TECTONIQUE ===
        GUILayout.Label("🏔️ ACTIVITÉ TECTONIQUE", GUI.skin.box);

        float tectonicMinCore = gameManager.TectonicMinCoreTemp;
        GUILayout.Label($"Noyau min tectonique: {tectonicMinCore:F0}°C");
        float newTectonicMinCore = GUILayout.HorizontalSlider(tectonicMinCore, 1800f, 3500f);
        if (Mathf.Abs(newTectonicMinCore - tectonicMinCore) > 10f)
        {
            gameManager.SetTectonicMinCoreTemp(newTectonicMinCore);
        }

        GUILayout.Space(10);

        // === SEUILS VIE ===
        GUILayout.Label("🌱 CONDITIONS VIE", GUI.skin.box);

        float bioThreshold = gameManager.BiologicalLifeThreshold;
        GUILayout.Label($"Température max vie: {bioThreshold:F0}°C");
        float newBioThreshold = GUILayout.HorizontalSlider(bioThreshold, 15f, 80f);
        if (Mathf.Abs(newBioThreshold - bioThreshold) > 1f)
        {
            gameManager.SetBiologicalLifeThreshold(newBioThreshold);
        }

        GUILayout.Space(10);

        // === DURÉES REFROIDISSEMENT ===
        GUILayout.Label("⏱️ DURÉES REFROIDISSEMENT", GUI.skin.box);

        float surfaceDuration = gameManager.SurfaceCoolingDuration;
        GUILayout.Label($"Durée refroidissement surface: {surfaceDuration:F0} Ma");
        float newSurfaceDuration = GUILayout.HorizontalSlider(surfaceDuration, 500f, 2000f);
        if (Mathf.Abs(newSurfaceDuration - surfaceDuration) > 10f)
        {
            gameManager.SetSurfaceCoolingDuration(newSurfaceDuration);
        }

        float coreDuration = gameManager.CoreCoolingDuration;
        GUILayout.Label($"Durée refroidissement noyau: {coreDuration:F0} Ma");
        float newCoreDuration = GUILayout.HorizontalSlider(coreDuration, 800f, 2500f);
        if (Mathf.Abs(newCoreDuration - coreDuration) > 10f)
        {
            gameManager.SetCoreCoolingDuration(newCoreDuration);
        }

        GUILayout.Space(10);

        // === STATUT TEMPS RÉEL ===
        GUILayout.Label("📊 STATUT TEMPS RÉEL", GUI.skin.box);

        GUI.color = gameManager.HasStableCrust ? Color.green : Color.red;
        GUILayout.Label($"Croûte: {(gameManager.HasStableCrust ? "✅ FORMÉE" : "🔥 LIQUIDE")}");

        GUI.color = gameManager.IsVolcanicActivityPossible() ? Color.green : Color.red;
        GUILayout.Label($"Volcans: {(gameManager.IsVolcanicActivityPossible() ? "✅ ACTIFS" : "🚫 INACTIFS")}");

        GUI.color = gameManager.IsTectonicActivityPossible() ? Color.green : Color.red;
        GUILayout.Label($"Tectonique: {(gameManager.IsTectonicActivityPossible() ? "✅ ACTIVE" : "🚫 INACTIVE")}");

        GUI.color = Color.white;

        GUILayout.Space(10);

        // === BOUTONS UTILES ===
        GUILayout.Label("🔧 ACTIONS RAPIDES", GUI.skin.box);

        if (GUILayout.Button("🔍 Test Tous Seuils"))
        {
            gameManager.TestAllThresholds();
        }

        if (GUILayout.Button("💾 Sauver Seuils"))
        {
            gameManager.SaveCurrentThresholds();
        }

        if (GUILayout.Button("📁 Charger Seuils"))
        {
            gameManager.LoadSavedThresholds();
        }

        if (GUILayout.Button("🔄 Reset Défaut"))
        {
            gameManager.ResetThresholdsToDefault();
        }

        GUILayout.EndArea();
    }

    private void HandleOtherInputs()
    {
        // Raccourcis clavier pour chaque onglet
        for (int i = 0; i < tabs.Count; i++)
        {
            if (Input.GetKeyDown(tabs[i].shortcutKey))
            {
                currentTabIndex = i;
                Debug.Log($"Onglet actif: {tabs[i].tabName}");
            }
        }

        // Navigation avec flèches
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentTabIndex = (currentTabIndex - 1 + tabs.Count) % tabs.Count;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentTabIndex = (currentTabIndex + 1) % tabs.Count;
        }
    }

    private void OnGUI()
    {
        // TOUJOURS afficher les instructions de base, même quand les onglets sont désactivés
        DrawInstructions();

        // Les onglets seulement si activés
        if (enableTabSystem)
        {
            DrawTabWindow();
        }
        DrawSimulationControls();
    }
    private void DrawSimulationControls()
    {
        if (gameManager == null || !gameManager.ShowSimulationControls)
            return;

        // Dimensions du panneau de contrôle
        float panelWidth = 220f;
        float panelHeight = 100f;
        float margin = 10f;

        // Position dans le coin inférieur droit
        float panelX = Screen.width - panelWidth - margin;
        float panelY = Screen.height - panelHeight - margin;

        // Rectangle principal
        Rect controlRect = new Rect(panelX, panelY, panelWidth, panelHeight);
        GUI.Box(controlRect, "");

        // Titre
        GUI.Label(new Rect(panelX + 5, panelY + 5, panelWidth - 10, 20),
                  "=== CONTRÔLES SIMULATION ===");

        // Boutons en ligne
        float buttonY = panelY + 30;
        float buttonWidth = 100f;
        float buttonHeight = 25f;
        float buttonSpacing = 5f;

        // Bouton Fermer
        if (GUI.Button(new Rect(panelX + 5, buttonY, buttonWidth, buttonHeight),
                       "🚪 Fermer"))
        {
            gameManager.CloseSimulation();
        }

        // Bouton Restart
        if (GUI.Button(new Rect(panelX + buttonWidth + buttonSpacing + 5, buttonY,
                               buttonWidth, buttonHeight),
                       "🔄 Restart"))
        {
            gameManager.CloseSimulation();
            // Note: RestartAfterCleanup sera appelé automatiquement si la coroutine existe
        }

        // Info Time Scale
        GUI.Label(new Rect(panelX + 5, buttonY + 30, 150, 20),
                  $"Time Scale: {Time.timeScale:F1}");

        // Bouton Pause/Play
        string pauseButtonText = Time.timeScale > 0 ? "⏸️ Pause" : "▶️ Play";
        if (GUI.Button(new Rect(panelX + 155, buttonY + 30, 60, 20), pauseButtonText))
        {
            Time.timeScale = Time.timeScale > 0 ? 0f : 1f;
        }
    }

    private void DrawTabWindow()
    {
        // Obtenir la taille de fenêtre selon l'onglet actuel
        Vector2 currentWindowSize = GetCurrentWindowSize();

        // Fenêtre principale avec taille adaptative
        Rect windowRect = new Rect(windowPosition.x, windowPosition.y, currentWindowSize.x, currentWindowSize.y);
        GUI.Box(windowRect, "");

        // Zone des onglets
        Rect tabAreaRect = new Rect(windowRect.x, windowRect.y, windowRect.width, tabHeight);
        DrawTabHeaders(tabAreaRect);

        // Zone de contenu
        Rect contentRect = new Rect(
            windowRect.x + 10,
            windowRect.y + tabHeight + 10,
            windowRect.width - 20,
            windowRect.height - tabHeight - 20
        );

        // Dessiner le contenu de l'onglet actuel
        if (currentTabIndex >= 0 && currentTabIndex < tabs.Count)
        {
            tabs[currentTabIndex].drawContent?.Invoke(contentRect);
        }
    }

    private Vector2 GetCurrentWindowSize()
    {
        if (currentTabIndex >= 0 && currentTabIndex < tabs.Count)
        {
            string currentTabName = tabs[currentTabIndex].tabName;

            switch (currentTabName)
            {
                case "Jeu":
                    return gameTabSize;
                case "Volcans":
                    return volcanicTabSize;
                case "Biomes":
                    return new Vector2(400, 450);
                case "Seuils":                                    // ✅ NOUVEAU
                    return new Vector2(350, 600);                // ✅ NOUVEAU - Taille pour sliders
                default:
                    return defaultTabSize;
            }
        }

        return defaultTabSize;
    }



    private void DrawTabHeaders(Rect tabArea)
    {
        float xOffset = tabArea.x;

        for (int i = 0; i < tabs.Count; i++)
        {
            Rect tabRect = new Rect(xOffset, tabArea.y, tabWidth, tabHeight);

            // Style de l'onglet (actif/inactif)
            if (i == currentTabIndex)
            {
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUI.backgroundColor = Color.gray;
            }

            // Bouton onglet
            if (GUI.Button(tabRect, tabs[i].tabName))
            {
                currentTabIndex = i;
            }

            xOffset += tabWidth;
        }

        GUI.backgroundColor = Color.white; // Reset
    }

    // === CONTENU DES ONGLETS ===

    private void DrawGameTab(Rect contentRect)
    {
        GUILayout.BeginArea(contentRect);

        // === BOUTONS DE NAVIGATION SOUS-SECTIONS ===
        GUILayout.BeginHorizontal();

        if (GUILayout.Toggle(currentGameSubSection == GameSubSection.General, "Général", "Button"))
            currentGameSubSection = GameSubSection.General;

        if (GUILayout.Toggle(currentGameSubSection == GameSubSection.Water, "Eau", "Button"))
            currentGameSubSection = GameSubSection.Water;

        if (GUILayout.Toggle(currentGameSubSection == GameSubSection.Atmosphere, "Atmosphère", "Button"))
            currentGameSubSection = GameSubSection.Atmosphere;

        if (GUILayout.Toggle(currentGameSubSection == GameSubSection.Life, "Vie", "Button"))
            currentGameSubSection = GameSubSection.Life;

        if (GUILayout.Toggle(currentGameSubSection == GameSubSection.All, "Tout", "Button"))
            currentGameSubSection = GameSubSection.All;

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // === CONTENU SELON LA SOUS-SECTION ===
        if (gameManager != null)
        {
            switch (currentGameSubSection)
            {
                case GameSubSection.General:
                    DrawGeneralSection();
                    break;

                case GameSubSection.Water:
                    DrawWaterSection();
                    break;

                case GameSubSection.Atmosphere:
                    DrawAtmosphereSection();
                    break;

                case GameSubSection.Life:
                    DrawLifeSection();
                    break;

                case GameSubSection.All:
                    DrawAllSections();
                    break;
            }
        }
        else
        {
            GUILayout.Label("GameManager non trouvé");
        }

        GUILayout.EndArea();
    }

    private void DrawGeneralSection()
    {
        GUILayout.Label("=== INFORMATIONS GÉNÉRALES ===", GUI.skin.box);

        GUILayout.Label($"Phase: {gameManager.CurrentPhase}");
        GUILayout.Label($"Âge: {gameManager.GetFormattedAge()}");
        GUILayout.Label($"Température surface: {gameManager.SurfaceTemperature:F0}°C");
        GUILayout.Label($"Temperature core: {gameManager.CoreTemperature:F0}°C" );
        GUILayout.Label($"Climat: {gameManager.CurrentClimate}");
        GUILayout.Label($"Échelle temps: ×{gameManager.CurrentTimeScale:F0}");
        GUILayout.Label($"Multiplicateur: ×{gameManager.GetPlayerTimeMultiplier():F1}");

        GUILayout.Space(10);

        // Boutons de contrôle rapide
        GUILayout.Label("=== CONTRÔLES RAPIDES ===", GUI.skin.box);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("×0.5"))
            gameManager.SetTimeScale(0.5f);
        if (GUILayout.Button("×1"))
            gameManager.SetTimeScale(1f);
        if (GUILayout.Button("×2"))
            gameManager.SetTimeScale(2f);
        if (GUILayout.Button("×5"))
            gameManager.SetTimeScale(5f);
        GUILayout.EndHorizontal();

        if (gameManager.IsPaused)
        {
            if (GUILayout.Button("▶ Reprendre"))
                gameManager.ResumeGame();
        }
        else
        {
            if (GUILayout.Button("⏸ Pause"))
                gameManager.PauseGame();
        }
    }

    private void DrawWaterSection()
    {
        GUILayout.Label("=== SYSTÈME HYDRIQUE ===", GUI.skin.box);

        GUILayout.Label($"État dominant: {gameManager.CurrentWaterState}");

        GUILayout.Space(5);

        float totalWater = gameManager.WaterLevel + gameManager.VaporLevel + gameManager.IceLevel;
        GUILayout.Label($"Eau totale: {totalWater:P1}");

        GUILayout.Space(5);

        // Barres de progression visuelles
        DrawProgressBar("Liquide", gameManager.WaterLevel, Color.blue);
        DrawProgressBar("Vapeur", gameManager.VaporLevel, Color.cyan);
        DrawProgressBar("Glace", gameManager.IceLevel, Color.white);

        GUILayout.Space(10);

        // Conditions pour la vie liées à l'eau
        GUILayout.Label("=== CONDITIONS EAU ===", GUI.skin.box);

        bool waterForLife = gameManager.WaterLevel >= 0.3f;
        string waterStatus = waterForLife ? "✓ Suffisante" : "✗ Insuffisante";
        Color waterColor = waterForLife ? Color.green : Color.red;

        GUI.color = waterColor;
        GUILayout.Label($"Eau pour la vie: {waterStatus} (≥30%)");
        GUI.color = Color.white;

        GUILayout.Label($"Température fusion: {(gameManager.SurfaceTemperature > 0 ? "✓" : "✗")} (>0°C)");
        GUILayout.Label($"Température ébullition: {(gameManager.SurfaceTemperature < 100 ? "✓" : "✗")} (<100°C)");
    }

    private void DrawAtmosphereSection()
    {
        GUILayout.Label("=== COMPOSITION ATMOSPHÉRIQUE ===", GUI.skin.box);

        GUILayout.Label($"Type: {gameManager.CurrentAtmosphere}");

        GUILayout.Space(5);

        float totalAtm = gameManager.NitrogenLevel + gameManager.MethaneLevel +
                        gameManager.CO2Level + gameManager.OxygenLevel;
        GUILayout.Label($"Densité totale: {totalAtm:P1}");

        GUILayout.Space(5);

        // Composition détaillée avec barres
        DrawProgressBar("N₂ (Azote)", gameManager.NitrogenLevel, new Color(0.7f, 0.7f, 1f));
        DrawProgressBar("CH₄ (Méthane)", gameManager.MethaneLevel, new Color(1f, 0.8f, 0.4f));
        DrawProgressBar("CO₂ (Dioxyde)", gameManager.CO2Level, new Color(0.8f, 0.4f, 0.4f));
        DrawProgressBar("O₂ (Oxygène)", gameManager.OxygenLevel, new Color(0.4f, 1f, 0.4f));

        GUILayout.Space(10);

        // Évolution atmosphérique
        GUILayout.Label("=== ÉVOLUTION ===", GUI.skin.box);

        if (gameManager.CurrentPhase == GamePhase.Geological)
        {
            if (gameManager.SurfaceTemperature > 800f)
            {
                GUILayout.Label("🔥 Trop chaud - Pas de dégazage");
            }
            else if (gameManager.SurfaceTemperature > 200f)
            {
                GUILayout.Label("🌋 Dégazage volcanique actif");
            }
            else
            {
                GUILayout.Label("❄️ Atmosphère stabilisée");
            }
        }
        else
        {
            GUILayout.Label("🌱 Production biologique d'O₂");
        }

        // Pressions partielles (bonus)
        GUILayout.Space(5);
        if (totalAtm > 0.01f)
        {
            GUILayout.Label("=== PRESSIONS PARTIELLES ===", GUI.skin.box);
            GUILayout.Label($"N₂: {(gameManager.NitrogenLevel / totalAtm):P1}");
            GUILayout.Label($"CH₄: {(gameManager.MethaneLevel / totalAtm):P1}");
            GUILayout.Label($"CO₂: {(gameManager.CO2Level / totalAtm):P1}");
            GUILayout.Label($"O₂: {(gameManager.OxygenLevel / totalAtm):P1}");
        }
    }

    private void DrawLifeSection()
    {
        GUILayout.Label("=== CONDITIONS POUR LA VIE ===", GUI.skin.box);

        if (gameManager.CurrentPhase == GamePhase.Geological)
        {
            // Analyser toutes les conditions
            bool tempOK = gameManager.SurfaceTemperature >= 5f && gameManager.SurfaceTemperature <= 80f;
            bool waterOK = gameManager.WaterLevel >= 0.3f;
            float totalAtm = gameManager.NitrogenLevel + gameManager.MethaneLevel + gameManager.CO2Level;
            bool atmOK = totalAtm > 0.20f;
            bool stableOK = gameManager.SurfaceTemperature < 200f; // Température de stabilisation

            // Affichage détaillé avec couleurs
            DrawConditionStatus("Température habitable", tempOK, "5-80°C");
            DrawConditionStatus("Eau liquide", waterOK, "≥30%");
            DrawConditionStatus("Atmosphère dense", atmOK, "≥20%");
            DrawConditionStatus("Stabilité thermique", stableOK, "<200°C");

            GUILayout.Space(10);

            // Résumé global
            bool allConditions = tempOK && waterOK && atmOK && stableOK;
            if (allConditions)
            {
                GUI.color = Color.green;
                GUILayout.Label("🌱 CONDITIONS OPTIMALES - VIE POSSIBLE !");
            }
            else
            {
                GUI.color = Color.yellow;
                int conditionsMet = (tempOK ? 1 : 0) + (waterOK ? 1 : 0) + (atmOK ? 1 : 0) + (stableOK ? 1 : 0);
                GUILayout.Label($"⏳ Progrès: {conditionsMet}/4 conditions");
            }
            GUI.color = Color.white;

            GUILayout.Space(10);

            // Prochaines étapes
            GUILayout.Label("=== PROCHAINES ÉTAPES ===", GUI.skin.box);
            if (!tempOK)
                GUILayout.Label("• Attendre refroidissement planète");
            if (!waterOK)
                GUILayout.Label("• Attendre condensation vapeur");
            if (!atmOK)
                GUILayout.Label("• Attendre dégazage volcanique");
            if (!stableOK)
                GUILayout.Label("• Attendre stabilisation thermique");
        }
        else
        {
            GUI.color = Color.green;
            GUILayout.Label("🎉 VIE ÉMERGÉE - PHASE ÉVOLUTION !");
            GUI.color = Color.white;

            GUILayout.Space(10);

            GUILayout.Label("=== ÉVOLUTION BIOLOGIQUE ===", GUI.skin.box);
            GUILayout.Label($"Oxygène produit: {gameManager.OxygenLevel:P1}");
            GUILayout.Label($"CO₂ consommé: {gameManager.CO2Level:P1}");

            if (gameManager.OxygenLevel >= 0.18f)
            {
                GUILayout.Label("🌍 Atmosphère terrestre atteinte !");
            }
            else if (gameManager.OxygenLevel >= 0.05f)
            {
                GUILayout.Label("🌿 Photosynthèse active");
            }
            else
            {
                GUILayout.Label("🦠 Vie microbienne primitive");
            }
        }
    }

    private void DrawAllSections()
    {
        // Version compacte de tout (police plus petite)
        int originalSize = GUI.skin.label.fontSize;
        GUI.skin.label.fontSize = 10;

        DrawGeneralSection();
        GUILayout.Space(5);
        DrawWaterSection();
        GUILayout.Space(5);
        DrawAtmosphereSection();
        GUILayout.Space(5);
        DrawLifeSection();

        GUI.skin.label.fontSize = originalSize; // Restaurer
    }

   
    private void DrawTimeTab(Rect contentRect)
    {
        GUILayout.BeginArea(contentRect);

        GUILayout.Label("=== CONTRÔLE TEMPS ===", GUI.skin.box);

        if (gameManager != null)
        {
            GUILayout.Label($"Multiplicateur actuel: ×{gameManager.GetPlayerTimeMultiplier():F1}");
            GUILayout.Label($"Échelle de base: ×{gameManager.GetBaseTimeScale():F0}");
            GUILayout.Label($"Échelle finale: ×{gameManager.CurrentTimeScale:F0}");

            GUILayout.Space(10);

            GUILayout.Label("Vitesses prédéfinies:");

            if (GUILayout.Button("×0.5"))
                gameManager.SetTimeScale(0.5f);
            if (GUILayout.Button("×1"))
                gameManager.SetTimeScale(1f);
            if (GUILayout.Button("×2"))
                gameManager.SetTimeScale(2f);
            if (GUILayout.Button("×5"))
                gameManager.SetTimeScale(5f);
            if (GUILayout.Button("×10"))
                gameManager.SetTimeScale(10f);

            GUILayout.Space(10);

            if (gameManager.IsPaused)
            {
                if (GUILayout.Button("Reprendre"))
                    gameManager.ResumeGame();
            }
            else
            {
                if (GUILayout.Button("Pause"))
                    gameManager.PauseGame();
            }
        }

        GUILayout.EndArea();
    }

 

    private void DrawPlanetTab(Rect contentRect)
    {
        GUILayout.BeginArea(contentRect);

        GUILayout.Label("=== GÉNÉRATION PLANÈTE ===", GUI.skin.box);

        var planetGen = FindAnyObjectByType<PlanetGenerator>();
        if (planetGen != null)
        {
            GUILayout.Label($"Rayon: {planetGen.PlanetRadius}");
            GUILayout.Label($"Résolution: {planetGen.Resolution}");

            GUILayout.Space(10);

            if (GUILayout.Button("Régénérer Planète"))
            {
                planetGen.GeneratePlanet();
            }
        }

        GUILayout.Label("=== CAMÉRA ===");
        var cameraController = FindAnyObjectByType<CameraModeController>();
        if (cameraController != null)
        {
            GUILayout.Label($"Mode: {cameraController.CurrentMode}");

            if (GUILayout.Button("Global"))
                cameraController.SetGlobalMode();
            if (GUILayout.Button("Regional"))
                cameraController.SetRegionalMode();
            if (GUILayout.Button("Local"))
                cameraController.SetLocalMode();
        }

        GUILayout.EndArea();
    }



    private void DrawInstructions()
    {
        Rect instructRect = new Rect(10, Screen.height - 60, 800, 50);
        GUI.Box(instructRect, "");

        if (enableTabSystem)
        {
            GUI.Label(new Rect(20, Screen.height - 50, 780, 20),
                      $"=== ONGLETS GUI === Actuel: {(currentTabIndex < tabs.Count ? tabs[currentTabIndex].tabName : "Aucun")}");
            GUI.Label(new Rect(20, Screen.height - 30, 780, 20),
                      "TAB=Désactiver | 1-7=Onglets | ←→=Navigation | 1=Jeu 2=Volcans 3=Matériaux 4=Temps 5=Diagnostic 6=Planète 7=Biomes");
        }
        else
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(20, Screen.height - 50, 780, 20),
                      "=== ONGLETS GUI DÉSACTIVÉS ===");
            GUI.Label(new Rect(20, Screen.height - 30, 780, 20),
                      "TAB = Réactiver les onglets");
            GUI.color = Color.white;
        }
    }

  


    // Méthodes utilitaires
    private void DrawProgressBar(string label, float value, Color barColor)
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label($"{label}:", GUILayout.Width(80));

        Rect barRect = GUILayoutUtility.GetRect(100, 15);
        GUI.Box(barRect, "");

        Rect fillRect = new Rect(barRect.x + 1, barRect.y + 1,
                                (barRect.width - 2) * value, barRect.height - 2);

        Color oldColor = GUI.color;
        GUI.color = barColor;
        GUI.Box(fillRect, "");
        GUI.color = oldColor;

        GUILayout.Label($"{value:P1}", GUILayout.Width(40));

        GUILayout.EndHorizontal();
    }

    private void DrawConditionStatus(string condition, bool isOK, string requirement)
    {
        GUILayout.BeginHorizontal();

        GUI.color = isOK ? Color.green : Color.red;
        GUILayout.Label(isOK ? "✓" : "✗", GUILayout.Width(20));

        GUI.color = Color.white;
        GUILayout.Label($"{condition}: {requirement}");

        GUILayout.EndHorizontal();
    }

    // Méthodes publiques pour contrôle externe
    public void SetActiveTab(int index)
    {
        if (index >= 0 && index < tabs.Count)
        {
            currentTabIndex = index;
        }
    }

    public void SetActiveTab(string tabName)
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            if (tabs[i].tabName == tabName)
            {
                currentTabIndex = i;
                break;
            }
        }
    }
}