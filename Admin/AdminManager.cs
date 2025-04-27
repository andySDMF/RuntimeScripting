using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class AdminManager : Singleton<AdminManager>
    {
        [Header("Panels")]
        public GameObject adminPanel;
        public GameObject loginPanel;

        [Header("Login")]
        [SerializeField]
        public TMP_InputField username;
        [SerializeField]
        public TMP_InputField password;
        [SerializeField]
        public TMP_InputField project;

        [Header("Admin Tabs")]
        [SerializeField]
        public Transform tabs;

        [Header("Admin Tab Options")]
        [SerializeField]
        public GameObject tabConsole;
        [SerializeField]
        public GameObject tabSystem;
        [SerializeField]
        public GameObject tabProfiler;
        [SerializeField]
        public GameObject tabNetwork;
        [SerializeField]
        public GameObject tabTools;


        [Header("Admin Logs")]
        [SerializeField]
        public ConsoleLog consoleLog;
        [SerializeField]
        public AdminTools adminTools;


        [Header("Flythrough")]
        public GameObject flythroughOverlay;

        public static AdminManager Instance
        {
            get
            {
                return ((AdminManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private LoginResponse m_loginResponce;
        private bool m_LoggedIn = false;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void Begin()
        {
            Application.logMessageReceived += consoleLog.HandleDebugLog;

            ToggleAdminPanel(false);
            consoleLog.gameObject.SetActive(false);

            tabs.GetComponentsInChildren<UnityEngine.UI.Toggle>(true).ToList()[0].isOn = true;

#if BRANDLAB360_INTERNAL
            if(tabSystem)
                tabSystem.SetActive(AppManager.Instance.Settings.projectSettings.showSystemTab);

            if (tabProfiler)
                tabProfiler.SetActive(AppManager.Instance.Settings.projectSettings.showProfilerTab);

            if (tabNetwork)
                tabNetwork.SetActive(AppManager.Instance.Settings.projectSettings.showNetworkTab);
#else
            if(tabSystem)
                tabSystem.SetActive(false);

            if (tabProfiler)
                tabProfiler.SetActive(false);

             if (tabNetwork)
                tabNetwork.SetActive(false);
#endif

            if (tabTools)
                tabTools.SetActive(AppManager.Instance.Settings.projectSettings.showToolsTab);

            project.text = AppManager.Instance.Data.ProjectID;
        }

        private void OnLoginResponceCallback(LoginResponse obj)
        {
            m_loginResponce = obj;
            m_LoggedIn = true;
            loginPanel.SetActive(false);
        }

        private void Update()
        {
            if(!AppManager.Instance.Data.IsMobile)
            {
                if (InputManager.Instance.GetKey("LeftShift") || InputManager.Instance.GetKey("RightShift"))
                {
                    if (InputManager.Instance.GetKeyDown("F1"))
                    {
                        ToggleAdminPanel(!adminPanel.activeInHierarchy);
                    }
                }
            }
        }

        public void Login()
        {
            if(m_LoggedIn)
            {
                return;
            }

            LoginsAPI.Instance.LoginUser(username.text, password.text, project.text, OnLoginResponceCallback);
        }

        /// <summary>
        /// Toggle the Admin Panel gameobject
        /// </summary>
        public void ToggleAdminPanel()
        {
            if (Application.isPlaying)
            {
                ToggleAdminPanel(!adminPanel.activeInHierarchy);
            }
        }

        /// <summary>
        /// Toggle the Admin Panel gameobject
        /// </summary>
        public void ToggleAdminPanel(bool isOn)
        {
            if(Application.isPlaying)
            {
                if(!m_LoggedIn && isOn && AppManager.Instance.Settings.projectSettings.enableAdminLogin)
                {
                    loginPanel.SetActive(true);
                }

                adminPanel.SetActive(isOn);

                if (AppManager.Instance.Data.GlobalVideoChatUsed && AppManager.Instance.Data.RoomEstablished)
                {
                    if(isOn)
                    {
                        AppManager.Instance.ToggleVideoChat(false, "");
                    }
                    else
                    {
                        StartupManager.Instance.OpenGlobalVideoChat();
                    }
                }
            }
        }

        public void ToggleFlythroughOverlay(bool toggle)
        {
            flythroughOverlay.SetActive(toggle);
        }

        public void ExitFlythrough()
        {
            adminTools.ExitFlythrough();
        }

        public void SendLogToWebClient()
        {
            string output = "";
            int count = 1;
            WebClientLogMessage webClientMessage = new WebClientLogMessage();

            foreach (ConsoleLog.DebugMessage dm in consoleLog.AllLogs)
            {
                output += "[" + dm.logType.ToString() + "]" + dm.log;

                if(count < consoleLog.AllLogs.Count)
                {
                    output += System.Environment.NewLine;
                }
            }

            webClientMessage.downloadLog = output;
            WebclientManager.Instance.Send(JsonUtility.ToJson(webClientMessage));
        }

        public void DebugLog(string str)
        {
            Debug.LogError(str);
        }

        [System.Serializable]
        private class WebClientLogMessage
        {
            public string downloadLog;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AdminManager), true)]
        public class AdminManager_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("adminPanel"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("loginPanel"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("username"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("password"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("project"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tabs"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tabConsole"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tabSystem"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tabProfiler"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tabNetwork"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tabTools"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("consoleLog"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("adminTools"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("flythroughOverlay"), true);

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
