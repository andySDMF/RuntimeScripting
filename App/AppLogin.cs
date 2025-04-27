using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class AppLogin : MonoBehaviour
    {
        [Header("Screens")]
        public GameObject nameScreen;
        public GameObject passwordScreen;
        public GameObject adminScreen;
        public GameObject registrationScreen;

        [Header("Overlay")]
        public GameObject loadingOverlay;

        [Header("UI")]
        public TMP_InputField inputPassword;
        public TMP_Text errorLabelPassword;

        public TMP_InputField inputName;
        public TMP_Text errorLabelName;

        [Header("Logos")]
        public GameObject[] brandlabLogos;

        private bool m_hasBegun = false;
        private bool m_skipPassword = false;
        private bool m_skipName = false;

        private LoginProcess m_processStage = LoginProcess.Loading;

        public static AppLogin Instance;

        private AppAdministrator m_admin;

        private void Awake()
        {
            Instance = this;

            //ensure both are off by default
            nameScreen.SetActive(false);
            passwordScreen.SetActive(false);
            registrationScreen.SetActive(false);

            //loading screen
            loadingOverlay.SetActive(true);

            m_processStage = LoginProcess.Loading;
        }

        public void PushComponents(string id, TMP_InputField input, TMP_Text label, GameObject logo)
        {
            if (id.Equals("Password"))
            {
                inputPassword = input;
                errorLabelPassword = label;
                brandlabLogos[0] = logo;
            }
            else
            {
                inputName = input;
                errorLabelName = label;
                brandlabLogos[1] = logo;
            }
        }

        /// <summary>
        /// Called to begin the login process
        /// </summary>
        /// <param name="skipPassword"></param>
        /// <param name="skipName"></param>
        public void Begin(bool useAdmin, bool skipPassword, bool skipName)
        {
            //do not continue if true
            if (m_hasBegun)
            {
                return;
            }

            //instantiate password / name if the settings dictate a custom prefab
            if (AppManager.Instance.Settings.HUDSettings.customIntroPassword != null)
            {
                GameObject password = Instantiate(AppManager.Instance.Settings.HUDSettings.customIntroPassword, Vector3.zero, Quaternion.identity);
                password.transform.parent = passwordScreen.transform.parent;
                password.transform.SetSiblingIndex(passwordScreen.transform.GetSiblingIndex() + 1);
                password.transform.localScale = Vector3.one;
                password.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                password.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                passwordScreen = password;
            }

            if (AppManager.Instance.Settings.HUDSettings.customIntroName != null)
            {
                GameObject uName = Instantiate(AppManager.Instance.Settings.HUDSettings.customIntroName, Vector3.zero, Quaternion.identity);
                uName.transform.parent = nameScreen.transform.parent;
                uName.transform.SetSiblingIndex(nameScreen.transform.GetSiblingIndex() + 1);
                uName.transform.localScale = Vector3.one;
                uName.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                uName.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                nameScreen = uName;
            }

            if (AppManager.Instance.Settings.HUDSettings.customAdminPanel != null)
            {
                GameObject admin = Instantiate(AppManager.Instance.Settings.HUDSettings.customAdminPanel, Vector3.zero, Quaternion.identity);
                admin.transform.parent = adminScreen.transform.parent;
                admin.transform.SetSiblingIndex(adminScreen.transform.GetSiblingIndex() + 1);
                admin.transform.localScale = Vector3.one;
                admin.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                admin.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                adminScreen = admin;
            }

            if (AppManager.Instance.Settings.HUDSettings.customRegistrationPanel != null)
            {
                GameObject regist = Instantiate(AppManager.Instance.Settings.HUDSettings.customRegistrationPanel, Vector3.zero, Quaternion.identity);
                regist.transform.parent = registrationScreen.transform.parent;
                regist.transform.SetSiblingIndex(registrationScreen.transform.GetSiblingIndex() + 1);
                regist.transform.localScale = Vector3.one;
                regist.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                regist.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                registrationScreen = regist;
            }

            m_admin = adminScreen.GetComponentInChildren<AppAdministrator>(true);

            StartCoroutine(WaitFrame(useAdmin, skipName, skipPassword));
        }

        private IEnumerator WaitFrame(bool useAdmin, bool skipName, bool skipPassword)
        {
            yield return new WaitForEndOfFrame();

            m_hasBegun = true;

            for (int i = 0; i < brandlabLogos.Length; i++)
            {
                brandlabLogos[i].SetActive(AppManager.Instance.Settings.HUDSettings.showBrandlabLogo);
            }

            m_skipName = skipName;
            m_skipPassword = skipPassword;

            loadingOverlay.SetActive(false);

            inputName.text = AppManager.Instance.Data.NickName;

            PanelCheck();
        }

        private void Update()
        {
            if (m_hasBegun)
            {
                if (InputManager.Instance.GetKeyUp("Enter"))
                {
                    if (m_processStage == LoginProcess.AdminName)
                    {
                        m_admin.OnAdminName();
                        return;
                    }

                    if (m_processStage == LoginProcess.AdminPassword)
                    {
                        //  m_admin.OnAdminPassword();
                        return;
                    }

                    if (m_processStage == LoginProcess.Password)
                    {
                        OnClickPassword();
                        return;
                    }

                    if (m_processStage == LoginProcess.Name)
                    {
                        OnClickName();
                        return;
                    }
                }
            }
        }

        private void PanelCheck()
        {
            if (AppManager.Instance.Settings.projectSettings.loginMode.Equals(LoginMode.Registration))
            {
                BlackScreen.Instance.Show(false);
                registrationScreen.SetActive(true);
                return;
            }

            if (!m_skipPassword)
            {
                BlackScreen.Instance.Show(false);
                nameScreen.SetActive(false);
                passwordScreen.SetActive(true);
                registrationScreen.SetActive(false);
                inputPassword.Select();

                m_processStage = LoginProcess.Password;
            }
            else if (!m_skipName)
            {
                BlackScreen.Instance.Show(false);
                nameScreen.SetActive(true);
                passwordScreen.SetActive(false);
                registrationScreen.SetActive(false);

                if (string.IsNullOrEmpty(inputName.text))
                {
                    inputName.Select();
                }

                m_processStage = LoginProcess.Name;
            }
            else
            {
                nameScreen.SetActive(false);
                passwordScreen.SetActive(false);
                registrationScreen.SetActive(false);

                if (string.IsNullOrEmpty(AppManager.Instance.Data.NickName))
                {
                    AppManager.Instance.Data.NickName = "User";
                }

                AppManager.Instance.LoginComplete();
            }
        }

        public void SetLogoVisibility(bool isVisible)
        {
            for (int i = 0; i < brandlabLogos.Length; i++)
            {
                brandlabLogos[i].transform.localScale = isVisible ? Vector3.one : Vector3.zero;
            }
        }

        /// <summary>
        /// Submitted password
        /// </summary>
        public void OnClickPassword()
        {
            if (inputPassword.text == AppManager.Instance.Settings.projectSettings.Password)
            {
                passwordScreen.SetActive(false);

                Debug.Log("Password entered correctly: " + inputPassword.text);

                if (!m_skipName)
                {
                    nameScreen.SetActive(true);

                    if (string.IsNullOrEmpty(inputName.text))
                    {
                        inputName.Select();
                    }

                    m_processStage = LoginProcess.Name;
                }
                else
                {
                    if (AppManager.Instance.Settings.projectSettings.useIndexedDB)
                    {
                        inputName.text = AppManager.Instance.Data.NickName;
                    }
                    else
                    {
                        inputName.text = "User";
                    }

                    OnClickName();
                }
            }
            else
            {
                errorLabelPassword.gameObject.SetActive(true);
                errorLabelPassword.text = "Incorrect password.";
            }
        }

        /// <summary>
        /// Submitted name
        /// </summary>
        public void OnClickName()
        {
            if (inputName.text != "")
            {
                AppManager.Instance.Data.NickName = inputName.text;
                nameScreen.SetActive(false);

                AppManager.Instance.LoginComplete();
            }
            else
            {
                errorLabelName.gameObject.SetActive(true);
                errorLabelName.text = "Please enter a name.";
            }
        }

        /// <summary>
        /// Called to set the loading overlay active
        /// </summary>
        /// <param name="show"></param>
        public void ShowLoadingOverlay(bool show)
        {
            loadingOverlay.SetActive(show);
        }

        #region Admin Panel
        public void OpenAdmin()
        {
            adminScreen.SetActive(!adminScreen.activeInHierarchy);

            if (!adminScreen.activeInHierarchy)
            {
                m_processStage = LoginProcess.Name;
                inputName.Select();
            }
        }

        public void OnAdminName()
        {
            m_processStage = LoginProcess.AdminName;
        }

        public void OnAdminPassword()
        {
            m_processStage = LoginProcess.AdminPassword;
        }

        public void OnSubmit(string username)
        {
            nameScreen.SetActive(false);
            adminScreen.SetActive(false);
            registrationScreen.SetActive(false);
            AppManager.Instance.Data.IsAdminUser = true;
            AppManager.Instance.Data.NickName = username;
            AppManager.Instance.Data.LoginProfileData = new ProfileData();
            AppManager.Instance.LoginComplete();
        }

        public void OnSkip()
        {
            //need to move onto standard login
            adminScreen.SetActive(false);
            AppManager.Instance.Data.IsAdminUser = false;
            PanelCheck();
        }
        #endregion

        private enum LoginProcess { Loading, AdminName, AdminPassword, Password, Name }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(AppLogin), true)]
    public class AppLogin_Editor : BaseInspectorEditor
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

                EditorGUILayout.PropertyField(serializedObject.FindProperty("nameScreen"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("passwordScreen"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("adminScreen"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("registrationScreen"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("loadingOverlay"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("inputPassword"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("errorLabelPassword"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("inputName"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("errorLabelName"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("brandlabLogos"), true);

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
