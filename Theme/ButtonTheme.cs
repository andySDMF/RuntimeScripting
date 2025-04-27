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
    [RequireComponent(typeof(Button))]
    public class ButtonTheme : MonoBehaviour, ITheme
    {
        [SerializeField]
        private int buttonThemeIndex;

#if UNITY_EDITOR
        public void Apply(ThemeSettings settings, int index)
        {
            buttonThemeIndex = index;
            Apply(settings);
        }

        public void Apply()
        {
            AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");
            Apply(appReferences.Settings.themeSettings);
        }
#endif

        public void Apply(ThemeSettings settings)
        {
            if (buttonThemeIndex < 0) return;

            Button but = GetComponentInChildren<Button>(true);

            if (but != null)
            {
                ColorBlock block = new ColorBlock();
                block.normalColor = settings.buttonThemes[buttonThemeIndex].normalColor;
                block.highlightedColor = settings.buttonThemes[buttonThemeIndex].useBrandColor ? settings.brandColor : settings.buttonThemes[buttonThemeIndex].highlightColor;
                block.pressedColor = settings.buttonThemes[buttonThemeIndex].useBrandColor ? settings.brandColor : settings.buttonThemes[buttonThemeIndex].highlightColor;
                block.selectedColor = settings.buttonThemes[buttonThemeIndex].useBrandColor ? settings.brandColor : settings.buttonThemes[buttonThemeIndex].highlightColor;
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
                            graphics[i].color = settings.buttonThemes[buttonThemeIndex].graphicColor;
                        }
                    }
                    else if (graphics[i].transform.Equals(gameObject.transform.GetChild(0)))
                    {
                        if (!graphics[i].Equals(but.targetGraphic))
                        {
                            graphics[i].color = settings.buttonThemes[buttonThemeIndex].graphicColor;
                        }
                    }
                }

                TMP_Text txt = GetComponentInChildren<TMP_Text>(true);

                if (txt != null)
                {
                    switch (settings.buttonThemes[buttonThemeIndex].fontTheme)
                    {
                        case ThemeSettings.FontThemeType._Bold:

                            if (settings.boldFont != null)
                            {
                                txt.font = settings.boldFont;
                            }

                            break;
                        case ThemeSettings.FontThemeType._Italic:

                            if (settings.italicFont != null)
                            {
                                txt.font = settings.italicFont;
                            }

                            break;
                        case ThemeSettings.FontThemeType._Light:

                            if (settings.lightFont != null)
                            {
                                txt.font = settings.lightFont;
                            }

                            break;
                        case ThemeSettings.FontThemeType._Medium:

                            if (settings.mediumFont != null)
                            {
                                txt.font = settings.mediumFont;
                            }

                            break;
                        default:

                            if (settings.regularFont != null)
                            {
                                txt.font = settings.regularFont;
                            }

                            break;
                    }
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ButtonTheme), true), CanEditMultipleObjects]
        public class ButtonTheme_Editor : BaseInspectorEditor
        {
            private ButtonTheme script;
            private string[] themes;


            private void OnEnable()
            {
                GetBanner();

                script = (ButtonTheme)target;

                AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");
                themes = new string[appReferences.Settings.themeSettings.buttonThemes.Count];

                for (int i = 0; i < themes.Length; i++)
                {
                    themes[i] = appReferences.Settings.themeSettings.buttonThemes[i].id;
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
