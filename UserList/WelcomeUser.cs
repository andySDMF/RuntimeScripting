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
    public class WelcomeUser : MonoBehaviour
    {
        [SerializeField]
        private string welcomeMessage = "Welcome";

        [SerializeField]
        private bool includeUserName = true;

        [SerializeField]
        private Image welcomeIcon;

        [SerializeField]
        private Sprite icon;

        private AudioSource m_audio;

        private float m_fontSize;

        private void Awake()
        {
            m_audio = GetComponent<AudioSource>();
            m_fontSize = GetComponentInChildren<TextMeshProUGUI>().fontSize;
        }

        private void OnEnable()
        {
            OrientationManager.Instance.OnOrientationChanged += OnOrientation;
        }

        private void OnDisable()
        {
            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if(AppManager.Instance.Settings.projectSettings.welcomeClip != null)
            {
                m_audio.clip = AppManager.Instance.Settings.projectSettings.welcomeClip;
                m_audio.Play();
            }

            string str = "";
            includeUserName = AppManager.Instance.Settings.projectSettings.includeUserName;
            welcomeMessage = AppManager.Instance.Settings.projectSettings.welcomeMessage;

            if(AppManager.Instance.Settings.projectSettings.welcomeIcon != null)
            {
                icon = AppManager.Instance.Settings.projectSettings.welcomeIcon;
            }

            if (includeUserName)
            {
                if (!AppManager.Instance.Data.NickName.Equals("User"))
                {
                    str = " " + AppManager.Instance.Data.NickName;

                    if (string.IsNullOrEmpty(str))
                    {
                        str = AppManager.Instance.Data.NickName;
                    }
                }
                else
                {
                    if (AppManager.Instance.Settings.projectSettings.useIndexedDB)
                    {
                        str = " " + AppManager.Instance.Data.NickName;

                        if (string.IsNullOrEmpty(str))
                        {
                            str = AppManager.Instance.Data.NickName;
                        }
                    }
                }
            }
            else
            {

            }

            GetComponentInChildren<TextMeshProUGUI>().text = welcomeMessage + str;

            welcomeIcon.sprite = icon;
            welcomeIcon.SetNativeSize();

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    Destroy(welcomeIcon.GetComponent<LayoutElement>());
                    welcomeIcon.SetNativeSize();

                    GetComponentInChildren<TextMeshProUGUI>().fontSize = m_fontSize;
                }
                else
                {
                    float aspect = arg2 / arg1;

                    LayoutElement le = welcomeIcon.gameObject.AddComponent<LayoutElement>();
                    le.minWidth = welcomeIcon.GetComponent<RectTransform>().sizeDelta.x * aspect;
                    le.minHeight = welcomeIcon.GetComponent<RectTransform>().sizeDelta.y * aspect;

                    GetComponentInChildren<TextMeshProUGUI>().fontSize = m_fontSize * aspect;
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(WelcomeUser), true)]
        public class WelcomeUser_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("welcomeMessage"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("includeUserName"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("welcomeIcon"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"), true);

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
