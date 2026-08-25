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
        rightLayout.spacing = 12f;
        rightLayout.childControlWidth = true;
        rightLayout.childControlHeight = true;
        rightLayout.childForceExpandWidth = true;
        rightLayout.childForceExpandHeight = false;

        LayoutElement rightColLayout = rightCol.AddComponent<LayoutElement>();
        rightColLayout.flexibleWidth = 1f;

        // Env Stats Label (Conditions Locales)
        GameObject envStatsGo = new GameObject("EnvStatsLabel", typeof(RectTransform));
        envStatsGo.transform.SetParent(rightCol.transform, false);
        envStatsText = envStatsGo.AddComponent<TextMeshProUGUI>();
        envStatsText.fontSize = 14f;
        envStatsText.lineSpacing = 4f;
        envStatsText.color = new Color(0.92f, 0.94f, 0.96f, 1f);
        ContentSizeFitter envFitter = envStatsGo.AddComponent<ContentSizeFitter>();
        envFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        envFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Population Stats Label (Télémétrie de Population & Réplication ARN)
        GameObject popStatsGo = new GameObject("PopStatsLabel", typeof(RectTransform));
        popStatsGo.transform.SetParent(rightCol.transform, false);
        populationStatsText = popStatsGo.AddComponent<TextMeshProUGUI>();
        populationStatsText.fontSize = 14f;
        populationStatsText.lineSpacing = 4f;
        populationStatsText.color = new Color(0.35f, 0.92f, 0.65f, 1f);
        ContentSizeFitter popFitter = popStatsGo.AddComponent<ContentSizeFitter>();
        popFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        popFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Env Control Buttons Grid
        GameObject ctrlGrid = new GameObject("ControlGrid", typeof(RectTransform));
        ctrlGrid.transform.SetParent(rightCol.transform, false);

        GridLayoutGroup grid = ctrlGrid.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(220f, 42f);
        grid.spacing = new Vector2(10f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        tempPlusButton = CreateStyledButton(ctrlGrid.transform, "Temp. +5°C", () => ChangeEnv(5f, 0f), new Color(0.85f, 0.4f, 0.2f, 1f));
        tempMinusButton = CreateStyledButton(ctrlGrid.transform, "Temp. -5°C", () => ChangeEnv(-5f, 0f), new Color(0.2f, 0.5f, 0.85f, 1f));
        phPlusButton = CreateStyledButton(ctrlGrid.transform, "pH +0.5", () => ChangeEnv(0f, 0.5f), new Color(0.7f, 0.3f, 0.85f, 1f));
        phMinusButton = CreateStyledButton(ctrlGrid.transform, "pH -0.5", () => ChangeEnv(0f, -0.5f), new Color(0.3f, 0.7f, 0.4f, 1f));

        feedNutrientsButton = CreateStyledButton(rightCol.transform, "🧪 Injecter Acides Aminés & Lipides", () => FeedNutrients(20f), new Color(0.2f, 0.7f, 0.6f, 1f));
        LayoutElement feedLayout = feedNutrientsButton.gameObject.AddComponent<LayoutElement>();
        feedLayout.minHeight = 44f;
        feedLayout.preferredHeight = 44f;

        InitializeViewportTexture();
        microViewPanel.SetActive(false);
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
            envStatsText.text = $"<b>[Conditions Locales - {zone.zoneType}]</b>\n" +
                $" • Température : {zone.localTemperature:F1} °C | pH : {zone.localPh:F1}\n" +
                $" • Concentration Lipides : {zone.lipidConcentration:F0}% ({zone.totalLipidMicelles:F0} micelles)\n" +
                $" • Acides Aminés / Nutriments : {zone.aminoAcidConcentration:F0}%\n" +
                $" • Gradient Chimique / Chimiosmose : {zone.chemicalGradientStrength * 100f:F0}%";
        }

        if (populationStatsText != null)
        {
            populationStatsText.text = $"<b>[Télémétrie de Population & Réplication ARN]</b>\n" +
                $" • Nombre de Vésicules Bilipidiques : <b>{zone.protocells.Count}</b> / {zone.maxCapacity}\n" +
                $" • Diversité Génétique : <b>{zone.GeneticDiversity * 100f:F1}%</b>\n" +
                $" • Perméabilité Moyenne : <b>{zone.MeanPermeability:F2}</b> (Cible: 0.40 - 0.60)\n" +
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
