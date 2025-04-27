using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Networking;

#if BRANDLAB360_AVATARS_READYPLAYERME
using BrandLab360.ReadyPlayerMe;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class UserProfile : MonoBehaviour, IFriend
    {
        [Header("Profile UI")]
        [SerializeField]
        private TextMeshProUGUI username;
        [SerializeField]
        private TMP_InputField birthname;
        [SerializeField]
        private TMP_InputField customizationData;
        [SerializeField]
        private GameObject buttonMore;

        [Header("Profile Image")]
        [SerializeField]
        private Transform emptyImage;
        [SerializeField]
        private RawImage profileImage;
        [SerializeField]
        private Image statusDisplay;

        [Header("Editors")]
        [SerializeField]
        private GameObject[] editors;

        [Header("Chat System Attributes")]
        [SerializeField]
        private GameObject[] disturbIndicators;
        [SerializeField]
        private PhotonChatPlayerNotification privateChatNotification;

        [Header("Options Interaction")]
        [SerializeField]
        private GameObject optionsContainer;
        [SerializeField]
        private CanvasGroup phoneInteraction;
        [SerializeField]
        private CanvasGroup messageInteraction;
        [SerializeField]
        private GameObject reportButton;

        [Header("Mobile Layout")]
        [SerializeField]
        private HorizontalLayoutGroup headerLayout;
        [SerializeField]
        private VerticalLayoutGroup nameLayout;
        [SerializeField]
        private VerticalLayoutGroup aboutLayout;
        [SerializeField]
        private LayoutElement reportLayout;
        [SerializeField]
        private RectTransform commsViewport;
        [SerializeField]
        private RectTransform friendsViewport;

        private bool m_donotdistrubMode = false;
        private bool m_localPlayer = false;
        private string m_data = "";
        private IPlayer iPlayer = null;
        private bool m_loadedTexture;

        private RectTransform m_mainLayout;
        private float m_layoutWidth;
        private float m_titleFontSize;
        private float m_birthnameInputHeigth;
        private float m_inputFontSize;
        private float m_reportLayoutHeight;

        public string Friend_ID
        {
            get
            {
                return iPlayer.NickName;
            }
        }

        public System.Action OnThisUpdate { get; set; }

        public string CurrentPlayerID
        {
            get
            {
                if(iPlayer != null)
                {
                    if (FriendsManager.Instance.IsEnabled)
                    {
                        if (m_localPlayer)
                        {
                            return AppManager.Instance.Data.LoginProfileData.username;
                        }
                        else
                        {
                            if (iPlayer.CustomizationData.ContainsKey("PROFILE_USERNAME"))
                            {
                                return iPlayer.CustomizationData["PROFILE_USERNAME"].ToString();
                            }
                        }
                    }

                    return iPlayer.ID;
                }

                return "";
            }
        }

        public System.Action OnProfileUpdate { get; set; }

        private void Awake()
        {
            m_mainLayout = transform.GetChild(0).GetComponent<RectTransform>();
            m_layoutWidth = m_mainLayout.sizeDelta.x;

            m_titleFontSize = username.fontSize;
            m_birthnameInputHeigth = birthname.GetComponent<LayoutElement>().minHeight;
            m_inputFontSize = birthname.textComponent.fontSize;

            m_reportLayoutHeight = reportLayout.GetComponentInChildren<LayoutElement>(true).minHeight;
        }

        private void OnEnable()
        {
            MMORoom.Instance.OnPlayerLeftRoom += OnPlayerLeftRoom;
            profileImage.GetComponent<RectTransform>().transform.localScale = Vector2.zero;

            buttonMore.SetActive(!m_localPlayer);
            optionsContainer.gameObject.SetActive(false);

            SubtitleManager.Instance.ToggleButtonVisibiliy(false);

            if (FriendsManager.Instance.IsEnabled)
            {
                if(m_localPlayer)
                {
                    StartCoroutine(LoadImage(AppManager.Instance.Data.LoginProfileData.picture_url));

                    reportButton.SetActive(false);
                }
                else
                {
                    if(iPlayer.CustomizationData.ContainsKey("PROFILE_PICTURE"))
                    {
                        StartCoroutine(LoadImage(iPlayer.CustomizationData["PROFILE_PICTURE"].ToString()));
                    }
                    else
                    {
                        if(iPlayer.CustomizationData.ContainsKey("FIXEDAVATAR"))
                        {
                            StartCoroutine(LoadImage(iPlayer.CustomizationData["FIXEDAVATAR"].ToString()));
                        }
                        else
                        {
                            if (iPlayer.Avatar != null)
                            {
                                StartCoroutine(LoadImage(iPlayer.Avatar.name));
                            }
                        }
                    }

                    reportButton.SetActive(true);
                }
            }
            else
            {
                reportButton.SetActive(false);

                if (m_localPlayer)
                {
                    StartCoroutine(LoadImage(AppManager.Instance.Data.FixedAvatarName));
                }
                else
                {
                    if (iPlayer.CustomizationData.ContainsKey("FIXEDAVATAR"))
                    {
                        StartCoroutine(LoadImage(iPlayer.CustomizationData["FIXEDAVATAR"].ToString()));
                    }
                    else
                    {
                        if (iPlayer.Avatar != null)
                        {
                            StartCoroutine(LoadImage(iPlayer.Avatar.name));
                        }
                    }
                }
            }

            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
        }

        private void OnDisable()
        {
            StopAllCoroutines();

            if(m_localPlayer)
            {
                SaveProfile();
            }

            PlayerManager.Instance.FreezePlayer(false);
            m_localPlayer = false;
            m_data = "";
            iPlayer = null;
            emptyImage.localScale = Vector3.one;

            if (m_loadedTexture)
            {
                Destroy(profileImage.texture);
                profileImage.texture = null;
            }

            profileImage.GetComponent<RectTransform>().transform.localScale = Vector2.zero;
            m_loadedTexture = false;

            optionsContainer.gameObject.SetActive(false);

            MMORoom.Instance.OnPlayerLeftRoom -= OnPlayerLeftRoom;

            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;

            SubtitleManager.Instance.ToggleButtonVisibiliy(true);
        }

        public void ToggleOptions()
        {
            optionsContainer.gameObject.SetActive(!optionsContainer.activeInHierarchy);
        }

        /// <summary>
        /// Called to update the user profiler and open
        /// About now derives form LoginProfileData.about not AppManager.Instance.Data.CustomizedData
        /// Name now derives form LoginProfileData.name and perminatly appears
        /// </summary>
        /// <param name="player"></param>
        /// <param name="data"></param>
        public virtual void Set(IPlayer player)
        {
            iPlayer = player;

            if(iPlayer != null)
            {
                username.text = PlayerManager.Instance.GetPlayerName(player.NickName) + " [" + player.ActorNumber + "]";
            }
            else
            {
                username.text = AppManager.Instance.Data.NickName;
            }
            
            customizationData.text = "";

            //need to display more information here
            PlayerManager.Instance.FreezePlayer(true);

            for (int i = 0; i < disturbIndicators.Length; i++)
            {
                disturbIndicators[i].GetComponentInChildren<Image>(true).color = CoreManager.Instance.chatSettings.busy;
                disturbIndicators[i].SetActive(false);
            }

            if (player != null)
            {
                m_localPlayer = player.ID.Equals(PlayerManager.Instance.GetLocalPlayer().ID);
                iPlayer = PlayerManager.Instance.GetPlayer(player.ID);
            }
            else
            {
                m_localPlayer = true;
                iPlayer = PlayerManager.Instance.GetLocalPlayer();
            }


            //BIRHTNAME - this shold be default now as customised data is irrelvant for this, use LoginProfileData.name 
            this.birthname.transform.parent.gameObject.SetActive(true);
            this.birthname.text = m_localPlayer ? AppManager.Instance.Data.LoginProfileData.name : iPlayer.CustomizationData.ContainsKey("PROFILE_BIRTHNAME") ? iPlayer.CustomizationData["PROFILE_BIRTHNAME"].ToString() : "";
            this.birthname.GetComponent<CanvasGroup>().blocksRaycasts = false;

            //editor buttons - only for local player
            for (int i = 0; i < editors.Length; i++)
            {
                //need to check if we can edit
                if(AppManager.Instance.Settings.projectSettings.useIndexedDB || AppManager.Instance.Settings.projectSettings.loginMode.Equals(LoginMode.Registration))
                {
                    editors[i].SetActive(m_localPlayer);
                }
                else
                {
                    editors[i].SetActive(false);
                }
            }

            //ABOUT - this shold be default now as customised data is irrelvant for this, use LoginProfileData.name 
            customizationData.text = m_localPlayer ? AppManager.Instance.Data.LoginProfileData.about : iPlayer.CustomizationData.ContainsKey("PROFILE_ABOUT") ? iPlayer.CustomizationData["PROFILE_ABOUT"].ToString() : "";
            m_data = customizationData.text;
            customizationData.GetComponent<CanvasGroup>().blocksRaycasts = false;

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Called to repaint this objects UI
        /// </summary>
        public void Update()
        {
            //can only edit the customization data if these are met
            if(!m_localPlayer)
            {
                customizationData.text = m_data;
            }

            if (iPlayer != null)
            {
                bool busy = MMOManager.Instance.IsPlayerBusy(iPlayer.ID);

                if (MMOManager.Instance.PlayerHasProperty(iPlayer, "DONOTDISTURB"))
                {
                    m_donotdistrubMode = (MMOManager.Instance.GetPlayerProperty(iPlayer, "DONOTDISTURB").Equals("1") ? true : false);

                    for (int i = 0; i < disturbIndicators.Length; i++)
                    {
                        disturbIndicators[i].SetActive(m_donotdistrubMode);
                    }
                }

                phoneInteraction.alpha = (busy || m_localPlayer) ? 0.1f : (!m_donotdistrubMode) ? 1.0f : 0.1f;
                phoneInteraction.interactable = (busy || m_localPlayer) ? false : (!m_donotdistrubMode) ? true : false;

                messageInteraction.alpha =  (m_localPlayer) ? 0.1f : (!m_donotdistrubMode) ? 1.0f : 0.1f;
                messageInteraction.interactable = (m_localPlayer) ? false : (!m_donotdistrubMode) ? true : false;

                statusDisplay.color = PlayerManager.Instance.GetPlayerStatus(iPlayer.ID);
            }

            if(OnThisUpdate != null)
            {
                OnThisUpdate.Invoke();
            }
        }

        private void OnPlayerLeftRoom(IPlayer player)
        {
            if(player.ID.Equals(iPlayer.ID))
            {
                gameObject.SetActive(false);
            }
        }

        public void ReportUser()
        {
            ReportCreator rc = HUDManager.Instance.GetHUDScreenObject("REPORT_SCREEN").GetComponentInChildren<ReportCreator>();

            if (rc != null)
            {
                //need to pass this to the reportCreator
                rc.CurrentObjectID = "USERPROFILE:" + iPlayer.NickName;
            }

            HUDManager.Instance.ToggleHUDScreen("REPORT_SCREEN");
        }

        public void EditName(bool state)
        {
            birthname.GetComponent<CanvasGroup>().blocksRaycasts = false;

            if (state)
            {
                birthname.Select();
            }
            else
            {
                SaveProfile();
            }
        }

        public void EditAbout(bool state)
        {
            customizationData.GetComponent<CanvasGroup>().blocksRaycasts = false;

            if (state)
            {
                customizationData.Select();
            }
            else
            {
                SaveProfile();
            }
        }

        public virtual void EditProfilePicture()
        {
            if (m_localPlayer)
            {
                //if (FriendsManager.Instance.IsEnabled)
               // {

#if UNITY_EDITOR
                    Debug.Log("using a profile file from local directory");
                    ProfileResponce response = new ProfileResponce();
                    response.profile_url = UnityEditor.EditorUtility.OpenFilePanelWithFilters("Upload File", Application.dataPath, new string[2] { "FileType", "jpg" });
                    ResponceCallback(JsonUtility.ToJson(response));
#else
                    var message = new ProfileMessage(true, AppManager.Instance.Data.LoginProfileData.picture_url);
                    var json = JsonUtility.ToJson(message);

                    Debug.Log("EditProfilePicture request: " + json);

                    //add responce listener and send
                    WebclientManager.WebClientListener += ResponceCallback;
                    WebclientManager.Instance.Send(json);
#endif
               // }
            }
        }

        private void ResponceCallback(string obj)
        {
            //ensure reponce data is tpye responce
            ProfileResponce responce = JsonUtility.FromJson<ProfileResponce>(obj).OrDefaultWhen(x => x.profile_url == null);
            //remove listener
            WebclientManager.WebClientListener -= ResponceCallback;

            if (responce != null)
            {
                if(!string.IsNullOrEmpty(responce.profile_url))
                {
                    if (m_loadedTexture)
                    {
                        Destroy(profileImage.texture);
                        profileImage.texture = null;
                    }

                    //load profile picture
                    AppManager.Instance.Data.LoginProfileData.picture_url = responce.profile_url;
                    StartCoroutine(LoadImage(AppManager.Instance.Data.LoginProfileData.picture_url));
                }
            }
        }

        protected virtual void SaveProfile()
        {
            if(m_localPlayer)
            {
               // if (FriendsManager.Instance.IsEnabled)
               // {
                    if (AppManager.Instance.Data == null || AppManager.Instance.Data.LoginProfileData == null) return;

                    AppManager.Instance.Data.LoginProfileData.name = birthname.text;
                    AppManager.Instance.Data.LoginProfileData.about = customizationData.text;

                    if(AppManager.Instance.Settings.projectSettings.loginMode.Equals(LoginMode.Registration))
                    {
                        AppManager.Instance.UpdateLoginsAPI();
                        //string projectID = string.IsNullOrEmpty(AppManager.Instance.Settings.projectSettings.clientName) ? AppManager.Instance.Data.ProjectID : AppManager.Instance.Settings.projectSettings.clientName;
                        // LoginsAPI.Instance.UpdateUser(AppManager.Instance.Data.NickName, projectID, JsonUtility.ToJson(AppManager.Instance.Data.LoginProfileData), AppManager.Instance.Data.LoginProfileData.password, AppManager.Instance.Data.RawFriendsData, AppManager.Instance.Data.RawGameData);
                    }
                    else
                    {
                        //need to update the profile for IDB
                        string nName = "User-" + AppManager.Instance.Data.NickName;
                        string admin = "Admin-" + (AppManager.Instance.Data.IsAdminUser ? 1 : 0).ToString();
                        string json = "Json-" + AppManager.Instance.Data.CustomiseJson;
                        string friends = "Friends-" + AppManager.Instance.Data.CustomiseFriends;
                        string games = "Games-" + AppManager.Instance.Data.RawGameData;

                        string prof = "Name*" + AppManager.Instance.Data.LoginProfileData.name + "|About*" + AppManager.Instance.Data.LoginProfileData.about + "|Pic*" + AppManager.Instance.Data.LoginProfileData.picture_url;
                        string profile = "Profile-" + prof;

                        if (AppManager.Instance.UserExists)
                        {
                            IndexedDbManager.Instance.UpdateEntry("userData", nName + ":" + admin + ":" + json + ":" + friends + ":" + profile + ":" + games);
                        }
                        else
                        {
                            IndexedDbManager.Instance.InsertEntry("userData", nName + ":" + admin + ":" + json + ":" + friends + ":" + profile + ":" + games);
                        }
                    }

                    if (OnProfileUpdate != null)
                    {
                        OnProfileUpdate.Invoke();
                    }

                    PlayerManager.Instance.SetRigistrationData();
               // }
               // else
              //  {
                   
               // }
            }
        }

        /// <summary>
        /// Called to open the global chat
        /// </summary>
        public void OnChatClick()
        {
            if (!m_donotdistrubMode)
            {
                privateChatNotification.Hide();
                MMOChat.Instance.SwitchChat(iPlayer.ID);
            }
        }

        /// <summary>
        /// Action called to start private phone call (smartphone only)
        /// </summary>
        public void StartVoiceCall()
        {
            if (!m_donotdistrubMode)
            {
                MMOChat.Instance.DialVoiceCall(iPlayer.ID);
            }
        }

        private IEnumerator LoadImage(string url)
        {
            bool rpmUsed = AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe);

            if (string.IsNullOrEmpty(url) || !url.Contains("http") || rpmUsed)
            {
                if (url.Contains("(Clone)"))
                {
                    url = url.Replace("(Clone)", "");
                }

                if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe))
                {
#if BRANDLAB360_AVATARS_READYPLAYERME
                    RPMPlayer rpm = PlayerManager.Instance.GetPlayer(iPlayer.ID).TransformObject.GetComponentInChildren<RPMPlayer>();

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
                else if(AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Custom))
                {
                    if (m_localPlayer)
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
                    if(url.Contains("Male"))
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
            else
            {

                //webrequest
                UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, true);

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(request.error))
                {
                    profileImage.texture = DownloadHandlerTexture.GetContent(request);

                    m_loadedTexture = true;
                }

                //dispose the request as not needed anymore
                request.Dispose();
            }

            if (profileImage.texture != null)
            {
                emptyImage.localScale = Vector3.zero;
                profileImage.GetComponent<RectTransform>().transform.localScale = Vector2.one;
            }
            else
            {
                emptyImage.localScale = Vector3.one;
                profileImage.transform.localScale = Vector2.zero;
            }
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    m_mainLayout.anchorMin = new Vector2(0.5f, 0.0f);
                    m_mainLayout.anchorMax = new Vector2(0.5f, 1.0f);
                    m_mainLayout.offsetMax = new Vector2(0, -100);
                    m_mainLayout.offsetMin = new Vector2(0, 100);
                    m_mainLayout.anchoredPosition = Vector2.zero;
                    m_mainLayout.sizeDelta = new Vector2(m_layoutWidth, m_mainLayout.sizeDelta.y);

                    nameLayout.spacing = 20;
                    aboutLayout.spacing = 20;
                    headerLayout.padding.top = 0;

                    username.fontSize = m_titleFontSize;

                    birthname.GetComponent<LayoutElement>().minHeight = m_birthnameInputHeigth;

                    foreach(TMP_Text txt in birthname.GetComponentsInChildren<TMP_Text>())
                    {
                        txt.fontSize = m_inputFontSize;
                    }

                    foreach (TMP_Text txt in customizationData.GetComponentsInChildren<TMP_Text>())
                    {
                        txt.fontSize = m_inputFontSize;
                    }

                    reportLayout.minHeight = m_reportLayoutHeight * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
                    reportLayout.preferredHeight = reportLayout.minHeight;

                    foreach (Selectable but in reportLayout.GetComponentsInChildren<Selectable>(true))
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

                    friendsViewport.GetComponentInChildren<LayoutElement>().minHeight = 60;
                    friendsViewport.GetComponentInChildren<LayoutElement>().preferredHeight = 60;

                    foreach (Selectable but in friendsViewport.GetComponentsInChildren<Selectable>(true))
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

                        if (txt != null)
                        {
                            txt.fontSize = 16;
                        }

                    }
                }
                else
                {
                    float aspect = arg2 / arg1;

                    m_mainLayout.anchorMin = new Vector2(0f, 0.0f);
                    m_mainLayout.anchorMax = new Vector2(1f, 1.0f);
                    m_mainLayout.offsetMax = new Vector2(-50, -100);
                    m_mainLayout.offsetMin = new Vector2(50, 100);

                    m_mainLayout.anchoredPosition = Vector2.zero;

                    nameLayout.spacing = 20 * aspect;
                    aboutLayout.spacing = 20 * aspect;
                    headerLayout.padding.top = 100;

                    username.fontSize = m_titleFontSize * aspect;

                    birthname.GetComponent<LayoutElement>().minHeight = m_birthnameInputHeigth * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;

                    foreach (TMP_Text txt in birthname.GetComponentsInChildren<TMP_Text>())
                    {
                        txt.fontSize = m_inputFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    }

                    foreach (TMP_Text txt in customizationData.GetComponentsInChildren<TMP_Text>())
                    {
                        txt.fontSize = m_inputFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    }

                    reportLayout.minHeight = (m_reportLayoutHeight * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler) * aspect;
                    reportLayout.preferredHeight = reportLayout.minHeight;


                    foreach (Selectable but in reportLayout.GetComponentsInChildren<Selectable>(true))
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

                    friendsViewport.GetComponentInChildren<LayoutElement>().minHeight = 60 * aspect;
                    friendsViewport.GetComponentInChildren<LayoutElement>().preferredHeight = 60 * aspect;

                    foreach (Selectable but in friendsViewport.GetComponentsInChildren<Selectable>(true))
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

                        if (txt != null)
                        {
                            txt.fontSize = 16 * aspect;
                        }

                    }


                }
            }
        }

        [System.Serializable]
        private class ProfileMessage
        {
            public bool BeginProfileUpload;
            public string profile;

            public ProfileMessage(bool upload, string pic)
            {
                BeginProfileUpload = upload;
                profile = pic;
            }
        }

        [System.Serializable]
        private class ProfileResponce
        {
            public string profile_url;
        }
    }

    [System.Serializable]
    public class ProfileData
    {
        public int id = -1;
        public string username = "";
        public string name = "";
        public string about = "";
        public string password = "";
        public string email = "";
        public string picture_url = "";
        public bool acceptTAC = true;
        public string avatar_data = "";
        public string player_settings = "";

        public List<AdditionalProfileData> additonal_data;

        public void AddAdditionalData(string key, string data)
        {
            if(additonal_data == null)
            {
                additonal_data = new List<AdditionalProfileData>();
            }

            AdditionalProfileData pd = additonal_data.FirstOrDefault(x => x.key.Equals(key));

            if (pd != null)
            {
                pd.data = data;
            }
            else
            {
                additonal_data.Add(new AdditionalProfileData(key, data));
            }
        }

        public string GetAdditonalData(string key)
        {
            string str = "";

            for(int i = 0; i < additonal_data.Count; i++)
            {
                if(additonal_data[i].key.Equals(key))
                {
                    str = additonal_data[i].data;
                    break;
                }
            }

            return str;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(UserProfile), true)]
        public class UserProfile_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("username"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("birthname"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("customizationData"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonMore"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("emptyImage"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profileImage"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("statusDisplay"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("editors"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("disturbIndicators"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("privateChatNotification"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("optionsContainer"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("phoneInteraction"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messageInteraction"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("reportButton"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("headerLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("nameLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("aboutLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("reportLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("commsViewport"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("friendsViewport"), true);

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

    [System.Serializable]
    public class AdditionalProfileData
    {
        public string key;
        public string data;

        public AdditionalProfileData(string key, string data)
        {
            this.key = key;
            this.data = data;
        }
    }
}
