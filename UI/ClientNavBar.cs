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
    public class ClientNavBar : MonoBehaviour
    {
        [SerializeField]
        private GameObject content;

        [SerializeField]
        private Image img;

        [SerializeField]
        private TextMeshProUGUI txt;

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if(AppManager.Instance.Settings.HUDSettings.clientDisplayType.Equals(ClientDisplayType.Off))
            {
                gameObject.SetActive(false);
            }
            else if(AppManager.Instance.Settings.HUDSettings.clientDisplayType.Equals(ClientDisplayType.Name))
            {
                txt.text = AppManager.Instance.Settings.HUDSettings.clientName;

                if (string.IsNullOrEmpty(txt.text))
                {
                    gameObject.SetActive(false);
                }
                else
                {
                    txt.gameObject.SetActive(true);
                    content.SetActive(true);
                }
            }
            else if (AppManager.Instance.Settings.HUDSettings.clientDisplayType.Equals(ClientDisplayType.Logo))
            {
                img.sprite = AppManager.Instance.Settings.HUDSettings.clientLogo;

                if(img.sprite == null)
                {
                    gameObject.SetActive(false);
                }
                else
                {
                    img.gameObject.SetActive(true);
                    content.SetActive(true);
                }
            }
            else
            {
                //create the prefab from settings
                if (AppManager.Instance.Settings.HUDSettings.clientPrefab != null)
                {
                    GameObject go = Instantiate(AppManager.Instance.Settings.HUDSettings.clientPrefab, Vector3.zero, Quaternion.identity, transform);
                    go.transform.localScale = Vector3.one;
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        [System.Serializable]
        public enum ClientDisplayType { Off, Logo, Name, Custom }

#if UNITY_EDITOR
        [CustomEditor(typeof(ClientNavBar), true)]
        public class ClientNavBar_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("content"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("img"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("txt"), true);


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
