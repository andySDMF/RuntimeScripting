using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{

    /// <summary>
    /// LoginsAPI is used to store / register user data to enable login functionality in a project. 
    /// e.g. if a project needed to allow user signup / login and to store any data for that user
    /// e.g. their avatar configuration
    /// </summary>
    public class LoginsAPI : Singleton<LoginsAPI>
    {
        public string LoginEndpoint = "/auth/unity/login";
        public string RegisterEndpoint = "/unity/users/register";
        public string CheckEndpoint = "/unity/users/";
        public string UpdateEndpoint = "/unity/users/update";

        private LoginResponse loginResponse;

        public static LoginsAPI Instance
        {
            get
            {
                return ((LoginsAPI)instance);
            }
            set
            {
                instance = value;
            }
        }

        /// <summary>
        /// Login a user with the given credentials
        /// </summary>
        public void LoginUser(string username, string password, string project, System.Action<LoginResponse> OnResponceRecieved)
        {
            StartCoroutine(login(username, password, project, OnResponceRecieved));
        }

        /// <summary>
        /// Update user's details
        /// </summary>
        public void UpdateUser(string username, string project, string data, string password, string friends = "", string games = "")
        {
            if (loginResponse != null)
            {
                StartCoroutine(update(username, project, data, password, friends, games));

            }
            else
            {
                Debug.LogError("Tried to update via LoginAPI without logging in first");
            }
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        public void RegisterUser(string username, string password, string passwordConfirmation, string project, string data, string role, System.Action<RegistrationResponse> OnResponceRecieved)
        {
            StartCoroutine(register(username, password, passwordConfirmation, project, data, role, OnResponceRecieved));
        }

        #region HTTP requests 

        private IEnumerator login(string username, string password, string project, System.Action<LoginResponse> OnResponceRecieved)
        {
            var loginCredentials = new LoginCredentials(username, password, project);
            var jsonEntry = JsonUtility.ToJson(loginCredentials);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);

            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + LoginEndpoint;

            Debug.Log("Request POST login method: uri= " + uri + "::" + jsonEntry);

            using (UnityWebRequest request = UnityWebRequest.PostWwwForm(uri, UnityWebRequest.kHttpVerbPOST))
            {
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 5;
                request.uploadHandler = new UploadHandlerRaw(jsonBytes);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    var errResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);

                    if (errResponse.errors != null)
                    {
                        for (int i = 0; i < errResponse.errors.Length; i++)
                        {
                            Debug.Log("Error POST contents: " + errResponse.errors[i]);
                        }
                    }

                    if (OnResponceRecieved != null)
                    {
                        OnResponceRecieved.Invoke(null);
                    }
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

                        loginResponse = response;

                        if (OnResponceRecieved != null)
                        {
                            OnResponceRecieved.Invoke(loginResponse);
                        }
                    }
                }

                request.Dispose();
            }
        }

        private IEnumerator update(string username, string project, string data, string password, string friends = "", string games = "")
        {
            var userData = new UserData(username, project, data, password, password, friends, games);
            var jsonEntry = JsonUtility.ToJson(userData);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);

            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + UpdateEndpoint;

            Debug.Log("Request POST login method: uri= " + uri + "::" + jsonEntry);

            using (UnityWebRequest request = UnityWebRequest.PostWwwForm(uri, UnityWebRequest.kHttpVerbPOST))
            {
                //update request to the api need to set the access token in header
                var bearer = "Bearer " + loginResponse.token;
                request.SetRequestHeader("Authorization", bearer);

                request.SetRequestHeader("Content-Type", "application/json");
                request.uploadHandler = new UploadHandlerRaw(jsonBytes);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    var errResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);

                    for (int i = 0; i < errResponse.errors.Length; i++)
                    {
                        Debug.Log("Error POST contents: " + errResponse.errors[i]);
                    }
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        var response = JsonUtility.FromJson<SuccessResponse>(request.downloadHandler.text);
                    }
                }

                request.Dispose();
            }
        }

        private IEnumerator register(string username, string password, string passwordConfirmation, string project, string data, string role, System.Action<RegistrationResponse> OnResponceRecieved)
        {
            var registrationData = new RegistrationData(username, password, passwordConfirmation, project, role, data, "", "");
            var jsonEntry = JsonUtility.ToJson(registrationData);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);

            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + RegisterEndpoint;

            Debug.Log("Request POST register method: uri= " + uri + "::" + jsonEntry);

            using (UnityWebRequest request = UnityWebRequest.PostWwwForm(uri, UnityWebRequest.kHttpVerbPOST))
            {
                request.SetRequestHeader("Content-Type", "application/json");
                request.uploadHandler = new UploadHandlerRaw(jsonBytes);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    var errResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);

                    for (int i = 0; i < errResponse.errors.Length; i++)
                    {
                        Debug.Log("Error POST contents: " + errResponse.errors[i]);
                    }

                    if (OnResponceRecieved != null)
                    {
                        OnResponceRecieved.Invoke(null);
                    }
                }
                else
                {
                    if (request.responseCode == 201)
                    {
                        var response = JsonUtility.FromJson<RegistrationResponse>(request.downloadHandler.text);

                        if (OnResponceRecieved != null)
                        {
                            OnResponceRecieved.Invoke(response);
                        }
                    }
                }

                request.Dispose();
            }
        }
        #endregion

#if UNITY_EDITOR
        [CustomEditor(typeof(LoginsAPI), true)]
        public class LoginsAPI_Editor : BaseInspectorEditor
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

                    EditorGUILayout.LabelField("Endpoints", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("LoginEndpoint"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("RegisterEndpoint"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("CheckEndpoint"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("UpdateEndpoint"), true);

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

#region Data Structures

[Serializable]
public struct LoginCredentials
{
    public string username;
    public string password;
    public string project;

    public LoginCredentials(string username, string password, string project)
    {
        this.username = username;
        this.password = password;
        this.project = project;
    }
}

public class LoginResponse
{
    public string token;
    public string exp;
    public string username;
    public string role;
    public string data;
    public string friends;
    public string games;
}

[Serializable]
public struct UserData
{
    public string username;
    public string project;
    public string data;
    public string friends;
    public string games;
    public string password;
    public string password_confirmation;

    public UserData(string username, string project, string data, string password, string password_confirmation, string friends, string games)
    {
        this.username = username;
        this.project = project;
        this.data = data;
        this.password = password;
        this.password_confirmation = password_confirmation;
        this.friends = friends;
        this.games = games;
    }
}

[Serializable]
public struct RegistrationData
{
    public string username;
    public string password;
    public string password_confirmation;
    public string project;
    public string role;
    public string data;
    public string friends;
    public string games;

    public RegistrationData(string username, string password, string password_confirmation, string project, string role, string data, string friends, string games)
    {
        this.username = username;
        this.password = password;
        this.password_confirmation = password_confirmation;
        this.project = project;
        this.role = role;
        this.data = data;
        this.friends = friends;
        this.games = games;
    }
}

public class RegistrationResponse
{
    public int id;
    public string username;
    public string role;
    public string project;
    public string data;
    public string friends;
    public string games;
    public string created_at;
    public string updated_at;
}

#endregion