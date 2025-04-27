using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using Colyseus;

#if BRANDLAB360_INTERNAL
using BrandLab360.Internal;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager instance;
        public List<RoomLobby.RoomInfo> allRooms = new List<RoomLobby.RoomInfo>();

        public bool AttainedRoomList { get; private set; }
        public System.Action OnRoomListUpdated { get; set; }

        public int CountActiveRooms
        {
            get
            {
                int n = 0;

                for(int i = 0; i < allRooms.Count; i++)
                {
                    if(!allRooms[i].RemovedFromList)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        private Coroutine m_process;

        private void Awake()
        {
            if (instance == null)
                instance = this;

            AttainedRoomList = false;

            if (AppManager.IsCreated && AppManager.Instance.Data.Mode == MultiplayerMode.Online)
            {
                MMOManager.Instance.OnNetworkProtocalCreated += OnProtocalCreated;
            }
        }

        private void OnProtocalCreated()
        {
            Debug.Log("Room Manager Netowork Protocal established. Setting Room Listener");

            MMOManager.Instance.OnNetworkProtocalCreated -= OnProtocalCreated;

            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                PhotonPunRoomManager.Callback_OnRoomListUpdate += OnRoomListUpdate;
#endif
            }
            else
            {
                GetColyseusRooms();
            }
        }

        private async void GetColyseusRooms()
        {
            ColyseusManager.CustomRoomAvailable [] rooms = await MMOManager.Instance.ColyseusManager_Ref.GetRooms();
            List<RoomLobby.RoomInfo> roomList = new List<RoomLobby.RoomInfo>();

            if(rooms != null)
            {
                for (int i = 0; i < rooms.Length; i++)
                {
                    if (string.IsNullOrEmpty(rooms[i].metadata.roomName)) continue;

                    foreach (RoomLobby.RoomInfo room in allRooms)
                    {
                        RoomLobby.RoomInfo rInfo = allRooms.FirstOrDefault(x => x.Name.Equals(rooms[i].metadata.roomName));

                        if (rInfo == null)
                        {
                            room.RemovedFromList = true;
                        }
                    }
                }

                for (int i = 0; i < rooms.Length; i++)
                {

                    RoomLobby.RoomInfo rInfo = new RoomLobby.RoomInfo();
                    rInfo.id = rooms[i].roomId;
                    rInfo.Name = rooms[i].metadata.roomName;
                    rInfo.MaxPlayers = (int)rooms[i].maxClients;
                    rInfo.PlayerCount = (int)rooms[i].clients;

                    roomList.Add(rInfo);
                }

                RoomListUpdate(roomList);
            }

            await Task.Delay(5000);

            if(!MMOManager.Instance.IsConnected())
            {
                GetColyseusRooms();
            }
        }

        private void OnRoomListUpdate(string data)
        {
            RoomLobby.RoomInfoWrapper wrapper = JsonUtility.FromJson<RoomLobby.RoomInfoWrapper>(data);

            if(wrapper != null)
            {
                RoomListUpdate(wrapper.rooms);
            }
        }

        private void RoomListUpdate(List<RoomLobby.RoomInfo> roomList)
        {
            Debug.Log("Room List Update");

            if(m_process != null)
            {
                StopCoroutine(m_process);
            }

            m_process = null;

            //make temp so it does not break the iteration loop
            List<RoomLobby.RoomInfo> temp = new List<RoomLobby.RoomInfo>();
            temp.AddRange(roomList);

            m_process = StartCoroutine(Process(temp));
        }

        private IEnumerator Process(List<RoomLobby.RoomInfo> roomList)
        {
            while(string.IsNullOrEmpty(CoreManager.Instance.ProjectID))
            {
                yield return null;
            }

            Debug.Log("Processing room list");

            foreach (var room in roomList)
            {
                bool addRoom = false;

                if (string.IsNullOrEmpty(room.Name))
                {
                    continue;
                }

                if (gameObject.scene.name.Equals(AppManager.Instance.Settings.projectSettings.mainSceneName))
                {
                    if (!room.Name.Contains(CoreManager.Instance.ProjectID))
                    {
                        continue;
                    }
                    else
                    {
                        //check to see if the room name contains any other stuff that does not match projectID
                        SwitchSceneTrigger[] allTriggers = FindObjectsByType<SwitchSceneTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                        bool isMainScene = true;

                        for (int i = 0; i < allTriggers.Length; i++)
                        {
                            if (room.Name.Contains(allTriggers[i].SceneName))
                            {
                                isMainScene = false;
                            }
                        }

                        if (!isMainScene)
                        {
                            continue;
                        }

                        addRoom = true;
                    }
                }
                else if (gameObject.scene.name.Equals(AppManager.Instance.Data.SceneSpawnLocation.scene))
                {
                    if (!room.Name.Contains(CoreManager.Instance.ProjectID))
                    {
                        continue;
                    }

                    addRoom = true;
                }

                if (addRoom)
                {
                    RoomLobby.RoomInfo rinfo = allRooms.FirstOrDefault(x => x.Name.Equals(room.Name));

                    if (rinfo == null)
                    {
                        allRooms.Add(room);
                    }
                    else
                    {
                        int n = allRooms.IndexOf(rinfo);
                        allRooms[n] = room;
                    }
                }
            }

            AttainedRoomList = true;
            m_process = null;

            if (OnRoomListUpdated != null)
            {
                OnRoomListUpdated.Invoke();
            }

            yield return null;
        }

        public void ConnectedToRoom()
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                PhotonPunRoomManager.Callback_OnRoomListUpdate -= OnRoomListUpdate;
#endif
            }
        }

        public RoomLobby CreateRoomLobby(RoomLobby.RoomInfo room)
        {
            RoomLobbyDisplay rDisplay = FindFirstObjectByType<RoomLobbyDisplay>(FindObjectsInactive.Include);

            if(rDisplay != null)
            {
                GameObject go = Instantiate(rDisplay.Prefab, rDisplay.Container);
                go.transform.localScale = Vector3.one;
                go.SetActive(true);
                RoomLobby roomLobby = go.GetComponent<RoomLobby>();
                roomLobby.Set(room);

                return roomLobby;
            }

            return null;
        }

        /// <summary>
        /// Json wrapper for sending room properties/data
        /// </summary>
        [System.Serializable]
        public class RoomChangeWrapper
        {
            public string id;
            public List<RoomChangeData> data;

            public RoomChangeData GetData(string key)
            {
                return data.FirstOrDefault(x => x.key.Equals(key));
            }

            public bool HasData(string key)
            {
                return data.FirstOrDefault(x => x.key.Equals(key)) != null;
            }

            public void Add(string key, string value)
            {
                RoomChangeData change = GetData(key);
                int index = -1;

                if (change == null)
                {
                    change = new RoomChangeData();
                }
                else
                {
                    index = data.IndexOf(change);
                }

                if (index > -1)
                {
                    data[index].data = value;
                }
                else
                {
                    change.key = key;
                    change.data = value;
                    data.Add(change);
                }

            }
        }

        /// <summary>
        /// Json Room data class
        /// </summary>
        [System.Serializable]
        public class RoomChangeData
        {
            public string key;
            public string data;
        }

        [System.Serializable]
        public class NameWrapper
        {
            public List<RoomName> x = new List<RoomName>();
        }


        [System.Serializable]
        public class RoomName
        {
            public string n = "";
            public int c = 0;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(RoomManager), true)]
        public class RoomManager_Editor : BaseInspectorEditor
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
}
