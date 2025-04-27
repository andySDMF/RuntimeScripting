using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class NPCManager : Singleton<NPCManager>, IRaycaster
    {
        public static NPCManager Instance
        {
            get
            {
                return ((NPCManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Interaction")]
        private float interactionDistance = 5;
        private bool useLocalDistance = true;

        public bool OverrideDistance { get { return useLocalDistance; } }

        private List<Transform> spawnPoints = new List<Transform>();
        private List<NPCBotPrefab> botAvatars = new List<NPCBotPrefab>();
        private int numberOfBots = 0;

        private NPCSettings m_settings;

        private bool m_initialised = false;
        private List<Bot> m_botsCreated = new List<Bot>();
        private List<Transform> m_spawnCache = new List<Transform>();

        private bool m_networked = false;
        private NPCBot hitBot;
        private int botID = 0;

        public string UserCheckKey
        {
            get
            {
                return m_userKey;
            }
        }

        public bool OverrideBotClothingColors
        {
            get;
            private set;
        }

        public float MaxStationaryTime
        {
            get;
            private set;
        }

        public float MaxWalkingDistance
        {
            get;
            private set;
        }

        private string m_userKey = "USERTYPE";


        public bool IsReady
        {
            get
            {
                return m_initialised;
            }
        }

        public float Distance
        {
            get
            {
                float distance = 5000;

                //define camera to use
                if (!MapManager.Instance.TopDownViewActive)
                {
                    if (PlayerManager.Instance.ThirdPersonCameraActive)
                    {
                        distance = interactionDistance + Vector3.Distance(PlayerManager.Instance.GetLocalPlayer().TransformObject.position, PlayerManager.Instance.GetLocalPlayer().MainCamera.transform.GetChild(0).position);
                    }
                    else
                    {
                        distance = interactionDistance;
                    }
                }
                else
                {
                    //cannot perform chair stuff in topdown view
                    return -1;
                }

                return distance;
            }
        }

        public void RaycastHit(RaycastHit hit, out Transform hitObject)
        {
            hitBot = hit.transform.GetComponent<NPCBot>();

            if (hitBot)
            {
                hitObject = hitBot.transform;
            }
            else
            {
                hitObject = null;
            }

            if (InputManager.Instance.GetMouseButtonUp(0))
            {
                if(hitBot)
                {
                    hitBot.OnClick();
                }
            }
        }

        public void RaycastMiss()
        {
            hitBot = null;
        }

        private void Awake()
        {
            RaycastManager.OnPointerOutsideOfViewport += RaycastMiss;
            RaycastManager.OnPointerOverUI += RaycastMiss;
            RaycastManager.Instance.Raycasters.Add(this);

            if (m_userKey != null)
            {

            }
        }

        private void OnDestroy()
        {
            RaycastManager.OnPointerOutsideOfViewport -= RaycastMiss;
            RaycastManager.OnPointerOverUI -= RaycastMiss;
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if (CoreManager.Instance.projectSettings.npcMode.Equals(NPCMode.Disabled))
            {
                Debug.Log("NPC Mode Setting Disbaled. Disbaling NPCManager [" + gameObject.scene.name + "]");
                return;
            }

            PlayerControlSettings.ManagerInteraction mInteration = CoreManager.Instance.playerSettings.GetIRaycasterManager(gameObject.name);

            if (mInteration != null)
            {
                interactionDistance = mInteration.interactionDistance;
                useLocalDistance = mInteration.overrideInteraction;
            }
            else
            {
                useLocalDistance = false;
            }

            m_settings = CoreManager.Instance.NPCSettings;

            NPCScene npcScene = m_settings.GetScene(gameObject.scene.name);
            botAvatars.Clear();

            if (npcScene != null)
            {
                //numberOfBots = npcScene.numberOfBots;
                OverrideBotClothingColors = npcScene.useAvatarClothingColors;
                MaxStationaryTime = npcScene.stationaryMaxTime;
                MaxWalkingDistance = npcScene.maxWalkingDistance;

                for (int i = 0; i < npcScene.botPrefabs.Count; i++)
                {
                    numberOfBots += npcScene.botPrefabs[i].quantity;
                    botAvatars.Add(npcScene.botPrefabs[i]);
                }
            }
            else
            {
                Debug.Log("NPC Scene could not be found. Disbaling NPCManager [" + gameObject.scene.name + "]");
                return;
            }

            NPCBotSpawnArea[] all = FindObjectsByType<NPCBotSpawnArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                spawnPoints.Add(all[i].transform);
            }
        }

        private void Update()
        {
            if(m_initialised)
            {
                //offline & masterclient control
                if(!m_networked || MMOManager.Instance.IsMasterClient() || CoreManager.Instance.projectSettings.npcMode.Equals(NPCMode.Local))
                {
                    //Process all bots behaviour
                    for (int i = 0; i < m_botsCreated.Count; i++)
                    {
                        if(m_botsCreated[i].behaviour.State.Equals(BotSate.Interrupted))
                        {
                            continue;
                        }
                        else if (m_botsCreated[i].behaviour.State.Equals(BotSate.Reset))
                        {
                            if(m_botsCreated[i].behaviour.HasReachedDestination)
                            {
                                m_botsCreated[i].behaviour.Stop();
                            }
                        }
                        else if (m_botsCreated[i].behaviour.State.Equals(BotSate.Ready))
                        {
                            Vector3 randomPoint;

                            if (m_botsCreated[i].walkableArea.Length > 0)
                            {
                                randomPoint = m_botsCreated[i].GetPoint();
                                m_botsCreated[i].behaviour.Walk(randomPoint);
                            }
                            else
                            {
                                randomPoint = GetRandomGameBoardLocation();

                                if (CheckDestination(randomPoint))
                                {
                                    m_botsCreated[i].behaviour.Walk(GetRandomGameBoardLocation());
                                }
                            }
                        }
                        else if (m_botsCreated[i].behaviour.State.Equals(BotSate.Walk))
                        {
                            //m_botsCreated[i].behaviour.AvoidPlayer();

                            if (m_botsCreated[i].behaviour.HasReachedDestination)
                            {
                                m_botsCreated[i].behaviour.Stop();
                            }
                            else if (m_botsCreated[i].behaviour.EncounteredObstacle)
                            {
                                //Find new destination if bot was inturrupted by obstical on NavMesh
                                //m_botsCreated[i].behaviour.Walk(GetRandomGameBoardLocation());
                                 m_botsCreated[i].behaviour.Stop();
                            }
                        }
                        else
                        {
                            if (m_botsCreated[i].behaviour.Wait())
                            {
                                //while wait they could do something
                            }
                            else
                            {

                            }
                        }

                        //ensure bot avatar remains centrol
                        m_botsCreated[i].behaviour.Avatar.localEulerAngles = m_botsCreated[i].rotation;
                        m_botsCreated[i].behaviour.Avatar.localPosition = m_botsCreated[i].position;
                    }
                }
                else
                {
                    //ensure bot avatar remains centrol
                    for (int i = 0; i < m_botsCreated.Count; i++)
                    {
                        m_botsCreated[i].behaviour.Avatar.localEulerAngles = m_botsCreated[i].rotation;
                        m_botsCreated[i].behaviour.Avatar.localPosition = m_botsCreated[i].position;
                    }
                }
            }
        }

        public bool IsInstantiatedBot(GameObject go)
        {
            for(int i = 0; i < m_botsCreated.Count; i++)
            {
                if(m_botsCreated[i].bot.gameObject.Equals(go))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Action to call to randimise bots on the nav mesh
        /// </summary>
        public void RandomiseBots()
        {
            if (m_initialised) return;

            if (botAvatars.Count <= 0) return;

            //get network state
            m_networked = AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online);

            //offline && masterclient
            if (MMOManager.Instance.IsMasterClient() || !m_networked || CoreManager.Instance.projectSettings.npcMode.Equals(NPCMode.Local))
            {
                //wrapper for bot creation json
                BotWrapper wrapper = new BotWrapper();
                Vector3 randomBoardLocation;
                Collider[] walkableArea = null;

                for (int i = 0; i < spawnPoints.Count; i++)
                {
                    int count = spawnPoints[i].GetComponent<NPCBotSpawnArea>().BotCount;

                    for(int j = 0; j < count; j++)
                    {
                        walkableArea = spawnPoints[i].GetComponent<NPCBotSpawnArea>().WalkableArea;
                        NPCBotSpawnArea spawnArea = spawnPoints[i].GetComponent<NPCBotSpawnArea>();
                        randomBoardLocation = spawnArea.GetPoint();

                        // spawn a random bot prefab at that location
                        int rand = Random.Range(0, botAvatars.Count);
                        GameObject spawnedBot = SpawnBotAtLocation(randomBoardLocation, botAvatars[rand], wrapper, botID, spawnArea.ID);

                        Bot bot = new Bot(spawnedBot.transform, walkableArea);
                        m_botsCreated.Add(bot);
                        bot.behaviour.Agent.SetDestination(spawnedBot.transform.position);
                        botID++;
                    }
                }

                for (int i = 0; i < numberOfBots; i++)
                {
                    // get a game location on the board
                    walkableArea = new Collider[0];
                    randomBoardLocation = GetRandomGameBoardLocation();

                    // spawn a random bot prefab at that location
                    int rand = Random.Range(0, botAvatars.Count);
                    GameObject spawnedBot = SpawnBotAtLocation(randomBoardLocation, botAvatars[rand], wrapper, botID, "");

                    Bot bot = new Bot(spawnedBot.transform, walkableArea);
                    m_botsCreated.Add(bot);
                    bot.behaviour.Agent.SetDestination(spawnedBot.transform.position);
                    botID++;
                }

                InitialiseFixedBots(wrapper);

                if (CoreManager.Instance.projectSettings.npcMode.Equals(NPCMode.Network))
                {
                    //post to room changes
                    Dictionary<string, string> data = new Dictionary<string, string>();
                    data.Add("EVENT_TYPE", "NPCBOTS");
                    data.Add("DATA", JsonUtility.ToJson(wrapper));

                    MMOManager.Instance.ChangeRoomProperty("NPCBOTS", data);
                }
            }

            m_initialised = true;
        }

        /// <summary>
        /// Called to initialise all non created bots
        /// </summary>
        private void InitialiseFixedBots(BotWrapper wrapper)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                NPCBotSpawnArea spawnArea = spawnPoints[i].GetComponent<NPCBotSpawnArea>();

                for (int j = 0; j < spawnArea.FixedBots.Count; j++)
                {
                    for(int k = 0; k < spawnArea.FixedBots[j].quantity; k++)
                    {
                        Collider[] walkableArea = spawnArea.WalkableArea;
                        Vector3 randomBoardLocation = spawnArea.GetPoint();

                        // spawn a bot prefab at that location
                        GameObject spawnedBot = SpawnBotAtLocation(randomBoardLocation, spawnArea.FixedBots[j], wrapper, botID, spawnArea.ID);

                        Bot bot = new Bot(spawnedBot.transform, walkableArea);
                        m_botsCreated.Add(bot);
                        bot.behaviour.Agent.SetDestination(spawnedBot.transform.position);
                        botID++;
                    }
                }
            }
        }

        /// <summary>
        /// Action called to populate the networked bots on any client is is not the masterclient
        /// </summary>
        /// <param name="rawData"></param>
        public void PopulateNetworkedBots(string rawData)
        {
            //non master client players
            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online) && !MMOManager.Instance.IsMasterClient())
            {
                BotWrapper wrapper = JsonUtility.FromJson<BotWrapper>(rawData);

                if (wrapper != null)
                {
                    foreach (BotJson bot in wrapper.bots)
                    {
                      //spawn all bots in the room data
                        GameObject spawnedBot = SpawnBotAtLocation(bot.Get(), JsonUtility.FromJson<NPCBotPrefab>(bot.prefab), null, int.Parse(bot.id), bot.area);
                        ICustomAvatar botAvatar = spawnedBot.GetComponentInChildren<ICustomAvatar>();

                        if(botAvatar != null)
                        {
                            botAvatar.SetProperties(bot.GetData());
                        }
                       
                        Collider[] walkableArea = null;

                        if(!string.IsNullOrEmpty(bot.area))
                        {
                            if(spawnPoints.Count > 0)
                            {
                                Transform sArea = spawnPoints.FirstOrDefault(x => x.GetComponent<NPCBotSpawnArea>().ID.Equals(bot.area));

                                if(sArea != null)
                                {
                                    walkableArea = sArea.GetComponent<NPCBotSpawnArea>().WalkableArea;
                                }
                            }
                        }

                        Bot nbot = new Bot(spawnedBot.transform, walkableArea);
                        m_botsCreated.Add(nbot);
                    }
                }
            }

            m_initialised = true;
            m_networked = true;
        }

        /// <summary>
        /// Action called on all non masterclients to sync bots
        /// </summary>
        /// <param name="id"></param>
        /// <param name="state"></param>
        /// <param name="data"></param>
        public void SyncBot(string id, string state, string data)
        {
            if(!MMOManager.Instance.IsMasterClient())
            {
                Bot bot = m_botsCreated.FirstOrDefault(x => x.bot.name.Equals(id));

                if (bot != null)
                {
                    SyncBotPath path = JsonUtility.FromJson<SyncBotPath>(data);
                    bot.behaviour.SyncBot(state, path);
                }
            }
        }

        /// <summary>
        /// Spawns a random bot at the given spawn point.
        /// </summary>
        /// <param name="spawnPosition">The position to spawn the person at.</param>
        /// <param name="objectRandomizer">ObjectRandomizer that spawns people.</param>
        /// <returns>The newly spawned person.</returns>
        private GameObject SpawnBotAtLocation(Vector3 spawnPosition, NPCBotPrefab character, BotWrapper wrapper, int id, string area = "")
        {
            Vector3 randomDestination = spawnPosition;

            UnityEngine.Object prefab = Resources.Load(character.prefabPath);
            GameObject bot = (GameObject)Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
            bot.transform.SetParent(transform);
            bot.GetComponent<NPCBot>().Create(character.avatarPath);
            ICustomAvatar botAvatar = bot.GetComponentInChildren<ICustomAvatar>();

            if(botAvatar != null)
            {
                botAvatar.Customise(botAvatar.Settings.Randomise());
            }

            //name
            bot.name = "BOT_" + id.ToString();
            bot.transform.position = bot.transform.up + randomDestination;

            if (!CheckDestination(bot.transform.position))
            {
                NavMeshHit myNavHit;

                if (NavMesh.SamplePosition(bot.transform.position, out myNavHit, 100, -1))
                {
                    bot.transform.position = bot.transform.up + myNavHit.position;
                }
            }

            NavMeshAgent agent = bot.AddComponent<NavMeshAgent>();
            agent.speed = m_settings.speed;
            agent.angularSpeed = m_settings.angularSpeed;
            agent.acceleration = m_settings.aceleration;
            agent.stoppingDistance = m_settings.stoppingDistance;
            agent.radius = m_settings.obsticalAvoidanceRadius;
            agent.avoidancePriority = m_settings.obsticalAvoidancePriority;

            //wrapper
            if (wrapper != null)
            {
                BotJson bJson;

                if(botAvatar != null)
                {
                    bJson = new BotJson(id.ToString(), JsonUtility.ToJson(character), area, botAvatar.GetProperties(), randomDestination);
                }
                else
                {
                    Hashtable hash = new Hashtable();
                    hash.Add("TYPE", "FIXED");
                    bJson = new BotJson(id.ToString(), JsonUtility.ToJson(character), area, hash, randomDestination);
                }

                wrapper.bots.Add(bJson);
            }

            return bot;
        }

        /// <summary>
        /// Selects a random point on the game board (NavMesh).
        /// </summary>
        /// <returns>Vector3 of the random location.</returns>
        private Vector3 GetRandomGameBoardLocation()
        {
            NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();

            int maxIndices = navMeshData.indices.Length - 3;

            // pick the first indice of a random triangle in the nav mesh
            int firstVertexSelected = UnityEngine.Random.Range(0, maxIndices);
            int secondVertexSelected = UnityEngine.Random.Range(0, maxIndices);

            // spawn on verticies
            Vector3 point = navMeshData.vertices[navMeshData.indices[firstVertexSelected]];

            Vector3 firstVertexPosition = navMeshData.vertices[navMeshData.indices[firstVertexSelected]];
            Vector3 secondVertexPosition = navMeshData.vertices[navMeshData.indices[secondVertexSelected]];

            // eliminate points that share a similar X or Z position to stop spawining in square grid line formations
            if ((int)firstVertexPosition.x == (int)secondVertexPosition.x || (int)firstVertexPosition.z == (int)secondVertexPosition.z)
            {
                point = GetRandomGameBoardLocation(); // re-roll a position - I'm not happy with this recursion it could be better
            }
            else
            {
                // select a random point on it
                point = Vector3.Lerp(firstVertexPosition, secondVertexPosition, UnityEngine.Random.Range(0.05f, 0.95f));
            }

            return point;
        }

        /// <summary>
        /// States if a new random position if on the navmesh
        /// </summary>
        /// <param name="targetDestination"></param>
        /// <returns></returns>
        private bool CheckDestination(Vector3 targetDestination)
        {
            NavMeshHit hit;

            if (NavMesh.SamplePosition(targetDestination, out hit, 1f, NavMesh.AllAreas))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds closes nav mesh point from source
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public Vector3 GetClosestPostionOnNavMesh(Vector3 source)
        {
            NavMeshHit hit;

            if (NavMesh.SamplePosition(source, out hit, 1f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return source;
        }

        /// <summary>
        /// Retuns if bot behaviour exists in the created bots
        /// </summary>
        /// <param name="bot"></param>
        /// <returns></returns>
        public bool BotExistsInCollection(NPCBot bot)
        {
            for(int i = 0; i < m_botsCreated.Count; i++)
            {
                if(m_botsCreated[i].behaviour.Equals(bot))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Class to represent a BOT
        /// </summary>
        private class Bot
        {
            public Transform bot;
            public NPCBot behaviour;

            public Vector3 rotation;
            public Vector3 position;

            public Collider[] walkableArea;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="bot"></param>
            public Bot(Transform bot, Collider[] walkableArea)
            {
                this.bot = bot;
                behaviour = bot.GetComponent<NPCBot>();
                this.walkableArea = walkableArea;

                rotation = Vector3.zero;
                position = new Vector3(behaviour.Avatar.localPosition.x, behaviour.Avatar.localPosition.y, behaviour.Avatar.localPosition.z);
            }

            public Vector3 GetPoint()
            {
                int rand = Random.Range(0, walkableArea.Length);

                Vector3 point = new Vector3(
                   Random.Range(walkableArea[rand].bounds.min.x, walkableArea[rand].bounds.max.x),
                   0.5f,
                   Random.Range(walkableArea[rand].bounds.min.z, walkableArea[rand].bounds.max.z)
               );

                return point;
            }
        }

        [System.Serializable]
        private class BotWrapper
        {
            public List<BotJson> bots = new List<BotJson>();
        }

        [System.Serializable]
        private class BotJson
        {
            public string id = "";
            public string prefab = "";
            public float x;
            public float y;
            public float z;
            public string area;
            public List<BotData> data = new List<BotData>();

            /// <summary>
            /// Return this json
            /// </summary>
            /// <returns></returns>
            public string ToJson()
            {
                return JsonUtility.ToJson(this);
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="id"></param>
            /// <param name="sex"></param>
            /// <param name="settings"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="z"></param>
            public BotJson(string id, string prefab, string spawnarea, Hashtable settings, float x, float y, float z)
            {
                this.id = id;
                this.prefab = prefab;
                this.x = x;
                this.y = y;
                this.z = z;
                area = spawnarea;

                foreach (DictionaryEntry obj in settings)
                {
                    data.Add(new BotData(obj.Key.ToString(), obj.Value.ToString()));
                }
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="id"></param>
            /// <param name="sex"></param>
            /// <param name="settings"></param>
            /// <param name="vec"></param>
            public BotJson(string id, string prefab, string spawnarea, Hashtable settings, Vector3 vec)
            {
                this.id = id;
                this.prefab = prefab;
                area = spawnarea;

                foreach (DictionaryEntry obj in settings)
                {
                    data.Add(new BotData(obj.Key.ToString(), obj.Value.ToString()));
                }

                x = vec.x;
                y = vec.y;
                z = vec.z;
            }

            /// <summary>
            /// Returns the avatar data for this bot
            /// </summary>
            /// <returns></returns>
            public Hashtable GetData()
            {
                Hashtable hash = new Hashtable();
                
                foreach(BotData bd in data)
                {
                    if (bd.key.Equals("SEX") || bd.key.Equals("TYPE")) continue;

                    hash.Add(bd.key, int.Parse(bd.value));
                }

                return hash;
            }

            /// <summary>
            /// Returns the default start position of this bot
            /// </summary>
            /// <returns></returns>
            public Vector3 Get()
            {
                return new Vector3(x, y, z);
            }
        }

        [System.Serializable]
        private class BotData
        {
            public string key;
            public string value;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            public BotData(string key, string value)
            {
                this.key = key;
                this.value = value;
            }
        }

        [System.Serializable]
        public enum BotSpawning { NavMeshPoint, UserDefinedArea }

        [System.Serializable]
        public enum BotNetwork { Sync, Local }

        [System.Serializable]
        public class NPCScene
        {
            public string sceneName = "";
            public int numberOfBots = 5;
            public bool useAvatarClothingColors = false;
            public float stationaryMaxTime = 5.0f;
            public float maxWalkingDistance = 100.0f;
            public List<NPCBotPrefab> botPrefabs = new List<NPCBotPrefab>();
        }

        [System.Serializable]
        public class NPCBotPrefab
        {
            public string prefabPath = "NPC/NPCBot";
            public string avatarPath = "";
            public int quantity = 1;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(NPCManager), true)]
    public class NPCManager_Editor : BaseInspectorEditor
    {
        private NPCManager managerScript;

        private void OnEnable()
        {
            GetBanner();
            Initialise();
        }

        public override void OnInspectorGUI()
        {
            DisplayBanner();

            serializedObject.Update();

           // EditorGUILayout.PropertyField(serializedObject.FindProperty("interactionDistance"), true);
           // EditorGUILayout.PropertyField(serializedObject.FindProperty("useLocalDistance"), true);

            /* EditorGUILayout.PropertyField(serializedObject.FindProperty("spawning"), true);

             if(serializedObject.FindProperty("spawning").enumValueIndex == 0)
             {
                 EditorGUILayout.PropertyField(serializedObject.FindProperty("numberOfBots"), true);
             }
             else
             {
                 EditorGUILayout.LabelField("Number of bots are calculated on the Scene's NPCBotSpawnArea scripts!");
             }

             EditorGUILayout.PropertyField(serializedObject.FindProperty("botAvatars"), true);*/

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(managerScript);
        }

        protected void Initialise()
        {
            managerScript = (NPCManager)target;
        }
    }
#endif
}
