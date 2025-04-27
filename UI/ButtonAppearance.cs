using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Image))]
    public class ButtonAppearance : MonoBehaviour
    {
        [SerializeField]
        private Appearance appearance = Appearance._Round;

        public enum Appearance { _Square, _Round }

        private bool m_hasChangedAtRuntime = false;

        private void Start()
        {
            if (AppManager.IsCreated)
            {
                if (CoreManager.Instance.gameObject.GetComponent<ThemeManager>().HasInstantiated)
                {
                    m_hasChangedAtRuntime = true;
                    return;
                }
                else
                {
                    Apply(AppManager.Instance.Settings.playerSettings.buttonAppearance);
                    m_hasChangedAtRuntime = true;
                }
            }
        }

        public void Apply(Appearance style)
        {
            if(Application.isPlaying)
            {
                if (appearance.Equals(style) || m_hasChangedAtRuntime) return;

                m_hasChangedAtRuntime = true;
            }


            appearance = style;

            if(appearance.Equals(Appearance._Round))
            {
                GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>("World/Circle");
            }
            else
            {
                GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>("World/Square");
            }


#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ButtonAppearance), true)]
        public class ButtonAppearance_Editor : BaseInspectorEditor
        {
            private ButtonAppearance script;

            private void OnEnable()
            {
                GetBanner();
                script = (ButtonAppearance)target;
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("appearance"), true);

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(script);

                    script.Apply(script.appearance);
                }
            }
        }
#endif
    }
}
