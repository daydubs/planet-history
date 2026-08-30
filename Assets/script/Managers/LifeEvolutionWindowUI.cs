using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LifeEvolutionWindowUI : MonoBehaviour
{
    private GameObject lifeWindowPanel;
    private TMP_Text explanationText;
    private TMP_Text titleText;
    private Button prevButton;
    private Button nextButton;
    private Button closeButton;
    private Button tryEvolutionButton;

    private int currentStepIndex = 0;
    private bool hasOpenedOnce = false;

    private readonly List<string> evolutionSteps = new List<string>
    {
        "<b>Contexte initial :</b> Avant cela, la vie était constituée de procaryotes (bactéries et archées), des cellules simples sans noyau ni organites complexes.",
        "<b>L'événement clé :</b> Une cellule hôte (probablement une archée) a englobé une bactérie aérobie (capable d'utiliser l'oxygène) par un processus de phagocytose, mais sans la digérer.",
        "<b>Symbiose :</b> Au lieu de mourir, la bactérie a survécu à l'intérieur. Elle fournissait à l'hôte une énergie bien plus efficace (grâce à la respiration cellulaire), tandis que l'hôte offrait protection et nutriments.",
        "<b>Évolution en organites :</b> Avec le temps, cette bactérie endosymbiotique est devenue la mitochondrie, le \"moteur\" de la cellule. Plus tard, certaines cellules ont intégré des cyanobactéries, donnant naissance aux chloroplastes chez les plantes et les algues.",
        "<b>Formation du noyau :</b> Parallèlement, des invaginations de la membrane plasmique de la cellule hôte se seraient repliées vers l'intérieur, entourant le matériel génétique (ADN) pour former la membrane nucléaire. Cela a protégé l'ADN et permis une régulation plus fine de l'expression des gènes.",
        "<b>Vers la Multicellularité :</b> La division cellulaire contrôlée (mitose), la communication via des jonctions, et la différenciation cellulaire ont permis de passer d'organismes unicellulaires à des entités multicellulaires plus complexes.",
        "<b>L'Explosion Cambrienne :</b> Juste après l'apparition de la vie multicellulaire, la biodiversité explose littéralement. Symbiose, compétition, chaînes alimentaires complexes et colonisation de nouveaux biomes marquent cette ère foisonnante.",
        "<b>La Colonisation des Terres Fermes :</b> La vie passe de l'eau à la terre, ce qui est l'aventure la plus risquée mais la plus gratifiante. La gravité, la dessiccation (séchage) et les UV deviennent des facteurs de survie."
    };

    private void Start()
    {
        CreateLifeWindowUI();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEpochChanged += CheckEpoch;
            // Check immediately on start just in case it's already past Prebiotic
            CheckEpoch(GameManager.Instance.CurrentEpoch);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEpochChanged -= CheckEpoch;
        }
    }

    private void CheckEpoch(PlanetEpoch epoch)
    {
        if (!hasOpenedOnce && (int)epoch >= (int)PlanetEpoch.Prebiotic)
        {
            ShowLifeWindow();
            hasOpenedOnce = true;
        }
    }

    private void EnsureMiniGameControllerExists()
    {
        if (EvolutionMiniGameController.Instance == null)
        {
            GameObject go = new GameObject("EvolutionMiniGameController");
            go.AddComponent<EvolutionMiniGameController>();
        }
    }

    private void EnsureMulticellularityMiniGameExists()
    {
        if (MulticellularityMiniGameController.Instance == null)
        {
            GameObject go = new GameObject("MulticellularityMiniGameController");
            go.AddComponent<MulticellularityMiniGameController>();
        }
    }

    private void EnsureCambrianMiniGameExists()
    {
        if (CambrianMiniGameController.Instance == null)
        {
            GameObject go = new GameObject("CambrianMiniGameController");
            go.AddComponent<CambrianMiniGameController>();
        }
    }

    private void EnsureLandColonizationMiniGameExists()
    {
        if (LandColonizationMiniGameController.Instance == null)
        {
            GameObject go = new GameObject("LandColonizationMiniGameController");
            go.AddComponent<LandColonizationMiniGameController>();
        }
    }

    private void CreateLifeWindowUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        lifeWindowPanel = new GameObject("HUD_LifeEvolutionWindow", typeof(RectTransform));
        lifeWindowPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = lifeWindowPanel.GetComponent<RectTransform>();
        // Anchor Bottom-Left
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(20f, 20f);
        panelRect.sizeDelta = new Vector2(400f, 250f);

        Image bg = lifeWindowPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.15f, 0.2f, 0.95f);

        VerticalLayoutGroup layout = lifeWindowPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Title Row
        GameObject titleRow = new GameObject("TitleRow", typeof(RectTransform));
        titleRow.transform.SetParent(lifeWindowPanel.transform, false);
        HorizontalLayoutGroup titleLayout = titleRow.AddComponent<HorizontalLayoutGroup>();
        titleLayout.childControlHeight = true;
        titleLayout.childControlWidth = true;
        LayoutElement titleRowElem = titleRow.AddComponent<LayoutElement>();
        titleRowElem.minHeight = 30f;
        titleRowElem.preferredHeight = 30f;

        GameObject titleTextObj = new GameObject("TitleText", typeof(RectTransform));
        titleTextObj.transform.SetParent(titleRow.transform, false);
        LayoutElement titleTextElem = titleTextObj.AddComponent<LayoutElement>();
        titleTextElem.flexibleWidth = 1f;
        titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "<b>Évolution de la vie</b>";
        titleText.fontSize = 18;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        closeButton = CreateButton(titleRow.transform, "X", HideLifeWindow, new Color(0.6f, 0.2f, 0.2f, 1f));
        LayoutElement closeBtnElem = closeButton.gameObject.AddComponent<LayoutElement>();
        closeBtnElem.minWidth = 30f;
        closeBtnElem.preferredWidth = 30f;
        closeBtnElem.flexibleWidth = 0;

        // Body Text
        GameObject bodyRow = new GameObject("BodyRow", typeof(RectTransform));
        bodyRow.transform.SetParent(lifeWindowPanel.transform, false);
        explanationText = bodyRow.AddComponent<TextMeshProUGUI>();
        explanationText.fontSize = 14;
        explanationText.color = new Color(0.85f, 0.9f, 0.95f, 1f);
        explanationText.enableWordWrapping = true;
        explanationText.alignment = TextAlignmentOptions.TopLeft;
        LayoutElement bodyElem = bodyRow.AddComponent<LayoutElement>();
        bodyElem.minHeight = 120f;
        bodyElem.flexibleHeight = 1f;

        // Try Evolution Button
        tryEvolutionButton = CreateButton(lifeWindowPanel.transform, "Essayer l'évolution (Mini-jeu)", OnTryEvolution, new Color(0.2f, 0.6f, 0.2f, 1f));
        LayoutElement tryBtnElem = tryEvolutionButton.gameObject.AddComponent<LayoutElement>();
        tryBtnElem.minHeight = 40f;
        tryBtnElem.preferredHeight = 40f;

        // Controls Row
        GameObject controlsRow = new GameObject("ControlsRow", typeof(RectTransform));
        controlsRow.transform.SetParent(lifeWindowPanel.transform, false);
        HorizontalLayoutGroup controlsLayout = controlsRow.AddComponent<HorizontalLayoutGroup>();
        controlsLayout.childControlHeight = true;
        controlsLayout.childControlWidth = true;
        controlsLayout.spacing = 20f;
        LayoutElement controlsElem = controlsRow.AddComponent<LayoutElement>();
        controlsElem.minHeight = 40f;
        controlsElem.preferredHeight = 40f;
        controlsElem.flexibleHeight = 0;

        prevButton = CreateButton(controlsRow.transform, "Précédent", OnPrevStep, new Color(0.2f, 0.4f, 0.6f, 1f));
        nextButton = CreateButton(controlsRow.transform, "Suivant", OnNextStep, new Color(0.2f, 0.4f, 0.6f, 1f));

        UpdateStepUI();
        lifeWindowPanel.SetActive(false);
    }

    private Button CreateButton(Transform parent, string text, UnityEngine.Events.UnityAction action, Color bgColor)
    {
        GameObject btnGo = new GameObject($"Btn_{text}", typeof(RectTransform));
        btnGo.transform.SetParent(parent, false);

        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.color = bgColor;

        Button button = btnGo.AddComponent<Button>();
        button.targetGraphic = btnImg;
        button.onClick.AddListener(action);

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(btnGo.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return button;
    }

    private void UpdateStepUI()
    {
        if (explanationText != null)
        {
            explanationText.text = evolutionSteps[currentStepIndex];
        }

        if (tryEvolutionButton != null)
        {
            bool showButton = (currentStepIndex == 1 || currentStepIndex == 2 || currentStepIndex == 5 || currentStepIndex == 6 || currentStepIndex == 7);
            tryEvolutionButton.gameObject.SetActive(showButton);

            TextMeshProUGUI btnText = tryEvolutionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                if (currentStepIndex == 5)
                    btnText.text = "Explorer la Multicellularité";
                else if (currentStepIndex == 6)
                    btnText.text = "Déclencher l'Explosion Cambrienne";
                else if (currentStepIndex == 7)
                    btnText.text = "Coloniser les Terres";
                else
                    btnText.text = "Essayer l'évolution (Mini-jeu)";
            }
        }

        if (prevButton != null)
        {
            prevButton.interactable = currentStepIndex > 0;
        }

        if (nextButton != null)
        {
            nextButton.interactable = currentStepIndex < evolutionSteps.Count - 1;
        }
    }

    private void OnNextStep()
    {
        if (currentStepIndex < evolutionSteps.Count - 1)
        {
            currentStepIndex++;
            UpdateStepUI();
        }
    }

    private void OnPrevStep()
    {
        if (currentStepIndex > 0)
        {
            currentStepIndex--;
            UpdateStepUI();
        }
    }

    private void OnTryEvolution()
    {
        HideLifeWindow();

        if (currentStepIndex == 7)
        {
            EnsureLandColonizationMiniGameExists();
            LandColonizationMiniGameController.Instance.StartMiniGame(() => {
                ShowLifeWindow();
                if (GameManager.Instance != null && !GameManager.Instance.IsLandColonizationUnlocked)
                {
                    GameManager.Instance.UnlockLandColonization();
                }
            });
        }
        else if (currentStepIndex == 6)
        {
            EnsureCambrianMiniGameExists();
            CambrianMiniGameController.Instance.StartMiniGame(() => {
                ShowLifeWindow();
                if (GameManager.Instance != null && !GameManager.Instance.IsCambrianExplosionUnlocked)
                {
                    GameManager.Instance.UnlockCambrianExplosion();
                }
                if (currentStepIndex < evolutionSteps.Count - 1)
                {
                    OnNextStep(); // advance to next step after success
                }
            });
        }
        else if (currentStepIndex == 5)
        {
            EnsureMulticellularityMiniGameExists();
            MulticellularityMiniGameController.Instance.StartMiniGame(() => {
                ShowLifeWindow();
                if (currentStepIndex < evolutionSteps.Count - 1)
                {
                    OnNextStep(); // advance to next step after success
                }
            });
        }
        else
        {
            EnsureMiniGameControllerExists();
            EvolutionMiniGameController.Instance.StartMiniGame(() => {
                ShowLifeWindow();
                if (currentStepIndex < evolutionSteps.Count - 1)
                {
                    OnNextStep(); // advance to next step after success
                }
            });
        }
    }

    public void ShowLifeWindow()
    {
        if (lifeWindowPanel != null)
        {
            lifeWindowPanel.SetActive(true);
            lifeWindowPanel.transform.SetAsLastSibling();
        }
    }

    public void HideLifeWindow()
    {
        if (lifeWindowPanel != null)
        {
            lifeWindowPanel.SetActive(false);
        }
    }

    public void ToggleLifeWindow()
    {
        if (lifeWindowPanel != null)
        {
            if (lifeWindowPanel.activeSelf)
                HideLifeWindow();
            else
                ShowLifeWindow();
        }
    }
}
