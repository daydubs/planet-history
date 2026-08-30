using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LandColonizationMiniGameController : MonoBehaviour
{
    public static LandColonizationMiniGameController Instance { get; private set; }

    private GameObject mainPanel;
    private RectTransform gameArea;
    private System.Action onWinCallback;

    private int currentTraitIndex = 0;

    private readonly string[] targetTraits = {
        "Bones/Exoskeleton (Support Structure)",
        "Lungs (Oxygen from air)",
        "Amniotic Sac/Seeds (Terrestrial Reproduction)"
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CreateUI();
    }

    private void CreateUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        mainPanel = new GameObject("LandColonizationMiniGamePanel", typeof(RectTransform));
        mainPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = mainPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500f, 400f);

        Image bg = mainPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.2f, 0.15f, 0.95f);

        VerticalLayoutGroup layout = mainPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 15f;
        layout.childAlignment = TextAnchor.UpperCenter;

        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
        titleObj.transform.SetParent(mainPanel.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Colonisation des Terres Fermes";
        titleText.fontSize = 24f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.8f, 1f, 0.8f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;

        GameObject descObj = new GameObject("DescText", typeof(RectTransform));
        descObj.transform.SetParent(mainPanel.transform, false);
        TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = "Choisissez l'ordre correct d'évolution des traits pour survivre sur la terre ferme (gravité, UV, sécheresse).";
        descText.fontSize = 16f;
        descText.color = Color.white;
        descText.alignment = TextAlignmentOptions.Center;
        descText.enableWordWrapping = true;
        LayoutElement descLayout = descObj.AddComponent<LayoutElement>();
        descLayout.minHeight = 60f;

        gameArea = new GameObject("GameArea", typeof(RectTransform)).GetComponent<RectTransform>();
        gameArea.transform.SetParent(mainPanel.transform, false);

        VerticalLayoutGroup gameLayout = gameArea.gameObject.AddComponent<VerticalLayoutGroup>();
        gameLayout.spacing = 10f;
        gameLayout.childAlignment = TextAnchor.MiddleCenter;
        gameLayout.childControlWidth = true;
        gameLayout.childForceExpandWidth = true;

        LayoutElement gameAreaElem = gameArea.gameObject.AddComponent<LayoutElement>();
        gameAreaElem.flexibleHeight = 1f;

        mainPanel.SetActive(false);
    }

    public void StartMiniGame(System.Action onWin)
    {
        onWinCallback = onWin;
        currentTraitIndex = 0;
        BuildGameArea();

        mainPanel.SetActive(true);
        mainPanel.transform.SetAsLastSibling();
    }

    private void BuildGameArea()
    {
        foreach (Transform child in gameArea)
        {
            Destroy(child.gameObject);
        }

        GameObject statusObj = new GameObject("StatusText", typeof(RectTransform));
        statusObj.transform.SetParent(gameArea.transform, false);
        TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = $"Étape {currentTraitIndex + 1}/3: Sélectionnez le prochain trait vital.";
        statusText.fontSize = 18f;
        statusText.fontStyle = FontStyles.Bold;
        statusText.color = new Color(0.9f, 0.8f, 0.2f, 1f);
        statusText.alignment = TextAlignmentOptions.Center;

        // Shuffle traits for buttons so order changes
        List<string> options = new List<string>(targetTraits);
        // Fisher-Yates shuffle
        for (int i = 0; i < options.Count; i++) {
            string temp = options[i];
            int randomIndex = Random.Range(i, options.Count);
            options[i] = options[randomIndex];
            options[randomIndex] = temp;
        }

        foreach(string trait in options)
        {
            CreateTraitButton(trait);
        }
    }

    private void CreateTraitButton(string traitName)
    {
        GameObject btnObj = new GameObject("TraitBtn_" + traitName, typeof(RectTransform));
        btnObj.transform.SetParent(gameArea, false);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.4f, 0.3f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        LayoutElement btnLayout = btnObj.AddComponent<LayoutElement>();
        btnLayout.minHeight = 40f;
        btnLayout.preferredHeight = 40f;

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = GetLocalizedTrait(traitName);
        tmp.fontSize = 16f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        btn.onClick.AddListener(() => OnTraitSelected(traitName));
    }

    private string GetLocalizedTrait(string englishTrait)
    {
        switch(englishTrait)
        {
            case "Bones/Exoskeleton (Support Structure)": return "Os/Exosquelette (Structure de soutien)";
            case "Lungs (Oxygen from air)": return "Poumons (Oxygène de l'air)";
            case "Amniotic Sac/Seeds (Terrestrial Reproduction)": return "Sac amniotique/Graines (Reproduction terrestre)";
            default: return englishTrait;
        }
    }

    private void OnTraitSelected(string traitName)
    {
        if (traitName == targetTraits[currentTraitIndex])
        {
            // Correct choice
            currentTraitIndex++;
            if (currentTraitIndex >= targetTraits.Length)
            {
                WinGame();
            }
            else
            {
                BuildGameArea();
            }
        }
        else
        {
            // Incorrect choice, reset
            currentTraitIndex = 0;
            BuildGameArea();

            // Note: because BuildGameArea calls Destroy (which is deferred), we use transform.Find on the newly created object.
            Transform statusObj = gameArea.Find("StatusText");
            if (statusObj != null)
            {
                TextMeshProUGUI statusText = statusObj.GetComponent<TextMeshProUGUI>();
                if (statusText != null)
                {
                    statusText.text = "Échec ! Mauvais trait évolutif choisi. Recommencez.";
                    statusText.color = Color.red;
                }
            }
        }
    }

    private void WinGame()
    {
        foreach (Transform child in gameArea)
        {
            Destroy(child.gameObject);
        }

        GameObject winObj = new GameObject("WinText", typeof(RectTransform));
        winObj.transform.SetParent(gameArea.transform, false);
        TextMeshProUGUI winText = winObj.AddComponent<TextMeshProUGUI>();
        winText.text = "Succès ! Votre espèce s'est adaptée à la terre ferme.";
        winText.fontSize = 20f;
        winText.fontStyle = FontStyles.Bold;
        winText.color = new Color(0.2f, 0.9f, 0.2f, 1f);
        winText.alignment = TextAlignmentOptions.Center;

        GameObject btnObj = new GameObject("CloseBtn", typeof(RectTransform));
        btnObj.transform.SetParent(gameArea.transform, false);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.3f, 1f);

        Button closeBtn = btnObj.AddComponent<Button>();
        closeBtn.targetGraphic = btnImg;
        closeBtn.onClick.AddListener(() => {
            mainPanel.SetActive(false);
            onWinCallback?.Invoke();
        });

        LayoutElement btnLayout = btnObj.AddComponent<LayoutElement>();
        btnLayout.minHeight = 40f;
        btnLayout.preferredHeight = 40f;

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Continuer";
        tmp.fontSize = 16f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }
}
