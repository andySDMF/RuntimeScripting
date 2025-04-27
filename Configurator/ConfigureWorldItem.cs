using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ConfigureWorldItem : MonoBehaviour
    {
        private Configurator m_config;

        public void Set(Configurator config)
        {
            m_config = config;
        }

        public void OnClick()
        {
            if (!m_config.UserCanControl()) return;

            ConfiguratorManager.instance.Set2DConfigPallette(m_config);

            if (m_config.Type.Equals(ConfiguratorManager.ConfiguratorType.Transform))
            {
                ConfiguratorManager.instance.UseRTE(true);
                ConfiguratorManager.instance.SetRTEObject(gameObject);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ConfigureWorldItem), true)]
        public class ConfigureWorldItem_Editor : BaseInspectorEditor
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
