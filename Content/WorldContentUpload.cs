using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class WorldContentUpload : UniqueID
    {
        [SerializeField]
        private ContentsManager.ContentType type = ContentsManager.ContentType.All;

        [SerializeField]
        private Transform uploadButton;

        [SerializeField]
        private Lock lockUsed;

        [Header("For type ALL, Loaders must be in order of Video|Image|PDF")]
        [SerializeField]
        private GameObject[] contentLoaders;

        [SerializeField]
        private UnityEvent onLoad;

        [SerializeField]
        private WorldUploadIOObject settings = new WorldUploadIOObject();

        private ContentsManager.ContentFileInfo m_info;
        private bool m_permissionGranted = false;
        private float m_closePermissionTimer = 0.0f;
        private bool m_TimerOn = false;
        private bool m_canInteract = true;
        private Vector3 m_uploadCacheScale;

        /// <summary>
        /// Access to set/get the interactive state of this upload script
        /// </summary>
        public bool Interactive
        {
            get;
            set;
        }

        public string URL
        {
            get
            {
                if(m_info == null)
                {
                    return "";
                }

                return m_info.url;
            }
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            m_uploadCacheScale = new Vector3(uploadButton.transform.localScale.x, uploadButton.transform.localScale.y, uploadButton.transform.localScale.z);

            if (!AppManager.Instance.Instances.ignoreIObjectSettings)
            {
                //need to get the settings from the instances script then update the settings
                foreach (AppInstances.IOObjectWorldUploadHandler setting in AppManager.Instance.Instances.ioWorldUploadObjects)
                {
                    if (setting.referenceID.Equals(GetRawID()))
                    {
                        ApplySettings(setting.settings);
                        break;
                    }
                }
            }

            Interactive = true;

            for (int i = 0; i < contentLoaders.Length; i++)
            {
                contentLoaders[i].GetComponentInChildren<IContentLoader>(true).ID = ID;
                contentLoaders[i].transform.localScale = Vector3.zero;
                contentLoaders[i].GetComponentInChildren<IContentLoader>(true).LockUsed = lockUsed;
            }

            if (lockUsed != null)
            {
                if (AppManager.Instance.Settings.projectSettings.overrideWorldUploadPassword)
                {
                    lockUsed.Password = AppManager.Instance.Settings.projectSettings.worldContentPassword;
                }

                lockUsed.ID = ID + "[LOCK]";
                lockUsed.IsNetworked = false;
                lockUsed.IgnoreRaycast = true;
                lockUsed.OverrideSettings(settings.lockSettings.useDataAPIPassword, settings.lockSettings.password, settings.lockSettings.displayType);

                if (settings.permissionRequired)
                {
                    lockUsed.LockThis();
                    lockUsed.OnUnlock += PermissionGranted;
                    lockUsed.OnLock += ResetPermission;
                    StartCoroutine(WaitForFrame());
                }
            }

            if (CoreManager.Instance.IsOffline)
            {
                lockUsed.transform.localScale = Vector3.zero;
                uploadButton.transform.localScale = Vector3.zero;
            }
        }

        private void Update()
        {
            if (!RaycastManager.Instance.UIRaycastOperation(uploadButton.gameObject))
            {
                if (m_canInteract)
                {
                    Selectable[] all = GetComponentsInChildren<Selectable>(true);

                    for (int i = 0; i < all.Length; i++)
                    {
                        if (all[i].GetComponentInParent<Report>() != null) continue;

                        if(all[i].GetComponentInParent<Lock>() == null)
                        {
                            all[i].interactable = false;
                        }
                    }
                }

                m_canInteract = false;
            }
            else
            {
                if (!m_canInteract)
                {
                    Selectable[] all = GetComponentsInChildren<Selectable>(true);

                    for (int i = 0; i < all.Length; i++)
                    {
                        if (all[i].GetComponentInParent<Report>() != null) continue;

                        all[i].interactable = true;
                    }
                }

                m_canInteract = true;
            }

            //if true then close lock after 5 seconds if not loaded file
            if (m_TimerOn)
            {
                if (m_closePermissionTimer < 5.0f)
                {
                    m_closePermissionTimer += Time.deltaTime;
                }
                else
                {
                    m_TimerOn = false;
                    ResetPermission();
                }
            }
        }

        /// <summary>
        /// Called via the locks 'Lock' subscription to reset the permision state of this upload object
        /// </summary>
        private void ResetPermission()
        {
            if (!m_permissionGranted) return;

            m_TimerOn = false;
            m_closePermissionTimer = 5.0f;

            StartCoroutine(WaitForFrame());
        }

        /// <summary>
        /// Called via the locks 'Unlock' subscription to grant the permision state of this upload object
        /// </summary>
        private void PermissionGranted()
        {
            if (m_permissionGranted) return;

            Debug.Log("WorldContentUpload: Permission granted for upload: " + ID);

            m_permissionGranted = true;
            m_closePermissionTimer = 0.0f;
            m_TimerOn = true;

            for (int i = 0; i < contentLoaders.Length; i++)
            {
                contentLoaders[i].transform.localScale = Vector3.zero;
            }
        }

        /// <summary>
        /// Delay the lock state if permision is reset
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitForFrame()
        {
            yield return new WaitForSeconds(0.5f);

            lockUsed.LockThis();
            lockUsed.IgnoreRaycast = false;

            for (int i = 0; i < contentLoaders.Length; i++)
            {
                if(!contentLoaders[i].GetComponent<IContentLoader>().IsLoaded)
                {
                    contentLoaders[i].transform.localScale = Vector3.zero;
                }
            }

            m_permissionGranted = false;

            Debug.Log("WorldContentUpload: Permission granted reset: " + ID);
        }

        /// <summary>
        /// Action called when the button is clicked
        /// </summary>
        public void OnClick()
        {
            if (!Interactive) return;

            //check permision is set if used
            if (settings.permissionRequired && !m_permissionGranted)
            {
                return;
            }

            if(CoreManager.Instance.IsOffline)
            {
                OfflineManager.Instance.ShowOfflineMessage();
                return;
            }

            //timer off
            m_TimerOn = false;
            m_closePermissionTimer = 0.0f;

            //subscribe to mamnagers file actions
            ContentsManager.Instance.OnFileUpload += UploadCallback;
            ContentsManager.Instance.OnFileDelete += DeleteCallback;

            Debug.Log("WorldContentUpload: OnClick Upload Content: " + ID);

            //request upload to web client
            ContentsManager.Instance.WebClientUploadWorldContent(ID, type.ToString());
        }

        /// <summary>
        /// upload callback when manager get responce
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fileInfo"></param>
        private void UploadCallback(string id, ContentsManager.ContentFileInfo fileInfo)
        {
            if (!ID.Equals(id)) return;

            ContentsManager.Instance.OnFileUpload -= UploadCallback;
            m_info = fileInfo;

            Debug.Log("WorldContentUpload: Upload content callback:" + ID);

            if(uploadButton)
            {
                uploadButton.transform.localScale = Vector3.zero;
            }

            switch (type)
            {
                case ContentsManager.ContentType.All:

                    switch(m_info.extensiontype)
                    {
                        case 1:
                            contentLoaders[0].GetComponentInChildren<IContentLoader>(true).Load(fileInfo);
                            contentLoaders[0].transform.localScale = Vector3.one;
                            break;
                        case 2:
                            contentLoaders[1].GetComponentInChildren<IContentLoader>(true).Load(fileInfo);
                            contentLoaders[1].transform.localScale = Vector3.one;
                            break;
                        default:
                            break;
                    }


                    break;
                default:
                    for (int i = 0; i < contentLoaders.Length; i++)
                    {
                        contentLoaders[i].GetComponentInChildren<IContentLoader>(true).Load(fileInfo);
                        contentLoaders[i].transform.localScale = Vector3.one;
                    }
                    break;
            }

            //need to send Room change on this IContentLoader to load file across network
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("EVENT_TYPE", "WORLDSCREENCONTENT");
            data.Add("A", "1");
            data.Add("I", ID);
            //  data.Add("F", JsonUtility.ToJson(fileInfo));
            data.Add("F", fileInfo.id.ToString());
            data.Add("C", "");
            data.Add("O", PlayerManager.Instance.GetLocalPlayer().ActorNumber.ToString());

            MMOManager.Instance.ChangeRoomProperty(ID, data);

            AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, "Content Upload " + AnalyticReference);

            //not interactive anymore as image loaded
            Interactive = false;
            onLoad.Invoke();
        }

        /// <summary>
        /// delete callback when file is deleted via the manager
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fileInfo"></param>
        private void DeleteCallback(string id, ContentsManager.ContentFileInfo fileInfo)
        {
            if (!ID.Equals(id)) return;

            Debug.Log("WorldContentUpload: Delete content callback:" + ID);

            if (uploadButton)
            {
                uploadButton.transform.localScale = m_uploadCacheScale;
            }

            //remove this callback so it does not get called again
            ContentsManager.Instance.OnFileDelete -= DeleteCallback;

            if (PlayerManager.Instance.GetLocalPlayer().ID != null)
            {
                //need to send Room change on this IContentLoader to delete file across network
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("EVENT_TYPE", "WORLDSCREENCONTENT");
                data.Add("A", "0");
                data.Add("I", ID);
                // data.Add("F", JsonUtility.ToJson(fileInfo));
                data.Add("F", fileInfo.id.ToString());
                data.Add("C", "");
                data.Add("O", PlayerManager.Instance.GetLocalPlayer().ActorNumber.ToString());

                MMOManager.Instance.ChangeRoomProperty(ID, data);
            }

            for (int i = 0; i < contentLoaders.Length; i++)
            {
                contentLoaders[i].transform.localScale = Vector3.zero;
            }

            AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, "Content Delete " + AnalyticReference);

            //set interaction as on
            m_info = null;
            Interactive = true;
        }

        /// <summary>
        /// Called to Network this upload state 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        public void OnNetworkedUpload(string id, string data)
        {
            if (!ID.Equals(id)) return;

            Debug.Log("WorldContentUpload: Network content upload:" + ID);

            ContentsManager.ContentFileInfo temp = JsonUtility.FromJson<ContentsManager.ContentFileInfo>(data);
            ContentsManager.ContentFileInfo fileInfo = ContentsManager.Instance.GetFileInfo(temp.id);
            ContentsManager.Instance.OnFileDelete += DeleteCallback;

            if (uploadButton)
            {
                uploadButton.transform.localScale = Vector3.zero;
            }

            if (fileInfo != null)
            {
                switch (type)
                {
                    case ContentsManager.ContentType.All:

                        switch (fileInfo.extensiontype)
                        {
                            case 1:
                                contentLoaders[0].GetComponentInChildren<IContentLoader>(true).Load(fileInfo);
                                contentLoaders[0].transform.localScale = Vector3.one;
                                break;
                            case 2:
                                contentLoaders[1].GetComponentInChildren<IContentLoader>(true).Load(fileInfo);
                                contentLoaders[1].transform.localScale = Vector3.one;
                                break;
                            default:
                                break;
                        }

                        break;
                    default:
                        for (int i = 0; i < contentLoaders.Length; i++)
                        {
                            contentLoaders[i].GetComponentInChildren<IContentLoader>(true).Load(fileInfo);
                            contentLoaders[i].transform.localScale = Vector3.one;
                        }
                        break;
                }
            }

            //not interactive anymore
            Interactive = false;
            onLoad.Invoke();
        }

        /// <summary>
        /// Called to network this delete state
        /// </summary>
        /// <param name="id"></param>
        public void OnNetworkedDelete(string id)
        {
            if (!ID.Equals(id)) return;

            Debug.Log("WorldContentUpload: Network delete content callback:" + ID);

            ContentsManager.Instance.OnFileDelete -= DeleteCallback;

            if (uploadButton)
            {
                uploadButton.transform.localScale = m_uploadCacheScale;
            }

            //find icontentloader and unload
            for (int i = 0; i < contentLoaders.Length; i++)
            {
                contentLoaders[i].GetComponentInChildren<IContentLoader>(true).Unload();
                contentLoaders[i].transform.localScale = Vector3.zero;
            }

            //is interactive now
            m_info = null;
            Interactive = true;
        }

        [System.Serializable]
        public class WorldUploadIOObject : IObjectSetting
        {
            public bool permissionRequired = false;


            public bool lerpAlpha = false;
            public bool autoPlay = true;
            public bool loopVideo = false;
            public bool showControls = false;
            public bool showScrubber = false;

            public float minZoom = 1.0f;
            public float maxZoom =  3.0f;
            public float zoomSpeed = 3.0f;
            public float fractionToZoomIn = 0.2f;

            public LockManager.LockSetting lockSettings = new LockManager.LockSetting();

            public ContentsManager.ContentType Type { get; set; }
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

            settings.Type = type;
            settings.ID = id;
            return settings;
        }

        protected override void ApplySettings(IObjectSetting settings)
        {
            if (settings == null) return;

            base.ApplySettings(settings);

            this.settings.permissionRequired = ((WorldUploadIOObject)settings).permissionRequired;
            this.settings.lerpAlpha = ((WorldUploadIOObject)settings).lerpAlpha;

            this.settings.autoPlay = ((WorldUploadIOObject)settings).autoPlay;
            this.settings.loopVideo = ((WorldUploadIOObject)settings).loopVideo;
            this.settings.showControls = ((WorldUploadIOObject)settings).showControls;
            this.settings.showScrubber = ((WorldUploadIOObject)settings).showScrubber;

            this.settings.minZoom = ((WorldUploadIOObject)settings).minZoom;
            this.settings.maxZoom = ((WorldUploadIOObject)settings).maxZoom;
            this.settings.zoomSpeed = ((WorldUploadIOObject)settings).zoomSpeed;
            this.settings.fractionToZoomIn = ((WorldUploadIOObject)settings).fractionToZoomIn;
            this.settings.lockSettings = ((WorldUploadIOObject)settings).lockSettings;

            //get the contentloader and apply the settings to this
            switch (type)
            {
                case ContentsManager.ContentType.All:
                    contentLoaders[0].GetComponentInChildren<ContentVideoScreen>(true).UpdateSettings(this.settings.lerpAlpha, this.settings.autoPlay, this.settings.loopVideo, this.settings.showControls, this.settings.showScrubber);
                    contentLoaders[1].GetComponentInChildren<ContentImageScreen>(true).UpdateSettings(this.settings.lerpAlpha, this.settings.minZoom, this.settings.maxZoom, this.settings.zoomSpeed, this.settings.fractionToZoomIn);

                    break;
                case ContentsManager.ContentType.Video:
                    contentLoaders[0].GetComponentInChildren<ContentVideoScreen>(true).UpdateSettings(this.settings.lerpAlpha, this.settings.autoPlay, this.settings.loopVideo, this.settings.showControls, this.settings.showScrubber);

                    break;
                case ContentsManager.ContentType.Image:
                    contentLoaders[0].GetComponentInChildren<ContentImageScreen>(true).UpdateSettings(this.settings.lerpAlpha, this.settings.minZoom, this.settings.maxZoom, this.settings.zoomSpeed, this.settings.fractionToZoomIn);

                    break;
                default:
                    break;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(WorldContentUpload), true), CanEditMultipleObjects]
        public class WorldContentUpload_Editor : UniqueID_Editor
        {
            private WorldContentUpload worldUploadScript;
            private Transform footer;

            private void OnEnable()
            {
                GetBanner();
                Initialise();

                footer = worldUploadScript.transform.Find("Canvas_Controls");

            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    m_instances.RemoveIOObject(worldUploadScript.GetSettings(true));
                }
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                serializedObject.Update();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Lock Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("useDataAPIPassword"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("password"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("displayType"), true);

                DrawReportButton();

                if (footer != null)
                {
                    float footerScaleFactor = worldUploadScript.transform.localScale.x >= 2.63 ? 0.0052f : worldUploadScript.transform.localScale.x >= 1.8f ? 0.0035f : worldUploadScript.transform.localScale.x >= 1.0f ? 0.002f : 0.001f;
                    Resize(footer.transform, footerScaleFactor);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("World Upload Setup", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("TYPE: " + ((ContentsManager.ContentType)serializedObject.FindProperty("type").enumValueIndex).ToString(), GUILayout.ExpandWidth(true));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lerpAlpha"), true);

                switch(((ContentsManager.ContentType)serializedObject.FindProperty("type").enumValueIndex))
                {
                    case ContentsManager.ContentType.All:
                        DrawVideo();
                        EditorGUILayout.Space();
                        DrawImage();
                        break;
                    case ContentsManager.ContentType.Video:
                        DrawVideo();
                        break;
                    case ContentsManager.ContentType.Image:
                        DrawImage();
                        break;
                    default:
                        break;
                }

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("uploadButton"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lockUsed"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contentLoaders"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onLoad"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("permissionRequired"), true);

                Resize(worldUploadScript.uploadButton.transform, 1.0f);
                Resize(worldUploadScript.lockUsed.transform, 1.0f);

                if (GUI.changed || GONameChanged())
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(worldUploadScript);

                    if (Application.isPlaying) return;

                    if (m_instances != null)
                    {
                        m_instances.AddIOObject(worldUploadScript.ID, worldUploadScript.GetSettings());
                    }
                }
            }

            protected override void Initialise()
            {
                base.Initialise();

                worldUploadScript = (WorldContentUpload)target;

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    //need to get the settings from the instances script then update the settings
                    foreach (AppInstances.IOObjectWorldUploadHandler setting in m_instances.ioWorldUploadObjects)
                    {
                        if (setting.referenceID.Equals(worldUploadScript.ID))
                        {
                            worldUploadScript.ApplySettings(setting.settings);
                            break;
                        }
                    }

                    m_instances.AddIOObject(worldUploadScript.ID, worldUploadScript.GetSettings());
                }
            }

            private void DrawReportButton()
            {
                if (!script.gameObject.scene.IsValid()) return;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);

                Report rButton = worldUploadScript.gameObject.GetComponentInChildren<Report>(true);
                bool containsReportButton = rButton != null;

                if(containsReportButton)
                {
                    Resize(rButton.transform, 1.0f);

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

                        if(prefab != null)
                        {
                            GameObject go = Instantiate(prefab as GameObject, Vector3.zero, Quaternion.identity, worldUploadScript.transform);

                            //position button somwhere, bottom left is good
                            float bottom = 0 - (worldUploadScript.GetComponent<BoxCollider>().size.y / 2);
                            float left = 0 - (worldUploadScript.GetComponent<BoxCollider>().size.x / 2);

                            go.transform.localPosition = new Vector3(left, bottom, -0.02f);

                        }
                    }
                }
            }

            private void Resize(Transform t, float scale)
            {
                t.localScale = new Vector3(scale / worldUploadScript.transform.localScale.x, scale / worldUploadScript.transform.localScale.y, scale / worldUploadScript.transform.localScale.z);
            }

            private void DrawVideo()
            {
                EditorGUILayout.LabelField("Video Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("autoPlay"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("loopVideo"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("showControls"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("showScrubber"), true);
            }

            private void DrawImage()
            {
                EditorGUILayout.LabelField("Image Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("minZoom"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("maxZoom"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("zoomSpeed"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("fractionToZoomIn"), true);
            }
        }
#endif
    }
}
