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
    public class SmartphoneChatMessage : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI ownerText;
        [SerializeField]
        private TextMeshProUGUI messageText;
        [SerializeField]
        private Image messageBKG;
        [SerializeField]
        private VerticalLayoutGroup messageLayout;

        /// <summary>
        /// Update the new smartphone chat message contents
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="message"></param>
        /// <param name="anchor"></param>
        /// <param name="col"></param>
        /// <param name="sp"></param>
        public void Set(string owner, string message, TextAnchor anchor, Color col, Sprite sp)
        {
            ownerText.text = owner;
            messageText.text = message;
            messageLayout.childAlignment = anchor;
            messageBKG.sprite = sp;
            messageBKG.color = col;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SmartphoneChatMessage), true)]
        public class SmartphoneChatMessage_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ownerText"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messageText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messageBKG"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messageLayout"), true);

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
