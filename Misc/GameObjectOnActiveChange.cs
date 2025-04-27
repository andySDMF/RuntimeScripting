using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class GameObjectOnActiveChange : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent onEnabled;
        [SerializeField]
        private UnityEvent onDisabled;

        private void OnEnable()
        {
            onEnabled.Invoke();
        }

        private void OnDisable()
        {
            onDisabled.Invoke();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(GameObjectOnActiveChange), true)]
        public class GameObjectOnActiveChange_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onEnabled"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onDisabled"), true);

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