using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class PopupTag : UniqueID, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private PopupTagIOObject settings = new PopupTagIOObject();

        [SerializeField]
        private Image popUpIcon;

        private bool m_enableRaycastOnPointerExit = true;

        public InfotagType Type
        {
            get
            {
                return settings.popUpType;
            }

#if UNITY_EDITOR
            set
            {
                settings.popUpType = value;
            }
#endif
        }

#if UNITY_EDITOR
        public Image Icon
        {
            get
            {
                return popUpIcon;
            }
        }
#endif

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if (!AppManager.Instance.Instances.ignoreIObjectSettings)
            {
                //need to get the settings from the instances script then update the settings
                foreach (AppInstances.IOObjectPopupHandler setting in AppManager.Instance.Instances.ioPopupObjects)
                {
                    if (setting.referenceID.Equals(GetRawID()))
                    {
                        ApplySettings(setting.settings);
                        break;
                    }
                }
            }

            if (settings.overrideManagerIcon && settings.icon != null)
            {
                popUpIcon.sprite = settings.icon;
            }
            else
            {
                popUpIcon.sprite = InfotagManager.Instance.GetIcon(settings.popUpType);
            }

            bool hasPulse = GetComponentInChildren<Pulse>(true) != null;
            bool hasBounce = GetComponentInChildren<Bounce>(true) != null;
            bool hasGlow = GetComponentInChildren<Glow>(true) != null;

            if (settings.addPulse)
            {
                if (!hasPulse)
                {
                    Pulse pulse = GetComponentInChildren<Pulse>(true);
                    if (pulse == null)
                    {
                        gameObject.AddComponent<Pulse>();
                        hasPulse = true;
                    }
                }
            }
            else
            {
                if (hasPulse)
                {
                    Pulse pulse = GetComponentInChildren<Pulse>(true);
                    if (pulse != null)
                    {
                        Destroy(pulse);
                    }

                    hasPulse = false;
                }
            }

            if (settings.addBounce)
            {
                if (!hasBounce)
                {
                    Bounce bounce = GetComponentInChildren<Bounce>(true);
                    if (bounce == null)
                    {
                        gameObject.AddComponent<Bounce>();
                        hasBounce = true;
                    }
                }
            }
            else
            {
                if (hasBounce)
                {
                    Bounce bounce = GetComponentInChildren<Bounce>(true);
                    if (bounce != null)
                    {
                        Destroy(bounce);
                    }

                    hasBounce = false;
                }
            }

            if (settings.addGlow)
            {
                if (!hasGlow)
                {
                    Glow glow = GetComponentInChildren<Glow>(true);
                    if (glow == null)
                    {
                        gameObject.AddComponent<Glow>();
                        hasGlow = true;
                    }
                }
            }
            else
            {
                if (hasGlow)
                {
                    Glow glow = GetComponentInChildren<Glow>(true);
                    if (glow != null)
                    {
                        Destroy(glow);
                    }

                    hasGlow = false;
                }
            }
        }


        /// <summary>
        /// Callback when the infotag was clicked on
        /// </summary>
        public void OnClick()
        {
            if (!RaycastManager.Instance.UIRaycastOperation(gameObject, true)) return;

            if (settings.popUpType.Equals(InfotagType.Text))
            {
                PopupManager.instance.ShowPopUp(settings.textPopUpData.title, settings.textPopUpData.message,
                    settings.textPopUpData.button, settings.textPopUpData.icon, settings.textPopUpData.audio, settings.textPopUpData.url);
            }
            else
            {
                InfotagManager.Instance.ShowInfoTag(settings.popUpType, settings.webTag);
                RaycastManager.Instance.UIRaycastSelectablePressed(GetComponent<Selectable>());
            }

            AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Open, settings.webTag.title);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!RaycastManager.Instance.UIRaycastOperation(gameObject, true)) return;

            if (!RaycastManager.Instance.CastRay)
            {
                m_enableRaycastOnPointerExit = false;
            }

            Tooltip tTip = GetComponent<Tooltip>();

            if (tTip != null)
            {
                RaycastManager.Instance.CastRay = false;
                TooltipManager.Instance.ShowTooltip(gameObject);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //need to check if tooltip is active
            if (!TooltipManager.Instance.IsVisible) return;

            TooltipManager.Instance.HideTooltip();

            if (m_enableRaycastOnPointerExit)
            {
                RaycastManager.Instance.CastRay = true;
            }
        }

        [System.Serializable]
        public class PopupTagIOObject : IObjectSetting
        {
            public InfotagType popUpType = InfotagType.Web;
            public InfotagManager.InfoTagURL webTag;
            public bool overrideManagerIcon = false;
            public Sprite icon;
            public PopUpInformation textPopUpData = new PopUpInformation();

            public bool addPulse = false;
            public bool addBounce = false;
            public bool addGlow = false;
        }

        public override IObjectSetting GetSettings(bool remove = false)
        {
            if (!remove)
            {
                IObjectSetting baseSettings = base.GetSettings();
                settings.adminOnly = baseSettings.adminOnly;
                settings.prefix = baseSettings.prefix;
                settings.controlledByUserType = baseSettings.controlledByUserType;
                settings.userTypes = baseSettings.userTypes;

                settings.GO = gameObject.name;
            }

            settings.ID = id;
            return settings;
        }

        protected override void ApplySettings(IObjectSetting settings)
        {
            if (settings == null) return;

            base.ApplySettings(settings);

            this.settings.webTag = ((PopupTagIOObject)settings).webTag;
            this.settings.popUpType = ((PopupTagIOObject)settings).popUpType;

            this.settings.addPulse = ((PopupTagIOObject)settings).addPulse;
            this.settings.addBounce = ((PopupTagIOObject)settings).addBounce;
            this.settings.addGlow = ((PopupTagIOObject)settings).addGlow;
        }

        [System.Serializable]
        public class PopUpInformation
        {
            public string title;
            public string message;
            public string button;
            public AudioClip audio;
            public Sprite icon;
            public string url;
        }


#if UNITY_EDITOR
        [CustomEditor(typeof(PopupTag), true), CanEditMultipleObjects]
        public class PopupTag_Editor : UniqueID_Editor
        {
            private PopupTag popupScript;

            private bool hasPulse;
            private bool hasBounce;
            private bool hasGlow;

            private void OnEnable()
            {
                GetBanner();
                Initialise();

                hasPulse = script.gameObject.GetComponentInChildren<Pulse>(true) != null;
                hasBounce = script.gameObject.GetComponentInChildren<Bounce>(true) != null;
                hasGlow = script.gameObject.GetComponentInChildren<Glow>(true) != null;
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    m_instances.RemoveIOObject(popupScript.GetSettings(true));
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();
                DisplayID();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Popup Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("popUpType"), true);

                if(serializedObject.FindProperty("settings").FindPropertyRelative("popUpType").enumValueIndex == 4)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("textPopUpData"), true);
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("webTag"), true);
                }
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("overrideManagerIcon"), true);

                if(popupScript.settings.overrideManagerIcon)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("icon"), true);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Animaton", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("addPulse"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("addBounce"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("addGlow"), true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("popUpIcon"), true);

                if (GUI.changed || GONameChanged())
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(popupScript);

                    if (serializedObject.FindProperty("settings").FindPropertyRelative("addPulse").boolValue)
                    {
                        if (!hasPulse)
                        {
                            Pulse pulse = script.gameObject.GetComponentInChildren<Pulse>(true);
                            if (pulse == null)
                            {
                                script.gameObject.AddComponent<Pulse>();
                                hasPulse = true;
                            }
                        }
                    }
                    else
                    {
                        if (hasPulse)
                        {
                            Pulse pulse = script.gameObject.GetComponentInChildren<Pulse>(true);
                            if (pulse != null)
                            {
                                DestroyImmediate(pulse);
                            }

                            hasPulse = false;
                        }
                    }

                    if (serializedObject.FindProperty("settings").FindPropertyRelative("addBounce").boolValue)
                    {
                        if (!hasBounce)
                        {
                            Bounce bounce = script.gameObject.GetComponentInChildren<Bounce>(true);
                            if (bounce == null)
                            {
                                script.gameObject.AddComponent<Bounce>();
                                hasBounce = true;
                            }
                        }
                    }
                    else
                    {
                        if (hasBounce)
                        {
                            Bounce bounce = script.gameObject.GetComponentInChildren<Bounce>(true);
                            if (bounce != null)
                            {
                                DestroyImmediate(bounce);
                            }

                            hasBounce = false;
                        }
                    }

                    if (serializedObject.FindProperty("settings").FindPropertyRelative("addGlow").boolValue)
                    {
                        if (!hasGlow)
                        {
                            Glow glow = script.gameObject.GetComponentInChildren<Glow>(true);
                            if (glow == null)
                            {
                                script.gameObject.AddComponent<Glow>();
                                hasGlow = true;
                            }
                        }
                    }
                    else
                    {
                        if (hasGlow)
                        {
                            Glow glow = script.gameObject.GetComponentInChildren<Glow>(true);
                            if (glow != null)
                            {
                                DestroyImmediate(glow);
                            }

                            hasGlow = false;
                        }
                    }

                    if (Application.isPlaying) return;

                    if (m_instances != null)
                    {
                        m_instances.AddIOObject(popupScript.ID, popupScript.GetSettings());
                    }
                }
            }

            protected override void Initialise()
            {
                base.Initialise();

                popupScript = (PopupTag)target;

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    //need to get the settings from the instances script then update the settings
                    foreach (AppInstances.IOObjectPopupHandler setting in m_instances.ioPopupObjects)
                    {
                        if (setting.referenceID.Equals(popupScript.ID))
                        {
                            popupScript.ApplySettings(setting.settings);
                            break;
                        }
                    }

                    m_instances.AddIOObject(popupScript.ID, popupScript.GetSettings());
                }
            }
        }
#endif
    }
}
