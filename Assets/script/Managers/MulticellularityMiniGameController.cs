using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class MulticellularityMiniGameController : MonoBehaviour
{
    public static MulticellularityMiniGameController Instance { get; private set; }

    private GameObject mainPanel;
    private RectTransform gameArea;
    private TMP_Text instructionText;
    private System.Action onWinCallback;

    private enum MinigamePhase { None, Phase1_Mitosis, Phase2_GapJunctions, Phase3_Specialization, Phase4_Success }
    private MinigamePhase currentPhase = MinigamePhase.None;

    // UI Containers for different phases
    private GameObject qteContainer;
    private RectTransform qteIndicator;
    private Image qteTargetImg;
    private GameObject gapJunctionsContainer;
    private GameObject specializationContainer;

    // Phase 1
    private float qteTimer = 0f;
    private float qteSpeed = 3f;
    private bool spaceWasPressed = false;
    private RectTransform mitosisCell;

    // Phase 2
    private float rejectionTimer = 0f;
    private float rejectionMaxTime = 12f;
    private int nodesClicked = 0;
    private TMP_Text rejectionText;
    private List<GameObject> nodeObjects = new List<GameObject>();
    private RectTransform cell1;
    private RectTransform cell2;
    private int activeNodePairIndex = -1; // To track which pair we are connecting
    private bool waitingForSecondNode = false;

    // Phase 3
    private bool hasChosenOption = false;
    private int chosenOption = -1;
    private float expressionTimer = 0f;
    private float expressionMaxTime = 2f;
    private RectTransform expressionBar;
    private Image expressionBarFill;

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

        mainPanel = new GameObject("MulticellularityMiniGamePanel", typeof(RectTransform));
        mainPanel.transform.SetParent(canvas.transform, false);
        RectTransform rt = mainPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = mainPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.95f);

        gameArea = new GameObject("GameArea", typeof(RectTransform)).GetComponent<RectTransform>();
        gameArea.transform.SetParent(mainPanel.transform, false);
        gameArea.anchorMin = new Vector2(0.5f, 0.5f);
        gameArea.anchorMax = new Vector2(0.5f, 0.5f);
        gameArea.sizeDelta = new Vector2(600, 600);
        gameArea.anchoredPosition = Vector2.zero;
        Image areaBg = gameArea.gameObject.AddComponent<Image>();
        areaBg.color = new Color(0.05f, 0.1f, 0.15f, 1f);

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

        // Phase 1 UI
        qteContainer = new GameObject("QTEContainer", typeof(RectTransform));
        qteContainer.transform.SetParent(gameArea, false);
        RectTransform qteRt = qteContainer.GetComponent<RectTransform>();
        qteRt.anchorMin = new Vector2(0.5f, 0.5f);
        qteRt.anchorMax = new Vector2(0.5f, 0.5f);
        qteRt.sizeDelta = new Vector2(400, 50);
        qteRt.anchoredPosition = new Vector2(0, -200);

        GameObject qteBg = new GameObject("QTEBg", typeof(RectTransform));
        qteBg.transform.SetParent(qteContainer.transform, false);
        RectTransform qteBgRt = qteBg.GetComponent<RectTransform>();
        qteBgRt.anchorMin = Vector2.zero;
        qteBgRt.anchorMax = Vector2.one;
        qteBgRt.offsetMin = Vector2.zero;
        qteBgRt.offsetMax = Vector2.zero;
        Image qteBgImg = qteBg.AddComponent<Image>();
        qteBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        GameObject qteTarget = new GameObject("QTETarget", typeof(RectTransform));
        qteTarget.transform.SetParent(qteContainer.transform, false);
        RectTransform qteTargetRt = qteTarget.GetComponent<RectTransform>();
        qteTargetRt.anchorMin = new Vector2(0.5f, 0f);
        qteTargetRt.anchorMax = new Vector2(0.5f, 1f);
        qteTargetRt.sizeDelta = new Vector2(80, 0); // width 80 -> center zone
        qteTargetImg = qteTarget.AddComponent<Image>();
        qteTargetImg.color = new Color(0, 1, 0, 0.5f);

        GameObject qteInd = new GameObject("QTEIndicator", typeof(RectTransform));
        qteInd.transform.SetParent(qteContainer.transform, false);
        qteIndicator = qteInd.GetComponent<RectTransform>();
        qteIndicator.anchorMin = new Vector2(0.5f, 0.5f);
        qteIndicator.anchorMax = new Vector2(0.5f, 0.5f);
        qteIndicator.sizeDelta = new Vector2(10, 60);
        Image qteIndImg = qteInd.AddComponent<Image>();
        qteIndImg.color = Color.white;

        // Phase 2 UI
        gapJunctionsContainer = new GameObject("GapJunctionsContainer", typeof(RectTransform));
        gapJunctionsContainer.transform.SetParent(gameArea, false);
        RectTransform gjRt = gapJunctionsContainer.GetComponent<RectTransform>();
        gjRt.anchorMin = Vector2.zero;
        gjRt.anchorMax = Vector2.one;
        gjRt.offsetMin = Vector2.zero;
        gjRt.offsetMax = Vector2.zero;

        // Phase 3 UI
        specializationContainer = new GameObject("SpecializationContainer", typeof(RectTransform));
        specializationContainer.transform.SetParent(gameArea, false);
        RectTransform spRt = specializationContainer.GetComponent<RectTransform>();
        spRt.anchorMin = Vector2.zero;
        spRt.anchorMax = Vector2.one;
        spRt.offsetMin = Vector2.zero;
        spRt.offsetMax = Vector2.zero;

        mainPanel.SetActive(false);
    }

    public void StartMiniGame(System.Action onSuccess)
    {
        onWinCallback = onSuccess;
        mainPanel.SetActive(true);
        mainPanel.transform.SetAsLastSibling();
        StartPhase1();
    }

    private RectTransform CreateEntity(string name, Vector2 size, Color color, Transform parent = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent == null) parent = gameArea;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.color = color;
        return rt;
    }

    private void ClearGameArea()
    {
        foreach (Transform child in gameArea)
        {
            if (child != qteContainer.transform && child != gapJunctionsContainer.transform && child != specializationContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // Also clear children of the phase containers so they don't stack on replay/failure
        foreach (Transform child in gapJunctionsContainer.transform)
        {
            if (child.name != "RejectionText")
                Destroy(child.gameObject);
        }
        foreach (Transform child in specializationContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void Update()
    {
        if (currentPhase == MinigamePhase.None) return;

        if (currentPhase == MinigamePhase.Phase1_Mitosis)
            UpdatePhase1();
        else if (currentPhase == MinigamePhase.Phase2_GapJunctions)
            UpdatePhase2();
        else if (currentPhase == MinigamePhase.Phase3_Specialization)
            UpdatePhase3();
    }

    private void StartPhase1()
    {
        currentPhase = MinigamePhase.Phase1_Mitosis;
        instructionText.text = "Phase 1 : Mitose Assistée. Appuyez sur ESPACE quand la barre est verte !";
        ClearGameArea();

        qteContainer.SetActive(true);
        qteContainer.transform.SetAsLastSibling();
        gapJunctionsContainer.SetActive(false);
        specializationContainer.SetActive(false);

        qteTimer = 0f;
        spaceWasPressed = false;
        qteTargetImg.color = new Color(0, 1, 0, 0.5f);

        mitosisCell = CreateEntity("MitosisCell", new Vector2(300, 300), new Color(0.2f, 0.6f, 0.8f, 0.8f));
        mitosisCell.anchoredPosition = new Vector2(0, 50);

        RectTransform nucleus = CreateEntity("Nucleus", new Vector2(100, 100), new Color(0.8f, 0.2f, 0.8f, 0.9f), mitosisCell);
        nucleus.anchoredPosition = Vector2.zero;
    }

    private void UpdatePhase1()
    {
        qteTimer += Time.deltaTime * qteSpeed;
        float x = Mathf.Sin(qteTimer) * 190f;
        qteIndicator.anchoredPosition = new Vector2(x, 0);

        bool spacePressed = false;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            spacePressed = true;
        }
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space))
        {
            spacePressed = true;
        }
#endif

        if (spacePressed && !spaceWasPressed)
        {
            spaceWasPressed = true;
            if (Mathf.Abs(x) < 40f) // Green zone is 80 width -> -40 to 40
            {
                qteTargetImg.color = Color.white;
                // Animate division quickly
                mitosisCell.sizeDelta = new Vector2(400, 200);
                Invoke(nameof(StartPhase2), 1f);
            }
            else
            {
                qteTargetImg.color = Color.red;
                instructionText.text = "Échec ! La division a cassé l'ADN.";
                Invoke(nameof(StartPhase1), 1.5f);
                currentPhase = MinigamePhase.None;
            }
        }
    }

    private void StartPhase2()
    {
        currentPhase = MinigamePhase.Phase2_GapJunctions;
        instructionText.text = "Phase 2 : Communication. Reliez les points correspondants !";
        ClearGameArea();
        qteContainer.SetActive(false);
        gapJunctionsContainer.SetActive(true);
        gapJunctionsContainer.transform.SetAsLastSibling();

        cell1 = CreateEntity("Cell1", new Vector2(250, 300), new Color(0.2f, 0.6f, 0.8f, 0.8f), gapJunctionsContainer.transform);
        cell1.anchoredPosition = new Vector2(-130, 0);
        cell2 = CreateEntity("Cell2", new Vector2(250, 300), new Color(0.2f, 0.6f, 0.8f, 0.8f), gapJunctionsContainer.transform);
        cell2.anchoredPosition = new Vector2(130, 0);

        if (rejectionText == null)
        {
            GameObject rtObj = new GameObject("RejectionText", typeof(RectTransform));
            rtObj.transform.SetParent(gapJunctionsContainer.transform, false);
            RectTransform rtRt = rtObj.GetComponent<RectTransform>();
            rtRt.anchorMin = new Vector2(0.5f, 1f);
            rtRt.anchorMax = new Vector2(0.5f, 1f);
            rtRt.anchoredPosition = new Vector2(0, -50);
            rtRt.sizeDelta = new Vector2(400, 50);
            rejectionText = rtObj.AddComponent<TextMeshProUGUI>();
            rejectionText.fontSize = 20;
            rejectionText.alignment = TextAlignmentOptions.Center;
            rejectionText.color = Color.red;
        }

        rejectionTimer = 0f;
        nodesClicked = 0;
        waitingForSecondNode = false;
        activeNodePairIndex = -1;

        foreach (var n in nodeObjects) Destroy(n);
        nodeObjects.Clear();

        // Create 3 pairs of nodes
        float[] yPositions = { 80f, 0f, -80f };
        for (int i = 0; i < 3; i++)
        {
            int pairIndex = i; // capture

            // Left Node (Cell 1 right edge)
            GameObject node1 = CreateNode(new Vector2(125, yPositions[i]), cell1.transform, pairIndex, true);
            // Right Node (Cell 2 left edge)
            GameObject node2 = CreateNode(new Vector2(-125, yPositions[i]), cell2.transform, pairIndex, false);

            nodeObjects.Add(node1);
            nodeObjects.Add(node2);
        }
    }

    private GameObject CreateNode(Vector2 pos, Transform parent, int pairIndex, bool isLeft)
    {
        GameObject btnGo = new GameObject($"Node_{pairIndex}_{(isLeft?"L":"R")}", typeof(RectTransform));
        btnGo.transform.SetParent(parent, false);
        RectTransform btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(30, 30);
        btnRt.anchoredPosition = pos;

        Image img = btnGo.AddComponent<Image>();
        img.color = Color.yellow;
        Button btn = btnGo.AddComponent<Button>();

        btn.onClick.AddListener(() => {
            if (!btn.interactable) return;

            if (!waitingForSecondNode)
            {
                // First click of a pair
                activeNodePairIndex = pairIndex;
                waitingForSecondNode = true;
                img.color = Color.cyan;
                btn.interactable = false;
            }
            else
            {
                // Second click
                if (activeNodePairIndex == pairIndex)
                {
                    // Match!
                    img.color = Color.green;
                    btn.interactable = false;

                    // Find the other node to turn green too
                    foreach (var n in nodeObjects)
                    {
                        if (n.name.StartsWith($"Node_{pairIndex}_"))
                        {
                            n.GetComponent<Image>().color = Color.green;
                            n.GetComponent<Button>().interactable = false;
                        }
                    }

                    // Draw bridge
                    GameObject bridge = new GameObject("GapJunction", typeof(RectTransform));
                    bridge.transform.SetParent(gapJunctionsContainer.transform, false);
                    RectTransform brt = bridge.GetComponent<RectTransform>();
                    Image bImg = bridge.AddComponent<Image>();
                    bImg.color = new Color(0, 1, 0, 0.6f);
                    brt.sizeDelta = new Vector2(50, 10);
                    brt.anchoredPosition = new Vector2(0, pos.y); // centered between cells

                    nodesClicked++;
                    waitingForSecondNode = false;
                    activeNodePairIndex = -1;
                }
                else
                {
                    // Mismatch - reset the previous node
                    img.color = Color.yellow;
                    foreach (var n in nodeObjects)
                    {
                        if (n.name.StartsWith($"Node_{activeNodePairIndex}_"))
                        {
                            n.GetComponent<Image>().color = Color.yellow;
                            n.GetComponent<Button>().interactable = true;
                        }
                    }
                    waitingForSecondNode = false;
                    activeNodePairIndex = -1;
                }
            }
        });
        return btnGo;
    }

    private void UpdatePhase2()
    {
        rejectionTimer += Time.deltaTime;
        float percent = (rejectionTimer / rejectionMaxTime) * 100f;
        rejectionText.text = $"Rejet de Surface : {percent:F0}%";

        if (rejectionTimer >= rejectionMaxTime)
        {
            instructionText.text = "Échec de la communication. Les cellules se séparent !";
            gapJunctionsContainer.SetActive(false);
            Invoke(nameof(StartPhase1), 1.5f);
            currentPhase = MinigamePhase.None;
        }
        else if (nodesClicked >= 3) // 3 pairs
        {
            currentPhase = MinigamePhase.None;
            Invoke(nameof(StartPhase3), 1f);
        }
    }

    private void StartPhase3()
    {
        currentPhase = MinigamePhase.Phase3_Specialization;
        instructionText.text = "Phase 3 : Spécialisation. Choisissez un rôle (A ou B) !";
        ClearGameArea();
        gapJunctionsContainer.SetActive(false);
        specializationContainer.SetActive(true);
        specializationContainer.transform.SetAsLastSibling();

        cell1 = CreateEntity("Cell1", new Vector2(250, 300), new Color(0.2f, 0.6f, 0.8f, 0.8f), specializationContainer.transform);
        cell1.anchoredPosition = new Vector2(-130, 0);
        cell2 = CreateEntity("Cell2", new Vector2(250, 300), new Color(0.2f, 0.6f, 0.8f, 0.8f), specializationContainer.transform);
        cell2.anchoredPosition = new Vector2(130, 0);

        // Options Buttons
        GameObject btnA = CreateChoiceButton(new Vector2(-130, -200), "A: Protection\n(Paroi épaisse)", 0);
        GameObject btnB = CreateChoiceButton(new Vector2(130, -200), "B: Capture\n(Cils/Mouvement)", 1);

        // Expression Bar
        expressionBar = CreateEntity("ExpressionBarBg", new Vector2(400, 30), new Color(0.2f, 0.2f, 0.2f, 1f), specializationContainer.transform);
        expressionBar.anchoredPosition = new Vector2(0, 200);

        RectTransform fillRt = CreateEntity("ExpressionBarFill", new Vector2(0, 30), new Color(1, 0, 1, 1f), expressionBar);
        fillRt.anchorMin = new Vector2(0, 0.5f);
        fillRt.anchorMax = new Vector2(0, 0.5f);
        fillRt.pivot = new Vector2(0, 0.5f);
        fillRt.anchoredPosition = new Vector2(-200, 0);
        expressionBarFill = fillRt.GetComponent<Image>();
        expressionBar.gameObject.SetActive(false);

        hasChosenOption = false;
        chosenOption = -1;
        expressionTimer = 0f;
    }

    private GameObject CreateChoiceButton(Vector2 pos, string text, int optionIndex)
    {
        GameObject btnGo = new GameObject($"ChoiceBtn_{optionIndex}", typeof(RectTransform));
        btnGo.transform.SetParent(specializationContainer.transform, false);
        RectTransform btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(200, 60);
        btnRt.anchoredPosition = pos;

        Image img = btnGo.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        Button btn = btnGo.AddComponent<Button>();

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(btnGo.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 16;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        btn.onClick.AddListener(() => {
            if (hasChosenOption) return;
            hasChosenOption = true;
            chosenOption = optionIndex;
            img.color = Color.yellow;
            instructionText.text = "Maintenez ESPACE pour exprimer le gène !";
            expressionBar.gameObject.SetActive(true);
        });

        return btnGo;
    }

    private void UpdatePhase3()
    {
        if (!hasChosenOption) return;

        bool spaceHeld = false;
        if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
        {
            spaceHeld = true;
        }
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.Space))
        {
            spaceHeld = true;
        }
#endif

        if (spaceHeld)
        {
            expressionTimer += Time.deltaTime;
            float fillAmount = expressionTimer / expressionMaxTime;
            expressionBarFill.rectTransform.sizeDelta = new Vector2(400f * fillAmount, 30);

            if (expressionTimer >= expressionMaxTime)
            {
                currentPhase = MinigamePhase.None;

                // Visual feedback of differentiation
                if (chosenOption == 0) // Protection
                {
                    cell1.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 1f); // Grey thick wall
                    cell1.sizeDelta = new Vector2(270, 320);
                }
                else // Capture
                {
                    cell2.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f, 1f); // Green cilia
                }

                Invoke(nameof(StartPhase4), 1f);
            }
        }
        else
        {
            // Decay if not held
            expressionTimer = Mathf.Max(0, expressionTimer - Time.deltaTime);
            float fillAmount = expressionTimer / expressionMaxTime;
            expressionBarFill.rectTransform.sizeDelta = new Vector2(400f * fillAmount, 30);
        }
    }

    private void StartPhase4()
    {
        currentPhase = MinigamePhase.Phase4_Success;
        specializationContainer.SetActive(false);
        ClearGameArea();

        instructionText.text = "<color=green>Multicellularité initiale réussie !</color>\nLa coopération permet de défier les géants.";

        // Big combined organism
        RectTransform org = CreateEntity("CombinedOrganism", new Vector2(500, 400), new Color(0.2f, 0.7f, 0.7f, 0.8f));

        if (chosenOption == 0)
        {
            RectTransform armor = CreateEntity("Armor", new Vector2(520, 420), new Color(0.5f, 0.5f, 0.5f, 0.5f), org);
            armor.anchoredPosition = Vector2.zero;
        }
        else
        {
            RectTransform cilia = CreateEntity("Cilia", new Vector2(50, 50), new Color(0.2f, 0.8f, 0.2f, 1f), org);
            cilia.anchoredPosition = new Vector2(250, 0); // Right side
        }

        Invoke(nameof(EndMiniGame), 4f);
    }

    private void EndMiniGame()
    {
        mainPanel.SetActive(false);
        currentPhase = MinigamePhase.None;
        onWinCallback?.Invoke();
    }
}
