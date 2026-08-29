using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CambrianMiniGameController : MonoBehaviour
{
    public static CambrianMiniGameController Instance { get; private set; }

    private GameObject mainPanel;
    private RectTransform gameArea;
    private TMP_Text instructionText;
    private System.Action onWinCallback;

    private float evolutionPoints = 100f; // Starting bonus
    private float pointsPerSecond = 10f;
    private TMP_Text pointsText;

    private float biodiversityLevel = 0f;
    private float targetBiodiversity = 100f;
    private TMP_Text biodiversityText;
    private Image biodiversityBarFill;

    // Mini-game entities
    private float exactPredatorCount = 1f;
    private float exactPreyCount = 5f;
    private int symbiosisCount = 0;

    private TMP_Text predatorText;
    private TMP_Text preyText;
    private TMP_Text symbiosisText;

    private bool isPlaying = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        CreateUI();
    }

    private void CreateUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        mainPanel = new GameObject("CambrianMiniGamePanel", typeof(RectTransform));
        mainPanel.transform.SetParent(canvas.transform, false);
        RectTransform rt = mainPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = mainPanel.AddComponent<Image>();
        bg.color = new Color(0, 0.1f, 0.1f, 0.95f);

        gameArea = new GameObject("GameArea", typeof(RectTransform)).GetComponent<RectTransform>();
        gameArea.transform.SetParent(mainPanel.transform, false);
        gameArea.anchorMin = new Vector2(0.5f, 0.5f);
        gameArea.anchorMax = new Vector2(0.5f, 0.5f);
        gameArea.sizeDelta = new Vector2(700, 600);
        gameArea.anchoredPosition = Vector2.zero;
        Image areaBg = gameArea.gameObject.AddComponent<Image>();
        areaBg.color = new Color(0.05f, 0.15f, 0.2f, 1f);

        GameObject textObj = new GameObject("InstructionText", typeof(RectTransform));
        textObj.transform.SetParent(mainPanel.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 1f);
        textRt.anchorMax = new Vector2(0.5f, 1f);
        textRt.anchoredPosition = new Vector2(0, -60);
        textRt.sizeDelta = new Vector2(800, 100);
        instructionText = textObj.AddComponent<TextMeshProUGUI>();
        instructionText.fontSize = 24;
        instructionText.alignment = TextAlignmentOptions.Center;
        instructionText.color = Color.white;
        instructionText.text = "Explosion Cambrienne\nGérez le pool d'espèces pour atteindre la biodiversité cible.";

        // Stats Row
        GameObject statsRow = new GameObject("StatsRow", typeof(RectTransform));
        statsRow.transform.SetParent(gameArea, false);
        RectTransform statsRt = statsRow.GetComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(0, 1);
        statsRt.anchorMax = new Vector2(1, 1);
        statsRt.sizeDelta = new Vector2(0, 50);
        statsRt.anchoredPosition = new Vector2(0, -25);

        HorizontalLayoutGroup statsLayout = statsRow.AddComponent<HorizontalLayoutGroup>();
        statsLayout.childControlWidth = true;
        statsLayout.childControlHeight = true;

        pointsText = CreateText(statsRow.transform, "Points d'Évolution: 0");
        biodiversityText = CreateText(statsRow.transform, "Biodiversité: 0%");

        // Biodiversity Bar
        GameObject bioBar = new GameObject("BioBar", typeof(RectTransform));
        bioBar.transform.SetParent(gameArea, false);
        RectTransform bioRt = bioBar.GetComponent<RectTransform>();
        bioRt.anchorMin = new Vector2(0.1f, 0.8f);
        bioRt.anchorMax = new Vector2(0.9f, 0.8f);
        bioRt.sizeDelta = new Vector2(0, 20);
        bioRt.anchoredPosition = new Vector2(0, 0);
        Image bioBg = bioBar.AddComponent<Image>();
        bioBg.color = Color.gray;

        GameObject bioFillObj = new GameObject("Fill", typeof(RectTransform));
        bioFillObj.transform.SetParent(bioBar.transform, false);
        RectTransform bioFillRt = bioFillObj.GetComponent<RectTransform>();
        bioFillRt.anchorMin = new Vector2(0, 0);
        bioFillRt.anchorMax = new Vector2(0, 1);
        bioFillRt.sizeDelta = new Vector2(0, 0);
        bioFillRt.anchoredPosition = new Vector2(0, 0);
        biodiversityBarFill = bioFillObj.AddComponent<Image>();
        biodiversityBarFill.color = Color.cyan;

        // Species Row
        GameObject speciesRow = new GameObject("SpeciesRow", typeof(RectTransform));
        speciesRow.transform.SetParent(gameArea, false);
        RectTransform speciesRt = speciesRow.GetComponent<RectTransform>();
        speciesRt.anchorMin = new Vector2(0, 0.5f);
        speciesRt.anchorMax = new Vector2(1, 0.5f);
        speciesRt.sizeDelta = new Vector2(0, 150);
        speciesRt.anchoredPosition = new Vector2(0, 0);

        HorizontalLayoutGroup speciesLayout = speciesRow.AddComponent<HorizontalLayoutGroup>();
        speciesLayout.childControlWidth = true;
        speciesLayout.childControlHeight = true;
        speciesLayout.spacing = 10;
        speciesLayout.padding = new RectOffset(20, 20, 0, 0);

        CreateSpeciesPanel(speciesRow.transform, "Proies", out preyText, () => SpawnSpecies(1), 10f);
        CreateSpeciesPanel(speciesRow.transform, "Prédateurs", out predatorText, () => SpawnSpecies(2), 25f);
        CreateSpeciesPanel(speciesRow.transform, "Symbioses", out symbiosisText, () => SpawnSpecies(3), 50f);

        mainPanel.SetActive(false);
    }

    private void CreateSpeciesPanel(Transform parent, string title, out TMP_Text countText, UnityEngine.Events.UnityAction action, float cost)
    {
        GameObject panel = new GameObject(title + "Panel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.2f, 0.25f, 1f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 10;
        layout.padding = new RectOffset(10, 10, 10, 10);

        TMP_Text titleText = CreateText(panel.transform, title);
        titleText.alignment = TextAlignmentOptions.Center;

        countText = CreateText(panel.transform, "0");
        countText.alignment = TextAlignmentOptions.Center;
        countText.fontSize = 32;

        GameObject btnObj = new GameObject("Btn_" + title, typeof(RectTransform));
        btnObj.transform.SetParent(panel.transform, false);
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.2f, 0.6f, 0.4f, 1f);
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        btn.onClick.AddListener(action);

        TMP_Text btnText = CreateText(btnObj.transform, $"Ajouter (-{cost} pts)");
        btnText.alignment = TextAlignmentOptions.Center;
        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.minHeight = 40;
    }

    private TMP_Text CreateText(Transform parent, string text)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.color = Color.white;
        return tmp;
    }

    public void StartMiniGame(System.Action onSuccess)
    {
        onWinCallback = onSuccess;
        evolutionPoints = 100f; // start with a small bonus
        biodiversityLevel = 0f;
        exactPredatorCount = 1f;
        exactPreyCount = 5f;
        symbiosisCount = 0;

        UpdateUI();

        mainPanel.SetActive(true);
        mainPanel.transform.SetAsLastSibling();
        isPlaying = true;
    }

    private void Update()
    {
        if (!isPlaying) return;

        // Passive point generation
        evolutionPoints += pointsPerSecond * Time.deltaTime;

        // Ecosystem dynamics
        // Preys multiply based on their numbers minus predator consumption
        float preyGrowth = exactPreyCount * 0.1f * Time.deltaTime;
        float preyConsumption = exactPredatorCount * 0.5f * Time.deltaTime;

        // Ensure we don't go below 1 to prevent complete wipeout blocking progress
        exactPreyCount += preyGrowth - preyConsumption;
        if(exactPreyCount < 1f) exactPreyCount = 1f;

        // Predators grow slowly if there is enough prey, starve if not
        float predatorGrowth = (exactPreyCount > exactPredatorCount * 2) ? (exactPredatorCount * 0.05f * Time.deltaTime) : (-exactPredatorCount * 0.1f * Time.deltaTime);
        exactPredatorCount += predatorGrowth;
        if(exactPredatorCount < 1f) exactPredatorCount = 1f;

        // Symbiosis grants a massive passive boost to biodiversity
        float symbiosisBoost = symbiosisCount * 1.5f * Time.deltaTime;

        // Calculate Biodiversity: combination of diversity types and balance
        // Ideal ratio is roughly 1 predator to 5 prey
        float balanceScore = 0f;
        int displayPredatorCount = Mathf.RoundToInt(exactPredatorCount);
        int displayPreyCount = Mathf.RoundToInt(exactPreyCount);
        if (displayPredatorCount > 0 && displayPreyCount > 0)
        {
            float ratio = (float)displayPreyCount / displayPredatorCount;
            // 5 is ideal, if ratio is 5, balanceScore is maxed
            float diff = Mathf.Abs(ratio - 5f);
            balanceScore = Mathf.Clamp(10f - diff, 0f, 10f) * 2f; // up to 20 base diversity
        }

        biodiversityLevel += (balanceScore * 0.1f * Time.deltaTime) + symbiosisBoost + ((exactPreyCount + exactPredatorCount)*0.01f*Time.deltaTime);

        UpdateUI();

        if (biodiversityLevel >= targetBiodiversity)
        {
            WinGame();
        }
    }

    private void SpawnSpecies(int type)
    {
        if (type == 1 && evolutionPoints >= 10f) // Prey
        {
            evolutionPoints -= 10f;
            exactPreyCount += 5f;
        }
        else if (type == 2 && evolutionPoints >= 25f) // Predator
        {
            evolutionPoints -= 25f;
            exactPredatorCount += 1f;
        }
        else if (type == 3 && evolutionPoints >= 50f) // Symbiosis
        {
            evolutionPoints -= 50f;
            symbiosisCount += 1;
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        pointsText.text = $"Points d'Évolution: {Mathf.FloorToInt(evolutionPoints)}";
        biodiversityText.text = $"Biodiversité: {(biodiversityLevel / targetBiodiversity * 100f):F1}%";
        biodiversityBarFill.anchorMax = new Vector2(Mathf.Clamp01(biodiversityLevel / targetBiodiversity), 1f);

        preyText.text = Mathf.RoundToInt(exactPreyCount).ToString();
        predatorText.text = Mathf.RoundToInt(exactPredatorCount).ToString();
        symbiosisText.text = symbiosisCount.ToString();
    }

    private void WinGame()
    {
        isPlaying = false;
        instructionText.text = "Succès ! L'Explosion Cambrienne est en marche !";
        Invoke(nameof(EndMiniGame), 3f);
    }

    private void EndMiniGame()
    {
        mainPanel.SetActive(false);
        onWinCallback?.Invoke();
    }
}
