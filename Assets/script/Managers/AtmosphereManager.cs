using UnityEngine;

public class AtmosphereManager : MonoBehaviour
{
    private CubeSphereTerrain terrain;
    private GameObject atmosphereSphere;
    private MeshRenderer atmosphereRenderer;
    private Material atmosphereMaterial;

    private void Start()
    {
        terrain = FindAnyObjectByType<CubeSphereTerrain>();
        if (terrain == null) return;

        CreateAtmosphere();
    }

    private void CreateAtmosphere()
    {
        atmosphereSphere = new GameObject("AtmosphereSphere");
        atmosphereSphere.transform.SetParent(terrain.transform, false);

        CubeSphereGenerator generator = atmosphereSphere.AddComponent<CubeSphereGenerator>();
        float baseRadius = terrain.BaseRadius;
        generator.SetRadius(baseRadius * 1.05f); // 5% larger

        atmosphereRenderer = atmosphereSphere.GetComponent<MeshRenderer>();

        Shader atmShader = Shader.Find("PlanetHistory/Atmosphere");
        if (atmShader != null)
        {
            atmosphereMaterial = new Material(atmShader);
            atmosphereRenderer.sharedMaterial = atmosphereMaterial;
        }
        else
        {
            Debug.LogError("Atmosphere shader 'PlanetHistory/Atmosphere' not found!");
        }
    }

    private void Update()
    {
        if (atmosphereMaterial == null || GameManager.Instance == null) return;

        float pressure = GameManager.Instance.Pressure;

        // Atmosphere density mapped to pressure.
        // Hadean pressure can be 300atm, Earth normal is 1atm
        // We use a log scale so it doesn't become opaque at high pressures.
        float normalizedPressure = Mathf.Log10(Mathf.Max(1f, pressure));
        float density = Mathf.Clamp(0.5f + normalizedPressure, 0.1f, 5f);

        atmosphereMaterial.SetFloat("_AtmosphereDensity", density);
    }
}
