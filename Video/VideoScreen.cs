using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class VideoScreen : UniqueID, VideoManager.IVideoControl
    {
        [SerializeField]
        private VideoScreenIOObject settings = new VideoScreenIOObject();

        [SerializeField]
        private VideoPlayer player;
        [SerializeField]
        private RawImage output;
        [SerializeField]
        private RectTransform content;
        [SerializeField]
        private GameObject loader;

        [SerializeField]
        private Transform controlsObject;
        [SerializeField]
        private Transform scrubberObject;
        [SerializeField]
        private Transform panelObject;

        [SerializeField]
        private GameObject folderButton;
        [SerializeField]
        private GameObject videoEntry;
        [SerializeField]
        private Transform videoEntryContainer;

        [SerializeField]
        private TextMeshProUGUI currentTimeText;
        [SerializeField]
        private TextMeshProUGUI durationTimeText;
        [SerializeField]
        private Slider scrubber;

        public string Folder
        {
            get
            {
                return "";
            }
        }

        private bool hasInit;
        private bool hasLoaded = false;
        private float m_elapsedTime = 0.0f;
        private float m_maxFrameCount = 0.0f;
        private int m_index = 0;
        private bool m_canInteract = true;

        /// <summary>
        /// States if this video is loaded
        /// </summary>
        public bool IsLoaded { get { return hasLoaded; } }

        public System.TimeSpan currentTime { get; private set; }
        public System.TimeSpan durationTime { get; private set; }

        public VideoPlayer VPlayer
        {
            get
            {
                return player;
            }
        }

        private void Start()
        {
            if(AppManager.IsCreated)
            {
                if (!AppManager.Instance.Instances.ignoreIObjectSettings)
                {
                    //need to get the settings from the instances script then update the settings
                    foreach (AppInstances.IOObjectVideoScreenHandler setting in AppManager.Instance.Instances.ioVideoScreenObjects)
                    {
                        if (setting.referenceID.Equals(GetRawID()))
                        {
                            ApplySettings(setting.settings);
                            break;
                        }
                    }
                }

                Initialise();
                loader.SetActive(false);

                switch (settings.sourceType)
                {
                    case VideoSourceType.API:
                        settings.url = "";
                        settings.videoURLList.Clear();
                        break;
                    case VideoSourceType.URLLIST:
                        settings.url = "";
                        if(settings.videoURLList.Count > 0)
                        {
                            m_index = 0;
                            Load(m_index);
                        }

                        if (videoEntry != null && videoEntryContainer != null)
                        {
                            for(int i = 0; i < settings.videoURLList.Count; i++)
                            {
                                GameObject GO = Instantiate(videoEntry, Vector3.zero, Quaternion.identity, videoEntryContainer);
                                GO.transform.localScale = Vector3.one;
                                GO.transform.localPosition = Vector3.zero;
                                GO.transform.localEulerAngles = Vector3.zero;
                                GO.SetActive(true);
                            }
                        }

                        break;
                    default:
                        settings.videoURLList.Clear();
                        Load(settings.url);
                        break;
                }
            }
        }

        /// <summary>
        /// Called to get the video URL by an index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetVideoByIndex(int index)
        {
            return settings.videoURLList[index];
        }

        /// <summary>
        /// Load video by index
        /// </summary>
        /// <param name="index"></param>
        public void Load(int index)
        {
            if (settings.sourceType.Equals(VideoSourceType.URL)) return;
            else
            {
                Unload();

                m_index = index;
                Load(settings.videoURLList[m_index]);
            }
        }

        /// <summary>
        /// Load video by URL
        /// </summary>
        /// <param name="url"></param>
        public void Load(string url)
        {
            if (hasLoaded) return;

            if (string.IsNullOrEmpty(url)) return;

            panelObject.transform.localScale = Vector3.one;

            hasLoaded = true;
            //prepare video
            gameObject.SetActive(true);
            loader.SetActive(true);

            StartCoroutine(Prepare(url));
        }


        /// <summary>
        /// Called to unload anything that has loaded onto this video screen
        /// </summary>
        public void Unload()
        {
            if (!hasLoaded)
            {
                StopAllCoroutines();
                return;
            }

            hasLoaded = false;

            //stop
            player.Stop();
            loader.SetActive(false);

            //set visuals
            player.EnableAudioTrack(0, false);
            player.clip = null;
            output.texture = null;
            if (settings.lerpAlpha) output.CrossFadeAlpha(0.0f, 0.0f, true);
            else output.transform.localScale = Vector3.zero;

            Flush();
        }

        private void Update()
        {
            if(!RaycastManager.Instance.UIRaycastOperation(gameObject))
            {
                if(m_canInteract)
                {
                    Selectable[] all = GetComponentsInChildren<Selectable>(true);

                    for (int i = 0; i < all.Length; i++)
                    {
                        all[i].interactable = false;
                    }
                }

                m_canInteract = false;
            }
            else
            {
                if(!m_canInteract)
                {
                    Selectable[] all = GetComponentsInChildren<Selectable>(true);

                    for (int i = 0; i < all.Length; i++)
                    {
                        all[i].interactable = true;
                    }
                }

                m_canInteract = true;
            }

            if (hasInit && IsLoaded)
            {
                //video scrubber
                if (player.isPlaying)
                {
                    m_elapsedTime += Time.deltaTime;

                    if (scrubber != null)
                    {
                        scrubber.value = player.frame;
                    }
                }

                currentTime = System.TimeSpan.FromSeconds(System.Math.Round(m_elapsedTime, 0));

                if (currentTime <= durationTime)
                {
                    if (durationTimeText != null)
                    {
                        string str = durationTime.ToString().Substring(3);
                        durationTimeText.text = str;
                    }

                    if (currentTimeText != null)
                    {
                        string str = currentTime.ToString().Substring(3);
                        currentTimeText.text = str;
                    }
                }
            }
        }

        /// <summary>
        /// Called to initialise this video screen
        /// </summary>
        private void Initialise()
        {
            if (hasInit) return;

            if (scrubber != null)
            {
                if (!scrubber.GetComponent<VideoScrubber>())
                {
                    scrubber.gameObject.AddComponent<VideoScrubber>();
                }

                scrubber.onValueChanged.AddListener(Scrub);
            }

            hasInit = true;

            folderButton.SetActive(settings.sourceType.Equals(VideoSourceType.URLLIST));

            player.loopPointReached += OnStopCallback;

            if (CoreManager.Instance.projectSettings.overrideWorldVideoScreensControls)
            {
                settings.loopVideo = CoreManager.Instance.projectSettings.loopVideos;
                settings.autoPlay = CoreManager.Instance.projectSettings.autoPlay;
                settings.showControls = CoreManager.Instance.projectSettings.showControls;
                settings.showScrubber = CoreManager.Instance.projectSettings.showScrubber;
            }

            if (controlsObject != null)
            {
                controlsObject.gameObject.SetActive(settings.showControls ? true : false);
            }

            if (scrubberObject != null)
            {
                scrubberObject.gameObject.SetActive(settings.showScrubber ? true : false);
            }

            //enure the main content screen matches the viewport
            Rect rect = content.parent.GetComponent<RectTransform>().rect;
            content.sizeDelta = new Vector2(rect.width, rect.height);

            //only use API for video
            player.renderMode = VideoRenderMode.APIOnly;
            player.EnableAudioTrack(0, false);
            player.playOnAwake = false;

            //ensure the main output is hidden
            if (settings.lerpAlpha) output.CrossFadeAlpha(0.0f, 0.0f, true);
            else output.transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Called to flush the scrubber
        /// </summary>
        private void Flush()
        {
            m_elapsedTime = 0.0f;

            if (hasLoaded)
            {
                if (scrubber != null)
                {
                    scrubber.wholeNumbers = false;
                    scrubber.minValue = 0;
                    scrubber.maxValue = player.frameCount;
                    m_maxFrameCount = scrubber.maxValue;
                }

                currentTime = System.TimeSpan.FromSeconds(System.Math.Round(m_elapsedTime, 0));
                durationTime = System.TimeSpan.FromSeconds(System.Math.Round(player.frameCount / player.frameRate, 0));
            }
            else
            {
                if (scrubber != null)
                {
                    scrubber.value = 0;
                }

                currentTime = System.TimeSpan.FromSeconds(System.Math.Round(m_elapsedTime, 0));
                durationTime = System.TimeSpan.FromSeconds(System.Math.Round(m_elapsedTime, 0));
            }
        }

        /// <summary>
        /// Callback for when player ends
        /// </summary>
        /// <param name="source"></param>
        private void OnStopCallback(VideoPlayer source)
        {
            if (scrubber != null) scrubber.value = m_maxFrameCount;

            player.EnableAudioTrack(0, false);
            player.Pause();

            if(settings.loopVideo)
            {
                Restart();
            }
        }

        /// <summary>
        /// Called by the scrubber when user lets go of input to update scrubber/video
        /// </summary>
        public void FrameUpdate()
        {
            m_elapsedTime = (float)player.time;
            currentTime = System.TimeSpan.FromSeconds(System.Math.Round(m_elapsedTime, 0));

            if (currentTime <= durationTime)
            {
                if (currentTimeText != null)
                {
                    string str = currentTime.ToString().Substring(3);
                    currentTimeText.text = str;
                }

                if (durationTimeText != null)
                {
                    string str = durationTime.ToString().Substring(3);
                    durationTimeText.text = str;
                }
            }
        }

        /// <summary>
        /// Called by the UI scrubber to update the video frame
        /// </summary>
        /// <param name="val"></param>
        public void Scrub(float val)
        {
            if (!hasLoaded) return;

            if (player.isPlaying) return;

            player.frame = (long)val;
            m_elapsedTime = (float)player.time;
        }

        /// <summary>
        /// Called to play and pause this video thats playing
        /// </summary>
        /// <param name="localPlayer"></param>
        public void PlayPause()
        {
            if (hasLoaded)
            {
                bool flush = currentTime >= durationTime;

                if (scrubber != null)
                {
                    scrubber.value = player.frame;
                }

                if (!player.isPaused && !flush)
                {
                    player.EnableAudioTrack(0, false);
                    player.Pause();
                }
                else
                {
                    if (flush)
                    {
                        Flush();
                    }

                    player.EnableAudioTrack(0, true);
                    player.Play();

                    if(player.clip != null)
                    {
                        AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Video, EventAction.Click, player.clip.name);
                    }
                }
            }
        }

        /// <summary>
        /// Called to restart this video which is playing
        /// </summary>
        /// <param name="localPlayer"></param>
        public void Restart()
        {
            if (hasLoaded)
            {
                if (!player.isPaused)
                {
                    player.Pause();
                }

                player.frame = 0;
                Flush();

                player.EnableAudioTrack(0, true);
                player.Play();
            }
        }

        /// <summary>
        /// Called to prepare the video display with the correct loaded url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private IEnumerator Prepare(string url)
        {
            //prepare and wait until true
            player.url = url;
            player.Prepare();

            while (!player.isPrepared)
            {
                yield return null;
            }

            Flush();

            //output the video to the texture
            output.texture = player.texture;
            output.SetNativeSize();

            //apply aspect ratio if true to fit within content display
            AspectRatioFitter ratio = null;

            if (GetComponentsInChildren<AspectRatioFitter>(true).Length > 0)
            {
                ratio = GetComponentsInChildren<AspectRatioFitter>(true)[0];
            }

            if (ratio != null)
            {
                float texWidth = output.texture.width;
                float texHeight = output.texture.height;
                float aspectRatio = texWidth / texHeight;
                ratio.aspectRatio = aspectRatio;
            }

            //play and visualise the output
            if (settings.autoPlay)
            {
                player.EnableAudioTrack(0, true);
                player.Play();
            }

            if (output.texture != null)
            {
                if (settings.lerpAlpha) output.CrossFadeAlpha(1.0f, 0.5f, true);
                else output.transform.localScale = Vector3.one;
            }

            loader.SetActive(false);
        }

        public void UpdateVideoFiles(VideoAPI.VideoEntries videos)
        {
            Debug.Log("UpdateVideoFiles not implemented yet. Derives from API request");
        }

        [System.Serializable]
        public class VideoScreenIOObject : IObjectSetting
        {
            public VideoSourceType sourceType = VideoSourceType.URLLIST;
            public string url = "";
            public List<string> videoURLList = new List<string>();
            public bool autoPlay = true;
            public bool loopVideo = false;
            public bool showControls = true;
            public bool showScrubber = true;
            public bool lerpAlpha = false;
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

            this.settings.sourceType = ((VideoScreenIOObject)settings).sourceType;
            this.settings.url = ((VideoScreenIOObject)settings).url;
            this.settings.videoURLList = ((VideoScreenIOObject)settings).videoURLList;
            this.settings.autoPlay = ((VideoScreenIOObject)settings).autoPlay;
            this.settings.loopVideo = ((VideoScreenIOObject)settings).loopVideo;
            this.settings.showControls = ((VideoScreenIOObject)settings).showControls;
            this.settings.showScrubber = ((VideoScreenIOObject)settings).showScrubber;
            this.settings.lerpAlpha = ((VideoScreenIOObject)settings).lerpAlpha;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(VideoScreen), true)]
        public class VideoScreen_Editor : UniqueID_Editor
        {
            private VideoScreen videoScript;
            private Transform footer;
            private Transform list;

            private void OnEnable()
            {
                GetBanner();
                Initialise();

                footer = videoScript.transform.Find("Canvas_Controls");
                list = videoScript.transform.Find("Canvas_List");
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    m_instances.RemoveIOObject(videoScript.GetSettings(true));
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();
                DrawID();

                DrawReportButton();

                if(footer != null)
                {
                    float footerScaleFactor = videoScript.transform.localScale.x >= 2.63 ? 0.0052f : videoScript.transform.localScale.x >= 1.8f ? 0.0035f : videoScript.transform.localScale.x >= 1.0f ? 0.002f : 0.001f;
                    Resize(footer.transform, footerScaleFactor);
                }

                if(list != null)
                {
                    float listScaleFactor = videoScript.transform.localScale.x >= 2.63 ? 0.0052f : videoScript.transform.localScale.x >= 1.8f ? 0.0035f : videoScript.transform.localScale.x >= 1.0f ? 0.002f : 0.001f;
                    Resize(list.transform, listScaleFactor);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Video", EditorStyles.boldLabel);

                var sourceType = serializedObject.FindProperty("settings").FindPropertyRelative("sourceType");
                sourceType.intValue = EditorGUILayout.Popup("Source", sourceType.intValue, sourceType.enumNames);

                switch ((VideoSourceType)sourceType.intValue)
                {
                    case VideoSourceType.API:
                        EditorGUILayout.LabelField("Unsupported ATM", EditorStyles.miniBoldLabel);
                        break;
                    case VideoSourceType.URLLIST:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("videoURLList"), true);
                        break;
                    default:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("url"), true);
                        break;
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("player"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lerpAlpha"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("content"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("output"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("loader"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("panelObject"), true);

                EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("autoPlay"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("loopVideo"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("showControls"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("controlsObject"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("showScrubber"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("scrubberObject"), true);

                EditorGUILayout.LabelField("List Display", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("folderButton"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("videoEntry"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("videoEntryContainer"), true);

                EditorGUILayout.LabelField("Scrubbing", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("scrubber"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("currentTimeText"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("durationTimeText"), true);

                if (GUI.changed || GONameChanged())
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(videoScript);

                    if (Application.isPlaying) return;

                    if (m_instances != null)
                    {
                        m_instances.AddIOObject(videoScript.ID, videoScript.GetSettings());
                    }
                }
            }

            protected override void Initialise()
            {
                base.Initialise();

                videoScript = (VideoScreen)target;

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    //need to get the settings from the instances script then update the settings
                    foreach (AppInstances.IOObjectVideoScreenHandler setting in m_instances.ioVideoScreenObjects)
                    {
                        if (setting.referenceID.Equals(videoScript.ID))
                        {
                            videoScript.ApplySettings(setting.settings);
                            break;
                        }
                    }

                    m_instances.AddIOObject(videoScript.ID, videoScript.GetSettings());
                }
            }

            private void DrawReportButton()
            {
                if (!script.gameObject.scene.IsValid()) return;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);

                Report rButton = videoScript.gameObject.GetComponentInChildren<Report>(true);
                bool containsReportButton = rButton != null;

                if (containsReportButton)
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

                        if (prefab != null)
                        {
                            GameObject go = Instantiate(prefab as GameObject, Vector3.zero, Quaternion.identity, videoScript.transform);

                            //position button somwhere, bottom left is good
                            float bottom = 0 - (videoScript.GetComponent<BoxCollider>().size.y / 2);
                            float left = 0 - (videoScript.GetComponent<BoxCollider>().size.x / 2);

                            go.transform.localPosition = new Vector3(left, bottom, -1f);

                        }
                    }
                }
            }

            private void Resize(Transform t, float scale)
            {
                t.localScale = new Vector3(scale / videoScript.transform.localScale.x, scale / videoScript.transform.localScale.y, scale / videoScript.transform.localScale.z);
            }
        }
#endif

        [System.Serializable]
        public enum VideoSourceType { URL, URLLIST, API }
    }
}

