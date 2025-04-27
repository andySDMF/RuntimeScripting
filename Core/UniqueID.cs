using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class UniqueID : MonoBehaviour
    {
        [SerializeField]
        protected string id = "";

        [SerializeField]
        protected string prefix = "";

        [SerializeField]
        protected string analyticReference = "";

        [SerializeField]
        protected bool controlledByUserType = false;

        [SerializeField]
        protected bool allAdminUsersOnly = false;

        [SerializeField]
        protected List<string> userTypes;

        public string ID
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }

        public string AnalyticReference
        {
            get
            {
                return analyticReference;
            }
        }

        public virtual string MonoType
        {
            get
            {
                return this.ToString();
            }
        }

        public virtual bool HasParent
        {
            get
            {
                return false;
            }
        }

        public string GetRawID()
        {
            return id.Replace(prefix + "_", "");
        }

        protected virtual void Awake()
        {
            if (!AppManager.IsCreated) return;

            if (string.IsNullOrEmpty(analyticReference))
            {
                analyticReference = gameObject.name;
            }

            //means instantaited at runtime
            if (MMORoom.Instance.RoomReady || PlayerManager.Instance.GetLocalPlayer() != null)
            {
                string str = id.Replace(prefix + "_", "");
                
                if (AppManager.Instance.Instances.UniqueIDExists(str))
                {
                    //need to count how many versions of this id there is in the room
                    if(UniqueIDManager.Instance.ReplicatedIDCount.ContainsKey(str))
                    {
                        UniqueIDManager.Instance.ReplicatedIDCount[str] += 1;
                    }
                    else
                    {
                        UniqueIDManager.Instance.ReplicatedIDCount.Add(str, 1);
                    }

                    id = str + "|" + UniqueIDManager.Instance.ReplicatedIDCount[str].ToString();
                }
            }

            id = prefix + "_" + id;
        }

        public virtual bool CanUserControlThis(string user)
        {
            if(AppManager.Instance.Settings.projectSettings.useAdminUser)
            {
                if(!AppManager.Instance.Settings.projectSettings.useMultipleAdminUsers)
                {
                    if (AppManager.Instance.Data.IsAdminUser && allAdminUsersOnly)
                    {
                        return true;
                    }
                    else
                    {
                        if (allAdminUsersOnly)
                        {
                            return false;
                        }
                    }
                }
            }

            if(controlledByUserType && !string.IsNullOrEmpty(user))
            {
                bool canPerform = userTypes.Contains(user);

                if (!canPerform)
                {
                    PlayerManager.Instance.ShowPermisionMessage(true);
                }

                return canPerform;
            }

            return true;
        }

        [System.Serializable]
        public class IObjectSetting
        {
            [HideInInspector]
            public string analyticReference = "";
            [HideInInspector]
            public string prefix = "";
            [HideInInspector]
            public bool controlledByUserType = false;
            [HideInInspector]
            public bool adminOnly = false;
            [HideInInspector]
            public List<string> userTypes = new List<string>();

            public string ID { get; set; }
            public string GO { get; set; }
        }

        public virtual IObjectSetting GetSettings(bool remove = false)
        {
            IObjectSetting baseSettings = new IObjectSetting();
            baseSettings.adminOnly = allAdminUsersOnly;
            baseSettings.prefix = prefix;
            baseSettings.controlledByUserType = controlledByUserType;

            if (baseSettings.userTypes == null)
            {
                baseSettings.userTypes = new List<string>();
            }

            if(userTypes == null)
            {
                userTypes = new List<string>();
            }

            baseSettings.userTypes.AddRange(userTypes);

            return baseSettings;
        }

        protected virtual void ApplySettings(IObjectSetting settings)
        {
            if (settings == null) return;

            prefix = settings.prefix;
            controlledByUserType = settings.controlledByUserType;
            allAdminUsersOnly = settings.adminOnly;
            userTypes = settings.userTypes;
            analyticReference = settings.analyticReference;
        }

#if UNITY_EDITOR

        [CustomEditor(typeof(UniqueID), true)]
        public class UniqueID_Editor : BaseInspectorEditor
        {
            protected UniqueID script;
            protected AppInstances m_instances;
            protected string GOName = "";
            protected string GONamePrevious = "";

            private void OnEnable()
            {
                GetBanner();
                Initialise();
            }

            private void OnDestroy()
            {
                Clear();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                DisplayID();

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(script);
            }

            protected bool GONameChanged()
            {
                bool nameChanged = GOName != GONamePrevious;
                GONamePrevious = GOName;

                return nameChanged;
            }

            protected void UpdateWindowChanged()
            {
                if (script == null)
                {
                    script = (UniqueID)target;
                }

                if(script != null)
                {
                    GOName = script.gameObject.name;
                }
            }

            protected virtual void Initialise()
            {
                EditorApplication.hierarchyChanged += UpdateWindowChanged;

                script = (UniqueID)target;
                GOName = script.gameObject.name;
                GONamePrevious = GOName;

                AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");

                if (appReferences != null)
                {
                    m_instances = appReferences.Instances;
                }
                else
                {
                    m_instances = Resources.Load<AppInstances>("ProjectAppInstances");
                }

                if (!Application.isPlaying)
                {
                    if (script.gameObject.scene.IsValid() && script.gameObject.scene.name != null)
                    {
                        if (string.IsNullOrEmpty(script.id) || UniqueIDManager.Instance.Exists(script.id, script))
                        {
                            string str = UniqueIDManager.Instance.NewID(script.gameObject);
                            script.id = str;
                        }

                        if (script.gameObject.scene.IsValid() && script.gameObject.scene.name != null)
                        {
                            UniqueIDManager.Instance.Add(script.id, script.gameObject);
                        }
                    }
                }
            }

            protected virtual void Clear()
            {
                EditorApplication.hierarchyChanged -= UpdateWindowChanged;

                if (script == null)
                {
                    script = (UniqueID)target;
                }

                if(script != null)
                {
                    if (script.gameObject.scene.IsValid() && script.gameObject.scene.name != null)
                    {
                        UniqueIDManager.Instance.Clear(script, script.id);
                    }
                }
                else
                {

                    UniqueIDManager.Instance.Clear(script, script.id);
                }
            }

            protected void DisplayID()
            {
                DrawID();
                EditorGUILayout.Space();
                DrawControl();
            }

            protected void DrawID()
            {
                EditorGUILayout.LabelField("ID", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(script.id, EditorStyles.miniLabel);//EditorGUILayout.TextField(new GUIContent("", ""), script.id);
                EditorGUILayout.LabelField("Prefix", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("prefix"), true);

                EditorGUILayout.Space();
                DrawAnayltics();
            }

            protected void DrawControl()
            {
                EditorGUILayout.LabelField("Control", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("allAdminUsersOnly"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("controlledByUserType"), true);

                if (serializedObject.FindProperty("controlledByUserType").boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("userTypes"), true);
                }
            }

            protected void DrawAnayltics()
            {
                EditorGUILayout.LabelField("Analytics", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("analyticReference"), true);
                EditorGUILayout.LabelField("If empty then GO name is used", EditorStyles.miniBoldLabel);

                if (string.IsNullOrEmpty(serializedObject.FindProperty("analyticReference").stringValue))
                {
                    serializedObject.FindProperty("analyticReference").stringValue = script.gameObject.name;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(script);
                }
            }
        }
#endif
    }
}
