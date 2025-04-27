using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Collider))]
    public class NPCBotSpawnArea : UniqueID
    {
        [SerializeField]
        private NPCBotSpawnAreaIOObject settings;

        [SerializeField]
        private Collider[] walkableArea;

        private int m_botsSpawned = 0;

        public int BotCount
        {
            get
            {
                return settings.numberOfBots;
            }
        }

        public bool ReachedSpawnCount
        {
            get
            {
                return m_botsSpawned >= settings.numberOfBots + 1;
            }
        }

        public Collider[] WalkableArea
        {
            get { return walkableArea; }
        }

        public List<NPCManager.NPCBotPrefab> FixedBots
        {
            get
            {
                return settings.fixedBots;
            }
        }

        public Vector3 GetPoint()
        {
            Collider col = GetComponent<Collider>();

            m_botsSpawned++;

            return new Vector3(
               Random.Range(col.bounds.min.x, col.bounds.max.x),
               0.5f,
               Random.Range(col.bounds.min.z, col.bounds.max.z)
           );
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if (!AppManager.Instance.Instances.ignoreIObjectSettings)
            {
                //need to get the settings from the instances script then update the settings
                foreach (AppInstances.IOObjectNPCBotSpawenAreaHandler setting in AppManager.Instance.Instances.ioNPCBotSpawnAreaObjects)
                {
                    if (setting.referenceID.Equals(GetRawID()))
                    {
                        ApplySettings(setting.settings);
                        break;
                    }
                }
            }

            GetComponent<Collider>().isTrigger = true;

        }

        [System.Serializable]
        public class NPCBotSpawnAreaIOObject : IObjectSetting
        {
            [Range(0, 100)]
            public int numberOfBots = 0;

            public List<NPCManager.NPCBotPrefab> fixedBots = new List<NPCManager.NPCBotPrefab>();
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

            this.settings.numberOfBots = ((NPCBotSpawnAreaIOObject)settings).numberOfBots;
            this.settings.fixedBots = ((NPCBotSpawnAreaIOObject)settings).fixedBots;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(NPCBotSpawnArea), true), CanEditMultipleObjects]
        public class NPCBotSpawnArea_Editor : UniqueID_Editor
        {
            private NPCBotSpawnArea botSpawnArea;

            private void OnEnable()
            {
                GetBanner();
                base.Initialise();

                botSpawnArea = (NPCBotSpawnArea)target;

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    //need to get the settings from the instances script then update the settings
                    foreach (AppInstances.IOObjectNPCBotSpawenAreaHandler setting in m_instances.ioNPCBotSpawnAreaObjects)
                    {
                        if (setting.referenceID.Equals(botSpawnArea.ID))
                        {
                            botSpawnArea.ApplySettings(setting.settings);
                            break;
                        }
                    }

                    m_instances.AddIOObject(botSpawnArea.ID, botSpawnArea.GetSettings());
                }
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    m_instances.RemoveIOObject(botSpawnArea.GetSettings(true));
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                DrawID();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("NPCBotSpawnArea", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("numberOfBots"), true);
                EditorGUILayout.LabelField("This will randomise x amount of bots in this zone", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("Avatars derived from app settings for this scene", EditorStyles.miniBoldLabel);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("This will generate set number of bots based on avatar prefab", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("Avatar Prefabs", GUILayout.ExpandWidth(true));

                for (int j = 0; j < botSpawnArea.FixedBots.Count; j++)
                {
                    if (string.IsNullOrEmpty(botSpawnArea.FixedBots[j].prefabPath))
                    {
                        botSpawnArea.FixedBots[j].prefabPath = "NPC/NPCBot";
                    }

                    botSpawnArea.FixedBots[j].prefabPath = EditorGUILayout.TextField("Prefab Path:", botSpawnArea.FixedBots[j].prefabPath);
                    botSpawnArea.FixedBots[j].avatarPath = EditorGUILayout.TextField("Avatar Path:", botSpawnArea.FixedBots[j].avatarPath);
                    botSpawnArea.FixedBots[j].quantity = EditorGUILayout.IntSlider("Quantity:", botSpawnArea.FixedBots[j].quantity, 0, 100);

                    if (GUILayout.Button("Remove"))
                    {
                        botSpawnArea.FixedBots.RemoveAt(j);
                        break;
                    }

                    if (j < botSpawnArea.FixedBots.Count - 1)
                    {
                        EditorGUILayout.Space();
                    }
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Add"))
                {
                    botSpawnArea.FixedBots.Add(new NPCManager.NPCBotPrefab());
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Walkable Area", GUILayout.ExpandWidth(true));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("walkableArea"), true);

                if (GUI.changed || GONameChanged())
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(script);

                    if (Application.isPlaying) return;

                    if (m_instances != null)
                    {
                        m_instances.AddIOObject(botSpawnArea.ID, botSpawnArea.GetSettings());
                    }
                }
            }
        }
#endif
    }
}
