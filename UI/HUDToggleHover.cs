using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class HUDToggleHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private bool m_activeOnHoverOver = false;
        private Transform m_child;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (m_child == null)
            {
                m_child = transform.GetChild(0);
            }

            m_activeOnHoverOver = m_child.gameObject.activeInHierarchy;
            m_child.gameObject.SetActive(true);
            m_child.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_child.SetAsFirstSibling();
            m_child.gameObject.SetActive(m_activeOnHoverOver);

            if(!GetComponentInParent<UnityEngine.UI.Toggle>().isOn)
            {
                m_child.gameObject.SetActive(true);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(HUDToggleHover), true)]
        public class HUDToggleHover_Editor : BaseInspectorEditor
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

