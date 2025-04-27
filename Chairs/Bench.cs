using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Bench : UniqueID, IChair, IChairObject
    {
        [SerializeField]
        private List<BenchSpot> sittingSpots = new List<BenchSpot>();

        [SerializeField]
        private Vector3 avatarRotation = Vector3.zero;

        [SerializeField]
        private Chair.SitMode sittingMode = Chair.SitMode.Chair;

        [SerializeField]
        private Chair.LiveStreamMode liveStreamMode = Chair.LiveStreamMode.None;

        public ChairGroup chairGroup;
        public Lock chairLock;
        private LiveStreamRole m_LiveStreamRole = LiveStreamRole.audience;
        private float m_lockTimer = 0.0f;
        private Coroutine m_lockTimerProcess;
        private string ani = "";

        //IChair Properties
        public bool IsOccupied
        {
            get;
            private set;
        }

        public int CountSittingPoints
        {
            get
            {
                if(sittingSpots.Count > 0)
                {
                    return sittingSpots.Count;
                }
                else
                {
                    return 1;
                }
            }
        }

        public List<IPlayer> Occupancies { get; private set; }
        public Chair.LiveStreamMode StreamMode { get { return liveStreamMode; } set { liveStreamMode = value; } }


        //IChair Properties
        public bool ChairOccupied { get { return IsOccupied; } }

        public GameObject GO { get { return gameObject; } }

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

        public string IDRef { get { return ID; } set { ID = value; } }

        public ChairGroup Group
        {
            get
            {
                GetChairGroup();
                return chairGroup;
            }
        }

        public string OccupantID { get; private set; }

        public void SetOccupantsPreviousPosition(Vector3 vec, string playerID = "")
        {
            for(int i = 0; i < sittingSpots.Count; i++)
            {
                if(sittingSpots[i].occupideBy.Equals(playerID))
                {
                    sittingSpots[i].occupancePreviousPosition = vec;
                    break;
                }
            }
        }

        public Vector3 GetOccupantsPreviousPosition(string playerID = "")
        {
            for (int i = 0; i < sittingSpots.Count; i++)
            {
                if (sittingSpots[i].occupideBy.Equals(playerID))
                {
                    return sittingSpots[i].occupancePreviousPosition;
                }
            }

            return Vector3.zero;
        }

        public IChair MainInteraface
        {
            get
            {
                return this;
            }
        }

        public Vector3 SittingPosition(string playerID = "")
        { 
            Vector3 sitSpot = Vector3.zero;
            IPlayer player = PlayerManager.Instance.GetPlayer(playerID);

            if (Occupancies != null && Occupancies.Count > 0)
            {
                sitSpot = new Vector3(transform.position.x, player.TransformObject.position.y, transform.position.z);
            }

            if (sittingSpots.Count > 0)
            {
                for(int i = 0; i < sittingSpots.Count; i++)
                {
                    if(sittingSpots[i].occupideBy.Equals(playerID))
                    {
                        sitSpot = new Vector3(sittingSpots[i].spot.position.x, sittingSpots[i].spot.position.y, sittingSpots[i].spot.position.z);
                        break;
                    }
                }
            }

            return sitSpot;
        }

        public Vector3 SittingDirection(string playerID = "")
        {
            //works
            if (sittingSpots.Count > 0)
            {
                for (int i = 0; i < sittingSpots.Count; i++)
                {
                    if (sittingSpots[i].occupideBy.Equals(playerID))
                    {
                        return sittingSpots[i].spot.forward;
                    }
                }
            }

            return new Vector3(0, transform.localEulerAngles.y, 0);
        }

        public bool HasSittingSpot
        {
            get
            {
                if (sittingSpots == null || sittingSpots.Count <= 0) return false;

                return true;
            }
        }

        public void SetSittinPoint(int sp, string playerID)
        {
            sittingSpots[sp].occupideBy = playerID;
        }

#if UNITY_EDITOR
        public void EditorSetChairVars(Transform[] sittingSpots)
        {
            this.sittingSpots.Clear();

            for(int i = 0; i < sittingSpots.Length; i++)
            {
                this.sittingSpots.Add(new BenchSpot(sittingSpots[i]));
            }
        }
        private void EditorAddChairVars(Transform sittingSpot)
        {
            this.sittingSpots.Add(new BenchSpot(sittingSpot));
        }

#endif

        private void Start()
        {
            ChairManager.Instance.AddIChairObject(this);

            //need to delete the empty/null sitting spots
            List<int> empty = new List<int>();

            for (int i = 0; i < sittingSpots.Count; i++)
            {
                if(sittingSpots[i] == null || sittingSpots[i].spot == null)
                {
                    empty.Add(i);
                }
            }

            for(int i = 0; i < empty.Count; i++)
            {
                sittingSpots.RemoveAt(i);
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).GetComponent<Lock>())
                    chairLock = transform.GetChild(i).GetComponent<Lock>();
            }

            bool hasCollider = GetComponentsInChildren<Collider>().Length > 0 ? true : false;

            if (!hasCollider)
            {
                gameObject.AddComponent<BoxCollider>();
            }

            if (AppManager.Instance.Settings.playerSettings.addHightLightToChairs)
            {
                if (GetComponent<Highlight>() == null)
                {
                    gameObject.AddComponent<Highlight>();
                }
            }

            if (chairLock != null)
            {
                if (Group.LockChairOnLeave)
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

            if (!chairLock.IsLocked && Group.LockChairOnLeave)
            {
                if (!IsOccupied)
                {
                    while (m_lockTimer < 5.0f)
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

        //functions
        public void Join(IPlayer player)
        {
            if (IsOccupied) return;

            if (player.IsLocal)
            {
                if (m_lockTimerProcess != null)
                {
                    StopCoroutine(m_lockTimerProcess);
                    m_lockTimerProcess = null;
                }

                AppManager.Instance.ToggleVideoChat(false, "");
            }

            //join to group
            if (GetChairGroup())
            {
                chairGroup.Join(player);
            }

            if (Occupancies == null)
            {
                Occupancies = new List<IPlayer>();
            }

            //add occupant
            Occupancies.Add(player);
            OccupantID += player.ID;

            if(Occupancies.Count.Equals(sittingSpots.Count))
            {
                IsOccupied = true;

                foreach (Collider col in transform.GetComponentsInChildren<Collider>(true))
                {
                    col.enabled = false;
                }
            }

            ChairManager.Instance.AmendPlayerToGlobalChairOccupancy(true, player.ID);

            //set up avatar position, rotation and animation
            if (player.IsLocal)
            {
                if (GetComponent<Highlight>())
                {
                    GetComponent<Highlight>().HighlightObject(false);
                    GetComponent<Highlight>().isEnabled = false;
                }

                //find free sit point
                int sitPoint = -1;

                for(int i = 0; i < sittingSpots.Count; i++)
                {
                    if(string.IsNullOrEmpty(sittingSpots[i].occupideBy))
                    {
                        sittingSpots[i].occupideBy = player.ID;
                        sitPoint = i;
                        break;
                    }
                }

                //need to network occupancy for this chair [SEND CHAIR AND GROUP]
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("EVENT_TYPE", "BENCH");
                //data.Add("G", chairGroup.ID);
                data.Add("I", ID);
                data.Add("O", "1");
                data.Add("SP", sitPoint.ToString());
                data.Add("P", player.ActorNumber.ToString());

                MMOManager.Instance.ChangeRoomProperty(ID, data);

                if (player.NavMeshAgentScript != null)
                {
                    player.NavMeshAgentScript.isStopped = true;
                    player.NavMeshAgentScript.enabled = false;
                }

                TransportPlayer(true, player);
                player.Animation.SetBool("Moved", false);

                if (sittingMode.Equals(Chair.SitMode.Chair))
                    ani = "Sit";
                else if (sittingMode.Equals(Chair.SitMode.Floor))
                    ani = "SitFloor";
                else if (sittingMode.Equals(Chair.SitMode.Standing))
                    ani = "Standing";

                player.Animation.SetBool(ani, true);
            }
            else
            {
                //set networkplayer animator to play sit
                MMOPlayer nPlayer = player.MainObject.GetComponent<MMOPlayer>();
                nPlayer.FreezePosition = true;
                nPlayer.FreezeRotation = true;

                if (nPlayer.NavMeshAgentUsed)
                {
                    nPlayer.NavMeshAgentUsed.isStopped = true;
                    nPlayer.NavMeshAgentUsed.enabled = false;
                }

                TransportPlayer(true, player);

                Vector3 labelPos = nPlayer.LabelNameFront.transform.parent.localPosition;
                nPlayer.LabelNameFront.transform.parent.localPosition = new Vector3(labelPos.x, labelPos.y - 0.3f, labelPos.z);
                nPlayer.animator.SetBool("Moved", false);

                if (sittingMode.Equals(Chair.SitMode.Chair))
                    ani = "Sit";
                else if (sittingMode.Equals(Chair.SitMode.Floor))
                    ani = "SitFloor";
                else if (sittingMode.Equals(Chair.SitMode.Standing))
                    ani = "Standing";

                nPlayer.SittingAnimation = ani;

                nPlayer.animator.SetBool(ani, true);
            }
        }

        public void Leave(IPlayer player)
        {
            if (Occupancies.Count <= 0) return;

            IsOccupied = false;

            //leave group
            if (GetChairGroup())
            {
                chairGroup.Leave(player);
            }

            if (Occupancies == null)
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

                int sitPoint = -1;

                for (int i = 0; i < sittingSpots.Count; i++)
                {
                    if (sittingSpots[i].occupideBy.Equals(player.ID))
                    {
                        sitPoint = i;
                        break;
                    }
                }

                //need to network occupancy for this chair [SEND CHAIR AND GROUP]
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("EVENT_TYPE", "BENCH");
                //data.Add("G", chairGroup.ID);
                data.Add("I", ID);
                data.Add("O", "0");
                data.Add("SP", sitPoint.ToString());
                data.Add("P", player.ActorNumber.ToString());

                MMOManager.Instance.ChangeRoomProperty(ID, data);

                player.Animation.SetBool(ani, false);
                player.Animation.Play("Idle", 0, 1.0f);

                ChairManager.Instance.ExitChair();

                TransportPlayer(false, player);

                if (player.NavMeshAgentScript != null)
                {
                    player.NavMeshAgentScript.enabled = true;
                }

                if (Group.LockChairOnLeave)
                {
                    if (chairLock != null)
                    {
                        chairLock.LockThis();
                    }
                }
            }
            else
            {
                //set networkplayer animator to play idle
                MMOPlayer nPlayer = player.MainObject.GetComponent<MMOPlayer>();

                TransportPlayer(false, player);

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

            Occupancies.Remove(player);

            for (int i = 0; i < sittingSpots.Count; i++)
            {
                if (sittingSpots[i].occupideBy.Equals(player.ID))
                {
                    sittingSpots[i].occupideBy = "";
                }
            }

            OccupantID = OccupantID.Replace(player.ID, "");
        }

        private void TransportPlayer(bool join, IPlayer player)
        {
            if (join)
            {
                player.TransformObject.GetComponent<Collider>().enabled = false;

                SetOccupantsPreviousPosition(new Vector3(player.TransformObject.position.x, player.TransformObject.position.y, player.TransformObject.position.z), player.ID);
                player.TransformObject.position = SittingPosition(player.ID);

                //works
                if (sittingSpots.Count > 0)
                {
                    player.TransformObject.forward = SittingDirection(player.ID);
                }
                else
                {
                    player.TransformObject.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
                }

                if (player.IsLocal)
                {
                    chairGroup.Cam.SetActive(true);
                    player.MainCamera.gameObject.SetActive(false);
                }
            }
            else
            {
                player.TransformObject.position = GetOccupantsPreviousPosition(player.ID);
                SetOccupantsPreviousPosition(Vector3.zero, player.ID);
                player.TransformObject.GetComponent<Collider>().enabled = true;

                if (player.IsLocal)
                {
                    chairGroup.Cam.SetActive(false);
                    player.MainCamera.gameObject.SetActive(true);
                }
            }
        }

        public void OnPlayerDisconnect(string id)
        {
            IsOccupied = false;

            IPlayer player = PlayerManager.Instance.GetPlayer(id);
            Occupancies.Remove(player);
            
            for(int i = 0; i < sittingSpots.Count; i++)
            {
                if(sittingSpots[i].occupideBy.Equals(id))
                {
                    sittingSpots[i].occupideBy = "";
                }

            }

            OccupantID = OccupantID.Replace(id, "");

            if (GetChairGroup())
            {
                chairGroup.OnPlayerDisconnect(id);
            }
        }

        public bool CanUserControl(string user)
        {
            return CanUserControlThis(user);        
        }

        public void VideoChat(bool join)
        {
            //video Message
            if (!Group.VideoUsed && join) return;

            if (Group.StreamingMode.Equals(ChairGroup.VideoStream.LiveStream) && join && !liveStreamMode.Equals(Chair.LiveStreamMode.None))
            {
                if (Group is ConferenceChairGroup)
                {
                    if (((ConferenceChairGroup)Group).Owner.Equals(OccupantID))
                    {
                        liveStreamMode = Chair.LiveStreamMode.Host;
                        m_LiveStreamRole = LiveStreamRole.host;
                    }
                    else
                    {
                        liveStreamMode = Chair.LiveStreamMode.Audience;
                        m_LiveStreamRole = LiveStreamRole.audience;
                    }
                }
                else
                {
                    m_LiveStreamRole = liveStreamMode.Equals(Chair.LiveStreamMode.Host) ? LiveStreamRole.host : LiveStreamRole.audience;
                }

                //request live stream mode
                WebClientCommsManager.Instance.RequestLivestream(m_LiveStreamRole, CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString() + "_" + chairGroup.ID, true, PlayerManager.Instance.GetLocalPlayer().NickName);
            }
            else
            {
                if (Group.StreamingMode.Equals(ChairGroup.VideoStream.LiveStream))
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

        public void ChangeGroupCamera()
        {
            Group.ChangeCamera();
        }

        public void UpdateLiveStreamRole()
        {
            if (Group.StreamingMode.Equals(ChairGroup.VideoStream.LiveStream))
            {
                if (Group is ConferenceChairGroup)
                {
                    if (((ConferenceChairGroup)Group).Owner.Equals(OccupantID))
                    {
                        liveStreamMode = Chair.LiveStreamMode.Host;
                        m_LiveStreamRole = LiveStreamRole.host;
                    }
                    else
                    {
                        liveStreamMode = Chair.LiveStreamMode.Audience;
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
        /// Returns the chair group
        /// </summary>
        /// <returns></returns>
        private bool GetChairGroup()
        {
            if (chairGroup == null)
            {
                chairGroup = GetComponentInParent<ChairGroup>();
            }

            return chairGroup;
        }

        [System.Serializable]
        private class BenchSpot
        {
            public Transform spot;
            [HideInInspector]
            public string occupideBy = "";
            [HideInInspector]
            public Vector3 occupancePreviousPosition;

            public BenchSpot(Transform t)
            {
                spot = t;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Bench), true), CanEditMultipleObjects]
        public class Bench_Editor : UniqueID_Editor
        {
            private Bench benchScript;

            private void OnEnable()
            {
                GetBanner();
                Initialise();

                if (Application.isPlaying) return;

                if (benchScript.GetComponentInParent<ChairGroup>() == null)
                {
                    base.Initialise();

                    //this will need to add/update the chair from standalone chair IOobjects
                }
                else
                {
                    benchScript.GetComponentInParent<ChairGroup>().UpdateIOChairSettings();
                }

                if (BenchConfiguratorWindow.IsOpen)
                {
                    BenchConfiguratorWindow window = (BenchConfiguratorWindow)EditorWindow.GetWindow(typeof(BenchConfiguratorWindow));
                    window.SetBench(benchScript);
                }
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (benchScript != null)
                {
                    if (benchScript.GetComponentInParent<ChairGroup>() == null)
                    {
                        //this will need to remove the chair from standalone chair IOobjects
                    }
                    else
                    {
                        benchScript.GetComponentInParent<ChairGroup>().UpdateIOChairSettings();
                    }
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                if (benchScript.GetComponentInParent<ChairGroup>() == null)
                {
                    DisplayID();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Bench Setup", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("liveStreamMode"), true);
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("sittingSpots"), true);

                EditorGUILayout.LabelField("Sitting Spots", EditorStyles.miniBoldLabel);
                //loop through all the siiting spots
                for(int i = 0; i < benchScript.sittingSpots.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sittingSpots").GetArrayElementAtIndex(i).FindPropertyRelative("spot"), true);

                    if (GUILayout.Button("Select"))
                    {
                        if (benchScript.sittingSpots[i].spot != null)
                        {
                            Selection.activeTransform = benchScript.sittingSpots[i].spot;
                        }
                    }

                    if (GUILayout.Button("Remove"))
                    {
                        if(benchScript.sittingSpots[i].spot != null)
                        {
                            DestroyImmediate(benchScript.sittingSpots[i].spot.gameObject);
                        }

                        benchScript.sittingSpots.RemoveAt(i);
                        GUIUtility.ExitGUI();
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space();

                if (serializedObject.FindProperty("sittingSpots").arraySize <= 0)
                {
                    if (GUILayout.Button("Create Sitting Spot"))
                    {
                        GameObject sittingPoint = new GameObject();
                        sittingPoint.transform.SetParent(benchScript.gameObject.transform);
                        sittingPoint.name = "SittingPoint";
                        sittingPoint.transform.localPosition = new Vector3(0, 1, 0);
                        sittingPoint.transform.localScale = Vector3.one;
                        sittingPoint.transform.localEulerAngles = new Vector3(0, 0, 0);

                        benchScript.EditorAddChairVars(sittingPoint.transform);

                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    if (GUILayout.Button("Create Additional Sitting Spot"))
                    {
                        GameObject sittingPoint = new GameObject();
                        sittingPoint.transform.SetParent(benchScript.gameObject.transform);
                        sittingPoint.name = "SittingPoint";
                        sittingPoint.transform.localPosition = benchScript.sittingSpots[0].spot.localPosition;

                        sittingPoint.transform.localScale = Vector3.one;
                        sittingPoint.transform.localEulerAngles = benchScript.sittingSpots[0].spot.localEulerAngles;

                        benchScript.EditorAddChairVars(sittingPoint.transform);

                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Open Configurator"))
                {
                    OpenConfigurator(benchScript);
                }

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(benchScript);
                }
            }

            protected override void Initialise()
            {
                benchScript = (Bench)target;
            }

            private void OpenConfigurator(Bench bench)
            {
                BenchConfiguratorWindow window = (BenchConfiguratorWindow)EditorWindow.GetWindow(typeof(BenchConfiguratorWindow));
                window.SetBench(bench);
                window.maxSize = new Vector2(800f, 400f);
                window.minSize = window.maxSize;
                window.Show();
            }

            public class BenchConfiguratorWindow : EditorWindow
            {
                private Bench m_bench;
                private Transform avatar;
                private AppSettings m_settings;
                private SerializedObject m_asset;
                private int m_selectedSittingSpot = 0;
                private Vector2 scrollPos;
                private Color m_active = Color.cyan;
                private Color m_normal = Color.white;

                public static bool IsOpen { get; private set; }

                public void SetBench(Bench bench)
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

                    m_bench = bench;
                    m_asset = new SerializedObject(m_bench);

                    if (avatar != null)
                    {
                        DestroyImmediate(avatar.gameObject);
                        CreateAvatar(0);
                    }

                    m_selectedSittingSpot = 0;

                    SelectSittingSpot(m_selectedSittingSpot);
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

                                if (m_bench.sittingMode.Equals(Chair.SitMode.Chair))
                                    aniName = "Sit";
                                else if (m_bench.Equals(Chair.SitMode.Floor))
                                    aniName = "SitFloor";
                                else if (m_bench.Equals(Chair.SitMode.Standing))
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

                    EditorGUILayout.LabelField("BENCH CONFIGURATOR", EditorStyles.boldLabel);
                    EditorGUILayout.Space();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Bench:", EditorStyles.boldLabel, GUILayout.Width(100));
                    EditorGUILayout.LabelField(m_bench.gameObject.name.ToString(), GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                    if (m_asset.FindProperty("sittingSpots").arraySize <= 0)
                    {
                        if (GUILayout.Button("Create Sitting Spot"))
                        {
                            GameObject sittingPoint = new GameObject();
                            sittingPoint.transform.SetParent(m_bench.gameObject.transform);
                            sittingPoint.name = "SittingPoint";
                            sittingPoint.transform.localPosition = new Vector3(0, 1, 0);
                            sittingPoint.transform.localScale = Vector3.one;
                            sittingPoint.transform.localEulerAngles = new Vector3(0, 0, 0);

                            m_bench.EditorAddChairVars(sittingPoint.transform);
                            m_asset.FindProperty("sittingSpots").GetArrayElementAtIndex(0).FindPropertyRelative("spot").objectReferenceValue = sittingPoint.transform;

                            m_selectedSittingSpot = 0;

                            SelectSittingSpot(m_selectedSittingSpot);

                            if (avatar != null)
                            {
                                avatar.transform.SetParent((Transform)m_bench.sittingSpots[0].spot);
                                avatar.transform.localPosition = Vector3.zero;
                                avatar.transform.eulerAngles = m_bench.sittingSpots[0].spot.eulerAngles + m_bench.avatarRotation;
                            }

                            GUIUtility.ExitGUI();
                        }
                    }
                    else
                    {

                        //need to draw all sitting spots and allow to select
                        EditorGUILayout.LabelField("Sitting Spots", EditorStyles.miniBoldLabel);
                        //loop through all the siiting spots
                        for (int i = 0; i < m_bench.sittingSpots.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Sitting Spot:", EditorStyles.boldLabel, GUILayout.ExpandWidth(false), GUILayout.Width(100));
                            EditorGUILayout.LabelField(m_bench.sittingSpots[i].spot.name, GUILayout.ExpandWidth(false), GUILayout.ExpandWidth(true));

                            if (m_selectedSittingSpot.Equals(i))
                            {
                                GUI.backgroundColor = m_active;
                            }
                            else
                            {
                                GUI.backgroundColor = m_normal;
                            }

                            if (GUILayout.Button("Select"))
                            {
                                if (avatar != null)
                                {
                                    DestroyImmediate(avatar.gameObject);

                                    CreateAvatar(i);
                                }

                                SelectSittingSpot(i);
                                GUIUtility.ExitGUI();
                            }

                            GUI.backgroundColor = m_normal;

                            if (GUILayout.Button("Remove"))
                            {
                                if (m_bench.sittingSpots[i].spot != null)
                                {
                                    if (avatar != null)
                                    {
                                        if (avatar.parent.Equals(m_bench.sittingSpots[i].spot))
                                        {
                                            DestroyImmediate(avatar.gameObject);
                                        }
                                    }

                                    DestroyImmediate(m_bench.sittingSpots[i].spot.gameObject);
                                }

                                m_bench.sittingSpots.RemoveAt(i);
                                GUIUtility.ExitGUI();
                                break;
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    EditorGUILayout.Space();

                    if (m_bench.sittingSpots.Count <= 0)
                    {
                        if (GUILayout.Button("Create Sitting Spot"))
                        {
                            GameObject sittingPoint = new GameObject();
                            sittingPoint.transform.SetParent(m_bench.gameObject.transform);
                            sittingPoint.name = "SittingPoint";
                            sittingPoint.transform.localPosition = new Vector3(0, 1, 0);
                            sittingPoint.transform.localScale = Vector3.one;
                            sittingPoint.transform.localEulerAngles = new Vector3(0, 0, 0);

                            m_bench.EditorAddChairVars(sittingPoint.transform);
                            SelectSittingSpot(0);
                            GUIUtility.ExitGUI();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Create Additional Sitting Spot"))
                        {
                            GameObject sittingPoint = new GameObject();
                            sittingPoint.transform.SetParent(m_bench.gameObject.transform);
                            sittingPoint.name = "SittingPoint";
                            sittingPoint.transform.localPosition = m_bench.sittingSpots[0].spot.localPosition;

                            sittingPoint.transform.localScale = Vector3.one;
                            sittingPoint.transform.localEulerAngles = m_bench.sittingSpots[0].spot.localEulerAngles;

                            m_bench.EditorAddChairVars(sittingPoint.transform);
                            SelectSittingSpot(m_bench.sittingSpots.Count - 1);
                            GUIUtility.ExitGUI();
                        }
                    }

                    EditorGUILayout.EndScrollView();

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
                            CreateAvatar(m_selectedSittingSpot);

                            GUIUtility.ExitGUI();
                        }
                    }

                    if (GUI.changed)
                    {
                        if (m_asset != null) m_asset.ApplyModifiedProperties();

                        if (m_bench != null)
                        {
                            EditorUtility.SetDirty(m_bench);
                        }

                        EditorUtility.SetDirty(this);
                    }

                }

                private void CreateAvatar(int spot)
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
                    if (m_bench.sittingSpots.Count > 0)
                    {
                        bot.transform.SetParent((Transform)m_bench.sittingSpots[spot].spot);
                        bot.transform.localPosition = Vector3.zero;
                        bot.transform.eulerAngles = m_bench.sittingSpots[spot].spot.eulerAngles + m_bench.avatarRotation;
                    }
                    else
                    {
                        //use the transform position value
                        bot.transform.SetParent(m_bench.gameObject.transform);
                        bot.transform.localPosition = new Vector3(0, 0, 1);
                        bot.transform.localEulerAngles = new Vector3(m_bench.gameObject.transform.localEulerAngles.x * -1, 0, 0);
                    }

                    avatar = bot.transform;

                    SelectSittingSpot(spot);
                }

                private void SelectSittingSpot(int spot)
                {
                    m_selectedSittingSpot = spot;
                    Transform target;

                    if (m_bench.sittingSpots.Count > 0)
                    {
                        target = m_bench.sittingSpots[spot].spot;
                    }
                    else
                    {
                        target = m_bench.transform;
                    }

                    Selection.activeTransform = target;
                }
            }
        }
#endif
    }
}
