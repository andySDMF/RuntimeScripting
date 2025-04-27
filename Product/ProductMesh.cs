using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360 
{ 
    public class ProductMesh : MonoBehaviour
    {
        public Product product;

        [HideInInspector]
        public int UniqueProductPlacementID = -1;
        [HideInInspector]
        public string ProductPlacementShop = "";
        [HideInInspector]
        public string ProductPlacementCollection = "";
        [HideInInspector]
        public string rawTextureSource = "";

#if UNITY_EDITOR
        [CustomEditor(typeof(ProductMesh), true)]
        public class ProductMesh_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("product"), true);

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