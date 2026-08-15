using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class VolcanoGrowthTester : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private CubeSphereTerrain terrain;

    [Header("Paramètres d'Éruption")]
    [SerializeField, Range(0, 5)] private int targetFace = 0;
    [SerializeField] private Vector2Int targetCell = new Vector2Int(16, 16);
    [SerializeField] private float eruptionDuration = 3.0f;

    [Header("Dimensions Finales")]
    [SerializeField] private float finalPeakHeight = 0.8f;
    [SerializeField] private int finalRadius = 8;

    [Header("Coordonnées Alternatives (Optionnel)")]
    [SerializeField] private bool useSphericalCoords = false;
    [SerializeField, Range(-180f, 180f)] private float targetLongitude = 45f;
    [SerializeField, Range(-90f, 90f)] private float targetLatitude = 15f;
    [SerializeField] private float finalRadiusDegrees = 12f;

    [Header("Options de Rendu")]
    [SerializeField] private bool clearBeforeUpdate = true;

    private bool isErupting = false;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame && !isErupting)
        {
            TriggerEruption();
        }
    }

    [ContextMenu("Trigger Smooth Eruption")]
    public void TriggerEruption()
    {
        if (!isErupting && Application.isPlaying)
        {
            StartCoroutine(EruptVolcanoSmoothly());
        }
    }

    private IEnumerator EruptVolcanoSmoothly()
    {
        if (terrain == null)
        {
            Debug.LogWarning("[VolcanoGrowthTester] Aucun terrain CubeSphereTerrain assigné !");
            yield break;
        }

        isErupting = true;
        Debug.Log("[VolcanoGrowthTester] Éruption démarrée...");

        float elapsedTime = 0f;

        while (elapsedTime < eruptionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / eruptionDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float currentPeak = smoothT * finalPeakHeight;

            // Récupération des coordonnées géographiques cibles
            GetEruptionCoords(out float lon, out float lat, out float finalRadiusDeg);
            float currentRadiusDeg = Mathf.Lerp(0.1f, finalRadiusDeg, smoothT);

            if (clearBeforeUpdate)
            {
                terrain.Field?.Clear(0f);
            }

            // Ajout du volcan
            terrain.AddVolcanoDegrees(lon, lat, currentRadiusDeg, currentPeak);

            // Forcer l'application immédiate au shader (contourne le StepGrowth lent ou si le jeu est en pause)
            terrain.Field?.SnapToTarget();

            yield return null;
        }

        Debug.Log("[VolcanoGrowthTester] Éruption terminée !");
        isErupting = false;
    }

    private void GetEruptionCoords(out float longitude, out float latitude, out float radiusDeg)
    {
        if (useSphericalCoords)
        {
            longitude = targetLongitude;
            latitude = targetLatitude;
            radiusDeg = finalRadiusDegrees;
            return;
        }

        int res = 64;
        if (terrain != null)
        {
            var fieldInfo = terrain.GetType().GetField("resolution", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldInfo != null)
            {
                res = (int)fieldInfo.GetValue(terrain);
            }
        }

        Vector3 dir = FaceCellToDirection(targetFace, targetCell.x, targetCell.y, res);
        longitude = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        latitude = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;

        radiusDeg = ((float)finalRadius / res) * 90f;
    }

    private Vector3 FaceCellToDirection(int face, int x, int y, int res)
    {
        Vector3[] faceNormals =
        {
            Vector3.up, Vector3.down,
            Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back
        };

        Vector3 localUp = faceNormals[Mathf.Clamp(face, 0, 5)];
        Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        Vector3 axisB = Vector3.Cross(localUp, axisA);

        Vector2 percent = new Vector2(
            Mathf.Clamp(x, 0, res - 1),
            Mathf.Clamp(y, 0, res - 1)
        ) / Mathf.Max(1, res - 1);

        Vector3 pointOnCube =
            localUp +
            (percent.x - 0.5f) * 2f * axisA +
            (percent.y - 0.5f) * 2f * axisB;

        return pointOnCube.normalized;
    }
}
