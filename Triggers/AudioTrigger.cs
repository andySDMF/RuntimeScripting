using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(SubtitleAudioConverter))]
    [RequireComponent(typeof(AudioSource))]
    public class AudioTrigger : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField]
        protected AudioClip clip;
        [SerializeField]
        protected bool useSubtitleConverterInstead = false;

        [Header("Action")]
        [SerializeField]
        protected bool playIfHoldingItem = true;
        [SerializeField]
        protected bool stopSubtitles = true;
        [SerializeField]
        protected bool hideMeshOnStart = true;

        protected AudioSource m_audio;
        protected SubtitleAudioConverter m_subtitleConverter;

        private void Awake()
        {
            m_audio = GetComponent<AudioSource>();
            m_audio.playOnAwake = false;
            m_audio.clip = clip;

            m_subtitleConverter = GetComponent<SubtitleAudioConverter>();
            m_subtitleConverter.PlayOnEnable = false;

            GetComponent<Renderer>().enabled = hideMeshOnStart;
        }

        private void OnDisable()
        {
            if (!useSubtitleConverterInstead)
            {
                m_audio.Stop();
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            Trigger(other);
        }

        protected virtual void Trigger(Collider other)
        {
            if (other.transform.Equals(PlayerManager.Instance.GetLocalPlayer().TransformObject))
            {
                if (!playIfHoldingItem)
                {
                    if (ItemManager.Instance.IsHolding) return;
                }

                if (stopSubtitles)
                {
                    SubtitleManager.Instance.Stop();
                }

                if(useSubtitleConverterInstead)
                {
                    m_subtitleConverter.Convert();
                }
                else
                {
                    m_audio.Play();
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AudioTrigger), true)]
        public class AudioTrigger_Editor : BaseInspectorEditor
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
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useSubtitleConverterInstead"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("playIfHoldingItem"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stopSubtitles"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hideMeshOnStart"), true);

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
