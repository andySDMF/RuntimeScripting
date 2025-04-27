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
    public class MMOManager : Singleton<MMOManager>
    {
        public static MMOManager Instance
        {
            get
            {
                return ((MMOManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        public System.Action OnNetworkProtocalCreated { get; set; }

#if BRANDLAB360_INTERNAL
        private PhotonManager m_photon;
        private GameObject m_photonGO;

        public PhotonManager PhotonManager_Ref
        {
            get
            {
                return m_photon;
            }
        }

        public GameObject PhotonManager_GO
        {
            get
            {
                return m_photonGO;
            }
        }
#endif



        private ColyseusManager m_colyseus;
        private GameObject m_colyseusGO;

        public ColyseusManager ColyseusManager_Ref
        {
            get
            {
                return m_colyseus;
            }
        }

        public GameObject ColyseusManager_GO
        {
            get
            {
                return m_colyseusGO;
            }
        }

        public List<MMOTransform> TransformSyncObjects = new List<MMOTransform>();


        private bool m_isConnected = false;

        public Dictionary<string, IPlayer> m_players = new Dictionary<string, IPlayer>();

        public System.Action<Hashtable> Callback_OnRPCRecieved;


        private void OnApplicationQuit()
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if !UNITY_EDITOR
                Disconnect();
#endif
            }
            else
            {
                Disconnect();
            }
        }

        private void Start()
        {
            if(AppManager.IsCreated)
            {
                if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Offline)) return;

                if(AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
#if BRANDLAB360_INTERNAL
                    //need to instantiate Photon prefab
                    UnityEngine.Object prefab = Resources.Load("PhotonManager");

                    if(prefab != null)
                    {
                        m_photonGO = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
                        m_photon = m_photonGO.GetComponent<PhotonManager>();
                        m_photon.SetPlayerPrefab(AppManager.Instance.Settings.playerSettings.playerController);

                        m_photon.Callback_OnConnectedToMaster += OnConnectedToMaster;
                        m_photon.Callback_OnJoinedLobby += OnJoinedLobby;
                        m_photon.Callback_OnJoinedRoom += OnJoinedRoom;
                        m_photon.Callback_OnPlayerEntered += OnPlayerEnteredRoom;
                        m_photon.Callback_OnPlayerLeft += OnPlayerLeftRoom;
                        m_photon.Callback_OnPlayerPropertiesUpdate += OnPlayerPropertiesChanged;
                        m_photon.Callback_OnRoomPropertiesUpdate += OnRoomPropertiesChanged;
                        m_photon.SendRate = AppManager.Instance.Settings.projectSettings.sendRate;
                        m_photon.SerializationRate = AppManager.Instance.Settings.projectSettings.serializationRate;
                        //RPC handler
                        m_photon.Callback_OnRPCRecieved += OnRPCRecieved;
                    }
#endif

                }
                else
                {
                    //need to instantiate Colyseus Prefab
                    UnityEngine.Object prefab = Resources.Load("ColyseusManager");

                    if (prefab != null)
                    {
                        m_colyseusGO = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
                        m_colyseus = m_colyseusGO.GetComponent<ColyseusManager>();

                        m_colyseus.SetPlayerPrefab(AppManager.Instance.Settings.playerSettings.playerController);

                        m_colyseus.Callback_OnConnectedToMaster += OnConnectedToMaster;
                        m_colyseus.Callback_OnJoinedLobby += OnJoinedLobby;
                        m_colyseus.Callback_OnJoinedRoom += OnJoinedRoom;
                        m_colyseus.Callback_OnPlayerEntered += OnPlayerEnteredRoom;
                        m_colyseus.Callback_OnPlayerLeft += OnPlayerLeftRoom;
                        m_colyseus.Callback_OnPlayerPropertiesUpdate += OnPlayerPropertiesChanged;
                        m_colyseus.Callback_OnRoomPropertiesUpdate += OnRoomPropertiesChanged;
                        m_colyseus.SendRate = AppManager.Instance.Settings.projectSettings.sendRate;
                        m_colyseus.SerializationRate = AppManager.Instance.Settings.projectSettings.serializationRate;

                        //RPC handler
                        m_colyseus.Callback_OnRPCRecieved += OnRPCRecieved;
                    }
                }
            }

            if (OnNetworkProtocalCreated != null)
            {
                OnNetworkProtocalCreated.Invoke();
            }
        }

        /// <summary>
        /// Connect to Sevrver
        /// </summary>
        public void Connect()
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if(m_photon != null)
                {
                    m_photon.Connect();
                }
#endif

            }
            else
            {
               if(m_colyseus != null)
                {
                    m_colyseus.Connect();
                }
            }
        }

        /// <summary>
        /// Disconnects from Sevrver
        /// </summary>
        public void Disconnect()
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (m_photon != null)
                {
                    m_photon.Disconnect();
                }
#endif

            }
            else
            {
                if(m_colyseus != null)
                {
                    m_colyseus.Disconnect();
                }
            }

            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL

                if (m_photon)
                {
                    m_photon.Callback_OnConnectedToMaster -= OnConnectedToMaster;
                    m_photon.Callback_OnJoinedLobby -= OnJoinedLobby;
                    m_photon.Callback_OnJoinedRoom -= OnJoinedRoom;
                    m_photon.Callback_OnPlayerEntered -= OnPlayerEnteredRoom;
                    m_photon.Callback_OnPlayerLeft -= OnPlayerLeftRoom;
                    m_photon.Callback_OnPlayerPropertiesUpdate -= OnPlayerPropertiesChanged;
                    m_photon.Callback_OnRoomPropertiesUpdate -= OnRoomPropertiesChanged;
                    //RPC handler
                    m_photon.Callback_OnRPCRecieved -= OnRPCRecieved;
                }
#endif

            }
            else
            {
                //need to instantiate Colyseus Prefab
                UnityEngine.Object prefab = Resources.Load("ColyseusManager");

                if (prefab != null)
                {
                    m_colyseusGO = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
                    m_colyseus = m_colyseusGO.GetComponent<ColyseusManager>();

                    m_colyseus.SetPlayerPrefab(AppManager.Instance.Settings.playerSettings.playerController);

                    m_colyseus.Callback_OnConnectedToMaster -= OnConnectedToMaster;
                    m_colyseus.Callback_OnJoinedLobby -= OnJoinedLobby;
                    m_colyseus.Callback_OnJoinedRoom -= OnJoinedRoom;
                    m_colyseus.Callback_OnPlayerEntered -= OnPlayerEnteredRoom;
                    m_colyseus.Callback_OnPlayerLeft -= OnPlayerLeftRoom;
                    m_colyseus.Callback_OnPlayerPropertiesUpdate -= OnPlayerPropertiesChanged;
                    m_colyseus.Callback_OnRoomPropertiesUpdate -= OnRoomPropertiesChanged;

                    //RPC handler
                    m_colyseus.Callback_OnRPCRecieved -= OnRPCRecieved;
                }
            }

            MMORoom.Instance.Disconnect();
            MMOChat.Instance.Disconnect();
        }

        /// <summary>
        /// Join MMO room
        /// </summary>
        public void JoinRoomID(string id)
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (m_photon != null)
                {
                    m_photon.JoinRoomID(id);
                }
#endif

            }
            else
            {
                if (m_colyseus != null)
                {
                    m_colyseus.JoinRoomID(id);
                }
            }
        }


        /// <summary>
        /// Join MMO room
        /// </summary>
        public void JoinRoom(string room)
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (m_photon != null)
                {
                    m_photon.JoinRoom(room, AppManager.Instance.Settings.projectSettings.maxPlayers);
                }
#endif

            }
            else
            {
                if(m_colyseus != null)
                {
                    m_colyseus.JoinRoom(room, AppManager.Instance.Settings.projectSettings.maxPlayers);
                }
            }
        }

        /// <summary>
        /// Create MMO room
        /// </summary>
        public void CreateRoom(string room)
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (m_photon != null)
                {
                    m_photon.CreateRoom(room, AppManager.Instance.Settings.projectSettings.maxPlayers);
                }
#endif

            }
            else
            {
                if (m_colyseus != null)
                {
                    m_colyseus.CreateRoom(room, AppManager.Instance.Settings.projectSettings.maxPlayers);
                }
            }
        }

        /// <summary>
        /// Callback joined Photon room
        /// </summary>
        public void OnJoinedRoom()
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (m_photon != null)
                {
                    //tell room to add player name
                    if (AppManager.Instance.Settings.projectSettings.checkDuplicatePhotonNames)
                    {
                        m_photon.UpdateRoomNames();
                    }
                }
#endif

            }
            else
            {

            }

            m_isConnected = true;

            //set up player
            PlayerManager.Instance.SpawnLocalPlayer(true);

            //add local player to player list
            m_players.Add(PlayerManager.Instance.GetLocalPlayer().ID, PlayerManager.Instance.GetLocalPlayer());

            //set room properties
            StartCoroutine(MMORoom.Instance.OnJoinRoom());

            //process all existing players
            StartCoroutine(ProcessExistingPlayers());
        }

        /// <summary>
        /// Join the lobby
        /// </summary>
        public void JoinLobby()
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (m_photon != null)
                {
                    m_photon.JoinLobby();
                }
#endif

            }
            else
            {
                if(m_colyseus != null)
                {
                    m_colyseus.JoinLobby();
                }
            }
        }

        /// <summary>
        /// Set Photon username
        /// </summary>
        /// <param name="UserName"></param>
        public void SetUsername(string UserName)
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (m_photon != null)
                {
                    m_photon.SetUsername(UserName);
                }
#endif

            }
            else
            {
                if (m_colyseus != null && PlayerManager.Instance.GetLocalPlayer() != null)
                {
                    m_colyseus.SetUsername(PlayerManager.Instance.GetLocalPlayer().ID, UserName);
                }
            }
        }

        /// <summary>
        /// Called when connected to master sever
        /// </summary>
        public  void OnConnectedToMaster()
        {
            Debug.Log("Connected to Master Server");
            CoreManager.Instance.OnConnected();
        }

        /// <summary>
        /// Called when joined lobby
        /// </summary>
        public void OnJoinedLobby()
        {
            Debug.Log("Joined Lobby");
            StartupManager.Instance.OnJoinedLobby();
        }


        /// <summary>
        /// Callback MMO create room fail
        /// </summary>
        public void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.Log("Error: create MMO room failed: " + message);
        }

        /// <summary>
        /// Callback MMO join room fail
        /// </summary>
        public void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.Log("Error: join MMO room failed: " + message);
        }

        /// <summary>
        /// Callback MMO disconnect
        /// </summary>
        public void OnDisconnected(string cause)
        {
            Debug.Log("Disconnected from MMO Server: " + cause);
        }

        /// <summary>
        /// Callback MMO player entered room
        /// </summary>
        public void OnPlayerEnteredRoom(string newPlayer)
        {
            Debug.Log("Player entered MMO room: " + newPlayer);

            StartCoroutine(ProcessNewPlayer(newPlayer));
        }

        private IEnumerator ProcessNewPlayer(string newPlayer)
        {
            Debug.Log("Processing new player: " + newPlayer);

            yield return new WaitForEndOfFrame();

            bool foundNewPlayer = false;
            IPlayer player = null;

            while(!foundNewPlayer)
            {
                MMOPlayer[] all = FindObjectsByType<MMOPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].ID.Equals(newPlayer) && !newPlayer.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                    {
                        if(all[i].View.ViewID >= 0)
                        {
                            if(all[i].view.Owner != null)
                            {
                                foundNewPlayer = true;
                                player = all[i].view.Owner;
                            }
                        }
                    }
                }

                yield return new WaitForEndOfFrame();
            }

            if(player != null)
            {
                if (!m_players.ContainsKey(newPlayer))
                {
                    m_players.Add(newPlayer, player);
                }
            }

            MMORoom.Instance.OnPlayerEnter(newPlayer);
        }

        private IEnumerator ProcessExistingPlayers()
        {
            Debug.Log("Processing existing players");

            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (m_photon != null)
                {
                    while (m_players.Count != m_photon.GetAllPlayerCount())
                    {
                        MMOPlayer[] all = FindObjectsByType<MMOPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                        for (int i = 0; i < all.Length; i++)
                        {
                            if (!all[i].ID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                            {
                                if(!m_players.ContainsKey(all[i].ID))
                                {
                                    m_players.Add(all[i].ID, all[i].view.Owner);
                                }
                            }
                        }

                        yield return new WaitForSeconds(1.0f);
                    }
                }
#endif

            }
            else
            {
                if (m_colyseus != null)
                {

                }
            }

            yield return null;

            Debug.Log("All existing players found");
        }

        /// <summary>
        /// Callback MMO player left room
        /// </summary>
        public void OnPlayerLeftRoom(string otherPlayer)
        {
            MMORoom.Instance.OnPlayerLeft(otherPlayer);

            Debug.Log("Player left MMO room: " + otherPlayer);

            MMOPlayer[] all = FindObjectsByType<MMOPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].ID.Equals(otherPlayer))
                {
                    m_players.Remove(all[i].ID);
                    break;
                }
            }
        }

        /// <summary>
        /// Callback MMO player left room
        /// </summary>
        public void OnPlayerPropertiesChanged(string otherPlayer, Hashtable hash)
        {
            StartCoroutine(ProcessPlayerPropertiesChange(otherPlayer, hash));
        }

        private IEnumerator ProcessPlayerPropertiesChange(string otherPlayer, Hashtable hash)
        {
            IPlayer player = GetPlayerByUserID(otherPlayer);
            bool playerFound = false;

            if (player != null && player.ActorNumber >= 0)
            {
                playerFound = true;
            }

            if(!playerFound)
            {
                while (!playerFound)
                {
                    player = GetPlayerByUserID(otherPlayer);

                    if (player != null && player.ActorNumber >= 0)
                    {
                        playerFound = true;
                    }

                    yield return new WaitForEndOfFrame();
                }

            }

            if (playerFound)
            {
                Debug.Log("player [" + player.ID + "] changed properties");

                //set player customization data
                PlayerManager.Instance.NetworkPlayerCustomizationData(player, hash);

                //Avatar
                AvatarManager.Instance.CustomiseNetworkPlayer(player, hash);
            }
        }

        /// <summary>
        /// Callback MMO room props changed
        /// </summary>
        public void OnRoomPropertiesChanged(Hashtable hash)
        {
            MMORoom.Instance.RoomPropertiesChange(hash);
        }

        /// <summary>
        /// Spawn MMMO network prefab
        /// </summary>
        /// <param name="spawnPosition">position to spawn</param>
        /// <param name="prefabName">name of the prefab (Within resources folder)</param>
        /// <returns></returns>
        public GameObject InstantiatePlayer(Vector3 spawnPosition, Quaternion spawnRotation, string playerPrafb)
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (m_photon != null)
                {
                    GameObject go = m_photon.Instantiate(spawnPosition, spawnRotation, "PhotonPlayer");
                    return go.GetComponent<PhotonPlayer>().RegisterLocalPlayer(spawnPosition, spawnRotation, playerPrafb);
                }
#endif

            }
            else
            {
                if(m_colyseus != null)
                {
                    GameObject go = m_colyseus.Instantiate(spawnPosition, spawnRotation, "ColyseusPlayer", true);
                    return go.GetComponent<ColyseusPlayer>().RegisterLocalPlayer(spawnPosition, spawnRotation, playerPrafb);
                }
            }

            return null;
        }

        /// <summary>
        /// Send RPC wrapper
        /// Valid Params are; float, int32, string, string[], boolean, Vector3, Vector2
        /// </summary>
        public void SendRPC(string methodName, int RPCTarget, params object[] parameters)
        {
            if (!IsConnected()) return;

            MMORPC.RPCMethod method = MMORPC.RPCMethods.FirstOrDefault(x => x.method.Equals(methodName));

            if (method == null)
            {
                Debug.Log("RPC Method [" + methodName + "] does not exists. Cannot send RPC!");
                return;
            }

            Dictionary<string, string> temp = new Dictionary<string, string>();
            List<string> paramNames = method.parameters;

            if(paramNames.Count == parameters.Length)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    temp.Add(paramNames[i], ConvertRPCParamToString(parameters[i]));
                }

                string dataParam = JsonUtility.ToJson(GetRPCWrapper(methodName, temp));

                PlayerManager.Instance.GetLocalPlayer().MainObject.GetComponent<MMOPlayer>().view.RPC(methodName, RPCTarget, dataParam);

            }
        }

        /// <summary>
        /// Send RPC wrapper
        ///  /// Valid Params are; float, int32, string, string[], boolean, Vector3, Vector2
        /// </summary>
        public void SendRPC(string methodName, IPlayer targetPlayer, params object[] parameters)
        {
            if (!IsConnected()) return;

            MMORPC.RPCMethod method = MMORPC.RPCMethods.FirstOrDefault(x => x.method.Equals(methodName));

            if(method == null)
            {
                Debug.Log("RPC Method [" + methodName + "] does not exists. Cannot send RPC!");
                return;
            }

            Dictionary<string, string> temp = new Dictionary<string, string>();
            List<string> paramNames = method.parameters;

            for (int i = 0; i < parameters.Length; i++)
            {
                temp.Add(paramNames[i], ConvertRPCParamToString(parameters[i]));
            }

            string dataParam = JsonUtility.ToJson(GetRPCWrapper(methodName, temp));

            PlayerManager.Instance.GetLocalPlayer().MainObject.GetComponent<MMOPlayer>().view.RPC(methodName, targetPlayer, dataParam);
        }

        private string ConvertRPCParamToString(object obj)
        {
            string temp = obj.GetType().ToString() + "?";

            if (obj is string)
            {
                temp += (string)obj;
            }
            else if (obj is float)
            {
                temp += ((float)obj).ToString();
            }
            else if (obj is int)
            {
                temp += ((int)obj).ToString();
            }
            else if (obj is bool)
            {
                temp += ((bool)obj).ToString();
            }
            else if (obj is Vector3)
            {
                temp += ((Vector3)obj).ToString();
            }
            else if (obj is Vector2)
            {
                temp += ((Vector2)obj).ToString();
            }
            else if (obj is string[])
            {
                for (int i = 0; i < ((string[])obj).Length; i++)
                {
                    temp += ((string[])obj)[i] + ((i < ((string[])obj).Length - 1) ? "$" : "");
                }
            }
            else
            {
                Debug.Log("Object type [" + obj.GetType().ToString() + "] is not supported for RPC!");
            }

            return temp;
        }

        private RoomChangeWrapper GetRPCWrapper(string objectID, Dictionary<string, string> data)
        {
            RoomChangeWrapper wrapper = new RoomChangeWrapper();
            wrapper.id = objectID;
            wrapper.data = new List<RoomChangeData>();

            //add data to wrapper.data
            foreach (KeyValuePair<string, string> prop in data)
            {
                wrapper.Add(prop.Key, prop.Value);
            }

            return wrapper;
        }

        public void InstantiateRoomObject(string prefab, string uniqueID, Vector3 pos, Vector3 rot, Vector3 scale)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("EVENT_TYPE", "ROOMOBJECT");
            data.Add("O", prefab);
            data.Add("I", uniqueID);
            data.Add("P", pos.ToString());
            data.Add("R", rot.ToString());
            data.Add("S", scale.ToString());
            data.Add("C", "T");

            ChangeRoomProperty(uniqueID, data);
        }

        public void DestoryRoomObject(string uniqueID)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("EVENT_TYPE", "ROOMOBJECT");
            data.Add("I", uniqueID);
            data.Add("C", "F");

            ChangeRoomProperty(uniqueID, data);
        }

        /// <summary>
        /// Returns if the MMO is connected
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            return m_isConnected;
        }

        /// <summary>
        /// Return if we are the master client
        /// </summary>
        public List<IPlayer> GetAllPlayers()
        {
            if (m_isConnected)
            {
                List<IPlayer> temp = new List<IPlayer>();

                foreach (KeyValuePair<string, IPlayer> p in m_players)
                {
                    temp.Add(p.Value);
                }

                return temp;
            }

            return new List<IPlayer>();
        }

        /// <summary>
        /// Return if we are the master client
        /// </summary>
        public IPlayer GetPlayerByUserID(string userID)
        {
            if(m_isConnected)
            {
                var playerList = GetAllPlayers();

                for (int i = 0; i < playerList.Count; i++)
                {
                    if (userID == playerList[i].ID)
                    {
                        return playerList[i];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Return if we are the master client
        /// </summary>
        public IPlayer GetPlayerByActor(int actor)
        {
            if (m_isConnected)
            {
                var playerList = GetAllPlayers();

                for (int i = 0; i < playerList.Count; i++)
                {
                    if (actor == playerList[i].ActorNumber)
                    {
                        return playerList[i];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Return if we are the master client
        /// </summary>
        public bool IsMasterClient()
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (m_photon != null)
                {
                    return m_photon.isMasterClient();
                }
#endif
            }
            else
            {
                if (m_colyseus != null)
                {
                    return m_colyseus.IsMasterClient();
                }
            }

            return true;
        }

        public string GetMasterClientID()
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (m_photon != null)
                {
                    return m_photon.GetMasterClientID();
                }
#endif
            }
            else
            {
                if (m_colyseus != null)
                {
                    return m_colyseus.GetMasterClientID();
                }
            }

            return "";
        }


        /// <summary>
        /// Returns if a player is busy
        /// </summary>
        /// <param name="playerID"></param>
        /// <returns></returns>
        public bool IsPlayerBusy(string playerID)
        {

            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL

                if (m_photon != null)
                {
                    return m_photon.IsPlayerBusy(playerID);
                }
#endif

            }
            else
            {
                if (m_colyseus != null)
                {
                    return m_colyseus.IsPlayerBusy(playerID);
                }
            }


            return false;
        }

        public bool PlayerHasProperty(IPlayer player, string prop)
        {
            Hashtable props = GetPlayerProperties(player);

            foreach(DictionaryEntry e in props)
            {
                if (e.Key.ToString().Equals(prop))
                {
                    return true;
                }
            }

            return false;
        }

        public string GetPlayerProperty(IPlayer player, string prop)
        {
            if (PlayerHasProperty(player, prop))
            {
                Hashtable props = GetPlayerProperties(player);

                foreach (DictionaryEntry e in props)
                {
                    if (e.Key.ToString().Equals(prop))
                    {
                        return e.Value.ToString();
                    }
                }
            }

            return "";
        }

        /// <summary>
        /// Gets the local players properties
        /// </summary>
        /// <returns></returns>
        public Hashtable GetLocalPlayerProperties()
        {
            Hashtable temp = new Hashtable();

            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL

                if (m_photon != null)
                {
                    temp = m_photon.GetLocalPlayerProperties();
                }
#endif

            }
            else
            {
                if (m_colyseus != null)
                {
                    temp = m_colyseus.GetLocalPlayerProperties();
                }
            }

            return temp;
        }

        /// <summary>
        /// Gets the local players properties
        /// </summary>
        /// <returns></returns>
        public Hashtable GetPlayerProperties(IPlayer player)
        {
            Hashtable temp = new Hashtable();

            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL

                if (m_photon != null)
                {
                    temp = m_photon.GetPlayerProperties(player.ID);
                }
#endif

            }
            else
            {
                if (m_colyseus != null)
                {
                    temp = m_colyseus.GetPlayerProperties(player.ID);
                }
            }

            return temp;
        }

        /// <summary>
        /// Sets the local player properties
        /// </summary>
        /// <param name="hash"></param>
        public void SetPlayerProperties(Hashtable hash)
        {

            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL

                if (m_photon != null)
                {
                    m_photon.SetPlayerProperties(hash);
                }
#endif

            }
            else
            {
                if (m_colyseus != null)
                {
                    m_colyseus.SetPlayerProperties(PlayerManager.Instance.GetLocalPlayer().ID, hash);
                }
            }
        }


        /// <summary>
        /// Set room property acrocss the network
        /// </summary>
        /// <param name="objectID"></param>
        /// <param name="data"></param>
        public void ChangeRoomProperty(string objectID, Dictionary<string, string> data)
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL

                if (m_photon != null)
                {
                    m_photon.ChangeRoomProperty(objectID, data);
                }
#endif

            }
            else
            {
                if(m_colyseus != null)
                {
                    m_colyseus.ChangeRoomProperty(objectID, data);
                }
            }
        }

        /// <summary>
        /// Set room properties acrocss the network
        /// </summary>
        /// <param name="data"></param>
        public void ChangeRoomProperties(Dictionary<string, Dictionary<string, string>> data)
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL

                if (m_photon != null)
                {
                    m_photon.ChangeRoomProperties(data);
                }
#endif

            }
            else
            {
                if (m_colyseus != null)
                {
                    m_colyseus.ChangeRoomProperties(data);
                }
            }
        }

        /// <summary>
        /// Called to handle any RPC
        /// </summary>
        /// <param name="data"></param>
        public void OnRPCRecieved(Hashtable data)
        {
            MMORPC.OnRPCRecieved(data);

            if(Callback_OnRPCRecieved != null)
            {
                Callback_OnRPCRecieved.Invoke(data);
            }
        }

        public enum RpcTarget
        {
            /// <summary>Sends the RPC to everyone else and executes it immediately on this client. Player who join later will not execute this RPC.</summary>
            All,

            /// <summary>Sends the RPC to everyone else. This client does not execute the RPC. Player who join later will not execute this RPC.</summary>
            Others,

            /// <summary>Sends the RPC to MasterClient only. Careful: The MasterClient might disconnect before it executes the RPC and that might cause dropped RPCs.</summary>
            MasterClient,

            /// <summary>Sends the RPC to everyone else and executes it immediately on this client. New players get the RPC when they join as it's buffered (until this client leaves).</summary>
            AllBuffered,

            /// <summary>Sends the RPC to everyone. This client does not execute the RPC. New players get the RPC when they join as it's buffered (until this client leaves).</summary>
            OthersBuffered,

            /// <summary>Sends the RPC to everyone (including this client) through the server.</summary>
            /// <remarks>
            /// This client executes the RPC like any other when it received it from the server.
            /// Benefit: The server's order of sending the RPCs is the same on all clients.
            /// </remarks>
            AllViaServer,

            /// <summary>Sends the RPC to everyone (including this client) through the server and buffers it for players joining later.</summary>
            /// <remarks>
            /// This client executes the RPC like any other when it received it from the server.
            /// Benefit: The server's order of sending the RPCs is the same on all clients.
            /// </remarks>
            AllBufferedViaServer
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
        [CustomEditor(typeof(MMOManager), true)]
        public class MMOManager_Editor : BaseInspectorEditor
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