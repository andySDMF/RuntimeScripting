using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class PhotonChatBasic : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField]
        private TMP_Dropdown chatMessageDropdown;

        [Header("Instantiate")]
        [SerializeField]
        private Transform container;
        [SerializeField]
        private GameObject textPrefab;

        [Header("Chat")]
        [SerializeField]
        private int maxCharPerLine = 45;
        [SerializeField]
        private GameObject messageOverlayError;


        private PhotonPhoneCall phoneCall;
        private GameObject phoneDisplay;

        /// <summary>
        /// Access to the dropdown object used for storing player chats
        /// </summary>
        public TMP_Dropdown DropDown
        {
            get
            {
                return chatMessageDropdown;
            }
        }

        /// <summary>
        /// Global access to all players subscribed to the smartphone chat system
        /// </summary>
        public Dictionary<string, string> PlayerLookup
        {
            get
            {
                return m_playerLookup;
            }
        }

        /// <summary>
        /// Global access to the current chat open
        /// </summary>
        public string CurrentChat
        {
            get
            {
                return m_currentChat;
            }
        }
        /// <summary>
        /// Global access to the current call open
        /// </summary>
        public string CurrentCall
        {
            get
            {
                return m_currentCall;
            }
        }


        /// <summary>
        /// The max char of the messge before created a new line
        /// </summary>
        public int MaxChar
        {
            get
            {
                return maxCharPerLine;
            }
        }

        private Dictionary<string, List<Chatstring>> m_messages = new Dictionary<string, List<Chatstring>>();
        private Dictionary<string, string> m_playerLookup = new Dictionary<string, string>();
        private string m_currentChat = "All";
        private int m_currentDropdownIndex = 0;
        private string m_currentCall = "";


        private void Start()
        {
            GetUIReferences();
        }

        private void GetUIReferences()
        {
            phoneDisplay = HUDManager.Instance.GetHUDScreenObject("PHONECALL_SCREEN");
            phoneCall = phoneDisplay.GetComponent<PhotonPhoneCall>();
        }

        /// <summary>
        /// Action called to enable the main UI dropdown, used when defining project chat system
        /// </summary>
        /// <param name="enable"></param>
        public void EnableDropdown(bool enable)
        {
            chatMessageDropdown.gameObject.SetActive(enable);
        }

        public bool ChatIDExists(string chatID)
        {
            return m_messages.ContainsKey(chatID);
        }

        /// <summary>
        /// Aciton called to add new chat to the dropdown
        /// </summary>
        /// <param name="player"></param>
        public void AddChatToDropdown(IPlayer player)
        {
            string chatID = "";

            if (player == null)
            {
                chatID = "All";
            }
            else
            {
                chatID = PlayerManager.Instance.GetPlayerName(player.NickName) + " [" + player.ActorNumber + "]";
            }

            //add global chat to dropdown chat list
            TMP_Dropdown.OptionDataList dList = new TMP_Dropdown.OptionDataList();
            dList.options = new List<TMP_Dropdown.OptionData>();
            dList.options.Add(new TMP_Dropdown.OptionData(chatID));

            //set the chat display to show current
            chatMessageDropdown.AddOptions(dList.options);

            if(player != null)
            {
                //create new chat message
                m_messages.Add(chatID, new List<Chatstring>());
                m_messages[chatID].Add(new Chatstring());

                //add to the lookup table
                m_playerLookup.Add(chatID, player.ID);
            }
        }

        /// <summary>
        /// Action called to remove existing chat from the dropdown
        /// </summary>
        /// <param name="player"></param>
        public void RemoveChatFromDropdown(IPlayer player)
        {
            TMP_Dropdown.OptionData index = null;

            string ChatID = PlayerManager.Instance.GetPlayerName(player.NickName) + " [" + player.ActorNumber + "]";

            //hide dropdown if open
            chatMessageDropdown.Hide();

            //get the dropdown option
            for (int i = 0; i < chatMessageDropdown.options.Count; i++)
            {
                if (chatMessageDropdown.options[i].text.Equals(ChatID))
                {
                    index = chatMessageDropdown.options[i];
                    break;
                }
            }

            //remove dropdown option from list
            chatMessageDropdown.options.Remove(index);

            //ensure the 'All' chat is now open
            if (m_currentChat.Equals(ChatID))
            {
                m_messages[ChatID].ForEach(x => Destroy(x.GO));
                chatMessageDropdown.value = 0;
            }

            if (!CoreManager.Instance.chatSettings.useGlobalChat)
            {
                if (DropDown.options.Count <= 0)
                {
                    m_currentChat = "All";
                    DropDown.captionText.text = "";
                }
            }

            //remove player from chat system
            m_playerLookup.Remove(ChatID);
            m_messages.Remove(ChatID);
        }

        /// <summary>
        /// Returns if the current chat is open
        /// </summary>
        /// <returns></returns>
        public bool IsCurrentChatOpen()
        {
            //get the dropdown option
            for (int i = 0; i < chatMessageDropdown.options.Count; i++)
            {
                if (chatMessageDropdown.options[i].text.Equals(CurrentChat))
                {
                    if(i.Equals(m_currentDropdownIndex))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Action called to switch the chat based on an index value of the dropdown
        /// </summary>
        /// <param name="index"></param>
        public void SwitchChat(int index)
        {
            if (index > chatMessageDropdown.options.Count - 1 || index < 0) return;

            if (index.Equals(m_currentDropdownIndex)) return;

            m_currentDropdownIndex = index;

            if(m_messages.ContainsKey(m_currentChat))
            {
                //destroy all current chat obejcts
                m_messages[m_currentChat].ForEach(x => Destroy(x.GO));
            }

            if (index > 0)
            {
                m_currentChat = chatMessageDropdown.options[m_currentDropdownIndex].text;

                //create chat objects
                m_messages[m_currentChat].ForEach(x => x.Create(textPrefab, container, maxCharPerLine));
            }
            else
            {
                m_currentChat = "All";
            }

            //hide overlay
            messageOverlayError.GetComponentInChildren<TextMeshProUGUI>(true).text = "";
            messageOverlayError.SetActive(false);
        }

        /// <summary>
        /// Action called to start/end a phone call
        /// </summary>
        /// <param name="playerID"></param>
        /// <param name="isRequest"></param>
        public void SwitchCall(string playerID, bool isRequest)
        {
            if (string.IsNullOrEmpty(playerID))
            {
                m_currentCall = "";
                phoneCall.EndPhoneCall();
                return;
            }

            m_currentCall = playerID;

            if (isRequest)
            {
                //request for accept and decline
                phoneCall.Set(PhotonPhoneCall.PhoneCallType.Reciever, MMOManager.Instance.GetPlayerByUserID(playerID));
            }
            else
            {
                //show calling
                phoneCall.Set(PhotonPhoneCall.PhoneCallType.Caller, MMOManager.Instance.GetPlayerByUserID(playerID));
            }

            phoneDisplay.SetActive(true);
        }

        public void StartPhoneCall()
        {
            if (!string.IsNullOrEmpty(m_currentCall))
            {
                phoneCall.StartPhoneCall();
            }
        }

        public void EndPhoneCall()
        {
            if(!string.IsNullOrEmpty(m_currentCall))
            {
                phoneCall.EndPhoneCall();
            }

            m_currentCall = "";
        }

        /// <summary>
        /// Action to call to switch the chat display using a a players photon ID
        /// </summary>
        /// <param name="playerID"></param>
        public void SwitchChat(string playerID)
        {
            //get chatID from playerlookup
            string player = m_playerLookup.FirstOrDefault(x => x.Value.Equals(playerID)).Key;

            //find index value
            for (int i = 0; i < chatMessageDropdown.options.Count; i++)
            {
                if (chatMessageDropdown.options[i].text.Equals(player))
                {
                    chatMessageDropdown.value = i;
                    break;
                }
            }

            if(!CoreManager.Instance.chatSettings.useGlobalChat && m_currentChat.Equals("All"))
            {
                m_currentChat = chatMessageDropdown.options[m_currentDropdownIndex].text;
                //create chat objects
                m_messages[m_currentChat].ForEach(x => x.Create(textPrefab, container, maxCharPerLine));

                messageOverlayError.GetComponentInChildren<TextMeshProUGUI>(true).text = "";
                messageOverlayError.SetActive(false);
            }
        }

        /// <summary>
        /// Action called to send a message across the network
        /// </summary>
        /// <param name="message"></param>
        public void SendChatMessage(string message)
        {
            if(m_currentChat.Equals("All"))
            {
                //global
                MMOChat.Instance.SendChatMessage("All", message);
            }
            else
            {

                string player = m_playerLookup.FirstOrDefault(x => x.Key.Equals(m_currentChat)).Value;

                IPlayer iPlayer = MMOManager.Instance.GetPlayerByUserID(player);

                if (MMOManager.Instance.GetPlayerProperty(iPlayer, "DONOTDISTURB").Equals("1"))
                {
                    //need to tell user to player is busy
                    messageOverlayError.GetComponentInChildren<TextMeshProUGUI>(true).text = "CANNOT SEND MESSAGE " + m_currentChat + " DOES NOT WANT TO BE DISTURBED";
                    messageOverlayError.SetActive(true);
                }
                else
                {
                    //send private message
                    MMOChat.Instance.SendChatMessage(m_playerLookup[m_currentChat], message);
                }
            }
        }

        /// <summary>
        /// Action called to add a new message to a chat
        /// </summary>
        /// <param name="chatID"></param>
        /// <param name="message"></param>
        public void AddMessage(string chatID, string message)
        {
            TextMeshProUGUI tText = null;

            if (m_messages[chatID][m_messages[chatID].Count - 1].GO != null)
            {
                tText = m_messages[chatID][m_messages[chatID].Count - 1].GO.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            //check if the total chat length of the current chatstring is less than max char count
            if (m_messages[chatID][m_messages[chatID].Count - 1].chat.Length + message.Length <= 2147483647)
            {
                //add
                m_messages[chatID][m_messages[chatID].Count - 1].chat += message;
            }
            else
            {
                //create new chatstring
                m_messages[chatID].Add(CreateNewText(maxCharPerLine));

                if (m_messages[chatID][m_messages[chatID].Count - 1].GO != null)
                {
                    tText = m_messages[chatID][m_messages[chatID].Count - 1].GO.GetComponentInChildren<TextMeshProUGUI>(true);
                }

                //add
                m_messages[chatID][m_messages[chatID].Count - 1].chat += message;
            }

            if (tText != null)
            {
                tText.text += message;
            }
        }

        /// <summary>
        /// Creates a new ChatString for a chat
        /// </summary>
        /// <returns></returns>
        public Chatstring CreateNewText(int maxCharPerLine = 45)
        {
            Chatstring cString = new Chatstring();
            cString.Create(textPrefab, container, maxCharPerLine);

            return cString;
        }

        [System.Serializable]
        public class Chatstring
        {
            public GameObject GO;
            public string chat = "";

            private GameObject prefab;
            private Transform container;
            private int maxCharPerLine = 45;

            /// <summary>
            /// Create chat object
            /// </summary>
            /// <param name="prefab"></param>
            /// <param name="container"></param>
            /// <param name="charPerLine"></param>
            public void Create(GameObject prefab, Transform container, int charPerLine = 45)
            {
                GO = Instantiate(prefab, Vector3.zero, Quaternion.identity, container);
                GO.transform.localScale = Vector3.one;
                GO.SetActive(true);

                GO.GetComponentInChildren<TextMeshProUGUI>(true).text = chat;

                this.prefab = prefab;
                this.container = container;
                this.maxCharPerLine = charPerLine;
            }

            /// <summary>
            /// Create all chat objects
            /// </summary>
            public void Create()
            {
                if(prefab != null && container != null)
                {
                    Create(prefab, container, maxCharPerLine);
                }
            }

            /// <summary>
            /// Add message to current chat object
            /// </summary>
            /// <param name="message"></param>
            /// <returns></returns>
            public bool AddMessage(string message)
            {
                TextMeshProUGUI tText = null;
                bool messageAdded = false;

                if (GO != null)
                {
                    tText = GO.GetComponentInChildren<TextMeshProUGUI>(true);
                }

                //check if the total chat length of the chatstring is less than max char count
                if (chat.Length + message.Length <= 2147483647)
                {
                    //add
                    chat += message;
                    messageAdded = true;
                }
                else
                {
                    messageAdded = false;
                }

                if (tText != null && messageAdded)
                {
                    tText.text += message;
                }

                return messageAdded;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PhotonChatBasic), true)]
        public class PhotonChatBasic_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chatMessageDropdown"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("container"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("textPrefab"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxCharPerLine"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messageOverlayError"), true);

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
