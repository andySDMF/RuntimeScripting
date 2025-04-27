using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class GameObjectOnDisable : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent onDisableEvent;

        private void OnDisable()
        {
            onDisableEvent.Invoke();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(GameObjectOnDisable), true)]
        public class GameObjectOnDisable_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.LabelField("Event", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onDisableEvent"), true);

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
