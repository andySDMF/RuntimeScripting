using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Button))]
    public class ConfigureButton : MonoBehaviour
    {
        private Configurator m_config;

        [SerializeField]
        private int m_index = 0;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);

            m_config = GetComponentInParent<Configurator>();

            if (m_config.Source != null)
            {
                m_config = m_config.Source;
            }
        }

        public void Set(int index)
        {
            m_index = index;
        }

        public void OnClick()
        {
            if (CoreManager.Instance.projectSettings.configTagMode.Equals(TagMode._3D) && !m_config.CanUse) return;

            RaycastManager.Instance.UIRaycastSelectablePressed(GetComponent<Selectable>());

            //send Room room change
            if (m_config != null)
            {
                bool networked = AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online);

                switch (m_config.Type)
                {
                    case ConfiguratorManager.ConfiguratorType.Color:
                        m_config.Set(m_index.ToString(), networked);
                        break;
                    case ConfiguratorManager.ConfiguratorType.Material:
                        m_config.Set(m_index.ToString(), networked);
                        break;
                    case ConfiguratorManager.ConfiguratorType.Model:
                        m_config.Set(m_index.ToString(), networked);
                        break;
                    default:
                        break;
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ConfigureButton), true)]
        public class ConfigureButton_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_index"), true);

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
