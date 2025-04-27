using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if BRANDLAB360_INTERNAL
using BrandLab360.Internal;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{

    public class MMORoom : Singleton<MMORoom>
    {
        public static MMORoom Instance
        {
            get
            {
                return ((MMORoom)instance);
            }
            set
            {
                instance = value;
            }
        }


        /// <summary>
        /// Subscribe actions to be called when a player enters room
        /// </summary>
        public System.Action<IPlayer> OnPlayerEnteredRoom { get; set; }
        /// <summary>
        /// Subscrive actions to be called when a player left room
        /// </summary>
        public System.Action<IPlayer> OnPlayerLeftRoom { get; set; }
        /// <summary>
        /// Subscrive actions to be called when the room is ready
        /// </summary>
        public System.Action OnRoomReady { get; set; }

        private bool m_joinedRoom = false;
        private bool m_roomReady = false;
        private PhotonChatLocalProfile m_localProfile;
        private UserProfile m_businessCardProfiler;

        private Dictionary<string, GameObject> m_instantiatedRoomObjects = new Dictionary<string, GameObject>();

        public UserProfile Profile
        {
            get
            {
                return m_businessCardProfiler;
            }
        }

        public bool RoomReady
        {
            get
            {
                return m_roomReady;
            }
        }


#if BRANDLAB360_INTERNAL
        private PhotonRoom m_photon;

        private PhotonRoom M_PhotonRoom
        {
            get
            {
                if (m_photon == null)
                {
                    m_photon = MMOManager.Instance.PhotonManager_GO.GetComponent<PhotonRoom>();
                    m_photon.Callback_OnRoomPropertiesChange += RoomPropertiesChange;

                }

                return m_photon;
            }
        }
#endif

        private ColyseusRoom m_colyseus;

        private ColyseusRoom M_ColyseusRoom
        {
            get
            {
                if(m_colyseus == null)
                {
                    m_colyseus = MMOManager.Instance.ColyseusManager_Ref.GetComponent<ColyseusRoom>();
                    m_colyseus.Callback_OnRoomPropertiesChange += RoomPropertiesChange;
                }

                return m_colyseus;
            }
        }

        private void Awake()
        {
            if (!AppManager.IsCreated) return;

            m_localProfile = HUDManager.Instance.ProfileGO.GetComponentInChildren<PhotonChatLocalProfile>(true);
            HUDManager.Instance.OnCustomSetupComplete += GetUIReferences;
            AppManager.Instance.Data.CurrentSceneReady = false;
        }

        public void Disconnect()
        {

            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online))
            {
                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
#if BRANDLAB360_INTERNAL
                    if (m_photon != null)
                    {
                        m_photon.Callback_OnRoomPropertiesChange -= RoomPropertiesChange;
                    }
#endif

                }
                else
                {
                    if (m_colyseus != null)
                    {
                        m_colyseus.Callback_OnRoomPropertiesChange -= RoomPropertiesChange;
                    }
                }
            }
        }

        private void GetUIReferences()
        {
            HUDManager.Instance.OnCustomSetupComplete -= GetUIReferences;
            m_businessCardProfiler = HUDManager.Instance.GetHUDScreenObject("USERPROFILE_SCREEN").GetComponentInChildren<UserProfile>(true);
        }

        public IEnumerator OnJoinRoom()
        {
            if (m_joinedRoom)
            {
                yield break;
            }

            //need a time out for player to be networked and player instantiated across network
            while (PlayerManager.Instance.GetLocalPlayer() == null)
            {
                yield return null;
            }

            //check if tyhe online local player is created
            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online))
            {
                if (MMOManager.Instance.IsConnected())
                {
                    if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Colyseus))
                    {
                        while(MMOManager.Instance.ColyseusManager_Ref.LocalPlayer == null)
                        {
                            yield return null;
                        }
                    }
                    else
                    {
#if BRANDLAB360_INTERNAL
                        while (MMOManager.Instance.PhotonManager_Ref.GetLocalUserID() == "")
                        {
                            yield return null;
                        }
#endif
                    }
                }
            }

            m_joinedRoom = true;

            PlayerManager.Instance.SetPlayerStatus("AVAILABLE");
            PlayerManager.Instance.SetPlayerProperty("DONOTDISTURB", "0");

            if (!CoreManager.Instance.projectSettings.npcMode.Equals(NPCMode.Disabled))
            {
                if (MMOManager.Instance.IsMasterClient() || CoreManager.Instance.projectSettings.npcMode.Equals(NPCMode.Local))
                {
                    Debug.Log("Randomising bots");

                    NPCManager.Instance.RandomiseBots();
                }
            }

            Debug.Log("Setting player status");

            if (m_localProfile != null)
            {
                m_localProfile.Set(PlayerManager.Instance.GetLocalPlayer());
            }
            
            if(MMOManager.Instance.IsConnected())
            {
                Debug.Log("Setting MMO Room properties");

                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
#if BRANDLAB360_INTERNAL 
                    if (M_PhotonRoom != null)
                    {
                        M_PhotonRoom.OnJoiedRoom();
                    }
#endif

                }
                else
                {
                    if(M_ColyseusRoom != null)
                    {
                        M_ColyseusRoom.OnJoiedRoom();
                    }
                }

                Debug.Log("Setting MMO chat");

                MMOChat.Instance.OnPlayerJoined();
            }

            if (OnRoomReady != null)
            {
                OnRoomReady.Invoke();
            }

            if (!AppManager.Instance.Data.RoomEstablished)
            {
                //settings
                AppManager.Instance.Data.RoomEstablished = true;
                AppManager.Instance.Data.RoomID = CoreManager.Instance.RoomID;

                StartupManager.Instance.ShowWelcomeScreen();

                if (CoreManager.Instance.projectSettings.showWelcomeScreen)
                {
                    float seconds = AppManager.Instance.Settings.projectSettings.welcomeScreenDuration;

                    if (AppManager.Instance.Settings.projectSettings.welcomeClip != null)
                    {
                        seconds = AppManager.Instance.Settings.projectSettings.welcomeClip.length;
                    }

                    yield return new WaitForSeconds(seconds);
                }
            }
            else
            {
                yield return new WaitForSeconds(2.0f);

                StartupManager.Instance.loadingOverlay.SetActive(false);
                BlackScreen.Instance.Show(false);
            }

            HUDManager.Instance.ShowWelcomePanel(false);
            HUDManager.Instance.Fade(FadeOutIn.FadeAction.In, null, null, 0.5f);

            if (!PlayerManager.Instance.OrbitCameraActive)
            {
                PlayerManager.Instance.GetLocalPlayer().FreezePosition = AppManager.Instance.Settings.playerSettings.freezePlayerOnStart;
                PlayerManager.Instance.GetLocalPlayer().FreezeRotation = AppManager.Instance.Settings.playerSettings.freezePlayerOnStart;
                RaycastManager.Instance.CastRay = AppManager.Instance.Settings.playerSettings.interactOnStart;
            }

            if (AppManager.Instance.Settings.projectSettings.showTutorialHint)
            {
                string message = AppManager.Instance.Data.IsMobile ? AppManager.Instance.Settings.projectSettings.tutorialMobileHintMesage : AppManager.Instance.Settings.projectSettings.tutorialHintMesage;
                AudioClip clip = AppManager.Instance.Data.IsMobile ? AppManager.Instance.Settings.projectSettings.tutorialMobileHintAudio : AppManager.Instance.Settings.projectSettings.tutorialHintAudio;

                PopupManager.instance.ShowHint(AppManager.Instance.Settings.projectSettings.tutorialHintTitle, message, AppManager.Instance.Settings.projectSettings.tutorialHintDuration, clip);
            }

            PlayerManager.Instance.SetRigistrationData();

            Debug.Log("Level Ready");

            m_roomReady = true;
            AppManager.Instance.Data.CurrentSceneReady = true;

        }

        public GameObject GetInstantiatedRoomObject(string uniqueID)
        {
            if(m_instantiatedRoomObjects.ContainsKey(uniqueID))
            {
                return m_instantiatedRoomObjects[uniqueID];
            }

            return null;
        }

        public void RoomPropertiesChange(Hashtable hash)
        {
            IPlayer player;

            foreach (DictionaryEntry item in hash)
            {

                RoomChangeWrapper wrapper = JsonUtility.FromJson<RoomChangeWrapper>(item.Value.ToString());

                if (wrapper != null)
                {
                    if (wrapper.HasData("EVENT_TYPE"))
                    {
                        RoomChangeData eventType = wrapper.GetData("EVENT_TYPE");

                        if (eventType != null)
                        {
                            Debug.Log("RoomPropertiesChange: " + eventType.data + "= " + JsonUtility.ToJson(wrapper));

                            //find event
                            switch (eventType.data)
                            {
                                case "ROOMOBJECT":
            
                                    if(wrapper.GetData("C").data.Equals("T"))
                                    {
                                        if (!m_instantiatedRoomObjects.ContainsKey(wrapper.GetData("I").data))
                                        {
                                            UnityEngine.Object prefab = Resources.Load(wrapper.GetData("O").data);

                                            if (prefab != null)
                                            {
                                                string[] array = wrapper.GetData("P").data.Replace("(", "").Replace(")", "").Split(",");
                                                Vector3 pos = new Vector3(float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2]));

                                                array = wrapper.GetData("R").data.Replace("(", "").Replace(")", "").Split(",");
                                                Vector3 rot = new Vector3(float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2]));

                                                array = wrapper.GetData("S").data.Replace("(", "").Replace(")", "").Split(",");
                                                Vector3 sca = new Vector3(float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2]));


                                                GameObject go = (GameObject)Instantiate(prefab, pos, Quaternion.Euler(rot));
                                                go.transform.position = pos;
                                                go.transform.localScale = sca;
                                                go.transform.eulerAngles = rot;
                                                go.name = wrapper.GetData("I").data;

                                                m_instantiatedRoomObjects.Add(wrapper.GetData("I").data, go);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (m_instantiatedRoomObjects.ContainsKey(wrapper.GetData("I").data))
                                        {
                                            Destroy(m_instantiatedRoomObjects[wrapper.GetData("I").data]);
                                            m_instantiatedRoomObjects.Remove(wrapper.GetData("I").data);
                                        }
                                    }

                                    break;

                                case "CHAIR":

                                    //find chair and set its occpied state
                                    if (wrapper.HasData("P"))
                                    {
                                        player = MMOManager.Instance.GetPlayerByActor(int.Parse(wrapper.GetData("P").data));

                                        //this won't wont when the player joins the room, as the player ID won't be the same
                                        if (player != null)
                                        {
                                            string chairGroupID = "";//wrapper.GetData("G").data;
                                            string chairID = wrapper.GetData("I").data;
                                            bool isOccupied = (int.Parse(wrapper.GetData("O").data) > 0) ? true : false;

                                            //if not the local player, network this chair
                                            if (!player.ID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                                            {
                                                ChairManager.Instance.NetworkChiar(player, chairGroupID, chairID, isOccupied);
                                            }
                                        }
                                    }

                                    break;
                                case "BENCH":

                                    //find chair and set its occpied state
                                    if (wrapper.HasData("P"))
                                    {
                                        player = MMOManager.Instance.GetPlayerByActor(int.Parse(wrapper.GetData("P").data));

                                        //this won't wont when the player joins the room, as the player ID won't be the same
                                        if (player != null)
                                        {
                                            string chairGroupID = "";//wrapper.GetData("G").data;
                                            string chairID = wrapper.GetData("I").data;
                                            bool isOccupied = (int.Parse(wrapper.GetData("O").data) > 0) ? true : false;

                                            //if not the local player, network this chair
                                            if (!player.ID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                                            {
                                                ChairManager.Instance.NetworkBench(player, chairGroupID, chairID, isOccupied, int.Parse(wrapper.GetData("SP").data));
                                            }
                                        }
                                    }

                                    break;
                                case "DOOR":

                                    //set door open state
                                    string doorID = wrapper.GetData("I").data;
                                    bool isOpen = (int.Parse(wrapper.GetData("O").data) > 0) ? true : false;

                                    DoorManager.Instance.NetworkDoor(doorID, isOpen);
                                    break;
                                case "LOCK":

                                    //set lock locked state
                                    string lockID = wrapper.GetData("I").data;
                                    bool isLocked = (int.Parse(wrapper.GetData("L").data) > 0) ? true : false;

                                    LockManager.Instance.NetworkLock(lockID, isLocked);
                                    break;
                                case "CONFERENCE":

                                    player = MMOManager.Instance.GetPlayerByActor(int.Parse(wrapper.GetData("O").data));

                                    bool continueCheck = true;

                                    switch (wrapper.GetData("E").data)
                                    {
                                        case "CE":
                                            ConferenceChairGroup[] allConferences = FindObjectsByType<ConferenceChairGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                                            for (int i = 0; i < allConferences.Length; i++)
                                            {
                                                if (allConferences[i].ID.Equals(wrapper.GetData("I").data))
                                                {
                                                    //need to inform the conference chair group of the file that is loaded
                                                    if (wrapper.GetData("A").data.Equals("1"))
                                                    {
                                                        allConferences[i].CurrentUploadedFile = wrapper.GetData("F").data;
                                                    }
                                                    else
                                                    {
                                                        allConferences[i].CurrentUploadedFile = "";
                                                    }
                                                }
                                            }

                                            if (player != null)
                                            {
                                                if (!player.ID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                                                {
                                                    if (wrapper.GetData("P").data.Contains(PlayerManager.Instance.GetLocalPlayer().ID))
                                                    {
                                                        ContentsManager.Instance.NetworkConferenceScreen(wrapper.GetData("I").data, wrapper.GetData("O").data, wrapper.GetData("A").data, wrapper.GetData("F").data);
                                                    }
                                                    else
                                                    {
                                                        ContentsManager.Instance.NetworkConferenceScreen(wrapper.GetData("I").data, "OTHERS", wrapper.GetData("A").data, wrapper.GetData("F").data);
                                                    }
                                                }
                                            }

                                            if (MMORoom.Instance.RoomReady) continueCheck = false;

                                            break;
                                        case "OU":
                                            ChairManager.Instance.SwitchConferenceOwner(wrapper.GetData("I").data, wrapper.GetData("O").data);

                                            if (MMORoom.Instance.RoomReady) continueCheck = false;

                                            break;
                                        default:
                                            break;
                                    }

                                    if (continueCheck)
                                    {
                                        string conferenceGroupID = wrapper.GetData("I").data;
                                        string password = wrapper.GetData("P").data;
                                        bool isClaimed = (int.Parse(wrapper.GetData("C").data) > 0) ? true : false;

                                        if (player != null)
                                        {
                                            if (!player.ID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                                            {
                                                if (isClaimed)
                                                {
                                                    ChairManager.Instance.NetworkConference(player, conferenceGroupID, password, isClaimed, null);
                                                }
                                                else
                                                {
                                                    ChairManager.Instance.NetworkConference(null, conferenceGroupID, password, isClaimed, JsonUtility.FromJson<PlayerVectorWrapper>(wrapper.GetData("V").data));
                                                }
                                            }
                                        }
                                    }

                                    break;
                                case "NPCBOTS":
                                    NPCManager.Instance.PopulateNetworkedBots(wrapper.GetData("DATA").data);
                                    break;
                                case "WORLDSCREENCONTENT":

                                    player = MMOManager.Instance.GetPlayerByActor(int.Parse(wrapper.GetData("O").data));

                                    if (player != null)
                                    {
                                        if (!player.ID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                                        {
                                            ContentsManager.Instance.NetworkWorldScreen(wrapper.GetData("I").data, wrapper.GetData("A").data, wrapper.GetData("F").data, wrapper.GetData("C").data);
                                        }
                                    }
                                    break;
                                case "CONFIGURATOR":
                                    ConfiguratorManager.instance.SetNetworkConfig(wrapper.GetData("C").data, wrapper.GetData("D").data, wrapper.GetData("T").data);
                                    break;
                                case "VEHICLE":
                                    VehicleManager.Instance.NetworkVechicle(wrapper.GetData("V").data, int.Parse(wrapper.GetData("P").data), wrapper.GetData("O").data);
                                    break;
                                case "ITEMPICKUP":
                                    ItemManager.Instance.NetworkItem(wrapper.GetData("I").data, wrapper.GetData("A").data, int.Parse(wrapper.GetData("H").data), wrapper.GetData("P").data);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Called when a player connects to room
        /// </summary>
        /// <param name="player"></param>
        public void OnPlayerEnter(string player)
        {
            StartCoroutine(ProcessOnPlayerEnter(player));
        }

        private IEnumerator ProcessOnPlayerEnter(string newPlayer)
        {
            bool foundNewPlayer = false;
            IPlayer player = null;

            while (!foundNewPlayer)
            {
                MMOPlayer[] all = FindObjectsByType<MMOPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].ID.Equals(newPlayer) && !newPlayer.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                    {
                        if(all[i].View.ViewID >= 0)
                        {
                            foundNewPlayer = true;
                            player = all[i].view.Owner;

                            //ensure the new player has my current location/rotation
                            PlayerManager.Instance.GetLocalPlayer().TransformObject.GetComponent<MMOPlayerSync>().SendCurrentPositionToPlayer(player);
                        }
                    }
                }

                yield return new WaitForEndOfFrame();
            }

            PlayerManager.Instance.OnPlayerEnteredRoom(player);

            if (OnPlayerEnteredRoom != null)
            {
                OnPlayerEnteredRoom.Invoke(player);
            }
        }

        /// <summary>
        /// Called when a player disconnects from room
        /// </summary>
        /// <param name="player"></param>
        public void OnPlayerLeft(string player)
        {
            //ensure the chairs have no reference to this player
            for (int i = 0; i < ChairManager.Instance.AllIChairObjects.Count; i++)
            {
                if (ChairManager.Instance.AllIChairObjects[i].ChairOccupied)
                {
                    //if player occupied chair, update the chair occupancy
                    if (ChairManager.Instance.AllIChairObjects[i].OccupantID.Contains(player))
                    {
                        ChairManager.Instance.AllIChairObjects[i].MainInteraface.OnPlayerDisconnect(player);
                        break;
                    }
                }
            }

            if (OnPlayerLeftRoom != null)
            {
                OnPlayerLeftRoom.Invoke(PlayerManager.Instance.GetPlayer(player));
            }
        }


        /// <summary>
        /// Action called to toggle the visibility of the local profile UI
        /// </summary>
        /// <param name="toggle"></param>
        public void ToggleLocalProfileVisibility(bool toggle)
        {
            if (m_localProfile != null)
            {
                m_localProfile.GetComponent<CanvasGroup>().alpha = (toggle) ? 1.0f : 0.0f;
                m_localProfile.GetComponent<CanvasGroup>().blocksRaycasts = toggle;
            }
        }


        /// <summary>
        /// Action called to toggle the interaction state of the local profile UI
        /// </summary>
        /// <param name="toggle"></param>
        public void ToggleLocalProfileInteraction(bool toggle)
        {
            if (m_localProfile != null)
            {
                m_localProfile.EnableActions(toggle);
            }
        }

        /// <summary>
        /// Called to open a business card user profiler
        /// </summary>
        /// <param name="player"></param>
        public void ShowPlayerProfile(IPlayer player)
        {
            if (m_businessCardProfiler != null)
            {
                m_businessCardProfiler.Set(player);
            }
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

#if UNITY_EDITOR
        [CustomEditor(typeof(MMORoom), true)]
        public class MMORoom_Editor : BaseInspectorEditor
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
