using UnityEngine;
using Unity.Cinemachine;
using LifeStory.Core;

namespace LifeStory.Core
{
    public class CameraModeController : MonoBehaviour
    {
        [Header("Camera References")]
        [SerializeField] private CinemachineCamera globalCamera;
        [SerializeField] private CinemachineCamera regionalCamera;
        [SerializeField] private CinemachineCamera localCamera;

        [Header("Priority Settings")]
        [SerializeField] private int activePriority = 10;
        [SerializeField] private int inactivePriority = 0;

        [Header("Mouse Control Settings")]
        [SerializeField] private bool enableMouseCapture = true;
        [SerializeField] private KeyCode mouseCaptureToggleKey = KeyCode.LeftAlt;
        [SerializeField] private bool showCursorInstructions = true;
        [SerializeField] private float instructionDisplayTime = 3f;

        // État actuel
        private CameraMode currentMode = CameraMode.Global;
        private bool isMouseCaptured = false;
        private bool hasShownInstructions = false;
        private float instructionTimer = 0f;

        // Events
        public static System.Action<CameraMode> OnCameraModeChanged;
        public static System.Action<bool> OnMouseCaptureChanged;

        public static CameraModeController Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeCameras();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Démarrer en mode Global
            SetCameraMode(CameraMode.Global);

            // Activer la capture de souris au démarrage si activée
            if (enableMouseCapture)
            {
                EnableMouseCapture();
            }
        }

        private void Update()
        {
            HandleInput();
            HandleMouseCapture();

            if (showCursorInstructions && !hasShownInstructions)
            {
                UpdateInstructionDisplay();
            }
        }

        private void InitializeCameras()
        {
            // Trouver automatiquement les caméras si pas assignées
            if (globalCamera == null)
                globalCamera = GameObject.Find("Global Camera")?.GetComponent<CinemachineCamera>();

            if (regionalCamera == null)
                regionalCamera = GameObject.Find("Regional Camera")?.GetComponent<CinemachineCamera>();

            if (localCamera == null)
                localCamera = GameObject.Find("Local Camera")?.GetComponent<CinemachineCamera>();

            // Vérifier que toutes les caméras sont trouvées
            if (globalCamera == null || regionalCamera == null || localCamera == null)
            {
                Debug.LogError($"CameraModeController: Caméras manquantes! Global:{globalCamera != null}, Regional:{regionalCamera != null}, Local:{localCamera != null}");
            }
        }

        private void HandleInput()
        {
            // Touches pour changer de mode (existant)
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SetCameraMode(CameraMode.Global);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetCameraMode(CameraMode.Regional);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SetCameraMode(CameraMode.Local);
            }

            // Alternative avec le pavé numérique (existant)
            if (Input.GetKeyDown(KeyCode.Keypad1))
            {
                SetCameraMode(CameraMode.Global);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad2))
            {
                SetCameraMode(CameraMode.Regional);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad3))
            {
                SetCameraMode(CameraMode.Local);
            }
        }

        private void HandleMouseCapture()
        {
            if (!enableMouseCapture) return;

            // Toggle capture avec la touche définie
            if (Input.GetKeyDown(mouseCaptureToggleKey))
            {
                ToggleMouseCapture();
            }

            // Auto-capture quand on clique dans la fenêtre
            if (!isMouseCaptured && Input.GetMouseButtonDown(0))
            {
                EnableMouseCapture();
            }

            // Libération avec Échap
            if (isMouseCaptured && Input.GetKeyDown(KeyCode.Escape))
            {
                DisableMouseCapture();
            }
        }

        private void EnableMouseCapture()
        {
            if (!isMouseCaptured)
            {
                Cursor.lockState = CursorLockMode.Confined; // Confiner à la fenêtre
                Cursor.visible = false; // Cacher le curseur pour une expérience plus immersive
                isMouseCaptured = true;

                OnMouseCaptureChanged?.Invoke(true);
                Debug.Log("🎮 Souris capturée - Utilisez Échap pour libérer");
            }
        }

        private void DisableMouseCapture()
        {
            if (isMouseCaptured)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                isMouseCaptured = false;

                OnMouseCaptureChanged?.Invoke(false);
                Debug.Log("🖱️ Souris libérée");
            }
        }

        private void ToggleMouseCapture()
        {
            if (isMouseCaptured)
            {
                DisableMouseCapture();
            }
            else
            {
                EnableMouseCapture();
            }
        }

        private void UpdateInstructionDisplay()
        {
            instructionTimer += Time.deltaTime;

            if (instructionTimer >= instructionDisplayTime)
            {
                hasShownInstructions = true;
            }
        }

        public void SetCameraMode(CameraMode newMode)
        {
            if (currentMode == newMode) return;

            CameraMode oldMode = currentMode;
            currentMode = newMode;

            // Mettre à jour les priorités
            UpdateCameraPriorities();

            // Notifier le changement
            OnCameraModeChanged?.Invoke(newMode);

            Debug.Log($"📷 Mode caméra: {oldMode} → {newMode}");
        }

        private void UpdateCameraPriorities()
        {
            // Réinitialiser toutes les priorités à inactif
            if (globalCamera != null)
                globalCamera.Priority = inactivePriority;

            if (regionalCamera != null)
                regionalCamera.Priority = inactivePriority;

            if (localCamera != null)
                localCamera.Priority = inactivePriority;

            // Activer la caméra du mode actuel
            switch (currentMode)
            {
                case CameraMode.Global:
                    if (globalCamera != null)
                        globalCamera.Priority = activePriority;
                    break;

                case CameraMode.Regional:
                    if (regionalCamera != null)
                        regionalCamera.Priority = activePriority;
                    break;

                case CameraMode.Local:
                    if (localCamera != null)
                        localCamera.Priority = activePriority;
                    break;
            }
        }

        // Propriétés publiques
        public CameraMode CurrentMode => currentMode;
        public bool IsMouseCaptured => isMouseCaptured;

        public CinemachineCamera GetActiveCamera()
        {
            switch (currentMode)
            {
                case CameraMode.Global: return globalCamera;
                case CameraMode.Regional: return regionalCamera;
                case CameraMode.Local: return localCamera;
                default: return globalCamera;
            }
        }

        // Méthodes publiques pour contrôle externe
        public void SetGlobalMode() => SetCameraMode(CameraMode.Global);
        public void SetRegionalMode() => SetCameraMode(CameraMode.Regional);
        public void SetLocalMode() => SetCameraMode(CameraMode.Local);

        // Méthodes publiques pour contrôle de la souris
        public void ForceEnableMouseCapture() => EnableMouseCapture();
        public void ForceDisableMouseCapture() => DisableMouseCapture();

        private void OnGUI()
        {
            if (!enableMouseCapture) return;

            // Interface avec instructions améliorées
            GUI.Box(new Rect(Screen.width - 280, 10, 270, 140), "");
            GUI.Label(new Rect(Screen.width - 270, 25, 250, 20), $"📷 Mode Caméra: {currentMode}");

            // Instructions de contrôle
            GUI.Label(new Rect(Screen.width - 270, 45, 250, 20), "Contrôles:");
            GUI.Label(new Rect(Screen.width - 270, 65, 250, 20), "• 1/2/3 = Changer mode caméra");

            // État de capture de souris
            if (isMouseCaptured)
            {
                GUI.Label(new Rect(Screen.width - 270, 85, 250, 20), "🎮 Souris: CAPTURÉE");
                GUI.Label(new Rect(Screen.width - 270, 105, 250, 20), $"• {mouseCaptureToggleKey}/Échap = Libérer");
            }
            else
            {
                GUI.Label(new Rect(Screen.width - 270, 85, 250, 20), "🖱️ Souris: LIBRE");
                GUI.Label(new Rect(Screen.width - 270, 105, 250, 20), $"• Clic/{mouseCaptureToggleKey} = Capturer");
            }

            // Instructions temporaires au démarrage
            if (showCursorInstructions && !hasShownInstructions)
            {
                float alpha = Mathf.Lerp(1f, 0f, instructionTimer / instructionDisplayTime);
                GUI.color = new Color(1, 1, 1, alpha);

                GUI.Box(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 30, 400, 60), "");
                GUI.Label(new Rect(Screen.width / 2 - 190, Screen.height / 2 - 20, 380, 20), "🎮 CONTRÔLES CAMÉRA");
                GUI.Label(new Rect(Screen.width / 2 - 190, Screen.height / 2, 380, 20), $"Cliquez ou appuyez sur {mouseCaptureToggleKey} pour capturer la souris");

                GUI.color = Color.white;
            }
        }

        private void OnValidate()
        {
            // S'assurer que les priorités sont logiques
            if (activePriority <= inactivePriority)
            {
                activePriority = inactivePriority + 10;
                Debug.LogWarning("Active Priority doit être supérieur à Inactive Priority");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Libérer la souris si l'application perd le focus
            if (!hasFocus && isMouseCaptured)
            {
                DisableMouseCapture();
            }
        }
    }
}