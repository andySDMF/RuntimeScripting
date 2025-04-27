using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class OrbitPanelButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        private CameraOrbit.Tool tool = CameraOrbit.Tool.None;

        [SerializeField]
        private Color on = Color.magenta;

        [SerializeField]
        private GameObject onGO;

        private bool m_isOn;
        private OrbitPanel m_panel;
        private Color m_color;
        private bool m_cachecColor = false;
        

        public bool IsSelected
        {
            get;
            set;
        }


        public bool IsOn
        {
            get
            {
                return m_isOn;
            }
            set
            {
                m_isOn = value;

                if(IsOn)
                {
                    if(!m_cachecColor)
                    {
                        m_cachecColor = true;
                        Color col = GetComponent<Image>().color;
                        m_color = new Color(col.r, col.g, col.b, col.a);
                    }

                    GetComponent<Image>().color = on;
                    onGO.SetActive(true);
                }
                else
                {
                    if(m_cachecColor)
                    {
                        GetComponent<Image>().color = m_color;
                    }

                    onGO.SetActive(false);
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            IsSelected = !IsSelected;
            IsOn = !IsOn;

            if (m_panel == null)
            {
                m_panel = GetComponentInParent<OrbitPanel>();
            }

            StopAllCoroutines();

            if(m_panel != null)
            {
                switch (tool)
                {
                    case CameraOrbit.Tool.Pan:
                        if(IsOn)
                        {
                            m_panel.Pan();
                        }
                        else
                        {
                            m_panel.None();
                        }
                      
                        break;
                    case CameraOrbit.Tool.Rotate:
                        if (IsOn)
                        {
                            m_panel.Rotate();
                        }
                        else
                        {
                            m_panel.None();
                        }
                        break;
                    case CameraOrbit.Tool.ZoomIn:
                        m_panel.ZoomIn();
                        IsSelected = false;
                        StartCoroutine(Wait());
                        break;
                    case CameraOrbit.Tool.ZoomOut:
                        m_panel.ZoomOut();
                        IsSelected = false;
                        StartCoroutine(Wait());
                        break;
                    default:
                        break;
                }
            }
        }

        private IEnumerator Wait()
        {
            yield return new WaitForSeconds(0.5f);

            IsOn = false;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OrbitPanelButton), true)]
        public class OrbitPanelButton_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tool"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("on"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("onGO"), true);

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
