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
            // On s'assure que la production de base de 0.0001 est balancée avec le puits biologique
            // pour permettre une stabilisation organique autour de 0.21 atm
            // (La consommation (respiration) est d'environ 0.00005 de base + scale,
            // donc 0.0001 de production permet à l'O2 de monter et de se stabiliser avec le scale de respiration)
            float dt = Time.deltaTime * gm.SimulatedYearsPerRealSecond / 300f; // Ensure production scales with simulation speed

            // Note: AddOxygen in GameManager handles the per-step rate, but we need to supply the rate multiplied by dt
            float productionRate = 0.0001f * gm.WaterRatio * dt; // Scale with oceans

            // Generate oxygen (O2)
            gm.AddOxygen(productionRate);
        }
    }
}
