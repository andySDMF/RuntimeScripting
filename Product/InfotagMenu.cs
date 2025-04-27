using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360 
{
    public class InfotagMenu : MonoBehaviour
    {
        public List<Infotag> Infotags;

        public void ToggleInfotag(InfotagType infotagType, bool isOn)
        {
            foreach(var infotag in Infotags)
            {
                if(infotag.infotagType == infotagType)
                {
                    infotag.gameObject.SetActive(isOn);
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(InfotagMenu), true)]
        public class InfotagMenu_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Infotags"), true);

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