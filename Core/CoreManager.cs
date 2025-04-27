using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

#if BRANDLAB360_INTERNAL
using BrandLab360.Internal;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class CoreManager : Singleton<CoreManager>
    {
        public static CoreManager Instance
        {
            get
            {
                if(!((CoreManager)instance).m_hasInit)
                {
                    ((CoreManager)instance).Init();
                }

                return ((CoreManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        [HideInInspector]
        public state CurrentState;

        [HideInInspector]
        public int RoomID = 0;

        [Header("Settings")]
        public ProjectSettings projectSettings;
        public PlayerControlSettings playerSettings;
        public AKFSettings AFKSettings;
        public ChatSettings chatSettings;
        public HUDSettings HUDSettings;
        public NPCSettings NPCSettings;

        private bool receivedRoomID = false;
        private bool waitingForRoomID = false;
        private bool hasJoinedRoom = false;


        [Header("Canvas")]
        public Canvas MainCanvas;
        public GameObject CalenderMananger;

        [Header("Environment")]
        [SerializeField]
        private Environment environment;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onConnectedEvent = new UnityEvent();
        [SerializeField]
        private UnityEvent onJoinedRoomEvent = new UnityEvent();


        public string ProjectID
        {
            get;
            private set;
        }

        public bool IsMobile
        {
            get;
            private set;
        }

        public bool IsOffline
        {
            get
            {
                return AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Offline);
            }
        }

        public bool RecievedRoomID
        {
            get
            {
                return receivedRoomID;
            }
        }

        public bool WaitingForRoomID
        {
            get
            {
                return waitingForRoomID;
            }
            set
            {
                waitingForRoomID = value;
            }
        }

        public bool HasJoinedRoom
        {
            get
            {
                return hasJoinedRoom;
            }
        }

        public Environment SceneEnvironment
        {
            get
            {
                return environment;
            }
            set
            {
                environment = value;
            }
        }

        public UnityEvent OnConnectedEvent
        { 
            get 
            { 
                return onConnectedEvent; 
            } 
            set 
            { 
                onConnectedEvent = value; 
            } 
        }

        public UnityEvent OnJoinedRoomEvent 
        { 
            get
            {
                return onJoinedRoomEvent;
            }
            set
            {
                onJoinedRoomEvent = value;
            }
        }

        private bool m_hasInit = false;

        private void Awake()
        {
            if(!m_hasInit)
            {
                Init();
            }
        }

        private void Init()
        {
            if (m_hasInit) return;

            m_hasInit = true;

            AppManager appManager = FindFirstObjectByType<AppManager>(FindObjectsInactive.Include);

            if (appManager == null)
            {
                //need to load login scene
                SceneManager.LoadScene(0);

                return;
            }

            if (environment == null)
            {
                environment = FindFirstObjectByType<Environment>(FindObjectsInactive.Include);
            }

            if (environment == null)
            {
                Debug.Log("No Environemnt Object exists!");
            }
            else
            {
                environment.Activate(false);
            }

#if BRANDLAB360_INTERNAL
            CalendarManager calender =  CalenderMananger.AddComponent<CalendarManager>();
            calender.Initialise(ProjectID, (int)AppManager.Instance.Settings.projectSettings.loginMode, AppManager.Instance.Data.LoginProfileData.name, PlayerManager.Instance.gameObject, RaycastManager.Instance.gameObject, HUDManager.Instance.gameObject);
#endif
        }

        public void Begin()
        {
            projectSettings = AppManager.Instance.Settings.projectSettings;
            playerSettings = AppManager.Instance.Settings.playerSettings;
            AFKSettings = AppManager.Instance.Settings.AFKSettings;
            chatSettings = AppManager.Instance.Settings.chatSettings;
            HUDSettings = AppManager.Instance.Settings.HUDSettings;
            NPCSettings = AppManager.Instance.Settings.NPCSettings;
       
            ProjectID = projectSettings.ProjectID;

            SpawnManager.Instance.EnableSpawnCamera(true);

            if(environment != null)
            {
                environment.Activate(true);
            }
            
            if (Application.isEditor || projectSettings.streamingMode.Equals(WebClientMode.None))
            {
                receivedRoomID = true;
            }

            WebClientListener(AppManager.Instance.Data.WebClientResponce);

            CameraBrain.Instance.ApplySetting();

            HUDManager.Instance.SetHUD();
            NavigationManager.Instance.Begin();
            StartupManager.Instance.Begin();
        }

        /// <summary>
        /// Connected to Photon 
        /// </summary>
        public void OnConnected()
        {
            if (onConnectedEvent != null)
            {
                onConnectedEvent.Invoke();
            }
        }

        /// <summary>
        /// Joined Photon Room
        /// </summary>
        public void OnJoinedRoom()
        {
            APIHandler();

            if(projectSettings.displayOnJoinedRoom)
            {
                AdminManager.Instance.ToggleAdminPanel(true);
            }

            if (onJoinedRoomEvent != null)
            {
                onJoinedRoomEvent.Invoke();
            }
        }

        public void APIHandler()
        {
            if (ApiManager.Instance.AccessTokenSuccess)
            {
                ProductAPI.Instance.OnAPIGETComplete += AssortmentAPI.Instance.GetProducts;
                ProductAPI.Instance.GetProducts();
                ContentsAPI.Instance.GetContents();
                FloorplanAPI.Instance.GetFloorplanItems();
                DataAPI.Instance.GetAll(ProjectID);
                ReportAPI.Instance.GetReports();
                NoticeBoardAPI.Instance.GetNoticeboards();

                //this might need to be called after all product placement object have been created
                //AssortmentAPI.Instance.GetProducts();
            }
        }

        /// <summary>
        /// Listener callback for the webclient
        /// </summary>
        /// <param name="obj"></param>
        private void WebClientListener(string json)
        {
            StartedResponse response = null;

            if (!string.IsNullOrEmpty(json))
            {
                response = (!AppManager.Instance.Settings.editorTools.createWebClientSimulator) ? JsonUtility.FromJson<StartedResponse>(json) : JsonUtility.FromJson<StartedResponse>(json).OrDefaultWhen(x => x.name == null);
            }

            if (response != null)
            {
                if (!RecievedRoomID)
                {
                    // If we have the default 'Plugin Demo' projectid and a different value was found in the response, use that.
                    if (AppManager.Instance.Settings.projectSettings.ProjectID == "Plugin Demo") { ProjectID = response.name; }
                    else
                    {
                        ProjectID = response.name;
                    }

                    if (!gameObject.scene.name.Equals(AppManager.Instance.Settings.projectSettings.mainSceneName))
                    {
                        ProjectID = ProjectID + "_" + gameObject.scene.name;
                    }

                    // tell manager to connect to photon now
                    ReceiveRoomID(response.room);

                    IsMobile = response.isMobile;

#if BRANDLAB360_INTERNAL
                    CalendarManager calender = CalenderMananger.GetComponent<CalendarManager>();
                    calender.Initialise(ProjectID, (int)AppManager.Instance.Settings.projectSettings.loginMode, AppManager.Instance.Data.LoginProfileData.name, PlayerManager.Instance.gameObject, RaycastManager.Instance.gameObject, HUDManager.Instance.gameObject);
#endif
                }
                else
                {
                    IsMobile = response.isMobile;
                    ProjectID = response.name;
                    RoomID = response.room;

                    if (!gameObject.scene.name.Equals(projectSettings.mainSceneName))
                    {
                        ProjectID = ProjectID + "_" + gameObject.scene.name;
                    }

#if BRANDLAB360_INTERNAL
                    CalendarManager calender = CalenderMananger.GetComponent<CalendarManager>();
                    calender.Initialise(ProjectID, (int)AppManager.Instance.Settings.projectSettings.loginMode, AppManager.Instance.Data.LoginProfileData.name, PlayerManager.Instance.gameObject, RaycastManager.Instance.gameObject, HUDManager.Instance.gameObject);
#endif
                }
            }
            else
            {
                ProjectID = AppManager.Instance.Settings.projectSettings.ProjectID;

                if (!gameObject.scene.name.Equals(AppManager.Instance.Settings.projectSettings.mainSceneName))
                {
                    ProjectID = ProjectID + "_" + gameObject.scene.name;
                }

                ReceiveRoomID(0);
            }
        }

        /// <summary>
        /// Received room id from webclient
        /// </summary>
        /// <param name="id">room id used for photon room name and videochat room</param>
        public void ReceiveRoomID(int id)
        {
            if(!AppManager.Instance.Data.RoomEstablished)
            {
                RoomID = id;
            }
            else
            {
                RoomID = AppManager.Instance.Data.RoomID;
            }

            receivedRoomID = true;

            if (waitingForRoomID)
            {
                waitingForRoomID = false;
                JoinRoom(ProjectID + "_" + RoomID.ToString());
            }
        }

        /// <summary>
        /// Join photon room
        /// </summary>
        public void JoinRoom(string room)
        {
            hasJoinedRoom = true;
            MMOManager.Instance.JoinRoom(room);
        }

        /// <summary>
        /// Join photon room
        /// </summary>
        public void JoinRoomByID(string room)
        {
            hasJoinedRoom = true;
            MMOManager.Instance.JoinRoomID(room);
        }

        /// <summary>
        /// Adds to the projectID
        /// </summary>
        /// <param name="str"></param>
        public void AddToProjectID(string str)
        {
            string proj = ProjectID;
            ProjectID = proj + str;

#if BRANDLAB360_INTERNAL
            CalendarManager calender = CalenderMananger.GetComponent<CalendarManager>();
            calender.Initialise(ProjectID, (int)AppManager.Instance.Settings.projectSettings.loginMode, AppManager.Instance.Data.LoginProfileData.name, PlayerManager.Instance.gameObject, RaycastManager.Instance.gameObject, HUDManager.Instance.gameObject);
#endif
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(CoreManager), true)]
        public class CoreManager_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();


                if (Application.productName.Equals("BL360 Plugin"))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("projectSettings"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("playerSettings"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("AFKSettings"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chatSettings"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("HUDSettings"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("NPCSettings"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("MainCanvas"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("CalenderMananger"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("environment"), true);
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("onConnectedEvent"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onJoinedRoomEvent"), true);

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}
