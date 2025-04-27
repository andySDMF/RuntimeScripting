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
    public class EmojiPanel : MonoBehaviour
    {
        [Header("Emoji")]
        [SerializeField]
        private GameObject emojiPrefab;

        [SerializeField]
        private Transform emojiContainer;

        [Header("Text")]
        [SerializeField]
        private TextMeshProUGUI textScript;


        private void Start()
        {
            for (int i = 0; i < AppManager.Instance.Settings.playerSettings.emoteIcons.Count; i++)
            {
                GameObject go = Instantiate(emojiPrefab, Vector3.zero, Quaternion.identity, emojiContainer);
                go.transform.localScale = Vector3.one;
                go.name = "Cell_Emoji_" + AppManager.Instance.Settings.playerSettings.emoteIcons[i].name;
                go.SetActive(true);
                go.transform.GetChild(0).GetComponent<Image>().sprite = AppManager.Instance.Settings.playerSettings.emoteIcons[i].icon;
                go.transform.GetChild(0).GetComponent<Image>().SetNativeSize();

                int n = AppManager.Instance.Settings.playerSettings.emoteIcons[i].id;
                go.GetComponent<Button>().onClick.AddListener(() => { SelectEmoji(n); });
            }
        }

        public void ToggleVisibility()
        {
            gameObject.SetActive(!gameObject.activeInHierarchy);
        }

        private void SelectEmoji(int emoji)
        {
            textScript.GetComponentInParent<TMP_InputField>().text += "<color=\"white\"><sprite=" + emoji.ToString() + "></color>";

            gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(EmojiPanel), true)]
        public class EmojiPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("emojiPrefab"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("emojiContainer"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("textScript"), true);

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