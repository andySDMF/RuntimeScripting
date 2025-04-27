using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(ContentSizeFitter))]
    public class LayoutRestriction : MonoBehaviour
    {
        [SerializeField]
        private bool hasRestrictedWidth;

        [SerializeField]
        private float maxWidth = 700.0f;

        [SerializeField]
        private bool hasRestrictedHeight;

        [SerializeField]
        private float maxHeight = 200.0f;

        private void OnEnable()
        {
            StartCoroutine(Delay());
        }

        private IEnumerator Delay()
        {
            yield return new WaitForEndOfFrame();

            ContentSizeFitter sizeFitter = GetComponent<ContentSizeFitter>();
            RectTransform rectT = GetComponent<RectTransform>();

            if (rectT.sizeDelta.x > maxWidth)
            {
                sizeFitter.horizontalFit = hasRestrictedWidth ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize;
            }

            if (rectT.sizeDelta.y > maxHeight)
            {
                sizeFitter.verticalFit = hasRestrictedHeight ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize;
            }

            rectT.sizeDelta = new Vector2(maxWidth, maxHeight);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(LayoutRestriction), true)]
        public class LayoutRestriction_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.LabelField("Width", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hasRestrictedWidth"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxWidth"), true);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Height", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hasRestrictedHeight"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHeight"), true);

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