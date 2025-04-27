using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// Spin component to rotate on axis
    /// </summary>
    public class Spin : MonoBehaviour
    {
        public Vector3 SpinAxis;
        public float speed = 20.2f;
        public bool useTime = true;

        private bool m_start = false;
        private float m_time = 0.0f;

        private void OnEnable()
        {
            m_time = 0.0f;
            m_start = true;   
        }

        private void OnDisable()
        {
            m_start = false;
            
        }

        void Update()
        {
            /*var rot = this.transform.localRotation;
            var time = useTime ? Time.deltaTime : 1;

            rot.eulerAngles = new Vector3(rot.eulerAngles.x + time * speed * SpinAxis.x,
                rot.eulerAngles.y + time * speed * SpinAxis.y,
                rot.eulerAngles.z + time * speed * SpinAxis.z);

            this.transform.localRotation = rot;*/

            if(m_start)
            {
                m_time = useTime ? Time.deltaTime : 1;
                transform.RotateAround(transform.position, SpinAxis, speed * m_time);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Spin), true)]
        public class Spin_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("SpinAxis"), true);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Velocity", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speed"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useTime"), true);

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