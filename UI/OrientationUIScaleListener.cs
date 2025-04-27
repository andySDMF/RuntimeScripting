using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class OrientationUIScaleListener : MonoBehaviour, IOrientationUI
    {
        public void Adjust(float aspectRatio)
        {
            transform.localScale = new Vector3(aspectRatio, aspectRatio, 1.0f);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OrientationUIScaleListener), true)]
        public class OrientationUIScaleListener_Editor : BaseInspectorEditor
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
