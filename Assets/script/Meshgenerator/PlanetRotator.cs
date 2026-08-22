using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlanetRotator : MonoBehaviour
{
    [Header("Keyboard Rotation Settings")]
    [SerializeField] private float keyboardRotationSpeed = 80f; // degrees per second

    [Header("Mouse/Touch Rotation Settings")]
    [SerializeField] private float mouseSensitivity = 0.25f;
    [SerializeField] private bool allowMouseDrag = true;
    [SerializeField] private float inertiaDamping = 5.0f;

    [Header("Input Action")]
    [SerializeField] private InputAction rotatePlanetAction;

    private float currentRotationVelocity = 0f;
    private Vector2 lastMousePosition;
    private bool isDragging = false;

    private void Awake()
    {
        // Define default InputAction with A/D and Arrow key bindings if none are set
        if (rotatePlanetAction == null || rotatePlanetAction.bindings.Count == 0)
        {
            rotatePlanetAction = new InputAction("RotatePlanet", InputActionType.Value);
            rotatePlanetAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
        }
    }

    private void OnEnable()
    {
        rotatePlanetAction?.Enable();
    }

    private void OnDisable()
    {
        rotatePlanetAction?.Disable();
    }

    private void Update()
    {
        float targetVelocity = 0f;

        // 1. Keyboard Input
        if (rotatePlanetAction != null)
        {
            float inputVal = rotatePlanetAction.ReadValue<float>();
            if (Mathf.Abs(inputVal) > 0.01f)
            {
                targetVelocity = inputVal * keyboardRotationSpeed;
            }
        }

        // 2. Mouse / Pointer Drag Input
        if (allowMouseDrag && Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            if (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
            {
                // Only start drag if pointer is NOT over UI elements
                bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (!pointerOverUI)
                {
                    isDragging = true;
                    lastMousePosition = mousePos;
                }
            }

            if (isDragging)
            {
                if (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed)
                {
                    Vector2 delta = mousePos - lastMousePosition;
                    lastMousePosition = mousePos;

                    // Calculate rotation velocity from horizontal mouse drag delta
                    if (Mathf.Abs(delta.x) > 0.01f)
                    {
                        targetVelocity = -delta.x * mouseSensitivity * 60f;
                    }
                }
                else
                {
                    isDragging = false;
                }
            }
        }

        // 3. Smooth velocity transition & inertia damping
        if (Mathf.Abs(targetVelocity) > 0.01f)
        {
            currentRotationVelocity = Mathf.Lerp(currentRotationVelocity, targetVelocity, 15f * Time.deltaTime);
        }
        else
        {
            currentRotationVelocity = Mathf.Lerp(currentRotationVelocity, 0f, inertiaDamping * Time.deltaTime);
        }

        // 4. Apply rotation around North-South axis (local Y-axis)
        if (Mathf.Abs(currentRotationVelocity) > 0.01f)
        {
            float angle = currentRotationVelocity * Time.deltaTime;
            transform.Rotate(Vector3.up, angle, Space.Self);
        }
    }
}
