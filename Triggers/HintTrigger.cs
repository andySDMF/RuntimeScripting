using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class HintTrigger : MonoBehaviour
    {
        [Header("Hint")]
        [SerializeField]
        protected string hintTitle = "Hint";
        [SerializeField]
        protected string hintMessage = "This is the hint message";
        [SerializeField]
        protected AudioClip clip;
        [SerializeField]
        [Tooltip("For Perminant display make duration 0.0f")]
        [Range(0.0f, 10.0f)]
        protected float hintDisplayDuration = 5.0f;

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

                PopupManager.instance.ShowHint(hintTitle, hintMessage, hintDisplayDuration, clip);
            }
        }

        public void HideHint()
        {
            //need to check if the hint is open
            if (HUDManager.Instance.GetHUDMessageObject("HINT_MESSAGE").activeInHierarchy)
            {
                PopupManager.instance.HideHint();
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(HintTrigger), true)]
        public class HintTrigger_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("hintTitle"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hintMessage"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("clip"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hintDisplayDuration"), true);

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
