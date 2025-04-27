using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class AppOnlineMode : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup displayOnline;
        [SerializeField]
        private CanvasGroup displayOffline;

        private bool m_isOnline = false;

        private void Start()
        {
            if(!AppManager.Instance.Settings.projectSettings.displayMultiplayerOption)
            {
                gameObject.SetActive(false);
                return;
            }

            m_isOnline = AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online);

            if(m_isOnline)
            {
                displayOnline.alpha = 1;
                displayOffline.alpha = 0;
            }
            else
            {
                displayOnline.alpha = 0;
                displayOffline.alpha = 1;
            }
        }

        public void OnClick()
        {
            m_isOnline = !m_isOnline;

            if (m_isOnline)
            {
                displayOnline.alpha = 1;
                displayOffline.alpha = 0;
            }
            else
            {
                displayOnline.alpha = 0;
                displayOffline.alpha = 1;
            }

            AppManager.Instance.Data.Mode = (m_isOnline) ? MultiplayerMode.Online : MultiplayerMode.Offline;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AppOnlineMode), true)]
        public class AppOnlineMode_Editor : BaseInspectorEditor
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

                    EditorGUILayout.LabelField("Displays", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("displayOnline"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("displayOffline"), true);

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
