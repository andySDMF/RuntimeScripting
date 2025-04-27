using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ButtonOpenAdmin : MonoBehaviour
    {
        public void OnClick()
        {
            AdminManager.Instance.ToggleAdminPanel(true);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ButtonOpenAdmin), true)]
        public class ButtonOpenAdmin_Editor : BaseInspectorEditor
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
