using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Mobile2DContentScaler : MonoBehaviour
    {
        [SerializeField]
        private ContentType controlType = ContentType._Image;

        [SerializeField]
        private RectTransform videoScrubber;

        private float m_layoutWidth;
        private RectTransform m_mainLayout;

        private float m_durationFontSize;
        private float m_scrubberSize;

        private void Awake()
        {
            m_mainLayout = transform.GetChild(0).GetComponent<RectTransform>();
            m_layoutWidth = m_mainLayout.sizeDelta.x;

            if(videoScrubber != null)
            {
                m_durationFontSize = videoScrubber.GetComponentInChildren<TextMeshProUGUI>().fontSize;
                m_scrubberSize = videoScrubber.GetComponentInChildren<Slider>().handleRect.sizeDelta.x;
            }
           
        }

        private void OnEnable()
        {
            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            SubtitleManager.Instance.ToggleButtonVisibiliy(false);
        }

        private void OnDisable()
        {
            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;

            SubtitleManager.Instance.ToggleButtonVisibiliy(true);
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    m_mainLayout.anchorMin = new Vector2(0.5f, 0.0f);
                    m_mainLayout.anchorMax = new Vector2(0.5f, 1.0f);
                    m_mainLayout.anchoredPosition = Vector2.zero;
                    m_mainLayout.sizeDelta = new Vector2(m_layoutWidth, m_mainLayout.sizeDelta.y);

                    if(controlType.Equals(ContentType._Video))
                    {
                        foreach(TextMeshProUGUI txt in videoScrubber.GetComponentsInChildren<TextMeshProUGUI>())
                        {
                            txt.fontSize = m_durationFontSize;
                        }

                        videoScrubber.GetComponentInChildren<Slider>().GetComponent<RectTransform>().sizeDelta = new Vector2(0, m_scrubberSize);
                        videoScrubber.GetComponentInChildren<Slider>().handleRect.sizeDelta = new Vector2(m_scrubberSize, 0);
                    }
                }
                else
                {
                    float aspect = arg2 / arg1;

                    m_mainLayout.anchorMin = new Vector2(0f, 0.0f);
                    m_mainLayout.anchorMax = new Vector2(1f, 1.0f);
                    m_mainLayout.offsetMax = new Vector2(-50, m_mainLayout.offsetMax.y);
                    m_mainLayout.offsetMin = new Vector2(50, m_mainLayout.offsetMin.y);

                    m_mainLayout.anchoredPosition = Vector2.zero;

                    if (controlType.Equals(ContentType._Video))
                    {
                        foreach (TextMeshProUGUI txt in videoScrubber.GetComponentsInChildren<TextMeshProUGUI>())
                        {
                            txt.fontSize = m_durationFontSize * aspect;
                        }

                        videoScrubber.GetComponentInChildren<Slider>().GetComponent<RectTransform>().sizeDelta = new Vector2(0, m_scrubberSize * aspect);
                        videoScrubber.GetComponentInChildren<Slider>().handleRect.sizeDelta = new Vector2(m_scrubberSize * aspect, 0);
                    }
                }
            }
        }


        [System.Serializable]
        private enum ContentType { _Image, _Video }

#if UNITY_EDITOR
        [CustomEditor(typeof(Mobile2DContentScaler), true)]
        public class Mobile2DContentScaler_Editor : BaseInspectorEditor
        {
            private Mobile2DContentScaler m_script;

            private void OnEnable()
            {
                m_script = (Mobile2DContentScaler)target;
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                if (Application.productName.Equals("BL360 Plugin"))
                {
                    serializedObject.Update();

                    EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("controlType"), true);

                    if (m_script.controlType.Equals(ContentType._Video))
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("videoScrubber"), true);
                    }

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
