using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace BrandLab360
{
    /// <summary>
    /// Script used on a toggle within the _CONTENTS GO
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class ContentFileTab : MonoBehaviour
    {
        [SerializeField]
        private ContentsManager.ContentType type = ContentsManager.ContentType.All;

        private void Awake()
        {
            GetComponent<Toggle>().onValueChanged.AddListener(OnValueChanged);
        }

        /// <summary>
        /// Action subscribed to the toggles listener
        /// </summary>
        /// <param name="val"></param>
        public void OnValueChanged(bool val)
        {
            //only send if true
            if(val)
            {
                ContentsPanel.Instance.ToggleCurrentContentIndex(type);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ContentFileTab), true)]
        public class ContentFileTab_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("type"), true);

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
