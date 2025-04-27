using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class NPCBot : MonoBehaviour
    {
        [SerializeField]
        private NPCBotCollisionDetection collisionDetection;

        [SerializeField]
        private Transform avatarHolder;

        [SerializeField]
        private bool isInteractable = false;

        private NavMeshAgent m_navAgent;
        private bool m_isFollowing = false;

        /// <summary>
        /// Global access to this BOT state
        /// </summary>
        public BotSate State
        {
            get
            {
                return m_state;
            }
        }

        /// <summary>
        /// Global access to this bots current synced path
        /// </summary>
        public SyncBotPath SyncPath
        {
            get;
            private set;
        }

        public bool IsInteractable
        {
            get
            {
                return isInteractable;
            }
            set
            {
                isInteractable = value;
            }
        }

        private float m_totalWalkingDistance = 0.0f;

        /// <summary>
        /// Returns if this BOT has reached the destination
        /// </summary>
        public bool HasReachedDestination
        {
            get
            {
                if(m_navAgent == null)
                {
                    m_navAgent = GetComponent<NavMeshAgent>();
                }

                if(m_isFollowing)
                {
                    if(m_followScript != null)
                    {
                        return m_followScript.HasReachedTarget;
                    }
                }

                if(m_navAgent)
                {
                    if (m_navAgent.isPathStale)
                    {
                        m_state = BotSate.Ready;
                        return true;
                    }

                    if(m_totalWalkingDistance - m_navAgent.remainingDistance > NPCManager.Instance.MaxWalkingDistance)
                    {
                        return true;
                    }

                    return m_navAgent.remainingDistance <= 0.1f;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Access to this bots navmesh agent
        /// </summary>
        public NavMeshAgent Agent
        {
            get
            {
                if (m_navAgent == null)
                {
                    m_navAgent = GetComponent<NavMeshAgent>();
                }

                return m_navAgent;
            }
        }

        /// <summary>
        /// Returns if this bot has encountered an obstical on the NavMesh
        /// </summary>
        public bool EncounteredObstacle
        {
            get
            {
                return collisionDetection.HasCollided;
            }
        }


        /// <summary>
        /// Access to this bots avatar
        /// </summary>
        public Transform Avatar
        {
            get
            {
                return m_animator.transform;
            }
        }

        /// <summary>
        /// Acces to the animator of this bot
        /// </summary>
        public Animator Ani
        {
            get
            {
                return m_animator;
            }
        }

        public System.Action<bool> OnInteract { get; set; }
        public System.Action<GameObject> OnBotCreated { get; set; }

        private float m_timer = 0.0f;
        private BotSate m_state = BotSate.Ready;
        private Vector3 m_destination;
        private Animator m_animator;
        private float m_stationaryTime = 0.0f;
        private NPCBotFollow m_followScript;
        private bool m_clickState = false;

        private void Awake()
        {
            m_navAgent = GetComponent<NavMeshAgent>();
            m_state = BotSate.Ready;
            m_destination = transform.position;

            m_animator = GetComponentInChildren<Animator>();

            m_followScript = GetComponent<NPCBotFollow>();
        }

        public void Create(string avatar)
        {
            //instantate the prefab
            UnityEngine.Object prefab = Resources.Load(avatar);
            GameObject go = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, avatarHolder);
            go.transform.localPosition = Vector3.zero;
            go.transform.localEulerAngles = Vector3.zero;
            go.SetActive(true);

            go.layer = LayerMask.NameToLayer("NPC");

            foreach (var child in go.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = LayerMask.NameToLayer("NPC");
            }

            if(OnBotCreated != null)
            {
                OnBotCreated.Invoke(go);
            }

            Awake();

            collisionDetection.Enabled = true;
        }

        //need to change this to raycaster event
        public void OnClick()
        {
            if (!isInteractable) return;

            if (CoreManager.Instance.CurrentState == state.Running && RaycastManager.Instance.CastRay)
            {
                if (OnInteract != null)
                {
                    AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, gameObject.name);

                    m_clickState = !m_clickState;

                    if(m_state.Equals(BotSate.Walk))
                    {
                        m_navAgent.destination = transform.position;

                        if (m_animator != null)
                        {
                            m_animator.SetBool("Moved", false);
                        }

                        if (CoreManager.Instance.projectSettings.npcMode.Equals(NPCMode.Network))
                        {
                            //RPC new synced path/state for bot
                            SyncBotPath syncPath = new SyncBotPath();
                            syncPath.currentPosition = new SyncBotPathPoint(transform.position);
                            syncPath.currentRotation = new SyncBotPathPoint(transform.localEulerAngles);
                            syncPath.points.Add(new SyncBotPathPoint(transform.position));

                            MMOManager.Instance.SendRPC("BotControl", (int)MMOManager.RpcTarget.Others, "IDLE", gameObject.name, JsonUtility.ToJson(syncPath));
                        }
                    }

                    m_state = BotSate.Interrupted;
                    OnInteract.Invoke(m_clickState);
                }
            }
        }

        /// <summary>
        /// Called to let bot follow object and ignore nav mesh
        /// </summary>
        /// <param name="t"></param>
        public void Follow(Transform t, float speed = 1.0f)
        {
            if (m_followScript != null)
            {
                m_isFollowing = true;
                collisionDetection.Clear();
                m_navAgent.isStopped = true;
                m_state = BotSate.Interrupted;
                m_followScript.Follow(t);
            }
        }

        public void ReEngageBot()
        {
            m_clickState = false;
            m_state = BotSate.Ready;
        }

        /// <summary>
        /// Called to reset this bot to a position back on navmesh
        /// </summary>
        /// <param name="destination"></param>
        public void ResetOnNavMesh(Vector3 destination)
        {
            if (m_navAgent == null)
            {
                m_navAgent = GetComponent<NavMeshAgent>();
            }

            //need to allow this bot to move back to destination on nav mesh
            if (m_followScript != null)
            {
                m_isFollowing = true;
                collisionDetection.Clear();
                m_state = BotSate.Reset;
                m_followScript.Follow(transform.up + destination, m_navAgent.speed);
            }
            else
            {
                m_isFollowing = true;
                transform.position = transform.up + destination;
                m_destination = transform.position;
                m_state = BotSate.Ready;
            }
        }

        /// <summary>
        /// Called to avoid the local player when moving is possible
        /// </summary>
        public void AvoidPlayer()
        {
            //avoid local player
            Vector3 delta = PlayerManager.Instance.GetLocalPlayer().TransformObject.position - transform.position;
            delta.y = 0f;

            float magnitude = delta.magnitude;

            if (magnitude < 1.0f)
            {
                transform.position -= delta.normalized * (1.0f - magnitude);
            }
        }

        /// <summary>
        /// Action called to make this bot walk
        /// </summary>
        /// <param name="destination"></param>
        public void Walk(Vector3 destination)
        {
            //clear any current collision otherwise the bot will not move
            m_isFollowing = false;
            collisionDetection.Clear();

            //check if bot forward direction has obsticle, if true, renew destination with position behind bot
            RaycastHit hit;
            float distance = collisionDetection.Size.z + 0.5f;

            if (Physics.Raycast(transform.position, transform.forward, out hit, distance))
            {
                if(hit.transform != null)
                {
                    //make destination behind the bot so that they animate and walk rotating 180
                    Vector3 vec = transform.position + -transform.forward * distance;
                    //maintain the Y height based on orignal destination-ensures we stay on navmash
                    destination = new Vector3(vec.x, destination.y, vec.z);
                }
            }

            //update state/destination/animation
            m_destination = destination;
            m_state = BotSate.Walk;
            m_totalWalkingDistance = Vector3.Distance(transform.position, m_destination);

            if (m_navAgent == null)
            {
                m_navAgent = GetComponent<NavMeshAgent>();
            }

            m_navAgent.isStopped = false;
            m_navAgent.destination = m_destination;

            if(m_animator != null)
            {
                m_animator.SetBool("Moved", true);
            }

            //RPC new synced path/state for bot
            if (CoreManager.Instance.projectSettings.npcMode.Equals(NPCMode.Network))
            {
                SyncBotPath syncPath = new SyncBotPath();
                syncPath.currentPosition = new SyncBotPathPoint(transform.position);
                syncPath.currentRotation = new SyncBotPathPoint(transform.localEulerAngles);
                syncPath.points.Add(new SyncBotPathPoint(m_destination));

                MMOManager.Instance.SendRPC("BotControl", (int)MMOManager.RpcTarget.Others, "WALK", gameObject.name, JsonUtility.ToJson(syncPath));
            }
        }

        /// <summary>
        /// Action called to make this bot stop walking
        /// </summary>
        public void Stop()
        {
            //set states and positions/ animations
            m_timer = 0.0f;
            m_state = BotSate.Idle;
            m_navAgent.destination = transform.position;

            m_stationaryTime = Random.Range(0.5f, NPCManager.Instance.MaxStationaryTime);

            if (m_animator != null)
            {
                m_animator.SetBool("Moved", false);
            }

            if (CoreManager.Instance.projectSettings.npcMode.Equals(NPCMode.Network))
            {
                //RPC new synced path/state for bot
                SyncBotPath syncPath = new SyncBotPath();
                syncPath.currentPosition = new SyncBotPathPoint(transform.position);
                syncPath.currentRotation = new SyncBotPathPoint(transform.localEulerAngles);
                syncPath.points.Add(new SyncBotPathPoint(transform.position));

                MMOManager.Instance.SendRPC("BotControl", (int)MMOManager.RpcTarget.Others, "IDLE", gameObject.name, JsonUtility.ToJson(syncPath));
            }
        }

        /// <summary>
        /// Acftion called to recalculate a new path to current destination
        /// </summary>
        /// <returns></returns>
        public bool RecalculatePath()
        {
            NavMeshPath path = new NavMeshPath();

            if(m_navAgent.CalculatePath(m_destination, path))
            {
                m_navAgent.SetPath(path);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Action called to check this bots idle state
        /// </summary>
        /// <returns></returns>
        public bool Wait()
        {
            if(m_state.Equals(BotSate.Idle))
            {
                m_timer += Time.deltaTime;

                //stay idle for certain amount of time
                if(m_timer >= m_stationaryTime)
                {
                    m_state = BotSate.Ready;
                }

                return m_timer < 5.0f;
            }

            return false;
        }

        /// <summary>
        /// Action called to sync this bot over the network
        /// </summary>
        /// <param name="state"></param>
        /// <param name="path"></param>
        public void SyncBot(string state, SyncBotPath path)
        {
           //ensure the bots transform is correct
            transform.position = path.currentPosition.Get();
            transform.localEulerAngles = path.currentRotation.Get();

            //check state and set motion
            switch (state)
            {
                case "WALK":
                    SyncPath = path;
                    m_state = BotSate.Walk;
                    SyncPath.Move(this);

                    if (m_animator != null)
                    {
                        m_animator.SetBool("Moved", true);
                    }
                    break;
                default:
                    SyncPath = path;
                    m_state = BotSate.Idle;
                    SyncPath.Move(this);

                    if (m_animator != null)
                    {
                        m_animator.SetBool("Moved", false);
                    }
                    break;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(NPCBot), true)]
        public class NPCBot_Editor : BaseInspectorEditor
        {
            private NPCBot npcScript;
            private GameObject avatar;
            private AppSettings m_settings;
            private bool botsExist = false;

            private string[] prefabs;
            private int prefabSelectedIndex = 0;

            private void OnEnable()
            {
                GetBanner();
                npcScript = (NPCBot)target;

                Transform avatarHolder = (Transform)serializedObject.FindProperty("avatarHolder").objectReferenceValue;

                if(avatarHolder != null && avatarHolder.childCount > 0)
                {
                    avatar = avatarHolder.GetChild(0).gameObject;
                }

                AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");

                if (appReferences != null)
                {
                    m_settings = appReferences.Settings;
                }
                else
                {
                    m_settings = Resources.Load<AppSettings>("ProjectAppSettings");
                }

                NPCManager.NPCScene scene = m_settings.NPCSettings.GetScene(npcScript.gameObject.scene.name);

                if(scene != null)
                {
                    prefabs = new string[scene.botPrefabs.Count];

                    for(int i = 0; i < scene.botPrefabs.Count; i++)
                    {
                        prefabs[i] = scene.botPrefabs[i].avatarPath;

                        if(avatar != null)
                        {
                            if(prefabs[i].Contains(avatar.name))
                            {
                                prefabSelectedIndex = 1;
                            }
                        }
                    }

                    if(prefabs.Length > 0)
                    {
                        botsExist = true;
                    }
                    else
                    {
                        botsExist = false;
                    }
                }
                else
                {
                    botsExist = false;
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("collisionDetection"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("avatarHolder"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("isInteractable"), true);

                if(!Application.isPlaying)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Avatar", EditorStyles.boldLabel);

                    if (botsExist)
                    {
                        //create dropdown
                        int selected = EditorGUILayout.Popup("Avatar", prefabSelectedIndex, prefabs);

                        //on change delete old and create new + randomise
                        if (selected != prefabSelectedIndex || avatar == null)
                        {
                            prefabSelectedIndex = selected;

                            if (avatar != null)
                            {
                                DestroyImmediate(avatar);
                            }

                            NPCManager.NPCScene scene = m_settings.NPCSettings.GetScene(npcScript.gameObject.scene.name);
                            UnityEngine.Object prefab = Resources.Load(scene.botPrefabs[selected].avatarPath);
                            GameObject bot = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, (Transform)serializedObject.FindProperty("avatarHolder").objectReferenceValue);
                            bot.transform.localPosition = Vector3.zero;
                            bot.SetActive(true);
                            bot.name = scene.botPrefabs[selected].avatarPath;
                            ICustomAvatar iAvatar = bot.GetComponent<ICustomAvatar>();

                            if (iAvatar != null)
                            {
                                iAvatar.Customise(iAvatar.Settings.Randomise());
                            }

                            string layer = "NPC";

                            bot.layer = LayerMask.NameToLayer(layer);

                            foreach (var child in bot.GetComponentsInChildren<Transform>(true))
                            {
                                child.gameObject.layer = LayerMask.NameToLayer(layer);
                            }

                            avatar = bot;
                        }

                        if(GUILayout.Button("Randomise Outfit") && avatar != null)
                        {
                            ICustomAvatar iAvatar = avatar.GetComponent<ICustomAvatar>();

                            if(iAvatar != null)
                            {
                                iAvatar.Customise(iAvatar.Settings.Randomise());
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Cannot pre-load avatar. NPC App Settings does not contain scene or avatars!", EditorStyles.miniBoldLabel);
                    }
                }

                if(GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(npcScript);
                }
            }
        }
#endif
    }

    public enum BotSate { Ready, Walk, Idle, Interrupted, Reset }

    [System.Serializable]
    public class SyncBotPath
    {
        public SyncBotPathPoint currentRotation;
        public SyncBotPathPoint currentPosition;
        public List<SyncBotPathPoint> points = new List<SyncBotPathPoint>();

        private int m_pointIndex = 0;

        /// <summary>
        /// current point inded to the synced path points (this did not work using the NavAgents.Path.corners
        /// </summary>
        public int PointIndex
        {
            get
            {
                return m_pointIndex;
            }
            set
            {
                m_pointIndex = value;
            }
        }

        /// <summary>
        /// Moves the bot to new destination (always the final destination)
        /// </summary>
        /// <param name="behaviour"></param>
        public void Move(NPCBot behaviour)
        {
            behaviour.Agent.destination = points[m_pointIndex].Get();
        }
    }

    /// <summary>
    /// Call for a single bot point vector
    /// </summary>
    [System.Serializable]
    public class SyncBotPathPoint
    {
        public float x;
        public float y;
        public float z;

        public SyncBotPathPoint(Vector3 vec)
        {
            x = vec.x;
            y = vec.y;
            z = vec.z;
        }

        public Vector3 Get()
        {
            return new Vector3(x, y, z);
        }
    }
}
