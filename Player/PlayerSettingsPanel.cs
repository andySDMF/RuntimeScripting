using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class PlayerSettingsPanel : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField]
        private Slider walkSpeed;
        [SerializeField]
        private Slider runSpeed;
        [SerializeField]
        private Slider mouseSensitivity;
        [SerializeField]
        private Toggle highlightToggle;
        [SerializeField]
        private Toggle tooltipToggle;
        [SerializeField]
        private Toggle nameToggle;
        [SerializeField]
        private Toggle invertXToggle;
        [SerializeField]
        private Toggle invertYToggle;

        [Header("Options")]
        [SerializeField]
        private GameObject keysHeader;
        [SerializeField]
        private GameObject keysSettings;
        [SerializeField]
        private GameObject mobileControlHeader;
        [SerializeField]
        private GameObject mobileControlSettings;

        [Header("Mobile Only Components")]
        [SerializeField]
        private TMPro.TMP_Dropdown contollerDropdown;

        [Header("Mobile Layout")]
        [SerializeField]
        private RectTransform tabLayout;


        private float m_layoutWidth;
        private RectTransform m_mainLayout;

        private void Awake()
        {
            mouseSensitivity.minValue = 1;
            mouseSensitivity.maxValue = PlayerManager.Instance.MainControlSettings.mouse * 2;

            walkSpeed.minValue = 1;
            walkSpeed.maxValue = PlayerManager.Instance.MainControlSettings.walk * 2;

            runSpeed.minValue = 1;
            runSpeed.maxValue = PlayerManager.Instance.MainControlSettings.run * 2;

            m_mainLayout = transform.GetChild(0).GetComponent<RectTransform>();
            m_layoutWidth = m_mainLayout.sizeDelta.x;

            StartCoroutine(WaitFrame());
        }

        private IEnumerator WaitFrame()
        {
            yield return new WaitForEndOfFrame();

            Toggle[] togs = GetComponentsInChildren<Toggle>();

            for(int i = 0; i < togs.Length; i++)
            {
                if(togs[i].name.Contains("Player"))
                {
                    togs[i].isOn = true;
                    break;
                }
            }
        }

        private void OnEnable()
        {
            mouseSensitivity.value = PlayerManager.Instance.MainControlSettings.mouse;
            walkSpeed.value = PlayerManager.Instance.MainControlSettings.walk;
            runSpeed.value = PlayerManager.Instance.MainControlSettings.run;

            contollerDropdown.value = PlayerManager.Instance.MainControlSettings.controllerType;

            highlightToggle.isOn = PlayerManager.Instance.MainControlSettings.highlightOn > 0 ? true : false;
            tooltipToggle.isOn = PlayerManager.Instance.MainControlSettings.tooltipOn > 0 ? true : false;
            nameToggle.isOn = PlayerManager.Instance.MainControlSettings.nameOn > 0 ? true : false;

            invertXToggle.isOn = PlayerManager.Instance.MainControlSettings.invertX > 0 ? true : false;
            invertYToggle.isOn = PlayerManager.Instance.MainControlSettings.invertY > 0 ? true : false;

            if(AppManager.Instance.Data.IsMobile)
            {
                //need to hide the keybaord controls
                keysHeader.gameObject.SetActive(false);
                keysSettings.gameObject.SetActive(false);
            }
            else
            {
                //need to hid ethe mobile only
                mobileControlHeader.gameObject.SetActive(false);
                mobileControlSettings.gameObject.SetActive(false);
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
                    m_mainLayout.anchorMin = new Vector2(0.5f, 0.0f);
                    m_mainLayout.anchorMax = new Vector2(0.5f, 1.0f);
                    m_mainLayout.anchoredPosition = Vector2.zero;
                    m_mainLayout.sizeDelta = new Vector2(m_layoutWidth, m_mainLayout.sizeDelta.y);
                }
                else
                {
                    float aspect = arg2 / arg1;

                    m_mainLayout.anchorMin = new Vector2(0f, 0.0f);
                    m_mainLayout.anchorMax = new Vector2(1f, 1.0f);
                    m_mainLayout.offsetMax = new Vector2(-50, m_mainLayout.offsetMax.y);
                    m_mainLayout.offsetMin = new Vector2(50, m_mainLayout.offsetMin.y);

                    m_mainLayout.anchoredPosition = Vector2.zero;
                }
            }
        }

        public void Close()
        {
            PlayerManager.Instance.FreezePlayer(false);
        }

        public void Apply()
        {
            PlayerManager.Instance.MainControlSettings.mouse = mouseSensitivity.value;
            PlayerManager.Instance.MainControlSettings.walk = walkSpeed.value;
            PlayerManager.Instance.MainControlSettings.run = runSpeed.value;
            PlayerManager.Instance.MainControlSettings.controllerType = contollerDropdown.value;

            PlayerManager.Instance.MainControlSettings.highlightOn = highlightToggle.isOn ? 1 : 0;
            PlayerManager.Instance.MainControlSettings.tooltipOn = tooltipToggle.isOn ? 1 : 0;
            PlayerManager.Instance.MainControlSettings.nameOn = nameToggle.isOn ? 1 : 0;
            PlayerManager.Instance.MainControlSettings.invertX = invertXToggle.isOn ? 1 : 0;
            PlayerManager.Instance.MainControlSettings.invertY = invertYToggle.isOn ? 1 : 0;

            //need to set all the keys
            PlayerSettingsPanelKeyDropdown[] dropdowns = GetComponentsInChildren<PlayerSettingsPanelKeyDropdown>();

            for(int i = 0; i < dropdowns.Length; i++)
            {
                switch (dropdowns[i].ThisSettings)
                {
                    case PlayerSettingsPanelKeyDropdown.SettingName._Forward:
                        PlayerManager.Instance.MainControlSettings.controls[0] = dropdowns[i].Key;
                        AppManager.Instance.Data.fowardDirectionKey = (InputKeyCode)dropdowns[i].Key;
                        break;
                    case PlayerSettingsPanelKeyDropdown.SettingName._Back:
                        PlayerManager.Instance.MainControlSettings.controls[1] = dropdowns[i].Key;
                        AppManager.Instance.Data.backDirectionKey = (InputKeyCode)dropdowns[i].Key;
                        break;
                    case PlayerSettingsPanelKeyDropdown.SettingName._Left:
                        PlayerManager.Instance.MainControlSettings.controls[2] = dropdowns[i].Key;
                        AppManager.Instance.Data.leftDirectionKey = (InputKeyCode)dropdowns[i].Key;
                        break;
                    case PlayerSettingsPanelKeyDropdown.SettingName._Right:
                        PlayerManager.Instance.MainControlSettings.controls[3] = dropdowns[i].Key;
                        AppManager.Instance.Data.rightDirectionKey = (InputKeyCode)dropdowns[i].Key;
                        break;
                    case PlayerSettingsPanelKeyDropdown.SettingName._Sprint:
                        PlayerManager.Instance.MainControlSettings.controls[4] = dropdowns[i].Key;
                        AppManager.Instance.Data.sprintKey = (InputKeyCode)dropdowns[i].Key;
                        break;
                    case PlayerSettingsPanelKeyDropdown.SettingName._StrifeLeft:
                        PlayerManager.Instance.MainControlSettings.controls[5] = dropdowns[i].Key;
                        AppManager.Instance.Data.strifeLeftDirectionKey = (InputKeyCode)dropdowns[i].Key;
                        break;
                    case PlayerSettingsPanelKeyDropdown.SettingName._StrifeRight:
                        PlayerManager.Instance.MainControlSettings.controls[6] = dropdowns[i].Key;
                        AppManager.Instance.Data.strifeRightDirectionKey = (InputKeyCode)dropdowns[i].Key;
                        break;
                    case PlayerSettingsPanelKeyDropdown.SettingName._Focus:
                        PlayerManager.Instance.MainControlSettings.controls[7] = dropdowns[i].Key;
                        AppManager.Instance.Data.focusKey = (InputKeyCode)dropdowns[i].Key;
                        break;
                    case PlayerSettingsPanelKeyDropdown.SettingName._Interact:
                        PlayerManager.Instance.MainControlSettings.controls[8] = dropdowns[i].Key;
                        AppManager.Instance.Data.interactionKey = (InputKeyCode)dropdowns[i].Key;
                        break;
                }
            }



            PlayerManager.Instance.ApplySettings();

            string str = "walk-" + PlayerManager.Instance.MainControlSettings.walk + "|" + "run-" + PlayerManager.Instance.MainControlSettings.run
         + "|" + "strife-" + PlayerManager.Instance.MainControlSettings.strife + "|" + "mouse-" + mouseSensitivity.value + "|" + "controls-" + PlayerManager.Instance.MainControlSettings.GetControlKeys() +
         "|" + "invertX-" + (invertXToggle.isOn ? "1" : "0") + "|" + "invertY-" + (invertYToggle.isOn ? "1" : "0") + "|" + "controllerType-" + (contollerDropdown.value.Equals(0) ? "0" : "1") +
            "|" + "highlight-" + (highlightToggle.isOn ? "1" : "0") + "|" + "tooltip-" + (tooltipToggle.isOn ? "1" : "0") + "name-" + (nameToggle.isOn ? "1" : "0");


            if (AppManager.Instance.Settings.projectSettings.loginMode.Equals(LoginMode.Standard))
            {
                //send to indexedDB
                if (CoreManager.Instance.projectSettings.useIndexedDB)
                {
                    if (PlayerManager.Instance.IDBSettingsExists)
                    {
                        IndexedDbManager.Instance.UpdateEntry("playerControlsData", str);
                    }
                    else
                    {
                        IndexedDbManager.Instance.InsertEntry("playerControlsData", str);
                    }
                }
            }
            else
            {
                AppManager.Instance.Data.LoginProfileData.player_settings = str;
                AppManager.Instance.UpdateLoginsAPI();
                //string projectID = string.IsNullOrEmpty(AppManager.Instance.Settings.projectSettings.clientName) ? AppManager.Instance.Data.ProjectID : AppManager.Instance.Settings.projectSettings.clientName;
               // LoginsAPI.Instance.UpdateUser(AppManager.Instance.Data.NickName, projectID, JsonUtility.ToJson(AppManager.Instance.Data.LoginProfileData), AppManager.Instance.Data.LoginProfileData.password, AppManager.Instance.Data.RawFriendsData, AppManager.Instance.Data.RawGameData);
            }

            Close();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PlayerSettingsPanel), true)]
        public class PlayerSettingsPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("walkSpeed"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("runSpeed"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mouseSensitivity"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("highlightToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tooltipToggle"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("nameToggle"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("invertXToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("invertYToggle"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("keysHeader"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("keysSettings"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mobileControlHeader"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mobileControlSettings"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("contollerDropdown"), true);

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