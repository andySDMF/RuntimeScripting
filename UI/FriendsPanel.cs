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
    public class FriendsPanel : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI title;

        private RectTransform m_mainLayout;
        private RectTransform m_parent;

        private OrientationType m_switch = OrientationType.landscape;
        private float m_layoutWidth;
        private float m_anchorX;
        private float m_layoutHeight;

        private float m_titleFontSize;

        private void Awake()
        {
            m_mainLayout = GetComponent<RectTransform>();
            m_parent = GetComponentInParent<Toggle>().GetComponent<RectTransform>();
            m_layoutWidth = m_mainLayout.sizeDelta.x;
            m_layoutHeight = m_mainLayout.sizeDelta.y;

            m_titleFontSize = title.fontSize;

            if (AppManager.Instance.Data.IsMobile)
            {
                transform.SetParent(MainHUDMenuPanel.Instance.transform);
                m_mainLayout.anchorMin = new Vector2(1, 1);
                m_mainLayout.anchorMax = new Vector2(1, 1);
                m_anchorX = m_mainLayout.anchoredPosition.x;

            }
        }

        private void Update()
        {
            if(AppManager.Instance.Data.IsMobile)
            {
                if(m_switch != OrientationManager.Instance.CurrentOrientation)
                {
                    m_switch = OrientationManager.Instance.CurrentOrientation;

                    if (m_switch.Equals(OrientationType.landscape))
                    {
                        m_mainLayout.anchorMin = new Vector2(1, 1);
                        m_mainLayout.anchorMax = new Vector2(1, 1);
                        m_mainLayout.pivot = new Vector2(1, 1);
                        m_mainLayout.sizeDelta = new Vector2(m_layoutWidth, m_layoutHeight);

                        title.fontSize = m_titleFontSize;
                    }
                    else
                    {
                        float aspect = OrientationManager.Instance.ScreenSize.y / OrientationManager.Instance.ScreenSize.x;

                        m_mainLayout.anchorMin = new Vector2(0f, 1f);
                        m_mainLayout.anchorMax = new Vector2(1f, 1f);
                        m_mainLayout.pivot = new Vector2(0.5f, 1);
                        m_mainLayout.offsetMax = new Vector2(-50, 0);
                        m_mainLayout.offsetMin = new Vector2(50, 0);
                        m_mainLayout.sizeDelta = new Vector2(m_mainLayout.sizeDelta.x, m_layoutHeight * aspect);

                        title.fontSize = m_titleFontSize * aspect;
                    }
                }

                if (m_switch.Equals(OrientationType.landscape))
                {
                    m_mainLayout.anchoredPosition = new Vector2(m_anchorX, (m_parent.anchoredPosition.y - 35) - (m_parent.sizeDelta.y / 2));
                }
                else
                {
                    m_mainLayout.anchoredPosition = new Vector2(m_mainLayout.anchoredPosition.x, (m_parent.anchoredPosition.y - 100) - m_parent.sizeDelta.y);
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(FriendsPanel), true)]
        public class FriendsPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("title"), true);

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
