using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ConferenceChairGroup : ChairGroup
    {
        [SerializeField]
        private Door entranceDoor;

        [SerializeField]
        private Lock doorLock;

        [SerializeField]
        private GameObject chairTrigger;

        [SerializeField]
        private Transform UIObject;

        [SerializeField]
        private ConferenceChairGroupIOObject conferenceSettings = new ConferenceChairGroupIOObject();

        [SerializeField]
        private Image availabilityImage;

       
        private Color availableColor = Color.green;
        private Color unavailableColor = Color.red;

        [SerializeField]
        private TMPro.TextMeshProUGUI instructionText;

        [SerializeField]
        private Transform[] endPoints;

        [SerializeField]
        private GameObject[] contentLoaders;

        private float m_closeDoorTimer = 0.0f;
        private bool m_TimerOn = false;
        private List<string> m_contentUploadURLs = new List<string>();

        /// <summary>
        /// Action to subscribe to when this conference is claimed
        /// </summary>
        public System.Action OnClaimed { get; set; }
        /// <summary>
        /// Action to subscribe to when this conference is unclaimed
        /// </summary>
        public System.Action OnUnclaimed { get; set; }

        /// <summary>
        /// Access to the owner of this conference
        /// </summary>
        public IPlayer Owner
        {
            get;
            set;
        }

        public string CurrentUploadedFile
        {
            get;
            set;
        }

        public ScreenContentDisplayType ContentDisplayType
        {
            get
            {
                return conferenceSettings.contentUploadDisplay;
            }
        }

        public GameObject[] ContentLoaders
        {
            get
            {
                return contentLoaders;
            }
        }

        public List<string> ContentUploadURLs
        {
            get
            {
                return m_contentUploadURLs;
            }
        }

        public ScreenContentPrivacy ContentDisplayMode
        {
            get
            {
                return conferenceSettings.contentUploadPrivacy;
            }
        }

        /// <summary>
        /// Find out if occupancies contains playerID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool OccupanciesContainsID(string id)
        {
            return m_lookup.ContainsKey(id);
        }

        /// <summary>
        /// Read only access to this conference password
        /// </summary>
        public string Password
        {
            get
            {
                return conferenceSettings.password;
            }
        }

        /// <summary>
        /// Read only access to the claimed state of this conferece
        /// </summary>
        public bool IsClaimed
        {
            get;
            private set;
        }

        public bool ResetOnUnClaim
        {
            get;
            set;
        }

        /// <summary>
        /// Read only access to the lock used for this conference
        /// </summary>
        public Lock DoorLock
        {
            get
            {
                return doorLock;
            }
        }

        private string m_disconnectOwnerID = "";

#if UNITY_EDITOR
        public void EditorSetContentLoaders(GameObject[] loaders)
        {
            contentLoaders = loaders;
        }

        public void EditorSetTrigger(GameObject trigger)
        {
            chairTrigger = trigger;
        }

        public void EditorSetDoor(Door door)
        {
            entranceDoor = door;
        }

        public void EditorSetLock(Lock dLock)
        {
            doorLock = dLock;
        }

        public void EditorSetUiObject(Transform objUI)
        {
            UIObject = objUI;
        }

        public void EditorSetInstruction(TMPro.TextMeshProUGUI instruct)
        {
            instructionText = instruct;
        }

        public void EditorSetAvailablity(Image img)
        {
            availabilityImage = img;
        }
#endif

        protected override void Awake()
        {
            if (!AppManager.IsCreated) return;

            base.Awake();

            availableColor = AppManager.Instance.Settings.playerSettings.conferenceAvailableColor;
            unavailableColor = AppManager.Instance.Settings.playerSettings.conferenceUnavailableColor;

            //subscribe to lock
            doorLock.IsNetworked = false;
            doorLock.OverrideSettings(settings.lockSettings.useDataAPIPassword, settings.lockSettings.password, settings.lockSettings.displayType);
            doorLock.OnLock += UnClaim;
            doorLock.OnUnlock += Claim;
            doorLock.OnPasswordChange += SetPassword;

            doorLock.transform.SetParent(entranceDoor.transform);
            UIObject.transform.SetParent(entranceDoor.transform);

            //if there is a trigger, add component and set event
            if(chairTrigger != null)
            {
                ChairGroupTrigger trigger = chairTrigger.GetComponentInChildren<ChairGroupTrigger>(true);

                if (trigger == null)
                {
                   trigger = chairTrigger.AddComponent<ChairGroupTrigger>();
                }

                trigger.OnTriggerEvent += OnPlayerEnteredTrigger;
            }

            if(CoreManager.Instance.IsOffline)
            {
                DoorLock.transform.localScale = Vector3.zero;

                if (availabilityImage)
                {
                    availabilityImage.color = unavailableColor;
                }
            }
            else
            {
                if (availabilityImage)
                {
                    availabilityImage.color = IsOccupied ? unavailableColor : availableColor;
                }
            }
        }

        protected override void Start()
        {
            if (!AppManager.IsCreated) return;

            if (!AppManager.Instance.Instances.ignoreIObjectSettings)
            {
                //need to get the settings from the instances script then update the settings
                foreach (AppInstances.IOObjectChairGroupHandler setting in AppManager.Instance.Instances.ioChairGroupObjects)
                {
                    if (setting.referenceID.Equals(GetRawID()))
                    {
                        ApplySettings(setting.conferenceSettings);
                        break;
                    }
                }
            }

            Initialize();
            entranceDoor.RaycastIgnored = true;

            if (instructionText)
            {
                instructionText.text = conferenceSettings.unclaimedInstruction;
            }

            if (chairTrigger)
            {
                //ensure all chairs colliders are triggers if there is conference trigger
                AllChairs.ForEach(x => x.GO.GetComponent<Collider>().isTrigger = true);
            }

            if(conferenceSettings.endType.Equals(EndPointType.Generate))
            {
                //identity is the doors localposition is open/below/left/right of the center of the conference room


                float direction = 0;

                int count = 0;

                for(int i = 0; i < AllChairs.Count; i++)
                {
                    if(AllChairs[i].GO.GetComponent<Bench>() != null)
                    {
                        count += AllChairs[i].GO.GetComponent<Bench>().CountSittingPoints;
                    }
                    else
                    {
                        count++;
                    }
                }

                endPoints = new Transform[count];

                //need to create points outside the room so that when the meeting is ended, all players jump to that position
                for (int i = 0; i < count; i++)
                {
                    GameObject point = new GameObject();
                    point.name = "Point_" + id + "_" + i.ToString();
                    point.transform.parent = transform;
                    point.transform.localEulerAngles = entranceDoor.transform.localEulerAngles;

                    float n = 0;

                    if(i == 0)
                    {
                        n = 0;
                        direction += 1.2f;
                    }
                    else
                    {
                        if ((i % 2) > 0)
                        {
                            n = 0 + direction;
                        }
                        else
                        {
                            n = 0 - direction;
                            direction += 1.2f;
                        }
                    }

                    Vector3 pos;

                    if(conferenceSettings.placement.Equals(PointDirection.Above))
                    {
                        pos = entranceDoor.transform.position - entranceDoor.transform.TransformDirection(new Vector3(n, 0, conferenceSettings.offset));
                    }
                    else if (conferenceSettings.placement.Equals(PointDirection.Below))
                    {
                        pos = entranceDoor.transform.position + entranceDoor.transform.TransformDirection(new Vector3(n, 0, conferenceSettings.offset));
                    }
                    else if (conferenceSettings.placement.Equals(PointDirection.Right))
                    {
                        pos = entranceDoor.transform.position - entranceDoor.transform.TransformDirection(new Vector3(conferenceSettings.offset, 0, n));
                    }
                    else
                    {
                        pos = entranceDoor.transform.position + entranceDoor.transform.TransformDirection(new Vector3(conferenceSettings.offset, 0, n));
                    }


                    point.transform.position = new Vector3(pos.x, 0.0f, pos.z);
                    endPoints[i] = point.transform;
                }
            }
            else
            {
                if(endPoints.Length < AllChairs.Count)
                {
                    Debug.Log("ConferecenceChairGroup [" + ID + "]: Not enough endpoints to match chair numbers!");
                }
            }

            for (int i = 0; i < contentLoaders.Length; i++)
            {
                contentLoaders[i].transform.localScale = Vector3.zero;
            }
        }

        private void Update()
        {
            if (Owner == null) return;

            //if true then close door/lock conference after 5 seconds if not claimed
            if(m_TimerOn)
            {
                if (m_closeDoorTimer < 5.0f)
                {
                    m_closeDoorTimer += Time.deltaTime;
                }
                else
                {
                    m_TimerOn = false;
                    CloseDoor();
                    Owner = null;
                    doorLock.LockThis();
                }
            }
        }

        public override void Join(IPlayer player)
        {
            base.Join(player);

            //if player joins then add the player to the list (only if list is active)
            if(Owner != null)
            {
                if (Owner.ID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                {
                    UserList uList = PlayerManager.Instance.GetUserList("Layout_Players");
                    uList.AddPlayer(MMOManager.Instance.GetPlayerByUserID(player.ID));
                }
            }
        }

        public override void Leave(IPlayer player)
        {
            base.Leave(player);

            //if player leaves then remove the player to the list (only if list is active)
            if (Owner != null)
            {
                if (Owner.ID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                {
                    UserList uList = PlayerManager.Instance.GetUserList("Layout_Players");
                    uList.RemovePlayer(MMOManager.Instance.GetPlayerByUserID(player.ID));
                }
            }
        }

        /// <summary>
        /// Action to claim this conference
        /// </summary>
        public void Claim()
        {
            if (Occupancies == null)
            {
                Occupancies = new List<IPlayer>();
            }

            Debug.Log("ConferecenceChairGroup [" + ID + "]: Claiming");

            //if local player and there are seats available, open door
            if (doorLock.LocalPlayerClicked)
            {
                if (Occupancies.Count <= AllChairs.Count)
                {
                    OpenDoor();
                    m_closeDoorTimer = 0.0f;
                    m_TimerOn = true;
                }
            }

            IsClaimed = true;

            if (instructionText)
            {
                instructionText.text = conferenceSettings.claimedInstruction;
            }

            //only set the owner if is local player and null. ensure that there are not multiple oweners upon the network call
            if (Owner == null)
            {
                Owner = PlayerManager.Instance.GetLocalPlayer();
            }

            //this is to cache the owner id so that if owner disconnects whilst in the conference, the conference gets unclaimed
            m_disconnectOwnerID = Owner.ID;

            if (availabilityImage)
            {
                availabilityImage.color = unavailableColor;
            }

            //network
            if (Owner.IsLocal)
            {
                //network this conference room
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("EVENT_TYPE", "CONFERENCE");
                data.Add("E", "NULL");
                data.Add("I", ID);
                data.Add("C", "1");
                data.Add("P", conferenceSettings.password);
                data.Add("O", Owner.ActorNumber.ToString());

                MMOManager.Instance.ChangeRoomProperty(ID, data);
            }

            if(OnClaimed != null)
            {
                OnClaimed.Invoke();
            }
        }

        /// <summary>
        /// Action to uncliam this conference
        /// </summary>
        public void UnClaim()
        {
            m_TimerOn = false;
            bool resetGroup = false;

            Debug.Log("ConferecenceChairGroup [" + ID + "]: Unclaiming");

            //only network if the owner is the local player
            if (Owner != null)
            {
                if (Owner.IsLocal)
                {
                    if (!Occupancies.Contains(Owner))
                    {
                        //network this conference room as unclaimed
                        Dictionary<string, string> data = new Dictionary<string, string>();
                        data.Add("EVENT_TYPE", "CONFERENCE");
                        data.Add("E", "NULL");
                        data.Add("I", ID);
                        data.Add("C", "0");
                        data.Add("P", "");
                        data.Add("O", Owner.ActorNumber.ToString());
                        data.Add("V", JsonUtility.ToJson(GetWrapper()));

                        MMOManager.Instance.ChangeRoomProperty(ID, data);

                        resetGroup = true;
                    }
                }
                else
                {
                    if (ResetOnUnClaim)
                    {
                        resetGroup = true;
                    }
                }
            }
            else
            {
                if (ResetOnUnClaim)
                {
                    resetGroup = true;
                }
            }

            if(resetGroup || (Occupancies != null && Occupancies.Count <= 0))
            {
                doorLock.Password = "";
                Owner = null;
                IsClaimed = false;
                m_disconnectOwnerID = "";

                CurrentUploadedFile = "";

                if (instructionText)
                {
                    instructionText.text = conferenceSettings.unclaimedInstruction;
                }

                if (availabilityImage)
                {
                    availabilityImage.color = availableColor;
                }

                if (OnUnclaimed != null)
                {
                    OnUnclaimed.Invoke();
                }
            }

            ResetOnUnClaim = false;
        }

        /// <summary>
        /// Returns a wrapper contianing all the occupants new end poiints when meeting has ended
        /// </summary>
        /// <returns></returns>
        private PlayerVectorWrapper GetWrapper()
        {
            PlayerVectorWrapper wrapper = new PlayerVectorWrapper();
            wrapper.points = new List<PlayerVector>();

            List<Transform> remaingPoints = new List<Transform>();
            remaingPoints.AddRange(endPoints);

            if(remaingPoints.Count > 0)
            {
                for (int i = 0; i < Occupancies.Count; i++)
                {
                    if (remaingPoints.Count <= 0)
                    {
                        remaingPoints.AddRange(endPoints);
                    }

                    int random = Random.Range(0, remaingPoints.Count);
                    PlayerVector pVec = new PlayerVector(Occupancies[i].ID, remaingPoints[random].position);
                    wrapper.points.Add(pVec);

                    remaingPoints.RemoveAt(random);
                }
            }

            return wrapper;
        }


        /// <summary>
        /// Called to open the door on this conference
        /// </summary>
        public void OpenDoor()
        {
            //only open if local player
            doorLock.IgnoreRaycast = true;
            entranceDoor.IsOpen = true;

            if (chairTrigger)
            {
                chairTrigger.SetActive(true);
            }
        }

        /// <summary>
        /// Called to close the door on this conference
        /// </summary>
        public void CloseDoor()
        {
            if(chairTrigger)
            {
                chairTrigger.SetActive(false);
            }

            entranceDoor.IsOpen = false;
            doorLock.IgnoreRaycast = false;
        }

        /// <summary>
        /// Sets the password for this conference
        /// </summary>
        /// <param name="str"></param>
        public void SetPassword(string str)
        {
            conferenceSettings.password = str;
        }

        /// <summary>
        /// Updates the owner of the conference
        /// </summary>
        /// <param name="player"></param>
        public void UpdateOwner(IPlayer player)
        {
            Owner = player;
            m_disconnectOwnerID = player.ID;

            AllChairs.ForEach(x => x.UpdateLiveStreamRole());
        }

        /// <summary>
        /// Callback for when the local player enters the room
        /// </summary>
        /// <param name="player"></param>
        private void OnPlayerEnteredTrigger(IPlayer player)
        {
            if(player.IsLocal)
            {
                List<IChairObject> availableChairs = AllChairs.FindAll(x => x.ChairOccupied == false).ToList();

                int random = Random.Range(0, availableChairs.Count);

                ChairManager.Instance.OccupyChiar(availableChairs[random], true);

                m_TimerOn = false;
                StartCoroutine(DelayDoorOnTrigger());
            }
        }

        /// <summary>
        /// delay the returning upon the trigger
        /// </summary>
        /// <returns></returns>
        private IEnumerator DelayDoorOnTrigger()
        {
            yield return new WaitForSeconds(0.5f);

            CloseDoor();
            doorLock.LockThis();
        }

        /// <summary>
        /// Called when a player disconnects from the room
        /// </summary>
        /// <param name="id"></param>
        public override void OnPlayerDisconnect(string id)
        {
            base.OnPlayerDisconnect(id);

            Debug.Log("ConferecenceChairGroup [" + ID + "]: OnPlayerDisconnect: " + id);

            if (m_disconnectOwnerID.Equals(id))
            {
                if(conferenceSettings.ownerDisconnectEvent.Equals(DisconnectedEvent.Unclaim))
                {
                    //need to remove all accupants from conference
                    foreach (IPlayer player in Occupancies)
                    {
                        ChairManager.Instance.LeaveChairGroup();
                    }

                    UnClaim();
                    m_disconnectOwnerID = "";
                }
                else
                {
                    if(Occupancies.Count > 0)
                    {
                        ChairManager.Instance.SwitchConferenceOwner(ID, Occupancies[0].ID);
                    }
                    else
                    {
                        ResetOnUnClaim = true;
                        UnClaim();
                        CurrentUploadedFile = "";
                    }
                }
            }
        }

        [System.Serializable]
        public enum EndPointType { Defined, Generate }

        [System.Serializable]
        public enum PointDirection {  Right, Left, Above, Below }

        [System.Serializable]
        public enum DisconnectedEvent { Unclaim, SwitchOwner }

        [System.Serializable]
        public enum ScreenContentDisplayType { UICanvas, WorldCanvas}

        [System.Serializable]
        public enum ScreenContentPrivacy { Private, Global }

        [System.Serializable]
        public class ConferenceChairGroupIOObject : ChairGroupIOObject
        {
            public string password = "";
            public string unclaimedInstruction = "Claim Room";
            public string claimedInstruction = "Enter Room";

            public EndPointType endType = EndPointType.Generate;
            public float offset = 1.0f;
            public PointDirection placement = PointDirection.Right;
            public DisconnectedEvent ownerDisconnectEvent = DisconnectedEvent.SwitchOwner;
            public ScreenContentDisplayType contentUploadDisplay = ScreenContentDisplayType.WorldCanvas;
            public ScreenContentPrivacy contentUploadPrivacy = ScreenContentPrivacy.Private;
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

                conferenceSettings.GO = gameObject.name;

                IChairObject[] all = GetComponentsInChildren<IChairObject>();
                conferenceSettings.Type = "Conference Group";

                conferenceSettings.StreamCache = new List<ChairStreamCache>();
                for (int i = 0; i < all.Length; i++)
                {
                    conferenceSettings.StreamCache.Add(new ChairStreamCache(all[i].IDRef, all[i].GO.name, all[i].StreamMode));
                }
            }

            conferenceSettings.ID = id;
            return conferenceSettings;
        }

        protected override void ApplySettings(IObjectSetting settings)
        {
            if (settings == null) return;

            base.ApplySettings(settings);

            this.conferenceSettings.password = ((ConferenceChairGroupIOObject)settings).password;
            this.conferenceSettings.unclaimedInstruction = ((ConferenceChairGroupIOObject)settings).unclaimedInstruction;
            this.conferenceSettings.claimedInstruction = ((ConferenceChairGroupIOObject)settings).claimedInstruction;

            this.conferenceSettings.endType = ((ConferenceChairGroupIOObject)settings).endType;
            this.conferenceSettings.offset = ((ConferenceChairGroupIOObject)settings).offset;
            this.conferenceSettings.placement = ((ConferenceChairGroupIOObject)settings).placement;
            this.conferenceSettings.ownerDisconnectEvent = ((ConferenceChairGroupIOObject)settings).ownerDisconnectEvent;
            this.conferenceSettings.contentUploadDisplay = ((ConferenceChairGroupIOObject)settings).contentUploadDisplay;
            this.conferenceSettings.contentUploadPrivacy = ((ConferenceChairGroupIOObject)settings).contentUploadPrivacy;
        }

#if UNITY_EDITOR
        public override void UpdateIOChairSettings()
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
            if (m_instances != null)
            {
                m_instances.AddIOObject(ID, GetSettings(true));
            }
        }

        [CustomEditor(typeof(ConferenceChairGroup), true)]
        public class ConferenceChairGroup_Editor : ChairGroup_Editor
        {
            private ConferenceChairGroup conferenceGroupScript;

            private void OnEnable()
            {
                GetBanner();
                Initialise();
            }
            protected override void Clear()
            {
                base.Clear();

                if (m_instances != null)
                {
                    m_instances.RemoveIOObject(conferenceGroupScript.GetSettings(true));
                }
            }


            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                serializedObject.Update();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Conference Setup", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("entranceDoor"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("doorLock"), true);
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("conferenceSettings").FindPropertyRelative("password"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chairTrigger"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("UIObject"), true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Availability", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("availabilityImage"), true);
                // EditorGUILayout.PropertyField(serializedObject.FindProperty("availableColor"), true);
                //  EditorGUILayout.PropertyField(serializedObject.FindProperty("unavailableColor"), true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Instruction", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("instructionText"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("conferenceSettings").FindPropertyRelative("unclaimedInstruction"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("conferenceSettings").FindPropertyRelative("claimedInstruction"), true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Ended Event", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("conferenceSettings").FindPropertyRelative("endType"), true);

                if(((EndPointType)serializedObject.FindProperty("conferenceSettings").FindPropertyRelative("endType").enumValueIndex).Equals(EndPointType.Defined))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("endPoints"), true);
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("conferenceSettings").FindPropertyRelative("offset"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("conferenceSettings").FindPropertyRelative("placement"), true);
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("conferenceSettings").FindPropertyRelative("ownerDisconnectEvent"), true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("conferenceSettings").FindPropertyRelative("contentUploadDisplay"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("conferenceSettings").FindPropertyRelative("contentUploadPrivacy"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contentLoaders"), true);

                if(GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(conferenceGroupScript);

                    if (m_instances != null)
                    {
                        m_instances.AddIOObject(conferenceGroupScript.ID, conferenceGroupScript.GetSettings());
                    }
                }
            }

            protected override void Initialise()
            {
                base.Initialise();

                conferenceGroupScript = (ConferenceChairGroup)target;

                if (m_instances != null)
                {
                    //need to get the settings from the instances script then update the settings
                    foreach (AppInstances.IOObjectChairGroupHandler setting in m_instances.ioChairGroupObjects)
                    {
                        if (setting.referenceID.Equals(conferenceGroupScript.ID))
                        {
                            conferenceGroupScript.ApplySettings(setting.conferenceSettings);
                            break;
                        }
                    }

                    m_instances.AddIOObject(conferenceGroupScript.ID, conferenceGroupScript.GetSettings());
                }
            }
        }
#endif
    }

    [System.Serializable]
    public class PlayerVectorWrapper
    {
        public List<PlayerVector> points;

        public PlayerVector GetVector(string player)
        {
            if(points != null)
            {
                return points.FirstOrDefault(x => x.id.Equals(player));
            }

            return null;
        }
    }

    [System.Serializable]
    public class PlayerVector
    {
        public string id;
        public float x;
        public float y;
        public float z;

        public PlayerVector(string id, float x, float y, float z)
        {
            this.id = id;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public PlayerVector(string id, Vector3 vec)
        {
            this.id = id;
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