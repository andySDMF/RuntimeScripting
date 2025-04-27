using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class PhotonChatNotification : MonoBehaviour
    {
        [SerializeField]
        private GameObject notification;

        [SerializeField]
        private PhotonChatNotification[] others;

        [SerializeField]
        private ChatSystem chatSystem = ChatSystem.Basic;

        /// <summary>
        /// Access to the ChatID of this notification
        /// </summary>
        public string ChatID { get; set; }

        private List<string> m_unreadMessages = new List<string>();

        public bool UnreadMessages
        {
            get
            {
                return m_unreadMessages.Count > 0;
            }
        }

        private void Start()
        {
            chatSystem = CoreManager.Instance.chatSettings.privateChat;
        }

        private void Update()
        {
            //check notifications and handle visual state
            if (m_unreadMessages.Count > 0)
            {
                if (!notification.activeInHierarchy)
                {
                    notification.SetActive(true);
                }
            }
            else
            {
                if (notification.activeInHierarchy)
                {
                    notification.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Called when a chat UI is open
        /// </summary>
        /// <param name="chatID"></param>
        /// <param name="chatOpen"></param>
        public void OnChatOpen(string chatID, bool chatOpen)
        { 
            if(chatOpen)
            {
                RemoveUnreadMessage(chatID);
            }
        }

        /// <summary>
        /// Adds a message chatID to the unread list
        /// </summary>
        /// <param name="chatID"></param>
        public void AddUnreadMessage(string chatID)
        {
            if(!m_unreadMessages.Contains(chatID))
            {
                m_unreadMessages.Add(chatID);
            }

            for(int i = 0; i < others.Length; i++)
            {
                others[i].AddUnreadMessage(chatID);
            }
        }

        /// <summary>
        /// Removes message chat ID to the unread list
        /// </summary>
        /// <param name="chatID"></param>
        public void RemoveUnreadMessage(string chatID)
        {
            if (m_unreadMessages.Contains(chatID))
            {
                m_unreadMessages.Remove(chatID);
            }

            for (int i = 0; i < others.Length; i++)
            {
                others[i].RemoveUnreadMessage(chatID);
            }
        }

        /// <summary>
        /// Has this player reference got an unread message
        /// </summary>
        /// <param name="chatID"></param>
        /// <returns></returns>
        public bool PlayerHasUnreadMessages(string chatID)
        {
            return m_unreadMessages.Contains(chatID);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PhotonChatNotification), true)]
        public class PhotonChatNotification_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("notification"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("others"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chatSystem"), true);

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
