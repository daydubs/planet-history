using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        containerLayout.minHeight = 120f;
        containerLayout.flexibleWidth = 1f;

        VerticalLayoutGroup vertical = containerGo.AddComponent<VerticalLayoutGroup>();
        vertical.childAlignment = TextAnchor.UpperLeft;
        vertical.childControlWidth = true;
        vertical.childControlHeight = false;
        vertical.spacing = 6f;

        // Header + Progress Label
        GameObject labelGo = new GameObject("PrebioticProgressLabel", typeof(RectTransform));
        labelGo.transform.SetParent(containerGo.transform, false);
        prebioticProgressText = labelGo.AddComponent<TextMeshProUGUI>();
        prebioticProgressText.fontSize = 18;
        prebioticProgressText.fontStyle = FontStyles.Bold;
        prebioticProgressText.color = new Color(0.26f, 0.82f, 0.72f, 1f);
        prebioticProgressText.alignment = TextAlignmentOptions.Left;

        // Buttons Grid / Row
        GameObject btnRowGo = new GameObject("PrebioticButtonsRow", typeof(RectTransform));
        btnRowGo.transform.SetParent(containerGo.transform, false);

        HorizontalLayoutGroup btnLayout = btnRowGo.AddComponent<HorizontalLayoutGroup>();
        btnLayout.childAlignment = TextAnchor.MiddleLeft;
        btnLayout.childControlWidth = true;
        btnLayout.childControlHeight = true;
        btnLayout.spacing = 8f;

        CreatePrebioticActionButton(btnRowGo.transform, "Miller-Urey", new Color(0.2f, 0.6f, 0.86f, 1f), () =>
        {
            if (PrebioticMiniGameController.Instance != null)
                PrebioticMiniGameController.Instance.TriggerLightningDischarge();
        });

        CreatePrebioticActionButton(btnRowGo.transform, "Hydrothermale", new Color(0.86f, 0.35f, 0.2f, 1f), () =>
        {
            if (PrebioticMiniGameController.Instance != null)
                PrebioticMiniGameController.Instance.TriggerHydrothermalVent();
        });

        CreatePrebioticActionButton(btnRowGo.transform, "Météorite", new Color(0.6f, 0.35f, 0.75f, 1f), () =>
        {
            if (PrebioticMiniGameController.Instance != null)
                PrebioticMiniGameController.Instance.TriggerMeteorBombardment();
        });

        CreatePrebioticActionButton(btnRowGo.transform, "Catalyse UV", new Color(0.9f, 0.75f, 0.2f, 1f), () =>
        {
            if (PrebioticMiniGameController.Instance != null)
                PrebioticMiniGameController.Instance.TriggerUvCatalysis();
        });

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(hudRoot);
    }

    private void CreatePrebioticActionButton(Transform parent, string title, Color btnColor, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonGo = new GameObject($"Btn_{title}", typeof(RectTransform));
        buttonGo.transform.SetParent(parent, false);

        Image buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = btnColor;

        Button button = buttonGo.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        prebioticActionButtons.Add(button);

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

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.OnEpochChanged += HandleEpochChanged;
        }

        RefreshAll();
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnEpochChanged -= HandleEpochChanged;
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
            atmosphereCompositionText.fontSize = 15f;

            float total = gameManager.Pressure;
            float h2oPct = total > 0 ? (gameManager.WaterVaporPressure / total) * 100f : 0f;
            float co2Pct = total > 0 ? (gameManager.Co2Pressure / total) * 100f : 0f;
            float n2Pct = total > 0 ? (gameManager.NitrogenPressure / total) * 100f : 0f;
            float otherPct = total > 0 ? (gameManager.OtherGasesPressure / total) * 100f : 0f;

            atmosphereCompositionText.text = $"Atmosphère (Pré-biotique) :\n" +
                $" • H2O (Vapeur d'eau) : {gameManager.WaterVaporPressure:0.0} atm ({h2oPct:0.0}%)\n" +
                $" • CO2 (Dioxyde de carbone) : {gameManager.Co2Pressure:0.0} atm ({co2Pct:0.0}%)\n" +
                $" • N2 (Azote) : {gameManager.NitrogenPressure:0.0} atm ({n2Pct:0.0}%)\n" +
                $" • Gaz réduits (CH4, NH3, SO2) : {gameManager.OtherGasesPressure:0.0} atm ({otherPct:0.0}%)";
        }

        if (sessionSlider != null) sessionSlider.value = gameManager.SessionProgress;
        if (waterSlider != null) waterSlider.value = gameManager.WaterRatio;
        if (tectonicSlider != null) tectonicSlider.value = gameManager.TectonicActivity;

        if (prebioticProgressText != null)
        {
            if (PrebioticMiniGameController.Instance != null && isPrebiotic)
            {
                var p = PrebioticMiniGameController.Instance;
                prebioticProgressText.text = $"[Phase Pré-Biotique - Synthèse d'Acides Aminés : {p.TotalProgress * 100f:0.0}%]\n" +
                    $"Glycine: {p.Glycine:0}% | Alanine: {p.Alanine:0}% | Aspartique: {p.AsparticAcid:0}% | Glutamique: {p.GlutamicAcid:0}%\n" +
                    $"Sérine: {p.Serine:0}% | Valine: {p.Valine:0}% | Leucine: {p.Leucine:0}% | Isoleucine: {p.Isoleucine:0}%";
            }
            else
            {
                prebioticProgressText.text = $"[Phase Pré-Biotique verrouillée - En attente de l'époque Pré-Biotique]\n" +
                    $"Glycine: 0% | Alanine: 0% | Aspartique: 0% | Glutamique: 0%\n" +
                    $"Sérine: 0% | Valine: 0% | Leucine: 0% | Isoleucine: 0%";
            }
        }

        // Update button interactable states based on epoch
        if (volcanoButton != null) volcanoButton.interactable = isPrebiotic;
        foreach (Button btn in prebioticActionButtons)
        {
            if (btn != null) btn.interactable = isPrebiotic;
        }

        var meteorCtrl = GetComponent<MeteorEventController>();
        if (meteorCtrl != null && meteorCtrl.MeteorButton != null)
        {
            meteorCtrl.MeteorButton.interactable = isPrebiotic;
        }

        string hex = GetEpochHex(gameManager.CurrentEpoch);
        if (epochBadgeImage != null && ColorUtility.TryParseHtmlString(hex, out Color c))
        {
            epochBadgeImage.color = c;
        }

        // Clear hex badge text to avoid displaying raw hex string on HUD
        SetText(epochBadgeHexText, string.Empty);
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
