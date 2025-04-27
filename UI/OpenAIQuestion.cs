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
    public class OpenAIQuestion : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField questionInput;

        [Header("Mobile Layout")]
        [SerializeField]
        private RectTransform mainLayout;
        [SerializeField]
        private TextMeshProUGUI titleText;

        private float m_titleFontSize;
        private float m_layoutWidth;
        private float m_inputFontSize;

        private System.Action<string> m_callback;

        private void Awake()
        {
            m_titleFontSize = titleText.fontSize;
            m_layoutWidth = mainLayout.sizeDelta.x;

            m_inputFontSize = questionInput.gameObject.GetComponentInParent<TMP_InputField>(true).textComponent.fontSize;
        }

        private void OnEnable()
        {
            questionInput.text = "";
            questionInput.Select();

            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            SubtitleManager.Instance.Stop();
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
                    titleText.fontSize = m_titleFontSize;

                    mainLayout.anchorMin = new Vector2(0.5f, 0.5f);
                    mainLayout.anchorMax = new Vector2(0.5f, 0.5f);
                    mainLayout.anchoredPosition = Vector2.zero;
                    mainLayout.sizeDelta = new Vector2(m_layoutWidth, mainLayout.sizeDelta.y);

                    foreach (Button but in mainLayout.GetComponentsInChildren<Button>())
                    {
                        but.GetComponentInChildren<TextMeshProUGUI>().fontSize = 32;
                        but.GetComponent<LayoutElement>().minHeight = 52 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
                        but.GetComponent<LayoutElement>().preferredHeight = 52 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
                    }

                    foreach (TextMeshProUGUI txt in questionInput.GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        txt.fontSize = m_inputFontSize;
                    }
                }
                else
                {
                    float aspect = arg2 / arg1;
                    titleText.fontSize = m_titleFontSize * aspect;

                    mainLayout.anchorMin = new Vector2(0f, 0.5f);
                    mainLayout.anchorMax = new Vector2(1f, 0.5f);
                    mainLayout.offsetMax = new Vector2(-50, 0);
                    mainLayout.offsetMin = new Vector2(50, 0);

                    mainLayout.anchoredPosition = Vector2.zero;

                    foreach (Button but in mainLayout.GetComponentsInChildren<Button>())
                    {
                        but.GetComponentInChildren<TextMeshProUGUI>().fontSize = 32 * AppManager.Instance.Settings.HUDSettings.mobileFontScaler * aspect;
                        but.GetComponent<LayoutElement>().minHeight = 52 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler * aspect;
                        but.GetComponent<LayoutElement>().preferredHeight = 52 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler * aspect;
                    }

                    foreach (TextMeshProUGUI txt in questionInput.GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        txt.fontSize = m_inputFontSize * aspect;
                    }
                }
            }
        }

        public void SetCallback(System.Action<string> callback)
        {
            m_callback = callback;
        }

        private void Update()
        {
            if (InputManager.Instance.GetKeyUp("Enter"))
            {
                AskQuestion();
            }
        }
        public void AskQuestion()
        {
            m_callback.Invoke(questionInput.text);

            Close();
        }

        public void Close()
        {
            m_callback = null;
            HUDManager.Instance.ToggleHUDMessage("OPENAIQUESTION_MESSAGE", false);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OpenAIQuestion), true)]
        public class OpenAIQuestion_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("questionInput"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mainLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("titleText"), true);

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
