using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Collider))]
    public class Lock : UniqueID, IDataAPICallback
    {
        [SerializeField]
        private LockIOObject settings = new LockIOObject();

        [SerializeField]
        private Image displayImage;

        [SerializeField]
        private GameObject lockPadlock;

        [SerializeField]
        private GameObject unlockedPadlock;

        private bool m_ignoreRaycast = false;

        /// <summary>
        /// Global acccess to the locked state
        /// </summary>
        public bool IsLocked
        {
            get
            {
                return settings.isLocked;
            }
            set
            {
                settings.isLocked = value;

                //apply lock action
                if(settings.isLocked)
                {
                    LockThis();
                }
                else
                {
                    UnlockThis();
                }
            }
        }

        /// <summary>
        /// Does this lock ignore any raycasts
        /// </summary>
        public bool IgnoreRaycast
        {
            get 
            {
                return m_ignoreRaycast;
            }
            set
            {
                m_ignoreRaycast = value;
            }
        }

        public override bool HasParent
        {
            get
            {
                return GetComponentInParent<ChairGroup>() != null || GetComponentInParent<WorldContentUpload>() != null 
                    || GetComponentInParent<ProductPlacement>() != null || GetComponentInParent<SwitchSceneTrigger>() != null ||
                    GetComponentInParent<IChairObject>() != null || GetComponentInParent<NoticeBoard>() != null;
            }
        }

        /// <summary>
        /// Action to subscribe to when lock is unlocked
        /// </summary>
        public System.Action OnUnlock { get; set; }
        /// <summary>
        /// Action to subscribe to when lock is locked
        /// </summary>
        public System.Action OnLock { get; set; }
        /// <summary>
        /// Action to subscribe to when lock is canceled
        /// </summary>
        public System.Action OnCancel { get; set; }
        /// <summary>
        /// Action to subscribe to when lock password is changed
        /// </summary>
        public System.Action<string> OnPasswordChange { get; set; }

        /// <summary>
        /// States if the lock is globally networked in the room
        /// </summary>
        public bool IsNetworked
        {
            get
            {
                return settings.isNetworked;
            }
            set
            {
                settings.isNetworked = value;
            }
        }

        /// <summary>
        /// Defines who clicked on the lock
        /// </summary>
        public bool LocalPlayerClicked
        {
            get;
            set;
        }

        /// <summary>
        /// Global access to the locks password
        /// </summary>
        public string Password
        {
            get
            {
                return settings.password;
            }
            set
            {
                settings.password = value;

                //call action on change
                if(OnPasswordChange != null)
                {
                    OnPasswordChange.Invoke(settings.password);
                }
            }
        }


        public void OverrideSettings(bool useDataAPI, string password, LockDisplayType display)
        {
            settings.displayType = display;
            settings.password = password;
            settings.useDataAPIPassword = useDataAPI;

            if (settings.displayType.Equals(LockDisplayType.UIButton))
            {
                if (lockPadlock != null) lockPadlock.SetActive(false);
                if (unlockedPadlock != null) unlockedPadlock.SetActive(false);
                displayImage.enabled = true;
                displayImage.transform.parent.GetComponent<Image>().enabled = true;
            }
            else
            {
                if (lockPadlock != null) lockPadlock.SetActive(settings.isLocked);
                if (unlockedPadlock != null) unlockedPadlock.SetActive(!settings.isLocked);
                displayImage.enabled = false;
                displayImage.transform.parent.GetComponent<Image>().enabled = false;
            }
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if (!AppManager.Instance.Instances.ignoreIObjectSettings)
            {
                //need to get the settings from the instances script then update the settings
                foreach (AppInstances.IOObjectLockHandler setting in AppManager.Instance.Instances.ioLockObjects)
                {
                    if (setting.referenceID.Equals(GetRawID()))
                    {
                        ApplySettings(setting.settings);
                        break;
                    }
                }
            }

            //set lock image
            displayImage.sprite = (IsLocked) ? LockManager.Instance.lockedSprite : LockManager.Instance.unlockedSprite;

            if (settings.displayType.Equals(LockDisplayType.UIButton))
            {
                if (lockPadlock != null) lockPadlock.SetActive(false);
                if (unlockedPadlock != null) unlockedPadlock.SetActive(false);
                displayImage.enabled = true;
                displayImage.transform.parent.GetComponent<Image>().enabled = true;
            }
            else
            {
                if (lockPadlock != null) lockPadlock.SetActive(settings.isLocked);
                if (unlockedPadlock != null) unlockedPadlock.SetActive(!settings.isLocked);
                displayImage.enabled = false;
                displayImage.transform.parent.GetComponent<Image>().enabled = false;
            }

            //ensure the subscribed action are called to other scripts
            if (settings.isLocked)
            {
                if (OnLock != null)
                {
                    OnLock.Invoke();
                }
            }
            else
            {
                if (OnUnlock != null)
                {
                    OnUnlock.Invoke();
                }
            }

            settings.useDataAPIPassword = !CoreManager.Instance.projectSettings.useDataAPI ? false : settings.useDataAPIPassword;
        }

        public void DataAPICallback(List<DataObject> objs)
        {
            if (!settings.useDataAPIPassword) return;

            for(int i = 0; i < objs.Count; i++)
            {
                if(objs[i].uniqueId.Equals(ID))
                {
                    settings.password = objs[i].data;
                    return;
                }
            }
        }

        /// <summary>
        /// Locks this, define if the action is networked
        /// </summary>
        /// <param name="network"></param>
        public void LockThis(bool network = false)
        {
            settings.isLocked = true;

            if (settings.displayType.Equals(LockDisplayType.Padlock))
            {
                if (lockPadlock != null) lockPadlock.SetActive(settings.isLocked);
                if (unlockedPadlock != null) unlockedPadlock.SetActive(!settings.isLocked);
                displayImage.enabled = false;
                displayImage.transform.parent.GetComponent<Image>().enabled = false;
            }

            if (AppManager.IsCreated)
            {
                displayImage.sprite = (Sprite)AppManager.Instance.Assets.Get("lockedSprite");
            }

            //only network if both true
            if (network && IsNetworked)
            {
                //need to network  this lock
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("EVENT_TYPE", "LOCK");
                data.Add("I", ID);
                data.Add("L", "1");

                MMOManager.Instance.ChangeRoomProperty(ID, data);
            }

            AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, "Locked " + AnalyticReference);

            //call action
            if (OnLock != null)
            {
                OnLock.Invoke();
            }
        }

        /// <summary>
        /// Unlocks this, define if the action is networked
        /// </summary>
        /// <param name="network"></param>
        public void UnlockThis(bool network = false)
        {
            settings.isLocked = false;

            if (settings.displayType.Equals(LockDisplayType.Padlock))
            {
                if (lockPadlock != null) lockPadlock.SetActive(settings.isLocked);
                if (unlockedPadlock != null) unlockedPadlock.SetActive(!settings.isLocked);
                displayImage.enabled = false;
                displayImage.transform.parent.GetComponent<Image>().enabled = false;
            }

            if (AppManager.IsCreated)
            {
                displayImage.sprite = (Sprite)AppManager.Instance.Assets.Get("unlockedSprite");
            }

            //only network if both true
            if (network && IsNetworked)
            {
                //need to network  this lock
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("EVENT_TYPE", "LOCK");
                data.Add("I", ID);
                data.Add("L", "0");

                MMOManager.Instance.ChangeRoomProperty(ID, data);
            }

            AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, "Unlocked " + AnalyticReference);

            //call action
            if (OnUnlock != null)
            {
                OnUnlock.Invoke();
            }
        }

        /// <summary>
        /// Push this password to the DataAPI
        /// </summary>
        public void PushToDataAPI()
        {
            if (!settings.useDataAPIPassword) return;

            DataManager.Instance.InsertDataObject(ID, settings.password, "password");
        }

        public enum LockDisplayType { UIButton, Padlock }

        [System.Serializable]
        public class LockIOObject : IObjectSetting
        {
            public bool useDataAPIPassword = false;
            public bool isLocked = true;
            public LockDisplayType displayType = LockDisplayType.UIButton;
            public string password = "";
            public bool isNetworked = true;
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

            this.settings.useDataAPIPassword = ((LockIOObject)settings).useDataAPIPassword;
            this.settings.isLocked = ((LockIOObject)settings).isLocked;
            this.settings.displayType = ((LockIOObject)settings).displayType;
            this.settings.password = ((LockIOObject)settings).password;
            this.settings.isNetworked = ((LockIOObject)settings).isNetworked;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Lock), true), CanEditMultipleObjects]
        public class Lock_Editor : UniqueID_Editor
        {
            private Lock lockScript;
            private bool isContentLock;
            private bool isChairGroupLock;
            private bool isChairLock;
            private bool isProductPlacement;
            private bool isSwitchSceneTrigger;
            private bool isNoticeboardLock;

            private void OnEnable()
            {
                GetBanner();
                Initialise();

                isChairGroupLock = lockScript.gameObject.GetComponentInParent<ChairGroup>() != null;
                isContentLock = lockScript.gameObject.GetComponentInParent<WorldContentUpload>() != null;
                isChairLock = lockScript.gameObject.GetComponentInParent<IChairObject>() != null;
                isProductPlacement = lockScript.gameObject.GetComponentInParent<ProductPlacement>() != null;
                isSwitchSceneTrigger = lockScript.gameObject.GetComponentInParent<SwitchSceneTrigger>() != null;
                isNoticeboardLock = lockScript.gameObject.GetComponentInParent<NoticeBoard>() != null;

                if (!isChairGroupLock && !isContentLock && !isChairLock && !isProductPlacement && !isSwitchSceneTrigger && !isNoticeboardLock)
                {
                    base.Initialise();

                    if (Application.isPlaying) return;

                    if (m_instances != null)
                    {
                        //need to get the settings from the instances script then update the settings
                        foreach (AppInstances.IOObjectLockHandler setting in m_instances.ioLockObjects)
                        {
                            if (setting.referenceID.Equals(lockScript.ID))
                            {
                                lockScript.ApplySettings(setting.settings);
                                break;
                            }
                        }

                        m_instances.AddIOObject(lockScript.ID, lockScript.GetSettings());
                    }
                }
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (m_instances != null && !isChairGroupLock && !isContentLock && !isChairLock && !isProductPlacement && !isSwitchSceneTrigger && !isNoticeboardLock)
                {
                    m_instances.RemoveIOObject(lockScript.GetSettings(true));
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                if (!isChairGroupLock && !isContentLock && !isChairLock && !isProductPlacement && !isSwitchSceneTrigger && !isNoticeboardLock)
                {
                    DisplayID();

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Lock Setup", EditorStyles.boldLabel);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("useDataAPIPassword"), true);

                    if (!isChairGroupLock && !isContentLock && !isChairLock && !isProductPlacement)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("isLocked"), true);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("isNetworked"), true);
                    }

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("password"), true);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("displayType"), true);

                if (serializedObject.FindProperty("settings").FindPropertyRelative("displayType").enumValueIndex == 0)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("displayImage"), true);
                    ((Image)serializedObject.FindProperty("displayImage").objectReferenceValue).enabled = true;
                    ((Image)serializedObject.FindProperty("displayImage").objectReferenceValue).transform.parent.GetComponent<Image>().enabled = true;

                    if (serializedObject.FindProperty("unlockedPadlock").objectReferenceValue != null)
                        ((GameObject)serializedObject.FindProperty("unlockedPadlock").objectReferenceValue).SetActive(false);

                    if (serializedObject.FindProperty("lockPadlock").objectReferenceValue != null)
                        ((GameObject)serializedObject.FindProperty("lockPadlock").objectReferenceValue).SetActive(false);
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lockPadlock"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("unlockedPadlock"), true);

                    ((Image)serializedObject.FindProperty("displayImage").objectReferenceValue).enabled = false;
                    ((Image)serializedObject.FindProperty("displayImage").objectReferenceValue).transform.parent.GetComponent<Image>().enabled = false;

                    if (serializedObject.FindProperty("unlockedPadlock").objectReferenceValue != null)
                        ((GameObject)serializedObject.FindProperty("unlockedPadlock").objectReferenceValue).SetActive(!serializedObject.FindProperty("settings").FindPropertyRelative("isLocked").boolValue);

                    if (serializedObject.FindProperty("lockPadlock").objectReferenceValue != null)
                        ((GameObject)serializedObject.FindProperty("lockPadlock").objectReferenceValue).SetActive(serializedObject.FindProperty("settings").FindPropertyRelative("isLocked").boolValue);
                }

                if (GUI.changed || GONameChanged())
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(lockScript);

                    if (Application.isPlaying) return;

                    if (m_instances != null && !isChairGroupLock && !isContentLock && !isChairLock && !isProductPlacement && !isSwitchSceneTrigger && !isNoticeboardLock)
                    {
                        m_instances.AddIOObject(lockScript.ID, lockScript.GetSettings());
                    }
                }
            }

            protected override void Initialise()
            {
                lockScript = (Lock)target;
            }
        }
#endif
    }
}
