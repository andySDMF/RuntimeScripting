using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class NoticeBoard : UniqueID, INotice, IBoard
    {
        [SerializeField]
        protected NoticeBoardIOObject settings;

        [SerializeField]
        protected Transform uploadButton;

        [SerializeField]
        protected GridLayoutGroup noticeLayout;

        [SerializeField]
        protected int layoutRows = 5;

        [SerializeField]
        protected int layoutColumns = 3;

        protected Lock m_lock;

        public GameObject GO { get { return gameObject; } }

        public string GetBoardID { get { return ID; } }

        public Transform ThisNoticeTransform { get { return transform; } }

        public NoticeType Type { get; }

        public ColorPicker.PickerDefaults PickerDefaults { get { return settings.pickerDefaults; } }

        public void Sync() {}

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if (!AppManager.Instance.Instances.ignoreIObjectSettings)
            {
                //need to get the settings from the instances script then update the settings
                foreach (AppInstances.IOObjectNoticeboardHandler setting in AppManager.Instance.Instances.ioNoticeboardObjects)
                {
                    if (setting.referenceID.Equals(GetRawID()))
                    {
                        ApplySettings(setting.settings);
                        break;
                    }
                }
            }

            if(uploadButton != null)
            {
                uploadButton.GetComponentInChildren<Button>().onClick.AddListener(OnClick);
            }

            m_lock = GetComponentInChildren<Lock>(true);

            GetComponent<Collider>().enabled = false;

            if (settings.lockUsed && settings.uploadEnabled && !AppManager.Instance.Data.IsMobile)
            {
                if (m_lock != null)
                {
                    m_lock.IsNetworked = false;
                    m_lock.IsLocked = true;
                    m_lock.OnLock += OnLocked;
                    m_lock.OnUnlock += OnUnlock;

                    OnLocked();

                    if(settings.adminOnly)
                    {
                        if(AppManager.Instance.Data.IsAdminUser)
                        {
                            m_lock.gameObject.SetActive(true);
                            m_lock.OverrideSettings(settings.lockSettings.useDataAPIPassword, settings.lockSettings.password, settings.lockSettings.displayType);
                        }
                        else
                        {
                            m_lock.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        m_lock.gameObject.SetActive(true);
                        m_lock.OverrideSettings(settings.lockSettings.useDataAPIPassword, settings.lockSettings.password, settings.lockSettings.displayType);
                    }
                }
            }
            else
            {
                if (m_lock != null)
                {
                    m_lock.gameObject.SetActive(false);
                }

                if(!settings.uploadEnabled || AppManager.Instance.Data.IsMobile)
                {
                    uploadButton.gameObject.SetActive(false);
                }
            }
        }

        public virtual void Remove(NoticeBoardAPI.NoticeJson json)
        {

        }

        public virtual void Create(NoticeBoardAPI.NoticeJson json)
        {
            //check if the notice already exists
            Notice[] all = GetComponentsInChildren<Notice>();

            for(int i = 0; i < all.Length; i++)
            {
                if(all[i].JsonID.Equals(json.id))
                {
                    all[i].OnEditCallback(json);
                    return;
                }
            }

            int limit = layoutRows * layoutColumns;

            if (noticeLayout.transform.childCount < limit)
            {
                UnityEngine.Object obj = Resources.Load<UnityEngine.Object>("Noticeboards/Canvas_Notice");

                if(obj != null)
                {
                    GameObject go = Instantiate(obj, Vector3.zero, Quaternion.identity, noticeLayout.transform) as GameObject;
                    go.transform.localScale = Vector3.one;
                    go.transform.localEulerAngles = Vector3.zero;
                    go.transform.localPosition = Vector3.zero;
                    go.GetComponentInChildren<Notice>(true).Json = json;
                    go.name = go.name + "_" + json.id;
                    go.SetActive(true);
                }
            }

            AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, "Created Notice " + AnalyticReference);
        }

        private void OnUnlock()
        {
            uploadButton.GetComponentInChildren<Button>().interactable = true;
        }

        private void OnLocked()
        {
            uploadButton.GetComponentInChildren<Button>().interactable = false;
        }

        public virtual void OnHover(bool isOver)
        {
            
        }

        public virtual void OnClick()
        {
            //check lock status
            if(m_lock != null)
            {
                if (m_lock.IsLocked) return;
            }

            int limit = layoutRows * layoutColumns;

            //cannot upload, reached max
            if (noticeLayout.transform.childCount >= limit)
            {
                return;
            }

            NoticeUploader nu = HUDManager.Instance.GetHUDScreenObject("NOTICE_SCREEN").GetComponentInChildren<NoticeUploader>(true);

            nu.PickerDefaults = settings.pickerDefaults;
            nu.NoticeBoard = ID;
            nu.EnableDisplayPeriod = settings.enableDisplayPeriod;
            HUDManager.Instance.ToggleHUDScreen("NOTICE_SCREEN");
        }

        public bool UserCanUse(string user)
        {
            if(m_lock != null && settings.lockUsed)
            {
                if(m_lock.IsLocked)
                {
                    return false;
                }
            }

            return CanUserControlThis(user);
        }


        [System.Serializable]
        public class NoticeBoardIOObject : IObjectSetting
        {
            public bool uploadEnabled = false;
            public NoticeType noticeType = NoticeType.Image;
            public bool lockUsed = false;
            public bool enableDisplayPeriod = false;
            public ColorPicker.PickerDefaults pickerDefaults = new ColorPicker.PickerDefaults();
            public LockManager.LockSetting lockSettings = new LockManager.LockSetting();
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

            this.settings.lockSettings = ((NoticeBoardIOObject)settings).lockSettings;
            this.settings.lockUsed = ((NoticeBoardIOObject)settings).lockUsed;
            this.settings.uploadEnabled = ((NoticeBoardIOObject)settings).uploadEnabled;
            this.settings.noticeType = ((NoticeBoardIOObject)settings).noticeType;
            this.settings.enableDisplayPeriod = ((NoticeBoardIOObject)settings).enableDisplayPeriod;
            this.settings.pickerDefaults = ((NoticeBoardIOObject)settings).pickerDefaults;
        }

#if UNITY_EDITOR

        [CustomEditor(typeof(NoticeBoard), true), CanEditMultipleObjects]
        public class NoticeBoard_Editor : UniqueID_Editor
        {
            private NoticeBoard noticeScript;

            private void OnEnable()
            {
                GetBanner();
                Initialise();
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    m_instances.RemoveIOObject(noticeScript.GetSettings(true));
                }
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                serializedObject.Update();

                DrawReportButton();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Noticeboard Setup", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("noticeType"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("enableDisplayPeriod"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("uploadEnabled"), true);
                EditorGUILayout.LabelField("If upload is disabled then lock will be hidden", EditorStyles.miniBoldLabel);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("pickerDefaults").FindPropertyRelative("type"), true);

                if(serializedObject.FindProperty("settings").FindPropertyRelative("pickerDefaults").FindPropertyRelative("type").enumValueIndex > 1)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("pickerDefaults").FindPropertyRelative("colors"), true);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Noticeboard Layout", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("uploadButton"), true);

                if(noticeScript is NoticePinBoard)
                {

                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("noticeLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("layoutRows"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("layoutColumns"), true);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Lock Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockUsed"), true);

                if(noticeScript.gameObject.scene.IsValid())
                {
                    if (noticeScript.settings.lockUsed)
                    {
                        Lock l = noticeScript.GetComponentInChildren<Lock>();

                        if (l == null)
                        {
                            if (GUILayout.Button("Create Lock"))
                            {
                                UnityEngine.Object prefab = (GameObject)CoreUtilities.GetAsset<GameObject>("Assets/com.brandlab360.core/Runtime/Prefabs/Lock.prefab");

                                if (prefab != null)
                                {
                                    GameObject lGO = Instantiate(prefab as GameObject, Vector3.zero, Quaternion.identity);
                                    lGO.name = "Lock";
                                    float bottom = 0 - (noticeScript.GetComponent<BoxCollider>().size.y / 2);
                                    lGO.transform.rotation = noticeScript.transform.rotation;
                                    lGO.transform.SetParent(noticeScript.transform);
                                    lGO.transform.localPosition = new Vector3(0, bottom, -0.02f);
                                }
                            }
                        }
                        else
                        {
                            float bottom = 0 - (noticeScript.GetComponent<BoxCollider>().size.y / 2);
                            l.transform.localPosition = new Vector3(0, bottom, -0.02f);
                            Resize(l.transform, 1);
                        }

                        EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("useDataAPIPassword"), true);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("password"), true);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("displayType"), true);
                    }
                    else
                    {
                        if (noticeScript.GetComponentInChildren<Lock>() != null)
                        {
                            DestroyImmediate(noticeScript.GetComponentInChildren<Lock>().gameObject);
                        }
                    }

                    if (noticeScript.uploadButton != null)
                    {
                        float bottom = 0 - (noticeScript.GetComponent<BoxCollider>().size.y / 2);
                        float right = 0 + (noticeScript.GetComponent<BoxCollider>().size.x / 2);

                        noticeScript.uploadButton.localPosition = new Vector3(right, bottom, -0.02f);

                        Resize(noticeScript.uploadButton, 1);
                    }

                    if (noticeScript.noticeLayout != null)
                    {
                        float cellX = (float)1 / noticeScript.layoutColumns;
                        float cellY = (float)1 / noticeScript.layoutRows;
                        noticeScript.noticeLayout.cellSize = new Vector2(cellX, cellY);

                        for (int i = 0; i < noticeScript.noticeLayout.transform.childCount; i++)
                        {
                            Vector2 delta = noticeScript.noticeLayout.transform.GetChild(i).GetComponent<RectTransform>().sizeDelta; ;
                            noticeScript.noticeLayout.transform.GetChild(i).GetComponent<BoxCollider>().size = new Vector3(delta.x, delta.y, 1);
                        }
                    }
                }

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(noticeScript);

                    if (m_instances != null)
                    {
                        m_instances.AddIOObject(noticeScript.ID, noticeScript.GetSettings());
                    }
                }
            }

            protected override void Initialise()
            {
                base.Initialise();

                noticeScript = (NoticeBoard)target;

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    //need to get the settings from the instances script then update the settings
                    foreach (AppInstances.IOObjectNoticeboardHandler setting in m_instances.ioNoticeboardObjects)
                    {
                        if (setting.referenceID.Equals(noticeScript.ID))
                        {
                            noticeScript.ApplySettings(setting.settings);
                            break;
                        }
                    }

                    m_instances.AddIOObject(noticeScript.ID, noticeScript.GetSettings());
                }
            }

            private void Resize(Transform t, float scale)
            {
                t.localScale = new Vector3(scale /  noticeScript.transform.localScale.x, scale / noticeScript.transform.localScale.y, scale / noticeScript.transform.localScale.z);
            }

            private void DrawReportButton()
            {
                if (!noticeScript.gameObject.scene.IsValid()) return;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);

                Report rButton = noticeScript.gameObject.GetComponentInChildren<Report>(true);
                bool containsReportButton = rButton != null;

                if (containsReportButton)
                {
                    Resize(rButton.transform, 1);

                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Select Report Button"))
                    {
                        Selection.activeTransform = rButton.transform;
                    }

                    if (GUILayout.Button("Remove Report Button"))
                    {
                        DestroyImmediate(rButton.gameObject);
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    if (GUILayout.Button("Add Report Button"))
                    {
                        UnityEngine.Object prefab = (GameObject)CoreUtilities.GetAsset<GameObject>("Assets/com.brandlab360.core/Runtime/Prefabs/Canvas_Report.prefab");

                        if (prefab != null)
                        {
                            GameObject go = Instantiate(prefab as GameObject, Vector3.zero, Quaternion.identity, noticeScript.transform);

                            //position button somwhere, bottom left is good
                            float bottom = 0 - (noticeScript.GetComponent<BoxCollider>().size.y / 2);
                            float left = 0 - (noticeScript.GetComponent<BoxCollider>().size.x / 2);

                            go.transform.localEulerAngles = Vector3.zero;
                            go.transform.localPosition = new Vector3(left, bottom, -0.02f);

                        }
                    }
                }
            }

        }
#endif
    }
}
