using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ToggleScale : MonoBehaviour
    {
        [SerializeField]
        private Vector3 onTrue = Vector3.one;

        [SerializeField]
        private Vector3 onFalse = Vector3.zero;

        public void Toggle(bool tog)
        {
            if(tog)
            {
                transform.localScale = onTrue;
            }
            else
            {
                transform.localScale = onFalse;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ToggleScale), true)]
        public class ToggleScale_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.LabelField("Toggle On", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onTrue"), true);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Toggle Off", EditorStyles.boldLabel);
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
