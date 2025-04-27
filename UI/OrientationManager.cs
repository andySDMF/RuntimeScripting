using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Newtonsoft.Json;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// Manager to handle orientation changes on mobile portrait/landscape
    /// </summary>
    public class OrientationManager : Singleton<OrientationManager>
    {
        private OrientationType currentOrientation = OrientationType.landscape;

        public OrientationType CurrentOrientation
        {
            get { return currentOrientation; }
        }

        public Vector2 ScreenSize
        {
            get
            {
                return new Vector2(m_screenWidth, m_screenHeight);
            }
        }

        public bool RecievedWebResponse
        {
            get;
            private set;
        }

        public UnityAction<OrientationType, int, int> OnOrientationChanged;

        public static OrientationManager Instance
        {
            get
            {
                return ((OrientationManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private int m_screenWidth = 1920;
        private int m_screenHeight = 1080;
        private int extraPadding = 50;

        public int ExtraPadding
        {
            get { return extraPadding; }
            set { extraPadding = value; }
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            RecievedWebResponse = false;
            
            WebclientManager.WebClientListener += ReceiveWebclientResponse;
        }

        private void Start()
        {
            // Default orientation should be set at project settings instead of here
            currentOrientation = AppManager.Instance.Settings.projectSettings.orientation;
        }

        private void OnDestroy()
        {
             WebclientManager.WebClientListener -= ReceiveWebclientResponse;
        }

        /// <summary>
        /// Callback for webclient response msg
        /// </summary>
        /// <param name="json">webclient response</param>
        public void ReceiveWebclientResponse(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            if(!AppManager.Instance.Data.IsMobile)
            {
                WebclientManager.WebClientListener -= ReceiveWebclientResponse;
                return;
            }

#if !UNITY_EDITOR
            if (!AppManager.Instance.Settings.projectSettings.streamingMode.Equals(WebClientMode.PureWeb))
            {
                WebclientManager.WebClientListener -= ReceiveWebclientResponse;
                return;
            }
#endif

            if(json.Contains("screenWidth") && json.Contains("screenHeight"))
            {
                // receive the orientation change event from webclient
                var response = JsonUtility.FromJson<OrientationResponse>(json);

                if (response != null)
                {
                    RecievedWebResponse = true;
                    Debug.Log("Received Orientation Response: " + JsonUtility.ToJson(response));
                    HandleOrientation(response);
                }
            }
        }

        /// <summary>
        /// Handle the orientation change
        /// </summary>
        /// <param name="newOrientation">new orientation to change</param>
        private void HandleOrientation(OrientationResponse response)
        {
            OrientationType newOrientation = OrientationType.landscape;

            if (response.orientation != null)
            {
                if (response.orientation.Contains("portrait"))
                {
                    newOrientation = OrientationType.portrait;
                }
            }
            else
            {
                if (response.orientation != null && response.orientation.Contains("portrait"))
                {
                    newOrientation = OrientationType.portrait;
                }
                else
                {
                    newOrientation = OrientationType.landscape;
                }
            }

            Debug.Log("Handling orientation [" + newOrientation.ToString() + "]");

            if (newOrientation != currentOrientation)
            {
                currentOrientation = newOrientation;
                m_screenWidth = response.screenWidth;
                m_screenHeight = response.screenHeight;

                if (OnOrientationChanged != null)
                {
                    OnOrientationChanged.Invoke(newOrientation, m_screenWidth, m_screenHeight);
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OrientationManager), true)]
        public class OrientationManager_Editor : BaseInspectorEditor
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

    [System.Serializable]
    public class OrientationResponse
    {
        public string orientation;
        public int screenWidth;
        public int screenHeight;
    }

    public enum OrientationType { portrait, landscape };

    public interface IOrientationUI
    {
        void Adjust(float aspectRatio);
    }
}