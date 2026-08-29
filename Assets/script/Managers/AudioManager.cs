using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Volume Settings")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.1f;

    [Header("Audio Clips - SFX")]
    [SerializeField] private AudioClip meteorFlightClip;
    [SerializeField] private AudioClip meteorImpactClip;
    [SerializeField] private AudioClip volcanoEruptionClip;
    [SerializeField] private AudioClip volcanicExplosionClip;
    [SerializeField] private AudioClip earthquakeRumbleClip;
    [SerializeField] private AudioClip buttonClickClip;

    [Header("Audio Clips - Music")]
    [SerializeField] private AudioClip musicHadeanClip;
    [SerializeField] private AudioClip musicVolcanicClip;
    [SerializeField] private AudioClip musicOceanClip;

    public float MusicVolume
    {
        get => musicVolume;
        set => SetMusicVolume(value);
    }

    public float SfxVolume
    {
        get => sfxVolume;
        set => SetSfxVolume(value);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioSources();
        LoadDefaultClips();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEpochChanged += HandleEpochChanged;
            PlayMusicForEpoch(GameManager.Instance.CurrentEpoch);
        }
        else
        {
            PlayMusicForEpoch(PlanetEpoch.Hadean);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEpochChanged -= HandleEpochChanged;
        }
    }

    private void InitializeAudioSources()
    {
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        musicSource.ignoreListenerPause = true;
        sfxSource.ignoreListenerPause = true;

        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
    }

    private void LoadDefaultClips()
    {
        if (meteorFlightClip == null) meteorFlightClip = Resources.Load<AudioClip>("Audio/meteor_flight");
        if (meteorImpactClip == null) meteorImpactClip = Resources.Load<AudioClip>("Audio/meteor_impact");
        if (volcanoEruptionClip == null) volcanoEruptionClip = Resources.Load<AudioClip>("Audio/volcano_eruption");
        if (volcanicExplosionClip == null) volcanicExplosionClip = Resources.Load<AudioClip>("Audio/volcanic_explosion");
        if (earthquakeRumbleClip == null) earthquakeRumbleClip = Resources.Load<AudioClip>("Audio/earthquake_rumble");

        if (musicHadeanClip == null) musicHadeanClip = Resources.Load<AudioClip>("Audio/music_hadean");
        if (musicVolcanicClip == null) musicVolcanicClip = Resources.Load<AudioClip>("Audio/music_volcanic");
        if (musicOceanClip == null) musicOceanClip = Resources.Load<AudioClip>("Audio/music_ocean");

        // Procedural fallbacks if clips are still missing
        if (buttonClickClip == null) buttonClickClip = CreateToneClip("ButtonClick", 880f, 0.05f, 0.3f);
        if (meteorFlightClip == null) meteorFlightClip = CreateNoiseClip("MeteorFlight", 1.5f, 0.2f);
        if (meteorImpactClip == null) meteorImpactClip = CreateNoiseClip("MeteorImpact", 0.8f, 0.6f);
        if (volcanoEruptionClip == null) volcanoEruptionClip = CreateNoiseClip("VolcanoEruption", 1.2f, 0.4f);
        if (volcanicExplosionClip == null) volcanicExplosionClip = CreateNoiseClip("VolcanicExplosion", 0.6f, 0.7f);
        if (earthquakeRumbleClip == null) earthquakeRumbleClip = CreateNoiseClip("EarthquakeRumble", 2.0f, 0.5f);

        if (musicHadeanClip == null) musicHadeanClip = CreateToneClip("MusicHadean", 220f, 4.0f, 0.2f);
        if (musicVolcanicClip == null) musicVolcanicClip = CreateToneClip("MusicVolcanic", 293.66f, 4.0f, 0.2f);
        if (musicOceanClip == null) musicOceanClip = CreateToneClip("MusicOcean", 329.63f, 4.0f, 0.2f);
    }

    private AudioClip CreateToneClip(string name, float frequency, float lengthSeconds, float volumeScale)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * lengthSeconds);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * (t / lengthSeconds));
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volumeScale;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateNoiseClip(string name, float lengthSeconds, float volumeScale)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * lengthSeconds);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - (t / lengthSeconds);
            samples[i] = (Random.value * 2f - 1f) * envelope * volumeScale;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void HandleEpochChanged(PlanetEpoch newEpoch)
    {
        PlayMusicForEpoch(newEpoch);
    }

    public void PlayMusicForEpoch(PlanetEpoch epoch)
    {
        AudioClip targetClip = epoch switch
        {
            PlanetEpoch.Hadean => musicHadeanClip,
            PlanetEpoch.CrustFormation => musicHadeanClip,
            PlanetEpoch.VolcanicAge => musicVolcanicClip,
            PlanetEpoch.ProtoOcean => musicOceanClip,
            PlanetEpoch.TectonicDrift => musicOceanClip,
            PlanetEpoch.Prebiotic => musicOceanClip,
            PlanetEpoch.Photosynthesis => musicOceanClip,
            PlanetEpoch.CambrianExplosion => musicOceanClip,
            _ => musicHadeanClip
        };

        if (targetClip != null)
        {
            PlayMusic(targetClip);
        }
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null || clip == null) return;

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            if (musicSource.volume < musicVolume * 0.1f)
            {
                musicSource.volume = musicVolume;
            }
            return;
        }

        StartCoroutine(CrossfadeMusic(clip, loop, 1.5f));
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip, bool loop, float fadeDuration)
    {
        float startVolume = musicSource.volume;

        if (musicSource.isPlaying && startVolume > 0.01f)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                yield return null;
            }
        }

        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.Play();

        float fadeUpElapsed = 0f;
        while (fadeUpElapsed < fadeDuration)
        {
            fadeUpElapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, fadeUpElapsed / fadeDuration);
            yield return null;
        }

        musicSource.volume = musicVolume;
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    // --- SFX METHODS ---

    /// <summary>
    /// Attaches a 3D looping sound source to a meteor during flight.
    /// </summary>
    public AudioSource PlayMeteorFlight(GameObject meteorObj)
    {
        if (meteorObj == null || meteorFlightClip == null) return null;

        AudioSource audioSource = meteorObj.AddComponent<AudioSource>();
        audioSource.clip = meteorFlightClip;
        audioSource.loop = true;
        audioSource.spatialBlend = 1.0f; // 3D sound
        audioSource.minDistance = 10f;
        audioSource.maxDistance = 300f;
        audioSource.volume = sfxVolume * 0.7f;
        audioSource.Play();

        return audioSource;
    }

    /// <summary>
    /// Plays meteor impact sound at a given world position.
    /// </summary>
    public void PlayMeteorImpact(Vector3 position, float volumeScale = 1f, float pitch = 1f)
    {
        if (meteorImpactClip == null) return;
        Play3D(meteorImpactClip, position, volumeScale, pitch);
    }

    /// <summary>
    /// Plays volcanic eruption sound effect.
    /// </summary>
    public void PlayVolcanoEruption(Vector3 position, float volumeScale = 1f)
    {
        if (volcanoEruptionClip == null) return;
        Play3D(volcanoEruptionClip, position, volumeScale, Random.Range(0.85f, 1.15f));
    }

    /// <summary>
    /// Plays volcanic explosion sound effect.
    /// </summary>
    public void PlayVolcanicExplosion(Vector3 position, float volumeScale = 1f, float pitch = 1f)
    {
        if (volcanicExplosionClip == null) return;
        Play3D(volcanicExplosionClip, position, volumeScale, pitch);
    }

    /// <summary>
    /// Plays earthquake rumble sound effect.
    /// </summary>
    public void PlayEarthquakeRumble(Vector3 position, float volumeScale = 1f)
    {
        if (earthquakeRumbleClip == null) return;
        Play3D(earthquakeRumbleClip, position, volumeScale, Random.Range(0.9f, 1.1f));
    }

    /// <summary>
    /// Plays a 3D spatialized sound effect at a given position.
    /// </summary>
    public void Play3D(AudioClip clip, Vector3 position, float volumeMultiplier = 1f, float pitch = 1f, float minDistance = 5f, float maxDistance = 250f)
    {
        if (clip == null) return;

        GameObject tempAudioGo = new GameObject($"SFX_3D_{clip.name}");
        tempAudioGo.transform.position = position;

        AudioSource audioSource = tempAudioGo.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = Mathf.Clamp01(sfxVolume * volumeMultiplier);
        audioSource.pitch = pitch;
        audioSource.spatialBlend = 1.0f; // 3D sound
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.Play();

        Destroy(tempAudioGo, clip.length / Mathf.Max(0.1f, pitch) + 0.2f);
    }

    /// <summary>
    /// Plays UI button click feedback sound effect.
    /// </summary>
    public void PlayButtonClick()
    {
        if (buttonClickClip != null)
        {
            Play2D(buttonClickClip, 0.6f, 1.0f);
        }
    }

    /// <summary>
    /// Plays a 2D sound effect.
    /// </summary>
    public void Play2D(AudioClip clip, float volumeMultiplier = 1f, float pitch = 1f)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * volumeMultiplier));
    }
}
