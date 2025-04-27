using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class AdminSRTab : MonoBehaviour
    {
        [SerializeField]
        private SRTab tab = SRTab.System;

        private void Awake()
        {
#if BRANDLAB360_INTERNAL
            UnityEngine.Object obj = Resources.Load(tab.ToString());
            GameObject goObj = Instantiate((GameObject)obj, Vector3.zero, Quaternion.identity, transform);
            goObj.transform.localScale = Vector3.one;
            goObj.transform.localEulerAngles = Vector3.zero;

            goObj.gameObject.SetActive(true);
            RectTransform rectT = goObj.GetComponent<RectTransform>();

            if(rectT != null)
            {
                rectT.anchorMin = Vector2.zero;
                rectT.anchorMax = Vector3.one;
                rectT.pivot = new Vector2(0.5f, 0.5f);

                rectT.offsetMax = Vector2.zero;
                rectT.offsetMin = Vector2.zero;
            }
#endif
        }

        [System.Serializable]
        private enum SRTab { System, Profiler, Network }


#if UNITY_EDITOR
        [CustomEditor(typeof(AdminSRTab), true)]
        public class AdminSRTab_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tab"), true);

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
