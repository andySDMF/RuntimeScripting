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
    public class InviteAcceptance : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI message;

        [Header("Mobile Layout")]
        [SerializeField]
        private RectTransform mainLayout;
        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private TextMeshProUGUI messageText;

        private string m_cacheInviteCode = "";
        private InviteManager.InviteType m_inviteType;

        private float m_titleFontSize;
        private float m_messageFontSize;
        private float m_layoutWidth;

        private void Awake()
        {
            m_titleFontSize = titleText.fontSize;
            m_layoutWidth = mainLayout.sizeDelta.x;
            m_messageFontSize = messageText.fontSize;
        }


        private void OnEnable()
        {
            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            SubtitleManager.Instance.Stop();
            SubtitleManager.Instance.ToggleButtonVisibiliy(false);

        }

        private void OnDisable()
        {
            m_cacheInviteCode = "";

            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;

            SubtitleManager.Instance.ToggleButtonVisibiliy(true);
        }

        public void Set(string sender, int type, string inviteCode)
        {
            m_cacheInviteCode = inviteCode;
            m_inviteType = (InviteManager.InviteType)type;

            if(m_inviteType.Equals(InviteManager.InviteType.Room))
            {
                string mes = (string.IsNullOrEmpty(AppManager.Instance.Settings.projectSettings.roomInviteMessage)) ? "has invited you to join room" : AppManager.Instance.Settings.projectSettings.roomInviteMessage;

                message.text = sender + " " + mes;
            }
            else
            {
                string mes = (string.IsNullOrEmpty(AppManager.Instance.Settings.projectSettings.videoInviteMessage)) ? "has invited you to join video chat" : AppManager.Instance.Settings.projectSettings.videoInviteMessage;

                message.text = sender + " " + mes;
            }
        }

        public void Decline()
        {
            HUDManager.Instance.ToggleHUDMessage("INVITE_MESSAGE", false);
        }

        public void Join()
        {
            InviteManager.Instance.JoinInvite(m_inviteType, m_cacheInviteCode);
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    titleText.fontSize = m_titleFontSize;
                    messageText.fontSize = m_messageFontSize;

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
                    messageText.fontSize = m_messageFontSize * aspect;

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
        [CustomEditor(typeof(InviteAcceptance), true)]
        public class InviteAcceptance_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("message"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mainLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("titleText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messageText"), true);

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