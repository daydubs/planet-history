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

    [Header("Palette (Hex)")]
    [SerializeField] private string hadeanHex = "#D1495B";
    [SerializeField] private string crustFormationHex = "#F79256";
    [SerializeField] private string volcanicAgeHex = "#F9C74F";
    [SerializeField] private string protoOceanHex = "#43AA8B";
    [SerializeField] private string tectonicDriftHex = "#4D96FF";
    [SerializeField] private string fallbackHex = "#9AA0A6";

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

        if (atmosphereCompositionText != null)
        {
            float total = gameManager.Pressure;
            float h2oPct = total > 0 ? (gameManager.WaterVaporPressure / total) * 100f : 0f;
            float co2Pct = total > 0 ? (gameManager.Co2Pressure / total) * 100f : 0f;
            float n2Pct = total > 0 ? (gameManager.NitrogenPressure / total) * 100f : 0f;
            float otherPct = total > 0 ? (gameManager.OtherGasesPressure / total) * 100f : 0f;

            atmosphereCompositionText.text = $"Atmosphere:\n" +
                $"- H2O (Vapeur): {gameManager.WaterVaporPressure:0.0} atm ({h2oPct:0.0}%)\n" +
                $"- CO2 (Dioxyde de carbone): {gameManager.Co2Pressure:0.0} atm ({co2Pct:0.0}%)\n" +
                $"- N2 (Azote): {gameManager.NitrogenPressure:0.0} atm ({n2Pct:0.0}%)\n" +
                $"- Autres gaz: {gameManager.OtherGasesPressure:0.0} atm ({otherPct:0.0}%)";
        }

        if (sessionSlider != null) sessionSlider.value = gameManager.SessionProgress;
        if (waterSlider != null) waterSlider.value = gameManager.WaterRatio;
        if (tectonicSlider != null) tectonicSlider.value = gameManager.TectonicActivity;

        string hex = GetEpochHex(gameManager.CurrentEpoch);
        if (epochBadgeImage != null && ColorUtility.TryParseHtmlString(hex, out Color c))
        {
            epochBadgeImage.color = c;
        }

        SetText(epochBadgeHexText, hex);
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
