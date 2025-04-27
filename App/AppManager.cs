using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class AppManager : Singleton<AppManager>
    {
        public static AppManager Instance
        {
            get
            {
                return ((AppManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        public AppSettings Settings { get; private set; }

        public AppData Data { get; private set; }

        public AppInstances Instances { get; private set; }

        public AppAssets Assets { get; private set; }

        public AppLoginAPISettings API { get; private set; }

        public static bool IsCreated
        {
            get;
            private set;
        }

        private Coroutine m_videoProcess;
        private bool m_skipName = false;
        private bool m_skipAvatar = false;
        private bool m_unloadIntro = false;
        private bool m_notStreamed = false;
        private WebClientSimulator webclientSimulator;
        private bool m_urlParamsExist = false;
        private bool m_streamingCallbackCalled = false;
        private bool m_streamBeginFailed = false;

        public bool URLParamUser
        {
            get
            {
                return m_urlParamsExist;
            }
        }

        public bool UserExists
        {
            get;
            private set;
        }

        public bool WebClientSimulatorExists
        {
            get
            {
                return webclientSimulator != null;
            }
        }

        public void Awake()
        {
#if UNITY_EDITOR
            var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var selectedSizeIndexProp = gvWndType.GetProperty("selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var gvWnd = EditorWindow.GetWindow(gvWndType);
            selectedSizeIndexProp.SetValue(gvWnd, 3, null);
#endif

            //create
            AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");

            if (appReferences != null)
            {
                Settings = appReferences.Settings;
                Instances = appReferences.Instances;
            }
            else
            {

                Settings = Resources.Load<AppSettings>("ProjectAppSettings");
                Instances = Resources.Load<AppInstances>("ProjectAppInstances");
            }

            API = Resources.Load<AppLoginAPISettings>("ProjectAPISettings");


#if UNITY_EDITOR
            if (Settings.projectSettings.streamingMode.Equals(WebClientMode.None))
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.SetResolution(1024, 720, false);
            }
#endif

            if (Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Standard))
            {
#if !BRANDLAB360_AVATARS_STANDARD && UNITY_EDITOR
                Debug.Log("Cannot continue!!! com.brandlab360.avatars.standard has not been installed!!");
                UnityEditor.EditorApplication.ExitPlaymode();
                return;
#endif
            }
            else if (Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe))
            {
#if !BRANDLAB360_AVATARS_READYPLAYERME && UNITY_EDITOR
                Debug.Log("Cannot continue!!! com.brandlab360.avatars.readyplayerme has not been installed!!");
                UnityEditor.EditorApplication.ExitPlaymode();
                return;
#endif
            }

            m_skipName = Settings.projectSettings.skipName;
            Data = new AppData();
            Data.Mode = Settings.projectSettings.multiplayerMode;

            //setup the defualt control keys
            Data.fowardDirectionKey = Settings.playerSettings.fowardDirectionKey;
            Data.backDirectionKey = Settings.playerSettings.backDirectionKey;
            Data.leftDirectionKey = Settings.playerSettings.leftDirectionKey;
            Data.rightDirectionKey = Settings.playerSettings.rightDirectionKey;
            Data.sprintKey = Settings.playerSettings.sprintKey;

            Data.strifeLeftDirectionKey = Settings.playerSettings.strifeLeftDirectionKey;
            Data.strifeRightDirectionKey = Settings.playerSettings.strifeRightDirectionKey;

            Data.focusKey = Settings.playerSettings.focusKey;
            Data.interactionKey = Settings.playerSettings.interactionKey;

            Assets = new AppAssets();

            //ensure that duplicate names check is set to false
            Settings.projectSettings.checkDuplicatePhotonNames = false;
            Data.FixedAvatarUsed = Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Custom) || Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe);


            if (!Settings.projectSettings.streamingMode.Equals(WebClientMode.PureWeb) || Application.isEditor)
            {
                Debug.Log("Creating AudioListener");

                AudioListener aListener = new GameObject("_BRANDLAB360_AUDIOLISTENER").AddComponent<AudioListener>();
                DontDestroyOnLoad(aListener.gameObject);
            }



#if BRANDLAB360_STREAMING && !UNITY_EDITOR

            Debug.Log("Creating streaming mode");
            UnityEngine.Object streaming = Resources.Load(Settings.projectSettings.streamingManagerPrefab);
            GameObject goStreaming = Instantiate((GameObject)streaming, Vector3.zero, Quaternion.identity);
            goStreaming.transform.localScale = Vector3.one;
            goStreaming.transform.position = Vector3.zero;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("Creating WebGL JSLib prefab");
            UnityEngine.Object webglPrefab = Resources.Load("WebGlManager");

            if (webglPrefab != null)
            {
                GameObject webglGO = Instantiate((GameObject)webglPrefab, Vector3.zero, Quaternion.identity);
                webglGO.transform.localScale = Vector3.one;
                webglGO.transform.position = Vector3.zero;
                webglGO.name = "WebGlManager";
            }
#endif

#if UNITY_EDITOR

            if (Settings.editorTools.createWebClientSimulator)
            {
                UnityEngine.Object prefab = Resources.Load("Tools/" + Settings.editorTools.simulator);
                GameObject go = Instantiate((GameObject)prefab, Vector3.zero, Quaternion.identity);
                go.transform.localScale = Vector3.one;
            }
#endif

            DontDestroyOnLoad(gameObject);

            IsCreated = true;
        }

        private void Start()
        {
            BlackScreen.Instance.Show(true);

            //begin process
            AdminManager.Instance.Begin();

            //if simulator is present we can load normally
            webclientSimulator = FindFirstObjectByType<WebClientSimulator>(FindObjectsInactive.Include);

            //wont work unless this is on for editor
            m_notStreamed = (Settings.projectSettings.streamingMode == WebClientMode.None || Application.isEditor);

            //add web client listener
            WebclientManager.WebClientListener += BeginCallback;
            WebclientManager.Instance.WebCommsVersion = Settings.projectSettings.webCommsVersionList[Settings.projectSettings.webCommsVersion];

#if UNITY_EDITOR
            WebclientManager.Instance.WebCommsVersion = "v1";
#endif

            if (m_notStreamed)
            {
                if (webclientSimulator == null)
                {
                    StartedResponse msg = new StartedResponse();
                    msg.isMobile = false;
                    msg.name = Settings.projectSettings.ProjectID;
                    msg.room = 0;
                    msg.releaseMode = "development";
                    BeginCallback(JsonUtility.ToJson(msg));
                }
                else
                {
                    StartCoroutine(WebClientSimulator.Instance.simulateStartedMessage());
                }
            }
            else
            {
                WebclientManager.Instance.Begin();

                if(Settings.projectSettings.useStartedMessageStreamTimeout)
                {
                    StartCoroutine(StreamingTimerBeforeCancel(0));
                }
            }
        }

        private void OnApplicationQuit()
        {
            IsCreated = false;
            Assets.Dispose();
        }

        /// <summary>
        /// callback for API setup
        /// </summary>
        /// <param name="success"></param>
        private void OnAPICallback(bool success)
        {
            ApiManager.Instance.OnAccessSuccess -= OnAPICallback;

            //request from the webclientcommsmanager to see if there is there is any data
            WebClientCommsManager.Instance.OnRecieveParams += OnWebClientCommsParams;

            //issue with URL params
             if (m_notStreamed)
             {
                Debug.Log("App is not streamed");

                if (webclientSimulator == null)
                {
                    OnWebClientCommsParams();
                }
                else
                {
                    StartCoroutine(WebClientSimulator.Instance.simulateURLParamsMessage());
                }
             }
             else
             {
                Debug.Log("App is streamed");

                if (Settings.projectSettings.useURLParams)
                {
                    if (m_streamBeginFailed)
                    {
                        Debug.Log("App stream startedMSG failed, diverting URL params & IDB");

                        WebClientCommsManager.Instance.OnRecieveParams -= OnWebClientCommsParams;
                        Begin();
                    }
                    else
                    {
                        Debug.Log("App stream startedMSG success, registering URL params");

                        WebClientCommsManager.Instance.Begin();
                    }
                }
                else
                {
                    if (m_streamBeginFailed)
                    {
                        Debug.Log("App stream startedMSG failed, diverting URL params & IDB");

                        WebClientCommsManager.Instance.OnRecieveParams -= OnWebClientCommsParams;
                        Begin();
                    }
                    else
                    {
                        Debug.Log("App stream startedMSG success, registering IDB");
                        OnWebClientCommsParams();
                    }
                }
             }
        }

        private IEnumerator StreamingTimerBeforeCancel(int stage)
        {
            float timer = 0.0f;

            while (timer < Settings.projectSettings.startedMessageStreamTimeout)
            {
                if (stage.Equals(0))
                {
                    if (m_streamingCallbackCalled) yield break;
                }

                yield return new WaitForSeconds(1.0f);

                timer += 1.0f;
            }

            if (stage.Equals(0))
            {
                if (!m_streamingCallbackCalled)
                {
                    m_streamBeginFailed = true;
                    StartedResponse msg = new StartedResponse();
                    msg.isMobile = false;
                    msg.name = Settings.projectSettings.ProjectID;
                    msg.room = 0;
                    msg.releaseMode = "production";

                    string temp = JsonUtility.ToJson(msg);

                    Debug.Log("App started MSG timeout on Stream. Setting default started MSG=" + temp);

                    BeginCallback(temp);
                }
            }
        }

        /// <summary>
        /// Callback when webclient recieves responce
        /// </summary>
        /// <param name="json"></param>
        private void BeginCallback(string json)
        {
            //first fails safe
            if (json.Contains("consoleLog") || string.IsNullOrEmpty(json)) return;

            Debug.Log("App Manager processing StartedResponse=" + json);

            var response = JsonUtility.FromJson<StartedResponse>(json).OrDefaultWhen(x => string.IsNullOrEmpty(x.name) && string.IsNullOrEmpty(x.releaseMode));

            if (response == null)
            {
                Debug.Log("App Manager failed StartedResponse=" + json);
                return;
            }

            m_streamingCallbackCalled = true;

            Debug.Log("App Mnager success StartedResponse=" + json);
            
            WebclientManager.WebClientListener -= BeginCallback;

            Data.WebClientResponce = json;
            Data.IsMobile = response.isMobile;
            Data.RoomID = response.room;
            Data.ProjectID = response.name;
            Data.releaseMode = response.releaseMode;

            if (Data.IsMobile)
            {
                Settings.HUDSettings.useTooltips = false;
#if !UNITY_EDITOR

                //if on mobile, then default join room mode to duplicate. Looby not available on mobile
                Settings.projectSettings.joinRoomMode = StartupManager.JoinRoomMode.Duplicate;

#endif
            }

            Debug.Log("Caching StartedResponce=" + Data.WebClientResponce);

#if UNITY_EDITOR
            Data.ProjectID = Settings.projectSettings.ProjectID;

            if (Settings.editorTools.ignoreIntroScene)
            {
                UserExists = false;
                Data.NickName = "User";
                m_skipAvatar = true;
                m_skipName = true;
                LoginComplete();
                return;
            }
#endif

            //add api listener
            ApiManager.Instance.OnAccessSuccess += OnAPICallback;
            //Call API
            ApiManager.Instance.Begin();
        }

        private void OnWebClientCommsParams()
        {
            WebClientCommsManager.Instance.OnRecieveParams -= OnWebClientCommsParams;

            bool iDBEnabled = Settings.projectSettings.loginMode.Equals(LoginMode.Standard) ? Settings.projectSettings.useIndexedDB : false;

            //check the param data in the webclientcommsmanager
            if (WebClientCommsManager.Instance.UrlParameters.Count > 0)
            {
                Debug.Log("App Manager OnWebClientCommsParams count > 0");

                if (WebClientCommsManager.Instance.UrlParameters.ContainsKey("InviteCode"))
                {
                    //need to check if the params include invite code. if so then set the invite code to the roomID
                    Data.inviteCode = WebClientCommsManager.Instance.UrlParameters["InviteCode"].ToString();
                }

                if (WebClientCommsManager.Instance.UrlParameters.ContainsKey("Username") &&
                    WebClientCommsManager.Instance.UrlParameters.ContainsKey("Avatar"))
                {
                    Debug.Log("Using URL Params Data for processing user data");

                    m_urlParamsExist = true;
                    iDBEnabled = false;
                    iDbResponse responce = new iDbResponse();
                    DBUserData udata = new DBUserData();
                    udata.user = WebClientCommsManager.Instance.UrlParameters["Username"].ToString();
                    udata.isAdmin = (WebClientCommsManager.Instance.UrlParameters.ContainsKey("IsAdmin")) ? WebClientCommsManager.Instance.UrlParameters["IsAdmin"].Equals("1") ? 1 : 0 : 0;
                    udata.json = WebClientCommsManager.Instance.UrlParameters["Avatar"].ToString();

                    if (WebClientCommsManager.Instance.UrlParameters.ContainsKey("Friends"))
                    {
                        udata.friends = WebClientCommsManager.Instance.UrlParameters["Friends"].ToString();
                    }

                    if (WebClientCommsManager.Instance.UrlParameters.ContainsKey("Profile"))
                    {
                        udata.profile = WebClientCommsManager.Instance.UrlParameters["Profile"].ToString();
                    }

                    responce.iDbEntry = JsonUtility.ToJson(udata);
                    OnIndexedDB(JsonUtility.ToJson(responce));
                    return;
                }
            }

            if (iDBEnabled)
            {
#if UNITY_EDITOR
                if (Settings.editorTools.createWebClientSimulator)
                {
                    IndexedDbManager.Instance.iDbListener += OnIndexedDB;
                    WebClientSimulator.Instance.ProcessIndexedDBRequest("userData");
                }
                else
                {
                    Begin();
                }

#else
                if(Settings.projectSettings.streamingMode != WebClientMode.None)
                {
                    IndexedDbManager.Instance.iDbListener += OnIndexedDB;
                    IndexedDbManager.Instance.GetEntry("userData");
                }
                else
                {
                    Begin();
                }
#endif
            }
            else
            {
                Begin();
            }
        }

        private void OnIndexedDB(string responce)
        {
            Debug.Log("App Manager processing OnIndexedDB=" + responce);

            var iDB = JsonUtility.FromJson<iDBUserDataResponse>(responce).OrDefaultWhen(x => x.iDbEntry == null);

            if (iDB == null)
            {
                Debug.Log("App Manager failed OnIndexedDB=" + responce);
                return;
            }

            Debug.Log("App Manager success OnIndexedDB=" + responce);

            DBUserData udata = null;

            IndexedDbManager.Instance.iDbListener -= OnIndexedDB;


#if UNITY_EDITOR || m_urlParamsExist

            iDbResponse iDBEditor = JsonUtility.FromJson<iDbResponse>(responce);
            udata = JsonUtility.FromJson<DBUserData>(iDBEditor.iDbEntry);
#else
            if (iDB != null)
            {
                if(m_urlParamsExist)
                {
                    iDbResponse iDBURLParams = JsonUtility.FromJson<iDbResponse>(responce);
                    udata = JsonUtility.FromJson<DBUserData>(iDBURLParams.iDbEntry);
                }
                else
                {
                    udata = new DBUserData();

                    if(iDB.iDbEntry != null)
                    {
                        if(iDB.iDbEntry.value is string)
                        {
                            string[] dataSplit = iDB.iDbEntry.value.Split(':');

                            if (dataSplit.Length >= 5)
                            {
                                string[] user = dataSplit[0].Split('-');
                                string[] admin = dataSplit[1].Split('-');
                                string[] json = dataSplit[2].Split('-');
                                string[] friends = dataSplit[3].Split('-');
                                string[] profile = dataSplit[4].Split('-');

                                if(user.Length == 2)
                                {
                                    udata.user = user[1];
                                }

                                if(admin.Length == 2)
                                {
                                    udata.isAdmin = int.Parse(admin[1]);
                                }

                                if(json.Length == 2)
                                {
                                    udata.json = json[1];
                                }

                                if(friends.Length == 2)
                                {
                                    udata.friends = friends[1];
                                }
                                else
                                {
                                    udata.friends = "";
                                }

                                if(profile.Length == 2)
                                {
                                    udata.profile = profile[1];
                                }
                                else
                                {
                                    udata.profile = "";
                                }

                                if(dataSplit.Length > 5)
                                {
                                    string[] games = dataSplit[5].Split('-');

                                    if(games.Length == 2)
                                    {
                                        udata.games = games[1];
                                    }
                                    else
                                    {
                                        udata.games = "";
                                    }
                                }

                            }
                        }
                    }
                }
            }
#endif

            if (udata != null)
            {
                Data.IsAdminUser = udata.isAdmin > 0 ? true : false;

                Debug.Log("IDB user is admin = [" + Data.IsAdminUser + "]");

                Data.NickName = udata.user;
                Data.CustomiseJson = udata.json;
                Data.CustomiseFriends = udata.friends;
                Data.CustomiseProfile = udata.profile;
                Data.RawGameData = udata.games;
                Data.LoginProfileData = new ProfileData();

                //Raw profile breakdown
                //Name*|About*|pic*
                string[] profileBreakdown = Data.CustomiseProfile.Split("|");
                ProfileData pd = new ProfileData();

                for (int i = 0; i < profileBreakdown.Length; i++)
                {
                    string[] split = profileBreakdown[i].Split('*');

                    if (split[0].Equals("Name"))
                    {
                        pd.name = split[1];
                    }
                    else if (split[0].Equals("About"))
                    {
                        pd.about = split[1];
                    }
                    else if (split[0].Equals("Pic") || split[0].Contains("Pic"))
                    {
                        if (split.Length > 1)
                        {
                            pd.picture_url = split[1];
                        }
                        else
                        {
                            pd.picture_url = split[0].Substring(3);
                        }
                    }

                }

                //set up proper profile class
                Data.LoginProfileData = pd;
                Data.LoginProfileData.username = Data.NickName;
                Data.LoginProfileData.password = "";
                Data.LoginProfileData.avatar_data = Data.CustomiseJson;
                Data.RawFriendsData = Data.CustomiseFriends;

                Debug.Log("Customise Json = " + Data.CustomiseJson);

                if (!string.IsNullOrEmpty(Data.NickName))
                {
                    m_skipName = !Settings.projectSettings.skipName ? Settings.projectSettings.useIndexedDB ? Settings.projectSettings.skipNameOnIndexedDB : Settings.projectSettings.skipName : Settings.projectSettings.skipName;
                }
                else
                {
                    m_skipName = false;
                }

                if (!string.IsNullOrEmpty(Data.CustomiseJson))
                {
                    Debug.Log("Creating Avatar Hash from customise json");

                    Hashtable hash = CustomiseAvatar.GetAvatarHashFromString(Data.CustomiseJson);
                    string avatarType = "";

                    if (hash.ContainsKey("TYPE"))
                    {
                        avatarType = hash["TYPE"].ToString();
                    }

                    if (!string.IsNullOrEmpty(avatarType))
                    {
                        if (avatarType.ToString().Equals("Simple") && !Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Simple))
                        {
                            m_skipAvatar = false;
                        }
                        else if (avatarType.ToString().Equals("Standard") && !Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Standard))
                        {
                            m_skipAvatar = false;
                        }
                        else
                        {
                            m_skipAvatar = Settings.projectSettings.skipAvatarOnIndexedDB;
                        }
                    }
                    else
                    {
                        m_skipAvatar = Settings.projectSettings.skipAvatarOnIndexedDB;
                    }
                }
                else
                {
                    m_skipAvatar = false;
                }

                UserExists = !string.IsNullOrEmpty(Data.NickName) && !string.IsNullOrEmpty(Data.CustomiseJson);
            }
            else
            {
                UserExists = false;
            }

            Begin();
        }

        /// <summary>
        /// Called locally to begin the app loadin process
        /// </summary>
        private void Begin()
        {
            AppLogin login = FindFirstObjectByType<AppLogin>(FindObjectsInactive.Include);

            if (login != null)
            {
                if (Settings.projectSettings.loginMode.Equals(LoginMode.Standard))
                {
                    bool skipPassword = m_urlParamsExist ? true : Settings.projectSettings.skipPassword;
                    bool skipName = m_urlParamsExist ? true : m_skipName;

                    login.Begin(Settings.projectSettings.useAdminUser, skipPassword, skipName);
                }
                else
                {
                    login.Begin(Settings.projectSettings.useAdminUser, false, false);
                }
            }
            else
            {
                Debug.Log("No login Exists. Skipping login");

                LoginComplete();
            }
        }

        /// <summary>
        /// Util to request URL reditect with necessary user params
        /// </summary>
        /// <param name="url"></param>
        public void RedirectURL(string url)
        {
            string inviteCode = "";

            if (!string.IsNullOrEmpty(Data.inviteCode))
            {
                inviteCode = "&" + "InviteCode" + "=" + Data.inviteCode;
            }

            string userParams = GetUserURLParams();

            if (string.IsNullOrEmpty(userParams))
            {
                userParams += "&" + "Username" + "=" + Data.NickName;
                userParams += "&" + "IsAdmin" + "=" + (Data.IsAdminUser ? "1" : "0").ToString();

                if (!Data.FixedAvatarUsed)
                {
                    userParams += "&" + "Avatar" + "=" + Data.CustomiseJson;
                }
                else
                {
                    userParams += "&" + "Avatar" + "=" + Data.FixedAvatarName;
                }

                userParams += "&" + "PlayerControls" + "=" + Data.CustomiseControls;
                userParams += "&" + "Friends" + "=" + Data.CustomiseFriends;
                userParams += "&" + "Profile" + "=" + Data.CustomiseProfile;
                userParams += "&" + "Games" + "=" + Data.RawGameData;
            }

            WebClientCommsManager.Instance.RequestRedirect(url + userParams + inviteCode);
        }


        /// <summary>
        /// Util to get user params
        /// </summary>
        /// <returns></returns>
        public string GetUserURLParams()
        {
            List<string> keys = new List<string>();
            keys.Add("Username");
            keys.Add("IsAdmin");
            keys.Add("Avatar");
            keys.Add("PlayerControls");
            keys.Add("Friends");
            keys.Add("Profile");
            keys.Add("Games");

            return WebClientCommsManager.Instance.CreateURLParamString(keys);
        }

        /// <summary>
        /// Called when login is completed
        /// </summary>
        public void LoginComplete()
        {
            bool useAvatarScene = Settings.projectSettings.loginMode.Equals(LoginMode.Standard) ? Settings.projectSettings.avatarMode.Equals(AvatarMode.Custom) && !m_skipAvatar : string.IsNullOrEmpty(Data.CustomiseJson);

            if (useAvatarScene)
            {
                BlackScreen.Instance.Show(true);
                //load avatar scene
                StartCoroutine(LoadSceneAsync(Settings.projectSettings.avatarSceneName, LoadSceneMode.Additive, UnloadIntroScene));
            }
            else
            {
                //set data
                if (m_skipAvatar && !string.IsNullOrEmpty(Data.CustomiseJson))
                {
                    Data.Sex = Data.CustomiseJson.Contains("Male") ? CustomiseAvatar.Sex.Male : CustomiseAvatar.Sex.Female;
                    Data.Avatar = null;

                    if (Data.FixedAvatarUsed)
                    {
                        Data.FixedAvatarName = Data.CustomiseJson;
                    }

                    //need to update the the indexedDB if the user has changed thier name // might do this by default all the time

                    if (Settings.projectSettings.loginMode.Equals(LoginMode.Standard))
                    {
                        if (!m_skipName && Settings.projectSettings.useIndexedDB)
                        {
                            DBUserData udata = new DBUserData();
                            udata.user = Data.NickName;
                            udata.json = string.IsNullOrEmpty(Data.CustomiseJson) ? "" : Data.CustomiseJson;
                            udata.friends = "Friends-";
                            udata.profile = "Profile-";
                            udata.games = "Games-";

                            IndexedDbManager.Instance.UpdateEntry("userData", JsonUtility.ToJson(udata));
                        }
                    }
                }

                m_unloadIntro = true;

                Data.StartupCompleted = true;
                BlackScreen.Instance.Show(true);

                if (Settings.projectSettings.useFirebase && !Application.isEditor)
                {
                    //send handle to firebase manager
                    FirebaseManager.Instance.FirebaseListener += OnFirebaseCallback;
                    FirebaseManager.Instance.InitializeFirebase();
                }
                else
                {
                    //load main scene async
                    StartCoroutine(LoadSceneAsync(Settings.projectSettings.mainSceneName, LoadSceneMode.Additive, CheckAppStatus));
                }
            }
        }

        private void OnFirebaseCallback(string auth, string id)
        {
            FirebaseManager.Instance.FirebaseListener -= OnFirebaseCallback;

            if (string.IsNullOrEmpty(auth) || string.IsNullOrEmpty(id))
            {
                Debug.Log("Firebase auth is null OR id is null, [" + auth + "][" + id + "]");
            }
            else
            {
                Debug.Log("Firebase auth [" + auth + "], ID [" + id + "]");
            }

            if (m_skipAvatar)
            {
                //load main scene async
                StartCoroutine(LoadSceneAsync(Settings.projectSettings.mainSceneName, LoadSceneMode.Additive, CheckAppStatus));
            }
            else
            {
                //load main scene async
                StartCoroutine(LoadSceneAsync(Settings.projectSettings.mainSceneName, LoadSceneMode.Single, CheckAppStatus));
            }
        }

        /// <summary>
        /// Called when avatar has been applied
        /// </summary>
        public void OnAvatarApplied()
        {
            if (!Data.StartupCompleted)
            {
                Data.StartupCompleted = true;

                //show black out with spinner
                BlackScreen.Instance.Show(true);

                if (Settings.projectSettings.useFirebase && !Application.isEditor)
                {
                    //send handle to firebase manager
                    FirebaseManager.Instance.FirebaseListener += OnFirebaseCallback;
                    FirebaseManager.Instance.InitializeFirebase();
                }
                else
                {
                    //load main scene async
                    StartCoroutine(LoadSceneAsync(Settings.projectSettings.mainSceneName, LoadSceneMode.Single, CheckAppStatus));
                }
            }
            else
            {
                AvatarManager.Instance.Customise(PlayerManager.Instance.GetLocalPlayer());
                HUDManager.Instance.CloseCustmizeAvatar();
            }
        }

        /// <summary>
        /// Async scene operation by build index
        /// </summary>
        /// <param name="load"></param>
        /// <param name="sceneName"></param>
        /// <param name="mode"></param>
        /// <param name="callback"></param>
        /// <param name="startCoreManager"></param>
        public void SceneAsyncOperation(bool load, int sceneName, LoadSceneMode mode = LoadSceneMode.Additive, System.Action callback = null, bool startCoreManager = false)
        {
            if (load)
            {
                StartCoroutine(LoadSceneAsync(sceneName, mode, callback, startCoreManager));
            }
            else
            {
                UnloadScene(sceneName);
            }
        }

        /// <summary>
        /// Async scene operation by scene name
        /// </summary>
        /// <param name="load"></param>
        /// <param name="sceneName"></param>
        /// <param name="mode"></param>
        /// <param name="callback"></param>
        /// <param name="startCoreManager"></param>
        public void SceneAsyncOperation(bool load, string sceneName, LoadSceneMode mode = LoadSceneMode.Additive, System.Action callback = null, bool startCoreManager = false)
        {
            if (load)
            {
                StartCoroutine(LoadSceneAsync(sceneName, mode, callback, startCoreManager));
            }
            else
            {
                UnloadScene(sceneName);
            }
        }

        /// <summary>
        /// Loads scene async by scene name
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="mode"></param>
        private IEnumerator LoadSceneAsync(string scene, LoadSceneMode mode, System.Action callback = null, bool startCoreManager = false)
        {
            Debug.Log("Loading scene [" + scene + "], mode: " + mode.ToString());

            AFKManager.Instance.Process = false;

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene, mode);

            // Wait until the asynchronous scene fully loads
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            AFKManager.Instance.Process = true;

            if (callback != null)
            {
                callback.Invoke();
            }

            if (startCoreManager)
            {
                CheckAppStatus();
            }
        }

        /// <summary>
        /// Loads scene async by scene build index
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="mode"></param>
        private IEnumerator LoadSceneAsync(int scene, LoadSceneMode mode, System.Action callback = null, bool startCoreManager = false)
        {
            Debug.Log("Loading scene [" + scene + "], mode: " + mode.ToString());

            AFKManager.Instance.Process = false;

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene, mode);

            // Wait until the asynchronous scene fully loads
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            AFKManager.Instance.Process = true;

            if (callback != null)
            {
                callback.Invoke();
            }

            if (startCoreManager)
            {
                CheckAppStatus();
            }
        }

        /// <summary>
        /// Called to load scene by name
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="mode"></param>
        public void LoadScene(string scene, LoadSceneMode mode)
        {
            Debug.Log("Loading scene [" + scene + "], mode: " + mode.ToString());

            SceneManager.LoadScene(scene, mode);
        }

        /// <summary>
        /// Called to load scene by buiild index
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="mode"></param>
        public void LoadScene(int scene, LoadSceneMode mode)
        {
            Debug.Log("Loading scene [" + scene + "], mode: " + mode.ToString());

            SceneManager.LoadScene(scene, mode);
        }

        /// <summary>
        /// Called to unload scene by name
        /// </summary>
        /// <param name="scene"></param>
        public void UnloadScene(string scene)
        {
            Scene temp = SceneManager.GetSceneByName(scene);

            if (temp != null && temp.isLoaded)
            {
                Debug.Log("Unloading scene [" + scene + "]");

                SceneManager.UnloadSceneAsync(scene);
            }
            else
            {
                Debug.Log("Scene does not exist OR is not loaded [" + scene + "]");
            }
        }

        /// <summary>
        /// Called to unload scenen by build index
        /// </summary>
        /// <param name="scene"></param>
        public void UnloadScene(int scene)
        {
            Scene temp = SceneManager.GetSceneByBuildIndex(scene);

            if (temp != null && temp.isLoaded)
            {
                Debug.Log("Unloading scene [" + scene + "]");

                SceneManager.UnloadSceneAsync(scene);
            }
            else
            {
                Debug.Log("Scene does not exist OR is not loaded [" + scene + "]");
            }
        }

        /// <summary>
        /// Called to unload intro scene
        /// </summary>
        private void UnloadIntroScene()
        {
            BlackScreen.Instance.Show(false);
            UnloadScene(Settings.projectSettings.loginSceneName);
        }

        /// <summary>
        /// Called when the main scene has loaded
        /// </summary>
        private void CheckAppStatus()
        {
            if (m_unloadIntro)
            {
                UnloadIntroScene();
                m_unloadIntro = false;
            }

            if (Data.StartupCompleted)
            {
                //hide black out with spinner
                BlackScreen.Instance.Show(false);
                //begin core manager
                CoreManager.Instance.Begin();
            }
        }

        /// <summary>
        /// Called to toggle the apps video chat
        /// </summary>
        /// <param name="open"></param>
        /// <param name="channel"></param>
        public void ToggleVideoChat(bool open, string channel)
        {
            var msg = new ToggleVideoChatMessage();
            msg.Username = Data.NickName;
            var json = "";
            bool webCamClosed = false;

            if (Data.WebCamActive)
            {
                if (m_videoProcess != null)
                {
                    StopCoroutine(m_videoProcess);
                    m_videoProcess = null;
                }

                webCamClosed = true;

                //esnsure web cam is closed
                msg.ToggleVideoChat = false;
                msg.Channel = Data.videoChannel;
                json = JsonUtility.ToJson(msg);
                WebclientManager.Instance.Send(json);
            }

            if (open)
            {
                if (MMOChat.Instance.OnCall)
                {
                    MMOChat.Instance.EndVoiceCall(PlayerManager.Instance.GetLocalPlayer().ID, true);
                }

                Data.WebCamActive = true;
                Data.videoChannel = channel;

                if (webCamClosed || Data.GlobalVideoChatUsed)
                {
                    m_videoProcess = StartCoroutine(DelayVideoChatOpen(msg));
                }
                else
                {
                    msg.ToggleVideoChat = true;
                    msg.Channel = Data.videoChannel;
                    json = JsonUtility.ToJson(msg);
                    WebclientManager.Instance.Send(json);
                }
            }
            else
            {
                Data.WebCamActive = false;
                Data.videoChannel = "";
            }
        }

        private IEnumerator DelayVideoChatOpen(ToggleVideoChatMessage msg)
        {
            yield return new WaitForSeconds(5.0f);

            msg.ToggleVideoChat = true;
            msg.Channel = Data.videoChannel;
            var json = JsonUtility.ToJson(msg);
            WebclientManager.Instance.Send(json);
        }

        public void AddUserProfileData(string key, string data)
        {
            if (Data.LoginProfileData != null)
            {
                Data.LoginProfileData.AddAdditionalData(key, data);
                UpdateLoginsAPI();
            }
        }

        public async void UpdateLoginsAPI()
        {
            await APIComms();
        }

        private async Task<bool> APIComms()
        {
            if (Settings.projectSettings.loginAPIMode.Equals(APILoginMode._Salesforce) || Settings.projectSettings.loginAPIMode.Equals(APILoginMode._Hubspot))
            {
                if (API == null)
                {
#if UNITY_EDITOR
                    Debug.LogError("ProjectAPISettings resource has not been created!! Please open the Control Panel and create settings resource");
#endif
                    return false;
                }
            }

            bool success = false;

            Dictionary<string, string> data = new Dictionary<string, string>();

            bool firstnameUsed = false;
            bool firstnameAttained = false;

            switch (Settings.projectSettings.loginAPIMode)
            {
                case APILoginMode._BrandLab:
                    string projectID = string.IsNullOrEmpty(Settings.projectSettings.clientName) ? Data.ProjectID : Settings.projectSettings.clientName;
                    LoginsAPI.Instance.UpdateUser(Data.NickName, projectID, JsonUtility.ToJson(Data.LoginProfileData), Data.LoginProfileData.password, Data.RawFriendsData, Data.RawGameData);
                    break;
                case APILoginMode._Salesforce:

                    //loop through all and get relevant data
                    for (int i = 0; i < API.salesforceSettings.loginDataToCollect.Length; i++)
                    {
                        if (API.salesforceSettings.loginDataToCollect[i].ignore) continue;

                        if (API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._ID))
                        {
                            data.Add(API.salesforceSettings.loginDataToCollect[i].tableField, Data.LoginProfileData.id.ToString());
                        }
                        else if (API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._name))
                        {
                            for (int j = i + 1; j < API.salesforceSettings.loginDataToCollect.Length; j++)
                            {
                                if (API.salesforceSettings.loginDataToCollect[j].profileReference.Equals(ProfileDataReference._name))
                                {
                                    firstnameUsed = true;
                                }
                            }

                            if (firstnameUsed)
                            {
                                string[] nameSplit = Data.LoginProfileData.name.Split(" ");

                                if (!firstnameAttained)
                                {
                                    firstnameAttained = true;
                                    data.Add(API.salesforceSettings.loginDataToCollect[i].tableField, nameSplit[0]);
                                }
                                else
                                {
                                    data.Add(API.salesforceSettings.loginDataToCollect[i].tableField, nameSplit[1]);
                                }
                            }
                            else
                            {
                                data.Add(API.salesforceSettings.loginDataToCollect[i].tableField, Data.LoginProfileData.name);
                            }
                        }
                        else if (API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._about))
                        {
                            data.Add(API.salesforceSettings.loginDataToCollect[i].tableField, Data.LoginProfileData.about);
                        }
                        else if (API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._avatar))
                        {
                            data.Add(API.salesforceSettings.loginDataToCollect[i].tableField, Data.LoginProfileData.avatar_data);
                        }
                        else if (API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._email))
                        {
                            data.Add(API.salesforceSettings.loginDataToCollect[i].tableField, Data.LoginProfileData.email);
                        }
                        else if (API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._friendsData))
                        {
                            data.Add(API.salesforceSettings.loginDataToCollect[i].tableField, Data.RawFriendsData);
                        }
                        else if (API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._gamesData))
                        {
                            data.Add(API.salesforceSettings.loginDataToCollect[i].tableField, Data.RawGameData);
                        }
                        else if (API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._password))
                        {
                            data.Add(API.salesforceSettings.loginDataToCollect[i].tableField, Data.LoginProfileData.password);
                        }
                        else if (API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._picture))
                        {
                            data.Add(API.salesforceSettings.loginDataToCollect[i].tableField, Data.LoginProfileData.picture_url);
                        }
                        else if (API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._admin))
                        {
                            data.Add(API.salesforceSettings.loginDataToCollect[i].tableField, Data.IsAdminUser ? "true" : "false");
                        }
                        else if (API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._playerSettings))
                        {
                            data.Add(API.salesforceSettings.loginDataToCollect[i].tableField, Data.LoginProfileData.player_settings);
                        }
                        else if (API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._termsAndConditions))
                        {
                            data.Add(API.salesforceSettings.loginDataToCollect[i].tableField, Data.LoginProfileData.acceptTAC ? "true" : "false");
                        }
                        else if (API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._username))
                        {
                            data.Add(API.salesforceSettings.loginDataToCollect[i].tableField, Data.LoginProfileData.username);
                        }
                    }

                    success = await API.salesforceSettings.Push(data);
                    break;
                case APILoginMode._Hubspot:
                    //loop through all and get relevant data
                    for (int i = 0; i < API.hubspotSettings.properties.Length; i++)
                    {
                        if (API.hubspotSettings.properties[i].ignore) continue;

                        if (API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._ID))
                        {
                            data.Add(API.hubspotSettings.properties[i].tableField, Data.LoginProfileData.id.ToString());
                        }
                        else if (API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._name))
                        {
                            for (int j = i + 1; j < API.hubspotSettings.properties.Length; j++)
                            {
                                if (API.hubspotSettings.properties[j].profileReference.Equals(ProfileDataReference._name))
                                {
                                    firstnameUsed = true;
                                }
                            }

                            if (firstnameUsed)
                            {
                                string[] nameSplit = Data.LoginProfileData.name.Split(" ");

                                if (!firstnameAttained)
                                {
                                    firstnameAttained = true;
                                    data.Add(API.hubspotSettings.properties[i].tableField, nameSplit[0]);
                                }
                                else
                                {
                                    data.Add(API.hubspotSettings.properties[i].tableField, nameSplit[1]);
                                }
                            }
                            else
                            {
                                data.Add(API.hubspotSettings.properties[i].tableField, Data.LoginProfileData.name);
                            }
                        }
                        else if (API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._about))
                        {
                            data.Add(API.hubspotSettings.properties[i].tableField, Data.LoginProfileData.about);
                        }
                        else if (API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._avatar))
                        {
                            data.Add(API.hubspotSettings.properties[i].tableField, Data.LoginProfileData.avatar_data);
                        }
                        else if (API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._email))
                        {
                            data.Add(API.hubspotSettings.properties[i].tableField, Data.LoginProfileData.email);
                        }
                        else if (API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._friendsData))
                        {
                            data.Add(API.hubspotSettings.properties[i].tableField, Data.RawFriendsData);
                        }
                        else if (API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._gamesData))
                        {
                            data.Add(API.hubspotSettings.properties[i].tableField, Data.RawGameData);
                        }
                        else if (API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._password))
                        {
                            data.Add(API.hubspotSettings.properties[i].tableField, Data.LoginProfileData.password);
                        }
                        else if (API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._picture))
                        {
                            data.Add(API.hubspotSettings.properties[i].tableField, Data.LoginProfileData.picture_url);
                        }
                        else if (API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._admin))
                        {
                            data.Add(API.hubspotSettings.properties[i].tableField, Data.IsAdminUser ? "true" : "false");
                        }
                        else if (API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._playerSettings))
                        {
                            data.Add(API.hubspotSettings.properties[i].tableField, Data.LoginProfileData.player_settings);
                        }
                        else if (API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._termsAndConditions))
                        {
                            data.Add(API.hubspotSettings.properties[i].tableField, Data.LoginProfileData.acceptTAC ? "true" : "false");
                        }
                        else if (API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._username))
                        {
                            data.Add(API.hubspotSettings.properties[i].tableField, Data.LoginProfileData.username);
                        }
                    }

                    success = await API.hubspotSettings.PushContact(Data.LoginProfileData.username, data);
                    break;
                default:
                    BaseCustomAPI customAPI = FindFirstObjectByType<BaseCustomAPI>(FindObjectsInactive.Include);

                    if (customAPI != null)
                    {
                        success = await customAPI.Push(Data.LoginProfileData, Data.RawFriendsData, Data.RawGameData, Data.IsAdminUser);
                    }
                    break;
            }

            return success;
        }

        [System.Serializable]
        public class iDBUserDataResponse
        {
            public iDBUserData iDbEntry;
        }

        [System.Serializable]
        public class iDBUserData
        {
            public string key;
            public string value;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AppManager), true)]
        public class AppManager_Editor : BaseInspectorEditor
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
