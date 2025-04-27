using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class FontTheme : MonoBehaviour, ITheme
    {
        [SerializeField]
        private ThemeSettings.FontThemeType theme = ThemeSettings.FontThemeType._Medium;

#if UNITY_EDITOR
        public void Apply(ThemeSettings settings, int index)
        {
            theme = (ThemeSettings.FontThemeType)index;
            Apply(settings);
        }
#endif

        public void Apply(ThemeSettings settings)
        {
            TMP_Text txt = GetComponentInChildren<TMP_Text>(true);

            if (txt != null)
            {
                switch(theme)
                {
                    case ThemeSettings.FontThemeType._Bold:

                        if(settings.boldFont != null)
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

#if UNITY_EDITOR
        [CustomEditor(typeof(FontTheme), true), CanEditMultipleObjects]
        public class FontTheme_Editor : BaseInspectorEditor
        {
            private FontTheme script;
         

            private void OnEnable()
            {
                GetBanner();

                script = (FontTheme)target;

                AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");
                script.Apply(appReferences.Settings.themeSettings);
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                if (Application.isPlaying) return;

                serializedObject.Update();

                EditorGUILayout.LabelField("Theme", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("theme"), true);

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
