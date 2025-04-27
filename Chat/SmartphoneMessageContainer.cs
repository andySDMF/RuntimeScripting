using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class SmartphoneMessageContainer : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(WaitFrame());
        }

        private IEnumerator WaitFrame()
        {
            yield return new WaitForEndOfFrame();

            Toggle[] togs = GetComponentsInChildren<Toggle>();

            for (int i = 0; i < togs.Length; i++)
            {
                if (togs[i].transform.parent.name != "Layout_Tabs") continue;

                if (togs[i].name.Contains("People"))
                {
                    togs[i].isOn = true;
                    break;
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SmartphoneMessageContainer), true)]
        public class SmartphoneMessageContainer_Editor : BaseInspectorEditor
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
