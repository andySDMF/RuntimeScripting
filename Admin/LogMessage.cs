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
    /// <summary>
    /// predominantly used for unity console log messages, but can be used for other log messages
    /// </summary>
    public class LogMessage : MonoBehaviour
    {
        [SerializeField]
        private Image logImage;
        [SerializeField]
        private TextMeshProUGUI logType;
        [SerializeField]
        private TextMeshProUGUI log;
        [SerializeField]
        private TextMeshProUGUI stackTrace;

        /// <summary>
        /// Called to update a log message
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="logType"></param>
        /// <param name="log"></param>
        /// <param name="stack"></param>
        public void Set(Sprite sp, string logType, string log, string stack = "")
        {
            logImage.sprite = sp;
            this.logType.text = logType;
            this.log.text = log;
            this.stackTrace.text = stack;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(LogMessage), true)]
        public class LogMessage_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("logImage"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("logType"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("log"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("stackTrace"), true);

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
