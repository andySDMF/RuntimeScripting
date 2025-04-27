using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ThemeManager : MonoBehaviour
    {
        public bool HasInstantiated
        {
            get;
            private set;
        }

        private void Start()
        {
            if (AppManager.IsCreated)
            {
                StartCoroutine(Wait());
            }
        }

        private IEnumerator Wait()
        {
            while (!AppManager.IsCreated || AppManager.Instance.Settings == null)
            {
                yield return null;
            }

            var themes = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<ITheme>();

            foreach (var theme in themes)
            {
                theme.Apply(AppManager.Instance.Settings.themeSettings);
            }

            //this is expensive so best to do it via the app settings window
            if (AppManager.Instance.Settings.playerSettings.apply3DButtonAppearenceAtRuntime)
            {
                ButtonAppearance[] appearences = FindObjectsByType<ButtonAppearance>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                for (int i = 0; i < appearences.Length; i++)
                {
                    appearences[i].Apply(AppManager.Instance.Settings.playerSettings.buttonAppearance);
                }
            }

            HasInstantiated = true;
        }
    }

    public interface ITheme
    {
        void Apply(ThemeSettings settings);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ThemeManager), true)]
    public class ThemeManager_Editor : BaseInspectorEditor
    {
        private void OnEnable()
        {
            GetBanner();
        }

        public override void OnInspectorGUI()
        {
            DisplayBanner();
        }
    }
#endif
}
