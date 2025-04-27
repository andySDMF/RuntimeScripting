using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace BrandLab360
{
    public class AppAdministrator : MonoBehaviour
    {
        public TMP_InputField inputPassword;
        public TMP_Text errorLabelPassword;

        public TMP_InputField inputName;
        public TMP_Text errorLabelName;

        private AppLogin m_login;
        private bool m_cancelled = false;

        private void Awake()
        {
            m_login = FindFirstObjectByType<AppLogin>(FindObjectsInactive.Include);

            if (m_login == null)
            {
                Destroy(gameObject);
            }

            inputPassword.onSelect.AddListener(OnPasswordSelect);
            inputName.onSelect.AddListener(OnNameSelect);
        }

        private void OnEnable()
        {
            m_cancelled = false;
            inputName.text = AppManager.Instance.Settings.projectSettings.useMultipleAdminUsers ? "" : AppManager.Instance.Settings.projectSettings.adminUserName;

            if(!string.IsNullOrEmpty(inputName.text))
            {
                OnAdminName();
            }
            else
            {
                inputName.Select();
            }
        }

        private void OnDisable()
        {
            errorLabelName.text = "";
            errorLabelPassword.text = "";
        }

        public void OnAdminName()
        {
            if (string.IsNullOrEmpty(inputName.text))
            {
                errorLabelName.gameObject.SetActive(true);
                errorLabelName.text = "Please enter a name.";
                return;
            }
            else
            {
                errorLabelName.gameObject.SetActive(false);
                errorLabelName.text = "";
            }

            inputPassword.Select();
        }

        private void OnNameSelect(string str)
        {
            m_login.OnAdminName();
        }

        private void OnPasswordSelect(string str)
        {
            m_login.OnAdminPassword();
        }

        public void OnSubmit()
        {
            StartCoroutine(ProcessSubmit());
        }

        private IEnumerator ProcessSubmit()
        {
            yield return new WaitForSeconds(1.0f);

            if (m_cancelled) yield break;

            inputPassword.OnDeselect(null);
            inputName.OnDeselect(null);

            if (string.IsNullOrEmpty(inputName.text))
            {
                errorLabelName.gameObject.SetActive(true);
                errorLabelName.text = "Please enter a name.";
                yield break;
            }
            else
            {
                errorLabelName.gameObject.SetActive(false);
                errorLabelName.text = "";
            }

            if (string.IsNullOrEmpty(inputPassword.text))
            {
                errorLabelPassword.gameObject.SetActive(true);
                errorLabelPassword.text = "Please enter a password.";
                yield break;
            }
            else
            {
                errorLabelPassword.gameObject.SetActive(false);
                errorLabelPassword.text = "";
            }

            if(AppManager.Instance.Settings.projectSettings.useMultipleAdminUsers)
            {
                //check local admin users
                AdminUser adminUser = AppManager.Instance.Settings.projectSettings.adminUsers.FirstOrDefault(x => x.user.Equals(inputName.text));

                if(adminUser != null)
                {
                    if(adminUser.password.Equals(inputPassword.text))
                    {
                        AppManager.Instance.Data.AdminRole = adminUser;
                        m_login.OnSubmit(adminUser.user);
                    }
                }
            }
            else
            {
                //need to use the logins API to check username and password if correct then 
                LoginsAPI.Instance.LoginUser(inputName.text, inputPassword.text, AppManager.Instance.Data.ProjectID, OnLoginResponse);
            }
        }

        private void OnLoginResponse(LoginResponse response)
        {
             m_login.OnSubmit(response.username);
        }

        public void Cancel()
        {
            errorLabelName.text = "";
            errorLabelPassword.text = "";
            m_cancelled = false;
            m_login.OpenAdmin();
        }

        public void OnSkip()
        {
            m_login.OnSkip();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AppAdministrator), true)]
        public class AppAdministrator_Editor : BaseInspectorEditor
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

                    EditorGUILayout.LabelField("Name", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("inputName"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("errorLabelName"), true);

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Password", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("inputPassword"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("errorLabelPassword"), true);

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
