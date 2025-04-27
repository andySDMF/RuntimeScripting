using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class HelpPanel : MonoBehaviour
    {
        [SerializeField]
        private Image output;

        [SerializeField]
        private GameObject conatinerButtons;

        [SerializeField]
        private Sprite defaultSprite;

        private int m_index = 0;

        private void OnEnable()
        {
            if(AppManager.IsCreated)
            {
                if(AppManager.Instance.Settings.HUDSettings.helpSprites.Count > 0)
                {
                    output.sprite = AppManager.Instance.Settings.HUDSettings.helpSprites[0];
                    output.SetNativeSize();

                    conatinerButtons.SetActive(AppManager.Instance.Settings.HUDSettings.helpSprites.Count > 1);
                }
                else
                {
                    output.sprite = defaultSprite;
                    output.SetNativeSize();
                    conatinerButtons.SetActive(false);
                }

                SetAspectRatio();

                m_index = 0;
            }
        }

        public void Close()
        {
            HUDManager.Instance.ShowHelp(false);
        }

        public void Next()
        {
            if (AppManager.Instance.Settings.HUDSettings.helpSprites.Count > 0)
            {
                if(m_index + 1 < AppManager.Instance.Settings.HUDSettings.helpSprites.Count - 1)
                {
                    m_index++;
                    output.sprite = AppManager.Instance.Settings.HUDSettings.helpSprites[m_index];
                    output.SetNativeSize();
                    SetAspectRatio();
                }
            }
        }

        public void Previous()
        {
            if (AppManager.Instance.Settings.HUDSettings.helpSprites.Count > 0)
            {
                if (m_index - 1 >= 0)
                {
                    m_index--;
                    output.sprite = AppManager.Instance.Settings.HUDSettings.helpSprites[m_index];
                    output.SetNativeSize();
                    SetAspectRatio();
                }
            }
        }

        private void SetAspectRatio()
        {
            //set aspect ratio
            AspectRatioFitter ratio = null;

            if (GetComponentsInChildren<AspectRatioFitter>(true).Length > 0)
            {
                ratio = GetComponentsInChildren<AspectRatioFitter>(true)[0];
            }

            if (ratio != null)
            {
                float texWidth = output.sprite.texture.width;
                float texHeight = output.sprite.texture.height;
                float aspectRatio = texWidth / texHeight;
                ratio.aspectRatio = aspectRatio;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(HelpPanel), true)]
        public class HelpPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("output"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("conatinerButtons"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultSprite"), true);

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