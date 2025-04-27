using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Pulse : MonoBehaviour
    {
        [SerializeField]
        private float speed = 0.5f;

        [SerializeField]
        private float length = 0.5f;

        [SerializeField]
        private bool beginOnStart = true;

        private float m_time = 0.0f;
        private Vector3 m_cache;
        private bool m_running = false;

        private void Start()
        {
            m_cache = transform.localScale;

            if(beginOnStart)
            {
                Begin();
            }
        }


        private void Update()
        {
            if(m_running)
            {
                m_time = Mathf.PingPong(Time.time * speed, length);
                Vector3 sca = m_cache * m_time;
                transform.localScale = m_cache + sca;
            }
        }

        public void Begin(bool startFromOrigin = true)
        {
            if(!m_running)
            {
                if (startFromOrigin)
                {
                    transform.localScale = m_cache;
                    m_time = 0.0f;
                }

                m_running = true;
            }
        }

        public void End()
        {
            m_running = false;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Pulse), true)]
        public class Pulse_Editor : BaseInspectorEditor
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
                EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speed"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("length"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("beginOnStart"), true);

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
