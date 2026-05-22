using UnityEngine;

namespace MathsClass
{
    // Bruitages synthétisés par code + musique de fond depuis un fichier MP3.
    // Écoute les Settings pour les volumes.
    public class AudioManager : MonoBehaviour
    {
        public AudioSource sfx;
        public AudioSource music;

        // Musique de fond. Déposez votre MP3 dans Assets/Resources/Music/.
        // Si le champ est laissé vide, le 1er fichier audio trouvé dans ce
        // dossier est chargé automatiquement.
        public AudioClip musicTrack;

        AudioClip ding;
        AudioClip buzz;
        AudioClip tick;
        AudioClip startClip;
        AudioClip gameOverClip;
        AudioClip musicFallback; // pad procédural de secours si pas de MP3

        float musicVolume = 0.35f;
        float sfxVolume   = 1f;

        void Awake()
        {
            ding = MakeChord(new[] { 880f, 1320f }, 0.28f, 0.35f);
            buzz = MakeTone(140f, 0.35f, 0.4f, true);
            tick = MakeTone(1500f, 0.04f, 0.25f);
            startClip = MakeChord(new[] { 523f, 659f, 784f }, 0.4f, 0.3f);
            gameOverClip = MakeChord(new[] { 196f, 233f, 277f }, 1.1f, 0.35f);

            // Musique : MP3 déposé dans Resources/Music/, sinon pad de secours.
            if (musicTrack == null)
            {
                var clips = Resources.LoadAll<AudioClip>("Music");
                if (clips != null && clips.Length > 0) musicTrack = clips[0];
            }
            if (musicTrack == null)
            {
                Debug.LogWarning("[AudioManager] Aucun MP3 dans Assets/Resources/Music/ — pad procédural de secours utilisé.");
                musicFallback = MakePad(220f, 0.1f);
            }
        }

        void OnEnable()
        {
            ApplySettings(SaveManager.LoadSettings());
            SaveManager.OnSettingsChanged += ApplySettings;
        }

        void OnDisable()
        {
            SaveManager.OnSettingsChanged -= ApplySettings;
        }

        void ApplySettings(Settings s)
        {
            musicVolume = 0.22f * Mathf.Clamp01(s.musicVolume);
            sfxVolume   = Mathf.Clamp01(s.sfxVolume);
            if (music) music.volume = musicVolume;
            if (sfx) sfx.volume = sfxVolume;
        }

        public void PlayDing()     { if (sfx && ding) sfx.PlayOneShot(ding, sfxVolume); }
        public void PlayBuzz()     { if (sfx && buzz) sfx.PlayOneShot(buzz, sfxVolume); }
        public void PlayTick()     { if (sfx && tick) sfx.PlayOneShot(tick, 0.5f * sfxVolume); }
        public void PlayStart()    { if (sfx && startClip) sfx.PlayOneShot(startClip, sfxVolume); }
        public void PlayGameOver() { if (sfx && gameOverClip) sfx.PlayOneShot(gameOverClip, sfxVolume); }

        // tier 0 = silence (menu / game over) ; tier >= 1 = musique de fond.
        // La même piste MP3 joue en boucle pendant toute la partie.
        public void SetMusicTier(int tier)
        {
            if (!music) return;
            var clip = (tier <= 0) ? null : (musicTrack != null ? musicTrack : musicFallback);
            if (music.clip == clip)
            {
                if (clip != null && !music.isPlaying) music.Play();
                return;
            }
            music.Stop();
            music.clip = clip;
            music.loop = true;
            music.volume = musicVolume;
            if (clip != null) music.Play();
        }

        // ---------- Synthèse procédurale ----------
        static AudioClip MakeTone(float freq, float duration, float vol, bool square = false)
        {
            int sr = 44100;
            int len = Mathf.Max(1, Mathf.RoundToInt(sr * duration));
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)sr;
                float env = Mathf.Exp(-3.5f * t / duration);
                float v = square ? (Mathf.Sin(2 * Mathf.PI * freq * t) > 0 ? 1f : -1f)
                                 : Mathf.Sin(2 * Mathf.PI * freq * t);
                data[i] = v * env * vol;
            }
            var clip = AudioClip.Create("tone_" + freq, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip MakeChord(float[] freqs, float duration, float vol)
        {
            int sr = 44100;
            int len = Mathf.Max(1, Mathf.RoundToInt(sr * duration));
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)sr;
                float env = Mathf.Exp(-3f * t / duration);
                float v = 0;
                foreach (var f in freqs) v += Mathf.Sin(2 * Mathf.PI * f * t);
                v /= freqs.Length;
                data[i] = v * env * vol;
            }
            var clip = AudioClip.Create("chord", len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Pad musical doux : accord majeur en ondes sinusoïdales pures.
        // Boucle de 4 s sans raccord (fréquences ET swell -> cycles entiers).
        static AudioClip MakePad(float root, float vol)
        {
            int sr = 22050;
            const float duration = 4f;
            int len = Mathf.RoundToInt(sr * duration);
            var data = new float[len];

            float third = Mathf.Round(root * 1.25f); // tierce majeure
            float fifth = Mathf.Round(root * 1.5f);  // quinte
            float oct   = Mathf.Round(root * 2f);    // octave

            for (int i = 0; i < len; i++)
            {
                float t = i / (float)sr;
                float v = Mathf.Sin(2f * Mathf.PI * root  * t) * 0.50f
                        + Mathf.Sin(2f * Mathf.PI * third * t) * 0.26f
                        + Mathf.Sin(2f * Mathf.PI * fifth * t) * 0.26f
                        + Mathf.Sin(2f * Mathf.PI * oct   * t) * 0.14f;
                // respiration lente à 0.25 Hz = 1 cycle sur 4 s -> boucle propre
                float swell = 0.84f + 0.16f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
                data[i] = v * swell * vol;
            }
            var clip = AudioClip.Create("pad_" + root, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
