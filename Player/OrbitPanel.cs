using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class OrbitPanel : MonoBehaviour
    {
        [Header("Control")]
        [SerializeField]
        private Transform layout;

        [SerializeField]
        private OrbitPanelButton panImage;

        [SerializeField]
        private OrbitPanelButton rotateImage;

        [SerializeField]
        private OrbitPanelButton zoomInImage;

        [SerializeField]
        private OrbitPanelButton zoomOutImage;

        private CameraOrbit m_orbit;
        private OrbitPanelButton m_currentImage;

        private RectTransform m_mainLayout;
        private float m_cacheAnchorFromBottom;

        private void Awake()
        {
            if (AppManager.Instance.Data.IsMobile || AppManager.Instance.Settings.HUDSettings.useMobileToolsForDesktop)
            {
                m_mainLayout = transform.GetChild(0).GetComponent<RectTransform>();

                //need to re-anchor the layout
                m_mainLayout.anchorMin = new Vector2(0.0f, 0.0f);
                m_mainLayout.anchorMax = new Vector2(1.0f, 0.0f);
                m_mainLayout.anchoredPosition = new Vector2(0, 25);
                m_mainLayout.pivot = new Vector2(0.5f, 0.0f);
                m_mainLayout.offsetMax = new Vector2(0, m_mainLayout.offsetMax.y);
                m_mainLayout.offsetMin = new Vector2(0, m_mainLayout.offsetMin.y);

                m_cacheAnchorFromBottom = m_mainLayout.anchoredPosition.y;
            }
        }

        private void OnEnable()
        {
            if(AppManager.IsCreated)
            {
                if(PlayerManager.Instance.OrbitCameraActive)
                {
                    m_orbit = PlayerManager.Instance.OrbitCamera.GetComponentInChildren<CameraOrbit>(true);
                }

                if(!AppManager.Instance.Data.IsMobile && !AppManager.Instance.Settings.HUDSettings.useMobileToolsForDesktop)
                {
                    //this needs to float above the main nav bar & to the left of the perspective button
                    GameObject perspectivebutton = HUDManager.Instance.GetMenuFeature(HUDManager.MenuFeature._Perspective).gameObject;
                    Vector3 pos = perspectivebutton.transform.position;

                    transform.GetChild(0).position = new Vector3(pos.x + 36, transform.GetChild(0).position.y, transform.GetChild(0).position.z);
                }

                OrientationManager.Instance.OnOrientationChanged += OnOrientation;

                OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
            }
        }

        private void OnDisable()
        {
            if (!AppManager.IsCreated) return;

            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        private void Update()
        {
            if(m_orbit != null)
            {
                if(m_orbit.CurrentTool.Equals(CameraOrbit.Tool.Pan))
                {
                    Pan();
                }
                else if(m_orbit.CurrentTool.Equals(CameraOrbit.Tool.Rotate))
                {
                    Rotate();
                }
                else
                {

                }
            }
        }

        public void Pan()
        {
            if (m_currentImage != panImage)
            {
                if (m_currentImage != null && m_currentImage.IsSelected)
                {
                    m_currentImage.IsOn = false;
                    m_currentImage.IsSelected = false;
                }

                m_currentImage = panImage;
                m_currentImage.IsOn = true;
                m_currentImage.IsSelected = true;

                m_orbit.SetTool(CameraOrbit.Tool.Pan);
            }
        }

        public void Rotate()
        {
            if (m_currentImage != rotateImage)
            {
                if (m_currentImage != null && m_currentImage.IsSelected)
                {
                    m_currentImage.IsOn = false;
                    m_currentImage.IsSelected = false;
                }

                m_currentImage = rotateImage;
                m_currentImage.IsOn = true;
                m_currentImage.IsSelected = true;

                m_orbit.SetTool(CameraOrbit.Tool.Rotate);
            }
        }

        public void ZoomIn()
        {
            m_orbit.UpdateCameraDistance(m_orbit.CameraDistance + 5f);
        }

        public void ZoomOut()
        {
            m_orbit.UpdateCameraDistance(m_orbit.CameraDistance - 5f);
        }

        public void None()
        {
            if (m_currentImage != null)
            {
                m_currentImage.IsOn = false;
                m_currentImage.IsSelected = false;
            }

            m_currentImage = null;
            m_orbit.SetTool(CameraOrbit.Tool.None);
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    m_mainLayout.pivot = new Vector2(0.5f, 0.0f);
                    m_mainLayout.anchoredPosition = new Vector2(0.0f, m_cacheAnchorFromBottom);
                }
                else
                {
                    float aspect = arg2 / arg1;
                    RectTransform mobileControl = HUDManager.Instance.GetHUDControlObject("MOBILE_JOYSTICK").GetComponent<RectTransform>();

                    m_mainLayout.pivot = new Vector2(0.0f, 0.0f);

                    if (mobileControl.gameObject.activeInHierarchy)
                    {

                        if (PlayerManager.Instance.MainControlSettings.controllerType == 0)
                        {
                            m_mainLayout.anchoredPosition = new Vector2(mobileControl.GetChild(0).GetComponent<RectTransform>().anchoredPosition.x, m_cacheAnchorFromBottom + mobileControl.GetChild(0).GetComponent<RectTransform>().sizeDelta.y * aspect);
                        }
                        else
                        {
                            m_mainLayout.anchoredPosition = new Vector2(mobileControl.GetChild(1).GetComponent<RectTransform>().anchoredPosition.x, m_cacheAnchorFromBottom + mobileControl.GetChild(1).GetComponent<RectTransform>().sizeDelta.y * aspect);
                        }
                    }
                    else
                    {
                        m_mainLayout.anchoredPosition = new Vector2(25.0f, m_cacheAnchorFromBottom);
                    }

                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OrbitPanel), true)]
        public class OrbitPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("layout"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("panImage"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rotateImage"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("zoomInImage"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("zoomOutImage"), true);

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
