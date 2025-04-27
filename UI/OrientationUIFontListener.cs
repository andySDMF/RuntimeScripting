using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class OrientationUIFontListener : MonoBehaviour, IOrientationUI
    {
        private float fontSize;
        private bool hasInit = false;

        private TextMeshProUGUI tmPro;

        public void Adjust(float aspectRatio)
        {
            if(!hasInit)
            {
                tmPro = GetComponentInChildren<TextMeshProUGUI>();
                fontSize = tmPro.fontSize;
            }

            tmPro.fontSize = fontSize * aspectRatio;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OrientationUIFontListener), true)]
        public class OrientationUIFontListener_Editor : BaseInspectorEditor
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
