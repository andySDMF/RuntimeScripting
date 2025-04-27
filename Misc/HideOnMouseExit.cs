using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace BrandLab360
{
    public class HideOnMouseExit : MonoBehaviour, IPointerExitHandler
    {
        [SerializeField]
        private GameObject hideObject;

        private void Awake()
        {
            if(hideObject == null)
            {
                hideObject = gameObject;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hideObject.SetActive(false);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(HideOnMouseExit), true)]
        public class HideOnMouseExit_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("hideObject"), true);

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
