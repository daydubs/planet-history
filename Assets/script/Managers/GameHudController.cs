using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHoverTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float grandeur = 5f; // multiplicateur de grandeur.
    public string title;
    public string body;
    public GameHudController hudController;

    public float Grandeur { get => grandeur; set => grandeur = value; }

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

public class MinimapInteractionHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IScrollHandler
{
    public GameHudController hudController;

    public void OnPointerDown(PointerEventData eventData)
    {
        // Intercept pointer down to capture drag focus on minimap
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (hudController == null) return;

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null) return;

        Vector2 size = rectTransform.rect.size;
        if (size.x <= 0f || size.y <= 0f) return;

        // Convert screen drag delta to normalized minimap delta
        Vector2 deltaNormalized = new Vector2(eventData.delta.x / size.x, eventData.delta.y / size.y);
        hudController.PanMinimap(deltaNormalized);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (hudController == null) return;

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null) return;

        // Calculate normalized cursor position within RawImage [0..1]
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);

        Vector2 size = rectTransform.rect.size;
        Vector2 pivotNormalized = new Vector2(
            (localPoint.x - rectTransform.rect.xMin) / size.x,
            (localPoint.y - rectTransform.rect.yMin) / size.y);

        pivotNormalized.x = Mathf.Clamp01(pivotNormalized.x);
        pivotNormalized.y = Mathf.Clamp01(pivotNormalized.y);

        // Determine zoom direction (mouse wheel scroll)
        float scrollDelta = eventData.scrollDelta.y;
        if (Mathf.Abs(scrollDelta) < 0.01f)
        {
            scrollDelta = eventData.scrollDelta.x;
        }

        if (scrollDelta > 0.01f)
        {
            hudController.ZoomMinimap(1.2f, pivotNormalized);
        }
        else if (scrollDelta < -0.01f)
        {
            hudController.ZoomMinimap(1f / 1.2f, pivotNormalized);
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

    [Header("Minimap")]
    [SerializeField] private RawImage minimapRawImage;
    [SerializeField] private int minimapWidth = 384;
    [SerializeField] private int minimapHeight = 192;
    [SerializeField, Min(0.1f)] private float minimapRefreshInterval = 5.0f;
    private float minimapRefreshTimer;
    private bool isMinimapDirty = true;
    private float lastMinimapSurfaceTemp = -1f;
    private float lastMinimapWaterRatio = -1f;
    private float lastMinimapZoom = -1f;
    private Vector2 lastMinimapPanOffset = new Vector2(-999f, -999f);

    [Header("Palette (Hex)")]
    [SerializeField] private string hadeanHex = "#D1495B";
    [SerializeField] private string crustFormationHex = "#F79256";
    [SerializeField] private string volcanicAgeHex = "#F9C74F";
    [SerializeField] private string protoOceanHex = "#43AA8B";
    [SerializeField] private string tectonicDriftHex = "#4D96FF";
    [SerializeField] private string prebioticHex = "#2A9D8F";
    [SerializeField] private string photosynthesisHex = "#228B22";
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

        BindFromHierarchy(transform);

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

        // Dynamically attach PrebioticMiniGameController & ProtocellSimulationManager to scene if missing
        if (FindAnyObjectByType<PrebioticMiniGameController>() == null)
        {
            GameObject prebioticObj = new GameObject("PrebioticMiniGameController");
            prebioticObj.AddComponent<PrebioticMiniGameController>();
        }

        if (FindAnyObjectByType<ProtocellSimulationManager>() == null)
        {
            GameObject simObj = new GameObject("ProtocellSimulationManager");
            simObj.AddComponent<ProtocellSimulationManager>();
        }

        if (FindAnyObjectByType<ProtocellMicroViewUI>() == null)
        {
            GameObject microObj = new GameObject("ProtocellMicroViewUI");
            microObj.AddComponent<ProtocellMicroViewUI>();
        }

        // Dynamically attach GameMenuController to scene if missing
        if (FindAnyObjectByType<GameMenuController>() == null && GameMenuController.Instance == null)
        {
            GameObject menuObj = new GameObject("GameMenuController");
            menuObj.AddComponent<GameMenuController>();
        }

        CreatePauseButtonUI();
        CreateMinimapUI();
        CreateVolcanoUI();
        CreatePrebioticUI();
        CreateTooltipUI();
        CreatePrebioticCompletionWindowUI();
    }

    private void CreatePauseButtonUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject pauseBtnGo = new GameObject("HUD_PauseButton", typeof(RectTransform));
        pauseBtnGo.transform.SetParent(canvas.transform, false);

        RectTransform btnRect = pauseBtnGo.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 1f);
        btnRect.anchorMax = new Vector2(1f, 1f);
        btnRect.pivot = new Vector2(1f, 1f);
        btnRect.anchoredPosition = new Vector2(-20f, -20f);
        btnRect.sizeDelta = new Vector2(130f, 40f);

        Image btnImg = pauseBtnGo.AddComponent<Image>();
        btnImg.color = new Color(0.18f, 0.25f, 0.35f, 0.90f);

        Button button = pauseBtnGo.AddComponent<Button>();
        button.targetGraphic = btnImg;

        ColorBlock cb = button.colors;
        cb.normalColor = new Color(0.18f, 0.25f, 0.35f, 0.90f);
        cb.highlightedColor = new Color(0.28f, 0.38f, 0.50f, 1.0f);
        cb.pressedColor = new Color(0.12f, 0.18f, 0.25f, 1.0f);
        button.colors = cb;

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(pauseBtnGo.transform, false);

        TextMeshProUGUI btnText = textGo.AddComponent<TextMeshProUGUI>();
        btnText.text = "⏸ Pause";
        btnText.fontSize = 16;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        button.onClick.AddListener(() =>
        {
            if (GameMenuController.Instance != null)
            {
                GameMenuController.Instance.TogglePauseMenu();
            }
        });
    }

    private GameObject minimapPanel;
    private Texture2D minimapTexture;
    private Color32[] minimapPixels32;
    private CubeSphereTerrain cachedTerrain;
    private bool minimapExpanded = true;

    private static readonly float oceanR = 0.02f, oceanG = 0.12f, oceanB = 0.32f;
    private static readonly float shoreR = 0.72f, shoreG = 0.68f, shoreB = 0.45f;
    private static readonly float landR = 0.16f, landG = 0.35f, landB = 0.14f;
    private static readonly float mtnR = 0.38f, mtnG = 0.33f, mtnB = 0.29f;
    private static readonly float iceR = 0.92f, iceG = 0.95f, iceB = 1.0f;
    private static readonly float dryShoreR = 0.12f, dryShoreG = 0.12f, dryShoreB = 0.13f;
    private static readonly float dryLandR = 0.18f, dryLandG = 0.18f, dryLandB = 0.20f;
    private static readonly float dryMtnR = 0.35f, dryMtnG = 0.35f, dryMtnB = 0.35f;
    private static readonly float lavaR = 0.875f, lavaG = 0.15f, lavaB = 0.0f;

    // Minimap Zoom & Pan State
    private float minimapZoom = 1.0f;
    private Vector2 minimapPanOffset = Vector2.zero; // UV center offset (0..1)
    private const float MinMinimapZoom = 1.0f;
    private const float MaxMinimapZoom = 8.0f;

    private void CreateMinimapUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Minimap panel container positioned top-right below pause button
        minimapPanel = new GameObject("HUD_MinimapPanel", typeof(RectTransform));
        minimapPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = minimapPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-20f, -70f);
        panelRect.sizeDelta = new Vector2(760f, 410f);

        Image panelBg = minimapPanel.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.12f, 0.16f, 0.92f); // Dark slate frame

        // Header Bar (Title + Controls + Toggle)
        GameObject headerGo = new GameObject("MinimapHeader", typeof(RectTransform));
        headerGo.transform.SetParent(minimapPanel.transform, false);

        RectTransform headerRect = headerGo.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, 32f);

        GameObject titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(headerGo.transform, false);

        TextMeshProUGUI titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "🗺 Carte Couleur";
        titleText.fontSize = 13;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.92f, 0.95f, 0.98f, 1f);
        titleText.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(10f, 0f);
        titleRect.offsetMax = new Vector2(-120f, 0f);

        // Header Buttons Container (Reset, Collapse)
        GameObject controlsGo = new GameObject("MinimapControls", typeof(RectTransform));
        controlsGo.transform.SetParent(headerGo.transform, false);

        RectTransform controlsRect = controlsGo.GetComponent<RectTransform>();
        controlsRect.anchorMin = new Vector2(1f, 0.5f);
        controlsRect.anchorMax = new Vector2(1f, 0.5f);
        controlsRect.pivot = new Vector2(1f, 0.5f);
        controlsRect.anchoredPosition = new Vector2(-5f, 0f);
        controlsRect.sizeDelta = new Vector2(60f, 24f);

        HorizontalLayoutGroup controlsLayout = controlsGo.AddComponent<HorizontalLayoutGroup>();
        controlsLayout.spacing = 4f;
        controlsLayout.childAlignment = TextAnchor.MiddleRight;
        controlsLayout.childControlWidth = false;
        controlsLayout.childControlHeight = false;

        Button resetBtn = CreateMinimapHeaderButton(controlsGo.transform, "⟲", "Réinitialiser Vue", ResetMinimapView);

        // Toggle Minimap Collapse Button
        GameObject toggleBtnGo = new GameObject("ToggleBtn", typeof(RectTransform));
        toggleBtnGo.transform.SetParent(controlsGo.transform, false);

        RectTransform toggleRect = toggleBtnGo.GetComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(24f, 22f);

        Image toggleImg = toggleBtnGo.AddComponent<Image>();
        toggleImg.color = new Color(0.22f, 0.30f, 0.40f, 0.9f);

        Button toggleBtn = toggleBtnGo.AddComponent<Button>();
        toggleBtn.targetGraphic = toggleImg;

        GameObject toggleTextGo = new GameObject("Text", typeof(RectTransform));
        toggleTextGo.transform.SetParent(toggleBtnGo.transform, false);
        TextMeshProUGUI toggleText = toggleTextGo.AddComponent<TextMeshProUGUI>();
        toggleText.text = "▼";
        toggleText.fontSize = 12;
        toggleText.fontStyle = FontStyles.Bold;
        toggleText.color = Color.white;
        toggleText.alignment = TextAlignmentOptions.Center;

        RectTransform toggleTextRect = toggleTextGo.GetComponent<RectTransform>();
        toggleTextRect.anchorMin = Vector2.zero;
        toggleTextRect.anchorMax = Vector2.one;
        toggleTextRect.sizeDelta = Vector2.zero;

        // Minimap RawImage display area
        GameObject rawImageGo = new GameObject("MinimapRawImage", typeof(RectTransform));
        rawImageGo.transform.SetParent(minimapPanel.transform, false);

        RectTransform rawRect = rawImageGo.GetComponent<RectTransform>();
        rawRect.anchorMin = new Vector2(0.5f, 0f);
        rawRect.anchorMax = new Vector2(0.5f, 0f);
        rawRect.pivot = new Vector2(0.5f, 0f);
        rawRect.anchoredPosition = new Vector2(0f, 8f);
        rawRect.sizeDelta = new Vector2(728f, 364f);

        minimapRawImage = rawImageGo.AddComponent<RawImage>();
        minimapRawImage.color = Color.white;

        // Attach interactive zoom and pan controller
        MinimapInteractionHandler interactionHandler = rawImageGo.AddComponent<MinimapInteractionHandler>();
        interactionHandler.hudController = this;

        // Overlay Zoom + and Zoom - buttons directly on the minimap
        CreateMinimapOverlayButton(rawImageGo.transform, "+", new Vector2(1f, 1f), new Vector2(-46f, -10f), () => ZoomMinimap(1.25f, new Vector2(0.5f, 0.5f)));
        CreateMinimapOverlayButton(rawImageGo.transform, "-", new Vector2(1f, 1f), new Vector2(-10f, -10f), () => ZoomMinimap(1f / 1.25f, new Vector2(0.5f, 0.5f)));

        // Toggle click handler
        toggleBtn.onClick.AddListener(() =>
        {
            minimapExpanded = !minimapExpanded;
            rawImageGo.SetActive(minimapExpanded);
            panelRect.sizeDelta = new Vector2(760f, minimapExpanded ? 410f : 32f);
            toggleText.text = minimapExpanded ? "▼" : "▲";
            if (minimapExpanded)
            {
                UpdateMinimapTexture(force: true);
            }
        });
    }

    public void ZoomMinimap(float factor, Vector2 pivotNormalized)
    {
        float prevZoom = minimapZoom;
        minimapZoom = Mathf.Clamp(minimapZoom * factor, MinMinimapZoom, MaxMinimapZoom);

        if (Mathf.Approximately(minimapZoom, prevZoom)) return;

        if (minimapZoom <= MinMinimapZoom)
        {
            minimapPanOffset = Vector2.zero;
            isMinimapDirty = true;
            return;
        }

        // Adjust panOffset to zoom centered around pivotNormalized (in 0..1 RawImage normalized space)
        float uPivotPrev = (pivotNormalized.x - 0.5f) / prevZoom + 0.5f + minimapPanOffset.x;
        float vPivotPrev = (pivotNormalized.y - 0.5f) / prevZoom + 0.5f + minimapPanOffset.y;

        float uPivotNew = (pivotNormalized.x - 0.5f) / minimapZoom + 0.5f;
        float vPivotNew = (pivotNormalized.y - 0.5f) / minimapZoom + 0.5f;

        minimapPanOffset.x = uPivotPrev - uPivotNew;
        minimapPanOffset.y = vPivotPrev - vPivotNew;

        ClampMinimapPanOffset();
        isMinimapDirty = true;
    }

    public void PanMinimap(Vector2 deltaNormalized)
    {
        if (minimapZoom <= 1.001f) return;

        minimapPanOffset.x -= deltaNormalized.x / minimapZoom;
        minimapPanOffset.y -= deltaNormalized.y / minimapZoom;

        ClampMinimapPanOffset();
        isMinimapDirty = true;
    }

    public void ResetMinimapView()
    {
        minimapZoom = 1.0f;
        minimapPanOffset = Vector2.zero;
        isMinimapDirty = true;
        UpdateMinimapTexture(force: true);
    }

    private void ClampMinimapPanOffset()
    {
        // Wrap X (longitude) seamlessly
        minimapPanOffset.x = Mathf.Repeat(minimapPanOffset.x, 1.0f);

        // Clamp Y (latitude) so viewport stays within valid [0, 1] range
        float halfVRange = 0.5f / minimapZoom;
        float maxOffsetV = 0.5f - halfVRange;
        if (maxOffsetV <= 0f)
        {
            minimapPanOffset.y = 0f;
        }
        else
        {
            minimapPanOffset.y = Mathf.Clamp(minimapPanOffset.y, -maxOffsetV, maxOffsetV);
        }
    }

    private Button CreateMinimapOverlayButton(Transform parent, string symbol, Vector2 anchor, Vector2 anchoredPos, UnityEngine.Events.UnityAction action)
    {
        GameObject btnGo = new GameObject($"OverlayBtn_{symbol}", typeof(RectTransform));
        btnGo.transform.SetParent(parent, false);

        RectTransform rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(32f, 32f);

        Image img = btnGo.AddComponent<Image>();
        img.color = new Color(0.10f, 0.15f, 0.22f, 0.85f);

        Button button = btnGo.AddComponent<Button>();
        button.targetGraphic = img;

        ColorBlock cb = button.colors;
        cb.normalColor = new Color(0.10f, 0.15f, 0.22f, 0.85f);
        cb.highlightedColor = new Color(0.25f, 0.38f, 0.55f, 0.95f);
        cb.pressedColor = new Color(0.05f, 0.08f, 0.12f, 1.0f);
        button.colors = cb;

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(btnGo.transform, false);

        TextMeshProUGUI txt = textGo.AddComponent<TextMeshProUGUI>();
        txt.text = symbol;
        txt.fontSize = 20;
        txt.fontStyle = FontStyles.Bold;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        button.onClick.AddListener(action);
        return button;
    }

    private Button CreateMinimapHeaderButton(Transform parent, string symbol, string tooltipText, UnityEngine.Events.UnityAction action)
    {
        GameObject btnGo = new GameObject($"Btn_{symbol}", typeof(RectTransform));
        btnGo.transform.SetParent(parent, false);

        RectTransform rect = btnGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(24f, 22f);

        Image img = btnGo.AddComponent<Image>();
        img.color = new Color(0.22f, 0.30f, 0.40f, 0.9f);

        Button button = btnGo.AddComponent<Button>();
        button.targetGraphic = img;

        ColorBlock cb = button.colors;
        cb.normalColor = new Color(0.22f, 0.30f, 0.40f, 0.9f);
        cb.highlightedColor = new Color(0.32f, 0.45f, 0.60f, 1.0f);
        cb.pressedColor = new Color(0.15f, 0.20f, 0.30f, 1.0f);
        button.colors = cb;

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(btnGo.transform, false);

        TextMeshProUGUI txt = textGo.AddComponent<TextMeshProUGUI>();
        txt.text = symbol;
        txt.fontSize = 13;
        txt.fontStyle = FontStyles.Bold;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        button.onClick.AddListener(action);
        return button;
    }

    private void UpdateMinimapTexture(bool force = false)
    {
        if (minimapRawImage == null || !minimapRawImage.gameObject.activeInHierarchy || !minimapExpanded) return;

        if (!force && !isMinimapDirty)
        {
            if (minimapRefreshTimer < minimapRefreshInterval) return;
        }

        float surfaceTemp = gameManager != null ? gameManager.SurfaceTemperature : 1800f;
        float waterRatio = gameManager != null ? gameManager.WaterRatio : 0f;

        // Skip regeneration if parameters haven't changed and not forced
        if (!force && !isMinimapDirty &&
            Mathf.Approximately(surfaceTemp, lastMinimapSurfaceTemp) &&
            Mathf.Approximately(waterRatio, lastMinimapWaterRatio) &&
            Mathf.Approximately(minimapZoom, lastMinimapZoom) &&
            Vector2.Distance(minimapPanOffset, lastMinimapPanOffset) < 1e-4f)
        {
            return;
        }

        minimapRefreshTimer = 0f;
        isMinimapDirty = false;

        if (cachedTerrain == null)
        {
            cachedTerrain = FindAnyObjectByType<CubeSphereTerrain>();
        }

        if (cachedTerrain == null || cachedTerrain.Field == null) return;

        PlanetHeightField field = cachedTerrain.Field;
        int width = Mathf.Clamp(minimapWidth, 32, 512);
        int height = Mathf.Clamp(minimapHeight, 16, 256);

        if (minimapTexture == null || minimapTexture.width != width || minimapTexture.height != height)
        {
            minimapTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            minimapPixels32 = new Color32[width * height];
            minimapRawImage.texture = minimapTexture;
        }

        lastMinimapSurfaceTemp = surfaceTemp;
        lastMinimapWaterRatio = waterRatio;
        lastMinimapZoom = minimapZoom;
        lastMinimapPanOffset = minimapPanOffset;

        float fieldWidth = field.Width;
        float fieldHeight = field.Height;

        float invHeightMinus1 = 1f / (height - 1);
        float invWidthMinus1 = 1f / (width - 1);
        float maxFieldX = fieldWidth - 1;
        float maxFieldY = fieldHeight - 1;

        bool evaluateLava = surfaceTemp > 500f;
        float lavaMask = evaluateLava ? SmoothStep(500f, 1400f, surfaceTemp) : 0f;
        bool evaluateWater = waterRatio > 0.001f;
        float waterLevel = (0.02f * 1.5f) * waterRatio;

        for (int y = 0; y < height; y++)
        {
            float normY = y * invHeightMinus1;
            float v = (normY - 0.5f) / minimapZoom + 0.5f + minimapPanOffset.y;
            v = Mathf.Clamp01(v);

            float lat01 = Mathf.Abs(v - 0.5f) * 2f;
            int fieldY = Mathf.Clamp(Mathf.RoundToInt(v * maxFieldY), 0, (int)maxFieldY);

            int rowOffset = y * width;

            for (int x = 0; x < width; x++)
            {
                float normX = x * invWidthMinus1;
                float u = (normX - 0.5f) / minimapZoom + 0.5f + minimapPanOffset.x;
                u = Mathf.Repeat(u, 1.0f);

                int fieldX = Mathf.Clamp(Mathf.RoundToInt(u * maxFieldX), 0, (int)maxFieldX);

                float h = field.GetCurrent(fieldX, fieldY);
                minimapPixels32[x + rowOffset] = EvaluateHeightAlbedoFast(h, lat01, waterRatio, lavaMask, evaluateLava, evaluateWater, waterLevel);
            }
        }

        // Draw Prebiotic Hotspot Markers on Minimap
        if (ProtocellSimulationManager.Instance != null && ProtocellSimulationManager.Instance.ActiveZones != null)
        {
            foreach (var zone in ProtocellSimulationManager.Instance.ActiveZones)
            {
                float uNorm = zone.longitudeDeg / 360f + 0.5f;
                float vNorm = zone.latitudeDeg / 180f + 0.5f;

                // Adjust for minimap zoom and pan offset
                float uView = (uNorm - 0.5f - minimapPanOffset.x) * minimapZoom + 0.5f;
                float vView = (vNorm - 0.5f - minimapPanOffset.y) * minimapZoom + 0.5f;

                uView = Mathf.Repeat(uView, 1.0f);

                if (vView >= 0f && vView <= 1f)
                {
                    int px = Mathf.RoundToInt(uView * (width - 1));
                    int py = Mathf.RoundToInt(vView * (height - 1));

                    Color32 markerColor = zone.zoneType == PrebioticZoneType.HydrothermalVent
                        ? new Color32(0, 240, 255, 255)   // Bright Cyan
                        : new Color32(255, 215, 0, 255);  // Bright Gold

                    int markerRadius = 2;
                    for (int dy = -markerRadius; dy <= markerRadius; dy++)
                    {
                        for (int dx = -markerRadius; dx <= markerRadius; dx++)
                        {
                            if (dx * dx + dy * dy <= markerRadius * markerRadius + 1)
                            {
                                int mx = px + dx;
                                int my = py + dy;
                                if (mx >= 0 && mx < width && my >= 0 && my < height)
                                {
                                    minimapPixels32[my * width + mx] = markerColor;
                                }
                            }
                        }
                    }
                }
            }
        }

        minimapTexture.SetPixelData(minimapPixels32, 0);
        minimapTexture.Apply(false, false);
    }

    private static Color32 EvaluateHeightAlbedoFast(float height, float latitude01, float waterRatio, float lavaMask, bool evaluateLava, bool evaluateWater, float waterLevel)
    {
        float tShoreLand = SmoothStep(0.08f, 0.35f, height);
        float tLandMtn = SmoothStep(0.35f, 0.70f, height);

        float stdLandR = shoreR + (landR - shoreR) * tShoreLand;
        float stdLandG = shoreG + (landG - shoreG) * tShoreLand;
        float stdLandB = shoreB + (landB - shoreB) * tShoreLand;

        float stdMtnR = stdLandR + (mtnR - stdLandR) * tLandMtn;
        float stdMtnG = stdLandG + (mtnG - stdLandG) * tLandMtn;
        float stdMtnB = stdLandB + (mtnB - stdLandB) * tLandMtn;

        float volLandR = dryShoreR + (dryLandR - dryShoreR) * tShoreLand;
        float volLandG = dryShoreG + (dryLandG - dryShoreG) * tShoreLand;
        float volLandB = dryShoreB + (dryLandB - dryShoreB) * tShoreLand;

        float volMtnR = volLandR + (dryMtnR - volLandR) * tLandMtn;
        float volMtnG = volLandG + (dryMtnG - volLandG) * tLandMtn;
        float volMtnB = volLandB + (dryMtnB - volLandB) * tLandMtn;

        float finalR = volMtnR + (stdMtnR - volMtnR) * waterRatio;
        float finalG = volMtnG + (stdMtnG - volMtnG) * waterRatio;
        float finalB = volMtnB + (stdMtnB - volMtnB) * waterRatio;

        if (latitude01 > 0.74f && waterRatio > 0.001f)
        {
            float ice = SmoothStep(0.74f, 0.90f, latitude01) * waterRatio;
            if (ice > 0.001f)
            {
                finalR += (iceR - finalR) * ice;
                finalG += (iceG - finalG) * ice;
                finalB += (iceB - finalB) * ice;
            }
        }

        if (evaluateLava && lavaMask > 0.001f)
        {
            float heightLavaBias = SmoothStep(0.5f, 0.1f, height);
            float effectiveLavaMask = Mathf.Clamp01(lavaMask * (0.3f + 0.7f * heightLavaBias));

            if (effectiveLavaMask > 0.001f)
            {
                finalR += (lavaR - finalR) * effectiveLavaMask;
                finalG += (lavaG - finalG) * effectiveLavaMask;
                finalB += (lavaB - finalB) * effectiveLavaMask;
            }
        }

        if (evaluateWater && height < waterLevel + 0.02f)
        {
            float depth = waterLevel - height;
            if (depth > 0f)
            {
                float waterBlend = Mathf.Clamp01(depth * 100f);
                finalR += (oceanR - finalR) * waterBlend;
                finalG += (oceanG - finalG) * waterBlend;
                finalB += (oceanB - finalB) * waterBlend;
            }
        }

        return new Color32(
            (byte)(Mathf.Clamp01(finalR) * 255f),
            (byte)(Mathf.Clamp01(finalG) * 255f),
            (byte)(Mathf.Clamp01(finalB) * 255f),
            255
        );
    }

    private static float SmoothStep(float min, float max, float value)
    {
        float t = Mathf.Clamp01((value - min) / (max - min));
        return t * t * (3f - 2f * t);
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

    public static Transform GetOrCreateEventsRow(RectTransform hudRoot)
    {
        Transform eventsRow = hudRoot.Find("EventsRow");
        if (eventsRow != null) return eventsRow;

        GameObject rowGo = new GameObject("EventsRow", typeof(RectTransform));
        rowGo.transform.SetParent(hudRoot, false);

        Transform prebioticPanel = hudRoot.Find("PrebioticPanel");
        if (prebioticPanel != null)
        {
            rowGo.transform.SetSiblingIndex(prebioticPanel.GetSiblingIndex());
        }

        RectTransform rowRect = rowGo.GetComponent<RectTransform>();
        rowRect.localScale = Vector3.one;

        LayoutElement rowLayout = rowGo.AddComponent<LayoutElement>();
        rowLayout.minHeight = 36f;
        rowLayout.preferredHeight = 36f;
        rowLayout.flexibleHeight = 0f;
        rowLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup horizontal = rowGo.AddComponent<HorizontalLayoutGroup>();
        horizontal.childAlignment = TextAnchor.MiddleLeft;
        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandWidth = true;
        horizontal.childForceExpandHeight = false;
        horizontal.spacing = 10f;

        return rowGo.transform;
    }

    private void CreateVolcanoUI()
    {
        RectTransform hudRoot = transform as RectTransform;
        if (hudRoot == null) return;

        Transform eventsRow = GetOrCreateEventsRow(hudRoot);

        Transform oldRow = hudRoot.Find("VolcanoRow");
        if (oldRow != null && oldRow != eventsRow) Destroy(oldRow.gameObject);

        Transform existingItem = eventsRow.Find("VolcanoItem");
        if (existingItem != null) Destroy(existingItem.gameObject);

        GameObject volContainer = new GameObject("VolcanoItem", typeof(RectTransform));
        volContainer.transform.SetParent(eventsRow, false);

        LayoutElement itemLayout = volContainer.AddComponent<LayoutElement>();
        itemLayout.minHeight = 36f;
        itemLayout.preferredHeight = 36f;
        itemLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup horizontal = volContainer.AddComponent<HorizontalLayoutGroup>();
        horizontal.childAlignment = TextAnchor.MiddleLeft;
        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = false;
        horizontal.spacing = 6f;

        // Label
        GameObject labelGo = new GameObject("VolcanoLabel", typeof(RectTransform));
        labelGo.transform.SetParent(volContainer.transform, false);
        TextMeshProUGUI labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.text = "🌋 Volcan :";
        labelText.enableAutoSizing = false;
        labelText.fontSize = 13.5f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = new Color(0.88f, 0.90f, 0.94f, 1f);
        labelText.alignment = TextAlignmentOptions.Left;

        LayoutElement labelLayout = labelGo.AddComponent<LayoutElement>();
        labelLayout.minWidth = 75f;
        labelLayout.preferredWidth = 85f;
        labelLayout.flexibleWidth = 0f;

        // Button
        GameObject buttonGo = new GameObject("VolcanoButton", typeof(RectTransform));
        buttonGo.transform.SetParent(volContainer.transform, false);

        Image buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = new Color(0.88f, 0.38f, 0.12f, 1f);

        Button button = buttonGo.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        volcanoButton = button;

        ColorBlock cb = button.colors;
        cb.normalColor = new Color(0.88f, 0.38f, 0.12f, 1f);
        cb.highlightedColor = new Color(0.98f, 0.48f, 0.22f, 1f);
        cb.pressedColor = new Color(0.68f, 0.28f, 0.05f, 1f);
        cb.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
        button.colors = cb;

        GameObject buttonTextGo = new GameObject("Text", typeof(RectTransform));
        buttonTextGo.transform.SetParent(buttonGo.transform, false);
        TextMeshProUGUI buttonText = buttonTextGo.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Créer Volcan";
        buttonText.enableAutoSizing = false;
        buttonText.fontSize = 13f;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = buttonTextGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        LayoutElement buttonLayout = buttonGo.AddComponent<LayoutElement>();
        buttonLayout.minWidth = 100f;
        buttonLayout.preferredWidth = 130f;
        buttonLayout.flexibleWidth = 1f;
        buttonLayout.minHeight = 32f;
        buttonLayout.preferredHeight = 34f;

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
        containerLayout.minHeight = 80f;
        containerLayout.preferredHeight = -1f;
        containerLayout.flexibleWidth = 1f;

        VerticalLayoutGroup vertical = containerGo.AddComponent<VerticalLayoutGroup>();
        vertical.childAlignment = TextAnchor.UpperLeft;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;
        vertical.spacing = 6f;

        // Header + Progress Label
        GameObject labelGo = new GameObject("PrebioticProgressLabel", typeof(RectTransform));
        labelGo.transform.SetParent(containerGo.transform, false);

        LayoutElement labelLayout = labelGo.AddComponent<LayoutElement>();
        labelLayout.minHeight = 40f;
        labelLayout.preferredHeight = -1f;
        labelLayout.flexibleWidth = 1f;

        prebioticProgressText = labelGo.AddComponent<TextMeshProUGUI>();
        prebioticProgressText.enableAutoSizing = false;
        prebioticProgressText.fontSize = 13f;
        prebioticProgressText.lineSpacing = 2f;
        prebioticProgressText.fontStyle = FontStyles.Bold;
        prebioticProgressText.color = new Color(0.26f, 0.82f, 0.72f, 1f);
        prebioticProgressText.alignment = TextAlignmentOptions.Left;
        prebioticProgressText.enableWordWrapping = true;
        prebioticProgressText.overflowMode = TextOverflowModes.Overflow;

        // Buttons Grid / Row
        GameObject btnRowGo = new GameObject("PrebioticButtonsRow", typeof(RectTransform));
        btnRowGo.transform.SetParent(containerGo.transform, false);

        LayoutElement btnRowLayout = btnRowGo.AddComponent<LayoutElement>();
        btnRowLayout.minHeight = 34f;
        btnRowLayout.preferredHeight = 36f;
        btnRowLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup btnLayout = btnRowGo.AddComponent<HorizontalLayoutGroup>();
        btnLayout.childAlignment = TextAnchor.MiddleLeft;
        btnLayout.childControlWidth = true;
        btnLayout.childControlHeight = true;
        btnLayout.childForceExpandWidth = true;
        btnLayout.childForceExpandHeight = false;
        btnLayout.spacing = 6f;

        CreatePrebioticActionButton(
            btnRowGo.transform,
            "🔬 Vue Micro",
            "Inspection Micro-Biotique",
            "Ouvre la vue microscope interactive pour observer l'auto-assemblage des lipides, les vésicules bilipidiques, la réplication ARN et la sélection naturelle en temps réel.",
            new Color(0.15f, 0.75f, 0.65f, 1f),
            () =>
            {
                if (ProtocellMicroViewUI.Instance != null)
                {
                    ProtocellMicroViewUI.Instance.ToggleMicroView();
                }
            });

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
        cb.highlightedColor = btnColor * 1.15f;
        cb.pressedColor = btnColor * 0.8f;
        cb.disabledColor = new Color(btnColor.r * 0.3f, btnColor.g * 0.3f, btnColor.b * 0.3f, 0.4f);
        button.colors = cb;

        GameObject buttonTextGo = new GameObject("Text", typeof(RectTransform));
        buttonTextGo.transform.SetParent(buttonGo.transform, false);
        TextMeshProUGUI buttonText = buttonTextGo.AddComponent<TextMeshProUGUI>();
        buttonText.text = title;
        buttonText.enableAutoSizing = false;
        buttonText.fontSize = 11.5f;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.textWrappingMode = TextWrappingModes.Normal;
        buttonText.overflowMode = TextOverflowModes.Overflow;

        RectTransform textRect = buttonTextGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        LayoutElement buttonLayout = buttonGo.AddComponent<LayoutElement>();
        buttonLayout.minWidth = 72f;
        buttonLayout.preferredWidth = 96f;
        buttonLayout.flexibleWidth = 1f;
        buttonLayout.minHeight = 32f;
        buttonLayout.preferredHeight = 36f;

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

        float dt = Time.deltaTime;

        minimapRefreshTimer += dt;
        if (minimapRefreshTimer >= minimapRefreshInterval)
        {
            minimapRefreshTimer = 0f;
            UpdateMinimapTexture(force: false);
        }

        refreshTimer += dt;
        if (refreshTimer < refreshIntervalSeconds) return;

        refreshTimer = 0f;
        RefreshAll();
    }

    private void HandleEpochChanged(PlanetEpoch _)
    {
        RefreshAll();
        isMinimapDirty = true;
        UpdateMinimapTexture(force: true);
    }

    private void RefreshAll()
    {
        if (gameManager == null) return;

        if (epochText != null)
        {
            epochText.enableAutoSizing = false;
            epochText.fontSize = 18f;
            epochText.fontStyle = FontStyles.Bold;
            epochText.text = $"Époque : {GetEpochDisplayName(gameManager.CurrentEpoch)}";
        }

        if (sessionText != null)
        {
            sessionText.enableAutoSizing = false;
            sessionText.fontSize = 15f;
            sessionText.text = $"Progression: {gameManager.SessionProgress * 100f:0.0}%";
        }

        float remainingHours = gameManager.SessionRemainingHoursAtCurrentSpeed;
        if (remainingTimeText != null)
        {
            remainingTimeText.enableAutoSizing = false;
            remainingTimeText.fontSize = 15f;
            remainingTimeText.text = float.IsInfinity(remainingHours)
                ? "Temps restant: infini (pause vitesse)"
                : $"Temps restant: {remainingHours:0.00} h";
        }

        if (internalTempText != null)
        {
            internalTempText.enableAutoSizing = false;
            internalTempText.fontSize = 15f;
            internalTempText.enableWordWrapping = false;
            internalTempText.overflowMode = TextOverflowModes.Overflow;
            internalTempText.text = $"Temp. interne: {gameManager.InternalTemperature:0.0} K ({gameManager.InternalTemperature - 273.15f:0.0} °C)";
        }

        string thermalExtras = "";
        if (gameManager.GreenhouseDeltaTemp > 0.1f)
        {
            thermalExtras += $" [Serre: +{gameManager.GreenhouseDeltaTemp:0.0} K]";
        }
        if (gameManager.ImpactThermalPulse > 0.1f)
        {
            thermalExtras += $" [Choc Météore: +{gameManager.ImpactThermalPulse:0.0} K]";
        }

        if (surfaceTempText != null)
        {
            surfaceTempText.enableAutoSizing = false;
            surfaceTempText.fontSize = 15f;
            surfaceTempText.enableWordWrapping = true;
            surfaceTempText.overflowMode = TextOverflowModes.Overflow;
            surfaceTempText.text = $"Temp. surface: {gameManager.SurfaceTemperature:0.0} K ({gameManager.SurfaceTemperature - 273.15f:0.0} °C){thermalExtras}";
        }

        if (pressureText != null)
        {
            pressureText.enableAutoSizing = false;
            pressureText.fontSize = 15f;
            pressureText.text = $"Pression: {gameManager.Pressure:0.000} atm";
        }

        if (waterText != null)
        {
            waterText.enableAutoSizing = false;
            waterText.fontSize = 15f;
            waterText.text = $"Eau liquide: {gameManager.WaterRatio * 100f:0.00}%";
        }

        if (tectonicText != null)
        {
            tectonicText.enableAutoSizing = false;
            tectonicText.fontSize = 15f;
            tectonicText.textWrappingMode = TextWrappingModes.Normal;
            tectonicText.overflowMode = TextOverflowModes.Overflow;
            tectonicText.text = $"Activité tectonique: {gameManager.TectonicActivity * 100f:0.00}%";
        }

        bool isPrebiotic = gameManager != null && gameManager.CurrentEpoch == PlanetEpoch.Prebiotic;

        if (atmosphereCompositionText != null)
        {
            atmosphereCompositionText.enableAutoSizing = false;
            atmosphereCompositionText.fontSize = 12.5f;
            atmosphereCompositionText.lineSpacing = 2f;
            atmosphereCompositionText.enableWordWrapping = true;
            atmosphereCompositionText.overflowMode = TextOverflowModes.Overflow;

            float total = gameManager.Pressure;
            float h2oPct = total > 0 ? (gameManager.WaterVaporPressure / total) * 100f : 0f;
            float co2Pct = total > 0 ? (gameManager.Co2Pressure / total) * 100f : 0f;
            float n2Pct = total > 0 ? (gameManager.NitrogenPressure / total) * 100f : 0f;
            float otherPct = total > 0 ? (gameManager.OtherGasesPressure / total) * 100f : 0f;
            float o2Pct = total > 0 ? (gameManager.OxygenPressure / total) * 100f : 0f;

            atmosphereCompositionText.text = $"Composition Atmosphérique ({GetEpochDisplayName(gameManager.CurrentEpoch)}) - Total: {total:0.00} atm :\n" +
                $" • H2O (Vapeur d'eau) : {gameManager.WaterVaporPressure:0.00} atm ({h2oPct:0.1}%)\n" +
                $" • CO2 (Dioxyde de carbone) : {gameManager.Co2Pressure:0.00} atm ({co2Pct:0.1}%)\n" +
                $" • N2 (Azote) : {gameManager.NitrogenPressure:0.00} atm ({n2Pct:0.1}%)\n" +
                $" • Gaz réduits (CH4, NH3, SO2) : {gameManager.OtherGasesPressure:0.00} atm ({otherPct:0.1}%)\n" +
                $" • O2 (Oxygène) : {gameManager.OxygenPressure:0.00} atm ({o2Pct:0.1}%)";
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
        if (ColorUtility.TryParseHtmlString(hex, out Color c))
        {
            if (epochBadgeImage != null) epochBadgeImage.color = c;
            if (epochText != null) epochText.color = c;
        }

        // Clear hex badge text to avoid displaying raw hex string on HUD (only if distinct from epochText)
        if (epochBadgeHexText != null && epochBadgeHexText != epochText)
        {
            SetText(epochBadgeHexText, string.Empty);
        }

        CheckPrebioticCompletion();
    }

    private string GetEpochDisplayName(PlanetEpoch epoch)
    {
        return epoch switch
        {
            PlanetEpoch.Hadean => "Hadéen",
            PlanetEpoch.CrustFormation => "Formation de la Croûte",
            PlanetEpoch.VolcanicAge => "Âge Volcanique",
            PlanetEpoch.ProtoOcean => "Proto-Océan",
            PlanetEpoch.TectonicDrift => "Dérive Tectonique",
            PlanetEpoch.Prebiotic => "Prébiotique",
            PlanetEpoch.Photosynthesis => "Photosynthèse",
            _ => epoch.ToString()
        };
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
            PlanetEpoch.Photosynthesis => photosynthesisHex,
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
