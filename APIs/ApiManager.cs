using System.Collections;
using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ApiManager : Singleton<ApiManager>
    {
        public string AccessEndpoint = "/auth/login";
        public string ApiEmail = "api@brandlab-360.com";
        public string ApiPassword = "$5?dy7^`!a#B6Ha3";

        [HideInInspector]
        public string AccessToken;

        public bool AccessTokenSuccess { get; private set; }

        public static ApiManager Instance
        {
            get
            {
                return ((ApiManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        public System.Action<bool> OnAccessSuccess { get; set; }

        public string APIPATH
        {
            get
            {
                return "/api/v1";
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        public void Begin()
        {
            GetAccessToken();
        }

        /// <summary>
        /// Return the host address for production/development
        /// </summary>
        /// <returns></returns>
        public string GetHostURI()
        {
            if(AppManager.IsCreated)
            {
                if (string.IsNullOrEmpty(AppManager.Instance.Data.releaseMode))
                {
                    if (AppManager.Instance.Settings.projectSettings.releaseMode == ReleaseMode.Production)
                    {
                        return AppManager.Instance.Settings.projectSettings.ProductionHost;
                    }
                    else
                    {
                        return AppManager.Instance.Settings.projectSettings.DevelopmentHost;
                    }
                }
                else
                {
                    if (!AppManager.Instance.Data.releaseMode.Equals("development"))
                    {
                        return AppManager.Instance.Settings.projectSettings.ProductionHost;
                    }
                    else
                    {
                        return AppManager.Instance.Settings.projectSettings.DevelopmentHost;
                    }
                }
            }
            else
            {
                return "https://api-staging.brandlab360.co.uk";
            }
        }

        /// <summary>
        /// Gets the JWT access token for API requests
        /// </summary>
        public void GetAccessToken()
        {
            if (ApiEmail.Length == 0 || ApiPassword.Length == 0)
            {
                Debug.LogError("Error API email or password not set");

                return;
            }

            StartCoroutine(getAccessToken());
        }

        /// <summary>
        /// Request access token from the assortment API using the api user login 
        /// </summary>
        private IEnumerator getAccessToken(bool retry = false)
        {
            User user = new User();
            user.email = ApiEmail;
            user.password = ApiPassword;

            string jsonEntry = JsonUtility.ToJson(user);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);
            string uri = GetHostURI() + APIPATH +  AccessEndpoint;

            Debug.Log("API Request URL: " + uri);

            using (UnityWebRequest request = UnityWebRequest.PostWwwForm(uri, ""))
            {
                request.SetRequestHeader("Content-Type", "application/json");
                request.uploadHandler = new UploadHandlerRaw(jsonBytes);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error ACCESS TOKEN: " + request.error);
                    AccessTokenSuccess = false;

                    if (OnAccessSuccess != null)
                    {
                        OnAccessSuccess.Invoke(false);
                    }
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        if(!request.downloadHandler.text.Contains("<!doctype html>"))
                        {
                            Debug.Log("ACCESS TOKEN: successful");

                            var response = JsonUtility.FromJson<AccessResponse>(request.downloadHandler.text);
                            AccessToken = response.token;
                            AccessTokenSuccess = true;
                        }
                        else
                        {
                            Debug.Log("ACCESS TOKEN: error in download request responce");

                            AccessToken = "";
                            AccessTokenSuccess = false;
                        }

                        if(OnAccessSuccess != null)
                        {
                            OnAccessSuccess.Invoke(true);
                        }
                    }
                }

                request.Dispose();
            }

            if(!AccessTokenSuccess)
            {
                StartCoroutine(ProcessGetAccessToken());
            }
            else
            {
                if(retry)
                {
                    if(AppManager.IsCreated)
                    {
                        //if room established then ensure API calls are handled
                        if (AppManager.Instance.Data.RoomEstablished)
                        {
                            CoreManager.Instance.APIHandler();
                        }
                    }
                }
            }
        }

        private IEnumerator ProcessGetAccessToken()
        {
            yield return new WaitForSeconds(30);
            Debug.Log("Trying to get access token again");
            StartCoroutine(getAccessToken(true));
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ApiManager), true)]
        public class ApiManager_Editor : BaseInspectorEditor
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

                    EditorGUILayout.LabelField("Endpoint", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("AccessEndpoint"), true);

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Credentials", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ApiEmail"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ApiPassword"), true);

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
public class User
{
    public string email;
    public string password;
}

[Serializable]
public class AccessResponse
{
    public string token;
    public string exp;
    public string username;
    public string role;
}

public struct SuccessResponse
{
    public string success;
}

public class ErrorResponse
{
    public string[] errors;
}

#endregion