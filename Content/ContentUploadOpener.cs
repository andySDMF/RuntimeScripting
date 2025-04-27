using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ContentUploadOpener : MonoBehaviour
    {
        private void Awake()
        {
            if(!CoreManager.Instance.projectSettings.useContentsAPI)
            {
                gameObject.SetActive(false);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ContentUploadOpener), true)]
        public class ContentUploadOpener_Editor : BaseInspectorEditor
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
