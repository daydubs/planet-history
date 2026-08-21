using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHoverTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string title;
    public string body;
    public GameHudController hudController;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hudController != null)
        {
            hudController.ShowTooltip(title, body);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hudController != null)
        {
            hudController.HideTooltip();
        }
    }
}

public class GameHudController : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private GameManager gameManager;
    [SerializeField, Min(0.05f)] private float refreshIntervalSeconds = 0.2f;

    [Header("Labels")]
    [SerializeField] private TMP_Text epochText;
    [SerializeField] private TMP_Text sessionText;
    [SerializeField] private TMP_Text remainingTimeText;
    [SerializeField] private TMP_Text internalTempText;
    [SerializeField] private TMP_Text surfaceTempText;
    [SerializeField] private TMP_Text pressureText;
    [SerializeField] private TMP_Text waterText;
    [SerializeField] private TMP_Text tectonicText;
    [SerializeField] private TMP_Text atmosphereCompositionText;

    [Header("Optional Bars")]
    [SerializeField] private Slider sessionSlider;
    [SerializeField] private Slider waterSlider;
    [SerializeField] private Slider tectonicSlider;

    [Header("Epoch Badge (optional)")]
    [SerializeField] private Image epochBadgeImage;
    [SerializeField] private TMP_Text epochBadgeHexText;

    [Header("Labels Prebiotique")]
    [SerializeField] private TMP_Text prebioticProgressText;

    [Header("Prebiotique Completion Popup")]
    [SerializeField] private Sprite prebioticCompletionSprite;

    [Header("Palette (Hex)")]
    [SerializeField] private string hadeanHex = "#D1495B";
    [SerializeField] private string crustFormationHex = "#F79256";
    [SerializeField] private string volcanicAgeHex = "#F9C74F";
    [SerializeField] private string protoOceanHex = "#43AA8B";
    [SerializeField] private string tectonicDriftHex = "#4D96FF";
    [SerializeField] private string prebioticHex = "#2A9D8F";
    [SerializeField] private string fallbackHex = "#9AA0A6";

    private Button volcanoButton;
    private readonly System.Collections.Generic.List<Button> prebioticActionButtons = new System.Collections.Generic.List<Button>();
    private float refreshTimer;

    private GameObject tooltipPanel;
    private TMP_Text tooltipTitleText;
    private TMP_Text tooltipBodyText;

    private GameObject prebioticCompletionPanel;
    private Image prebioticCompletionImageComponent;
    private bool hasShownPrebioticCompletionWindow = false;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }

        // Dynamically add the MeteorEventController to handle programmatic meteor button creation and impact logic
        if (gameObject.GetComponent<MeteorEventController>() == null)
        {
            gameObject.AddComponent<MeteorEventController>();
        }

        // Dynamically attach VolcanoManager to scene if missing
        if (FindAnyObjectByType<VolcanoManager>() == null)
        {
            GameObject volcanoManagerObj = new GameObject("VolcanoManager");
            volcanoManagerObj.AddComponent<VolcanoManager>();
        }

        // Dynamically attach PrebioticMiniGameController to scene if missing
        if (FindAnyObjectByType<PrebioticMiniGameController>() == null)
        {
            GameObject prebioticObj = new GameObject("PrebioticMiniGameController");
            prebioticObj.AddComponent<PrebioticMiniGameController>();
        }

        CreateVolcanoUI();
        CreatePrebioticUI();
        CreateTooltipUI();
        CreatePrebioticCompletionWindowUI();
    }

    private void CreateTooltipUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Container in bottom right
        tooltipPanel = new GameObject("PrebioticTooltipPanel", typeof(RectTransform));
        tooltipPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = tooltipPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-20f, 20f);
        panelRect.sizeDelta = new Vector2(340f, 160f);

        Image bg = tooltipPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.10f, 0.14f, 0.92f);

        VerticalLayoutGroup layout = tooltipPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = tooltipPanel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Title
        GameObject titleGo = new GameObject("TooltipTitle", typeof(RectTransform));
        titleGo.transform.SetParent(tooltipPanel.transform, false);
        tooltipTitleText = titleGo.AddComponent<TextMeshProUGUI>();
        tooltipTitleText.fontSize = 16;
        tooltipTitleText.fontStyle = FontStyles.Bold;
        tooltipTitleText.color = new Color(0.95f, 0.80f, 0.30f, 1f);

        // Body
        GameObject bodyGo = new GameObject("TooltipBody", typeof(RectTransform));
        bodyGo.transform.SetParent(tooltipPanel.transform, false);
        tooltipBodyText = bodyGo.AddComponent<TextMeshProUGUI>();
        tooltipBodyText.fontSize = 13;
        tooltipBodyText.fontStyle = FontStyles.Normal;
        tooltipBodyText.color = new Color(0.90f, 0.92f, 0.95f, 1f);
        tooltipBodyText.enableWordWrapping = true;

        tooltipPanel.SetActive(false);
    }

    public void ShowTooltip(string title, string body)
    {
        if (tooltipPanel == null) return;

        if (tooltipTitleText != null) tooltipTitleText.text = title;
        if (tooltipBodyText != null) tooltipBodyText.text = body;

        tooltipPanel.SetActive(true);
        tooltipPanel.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel.GetComponent<RectTransform>());
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    private void CreateVolcanoUI()
    {
        RectTransform hudRoot = transform as RectTransform;
        if (hudRoot == null) return;

        // Create a new Row GameObject for Volcano Trigger
        GameObject rowGo = new GameObject("VolcanoRow", typeof(RectTransform));
        rowGo.transform.SetParent(hudRoot, false);

        RectTransform rowRect = rowGo.GetComponent<RectTransform>();
        rowRect.localScale = Vector3.one;

        LayoutElement rowLayout = rowGo.AddComponent<LayoutElement>();
        rowLayout.minHeight = 44f;
        rowLayout.preferredHeight = 44f;
        rowLayout.flexibleHeight = 0f;
        rowLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup horizontal = rowGo.AddComponent<HorizontalLayoutGroup>();
        horizontal.childAlignment = TextAnchor.MiddleLeft;
        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = true;
        horizontal.spacing = 10f;

        // Label
        GameObject labelGo = new GameObject("VolcanoLabel", typeof(RectTransform));
        labelGo.transform.SetParent(rowRect, false);
        TextMeshProUGUI labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.text = "Volcano Event :";
        labelText.fontSize = 22;
        labelText.fontStyle = FontStyles.Normal;
        labelText.color = new Color(0.83f, 0.86f, 0.90f, 1f);
        labelText.alignment = TextAlignmentOptions.Left;

        LayoutElement labelLayout = labelGo.AddComponent<LayoutElement>();
        labelLayout.minWidth = 120f;
        labelLayout.flexibleWidth = 1f;

        // Button
        GameObject buttonGo = new GameObject("VolcanoButton", typeof(RectTransform));
        buttonGo.transform.SetParent(rowRect, false);

        Image buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = new Color(0.92f, 0.45f, 0.15f, 1f); // Vibrant orange button

        Button button = buttonGo.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        volcanoButton = button;

        ColorBlock cb = button.colors;
        cb.normalColor = new Color(0.92f, 0.45f, 0.15f, 1f);
        cb.highlightedColor = new Color(1.0f, 0.55f, 0.25f, 1f);
        cb.pressedColor = new Color(0.72f, 0.35f, 0.05f, 1f);
        cb.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
        button.colors = cb;

        GameObject buttonTextGo = new GameObject("Text", typeof(RectTransform));
        buttonTextGo.transform.SetParent(buttonGo.transform, false);
        TextMeshProUGUI buttonText = buttonTextGo.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Créer Volcan";
        buttonText.enableAutoSizing = true;
        buttonText.fontSizeMin = 12;
        buttonText.fontSizeMax = 18;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = buttonTextGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        LayoutElement buttonLayout = buttonGo.AddComponent<LayoutElement>();
        buttonLayout.minWidth = 140f;
        buttonLayout.preferredWidth = 160f;
        buttonLayout.flexibleWidth = 0f;
        buttonLayout.minHeight = 32f;
        buttonLayout.preferredHeight = 32f;

        button.onClick.AddListener(() =>
        {
            if (VolcanoManager.Instance != null)
            {
                VolcanoManager.Instance.SpawnRandomVolcano();
            }
            else
            {
                var mgr = FindAnyObjectByType<VolcanoManager>();
                if (mgr != null) mgr.SpawnRandomVolcano();
            }
        });

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(hudRoot);
    }

    private void CreatePrebioticUI()
    {
        RectTransform hudRoot = transform as RectTransform;
        if (hudRoot == null) return;

        // Container Panel
        GameObject containerGo = new GameObject("PrebioticPanel", typeof(RectTransform));
        containerGo.transform.SetParent(hudRoot, false);

        LayoutElement containerLayout = containerGo.AddComponent<LayoutElement>();
        containerLayout.minHeight = 150f;
        containerLayout.preferredHeight = 150f;
        containerLayout.flexibleWidth = 1f;

        VerticalLayoutGroup vertical = containerGo.AddComponent<VerticalLayoutGroup>();
        vertical.childAlignment = TextAnchor.UpperLeft;
        vertical.childControlWidth = true;
        vertical.childControlHeight = false;
        vertical.spacing = 8f;

        // Header + Progress Label
        GameObject labelGo = new GameObject("PrebioticProgressLabel", typeof(RectTransform));
        labelGo.transform.SetParent(containerGo.transform, false);

        LayoutElement labelLayout = labelGo.AddComponent<LayoutElement>();
        labelLayout.minHeight = 85f;
        labelLayout.preferredHeight = 85f;
        labelLayout.flexibleWidth = 1f;

        prebioticProgressText = labelGo.AddComponent<TextMeshProUGUI>();
        prebioticProgressText.fontSize = 15;
        prebioticProgressText.fontStyle = FontStyles.Bold;
        prebioticProgressText.color = new Color(0.26f, 0.82f, 0.72f, 1f);
        prebioticProgressText.alignment = TextAlignmentOptions.Left;
        prebioticProgressText.enableWordWrapping = true;
        prebioticProgressText.overflowMode = TextOverflowModes.Overflow;

        // Buttons Grid / Row
        GameObject btnRowGo = new GameObject("PrebioticButtonsRow", typeof(RectTransform));
        btnRowGo.transform.SetParent(containerGo.transform, false);

        LayoutElement btnRowLayout = btnRowGo.AddComponent<LayoutElement>();
        btnRowLayout.minHeight = 40f;
        btnRowLayout.preferredHeight = 40f;
        btnRowLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup btnLayout = btnRowGo.AddComponent<HorizontalLayoutGroup>();
        btnLayout.childAlignment = TextAnchor.MiddleLeft;
        btnLayout.childControlWidth = true;
        btnLayout.childControlHeight = true;
        btnLayout.spacing = 8f;

        CreatePrebioticActionButton(
            btnRowGo.transform,
            "Miller-Urey",
            "Expérience de Miller-Urey (1953)",
            "Simule des décharges électriques (éclairs) traversant une atmosphère réductrice riche en vapeur d'eau et gaz volcaniques. Synthétise principalement la Glycine et l'Alanine.",
            new Color(0.2f, 0.6f, 0.86f, 1f),
            () =>
            {
                if (PrebioticMiniGameController.Instance != null)
                    PrebioticMiniGameController.Instance.TriggerLightningDischarge();
            });

        CreatePrebioticActionButton(
            btnRowGo.transform,
            "Hydrothermale",
            "Sources Hydrothermales",
            "Simule les réactions chimiques au niveau des évents hydrothermaux sous-marins (fumeurs noirs). Synthétise l'Acide Aspartique et l'Acide Glutamique sous haute pression et chaleur.",
            new Color(0.86f, 0.35f, 0.2f, 1f),
            () =>
            {
                if (PrebioticMiniGameController.Instance != null)
                    PrebioticMiniGameController.Instance.TriggerHydrothermalVent();
            });

        CreatePrebioticActionButton(
            btnRowGo.transform,
            "Météorite",
            "Bombardement Météoritique",
            "Simule l'apport extraterrestre de molécules organiques (ex: météorite de Murchison). Apporte de la Sérine et de la Valine à la soupe primitive.",
            new Color(0.6f, 0.35f, 0.75f, 1f),
            () =>
            {
                if (PrebioticMiniGameController.Instance != null)
                    PrebioticMiniGameController.Instance.TriggerMeteorBombardment();
            });

        CreatePrebioticActionButton(
            btnRowGo.transform,
            "Catalyse UV",
            "Catalyse Photochimique UV",
            "Simule l'impact des rayons ultraviolets solaires non filtrés sur la surface océanique primitive. Synthétise la Leucine et l'Isoleucine.",
            new Color(0.9f, 0.75f, 0.2f, 1f),
            () =>
            {
                if (PrebioticMiniGameController.Instance != null)
                    PrebioticMiniGameController.Instance.TriggerUvCatalysis();
            });

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(hudRoot);
    }

    private void CreatePrebioticActionButton(Transform parent, string title, string tooltipTitle, string tooltipBody, Color btnColor, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonGo = new GameObject($"Btn_{title}", typeof(RectTransform));
        buttonGo.transform.SetParent(parent, false);

        Image buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = btnColor;

        Button button = buttonGo.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        prebioticActionButtons.Add(button);

        UIHoverTooltipTrigger hoverTrigger = buttonGo.AddComponent<UIHoverTooltipTrigger>();
        hoverTrigger.title = tooltipTitle;
        hoverTrigger.body = tooltipBody;
        hoverTrigger.hudController = this;

        ColorBlock cb = button.colors;
        cb.normalColor = btnColor;
        cb.highlightedColor = btnColor * 1.2f;
        cb.pressedColor = btnColor * 0.8f;
        cb.disabledColor = new Color(btnColor.r * 0.35f, btnColor.g * 0.35f, btnColor.b * 0.35f, 0.5f);
        button.colors = cb;

        GameObject buttonTextGo = new GameObject("Text", typeof(RectTransform));
        buttonTextGo.transform.SetParent(buttonGo.transform, false);
        TextMeshProUGUI buttonText = buttonTextGo.AddComponent<TextMeshProUGUI>();
        buttonText.text = title;
        buttonText.fontSize = 14;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.textWrappingMode = TextWrappingModes.Normal;

        RectTransform textRect = buttonTextGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        LayoutElement buttonLayout = buttonGo.AddComponent<LayoutElement>();
        buttonLayout.minWidth = 90f;
        buttonLayout.preferredWidth = 115f;
        buttonLayout.flexibleWidth = 1f;
        buttonLayout.minHeight = 32f;
        buttonLayout.preferredHeight = 34f;

        button.onClick.AddListener(action);
    }

    private void CreatePrebioticCompletionWindowUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Container anchored on Middle-Left (gauche centre)
        prebioticCompletionPanel = new GameObject("PrebioticCompletionPanel", typeof(RectTransform));
        prebioticCompletionPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = prebioticCompletionPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(25f, 0f);
        panelRect.sizeDelta = new Vector2(380f, 480f);

        // Panel Background
        Image bg = prebioticCompletionPanel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.12f, 0.16f, 0.95f); // Dark cyan / slate background

        VerticalLayoutGroup layout = prebioticCompletionPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 18, 18);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = prebioticCompletionPanel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Title: Félicitations !
        GameObject titleGo = new GameObject("CompletionTitle", typeof(RectTransform));
        titleGo.transform.SetParent(prebioticCompletionPanel.transform, false);
        TextMeshProUGUI titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "FÉLICITATIONS !";
        titleText.fontSize = 22;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.98f, 0.85f, 0.35f, 1f); // Gold
        titleText.alignment = TextAlignmentOptions.Center;

        // Subtitle: Synthèse des Acides Aminés Complétée
        GameObject subtitleGo = new GameObject("CompletionSubtitle", typeof(RectTransform));
        subtitleGo.transform.SetParent(prebioticCompletionPanel.transform, false);
        TextMeshProUGUI subtitleText = subtitleGo.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "Création des Acides Aminés (100%)";
        subtitleText.fontSize = 16;
        subtitleText.fontStyle = FontStyles.Bold;
        subtitleText.color = new Color(0.30f, 0.90f, 0.75f, 1f); // Teal
        subtitleText.alignment = TextAlignmentOptions.Center;

        // Description Message
        GameObject msgGo = new GameObject("CompletionMessage", typeof(RectTransform));
        msgGo.transform.SetParent(prebioticCompletionPanel.transform, false);
        TextMeshProUGUI msgText = msgGo.AddComponent<TextMeshProUGUI>();
        msgText.text = "Toutes les briques fondamentales du vivant (les 8 acides aminés essentiels : Glycine, Alanine, Acides Aspartique et Glutamique, Sérine, Valine, Leucine et Isoleucine) ont été avec succès synthétisées dans la soupe primitive !";
        msgText.fontSize = 13;
        msgText.fontStyle = FontStyles.Normal;
        msgText.color = new Color(0.92f, 0.95f, 0.97f, 1f);
        msgText.alignment = TextAlignmentOptions.Left;
        msgText.enableWordWrapping = true;

        // Image Container for Custom Downloaded Image
        GameObject imageGo = new GameObject("CompletionImage", typeof(RectTransform));
        imageGo.transform.SetParent(prebioticCompletionPanel.transform, false);
        prebioticCompletionImageComponent = imageGo.AddComponent<Image>();
        prebioticCompletionImageComponent.preserveAspect = true;

        LayoutElement imageLayout = imageGo.AddComponent<LayoutElement>();
        imageLayout.minWidth = 320f;
        imageLayout.preferredWidth = 340f;
        imageLayout.minHeight = 180f;
        imageLayout.preferredHeight = 200f;
        imageLayout.flexibleWidth = 1f;

        if (prebioticCompletionSprite != null)
        {
            prebioticCompletionImageComponent.sprite = prebioticCompletionSprite;
        }
        else
        {
            Sprite loadedSprite = Resources.Load<Sprite>("PrebioticAminoAcids");
            if (loadedSprite != null) prebioticCompletionImageComponent.sprite = loadedSprite;
        }

        // Close / Continue Button
        GameObject closeBtnGo = new GameObject("CompletionCloseBtn", typeof(RectTransform));
        closeBtnGo.transform.SetParent(prebioticCompletionPanel.transform, false);

        Image closeBtnImg = closeBtnGo.AddComponent<Image>();
        closeBtnImg.color = new Color(0.18f, 0.65f, 0.55f, 1f);

        Button closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnImg;
        closeBtn.onClick.AddListener(HidePrebioticCompletionWindow);

        LayoutElement btnLayout = closeBtnGo.AddComponent<LayoutElement>();
        btnLayout.minHeight = 36f;
        btnLayout.preferredHeight = 38f;
        btnLayout.flexibleWidth = 1f;

        GameObject btnTextGo = new GameObject("Text", typeof(RectTransform));
        btnTextGo.transform.SetParent(closeBtnGo.transform, false);
        TextMeshProUGUI btnText = btnTextGo.AddComponent<TextMeshProUGUI>();
        btnText.text = "Fermer";
        btnText.fontSize = 15;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;

        RectTransform btnTextRect = btnTextGo.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;

        prebioticCompletionPanel.SetActive(false);
    }

    public void ShowPrebioticCompletionWindow()
    {
        hasShownPrebioticCompletionWindow = true;

        if (prebioticCompletionPanel == null)
        {
            CreatePrebioticCompletionWindowUI();
        }

        if (prebioticCompletionPanel != null)
        {
            if (prebioticCompletionSprite != null && prebioticCompletionImageComponent != null)
            {
                prebioticCompletionImageComponent.sprite = prebioticCompletionSprite;
            }
            else if (prebioticCompletionImageComponent != null && prebioticCompletionImageComponent.sprite == null)
            {
                Sprite loaded = Resources.Load<Sprite>("PrebioticAminoAcids");
                if (loaded != null) prebioticCompletionImageComponent.sprite = loaded;
            }

            prebioticCompletionPanel.SetActive(true);
            prebioticCompletionPanel.transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(prebioticCompletionPanel.GetComponent<RectTransform>());
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LogEvent("Prebiotic Synthesis Complete", "100% of amino acids synthesized.");
        }
    }

    public void HidePrebioticCompletionWindow()
    {
        if (prebioticCompletionPanel != null)
        {
            prebioticCompletionPanel.SetActive(false);
        }
    }

    private void CheckPrebioticCompletion()
    {
        if (hasShownPrebioticCompletionWindow) return;

        if (PrebioticMiniGameController.Instance != null && PrebioticMiniGameController.Instance.TotalProgress >= 0.999f)
        {
            ShowPrebioticCompletionWindow();
        }
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.OnEpochChanged += HandleEpochChanged;
        }

        if (PrebioticMiniGameController.Instance != null)
        {
            PrebioticMiniGameController.Instance.OnPrebioticProgressUpdated += CheckPrebioticCompletion;
        }

        RefreshAll();
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnEpochChanged -= HandleEpochChanged;
        }

        if (PrebioticMiniGameController.Instance != null)
        {
            PrebioticMiniGameController.Instance.OnPrebioticProgressUpdated -= CheckPrebioticCompletion;
        }
    }

    private void Update()
    {
        if (gameManager == null) return;

        refreshTimer += Time.deltaTime;
        if (refreshTimer < refreshIntervalSeconds) return;

        refreshTimer = 0f;
        RefreshAll();
    }

    private void HandleEpochChanged(PlanetEpoch _)
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (gameManager == null) return;

        SetText(epochText, $"Epoch: {gameManager.CurrentEpoch}");
        SetText(sessionText, $"Progression: {gameManager.SessionProgress * 100f:0.0}%");

        float remainingHours = gameManager.SessionRemainingHoursAtCurrentSpeed;
        SetText(
            remainingTimeText,
            float.IsInfinity(remainingHours)
                ? "Temps restant: infini (pause vitesse)"
                : $"Temps restant: {remainingHours:0.00} h");

        SetText(internalTempText, $"Temp. interne: {gameManager.InternalTemperature:0.0} K ({gameManager.InternalTemperature - 273.15f:0.0} °C)");
        SetText(surfaceTempText, $"Temp. surface: {gameManager.SurfaceTemperature:0.0} K ({gameManager.SurfaceTemperature - 273.15f:0.0} °C)");
        SetText(pressureText, $"Pression: {gameManager.Pressure:0.000} atm");
        SetText(waterText, $"Eau liquide: {gameManager.WaterRatio * 100f:0.00}%");
        SetText(tectonicText, $"Activite tectonique: {gameManager.TectonicActivity * 100f:0.00}%");

        bool isPrebiotic = gameManager != null && gameManager.CurrentEpoch == PlanetEpoch.Prebiotic;

        if (atmosphereCompositionText != null)
        {
            atmosphereCompositionText.enableWordWrapping = true;
            atmosphereCompositionText.overflowMode = TextOverflowModes.Overflow;
            atmosphereCompositionText.fontSize = 15f;

            float total = gameManager.Pressure;
            float h2oPct = total > 0 ? (gameManager.WaterVaporPressure / total) * 100f : 0f;
            float co2Pct = total > 0 ? (gameManager.Co2Pressure / total) * 100f : 0f;
            float n2Pct = total > 0 ? (gameManager.NitrogenPressure / total) * 100f : 0f;
            float otherPct = total > 0 ? (gameManager.OtherGasesPressure / total) * 100f : 0f;

            atmosphereCompositionText.text = $"Composition Atmosphérique ({gameManager.CurrentEpoch}) - Total: {total:0.00} atm :\n" +
                $" • H2O (Vapeur d'eau) : {gameManager.WaterVaporPressure:0.00} atm ({h2oPct:0.1}%)\n" +
                $" • CO2 (Dioxyde de carbone) : {gameManager.Co2Pressure:0.00} atm ({co2Pct:0.1}%)\n" +
                $" • N2 (Azote) : {gameManager.NitrogenPressure:0.00} atm ({n2Pct:0.1}%)\n" +
                $" • Gaz réduits / volcaniques (CH4, NH3, SO2) : {gameManager.OtherGasesPressure:0.00} atm ({otherPct:0.1}%)";
        }

        if (sessionSlider != null) sessionSlider.value = gameManager.SessionProgress;
        if (waterSlider != null) waterSlider.value = gameManager.WaterRatio;
        if (tectonicSlider != null) tectonicSlider.value = gameManager.TectonicActivity;

        if (prebioticProgressText != null)
        {
            if (PrebioticMiniGameController.Instance != null && isPrebiotic)
            {
                var p = PrebioticMiniGameController.Instance;
                prebioticProgressText.text = $"<b>[Synthèse Pre-Biotique - Avancement : {p.TotalProgress * 100f:0.0}%]</b>\n" +
                    $" • Glycine: {p.Glycine:0}% | Alanine: {p.Alanine:0}% | Ac. Aspartique: {p.AsparticAcid:0}%\n" +
                    $" • Ac. Glutamique: {p.GlutamicAcid:0}% | Sérine: {p.Serine:0}% | Valine: {p.Valine:0}%\n" +
                    $" • Leucine: {p.Leucine:0}% | Isoleucine: {p.Isoleucine:0}%";
            }
            else
            {
                prebioticProgressText.text = $"<b>[Synthèse Pre-Biotique (Verrouillée - Attente de l'époque Prebiotic)]</b>\n" +
                    $" • Glycine: 0% | Alanine: 0% | Ac. Aspartique: 0%\n" +
                    $" • Ac. Glutamique: 0% | Sérine: 0% | Valine: 0%\n" +
                    $" • Leucine: 0% | Isoleucine: 0%";
            }
        }

        // Update button interactable states based on epoch
        if (volcanoButton != null) volcanoButton.interactable = true;
        foreach (Button btn in prebioticActionButtons)
        {
            if (btn != null) btn.interactable = isPrebiotic;
        }

        var meteorCtrl = GetComponent<MeteorEventController>();
        if (meteorCtrl != null && meteorCtrl.MeteorButton != null)
        {
            meteorCtrl.MeteorButton.interactable = true;
        }

        string hex = GetEpochHex(gameManager.CurrentEpoch);
        if (epochBadgeImage != null && ColorUtility.TryParseHtmlString(hex, out Color c))
        {
            epochBadgeImage.color = c;
        }

        // Clear hex badge text to avoid displaying raw hex string on HUD
        SetText(epochBadgeHexText, string.Empty);

        CheckPrebioticCompletion();
    }

    private string GetEpochHex(PlanetEpoch epoch)
    {
        return epoch switch
        {
            PlanetEpoch.Hadean => hadeanHex,
            PlanetEpoch.CrustFormation => crustFormationHex,
            PlanetEpoch.VolcanicAge => volcanicAgeHex,
            PlanetEpoch.ProtoOcean => protoOceanHex,
            PlanetEpoch.TectonicDrift => tectonicDriftHex,
            PlanetEpoch.Prebiotic => prebioticHex,
            _ => fallbackHex
        };
    }

    private static void SetText(TMP_Text label, string content)
    {
        if (label != null) label.text = content;
    }

    public void BindFromHierarchy(Transform root)
    {
        if (root == null) return;

        epochText = FindTmpDeep(root, "EpochLabel") ?? epochText;
        sessionText = FindTmpDeep(root, "SessionLabel") ?? sessionText;
        remainingTimeText = FindTmpDeep(root, "RemainingLabel") ?? remainingTimeText;
        internalTempText = FindTmpDeep(root, "InternalTempLabel") ?? internalTempText;
        surfaceTempText = FindTmpDeep(root, "SurfaceTempLabel") ?? surfaceTempText;
        pressureText = FindTmpDeep(root, "PressureLabel") ?? pressureText;
        waterText = FindTmpDeep(root, "WaterLabel") ?? waterText;
        tectonicText = FindTmpDeep(root, "TectonicLabel") ?? tectonicText;

        TMP_Text foundAtm = FindTmpDeep(root, "AtmosphereCompositionLabel") ?? FindTmpDeep(root, "CompAtmTxt");
        if (foundAtm != null) atmosphereCompositionText = foundAtm;

        epochBadgeHexText = FindTmpDeep(root, "EpochHexText") ?? epochBadgeHexText;

        sessionSlider = FindSliderDeep(root, "SessionSlider") ?? sessionSlider;
        waterSlider = FindSliderDeep(root, "WaterSlider") ?? waterSlider;
        tectonicSlider = FindSliderDeep(root, "TectonicSlider") ?? tectonicSlider;

        Transform badge = FindDeep(root, "EpochBadge");
        if (badge != null) epochBadgeImage = badge.GetComponent<Image>();

        if (gameManager == null) gameManager = GameManager.Instance;
    }

    private static TMP_Text FindTmpDeep(Transform root, string childName)
    {
        Transform found = FindDeep(root, childName);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private static Slider FindSliderDeep(Transform root, string childName)
    {
        Transform found = FindDeep(root, childName);
        return found != null ? found.GetComponent<Slider>() : null;
    }

    private static Transform FindDeep(Transform root, string childName)
    {
        if (root.name == childName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), childName);
            if (found != null) return found;
        }

        return null;
    }
}
