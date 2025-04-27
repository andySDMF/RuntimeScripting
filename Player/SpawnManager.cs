using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// Spawn Manager to handle player spawning
    /// at base it just defines the spawn position and spawn camera
    /// </summary>
    public class SpawnManager : Singleton<SpawnManager>
    {
        public static SpawnManager Instance
        {
            get
            {
                return ((SpawnManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        [SerializeField]
        private Transform SpawnPoint;
        [SerializeField]
        private GameObject spawnCamera;


        private MainSpawnPoint[] points;
        private SwitchSceneTrigger[] switchScenes;

        private void Start()
        {
            points = FindObjectsByType<MainSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            switchScenes = FindObjectsByType<SwitchSceneTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        public Vector3 GetSpawnPosition()
        {
            Collider col = null;

            if (points.Length > 0)
            {
                if(points.Length > 1)
                {
                    Debug.Log("More than 1 MainSpawnPoint exists in the scene. Using index 0");
                }
                else
                {
                    Debug.Log("Using MainSpawnPoint");
                }

                col = points[0].Col;
            }

            if(col == null)
            {
                col = SpawnPoint.GetComponent<Collider>();
            }

            if (AppManager.Instance.Data.RoomEstablished)
            {
                if(AppManager.Instance.Data.SceneSpawnLocation != null)
                {
                    for (int i = 0; i < switchScenes.Length; i++)
                    {
                        string[] split = switchScenes[i].ID.Split('_');

                        if (split[split.Length - 1].Equals(AppManager.Instance.Data.SceneSpawnLocation.id))
                        {
                            if (switchScenes[i].gameObject.GetComponent<Collider>())
                            {
                                col = switchScenes[i].GetComponent<Collider>();
                                break;
                            }
                            else
                            {
                                return switchScenes[i].transform.position + switchScenes[i].transform.forward * 2;
                            }
                        }
                    }
                }
            }

            return new Vector3(
                Random.Range(col.bounds.min.x, col.bounds.max.x),
                col.transform.position.y,
                Random.Range(col.bounds.min.z, col.bounds.max.z)
            );
        }

        public Vector3 Forward()
        {
            if (points.Length > 0)
            {
                if (points.Length > 1)
                {
                    Debug.Log("More than 1 MainSpawnPoint exists in the scene. Using index 0");
                }
                else
                {
                    Debug.Log("Using MainSpawnPoint");
                }

                return points[0].Forward;
            }
            else
            {
                return SpawnPoint.forward;
            }
        }

        public Quaternion GetSpawnRotation()
        {
            if (points.Length > 0)
            {
                if (points.Length > 1)
                {
                    Debug.Log("More than 1 MainSpawnPoint exists in the scene. Using index 0");
                }
                else
                {
                    Debug.Log("Using MainSpawnPoint");
                }

                return points[0].Rotation;
            }
            else
            {
                return SpawnPoint.rotation;
            }
        }

        public Vector3 GetLocalAngles()
        {
            if (AppManager.Instance.Data.RoomEstablished)
            {
                if (AppManager.Instance.Data.SceneSpawnLocation != null)
                {
                    for (int i = 0; i < switchScenes.Length; i++)
                    {
                        string[] split = switchScenes[i].ID.Split('_');

                        if (split[split.Length - 1].Equals(AppManager.Instance.Data.SceneSpawnLocation.id))
                        {
                            if (switchScenes[i].gameObject.GetComponent<Collider>())
                            {
                                return switchScenes[i].transform.localEulerAngles;
                            }
                            else
                            {
                                //UI button
                                return switchScenes[i].GetComponentInParent<Canvas>().transform.localEulerAngles;
                            }
                        }
                    }
                }
            }

            if (points.Length > 0)
            {
                if (points.Length > 1)
                {
                    Debug.Log("More than 1 MainSpawnPoint exists in the scene. Using index 0");
                }
                else
                {
                    Debug.Log("Using MainSpawnPoint");
                }

                return points[0].LocalEuler;
            }
            else
            {
                return SpawnPoint.transform.localEulerAngles;
            }
        }

        public void EnableSpawnCamera(bool enable)
        {
            if (points.Length > 0)
            {
                if (points.Length > 1)
                {
                    Debug.Log("More than 1 MainSpawnPoint exists in the scene. Using index 0");
                }
                else
                {
                    Debug.Log("Using MainSpawnPoint");
                }

                points[0].Cam.SetActive(enable);
            }
            else
            {
                spawnCamera.SetActive(enable);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SpawnManager), true)]
        public class SpawnManager_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                if (Application.productName.Equals("BL360 Plugin"))
                {
                    serializedObject.Update();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("SpawnPoint"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnCamera"), true);

                    if (GUI.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(this);
                    }
                }
            }
        }
#endif
    }
}