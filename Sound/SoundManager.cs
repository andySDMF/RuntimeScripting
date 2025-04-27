using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundManager : Singleton<SoundManager>
    {
        public static SoundManager Instance
        {
            get
            {
                return ((SoundManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private AudioSource Audio;
        private Music[] m_enironmentalMusic;


        private void Start()
        {
            Audio = this.GetComponent<AudioSource>();

            m_enironmentalMusic = FindObjectsByType<Music>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        public void PlaySound(AudioClip clip)
        {
            if(Audio.isPlaying)
            {
                Audio.Stop();
            }

            if(clip != null)
            {
                Audio.clip = clip;
                Audio.Play();
            }
        }

        public void Stop(bool removeClip = true)
        {
            if(Audio.isPlaying)
            {
                Audio.Stop();
            }

            if(removeClip)
            {
                Audio.clip = null;
            }
        }

        public void Play()
        {
            if(Audio.clip != null)
            {
                Audio.Play();
            }
        }

        public void Pause(bool pause)
        {
            if(Audio.isPlaying)
            {
                if(pause)
                {
                    Audio.Pause();
                }
                else
                {
                    Audio.UnPause();
                }
            }
            else
            {
                if(!pause)
                {
                    Play();
                }
            }
        }

        public void ToggleEnvironmentalMusic(bool on)
        {
            for(int i = 0; i < m_enironmentalMusic.Length; i++)
            {
                m_enironmentalMusic[i].Mute(!on);
            }

            AppManager.Instance.Data.EnvironmentalSoundOn = on;
        }

        public void SetEnvironmentalMusicVolume(float volume)
        {
            for (int i = 0; i < m_enironmentalMusic.Length; i++)
            {
                m_enironmentalMusic[i].SetVolume(volume);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SoundManager), true)]
        public class SoundManager_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
            }
        }
#endif
    }
}