using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Collider))]
    public class Door : UniqueID
    {
        [SerializeField]
        private DoorIOObject settings = new DoorIOObject();

        private bool m_isOpen;
        private bool m_moving = false;

        private float m_target = 0.0f;
        private float m_default = 0.0f;


        /// <summary>
        /// States if the door is interactable
        /// </summary>
        public bool RaycastIgnored
        {
            get
            {
                return settings.ignoreRaycast;
            }
            set
            {
                settings.ignoreRaycast = value;
            }
        }

        /// <summary>
        /// Access to the open state of the door
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return m_isOpen;
            }
            set
            {
                m_isOpen = value;

                //action new state
                if(m_isOpen)
                {
                    Open();
                }
                else
                {
                    Close();
                }
            }
        }

        public override bool HasParent
        {
            get
            {
                return GetComponentInParent<ConferenceChairGroup>() != null;
            }
        }

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if (!AppManager.Instance.Instances.ignoreIObjectSettings)
            {
                //need to get the settings from the instances script then update the settings
                foreach (AppInstances.IOObjectDoorHandler setting in AppManager.Instance.Instances.ioDoorObjects)
                {
                    if (setting.referenceID.Equals(GetRawID()))
                    {
                        ApplySettings(setting.settings);
                        break;
                    }
                }
            }

            //store angles
            m_default = transform.localEulerAngles.y;
            m_target = settings.openRotation;
        }

        private void Update()
        {
            if(m_moving)
            {
                transform.localEulerAngles = new Vector3(0.0f, Mathf.LerpAngle(transform.localEulerAngles.y, m_target, settings.speed * Time.deltaTime));

                //check distance between the start/end angles, if less then stop moving
                if(Vector3.Distance(transform.localEulerAngles, new Vector3(0.0f, m_target, 0.0f)) <= 0.1f)
                {
                    m_moving = false;
                    transform.localEulerAngles = new Vector3(0.0f, m_target, 0.0f);
                    GetComponent<Collider>().isTrigger = false;
                }
            }
        }

        /// <summary>
        /// Called to open this door
        /// </summary>
        /// <param name="network"></param>
        public void Open(bool network = false)
        {
            //set vars
            m_target = settings.openRotation;
            m_isOpen = true;
            m_moving = true;
            GetComponent<Collider>().isTrigger = true;

            if(settings.isNetworked && network)
            {
                //need to network  this door
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("EVENT_TYPE", "DOOR");
                data.Add("I", ID);
                data.Add("O", "1");

                MMOManager.Instance.ChangeRoomProperty(ID, data);
            }
        }

        /// <summary>
        /// Called to close this door
        /// </summary>
        /// <param name="network"></param>
        public void Close(bool network = false)
        {
            //set vars
            m_target = m_default;
            GetComponent<Collider>().isTrigger = true;
            m_isOpen = false;
            m_moving = true;

            if (settings.isNetworked && network)
            {
                //need to network  this door
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("EVENT_TYPE", "DOOR");
                data.Add("I", ID);
                data.Add("O", "0");

                MMOManager.Instance.ChangeRoomProperty(ID, data);
            }
        }

        [System.Serializable]
        public class DoorIOObject : IObjectSetting
        {
            public bool ignoreRaycast;
            public float openRotation = 270.0f;
            public float speed = 5.0f;
            public bool isNetworked = true;
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
            }

            settings.ID = id;
            return settings;
        }

        protected override void ApplySettings(IObjectSetting settings)
        {
            if (settings == null) return;

            base.ApplySettings(settings);

            this.settings.openRotation = ((DoorIOObject)settings).openRotation;
            this.settings.speed = ((DoorIOObject)settings).speed;
            this.settings.ignoreRaycast = ((DoorIOObject)settings).ignoreRaycast;
            this.settings.isNetworked = ((DoorIOObject)settings).isNetworked;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Door), true)]
        public class Door_Editor : UniqueID_Editor
        {
            private Door doorScript;
            private bool isConferenceDoor;

            private void OnEnable()
            {
                GetBanner();
                Initialise();

                isConferenceDoor = doorScript.gameObject.GetComponentInParent<ConferenceChairGroup>() != null;

                if (!isConferenceDoor)
                {
                    base.Initialise();

                    if (Application.isPlaying) return;

                    if (m_instances != null)
                    {
                        //need to get the settings from the instances script then update the settings
                        foreach (AppInstances.IOObjectDoorHandler setting in m_instances.ioDoorObjects)
                        {
                            if (setting.referenceID.Equals(doorScript.ID))
                            {
                                doorScript.ApplySettings(setting.settings);
                                break;
                            }
                        }

                        m_instances.AddIOObject(doorScript.ID, doorScript.GetSettings());
                    }
                }
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (m_instances != null && !isConferenceDoor)
                {
                    m_instances.RemoveIOObject(doorScript.GetSettings(true));
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                if (!isConferenceDoor)
                {
                    DisplayID();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Door Setup", EditorStyles.boldLabel);

                if (!isConferenceDoor)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("ignoreRaycast"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("isNetworked"), true);
                }
              
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("openRotation"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("speed"), true);

                if (GUI.changed || GONameChanged())
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(doorScript);

                    if (Application.isPlaying) return;

                    if (m_instances != null && !isConferenceDoor)
                    {
                        m_instances.AddIOObject(doorScript.ID, doorScript.GetSettings());
                    }
                }
            }

            protected override void Initialise()
            {
                doorScript = (Door)target;
            }
        }
#endif
    }
}
