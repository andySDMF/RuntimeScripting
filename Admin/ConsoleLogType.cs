using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ConsoleLogType : MonoBehaviour
    {
        [SerializeField]
        private LogType type;

        private bool m_state = true;

        private ConsoleLog m_log;

        public LogType Type
        {
            get
            {
                return type;
            }
        }

        /// <summary>
        /// Action called by the ConsoleLog.cs to set this logtype toggle state
        /// </summary>
        public void Set(bool isOn)
        {
            if (m_log == null)
            {
                m_log = GetComponentInParent<ConsoleLog>();
            }

            m_state = isOn;
            GetComponentInChildren<UnityEngine.UI.Toggle>(true).isOn = isOn;
        }

        private void OnEnable()
        {
            GetComponentInChildren<UnityEngine.UI.Toggle>(true).onValueChanged.AddListener(Toggle);
        }

        private void OnDisable()
        {
            GetComponentInChildren<UnityEngine.UI.Toggle>(true).onValueChanged.RemoveListener(Toggle);
        }

        /// <summary>
        /// Toggle the state of this logtype within the ConsoleLog filters
        /// </summary>
        /// <param name="state"></param>
        public void Toggle(bool state)
        {
            if(m_log == null)
            {
                m_log = GetComponentInParent<ConsoleLog>();
            }

            m_state = state;

            if(m_state)
            {
                m_log.AddLogTypeFilter(type);
            }
            else
            {
                m_log.RemoveLogTypeFilter(type);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ConsoleLogType), true)]
        public class ConsoleLogType_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("type"), true);

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
