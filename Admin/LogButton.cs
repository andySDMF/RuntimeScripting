using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class LogButton : MonoBehaviour
    {
        public void SendLogToWebClient()
        {
            AdminManager.Instance.SendLogToWebClient();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(LogButton), true)]
        public class LogButton_Editor : BaseInspectorEditor
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
