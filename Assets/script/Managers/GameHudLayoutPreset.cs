using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameHudLayoutPreset : MonoBehaviour
{
    [Header("Root References")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private VerticalLayoutGroup verticalLayout;
    [SerializeField] private Image panelBackground;

    [Header("Rows")]
    [SerializeField] private RectTransform epochRow;
    [SerializeField] private RectTransform sessionRow;
    [SerializeField] private RectTransform remainingRow;
    [SerializeField] private RectTransform internalTempRow;
    [SerializeField] private RectTransform surfaceTempRow;
    [SerializeField] private RectTransform pressureRow;
    [SerializeField] private RectTransform waterRow;
    [SerializeField] private RectTransform tectonicRow;
    [SerializeField] private RectTransform atmosphereRow;

    [Header("UI Controls")]
    [SerializeField] private Slider sessionSlider;
    [SerializeField] private Slider waterSlider;
    [SerializeField] private Slider tectonicSlider;

    [Header("Typography")]
    [SerializeField, Min(10)] private int titleFontSize = 22;
    [SerializeField, Min(10)] private int bodyFontSize = 15;
    [SerializeField] private FontStyles titleStyle = FontStyles.Bold;
    [SerializeField] private FontStyles bodyStyle = FontStyles.Normal;

    [Header("Panel Style (Hex)")]
    [SerializeField] private string panelBackgroundHex = "#0D111DE6";
    [SerializeField] private string textColorHex = "#F5F7FA";
    [SerializeField] private string subtleTextHex = "#D3D7DE";

    [Header("Spacing")]
    [SerializeField] private float panelWidth = 540f;
    [SerializeField] private float panelPadding = 16f;
    [SerializeField] private float rowHeight = 28f;
    [SerializeField] private float rowSpacing = 6f;
    [SerializeField] private float sliderHeight = 14f;
    [SerializeField] private bool applyOnStart = true;

    private void Reset()
    {
        panelRoot = transform as RectTransform;
    }

    private void Start()
    {
        if (applyOnStart && Application.isPlaying)
        {
            SetupHudFull();
        }
    }

    [ContextMenu("Setup HUD (Full)")]
    public void SetupHudFull()
    {
        ResolvePanelRoot();
        AutoWireByName();
        ApplyPreset();
        WireGameHudController();
        ForceRebuildLayout();
    }

    [ContextMenu("Apply HUD Layout Preset")]
    public void ApplyPreset()
    {
        ResolvePanelRoot();
        if (panelRoot == null)
        {
            Debug.LogWarning("[GameHudLayoutPreset] HUDRoot introuvable.");
            return;
        }

        SetupPanelRoot();
        SetupVerticalLayout();
        SetupRows();
        SetupSliders();
        SetupTypography();
        SetupColors();
        ForceRebuildLayout();

        Debug.Log($"[GameHudLayoutPreset] Layout applique sur {panelRoot.name}.");
    }

    [ContextMenu("Auto Wire By Name")]
    public void AutoWireByName()
    {
        ResolvePanelRoot();
        if (panelRoot == null) return;

        epochRow = FindChildRect("EpochRow");
        sessionRow = FindChildRect("SessionRow");
        remainingRow = FindChildRect("RemainingRow");
        internalTempRow = FindChildRect("InternalTempRow");
        surfaceTempRow = FindChildRect("SurfaceTempRow");
        pressureRow = FindChildRect("PressureRow");
        waterRow = FindChildRect("WaterRow");
        tectonicRow = FindChildRect("TectonicRow");
        atmosphereRow = FindChildRect("AtmosphereRow") ?? FindChildRect("compositionAtmpan");

        sessionSlider = FindChildComponentDeep<Slider>("SessionSlider");
        waterSlider = FindChildComponentDeep<Slider>("WaterSlider");
        tectonicSlider = FindChildComponentDeep<Slider>("TectonicSlider");
    }

    [ContextMenu("Wire GameHudController")]
    public void WireGameHudController()
    {
        ResolvePanelRoot();

        GameHudController controller = GetComponent<GameHudController>();
        if (controller == null && panelRoot != null)
        {
            controller = panelRoot.GetComponent<GameHudController>();
        }
        if (controller == null && panelRoot != null)
        {
            controller = panelRoot.gameObject.AddComponent<GameHudController>();
        }

        if (controller != null && panelRoot != null)
        {
            controller.BindFromHierarchy(panelRoot);
        }
    }

    private void ResolvePanelRoot()
    {
        if (panelRoot != null && FindChildRect("EpochRow") != null) return;

        if (name == "HUDRoot")
        {
            panelRoot = transform as RectTransform;
            return;
        }

        Transform hudRoot = FindDeep(transform, "HUDRoot");
        if (hudRoot != null)
        {
            panelRoot = hudRoot as RectTransform;
            return;
        }

        panelRoot = transform as RectTransform;
    }

    private void SetupPanelRoot()
    {
        panelRoot.anchorMin = new Vector2(0f, 1f);
        panelRoot.anchorMax = new Vector2(0f, 1f);
        panelRoot.pivot = new Vector2(0f, 1f);
        panelRoot.anchoredPosition = new Vector2(20f, -20f);
        panelRoot.sizeDelta = new Vector2(panelWidth, panelRoot.sizeDelta.y);
        panelRoot.localScale = Vector3.one;
    }

    private void SetupVerticalLayout()
    {
        verticalLayout = panelRoot.GetComponent<VerticalLayoutGroup>();
        if (verticalLayout == null) verticalLayout = panelRoot.gameObject.AddComponent<VerticalLayoutGroup>();

        verticalLayout.childAlignment = TextAnchor.UpperLeft;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.spacing = rowSpacing;
        verticalLayout.padding = new RectOffset(
            (int)panelPadding,
            (int)panelPadding,
            (int)panelPadding,
            (int)panelPadding);

        ContentSizeFitter fitter = panelRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = panelRoot.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void SetupRows()
    {
        List<RectTransform> rows = new List<RectTransform>();
        AddRowIfPresent(rows, epochRow);
        AddRowIfPresent(rows, sessionRow);
        AddRowIfPresent(rows, remainingRow);
        AddRowIfPresent(rows, internalTempRow);
        AddRowIfPresent(rows, surfaceTempRow);
        AddRowIfPresent(rows, pressureRow);
        AddRowIfPresent(rows, waterRow);
        AddRowIfPresent(rows, tectonicRow);
        AddRowIfPresent(rows, atmosphereRow);

        if (rows.Count == 0)
        {
            CollectRowsRecursive(panelRoot, rows);
        }

        foreach (RectTransform row in rows)
        {
            float minH = (row == atmosphereRow || row.name == "compositionAtmpan" || row.name == "atmosphereRow") ? 80f : 24f;
            SetupRow(row, minH);
        }
    }

    private static void AddRowIfPresent(List<RectTransform> rows, RectTransform row)
    {
        if (row != null && !rows.Contains(row)) rows.Add(row);
    }

    private static void CollectRowsRecursive(Transform root, List<RectTransform> rows)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.EndsWith("Row"))
            {
                if (child is RectTransform row && !rows.Contains(row))
                {
                    rows.Add(row);
                }
            }

            CollectRowsRecursive(child, rows);
        }
    }

    private void SetupRow(RectTransform row, float height)
    {
        PrepareRowForVerticalLayout(row, height);

        // Remove or disable any standalone Image background attached directly to individual rows
        // to prevent Unity's default UI panel sprite (grey rounded rectangle) from showing behind rows.
        Image rowBg = row.GetComponent<Image>();
        if (rowBg != null && row != panelRoot)
        {
            rowBg.enabled = false;
        }

        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        if (rowLayout == null) rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = height;
        rowLayout.preferredHeight = -1f;
        rowLayout.flexibleHeight = 0f;
        rowLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup horizontal = row.GetComponent<HorizontalLayoutGroup>();
        if (horizontal == null) horizontal = row.gameObject.AddComponent<HorizontalLayoutGroup>();

        horizontal.childAlignment = TextAnchor.MiddleLeft;
        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = false;
        horizontal.spacing = 8f;
        horizontal.padding = new RectOffset(0, 0, 0, 0);

        for (int i = 0; i < row.childCount; i++)
        {
            RectTransform child = row.GetChild(i) as RectTransform;
            if (child == null) continue;

            PrepareChildForHorizontalLayout(child);
            ConfigureChildLayoutElement(child);
        }
    }

    private void ConfigureChildLayoutElement(RectTransform child)
    {
        LayoutElement layout = child.GetComponent<LayoutElement>();
        if (layout == null) layout = child.gameObject.AddComponent<LayoutElement>();

        layout.flexibleHeight = 0f;

        if (child.name.Contains("CompAtm") || child.name.Contains("Atmosphere"))
        {
            layout.minWidth = 200f;
            layout.preferredWidth = -1f;
            layout.flexibleWidth = 1f;
            layout.minHeight = 70f;
            layout.preferredHeight = -1f;
            return;
        }

        if (child.name.Contains("Slider"))
        {
            layout.minWidth = 110f;
            layout.preferredWidth = 150f;
            layout.flexibleWidth = 0f;
            layout.minHeight = sliderHeight;
            layout.preferredHeight = sliderHeight;
            return;
        }

        if (child.name.Contains("Badge"))
        {
            layout.minWidth = 24f;
            layout.preferredWidth = 24f;
            layout.flexibleWidth = 0f;
            layout.minHeight = 24f;
            layout.preferredHeight = 24f;
            return;
        }

        if (child.name.Contains("Hex"))
        {
            layout.minWidth = 80f;
            layout.preferredWidth = 80f;
            layout.flexibleWidth = 0f;
            layout.minHeight = 20f;
            layout.preferredHeight = 20f;
            return;
        }

        layout.minWidth = 140f;
        layout.preferredWidth = -1f;
        layout.flexibleWidth = 1f;
        layout.minHeight = 22f;
        layout.preferredHeight = -1f;
    }

    private void SetupSliders()
    {
        SetupSlider(sessionSlider, new Color(0.23f, 0.53f, 1.0f, 1f));   // Blue / Cyan
        SetupSlider(waterSlider, new Color(0.0f, 0.71f, 0.85f, 1f));     // Ocean Teal
        SetupSlider(tectonicSlider, new Color(0.97f, 0.57f, 0.34f, 1f)); // Lava Amber
    }

    private void SetupSlider(Slider slider, Color fillColor)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = false;

        // Hide handle slide area so the slider renders purely as a sleek progress gauge
        Transform handleArea = slider.transform.Find("Handle Slide Area");
        if (handleArea != null)
        {
            handleArea.gameObject.SetActive(false);
        }

        // Style Track Background
        Transform bgTrans = slider.transform.Find("Background");
        if (bgTrans != null)
        {
            Image bgImg = bgTrans.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.color = new Color(0.12f, 0.15f, 0.20f, 0.85f);
            }
        }

        // Style Fill Area and Fill
        Transform fillTrans = slider.transform.Find("Fill Area/Fill");
        if (fillTrans == null) fillTrans = slider.transform.Find("Fill");
        if (fillTrans != null)
        {
            Image fillImg = fillTrans.GetComponent<Image>();
            if (fillImg != null)
            {
                fillImg.color = fillColor;
            }
        }
    }

    private void SetupTypography()
    {
        if (panelRoot == null) return;

        TMP_Text[] texts = panelRoot.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            bool isTitle = text.name.Contains("Epoch") || text.name.Contains("Title");
            bool isAtmosphere = text.name.Contains("CompAtm") || text.name.Contains("atmosphere");
            bool isPrebiotic = text.name.Contains("Prebiotic");

            // Disable auto-sizing to prevent TMP from shrinking multiline text into unreadable micro-text
            text.enableAutoSizing = false;

            if (isTitle)
            {
                text.fontSize = titleFontSize;
                text.fontStyle = titleStyle;
            }
            else if (isAtmosphere)
            {
                text.fontSize = 12.5f;
                text.fontStyle = FontStyles.Normal;
                text.lineSpacing = 2f;
            }
            else if (isPrebiotic)
            {
                text.fontSize = 13f;
                text.fontStyle = FontStyles.Normal;
                text.lineSpacing = 2f;
            }
            else
            {
                text.fontSize = bodyFontSize;
                text.fontStyle = bodyStyle;
            }

            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.margin = Vector4.zero;
        }
    }

    private void SetupColors()
    {
        if (panelBackground == null) panelBackground = panelRoot.GetComponent<Image>();
        if (panelBackground == null) panelBackground = panelRoot.gameObject.AddComponent<Image>();

        if (panelBackground != null && ColorUtility.TryParseHtmlString(panelBackgroundHex, out Color bg))
        {
            panelBackground.color = bg;
        }

        Color body = ParseHexOrFallback(textColorHex, new Color(0.96f, 0.97f, 0.98f, 1f));
        Color subtle = ParseHexOrFallback(subtleTextHex, new Color(0.83f, 0.86f, 0.90f, 1f));

        TMP_Text[] texts = panelRoot.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            bool isSubtle = text.name.Contains("Hex") || text.name.Contains("Hint");
            text.color = isSubtle ? subtle : body;
        }
    }

    private void ForceRebuildLayout()
    {
        if (panelRoot == null) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);

        for (int i = 0; i < panelRoot.childCount; i++)
        {
            if (panelRoot.GetChild(i) is RectTransform row)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(row);
            }
        }
    }

    private RectTransform FindChildRect(string childName)
    {
        if (panelRoot == null) return null;
        Transform found = FindDeep(panelRoot, childName);
        return found as RectTransform;
    }

    private T FindChildComponentDeep<T>(string childName) where T : Component
    {
        if (panelRoot == null) return null;
        Transform found = FindDeep(panelRoot, childName);
        return found != null ? found.GetComponent<T>() : null;
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

    private static void PrepareRowForVerticalLayout(RectTransform row, float height)
    {
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.anchoredPosition = Vector2.zero;
        row.sizeDelta = new Vector2(0f, height);
        row.localScale = Vector3.one;
    }

    private static void PrepareChildForHorizontalLayout(RectTransform child)
    {
        child.anchorMin = new Vector2(0f, 0f);
        child.anchorMax = new Vector2(0f, 1f);
        child.pivot = new Vector2(0f, 0.5f);
        child.anchoredPosition = Vector2.zero;
        child.sizeDelta = new Vector2(200f, 0f);
        child.localScale = Vector3.one;
    }

    private static Color ParseHexOrFallback(string hex, Color fallback)
    {
        return ColorUtility.TryParseHtmlString(hex, out Color parsed) ? parsed : fallback;
    }
}
