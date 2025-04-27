using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(BoxCollider))]
    public class ColliderTriggerEvent : MonoBehaviour
    {
        public System.Action<bool> OnTriggerEvent { get; set; }

        private bool m_entered = false;

        private void Start()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.transform.Equals(PlayerManager.Instance.GetLocalPlayer().TransformObject))
            {
                if(!m_entered)
                {
                    m_entered = true;

                    if (OnTriggerEvent != null)
                    {
                        OnTriggerEvent.Invoke(true);
                    }
                }
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (other.transform.Equals(PlayerManager.Instance.GetLocalPlayer().TransformObject))
            {
                if (m_entered)
                {
                    m_entered = false;

                    if (OnTriggerEvent != null)
                    {
                        OnTriggerEvent.Invoke(false);
                    }
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ColliderTriggerEvent), true)]
        public class ColliderTriggerEvent_Editor : BaseInspectorEditor
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
                EditorGUILayout.LabelField("Event", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OnTriggerEvent"), true);

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
