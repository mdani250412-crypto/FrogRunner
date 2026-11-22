using UnityEngine;

namespace FrogRunner.Audio
{
    /// <summary>
    /// Centralized audio: procedural retro fallback clips, optional Inspector overrides,
    /// and persisted Music/SFX volume + mute settings.
    /// TUNING: adjust clip generation frequencies/durations for different retro feel.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Optional overrides (drop .wav/.mp3 here)")]
        public AudioClip musicClip;
        public AudioClip hopClip;
        public AudioClip dashClip;
        public AudioClip powerUpClip;
        public AudioClip shieldBlockClip;
        public AudioClip splashClip;
        public AudioClip carHornClip;
        public AudioClip crashClip;
        public AudioClip uiClickClip;

        [Header("Volumes")]
        [Range(0f, 1f)] public float musicVolume = 0.55f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        public bool muted;

        AudioSource _music;
        AudioSource _sfx;

        const string MusicKey = "FR_MusicVol";
        const string SfxKey = "FR_SfxVol";
        const string MuteKey = "FR_Mute";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Load();

            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;
            _music.spatialBlend = 0f;

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.loop = false;
            _sfx.playOnAwake = false;
            _sfx.spatialBlend = 0f;

            EnsureClips();
            ApplyVolumes();
            if (musicClip != null)
            {
                _music.clip = musicClip;
                _music.Play();
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void EnsureClips()
        {
            if (musicClip == null) musicClip = ProceduralAudio.CreateMusicLoop();
            if (hopClip == null) hopClip = ProceduralAudio.CreateHop();
            if (dashClip == null) dashClip = ProceduralAudio.CreateDash();
            if (powerUpClip == null) powerUpClip = ProceduralAudio.CreatePowerUp();
            if (shieldBlockClip == null) shieldBlockClip = ProceduralAudio.CreateShieldBlock();
            if (splashClip == null) splashClip = ProceduralAudio.CreateSplash();
            if (carHornClip == null) carHornClip = ProceduralAudio.CreateHorn();
            if (crashClip == null) crashClip = ProceduralAudio.CreateCrash();
            if (uiClickClip == null) uiClickClip = ProceduralAudio.CreateUiClick();
        }

        public void ApplyVolumes()
        {
            float m = muted ? 0f : musicVolume;
            float s = muted ? 0f : sfxVolume;
            if (_music != null) _music.volume = m;
            if (_sfx != null) _sfx.volume = s;
        }

        public void SetMusicVolume(float v) { musicVolume = Mathf.Clamp01(v); ApplyVolumes(); Save(); }
        public void SetSfxVolume(float v) { sfxVolume = Mathf.Clamp01(v); ApplyVolumes(); Save(); }
        public void ToggleMute() { muted = !muted; ApplyVolumes(); Save(); }

        public void PlayHop() => PlaySfx(hopClip, 0.9f);
        public void PlayDash() => PlaySfx(dashClip, 0.9f);
        public void PlayPowerUp() => PlaySfx(powerUpClip, 1f);
        public void PlayShieldBlock() => PlaySfx(shieldBlockClip, 1f);
        public void PlaySplash() => PlaySfx(splashClip, 1f);
        public void PlayHorn() => PlaySfx(carHornClip, 0.55f);
        public void PlayCrash() => PlaySfx(crashClip, 1f);
        public void PlayUiClick() => PlaySfx(uiClickClip, 0.7f);

        void PlaySfx(AudioClip clip, float scale)
        {
            if (clip == null || _sfx == null) return;
            float vol = muted ? 0f : sfxVolume * scale;
            if (vol <= 0.001f) return;
            _sfx.PlayOneShot(clip, vol);
        }

        void Load()
        {
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, musicVolume));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, sfxVolume));
            muted = PlayerPrefs.GetInt(MuteKey, muted ? 1 : 0) == 1;
        }

        void Save()
        {
            PlayerPrefs.SetFloat(MusicKey, musicVolume);
            PlayerPrefs.SetFloat(SfxKey, sfxVolume);
            PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
