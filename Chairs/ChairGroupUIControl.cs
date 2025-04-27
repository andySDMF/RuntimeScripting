using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ChairGroupUIControl : MonoBehaviour
    {
        [SerializeField]
        public Toggle chatToggle;

        [SerializeField]
        public GameObject chatNotification;

        [Header("Mobile Layout")]
        [SerializeField]
        private RectTransform buttonRect;

        private float m_cacheButtonAnchorFromBottom;


        private void Awake()
        {
            if (!AppManager.IsCreated) return;

            //subscrive to the photon chat notification system
            MMOChat.Instance.OnNotify += Notify;

            m_cacheButtonAnchorFromBottom = buttonRect.anchoredPosition.y;
        }

        private void OnEnable()
        {
            if (!AppManager.IsCreated) return;

            if(!AppManager.Instance.Settings.playerSettings.enableGlobalChatWhilstInChair)
            {
                chatToggle.gameObject.SetActive(false);
                ((Toggle)HUDManager.Instance.GetMenuFeature(HUDManager.MenuFeature._Chat)).isOn = false;
            }
            else if(!AppManager.Instance.Settings.chatSettings.useGlobalChat)
            {
                chatToggle.gameObject.SetActive(false);
                ((Toggle)HUDManager.Instance.GetMenuFeature(HUDManager.MenuFeature._Chat)).isOn = false;
            }
            else if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Offline))
            {
                chatToggle.gameObject.SetActive(false);
                ((Toggle)HUDManager.Instance.GetMenuFeature(HUDManager.MenuFeature._Chat)).isOn = false;
            }
            else
            {
                chatToggle.gameObject.SetActive(true);
                chatToggle.onValueChanged.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
                chatToggle.isOn = MMOChat.Instance.GlobalChatOpen;
                chatToggle.onValueChanged.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.RuntimeOnly);

                StartCoroutine(Delay());
            }

            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
        }

        private void OnDisable()
        {
            if (!AppManager.IsCreated) return;

            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        public void OnChatToggle(bool state)
        {
            MMOChat.Instance.GlobalChatOpen = state;
            StartCoroutine(Delay());
        }

        /// <summary>
        /// wait for end frame
        /// </summary>
        /// <returns></returns>
        private IEnumerator Delay()
        {
            yield return new WaitForEndOfFrame();

            if (chatNotification != null)
            {
                //if user read recent notification
                if(MMOChat.Instance.GlobalChatOpen)
                {
                    chatNotification.SetActive(false);
                }
                else
                {
                    chatNotification.SetActive(MMOChat.Instance.GlobalUnreadMessages);
                }
            }
        }

        private void OnDestroy()
        {
            if (!AppManager.IsCreated) return;

            //unsubscrive from photon chat notification system
            MMOChat.Instance.OnNotify -= Notify;
        }

        private void Notify(string id)
        {
            if (id.Equals("All") && chatNotification != null)
            {
                if(string.IsNullOrEmpty(MMOChat.Instance.CurrentChatID))
                {
                    chatNotification.SetActive(true);
                }
                else
                {
                    if(!MMOChat.Instance.CurrentChatID.Equals("All"))
                    {
                        chatNotification.SetActive(true);
                    }
                }
            }
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    buttonRect.pivot = new Vector2(0.5f, 0.0f);
                    buttonRect.anchoredPosition = new Vector2(0.0f, m_cacheButtonAnchorFromBottom);
                }
                else
                {

                    buttonRect.pivot = new Vector2(0.0f, 0.0f);
                    buttonRect.anchoredPosition = new Vector2(25.0f, m_cacheButtonAnchorFromBottom);
                }
            }
        }


#if UNITY_EDITOR
        [CustomEditor(typeof(ChairGroupUIControl), true)]
        public class ChairGroupUIControl_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chatToggle"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chatNotification"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonRect"), true);

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
