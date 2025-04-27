using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ChairGroup : UniqueID, IChair
    {
        [SerializeField]
        protected GameObject groupCamera;

        [SerializeField]
        protected GameObject[] additinalCameras;

        [SerializeField]
        protected ChairGroupIOObject settings = new ChairGroupIOObject();

        public Lock chairLock;
        public bool useTrigger;
        public bool inTrigger;

        private Coroutine m_triggerDelay;

#if UNITY_EDITOR
        public void EditorSetGroupCamera(GameObject cam)
        {
            groupCamera = cam;
        }

        public void EditorSetGroupLockPassword(string password)
        {
            settings.lockSettings.password = password;
        }
#endif

        /// <summary>
        /// Access to the groups camera
        /// </summary>
        public GameObject Cam
        {
            get
            {
                return groupCamera;
            }
        }

        /// <summary>
        /// Global access to the occupied state of this group
        /// </summary>
        public bool IsOccupied
        {
            get
            {
                IChairObject[] all = GetComponentsInChildren<IChairObject>();
                return (all.Count(x => x.ChairOccupied) == all.Length) ? true : false;
            }
        }

        /// <summary>
        /// Returns all the chair within this group
        /// </summary>
        public List<IChairObject> AllChairs
        {
            get
            {
                return GetComponentsInChildren<IChairObject>().ToList();
            }
        }

        /// <summary>
        /// Global access to the occupants of this group
        /// </summary>
        public List<IPlayer> Occupancies { get; protected set; }

        /// <summary>
        /// private look up table for player ID, occupant index
        /// </summary>
        protected Dictionary<string, int> m_lookup = new Dictionary<string, int>();

        public bool VideoUsed
        { 
            get 
            { 
                return settings.videoStream.Equals(VideoStream.Disabled) ? false : true ; 
            } 
        }

        public VideoStream StreamingMode
        {
            get
            {
                return settings.videoStream;
            }
        }

        public bool HasAdditionalCameras
        {
            get
            {
                return additinalCameras.Length > 0;
            }
        }

        protected int m_currentCamera = -1;

        protected virtual void Start()
        {
            if (!AppManager.IsCreated) return;

            if (!AppManager.Instance.Instances.ignoreIObjectSettings)
            {
                //need to get the settings from the instances script then update the settings
                foreach (AppInstances.IOObjectChairGroupHandler setting in AppManager.Instance.Instances.ioChairGroupObjects)
                {
                    if (setting.referenceID.Equals(GetRawID()))
                    {
                        ApplySettings(setting.settings);
                        break;
                    }
                }
            }

            Initialize();
        }

        protected void Initialize()
        {
            IChairObject[] all = GetComponentsInChildren<IChairObject>();

            if (GetComponent<BoxCollider>() != null)
                useTrigger = GetComponent<BoxCollider>().isTrigger;

            for(int i = 0; i < transform.childCount; i++)
            {
                if(transform.GetChild(i).GetComponent<Lock>() != null)
                {
                    chairLock = transform.GetChild(i).GetComponent<Lock>();
                    break;
                }
            }

            CheckLocks(all);

            for (int i = 0; i < all.Length; i++)
            {
                all[i].IDRef = ID + "[" + all[i].GO.transform.GetSiblingIndex() + "]";
            }

            groupCamera.SetActive(false);

            additinalCameras.ToList().ForEach(x => x.SetActive(false));


            StartCoroutine(CheckOverrideSettings(all));
        }

        private IEnumerator CheckOverrideSettings(IChairObject[] all)
        {
            yield return new WaitForEndOfFrame();

            if (!IsLockedType())
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].GO.GetComponentInChildren<Lock>() != null)
                    {
                        Lock l = all[i].GO.GetComponentInChildren<Lock>();
                        l.OverrideSettings(settings.lockSettings.useDataAPIPassword, settings.lockSettings.password, settings.lockSettings.displayType);
                        l.IsLocked = true;
                        l.IsNetworked = false;
                    }
                }
            }
            else
            {
                if (chairLock != null)
                {
                    chairLock.IsLocked = true;
                    chairLock.IsNetworked = true;
                    chairLock.OverrideSettings(settings.lockSettings.useDataAPIPassword, settings.lockSettings.password, settings.lockSettings.displayType);
                }
            }
        }

        private void CheckLocks(IChairObject[] chairs)
        {
            if(chairLock != null || useTrigger)
            {
                for (int i = 0; i < chairs.Length; i++)
                {
                    if (chairs[i].ChairLock != null)
                    {
                        chairs[i].ChairLock.gameObject.SetActive(false);
                    }
                }
            } 
        }

        public bool IsLockedType()
        {
            for(int i = 0; i < transform.childCount; i++)
            {
                if(transform.GetChild(i).GetComponent<Lock>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool LockChairOnLeave
        {
            get
            {
                return settings.lockChairOnLeave;
            }
        }

        public string GroupName
        {
            get
            {
                return settings.groupName;
            }
        }

        /// <summary>
        /// Called when a player joins the group
        /// </summary>
        /// <param name="player"></param>
        public virtual void Join(IPlayer player)
        {
            if (IsOccupied) return;

            if (Occupancies == null)
            {
                Occupancies = new List<IPlayer>();
            }

            //add occupant to group
            Occupancies.Add(player);
            m_lookup.Add(player.ID, Occupancies.IndexOf(player));

            AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Enter, AnalyticReference);
        }

        /// <summary>
        /// Called if a player leaves the group
        /// </summary>
        /// <param name="player"></param>
        public virtual void Leave(IPlayer player)
        {
            if (Occupancies == null)
            {
                Occupancies = new List<IPlayer>();
            }

            m_currentCamera = -1;

            //remove occupant from group
            Occupancies.Remove(player);
            m_lookup.Remove(player.ID);
        }

        /// <summary>
        /// Called when a player disconnects from the room
        /// </summary>
        /// <param name="id"></param>
        public virtual void OnPlayerDisconnect(string id)
        {
            if (m_lookup.ContainsKey(id))
            {
                int index = m_lookup[id];
                m_lookup.Remove(id);
                Occupancies.RemoveAt(index);
            }
        }

        /// <summary>
        /// Called to change this groups main camera (local only)
        /// </summary>
        public virtual void ChangeCamera()
        {
            if (additinalCameras.Length > 0)
            {
                m_currentCamera += 1;

                if (m_currentCamera > additinalCameras.Length - 1)
                {
                    groupCamera.SetActive(true);
                    additinalCameras[m_currentCamera - 1].SetActive(false);
                    m_currentCamera = -1;
                }
                else
                {
                    additinalCameras[m_currentCamera].SetActive(true);

                    if (m_currentCamera - 1 < 0)
                    {
                        groupCamera.SetActive(false);
                    }
                    else
                    {
                        additinalCameras[m_currentCamera - 1].SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// Seat the player in a random free chair in this chair group.
        /// </summary>
        public void AutoSeat()
        {
            List<IChairObject> freeChairs = new List<IChairObject>();

            for (int i = 0; i < AllChairs.Count; i++)
            {
                if (!AllChairs[i].ChairOccupied)
                    freeChairs.Add(AllChairs[i]);
            }

            if (freeChairs.Count > 0)
            {
                ChairManager.Instance.OccupyChiar(freeChairs[Random.Range(0, freeChairs.Count)], true);
                inTrigger = true;
            }
            else
                PopupManager.instance.ShowHint("Chair Group", "This group is full", 0.0f);
        }

        void OnTriggerEnter(Collider other)
        {
            if (ProductManager.Instance.isHolding || ItemManager.Instance.IsHolding) return;

            if (other.CompareTag("Player"))
            {
                if (other.GetComponent<MMOPlayer>().view.IsMine && !inTrigger)
                {
                    if(m_triggerDelay != null)
                    {
                        StopCoroutine(m_triggerDelay);
                        m_triggerDelay = null;
                    }

                    AutoSeat();
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (ProductManager.Instance.isHolding || ItemManager.Instance.IsHolding) return;

            if (other.CompareTag("Player"))
            {
                if (other.GetComponent<MMOPlayer>().view.IsMine && inTrigger)
                {
                    m_triggerDelay = StartCoroutine(DelayTrigger());
                }
            }
        }

        private IEnumerator DelayTrigger()
        {
            float delay = (AppManager.Instance.Settings.playerSettings.chairFadePauseTime + 2.0f) * 2;
            yield return new WaitForSeconds(delay);
            
            inTrigger = false;
        }

        [System.Serializable]
        public enum VideoStream { VideoChat, LiveStream, Disabled }

        [System.Serializable]
        public class ChairGroupIOObject : IObjectSetting
        {
            public string groupName = "ChairGroup";
            public VideoStream videoStream = VideoStream.VideoChat;
            public LockManager.LockSetting lockSettings = new LockManager.LockSetting();
            public bool lockChairOnLeave = true;

            public string Type { get; set; }
            public List<ChairStreamCache> StreamCache { get; set; }

        }

        [System.Serializable]
        public class ChairStreamCache
        {
            public string id;
            public Chair.LiveStreamMode mode;
            public string GO;

            public ChairStreamCache(string id, string GO, Chair.LiveStreamMode mode)
            {
                this.id = id;
                this.GO = GO;
                this.mode = mode;
            }
        }

        public override IObjectSetting GetSettings(bool remove = false)
        {
            if (!remove)
            {
                IObjectSetting baseSettings = base.GetSettings();
                settings.adminOnly = baseSettings.adminOnly;
                settings.prefix = baseSettings.prefix;
                settings.controlledByUserType = baseSettings.controlledByUserType;
                settings.userTypes = baseSettings.userTypes;

                settings.GO = gameObject.name;
                IChairObject[] all = GetComponentsInChildren<IChairObject>();

                settings.Type = IsLockedType() ? "Locked Group" : GetComponent<Collider>() != null ? "Trigger Group" : "Standard Group";

                settings.StreamCache = new List<ChairStreamCache>();
                for (int i = 0; i < all.Length; i++)
                {
                    settings.StreamCache.Add(new ChairStreamCache(all[i].IDRef, all[i].GO.name, all[i].StreamMode));
                }
            }

            settings.ID = id;
            return settings;
        }

        protected override void ApplySettings(IObjectSetting settings)
        {
            if (settings == null) return;

            base.ApplySettings(settings);

            this.settings.videoStream = ((ChairGroupIOObject)settings).videoStream;
            this.settings.lockSettings = ((ChairGroupIOObject)settings).lockSettings;

            IChairObject[] all = GetComponentsInChildren<IChairObject>();

            for (int i = 0; i < all.Length; i++)
            {
                if(((ChairGroupIOObject)settings).StreamCache != null)
                {
                    ChairStreamCache cache = ((ChairGroupIOObject)settings).StreamCache.FirstOrDefault(x => !string.IsNullOrEmpty(x.id) && x.id.Equals(all[i]));

                    if (cache != null)
                    {
                        all[i].StreamMode = cache.mode;
                    }
                }
            }
        }

#if UNITY_EDITOR
        public virtual void UpdateIOChairSettings()
        {
            AppInstances m_instances;
            AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");

            if (appReferences != null)
            {
                m_instances = appReferences.Instances;
            }
            else
            {
                m_instances = Resources.Load<AppInstances>("ProjectAppInstances");
            }

            //called to update the chair settings via the chair script
            if (m_instances != null && (this is ConferenceChairGroup) == false)
            {
                m_instances.AddIOObject(ID, GetSettings(true));
            }
        }

        [CustomEditor(typeof(ChairGroup), true), CanEditMultipleObjects]
        public class ChairGroup_Editor : UniqueID_Editor
        {
            private ChairGroup chairGroupScript;

            private void OnEnable()
            {
                GetBanner();
                Initialise();
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (m_instances != null && (chairGroupScript is ConferenceChairGroup) == false)
                {
                    m_instances.RemoveIOObject(chairGroupScript.GetSettings(true));
                }
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                serializedObject.Update();

                if(chairGroupScript is ConferenceChairGroup)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Lock Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("useDataAPIPassword"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("password"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("displayType"), true);
                }
                else
                {
                    if(chairGroupScript.gameObject.GetComponent<BoxCollider>() == null)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Lock Settings", EditorStyles.boldLabel);

                        if (chairGroupScript.IsLockedType())
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("useDataAPIPassword"), true);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("password"), true);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("displayType"), true);
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("password"), true);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockChairOnLeave"), true);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("displayType"), true);

                        }
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Chair Group Setup", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("groupName"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("groupCamera"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("additinalCameras"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("videoStream"), true);

                if (GUI.changed || GONameChanged())
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(chairGroupScript);

                    if (Application.isPlaying) return;

                    if (m_instances != null && (chairGroupScript is ConferenceChairGroup) == false)
                    {
                        m_instances.AddIOObject(chairGroupScript.ID, chairGroupScript.GetSettings());
                    }
                }
            }

            protected override void Initialise()
            {
                base.Initialise();

                chairGroupScript = (ChairGroup)target;

                if (Application.isPlaying) return;

                if (m_instances != null && (chairGroupScript is ConferenceChairGroup) == false)
                {
                    //need to get the settings from the instances script then update the settings
                    foreach (AppInstances.IOObjectChairGroupHandler setting in m_instances.ioChairGroupObjects)
                    {
                        if (setting.referenceID.Equals(chairGroupScript.ID))
                        {
                            chairGroupScript.ApplySettings(setting.settings);
                            break;
                        }
                    }

                    m_instances.AddIOObject(chairGroupScript.ID, chairGroupScript.GetSettings());
                }
            }
        }
#endif
    }
}
