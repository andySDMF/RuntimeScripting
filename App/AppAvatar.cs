using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class AppAvatar : MonoBehaviour
    {
        [Header("Screens")]
        public GameObject avatarScreen;
        public GameObject RPMScreen;
        public GameObject brandlabLogo;

        [Header("Overlay")]
        public FadeOutIn fadeOverlay;

        [Header("Config")]
        public Transform configHolder;

        [Header("RPM")]
        [SerializeField]
        private GameObject viewRPM;

        [Header("Environment")]
        [SerializeField]
        private Environment environment;

        private CustomiseAvatar m_customerHandler;
        private bool isFixedAvatarUsed = false;

        public System.Action<string> RPMActionOnRecieved { get; set; }

        private void Awake()
        {
            AppManager appManager = FindFirstObjectByType<AppManager>(FindObjectsInactive.Include);

            if (appManager == null)
            {
                //need to load login scene
                SceneManager.LoadScene(0);

                return;
            }

            brandlabLogo.SetActive(AppManager.Instance.Settings.HUDSettings.showBrandlabLogo);

            //this is to ensure the config does not interfear with the current scene
            configHolder.position = new Vector3(0.0f, -100, 0.0f);

            isFixedAvatarUsed = appManager.Data.FixedAvatarUsed;
        }

        private void Start()
        {
            if(AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe))
            {
                bool openRPMScreen = false;

                //need to open the selection screen first
                for(int i = 0; i < AppManager.Instance.Settings.playerSettings.fixedAvatars.Count; i++)
                {
                    if(AppManager.Instance.Settings.playerSettings.fixedAvatars[i].Contains("RPM_"))
                    {
                        openRPMScreen = true;
                        break;
                    }
                }

                if(openRPMScreen && string.IsNullOrEmpty(AppManager.Instance.Data.FixedAvatarName))
                {
                    RPMScreen.SetActive(true);
                    return;
                }
                else
                {
                    OpenRPM();
                    return;
                }
            }

            environment.Activate(false);
            avatarScreen.SetActive(true);

            //fade in
            fadeOverlay.PauseTime = 0.5f;
            fadeOverlay.FadeType = FadeOutIn.FadeAction.In;
            fadeOverlay.ImplementChange = OnFadeInCallback;
            fadeOverlay.Callback = null;

            CameraBrain.Instance.ApplySetting();

            if (!fadeOverlay.gameObject.activeInHierarchy)
            {
                fadeOverlay.gameObject.SetActive(true);
            }
            else
            {
                fadeOverlay.PerformFade();
            }
        }

        public void SetLogoVisibility(bool isVisible)
        {
            brandlabLogo.transform.localScale = isVisible ? Vector3.one : Vector3.zero;
        }

        public void OpenRPM()
        {
            if (AppManager.Instance.Settings.projectSettings.readyPlayerMeMode.Equals(ReadyPlayerMeMode.Selfie))
            {
#if !READY_PLAYER_ME && !BRANDLAB360_AVATARS_READYPLAYERME
                    Debug.Log("Converting to Fixed Avatar Mode!!! com.brandlab360.avatars.readyplayerme || readyplayerme has not been installed!!");
                    AppManager.Instance.Data.FixedAvatarUsed = true;
                    isFixedAvatarUsed = AppManager.Instance.Data.FixedAvatarUsed;
#else
                if (AppManager.Instance.Settings.projectSettings.readyPlayerMeSelfieMode.Equals(ReadyPlayerMeSelfieMode.Vuplex) || Application.isEditor)
                {
                    if (Application.isEditor)
                    {
                        Debug.Log("if projectSettings.readyPlayerMeSelfieMode = WebClient, it will use Vuplex in editor");
                    }

                    //instantiate the web view 
                    viewRPM.SetActive(true);
                    ReadyPlayerMe.RPMOpenWebview wView = viewRPM.AddComponent<ReadyPlayerMe.RPMOpenWebview>();
                    wView.Open(OnRPMAvatarUrlReceived);
                }
                else
                {
                    //do web comms
                    RPMRequest request = new RPMRequest();
                    WebclientManager.WebClientListener += WebClientRPMResponse;
                    WebclientManager.Instance.Send(JsonUtility.ToJson(request));
                }
#endif
            }
        }

        private void WebClientRPMResponse(string json)
        {
            WebclientManager.WebClientListener -= WebClientRPMResponse;

            var rpmResponse = JsonUtility.FromJson<RPMResponse>(json).OrDefaultWhen(x => x.readyPlayerMeAvatarUrl == null);

            if(rpmResponse != null)
            {
                OnRPMAvatarUrlReceived(rpmResponse.readyPlayerMeAvatarUrl);
            }
            else
            {
                Debug.Log("No Avatar URL was returned via WebClientRPMResponse");
            }

        }

        private void OnRPMAvatarUrlReceived(string url)
        {
            AppManager.Instance.Data.FixedAvatarName = url;

            //fade out
            fadeOverlay.PauseTime = 0.5f;
            fadeOverlay.FadeType = FadeOutIn.FadeAction.Out;
            fadeOverlay.ImplementChange = null;
            fadeOverlay.Callback = OnFadeOutCallback;

            if(RPMActionOnRecieved != null)
            {
                RPMActionOnRecieved.Invoke(url);
            }
            else
            {
                if (!fadeOverlay.gameObject.activeInHierarchy)
                {
                    fadeOverlay.gameObject.SetActive(true);
                }
                else
                {
                    fadeOverlay.PerformFade();
                }
            }
        }

        /// <summary>
        /// Called on avatar menu upon Apply
        /// </summary>
        public void OnClickAvatar(bool justFade = false)
        {
            if (justFade)
            {
                //fade out
                fadeOverlay.PauseTime = 0.5f;
                fadeOverlay.FadeType = FadeOutIn.FadeAction.Out;
                fadeOverlay.ImplementChange = null;
                fadeOverlay.Callback = OnFadeOutCallback;

                if (!fadeOverlay.gameObject.activeInHierarchy)
                {
                    fadeOverlay.gameObject.SetActive(true);
                }
                else
                {
                    fadeOverlay.PerformFade();
                }

                return;
            }

            if (m_customerHandler != null)
            {
                //set app data
                if(!isFixedAvatarUsed)
                {
                    AppManager.Instance.Data.Sex = m_customerHandler.CustomAvatar.Sex;
                    AppManager.Instance.Data.Avatar = m_customerHandler.CustomAvatar.Settings;
                    AppManager.Instance.Data.CustomiseJson = CustomiseAvatar.GetAvatarHashString(m_customerHandler.CustomAvatar.GetProperties());
                }
                else
                {
                    AppManager.Instance.Data.CustomiseJson = m_customerHandler.GetAvatarName;
                }

                AppManager.Instance.Data.FixedAvatarName = m_customerHandler.GetAvatarName;

                if(AppManager.Instance.Settings.projectSettings.loginMode.Equals(LoginMode.Standard))
                {
                    bool save = AppManager.Instance.Settings.projectSettings.avatarMode.Equals(AvatarMode.Custom) ? true : AppManager.Instance.Settings.projectSettings.alwaysRandomiseAvatar ? false : true;

                    if (AppManager.Instance.Settings.projectSettings.useIndexedDB && save)
                    {
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
                }
                else
                {
                    AppManager.Instance.Data.LoginProfileData.avatar_data = AppManager.Instance.Data.CustomiseJson;
                    AppManager.Instance.UpdateLoginsAPI();
                   // string projectID = string.IsNullOrEmpty(AppManager.Instance.Settings.projectSettings.clientName) ? AppManager.Instance.Data.ProjectID : AppManager.Instance.Settings.projectSettings.clientName;
                    //LoginsAPI.Instance.UpdateUser(AppManager.Instance.Data.NickName, projectID, JsonUtility.ToJson(AppManager.Instance.Data.LoginProfileData), AppManager.Instance.Data.LoginProfileData.password, AppManager.Instance.Data.RawFriendsData, AppManager.Instance.Data.RawGameData);
                }

                //fade out
                fadeOverlay.PauseTime = 0.5f;
                fadeOverlay.FadeType = FadeOutIn.FadeAction.Out;
                fadeOverlay.ImplementChange = null;
                fadeOverlay.Callback = OnFadeOutCallback;

                if (!fadeOverlay.gameObject.activeInHierarchy)
                {
                    fadeOverlay.gameObject.SetActive(true);
                }
                else
                {
                    fadeOverlay.PerformFade();
                }
            }
        }

        /// <summary>
        /// Callback when fade in has finished
        /// </summary>
        private void OnFadeInCallback()
        {
            environment.Activate(true);

            m_customerHandler = FindFirstObjectByType<CustomiseAvatar>(FindObjectsInactive.Include);

            //set avatar based on AppManager.Data settings
            if (AppManager.Instance.Data.StartupCompleted)
            {
                if (!isFixedAvatarUsed)
                {
                    m_customerHandler.currentSex = AppManager.Instance.Data.Sex;
                    m_customerHandler.CustomAvatar.Customise(AppManager.Instance.Data.Avatar);
                }
                else
                {
                    //need to set the fixed avatar by name.
                    m_customerHandler.SetFixedAvatar(AppManager.Instance.Data.FixedAvatarName);
                }
            }
        }

        /// <summary>
        /// Callback when fade out is complete
        /// </summary>
        private void OnFadeOutCallback()
        {
            environment.Activate(false);

            //load main scene
            AppManager.Instance.OnAvatarApplied();
        }

        [System.Serializable]
        private class RPMRequest
        {
            public bool showReadyPlayerMe = true;
        }

        [System.Serializable]
        private class RPMResponse
        {
            public string readyPlayerMeAvatarUrl;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AppAvatar), true)]
        public class AppAvatar_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("avatarScreen"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("RPMScreen"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("brandlabLogo"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeOverlay"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("configHolder"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("viewRPM"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("environment"), true);

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
