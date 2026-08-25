using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProtocellMicroViewUI : MonoBehaviour
{
    public static ProtocellMicroViewUI Instance { get; private set; }

    [Header("UI Panel Elements")]
    [SerializeField] private GameObject microViewPanel;
    [SerializeField] private TMP_Text zoneTitleText;
    [SerializeField] private TMP_Text envStatsText;
    [SerializeField] private TMP_Text populationStatsText;
    [SerializeField] private RawImage vesicleViewportRawImage;

    [Header("Controls")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button prevZoneButton;
    [SerializeField] private Button nextZoneButton;
    [SerializeField] private Button tempPlusButton;
    [SerializeField] private Button tempMinusButton;
    [SerializeField] private Button phPlusButton;
    [SerializeField] private Button phMinusButton;
    [SerializeField] private Button feedNutrientsButton;
    [SerializeField] private Button infoButton;
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private Button closeInfoButton;

    // Viewport Texture & 2D Simulation Rendering
    private Texture2D viewportTexture;
    private Color32[] viewportPixels;
    private const int ViewportWidth = 380;
    private const int ViewportHeight = 380;

    private readonly List<Vector2> bubblePositions = new List<Vector2>();
    private readonly List<Vector2> bubbleVelocities = new List<Vector2>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateMicroViewUIWindow();
    }

    private void CreateMicroViewUIWindow()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        microViewPanel = new GameObject("HUD_ProtocellMicroViewPanel", typeof(RectTransform));
        microViewPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = microViewPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(1050f, 680f);

        Image bg = microViewPanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.08f, 0.12f, 0.96f); // Deep dark blue background

        // Main Layout: Header, Content Row (Viewport Left, Telemetry & Controls Right)
        VerticalLayoutGroup mainLayout = microViewPanel.AddComponent<VerticalLayoutGroup>();
        mainLayout.padding = new RectOffset(20, 20, 18, 18);
        mainLayout.spacing = 14f;
        mainLayout.childControlWidth = true;
        mainLayout.childControlHeight = false;

        // Header Row
        GameObject headerGo = new GameObject("HeaderRow", typeof(RectTransform));
        headerGo.transform.SetParent(microViewPanel.transform, false);

        HorizontalLayoutGroup headerLayout = headerGo.AddComponent<HorizontalLayoutGroup>();
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;

        zoneTitleText = headerGo.AddComponent<TextMeshProUGUI>();
        zoneTitleText.text = "🔬 VUE MICRO-BIOTIQUE : Auto-Assemblage & Protocellules";
        zoneTitleText.fontSize = 20;
        zoneTitleText.fontStyle = FontStyles.Bold;
        zoneTitleText.alignment = TextAlignmentOptions.Center;
        zoneTitleText.color = new Color(0.3f, 0.85f, 0.75f, 1f);

        LayoutElement headerLayoutElem = headerGo.AddComponent<LayoutElement>();
        headerLayoutElem.minHeight = 32f;
        headerLayoutElem.preferredHeight = 32f;

        infoButton = CreateStyledButton(headerGo.transform, "?", ToggleInfoPanel, new Color(0.2f, 0.4f, 0.8f, 1f));
        LayoutElement infoBtnLayout = infoButton.gameObject.AddComponent<LayoutElement>();
        infoBtnLayout.minWidth = 32f;
        infoBtnLayout.preferredWidth = 32f;
        infoBtnLayout.minHeight = 32f;
        infoBtnLayout.preferredHeight = 32f;

        // Content Row
        GameObject contentRow = new GameObject("ContentRow", typeof(RectTransform));
        contentRow.transform.SetParent(microViewPanel.transform, false);

        HorizontalLayoutGroup contentLayout = contentRow.AddComponent<HorizontalLayoutGroup>();
        contentLayout.spacing = 20f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;

        LayoutElement contentRowLayout = contentRow.AddComponent<LayoutElement>();
        contentRowLayout.minHeight = 520f;
        contentRowLayout.preferredHeight = 540f;

        // Left Container: Viewport RawImage (Vesicle 2D rendering)
        GameObject leftCol = new GameObject("LeftColumn", typeof(RectTransform));
        leftCol.transform.SetParent(contentRow.transform, false);

        VerticalLayoutGroup leftLayout = leftCol.AddComponent<VerticalLayoutGroup>();
        leftLayout.spacing = 12f;
        leftLayout.childControlWidth = true;
        leftLayout.childControlHeight = false;
        leftLayout.childAlignment = TextAnchor.UpperCenter;

        LayoutElement leftColLayout = leftCol.AddComponent<LayoutElement>();
        leftColLayout.minWidth = 390f;
        leftColLayout.preferredWidth = 390f;

        GameObject rawImgGo = new GameObject("VesicleViewport", typeof(RectTransform));
        rawImgGo.transform.SetParent(leftCol.transform, false);
        vesicleViewportRawImage = rawImgGo.AddComponent<RawImage>();

        LayoutElement viewportLayout = rawImgGo.AddComponent<LayoutElement>();
        viewportLayout.minWidth = 380f;
        viewportLayout.preferredWidth = 380f;
        viewportLayout.minHeight = 380f;
        viewportLayout.preferredHeight = 380f;

        // Zone Selector Row below Viewport
        GameObject zoneSelRow = new GameObject("ZoneSelectorRow", typeof(RectTransform));
        zoneSelRow.transform.SetParent(leftCol.transform, false);

        HorizontalLayoutGroup zoneSelLayout = zoneSelRow.AddComponent<HorizontalLayoutGroup>();
        zoneSelLayout.spacing = 10f;
        zoneSelLayout.childControlWidth = true;
        zoneSelLayout.childControlHeight = true;

        prevZoneButton = CreateStyledButton(zoneSelRow.transform, "◀ Zone Préc.", () => SwitchZone(-1), new Color(0.2f, 0.35f, 0.45f, 1f));
        nextZoneButton = CreateStyledButton(zoneSelRow.transform, "Zone Suiv. ▶", () => SwitchZone(1), new Color(0.2f, 0.35f, 0.45f, 1f));

        LayoutElement prevLayout = prevZoneButton.gameObject.AddComponent<LayoutElement>();
        prevLayout.minHeight = 38f;
        LayoutElement nextLayout = nextZoneButton.gameObject.AddComponent<LayoutElement>();
        nextLayout.minHeight = 38f;

        // Bottom-Left Close Button Row
        GameObject closeRow = new GameObject("CloseRow", typeof(RectTransform));
        closeRow.transform.SetParent(leftCol.transform, false);

        HorizontalLayoutGroup closeRowLayout = closeRow.AddComponent<HorizontalLayoutGroup>();
        closeRowLayout.childControlWidth = false;
        closeRowLayout.childControlHeight = true;
        closeRowLayout.childAlignment = TextAnchor.LowerLeft;

        closeButton = CreateStyledButton(closeRow.transform, "✕ Fermer", HideMicroView, new Color(0.75f, 0.22f, 0.22f, 1f));
        LayoutElement closeBtnLayout = closeButton.gameObject.AddComponent<LayoutElement>();
        closeBtnLayout.minWidth = 130f;
        closeBtnLayout.preferredWidth = 130f;
        closeBtnLayout.minHeight = 36f;
        closeBtnLayout.preferredHeight = 36f;

        // Right Container: Telemetries & Environmental Control Sliders/Buttons
        GameObject rightCol = new GameObject("RightColumn", typeof(RectTransform));
        rightCol.transform.SetParent(contentRow.transform, false);

        VerticalLayoutGroup rightLayout = rightCol.AddComponent<VerticalLayoutGroup>();
        rightLayout.spacing = 10f;
        rightLayout.childControlWidth = true;
        rightLayout.childControlHeight = false;
        rightLayout.childForceExpandWidth = true;
        rightLayout.childForceExpandHeight = false;

        LayoutElement rightColLayout = rightCol.AddComponent<LayoutElement>();
        rightColLayout.flexibleWidth = 1f;

        // Card 1: Conditions Locales
        GameObject envCardGo = new GameObject("EnvStatsCard", typeof(RectTransform));
        envCardGo.transform.SetParent(rightCol.transform, false);
        Image envBg = envCardGo.AddComponent<Image>();
        envBg.color = new Color(0.08f, 0.14f, 0.22f, 0.85f);

        VerticalLayoutGroup envCardLayout = envCardGo.AddComponent<VerticalLayoutGroup>();
        envCardLayout.padding = new RectOffset(14, 14, 10, 10);
        envCardLayout.childControlWidth = true;
        envCardLayout.childControlHeight = true;
        envCardLayout.childForceExpandWidth = true;
        envCardLayout.childForceExpandHeight = true;

        LayoutElement envCardLayoutEl = envCardGo.AddComponent<LayoutElement>();
        envCardLayoutEl.minHeight = 135f;
        envCardLayoutEl.preferredHeight = 135f;

        GameObject envStatsGo = new GameObject("EnvStatsLabel", typeof(RectTransform));
        envStatsGo.transform.SetParent(envCardGo.transform, false);
        envStatsText = envStatsGo.AddComponent<TextMeshProUGUI>();
        envStatsText.fontSize = 13.5f;
        envStatsText.lineSpacing = 3f;
        envStatsText.color = new Color(0.92f, 0.94f, 0.96f, 1f);

        // Card 2: Télémétrie de Population
        GameObject popCardGo = new GameObject("PopStatsCard", typeof(RectTransform));
        popCardGo.transform.SetParent(rightCol.transform, false);
        Image popBg = popCardGo.AddComponent<Image>();
        popBg.color = new Color(0.06f, 0.18f, 0.15f, 0.85f);

        VerticalLayoutGroup popCardLayout = popCardGo.AddComponent<VerticalLayoutGroup>();
        popCardLayout.padding = new RectOffset(14, 14, 10, 10);
        popCardLayout.childControlWidth = true;
        popCardLayout.childControlHeight = true;
        popCardLayout.childForceExpandWidth = true;
        popCardLayout.childForceExpandHeight = true;

        LayoutElement popCardLayoutEl = popCardGo.AddComponent<LayoutElement>();
        popCardLayoutEl.minHeight = 135f;
        popCardLayoutEl.preferredHeight = 135f;

        GameObject popStatsGo = new GameObject("PopStatsLabel", typeof(RectTransform));
        popStatsGo.transform.SetParent(popCardGo.transform, false);
        populationStatsText = popStatsGo.AddComponent<TextMeshProUGUI>();
        populationStatsText.fontSize = 13.5f;
        populationStatsText.lineSpacing = 3f;
        populationStatsText.color = new Color(0.35f, 0.92f, 0.65f, 1f);

        // Control Section Header
        GameObject ctrlHeaderGo = new GameObject("CtrlHeaderLabel", typeof(RectTransform));
        ctrlHeaderGo.transform.SetParent(rightCol.transform, false);
        TMP_Text ctrlHeaderTxt = ctrlHeaderGo.AddComponent<TextMeshProUGUI>();
        ctrlHeaderTxt.text = "⚙️ CONTRÔLES DU MILIEU & INJECTION";
        ctrlHeaderTxt.fontSize = 13.5f;
        ctrlHeaderTxt.fontStyle = FontStyles.Bold;
        ctrlHeaderTxt.color = new Color(0.85f, 0.9f, 0.95f, 1f);
        ctrlHeaderTxt.alignment = TextAlignmentOptions.Left;

        LayoutElement ctrlHeaderLayout = ctrlHeaderGo.AddComponent<LayoutElement>();
        ctrlHeaderLayout.minHeight = 22f;
        ctrlHeaderLayout.preferredHeight = 22f;

        // Env Control Buttons Grid
        GameObject ctrlGrid = new GameObject("ControlGrid", typeof(RectTransform));
        ctrlGrid.transform.SetParent(rightCol.transform, false);

        GridLayoutGroup grid = ctrlGrid.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(285f, 40f);
        grid.spacing = new Vector2(10f, 8f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        LayoutElement gridLayout = ctrlGrid.AddComponent<LayoutElement>();
        gridLayout.minHeight = 88f;
        gridLayout.preferredHeight = 88f;

        tempPlusButton = CreateStyledButton(ctrlGrid.transform, "Temp. +5°C", () => ChangeEnv(5f, 0f), new Color(0.85f, 0.4f, 0.2f, 1f));
        tempMinusButton = CreateStyledButton(ctrlGrid.transform, "Temp. -5°C", () => ChangeEnv(-5f, 0f), new Color(0.2f, 0.5f, 0.85f, 1f));
        phPlusButton = CreateStyledButton(ctrlGrid.transform, "pH +0.5", () => ChangeEnv(0f, 0.5f), new Color(0.7f, 0.3f, 0.85f, 1f));
        phMinusButton = CreateStyledButton(ctrlGrid.transform, "pH -0.5", () => ChangeEnv(0f, -0.5f), new Color(0.3f, 0.7f, 0.4f, 1f));

        feedNutrientsButton = CreateStyledButton(rightCol.transform, "🧪 Injecter Acides Aminés & Lipides", () => FeedNutrients(20f), new Color(0.2f, 0.7f, 0.6f, 1f));
        LayoutElement feedLayout = feedNutrientsButton.gameObject.AddComponent<LayoutElement>();
        feedLayout.minHeight = 44f;
        feedLayout.preferredHeight = 44f;

        // Explication / Info Card below controls
        GameObject explCardGo = new GameObject("ExplicationCard", typeof(RectTransform));
        explCardGo.transform.SetParent(rightCol.transform, false);
        Image explBg = explCardGo.AddComponent<Image>();
        explBg.color = new Color(0.12f, 0.12f, 0.16f, 0.85f);

        VerticalLayoutGroup explCardLayout = explCardGo.AddComponent<VerticalLayoutGroup>();
        explCardLayout.padding = new RectOffset(10, 10, 8, 8);
        explCardLayout.childControlWidth = true;
        explCardLayout.childControlHeight = true;
        explCardLayout.childForceExpandWidth = true;
        explCardLayout.childForceExpandHeight = true;

        LayoutElement explLayout = explCardGo.AddComponent<LayoutElement>();
        explLayout.minHeight = 65f;
        explLayout.preferredHeight = 65f;

        GameObject explStatsGo = new GameObject("ExplicationLabel", typeof(RectTransform));
        explStatsGo.transform.SetParent(explCardGo.transform, false);
        TextMeshProUGUI explStatsText = explStatsGo.AddComponent<TextMeshProUGUI>();
        explStatsText.fontSize = 11.5f;
        explStatsText.lineSpacing = 2f;
        explStatsText.color = new Color(0.8f, 0.85f, 0.9f, 1f);
        explStatsText.text = "<i><b>Effets environnementaux :</b></i>\n• <b>Chaleur:</b> Accélère les réactions, augmente la perméabilité membranaire (risque d'instabilité).\n• <b>pH:</b> Affecte la charge des lipides et le repliement de l'ARN.\n• <b>Injection Nutriments:</b> Fournit des blocs de construction pour la croissance.";

        InitializeViewportTexture();

        CreateInfoPanel();

        microViewPanel.SetActive(false);
    }

    private void CreateInfoPanel()
    {
        infoPanel = new GameObject("HUD_ProtocellInfoPanel", typeof(RectTransform));
        infoPanel.transform.SetParent(microViewPanel.transform, false);

        RectTransform panelRect = infoPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(800f, 500f);

        Image bg = infoPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.12f, 0.18f, 0.98f); // Slightly lighter than main panel

        VerticalLayoutGroup layout = infoPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(25, 25, 25, 25);
        layout.spacing = 15f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        // Title
        GameObject titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(infoPanel.transform, false);
        TextMeshProUGUI titleTxt = titleGo.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "ℹ️ PROCESSUS DE REPRODUCTION CELLULAIRE";
        titleTxt.fontSize = 20;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color = new Color(0.4f, 0.85f, 0.95f, 1f);

        LayoutElement titleLayout = titleGo.AddComponent<LayoutElement>();
        titleLayout.minHeight = 35f;
        titleLayout.preferredHeight = 35f;

        // Content text
        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(infoPanel.transform, false);
        TextMeshProUGUI contentTxt = contentGo.AddComponent<TextMeshProUGUI>();
        contentTxt.fontSize = 14f;
        contentTxt.lineSpacing = 3f;
        contentTxt.color = Color.white;
        contentTxt.text =
            "<b>1. Auto-assemblage (Micelles → Vésicules) :</b> Les lipides s'organisent spontanément en sphères creuses (vésicules) pour protéger leurs queues hydrophobes de l'eau.\n\n" +
            "<b>2. Perméabilité Membranaire :</b> La membrane doit être assez stable pour ne pas éclater, mais assez perméable pour laisser entrer les nutriments (acides aminés, nucléotides).\n\n" +
            "<b>3. Réplication ARN :</b> À l'intérieur, les brins d'ARN (ribozymes) utilisent les nutriments pour se copier. Ce processus est sensible aux variations de température et de pH.\n\n" +
            "<b>4. Division Cellulaire :</b> Lorsque la vésicule grossit grâce à l'incorporation de nouveaux lipides et que l'ARN se réplique, l'instabilité physique provoque sa division en deux cellules filles.\n\n" +
            "<i>Votre but :</i> Ajuster l'environnement (Température, pH) et fournir des nutriments pour optimiser ce cycle fragile avant que la vésicule ne se désintègre !";

        // Close button
        closeInfoButton = CreateStyledButton(infoPanel.transform, "Compris !", ToggleInfoPanel, new Color(0.2f, 0.6f, 0.4f, 1f));
        LayoutElement closeLayout = closeInfoButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.minHeight = 45f;
        closeLayout.preferredHeight = 45f;

        infoPanel.SetActive(false);
    }

    private void ToggleInfoPanel()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(!infoPanel.activeSelf);
            if (infoPanel.activeSelf)
            {
                infoPanel.transform.SetAsLastSibling();
            }
        }
    }

    private Button CreateStyledButton(Transform parent, string label, UnityEngine.Events.UnityAction action, Color color)
    {
        GameObject btnGo = new GameObject($"Btn_{label}", typeof(RectTransform));
        btnGo.transform.SetParent(parent, false);

        Image img = btnGo.AddComponent<Image>();
        img.color = color;

        Button button = btnGo.AddComponent<Button>();
        button.targetGraphic = img;

        ColorBlock cb = button.colors;
        cb.normalColor = color;
        cb.highlightedColor = color * 1.2f;
        cb.pressedColor = color * 0.8f;
        button.colors = cb;

        GameObject txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(btnGo.transform, false);

        TextMeshProUGUI txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 13.5f;
        txt.fontStyle = FontStyles.Bold;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;

        RectTransform tRect = txtGo.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one; tRect.sizeDelta = Vector2.zero;

        button.onClick.AddListener(action);
        return button;
    }

    private void InitializeViewportTexture()
    {
        viewportTexture = new Texture2D(ViewportWidth, ViewportHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        viewportPixels = new Color32[ViewportWidth * ViewportHeight];
        if (vesicleViewportRawImage != null)
        {
            vesicleViewportRawImage.texture = viewportTexture;
        }
    }

    private void OnEnable()
    {
        if (ProtocellSimulationManager.Instance != null)
        {
            ProtocellSimulationManager.Instance.OnSimulationUpdated += RefreshUI;
        }
    }

    private void OnDisable()
    {
        if (ProtocellSimulationManager.Instance != null)
        {
            ProtocellSimulationManager.Instance.OnSimulationUpdated -= RefreshUI;
        }
    }

    private void Update()
    {
        if (microViewPanel != null && microViewPanel.activeInHierarchy)
        {
            RenderViewport2D();
        }
    }

    public void ShowMicroView()
    {
        if (microViewPanel == null) CreateMicroViewUIWindow();

        microViewPanel.SetActive(true);
        microViewPanel.transform.SetAsLastSibling();
        RefreshUI();
    }

    public void HideMicroView()
    {
        if (microViewPanel != null)
        {
            microViewPanel.SetActive(false);
        }
    }

    public void ToggleMicroView()
    {
        if (microViewPanel != null && microViewPanel.activeInHierarchy)
        {
            HideMicroView();
        }
        else
        {
            ShowMicroView();
        }
    }

    private void SwitchZone(int delta)
    {
        if (ProtocellSimulationManager.Instance == null) return;
        var mgr = ProtocellSimulationManager.Instance;
        int nextIndex = Mathf.Clamp(mgr.SelectedZoneIndex + delta, 0, mgr.ActiveZones.Count - 1);
        mgr.SelectZone(nextIndex);
        RefreshUI();
    }

    private void ChangeEnv(float deltaTemp, float deltaPh)
    {
        if (ProtocellSimulationManager.Instance != null)
        {
            ProtocellSimulationManager.Instance.ModifySelectedZoneEnvironment(deltaTemp, deltaPh);
        }
    }

    private void FeedNutrients(float amount)
    {
        if (ProtocellSimulationManager.Instance != null)
        {
            ProtocellSimulationManager.Instance.AddNutrientsToSelectedZone(amount);
        }
    }

    private void RefreshUI()
    {
        if (ProtocellSimulationManager.Instance == null) return;
        PrebioticZone zone = ProtocellSimulationManager.Instance.SelectedZone;
        if (zone == null) return;

        if (zoneTitleText != null)
        {
            zoneTitleText.text = $"🔬 VUE MICRO-BIOTIQUE : {zone.name.ToUpper()}";
        }

        if (envStatsText != null)
        {
            envStatsText.text = $"<color=#50E3C2><b>🌡️ CONDITIONS LOCALES — {zone.zoneType}</b></color>\n" +
                $" • Température : <b>{zone.localTemperature:F1} °C</b>  |  pH : <b>{zone.localPh:F1}</b>\n" +
                $" • Concentration Lipides : <b>{zone.lipidConcentration:F0}%</b> ({zone.totalLipidMicelles:F0} micelles)\n" +
                $" • Acides Aminés / Nutriments : <b>{zone.aminoAcidConcentration:F0}%</b>\n" +
                $" • Gradient Chimiosmotique : <b>{zone.chemicalGradientStrength * 100f:F0}%</b>";
        }

        if (populationStatsText != null)
        {
            populationStatsText.text = $"<color=#4EFA8B><b>🧬 TÉLÉMÉTRIE & RÉPLICATION ARN</b></color>\n" +
                $" • Vésicules Bilipidiques : <b>{zone.protocells.Count}</b> / {zone.maxCapacity}\n" +
                $" • Diversité Génétique : <b>{zone.GeneticDiversity * 100f:F1}%</b>\n" +
                $" • Perméabilité Membranaire : <b>{zone.MeanPermeability:F2}</b> <color=#A0B0C0>(cible: 0.40-0.60)</color>\n" +
                $" • Efficacité Métabolique : <b>{zone.MeanEnergyEfficiency:F2}</b>";
        }
    }

    private void RenderViewport2D()
    {
        if (viewportTexture == null || ProtocellSimulationManager.Instance == null) return;
        PrebioticZone zone = ProtocellSimulationManager.Instance.SelectedZone;
        if (zone == null) return;

        // Clear background with fluid tint based on zone type
        Color32 bgCol = zone.zoneType == PrebioticZoneType.HydrothermalVent
            ? new Color32(12, 24, 38, 255)  // Dark hydrothermal blue/gray
            : new Color32(18, 42, 36, 255); // Shallow tide pool teal

        for (int i = 0; i < viewportPixels.Length; i++)
        {
            viewportPixels[i] = bgCol;
        }

        // Synchronize positions list with cell count
        int count = zone.protocells.Count;
        while (bubblePositions.Count < count)
        {
            bubblePositions.Add(new Vector2(Random.Range(25, ViewportWidth - 25), Random.Range(25, ViewportHeight - 25)));
            bubbleVelocities.Add(new Vector2(Random.Range(-0.8f, 0.8f), Random.Range(-0.8f, 0.8f)));
        }

        float dt = Time.deltaTime;

        // Draw Micelles background dots
        int micelleCount = Mathf.Clamp((int)zone.totalLipidMicelles / 10, 0, 120);
        for (int m = 0; m < micelleCount; m++)
        {
            int mx = (int)Mathf.Repeat((m * 37 + Time.time * 15f), ViewportWidth);
            int my = (int)Mathf.Repeat((m * 59 + Time.time * 8f), ViewportHeight);
            int idx = my * ViewportWidth + mx;
            if (idx >= 0 && idx < viewportPixels.Length)
            {
                viewportPixels[idx] = new Color32(180, 220, 150, 180);
            }
        }

        // Render Vesicle Bilipid Bubbles
        for (int i = 0; i < count; i++)
        {
            Protocell cell = zone.protocells[i];
            Vector2 pos = bubblePositions[i];
            Vector2 vel = bubbleVelocities[i];

            // Gentle brownian motion drift
            pos += vel * (10f * dt);
            pos.x = Mathf.PingPong(pos.x, ViewportWidth - 30) + 15;
            pos.y = Mathf.PingPong(pos.y, ViewportHeight - 30) + 15;
            bubblePositions[i] = pos;

            int radiusPx = Mathf.Clamp((int)(cell.radius * 14f), 6, 32);
            Color32 cellCol = cell.mutationColor;
            Color32 membraneCol = new Color32(255, 255, 255, 220);

            // Draw circle & membrane lip
            int cx = (int)pos.x;
            int cy = (int)pos.y;

            for (int dy = -radiusPx; dy <= radiusPx; dy++)
            {
                for (int dx = -radiusPx; dx <= radiusPx; dx++)
                {
                    float distSq = dx * dx + dy * dy;
                    float rSq = radiusPx * radiusPx;

                    if (distSq <= rSq)
                    {
                        int px = cx + dx;
                        int py = cy + dy;

                        if (px >= 0 && px < ViewportWidth && py >= 0 && py < ViewportHeight)
                        {
                            int pIdx = py * ViewportWidth + px;

                            // Membrane border vs interior core
                            if (distSq >= (radiusPx - 2) * (radiusPx - 2))
                            {
                                viewportPixels[pIdx] = membraneCol;
                            }
                            else
                            {
                                viewportPixels[pIdx] = cellCol;
                            }
                        }
                    }
                }
            }
        }

        viewportTexture.SetPixelData(viewportPixels, 0);
        viewportTexture.Apply(false, false);
    }
}
