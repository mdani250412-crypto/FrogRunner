using UnityEngine;

namespace FrogRunner.Audio
{
    /// <summary>
    /// Generates simple chiptune/synth AudioClips at runtime so the game has audio
    /// with zero external assets. Clips are small and cached by AudioManager.
    /// </summary>
    public static class ProceduralAudio
    {
        const int SampleRate = 22050;

        public static AudioClip CreateHop() => Make("SFX_Hop", 0.16f, (t, i) =>
        {
            float f = Mathf.Lerp(320f, 740f, t / 0.16f);
            float env = 1f - t / 0.16f;
            return Square(f, i) * env * 0.35f;
        });

        public static AudioClip CreateDash() => Make("SFX_Dash", 0.22f, (t, i) =>
        {
            float f = Mathf.Lerp(180f, 520f, Mathf.Pow(t / 0.22f, 0.5f));
            float env = 1f - t / 0.22f;
            return (Square(f, i) * 0.7f + Noise(i) * 0.2f) * env * 0.35f;
        });

        public static AudioClip CreatePowerUp() => Make("SFX_PowerUp", 0.45f, (t, i) =>
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            int idx = Mathf.Clamp(Mathf.FloorToInt(t / 0.11f), 0, notes.Length - 1);
            float env = 1f - (t % 0.11f) / 0.11f * 0.5f;
            return Square(notes[idx], i) * env * 0.28f;
        });

        public static AudioClip CreateShieldBlock() => Make("SFX_Shield", 0.28f, (t, i) =>
        {
            float f = 220f + Mathf.Sin(t * 55f) * 90f;
            float env = 1f - t / 0.28f;
            return (Square(f, i) * 0.5f + Triangle(f * 2f, i) * 0.3f) * env * 0.32f;
        });

        public static AudioClip CreateSplash() => Make("SFX_Splash", 0.45f, (t, i) =>
        {
            float env = Mathf.Exp(-t * 7f);
            float tone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(220f, 70f, t / 0.45f) * t) * 0.25f;
            return (Noise(i) * 0.75f + tone) * env * 0.4f;
        });

        public static AudioClip CreateHorn() => Make("SFX_Horn", 0.22f, (t, i) =>
        {
            float env = t < 0.02f ? t / 0.02f : Mathf.Clamp01(1f - (t - 0.16f) / 0.06f);
            return (Square(330f, i) * 0.5f + Square(415f, i) * 0.4f) * env * 0.18f;
        });

        public static AudioClip CreateCrash() => Make("SFX_Crash", 0.55f, (t, i) =>
        {
            float env = Mathf.Exp(-t * 5f);
            float low = Square(Mathf.Lerp(160f, 40f, t / 0.55f), i) * 0.35f;
            return (Noise(i) * 0.7f + low) * env * 0.45f;
        });

        public static AudioClip CreateUiClick() => Make("SFX_UIClick", 0.07f, (t, i) =>
        {
            float env = 1f - t / 0.07f;
            return Square(880f, i) * env * 0.15f;
        });

        public static AudioClip CreateMusicLoop()
        {
            // Simple upbeat 8-bit loop over a I-V-vi-IV-ish progression.
            float[] scale = { 261.63f, 293.66f, 329.63f, 392.00f, 440.00f, 523.25f };
            int[] melody = { 0,2,4,5,4,2,0,2, 3,4,5,4,3,2,1,2, 4,5,4,2,4,5,4,2, 0,2,4,2,3,2,1,0 };
            float step = 0.16f;
            float length = step * melody.Length;
            return Make("MUS_Loop", length, (t, i) =>
            {
                int idx = Mathf.FloorToInt(t / step) % melody.Length;
                float local = (t % step) / step;
                float env = local < 0.08f ? local / 0.08f : 1f - (local - 0.08f) / 0.92f * 0.75f;
                float freq = scale[melody[idx]];
                float bassFreq = scale[melody[idx / 4 * 4 % scale.Length]] * 0.5f;
                float lead = Square(freq, i) * 0.16f;
                float bass = Triangle(bassFreq, i) * 0.12f;
                float drum = (idx % 4 == 0 ? Noise(i) * Mathf.Exp(-local * 18f) * 0.08f : 0f);
                return (lead + bass + drum) * env;
            });
        }

        static AudioClip Make(string name, float lengthSeconds, System.Func<float, int, float> sample)
        {
            int samples = Mathf.Max(1, Mathf.CeilToInt(SampleRate * lengthSeconds));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(sample(t, i), -1f, 1f);
            }
            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Square(float freq, int i)
        {
            float phase = Mathf.Repeat(i * freq / SampleRate, 1f);
            return phase < 0.5f ? 0.8f : -0.8f;
        }

        static float Triangle(float freq, int i)
        {
            float phase = Mathf.Repeat(i * freq / SampleRate, 1f);
            return 4f * Mathf.Abs(phase - 0.5f) - 1f;
        }

        static float Noise(int i)
        {
            // Deterministic-ish LCG per sample index for stable clip generation.
            uint x = (uint)(i * 1103515245 + 12345);
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            return (x / (float)uint.MaxValue) * 2f - 1f;
        }
    }
}
