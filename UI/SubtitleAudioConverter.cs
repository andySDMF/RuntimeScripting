using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class SubtitleAudioConverter : MonoBehaviour
    {
        [SerializeField]
        private bool playOnEnable = false;

        [SerializeField]
        private AudioClip clip;

        [SerializeField]
        private UnityEngine.Audio.AudioMixerGroup mixer;

        [SerializeField]
        private TextAsset transcript;

        public bool PlayOnEnable
        {
            get
            {
                return playOnEnable;
            }
            set
            {
                playOnEnable = value;
            }
        }

        private void OnEnable()
        {
            if(playOnEnable)
            {
                Convert();
            }
        }

        public void Convert()
        {
            SubtitleManager.Instance.PlayAudioTranscript(clip, transcript.text, null, mixer);

            AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, "Played Subtitle");
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SubtitleAudioConverter), true)]
        public class SubtitleAudioConverter_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnEnable"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("clip"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("mixer"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("transcript"), true);

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
