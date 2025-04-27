using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(CanvasScaler))]
    public class OrientationLetterbox : MonoBehaviour
    {
        [SerializeField]
        private RectTransform leftPadding;

        [SerializeField]
        private RectTransform rightPadding;

        public void Set(Vector2 resolution, float padding)
        {
            GetComponent<CanvasScaler>().referenceResolution = resolution;

            leftPadding.sizeDelta = new Vector2(padding, 0);
            rightPadding.sizeDelta = new Vector2(padding, 0);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OrientationLetterbox), true)]
        public class OrientationLetterbox_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("leftPadding"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("rightPadding"), true);

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
