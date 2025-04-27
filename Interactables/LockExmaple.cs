using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Renderer))]
    public class LockExmaple : MonoBehaviour
    {
        [SerializeField]
        private Color lockedColor = Color.red;
        [SerializeField]
        private Color unlockedColor = Color.gray;

        private void Awake()
        {
            GetComponentInChildren<Lock>(true).OnLock += Lock;
            GetComponentInChildren<Lock>(true).OnUnlock += Unlock;
        }

        private void Lock()
        {
            GetComponent<Renderer>().material.color = lockedColor;
        }

        private void Unlock()
        {
            GetComponent<Renderer>().material.color = unlockedColor;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(LockExmaple), true)]
        public class LockExmaple_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("lockedColor"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("unlockedColor"), true);

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
