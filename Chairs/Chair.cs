using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Chair : UniqueID, IChair, IChairObject
    {
        [SerializeField]
        private Transform sittingSpot;

        [SerializeField]
        private Vector3 avatarRotation = Vector3.zero;

        [SerializeField]
        private SitMode sittingMode = SitMode.Chair;

        [SerializeField]
        private LiveStreamMode liveStreamMode = LiveStreamMode.None;

        public ChairGroup chairGroup;
        private string ani = "";

        public Lock chairLock;
        private float m_lockTimer = 0.0f;
        private Coroutine m_lockTimerProcess;

        private LiveStreamRole m_LiveStreamRole = LiveStreamRole.audience;


#if UNITY_EDITOR
        public void EditorSetChairVars(Transform sittingSpot)
        {
            this.sittingSpot = sittingSpot;
        }
#endif

        /// <summary>
        /// Global access to the occpancy state
        /// </summary>
        public bool IsOccupied
        {
            get;
            private set;
        }

        public bool HasSittingSpot
        {
            get
            {
                return sittingSpot != null;
            }
        }

        /// <summary>
        /// Global access to this chairs occupied IPlayer interface
        /// </summary>
        public List<IPlayer> Occupancies { get; private set; }

        private Vector3 occupantPreviousPosition;

        public void SetOccupantsPreviousPosition(Vector3 vec, string playerID = "")
        {
            occupantPreviousPosition = vec;
        }

        public Vector3 GetOccupantsPreviousPosition(string playerID = "")
        {
            return occupantPreviousPosition;
        }

        public IChair MainInteraface
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// Global access to the occupied network players ID
        /// </summary>
        public string OccupantID { get; private set; }

        /// <summary>
        /// Access to the chairs group
        /// </summary>
        public ChairGroup Group
        {
            get
            {
                GetChairGroup();
                return chairGroup;
            }
        }

        public LiveStreamMode StreamMode { get { return liveStreamMode; } set { liveStreamMode = value; } }

        public bool ChairOccupied
        { 
            get
            {
                return IsOccupied;
            }
        }

        public GameObject GO 
        {
            get
            {
                return gameObject;
            }
        }

        public Lock ChairLock 
        { 
            get
            {
                return chairLock;
            }
            set
            {
                chairLock = value;
            }
        }

        public string IDRef
        { 
            get
            {
                return ID;
            }
            set
            {
                ID = value;
            }
        }

        public bool CanUserControl(string user)
        {
            return CanUserControlThis(user);
        }

        public override bool HasParent
        {
            get
            {
                return GetComponentInParent<ChairGroup>() != null;
            }
        }

        public Vector3 SittingPosition(string playerID = "")
        {
            Vector3 sitSpot = Vector3.zero;

            if (Occupancies != null && Occupancies.Count > 0)
            {
                sitSpot = new Vector3(transform.position.x, Occupancies[0].TransformObject.position.y, transform.position.z);
            }

            if (sittingSpot != null)
            {
                sitSpot = new Vector3(sittingSpot.position.x, sittingSpot.position.y, sittingSpot.position.z);
            }

            return sitSpot;
        }

        public Vector3 SittingDirection(string playerID = "")
        {
            //works
            if (sittingSpot != null)
            {
                return sittingSpot.forward;
            }
            else
            {
                return new Vector3(0, transform.localEulerAngles.y, 0);
            }
        }

        void Start()
        {
            ChairManager.Instance.AddIChairObject(this);

            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).GetComponent<Lock>())
                    chairLock = transform.GetChild(i).GetComponent<Lock>();
            }

            bool hasCollider = GetComponentsInChildren<Collider>().Length > 0 ? true : false;

            if(!hasCollider)
            {
                gameObject.AddComponent<BoxCollider>();
            }

            if(AppManager.Instance.Settings.playerSettings.addHightLightToChairs)
            {
                if(GetComponent<Highlight>() == null)
                {
                    gameObject.AddComponent<Highlight>();
                }
            }

            if(chairLock != null)
            {
                if(Group.LockChairOnLeave)
                {
                    chairLock.OnUnlock += LockTimer;
                }

                chairLock.IsLocked = true;
                chairLock.IsNetworked = false;
            }
        }

        private void LockTimer()
        {
            m_lockTimerProcess = StartCoroutine(ProcessLockTimer());
        }

        /// <summary>
        /// Will lock chair if unlocked and not occupied after 5 seconds
        /// </summary>
        private IEnumerator ProcessLockTimer()
        {
            if (chairLock == null) yield break;

            if(!chairLock.IsLocked && Group.LockChairOnLeave)
            {
                if(!IsOccupied)
                {
                    while(m_lockTimer < 5.0f)
                    {
                        m_lockTimer += Time.deltaTime;
                        yield return null;
                    }

                    m_lockTimer = 0.0f;
                    chairLock.LockThis();
                    m_lockTimerProcess = null;
                }
            }
        }

        /// <summary>
        /// Joins player to chair
        /// </summary>
        /// <param name="player"></param>
        public void Join(IPlayer player)
        {
            if (IsOccupied) return;

            if(player.IsLocal)
            {
                if(m_lockTimerProcess != null)
                {
                    StopCoroutine(m_lockTimerProcess);
                    m_lockTimerProcess = null;
                }

                AppManager.Instance.ToggleVideoChat(false, "");
            }

            foreach (Collider col in transform.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
            }

            //join to group
            if (GetChairGroup())
            {
                chairGroup.Join(player);
            }

            IsOccupied = true;

            if (Occupancies == null)
            {
                Occupancies = new List<IPlayer>();
            }

            //add occupant
            Occupancies.Add(player);
            OccupantID = player.ID;

            ChairManager.Instance.AmendPlayerToGlobalChairOccupancy(true, player.ID);

            //set up avatar position, rotation and animation
            if (player.IsLocal)
            {
                if (GetComponent<Highlight>())
                {
                    GetComponent<Highlight>().HighlightObject(false);
                    GetComponent<Highlight>().isEnabled = false;
                }

                //need to network occupancy for this chair [SEND CHAIR AND GROUP]
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("EVENT_TYPE", "CHAIR");
                //data.Add("G", chairGroup.ID);
                data.Add("I", ID);
                data.Add("O", "1");
                data.Add("P", player.ActorNumber.ToString());

                MMOManager.Instance.ChangeRoomProperty(ID, data);

                if (player.NavMeshAgentScript != null)
                {
                    player.NavMeshAgentScript.isStopped = true;
                    player.NavMeshAgentScript.enabled = false;
                }

                TransportPlayer(true);
                player.Animation.SetBool("Moved", false);

                if (sittingMode.Equals(SitMode.Chair))
                    ani = "Sit";
                else if (sittingMode.Equals(SitMode.Floor))
                    ani = "SitFloor";
                else if (sittingMode.Equals(SitMode.Standing))
                    ani = "Standing";

                player.Animation.SetBool(ani, true);
            }
            else
            {
                //set networkplayer animator to play sit
                MMOPlayer nPlayer = player.MainObject.GetComponent<MMOPlayer>();
                nPlayer.FreezePosition = true;
                nPlayer.FreezeRotation = true;

                if(nPlayer.NavMeshAgentUsed)
                {
                    nPlayer.NavMeshAgentUsed.isStopped = true;
                    nPlayer.NavMeshAgentUsed.enabled = false;
                }

                TransportPlayer(true);

                Vector3 labelPos = nPlayer.LabelNameFront.transform.parent.localPosition;
                nPlayer.LabelNameFront.transform.parent.localPosition = new Vector3(labelPos.x, labelPos.y - 0.3f, labelPos.z);
                nPlayer.animator.SetBool("Moved", false);

                if (sittingMode.Equals(SitMode.Chair))
                    ani = "Sit";
                else if (sittingMode.Equals(SitMode.Floor))
                    ani = "SitFloor";
                else if (sittingMode.Equals(SitMode.Standing))
                    ani = "Standing";

                nPlayer.SittingAnimation = ani;

                nPlayer.animator.SetBool(ani, true);
            }
        }

        /// <summary>
        /// Leaves player from chair
        /// </summary>
        /// <param name="player"></param>
        public void Leave(IPlayer player)
        {
            if (!IsOccupied) return;

            IsOccupied = false;

            //leave group
            if (GetChairGroup())
            {
                chairGroup.Leave(player);
            }

            if(Occupancies == null)
            {
                Occupancies = new List<IPlayer>();
            }

            ChairManager.Instance.AmendPlayerToGlobalChairOccupancy(false, player.ID);


            //reset up avatar position, rotation and animation
            if (player.IsLocal)
            {
                if (GetComponent<Highlight>())
                {
                    GetComponent<Highlight>().isEnabled = true;
                }

                //need to network occupancy for this chair [SEND CHAIR AND GROUP]
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("EVENT_TYPE", "CHAIR");
                //data.Add("G", chairGroup.ID);
                data.Add("I", ID);
                data.Add("O", "0");
                data.Add("P", player.ActorNumber.ToString());

                MMOManager.Instance.ChangeRoomProperty(ID, data);

                player.Animation.SetBool(ani, false);
                player.Animation.Play("Idle", 0, 1.0f);

                ChairManager.Instance.ExitChair();

                TransportPlayer(false);

                if (player.NavMeshAgentScript != null)
                {
                    player.NavMeshAgentScript.enabled = true;
                }

                if(Group.LockChairOnLeave)
                {
                    if(chairLock != null)
                    {
                        chairLock.LockThis();
                    }
                }
            }
            else
            {
                //set networkplayer animator to play idle
                MMOPlayer nPlayer = player.MainObject.GetComponent<MMOPlayer>();

                TransportPlayer(false);

                Vector3 labelPos = nPlayer.LabelNameFront.transform.parent.localPosition;
                nPlayer.LabelNameFront.transform.parent.localPosition = new Vector3(labelPos.x, labelPos.y + 0.3f, labelPos.z);

                nPlayer.animator.SetBool(ani, false);
                nPlayer.animator.Play("Idle", 0, 1.0f);

                nPlayer.FreezePosition = false;
                nPlayer.FreezeRotation = false;

                if (nPlayer.NavMeshAgentUsed)
                {
                    nPlayer.NavMeshAgentUsed.enabled = true;
                }

                nPlayer.SetPlayerToIdle();
            }

            foreach (Collider col in transform.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = true;
            }

            //remove occupant
            Occupancies.Clear();
            OccupantID = "";
        }


        /// <summary>
        /// Sets the players orientation/camera
        /// </summary>
        /// <param name="join"></param>
        private void TransportPlayer(bool join)
        {
            if(join)
            {
                Occupancies[0].TransformObject.GetComponent<Collider>().enabled = false;

                occupantPreviousPosition = new Vector3(Occupancies[0].TransformObject.position.x, Occupancies[0].TransformObject.position.y, Occupancies[0].TransformObject.position.z);
                Occupancies[0].TransformObject.position = SittingPosition();

                //works
                if (sittingSpot != null)
                {
                    Occupancies[0].TransformObject.forward = sittingSpot.forward;
                }
                else
                {
                    Occupancies[0].TransformObject.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
                }

                if (Occupancies[0].IsLocal)
                {
                    chairGroup.Cam.SetActive(true);
                    Occupancies[0].MainCamera.gameObject.SetActive(false);
                }
            }
            else
            {
                Occupancies[0].TransformObject.position = occupantPreviousPosition;
                occupantPreviousPosition = Vector3.zero;
                Occupancies[0].TransformObject.GetComponent<Collider>().enabled = true;

                if(Occupancies[0].IsLocal)
                {
                    chairGroup.Cam.SetActive(false);
                    Occupancies[0].MainCamera.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Returns the chair group
        /// </summary>
        /// <returns></returns>
        private bool GetChairGroup()
        {
            if(chairGroup == null)
            {
                chairGroup = GetComponentInParent<ChairGroup>();
            }

            return chairGroup;
        }

        public void UpdateLiveStreamRole()
        {
            if (Group.StreamingMode.Equals(ChairGroup.VideoStream.LiveStream))
            {
                if (Group is ConferenceChairGroup)
                {
                    if(((ConferenceChairGroup)Group).Owner.ID.Equals(OccupantID))
                    {
                        liveStreamMode = LiveStreamMode.Host;
                        m_LiveStreamRole = LiveStreamRole.host;
                    }
                    else
                    {
                        liveStreamMode = LiveStreamMode.Audience;
                        m_LiveStreamRole = LiveStreamRole.audience;
                    }

                    StartCoroutine(SwitchLiveStreamRole(m_LiveStreamRole));
                }
            }
        }

        private IEnumerator SwitchLiveStreamRole(LiveStreamRole role)
        {
            if (Group.StreamingMode.Equals(ChairGroup.VideoStream.LiveStream))
            {
                if (Group is ConferenceChairGroup)
                {
                    //request live stream mode
                    WebClientCommsManager.Instance.RequestLivestream(role, CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString() + "_" + chairGroup.ID, false, PlayerManager.Instance.GetLocalPlayer().NickName);

                    yield return new WaitForSeconds(5.0f);

                    WebClientCommsManager.Instance.RequestLivestream(role, CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString() + "_" + chairGroup.ID, true, PlayerManager.Instance.GetLocalPlayer().NickName);
                }
            }
        }

        /// <summary>
        /// Send message to webclient to open/close video chat
        /// </summary>
        /// <param name="join"></param>
        public void VideoChat(bool join)
        {
            //video Message
            if (!Group.VideoUsed && join) return;

            if(Group.StreamingMode.Equals(ChairGroup.VideoStream.LiveStream) && join && !liveStreamMode.Equals(LiveStreamMode.None))
            {
                if (Group is ConferenceChairGroup)
                {
                    if (((ConferenceChairGroup)Group).Owner.ID.Equals(OccupantID))
                    {
                        liveStreamMode = LiveStreamMode.Host;
                        m_LiveStreamRole = LiveStreamRole.host;
                    }
                    else
                    {
                        liveStreamMode = LiveStreamMode.Audience;
                        m_LiveStreamRole = LiveStreamRole.audience;
                    }
                }
                else
                {
                    m_LiveStreamRole = liveStreamMode.Equals(LiveStreamMode.Host) ? LiveStreamRole.host : LiveStreamRole.audience;
                }

                //request live stream mode
                WebClientCommsManager.Instance.RequestLivestream(m_LiveStreamRole, CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString() + "_" + chairGroup.ID, true, PlayerManager.Instance.GetLocalPlayer().NickName);
            }
            else
            {
                if(Group.StreamingMode.Equals(ChairGroup.VideoStream.LiveStream))
                {
                    //request live stream mode
                    WebClientCommsManager.Instance.RequestLivestream(m_LiveStreamRole, CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString() + "_" + chairGroup.ID, false, PlayerManager.Instance.GetLocalPlayer().NickName);
                }
                else
                {
                    AppManager.Instance.ToggleVideoChat(join, CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString() + "_" + chairGroup.ID);
                }

                if (AppManager.Instance.Data.GlobalVideoChatUsed && !join)
                {
                    StartupManager.Instance.OpenGlobalVideoChat();
                }
            }
        }

        /// <summary>
        /// Called to change the group camera (local only)
        /// </summary>
        public void ChangeGroupCamera()
        {
            Group.ChangeCamera();
        }

        /// <summary>
        /// Called when a player disconnects from the room
        /// </summary>
        /// <param name="id"></param>
        public void OnPlayerDisconnect(string id)
        {
            IsOccupied = false;
            Occupancies.Clear();
            OccupantID = "";

            if(GetChairGroup())
            {
                chairGroup.OnPlayerDisconnect(id);
            }
        }

        [System.Serializable]
        public enum SitMode { Chair, Floor, Standing }

        [System.Serializable]
        public enum LiveStreamMode { Host, Audience, None }

#if UNITY_EDITOR
        [CustomEditor(typeof(Chair), true), CanEditMultipleObjects]
        public class Chair_Editor : UniqueID_Editor
        {
            private Chair chairScript;

            private void OnEnable()
            {
                GetBanner();
                Initialise();

                if (Application.isPlaying) return;

                if (chairScript.GetComponentInParent<ChairGroup>() == null)
                {
                    base.Initialise();

                    //this will need to add/update the chair from standalone chair IOobjects
                }
                else
                {
                    chairScript.GetComponentInParent<ChairGroup>().UpdateIOChairSettings();
                }

                if(ChairConfiguratorWindow.IsOpen)
                {
                    ChairConfiguratorWindow window = (ChairConfiguratorWindow)EditorWindow.GetWindow(typeof(ChairConfiguratorWindow));
                    window.SetChair(chairScript);
                }
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (chairScript != null)
                {
                    if (chairScript.GetComponentInParent<ChairGroup>() == null)
                    {
                        //this will need to remove the chair from standalone chair IOobjects
                    }
                    else
                    {
                        chairScript.GetComponentInParent<ChairGroup>().UpdateIOChairSettings();
                    }
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                if (chairScript.GetComponentInParent<ChairGroup>() == null)
                {
                    DisplayID();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Chair Setup", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("liveStreamMode"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sittingSpot"), true);

                if(serializedObject.FindProperty("sittingSpot").objectReferenceValue == null)
                {
                    if (GUILayout.Button("Create Sitting Spot"))
                    {
                        GameObject sittingPoint = new GameObject();
                        sittingPoint.transform.SetParent(chairScript.gameObject.transform);
                        sittingPoint.name = "SittingPoint";
                        sittingPoint.transform.localPosition = new Vector3(0, 1, 0);
                        sittingPoint.transform.localScale = Vector3.one;
                        sittingPoint.transform.localEulerAngles = new Vector3(0, 0, 0);

                        chairScript.EditorSetChairVars(sittingPoint.transform);

                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("sittingMode"), true);

                if (GUILayout.Button("Open Configurator"))
                {
                    OpenConfigurator(chairScript);
                }

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(chairScript);
                }
            }

            protected override void Initialise()
            {
                chairScript = (Chair)target;
            }

            private void OpenConfigurator(Chair chair)
            {
                ChairConfiguratorWindow window = (ChairConfiguratorWindow)EditorWindow.GetWindow(typeof(ChairConfiguratorWindow));
                window.SetChair(chair);
                window.maxSize = new Vector2(800f, 300f);
                window.minSize = window.maxSize;
                window.Show();
            }
        }


        public class ChairConfiguratorWindow : EditorWindow
        {
            private Chair m_chair;
            private Transform avatar;
            private AppSettings m_settings;
            private SerializedObject m_asset;

            public static bool IsOpen { get; private set; }

            public void SetChair(Chair chair)
            {
                IsOpen = true;

                AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");

                if (appReferences != null)
                {
                    m_settings = appReferences.Settings;
                }
                else
                {
                    m_settings = Resources.Load<AppSettings>("ProjectAppSettings");
                }

                m_chair = chair;
                m_asset = new SerializedObject(m_chair);

                if (avatar != null)
                {
                    DestroyImmediate(avatar.gameObject);
                    CreateAvatar();
                }

                SelectSittingSpot();
            }

            private void OnDestroy()
            {
                IsOpen = false;

                if (avatar != null)
                {
                    DestroyImmediate(avatar.gameObject);
                }
            }

            private void Update()
            {
                if (avatar != null)
                {
                    if (avatar.GetComponentInChildren<IPlayer>(true) != null)
                    {
                        Animator ani = avatar.GetComponentInChildren<Animator>();

                        if (ani != null)
                        {
                            string aniName = "";

                            if (m_chair.sittingMode.Equals(SitMode.Chair))
                                aniName = "Sit";
                            else if (m_chair.Equals(SitMode.Floor))
                                aniName = "SitFloor";
                            else if (m_chair.Equals(SitMode.Standing))
                                aniName = "Standing";

                            ani.Play(aniName);
                            ani.Update(Time.deltaTime);
                        }
                    }
                }
            }

            private void OnGUI()
            {
                if (Application.isPlaying)
                {
                    if (avatar != null)
                    {
                        DestroyImmediate(avatar.gameObject);
                    }

                    Close();
                    return;
                }

                if (m_settings != null)
                {
                    if (m_settings.brandlabLogo_Banner != null)
                    {
                        GUILayout.Box(m_settings.brandlabLogo_Banner.texture, GUILayout.ExpandWidth(true));
                    }
                    else
                    {
                        m_settings.brandlabLogo_Banner = Resources.Load<Sprite>("Logos/BrandLab360_Banner");
                    }
                }

                EditorGUILayout.LabelField("CHAIR CONFIGURATOR", EditorStyles.boldLabel);
                EditorGUILayout.Space();
               
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Chair:", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField(m_chair.gameObject.name.ToString(), GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                if (m_asset.FindProperty("sittingSpot").objectReferenceValue == null)
                {
                    if (GUILayout.Button("Create Sitting Spot"))
                    {
                        GameObject sittingPoint = new GameObject();
                        sittingPoint.transform.SetParent(m_chair.gameObject.transform);
                        sittingPoint.name = "SittingPoint";
                        sittingPoint.transform.localPosition = new Vector3(0, 1, 0);
                        sittingPoint.transform.localScale = Vector3.one;
                        sittingPoint.transform.localEulerAngles = new Vector3(0, 0, 0);

                        m_chair.EditorSetChairVars(sittingPoint.transform);
                        m_asset.FindProperty("sittingSpot").objectReferenceValue = sittingPoint.transform;

                        SelectSittingSpot();

                        if(avatar !=  null)
                        {
                            avatar.transform.SetParent((Transform)m_chair.sittingSpot);
                            avatar.transform.localPosition = Vector3.zero;
                            avatar.transform.eulerAngles = m_chair.sittingSpot.eulerAngles + m_chair.avatarRotation;
                        }

                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Sitting Spot:", EditorStyles.boldLabel, GUILayout.ExpandWidth(false), GUILayout.Width(100));
                    EditorGUILayout.LabelField(m_chair.sittingSpot.name, GUILayout.ExpandWidth(false), GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();

                    if(GUILayout.Button("Select"))
                    {
                        SelectSittingSpot();
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Remove Sitting Spot"))
                    {
                        if (avatar != null)
                        {
                            DestroyImmediate(avatar.gameObject);
                        }

                        DestroyImmediate(m_chair.sittingSpot.gameObject);
                        m_chair.sittingSpot = null;
                        m_asset.FindProperty("sittingSpot").objectReferenceValue = null;
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Avatar", EditorStyles.boldLabel);

                if (avatar != null)
                {
                    EditorGUILayout.LabelField("Use the sitting spot to position and rotate Avatar", EditorStyles.miniBoldLabel);

                    if (GUILayout.Button("Destroy"))
                    {
                        DestroyImmediate(avatar.gameObject);
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(m_asset.FindProperty("sittingMode"), true);

                    if (GUILayout.Button("Display Avatar"))
                    {
                        CreateAvatar();

                        GUIUtility.ExitGUI();
                    }
                }

                if (GUI.changed)
                {
                    if (m_asset != null) m_asset.ApplyModifiedProperties();

                    if (m_chair != null)
                    {
                        EditorUtility.SetDirty(m_chair);
                    }

                    EditorUtility.SetDirty(this);
                }

            }

            private void CreateAvatar()
            {
                //create avatar
                UnityEngine.Object prefab = Resources.Load(m_settings.playerSettings.playerController);
                GameObject bot = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity);
                bot.SetActive(true);
                bot.name = "ChairPlayer";

                bool fixedAvatarUsed = m_settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Custom) || m_settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe);

                string avatarName = fixedAvatarUsed ? m_settings.playerSettings.fixedAvatars[0] : m_settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Simple) ? m_settings.playerSettings.simpleMan : m_settings.playerSettings.standardMan;

                prefab = Resources.Load(avatarName);
                GameObject av = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, bot.transform.Find("AvatarHolder"));
                av.transform.localPosition = Vector3.zero;
                av.transform.localEulerAngles = Vector3.zero;
                av.transform.parent.transform.localScale = Vector3.one;

                //attach avatar to sitting spot
                if (m_chair.sittingSpot != null)
                {
                    bot.transform.SetParent((Transform)m_chair.sittingSpot);
                    bot.transform.localPosition = Vector3.zero;
                    bot.transform.eulerAngles = m_chair.sittingSpot.eulerAngles + m_chair.avatarRotation;
                }
                else
                {
                    //use the transform position value
                    bot.transform.SetParent(m_chair.gameObject.transform);
                    bot.transform.localPosition = new Vector3(0, 0, 1);
                    bot.transform.localEulerAngles = new Vector3(m_chair.gameObject.transform.localEulerAngles.x * -1, 0, 0);
                }

                avatar = bot.transform;

                SelectSittingSpot();
            }

            private void SelectSittingSpot()
            {
                Transform target;

                if(m_chair.sittingSpot != null)
                {
                    target = m_chair.sittingSpot;
                }
                else
                {
                    target = m_chair.transform;
                }

                //SceneView es = UnityEditor.SceneView.lastActiveSceneView;
                //es.AlignViewToObject(target);
               // es.LookAtDirect(target.position, Quaternion.identity);
                 //es.LookAt(target.position);

                Selection.activeTransform = target;
                //SceneView.lastActiveSceneView.FrameSelected();
            }
        }
#endif
    }
}