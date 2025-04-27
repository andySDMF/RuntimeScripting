using System;
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
    public class ToolsPanel : MonoBehaviour
    {
        [Header("Mobile Layout")]
        [SerializeField]
        private RectTransform layoutObject;
        [SerializeField]
        private LayoutElement scrollArea;
        [SerializeField]
        private TextMeshProUGUI titleText;

        [Header("Tools")]
        [SerializeField]
        private List<ToolAction> toolActions = new List<ToolAction>();

        private Vector2 m_cacheSize;
        private float m_cacheTitleFontSize;

        private void Start()
        {
            if (AppManager.IsCreated)
            {
                m_cacheSize = new Vector2(layoutObject.sizeDelta.x, scrollArea.minHeight);
                m_cacheTitleFontSize = titleText.fontSize;
                OrientationManager.Instance.OnOrientationChanged += OnOrientation;

                OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
            }
        }

        private void OnEnable()
        {
            for (int i = 0; i < toolActions.Count; i++)
            {
                if (toolActions[i].reference is Toggle)
                {
                    ((Toggle)toolActions[i].reference).isOn = false;
                }
            }
        }

        private void Update()
        {
            if (AppManager.IsCreated)
            {
                if (AppManager.Instance.Data.IsMobile)
                {
                    layoutObject.anchoredPosition = new Vector2(0 - (MainHUDMenuPanel.Instance.MobileButton.sizeDelta.x + 30), 0 + (MainHUDMenuPanel.Instance.MobileButton.sizeDelta.y + 30));

                    for (int i = 0; i < toolActions.Count; i++)
                    {
                        if (toolActions[i].tool.gameObject.activeInHierarchy != toolActions[i].reference.gameObject.activeInHierarchy)
                        {
                            toolActions[i].tool.gameObject.SetActive(toolActions[i].reference.gameObject.activeInHierarchy);
                        }

                        if (toolActions[i].tool.gameObject.name.Contains("Music"))
                        {
                            if (toolActions[i].tool.transform.GetChild(0).gameObject.activeInHierarchy != toolActions[i].reference.transform.GetChild(0).gameObject.activeInHierarchy)
                            {
                                toolActions[i].tool.transform.GetChild(0).gameObject.SetActive(toolActions[i].reference.transform.GetChild(0).gameObject.activeInHierarchy);
                                toolActions[i].reference.transform.GetChild(1).gameObject.SetActive(!toolActions[i].reference.transform.GetChild(0).gameObject.activeInHierarchy);
                            }
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (!AppManager.IsCreated) return;

            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if(arg0.Equals(OrientationType.landscape))
                {
                    scrollArea.minHeight = m_cacheSize.y;
                    layoutObject.sizeDelta = new Vector2(m_cacheSize.x, layoutObject.sizeDelta.y);
                    titleText.fontSize = m_cacheTitleFontSize;
                }
                else
                {
                    float aspect = arg2 / arg1;
                    layoutObject.sizeDelta = new Vector2(m_cacheSize.x * aspect, layoutObject.sizeDelta.y);
                    scrollArea.minHeight = m_cacheSize.y * aspect;

                    titleText.fontSize = m_cacheTitleFontSize * aspect;
                }
            }
        }

        public void Close()
        {
            MainHUDMenuPanel.Instance.MobileButton.GetComponent<Toggle>().isOn = false;
        }

        public void ActionTool(Selectable tool)
        {
            for (int i = 0; i < toolActions.Count; i++)
            {
                if(tool.gameObject.name.Equals(toolActions[i].tool.gameObject.name))
                {
                    if(toolActions[i].tool.gameObject.name.Contains("Share"))
                    {
                        if(AppManager.Instance.Settings.socialMediaSettings.enableLinkedInPost)
                        {
                            //this needs to open Social Media panel
                            SocialMediaManager.Instance.OpenSocialMediaPost(SocialMediaSettings.SocialMediaPlatform.LinkedIn, SocialMediaManager.PostType.Link_Comment, AppManager.Instance.Settings.socialMediaSettings.mainURL);
                        }
                        else if (AppManager.Instance.Settings.socialMediaSettings.enableWhatsAppPost)
                        {
                            //this needs to open Social Media panel
                            SocialMediaManager.Instance.OpenSocialMediaPost(SocialMediaSettings.SocialMediaPlatform.WhatsApp, SocialMediaManager.PostType.Link_Comment, AppManager.Instance.Settings.socialMediaSettings.mainURL);
                        }
                        else if (AppManager.Instance.Settings.socialMediaSettings.enableTwitterPost)
                        {
                            //this needs to open Social Media panel
                            SocialMediaManager.Instance.OpenSocialMediaPost(SocialMediaSettings.SocialMediaPlatform.Twitter, SocialMediaManager.PostType.Link_Comment, AppManager.Instance.Settings.socialMediaSettings.mainURL);
                        }
                        else if (AppManager.Instance.Settings.socialMediaSettings.enableFacebookPost)
                        {
                            //this needs to open Social Media panel
                            SocialMediaManager.Instance.OpenSocialMediaPost(SocialMediaSettings.SocialMediaPlatform.Facebook, SocialMediaManager.PostType.Link_Comment, AppManager.Instance.Settings.socialMediaSettings.mainURL);
                        }
                        else
                        {

                        }

                    }
                    else
                    {
                        if (toolActions[i].reference is Toggle)
                        {
                            ((Toggle)toolActions[i].reference).isOn = !((Toggle)toolActions[i].reference).isOn;
                        }
                        else
                        {
                            ((Button)toolActions[i].reference).onClick.Invoke();
                        }
                    }


                    if (toolActions[i].autoClosePanel)
                    {
                        Close();
                    }

                    break;
                }
            }
        }

        [System.Serializable]
        private class ToolAction
        {
            public Selectable tool;
            public Selectable reference;
            public bool autoClosePanel = true;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ToolsPanel), true)]
        public class ToolsPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("toolActions"), true);


                    EditorGUILayout.PropertyField(serializedObject.FindProperty("layoutObject"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("scrollArea"), true);
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
