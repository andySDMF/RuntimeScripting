using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class RegisterInterestButton : MonoBehaviour
    {
        public void Open()
        {
            HUDManager.Instance.ToggleHUDScreen("REGISTERINTEREST_SCREEN");
        }

        public void Close()
        {
            HUDManager.Instance.ToggleHUDScreen("REGISTERINTEREST_SCREEN");
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(RegisterInterestButton), true)]
        public class RegisterInterestButton_Editor : BaseInspectorEditor
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
