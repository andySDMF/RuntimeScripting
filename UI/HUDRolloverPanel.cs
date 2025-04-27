using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class HUDRolloverPanel : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI textScript;
        [SerializeField]
        private LayoutElement layoutElement;
        [SerializeField]
        private float tooltipOffset = 75.0f;

        private RectTransform m_recT;

        private OrientationType m_switch = OrientationType.landscape;
        private float m_scaler;
        private float m_fontSize;

        private void OnDisable()
        {
            Set("");
        }

        private void Awake()
        {
            m_recT = GetComponent<RectTransform>();

            m_fontSize = textScript.fontSize;
            m_scaler = AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
        }

        private void OnEnable()
        {
            if(m_recT != null)
            {
                Vector3 pos = InputManager.Instance.GetMousePosition();
                m_recT.position = new Vector3(pos.x, pos.y, pos.z);
            }
        }

        private void Update()
        {
            Vector3 pos = InputManager.Instance.GetMousePosition();

            if(m_recT.rect.width > 300.0f)
            {
                layoutElement.preferredWidth = 300.0f;
            }
            else
            {
                layoutElement.preferredWidth = -1;
            }

            //handle flip
            if(pos.y < Screen.height / 2)
            {
                transform.GetChild(0).localEulerAngles = new Vector3(0, 0, 0);
                textScript.transform.localEulerAngles = new Vector3(0, 0, 0);
                m_recT.position = new Vector3(pos.x, pos.y + tooltipOffset, pos.z);
            }
            else
            {
                transform.GetChild(0).localEulerAngles = new Vector3(180, 0, 0);
                textScript.transform.localEulerAngles = new Vector3(180, 0, 0);
                m_recT.position = new Vector3(pos.x, pos.y - tooltipOffset, pos.z);
            }

            if (AppManager.Instance.Data.IsMobile && !m_switch.Equals(OrientationManager.Instance.CurrentOrientation))
            {
                m_switch = OrientationManager.Instance.CurrentOrientation;

                if (m_switch.Equals(OrientationType.landscape))
                {
                    textScript.fontSize = m_fontSize;
                }
                else
                {
                    textScript.fontSize = m_fontSize * m_scaler;
                }
            }
        }

        public void Set(string txt)
        {
            if(m_recT != null)
            {
                Vector3 pos = InputManager.Instance.GetMousePosition();
                m_recT.position = new Vector3(pos.x, pos.y, pos.z);
            }

            textScript.text = txt;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(HUDRolloverPanel), true)]
        public class HUDRolloverPanel_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                if (Application.productName.Equals("BL360 Plugin"))
                {
                    serializedObject.Update();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("textScript"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("layoutElement"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tooltipOffset"), true);

                    if (GUI.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(this);
                    }
                }
            }
        }
#endif
    }
}
