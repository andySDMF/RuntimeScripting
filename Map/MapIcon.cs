using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class MapIcon : MonoBehaviour, IPointerClickHandler
    {
        private string m_ref = "";

        public void Set(string positionRef)
        {
            m_ref = positionRef;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick();
        }

        public void OnClick()
        {
            MapManager.Instance.MoveToPosition(m_ref.ToUpper());
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(MapIcon), true)]
        public class MapIcon_Editor : BaseInspectorEditor
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
