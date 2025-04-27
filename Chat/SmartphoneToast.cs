using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Button))]
    public class SmartphoneToast : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField]
        private Image icon;

        [SerializeField]
        private TMPro.TextMeshProUGUI title;

        [Header("Content")]
        [SerializeField]
        private TMPro.TextMeshProUGUI sender;

        [SerializeField]
        private TMPro.TextMeshProUGUI message;

   
        private int maxMessageCharDisplay = 25;
        private float displayDuration = 5.0f;
        private float m_displayTimer = 0.0f;
        private ToastPanel.ToastType m_type;

        private OrientationType m_switch = OrientationType.landscape;
        private float m_scaler;

        private float m_titleFontSize;
        private float m_senderFontSize;
        private float m_messageFontSize;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
            m_scaler = AppManager.Instance.Settings.HUDSettings.mobileIconFontScaler;

            m_titleFontSize = title.fontSize;
            m_senderFontSize = sender.fontSize;
            m_messageFontSize = message.fontSize;


            if(icon.sprite == null)
            {
                icon.sprite = GetComponentInParent<ToastPanel>().GetToastIcon(ToastPanel.ToastType._Message);
            }
        }

        /// <summary>
        /// Action posted directly to the button script
        /// </summary>
        private void OnClick()
        {
            if(m_type.Equals(ToastPanel.ToastType._Message))
            {
                //open the chat and close toast object
                MMOChat.Instance.SwitchChat(MMOChat.Instance.GetPlayerIDFromChat(sender.text));
            }

            Close();
        }

        /// <summary>
        /// Action called to update the taost information
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        public void Set(ToastPanel.ToastType type, ToastPanel.ToastTypeStyle style, string sender, string message, int charDisplay = 25, float displayTime = 5.0f)
        {
            m_type = type;

            icon.sprite = style.icon;
            icon.SetNativeSize();
            title.text = style.title;

            this.sender.text = sender;

            this.message.text = (message.Length > maxMessageCharDisplay) ? message.Substring(0, 24) + "..." : message;

            maxMessageCharDisplay = charDisplay;
            displayDuration = displayTime;
            m_displayTimer = 0.0f;

            gameObject.SetActive(true);
        }

        private void Update()
        {
            //timer to close toast object
            if(m_displayTimer < displayDuration)
            {
                m_displayTimer += Time.deltaTime;
            }
            else
            {
                Close();
            }

            if (AppManager.Instance.Data.IsMobile && !m_switch.Equals(OrientationManager.Instance.CurrentOrientation))
            {
                m_switch = OrientationManager.Instance.CurrentOrientation;

                if (m_switch.Equals(OrientationType.landscape))
                {
                    Destroy(icon.GetComponent<LayoutElement>());
                    icon.SetNativeSize();
                    title.fontSize = m_titleFontSize;
                    sender.fontSize = m_senderFontSize;
                    message.fontSize = m_messageFontSize;
                }
                else
                {
                    LayoutElement le = icon.gameObject.AddComponent<LayoutElement>();
                    le.minWidth = icon.GetComponent<RectTransform>().sizeDelta.x * AppManager.Instance.Settings.HUDSettings.mobileIconFontScaler;
                    le.minHeight = icon.GetComponent<RectTransform>().sizeDelta.y * AppManager.Instance.Settings.HUDSettings.mobileIconFontScaler;

                    title.fontSize = m_titleFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    sender.fontSize = m_senderFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    message.fontSize = m_messageFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                }
            }
        }

        public void Close()
        {
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SmartphoneToast), true)]
        public class SmartphoneToast_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("title"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sender"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("message"), true);

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
