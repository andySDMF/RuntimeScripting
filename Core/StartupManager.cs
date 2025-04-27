using System.Collections.Generic;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class StartupManager : Singleton<StartupManager>
    {
        public static StartupManager Instance
        {
            get
            {
                return ((StartupManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Overlay")]
        public GameObject loadingOverlay;
        public TMP_Text queueText;

        [Header("Room Info")]
        private bool allFull;
        private int checkTime;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onStarted;

        private bool m_hasBegun = false;
        private List<RoomLobby> m_photonRooms = new List<RoomLobby>();

        private void Start()
        {
            if (AppManager.IsCreated)
            {
                CoreManager.Instance.OnJoinedRoomEvent.AddListener(OnJoinedRoom);
                CoreManager.Instance.OnConnectedEvent.AddListener(OnConnectedToNetowork);

                loadingOverlay.SetActive(true);
            }
        }

        public void Begin()
        {
            if (m_hasBegun) return;

            loadingOverlay.SetActive(true);
            m_hasBegun = true;

            SceneManager.SetActiveScene(gameObject.scene);

            if (AppManager.Instance.Data.Mode == MultiplayerMode.Online)
            {
                CoreManager.Instance.CurrentState = state.Inactive;
                MMOManager.Instance.Connect();
            }
            else
            {
                CoreManager.Instance.CurrentState = state.Running;
                PlayerManager.Instance.SpawnLocalPlayer(false);
            }
        }

        /// <summary>
        /// On connected to photon callback
        /// </summary>
        public void OnConnectedToNetowork()
        {
            if (CoreManager.Instance.projectSettings.joinRoomMode.Equals(JoinRoomMode.Lobby))
                RoomManager.instance.OnRoomListUpdated += OnRoomUpdated;

            MMOManager.Instance.JoinLobby();
        }

        public void OnJoinedLobby()
        {
            if(!string.IsNullOrEmpty(AppManager.Instance.Data.inviteCode))
            {
                StartCoroutine(ProcessInviteRoom());

                return;
            }

            if (CoreManager.Instance.projectSettings.joinRoomMode.Equals(JoinRoomMode.Queue))
            {
                StartCoroutine(CheckQueue());
            }
            else if (CoreManager.Instance.projectSettings.joinRoomMode.Equals(JoinRoomMode.Lobby))
            {
                StartCoroutine(CheckLobby());
            }
            else
            {
                StartCoroutine(RoomDuplicate());
            }
        }

        private void OnRoomUpdated()
        {
            //destroy UI rooms if no longer exists
            List<RoomLobby> indexes = new List<RoomLobby>();

            int activeScenes = 0;

            //renew room list
            for (int i = 0; i < RoomManager.instance.allRooms.Count; i++)
            {
                RoomLobby rLobby = m_photonRooms.FirstOrDefault(x => x.Room.Equals(RoomManager.instance.allRooms[i].Name));

                if (rLobby != null)
                {
                    //update
                    rLobby.Set(RoomManager.instance.allRooms[i]);
                }
                else
                {
                    //create new
                    if(RoomManager.instance.allRooms[i].MaxPlayers > 0)
                    {
                        rLobby = RoomManager.instance.CreateRoomLobby(RoomManager.instance.allRooms[i]);

                        if (rLobby != null)
                        {
                            m_photonRooms.Add(rLobby);
                        }
                    }
                }

                if(!RoomManager.instance.allRooms[i].RemovedFromList)
                {
                    activeScenes++;
                }

                if(rLobby != null)
                {
                    rLobby.gameObject.SetActive(!RoomManager.instance.allRooms[i].RemovedFromList);
                }
                else
                {
                    activeScenes--;
                    rLobby.gameObject.SetActive(false);
                }
            }

            if(activeScenes <= 0 && indexes.Count > 0)
            {
                Debug.Log("<color=red>NO ROOMS FOUND, AUTO CREATING ROOM</color>");
                HUDManager.Instance.ShowRoomLobbyPanel(false);
                ConnectToRoom(CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString());
            }
        }

        private IEnumerator ProcessInviteRoom()
        {
            while (!RoomManager.instance.AttainedRoomList)
            {
                yield return null;
            }

            string inviteRoom = CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString() + "_" + AppManager.Instance.Data.inviteCode;

            RoomLobby.RoomInfo room = RoomManager.instance.allRooms.FirstOrDefault(x => x.Name.Equals(inviteRoom));

            //check if the room exists
            if (room != null)
            {
                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
                    ConnectToRoom(inviteRoom);
                }
                else
                {
                    ConnectToRoom(room.id);
                }
            }
            else
            {
                CreateNewRoom(inviteRoom);
            }
        }

        private IEnumerator CheckLobby()
        {
            while (!RoomManager.instance.AttainedRoomList)
            {
                yield return null;
            }

            if (RoomManager.instance.allRooms.Count > 0)
            {
                HUDManager.Instance.ShowRoomLobbyPanel(true);
            }
            else
            {
                Debug.Log("<color=red>NO ROOMS FOUND, AUTO CREATING ROOM</color>");
                ConnectToRoom(CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString());
            }
        }

        private IEnumerator CheckQueue()
        {
            while(!RoomManager.instance.AttainedRoomList)
            {
                yield return null;
            }

            List<RoomLobby.RoomInfo> rooms = new List<RoomLobby.RoomInfo>();
            allFull = false;

            foreach(RoomLobby.RoomInfo r in RoomManager.instance.allRooms)
            {
                if(r.Name.Equals(CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString()) && r.RemovedFromList == false)
                {
                    rooms.Add(r);
                }
            }

            if (rooms.Count > 0)
            {
                allFull = rooms.All(c => c.PlayerCount == c.MaxPlayers);
                Debug.Log($"<color=green>ROOMS FOUND ({rooms.Count})</color>");


                while (allFull)
                {
                    queueText.gameObject.SetActive(true);
                    queueText.text = $"You are in queue ({rooms.Count} FULL)";
                    yield return new WaitForEndOfFrame();

                    rooms.Clear();

                    foreach (RoomLobby.RoomInfo r in RoomManager.instance.allRooms)
                    {
                        if (r.Name.Equals(CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString()) && r.RemovedFromList == false)
                        {
                            rooms.Add(r);
                        }
                    }

                    allFull = rooms.All(c => c.PlayerCount == c.MaxPlayers);
                }


                queueText.gameObject.SetActive(false);
                queueText.text = "";

                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
                    ConnectToRoom(CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString());
                }
                else
                {
                    ConnectToRoom(rooms[0].id);
                }
            }
            else
            {
                Debug.Log("<color=red>NO ROOMS FOUND</color>");

                CreateNewRoom(CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString());
            }
        }

        private IEnumerator RoomDuplicate()
        {
            while (!RoomManager.instance.AttainedRoomList)
            {
                yield return null;
            }

            if (RoomManager.instance.allRooms.Count > 0)
            {
                allFull = RoomManager.instance.allRooms.All(c => c.PlayerCount == c.MaxPlayers);

                if (allFull)
                {
                    CoreManager.Instance.AddToProjectID("_" + RoomManager.instance.CountActiveRooms.ToString());
                    CreateNewRoom(CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString());
                }
                else
                {
                    System.Collections.Generic.List<RoomLobby.RoomInfo> freeRooms = new System.Collections.Generic.List<RoomLobby.RoomInfo>();

                    for (int i = 0; i < RoomManager.instance.allRooms.Count; i++)
                    {
                        if (RoomManager.instance.allRooms[i].PlayerCount < RoomManager.instance.allRooms[i].MaxPlayers && !RoomManager.instance.allRooms[i].RemovedFromList)
                        {
                            freeRooms.Add(RoomManager.instance.allRooms[i]);
                        }
                    }

                    if(freeRooms.Count > 0)
                    {
                        if(AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                        {
                            ConnectToRoom(freeRooms[Random.Range(0, freeRooms.Count)].Name);
                        }
                        else
                        {
                            ConnectToRoom(freeRooms[Random.Range(0, freeRooms.Count)].id);
                        }
                    }
                    else
                    {
                        CreateNewRoom(CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString());
                        Debug.Log("<color=red>NO ROOMS FOUND</color>");
                    }
                }

                Debug.Log($"<color=green>ROOMS FOUND ({RoomManager.instance.allRooms.Count})</color>");
            }
            else
            {
                Debug.Log("<color=red>NO ROOMS FOUND</color>");
                CreateNewRoom(CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString());
            }    
        }

        public void CreateNewRoom(string roomName)
        {
            Debug.Log($"RecievedRoomID: {CoreManager.Instance.RecievedRoomID}");

            string username = AppManager.Instance.Data.NickName;

            if(AppManager.Instance.Settings.projectSettings.checkDuplicatePhotonNames)
            {
                username = "$0$" + AppManager.Instance.Data.NickName;
            }
            else
            {
                username = AppManager.Instance.Data.NickName;
            }

            MMOManager.Instance.SetUsername(username);

            if (CoreManager.Instance.RecievedRoomID)
            {
                MMOManager.Instance.CreateRoom(roomName);
            }

            if (CoreManager.Instance.projectSettings.joinRoomMode.Equals(JoinRoomMode.Lobby))
            {
                RoomManager.instance.OnRoomListUpdated -= OnRoomUpdated;

                for (int i = 0; i < m_photonRooms.Count; i++)
                {
                    Destroy(m_photonRooms[i].gameObject);
                }

                m_photonRooms.Clear();
            }

            CoreManager.Instance.OnConnectedEvent.RemoveListener(OnConnectedToNetowork); 
        }

        public void ConnectToRoom(string room)
        {
            string username = AppManager.Instance.Data.NickName;

            if (AppManager.Instance.Settings.projectSettings.checkDuplicatePhotonNames)
            {
                //get the names list of the players currently in the room
                RoomLobby.RoomInfo rInfo = RoomManager.instance.allRooms.FirstOrDefault(x => x.Name.Equals(room));

                Debug.Log(rInfo.CustomProperties);

                foreach (DictionaryEntry item in rInfo.CustomProperties)
                {
                    RoomManager.RoomChangeWrapper wrapper = JsonUtility.FromJson<RoomManager.RoomChangeWrapper>(item.Value.ToString());

                    if (wrapper != null)
                    {
                        if (wrapper.HasData("EVENT_TYPE"))
                        {
                            Debug.Log("Namelist: " + wrapper.data + "= " + JsonUtility.ToJson(wrapper));

                            RoomManager.RoomChangeData names = wrapper.GetData("EVENT_TYPE");

                            if (names != null)
                            {
                                if (names.data.Equals("NAMELIST"))
                                {
                                    RoomManager.NameWrapper nWrapper = JsonUtility.FromJson<RoomManager.NameWrapper>(wrapper.GetData("LIST").data);
                                    RoomManager.RoomName pName = nWrapper.x.FirstOrDefault(x => x.n.Equals(AppManager.Instance.Data.NickName));

                                    if (pName != null)
                                    {
                                        username = "$" + pName.c + 1 + "$" + AppManager.Instance.Data.NickName;
                                    }
                                    else
                                    {
                                        username = "$0$" + AppManager.Instance.Data.NickName;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                username = AppManager.Instance.Data.NickName;
            }

            MMOManager.Instance.SetUsername(username);

            if (CoreManager.Instance.RecievedRoomID)
            {
                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
                    CoreManager.Instance.JoinRoom(room);
                }
                else
                {
                    if(room.Contains(AppManager.Instance.Settings.projectSettings.ProjectID))
                    {
                        CoreManager.Instance.JoinRoom(room);
                    }
                    else
                    {
                        CoreManager.Instance.JoinRoomByID(room);
                    }
                }
            }
            else
            {
                if (CoreManager.Instance.RoomID.ToString().Equals("0"))
                {
                    if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                    {
                        CoreManager.Instance.JoinRoom(room);
                    }
                    else
                    {
                        if (room.Contains(AppManager.Instance.Settings.projectSettings.ProjectID))
                        {
                            CoreManager.Instance.JoinRoom(room);
                        }
                        else
                        {
                            CoreManager.Instance.JoinRoomByID(room);
                        }
                    }
                }
                else
                {
                    CoreManager.Instance.WaitingForRoomID = true;
                }
            }

            CoreManager.Instance.OnConnectedEvent.RemoveListener(OnConnectedToNetowork);
        }

        /// <summary>
        /// Joined Photon Room callback
        /// </summary>
        public void OnJoinedRoom()
        {
            CoreManager.Instance.CurrentState = state.Running;
            RoomManager.instance.ConnectedToRoom();

            // If using video chat, tell the webclient to load it
            OpenGlobalVideoChat();

            if (onStarted != null)
            {
                onStarted.Invoke();
            }

            CoreManager.Instance.OnJoinedRoomEvent.RemoveListener(OnJoinedRoom);

            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Offline))
            {
                StartCoroutine(MMORoom.Instance.OnJoinRoom());
            }
        }

        /// <summary>
        /// Called to open global video chat
        /// </summary>
        public void OpenGlobalVideoChat(bool overrideRoomVariable = false)
        {
            // If using video chat, tell the webclient to load it
            if (CoreManager.Instance.RoomID.ToString().Equals("0")) return;

            if (CoreManager.Instance.projectSettings.useWebClientRoomVariable || overrideRoomVariable)
            {
                AppManager.Instance.Data.GlobalVideoChatUsed = true;

                string inviteCode = string.IsNullOrEmpty(AppManager.Instance.Data.inviteCode) ? "" : "_" + AppManager.Instance.Data.inviteCode;
                AppManager.Instance.ToggleVideoChat(true, CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString() + inviteCode);
            }
        }

        /// <summary>
        /// Called to show Welcome Screen
        /// </summary>
        public void ShowWelcomeScreen()
        {
            BlackScreen.Instance.Show(false);
            loadingOverlay.SetActive(false);
            HUDManager.Instance.ShowWelcomePanel(true);
        }

        [System.Serializable]
        public enum JoinRoomMode { Queue, Duplicate, Lobby }

#if UNITY_EDITOR
        [CustomEditor(typeof(StartupManager), true)]
        public class StartupManager_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("loadingOverlay"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("queueText"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("onStarted"), true);

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
