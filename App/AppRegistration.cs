using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Defective.JSON;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class AppRegistration : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField]
        private GameObject loginPanel;
        [SerializeField]
        private GameObject registerPanel;

        [Header("Login Inputs")]
        [SerializeField]
        private TMP_InputField usernameLogin;
        [SerializeField]
        private TMP_InputField passwordLogin;

        [Header("Login Errors")]
        [SerializeField]
        private TextMeshProUGUI loginError;
        [SerializeField]
        private TextMeshProUGUI loginFailed;

        [Header("Mobile Register")]
        [SerializeField]
        private GameObject registerButton;
        [SerializeField]
        private GameObject registerNote;

        [Header("Register Inputs")]
        [SerializeField]
        private TMP_InputField username;
        [SerializeField]
        private TMP_InputField email;
        [SerializeField]
        private TMP_InputField password;
        [SerializeField]
        private TMP_InputField confirmPassword;
        [SerializeField]
        private Toggle acceptTOC;

        [Header("Register Errors")]
        [SerializeField]
        private TextMeshProUGUI usernameError;
        [SerializeField]
        private TextMeshProUGUI usernameExistsError;
        [SerializeField]
        private TextMeshProUGUI emailError;
        [SerializeField]
        private TextMeshProUGUI passwordError;
        [SerializeField]
        private TextMeshProUGUI confirmPasswordError;
        [SerializeField]
        private TextMeshProUGUI acceptTOCError;

        [Header("Admin")]
        public GameObject adminButton;

        [Header("TAC")]
        [SerializeField]
        private TextMeshProUGUI textTAC;

        private BaseCustomAPI m_custommAPIClass;

        private void Awake()
        {
            loginPanel.SetActive(true);
            registerPanel.SetActive(false);

            usernameLogin.Select();
        }

        private void Start()
        {
            if (adminButton != null)
            {
                if (AppManager.Instance.Data.IsMobile)
                {
                    adminButton.SetActive(false);
                }
                else
                {
                    adminButton.SetActive(AppManager.Instance.Settings.projectSettings.useAdminUser);
                }
            }

            if (AppManager.Instance.Data.IsMobile)
            {
                registerButton.SetActive(false);
                registerNote.SetActive(true);
            }

            if (AppManager.Instance.Settings.projectSettings.loginAPIMode.Equals(APILoginMode._Custom))
            {
                m_custommAPIClass = FindFirstObjectByType<BaseCustomAPI>(FindObjectsInactive.Include);

                if (m_custommAPIClass == null)
                {
                    if(AppManager.Instance.Settings.projectSettings.customAPIPrefab != null)
                    {
                        GameObject go = Instantiate(AppManager.Instance.Settings.projectSettings.customAPIPrefab);
                        go.name = AppManager.Instance.Settings.projectSettings.customAPIPrefab.ToString();
                        m_custommAPIClass = go.GetComponent<BaseCustomAPI>();
                    }
                }
            }

            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
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
                    transform.localScale = new Vector3(1, 1, 1);
                }
                else
                {
                    float aspect = arg2 / arg1;
                    float scaler = aspect / 4;
                    transform.localScale = new Vector3(1 + scaler, 1 + scaler, 1);
                }
            }
        }

        protected virtual void Update()
        {
            if (InputManager.Instance.GetKeyUp("Enter"))
            {
                if (loginPanel.activeInHierarchy)
                {
                    if (usernameLogin.isFocused)
                    {
                        passwordLogin.Select();
                    }
                    else if(passwordLogin.isFocused)
                    {
                        Login();
                    }
                }
                else
                {
                    if (username.isFocused)
                    {
                        email.Select();
                    }
                    else if (email.isFocused)
                    {
                        password.Select();
                    }
                    else if (password.isFocused)
                    {
                        confirmPassword.Select();
                    }
                    else if (confirmPassword.isFocused)
                    {
                        Register();
                    }
                }
            }
        }

        public void ToggleAdmin()
        {
            AppLogin.Instance.OpenAdmin();
        }

        public virtual void Register()
        {
            usernameExistsError.gameObject.SetActive(false);
            loginFailed.gameObject.SetActive(false);

            //will need to validate entries
            if (string.IsNullOrEmpty(username.text))
            {
                usernameError.gameObject.SetActive(true);
                return;
            }
            else
            {
                usernameError.gameObject.SetActive(false);
            }

            //will need to validate entries
            if (string.IsNullOrEmpty(email.text))
            {
                emailError.gameObject.SetActive(true);
                return;
            }
            else
            {
                if(!IsValidEmail(email.text))
                {
                    emailError.gameObject.SetActive(true);
                    return;
                }
                else
                {
                    emailError.gameObject.SetActive(false);
                }
            }

            if (string.IsNullOrEmpty(password.text))
            {
                passwordError.gameObject.SetActive(true);
                return;
            }
            else
            {
                passwordError.gameObject.SetActive(false);
            }

            if (string.IsNullOrEmpty(confirmPassword.text) || !password.text.Equals(confirmPassword.text))
            {
                confirmPasswordError.gameObject.SetActive(true);
                return;
            }
            else
            {
                confirmPasswordError.gameObject.SetActive(false);
            }

            if(!acceptTOC.isOn)
            {
                acceptTOCError.gameObject.SetActive(true);
                return;
            }
            else
            {
                acceptTOCError.gameObject.SetActive(false);
            }

            //need to check if user exists first
            string projectID = string.IsNullOrEmpty(AppManager.Instance.Settings.projectSettings.clientName) ? AppManager.Instance.Data.ProjectID :
                usernameLogin.text.Equals("brandlab") ? AppManager.Instance.Data.ProjectID : AppManager.Instance.Settings.projectSettings.clientName;

            ProfileData profile = new ProfileData();
            profile.email = email.text;
            profile.username = username.text;
            string data = JsonUtility.ToJson(profile);

            //all good then continue
            switch (AppManager.Instance.Settings.projectSettings.loginAPIMode)
            {
                case APILoginMode._Salesforce:
                    APIRegister(APILoginMode._Salesforce);
                    break;
                case APILoginMode._Hubspot:
                    APIRegister(APILoginMode._Hubspot);
                    break;
                case APILoginMode._BrandLab:
                    LoginsAPI.Instance.RegisterUser(username.text, password.text, confirmPassword.text, projectID, data, "User", RegisterCallback);
                    break;
                default:
                    APIRegister(APILoginMode._Custom);
                    break;
            }
        }

        public virtual void Login()
        {
            loginError.gameObject.SetActive(false);

            if (string.IsNullOrEmpty(usernameLogin.text) || string.IsNullOrEmpty(passwordLogin.text))
            {
                loginError.gameObject.SetActive(true);
                return;
            }

            string projectID = string.IsNullOrEmpty(AppManager.Instance.Settings.projectSettings.clientName) ? AppManager.Instance.Data.ProjectID : 
                usernameLogin.text.Equals("brandlab") ? AppManager.Instance.Data.ProjectID : AppManager.Instance.Settings.projectSettings.clientName;

            switch(AppManager.Instance.Settings.projectSettings.loginAPIMode)
            {
                case APILoginMode._Salesforce:
                    APILogin(APILoginMode._Salesforce);
                    break;
                case APILoginMode._Hubspot:
                    APILogin(APILoginMode._Hubspot);
                    break;
                case APILoginMode._BrandLab:
                    LoginsAPI.Instance.LoginUser(usernameLogin.text, passwordLogin.text, projectID, LoginCallback);
                    break;
                default:
                    APILogin(APILoginMode._Custom);
                    break;
            }
        }

        private async void APILogin(APILoginMode mode)
        {
            if (AppManager.Instance.API == null)
            { 
#if UNITY_EDITOR
                Debug.LogError("ProjectAPISettings resource has not been created!! Please open the Control Panel and create settings resource");
#endif
                return;
            }

            ProfileData profile = new ProfileData();
            string username = "";
            string password = "";
            string friends = "";
            string games = "";
            bool isAdmin = false;

            if (mode.Equals(APILoginMode._Salesforce))
            {
                bool authenticated = AppManager.Instance.API.salesforceSettings.AuthPostData == null ? await AppManager.Instance.API.salesforceSettings.Authenticate() : true;

                if (authenticated)
                {
                    string result = await AppManager.Instance.API.salesforceSettings.Login(usernameLogin.text, passwordLogin.text);

                    if (!string.IsNullOrEmpty(result))
                    {
                        JSONObject json = new JSONObject(result);

                        for (int i = 0; i < AppManager.Instance.API.salesforceSettings.loginDataToCollect.Length; i++)
                        {
                            if (AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].ignore) continue;

                            if (AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._ID))
                            {
                                if (json.HasField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField))
                                {
                                    profile.id = json.GetField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField).intValue;
                                }
                            }
                            else if (AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._username))
                            {
                                if (json.HasField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField))
                                {
                                    username = (string.IsNullOrEmpty(username) ? "" : " ") + json.GetField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField).stringValue;
                                }
                            }
                            else if (AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._password))
                            {
                                if (json.HasField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField))
                                {
                                    password = (string.IsNullOrEmpty(password) ? "" : " ") + json.GetField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField).stringValue;
                                }
                            }
                            else if (AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._gamesData))
                            {
                                if (json.HasField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField))
                                {
                                    games = (string.IsNullOrEmpty(games) ? "" : " ") + json.GetField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField).stringValue;
                                }
                            }
                            else if (AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._friendsData))
                            {
                                if (json.HasField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField))
                                {
                                    friends = (string.IsNullOrEmpty(friends) ? "" : " ") + json.GetField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField).stringValue;
                                }
                            }
                            else if (AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._name))
                            {
                                if (json.HasField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField))
                                {
                                    profile.name += (string.IsNullOrEmpty(profile.name) ? "" : " ") + json.GetField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField).stringValue;
                                }
                            }
                            else if (AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._picture))
                            {
                                if (json.HasField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField))
                                {
                                    profile.picture_url += (string.IsNullOrEmpty(profile.picture_url) ? "" : " ") + json.GetField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField).stringValue;
                                }
                            }
                            else if (AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._about))
                            {
                                if (json.HasField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField))
                                {
                                    profile.about += (string.IsNullOrEmpty(profile.about) ? "" : " ") + json.GetField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField).stringValue;
                                }
                            }
                            else if (AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._termsAndConditions))
                            {
                                if (json.HasField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField))
                                {
                                    profile.acceptTAC = json.GetField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField).boolValue;
                                }
                            }
                            else if (AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._admin))
                            {
                                if (json.HasField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField))
                                {
                                    isAdmin = json.GetField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField).boolValue;
                                }
                            }
                            else if (AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._email))
                            {
                                if (json.HasField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField))
                                {
                                    profile.email += (string.IsNullOrEmpty(profile.email) ? "" : " ") + json.GetField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField).stringValue;
                                }
                            }
                            else if (AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._avatar))
                            {
                                if (json.HasField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField))
                                {
                                    profile.avatar_data += (string.IsNullOrEmpty(profile.avatar_data) ? "" : " ") + json.GetField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField).stringValue;
                                }
                            }
                            else if (AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._playerSettings))
                            {
                                if (json.HasField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField))
                                {
                                    profile.player_settings += (string.IsNullOrEmpty(profile.avatar_data) ? "" : " ") + json.GetField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField).stringValue;
                                }
                            }
                            else if (AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].profileReference.Equals(ProfileDataReference._custom))
                            {
                                if (json.HasField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField))
                                {
                                    profile.AddAdditionalData(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField, json.GetField(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField).stringValue);
                                }
                            }
                        }

                        ApplyCredentials(username, password, JsonUtility.ToJson(profile), friends, games, isAdmin);
                    }
                    else
                    {
                        loginFailed.gameObject.SetActive(true);
                    }
                }
                else
                {
                    loginFailed.gameObject.SetActive(true);
                }
            }
            else if (mode.Equals(APILoginMode._Hubspot))
            {
                string result = await AppManager.Instance.API.hubspotSettings.GetContact(this.username.text, this.password.text);

                if (!string.IsNullOrEmpty(result))
                {
                    JSONObject json = new JSONObject(result);

                    for (int i = 0; i < AppManager.Instance.API.hubspotSettings.properties.Length; i++)
                    {
                        if (AppManager.Instance.API.hubspotSettings.properties[i].ignore) continue;

                        if (AppManager.Instance.API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._ID))
                        {
                            if (json.HasField(AppManager.Instance.API.hubspotSettings.properties[i].tableField))
                            {
                                profile.id = json.GetField(AppManager.Instance.API.hubspotSettings.properties[i].tableField).intValue;
                            }
                        }
                        else if (AppManager.Instance.API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._username))
                        {
                            if (json.HasField(AppManager.Instance.API.hubspotSettings.properties[i].tableField))
                            {
                                username = (string.IsNullOrEmpty(username) ? "" : " ") + json.GetField(AppManager.Instance.API.hubspotSettings.properties[i].tableField).stringValue;
                            }
                        }
                        else if (AppManager.Instance.API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._password))
                        {
                            if (json.HasField(AppManager.Instance.API.hubspotSettings.properties[i].tableField))
                            {
                                password = (string.IsNullOrEmpty(password) ? "" : " ") + json.GetField(AppManager.Instance.API.hubspotSettings.properties[i].tableField).stringValue;
                            }
                        }
                        else if (AppManager.Instance.API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._gamesData))
                        {
                            if (json.HasField(AppManager.Instance.API.hubspotSettings.properties[i].tableField))
                            {
                                games = (string.IsNullOrEmpty(games) ? "" : " ") + json.GetField(AppManager.Instance.API.hubspotSettings.properties[i].tableField).stringValue;
                            }
                        }
                        else if (AppManager.Instance.API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._friendsData))
                        {
                            if (json.HasField(AppManager.Instance.API.hubspotSettings.properties[i].tableField))
                            {
                                friends = (string.IsNullOrEmpty(friends) ? "" : " ") + json.GetField(AppManager.Instance.API.hubspotSettings.properties[i].tableField).stringValue;
                            }
                        }
                        else if (AppManager.Instance.API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._name))
                        {
                            if (json.HasField(AppManager.Instance.API.hubspotSettings.properties[i].tableField))
                            {
                                profile.name += (string.IsNullOrEmpty(profile.name) ? "" : " ") + json.GetField(AppManager.Instance.API.hubspotSettings.properties[i].tableField).stringValue;
                            }
                        }
                        else if (AppManager.Instance.API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._picture))
                        {
                            if (json.HasField(AppManager.Instance.API.hubspotSettings.properties[i].tableField))
                            {
                                profile.picture_url += (string.IsNullOrEmpty(profile.picture_url) ? "" : " ") + json.GetField(AppManager.Instance.API.hubspotSettings.properties[i].tableField).stringValue;
                            }
                        }
                        else if (AppManager.Instance.API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._about))
                        {
                            if (json.HasField(AppManager.Instance.API.hubspotSettings.properties[i].tableField))
                            {
                                profile.about += (string.IsNullOrEmpty(profile.about) ? "" : " ") + json.GetField(AppManager.Instance.API.hubspotSettings.properties[i].tableField).stringValue;
                            }
                        }
                        else if (AppManager.Instance.API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._termsAndConditions))
                        {
                            if (json.HasField(AppManager.Instance.API.hubspotSettings.properties[i].tableField))
                            {
                                profile.acceptTAC = json.GetField(AppManager.Instance.API.hubspotSettings.properties[i].tableField).boolValue;
                            }
                        }
                        else if (AppManager.Instance.API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._admin))
                        {
                            if (json.HasField(AppManager.Instance.API.hubspotSettings.properties[i].tableField))
                            {
                                isAdmin = json.GetField(AppManager.Instance.API.hubspotSettings.properties[i].tableField).boolValue;
                            }
                        }
                        else if (AppManager.Instance.API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._email))
                        {
                            if (json.HasField(AppManager.Instance.API.hubspotSettings.properties[i].tableField))
                            {
                                profile.email += (string.IsNullOrEmpty(profile.email) ? "" : " ") + json.GetField(AppManager.Instance.API.hubspotSettings.properties[i].tableField).stringValue;
                            }
                        }
                        else if (AppManager.Instance.API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._avatar))
                        {
                            if (json.HasField(AppManager.Instance.API.hubspotSettings.properties[i].tableField))
                            {
                                profile.avatar_data += (string.IsNullOrEmpty(profile.avatar_data) ? "" : " ") + json.GetField(AppManager.Instance.API.hubspotSettings.properties[i].tableField).stringValue;
                            }
                        }
                        else if (AppManager.Instance.API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._playerSettings))
                        {
                            if (json.HasField(AppManager.Instance.API.hubspotSettings.properties[i].tableField))
                            {
                                profile.player_settings += (string.IsNullOrEmpty(profile.avatar_data) ? "" : " ") + json.GetField(AppManager.Instance.API.hubspotSettings.properties[i].tableField).stringValue;
                            }
                        }
                        else if (AppManager.Instance.API.hubspotSettings.properties[i].profileReference.Equals(ProfileDataReference._custom))
                        {
                            if (json.HasField(AppManager.Instance.API.hubspotSettings.properties[i].tableField))
                            {
                                profile.AddAdditionalData(AppManager.Instance.API.salesforceSettings.loginDataToCollect[i].tableField, json.GetField(AppManager.Instance.API.hubspotSettings.properties[i].tableField).stringValue);
                            }
                        }
                    }

                    ApplyCredentials(username, password, JsonUtility.ToJson(profile), friends, games, isAdmin);
                }
                else
                {
                    loginFailed.gameObject.SetActive(true);
                }
            }
            else
            {
                if(m_custommAPIClass != null)
                {
                    bool success = await m_custommAPIClass.Authenticate();

                    if(success)
                    {
                        bool result = await m_custommAPIClass.Login(this.username.text, this.password.text);

                        if(!result)
                        {
                            Debug.LogError("CustomAPIPrefab login Failed!");
                        }
                        else
                        {
                            Debug.LogError("CustomAPIPrefab login Success!");
                        }
                    }
                    else
                    {
                        Debug.LogError("CustomAPIPrefab Authenticate Failed!");
                    }
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogError("CustomAPIPrefab has not been assigned in the Project Settings!");
#endif
                }
            }
        }

        private async void APIRegister(APILoginMode mode)
        {
            if (AppManager.Instance.API == null)
            {
#if UNITY_EDITOR
                Debug.LogError("ProjectAPISettings resource has not been created!! Please open the Control Panel and create settings resource");
#endif
                return;
            }

            if (mode.Equals(APILoginMode._Salesforce))
            {
                bool authenticated = AppManager.Instance.API.salesforceSettings.AuthPostData == null ? await AppManager.Instance.API.salesforceSettings.Authenticate() : true;

                if (authenticated)
                {
                    Dictionary<string, string> data = new Dictionary<string, string>();
                    data.Add(AppManager.Instance.API.salesforceSettings.username.tableField, this.username.text);
                    data.Add(AppManager.Instance.API.salesforceSettings.password.tableField, this.password.text);
                    data.Add(AppManager.Instance.API.salesforceSettings.email.tableField, email.text);
                    data.Add(AppManager.Instance.API.salesforceSettings.termsAndCondition.tableField, acceptTOC.isOn ? "true" : "false");

                    string result = await AppManager.Instance.API.salesforceSettings.Register(data);

                    if (!string.IsNullOrEmpty(result))
                    {

                        //clear all register 
                        ClearRegisterInputs();
                        ClearLoginInputs();

                        registerPanel.SetActive(false);
                        loginPanel.SetActive(true);
                    }
                    else
                    {
                        usernameExistsError.gameObject.SetActive(true);
                    }
                }
                else
                {
                    usernameExistsError.gameObject.SetActive(true);
                }
            }
            else if(mode.Equals(APILoginMode._Hubspot))
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add(AppManager.Instance.API.hubspotSettings.username.tableField, this.username.text);
                data.Add(AppManager.Instance.API.hubspotSettings.password.tableField, this.password.text);
                data.Add(AppManager.Instance.API.hubspotSettings.email.tableField, email.text);
                data.Add(AppManager.Instance.API.hubspotSettings.termsAndCondition.tableField, acceptTOC.isOn ? "true" : "false");

                string result = await AppManager.Instance.API.hubspotSettings.CreateContact(this.username.text, this.password.text, data);

                if (!string.IsNullOrEmpty(result))
                {
                    //clear all register 
                    ClearRegisterInputs();
                    ClearLoginInputs();

                    registerPanel.SetActive(false);
                    loginPanel.SetActive(true);
                }
                else
                {
                    usernameExistsError.gameObject.SetActive(true);
                }
            }
            else
            {
                if (m_custommAPIClass != null)
                {
                    bool success = await m_custommAPIClass.Authenticate();

                    if (success)
                    {
                        bool result = await m_custommAPIClass.Register(username.text, password.text, email.text, acceptTOC.isOn, false);

                        if (!result)
                        {
                            Debug.LogError("CustomAPIPrefab Register Failed!");
                        }
                        else
                        {
                            Debug.LogError("CustomAPIPrefab Register Success!");
                        }
                    }
                    else
                    {
                        Debug.LogError("CustomAPIPrefab Authenticate Failed!");
                    }
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogError("CustomAPIPrefab has not been assigned in the Project Settings!");
#endif
                }
            }
        }

        private void LoginCallback(LoginResponse response)
        {
            if(response != null)
            {
                Debug.Log("User Logged In=" + JsonUtility.ToJson(response));

                ApplyCredentials(response.username, passwordLogin.text, response.data, response.friends, response.games);
            }
            else
            {
                loginFailed.gameObject.SetActive(true);
            }
        }

        private void RegisterCallback(RegistrationResponse response)
        {
            if (response != null)
            {
                Debug.Log("User Registerd=" + JsonUtility.ToJson(response));

                //clear all register 
                ClearRegisterInputs();
                ClearLoginInputs();

                registerPanel.SetActive(false);
                loginPanel.SetActive(true);
            }
            else
            {
                usernameExistsError.gameObject.SetActive(true);
            }
        }

        protected virtual void ApplyCredentials(string username, string password, string data, string freinds, string games, bool isAdmin = false)
        {
            AppManager.Instance.Data.LoginProfileData = JsonUtility.FromJson<ProfileData>(data);
            AppManager.Instance.Data.LoginProfileData.username = username;
            AppManager.Instance.Data.LoginProfileData.password = password;
            AppManager.Instance.Data.NickName = username;
            AppManager.Instance.Data.CustomiseJson = AppManager.Instance.Data.LoginProfileData.avatar_data;
            AppManager.Instance.Data.FixedAvatarName = AppManager.Instance.Data.FixedAvatarUsed ? AppManager.Instance.Data.CustomiseJson : "";
            AppManager.Instance.Data.IsAdminUser = isAdmin;
            AppManager.Instance.Data.RawFriendsData = freinds;
            AppManager.Instance.Data.RawGameData = games;


            AppLogin login = FindFirstObjectByType<AppLogin>(FindObjectsInactive.Include);
            login.ShowLoadingOverlay(true);

            AppManager.Instance.LoginComplete();
        }

        public void ShowRegister(bool show)
        {
            ClearRegisterInputs();
            registerPanel.SetActive(true);
            loginPanel.SetActive(false);

            textTAC.text = AppManager.Instance.Settings.projectSettings.clientTAC;
        }

        public void ShowLogin(bool show)
        {
            ClearLoginInputs();
            registerPanel.SetActive(false);
            loginPanel.SetActive(true);
        }

        protected virtual void ClearRegisterInputs()
        {
            username.text = "";
            usernameError.gameObject.SetActive(false);
            email.text = "";
            emailError.gameObject.SetActive(false);
            password.text = "";
            passwordError.gameObject.SetActive(false);
            confirmPassword.text = "";
            confirmPasswordError.gameObject.SetActive(false);

            acceptTOC.isOn = false;
            acceptTOCError.gameObject.SetActive(false);

            usernameExistsError.gameObject.SetActive(false);
        }

        protected virtual void ClearLoginInputs()
        {
            usernameLogin.text = "";
            passwordLogin.text = "";
            loginError.gameObject.SetActive(false);
        }

        protected bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AppRegistration), true)]
        public class AppRegistration_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("loginPanel"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("registerPanel"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("usernameLogin"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("passwordLogin"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("loginError"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("loginFailed"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("registerButton"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("registerNote"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("username"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("email"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("password"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("confirmPassword"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("acceptTOC"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("usernameError"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("usernameExistsError"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("emailError"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("passwordError"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("confirmPasswordError"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("acceptTOCError"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("adminButton"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("textTAC"), true);

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
