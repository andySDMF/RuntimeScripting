using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(AudioSource))]
    public class Music : MonoBehaviour
    {
        [SerializeField]
        private AudioClip clip;

        [SerializeField]
        private float maxVolume = 1.0f;

        [SerializeField]
        private bool loop = true;

        [SerializeField]
        private bool playOnStart = true;

        private AudioSource m_audio;
        private Coroutine m_process;

        public AudioSource Audio
        {
            get
            {
                return m_audio;
            }
        }

        private void Awake()
        {
            m_audio = GetComponent<AudioSource>();
            m_audio.clip = clip;
            m_audio.loop = loop;
        }

        private void Start()
        {
            m_audio.Stop();

            if(playOnStart)
            {
                StartCoroutine(Wait());
            }
        }

        private IEnumerator Wait()
        {
            while(PlayerManager.Instance.GetLocalPlayer() == null)
            {
                yield return null;
            }

            m_audio.volume = 0.0f;

            m_audio.Play();

            Blend(AudioBlendDirection._In, maxVolume);
        }

        public void Play()
        {
            if(!m_audio.isPlaying)
            {
                m_audio.Play();
            }
        }

        public void Stop()
        {
            if (m_audio.isPlaying)
            {
                m_audio.Stop();
            }
        }

        public void Pause(bool pause)
        {
            if(pause)
            {
                m_audio.Pause();
            }
            else
            {
                m_audio.Play();
            }
        }

        public void Mute(bool mute)
        {
            m_audio.mute = mute;
        }

        public void SetVolume(float volume, bool blend = false)
        {
            if(blend)
            {
                if (m_audio.volume > volume)
                {
                    Blend(AudioBlendDirection._Out, volume);
                }
                else
                {
                    Blend(AudioBlendDirection._In, volume);
                }
            }
            else
            {
                m_audio.volume = volume;
            }
        }

        public void Blend(AudioBlendDirection dir, float volume)
        {
            if(m_process != null)
            {
                StopCoroutine(m_process);
            }

            m_process = StartCoroutine(BlendDirection(dir, volume));
        }

        private IEnumerator BlendDirection(AudioBlendDirection dir, float volume)
        {
            if(dir.Equals(AudioBlendDirection._In))
            {
                while (m_audio.volume < volume)
                {
                    m_audio.volume += 0.01f;
                    yield return null;
                }

                m_audio.volume = volume;
            }
            else
            {
                while (m_audio.volume > volume)
                {
                    m_audio.volume -= 0.01f;
                    yield return null;
                }

                m_audio.volume = volume;
            }
        }

        public enum AudioBlendDirection { _In, _Out }

#if UNITY_EDITOR
        [CustomEditor(typeof(Music), true)]
        public class Music_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("clip"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxVolume"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("loop"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnStart"), true);

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}
