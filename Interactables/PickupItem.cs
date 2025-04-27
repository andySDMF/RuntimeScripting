using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class PickupItem : UniqueID
    {
        public Vector3 OverridePosition;
        public Vector3 OverrideRotation;
        public float OverrideScale = 0.5f;
        public GameObject Parent;

        public ItemIOObject settings = new ItemIOObject();

        protected Vector3 originPosition;
        protected Vector3 originRotation;
        protected Vector3 originScale;
        protected bool hasInit = false;
        protected float distToGround = 0.0f;
        protected Transform parent;

        public Vector3 OriginPosition
        {
            get
            {
                return originPosition;
            }
        }

        public Vector3 OriginRotation
        {
            get
            {
                return originRotation;
            }
        }

        public Vector3 OriginScale
        {
            get
            {
                return originScale;
            }
        }

        public DropPoint CurrentDropPoint
        {
            get;
            set;
        }

        public bool Instantiated
        {
            get;
            set;
        }

        public string OwnerID
        {
            get;
            set;
        }

        public System.Action<bool> OnInteracted { get; set; }

        private Rigidbody m_rigidbody;

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if (!AppManager.Instance.Instances.ignoreIObjectSettings)
            {
                //need to get the settings from the instances script then update the settings
                foreach (AppInstances.IOObjectItemHandler setting in AppManager.Instance.Instances.ioItemObjects)
                {
                    if (setting.referenceID.Equals(GetRawID()))
                    {
                        ApplySettings(setting.settings);
                        break;
                    }
                }
            }

            m_rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (m_rigidbody != null)
            {
                if (IsGrounded())
                {
                    m_rigidbody.isKinematic = true;
                }
                else
                {
                    m_rigidbody.isKinematic = false;
                }
            }
        }

        public void Pickup(string owner = "")
        {
            Initialise();

            if (string.IsNullOrEmpty(owner))
            {
                owner = PlayerManager.Instance.GetLocalPlayer().ID;
            }

            OwnerID = owner;

            ItemManager.Instance.Pickup3D(gameObject);

            AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, AnalyticReference);

            if (OnInteracted != null)
            {
                OnInteracted.Invoke(true);
            }
        }

        public void ResetToOrigin()
        {
            Initialise();

            if (Parent != null)
            {
                transform.SetParent(Parent.transform);
            }
            else
            {
                transform.SetParent(null);
            }

            transform.localScale = originScale;
            transform.position = originPosition;
            transform.eulerAngles = originRotation;

            if (OnInteracted != null)
            {
                OnInteracted.Invoke(false);
            }
        }

        public void ReparentTo(Transform newParent, Vector3 pos, Vector3 rot, Vector3 scale)
        {
            transform.SetParent(newParent);

            transform.localScale = scale;
            transform.position = pos;
            transform.eulerAngles = scale;

            if (OnInteracted != null)
            {
                OnInteracted.Invoke(false);
            }
        }

        public void ReparentTo(Transform newParent, float spacing = 0.01f)
        {
            transform.SetParent(newParent);

            transform.localScale = originScale;

            Vector3 shift = (originScale / 2);
            Vector3 newPosition = new Vector3(newParent.position.x, newParent.position.y + shift.y + spacing, newParent.position.z);

            transform.position = newPosition;
            transform.eulerAngles = originRotation;

            if (OnInteracted != null)
            {
                OnInteracted.Invoke(false);
            }
        }

        public void Initialise()
        {
            if (!hasInit)
            {
                Instantiated = false;
                hasInit = true;
                ApplyNewOrigins(transform.position, transform.eulerAngles, transform.localScale);

                if (Parent == null)
                {
                    if (transform.parent != null)
                    {
                        Parent = transform.parent.gameObject;
                    }
                }
            }
        }

        public void ApplyNewOrigins(Vector3 pos, Vector3 rot, Vector3 sca)
        {
            originPosition = new Vector3(pos.x, pos.y, pos.z);
            originRotation = new Vector3(rot.x, rot.y, rot.z);
            originScale = new Vector3(sca.x, sca.y, sca.z);
        }

        public void AddRigidbody()
        {
            StartCoroutine(DelayRigidbodyOnDrop());
        }

        public void DestroyRigidbody()
        {
            if (GetComponent<Rigidbody>())
            {
                Destroy(GetComponent<Rigidbody>());
            }
        }

        private IEnumerator DelayRigidbodyOnDrop()
        {
            yield return new WaitForEndOfFrame();

            if (GetComponent<Collider>())
            {
                GetComponent<Collider>().enabled = true;
                distToGround = GetComponent<Collider>().bounds.extents.y;
            }

            Rigidbody rBody;

            if (!GetComponent<Rigidbody>())
            {
                rBody = gameObject.AddComponent<Rigidbody>();
            }
            else
            {
                rBody = GetComponent<Rigidbody>();
            }

            rBody.angularDrag = 0.0f;
            rBody.freezeRotation = true;

            m_rigidbody = rBody;

            transform.eulerAngles = Vector3.up;
            transform.localScale = originScale;
        }

        private bool IsGrounded()
        {
            return Physics.Raycast(transform.position, -Vector3.up, distToGround + 0.1f);
        }

        [System.Serializable]
        public class ItemIOObject : IObjectSetting
        {
            public bool spin = false;
            public Vector3 spinAxis = new Vector3(1f, 1f, 1f);
            public float spinSpeed = 20;
            public PickUpBehaviourType pickupBehaviour = PickUpBehaviourType.Reparent;
            public DropBehaviourType dropBehaviour = DropBehaviourType.Reset;
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

            this.settings.spin = ((ItemIOObject)settings).spin;
            this.settings.pickupBehaviour = ((ItemIOObject)settings).pickupBehaviour;
            this.settings.dropBehaviour = ((ItemIOObject)settings).dropBehaviour;
        }


#if UNITY_EDITOR
        [CustomEditor(typeof(PickupItem), true), CanEditMultipleObjects]
        public class PickupItem_Editor : UniqueID_Editor
        {
            private PickupItem itemScript;

            private void OnEnable()
            {
                GetBanner();
                Initialise();
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    m_instances.RemoveIOObject(itemScript.GetSettings(true));
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();
                DisplayID();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Item Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("spin"), true);

                if (serializedObject.FindProperty("settings").FindPropertyRelative("spin").boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("spinAxis"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("spinSpeed"), true);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("pickupBehaviour"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("dropBehaviour"), true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Item Overrides", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OverridePosition"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OverrideRotation"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OverrideScale"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Parent"), true);


                if (GUI.changed || GONameChanged())
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(itemScript);

                    if (Application.isPlaying) return;

                    if (m_instances != null)
                    {
                        m_instances.AddIOObject(itemScript.ID, itemScript.GetSettings());
                    }
                }
            }

            protected override void Initialise()
            {
                base.Initialise();

                itemScript = (PickupItem)target;

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    //need to get the settings from the instances script then update the settings
                    foreach (AppInstances.IOObjectItemHandler setting in m_instances.ioItemObjects)
                    {
                        if (setting.referenceID.Equals(itemScript.ID))
                        {
                            itemScript.ApplySettings(setting.settings);
                            break;
                        }
                    }

                    m_instances.AddIOObject(itemScript.ID, itemScript.GetSettings());
                }
            }
        }
#endif
    }

}