using UnityEngine;

public class MicrobeManager : MonoBehaviour
{
    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSimulationStep += HandleSimulationStep;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSimulationStep -= HandleSimulationStep;
        }
    }

    private void HandleSimulationStep()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        // Photosynthesis only happens in the Photosynthesis epoch
        if (gm.CurrentEpoch != PlanetEpoch.Photosynthesis) return;

        // Life thrives in a habitable temperature range
        if (gm.SurfaceTemperature > 273f && gm.SurfaceTemperature < 350f)
        {
            // Calculate a production rate. It could be scaled by temperature, water ratio, etc.
            float productionRate = 0.0001f * gm.WaterRatio; // Scale with oceans

            // Generate oxygen (O2)
            gm.AddOxygen(productionRate);
        }
    }
}
