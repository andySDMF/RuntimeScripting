using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ScrollViewArrow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField]
        private ScrollRect scrollable;

        [SerializeField]
        private ScrollDir direction = ScrollDir._Down;

        private bool m_hovered = false;
        private bool m_pointerDown = false;

        private void Update()
        {
            if(scrollable != null && (m_hovered || m_pointerDown))
            {
                if(direction.Equals(ScrollDir._Down))
                {
                    if (scrollable.verticalNormalizedPosition > 0)
                        scrollable.verticalNormalizedPosition -= 0.01f;
                }
                else
                {
                    if (scrollable.verticalNormalizedPosition < 1)
                        scrollable.verticalNormalizedPosition += 0.01f;
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            m_hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_hovered = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_pointerDown = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_pointerDown = false;
        }


        [System.Serializable]
        private enum ScrollDir { _Down, _up }

#if UNITY_EDITOR
        [CustomEditor(typeof(ScrollViewArrow), true)]
        public class ScrollViewArrow_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                EditorGUILayout.LabelField("Component", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("scrollable"), true);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Behaviour", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("direction"), true);

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
