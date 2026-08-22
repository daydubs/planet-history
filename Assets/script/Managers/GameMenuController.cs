using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GameMenuController : MonoBehaviour
{
    public static GameMenuController Instance { get; private set; }

    [Header("State")]
    [SerializeField] private bool showLaunchScreenOnStart = true;

    private GameObject canvasObj;
    private GameObject launchScreenPanel;
    private GameObject pauseScreenPanel;
    private GameObject optionsScreenPanel;
    private bool previousStateIsPause = false;

    // UI Controls for sync
    private TMP_Text musicVolumeText;
    private TMP_Text sfxVolumeText;
    private TMP_Text sessionLengthText;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Slider customLengthSlider;

    private Button[] presetButtons;
    private SessionLengthPreset currentPreset = SessionLengthPreset.ThreeHours;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureCanvasExists();
        CreateLaunchScreenUI();
        CreatePauseScreenUI();
        CreateOptionsMenuUI();
    }

    private void Start()
    {
        if (showLaunchScreenOnStart)
        {
            ShowLaunchScreen();
        }
        else
        {
            HideAllMenus();
        }

        SyncUIValuesWithManagers();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame))
        {
            if (optionsScreenPanel != null && optionsScreenPanel.activeSelf)
            {
                CloseOptions();
                return;
            }

            if (launchScreenPanel != null && launchScreenPanel.activeSelf)
            {
                // In launch screen, ignore Escape or stay in launch screen
                return;
            }

            TogglePauseMenu();
        }
    }

    private void EnsureCanvasExists()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            canvasObj = new GameObject("GameMenuCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Above HUD

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasObj = canvas.gameObject;
        }
    }

    #region Launch Screen Construction
    private void CreateLaunchScreenUI()
    {
        if (canvasObj == null) return;

        // Launch Panel Container
        launchScreenPanel = new GameObject("LaunchScreenPanel", typeof(RectTransform));
        launchScreenPanel.transform.SetParent(canvasObj.transform, false);

        RectTransform panelRect = launchScreenPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        // Background dark semi-transparent overlay to reveal 3D planet
        Image bg = launchScreenPanel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.04f, 0.08f, 0.45f);

        // Main Layout Container
        GameObject contentObj = new GameObject("LaunchContent", typeof(RectTransform));
        contentObj.transform.SetParent(launchScreenPanel.transform, false);

        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.10f, 0.03f);
        contentRect.anchorMax = new Vector2(0.90f, 0.97f);
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        // Title
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
        titleObj.transform.SetParent(contentObj.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "PLANET HISTORY";
        titleText.fontSize = 52;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.98f, 0.85f, 0.35f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;

        // Subtitle
        GameObject subTitleObj = new GameObject("SubtitleText", typeof(RectTransform));
        subTitleObj.transform.SetParent(contentObj.transform, false);
        TextMeshProUGUI subTitleText = subTitleObj.AddComponent<TextMeshProUGUI>();
        subTitleText.text = "Simulateur d'Évolution Planétaire et Géologique";
        subTitleText.fontSize = 22;
        subTitleText.fontStyle = FontStyles.Italic;
        subTitleText.color = new Color(0.75f, 0.82f, 0.90f, 1f);
        subTitleText.alignment = TextAlignmentOptions.Center;

        // Spacer
        CreateSpacer(contentObj.transform, 10f);

        // Main Actions Row (Play / Quit)
        GameObject mainActionsRow = new GameObject("MainActionsRow", typeof(RectTransform));
        mainActionsRow.transform.SetParent(contentObj.transform, false);

        HorizontalLayoutGroup mainActionsLayout = mainActionsRow.AddComponent<HorizontalLayoutGroup>();
        mainActionsLayout.childAlignment = TextAnchor.MiddleCenter;
        mainActionsLayout.spacing = 20f;
        mainActionsLayout.childControlWidth = false;

        LayoutElement mainActionsLayoutEl = mainActionsRow.AddComponent<LayoutElement>();
        mainActionsLayoutEl.minHeight = 50f;

        CreateButton(mainActionsRow.transform, "▶ JOUER", new Color(0.18f, 0.65f, 0.38f, 1f), 160f, 48f, () => StartGameFromLaunch());
        CreateButton(mainActionsRow.transform, "⚙ OPTIONS", new Color(0.20f, 0.50f, 0.70f, 1f), 160f, 48f, () => ToggleLaunchOptions());
        CreateButton(mainActionsRow.transform, "✖ QUITTER", new Color(0.75f, 0.25f, 0.25f, 1f), 160f, 48f, () => QuitGame());

        // Spacer
        CreateSpacer(contentObj.transform, 10f);

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
    }
    #endregion

    #region Pause Screen Construction
    private void CreatePauseScreenUI()
    {
        if (canvasObj == null) return;

        pauseScreenPanel = new GameObject("PauseScreenPanel", typeof(RectTransform));
        pauseScreenPanel.transform.SetParent(canvasObj.transform, false);

        RectTransform panelRect = pauseScreenPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        Image bg = pauseScreenPanel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.03f, 0.05f, 0.55f); // Dark translucent overlay

        // Content
        GameObject contentObj = new GameObject("PauseContent", typeof(RectTransform));
        contentObj.transform.SetParent(pauseScreenPanel.transform, false);

        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.10f, 0.03f);
        contentRect.anchorMax = new Vector2(0.90f, 0.97f);
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        // Pause Title
        GameObject titleObj = new GameObject("PauseTitleText", typeof(RectTransform));
        titleObj.transform.SetParent(contentObj.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "⏸ JEU EN PAUSE";
        titleText.fontSize = 44;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.98f, 0.85f, 0.35f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;

        // Actions Row
        GameObject actionsRow = new GameObject("PauseActionsRow", typeof(RectTransform));
        actionsRow.transform.SetParent(contentObj.transform, false);

        HorizontalLayoutGroup actionsLayout = actionsRow.AddComponent<HorizontalLayoutGroup>();
        actionsLayout.childAlignment = TextAnchor.MiddleCenter;
        actionsLayout.spacing = 16f;
        actionsLayout.childControlWidth = false;

        LayoutElement actionsLayoutEl = actionsRow.AddComponent<LayoutElement>();
        actionsLayoutEl.minHeight = 44f;

        CreateButton(actionsRow.transform, "▶ REPRENDRE", new Color(0.18f, 0.65f, 0.38f, 1f), 160f, 44f, () => ResumeGame());
        CreateButton(actionsRow.transform, "⚙ OPTIONS", new Color(0.20f, 0.50f, 0.70f, 1f), 160f, 44f, () => TogglePauseOptions());
        CreateButton(actionsRow.transform, "🏠 MENU PRINCIPAL", new Color(0.25f, 0.50f, 0.75f, 1f), 200f, 44f, () => ReturnToLaunchScreen());
        CreateButton(actionsRow.transform, "✖ QUITTER", new Color(0.75f, 0.25f, 0.25f, 1f), 160f, 44f, () => QuitGame());

        // Spacer
        CreateSpacer(contentObj.transform, 10f);

        pauseScreenPanel.SetActive(false);
    }
    #endregion

    #region Options Screen Construction
    private void CreateOptionsMenuUI()
    {
        if (canvasObj == null) return;

        // Full Screen Overlay Panel masking the screen completely
        optionsScreenPanel = new GameObject("OptionsScreenPanel", typeof(RectTransform));
        optionsScreenPanel.transform.SetParent(canvasObj.transform, false);

        RectTransform panelRect = optionsScreenPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        // Opaque dark background overlay to completely mask the scene/planet screen
        Image bg = optionsScreenPanel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.10f, 1.0f); // Opaque dark navy background

        // Main Layout Container
        GameObject contentObj = new GameObject("OptionsContent", typeof(RectTransform));
        contentObj.transform.SetParent(optionsScreenPanel.transform, false);

        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.12f, 0.04f);
        contentRect.anchorMax = new Vector2(0.88f, 0.96f);
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        // Title Header
        GameObject titleObj = new GameObject("OptionsTitleText", typeof(RectTransform));
        titleObj.transform.SetParent(contentObj.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "<b>⚙ OPTIONS & RÉGLAGES</b>";
        titleText.fontSize = 42;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.98f, 0.85f, 0.35f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;

        LayoutElement titleEl = titleObj.AddComponent<LayoutElement>();
        titleEl.minHeight = 50f;

        // Subtitle
        GameObject subTitleObj = new GameObject("OptionsSubtitleText", typeof(RectTransform));
        subTitleObj.transform.SetParent(contentObj.transform, false);
        TextMeshProUGUI subTitleText = subTitleObj.AddComponent<TextMeshProUGUI>();
        subTitleText.text = "Personnalisez votre expérience de simulation planétaire";
        subTitleText.fontSize = 20;
        subTitleText.fontStyle = FontStyles.Italic;
        subTitleText.color = new Color(0.75f, 0.82f, 0.90f, 1f);
        subTitleText.alignment = TextAlignmentOptions.Center;

        LayoutElement subTitleEl = subTitleObj.AddComponent<LayoutElement>();
        subTitleEl.minHeight = 30f;

        // Scroll View Container for settings
        GameObject scrollView = new GameObject("ScrollView", typeof(RectTransform));
        scrollView.transform.SetParent(contentObj.transform, false);

        LayoutElement scrollLayoutEl = scrollView.AddComponent<LayoutElement>();
        scrollLayoutEl.minHeight = 400f;
        scrollLayoutEl.flexibleHeight = 1f;

        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 25f;

        // Viewport with Mask
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.sizeDelta = Vector2.zero;

        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0.01f); // Raycast target
        viewport.AddComponent<RectMask2D>();

        // Scroll Content
        GameObject scrollContent = new GameObject("Content", typeof(RectTransform));
        scrollContent.transform.SetParent(viewport.transform, false);
        RectTransform scrollContentRect = scrollContent.GetComponent<RectTransform>();
        scrollContentRect.anchorMin = new Vector2(0f, 1f);
        scrollContentRect.anchorMax = Vector2.one;
        scrollContentRect.pivot = new Vector2(0.5f, 1f);
        scrollContentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout = scrollContent.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 20f;
        contentLayout.padding = new RectOffset(24, 24, 20, 20);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;

        ContentSizeFitter csf = scrollContent.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.viewport = vpRect;
        scrollRect.content = scrollContentRect;

        // Add settings sections inside scroll content
        CreateGameLengthControls(scrollContent.transform);
        CreateSoundControls(scrollContent.transform);
        CreateControlsReminder(scrollContent.transform);

        // Bottom Actions Row (Return Button)
        GameObject bottomRow = new GameObject("OptionsBottomRow", typeof(RectTransform));
        bottomRow.transform.SetParent(contentObj.transform, false);

        HorizontalLayoutGroup bottomLayout = bottomRow.AddComponent<HorizontalLayoutGroup>();
        bottomLayout.childAlignment = TextAnchor.MiddleCenter;
        bottomLayout.spacing = 20f;

        LayoutElement bottomRowEl = bottomRow.AddComponent<LayoutElement>();
        bottomRowEl.minHeight = 50f;

        CreateButton(bottomRow.transform, "◀ RETOUR", new Color(0.20f, 0.50f, 0.70f, 1f), 220f, 48f, () => CloseOptions());

        optionsScreenPanel.SetActive(false);
    }
    #endregion

    #region Reusable Control Builders
    private void CreateGameLengthControls(Transform parent)
    {
        GameObject lengthContainer = new GameObject("GameLengthBlock", typeof(RectTransform));
        lengthContainer.transform.SetParent(parent, false);

        VerticalLayoutGroup vLayout = lengthContainer.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 10f;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;

        // Header + Status text
        GameObject headerObj = new GameObject("GameLengthHeader", typeof(RectTransform));
        headerObj.transform.SetParent(lengthContainer.transform, false);
        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = "<b>Longueur de la Session de Jeu :</b>";
        headerText.fontSize = 16;
        headerText.color = Color.white;

        // Preset Buttons Row
        GameObject btnRow = new GameObject("PresetButtonsRow", typeof(RectTransform));
        btnRow.transform.SetParent(lengthContainer.transform, false);

        HorizontalLayoutGroup hLayout = btnRow.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 10f;
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;

        LayoutElement btnRowLayout = btnRow.AddComponent<LayoutElement>();
        btnRowLayout.minHeight = 44f;
        btnRowLayout.preferredHeight = 44f;

        presetButtons = new Button[5];
        presetButtons[0] = CreatePresetButton(btnRow.transform, "1 Heure", SessionLengthPreset.OneHour);
        presetButtons[1] = CreatePresetButton(btnRow.transform, "3 Heures", SessionLengthPreset.ThreeHours);
        presetButtons[2] = CreatePresetButton(btnRow.transform, "6 Heures", SessionLengthPreset.SixHours);
        presetButtons[3] = CreatePresetButton(btnRow.transform, "12 Heures", SessionLengthPreset.TwelveHours);
        presetButtons[4] = CreatePresetButton(btnRow.transform, "Sur Mesure", SessionLengthPreset.Custom);

        // Custom Slider Row
        GameObject sliderRow = new GameObject("CustomSliderRow", typeof(RectTransform));
        sliderRow.transform.SetParent(lengthContainer.transform, false);

        HorizontalLayoutGroup sliderLayout = sliderRow.AddComponent<HorizontalLayoutGroup>();
        sliderLayout.spacing = 12f;
        sliderLayout.childAlignment = TextAnchor.MiddleLeft;
        sliderLayout.childControlWidth = true;
        sliderLayout.childControlHeight = true;

        LayoutElement sliderRowEl = sliderRow.AddComponent<LayoutElement>();
        sliderRowEl.minHeight = 36f;

        // Label for custom slider
        GameObject labelObj = new GameObject("CustomSliderLabel", typeof(RectTransform));
        labelObj.transform.SetParent(sliderRow.transform, false);
        TMP_Text customLabel = labelObj.AddComponent<TextMeshProUGUI>();
        customLabel.text = "Durée personnalisée (heures) :";
        customLabel.fontSize = 15;
        customLabel.color = new Color(0.85f, 0.88f, 0.92f, 1f);

        LayoutElement labelEl = labelObj.AddComponent<LayoutElement>();
        labelEl.minWidth = 220f;
        labelEl.preferredWidth = 220f;

        // Custom hours slider
        GameObject sliderObj = CreateSlider(sliderRow.transform, 0.25f, 24f, 3f, (val) =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetSessionLengthPreset(SessionLengthPreset.Custom);
                GameManager.Instance.SetCustomSessionHours(val);
            }
            SelectPresetUI(SessionLengthPreset.Custom);
            UpdateSessionLengthText();
        });
        customLengthSlider = sliderObj.GetComponent<Slider>();

        // Info status label
        GameObject statusObj = new GameObject("SessionLengthStatus", typeof(RectTransform));
        statusObj.transform.SetParent(lengthContainer.transform, false);
        sessionLengthText = statusObj.AddComponent<TextMeshProUGUI>();
        sessionLengthText.fontSize = 14;
        sessionLengthText.fontStyle = FontStyles.Italic;
        sessionLengthText.color = new Color(0.40f, 0.85f, 0.95f, 1f);

        UpdateSessionLengthText();
    }

    private Button CreatePresetButton(Transform parent, string label, SessionLengthPreset preset)
    {
        Color baseColor = new Color(0.18f, 0.25f, 0.35f, 1f);
        Button btn = CreateButton(parent, label, baseColor, 100f, 38f, () =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetSessionLengthPreset(preset);
            }
            SelectPresetUI(preset);
            UpdateSessionLengthText();
        });

        return btn;
    }

    private void SelectPresetUI(SessionLengthPreset preset)
    {
        currentPreset = preset;
        if (presetButtons == null) return;

        Color selectedColor = new Color(0.18f, 0.65f, 0.55f, 1f);
        Color defaultColor = new Color(0.20f, 0.28f, 0.38f, 1f);

        for (int i = 0; i < presetButtons.Length; i++)
        {
            if (presetButtons[i] == null) continue;
            SessionLengthPreset btnPreset = (SessionLengthPreset)i;
            Image img = presetButtons[i].GetComponent<Image>();
            if (img != null)
            {
                img.color = (btnPreset == preset) ? selectedColor : defaultColor;
            }
        }
    }

    private void CreateSoundControls(Transform parent)
    {
        GameObject soundContainer = new GameObject("SoundBlock", typeof(RectTransform));
        soundContainer.transform.SetParent(parent, false);

        VerticalLayoutGroup vLayout = soundContainer.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 10f;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;

        // Header
        GameObject headerObj = new GameObject("SoundHeader", typeof(RectTransform));
        headerObj.transform.SetParent(soundContainer.transform, false);
        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = "<b>Réglages Sonores :</b>";
        headerText.fontSize = 16;
        headerText.color = Color.white;

        // Music Volume Row
        GameObject musicRow = new GameObject("MusicVolumeRow", typeof(RectTransform));
        musicRow.transform.SetParent(soundContainer.transform, false);

        HorizontalLayoutGroup mLayout = musicRow.AddComponent<HorizontalLayoutGroup>();
        mLayout.spacing = 12f;
        mLayout.childAlignment = TextAnchor.MiddleLeft;
        mLayout.childControlWidth = true;
        mLayout.childControlHeight = true;

        LayoutElement mRowEl = musicRow.AddComponent<LayoutElement>();
        mRowEl.minHeight = 36f;

        musicVolumeText = CreateLabel(musicRow.transform, "🔊 Volume Musique : 50%", 220f);
        GameObject mSliderObj = CreateSlider(musicRow.transform, 0f, 1f, 0.5f, (val) =>
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(val);
            }
            if (musicVolumeText != null)
            {
                musicVolumeText.text = $"🔊 Volume Musique : {Mathf.RoundToInt(val * 100f)}%";
            }
        });
        musicSlider = mSliderObj.GetComponent<Slider>();

        // SFX Volume Row
        GameObject sfxRow = new GameObject("SFXVolumeRow", typeof(RectTransform));
        sfxRow.transform.SetParent(soundContainer.transform, false);

        HorizontalLayoutGroup sLayout = sfxRow.AddComponent<HorizontalLayoutGroup>();
        sLayout.spacing = 12f;
        sLayout.childAlignment = TextAnchor.MiddleLeft;
        sLayout.childControlWidth = true;
        sLayout.childControlHeight = true;

        LayoutElement sRowEl = sfxRow.AddComponent<LayoutElement>();
        sRowEl.minHeight = 36f;

        sfxVolumeText = CreateLabel(sfxRow.transform, "💥 Volume Effets (SFX) : 80%", 220f);
        GameObject sSliderObj = CreateSlider(sfxRow.transform, 0f, 1f, 0.8f, (val) =>
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSfxVolume(val);
            }
            if (sfxVolumeText != null)
            {
                sfxVolumeText.text = $"💥 Volume Effets (SFX) : {Mathf.RoundToInt(val * 100f)}%";
            }
        });
        sfxSlider = sSliderObj.GetComponent<Slider>();
    }

    private void CreateControlsReminder(Transform parent)
    {
        GameObject controlsContainer = new GameObject("ControlsReminderBlock", typeof(RectTransform));
        controlsContainer.transform.SetParent(parent, false);

        VerticalLayoutGroup vLayout = controlsContainer.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 8f;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;

        // Header
        GameObject headerObj = new GameObject("ControlsHeader", typeof(RectTransform));
        headerObj.transform.SetParent(controlsContainer.transform, false);
        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = "<b>Rappels des Commandes & Contrôles :</b>";
        headerText.fontSize = 16;
        headerText.color = Color.white;

        // Content body text
        GameObject textObj = new GameObject("ControlsBody", typeof(RectTransform));
        textObj.transform.SetParent(controlsContainer.transform, false);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14;
        text.color = new Color(0.85f, 0.88f, 0.92f, 1f);
        text.text = " • <b>A / D</b> : Effectuer une rotation de la planète sur son axe\n" +
                    " • <b>Échap / P</b> : Mettre en pause / Reprendre le jeu ou basculer les menus\n" +
                    " • <b>Glisser-déposer Souris sur Carte</b> : Naviguer dans la mini-carte\n" +
                    " • <b>Molette Souris sur Carte</b> : Zoomer / Dézoomer sur la mini-carte";
    }

    private TMP_Text CreateLabel(Transform parent, string text, float width)
    {
        GameObject obj = new GameObject("Label", typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 15;
        label.color = new Color(0.88f, 0.92f, 0.95f, 1f);
        label.alignment = TextAlignmentOptions.Left;

        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;

        return label;
    }

    private GameObject CreateSlider(Transform parent, float min, float max, float defaultValue, UnityEngine.Events.UnityAction<float> onValueChanged)
    {SubCreateSlider(parent, min, max, defaultValue, onValueChanged, out GameObject sliderObj); return sliderObj;}

    private void SubCreateSlider(Transform parent, float min, float max, float defaultValue, UnityEngine.Events.UnityAction<float> onValueChanged, out GameObject sliderObj)
    {
        sliderObj = new GameObject("Slider", typeof(RectTransform));
        sliderObj.transform.SetParent(parent, false);

        LayoutElement sliderLayout = sliderObj.AddComponent<LayoutElement>();
        sliderLayout.minWidth = 180f;
        sliderLayout.flexibleWidth = 1f;
        sliderLayout.minHeight = 24f;

        // Background track (high contrast dark rounded track)
        GameObject bgObj = new GameObject("Background", typeof(RectTransform));
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.10f, 0.15f, 0.22f, 0.95f);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.30f);
        bgRect.anchorMax = new Vector2(1f, 0.70f);
        bgRect.sizeDelta = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.30f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.70f);
        fillAreaRect.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.20f, 0.75f, 0.60f, 1f); // Vibrant teal fill

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.sizeDelta = Vector2.zero;

        // Handle Area
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = Vector2.zero;

        GameObject handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.98f, 0.98f, 1.0f, 1f); // Bright white handle

        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, 26f);

        // Slider component
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = defaultValue;

        slider.onValueChanged.AddListener(onValueChanged);
    }

    private Button CreateButton(Transform parent, string labelText, Color buttonColor, float width, float height, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonGo = new GameObject($"Btn_{labelText}", typeof(RectTransform));
        buttonGo.transform.SetParent(parent, false);

        Image buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = buttonColor;

        Button button = buttonGo.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        ColorBlock cb = button.colors;
        cb.normalColor = buttonColor;
        cb.highlightedColor = buttonColor * 1.25f;
        cb.pressedColor = buttonColor * 0.75f;
        button.colors = cb;

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(buttonGo.transform, false);
        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = labelText;
        text.fontSize = 17;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12;
        text.fontSizeMax = 18;

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);

        LayoutElement buttonLayout = buttonGo.AddComponent<LayoutElement>();
        buttonLayout.minWidth = width;
        buttonLayout.preferredWidth = width;
        buttonLayout.minHeight = height;
        buttonLayout.preferredHeight = height;

        button.onClick.AddListener(action);
        return button;
    }

    private void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(parent, false);

        LayoutElement layout = spacer.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;
    }
    #endregion

    #region Menu Actions & Sync
    public void ToggleLaunchOptions()
    {
        if (optionsScreenPanel != null && optionsScreenPanel.activeSelf)
        {
            CloseOptions();
        }
        else
        {
            ShowOptionsScreen(fromPause: false);
        }
    }

    public void TogglePauseOptions()
    {
        if (optionsScreenPanel != null && optionsScreenPanel.activeSelf)
        {
            CloseOptions();
        }
        else
        {
            ShowOptionsScreen(fromPause: true);
        }
    }

    public void ShowOptionsScreen(bool fromPause)
    {
        previousStateIsPause = fromPause;

        if (launchScreenPanel != null) launchScreenPanel.SetActive(false);
        if (pauseScreenPanel != null) pauseScreenPanel.SetActive(false);
        if (optionsScreenPanel != null) optionsScreenPanel.SetActive(true);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPause(true);
        }

        SyncUIValuesWithManagers();
    }

    public void CloseOptions()
    {
        if (optionsScreenPanel != null) optionsScreenPanel.SetActive(false);

        if (previousStateIsPause)
        {
            ShowPauseMenu();
        }
        else
        {
            ShowLaunchScreen();
        }
    }

    public void ShowLaunchScreen()
    {
        if (launchScreenPanel != null) launchScreenPanel.SetActive(true);
        if (pauseScreenPanel != null) pauseScreenPanel.SetActive(false);
        if (optionsScreenPanel != null) optionsScreenPanel.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPause(true);
        }

        SyncUIValuesWithManagers();
    }

    public void ShowPauseMenu()
    {
        if (launchScreenPanel != null && launchScreenPanel.activeSelf) return;

        if (pauseScreenPanel != null) pauseScreenPanel.SetActive(true);
        if (optionsScreenPanel != null) optionsScreenPanel.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPause(true);
        }

        SyncUIValuesWithManagers();
    }

    public void ResumeGame()
    {
        if (launchScreenPanel != null) launchScreenPanel.SetActive(false);
        if (pauseScreenPanel != null) pauseScreenPanel.SetActive(false);
        if (optionsScreenPanel != null) optionsScreenPanel.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPause(false);
        }
    }

    public void TogglePauseMenu()
    {
        if (launchScreenPanel != null && launchScreenPanel.activeSelf) return;

        if (pauseScreenPanel != null && pauseScreenPanel.activeSelf)
        {
            ResumeGame();
        }
        else
        {
            ShowPauseMenu();
        }
    }

    public void StartGameFromLaunch()
    {
        ResumeGame();
    }

    public void ReturnToLaunchScreen()
    {
        ShowLaunchScreen();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SyncUIValuesWithManagers()
    {
        if (AudioManager.Instance != null)
        {
            float musicVol = AudioManager.Instance.MusicVolume;
            float sfxVol = AudioManager.Instance.SfxVolume;

            if (musicSlider != null) musicSlider.value = musicVol;
            if (sfxSlider != null) sfxSlider.value = sfxVol;

            if (musicVolumeText != null) musicVolumeText.text = $"🔊 Volume Musique : {Mathf.RoundToInt(musicVol * 100f)}%";
            if (sfxVolumeText != null) sfxVolumeText.text = $"💥 Volume Effets (SFX) : {Mathf.RoundToInt(sfxVol * 100f)}%";
        }

        if (GameManager.Instance != null)
        {
            float hours = GameManager.Instance.SessionDurationHours;
            if (customLengthSlider != null) customLengthSlider.value = hours;

            SelectPresetUI(currentPreset);
            UpdateSessionLengthText();
        }
    }

    private void UpdateSessionLengthText()
    {
        if (sessionLengthText == null) return;

        float duration = GameManager.Instance != null ? GameManager.Instance.SessionDurationHours : 3f;
        sessionLengthText.text = $"⏱ Durée totale configurée : <b>{duration:F2} heures</b> (Simulée en temps réel)";
    }

    private void HideAllMenus()
    {
        if (launchScreenPanel != null) launchScreenPanel.SetActive(false);
        if (pauseScreenPanel != null) pauseScreenPanel.SetActive(false);
        if (optionsScreenPanel != null) optionsScreenPanel.SetActive(false);
    }
    #endregion
}
