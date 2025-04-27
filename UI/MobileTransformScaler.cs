using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class MobileTransformScaler : MonoBehaviour
    {
        private void Start()
        {
            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
        }

        private void OnDisable()
        {
            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    transform.localScale = new Vector3(1, 1, 1);
                }
                else
                {
                    float aspect = arg2 / arg1;
                    float scaler = aspect / 4;
                    transform.localScale = new Vector3(1 + scaler, 1 + scaler, 1);
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(MobileTransformScaler), true)]
        public class MobileTransformScaler_Editor : BaseInspectorEditor
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
