using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class VehicleSpawn : UniqueID
    {
        [Header("Vehicles")]
        [SerializeField]
        private VehicleSpawnIOObject settings = new VehicleSpawnIOObject();

        public bool SpawnOnAwake
        {
            get
            {
                return settings.spawnOnAwake;
            }
        }

        public string SpawnDefaultVehicle
        {
            get
            {
                return settings.vehicleName;
            }
        }

        private void Start()
        {
            if (!AppManager.Instance.Instances.ignoreIObjectSettings)
            {
                //need to get the settings from the instances script then update the settings
                foreach (AppInstances.IOObjectVehcileSpawnHandler setting in AppManager.Instance.Instances.ioVehicleSpawnObjects)
                {
                    if (setting.referenceID.Equals(GetRawID()))
                    {
                        ApplySettings(setting.settings);
                        break;
                    }
                }
            }
        }

        public void Spawn(string uniqueID)
        {
            UnityEngine.Object prefab = Resources.Load(settings.vehicleName);

            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online))
            {
                if (prefab != null)
                {
                    MMOManager.Instance.InstantiateRoomObject(settings.vehicleName, prefab.name + "_" + ID + "_" + uniqueID, transform.position, transform.eulerAngles, Vector3.one);
                }
                else
                {
                    Debug.Log("Cannot load vehicle. Vehicle does not exist in Resources!");
                }
              
            }
            else
            {
                if (prefab != null)
                {
                    GameObject go = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    go.transform.position = transform.position;
                    go.transform.localScale = Vector3.one;
                    go.transform.eulerAngles = transform.eulerAngles;
                    go.name = prefab.name + "_" + ID + "_" + Random.Range(0, 10001).ToString();
                }
                else
                {
                    Debug.Log("Cannot load vehicle. Vehicle does not exist in Resources!");
                }
            }
        }

        public void SpawnOtherVehicle(string resourcePath, string uniqueID)
        {
            UnityEngine.Object prefab = Resources.Load(resourcePath);

            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online))
            {
                if (prefab != null)
                {
                    MMOManager.Instance.InstantiateRoomObject(settings.vehicleName, prefab.name + "_" + ID + "_" + uniqueID, transform.position, transform.eulerAngles, Vector3.one);
                }
                else
                {
                    Debug.Log("Cannot load vehicle. Vehicle does not exist in Resources!");
                }

            }
            else
            {
                if (prefab != null)
                {
                    GameObject go = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    go.transform.position = transform.position;
                    go.transform.localScale = Vector3.one;
                    go.transform.eulerAngles = transform.eulerAngles;
                    go.name = prefab.name + "_" + ID + "_" + Random.Range(0, 10001).ToString();
                }
                else
                {
                    Debug.Log("Cannot load vehicle. Vehicle does not exist in Resources!");
                }
            }
        }

        [System.Serializable]
        public class VehicleSpawnIOObject : IObjectSetting
        {
            public string vehicleName = "SimpleVehicle";
            public bool spawnOnAwake = true;
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

            this.settings.vehicleName = ((VehicleSpawnIOObject)settings).vehicleName;
            this.settings.spawnOnAwake = ((VehicleSpawnIOObject)settings).spawnOnAwake;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(VehicleSpawn), true)]
        public class VehicleSpawn_Editor : UniqueID_Editor
        {
            private VehicleSpawn vsScript;

            protected override void Initialise()
            {
                base.Initialise();

                vsScript = (VehicleSpawn)target;

                if (m_instances != null)
                {
                    //need to get the settings from the instances script then update the settings
                    foreach (AppInstances.IOObjectVehcileSpawnHandler setting in m_instances.ioVehicleSpawnObjects)
                    {
                        if (setting.referenceID.Equals(vsScript.ID))
                        {
                            vsScript.ApplySettings(setting.settings);
                            break;
                        }
                    }

                    m_instances.AddIOObject(script.ID, script.GetSettings());
                }
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    m_instances.RemoveIOObject(vsScript.GetSettings(true));
                }
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("vehicleName"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("spawnOnAwake"), true);

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);

                    if (Application.isPlaying) return;

                    if (m_instances != null)
                    {
                        m_instances.AddIOObject(vsScript.ID, vsScript.GetSettings());
                    }
                }
            }
        }
#endif
    }
}
