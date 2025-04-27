using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class SliderValueText : MonoBehaviour
    {
        private Text m_text;

        private void Awake()
        {
            GetComponentInParent<Slider>().onValueChanged.AddListener(OnValueChanged);
            m_text = GetComponent<Text>();
        }

        private void OnValueChanged(float arg0)
        {
            if(m_text != null)
            {
                m_text.text = Math.Round(arg0, 2).ToString();
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SliderValueText), true)]
        public class SliderValueText_Editor : BaseInspectorEditor
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
