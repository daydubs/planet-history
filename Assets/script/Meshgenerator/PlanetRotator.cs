using UnityEngine;
using UnityEngine.InputSystem;

public class PlanetRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 50f; // degrees per second

    [Header("Input Action")]
    [SerializeField] private InputAction rotatePlanetAction;

    private void Awake()
    {
        // Define default InputAction with A/D bindings if none are set
        if (rotatePlanetAction == null || rotatePlanetAction.bindings.Count == 0)
        {
            rotatePlanetAction = new InputAction("RotatePlanet", InputActionType.Value);
            rotatePlanetAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
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
        if (rotatePlanetAction == null) return;

        float inputVal = rotatePlanetAction.ReadValue<float>();
        if (Mathf.Abs(inputVal) > 0.01f)
        {
            // Rotate the planet around its North-South axis (local Y-axis)
            float angle = inputVal * rotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, angle, Space.Self);
        }
    }
}
