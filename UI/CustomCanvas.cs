using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class CustomCanvas : MonoBehaviour
    {
        public string ID
        {
            get
            {
                return gameObject.name;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(CustomCanvas), true)]
        public class CustomCanvas_Editor : BaseInspectorEditor
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
