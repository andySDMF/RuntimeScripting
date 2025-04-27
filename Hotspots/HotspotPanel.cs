using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class HotspotPanel : MonoBehaviour
    {
        [Header("Hotspots")]
        [SerializeField]
        private GameObject prefab;

        [SerializeField]
        private Transform container;

        [Header("Mobile Layout")]
        [SerializeField]
        private TextMeshProUGUI titleText;

        private bool initialized = false;

        private float m_scaler;
        private float m_cacheTitleText;
        private Vector2 m_cacheSize;

        private LayoutElement m_areaLayout;
        private GridLayoutGroup m_gridLayout;

        private void Start()
        {
            if (prefab != null && container != null && !initialized) 
            {
                m_cacheTitleText = titleText.fontSize;

                m_areaLayout = container.GetComponentInParent<LayoutElement>(true);
                m_cacheSize = new Vector2(m_areaLayout.minWidth, m_areaLayout.minHeight);

                m_gridLayout = container.GetComponent<GridLayoutGroup>();

                m_scaler = AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;

                // on first load, try to load hotspots from the hotspots txt file (json)

                TextAsset txtData = (TextAsset)Resources.Load("Hotspots/hotspots");

                if (txtData != null)
                {
                    var json = "{\"Hotspots\":[";
                    json += txtData.text + "]}";

                    var hotspots = JsonConvert.DeserializeObject<HotspotCollection>(json);

                    for (int i = 0; i < hotspots.Hotspots.Length; i++)
                    {
                        var hotspot = hotspots.Hotspots[i];

                        if(!hotspot.sceneName.Equals(gameObject.scene.name))
                        {
                            continue;
                        }

                        GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity, container);
                        go.transform.localScale = Vector3.one;
                        go.GetComponentInChildren<HotspotButton>(true).Set(hotspot);
                        go.SetActive(true);
                    }
                }

                initialized = true;


                if (AppManager.Instance.Data.IsMobile || AppManager.Instance.Settings.HUDSettings.useMobileToolsForDesktop)
                {
                    transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 25);
                }

                OrientationManager.Instance.OnOrientationChanged += OnOrientation;

                OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
            }
        }

        private void OnDestroy()
        {
            if (!AppManager.IsCreated) return;

            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        private void Update()
        {
            if (ItemManager.Instance.IsHolding || ProductManager.Instance.isHolding || ChairManager.Instance.HasPlayerOccupiedChair(PlayerManager.Instance.GetLocalPlayer().ID)
                || ConfiguratorManager.instance.ActiveRTEObject != null || !HUDManager.Instance.NavigationHUDVisibility)
            {
                //close the emote HUD via the toggle
                HUDManager.Instance.GetMenuItem("Toggle_Hotspots").GetComponent<UnityEngine.UI.Toggle>().isOn = false;
            }

            if (AppManager.Instance.Data.IsMobile)
            {
                RectTransform mobileControl = HUDManager.Instance.GetHUDControlObject("MOBILE_JOYSTICK").GetComponent<RectTransform>();

                if (OrientationManager.Instance.CurrentOrientation.Equals(OrientationType.landscape))
                {

                    transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 25);
                }
                else
                {
                    if (mobileControl.gameObject.activeInHierarchy)
                    {
                        if (PlayerManager.Instance.MainControlSettings.controllerType == 0)
                        {
                            transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 25 + (mobileControl.GetChild(0).GetComponent<RectTransform>().sizeDelta.y + 10));
                        }
                        else
                        {
                            transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 25 + (mobileControl.GetChild(1).GetComponent<RectTransform>().sizeDelta.y + 10));
                        }
                    }
                    else
                    {
                        transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 25 + (MainHUDMenuPanel.Instance.MobileButton.sizeDelta.y + 10));
                    }
                }
            }

        }

        public void Close()
        {
            HUDManager.Instance.GetMenuItem("Toggle_Hotspots").GetComponent<UnityEngine.UI.Toggle>().isOn = false;
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                Vector2 cellsize = new Vector2(52 * m_scaler, 52 * m_scaler);

                if (arg0.Equals(OrientationType.landscape))
                {
                    titleText.fontSize = m_cacheTitleText;
                    m_areaLayout.minWidth = m_cacheSize.x;
                    m_areaLayout.minHeight = m_cacheSize.y;

                    m_gridLayout.cellSize = new Vector2(m_cacheSize.x, m_cacheSize.x / 2);
                }
                else
                {
                    float aspect = arg2 / arg1;
                    titleText.fontSize = m_cacheTitleText * aspect;

                    m_areaLayout.minWidth = m_cacheSize.x * aspect;
                    m_areaLayout.minHeight = m_cacheSize.y * aspect;

                    m_gridLayout.cellSize = new Vector2(m_cacheSize.x * aspect, (m_cacheSize.x  * aspect) / 2);
                }

                HotspotButton[] all = GetComponentsInChildren<HotspotButton>(true);

                for (int i = 0; i < all.Length; i++)
                {
                    all[i].Resize();
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(HotspotPanel), true)]
        public class HotspotPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("prefab"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("container"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("titleText"), true);

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

    public class HotspotCollection
    {
        public Hotspot[] Hotspots;
    }
}
