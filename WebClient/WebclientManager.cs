using System;
using System.Text;
using UnityEngine;

#if BRANDLAB360_STREAMING && !UNITY_EDITOR
using BrandLab360.Streaming;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class WebclientManager : Singleton<WebclientManager>
    {
        [HideInInspector]
        public WebClientMode BackendMode;

        public static WebclientManager Instance
        {
            get
            {
                return ((WebclientManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        /// <summary>
        /// Action for any script to access and subscribe to
        /// </summary>
        public static System.Action<string> WebClientListener;

        public bool IsWebActive { get; private set; }

        public string WebCommsVersion
        {
            get;
            set;
        }

        private void Awake()
        {
            WebCommsVersion = "v1";
        }

        public void Begin()
        {
            //wont work unless this is on for editor
            if (AppManager.Instance.Settings.projectSettings.streamingMode == WebClientMode.None || Application.isEditor)
            {
                Debug.Log("Streaming cannot be configured in editor mode || WebClientMode.None");
                return;
            }

            Debug.Log("Initialising Streaming mode");

            BackendMode = AppManager.Instance.Settings.projectSettings.streamingMode;
            WebCommsVersion = AppManager.Instance.Settings.projectSettings.webCommsVersionList[AppManager.Instance.Settings.projectSettings.webCommsVersion];

#if BRANDLAB360_STREAMING && !UNITY_EDITOR

            // if the streaming package is present, intialize it 

            if (StreamingManager.Instance != null && !Application.isEditor)
            {
                StreamingManager.Instance.Begin(BackendMode == WebClientMode.Vagon ? 1 : 0, onStreamingMessageData, WebCommsVersion);
            }
#endif

#if UNITY_WEBGL
            if (BackendMode == WebClientMode.WebGL)
            {
                var webGlManager = GameObject.Find("WebGlManager");

                if (webGlManager != null)
                {
                    webGlManager.SendMessage("Begin");
                }
            }
#endif
        }

        private void OnDestroy()
        {
#if BRANDLAB360_STREAMING && !UNITY_EDITOR
            if (StreamingManager.Instance != null)
            {
                if (BackendMode == WebClientMode.Vagon)
                {
                    StreamingManager.Instance.VagonOnDataListner -= onStreamingMessageData;

                }
                else if (BackendMode == WebClientMode.PureWeb)
                {
                    StreamingManager.Instance.PureWebOnDataListner -= onStreamingMessageData;
                }
            }
#endif
        }

        /// <summary>
        /// Send data to the webclient
        /// </summary>
        /// <param name="data">A json string containing the object to send</param>
        public void Send(string data)
        {
            //this will decide what version to use
            string temp = WebClientMessaging.Construct(data);

            Debug.Log("WebClient method SEND: " + temp);

#if BRANDLAB360_STREAMING && !UNITY_EDITOR

            // if streaming package is present, send via furioos, pureweb or bl360

            if (StreamingManager.Instance != null)
            {
                if (BackendMode == WebClientMode.Vagon)
                {
                    StreamingManager.Instance.SendVagonMessage(temp);
                }
                else if (BackendMode == WebClientMode.PureWeb)
                {
                    StreamingManager.Instance.SendPureWeb(temp);
                }
            }
#endif

#if UNITY_WEBGL

            if (BackendMode == WebClientMode.WebGL)
            {
                var webGlManager = GameObject.Find("WebGlManager");

                if (webGlManager != null)
                {
                    webGlManager.SendMessage("Send", temp);
                }
            }
#endif

#if UNITY_EDITOR
            ProcessReceiveJson(temp);
#endif
        }

        /// <summary>
        /// Process the JSON string received from the webclient message
        /// </summary>
        /// <param name="json">A JSON object containing data sent from the webclient</param>
        public void ProcessReceiveJson(string json)
        {
            var response = JsonUtility.FromJson<PWResponce>(json).OrDefaultWhen(x => x.action > -100);

            if(response == null)
            {
                Debug.Log("WebClient Ignore response from PW");
                return;
            }

            Debug.Log("WebClient ReceiveJson: " + json);

            //this will handle the version control message
            json = WebClientMessaging.Return(json);

            //invoke action event to all listeners
            if (WebClientListener != null)
            {
                WebClientListener.Invoke(json);
            }
        }

        private void onStreamingMessageData(string json)
        {
            Debug.Log("onStreamingMessageData recieved=" + json);

            ProcessReceiveJson(json);
        }

        [System.Serializable]
        private class PWResponce
        {
            public int type;
            public float width;
            public float height;
            public int action = -100;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(WebclientManager), true)]
        public class WebclientManager_Editor : BaseInspectorEditor
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

    /// <summary>
    /// Denotes which backend/messaging system we are using 
    /// </summary>
    public enum WebClientMode { None, Vagon, PureWeb, BrandLab360, WebGL };
}