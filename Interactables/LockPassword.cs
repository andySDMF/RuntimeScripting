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
    public class LockPassword : MonoBehaviour
    {
        [Header("Error")]
        [SerializeField]
        private GameObject errorLabel;

        [Header("Mobile Layout")]
        [SerializeField]
        private RectTransform mainLayout;
        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private TextMeshProUGUI errorText;
        [SerializeField]
        private LayoutElement inputLayout;


        private float m_titleFontSize;
        private float m_layoutWidth;
        private float m_errorFontSize;
        private float m_inputLayoutSize;
        private float m_inputTextSize;

        private void Awake()
        {
            m_titleFontSize = titleText.fontSize;
            m_errorFontSize = errorText.fontSize;
            m_layoutWidth = mainLayout.sizeDelta.x;

            m_inputLayoutSize = inputLayout.minHeight;

            m_inputTextSize = inputLayout.gameObject.GetComponentInParent<TMP_InputField>(true).textComponent.fontSize;

        }

        private void OnEnable()
        {
            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            SubtitleManager.Instance.Stop();
            SubtitleManager.Instance.ToggleButtonVisibiliy(false);
        }

        private void Update()
        {
            if(InputManager.Instance.GetKeyUp("Enter"))
            {
                LockManager.Instance.ApplyPassword();
            }
        }

        private void OnDisable()
        {
            ShowError(false);

            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;


            SubtitleManager.Instance.ToggleButtonVisibiliy(true);
        }

        public void ApplyPassword()
        {
            LockManager.Instance.ApplyPassword();
        }

        public void ShowError(bool show)
        {
            errorLabel.SetActive(show);
        }

        public void Cancel()
        {
            LockManager.Instance.CancelLockPassword();
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    titleText.fontSize = m_titleFontSize;
                    errorText.fontSize = m_errorFontSize;

                    inputLayout.minHeight = m_inputLayoutSize;
                    inputLayout.preferredHeight = inputLayout.minHeight;

                    foreach(TextMeshProUGUI txt in inputLayout.GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        txt.fontSize = m_inputTextSize;
                    }

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
                    errorText.fontSize = m_errorFontSize * aspect;

                    inputLayout.minHeight = m_inputLayoutSize * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
                    inputLayout.preferredHeight = inputLayout.minHeight;


                    foreach (TextMeshProUGUI txt in inputLayout.GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        txt.fontSize = m_inputTextSize * aspect;
                    }

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
        [CustomEditor(typeof(LockPassword), true)]
        public class LockPassword_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("errorLabel"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mainLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("inputLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("titleText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("errorText"), true);

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
