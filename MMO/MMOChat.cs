using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

#if BRANDLAB360_INTERNAL
using BrandLab360.Internal;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class MMOChat : Singleton<MMOChat>
    {
        public static MMOChat Instance
        {
            get
            {
                return ((MMOChat)instance);
            }
            set
            {
                instance = value;
            }
        }

#if BRANDLAB360_INTERNAL
        private PhotonChat m_photon;

        private PhotonChat M_PhotonChat
        {
            get
            {
                if(m_photon == null)
                {
                    m_photon = MMOManager.Instance.PhotonManager_GO.GetComponent<PhotonChat>();
                }

                return m_photon;
            }
        }

#endif

        private ColyseusChat m_colyseus;

        public ColyseusChat m_ColyseusChat
        {
            get
            {
                if(m_colyseus == null)
                {
                    m_colyseus = MMOManager.Instance.ColyseusManager_Ref.GetComponent<ColyseusChat>();
                }

                return m_colyseus;
            }
        }

        private PhotonChatBasic basicSystem;

        [Header("Notifications")]
        [SerializeField]
        private PhotonChatNotification basicNotification;

        private PhotonChatSmartphone smartphoneSystem;

        [SerializeField]
        private PhotonChatNotification smartphoneNotification;

        [Header("Message display")]
        [SerializeField]
        private GameObject chatToggle;

        private GameObject chatDisplay;

        private RectTransform chatMessageViewport;
        private RectTransform chatDisplayScrollbar;
        private TMP_InputField chatMessageDialog;

        private bool displayDate = false;
        private bool displayTime = true;

        [HideInInspector]
        [Header("Global Chat")]
        [SerializeField]
        private int maxCharPerLine = 45;

        private List<PhotonChatBasic.Chatstring> m_globalChat = new List<PhotonChatBasic.Chatstring>();
        private Dictionary<string, string> m_playerLookup = new Dictionary<string, string>();
        private int m_globalChatIndex = 0;

        /// <summary>
        /// Action to subscribe for notifications
        /// </summary>
        public System.Action<string> OnNotify { get; set; }

        public bool OnCall
        {
            get;
            private set;
        }

        public List<PhotonChatBasic.Chatstring> Globalchat
        {
            get
            {
                return m_globalChat;
            }
        }

        public string CurrentChatID
        {
            get
            {
                if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
                {
                    return basicSystem.CurrentChat;
                }
                else
                {
                    return smartphoneSystem.CurrentChat;
                }
            }
        }

        public PhotonChatBasic BasicChat
        {
            get
            {
                return basicSystem;
            }
        }

        public PhotonChatSmartphone SmartphoneChat
        {
            get
            {
                return smartphoneSystem;
            }
        }

        public int MaxcharPerLine
        {
            get
            {
                return maxCharPerLine;
            }
        }

        public Dictionary<string, string> PlayerLookup
        {
            get
            {
                return m_playerLookup;
            }
        }

        /// <summary>
        /// Global access to the current call open
        /// </summary>
        public string CurrentCallID
        {
            get
            {
                if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
                {
                    return basicSystem.CurrentCall;
                }
                else
                {
                    return smartphoneSystem.CurrentCall;
                }
            }
        }

        /// <summary>
        /// Returns is the chat is open/visible
        /// </summary>
        public bool IsOpen
        {
            get
            {
                if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
                {
                    return chatToggle.GetComponent<UnityEngine.UI.Toggle>().isOn;
                }
                else
                {
                    return smartphoneSystem.IsOpen;
                }
            }
        }

        public bool GlobalChatOpen
        {
            get
            {
                return chatToggle.GetComponent<UnityEngine.UI.Toggle>().isOn;
            }
            set
            {
                chatToggle.GetComponent<UnityEngine.UI.Toggle>().isOn = value;
            }
        }

        public bool GlobalUnreadMessages
        {
            get
            {
                return basicNotification.UnreadMessages;
            }
        }

        private void Awake()
        {
            HUDManager.Instance.OnCustomSetupComplete += GetUIReferences;
        }

        private void GetUIReferences()
        {
            HUDManager.Instance.OnCustomSetupComplete -= GetUIReferences;


            chatToggle = HUDManager.Instance.GetMenuFeature(HUDManager.MenuFeature._Chat).gameObject;

            basicNotification = chatToggle.GetComponentInChildren<PhotonChatNotification>(true);
            smartphoneNotification = HUDManager.Instance.GetMenuFeature(HUDManager.MenuFeature._Phone).gameObject.GetComponentInChildren<PhotonChatNotification>(true);


            chatDisplay = HUDManager.Instance.GetHUDScreenObject("CHAT_SCREEN");
            chatMessageViewport = chatDisplay.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
            chatDisplayScrollbar = chatDisplay.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>();
            chatMessageDialog = chatDisplay.transform.GetChild(0).GetChild(3).GetComponentInChildren<TMP_InputField>(true);

            smartphoneSystem = HUDManager.Instance.GetHUDScreenObject("SMARTPHONE_SCREEN").GetComponentInChildren<PhotonChatSmartphone>(true);
            basicSystem = chatDisplay.GetComponentInChildren<PhotonChatBasic>(true);

            Begin();
        }

        private void Begin()
        {
            chatDisplay.SetActive(false);

            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online))
            {
                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
#if BRANDLAB360_INTERNAL
                    if (M_PhotonChat != null)
                    {
                        M_PhotonChat.Begin(CoreManager.Instance.ProjectID, CoreManager.Instance.RoomID, AppManager.Instance.Settings.chatSettings.useGlobalChat);
                        M_PhotonChat.Callback_OnGetMessages += OnGetMessages;
                        M_PhotonChat.Callback_OnPrivateMessage += OnPrivateMessage;
                        M_PhotonChat.Callback_OnSubscribed += OnChatSubscribed;
                    }
#endif

                }
                else
                {
                    if (m_ColyseusChat != null)
                    {
                        m_ColyseusChat.Begin(CoreManager.Instance.ProjectID, CoreManager.Instance.RoomID, AppManager.Instance.Settings.chatSettings.useGlobalChat);
                        m_ColyseusChat.Callback_OnGetMessages += OnGetMessages;
                        m_ColyseusChat.Callback_OnPrivateMessage += OnPrivateMessage;
                        m_ColyseusChat.Callback_OnSubscribed += OnChatSubscribed;
                    }
                }


                if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
                {
                    //show the dropdown
                    basicSystem.EnableDropdown(false);

                    //hide the smartphone toggle
                    smartphoneSystem.EnableSmartphone(false);

                    if (CoreManager.Instance.chatSettings.useGlobalChat)
                    {
                        basicSystem.DropDown.options = new List<TMP_Dropdown.OptionData>();
                        basicSystem.AddChatToDropdown(null);
                        basicSystem.DropDown.value = 0;
                    }
                    else
                    {
                        basicSystem.DropDown.options = new List<TMP_Dropdown.OptionData>();
                    }
                }
                else
                {
                    //resixe the chatDisplayViewport
                  //  chatMessageViewport.offsetMin = new Vector2(chatMessageViewport.offsetMin.x, chatMessageViewport.offsetMin.y - (basicSystem.DropDown.GetComponent<RectTransform>().sizeDelta.y + 10));
                  //  chatDisplayScrollbar.offsetMin = new Vector2(chatDisplayScrollbar.offsetMin.x, chatDisplayScrollbar.offsetMin.y - (basicSystem.DropDown.GetComponent<RectTransform>().sizeDelta.y + 10));

                    //hide the dropdown
                    basicSystem.EnableDropdown(false);

                    //true the smartphone toggle
                    smartphoneSystem.EnableSmartphone(true);
                }

                displayDate = CoreManager.Instance.chatSettings.displayDate;
                displayTime = CoreManager.Instance.chatSettings.displayTime;

                //subscrito to th photon room system for player connection states
                MMORoom.Instance.OnPlayerEnteredRoom += OnPlayerEntered;
                MMORoom.Instance.OnPlayerLeftRoom += OnPlayerLeft;
            }
        }

        public void Disconnect()
        {
            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online))
            {
                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
#if BRANDLAB360_INTERNAL
                    if (M_PhotonChat != null)
                    {
                        M_PhotonChat.Disconnect();
                    }
#endif
                }
                else
                {
                    if (m_ColyseusChat != null)
                    {
                        m_ColyseusChat.Disconnect();
                    }
                }
            }

            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online))
            {
                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
#if BRANDLAB360_INTERNAL
                    if (M_PhotonChat != null)
                    {
                        M_PhotonChat.Callback_OnGetMessages -= OnGetMessages;
                        M_PhotonChat.Callback_OnPrivateMessage -= OnPrivateMessage;
                        M_PhotonChat.Callback_OnSubscribed -= OnChatSubscribed;
                    }
#endif

                }
                else
                {
                    if (m_ColyseusChat != null)
                    {
                        m_ColyseusChat.Callback_OnGetMessages -= OnGetMessages;
                        m_ColyseusChat.Callback_OnPrivateMessage -= OnPrivateMessage;
                        m_ColyseusChat.Callback_OnSubscribed -= OnChatSubscribed;
                    }
                }

                if(FindFirstObjectByType<MMORoom>() != null)
                {
                    //unsubscrito to th photon room system for player connection states
                    MMORoom.Instance.OnPlayerEnteredRoom -= OnPlayerEntered;
                    MMORoom.Instance.OnPlayerLeftRoom -= OnPlayerLeft;
                }
            }
        }

        /// <summary>
        /// Return the unique player ID of the player from the chatID
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public string GetPlayerIDFromChat(string chatID)
        {
            return m_playerLookup.FirstOrDefault(x => x.Key.Equals(chatID)).Value;
        }

        /// <summary>
        /// Return the unique chat ID of the player from the player
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public string GetChatIDFromPlayer(string playerID)
        {
            return m_playerLookup.FirstOrDefault(x => x.Value.Equals(playerID)).Key;
        }

        /// <summary>
        /// Action to call to display the chat box
        /// </summary>
        /// <param name="show"></param>
        public void ShowChatBox(bool show)
        {
            chatDisplay.SetActive(show);

            if (show)
            {
                if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
                {
                    basicNotification.OnChatOpen(basicSystem.CurrentChat, basicSystem.IsCurrentChatOpen());
                }
                else
                {
                    basicNotification.RemoveUnreadMessage("All");
                }
            }
        }

        /// <summary>
        /// Called to hide the chat UI
        /// </summary>
        public void HideChat()
        {
            if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
            {
                if (!CoreManager.Instance.playerSettings.enableGlobalChatWhilstInChair)
                {
                    chatToggle.GetComponent<UnityEngine.UI.Toggle>().isOn = false;
                }
            }
            else
            {
                if (smartphoneSystem.IsOpen)
                {
                    smartphoneSystem.IsOpen = false;
                }

                if (!CoreManager.Instance.playerSettings.enableGlobalChatWhilstInChair)
                {
                    chatToggle.GetComponent<UnityEngine.UI.Toggle>().isOn = false;
                }
            }
        }

        /// <summary>
        /// Action called to remove a chatID from the main notification thread
        /// </summary>
        /// <param name="chatID"></param>
        public void RemoveNotification(string chatID)
        {
            if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
            {
                basicNotification.RemoveUnreadMessage(chatID);
            }
            else
            {
                smartphoneNotification.RemoveUnreadMessage(chatID);
            }
        }

        /// <summary>
        /// Action called to send a chat message via the chat display to the current chat open, this uses the input box
        /// </summary>
        public void SendChatMessage()
        {
            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online))
            {
                if (string.IsNullOrEmpty(chatMessageDialog.text)) return;

                if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
                {
                    basicSystem.SendChatMessage(chatMessageDialog.text);
                }
                else
                {
                    SendChatMessage("All", chatMessageDialog.text);
                }

                chatMessageDialog.text = "";
            }
        }

        /// <summary>
        /// Action called to send a chat message, global chat use All, private chat use playerID
        /// </summary>
        /// <param name="message"></param>
        public void SendChatMessage(string id, string message)
        {
            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online))
            {
                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
#if BRANDLAB360_INTERNAL
                    if (M_PhotonChat != null)
                    {
                        M_PhotonChat.SendChatMessage(id, message);
                    }
#endif

                }
                else
                {
                    if (m_ColyseusChat != null)
                    {
                        m_ColyseusChat.SendChatMessage(id, message);
                    }
                }
            }
        }

        /// <summary>
        /// Action called when a player is joins the room
        /// </summary>
        public void OnPlayerJoined()
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (M_PhotonChat != null)
                {
                    M_PhotonChat.OnPlayerJoined();
                }
#endif
            }
            else
            {
                if (m_ColyseusChat != null)
                {
                    m_ColyseusChat.OnPlayerJoined();
                }
            }
        }

        /// <summary>
        /// Callback for when a player enters the room
        /// </summary>
        /// <param name="player"></param>
        public void OnPlayerEntered(IPlayer player)
        {
            string chatID = PlayerManager.Instance.GetPlayerName(player.NickName) + " [" + player.ActorNumber + "]";

            Debug.Log("Creating Chat for new player:" + chatID);

            if(CoreManager.Instance.chatSettings.usePrivateChat)
            {
                if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
                {
                    if (basicSystem.ChatIDExists(chatID)) return;

                    bool firstPlayer = false;

                    if (!CoreManager.Instance.chatSettings.useGlobalChat)
                    {
                        if (basicSystem.DropDown.options.Count <= 0)
                        {
                            firstPlayer = true;
                        }
                    }

                    basicSystem.AddChatToDropdown(player);

                    if (firstPlayer)
                    {
                        basicSystem.SwitchChat(player.ID);
                    }
                }
                else
                {
                    if (smartphoneSystem.ChatIDExists(chatID)) return;

                    smartphoneSystem.AddChatToSmartphone(player);
                }
            }

            if(!m_playerLookup.ContainsKey(chatID))
            {
                m_playerLookup.Add(chatID, player.ID);
            }

            if (CoreManager.Instance.chatSettings.useGlobalChat)
            {
                AddMessage("", chatID, "All", "Server: " + chatID + " Joined Room", false);
            }
        }

        /// <summary>
        /// Callback for when a player leaves a the room
        /// </summary>
        /// <param name="player"></param>
        private void OnPlayerLeft(IPlayer player)
        {
            string chatID = PlayerManager.Instance.GetPlayerName(player.NickName) + " [" + player.ActorNumber + "]";

            Debug.Log("Deleting Chat for player:" + chatID);

            if (CoreManager.Instance.chatSettings.usePrivateChat)
            {
                if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
                {
                    basicSystem.RemoveChatFromDropdown(player);
                }
                else
                {
                    smartphoneSystem.RemoveChatToSmartphone(player);
                }
            }

            //local look up
            if (m_playerLookup.ContainsKey(chatID))
            {
                m_playerLookup.Remove(chatID);
            }

            //add to chat
            if (CoreManager.Instance.chatSettings.useGlobalChat)
            {
                AddMessage("", chatID, "All", "Server: " + chatID + " Left Room", false);
            }
        }

        /// <summary>
        /// Action to call to switch the chat display using a raw index value within the dropdown options
        /// </summary>
        /// <param name="index"></param>
        public void SwitchChat(int index)
        {
            if (index < 0) return;

            Debug.Log("Switching Chat");

            if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
            {
                chatToggle.GetComponent<UnityEngine.UI.Toggle>().isOn = true;

                if (basicSystem.CurrentChat.Equals("All"))
                {
                    m_globalChat.ForEach(x => Destroy(x.GO));
                    basicSystem.SwitchChat(index);
                }
                else
                {
                    basicSystem.SwitchChat(index);
                    m_globalChat.ForEach(x => x.Create());
                }

                basicNotification.RemoveUnreadMessage(basicSystem.CurrentChat);
            }
            else
            {

            }
        }

        /// <summary>
        /// Action to call to switch the chat display using a a players photon ID
        /// </summary>
        /// <param name="playerID"></param>
        public void SwitchChat(string playerID)
        {
            if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
            {
                chatToggle.GetComponent<UnityEngine.UI.Toggle>().isOn = true;

                basicSystem.SwitchChat(playerID);
            }
            else
            {
                if (!smartphoneSystem.IsOpen)
                {
                    smartphoneSystem.IsOpen = true;
                }

                smartphoneSystem.OpenChatMessages();
                smartphoneSystem.SwitchChat(playerID);
            }
        }

        /// <summary>
        /// Action to call to switch and call player
        /// </summary>
        /// <param name="playerID"></param>
        /// <param name="isReciever"></param>
        public void SwitchCall(string playerID, bool isReciever)
        {
            if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
            {
                basicSystem.SwitchCall(playerID, isReciever);
            }
            else
            {
                if (!smartphoneSystem.IsOpen)
                {
                    smartphoneSystem.IsOpen = true;
                }

                smartphoneSystem.SwitchCall(playerID, isReciever);
            }
        }

        public void SetDisturbedMode(bool state)
        {
            if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
            {
                PlayerManager.Instance.SetPlayerProperty("DONOTDISTURB", (state) ? "1" : "0");
            }
            else
            {
                smartphoneSystem.ExternDoNotDisturbToggleState(state);
                smartphoneSystem.DoNotDisturb(state);
            }
        }

        /// <summary>
        /// Action called to add to a chat channel
        /// </summary>
        /// <param name="chat"></param>
        /// <param name="str"></param>
        public void AddMessage(string playerID, string owner, string chatID, string message, bool playerMessage = true)
        {
            //add date and time if applicable
            string date = "";
            string time = "";

            Debug.Log("New Message added to Chat:" + chatID);

            if (displayDate)
            {
                date = System.DateTime.Now.ToString("MM/dd/yyyy") + " ";
            }

            if (displayTime)
            {
                time = System.DateTime.Now.ToString("HH:mm") + " ";
            }

            //combine string
            string add = "";
            string anchor = "<align=left>";

            if (playerID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
            {
                add = anchor + date + time + owner + System.Environment.NewLine;
                add += anchor + message + System.Environment.NewLine;
            }
            else
            {
                add = anchor + date + time;

                if (!playerMessage)
                {
                    add += anchor + System.Environment.NewLine + message + System.Environment.NewLine;
                }
                else
                {
                    add += owner + System.Environment.NewLine + message + System.Environment.NewLine;
                }
            }

            if (chatID.Equals("All"))
            {
                if(m_globalChat.Count == 0)
                {
                    m_globalChat.Add(basicSystem.CreateNewText(maxCharPerLine));
                    m_globalChat[m_globalChatIndex].AddMessage(add);
                }
                else if (!m_globalChat[m_globalChatIndex].AddMessage(add))
                {
                    m_globalChat.Add(basicSystem.CreateNewText(maxCharPerLine));
                    m_globalChatIndex++;
                    m_globalChat[m_globalChatIndex].AddMessage(add);
                }

                //notification for global uses basic
                if (!chatID.Equals(basicSystem.CurrentChat) || !chatDisplay.activeInHierarchy)
                {
                    basicNotification.AddUnreadMessage(chatID);
                }
            }
            else
            {
                if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
                {
                    basicSystem.AddMessage(chatID, add);

                    //notification
                    if (!chatID.Equals(basicSystem.CurrentChat) || !chatDisplay.activeInHierarchy)
                    {
                        basicNotification.AddUnreadMessage(chatID);
                    }
                }
                else
                {
                    add = date + time + owner + "|" + message;
                    smartphoneSystem.AddMessage(chatID, add, (playerID.Equals(PlayerManager.Instance.GetLocalPlayer().ID)) ? true : false);

                    if (!chatID.Equals(smartphoneSystem.CurrentChat))
                    {
                        smartphoneNotification.AddUnreadMessage(chatID);
                        smartphoneSystem.Toast(playerID, chatID, message);
                    }
                }
            }

            //invoke subcribed notifications
            if (OnNotify != null)
            {
                OnNotify.Invoke(chatID);
            }
        }

        /// <summary>
        /// Action called to post to the networked players chat display
        /// </summary>
        /// <param name="playerID"></param>
        /// <param name="message"></param>
        public void PostToNetworkedPlayer(string playerID, string message)
        {
            if (CoreManager.Instance.chatSettings.usePlayerChat && !playerID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
            {
                IPlayer player = PlayerManager.Instance.GetPlayer(playerID);

                if(player != null)
                {
                    player.MainObject.GetComponent<MMOPlayer>().RecieveChatMessage(message);
                }
            }
        }

        /// <summary>
        /// Returns whether a player has unread messages, uses the raw chatID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool PlayerHasUnreadMessage(string chatID)
        {
            if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
            {
                return basicNotification.PlayerHasUnreadMessages(chatID);
            }
            else
            {
                return smartphoneNotification.PlayerHasUnreadMessages(chatID);
            }
        }

        public void DialVoiceCall(string playerID, bool sendRPC = true)
        {
            Debug.Log("Dialing phone call: " + playerID);

            if (sendRPC)
            {
                MMOManager.Instance.SendRPC("SendVoiceCallRequest", (int)MMOManager.RpcTarget.AllBufferedViaServer, PlayerManager.Instance.GetLocalPlayer().ID, playerID);
            }

            SwitchCall(playerID, false);
        }

        public void AcceptVoiceCall(string playerID, bool sendRPC = true)
        {
            Debug.Log("Call accepted: " + playerID);

            OnCall = true;

            ToggleAudioChatMessage webRequest = new ToggleAudioChatMessage();

            //get the player photon view ID
            int actorNumber = PlayerManager.Instance.GetPlayer(playerID).ActorNumber;

            if (sendRPC)
            {
                MMOManager.Instance.SendRPC("VoiceCallAccepted", (int)MMOManager.RpcTarget.AllBufferedViaServer, PlayerManager.Instance.GetLocalPlayer().ID, playerID);

                webRequest.ToggleAudioChat = true;
                webRequest.Channel = CoreManager.Instance.ProjectID + "_voip_" + actorNumber.ToString() + "_" + PlayerManager.Instance.GetLocalPlayer().ActorNumber.ToString();
                webRequest.Username = PlayerManager.Instance.GetPlayerName(PlayerManager.Instance.GetPlayer(PlayerManager.Instance.GetLocalPlayer().ID).NickName);
            }
            else
            {
                webRequest.ToggleAudioChat = true;
                webRequest.Channel = CoreManager.Instance.ProjectID + "_voip_" + PlayerManager.Instance.GetLocalPlayer().ActorNumber.ToString() + "_" + actorNumber.ToString();
                webRequest.Username = PlayerManager.Instance.GetPlayerName(PlayerManager.Instance.GetPlayer(PlayerManager.Instance.GetLocalPlayer().ID).NickName);
            }

            WebclientManager.Instance.Send(JsonUtility.ToJson(webRequest));

            if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
            {
                basicSystem.StartPhoneCall();
            }
            else
            {
                smartphoneSystem.StartPhoneCall();
            }
        }

        public void JoinVoiceArea(string voiceChatId)
        {
            ToggleAudioChatMessage webRequest = new ToggleAudioChatMessage();

            webRequest.ToggleAudioChat = true;
            webRequest.Channel = voiceChatId;
            webRequest.Username = PlayerManager.Instance.GetPlayerName(PlayerManager.Instance.GetPlayer(PlayerManager.Instance.GetLocalPlayer().ID).NickName);

            WebclientManager.Instance.Send(JsonUtility.ToJson(webRequest));

            basicSystem.StartPhoneCall();

            OnCall = true;

            Debug.Log($"JOINED VOICE CHAT ({voiceChatId})");
        }

        public void JoinVoiceAreaTrigger(string voiceChatId)
        {
            ToggleAudioChatMessage webRequest = new ToggleAudioChatMessage();

            webRequest.ToggleAudioChat = true;
            webRequest.Channel = voiceChatId;
            webRequest.Username = PlayerManager.Instance.GetPlayerName(PlayerManager.Instance.GetPlayer(PlayerManager.Instance.GetLocalPlayer().ID).NickName);

            WebclientManager.Instance.Send(JsonUtility.ToJson(webRequest));
            Debug.Log($"JOINED VOICE CHAT ({voiceChatId})");
        }

        public void LeaveVoiceArea(string voiceChatId)
        {
            ToggleAudioChatMessage webRequest = new ToggleAudioChatMessage();

            webRequest.ToggleAudioChat = false;
            webRequest.Channel = voiceChatId;
            webRequest.Username = PlayerManager.Instance.GetPlayerName(PlayerManager.Instance.GetPlayer(PlayerManager.Instance.GetLocalPlayer().ID).NickName);

            WebclientManager.Instance.Send(JsonUtility.ToJson(webRequest));

            basicSystem.EndPhoneCall();

            OnCall = false;

            Debug.Log($"LEFT VOICE CHAT ({voiceChatId})");
        }

        public void LeaveVoiceAreaTrigger(string voiceChatId)
        {
            ToggleAudioChatMessage webRequest = new ToggleAudioChatMessage();

            webRequest.ToggleAudioChat = false;
            webRequest.Channel = voiceChatId;
            webRequest.Username = PlayerManager.Instance.GetPlayerName(PlayerManager.Instance.GetPlayer(PlayerManager.Instance.GetLocalPlayer().ID).NickName);

            WebclientManager.Instance.Send(JsonUtility.ToJson(webRequest));
            Debug.Log($"LEFT VOICE CHAT ({voiceChatId})");
        }

        public void DeclineVoiceCall(string playerID, bool sendRPC = true)
        {
            Debug.Log("Call declined: " + playerID);

            OnCall = false;

            if (sendRPC)
            {
                MMOManager.Instance.SendRPC("VoiceCallDeclined", (int)MMOManager.RpcTarget.AllBufferedViaServer, PlayerManager.Instance.GetLocalPlayer().ID, playerID);
            }

            if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
            {
                basicSystem.EndPhoneCall();
            }
            else
            {
                smartphoneSystem.EndPhoneCall();
            }
        }

        public void EndVoiceCall(string playerID, bool sendRPC = true)
        {
            Debug.Log("Call ended: " + playerID);

            OnCall = false;

            ToggleAudioChatMessage webRequest = new ToggleAudioChatMessage();

            //get the player photon view ID
            int actorNumber = PlayerManager.Instance.GetPlayer(playerID).ActorNumber;

            if (sendRPC)
            {
                MMOManager.Instance.SendRPC("VoiceCallEnded", (int)MMOManager.RpcTarget.AllBufferedViaServer, PlayerManager.Instance.GetLocalPlayer().ID, playerID);

                webRequest.ToggleAudioChat = false;
                webRequest.Channel = CoreManager.Instance.ProjectID + "_voip_" + actorNumber.ToString() + "_" + PlayerManager.Instance.GetLocalPlayer().ActorNumber.ToString();
                webRequest.Username = PlayerManager.Instance.GetPlayerName(PlayerManager.Instance.GetPlayer(PlayerManager.Instance.GetLocalPlayer().ID).NickName);
            }
            else
            {
                webRequest.ToggleAudioChat = false;
                webRequest.Channel = CoreManager.Instance.ProjectID + "_voip_" + PlayerManager.Instance.GetLocalPlayer().ActorNumber.ToString() + "_" + actorNumber.ToString();
                webRequest.Username = PlayerManager.Instance.GetPlayerName(PlayerManager.Instance.GetPlayer(PlayerManager.Instance.GetLocalPlayer().ID).NickName);
            }

            WebclientManager.Instance.Send(JsonUtility.ToJson(webRequest));

            if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
            {
                basicSystem.EndPhoneCall();
            }
            else
            {
                smartphoneSystem.EndPhoneCall();
            }
        }

        #region PhotonCallbacks
        /// <summary>
        /// Notifies app that client got new messages from server
        /// Number of senders is equal to number of messages in 'messages'. Sender with number '0' corresponds to message with
        /// number '0', sender with number '1' corresponds to message with number '1' and so on
        /// </summary>
        /// <param name="channelName">channel from where messages came</param>
        /// <param name="senders">list of users who sent messages</param>
        /// <param name="messages">list of messages it self</param>
        public void OnGetMessages(string channelName, string[] senders, object[] messages)
        {
            for (int i = 0; i < messages.Length; i++)
            {
                string player = PlayerLookup.FirstOrDefault(x => x.Value.Equals(senders[i])).Key;

                if (senders[i].Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                {
                    player = "[You]";
                }

                AddMessage(senders[i], player, "All", messages[i].ToString());

                if (!messages[i].ToString().Contains("#EVT#"))
                {
                    PostToNetworkedPlayer(senders[i], messages[i].ToString());
                }
            }
        }

        /// <summary>
        /// Notifies client about private message
        /// </summary>
        /// <param name="sender">user who sent this message</param>
        /// <param name="message">message it self</param>
        /// <param name="channelName">channelName for private messages (messages you sent yourself get added to a channel per target username)</param>
        public void OnPrivateMessage(string sender, object message, string channelName)
        {
            string player = PlayerLookup.FirstOrDefault(x => x.Value.Equals(sender)).Key;
            string id = player;

            if (sender.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
            {
                id = "[You]";

                if (CoreManager.Instance.chatSettings.privateChat.Equals(ChatSystem.Basic))
                {
                    player = BasicChat.CurrentChat;
                }
                else
                {
                    player = MMOChat.Instance.SmartphoneChat.CurrentChat;
                }
            }

            AddMessage(sender, id, player, message.ToString());
        }

        /// <summary>
        /// Result of Subscribe operation. Returns subscription result for every requested channel name.
        /// </summary>
        /// <remarks>
        /// If multiple channels sent in Subscribe operation, OnSubscribed may be called several times, each call with part of sent array or with single channel in "channels" parameter.
        /// Calls order and order of channels in "channels" parameter may differ from order of channels in "channels" parameter of Subscribe operation.
        /// </remarks>
        /// <param name="channels">Array of channel names.</param>
        /// <param name="results">Per channel result if subscribed.</param>
        public void OnChatSubscribed(string[] channels, bool[] results)
        {
            Globalchat.Add(BasicChat.CreateNewText(MaxcharPerLine));

            foreach (var player in MMOManager.Instance.GetAllPlayers())
            {
                if (player.IsLocal)
                {
                    if (CoreManager.Instance.chatSettings.useGlobalChat)
                    {
                        string pRef = PlayerManager.Instance.GetPlayerName(player.NickName) + " [" + player.ActorNumber + "]";
                        AddMessage("", pRef, "All", pRef + ": Joined Room", false);
                    }

                    continue;
                }

               OnPlayerEntered(PlayerManager.Instance.GetPlayer(player.ID));
            }
        }
        #endregion

#if UNITY_EDITOR
        [CustomEditor(typeof(MMOChat), true)]
        public class MMOChat_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
            }
        }
#endif
    }

}