using UnityEngine;
using Unity.Mathematics;
using Unity.Cinemachine;

namespace LifeStory.Core
{

    public class PlanetCameraController : MonoBehaviour
    {
        [Header("Camera References")]
        [SerializeField] private CinemachineCamera globalCamera;
        [SerializeField] private CinemachineCamera regionalCamera;
        [SerializeField] private CinemachineCamera localCamera;
        [SerializeField] private Transform planetCenter;

        [Header("Global View Settings")]
        [SerializeField] private float globalDistance = 25f;        // Réduit pour voir la sphère
        [SerializeField] private float globalMinDistance = 18f;
        [SerializeField] private float globalMaxDistance = 30f;
        [SerializeField] private float globalRotationSpeed = 50f;

        [Header("Regional View Settings")]
        [SerializeField] private float regionalDistance = 18f;       // Réduit
        [SerializeField] private float regionalMinDistance = 6f;
        [SerializeField] private float regionalMaxDistance = 18f;
        [SerializeField] private float regionalPanSpeed = 10f;

        [Header("Local View Settings")]
        [SerializeField] private float localDistance = 6f;         // Réduit
        [SerializeField] private float localMinDistance = 5f;
        [SerializeField] private float localMaxDistance = 6f;
        [SerializeField] private float localPanSpeed = 5f;

        [Header("Transition Settings")]
        [SerializeField] private float transitionSpeed = 2f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Input Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float scrollSensitivity = 5f;
        [SerializeField] private float panSensitivity = 1f;

        // État actuel
        private CameraMode currentMode = CameraMode.Global;
        private bool isTransitioning = false;
        private float currentDistance;
        private Vector3 currentTarget;
        private Vector2 currentRotation;

        // Input tracking
        private Vector3 lastMousePosition;
        private bool isDragging = false;
        private bool isPanning = false;

        // Events
        public static System.Action<CameraMode> OnCameraModeChanged;
        public static System.Action<Vector3> OnCameraTargetChanged;

        public static PlanetCameraController Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeCamera();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SetupCinemachineCameras();
            SetCameraMode(CameraMode.Global);
        }

        private void Update()
        {
            if (!isTransitioning)
            {
                HandleInput();
                UpdateCameraPosition();
            }
        }

        private void InitializeCamera()
        {
            if (planetCenter == null)
            {
                // Créer un point central par défaut
                GameObject centerObj = new GameObject("PlanetCenter");
                planetCenter = centerObj.transform;
                planetCenter.position = Vector3.zero;
            }

            currentTarget = planetCenter.position;
            currentDistance = globalDistance;
            currentRotation = new Vector2(0, 30); // Angle initial
        }

        private void SetupCinemachineCameras()
        {
            // Si les caméras Cinemachine ne sont pas assignées, les créer
            if (globalCamera == null)
                globalCamera = CreateVirtualCamera("Global Camera", globalDistance);

            if (regionalCamera == null)
                regionalCamera = CreateVirtualCamera("Regional Camera", regionalDistance);

            if (localCamera == null)
                localCamera = CreateVirtualCamera("Local Camera", localDistance);

            // Désactiver toutes sauf la globale
            regionalCamera.gameObject.SetActive(false);
            localCamera.gameObject.SetActive(false);
        }

        private CinemachineCamera CreateVirtualCamera(string name, float distance)
        {
            GameObject camObj = new GameObject(name);
            camObj.transform.SetParent(transform);

            var vcam = camObj.AddComponent<CinemachineCamera>();
            vcam.Follow = planetCenter;
            vcam.LookAt = planetCenter;

            // Configuration Orbital Follow pour Unity 6
            var orbitalFollow = camObj.AddComponent<CinemachineOrbitalFollow>();
            orbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.ThreeRing;
            orbitalFollow.Radius = distance;

            return vcam;
        }

        private void HandleInput()
        {
            HandleMouseInput();
            HandleScrollInput();
            HandleKeyboardInput();
        }

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonDown(1))
            {
                isPanning = true;
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                isPanning = false;
            }

            if (isDragging)
            {
                HandleCameraRotation();
            }
            else if (isPanning && currentMode != CameraMode.Global)
            {
                HandleCameraPanning();
            }
        }

        private void HandleCameraRotation()
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

            currentRotation.x += mouseDelta.y * mouseSensitivity * Time.deltaTime;
            currentRotation.y += mouseDelta.x * mouseSensitivity * Time.deltaTime;

            // Limiter la rotation verticale
            currentRotation.x = Mathf.Clamp(currentRotation.x, -80f, 80f);

            lastMousePosition = Input.mousePosition;
        }

        private void HandleCameraPanning()
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

            float panSpeed = currentMode == CameraMode.Regional ? regionalPanSpeed : localPanSpeed;
            Vector3 panOffset = new Vector3(-mouseDelta.x, 0, -mouseDelta.y) * panSpeed * Time.deltaTime;

            // Transformer selon l'orientation de la caméra
            panOffset = transform.TransformDirection(panOffset);
            currentTarget += panOffset;

            lastMousePosition = Input.mousePosition;
        }

        private void HandleScrollInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                ZoomCamera(scroll * scrollSensitivity);

                // Auto-transition entre modes selon la distance
                CheckAutoModeTransition();
            }
        }

        private void HandleKeyboardInput()
        {
            // Raccourcis clavier pour changer de mode
            if (Input.GetKeyDown(KeyCode.Alpha1))
                SetCameraMode(CameraMode.Global);
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                SetCameraMode(CameraMode.Regional);
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                SetCameraMode(CameraMode.Local);
        }

        private void ZoomCamera(float zoomDelta)
        {
            currentDistance -= zoomDelta;

            // Appliquer les limites selon le mode
            switch (currentMode)
            {
                case CameraMode.Global:
                    currentDistance = Mathf.Clamp(currentDistance, globalMinDistance, globalMaxDistance);
                    break;
                case CameraMode.Regional:
                    currentDistance = Mathf.Clamp(currentDistance, regionalMinDistance, regionalMaxDistance);
                    break;
                case CameraMode.Local:
                    currentDistance = Mathf.Clamp(currentDistance, localMinDistance, localMaxDistance);
                    break;
            }
        }

        private void CheckAutoModeTransition()
        {
            // Transition automatique basée sur la distance avec hysteresis pour éviter les allers-retours
            if (currentMode == CameraMode.Global && currentDistance < 18f) // Seuil plus bas
            {
                SetCameraMode(CameraMode.Regional);
            }
            else if (currentMode == CameraMode.Regional)
            {
                if (currentDistance > 22f) // Seuil plus haut pour éviter l'aller-retour
                    SetCameraMode(CameraMode.Global);
                else if (currentDistance < 6f) // Seuil pour passer au local
                    SetCameraMode(CameraMode.Local);
            }
            else if (currentMode == CameraMode.Local && currentDistance > 10f) // Seuil plus haut
            {
                SetCameraMode(CameraMode.Regional);
            }
        }

        private void UpdateCameraPosition()
        {
            var activeCamera = GetActiveCamera();
            if (activeCamera != null)
            {
                // Chercher le component CinemachineOrbitalFollow
                var orbitalFollow = activeCamera.GetComponent<CinemachineOrbitalFollow>();
                if (orbitalFollow != null)
                {
                    try
                    {
                        // Mettre à jour les axes - c'est ça le secret !
                        orbitalFollow.HorizontalAxis.Value = currentRotation.y;
                        orbitalFollow.VerticalAxis.Value = currentRotation.x;
                        orbitalFollow.RadialAxis.Value = currentDistance;  // ← C'EST ÇA !

                        ////Debug.Log($"Mis à jour axes - H:{currentRotation.y}, V:{currentRotation.x}, Radial:{currentDistance}");
                    }
                    catch (System.Exception e)
                    {
                        ////Debug.Log($"Erreur mise à jour caméra: {e.Message}");
                    }
                }

                // Mettre à jour la cible si on fait du panning
                if (currentMode != CameraMode.Global && currentTarget != planetCenter.position)
                {
                    Transform targetTransform = CreateOrUpdateTargetTransform(currentTarget);
                    activeCamera.Follow = targetTransform;
                    activeCamera.LookAt = targetTransform;
                }
                else
                {
                    activeCamera.Follow = planetCenter;
                    activeCamera.LookAt = planetCenter;
                }
            }
        }

        public void SetCameraMode(CameraMode newMode)
        {
            if (currentMode == newMode || isTransitioning) return;

            CameraMode oldMode = currentMode;
            currentMode = newMode;

            // Activer/désactiver les bonnes caméras
            globalCamera.gameObject.SetActive(newMode == CameraMode.Global);
            regionalCamera.gameObject.SetActive(newMode == CameraMode.Regional);
            localCamera.gameObject.SetActive(newMode == CameraMode.Local);

            // Ajuster la distance selon le nouveau mode
            switch (newMode)
            {
                case CameraMode.Global:
                    currentDistance = globalDistance;
                    currentTarget = planetCenter.position;
                    break;
                case CameraMode.Regional:
                    currentDistance = regionalDistance;
                    break;
                case CameraMode.Local:
                    currentDistance = localDistance;
                    break;
            }

            OnCameraModeChanged?.Invoke(newMode);
            ////Debug.Log($"Camera mode: {oldMode} → {newMode}");
        }

        public void FocusOnPosition(Vector3 worldPosition)
        {
            currentTarget = worldPosition;
            OnCameraTargetChanged?.Invoke(worldPosition);
        }

        private Transform targetTransform;

        private Transform CreateOrUpdateTargetTransform(Vector3 position)
        {
            if (targetTransform == null)
            {
                GameObject targetObj = new GameObject("CameraTarget");
                targetTransform = targetObj.transform;
            }
            targetTransform.position = position;
            return targetTransform;
        }

        private CinemachineCamera GetActiveCamera()
        {
            switch (currentMode)
            {
                case CameraMode.Global: return globalCamera;
                case CameraMode.Regional: return regionalCamera;
                case CameraMode.Local: return localCamera;
                default: return globalCamera;
            }
        }

        // Propriétés publiques
        public CameraMode CurrentMode => currentMode;
        public Vector3 CurrentTarget => currentTarget;
        public float CurrentDistance => currentDistance;

        private void OnGUI()
        {
            return;
            // Debug UI
            GUI.Box(new Rect(Screen.width - 250, 10, 240, 120), "");
            GUI.Label(new Rect(Screen.width - 240, 25, 220, 20), $"Mode Caméra: {currentMode}");
            GUI.Label(new Rect(Screen.width - 240, 45, 220, 20), $"Distance: {currentDistance:F1}");
            GUI.Label(new Rect(Screen.width - 240, 65, 220, 20), $"Rotation: {currentRotation.x:F0}°, {currentRotation.y:F0}°");
            GUI.Label(new Rect(Screen.width - 240, 85, 220, 20), "1/2/3: Changer mode");
            GUI.Label(new Rect(Screen.width - 240, 105, 220, 20), "Clic: Rotation, Clic droit: Pan");
        }
    }
}