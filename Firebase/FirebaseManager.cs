using System;
using System.Collections.Generic;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// A simple firebase bridge through the webclient 
    /// - Currently this is only used to facilitate phone authentication but can
    /// be extended to allow other features / client held data
    /// </summary>
    public class FirebaseManager : Singleton<FirebaseManager>
    {
        public System.Action<string, string> FirebaseListener;

        public static FirebaseManager Instance
        {
            get
            {
                return ((FirebaseManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        /// <summary>
        /// Global access to the Firebase Authice
        /// </summary>
        public string Auth
        {
            get;
            private set;
        }

        /// <summary>
        /// Global access to the firebase ID
        /// </summary>
        public string ID
        {
            get;
            private set;
        }

        /// <summary>
        /// Sends the firebase config settings to the webclient
        /// </summary>
        public void InitializeFirebase()
        {
            if (!AppManager.IsCreated) return;

            if (!AppManager.Instance.Settings.projectSettings.useFirebase) return;

            FirebaseConfig firebaseConfig = AppManager.Instance.Settings.projectSettings.firebaseConfig;

            var json = JsonUtility.ToJson(firebaseConfig);

            WebclientManager.WebClientListener += subscribeWebClientEvent;
            WebclientManager.Instance.Send(json);
        }

        /// <summary>
        /// Callback for receiving firebase response from the webclient 
        /// </summary>
        /// <param name="json"></param>
        private void subscribeWebClientEvent(string json)
        {
            WebclientManager.WebClientListener -= subscribeWebClientEvent;

            var response = JsonUtility.FromJson<FirebaseResponse>(json).OrDefaultWhen(x => x.authorization == null && x.userId == null);

            if (response != null)
            {
                Debug.Log("Received Firebase data. Token: " + response.authorization + " userId: " + response.userId);

                Auth = response.authorization;
                ID = response.userId;

                if (FirebaseListener != null)
                {
                    FirebaseListener.Invoke(response.authorization, response.userId);
                }
            }
            else
            {
                if (FirebaseListener != null)
                {
                    FirebaseListener.Invoke("", "");
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(FirebaseManager), true)]
        public class FirebaseManager_Editor : BaseInspectorEditor
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

    [Serializable]
    public class FirebaseConfig
    {
        public string apiKey;
        public string authDomain;
        public string databaseURL;
        public string projectId;
        public string storageBucket;
        public string messagingSenderId;
        public string appId;
        public string measurementId;

        public FirebaseConfig(string apiKey, string authDomain, string databaseURL, string projectId, 
            string storageBucket, string messagingSenderId, string appId, string measurementId)
        {
            this.apiKey = apiKey;
            this.authDomain = authDomain;
            this.databaseURL = databaseURL;
            this.projectId = projectId;
            this.storageBucket = storageBucket;
            this.messagingSenderId = messagingSenderId;
            this.appId = appId;
            this.measurementId = measurementId;
        }
    }

    [Serializable]
    public class FirebaseResponse
    {
        public string authorization;
        public string userId;
    }
}