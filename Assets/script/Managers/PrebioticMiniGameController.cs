using System;
using System.Collections.Generic;
using UnityEngine;

public class PrebioticMiniGameController : MonoBehaviour
{
    public static PrebioticMiniGameController Instance { get; private set; }

    [Header("Concentrations d'Acides Aminés (0 à 100%)")]
    [SerializeField] private float glycine;
    [SerializeField] private float alanine;
    [SerializeField] private float asparticAcid;
    [SerializeField] private float glutamicAcid;
    [SerializeField] private float serine;
    [SerializeField] private float valine;
    [SerializeField] private float leucine;
    [SerializeField] private float isoleucine;

    [Header("Progression Globale")]
    [SerializeField] private float targetPerAcid = 100f; // 100% par acide aminé

    public event Action OnPrebioticProgressUpdated;

    // Direct Accessors
    public float Glycine => glycine;
    public float Alanine => alanine;
    public float AsparticAcid => asparticAcid;
    public float GlutamicAcid => glutamicAcid;
    public float Serine => serine;
    public float Valine => valine;
    public float Leucine => leucine;
    public float Isoleucine => isoleucine;

    public float TotalProgress
    {
        get
        {
            float sum = glycine + alanine + asparticAcid + glutamicAcid + serine + valine + leucine + isoleucine;
            return Mathf.Clamp01(sum / (8f * targetPerAcid));
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Simule l'expérience de Miller-Urey (1953) : Décharges électriques dans une atmosphère réductrice.
    /// Synthétise principalement Glycine et Alanine.
    /// </summary>
    public void TriggerLightningDischarge()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentEpoch != PlanetEpoch.Prebiotic)
        {
            Debug.LogWarning("[PrebioticMiniGameController] Action indisponible : la planète n'est pas en période pré-biotique.");
            return;
        }

        if (GameManager.Instance != null)
        {
            // Vérification simple de l'atmosphère réductrice
            float availableGas = GameManager.Instance.OtherGasesPressure + GameManager.Instance.WaterVaporPressure;
            float gain = Mathf.Clamp(availableGas * 1.5f, 10f, 25f);

            glycine = Mathf.Min(targetPerAcid, glycine + gain);
            alanine = Mathf.Min(targetPerAcid, alanine + gain * 0.8f);

            GameManager.Instance.LogPlayerAction("Prebiotic Lightning", $"Décharge Miller-Urey déclenchée. Glycine: {glycine:F0}%, Alanine: {alanine:F0}%");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayVolcanicExplosion(Vector3.zero, 0.6f, 1.4f); // Son d'impact/éclair
            }
        }
        else
        {
            glycine = Mathf.Min(targetPerAcid, glycine + 20f);
            alanine = Mathf.Min(targetPerAcid, alanine + 15f);
        }

        OnPrebioticProgressUpdated?.Invoke();
    }

    /// <summary>
    /// Simule les réactions autour des sources hydrothermales sous-marines.
    /// Synthétise Acide Aspartique et Acide Glutamique grâce aux gradients de chaleur et minéraux.
    /// </summary>
    public void TriggerHydrothermalVent()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentEpoch != PlanetEpoch.Prebiotic)
        {
            Debug.LogWarning("[PrebioticMiniGameController] Action indisponible : la planète n'est pas en période pré-biotique.");
            return;
        }

        if (GameManager.Instance != null)
        {
            float gain = 20f;
            asparticAcid = Mathf.Min(targetPerAcid, asparticAcid + gain);
            glutamicAcid = Mathf.Min(targetPerAcid, glutamicAcid + gain * 0.9f);

            GameManager.Instance.LogPlayerAction("Prebiotic Hydrothermal", $"Source hydrothermale activée. Aspartique: {asparticAcid:F0}%, Glutamique: {glutamicAcid:F0}%");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayVolcanoEruption(Vector3.zero, 0.7f);
            }
        }
        else
        {
            asparticAcid = Mathf.Min(targetPerAcid, asparticAcid + 20f);
            glutamicAcid = Mathf.Min(targetPerAcid, glutamicAcid + 18f);
        }

        OnPrebioticProgressUpdated?.Invoke();
    }

    /// <summary>
    /// Simule l'apport extraterrestre d'acides aminés via le bombardement météoritique (ex. Météorite de Murchison).
    /// Synthétise Sérine et Valine.
    /// </summary>
    public void TriggerMeteorBombardment()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentEpoch != PlanetEpoch.Prebiotic)
        {
            Debug.LogWarning("[PrebioticMiniGameController] Action indisponible : la planète n'est pas en période pré-biotique.");
            return;
        }

        if (GameManager.Instance != null)
        {
            float gain = 22f;
            serine = Mathf.Min(targetPerAcid, serine + gain);
            valine = Mathf.Min(targetPerAcid, valine + gain * 0.85f);

            GameManager.Instance.LogPlayerAction("Prebiotic Meteor Delivery", $"Apport météoritique prébiotique. Sérine: {serine:F0}%, Valine: {valine:F0}%");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMeteorImpact(Vector3.zero, 0.8f, 1.1f);
            }
        }
        else
        {
            serine = Mathf.Min(targetPerAcid, serine + 22f);
            valine = Mathf.Min(targetPerAcid, valine + 19f);
        }

        OnPrebioticProgressUpdated?.Invoke();
    }

    /// <summary>
    /// Simule l'impact du rayonnement UV solaire et du chauffage thermique.
    /// Synthétise Leucine et Isoleucine.
    /// </summary>
    public void TriggerUvCatalysis()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentEpoch != PlanetEpoch.Prebiotic)
        {
            Debug.LogWarning("[PrebioticMiniGameController] Action indisponible : la planète n'est pas en période pré-biotique.");
            return;
        }

        if (GameManager.Instance != null)
        {
            float gain = 25f;
            leucine = Mathf.Min(targetPerAcid, leucine + gain);
            isoleucine = Mathf.Min(targetPerAcid, isoleucine + gain * 0.8f);

            GameManager.Instance.LogPlayerAction("Prebiotic UV Catalysis", $"Catalyse UV solaire activée. Leucine: {leucine:F0}%, Isoleucine: {isoleucine:F0}%");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.Play2D(null, 0.5f);
            }
        }
        else
        {
            leucine = Mathf.Min(targetPerAcid, leucine + 25f);
            isoleucine = Mathf.Min(targetPerAcid, isoleucine + 20f);
        }

        OnPrebioticProgressUpdated?.Invoke();
    }
}
