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
    public class ChatPanel : MonoBehaviour
    {
        [Header("User Control")]
        [SerializeField]
        private TMP_InputField inputField;

        [Header("Mobile Layout")]
        [SerializeField]
        private RectTransform messageViewport;
        [SerializeField]
        private RectTransform scrollViewport;
        [SerializeField]
        private RectTransform emotesViewport;

        private RectTransform m_rect;
        private Vector2 m_minOffset;
        private Vector2 m_maxOffset;


        private Vector2 m_inputCacheSize;
        private float m_scaler;
        private float m_inputFieldFontSize;
        private float m_emotesHeight;
        private float m_emoteAnchorPos;


        private void Awake()
        {
            m_rect = transform.GetChild(0).GetComponent<RectTransform>();
            m_minOffset = m_rect.offsetMin;
            m_maxOffset = m_rect.offsetMax;

            m_inputCacheSize = inputField.GetComponent<RectTransform>().sizeDelta;
            m_scaler = AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
            m_inputFieldFontSize = inputField.textComponent.fontSize;

            m_emotesHeight = emotesViewport.sizeDelta.y;
            m_emoteAnchorPos = emotesViewport.anchoredPosition.y;

            Repaint();
        }

        private void OnEnable()
        {
            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            if (AppManager.Instance.Data.IsMobile)
            {
                OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
            }
        }

        private void OnDisable()
        {
            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        private void Update()
        {
            Repaint();
        }

        private void Repaint()
        {
            if (AppManager.IsCreated)
            {
                float subtitleAspect = SubtitleManager.Instance.ToggleAspect;
                bool subtitleActive = SubtitleManager.Instance.ToggleActive;

                if (AppManager.Instance.Data.IsMobile || AppManager.Instance.Settings.HUDSettings.useMobileToolsForDesktop)
                {
                    if (OrientationManager.Instance.CurrentOrientation.Equals(OrientationType.landscape))
                    {
                        m_rect.anchorMin = new Vector2(1, 0);
                        m_rect.anchorMax = new Vector2(1, 1);
                        m_rect.sizeDelta = new Vector2(500, m_rect.sizeDelta.y);
                        m_rect.anchoredPosition = new Vector2(-25, m_rect.anchoredPosition.y);

                        if (HUDManager.Instance.NavigationHUDVisibility)
                        {
                            if (subtitleActive)
                            {
                                m_rect.offsetMin = new Vector2(m_minOffset.x, (m_minOffset.y * 2) + subtitleAspect + MainHUDMenuPanel.Instance.MobileButton.sizeDelta.y);
                            }
                            else
                            {
                                m_rect.offsetMin = new Vector2(m_minOffset.x, (m_minOffset.y * 2) + MainHUDMenuPanel.Instance.MobileButton.sizeDelta.y);
                            }
                        }
                        else
                        {
                            if (subtitleActive)
                            {
                                m_rect.offsetMin = new Vector2(m_minOffset.x, (m_minOffset.y * 2) + subtitleAspect);
                            }
                            else
                            {
                                m_rect.offsetMin = new Vector2(m_minOffset.x, m_minOffset.y);
                            }
                        }
                    }
                    else
                    {
                        m_rect.anchorMin = new Vector2(0, 0);
                        m_rect.anchorMax = new Vector2(1, 1);
                        m_rect.offsetMax = new Vector2(-25, m_rect.offsetMax.y);

                        RectTransform mobileControl = HUDManager.Instance.GetHUDControlObject("MOBILE_JOYSTICK").GetComponent<RectTransform>();

                        if (mobileControl.gameObject.activeInHierarchy)
                        {
                            if (PlayerManager.Instance.MainControlSettings.controllerType == 0)
                            {
                                m_rect.offsetMin = new Vector2(25, (m_minOffset.y * 2) + (mobileControl.GetChild(0).GetComponent<RectTransform>().sizeDelta.y + 10));
                            }
                            else
                            {
                                m_rect.offsetMin = new Vector2(25, (m_minOffset.y * 2) + (mobileControl.GetChild(1).GetComponent<RectTransform>().sizeDelta.y + 10));
                            }
                        }
                        else
                        {
                            if (HUDManager.Instance.NavigationHUDVisibility)
                            {
                                if (subtitleActive)
                                {
                                    m_rect.offsetMin = new Vector2(25, (m_minOffset.y * 2) + subtitleAspect + MainHUDMenuPanel.Instance.MobileButton.sizeDelta.y);
                                }
                                else
                                {
                                    m_rect.offsetMin = new Vector2(25, (m_minOffset.y * 2) + MainHUDMenuPanel.Instance.MobileButton.sizeDelta.y);
                                }
                            }
                            else
                            {
                                if (subtitleActive)
                                {
                                    m_rect.offsetMin = new Vector2(25, (m_minOffset.y * 2) + subtitleAspect);
                                }
                                else
                                {
                                    m_rect.offsetMin = new Vector2(25, m_minOffset.y);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (subtitleActive)
                    {
                        m_rect.offsetMin = new Vector2(m_minOffset.x, (m_minOffset.y * 2) + subtitleAspect);
                    }
                    else
                    {
                        m_rect.offsetMin = new Vector2(m_minOffset.x, m_minOffset.y);
                    }
                }
            }
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                TMP_Text[] txtComps = inputField.GetComponentsInChildren<TMP_Text>(true);

                TMP_Text[] txtMessages = messageViewport.GetComponentsInChildren<TMP_Text>(true);

                Vector2 cellsize = new Vector2(52 * m_scaler, 52 * m_scaler);


                if (arg0.Equals(OrientationType.landscape))
                {
                    inputField.GetComponent<RectTransform>().sizeDelta = new Vector2(m_inputCacheSize.x, m_inputCacheSize.y * m_scaler);
                    inputField.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(inputField.transform.parent.GetComponent<RectTransform>().sizeDelta.x, m_inputCacheSize.y * m_scaler);

                    for (int i = 0; i < txtComps.Length; i++)
                    {
                        txtComps[i].fontSize = m_inputFieldFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    }

                    messageViewport.offsetMin = new Vector2(messageViewport.offsetMin.x, inputField.GetComponent<RectTransform>().sizeDelta.y + 25);
                    scrollViewport.offsetMin = new Vector2(messageViewport.offsetMin.x, inputField.GetComponent<RectTransform>().sizeDelta.y + 25);

                    for (int i = 0; i < txtComps.Length; i++)
                    {
                        txtMessages[i].fontSize = m_inputFieldFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    }

                    emotesViewport.GetComponentInChildren<GridLayoutGroup>().cellSize = cellsize;
                    emotesViewport.sizeDelta = new Vector2(emotesViewport.sizeDelta.x, m_emotesHeight);
                    emotesViewport.anchoredPosition = new Vector2(emotesViewport.anchoredPosition.x, m_emoteAnchorPos + 25);

                    foreach (Button but in emotesViewport.GetComponentsInChildren<Button>())
                    {
                        RectTransform rt = but.transform.GetChild(0).GetComponentInChildren<RectTransform>(true);

                        if (rt != null)
                        {
                            rt.GetComponentInChildren<Image>(true).SetNativeSize();
                        }
                    }
                }
                else
                {
                    float aspect = arg2 / arg1;
                    inputField.GetComponent<RectTransform>().sizeDelta = new Vector2(m_inputCacheSize.x, (m_inputCacheSize.y * m_scaler) * aspect);
                    inputField.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(inputField.transform.parent.GetComponent<RectTransform>().sizeDelta.x, (m_inputCacheSize.y * m_scaler) * aspect);

                    for (int i = 0; i < txtComps.Length; i++)
                    {
                        txtComps[i].fontSize = m_inputFieldFontSize * (AppManager.Instance.Settings.HUDSettings.mobileFontScaler * aspect);
                    }

                    messageViewport.offsetMin = new Vector2(messageViewport.offsetMin.x, inputField.GetComponent<RectTransform>().sizeDelta.y + 25);
                    scrollViewport.offsetMin = new Vector2(messageViewport.offsetMin.x, inputField.GetComponent<RectTransform>().sizeDelta.y + 25);


                    for (int i = 0; i < txtComps.Length; i++)
                    {
                        txtMessages[i].fontSize = m_inputFieldFontSize * (AppManager.Instance.Settings.HUDSettings.mobileFontScaler * aspect);
                    }

                    emotesViewport.GetComponentInChildren<GridLayoutGroup>().cellSize = new Vector2(cellsize.x * aspect, cellsize.y * aspect);
                    emotesViewport.sizeDelta = new Vector2(emotesViewport.sizeDelta.x, m_emotesHeight * aspect);
                    emotesViewport.anchoredPosition = new Vector2(emotesViewport.anchoredPosition.x, (m_emoteAnchorPos  * aspect) + 25);

                    foreach (Button but in emotesViewport.GetComponentsInChildren<Button>())
                    { 
                        RectTransform rt = but.transform.GetChild(0).GetComponentInChildren<RectTransform>(true);

                        if (rt != null)
                        {
                            rt.sizeDelta = new Vector2(rt.sizeDelta.x * aspect, rt.sizeDelta.y * aspect);
                        }
                    }
                }
            }
        }

        public void SwitchChat(int n)
        {
            MMOChat.Instance.SwitchChat(n);
        }

        public void FreezePlayer(bool state)
        {
            PlayerManager.Instance.FreezePlayer(state);
        }

        public void SendChatMessage()
        {
            MMOChat.Instance.SendChatMessage();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ChatPanel), true)]
        public class ChatPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("inputField"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messageViewport"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("scrollViewport"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("emotesViewport"), true);

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
