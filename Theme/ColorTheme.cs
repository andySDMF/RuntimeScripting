using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Graphic))]
    public class ColorTheme : MonoBehaviour, ITheme
    {
        [SerializeField]
        private int colorThemeIndex;

#if UNITY_EDITOR
        public void Apply(ThemeSettings settings, int index)
        {
            colorThemeIndex = index;
            Apply(settings);
        }
#endif

        public void Apply(ThemeSettings settings)
        {
            if (colorThemeIndex < 0) return;

            Graphic[] all = GetComponentsInChildren<Graphic>(true);

            for(int i = 0; i < all.Length; i++)
            {
                if(all[i].gameObject.Equals(gameObject))
                {
                    all[i].color = settings.colorThemes[colorThemeIndex].color;
                    break;
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ColorTheme), true), CanEditMultipleObjects]
        public class ColorTheme_Editor : BaseInspectorEditor
        {
            private ColorTheme script;
            private string[] colors;


            private void OnEnable()
            {
                GetBanner();

                script = (ColorTheme)target;

                AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");
                colors = new string[appReferences.Settings.themeSettings.colorThemes.Count];

                for(int i = 0;i < colors.Length; i++)
                {
                    colors[i] = appReferences.Settings.themeSettings.colorThemes[i].id;
                }

                if(script.colorThemeIndex >= colors.Length)
                {
                    if(colors.Length > 0)
                    {
                        script.colorThemeIndex = colors.Length - 1;
                    }
                    else
                    {
                        script.colorThemeIndex = -1;
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

                if(colors.Length > 0)
                {
                    script.colorThemeIndex = EditorGUILayout.Popup(script.colorThemeIndex, colors);
                }
                else
                {
                    EditorGUILayout.LabelField("No Colors Available within Theme Settings");
                    script.colorThemeIndex = -1;
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
