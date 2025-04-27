using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// AssortmentBounds are the bounds of the assortment within which the product can display
    /// and be interacted with to manipulate their position etc.
    /// </summary>
    public class AssortmentBounds : MonoBehaviour
    {
        public Assortment ParentAssortment;

#if UNITY_EDITOR
        [CustomEditor(typeof(AssortmentBounds), true)]
        public class AssortmentBounds_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
            }
        }
#endif
    }
}