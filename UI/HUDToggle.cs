using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Toggle))]
    public class HUDToggle : MonoBehaviour
    {
        public UnityEvent onTrue;
        public UnityEvent onFalse;

        private bool m_isOn = false;

        private void Awake()
        {
            Toggle tog = GetComponent<Toggle>();

            tog.onValueChanged.AddListener(OnValueChange);

            if(tog.isOn != m_isOn)
            {
                OnValueChange(tog.isOn);
            }

            m_isOn = tog.isOn;
        }

        private void OnValueChange(bool isOn)
        {
            if (isOn.Equals(m_isOn)) return;

            m_isOn = isOn;

            if(m_isOn)
            {
                onTrue.Invoke();
            }
            else
            {
                onFalse.Invoke();
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(HUDToggle), true)]
        public class HUDToggle_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.LabelField("On Toggle On", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onTrue"), true);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("On Toggle Off", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onFalse"), true);

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}
