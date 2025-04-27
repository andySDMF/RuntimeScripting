using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class DeviceGOVisibility : MonoBehaviour
    {
        [SerializeField]
        private DeviceCheck check = DeviceCheck._Mobile;

        private void Start()
        {
            if(AppManager.IsCreated)
            {
                if(AppManager.Instance.Data.IsMobile)
                {
                    gameObject.SetActive(check.Equals(DeviceCheck._Mobile) ? true : false);
                }
                else
                {
                    gameObject.SetActive(check.Equals(DeviceCheck._Desktop) ? true : false);
                }
            }
        }

        [System.Serializable]
        private enum DeviceCheck { _Mobile, _Desktop }

#if UNITY_EDITOR
        [CustomEditor(typeof(DeviceGOVisibility), true)]
        public class DeviceGOVisibility_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("check"), true);

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}
