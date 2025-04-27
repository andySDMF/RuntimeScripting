using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class PopUpTrigger : MonoBehaviour
    {
        [Header("PopUp")]
        [SerializeField]
        protected string title = "PopUp";
        [SerializeField]
        protected string message = "This is the pop up message";
        [SerializeField]
        protected string button = "OK";
        [SerializeField]
        protected Sprite icon;
        [SerializeField]
        protected AudioClip clip;

        [Header("Action")]
        [SerializeField]
        protected bool showIfHoldingItem = true;
        [SerializeField]
        protected bool stopSubtitles = true;
        [SerializeField]
        protected bool hideMeshOnStart = true;

        private void Awake()
        {
            GetComponent<Renderer>().enabled = hideMeshOnStart;
        }

        public void OnTriggerEnter(Collider other)
        {
            Trigger(other);
        }

        protected virtual void Trigger(Collider other)
        {
            if (other.transform.Equals(PlayerManager.Instance.GetLocalPlayer().TransformObject))
            {
                if (!showIfHoldingItem)
                {
                    if (ItemManager.Instance.IsHolding) return;
                }

                if (stopSubtitles)
                {
                    SubtitleManager.Instance.Stop();
                }

                PopupManager.instance.ShowPopUp(title, message, button, icon, clip);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PopUpTrigger), true)]
        public class PopUpTrigger_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("title"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("message"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("button"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("clip"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("showIfHoldingItem"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stopSubtitles"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hideMeshOnStart"), true);

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
