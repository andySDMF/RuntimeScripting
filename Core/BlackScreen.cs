using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class BlackScreen : Singleton<BlackScreen>
    {
        public static BlackScreen Instance
        {
            get
            {
                return ((BlackScreen)instance);
            }
            set
            {
                instance = value;
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            Show(false);
        }

        public void Show(bool show)
        {
            gameObject.SetActive(show);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(BlackScreen), true)]
        public class BlackScreen_Editor : BaseInspectorEditor
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
