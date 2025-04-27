using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Tooltip : MonoBehaviour
    {
        [SerializeField]
        private string id = "";

        //used to get the ID that this tooltip is using from the AppInstances.Tooltips
        public string ID
        {
            get
            {
                return id;
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Tooltip), true)]
    public class Tooltip_Editor : BaseInspectorEditor
    {
        private Tooltip script;

        private string[] m_tooltips;
        private int m_selectedTooltip = 0;
        private AppInstances m_content;

        private void OnEnable()
        {
            GetBanner();
            script = (Tooltip)target;

            AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");

            if (appReferences != null)
            {
                m_content = appReferences.Instances;
            }
            else
            {
                m_content = Resources.Load<AppInstances>("ProjectAppInstances");
            }

            if (m_content != null)
            {
                m_tooltips = new string[m_content.tooltips.Count];

                for (int i = 0; i < m_content.tooltips.Count; i++)
                {
                    m_tooltips[i] = m_content.tooltips[i].id;

                    if(script.ID.Equals(m_tooltips[i]))
                    {
                        m_selectedTooltip = i;
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            DisplayBanner();
            serializedObject.Update();

            if(m_tooltips.Length > 0)
            {
                int selected = EditorGUILayout.Popup("Tooltip", m_selectedTooltip, m_tooltips);

                if (selected != m_selectedTooltip)
                {
                    m_selectedTooltip = selected;
                }

                serializedObject.FindProperty("id").stringValue = m_tooltips[m_selectedTooltip];
            }
            else
            {
                EditorGUILayout.LabelField("No Tooltip exist!", EditorStyles.miniBoldLabel);

                m_selectedTooltip = 0;
                serializedObject.FindProperty("id").stringValue = "";
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(script);
        }
    }
#endif
}
