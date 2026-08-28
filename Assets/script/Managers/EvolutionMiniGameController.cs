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
    private RectTransform visualizerArea;
    private class VisualCell
    {
        public RectTransform rt;
        public Image membrane;
        public RectTransform nucleusRt;
        public Image nucleus;
        public UnityEngine.UI.Outline glow;
        public System.Collections.Generic.List<RectTransform> engulfedBacteria = new System.Collections.Generic.List<RectTransform>();
        public System.Collections.Generic.List<RectTransform> connections = new System.Collections.Generic.List<RectTransform>();
        public System.Collections.Generic.List<RectTransform> internalParticles = new System.Collections.Generic.List<RectTransform>();
        public Vector2 velocity;
        public float divisionTimer;
        public int phaseLevel;
        public Vector2 targetSize = new Vector2(40, 40);
    }
    private System.Collections.Generic.List<VisualCell> visualCells = new System.Collections.Generic.List<VisualCell>();
    private System.Action onWinCallback;
    public int highestCompletedPhase = 1;
    private int highestActivePhase = 1;

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
        visualizerArea = new GameObject("VisualizerArea", typeof(RectTransform)).GetComponent<RectTransform>();
        visualizerArea.transform.SetParent(canvas.transform, false);
        visualizerArea.anchorMin = new Vector2(1f, 0f);
        visualizerArea.anchorMax = new Vector2(1f, 0f);
        visualizerArea.sizeDelta = new Vector2(350, 450);
        visualizerArea.anchoredPosition = new Vector2(-185, 235);
        Image visBg = visualizerArea.gameObject.AddComponent<Image>();
        visBg.color = new Color(0.02f, 0.05f, 0.1f, 1f);
        GameObject visTextObj = new GameObject("VisTitle", typeof(RectTransform));
        visTextObj.transform.SetParent(visualizerArea.transform, false);
        RectTransform visTextRt = visTextObj.GetComponent<RectTransform>();
        visTextRt.anchorMin = new Vector2(0.5f, 1f);
        visTextRt.anchorMax = new Vector2(0.5f, 1f);
        visTextRt.anchoredPosition = new Vector2(0, -30);
        visTextRt.sizeDelta = new Vector2(300, 50);
        TMP_Text visTitle = visTextObj.AddComponent<TextMeshProUGUI>();
        visTitle.fontSize = 20;
        visTitle.alignment = TextAlignmentOptions.Center;
        visTitle.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        visTitle.text = "Évolution Cellulaire";

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
        foreach (var cell in visualCells) { if (cell.rt != null) Destroy(cell.rt.gameObject); }
        visualCells.Clear();
        visualCells.Add(CreateVisualCell(Vector2.zero, 1));
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
        int currentLevel = 1;
        if (currentPhase == MinigamePhase.Phase2_Engulfment) currentLevel = 2;
        else if (currentPhase == MinigamePhase.Phase3_Symbiosis) currentLevel = 3;
        else if (currentPhase == MinigamePhase.Phase4_Success) currentLevel = 4;

        highestActivePhase = UnityEngine.Mathf.Max(highestActivePhase, currentLevel);
        int targetPhaseLevel = UnityEngine.Mathf.Max(highestCompletedPhase, highestActivePhase);

        UpdateVisualCells(targetPhaseLevel);

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

    private VisualCell CreateVisualCell(Vector2 pos, int phaseLevel)
    {
        VisualCell cell = new VisualCell();
        cell.phaseLevel = phaseLevel;
        GameObject cellGo = new GameObject("VisCell", typeof(RectTransform));
        cellGo.transform.SetParent(visualizerArea, false);
        cell.rt = cellGo.GetComponent<RectTransform>();
        cell.rt.anchorMin = new Vector2(0.5f, 0.5f);
        cell.rt.anchorMax = new Vector2(0.5f, 0.5f);
        cell.rt.sizeDelta = new Vector2(40, 40);
        cell.rt.anchoredPosition = pos;
        cell.membrane = cellGo.AddComponent<Image>();
        cell.membrane.color = new Color(0.2f, 0.8f, 0.8f, 0.5f);

        cell.glow = cellGo.AddComponent<UnityEngine.UI.Outline>();
        cell.glow.effectColor = new Color(1f, 1f, 0.5f, 0.8f);
        cell.glow.effectDistance = new Vector2(2, -2);

        GameObject nucGo = new GameObject("Nucleus", typeof(RectTransform));
        nucGo.transform.SetParent(cellGo.transform, false);
        cell.nucleusRt = nucGo.GetComponent<RectTransform>();
        cell.nucleusRt.anchorMin = new Vector2(0.5f, 0.5f);
        cell.nucleusRt.anchorMax = new Vector2(0.5f, 0.5f);
        cell.nucleusRt.sizeDelta = new Vector2(10, 10);
        cell.nucleus = nucGo.AddComponent<Image>();
        cell.nucleus.color = new Color(0.2f, 0.2f, 0.8f, 0.8f);
        cell.nucleus.gameObject.SetActive(false);

        cell.velocity = Random.insideUnitCircle.normalized * Random.Range(20f, 40f);
        cell.divisionTimer = Random.Range(3f, 5f);
        UpdateCellVisuals(cell, phaseLevel);
        return cell;
    }

    private void UpdateCellVisuals(VisualCell cell, int phaseLevel)
    {
        cell.phaseLevel = phaseLevel;
        if (phaseLevel >= 1)
        {
             cell.membrane.color = new Color(0.2f, 0.8f, 0.8f, 0.5f);
        }

        if (phaseLevel >= 2 && cell.engulfedBacteria.Count == 0)
        {
            GameObject bactGo = new GameObject("EngulfedBact", typeof(RectTransform));
            bactGo.transform.SetParent(cell.rt, false);
            RectTransform bactRt = bactGo.GetComponent<RectTransform>();
            bactRt.anchorMin = new Vector2(0.5f, 0.5f);
            bactRt.anchorMax = new Vector2(0.5f, 0.5f);
            bactRt.sizeDelta = new Vector2(10, 10);
            bactRt.anchoredPosition = new Vector2(10, 10);
            Image bactImg = bactGo.AddComponent<Image>();
            bactImg.color = new Color(0.8f, 0.1f, 0.1f, 1f);
            cell.engulfedBacteria.Add(bactRt);
        }
        if (phaseLevel >= 3 && cell.connections.Count == 0)
        {
            foreach (var bact in cell.engulfedBacteria)
            {
                GameObject connGo = new GameObject("Connection", typeof(RectTransform));
                connGo.transform.SetParent(cell.rt, false);
                connGo.transform.SetSiblingIndex(0);
                RectTransform connRt = connGo.GetComponent<RectTransform>();
                connRt.anchorMin = new Vector2(0.5f, 0.5f);
                connRt.anchorMax = new Vector2(0.5f, 0.5f);
                connRt.sizeDelta = new Vector2(Vector2.Distance(Vector2.zero, bact.anchoredPosition), 2);
                connRt.anchoredPosition = bact.anchoredPosition / 2f;
                float angle = Mathf.Atan2(bact.anchoredPosition.y, bact.anchoredPosition.x) * Mathf.Rad2Deg;
                connRt.localRotation = Quaternion.Euler(0, 0, angle);
                Image connImg = connGo.AddComponent<Image>();
                connImg.color = new Color(1, 1, 1, 0.5f);
                cell.connections.Add(connRt);
            }
        }
        if (phaseLevel >= 4)
        {
            cell.targetSize = new Vector2(60, 60);
            cell.nucleus.gameObject.SetActive(true);
            cell.nucleusRt.sizeDelta = new Vector2(18, 18);
            cell.nucleus.color = new Color(0.4f, 0.2f, 0.9f, 0.9f);
            cell.membrane.color = new Color(0.2f, 0.9f, 0.5f, 0.6f);
            cell.glow.effectColor = new Color(0.2f, 0.9f, 0.5f, 0.8f);

            if (cell.internalParticles.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    GameObject pGo = new GameObject("Particle", typeof(RectTransform));
                    pGo.transform.SetParent(cell.rt, false);
                    RectTransform pRt = pGo.GetComponent<RectTransform>();
                    pRt.anchorMin = new Vector2(0.5f, 0.5f);
                    pRt.anchorMax = new Vector2(0.5f, 0.5f);
                    pRt.sizeDelta = new Vector2(4, 4);
                    pRt.anchoredPosition = Random.insideUnitCircle * 15f;
                    Image pImg = pGo.AddComponent<Image>();
                    pImg.color = new Color(1f, 1f, 1f, 0.6f);
                    cell.internalParticles.Add(pRt);
                }
            }
        }
    }

    private void UpdateVisualCells(int targetPhaseLevel)
    {
        if (visualizerArea == null || !visualizerArea.gameObject.activeInHierarchy) return;

        System.Collections.Generic.List<VisualCell> newCells = new System.Collections.Generic.List<VisualCell>();
        float dt = Time.deltaTime;
        foreach (var cell in visualCells)
        {
            if (cell.phaseLevel < targetPhaseLevel) UpdateCellVisuals(cell, targetPhaseLevel);

            cell.rt.anchoredPosition += cell.velocity * dt;
            if (cell.rt.anchoredPosition.x < -180) { cell.rt.anchoredPosition = new Vector2(-180, cell.rt.anchoredPosition.y); cell.velocity.x *= -1; }
            if (cell.rt.anchoredPosition.x > 180) { cell.rt.anchoredPosition = new Vector2(180, cell.rt.anchoredPosition.y); cell.velocity.x *= -1; }
            if (cell.rt.anchoredPosition.y < -280) { cell.rt.anchoredPosition = new Vector2(cell.rt.anchoredPosition.x, -280); cell.velocity.y *= -1; }
            if (cell.rt.anchoredPosition.y > 250) { cell.rt.anchoredPosition = new Vector2(cell.rt.anchoredPosition.x, 250); cell.velocity.y *= -1; }

            // Phase animations
            if (cell.phaseLevel >= 2)
            {
                foreach (var bact in cell.engulfedBacteria)
                {
                    if (bact != null) {
                        float scale = 1f + Mathf.Sin(Time.time * 5f) * 0.3f;
                        bact.localScale = new Vector3(scale, scale, 1f);
                        bact.GetComponent<Image>().color = Color.Lerp(Color.red, Color.magenta, Mathf.PingPong(Time.time * 2f, 1f));
                    }
                }
            }
            if (cell.phaseLevel >= 3)
            {
                foreach (var conn in cell.connections)
                {
                    if (conn != null) {
                        conn.GetComponent<Image>().color = Color.Lerp(new Color(1,1,1,0.2f), new Color(1,1,1,0.8f), Mathf.PingPong(Time.time * 4f, 1f));
                    }
                }
            }
            if (cell.phaseLevel >= 4)
            {
                cell.rt.sizeDelta = Vector2.Lerp(cell.rt.sizeDelta, cell.targetSize, dt * 2f);
                cell.rt.localScale = new Vector3(1f + Mathf.Sin(Time.time * 2f + cell.rt.GetInstanceID()) * 0.1f, 1f + Mathf.Cos(Time.time * 2.5f + cell.rt.GetInstanceID()) * 0.1f, 1f);
                foreach (var p in cell.internalParticles)
                {
                    if (p != null) {
                        p.anchoredPosition += Random.insideUnitCircle * 20f * dt;
                        if (p.anchoredPosition.magnitude > 20f) p.anchoredPosition = Vector2.zero;
                    }
                }
            }
            else {
                cell.rt.sizeDelta = Vector2.Lerp(cell.rt.sizeDelta, new Vector2(40, 40), dt * 2f);
                cell.rt.localScale = Vector3.one;
            }

            float divisionSpeed = (targetPhaseLevel >= 4) ? 3f : 1f;
            cell.divisionTimer -= dt * divisionSpeed;
            if (cell.divisionTimer <= 0 && visualCells.Count + newCells.Count < 30)
            {
                cell.divisionTimer = Random.Range(4f, 6f);
                VisualCell child = CreateVisualCell(cell.rt.anchoredPosition + Random.insideUnitCircle * 10f, targetPhaseLevel);
                child.velocity = -cell.velocity + Random.insideUnitCircle * 10f;
                newCells.Add(child);
            }
            else if (cell.divisionTimer <= 0) cell.divisionTimer = Random.Range(4f, 6f);
        }
        visualCells.AddRange(newCells);
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
        highestCompletedPhase = 4;
        onWinCallback?.Invoke();
    }
}
