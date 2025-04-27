using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ConfiguratorControlPanel : MonoBehaviour
    {
        [Header("Mobile Layout")]
        [SerializeField]
        private RectTransform mainLayout;

        private float m_cacheAnchorFromBottom;

        private void Awake()
        {
            if (!AppManager.IsCreated) return;

            m_cacheAnchorFromBottom = mainLayout.anchoredPosition.y;
        }

        private void OnEnable()
        {
            if (!AppManager.IsCreated) return;

            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
        }

        private void OnDisable()
        {
            if (!AppManager.IsCreated) return;

            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    mainLayout.pivot = new Vector2(0.5f, 0.0f);
                    mainLayout.anchoredPosition = new Vector2(0.0f, m_cacheAnchorFromBottom);
                }
                else
                {
                    float aspect = arg2 / arg1;
                    RectTransform mobileControl = HUDManager.Instance.GetHUDControlObject("MOBILE_JOYSTICK").GetComponent<RectTransform>();

                    mainLayout.pivot = new Vector2(0.0f, 0.0f);

                    if (mobileControl.gameObject.activeInHierarchy)
                    {

                        if (PlayerManager.Instance.MainControlSettings.controllerType == 0)
                        {
                            mainLayout.anchoredPosition = new Vector2(mobileControl.GetChild(0).GetComponent<RectTransform>().anchoredPosition.x, m_cacheAnchorFromBottom + mobileControl.GetChild(0).GetComponent<RectTransform>().sizeDelta.y * aspect);
                        }
                        else
                        {
                            mainLayout.anchoredPosition = new Vector2(mobileControl.GetChild(1).GetComponent<RectTransform>().anchoredPosition.x, m_cacheAnchorFromBottom + mobileControl.GetChild(1).GetComponent<RectTransform>().sizeDelta.y * aspect);
                        }
                    }
                    else
                    {
                        mainLayout.anchoredPosition = new Vector2(25.0f, m_cacheAnchorFromBottom);
                    }

                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ConfiguratorControlPanel), true)]
        public class LConfiguratorControlPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mainLayout"), true);

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
