// TimeScaleGUI.cs - Nouveau script pour contrôler la vitesse de jeu
using UnityEngine;
using LifeStory.Core;
using UnityEditor;

namespace LifeStory.UI
{
    public class TimeScaleGUI : MonoBehaviour
    {
        [Header("GUI Settings")]
        [SerializeField] private bool showGUI = true;
        [SerializeField] private Rect windowRect = new Rect(20, 20, 300, 200);
        [SerializeField] private string windowTitle = "Contrôle Vitesse Temps";

        [Header("Speed Control")]
        [SerializeField] private float minSpeed = 0.1f;
        [SerializeField] private float maxSpeed = 10f;
        [SerializeField] private float[] presetSpeeds = { 0.5f, 1f, 2f, 3f, 5f, 10f };

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // État interne
        private float currentMultiplier = 1f;
        private GameManager gameManager;
        private bool isWindowOpen = true;

        // Style GUI (optionnel)
        private GUIStyle windowStyle;
        private GUIStyle buttonStyle;
        private GUIStyle labelStyle;

        private void Start()
        {
            // Trouver le GameManager
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                //Debug.LogError("TimeScaleGUI: GameManager non trouvé!");
                enabled = false;
                return;
            }

            // Obtenir le multiplicateur actuel
            currentMultiplier = gameManager.GetPlayerTimeMultiplier();
            LogDebug($"TimeScaleGUI initialisé - Multiplicateur actuel: {currentMultiplier}");

            InitializeStyles();
        }

        private void InitializeStyles()
        {
            // Styles GUI personnalisés (optionnel - pour un meilleur rendu)
            //windowStyle = new GUIStyle(GUI.skin.window);
            //buttonStyle = new GUIStyle(GUI.skin.button);
            //labelStyle = new GUIStyle(GUI.skin.label);

            // Personnalisation des styles
            //buttonStyle.fontSize = 12;
            //labelStyle.fontSize = 11;
        }

        private void Update()
        {
            HandleKeyboardInput();

            if (gameManager != null)
            {
                float gmMultiplier = gameManager.GetPlayerTimeMultiplier();

                // PROTECTION : Détecter les vitesses anormales
                if (gmMultiplier > 50f)
                {
                    //Debug.LogError($"🚨 VITESSE ANORMALE DÉTECTÉE: ×{gmMultiplier} - Correction automatique");
                    gameManager.SetTimeScale(1f);
                    currentMultiplier = 1f;
                    return;
                }

                // Synchronisation normale
                if (Mathf.Abs(gmMultiplier - currentMultiplier) > 0.1f)
                {
                    currentMultiplier = gmMultiplier;
                }
            }
        }

        private void HandleKeyboardInput()
        {
            // Raccourcis clavier
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                ChangeSpeed(-0.5f);
            }
            else if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                ChangeSpeed(0.5f);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                SetSpeed(1f); // Reset à vitesse normale
            }

            // Touches numériques pour vitesses prédéfinies
            for (int i = 0; i < presetSpeeds.Length && i < 10; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    SetSpeed(presetSpeeds[i]);
                }
            }
        }

        private void OnGUI()
        {
            return;
            if (!showGUI || !isWindowOpen) return;

            // Dessiner la fenêtre
            windowRect = GUI.Window(0, windowRect, DrawTimeScaleWindow, windowTitle);
        }

        private void DrawTimeScaleWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // === INFORMATIONS ACTUELLES ===
            GUILayout.Label("État Actuel:", EditorStyles.boldLabel);

            if (gameManager != null)
            {
                float baseScale = gameManager.GetBaseTimeScale();
                float finalScale = gameManager.CurrentTimeScale;

                GUILayout.Label($"Phase: {gameManager.CurrentPhase}");
                GUILayout.Label($"Base: ×{baseScale:F0}");
                GUILayout.Label($"Multiplicateur: ×{currentMultiplier:F1}");
                GUILayout.Label($"Final: ×{finalScale:F0}", EditorStyles.boldLabel);
            }

            GUILayout.Space(10);

            // === CONTRÔLE PAR SLIDER SÉCURISÉ ===
            GUILayout.Label("Contrôle Vitesse:", EditorStyles.boldLabel);

            float sliderValue = GUILayout.HorizontalSlider(currentMultiplier, minSpeed, maxSpeed);

            // CORRECTION : Seulement appliquer si différence significative ET relâchement souris
            if (Mathf.Abs(sliderValue - currentMultiplier) > 0.1f && Event.current.type == EventType.Used)
            {
                //Debug.Log($"🎛️ SLIDER: {currentMultiplier:F1} → {sliderValue:F1}");
                SetSpeed(sliderValue);
            }
            // OU mettre à jour visuel seulement
            else if (Mathf.Abs(sliderValue - currentMultiplier) > 0.01f)
            {
                currentMultiplier = sliderValue; // Mise à jour visuelle seulement
            }

            GUILayout.Space(5);

            // === BOUTONS VITESSES PRÉDÉFINIES (sécurisés) ===
            GUILayout.Label("Vitesses Prédéfinies:");

            GUILayout.BeginHorizontal();
            foreach (float speed in presetSpeeds)
            {
                bool isCurrentSpeed = Mathf.Abs(speed - currentMultiplier) < 0.1f;

                Color oldColor = GUI.backgroundColor;
                if (isCurrentSpeed)
                    GUI.backgroundColor = Color.green;

                // CORRECTION : Vérifier qu'on ne remet pas la même vitesse
                if (GUILayout.Button($"×{speed:F1}") && !isCurrentSpeed)
                {
                    //Debug.Log($"🔘 BOUTON: Vitesse prédéfinie ×{speed:F1}");
                    SetSpeed(speed);
                }

                GUI.backgroundColor = oldColor;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // === CONTRÔLES RELATIFS (sécurisés) ===
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("−0.5"))
            {
                //Debug.Log($"🔽 RELATIF: -{0.5f}");
                ChangeSpeed(-0.5f);
            }
            if (GUILayout.Button("Reset") && Mathf.Abs(currentMultiplier - 1f) > 0.1f)
            {
                //Debug.Log($"🔄 RESET: ×1");
                SetSpeed(1f);
            }
            if (GUILayout.Button("+0.5"))
            {
                //Debug.Log($"🔼 RELATIF: +{0.5f}");
                ChangeSpeed(0.5f);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // === CONTRÔLES DE FENÊTRE ===
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Fermer"))
            {
                isWindowOpen = false;
            }
            if (GUILayout.Button("Debug"))
            {
                DebugTimeScale();
            }
            if (GUILayout.Button("URGENCE STOP"))
            {
                // BOUTON PANIQUE pour arrêter les vitesses folles
                ////Debug.LogWarning("🚨 ARRÊT D'URGENCE - Retour vitesse normale");
                gameManager?.SetTimeScale(1f);
                currentMultiplier = 1f;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }


        // === MÉTHODES DE CONTRÔLE ===
        private void SetSpeed(float multiplier)
        {
            if (gameManager == null)
            {
                //Debug.LogError("GameManager null dans SetSpeed");
                return;
            }

            float clampedMultiplier = Mathf.Clamp(multiplier, minSpeed, maxSpeed);

            // PROTECTION : Éviter les appels redondants
            if (Mathf.Abs(clampedMultiplier - currentMultiplier) < 0.01f)
            {
                return; // Pas de changement significatif
            }

            float oldMultiplier = currentMultiplier;
            currentMultiplier = clampedMultiplier;

            // APPEL SÉCURISÉ au GameManager
            //Debug.Log($"🎮 SetSpeed: {oldMultiplier:F2} → {clampedMultiplier:F2}");
            gameManager.SetTimeScale(clampedMultiplier);

            // Vérification post-appel
            float actualMultiplier = gameManager.GetPlayerTimeMultiplier();
            if (Mathf.Abs(actualMultiplier - clampedMultiplier) > 0.1f)
            {
                ////Debug.LogWarning($"⚠️ Incohérence: Demandé={clampedMultiplier:F2}, Obtenu={actualMultiplier:F2}");
            }
        }

        private void ChangeSpeed(float delta)
        {
            SetSpeed(currentMultiplier + delta);
        }

        // === MÉTHODES PUBLIQUES ===
        public void ToggleGUI()
        {
            isWindowOpen = !isWindowOpen;
        }

        public void ShowGUI()
        {
            isWindowOpen = true;
        }

        public void HideGUI()
        {
            isWindowOpen = false;
        }

        // === DEBUG ===
        private void DebugTimeScale()
        {
            if (gameManager == null) return;

            //Debug.Log("=== DEBUG TIMESCALE GUI ===");
            //Debug.Log($"GUI Multiplicateur: {currentMultiplier}");
            //Debug.Log($"GM Multiplicateur: {gameManager.GetPlayerTimeMultiplier()}");
            //Debug.Log($"GM Base Scale: {gameManager.GetBaseTimeScale()}");
            //Debug.Log($"GM Current Scale: {gameManager.CurrentTimeScale}");
            //Debug.Log($"GM Phase: {gameManager.CurrentPhase}");
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                //Debug.Log($"[TimeScaleGUI] {message}");
            }
        }

        // AJOUTER cette méthode d'urgence
        [ContextMenu("Emergency Reset TimeScale")]
        public void EmergencyResetTimeScale()
        {
            ////Debug.LogWarning("🚨 RESET D'URGENCE TIMESCALE");
            if (gameManager != null)
            {
                gameManager.SetTimeScale(1f);
                currentMultiplier = 1f;
                //Debug.Log("TimeScale remis à ×1");
            }
        }
    }
}
