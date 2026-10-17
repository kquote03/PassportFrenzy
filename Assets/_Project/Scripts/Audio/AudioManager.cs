using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// Named sound hooks. Every call is safe with no clip assigned (silent placeholder).
    /// Fill clips in Data/SoundLibrary.asset; per-country music via CountryDefinition.music or "music_&lt;id&gt;".
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioSource musicSource;

        SoundLibrary Lib => GameRoot.I.sounds;

        public void Play(string id)
        {
            var e = Lib != null ? Lib.Find(id) : null;
            if (e == null || e.clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(e.clip, e.volume);
        }

        public void PlayMusic(CountryDefinition c)
        {
            if (musicSource == null || c == null) return;
            AudioClip clip = c.music;
            float volume = 1f;
            if (clip == null && Lib != null)
            {
                var e = Lib.Find(SoundIds.MusicPrefix + c.id);
                if (e != null) { clip = e.clip; volume = e.volume; }
            }
            if (clip == null) { musicSource.Stop(); musicSource.clip = null; return; }
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.volume = volume;
            musicSource.loop = true;
            musicSource.Play();
        }
    }
}
