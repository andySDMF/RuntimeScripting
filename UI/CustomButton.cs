using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class CustomButton : MonoBehaviour
    {
        [SerializeField]
        private string guid = "";

        public void PrintGUID()
        {
            if (string.IsNullOrEmpty(guid))
            {
                Debug.Log(guid);
            }
        }

#if UNITY_EDITOR
        public string GUID
        {
            get
            {
                return guid;
            }
            set
            {
                guid = value;
            }
        }

        public void Apply(string appearance, string style, string content, int theme = -1)
        {
            if(string.IsNullOrEmpty(guid))
            {

            }

            ButtonAppearance bAppearance = GetComponentInChildren<ButtonAppearance>(true);

            if (bAppearance != null)
            {
                bAppearance.Apply(appearance.Equals("_Square") ? ButtonAppearance.Appearance._Square : ButtonAppearance.Appearance._Round);
            }

            Graphic graphic = transform.GetChild(0).GetComponentInChildren<Graphic>();

            if(graphic is Image)
            {
                if(style.Equals("_Icon"))
                {
                    Sprite sp = (Sprite)UnityEditor.AssetDatabase.LoadAssetAtPath(content, typeof(Sprite));

                    if(sp != null)
                    {
                        ((Image)graphic).sprite = sp;
                    }
                }
                else
                {
                    DestroyImmediate(graphic);

                    //add TMP
                }
            }
            else
            {
                if (style.Equals("_Text"))
                {
                    ((TextMeshProUGUI)graphic).text = content;
                }
                else
                {
                    DestroyImmediate(graphic);

                    //add image
                }
            }
        }
#endif

#if UNITY_EDITOR
        [CustomEditor(typeof(CustomButton), true)]
        public class CustomButton_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("guid"), true);

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
