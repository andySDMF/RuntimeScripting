using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class MapController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField]
        private GameObject labelPanel;
        [SerializeField]
        private GameObject zoomContainer;

        [Header("Labels")]
        [SerializeField]
        private GameObject labelPrefab;
        [SerializeField]
        private Transform labelContainer;


        [Header("Key")]
        [SerializeField]
        private GameObject keyContainer;
        [SerializeField]
        private TextMeshProUGUI keyTitle;
        [SerializeField]
        private Image keyImage;
        [SerializeField]
        private GameObject definedKeyContainer;
        [SerializeField]
        private GameObject definedKeyPrefab;

        private List<GameObject> m_labels = new List<GameObject>();
        private List<GameObject> m_keys = new List<GameObject>();


        private float m_scaler;
        private Vector2 m_labelCache;
        private Vector2 m_labelCahceAnchor;

        private float m_keyTitleFontCache;
        private Vector2 m_keyTitleImageSizeCache;
        private Vector2 m_keyImageSizeCache;
        private float m_keyFontCache;

        private bool m_orientationInit = false;

        private void OnEnable()
        {
            if(CoreManager.Instance.projectSettings.mapRenderMode.Equals(RenderMapMode.Camera) && MapManager.Instance.HasController)
            {
                zoomContainer.SetActive(true);
            }
            else
            {
                zoomContainer.SetActive(false);
            }

            //need to turn off the main hudvisibility
            HUDManager.Instance.ShowHUDNavigationVisibility(false);

            //hide hint
            PopupManager.instance.HideHint();

            m_scaler = AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            if (!CoreManager.Instance.projectSettings.useMultipleMaps || !MapManager.Instance.UseLabels)
            {
                labelPanel.SetActive(false);
                OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
                return;
            }

            SubtitleManager.Instance.ToggleButtonVisibiliy(false);

            HUDManager.Instance.ToggleHUDControl("MOBILE_JOYSTICK", false);

            List<string> labels = MapManager.Instance.GetLabels;

            labelPanel.SetActive(labels.Count > 0);

            for (int i = 0; i < labels.Count; i++)
            {
                GameObject go = Instantiate(labelPrefab, Vector3.zero, Quaternion.identity, labelContainer);
                go.transform.localScale = Vector3.one;
                go.GetComponentInChildren<TextMeshProUGUI>(true).text = labels[i];

                Sprite sp = MapManager.Instance.GetIcon(labels[i]);

                if(sp != null)
                {
                    go.transform.GetChild(0).GetComponentInChildren<Image>(true).sprite = sp;
                    go.transform.GetChild(0).gameObject.SetActive(true);
                }
                else
                {
                    go.transform.GetChild(0).gameObject.SetActive(false);
                }

                string str = labels[i];
                go.GetComponentInChildren<Button>(true).onClick.AddListener(() => { OnLabelClicked(str); });
                go.SetActive(true);

                m_labels.Add(go);
            }

            if (labels.Count > 0)
            {
                //add reset
                GameObject go = Instantiate(labelPrefab, Vector3.zero, Quaternion.identity, labelContainer);
                go.transform.localScale = Vector3.one;
                go.transform.GetChild(0).GetComponentInChildren<Image>(true).CrossFadeAlpha(0.0f, 0.0f, true);
                go.GetComponentInChildren<TextMeshProUGUI>(true).text = "Main";
                string str = "Main";
                go.GetComponentInChildren<Button>(true).onClick.AddListener(() => { OnLabelClicked(str); });
                go.SetActive(true);

                m_labels.Add(go);
            }

            //add key
            List<MapManager.MapDefinedKey> temp = MapManager.Instance.GetKey;

            if(temp != null && temp.Count > 0)
            {
                if(temp[0].keyName.Equals("$IMAGE"))
                {
                    keyImage.sprite = temp[0].keyIcon;
                    keyImage.SetNativeSize();

                    keyImage.gameObject.SetActive(true);
                    definedKeyContainer.gameObject.SetActive(false);
                }
                else
                {
                    keyImage.gameObject.SetActive(false);

                    for(int i = 0; i < temp.Count; i++)
                    {
                        GameObject key = Instantiate(definedKeyPrefab, Vector3.zero, Quaternion.identity, definedKeyContainer.transform);
                        key.transform.localScale = Vector3.one;
                        key.name = "KEY_" + temp[i].keyName;

                        if(temp[i].keyIcon != null)
                        {
                            key.transform.GetChild(0).GetComponentInChildren<Image>(true).sprite = temp[i].keyIcon;
                            key.transform.GetChild(0).gameObject.SetActive(true);
                        }
                        else
                        {
                            key.transform.GetChild(0).gameObject.SetActive(false);
                        }
                        
                        key.transform.GetChild(1).GetComponentInChildren<TextMeshProUGUI>(true).text = temp[i].keyName;
                        key.SetActive(true);

                        m_keys.Add(key);
                    }

                    definedKeyContainer.gameObject.SetActive(true);
                }

                keyContainer.gameObject.SetActive(true);
            }
            else
            {
                keyContainer.gameObject.SetActive(false);
            }

            if(!m_orientationInit)
            {
                m_orientationInit = true;
                m_labelCache = new Vector2(labelContainer.GetComponent<RectTransform>().sizeDelta.x, labelContainer.GetComponent<RectTransform>().anchoredPosition.y);
                m_labelCahceAnchor = labelContainer.GetComponent<RectTransform>().anchoredPosition;

                m_keyTitleFontCache = keyTitle.fontSize;

                if(keyImage.gameObject.activeInHierarchy)
                {
                    m_keyImageSizeCache = keyImage.GetComponent<RectTransform>().sizeDelta;
                }

                m_keyFontCache = definedKeyPrefab.GetComponentInChildren<TextMeshProUGUI>(true).fontSize;

                LayoutElement le = definedKeyContainer.GetComponentInChildren<LayoutElement>(true);
                m_keyImageSizeCache = new Vector2(le.minWidth, le.minHeight);
            }

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
        }

        private void OnDisable()
        {
            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;

            SubtitleManager.Instance.ToggleButtonVisibiliy(true);

            if(AppManager.Instance.Data.IsMobile)
            {
                HUDManager.Instance.ToggleHUDControl("MOBILE_JOYSTICK", true);
            }

            for (int i = 0; i < m_labels.Count; i++)
            {
                Destroy(m_labels[i]);
            }

            m_labels.Clear();

            keyImage.sprite = null;

            for(int i = 0; i < m_keys.Count; i++)
            {
                Destroy(m_keys[i]);
            }

            m_keys.Clear();
            labelPanel.SetActive(false);
            keyContainer.gameObject.SetActive(false);

            HUDManager.Instance.ShowHUDNavigationVisibility(true);
        }

        private void OnLabelClicked(string label)
        {
            MapManager.Instance.MoveToPosition(label);
        }

        public void Close()
        {
            HUDManager.Instance.GetMenuItem("Toggle_TopDown").GetComponent<UnityEngine.UI.Toggle>().isOn = false;
        }

        public void ZoomIn()
        {
            MapManager.Instance.ZoomIn();
        }

        public void ZoomOut()
        {
            MapManager.Instance.ZoomOut();
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (CoreManager.Instance.projectSettings.useMultipleMaps || MapManager.Instance.UseLabels)
                {
                    RectTransform labelRectT = labelContainer.GetComponent<RectTransform>();

                    if (arg0.Equals(OrientationType.landscape))
                    {
                        labelRectT.sizeDelta = new Vector2(m_labelCache.x, labelRectT.sizeDelta.y);
                        labelRectT.anchoredPosition = m_labelCahceAnchor;

                        for(int i = 0; i < m_keys.Count; i++)
                        {
                            m_keys[i].GetComponentInChildren<TextMeshProUGUI>().fontSizeMax = m_keyFontCache;


                            LayoutElement le = m_keys[i].GetComponentInChildren<LayoutElement>();

                            if(le != null)
                            {
                                le.minWidth = m_keyImageSizeCache.x;
                                le.minHeight = m_keyImageSizeCache.y;
                                le.preferredWidth = le.minWidth;
                                le.preferredHeight = le.minHeight;
                            }
                        }
                    }
                    else
                    {

                        float aspect = arg2 / arg1;
                        labelRectT.sizeDelta = new Vector2(m_labelCache.x * m_scaler, labelRectT.sizeDelta.y);
                        labelRectT.anchoredPosition = new Vector2(m_labelCahceAnchor.x, (m_labelCahceAnchor.y + 25.0f) + (zoomContainer.GetComponent<RectTransform>().sizeDelta.y * aspect) );

                        for (int i = 0; i < m_keys.Count; i++)
                        {
                            m_keys[i].GetComponentInChildren<TextMeshProUGUI>().fontSizeMax = m_keyFontCache * aspect;


                            LayoutElement le = m_keys[i].GetComponentInChildren<LayoutElement>();

                            if (le != null)
                            {
                                le.minWidth = m_keyImageSizeCache.x * aspect;
                                le.minHeight = m_keyImageSizeCache.y * aspect;
                                le.preferredWidth = le.minWidth;
                                le.preferredHeight = le.minHeight;
                            }
                        }
                    }
                }

                RectTransform zoomRectT = zoomContainer.GetComponent<RectTransform>();

                if (arg0.Equals(OrientationType.landscape))
                {
                    zoomRectT.pivot = new Vector2(0.5f, 0.0f);
                    zoomRectT.anchoredPosition = new Vector2(0.0f, zoomRectT.anchoredPosition.y);

                    keyTitle.fontSize = m_keyTitleFontCache;

                    if (keyImage.gameObject.activeInHierarchy)
                    {
                        keyImage.SetNativeSize();
                    }
                }
                else
                {

                    float aspect = arg2 / arg1;
                    zoomRectT.pivot = new Vector2(0.0f, 0.0f);
                    zoomRectT.anchoredPosition = new Vector2(25.0f, zoomRectT.anchoredPosition.y);

                    keyTitle.fontSize = m_keyTitleFontCache * aspect;

                    if (keyImage.gameObject.activeInHierarchy)
                    {
                        keyImage.GetComponent<RectTransform>().sizeDelta = new Vector2(m_keyTitleImageSizeCache.x * aspect, m_keyTitleImageSizeCache.y * aspect);
                    }
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(MapController), true)]
        public class MapController_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("labelPanel"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("zoomContainer"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("labelPrefab"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("labelContainer"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("keyContainer"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("keyTitle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("keyImage"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("definedKeyContainer"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("definedKeyPrefab"), true);

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
