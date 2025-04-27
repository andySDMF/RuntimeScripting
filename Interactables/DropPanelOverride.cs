using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(PickupItem))]
    public class DropPanelOverride : MonoBehaviour
    {
        [SerializeField]
        private bool showInstruction = true;
        [SerializeField]
        private string title = "Instruction";
        [SerializeField]
        private string message = "Place item on a interactive area or click 'Drop' to drop item.";
        [SerializeField]
        private string button = "Drop";

        private void Awake()
        {
            GetComponent<PickupItem>().OnInteracted += OnInteracted;
        }

        private void OnInteracted(bool obj)
        {
            if(obj)
            {
                StartCoroutine(Delay());
            }
        }

        private IEnumerator Delay()
        {
            yield return new WaitForEndOfFrame();

            DropPanel dropPanel = HUDManager.Instance.GetHUDControlObject("DROP").GetComponentInChildren<DropPanel>(true);
            dropPanel.SetStrings(showInstruction, title, message, button);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(DropPanelOverride), true)]
        public class DropPanelOverride_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("showInstruction"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("title"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("message"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("button"), true);

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
