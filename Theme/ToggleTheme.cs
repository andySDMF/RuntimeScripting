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
    [RequireComponent(typeof(Toggle))]
    public class ToggleTheme : MonoBehaviour, ITheme
    {
        [SerializeField]
        private int buttonThemeIndex;

        public void Apply(ThemeSettings settings)
        {
            if (buttonThemeIndex < 0) return;

            Toggle but = GetComponentInChildren<Toggle>(true);

            if (but != null)
            {
                ColorBlock block = new ColorBlock();
                block.normalColor = settings.toggleThemes[buttonThemeIndex].normalColor;
                block.highlightedColor = settings.toggleThemes[buttonThemeIndex].useBrandColor ? settings.brandColor : settings.toggleThemes[buttonThemeIndex].highlightColor;
                block.pressedColor = settings.toggleThemes[buttonThemeIndex].useBrandColor ? settings.brandColor : settings.toggleThemes[buttonThemeIndex].highlightColor;
                block.selectedColor = settings.toggleThemes[buttonThemeIndex].useBrandColor ? settings.brandColor : settings.toggleThemes[buttonThemeIndex].highlightColor;
                block.disabledColor = but.colors.normalColor;
                block.colorMultiplier = but.colors.colorMultiplier;
                block.fadeDuration = but.colors.fadeDuration;

                but.colors = block;

                Graphic[] graphics = GetComponentsInChildren<Graphic>(true);

                for (int i = 0; i < graphics.Length; i++)
                {
                    if (graphics[i].gameObject.Equals(gameObject))
                    {
                        if (!graphics[i].Equals(but.targetGraphic))
                        {
                            graphics[i].color = settings.toggleThemes[buttonThemeIndex].graphicColor;
                        }
                    }
                    else if (graphics[i].transform.Equals(gameObject.transform.GetChild(0)))
                    {
                        if (!graphics[i].Equals(but.targetGraphic))
                        {
                            graphics[i].color = settings.toggleThemes[buttonThemeIndex].graphicColor;
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ToggleTheme), true), CanEditMultipleObjects]
        public class ToggleTheme_Editor : BaseInspectorEditor
        {
            private ToggleTheme script;
            private string[] themes;


            private void OnEnable()
            {
                GetBanner();

                script = (ToggleTheme)target;

                AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");
                themes = new string[appReferences.Settings.themeSettings.toggleThemes.Count];

                for (int i = 0; i < themes.Length; i++)
                {
                    themes[i] = appReferences.Settings.themeSettings.toggleThemes[i].id;
                }

                if (script.buttonThemeIndex >= themes.Length)
                {
                    if (themes.Length > 0)
                    {
                        script.buttonThemeIndex = themes.Length - 1;
                    }
                    else
                    {
                        script.buttonThemeIndex = -1;
                    }
                }

                script.Apply(appReferences.Settings.themeSettings);
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                if (Application.isPlaying) return;

                serializedObject.Update();

                EditorGUILayout.LabelField("Theme", EditorStyles.boldLabel);

                if (themes.Length > 0)
                {
                    script.buttonThemeIndex = EditorGUILayout.Popup(script.buttonThemeIndex, themes);
                }
                else
                {
                    EditorGUILayout.LabelField("No Buttons Available within Theme Settings");
                    script.buttonThemeIndex = -1;
                }

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(script);

                    AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");
                    script.Apply(appReferences.Settings.themeSettings);

                }
            }

        }
#endif

    }
}