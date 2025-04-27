using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class OrientationSimulator : MonoBehaviour
    {
        [SerializeField]
        private OrientationType orientation;

        public OrientationType Type
        {
            get
            {
                return orientation;
            }
        }

        private void Start()
        {
#if !UNITY_EDITOR
            Destroy(this);
            return;
#else
            if (!AppManager.Instance.Settings.editorTools.createWebClientSimulator)
            {
                Destroy(this);
            }
#endif
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OrientationSimulator), true)]
        public class OrientationSimulator_Editor : BaseInspectorEditor
        {
            private OrientationSimulator script;

            private void OnEnable()
            {
                GetBanner();
                script = (OrientationSimulator)target;
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("orientation"), true);

                EditorGUILayout.LabelField("Click this button to simulate orientation web responce (via Simulator)", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("This script will be destroyed when built", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space();

                if(GUILayout.Button("Simulate Orientation Responce"))
                {
                    WebClientSimulator.Instance.OnSimulateOrientation(script.Type);
                }

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(script);
                }
            }
        }
#endif
    }
}
