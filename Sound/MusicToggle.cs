using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Toggle))]
    public class MusicToggle : MonoBehaviour
    {
        private void Start()
        {
            if(AppManager.IsCreated)
            {
                GetComponent<Toggle>().isOn = !AppManager.Instance.Data.EnvironmentalSoundOn;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(MusicToggle), true)]
        public class MusicToggle_Editor : BaseInspectorEditor
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
