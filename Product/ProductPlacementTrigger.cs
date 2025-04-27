using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Collider))]
    public class ProductPlacementTrigger : MonoBehaviour
    {
        public System.Action OnEnter;
        public System.Action OnExit;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.Equals(PlayerManager.Instance.GetLocalPlayer().TransformObject))
            {
                if(OnEnter != null)
                {
                    OnEnter.Invoke();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform.Equals(PlayerManager.Instance.GetLocalPlayer().TransformObject))
            {
                if (OnExit != null)
                {
                    OnExit.Invoke();
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ProductPlacementTrigger), true)]
        public class ProductPlacementTrigger_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("OnEnter"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OnExit"), true);

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
