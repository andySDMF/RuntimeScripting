using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class AppIntroPanel : MonoBehaviour
    {
        [SerializeField]
        private string id = "";

        [Header("UI")]
        public TMP_InputField inputObj;
        public TMP_Text errorLabelObj;

        [Header("Logos")]
        public GameObject brandlabLogo;

        [Header("Admin")]
        public GameObject adminButton;

        private void Start()
        {
            //need to push vars to the AppLogin
            AppLogin.Instance.PushComponents(id, inputObj, errorLabelObj, brandlabLogo);

            if(adminButton != null)
            {
                if(AppManager.Instance.Data.IsMobile)
                {
                    adminButton.SetActive(false);
                }
                else
                {
                    adminButton.SetActive(AppManager.Instance.Settings.projectSettings.useAdminUser);
                }
            }

            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

        }

        private void OnDisable()
        {
            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    transform.localScale = new Vector3(1, 1, 1);
                }
                else
                {
                    float aspect = arg2 / arg1;
                    float scaler = aspect / 4;
                    transform.localScale = new Vector3(1 + scaler, 1 + scaler, 1);
                }
            }
        }

        public void ApplyPassword()
        {
            AppLogin.Instance.OnClickPassword();
        }

        public void ApplyName()
        {
            AppLogin.Instance.OnClickName();
        }

        public void ToggleAdmin()
        {
            AppLogin.Instance.OpenAdmin();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AppIntroPanel), true)]
        public class AppIntroPanel_Editor : BaseInspectorEditor
        {
            private AppIntroPanel m_script;

            private void OnEnable()
            {
                GetBanner();

                m_script = (AppIntroPanel)target;
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                if (Application.productName.Equals("BL360 Plugin"))
                {
                    serializedObject.Update();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("id"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("inputObj"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("errorLabelObj"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("brandlabLogo"), true);

                    if (!m_script.gameObject.name.Contains("PASSWORD"))
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("adminButton"), true);
                    }

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
