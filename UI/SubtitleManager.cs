using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class SubtitleManager : Singleton<SubtitleManager>
    {
        public static SubtitleManager Instance
        {
            get
            {
                return ((SubtitleManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        [SerializeField]
        private SubtitlePanel subtitlePanel;
        private Coroutine m_openAPIRequest;

        public bool ToggleActive
        {
            get
            {
                return subtitlePanel.ToggleActive;
            }
        }

        public float ToggleAspect
        {
            get
            {
                return subtitlePanel.ToggleAspect;
            }
        }

        public string CurrentSubtitle
        {
            get
            {
                return subtitlePanel.CurrentSubtitle;
            }
        }

        public void Awake()
        {
            if(AppManager.IsCreated)
            {
                subtitlePanel.gameObject.SetActive(true);
            }
        }

        public void ToggleButtonVisibiliy(bool isOn)
        {
            if (AppManager.Instance.Settings.HUDSettings.useSubtitles)
            {
                subtitlePanel.ToggleButtonVisibiliy(isOn);
            }
        }

        public void PlayAudioTranscript(AudioClip clip, string transcript, System.Action onEndCallback = null, UnityEngine.Audio.AudioMixerGroup mixer = null)
        {
            if (subtitlePanel != null)
            {
                if (subtitlePanel.IsPlaying)
                {
                    Stop();
                }

                //need to check if the hint is open
                if (HUDManager.Instance.GetHUDMessageObject("HINT_MESSAGE").activeInHierarchy)
                {
                    PopupManager.instance.HideHint();
                }

                subtitlePanel.Open(clip, transcript, onEndCallback, mixer);
            }
        }

        public void PlayRawAudioClip(string absoluteAudioClipPath, bool getTranscript = true)
        {
            if (m_openAPIRequest != null)
            {
                StopCoroutine(m_openAPIRequest);
            }

            //need to use OpenAPI to get transcript
            m_openAPIRequest = OpenAiAPI.Instance.TranscriptRequest(absoluteAudioClipPath, OpenAPITranscriptCallback);
        }

        public void Stop()
        {
            subtitlePanel.Stop();
        }

        public void Pause(bool pause)
        {
            subtitlePanel.Pause(pause);
        }

        private void OpenAPITranscriptCallback(AudioClip clip, string transcript)
        {
            PlayAudioTranscript(clip, transcript);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SubtitleManager), true)]
        public class SubtitleManager_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                if (Application.productName.Equals("BL360 Plugin"))
                {
                    serializedObject.Update();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("subtitlePanel"), true);

                    if (GUI.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(this);
                    }
                }
            }
        }
#endif
    }
}
