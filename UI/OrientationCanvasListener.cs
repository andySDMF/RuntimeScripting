using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(CanvasScaler))]
    public class OrientationCanvasListener : MonoBehaviour
    {
        [SerializeField]
        private RectTransform root;
        [SerializeField]
        private bool scaleResolution = true;

        private Vector2 defaultReferenceResolution;
        private OrientationLetterbox m_letterbox;

        public System.Action<OrientationType, int , int> OnOrientationChanged { get; set; }

        public static float AspectRatioValue
        {
            get;
            private set;
        }

        public static float CanvasMultiplier
        {
            get { return CanvasMultiplier; }
        }

        private float canvasMultiplier;

        private void Awake()
        {
            if (!AppManager.IsCreated && GetComponentInParent<WebClientSimulator>() == null) return;

            OrientationManager.Instance.OnOrientationChanged += OnOrientationChange;
            defaultReferenceResolution = GetComponent<CanvasScaler>().referenceResolution;
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if(OrientationManager.Instance.RecievedWebResponse)
            {
                Vector2 size = OrientationManager.Instance.ScreenSize;
                OnOrientationChange(OrientationManager.Instance.CurrentOrientation, (int)size.x, (int)size.y);
            }
        }

        private void OnDestroy()
        {
            if (!AppManager.IsCreated) return;

            OrientationManager.Instance.OnOrientationChanged -= OnOrientationChange;
        }

        public void OnOrientationChange(OrientationType orientation, int width, int height)
        {
            if(m_letterbox == null)
            {
                UnityEngine.Object prefab = Resources.Load("_ORIENTATION_LETTERBOX");

                GameObject letterbox = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
                letterbox.transform.localScale = Vector3.one;
                RectTransform rectT = letterbox.GetComponent<RectTransform>();
                rectT.anchorMin = new Vector2(0.0f, 0.0f);
                rectT.anchorMax = new Vector2(1.0f, 1.0f);
                rectT.anchoredPosition = new Vector2(0, 0);
                m_letterbox = letterbox.GetComponent<OrientationLetterbox>();
            }

            if (orientation.Equals(OrientationType.portrait))
            {
                float padding = 0;

                //need to calculate the padding based on width and height

                if (scaleResolution)
                {
                    float aspectRatio = (float)height / width;
                    canvasMultiplier = defaultReferenceResolution.x / (width + height);
                    canvasMultiplier = aspectRatio + canvasMultiplier;

                    GetComponent<CanvasScaler>().referenceResolution = new Vector2(defaultReferenceResolution.x * canvasMultiplier, defaultReferenceResolution.y * canvasMultiplier);
                    padding = (height - width) / 2 + OrientationManager.Instance.ExtraPadding;
                    padding = (padding * canvasMultiplier) + OrientationManager.Instance.ExtraPadding;
                }
                else
                {
                    AspectRatioValue = (float)height / width;
                    padding = (height - width) / 2 + OrientationManager.Instance.ExtraPadding;
                }

                root.offsetMin = new Vector2(padding, root.offsetMin.y);
                root.offsetMax = new Vector2(-padding, root.offsetMax.y);

                m_letterbox.Set(GetComponent<CanvasScaler>().referenceResolution, padding);
            }
            else
            {
                float padding = OrientationManager.Instance.ExtraPadding;

                canvasMultiplier = 1.0f;
                AspectRatioValue = 1.0f;
                GetComponent<CanvasScaler>().referenceResolution = defaultReferenceResolution;
                root.offsetMin = new Vector2(padding, root.offsetMin.y);
                root.offsetMax = new Vector2(-padding, root.offsetMax.y);

                m_letterbox.Set(GetComponent<CanvasScaler>().referenceResolution, padding);
            }

            if(!scaleResolution)
            {
                IOrientationUI[] allUIListeners = GetComponentsInChildren<IOrientationUI>(true);

                for (int i = 0; i < allUIListeners.Length; i++)
                {
                    allUIListeners[i].Adjust(AspectRatioValue);
                }
            }

            if(OnOrientationChanged != null)
            {
                OnOrientationChanged.Invoke(orientation, width, height);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OrientationCanvasListener), true)]
        public class OrientationCanvasListener_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.LabelField("Monitor Object", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("root"), true);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Behaviour", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleResolution"), true);

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif

#if UNITY_EDITOR
        private void SelectCustomSize(GameViewSizeType viewSizeType, GameViewSizeGroupType sizeGroupType, int width, int height, string text)
        {
            int idx = FindSize(GameViewSizeGroupType.Standalone, width, height);
            SetSize(idx);
        }

        private void SetSize(int index)
        {
            var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var selectedSizeIndexProp = gvWndType.GetProperty("selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var gvWnd = EditorWindow.GetWindow(gvWndType);
            selectedSizeIndexProp.SetValue(gvWnd, index, null);
        }

        private enum GameViewSizeType
        {
            AspectRatio, FixedResolution
        }

        private int FindSize(GameViewSizeGroupType sizeGroupType, int width, int height)
        {
            // goal:
            // GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupType);
            // int sizesCount = group.GetBuiltinCount() + group.GetCustomCount();
            // iterate through the sizes via group.GetGameViewSize(int index)

            var group = GetGroup(sizeGroupType);
            var groupType = group.GetType();
            var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
            var getCustomCount = groupType.GetMethod("GetCustomCount");
            int sizesCount = (int)getBuiltinCount.Invoke(group, null) + (int)getCustomCount.Invoke(group, null);
            var getGameViewSize = groupType.GetMethod("GetGameViewSize");
            var gvsType = getGameViewSize.ReturnType;
            var widthProp = gvsType.GetProperty("width");
            var heightProp = gvsType.GetProperty("height");
            var indexValue = new object[1];
            for (int i = 0; i < sizesCount; i++)
            {
                indexValue[0] = i;
                var size = getGameViewSize.Invoke(group, indexValue);
                int sizeWidth = (int)widthProp.GetValue(size, null);
                int sizeHeight = (int)heightProp.GetValue(size, null);
                if (sizeWidth == width && sizeHeight == height)
                    return i;
            }
            return -1;
        }

        private int FindSize(GameViewSizeGroupType sizeGroupType, string text)
        {
            // GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupType);
            // string[] texts = group.GetDisplayTexts();
            // for loop...

            var group = GetGroup(sizeGroupType);
            var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts");
            var displayTexts = getDisplayTexts.Invoke(group, null) as string[];
            for (int i = 0; i < displayTexts.Length; i++)
            {
                string display = displayTexts[i];
                // the text we get is "Name (W:H)" if the size has a name, or just "W:H" e.g. 16:9
                // so if we're querying a custom size text we substring to only get the name
                // You could see the outputs by just logging
                // Debug.Log(display);
                int pren = display.IndexOf('(');
                if (pren != -1)
                    display = display.Substring(0, pren - 1); // -1 to remove the space that's before the prens. This is very implementation-depdenent
                if (display == text)
                    return i;
            }
            return -1;
        }

        private static object GetGroup(GameViewSizeGroupType type)
        {
            // gameViewSizesInstance  = ScriptableSingleton<GameViewSizes>.instance;
            var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instanceProp = singleType.GetProperty("instance");
            var getGroup = sizesType.GetMethod("GetGroup");
            var gameViewSizesInstance = instanceProp.GetValue(null, null);

            return getGroup.Invoke(gameViewSizesInstance, new object[] { (int)type });
        }

#endif
    }
}
