using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class PhotonChatSmartphone : MonoBehaviour
    {
        [Header("Smartphone Main")]
        [SerializeField]
        private GameObject smartphoneDisplay;

        private GameObject phoneToggle;

        [Header("Smartphone Screens")]
        [SerializeField]
        private GameObject homescreenDisplay;
        [SerializeField]
        private GameObject usersPhoneDisplay;
        [SerializeField]
        private GameObject usersMessageDisplay;
        [SerializeField]
        private GameObject settingsDisplay;

        [Header("Smartphone Services")]
        [SerializeField]
        private GameObject messageDisplay;
        [SerializeField]
        private GameObject phoneDisplay;

        [Header("Smartphone Messages")]
        [SerializeField]
        private GameObject messageOption;
        [SerializeField]
        private TextMeshProUGUI headerPlayer;
        [SerializeField]
        private int maxCharPerLine = 45;
        [SerializeField]
        private Color localPlayer = Color.green;
        [SerializeField]
        private Sprite localSprite;
        [SerializeField]
        private Color otherPlayer = Color.red;
        [SerializeField]
        private Sprite remoteSprite;
        [SerializeField]
        private Transform container;
        [SerializeField]
        private GameObject messagePrefab;
        [SerializeField]
        private TMP_InputField dialogText;
        [SerializeField]
        private GameObject newMessageButton;
        [SerializeField]
        private GameObject messageOverlayError;
      
        private ToastPanel toast;

        [Header("Smartphone Settings")]
        [SerializeField]
        private Toggle disturbedToggle;
        [SerializeField]
        private Image disturbToggleOnImage;

        [Header("Smartphone Call")]
        [SerializeField]
        private PhotonPhoneCall phoneCall;

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
                return m_currenCall;
            }
        }

        /// <summary>
        /// Is the smartphone chat open, sets the active state of the smartphone
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return smartphoneDisplay.activeInHierarchy;
            }
            set
            {
                if(phoneToggle == null)
                {
                    phoneToggle = HUDManager.Instance.GetMenuFeature(HUDManager.MenuFeature._Phone).gameObject;
                }

                phoneToggle.GetComponent<Toggle>().isOn = value;
            }
        }

        private Dictionary<string, string> m_playerLookup = new Dictionary<string, string>();
        private Dictionary<string, SmartphoneChat> m_messages = new Dictionary<string, SmartphoneChat>();
        private string m_currentChat = "";
        private string m_currenCall = "";
        private bool m_ignoreDisturbedState = false;

        private RectTransform m_mainLayout;

        private void Awake()
        {
            m_mainLayout = GetComponent<RectTransform>();
        }

        private void Start()
        {
           GetUIReferences();
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

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    m_mainLayout.anchorMin = new Vector2(0.5f, 0.0f);
                    m_mainLayout.anchorMax = new Vector2(0.5f, 1.0f);
                    m_mainLayout.offsetMax = new Vector2(0, m_mainLayout.offsetMax.y);
                    m_mainLayout.offsetMin = new Vector2(0, m_mainLayout.offsetMin.y);

                    m_mainLayout.anchoredPosition = Vector2.zero;
                    m_mainLayout.GetChild(0).transform.localScale = new Vector3(1.3f, 1.3f, 1.0f);
                }
                else
                {
                    float aspect = arg2 / arg1;

                    m_mainLayout.anchorMin = new Vector2(0.5f, 0.0f);
                    m_mainLayout.anchorMax = new Vector2(0.5f, 1.0f);
                    m_mainLayout.offsetMax = new Vector2(0, m_mainLayout.offsetMax.y);
                    m_mainLayout.offsetMin = new Vector2(0, m_mainLayout.offsetMin.y);

                    m_mainLayout.anchoredPosition = Vector2.zero;
                    m_mainLayout.GetChild(0).transform.localScale = new Vector3(1.5f * aspect, 1.5f * aspect, 1.0f);
                }
            }
        }

        private void GetUIReferences()
        {
            phoneToggle = HUDManager.Instance.GetMenuFeature(HUDManager.MenuFeature._Phone).gameObject;
            toast = HUDManager.Instance.GetHUDScreenObject("SMARTPHONETOAST_SCREEN").GetComponent<ToastPanel>();

            messageOption.GetComponent<CanvasGroup>().alpha = AppManager.Instance.Settings.chatSettings.usePrivateChat ? 1.0f : 0.2f;
        }

        /// <summary>
        /// Action called to enable smartphone main UI toggle
        /// </summary>
        /// <param name="enable"></param>
        public void EnableSmartphone(bool enable)
        {
            disturbToggleOnImage.color = CoreManager.Instance.chatSettings.busy;
        }

        public bool ChatIDExists(string chatID)
        {
            return m_messages.ContainsKey(chatID);
        }

        /// <summary>
        /// Action called to add a new chat to the smartphone
        /// </summary>
        /// <param name="player"></param>
        public void AddChatToSmartphone(IPlayer player)
        {
            string chatID = PlayerManager.Instance.GetPlayerName(player.NickName) + " [" + player.ActorNumber + "]";

            //create new chat message
            m_messages.Add(chatID, new SmartphoneChat(messagePrefab, container, localPlayer, otherPlayer, localSprite, remoteSprite));

            //add to the lookup table
            m_playerLookup.Add(chatID, player.ID);
        }

        /// <summary>
        /// Action called to remove existing chat from the smartphone
        /// </summary>
        /// <param name="player"></param>
        public void RemoveChatToSmartphone(IPlayer player)
        {
            string chatID = player.NickName + " [" + player.ActorNumber + "]";

            if(!string.IsNullOrEmpty(m_currentChat))
            {
                //remove player from chat system
                if (m_currentChat.Equals(chatID))
                {
                    m_messages[chatID].Clear();

                    //close if open
                    if (messageDisplay.activeInHierarchy)
                    {
                        SwitchChat("");
                    }
                }
            }

            if (!string.IsNullOrEmpty(m_currenCall))
            {
                //if player was connected to call, close
                if (m_currenCall.Equals(player.ID))
                {
                    if (phoneDisplay.activeInHierarchy)
                    {
                        EndPhoneCall();
                    }

                    m_currenCall = "";
                }
            }

            //remove
            m_playerLookup.Remove(chatID);
            m_messages.Remove(chatID);
        }

        /// <summary>
        /// Action called to switch the current chat using the player.usedID
        /// </summary>
        /// <param name="playerID"></param>
        public void SwitchChat(string playerID)
        {
            if (string.IsNullOrEmpty(playerID) && !string.IsNullOrEmpty(m_currentChat))
            {
                //destroy all objects in the chat
                m_messages[m_currentChat].StopService(this);
                m_messages[m_currentChat].Clear();

                //close message overlay
                messageOverlayError.GetComponentInChildren<TextMeshProUGUI>(true).text = "";
                messageOverlayError.SetActive(false);

                messageDisplay.SetActive(false);

                m_currentChat = "";

                return;
            }

            //stop the chat service
            if(!string.IsNullOrEmpty(m_currentChat))
            {
                m_messages[m_currentChat].StopService(this);
                m_messages[m_currentChat].Clear();
            }

            //get chatID from playerlookup
            string player = m_playerLookup.FirstOrDefault(x => x.Value.Equals(playerID)).Key;

            m_currentChat = player;

            if (!string.IsNullOrEmpty(m_currentChat))
            {
                headerPlayer.text = MMOManager.Instance.GetPlayerByUserID(playerID).NickName + "[" + MMOManager.Instance.GetPlayerByUserID(playerID).ActorNumber + "]";

                //create the chat service
                m_messages[m_currentChat].Create();
                messageDisplay.SetActive(true);
                m_messages[m_currentChat].StartService(this);

                //clear notifications
                MMOChat.Instance.RemoveNotification(m_currentChat);
            }
        }

        /// <summary>
        /// Action called to start a call with a player
        /// </summary>
        /// <param name="playerID"></param>
        /// <param name="isRequest"></param>
        public void SwitchCall(string playerID, bool isRequest)
        {
            //end if null
            if (string.IsNullOrEmpty(playerID))
            {
                phoneCall.EndPhoneCall();
                return;
            }

            m_currenCall = playerID;

            //set UI
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

            //open phone call
            phoneDisplay.SetActive(true);
        }


        /// <summary>
        /// Action called to start call
        /// </summary>
        public void StartPhoneCall()
        {
            if(!string.IsNullOrEmpty(m_currenCall))
            {
                phoneCall.StartPhoneCall();
            }
        }

        /// <summary>
        /// Action called to end a call
        /// </summary>
        public void EndPhoneCall()
        {
            if (!string.IsNullOrEmpty(m_currenCall))
            {
                phoneCall.EndPhoneCall();
            }
        }

        /// <summary>
        /// Action called to scroll to new chat message
        /// </summary>
        public void ViewNewChatMessages()
        {
            if (!string.IsNullOrEmpty(m_currentChat))
            {
                container.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 0;
                newMessageButton.SetActive(false);
            }
        }

        /// <summary>
        /// Action called to send a smartphone message out across the network
        /// </summary>
        public void SendMessage()
        {
            if (string.IsNullOrEmpty(dialogText.text)) return;

            string player = m_playerLookup.FirstOrDefault(x => x.Key.Equals(m_currentChat)).Value;

            IPlayer iPlayer = MMOManager.Instance.GetPlayerByUserID(player);

            //do not send if desitnation player should not be disturbed
            if (MMOManager.Instance.GetPlayerProperty(iPlayer, "DONOTDISTURB").Equals("1"))
            {
                //need to tell user to player is busy
                messageOverlayError.GetComponentInChildren<TextMeshProUGUI>(true).text = "CANNOT SEND MESSAGE " + m_currentChat + " DOES NOT WANT TO BE DISTURBED";
                messageOverlayError.SetActive(true);
            }
            else
            {
                MMOChat.Instance.SendChatMessage(player, dialogText.text);
            }

            dialogText.text = "";
        }

        /// <summary>
        /// Action called to set the local players disturbed state across the network
        /// </summary>
        /// <param name="state"></param>
        public void DoNotDisturb(bool state)
        {
            if (m_ignoreDisturbedState) return;

            PlayerManager.Instance.SetPlayerProperty("DONOTDISTURB", (state) ? "1" : "0");
        }

        /// <summary>
        /// Action called to open the settings on the smartphone
        /// </summary>
        public void OpenSettings()
        {
            m_ignoreDisturbedState = true;
            homescreenDisplay.SetActive(false);
            settingsDisplay.SetActive(true);

            //need to get the state of the player disturbed state;
            ExternDoNotDisturbToggleState(MMOManager.Instance.GetLocalPlayerProperties()["DONOTDISTURB"].Equals("1"));

            m_ignoreDisturbedState = false;
        }

        /// <summary>
        /// Action called to externally update the disturb toggle within the settings panel
        /// </summary>
        /// <param name="state"></param>
        public void ExternDoNotDisturbToggleState(bool state)
        {
            disturbedToggle.onValueChanged.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
            disturbedToggle.isOn = state;
            disturbedToggle.onValueChanged.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.RuntimeOnly);
        }

        /// <summary>
        /// Action called to close the settings UI
        /// </summary>
        public void CloseSettings()
        {
            if(smartphoneDisplay.activeInHierarchy)
            {
                settingsDisplay.SetActive(false);
                homescreenDisplay.SetActive(true);
            }
        }

        /// <summary>
        /// Action called to open the phone call UI
        /// </summary>
        public void OpenPhoneCall()
        {
            if (smartphoneDisplay.activeInHierarchy)
            {
                homescreenDisplay.SetActive(false);
                usersPhoneDisplay.SetActive(true);
            }
        }

        /// <summary>
        /// Action called to close the phone call UI
        /// </summary>
        public void ClosePhoneCall()
        {
            if (smartphoneDisplay.activeInHierarchy)
            {
                usersPhoneDisplay.SetActive(false);
                homescreenDisplay.SetActive(true);
            }
        }

        /// <summary>
        /// Acrion called to open the chat message UI
        /// </summary>
        public void OpenChatMessages()
        {
            if (smartphoneDisplay.activeInHierarchy)
            {
                homescreenDisplay.SetActive(false);
                usersMessageDisplay.SetActive(true);
            }
        }

        /// <summary>
        /// Action called to close the chat message UI
        /// </summary>
        public void CloseChatMessages()
        {
            if (smartphoneDisplay.activeInHierarchy)
            {
                usersMessageDisplay.SetActive(false);
                homescreenDisplay.SetActive(true);

                dialogText.text = "";
            }
        }

        /// <summary>
        /// Action called to add new message to a chat
        /// </summary>
        /// <param name="chatID"></param>
        /// <param name="message"></param>
        /// <param name="isLocal"></param>
        public void AddMessage(string chatID, string message, bool isLocal = false)
        {
            //adds new message
            m_messages[chatID].AddSilentMessage(message, maxCharPerLine, isLocal);

            //show new message button if user if not at the bottom of the scroll area
            if(chatID.Equals(m_currentChat) && messageDisplay.activeInHierarchy)
            {
                if (container.GetComponentInParent<ScrollRect>().verticalNormalizedPosition > 0.1f)
                {
                    newMessageButton.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Action called to show toast notrification
        /// </summary>
        /// <param name="playerID"></param>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        public void Toast(string playerID, string sender, string message)
        {
            if (m_currentChat.Equals(sender)) return;

            if(toast == null)
            {
                toast = HUDManager.Instance.GetHUDScreenObject("SMARTPHONETOAST_SCREEN").GetComponent<ToastPanel>();
            }

            if(!playerID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
            {
                toast.Add(ToastPanel.ToastType._Message, sender, message.Replace("\n", " "));
            }
        }

        /// <summary>
        /// Action called to toggle the smartphone on/off
        /// </summary>
        /// <param name="show"></param>
        public void ShowSmartphone(bool show)
        {
            smartphoneDisplay.SetActive(show);

            if(!show)
            {
                if (string.IsNullOrEmpty(m_currentChat))
                {
                    SwitchChat("");
                }
            }
        }

        [System.Serializable]
        protected class SmartphoneChat
        {
            public List<Message> messages = new List<Message>();
            public Color local;
            public Sprite localSprite;
            public Color remote;
            public Sprite remoteSprite;

            private GameObject prefab;
            private Transform container;

            private Queue<Message> newMessages = new Queue<Message>();
            private Coroutine task;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="prefab"></param>
            /// <param name="container"></param>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <param name="sA"></param>
            /// <param name="sB"></param>
            public SmartphoneChat(GameObject prefab, Transform container, Color a, Color b, Sprite sA, Sprite sB)
            {
                this.prefab = prefab;
                this.container = container;
                local = a;
                remote = b;
                localSprite = sA;
                remoteSprite = sB;
            }

            /// <summary>
            /// Action called to add message to this chat 
            /// </summary>
            /// <param name="message"></param>
            /// <param name="charPerLine"></param>
            /// <param name="local"></param>
            public void AddSilentMessage(string message, int charPerLine = 45, bool local = false)
            {
                Message m = new Message();
                m.chat = message;
                m.isLocal = local;

                newMessages.Enqueue(m);
            }

            /// <summary>
            /// Makes this chat live
            /// </summary>
            /// <returns></returns>
            private IEnumerator Service()
            {
                WaitForEndOfFrame wait = new WaitForEndOfFrame();

                while(true)
                {
                    //ensure all new messages are displayed
                    if(newMessages.Count > 0)
                    {
                        Message m = newMessages.Dequeue();
                        InstantiateMessage(m);
                        messages.Add(m);
                    }

                    yield return wait;
                }
            }

            /// <summary>
            /// Start this live service
            /// </summary>
            /// <param name="mono"></param>
            public void StartService(MonoBehaviour mono)
            {
                task = mono.StartCoroutine(Service());
            }

            /// <summary>
            /// Stop this live service
            /// </summary>
            /// <param name="mono"></param>
            public void StopService(MonoBehaviour mono)
            {
                if(task != null)
                {
                    mono.StopCoroutine(task);
                }
            }

            /// <summary>
            /// Creates are cached messages
            /// </summary>
            public void Create()
            {
                if (prefab != null && container != null)
                {
                    foreach(Message m in messages)
                    {
                        InstantiateMessage(m);
                    }
                }
            }

            /// <summary>
            /// Create single message
            /// </summary>
            /// <param name="m"></param>
            private void InstantiateMessage(Message m)
            {
                GameObject GO = Instantiate(prefab, Vector3.zero, Quaternion.identity, container);
                GO.transform.localScale = Vector3.one;
                GO.SetActive(true);
                m.GO = GO;

                AddMessage(m.chat, m);
            }

            /// <summary>
            /// Set up the message content per single message
            /// </summary>
            /// <param name="message"></param>
            /// <param name="m"></param>
            private void AddMessage(string message, Message m)
            {
                SmartphoneChatMessage sMessage = m.GO.GetComponentInChildren<SmartphoneChatMessage>(true);
                string[] split = message.Split('|');
                TextAnchor anchor = TextAnchor.UpperLeft;
                Color col = remote;
                Sprite sp = remoteSprite;

                if(m.isLocal)
                {
                    anchor = TextAnchor.UpperRight;
                    col = local;
                    sp = localSprite;
                }

                sMessage.Set(split[0], split[1], anchor, col, sp);
            }

            /// <summary>
            /// clear/destoy all chat messages
            /// </summary>
            public void Clear()
            {
                foreach (Message obj in messages)
                {
                    Destroy(obj.GO);
                }
            }

            /// <summary>
            /// Class for individual messages
            /// </summary>
            public class Message
            {
                public string chat;
                public bool isLocal;
                public GameObject GO;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PhotonChatSmartphone), true)]
        public class PhotonChatSmartphone_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("smartphoneDisplay"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("homescreenDisplay"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("usersPhoneDisplay"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("usersMessageDisplay"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settingsDisplay"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messageDisplay"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("phoneDisplay"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messageOption"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("headerPlayer"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxCharPerLine"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("localPlayer"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("localSprite"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("otherPlayer"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("remoteSprite"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("container"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messagePrefab"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dialogText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("newMessageButton"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messageOverlayError"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("disturbedToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("disturbToggleOnImage"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("phoneCall"), true);

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
