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
    public class ConferenceControlPanel : MonoBehaviour
    {
        [Header("Mobile Layout")]
        [SerializeField]
        private RectTransform folderViewport;

        [SerializeField]
        private RectTransform playerPanel;

        [SerializeField]
        private RectTransform buttonRef;

        [SerializeField]
        private TextMeshProUGUI playerPanelTitle;

        private Vector2 m_folderViewportSize;
        private float m_scaler;

        private Vector2 m_playerPanelCache;
        private float m_titleFontSize;
        private float m_layoutScrollHeight;

        private void Awake()
        {
            m_folderViewportSize = folderViewport.sizeDelta;
            m_scaler = AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;

            m_playerPanelCache = new Vector2(playerPanel.sizeDelta.x, playerPanel.anchoredPosition.y);
            m_layoutScrollHeight = playerPanel.GetChild(1).GetComponentInChildren<LayoutElement>().minHeight;
            m_titleFontSize = playerPanelTitle.fontSize;
        }

        private void OnEnable()
        {
            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
        }

        private void OnDisable()
        {
            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        private void Update()
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                playerPanel.anchoredPosition = new Vector2(0, 25 + (buttonRef.sizeDelta.y + 10));

                if (OrientationManager.Instance.CurrentOrientation.Equals(OrientationType.landscape))
                {
                    folderViewport.sizeDelta = m_folderViewportSize;
                }
                else
                {
                    folderViewport.sizeDelta = new Vector2(OrientationManager.Instance.ScreenSize.x - 50, m_folderViewportSize.y * m_scaler);
                }
            }
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    playerPanel.sizeDelta = new Vector2(m_playerPanelCache.x, 0);
                    playerPanelTitle.fontSize = m_titleFontSize;
                    playerPanel.GetChild(1).GetComponentInChildren<LayoutElement>().minHeight = m_layoutScrollHeight;
                }
                else
                {
                    float aspect = arg2 / arg1;
                    playerPanel.sizeDelta = new Vector2(m_playerPanelCache.x * aspect, 0);
                    playerPanelTitle.fontSize = m_titleFontSize * aspect;
                    playerPanel.GetChild(1).GetComponentInChildren<LayoutElement>().minHeight = m_layoutScrollHeight * aspect;
                }
            }
        }

        public void ToggleConferenceList(bool val)
        {
            ChairManager.Instance.ShowConferenceUserList(val);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ConferenceControlPanel), true)]
        public class ConferenceControlPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("folderViewport"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("playerPanel"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonRef"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("playerPanelTitle"), true);

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
