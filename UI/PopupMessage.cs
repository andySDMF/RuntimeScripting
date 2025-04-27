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
    public class PopupMessage : MonoBehaviour
    {
        [Header("Standard")]
        [SerializeField]
        private TextMeshProUGUI title;
        [SerializeField]
        private RectTransform icon;
        [SerializeField]
        private TextMeshProUGUI message;
        [SerializeField]
        private TextMeshProUGUI link;

        [Header("Scroll")]
        [SerializeField]
        private int stringCharLimit = 500;

        [SerializeField]
        private ScrollRect scrollRect;

        private RectTransform m_mainLayout;
        private float m_layoutWidth;

        private float m_titleFontSize;
        private float m_messageFontSize;
        private float m_linkFontSize;

        private void Awake()
        {
            m_mainLayout = transform.GetChild(0).GetComponent<RectTransform>();
            m_layoutWidth = m_mainLayout.sizeDelta.x;

            m_titleFontSize = title.fontSize;
            m_messageFontSize = message.fontSize;
            m_linkFontSize = link.fontSize;
        }

        private void OnEnable()
        {
            if(link.text.Length > 0)
            {
                link.gameObject.SetActive(true);
            }
            else
            {
                link.gameObject.SetActive(false);
            }

            if(message.text.Length > stringCharLimit)
            {
                message.alignment = TextAlignmentOptions.TopLeft;
                link.alignment = TextAlignmentOptions.TopLeft;

                scrollRect.gameObject.SetActive(true);
                scrollRect.normalizedPosition = Vector2.up;
                message.transform.SetParent(scrollRect.content);
                link.transform.SetParent(scrollRect.content);
                transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(1000, transform.GetComponent<RectTransform>().sizeDelta.y);
            }
            else
            {
                // message.alignment = TextAlignmentOptions.Center;
                //link.alignment = TextAlignmentOptions.Center;
                message.alignment = TextAlignmentOptions.TopLeft;
                link.alignment = TextAlignmentOptions.TopLeft;

                message.transform.SetParent(transform.GetChild(0));
                message.transform.SetSiblingIndex(2);

                link.transform.SetParent(transform.GetChild(0));
                link.transform.SetSiblingIndex(3);

                message.gameObject.SetActive(true);
                scrollRect.gameObject.SetActive(false);
                transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(700, transform.GetComponent<RectTransform>().sizeDelta.y);
            }

            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            SubtitleManager.Instance.Stop();
            SubtitleManager.Instance.ToggleButtonVisibiliy(false);
        }

        private void OnDisable()
        {
            transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(700, transform.GetComponent<RectTransform>().sizeDelta.y);
            message.transform.SetParent(transform.GetChild(0));
            message.transform.SetSiblingIndex(2);

            link.transform.SetParent(transform.GetChild(0));
            link.transform.SetSiblingIndex(3);
            link.gameObject.SetActive(true);

            //   message.alignment = TextAlignmentOptions.Center;
            //   link.alignment = TextAlignmentOptions.Center;
            message.alignment = TextAlignmentOptions.TopLeft;
            link.alignment = TextAlignmentOptions.TopLeft;

            scrollRect.gameObject.SetActive(false);

            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;

            SubtitleManager.Instance.ToggleButtonVisibiliy(true);
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    m_mainLayout.anchorMin = new Vector2(0.5f, 0.5f);
                    m_mainLayout.anchorMax = new Vector2(0.5f, 0.5f);
                    m_mainLayout.anchoredPosition = Vector2.zero;
                    m_mainLayout.sizeDelta = new Vector2(m_layoutWidth, m_mainLayout.sizeDelta.y);

                    title.fontSize = m_titleFontSize;
                    message.fontSize = m_messageFontSize;
                    link.fontSize = m_linkFontSize;

                    foreach (Button but in m_mainLayout.GetComponentsInChildren<Button>())
                    {
                        if (but.gameObject.name.Contains("URL")) continue;

                        but.GetComponentInChildren<TextMeshProUGUI>().fontSize = 32;
                        but.GetComponent<LayoutElement>().minHeight = 52 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
                        but.GetComponent<LayoutElement>().preferredHeight = 52 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
                    }

                    Destroy(icon.GetComponent<LayoutElement>());
                    icon.GetComponent<Image>().SetNativeSize();
                }
                else
                {
                    float aspect = arg2 / arg1;

                    m_mainLayout.anchorMin = new Vector2(0f, 0.5f);
                    m_mainLayout.anchorMax = new Vector2(1f, 0.5f);
                    m_mainLayout.offsetMax = new Vector2(-50, 0);
                    m_mainLayout.offsetMin = new Vector2(50, 0);

                    title.fontSize = m_titleFontSize * aspect;
                    message.fontSize = m_messageFontSize * aspect;
                    link.fontSize = m_linkFontSize * aspect;

                    foreach (Button but in m_mainLayout.GetComponentsInChildren<Button>())
                    {
                        if (but.gameObject.name.Contains("URL")) continue;

                        but.GetComponentInChildren<TextMeshProUGUI>().fontSize = 32 * AppManager.Instance.Settings.HUDSettings.mobileFontScaler * aspect;
                        but.GetComponent<LayoutElement>().minHeight = 52 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler * aspect;
                        but.GetComponent<LayoutElement>().preferredHeight = 52 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler * aspect;
                    }

                    LayoutElement le = icon.gameObject.AddComponent<LayoutElement>();
                    le.minWidth = icon.sizeDelta.x * aspect;
                    le.minHeight = icon.sizeDelta.y * aspect;
                }
            }
        }

        public void OpenLink()
        {
            InfotagManager.InfoTagURL tag = new InfotagManager.InfoTagURL();
            tag.title = "Web";
            tag.url = link.text;
            InfotagManager.Instance.OpenURL(tag);
        }

        public void Close()
        {
            //unfreeze player
            PlayerManager.Instance.FreezePlayer(false);
            RaycastManager.Instance.CastRay = true;

            AudioSource audio = GetComponent<AudioSource>();

            if (audio != null)
            {
                if(audio.isPlaying)
                {
                    audio.Stop();
                }

                audio.clip = null;
            }

            gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PopupMessage), true)]
        public class PopupMessage_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("title"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("message"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("link"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("stringCharLimit"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("scrollRect"), true);

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
