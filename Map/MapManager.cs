using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class MapManager : Singleton<MapManager>
    {
        public static MapManager Instance
        {
            get
            {
                return ((MapManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        [SerializeField]
        private GameObject topdownCamera;

        [SerializeField]
        private GameObject imageMap;

        private bool m_topDownEnabled = false;
        private RenderMapMode m_renderMode = RenderMapMode.Camera;
        private MainTopDownCamera m_mainCameraController;

        public bool TopDownViewActive
        {
            get
            {
                return m_topDownEnabled;
            }
        }

        public bool HasController
        {
            get
            {
                return m_renderMode.Equals(RenderMapMode.Camera) ? m_mainCameraController == null ? false : true : true;
            }
        }

        public List<string> GetLabels
        {
            get
            {
                if (m_renderMode.Equals(RenderMapMode.Camera))
                {
                    if(m_mainCameraController != null)
                    {
                        return m_mainCameraController.CameraLabels;
                    }
                }
                else
                {
                    MapScene mapScene = CoreManager.Instance.projectSettings.GetMapScene(gameObject.scene.name);

                    List<string> temp = new List<string>();
                    mapScene.areas.ForEach(x => temp.Add(x.label.ToUpper()));

                    return temp;
                }

                return null;
            }
        }

        public Sprite GetIcon(string id)
        {
            if (m_renderMode.Equals(RenderMapMode.Camera))
            {
                if (m_mainCameraController != null)
                {
                    return m_mainCameraController.GetIcon(id);
                }
            }

            return null;
        }

        public List<MapDefinedKey> GetKey
        {
            get
            {
                if (m_renderMode.Equals(RenderMapMode.Camera))
                {
                    if (m_mainCameraController != null)
                    {
                        return m_mainCameraController.GetKey();
                    }
                }
                else
                {
                    MapScene mapScene = CoreManager.Instance.projectSettings.GetMapScene(gameObject.scene.name);

                    List<MapDefinedKey> temp = new List<MapDefinedKey>();

                    if(mapScene.keyType.Equals(MapKeyType.Defined))
                    {
                        mapScene.definedKeys.ForEach(x => temp.Add(x));
                    }
                    else
                    {
                        MapDefinedKey dKey = new MapDefinedKey();
                        dKey.keyName = "$IMAGE";
                        dKey.keyIcon = mapScene.keyImage;

                        temp.Add(dKey);
                    }

                    return temp;
                }

                return null;
            }
        }

        public bool UseLabels
        {
            get
            {
                if (m_renderMode.Equals(RenderMapMode.Camera))
                {
                    return CoreManager.Instance.projectSettings.useMultipleMaps;
                }
                else
                {
                    MapScene mapScene = CoreManager.Instance.projectSettings.GetMapScene(gameObject.scene.name);
                    return mapScene.useMultipleMaps;
                }
            }
        }

        public MainTopDownCamera TopDownCameraExists
        {
            get
            {
                return topdownCamera.GetComponentInChildren<MainTopDownCamera>(true);
            }
        }

        private void Start()
        {
            List<MainTopDownCamera> tCams = FindObjectsByType <MainTopDownCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
            tCams.Remove(topdownCamera.GetComponentInChildren<MainTopDownCamera>(true));

            m_renderMode = CoreManager.Instance.projectSettings.mapRenderMode;

            if (tCams.Count > 0)
            {
                if (tCams.Count > 1)
                {
                    Debug.Log("More than 1 MainTopDownCamera exists in the scene. Using index 0");
                }
                else
                {
                    Debug.Log("Using MainTopDownCamera");
                }

                topdownCamera = tCams[0].gameObject;
                m_mainCameraController = tCams[0];
            }
            else
            {
                Debug.Log("Using Default Top Down Camera");
            }
        }

        /// <summary>
        /// Toggle the topdown state of the navigation
        /// </summary>
        /// <param name="isOn"></param>
        public void ToggleTopdown(bool isOn)
        {
            PlayerManager.Instance.ToggleThirdPersonMenuUIVisibility(isOn);
            HUDManager.Instance.ToggleHUDControl("MAPCAMERA_CONTROL", isOn);

            if (m_renderMode.Equals(RenderMapMode.Camera))
            {
                topdownCamera.SetActive(isOn);

                if (TopDownCameraExists)
                {
                    TopDownCameraExists.ToogleMapIcons(isOn);
                }
            }
            else
            {
                imageMap.SetActive(isOn);

                if (isOn)
                {
                    StartCoroutine(WaitFrame());
                }
            }

            if (PlayerManager.Instance.GetLocalPlayer() != null)
            {
                PlayerManager.Instance.GetLocalPlayer().ToggleTopDown(isOn);
            }

            // Disable current teleport type during topdown
            NavigationManager.Instance.ToggleTeleport(isOn);

            m_topDownEnabled = isOn;
        }

        public void MoveToPosition(string label)
        {
            if (m_renderMode.Equals(RenderMapMode.Camera))
            {
                if (m_mainCameraController != null)
                {
                    m_mainCameraController.MoveToPosition(label);
                }
            }
            else
            {
                MapScene mapScene = CoreManager.Instance.projectSettings.GetMapScene(gameObject.scene.name);
                string resourceImage = label.Equals("MAIN") ? mapScene.image : mapScene.areas.FirstOrDefault(x => x.label.Equals(label)).image;
                
                string path = Application.dataPath + "/Resources/" + resourceImage;
                ContentsManager.ContentFileInfo fInfo = new ContentsManager.ContentFileInfo();
                fInfo.url = path;

                ContentImageScreen iScreen = imageMap.GetComponentInChildren<ContentImageScreen>(true);


                if (iScreen.URL.Equals(fInfo.url))
                {
                    iScreen.ResetControl();
                    return;
                }

                iScreen.Unload();

                iScreen.IsNetworked = false;
                iScreen.Load(fInfo);
                iScreen.ResetControl();
            }
        }

        private IEnumerator WaitFrame()
        {
            MapScene mapScene = CoreManager.Instance.projectSettings.GetMapScene(gameObject.scene.name);
            string resourceImage = "";

            if (mapScene != null)
            {
                resourceImage = mapScene.image;
            }
            else
            {
                Debug.Log("Map Scene could not be found. Cannot load map image for [" + gameObject.scene.name + "]");
                imageMap.SetActive(false);
                yield break;
            }

            yield return new WaitForEndOfFrame();

            string path = resourceImage;
            ContentsManager.ContentFileInfo fInfo = new ContentsManager.ContentFileInfo();
            fInfo.url = path;

            ContentImageScreen iScreen = imageMap.GetComponentInChildren<ContentImageScreen>(true);
            iScreen.IsNetworked = false;
            iScreen.Load(fInfo);
        }

        /// <summary>
        /// Only works with Camera not Image
        /// </summary>
        public void ZoomIn()
        {
            if (!m_renderMode.Equals(RenderMapMode.Camera)) return;

            if (m_mainCameraController != null)
            {
                m_mainCameraController.ZoomIn();
            }
        }

        /// <summary>
        /// Only works with Camera not Image
        /// </summary>
        public void ZoomOut()
        {
            if (!m_renderMode.Equals(RenderMapMode.Camera)) return;

            if (m_mainCameraController != null)
            {
                m_mainCameraController.ZoomOut();
            }
        }

        [System.Serializable]
        public class MapScene
        {
            public string scene;
            public string image;
            public bool useMultipleMaps = true;
            public List<MapSceneArea> areas = new List<MapSceneArea>();

            public MapManager.MapKeyType keyType = MapManager.MapKeyType.Defined;
            public Sprite keyImage;
            public List<MapManager.MapDefinedKey> definedKeys = new List<MapManager.MapDefinedKey>();
        }

        [System.Serializable]
        public class MapSceneArea
        {
            public string label;
            public string image;
        }

        [System.Serializable]
        public enum MapKeyType { Image, Defined }

        [System.Serializable]
        public class MapDefinedKey
        {
            public string keyName = "";
            public Sprite keyIcon;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(MapManager), true)]
        public class MapManager_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("topdownCamera"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("imageMap"), true);

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

    [System.Serializable]
    public enum RenderMapMode { Camera, Image }
}
