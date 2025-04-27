using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class SocialMediaCanvas : MonoBehaviour
    {
        [SerializeField]
        private string m_reference = "";

        [SerializeField]
        private GameObject selection;

        [SerializeField]
        private CommentType commentType = CommentType.None;

        [SerializeField]
        private string m_comment = "";

        public string Reference
        {
            get
            {
                return m_reference;
            }
            set
            {
                m_reference = value;
            }
        }

        public string Comment
        {
            get
            {
                string str = "";

                switch(commentType)
                {
                    case CommentType.Allow:
                        str = "-2";
                        break;
                    case CommentType.Fixed:
                        str = m_comment;
                        break;
                    case CommentType.None:
                        str = "-1";
                        break;
                }

                return str;
            }
        }

        private void Start()
        {
          //  selection.SetActive(false);
        }

        public void IsVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        [SerializeField]
        public enum CommentType { Allow, Fixed, None }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SocialMediaCanvas), true), CanEditMultipleObjects]
    public class SocialMediaCanvas_Editor : BaseInspectorEditor
    {
        private SocialMediaCanvas script;

        private void OnEnable()
        {
            GetBanner();
            script = (SocialMediaCanvas)target;
        }

        public override void OnInspectorGUI()
        {
            DisplayBanner();
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_reference"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("selection"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("commentType"), true);

            if (serializedObject.FindProperty("commentType").enumValueIndex == 1)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_comment"), true);
            }

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(script);
            }
        }
    }
#endif
}
