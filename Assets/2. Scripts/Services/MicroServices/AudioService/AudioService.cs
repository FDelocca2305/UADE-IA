using System;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace Services.MicroServices.AudioService
{
    [DefaultExecutionOrder(-10000)]
    public class AudioService : MonoBehaviour
    {
        const string MASTER = "MasterVolume", MUSIC = "MusicVolume", SFX = "SFXVolume";
        GameObject m_root;

        [SerializeField] private AudioConfig config;
        
        public static AudioService Instance { get; private set; }
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource m_musicSource;
        [SerializeField] private AudioSource m_sfxSource;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (!m_musicSource)
            {
                var go = new GameObject("~MusicSource");
                DontDestroyOnLoad(go);
                m_musicSource = go.AddComponent<AudioSource>();
                m_musicSource.playOnAwake = false;
                m_musicSource.loop = true;
                m_musicSource.spatialBlend = 0f;
            }
            else m_musicSource.spatialBlend = 0f;

            if (!m_sfxSource)
            {
                var go = new GameObject("~SFXSource");
                DontDestroyOnLoad(go);
                m_sfxSource = go.AddComponent<AudioSource>();
                m_sfxSource.playOnAwake = false;
                m_sfxSource.spatialBlend = 0f;
            }
            else m_sfxSource.spatialBlend = 0f;
        }

        public void SetAudioSources(AudioSource music, AudioSource sfx)
        {
            if (music)  { Object.DontDestroyOnLoad(music.gameObject);  m_musicSource = music;  m_musicSource.spatialBlend = 0f; }
            if (sfx)    { Object.DontDestroyOnLoad(sfx.gameObject);    m_sfxSource   = sfx;    m_sfxSource.spatialBlend   = 0f; }
        }

        public AudioConfig GetConfig() => config;
        public void SetConfig(AudioConfig p_newConfig) => config = p_newConfig;

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (!clip) return;
            if (m_musicSource.clip == clip && m_musicSource.isPlaying) return;
            m_musicSource.clip = clip;
            m_musicSource.loop = loop;
            m_musicSource.Play();
        }

        public void StopMusic()
        {
            if (m_musicSource != null)
                m_musicSource.Stop();
        }

        public void PauseMusic()
        {
            if (m_musicSource != null)
                m_musicSource.Pause();
        }

        public void ResumeMusic()
        {
            if (m_musicSource != null)
                m_musicSource.UnPause();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (!clip) return;
            if (!clip.preloadAudioData) clip.LoadAudioData();
            m_sfxSource.PlayOneShot(clip);
        }

        public bool IsMusicPlaying()
        {
            return m_musicSource != null && m_musicSource.isPlaying;
        }

        public void SetMusicVolume(float p_volume)
        {
            if (config?.audioMixer == null)
            {
                if (m_musicSource != null)
                    m_musicSource.volume = Mathf.Clamp01(p_volume);
                return;
            }

            SetMixerVolume(MUSIC, p_volume);
        }

        public void SetSFXVolume(float p_volume)
        {
            if (config?.audioMixer == null)
            {
                if (m_sfxSource != null)
                    m_sfxSource.volume = Mathf.Clamp01(p_volume);
                return;
            }

            SetMixerVolume(SFX, p_volume);
        }

        public void SetMasterVolume(float p_volume)
        {
            if (config?.audioMixer == null) return;
            SetMixerVolume(MASTER, p_volume);
        }

        private void SetMixerVolume(string p_parameterName, float p_normalizedVolume)
        {
            if (config?.audioMixer == null) return;

            float volume = Mathf.Clamp01(p_normalizedVolume);
            float db = volume > 0.0001f ? 20f * Mathf.Log10(volume) : -80f;
            
            config.audioMixer.SetFloat(p_parameterName, db);
        }
    }
}