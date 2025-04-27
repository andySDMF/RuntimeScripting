using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Bounce : MonoBehaviour
    {
        [SerializeField]
        private float speed = 0.5f;

        [SerializeField]
        private float length = 0.5f;

        [SerializeField]
        private bool beginOnStart = true;

        [Tooltip("Based on World Rotation")]
        [SerializeField]
        private BounceDirection primaryDirection = BounceDirection._Up;

        [Tooltip("Based on World Rotation")]
        [SerializeField]
        private BounceDirection secondaryDirection = BounceDirection._None;

        private Vector3 m_direction = Vector3.right;
        private float m_time;
        private Vector3 m_orgin;
        private bool m_running = false;

        private void Start()
        {
            m_direction = GetVector(primaryDirection) + GetVector(secondaryDirection);
            m_orgin = transform.localPosition;

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
                Vector3 dir = m_direction * m_time;
                transform.localPosition = m_orgin + dir;
            }
        }

        public void Begin(bool startFromOrigin = true)
        {
            if (!m_running)
            {
                if (startFromOrigin)
                {
                    transform.localPosition = m_orgin;
                    m_time = 0.0f;
                }

                m_running = true;
            }
        }

        public void End()
        {
            m_running = false;
        }


        private Vector3 GetVector(BounceDirection dir)
        {
            Vector3 vec = Vector3.zero;

            switch(dir)
            {
                case BounceDirection._Up:
                    vec = new Vector3(0, 1, 0);
                    break;
                case BounceDirection._Down:
                    vec = new Vector3(0, -1, 0);
                    break;
                case BounceDirection._Forward:
                    vec = new Vector3(0, 0, 1);
                    break;
                case BounceDirection._Back:
                    vec = new Vector3(0, 0, -1);
                    break;
                case BounceDirection._Left:
                    vec = new Vector3(-1, 0, 0);
                    break;
                case BounceDirection._Right:
                    vec = new Vector3(1, 0, 0);
                    break;
            }

            return vec;
        }

        [System.Serializable]
        private enum BounceDirection { _None, _Up, _Down, _Left, _Right, _Forward, _Back }

#if UNITY_EDITOR
        [CustomEditor(typeof(Bounce), true)]
        public class Bounce_Editor : BaseInspectorEditor
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
                EditorGUILayout.PropertyField(serializedObject.FindProperty("primaryDirection"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("secondaryDirection"), true);

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
