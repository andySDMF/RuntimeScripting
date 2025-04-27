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
    public class AvatarSelectionCell : MonoBehaviour
    {
        [SerializeField]
        private RawImage img;

        [SerializeField]
        private TextMeshProUGUI id;

        private string m_avatar;

        public string AvatarName
        {
            get
            {
                return m_avatar;
            }
        }

        public void Set(string avatarName, string pictureResource)
        {
            img.texture = Resources.Load<Texture>(pictureResource);
            m_avatar = avatarName;

            string nm = avatarName.Contains("RPM_") ? avatarName.Substring(4) : avatarName;
            id.text = nm;
        }

        public void OnClick()
        {
            GetComponentInParent<AvatarSelectionPreview>().ChooseAvatar(m_avatar);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AvatarSelectionCell), true)]
        public class AvatarSelectionCell_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("img"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("id"), true);
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
