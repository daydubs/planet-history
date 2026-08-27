using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class EvolutionMiniGameController : MonoBehaviour
{
    public static EvolutionMiniGameController Instance { get; private set; }

    private GameObject mainPanel;
    private RectTransform gameArea;
    private TMP_Text instructionText;
    private System.Action onWinCallback;

    private enum MinigamePhase { None, Phase1_Pursuit, Phase2_Engulfment, Phase3_Symbiosis, Phase4_Success }
    private MinigamePhase currentPhase = MinigamePhase.None;

    // Phase 1
    private RectTransform playerRect;
    private Vector2 playerPos;
    private List<RectTransform> predators = new List<RectTransform>();
    private List<RectTransform> atpParticles = new List<RectTransform>();
    private RectTransform targetBacteria;
    private float phase1Timer = 0f;
    private float predatorSpawnTimer = 0f;
    private float atpSpawnTimer = 0f;
    private bool targetSpawned = false;

    // Phase 2
    private GameObject qteContainer;
    private RectTransform qteIndicator;
    private float qteTimer = 0f;
    private float qteSpeed = 3f;
    private bool spaceWasPressed = false;
    private Image qteTargetImg;

    // Phase 3
    private GameObject symbiosisContainer;
    private TMP_Text rejectionText;
    private float rejectionTimer = 0f;
    private float rejectionMaxTime = 10f;
    private int nodesClicked = 0;
    private List<GameObject> nodeObjects = new List<GameObject>();
    private RectTransform hostCell;
    private RectTransform innerBact;

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

        mainPanel = new GameObject("EvolutionMiniGamePanel", typeof(RectTransform));
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

        // Phase 2 UI
        qteContainer = new GameObject("QTEContainer", typeof(RectTransform));
        qteContainer.transform.SetParent(gameArea, false);
        RectTransform qteRt = qteContainer.GetComponent<RectTransform>();
        qteRt.anchorMin = new Vector2(0.5f, 0.5f);
        qteRt.anchorMax = new Vector2(0.5f, 0.5f);
        qteRt.sizeDelta = new Vector2(400, 50);
        qteRt.anchoredPosition = new Vector2(0, -200);
        Image qteBg = qteContainer.AddComponent<Image>();
        qteBg.color = Color.gray;

        GameObject qteTarget = new GameObject("QTETarget", typeof(RectTransform));
        qteTarget.transform.SetParent(qteContainer.transform, false);
        RectTransform qteTargetRt = qteTarget.GetComponent<RectTransform>();
        qteTargetRt.anchorMin = new Vector2(0.5f, 0.5f);
        qteTargetRt.anchorMax = new Vector2(0.5f, 0.5f);
        qteTargetRt.sizeDelta = new Vector2(80, 50);
        qteTargetImg = qteTarget.AddComponent<Image>();
        qteTargetImg.color = Color.green;

        GameObject qteIndObj = new GameObject("QTEIndicator", typeof(RectTransform));
        qteIndObj.transform.SetParent(qteContainer.transform, false);
        qteIndicator = qteIndObj.GetComponent<RectTransform>();
        qteIndicator.anchorMin = new Vector2(0.5f, 0.5f);
        qteIndicator.anchorMax = new Vector2(0.5f, 0.5f);
        qteIndicator.sizeDelta = new Vector2(10, 60);
        Image qteIndImg = qteIndObj.AddComponent<Image>();
        qteIndImg.color = Color.white;
        qteContainer.SetActive(false);

        // Phase 3 UI
        symbiosisContainer = new GameObject("SymbiosisContainer", typeof(RectTransform));
        symbiosisContainer.transform.SetParent(gameArea, false);
        RectTransform symRt = symbiosisContainer.GetComponent<RectTransform>();
        symRt.anchorMin = Vector2.zero;
        symRt.anchorMax = Vector2.one;
        symRt.sizeDelta = Vector2.zero;
        symRt.offsetMin = Vector2.zero;
        symRt.offsetMax = Vector2.zero;

        GameObject rejTextObj = new GameObject("RejectionText", typeof(RectTransform));
        rejTextObj.transform.SetParent(symbiosisContainer.transform, false);
        RectTransform rejRt = rejTextObj.GetComponent<RectTransform>();
        rejRt.anchorMin = new Vector2(0.5f, 1f);
        rejRt.anchorMax = new Vector2(0.5f, 1f);
        rejRt.anchoredPosition = new Vector2(0, -30);
        rejRt.sizeDelta = new Vector2(400, 50);
        rejectionText = rejTextObj.AddComponent<TextMeshProUGUI>();
        rejectionText.fontSize = 24;
        rejectionText.alignment = TextAlignmentOptions.Center;
        rejectionText.color = Color.red;
        symbiosisContainer.SetActive(false);

        mainPanel.SetActive(false);
    }

    public void StartMiniGame(System.Action onSuccess)
    {
        onWinCallback = onSuccess;
        mainPanel.SetActive(true);
        mainPanel.transform.SetAsLastSibling();
        StartPhase1();
    }

    private void StartPhase1()
    {
        currentPhase = MinigamePhase.Phase1_Pursuit;
        instructionText.text = "Phase 1 : Évitez les prédateurs et cherchez la bactérie rouge !";

        ClearGameArea();
        qteContainer.SetActive(false);
        symbiosisContainer.SetActive(false);

        playerRect = CreateEntity("Player", new Vector2(30, 30), Color.cyan);
        playerPos = Vector2.zero;
        playerRect.anchoredPosition = playerPos;

        phase1Timer = 0f;
        predatorSpawnTimer = 0f;
        atpSpawnTimer = 0f;
        targetSpawned = false;
        predators.Clear();
        atpParticles.Clear();
    }

    private RectTransform CreateEntity(string name, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(gameArea, false);
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
            if (child != qteContainer.transform && child != symbiosisContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void Update()
    {
        if (currentPhase == MinigamePhase.None) return;

        if (currentPhase == MinigamePhase.Phase1_Pursuit)
        {
            UpdatePhase1();
        }
        else if (currentPhase == MinigamePhase.Phase2_Engulfment)
        {
            UpdatePhase2();
        }
        else if (currentPhase == MinigamePhase.Phase3_Symbiosis)
        {
            UpdatePhase3();
        }
    }

    private void UpdatePhase1()
    {
        phase1Timer += Time.deltaTime;

        // Player Movement
        Vector2 move = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) move.y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) move.y -= 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) move.x -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) move.x += 1;
        }

        if (move.sqrMagnitude > 0) move.Normalize();
        playerPos += move * 250f * Time.deltaTime;

        // Clamp
        playerPos.x = Mathf.Clamp(playerPos.x, -280, 280);
        playerPos.y = Mathf.Clamp(playerPos.y, -280, 280);
        playerRect.anchoredPosition = playerPos;

        // Spawn Predators
        predatorSpawnTimer += Time.deltaTime;
        if (predatorSpawnTimer > 2.5f && predators.Count < 5)
        {
            predatorSpawnTimer = 0f;
            RectTransform pred = CreateEntity("Predator", new Vector2(40, 40), Color.magenta);
            Vector2 spawnPos = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * 350f;
            pred.anchoredPosition = spawnPos;
            predators.Add(pred);
        }

        // Spawn ATP
        atpSpawnTimer += Time.deltaTime;
        if (atpSpawnTimer > 1f)
        {
            atpSpawnTimer = 0f;
            RectTransform atp = CreateEntity("ATP", new Vector2(15, 15), Color.yellow);
            atp.anchoredPosition = new Vector2(Random.Range(-280, 280), Random.Range(-280, 280));
            atpParticles.Add(atp);
            Destroy(atp.gameObject, 3f); // Disappear quickly
        }

        atpParticles.RemoveAll(item => item == null);

        // Collect ATP
        for (int i = atpParticles.Count - 1; i >= 0; i--)
        {
            if (atpParticles[i] != null && Vector2.Distance(atpParticles[i].anchoredPosition, playerPos) < 25f)
            {
                Destroy(atpParticles[i].gameObject);
                atpParticles.RemoveAt(i);
                // Slight speed boost or score, ignored for now
            }
        }

        // Move Predators
        for (int i = predators.Count - 1; i >= 0; i--)
        {
            RectTransform pred = predators[i];
            Vector2 dir = (playerPos - pred.anchoredPosition).normalized;
            pred.anchoredPosition += dir * 120f * Time.deltaTime;

            if (Vector2.Distance(pred.anchoredPosition, playerPos) < 35f)
            {
                // GameOver -> Restart Phase 1
                StartPhase1();
                return;
            }
        }

        // Spawn Target
        if (!targetSpawned && phase1Timer > 6f)
        {
            targetSpawned = true;
            targetBacteria = CreateEntity("TargetBact", new Vector2(25, 25), Color.red);
            targetBacteria.anchoredPosition = new Vector2(Random.Range(-250, 250), Random.Range(-250, 250));
        }

        if (targetSpawned && targetBacteria != null)
        {
            if (Vector2.Distance(playerPos, targetBacteria.anchoredPosition) < 30f)
            {
                StartPhase2();
            }
        }
    }

    private void StartPhase2()
    {
        currentPhase = MinigamePhase.Phase2_Engulfment;
        instructionText.text = "Phase 2 : Appuyez sur ESPACE quand la jauge est verte pour englober !";
        ClearGameArea();

        qteContainer.SetActive(true);
        qteContainer.transform.SetAsLastSibling();
        qteTimer = 0f;
        spaceWasPressed = false;

        // Draw huge player and bacteria in background
        hostCell = CreateEntity("BigPlayer", new Vector2(400, 400), new Color(0, 1, 1, 0.3f));
        hostCell.anchoredPosition = new Vector2(0, 50);
        innerBact = CreateEntity("BigBact", new Vector2(100, 100), new Color(1, 0, 0, 0.8f));
        innerBact.anchoredPosition = new Vector2(0, 50);
    }

    private void UpdatePhase2()
    {
        qteTimer += Time.deltaTime * qteSpeed;
        float x = Mathf.Sin(qteTimer) * 190f; // moves between -190 and +190
        qteIndicator.anchoredPosition = new Vector2(x, 0);

        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        if (spacePressed && !spaceWasPressed)
        {
            spaceWasPressed = true;
            if (Mathf.Abs(x) < 40f) // Green zone is 80 width -> -40 to 40
            {
                qteTargetImg.color = Color.white;
                Invoke(nameof(StartPhase3), 1f);
            }
            else
            {
                qteTargetImg.color = Color.red;
                instructionText.text = "Échec ! Bactérie digérée ou rejetée.";
                Invoke(nameof(StartPhase1), 1.5f);
                currentPhase = MinigamePhase.None; // wait
            }
        }
    }

    private void StartPhase3()
    {
        currentPhase = MinigamePhase.Phase3_Symbiosis;
        instructionText.text = "Phase 3 : Cliquez sur les points de connexion avant le rejet !";
        qteContainer.SetActive(false);
        ClearGameArea();

        symbiosisContainer.SetActive(true);
        symbiosisContainer.transform.SetAsLastSibling();

        // Visuals
        hostCell = CreateEntity("HostCell", new Vector2(350, 350), new Color(0, 0.8f, 0.8f, 0.5f));
        innerBact = CreateEntity("InnerBact", new Vector2(80, 80), new Color(1, 0.2f, 0.2f, 0.8f));
        innerBact.transform.SetParent(hostCell, false);

        rejectionTimer = 0f;
        nodesClicked = 0;
        foreach (var n in nodeObjects) Destroy(n);
        nodeObjects.Clear();

        for (int i = 0; i < 5; i++)
        {
            GameObject btnGo = new GameObject("NodeBtn", typeof(RectTransform));
            btnGo.transform.SetParent(gameArea, false);
            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0.5f);
            btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.sizeDelta = new Vector2(40, 40);
            Vector2 pos = Random.insideUnitCircle * 140f;
            btnRt.anchoredPosition = pos;

            Image img = btnGo.AddComponent<Image>();
            img.color = Color.yellow;
            Button btn = btnGo.AddComponent<Button>();

            btn.onClick.AddListener(() => {
                nodesClicked++;
                img.color = Color.green;
                btn.interactable = false;

                // Draw a simple bridge
                GameObject bridge = new GameObject("Bridge", typeof(RectTransform));
                bridge.transform.SetParent(hostCell, false);
                RectTransform brt = bridge.GetComponent<RectTransform>();
                Image bImg = bridge.AddComponent<Image>();
                bImg.color = new Color(1, 1, 1, 0.5f);

                Vector2 dir = pos.normalized;
                float dist = pos.magnitude;
                brt.sizeDelta = new Vector2(dist, 5);
                brt.anchoredPosition = pos / 2f;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                brt.localRotation = Quaternion.Euler(0, 0, angle);
            });

            nodeObjects.Add(btnGo);
        }
    }

    private void UpdatePhase3()
    {
        rejectionTimer += Time.deltaTime;
        float percent = (rejectionTimer / rejectionMaxTime) * 100f;
        rejectionText.text = $"Rejet Immunitaire : {percent:F0}%";

        if (rejectionTimer >= rejectionMaxTime)
        {
            instructionText.text = "Rejet immunitaire total !";
            symbiosisContainer.SetActive(false);
            Invoke(nameof(StartPhase1), 1.5f);
            currentPhase = MinigamePhase.None;
        }
        else if (nodesClicked >= 5)
        {
            currentPhase = MinigamePhase.None;
            Invoke(nameof(StartPhase4), 0.5f);
        }
    }

    private void StartPhase4()
    {
        currentPhase = MinigamePhase.Phase4_Success;
        symbiosisContainer.SetActive(false);
        ClearGameArea();

        instructionText.text = "<color=green>Symbiose réussie !</color>\nLa cellule est plus complexe et produit 10x plus d'énergie.";

        // Beautiful success cell
        RectTransform finalCell = CreateEntity("FinalCell", new Vector2(400, 400), new Color(0.2f, 0.9f, 0.5f, 0.6f));
        RectTransform mito = CreateEntity("Mitochondria", new Vector2(100, 60), new Color(1, 0.5f, 0, 0.9f));
        mito.transform.SetParent(finalCell, false);

        Invoke(nameof(EndMiniGame), 4f);
    }

    private void EndMiniGame()
    {
        mainPanel.SetActive(false);
        currentPhase = MinigamePhase.None;
        onWinCallback?.Invoke();
    }
}
