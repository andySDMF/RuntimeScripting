using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class MainTopDownCamera : MonoBehaviour
    {
        [SerializeField]
        private GameObject cameraObject;

        [SerializeField]
        private CameraMapMode cameraMode = CameraMapMode.Orthographic;

        [SerializeField]
        private float projectionFOV = 60.0f;

        [SerializeField]
        private float orthoSize = 10;

        [SerializeField]
        private float maxZoom = 5;

        [SerializeField]
        private float zoomSpeed = 10;

        [SerializeField]
        private float fractionToZoomIn = 5.0f;

        [SerializeField]
        private float panSpeed = 10;

        [SerializeField]
        private List<CameraPosition> cameraPositions;

        [SerializeField]
        private MapManager.MapKeyType keyType = MapManager.MapKeyType.Defined;

        [SerializeField]
        private Sprite keyImage;

        [SerializeField]
        private List<MapManager.MapDefinedKey> definedKeys = new List<MapManager.MapDefinedKey>();

        private float m_minZoom;
        private float m_currentZoom = 0;
        private Vector3 m_origin;
        private bool m_lerpToLabel = false;
        private Vector3 m_lerpTo = Vector3.zero;
        private float m_lerpOrthoTo = 0;
        private CameraMapMode m_cacheCameraMode;

        private Unity.Cinemachine.CinemachineCamera vCamera;
        private List<GameObject> m_mapIcons = new List<GameObject>();
        private MapLabel[] m_mapLabels;

        public List<string> CameraLabels
        {
            get
            {
                List<string> temp = new List<string>();

                cameraPositions.ForEach(x => temp.Add(x.label.ToUpper()));

                return temp;
            }
        }

        public Sprite GetIcon(string id)
        {
            CameraPosition cPos = cameraPositions.FirstOrDefault(x => x.label.ToUpper().Equals(id));

            if(cPos != null)
            {
                return cPos.icon;
            }

            return null;
        }

        public List<MapManager.MapDefinedKey> GetKey()
        {
            List<MapManager.MapDefinedKey> temp = new List<MapManager.MapDefinedKey>();

            if (keyType.Equals(MapManager.MapKeyType.Image))
            {
                MapManager.MapDefinedKey dKey = new MapManager.MapDefinedKey();
                dKey.keyName = "$IMAGE";
                dKey.keyIcon = keyImage;

                temp.Add(dKey);
            }
            else
            {
                temp.AddRange(definedKeys);
            }

            return temp;
        }

        private void Awake()
        {
            if (!AppManager.IsCreated) return;

            vCamera = GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>(true);

            if (cameraMode.Equals(CameraMapMode.Orthographic))
            {
                m_minZoom = orthoSize;
            }
            else
            {
                m_minZoom = gameObject.transform.localPosition.y;
            }

            m_currentZoom = m_minZoom;
            m_origin = gameObject.transform.localPosition;
            m_lerpOrthoTo = m_currentZoom;

            if (vCamera != null)
            {
                vCamera.Lens.ModeOverride = cameraMode.Equals(CameraMapMode.Orthographic) ? Unity.Cinemachine.LensSettings.OverrideModes.Orthographic : Unity.Cinemachine.LensSettings.OverrideModes.Perspective;
                vCamera.Lens.FieldOfView = projectionFOV;
                vCamera.Lens.OrthographicSize = orthoSize;
            }

            m_mapLabels = FindObjectsByType<MapLabel>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (cameraObject == null) return;

            //controller for this camera.
            //scroll wheel for mous zoom - editor and PC 
            float scrollWheelInput = InputManager.Instance.GetMouseScrollWheel();
            float yPos = 0;

            for (int i = 0; i < m_mapIcons.Count; i++)
            {
                float scale = Vector3.Distance(m_mapIcons[i].transform.localPosition, transform.localPosition) / 1000;
                m_mapIcons[i].transform.localScale = new Vector3(scale, scale, 1);
            }

            for(int i = 0; i < m_mapLabels.Length; i++)
            {
                float scale = Vector3.Distance(m_mapLabels[i].transform.localPosition, transform.localPosition) / 2500;
                m_mapLabels[i].transform.localScale = new Vector3(scale, scale, 1);
            }

            if (!scrollWheelInput.Equals(0.0f))
            {
                m_lerpToLabel = false;

                if (Mathf.Abs(scrollWheelInput) > float.Epsilon)
                {
                    m_currentZoom *= 1 + scrollWheelInput * 1;
                    m_currentZoom = Mathf.Clamp(m_currentZoom, m_minZoom, maxZoom);

                    if (cameraMode.Equals(CameraMapMode.Orthographic))
                    {
                        if (Mathf.Abs(vCamera.Lens.OrthographicSize - m_currentZoom) > 0.0f)
                        {
                            yPos = Mathf.Lerp(vCamera.Lens.OrthographicSize, m_currentZoom, zoomSpeed * Time.deltaTime);
                            vCamera.Lens.OrthographicSize = yPos;
                        }
                    }
                    else
                    {
                        if (Mathf.Abs(gameObject.transform.localPosition.y - m_currentZoom) > 0.0f)
                        {
                            yPos = Mathf.Lerp(gameObject.transform.localPosition.y, m_currentZoom, zoomSpeed * Time.deltaTime);
                            gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, yPos, gameObject.transform.localPosition.z);
                        }
                    }
                }

                return;
            }
            else
            {
                if (!m_lerpToLabel)
                {
                    LerpZoom();
                }
            }

            if (InputManager.Instance.GetMouseButton(0))
            {
                m_lerpToLabel = false;

                Vector2 mousePosition = InputManager.Instance.GetMouseDelta("Mouse X", "Mouse Y");

                float camY = gameObject.transform.localPosition.y;
                bool canMove = cameraMode.Equals(CameraMapMode.Perspective) ? camY < m_minZoom - 1 && camY >= maxZoom : vCamera.Lens.OrthographicSize < m_minZoom - 0.1f;

                if (canMove)
                {
                    //need to check if mouse hit the any collider, this is to ensure the user cannot pan too far
                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMousePosition());

                    if (Physics.Raycast(ray, out hit, 5000))
                    {
                        float posX = gameObject.transform.localPosition.x - mousePosition.x;
                        float posY = gameObject.transform.localPosition.z - mousePosition.y;

                        float xPos = Mathf.Lerp(gameObject.transform.localPosition.x, posX, panSpeed * Time.deltaTime);
                        float zPos = Mathf.Lerp(gameObject.transform.localPosition.z, posY, panSpeed * Time.deltaTime);

                        gameObject.transform.localPosition = new Vector3(xPos, gameObject.transform.localPosition.y, zPos);
                    }
                }
            }

            if (m_lerpToLabel)
            {
                if (Vector3.Distance(gameObject.transform.localPosition, m_lerpTo) > 0.1f)
                {
                    gameObject.transform.localPosition = Vector3.Lerp(gameObject.transform.localPosition, m_lerpTo, Time.deltaTime * 10);
                }
                else
                {
                    m_lerpToLabel = false;
                }

                if (cameraMode.Equals(CameraMapMode.Orthographic))
                {
                    vCamera.Lens.OrthographicSize = Mathf.Lerp(vCamera.Lens.OrthographicSize, m_lerpOrthoTo, zoomSpeed * Time.deltaTime);
                }
            }

            if (m_currentZoom >= m_minZoom && !m_lerpToLabel)
            {
                if (cameraMode.Equals(CameraMapMode.Orthographic))
                {
                    if (Mathf.Abs(vCamera.Lens.OrthographicSize - m_currentZoom) > 0.0f)
                    {
                        vCamera.Lens.OrthographicSize = Mathf.Lerp(vCamera.Lens.OrthographicSize, m_minZoom, zoomSpeed * Time.deltaTime);
                    }
                }

                if (!gameObject.transform.localPosition.Equals(m_origin))
                {
                    gameObject.transform.localPosition = Vector3.Lerp(gameObject.transform.localPosition, m_origin, Time.deltaTime * 10);
                }
            }
        }

        private void LerpZoom()
        {
            if (m_currentZoom < maxZoom)
            {
                m_currentZoom = maxZoom;
            }

            if (m_currentZoom > m_minZoom)
            {
                m_currentZoom = m_minZoom;
            }

            if (cameraMode.Equals(CameraMapMode.Orthographic))
            {
                if (Mathf.Abs(vCamera.Lens.OrthographicSize - m_currentZoom) > 0.0f)
                {
                    float yPos = Mathf.Lerp(vCamera.Lens.OrthographicSize, m_currentZoom, zoomSpeed * Time.deltaTime);
                    vCamera.Lens.OrthographicSize = yPos;
                }
            }
            else
            {
                if (Mathf.Abs(gameObject.transform.localPosition.y - m_currentZoom) > 0.0f)
                {
                    float yPos = Mathf.Lerp(gameObject.transform.localPosition.y, m_currentZoom, zoomSpeed * Time.deltaTime);
                    gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, yPos, gameObject.transform.localPosition.z);
                }
            }
        }

        public void MoveToPosition(string id)
        {
            if (id.Equals("MAIN"))
            {
                m_currentZoom = m_minZoom;
                m_lerpTo = m_origin;
                m_lerpToLabel = true;
                return;
            }

            CameraPosition cPos = cameraPositions.FirstOrDefault(x => x.label.ToUpper().Equals(id));

            if (cPos != null)
            {
                m_currentZoom = cPos.position.y;
                m_lerpTo = cPos.position;
                m_lerpOrthoTo = cPos.ortho;
                m_lerpToLabel = true;
            }
        }

        public void ZoomIn()
        {
            if (m_lerpToLabel) return;

            m_currentZoom -= fractionToZoomIn;
        }

        public void ZoomOut()
        {
            if (m_lerpToLabel) return;

            m_currentZoom += fractionToZoomIn;
        }

        public void ToogleMapIcons(bool toggle)
        {
            if (!MapManager.Instance.UseLabels) return;

            if(toggle)
            {
                //create all the map icons
                for (int i = 0; i < cameraPositions.Count; i++)
                {
                    if(cameraPositions[i].icon != null)
                    {
                        GameObject icon = new GameObject();
                        icon.name = "CAMICON_" + cameraPositions[i].label;
                        icon.transform.SetParent(transform.parent);
                        icon.transform.localPosition = new Vector3(cameraPositions[i].position.x, maxZoom, cameraPositions[i].position.z);
                        icon.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                        icon.transform.localEulerAngles = new Vector3(90, 0, 0);
                        m_mapIcons.Add(icon);

                        //add components
                        Canvas can = icon.AddComponent<Canvas>();
                        GraphicRaycaster rayCaster = icon.AddComponent<GraphicRaycaster>();
                        Image img = icon.AddComponent<Image>();
                        img.sprite = cameraPositions[i].icon;
                        img.SetNativeSize();

                        MapIcon mIcon = icon.AddComponent<MapIcon>();
                        mIcon.Set(cameraPositions[i].label);
                    }
                }
            }
            else
            {
                int count = m_mapIcons.Count;

                for (int i = 0; i < count; i++)
                {
                    Destroy(m_mapIcons[i]);
                }

                m_mapIcons.Clear();
            }
        }

        [System.Serializable]
        public enum CameraMapMode { Orthographic, Perspective }

        [System.Serializable]
        private class CameraPosition
        {
            public string label = "";
            public Vector3 position;
            public float ortho = 10f;
            public Sprite icon;
        }

        [System.Serializable]
        private class CameraPositionKey
        {
            public string id = "";
            public Sprite icon;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(MainTopDownCamera), true)]
        public class MainTopDownCamera_Editor : BaseInspectorEditor
        {
            private MainTopDownCamera script;
            public Unity.Cinemachine.CinemachineCamera vCamera;
            public bool isOrtho = false;

            private static bool positionFoldout = false;
            private SerializedProperty positions;
            private Color m_active = Color.magenta;
            private Color m_normal = Color.white;
            private CameraPosition editablePosition;
            private bool editingDefault = false;

            private GameObject cameraBrain = null;
            private Vector3 originalPosition;
            private float originalOrthoSize = 0;
            private GUIStyle greenFont;
            private GameObject controller;

            private MapTopdownConfiguratorWindow window;

            public float Mode;

            private void OnEnable()
            {
                GetBanner();

                script = (MainTopDownCamera)target;
                positions = serializedObject.FindProperty("cameraPositions");
                originalPosition = script.transform.localPosition;
                originalOrthoSize = script.orthoSize;
                vCamera = script.gameObject.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>(true);

                greenFont = new GUIStyle();
                greenFont.normal.textColor = Color.green;
            }

            private void OnDisable()
            {
                DestroyCameraBrain();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraObject"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraMode"), true);

                Mode = serializedObject.FindProperty("cameraMode").enumValueIndex;

                if (serializedObject.FindProperty("cameraMode").enumValueIndex == 0)
                {
                    isOrtho = true;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("orthoSize"), true);
                }
                else
                {
                    isOrtho = false;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("projectionFOV"), true);
                }

                if (vCamera != null)
                {
                    vCamera.Lens.ModeOverride = isOrtho ? Unity.Cinemachine.LensSettings.OverrideModes.Orthographic : Unity.Cinemachine.LensSettings.OverrideModes.Perspective;
                    vCamera.Lens.FieldOfView = serializedObject.FindProperty("projectionFOV").floatValue;
                    vCamera.Lens.OrthographicSize = serializedObject.FindProperty("orthoSize").floatValue;
                }

                string editing = editingDefault ? "Done" : "Edit";
                Color bckColor = editingDefault ? m_active : m_normal;

                GUI.backgroundColor = bckColor;

                if (GUILayout.Button(editing))
                {
                    editingDefault = !editingDefault;

                    if (editing.Equals("Edit"))
                    {
                        editablePosition = null;
                        //create camera and update positions based ont camera pos
                        CreateCameraBrain();
                    }
                    else
                    {
                        editablePosition = null;
                        //destroy camera
                        DestroyCameraBrain();
                    }
                }

                if (editingDefault)
                {
                    EditorGUILayout.LabelField("Use the transform script to adjust the veiwport of the camera", greenFont);

                    if (serializedObject.FindProperty("cameraMode").enumValueIndex == 0)
                    {
                        originalPosition = new Vector3(script.transform.localPosition.x, originalPosition.y, script.transform.localPosition.z);
                        vCamera.Lens.OrthographicSize = script.orthoSize;
                        originalOrthoSize = vCamera.Lens.OrthographicSize;
                    }
                    else
                    {
                        originalPosition = script.transform.localPosition;
                    }
                }

                GUI.backgroundColor = m_normal;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Control", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxZoom"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("zoomSpeed"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fractionToZoomIn"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("panSpeed"), true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Key", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("keyType"), true);

                if (serializedObject.FindProperty("keyType").enumValueIndex == 0)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("keyImage"), true);
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("definedKeys"), true);
                }

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                    
                positionFoldout = EditorGUILayout.Foldout(positionFoldout, "Positions");
                serializedObject.FindProperty("cameraPositions").arraySize = EditorGUILayout.IntField(serializedObject.FindProperty("cameraPositions").arraySize, GUILayout.Width(50));

                EditorGUILayout.EndHorizontal();

                if (positionFoldout)
                {
                    EditorGUI.indentLevel++;

                    for (int i = 0; i < positions.arraySize; i++)
                    {
                        positions.GetArrayElementAtIndex(i).FindPropertyRelative("label").stringValue = EditorGUILayout.TextField("", positions.GetArrayElementAtIndex(i).FindPropertyRelative("label").stringValue);
                        positions.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector3Value = EditorGUILayout.Vector3Field("", positions.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector3Value);
                        positions.GetArrayElementAtIndex(i).FindPropertyRelative("icon").objectReferenceValue = (Sprite)EditorGUILayout.ObjectField("Icon", positions.GetArrayElementAtIndex(i).FindPropertyRelative("icon").objectReferenceValue, typeof(Sprite), false, GUILayout.ExpandWidth(true));

                        if (serializedObject.FindProperty("cameraMode").enumValueIndex == 0)
                        {
                            positions.GetArrayElementAtIndex(i).FindPropertyRelative("ortho").floatValue = EditorGUILayout.FloatField("", positions.GetArrayElementAtIndex(i).FindPropertyRelative("ortho").floatValue);
                            EditorGUILayout.Space();
                        }

                        EditorGUILayout.BeginHorizontal();

                        editing = script.cameraPositions[i].Equals(editablePosition) ? "Done" : "Edit";
                        bckColor = script.cameraPositions[i].Equals(editablePosition) ? m_active : m_normal;

                        GUI.backgroundColor = bckColor;

                        if (GUILayout.Button(editing))
                        {
                            if (editing.Equals("Edit"))
                            {
                                if (editingDefault)
                                {
                                    editingDefault = false;
                                }

                                editablePosition = script.cameraPositions[i];
                                //create camera and update positions based ont camera pos
                                CreateCameraBrain();
                            }
                            else
                            {
                                editablePosition = null;
                                //destroy camera
                                DestroyCameraBrain();
                            }
                        }
   
                        if (editablePosition == script.cameraPositions[i])
                        {
                            if (serializedObject.FindProperty("cameraMode").enumValueIndex == 0)
                            {
                                script.cameraPositions[i].position = new Vector3(script.transform.localPosition.x, originalPosition.y, script.transform.localPosition.z);
                                vCamera.Lens.OrthographicSize = script.orthoSize;
                                script.cameraPositions[i].ortho = vCamera.Lens.OrthographicSize;
                            }
                            else
                            {
                                script.cameraPositions[i].position = script.transform.localPosition;
                            }
                        }

                        GUI.backgroundColor = m_normal;

                        if (GUILayout.Button("Delete"))
                        {
                            //remove from array
                            script.cameraPositions.RemoveAt(i);
                            GUIUtility.ExitGUI();
                            return;
                        }

                        EditorGUILayout.EndHorizontal();

                        if (!editingDefault)
                        {
                            if (editablePosition == script.cameraPositions[i])
                            {
                                EditorGUILayout.LabelField("Use the transform script to adjust the veiwport of the camera", greenFont);
                            }
                        }

                        if (i < positions.arraySize - 1)
                        {
                            EditorGUILayout.Space();
                        }
                    }

                    EditorGUI.indentLevel--;

                    EditorGUILayout.Space();

                    if (GUILayout.Button("Add"))
                    {
                        script.cameraPositions.Add(new CameraPosition());
                        GUIUtility.ExitGUI();
                        return;
                    }
                }

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(script);
                }
            }

            private void CreateCameraBrain()
            {
                UnityEngine.Object prefab = (GameObject)GetAsset<GameObject>("Assets/com.brandlab360.core/Editor/Prefabs/EDITOR_CAMERABRAIN.prefab");
                bool cameraBrainCreated = false;

                if (cameraBrain == null)
                {
                    cameraBrainCreated = true;
                    cameraBrain = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                }

                cameraBrain.transform.position = new Vector3(0, 0, 0);

                //set camera position
                if (serializedObject.FindProperty("cameraMode").enumValueIndex == 0)
                {
                    //ortho
                    if(editingDefault)
                    {
                        script.transform.localPosition = originalPosition;
                    }
                    else
                    {
                        script.transform.localPosition = new Vector3(editablePosition.position.x, originalPosition.y, editablePosition.position.z);
                    }
                    
                    vCamera.Lens.ModeOverride = Unity.Cinemachine.LensSettings.OverrideModes.Orthographic;
                    vCamera.Lens.OrthographicSize = editablePosition.ortho;
                }
                else
                {
                    if (editingDefault)
                    {
                        script.transform.localPosition = originalPosition;
                    }
                    else
                    {
                        script.transform.localPosition = editablePosition.position;
                    }

                    vCamera.Lens.ModeOverride = Unity.Cinemachine.LensSettings.OverrideModes.Perspective;
                    vCamera.Lens.FieldOfView = script.projectionFOV;
                }

                if (cameraBrainCreated)
                {
                    controller = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    controller.GetComponent<Renderer>().material = (Material)GetAsset<Material>("Assets/com.brandlab360.core/Runtime/Materials/Red.mat");
                    controller.transform.SetParent(script.transform.parent);
                    controller.transform.localPosition = new Vector3(script.transform.localPosition.x, script.transform.localPosition.y - 5, script.transform.localPosition.z);
                    controller.transform.SetParent(script.transform);
                    controller.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                }
                else
                {
                    window.Set(script.gameObject, script.orthoSize, this);
                }

                Selection.activeTransform = script.transform;

                //switch to game view
                EditorApplication.ExecuteMenuItem("Window/General/Game");

                if (cameraBrainCreated)
                {
                    window = (MapTopdownConfiguratorWindow)EditorWindow.GetWindow(typeof(MapTopdownConfiguratorWindow));
                    window.Set(script.gameObject, script.orthoSize, this);
                    window.maxSize = new Vector2(800f, 500f);
                    window.minSize = window.maxSize;
                    window.Show();
                }
            }

            public void DestroyCameraBrain(bool fromWindow = false)
            {
                if(cameraBrain != null)
                {
                    DestroyImmediate(cameraBrain);
                    cameraBrain = null;

                    //set camera position
                    if (serializedObject.FindProperty("cameraMode").enumValueIndex == 0)
                    {
                        //ortho
                        script.transform.localPosition = originalPosition;
                        vCamera.Lens.ModeOverride = Unity.Cinemachine.LensSettings.OverrideModes.Orthographic;
                        vCamera.Lens.OrthographicSize = script.orthoSize;
                    }
                    else
                    {
                        script.transform.localPosition = originalPosition;
                        vCamera.Lens.ModeOverride = Unity.Cinemachine.LensSettings.OverrideModes.Perspective;
                        vCamera.Lens.FieldOfView = script.projectionFOV;
                    }
                }

                if(controller != null)
                {
                    DestroyImmediate(controller);
                }

                if(window != null && !fromWindow)
                {
                    window.ControllerEditor = null;
                    window.Close();
                    window = null;
                }

                editablePosition = null;

                if(editingDefault)
                {
                    editingDefault = false;
                }
            }

            public void EditOrtho(float val)
            {
                script.orthoSize = val;
            }

            private static Object GetAsset<T>(string path)
            {
                Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(T));

                if (obj == null)
                {
                    obj = GetPackageAsset<T>(path);
                }

                return obj;
            }

            private static Object GetPackageAsset<T>(string path)
            {
                return AssetDatabase.LoadAssetAtPath(path.Replace("Assets", "Packages"), typeof(T));
            }
        }

        public class MapTopdownConfiguratorWindow : EditorWindow
        {
            private GameObject Controller;
            public MainTopDownCamera_Editor ControllerEditor;
            private AppSettings m_settings;
            private Vector3 posController = Vector3.zero;
            private float add = 1.0f;
            private bool editPosition = false;
            private bool editOrtho = false;
            private float orthoValue;

            public void Set(GameObject controller, float ortho, MainTopDownCamera_Editor controllerEditor)
            {
                Controller = controller;
                ControllerEditor = controllerEditor;
                AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");

                if (appReferences != null)
                {
                    m_settings = appReferences.Settings;
                }
                else
                {
                    m_settings = Resources.Load<AppSettings>("ProjectAppSettings");
                }
                posController = controller.transform.localPosition;
                orthoValue = ortho;
            }

            private void Update()
            {
                if(Controller != null)
                {
                    Controller.transform.localPosition = Vector3.Lerp(Controller.transform.localPosition, posController, Time.deltaTime * 2);

                    if(ControllerEditor.isOrtho)
                    {
                        ControllerEditor.EditOrtho(orthoValue);
                    }
                }
            }

            private void OnDestroy()
            { 
                if(ControllerEditor != null)
                {
                    ControllerEditor.DestroyCameraBrain(true);
                }
            }

            private void OnGUI()
            {
                if (Application.isPlaying)
                {
                    Close();
                    return;
                }

                var e = Event.current;

                if (e != null)
                {
                    if (e.type == EventType.KeyDown)
                    {
                        switch (e.keyCode)
                        {
                            case KeyCode.W:
                            case KeyCode.UpArrow:
                                posController.z += add;
                                break;
                            case KeyCode.A:
                            case KeyCode.LeftArrow:
                                posController.x -= add;
                                break;
                            case KeyCode.S:
                            case KeyCode.DownArrow:
                                posController.z -= add;
                                break;
                            case KeyCode.D:
                            case KeyCode.RightArrow:
                                posController.x += add;
                                break;
                            case KeyCode.Plus:
                            case KeyCode.KeypadPlus:

                                if(ControllerEditor.Mode == 0)
                                {
                                    orthoValue += add;
                                }
                                else
                                {
                                    posController.y += add;
                                }

                                break;
                            case KeyCode.Minus:
                            case KeyCode.KeypadMinus:

                                if (ControllerEditor.Mode == 0)
                                {
                                    orthoValue -= add;
                                }
                                else
                                {
                                    posController.y -= add;
                                }
                                break;
                        }
                    }
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

                EditorGUILayout.LabelField("TOPDOWN CAMERA CONFIGURATOR", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Use Keyboard AWSD/Arrows for movement, Plus/Minus to zoom in/out", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("If Input not working, select this window", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space();


                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Position", EditorStyles.boldLabel, GUILayout.ExpandWidth(false));

                string btnName = (editPosition) ? "Done" : "Edit";

                if (GUILayout.Button(btnName))
                {
                    editOrtho = !editOrtho;
                }

                EditorGUILayout.EndHorizontal();

                if(editPosition)
                {
                    posController = EditorGUILayout.Vector3Field("", posController);
                }
                else
                {
                    EditorGUILayout.LabelField(posController.ToString(), EditorStyles.boldLabel);
                }

                if(ControllerEditor.isOrtho)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Ortho Size", EditorStyles.boldLabel, GUILayout.ExpandWidth(false));

                    btnName = (editPosition) ? "Done" : "Edit";

                    if (GUILayout.Button(btnName))
                    {
                        editPosition = !editPosition;
                    }

                    EditorGUILayout.EndHorizontal();

                    if (editOrtho)
                    {
                        orthoValue = EditorGUILayout.FloatField("", orthoValue);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(orthoValue.ToString(), EditorStyles.boldLabel);
                    }
                }

                //draw pad

                //draw zoom
            }
        }
#endif
    }
}
