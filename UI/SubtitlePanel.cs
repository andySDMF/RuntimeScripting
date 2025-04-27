using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(AudioSource))]
    public class SubtitlePanel : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text script;
        [SerializeField]
        private AudioSource audiosource;
        [SerializeField]
        private Toggle toggleVisibility;
        [SerializeField]
        private Image background;
        [SerializeField]
        private float padding = 10.0f;
        [SerializeField]
        private float yOffset = 0.0f;
        [SerializeField]
        private bool adjustAnchoredfPositionOnMobile = false;

        private System.Action m_onEnd;
        private SubtitleTimingWrapper m_json;
        private SubtitleTiming m_currentTiming;
        private int m_index = 0;
        private bool m_paused = false;

        private Vector2 m_cacheTogglePosition;
        private RectTransform m_rectTToggle;

        public string CurrentSubtitle
        {
            get
            {
                return script.text;
            }
        }

        public bool IsPlaying
        {
            get
            {
                 return audiosource.isPlaying;
            }
        }

        public bool ToggleActive
        {
            get
            {
                return toggleVisibility.gameObject.activeInHierarchy;
            }
        }

        public float ToggleAspect
        {
            get
            {
                return m_rectTToggle.sizeDelta.y;
            }
        }


        private void Start()
        {
            if (AppManager.IsCreated)
            {
                background.transform.parent.GetComponent<CanvasGroup>().alpha = 0.0f;

                if (!AppManager.Instance.Settings.HUDSettings.useSubtitles)
                {
                    toggleVisibility.isOn = false;
                    background.transform.parent.GetComponent<CanvasGroup>().alpha = 0.0f;
                    toggleVisibility.gameObject.SetActive(false);
                }
                else
                {
                    toggleVisibility.isOn = AppManager.Instance.Settings.HUDSettings.startWithSubtiles;
                }

                if (AppManager.Instance.Data.IsMobile && adjustAnchoredfPositionOnMobile)
                {
                    transform.GetChild(0).GetComponent<RectTransform>().offsetMax = new Vector2(-300, 0);
                    transform.GetChild(0).GetComponent<RectTransform>().offsetMin = new Vector2(300, 0);
                    transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 150);
                }

                m_rectTToggle = toggleVisibility.GetComponent<RectTransform>();
                m_cacheTogglePosition = m_rectTToggle.anchoredPosition;
            }
        }

        private void OnDisable()
        {
            m_onEnd = null;
            m_json = null;
            background.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            background.transform.parent.GetComponent<CanvasGroup>().alpha = 0.0f;

            script.text = "";
        }

        private void OnEnable()
        {
            StartCoroutine(Wait());
        }

        private void Update()
        {
            if(AppManager.IsCreated)
            {
                if(AppManager.Instance.Data.IsMobile || AppManager.Instance.Settings.HUDSettings.useMobileToolsForDesktop)
                {
                    if(HUDManager.Instance.NavigationHUDVisibility)
                    {
                        m_rectTToggle.anchoredPosition = new Vector2(m_cacheTogglePosition.x, m_cacheTogglePosition.y + (MainHUDMenuPanel.Instance.MobileButton.sizeDelta.y + 10));

                    }
                    else
                    {
                        m_rectTToggle.anchoredPosition = new Vector2(m_cacheTogglePosition.x, m_cacheTogglePosition.y);
                    }
                }
            }
        }

        public void ToggleButtonVisibiliy(bool isOn)
        {
            if (AppManager.Instance.Settings.HUDSettings.useSubtitles)
            {
                toggleVisibility.gameObject.SetActive(isOn);
            }
        }

        private IEnumerator Wait()
        {
            yield return StartCoroutine(UpdateBKG());

            if (audiosource.clip != null)
            {
                audiosource.Play();
                StartCoroutine(ProcessCallback());
            }
        }

        private IEnumerator ProcessCallback()
        {
            while (audiosource.isPlaying)
            {
                if (m_paused) yield return null;

                if (m_json != null)
                {
                    if (m_index + 1 < m_json.timings.Count)
                    {
                        if (audiosource.time >= m_json.timings[m_index + 1].start)
                        {
                            m_currentTiming = m_json.timings[m_index + 1];
                            m_index++;

                            script.text = m_json.timings[m_index].text;
                            StartCoroutine(UpdateBKG());
                        }
                    }
                }

                yield return null;
            }

            if (m_json != null)
            {
                //on last one wait 1 second before closing
                yield return new WaitForSeconds(1.0f);

                if (script.text.Equals(m_json.timings[m_index].text))
                {
                    if (m_onEnd != null)
                    {
                        m_onEnd.Invoke();
                    }

                    background.transform.parent.GetComponent<CanvasGroup>().alpha = 0.0f;
                }
            }
            else
            {
                if (m_onEnd != null)
                {
                    m_onEnd.Invoke();
                }

                background.transform.parent.GetComponent<CanvasGroup>().alpha = 0.0f;
            }
        }

        private IEnumerator UpdateBKG()
        {
            yield return new WaitForEndOfFrame();
            Vector2 size = script.GetComponent<RectTransform>().sizeDelta;
            background.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x + padding * 2, size.y + padding * 2);
            background.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, 0.0f + yOffset);

        }

        public void Open(AudioClip clip, string transcript, System.Action onEndCallback = null, UnityEngine.Audio.AudioMixerGroup mixer = null)
        {
            m_json = null;
            m_index = 0;
            m_onEnd = onEndCallback;

            if (transcript[0].ToString().Equals("{"))
            {
                m_json = JsonUtility.FromJson<SubtitleTimingWrapper>(transcript);
            }

            m_paused = false;
            audiosource.clip = clip;
            audiosource.outputAudioMixerGroup = mixer;

            if (script is TMP_Text)
            {
                if (m_json != null)
                {
                    m_currentTiming = m_json.timings[m_index];
                    script.text = m_json.timings[m_index].text;
                }
                else
                {
                    script.text = transcript;
                }
            }

            background.transform.parent.GetComponent<CanvasGroup>().alpha = 1.0f;
        }

        public void Stop()
        {
            StopAllCoroutines();

            audiosource.Stop();

            m_paused = false;
            m_json = null;
            m_onEnd = null;
            background.transform.parent.GetComponent<CanvasGroup>().alpha = 0.0f;
            audiosource.outputAudioMixerGroup = null;
        }

        public void Pause(bool pause)
        {
            m_paused = pause;

            if (audiosource.enabled)
            {
                if (pause)
                {
                    audiosource.Pause();
                }
                else
                {
                    audiosource.Play();
                }
            }
        }

        [System.Serializable]
        public class SubtitleTimingWrapper
        {
            public List<SubtitleTiming> timings;
        }

        [System.Serializable]
        public class SubtitleTiming
        {
            public float start;
            public float end;
            public string text;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SubtitlePanel), true)]
        public class SubtitlePanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.LabelField("Style", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("padding"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("yOffset"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("adjustAnchoredfPositionOnMobile"), true);

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Components", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("script"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("audiosource"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("toggleVisibility"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("background"), true);

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
