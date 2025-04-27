using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

#if BRANDLAB360_AVATARS_READYPLAYERME
using BrandLab360.ReadyPlayerMe;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
   // [RequireComponent(typeof(Button))]
    public class GlobalUserListButton : MonoBehaviour, IUserList, IFriend
    {
        [Header("Profile")]
        [SerializeField]
        private TMPro.TextMeshProUGUI nameDisplay;

        [SerializeField]
        private Image status;

        [SerializeField]
        private GameObject options;

        [SerializeField]
        private GameObject[] disturbIndicators;

        [Header("Profile Image")]
        [SerializeField]
        private Transform emptyImage;
        [SerializeField]
        private RawImage profileImage;

        [Header("Notification")]
        [SerializeField]
        private PhotonChatPlayerNotification privateChatNofication;

        [Header("Options Interaction")]
        [SerializeField]
        private CanvasGroup phoneInteraction;
        [SerializeField]
        private CanvasGroup messageInteraction;
        [SerializeField]
        private CanvasGroup teleportInteraction;
        [SerializeField]
        private CanvasGroup followInteraction;

        [Header("Mobile Layout")]
        [SerializeField]
        private RectTransform friendsViewport;
        [SerializeField]
        private RectTransform gameViewport;
        [SerializeField]
        private RectTransform commsViewport;

        private string m_playerID;
        private IPlayer m_player;
        private string m_listID;
        private IPlayer m_teleportTo;

        private UserList m_handler;
        private bool m_donotdistrubMode = false;
        private bool m_loadedTexture;
        private bool m_isWebRequest = false;

        private RectTransform m_rectT;

        private OrientationType m_switch = OrientationType.landscape;
        private bool m_loaded = false;
        private float m_playerFontSize;
        private Vector2 m_profileSize;
        private LayoutElement m_profileLayout;

        /// <summary>
        /// Access to the active state of this button
        /// </summary>
        public bool IsActive
        {
            get;
            private set;
        }

        public string Friend_ID
        {
            get
            {
                return m_player.NickName;
            }
        }

        public System.Action OnThisUpdate { get; set; }

        /// <summary>
        /// Access to the owner of this button
        /// </summary>
        public string Owner
        {
            get
            {
                return m_playerID;
            }
        }

        private void Awake()
        {
            IsActive = false;

            //interaction
            phoneInteraction.alpha = 1.0f;
            phoneInteraction.interactable = true;

            messageInteraction.alpha = 1.0f;
            messageInteraction.interactable = true;

            teleportInteraction.alpha = 1.0f;
            teleportInteraction.interactable = true;

            followInteraction.alpha = 1.0f;
            followInteraction.interactable = true;

            m_rectT = GetComponent<RectTransform>();

            for (int i = 0; i < disturbIndicators.Length; i++)
            {
                disturbIndicators[i].GetComponentInChildren<Image>(true).color = CoreManager.Instance.chatSettings.busy;
                disturbIndicators[i].SetActive(false);
            }

            Toggle(false);
        }

        private void OnUpdate()
        {
            if(OnThisUpdate != null)
            {
                OnThisUpdate.Invoke();
            }

            //need to see if the object is within viewport
            if (m_rectT)
            {
                if (CoreUtilities.IsRectTransformCulled(m_rectT))
                {
                    if (!m_loadedTexture)
                    {
                        m_loadedTexture = true;

                        if (m_player.IsLocal)
                        {
                            if(AppManager.Instance.Data.LoginProfileData != null)
                            {
                                StartCoroutine(LoadImage(AppManager.Instance.Data.LoginProfileData.picture_url));
                            }
                            else
                            {
                                StartCoroutine(LoadImage(AppManager.Instance.Data.FixedAvatarName));
                            }
                        }
                        else
                        {
                            IPlayer iPlayer = PlayerManager.Instance.GetPlayer(m_player.ID);

                            if (iPlayer != null)
                            {
                                if (iPlayer.CustomizationData.ContainsKey("PROFILE_PICTURE"))
                                {
                                    StartCoroutine(LoadImage(iPlayer.CustomizationData["PROFILE_PICTURE"].ToString()));
                                }
                                else if (iPlayer.CustomizationData.ContainsKey("FIXEDAVATAR"))
                                {
                                    StartCoroutine(LoadImage(iPlayer.CustomizationData["FIXEDAVATAR"].ToString()));
                                }
                                else
                                {
                                    if(iPlayer.Avatar != null)
                                    {
                                        StartCoroutine(LoadImage(iPlayer.Avatar.name));
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (m_loadedTexture)
                    {
                        StopAllCoroutines();

                        if (m_loadedTexture && m_isWebRequest)
                        {
                            Destroy(profileImage.texture);
                        }

                        m_isWebRequest = false;
                        m_loadedTexture = false;
                        profileImage.texture = null;
                        profileImage.transform.localScale = Vector3.zero;
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (m_loadedTexture)
            {
                StopAllCoroutines();

                if (m_loadedTexture && m_isWebRequest)
                {
                    Destroy(profileImage.texture);
                }

                m_loadedTexture = false;
                profileImage.texture = null;
                profileImage.transform.localScale = Vector3.zero;
            }
        }

        /// <summary>
        /// Called to repaint this objects UI
        /// </summary>
        public void Repaint()
        {
            OnUpdate();

            //handle status
            if(ChairManager.Instance.HasPlayerOccupiedChair(m_playerID))
            {
                if(options.activeInHierarchy)
                {
                    if (m_handler != null)
                    {
                        m_handler.SetCurrentInterface(this);
                    }

                    Toggle(false);
                }
            }

            status.color = PlayerManager.Instance.GetPlayerStatus(m_playerID);
            bool busy = MMOManager.Instance.IsPlayerBusy(m_playerID);

            if(m_player != null)
            {
                if (MMOManager.Instance.PlayerHasProperty(m_player, "DONOTDISTURB"))
                {
                    m_donotdistrubMode = (MMOManager.Instance.GetPlayerProperty(m_player, "DONOTDISTURB").Equals("1") ? true : false);

                    for (int i = 0; i < disturbIndicators.Length; i++)
                    {
                        disturbIndicators[i].SetActive(m_donotdistrubMode);
                    }
                }


                phoneInteraction.alpha =  (busy) ? 0.1f : (!m_donotdistrubMode) ? 1.0f : 0.1f;
                phoneInteraction.interactable = (busy) ? false : (!m_donotdistrubMode) ? true : false;

                messageInteraction.alpha = (!m_donotdistrubMode) ? 1.0f : 0.1f;
                messageInteraction.interactable = (!m_donotdistrubMode) ? true : false;

                teleportInteraction.alpha = (busy) ? 0.1f : (!m_donotdistrubMode) ? 1.0f : 0.1f;
                teleportInteraction.interactable = (busy) ? false : (!m_donotdistrubMode) ? true : false;

                followInteraction.alpha = (busy) ? 0.1f : (!m_donotdistrubMode) ? 1.0f : 0.1f;
                followInteraction.interactable = (busy) ? false : (!m_donotdistrubMode) ? true : false;
            }

            if (AppManager.Instance.Data.IsMobile && m_loaded && !m_switch.Equals(OrientationManager.Instance.CurrentOrientation))
            {
                m_switch = OrientationManager.Instance.CurrentOrientation;
                float aspect = OrientationManager.Instance.ScreenSize.y / OrientationManager.Instance.ScreenSize.x;

                if (m_switch.Equals(OrientationType.landscape))
                {
                    nameDisplay.fontSize = m_playerFontSize;

                    if (m_profileLayout != null)
                    {
                        m_profileLayout.minWidth = m_profileSize.x;
                        m_profileLayout.minHeight = m_profileSize.y;
                    }

                    friendsViewport.GetComponentInChildren<LayoutElement>().minHeight = 60;
                    friendsViewport.GetComponentInChildren<LayoutElement>().preferredHeight = 60;

                    foreach (Selectable but in friendsViewport.GetComponentsInChildren<Selectable>(true))
                    {
                        Image[] imgs = but.GetComponentsInChildren<Image>(true);

                        for (int i = 0; i < imgs.Length; i++)
                        {
                            if (imgs[i].transform.Equals(but.transform))
                            {
                                continue;
                            }

                            if (imgs[i].transform.name.Contains("Notification")) continue;

                            Destroy(imgs[i].GetComponent<LayoutElement>());
                            imgs[i].SetNativeSize();
                        }

                        TextMeshProUGUI txt = but.GetComponentInChildren<TextMeshProUGUI>(true);

                        if (txt != null)
                        {
                            txt.fontSize = 16;
                        }

                    }

                    foreach (Selectable but in gameViewport.GetComponentsInChildren<Selectable>(true))
                    {
                        Image[] imgs = but.GetComponentsInChildren<Image>(true);

                        for (int i = 0; i < imgs.Length; i++)
                        {
                            if (imgs[i].transform.Equals(but.transform))
                            {
                                imgs[i].GetComponent<LayoutElement>().minHeight = 60;
                                continue;
                            }

                            if (imgs[i].transform.name.Contains("Notification")) continue;

                            Destroy(imgs[i].GetComponent<LayoutElement>());
                            imgs[i].SetNativeSize();
                        }

                        TextMeshProUGUI txt = but.GetComponentInChildren<TextMeshProUGUI>(true);
                        txt.fontSize = 16;
                    }

                    foreach (Selectable but in commsViewport.GetComponentsInChildren<Selectable>(true))
                    {
                        Image[] imgs = but.GetComponentsInChildren<Image>(true);

                        for (int i = 0; i < imgs.Length; i++)
                        {
                            if (imgs[i].transform.Equals(but.transform))
                            {
                                imgs[i].GetComponent<LayoutElement>().minHeight = 60;
                                continue;
                            }

                            if (imgs[i].transform.name.Contains("Notification")) continue;

                            Destroy(imgs[i].GetComponent<LayoutElement>());
                            imgs[i].SetNativeSize();
                        }

                        TextMeshProUGUI txt = but.GetComponentInChildren<TextMeshProUGUI>(true);
                        txt.fontSize = 16;
                    }
                }
                else
                {
                    nameDisplay.fontSize = m_playerFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler * aspect;

                    if (m_profileLayout != null)
                    {
                        m_profileLayout.minWidth = m_profileSize.x * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
                        m_profileLayout.minHeight = m_profileSize.y * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
                    }

                    friendsViewport.GetComponentInChildren<LayoutElement>().minHeight = 60 * aspect;
                    friendsViewport.GetComponentInChildren<LayoutElement>().preferredHeight = 60 * aspect;

                    foreach (Selectable but in friendsViewport.GetComponentsInChildren<Selectable>(true))
                    {
                        Image[] imgs = but.GetComponentsInChildren<Image>(true);

                        for (int i = 0; i < imgs.Length; i++)
                        {
                            if (imgs[i].transform.Equals(but.transform))
                            {
                                continue;
                            }

                            if (imgs[i].transform.name.Contains("Notification")) continue;

                            Vector2 size = imgs[i].GetComponent<RectTransform>().sizeDelta;
                            LayoutElement le = imgs[i].gameObject.AddComponent<LayoutElement>();
                            le.minWidth = size.x * aspect;
                            le.minHeight = size.y * aspect;
                        }

                        TextMeshProUGUI txt = but.GetComponentInChildren<TextMeshProUGUI>(true);

                        if (txt != null)
                        {
                            txt.fontSize = 16 * aspect;
                        }

                    }

                    foreach (Selectable but in gameViewport.GetComponentsInChildren<Selectable>(true))
                    {
                        Image[] imgs = but.GetComponentsInChildren<Image>(true);

                        for (int i = 0; i < imgs.Length; i++)
                        {
                            if (imgs[i].transform.Equals(but.transform))
                            {
                                imgs[i].GetComponent<LayoutElement>().minHeight = 60 * aspect;
                                continue;
                            }

                            if (imgs[i].transform.name.Contains("Notification")) continue;

                            Vector2 size = imgs[i].GetComponent<RectTransform>().sizeDelta;
                            LayoutElement le = imgs[i].gameObject.AddComponent<LayoutElement>();
                            le.minWidth = size.x * aspect;
                            le.minHeight = size.y * aspect;
                        }

                        TextMeshProUGUI txt = but.GetComponentInChildren<TextMeshProUGUI>(true);
                        txt.fontSize = 16 * aspect;
                    }

                    foreach (Selectable but in commsViewport.GetComponentsInChildren<Selectable>(true))
                    {
                        Image[] imgs = but.GetComponentsInChildren<Image>(true);

                        for (int i = 0; i < imgs.Length; i++)
                        {
                            if (imgs[i].transform.Equals(but.transform))
                            {
                                imgs[i].GetComponent<LayoutElement>().minHeight = 60 * aspect;
                                continue;
                            }

                            if (imgs[i].transform.name.Contains("Notification")) continue;

                            Vector2 size = imgs[i].GetComponent<RectTransform>().sizeDelta;
                            LayoutElement le = imgs[i].gameObject.AddComponent<LayoutElement>();
                            le.minWidth = size.x * aspect;
                            le.minHeight = size.y * aspect;
                        }

                        TextMeshProUGUI txt = but.GetComponentInChildren<TextMeshProUGUI>(true);
                        txt.fontSize = 16 * aspect;

                    }
                }
            }
        }

        /// <summary>
        /// Called to set the button display
        /// </summary>
        /// <param name="player"></param>
        /// <param name="listID"></param>
        public void Set(IPlayer player, string listID)
        {
            m_playerID = player.ID;
            m_player = player;
            m_listID = listID;
            nameDisplay.text = (m_playerID.Equals(PlayerManager.Instance.GetLocalPlayer().ID)) ? "[You] " + PlayerManager.Instance.GetPlayerName(player.NickName) : PlayerManager.Instance.GetPlayerName(player.NickName) + " [" + player.ActorNumber + "]";

            m_handler = GetComponentInParent<UserList>();

            privateChatNofication.ChatID = nameDisplay.text;

            m_playerFontSize = nameDisplay.fontSize;
            m_profileLayout = profileImage.GetComponentInParent<LayoutElement>(true);

            if (m_profileLayout != null)
            {
                m_profileSize = new Vector2(m_profileLayout.minWidth, m_profileLayout.minHeight);
            }

            m_loaded = true;
        }

        /// <summary>
        /// Toggle this object on/off
        /// </summary>
        /// <param name="toggle"></param>
        public void Toggle(bool toggle)
        {
            if (PlayerManager.Instance.GetLocalPlayer().ID.Equals(m_playerID)) return;
            
            IsActive = toggle;
            options.SetActive(IsActive);

            //   float sizeY = GetComponent<LayoutElement>().minHeight;
            // float optionsY = options.GetComponent<RectTransform>().sizeDelta.y;
            //  float newY = (IsActive) ? sizeY + optionsY : sizeY - optionsY;

            //    GetComponent<LayoutElement>().minHeight = newY;

            /* if (!IsActive)
             {
                 options.SetActive(false);
             }*/
        }

        /// <summary>
        /// Called via the button to teleport the local player to this owners position in the VR world
        /// </summary>
        public void OnClick()
        {
            if (PlayerManager.Instance.GetLocalPlayer().ID.Equals(m_playerID)) return;

            if (m_handler != null)
            {
                m_handler.SetCurrentInterface(this);
            }

            Toggle(!IsActive);

            if(IsActive)
            {
                followInteraction.GetComponent<Toggle>().onValueChanged.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);

                if(PlayerManager.Instance.CurrentPlayerFollowing != null)
                {
                    if (!PlayerManager.Instance.CurrentPlayerFollowing.ID.Equals(Owner))
                    {
                        followInteraction.GetComponent<Toggle>().isOn = false;
                    }
                    else
                    {
                        followInteraction.GetComponent<Toggle>().isOn = true;
                    }
                }
                else
                {
                    followInteraction.GetComponent<Toggle>().isOn = false;
                }

                followInteraction.GetComponent<Toggle>().onValueChanged.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.RuntimeOnly);
            }
        }

        /// <summary>
        /// Function to teleport to user
        /// </summary>
        public void OnTeleport()
        {
            if (PlayerManager.Instance.GetLocalPlayer().ID.Equals(m_playerID) || ChairManager.Instance.HasPlayerOccupiedChair(m_playerID)
                 || MMOManager.Instance.IsPlayerBusy(m_playerID)) return;

            foreach (var view in MMOManager.Instance.GetAllPlayers())
            {
                if (view.ID.Equals(m_playerID))
                {
                    PlayerManager.Instance.GetLocalPlayer().FreezePosition = true;
                    PlayerManager.Instance.GetLocalPlayer().FreezeRotation = true;
                    m_teleportTo = view;

                    //need to send event out to hide player avatar
                    Hashtable hash = new Hashtable();
                    hash.Add("SHOWAVATAR", "0");
                    MMOManager.Instance.SetPlayerProperties(hash);

                    //fade out
                    HUDManager.Instance.Fade(FadeOutIn.FadeAction.Out_In, PerformTeleportation, TeleportationCallback, 0.5f);

                    break;
                }
            }
        }

        /// <summary>
        /// Action called to start private video call
        /// </summary>
        public void FollowPlayer(bool follow)
        {
            if(m_player != null)
            {
                PlayerManager.Instance.FollowRemotePlayer(m_player, follow);
            }
        }

        /// <summary>
        /// Called to toggle this players disturn networked property
        /// </summary>
        public void TogglePlayerDisturbMode()
        {
            if (MMOManager.Instance.PlayerHasProperty(m_player, "DONOTDISTURB"))
            {
                bool currentDisturb = (MMOManager.Instance.GetPlayerProperty(m_player, "DONOTDISTURB").Equals("1")) ? true : false;
                MMOChat.Instance.SetDisturbedMode(!currentDisturb);
            }
        }

        /// <summary>
        /// Action called to start private phone call (smartphone only)
        /// </summary>
        public void StartVoiceCall()
        {
            if (!m_donotdistrubMode)
            {
                MMOChat.Instance.DialVoiceCall(m_playerID);
            }
        }

        /// <summary>
        /// Called to open the global chat
        /// </summary>
        public void OnChatClick()
        {
            if (!m_donotdistrubMode)
            {
                privateChatNofication.Hide();
                MMOChat.Instance.SwitchChat(m_playerID);
            }
        }

        /// <summary>
        /// Called to view this local players profile
        /// </summary>
        public void ViewProfile()
        {
            MMORoom.Instance.ShowPlayerProfile(m_player);
        }

        /// <summary>
        /// Action performed during the fade out state
        /// </summary>
        private void PerformTeleportation()
        {
            Vector2 rot = new Vector2(PlayerManager.Instance.GetLocalPlayer().TransformObject.eulerAngles.y, 0.0f);
            PlayerManager.Instance.TeleportLocalPlayer(m_teleportTo.TransformObject.position, rot);
        }

        /// <summary>
        /// Callback for when the fade in is complete
        /// </summary>
        private void TeleportationCallback()
        {
            //need to send event out to show player avatar
            Hashtable hash = new Hashtable();
            hash.Add("SHOWAVATAR", "1");
            MMOManager.Instance.SetPlayerProperties(hash);

            PlayerManager.Instance.GetLocalPlayer().FreezePosition = false;
            PlayerManager.Instance.GetLocalPlayer().FreezeRotation = false;
        }


        private IEnumerator LoadImage(string url)
        {
            if (string.IsNullOrEmpty(url) || !url.Contains("http"))
            {
                if (url.Contains("(Clone)"))
                {
                    url = url.Replace("(Clone)", "");
                }

                if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe))
                {
#if BRANDLAB360_AVATARS_READYPLAYERME
                    RPMPlayer rpm = PlayerManager.Instance.GetPlayer(m_player.ID).TransformObject.GetComponentInChildren<RPMPlayer>();

                    if (rpm != null)
                    {
                        if (rpm.Picture != null)
                        {
                            profileImage.texture = rpm.Picture;
                        }
                        else
                        {
                            profileImage.texture = Resources.Load<Texture>("ProfilePictures/" + url);
                        }
                    }
                    else
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/" + url);
                    }
#else
                    profileImage.texture = Resources.Load<Texture>("ProfilePictures/" + url);
#endif
                }
                else if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Custom))
                {
                    if (m_player.IsLocal)
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/" + AppManager.Instance.Data.FixedAvatarName);
                    }
                    else
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/" + url);
                    }

                    //use default
                    if (profileImage.texture == null)
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/MaleSimple");
                    }
                }
                else if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Simple))
                {
                    if (url.Contains("Male"))
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/MaleSimple");
                    }
                    else
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/FemaleSimple");
                    }
                }
                else
                {
                    Debug.Log(url);

                    if (url.Contains("Male"))
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/MaleStandard");
                    }
                    else
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/FemaleStandard");
                    }
                }
            }
            else if(!m_player.IsLocal)
            {
                //check if the player has a texture2D RPM
            }
            else
            {
                //webrequest
                UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, true);

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(request.error))
                {
                    profileImage.texture = DownloadHandlerTexture.GetContent(request);

                    m_isWebRequest = true;
                }

                //dispose the request as not needed anymore
                request.Dispose();
            }

            if (profileImage.texture != null)
            {
                emptyImage.localScale = Vector3.zero;
                profileImage.transform.localScale = Vector2.one;
            }
            else
            {
                emptyImage.localScale = Vector3.one;
                profileImage.transform.localScale = Vector2.zero;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(GlobalUserListButton), true)]
        public class GlobalUserListButton_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("nameDisplay"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("status"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("options"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("disturbIndicators"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("emptyImage"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profileImage"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("privateChatNofication"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("phoneInteraction"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messageInteraction"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("teleportInteraction"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("followInteraction"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("friendsViewport"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("gameViewport"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("commsViewport"), true);

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
