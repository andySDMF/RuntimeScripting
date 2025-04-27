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
    public class PromptMessagePanel : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup panelCanvas;

        [SerializeField]
        private TextMeshProUGUI promptMessage;

        [SerializeField]
        private TextMeshProUGUI yesText;

        [SerializeField]
        private TextMeshProUGUI noText;

        [Header("Mobile Layout")]
        [SerializeField]
        private RectTransform mainLayout;
        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private TextMeshProUGUI continueText;

        private float m_titleFontSize;
        private float m_continueFontSize;
        private float m_layoutWidth;

        private System.Action<bool> promptAction;

        private void Awake()
        {
            m_titleFontSize = titleText.fontSize;
            m_layoutWidth = mainLayout.sizeDelta.x;
            m_continueFontSize = continueText.fontSize;
        }

        private void Update()
        {
            if(InputManager.Instance.GetKeyUp("Y"))
            {
                Yes();
            }

            if (InputManager.Instance.GetKeyUp("N"))
            {
                No();
            }
        }

        private void OnDisable()
        {
            PlayerManager.Instance.FreezePlayer(false);
            RaycastManager.Instance.CastRay = true;
            SoundManager.Instance.Stop(true);

            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;

            SubtitleManager.Instance.Stop();
            SubtitleManager.Instance.ToggleButtonVisibiliy(true);

        }

        private void OnEnable()
        {
            PlayerManager.Instance.FreezePlayer(true);
            RaycastManager.Instance.CastRay = false;

            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            SubtitleManager.Instance.ToggleButtonVisibiliy(false);

        }

        public void Set(string prompt, System.Action<bool> callback, string yes = "Yes", string no = "No")
        {
            promptMessage.text = prompt;
            panelCanvas.alpha = string.IsNullOrEmpty(prompt) ? 0.0f : 1.0f;

            promptAction = callback;

            yesText.text = yes;
            noText.text = no;
        }

        public void Yes()
        {
            if(promptAction != null)
            {
                promptAction.Invoke(true);
            }

            HUDManager.Instance.ToggleHUDMessage("PROMPT_MESSAGE", false);
        }

        public void No()
        {
            if (promptAction != null)
            {
                promptAction.Invoke(false);
            }


            HUDManager.Instance.ToggleHUDMessage("PROMPT_MESSAGE", false);
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    titleText.fontSize = m_titleFontSize;
                    continueText.fontSize = m_continueFontSize;

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
                }
                else
                {
                    float aspect = arg2 / arg1;
                    titleText.fontSize = m_titleFontSize * aspect;
                    continueText.fontSize = m_continueFontSize * aspect;

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
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PromptMessagePanel), true)]
        public class PromptMessagePanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("panelCanvas"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("promptMessage"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("yesText"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("noText"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mainLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("titleText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("continueText"), true);

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
