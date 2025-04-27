using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ContentVideoScreen : UniqueID, IContentLoader, VideoManager.IVideoControl
    {
        [SerializeField]
        private VideoPlayer player;
        [SerializeField]
        private RawImage output;
        [SerializeField]
        private RectTransform content;
        [SerializeField]
        private GameObject loader;
        [SerializeField]
        private bool lerpAlpha = false;
        [SerializeField]
        private Lock deleteLock;

        [SerializeField]
        private bool autoPlay = true;
        [SerializeField]
        private bool loopVideo = false;
        [SerializeField]
        private bool showControls = true;
        [SerializeField]
        private bool showScrubber = true;

        [SerializeField]
        private Transform controlsObject;
        [SerializeField]
        private Transform scrubberObject;

        [SerializeField]
        private TextMeshProUGUI currentTimeText;
        [SerializeField]
        private TextMeshProUGUI durationTimeText;
        [SerializeField]
        private Slider scrubber;

        private bool hasInit;
        private bool hasLoaded = false;
        private ContentsManager.ContentFileInfo m_fileInfo;
        private string m_url = "";
        private float m_elapsedTime = 0.0f;
        private float m_maxFrameCount = 0.0f;

        /// <summary>
        /// Access to the lock object used on this video screen
        /// </summary>
        public Lock LockUsed { get { return deleteLock; } set { deleteLock = value; } }

        /// <summary>
        /// States if this video screen is networked or not
        /// </summary>
        public bool IsNetworked { get; set; }

        /// <summary>
        /// The owner who is controlling this video screen
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Event to subscribe to on this video controls
        /// </summary>
        public System.Action<string, string> LocalStateChange { get; set; }

        /// <summary>
        /// Access to the current settings data
        /// </summary>
        public string Data { get; private set; }

        /// <summary>
        /// Access to the URL content that is loaded
        /// </summary>
        public string URL { get { return m_url; } }

        /// <summary>
        /// States if this video is loaded
        /// </summary>
        public bool IsLoaded { get { return hasLoaded; } }

        public System.TimeSpan currentTime { get; private set; }
        public System.TimeSpan durationTime { get; private set; }

        public override bool HasParent
        {
            get
            {
                return GetComponentInParent<ConferenceScreen>() != null || GetComponentInParent<WorldContentUpload>() != null || deleteLock == null;
            }
        }

        public VideoPlayer VPlayer
        {
            get
            {
                return player;
            }
        }

        private void OnEnable()
        {
            //init
            Initialise();
            loader.SetActive(false);
        }

        private void OnDisable()
        {
            //ensure that if conference, sync close
            if (IsNetworked)
            {
                if (LocalStateChange != null)
                {
                    LocalStateChange.Invoke("CLOSE", "");
                }
            }

            IsNetworked = false;
            Owner = "";
            LocalStateChange = null;

            //set all button visible
            Button[] all = GetComponentsInChildren<Button>();

            for (int i = 0; i < all.Length; i++)
            {
                all[i].transform.localScale = Vector3.one;
            }

            //unload anything when this object is disabled-memory
            Unload();
        }

        private void Update()
        {
            if(hasInit && IsLoaded)
            {
                //video scrubber
                if(player.isPlaying)
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

        public void UpdateSettings(bool lerpAlpha, bool autoPlay, bool loopVideo, bool showControls, bool showScrubber)
        {
            this.lerpAlpha = lerpAlpha;
            this.autoPlay = autoPlay;
            this.loopVideo = loopVideo;
            this.showControls = showControls;
            this.showScrubber = showScrubber;

#if UNITY_EDITOR
            if(!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        /// <summary>
        /// Called to flush the scrubber
        /// </summary>
        private void Flush()
        {
            m_elapsedTime = 0.0f;

            if(hasLoaded)
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
                if(scrubber != null)
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

            player.Pause();

            if (loopVideo)
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

            NetworkScrub();
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
        /// Called to initialise this video screen
        /// </summary>
        private void Initialise()
        {
            if (hasInit) return;

            if(scrubber != null)
            {
                if (!scrubber.GetComponent<VideoScrubber>())
                {
                    scrubber.gameObject.AddComponent<VideoScrubber>();
                }

                scrubber.onValueChanged.AddListener(Scrub);
            }

            hasInit = true;

            player.loopPointReached += OnStopCallback;

            if(CoreManager.Instance.projectSettings.overrideWorldVideoScreensControls)
            {
                autoPlay = CoreManager.Instance.projectSettings.autoPlay;
                loopVideo = CoreManager.Instance.projectSettings.loopVideos;
                showControls = CoreManager.Instance.projectSettings.showControls;
                showScrubber = CoreManager.Instance.projectSettings.showScrubber;
            }

            if(controlsObject != null)
            {
                controlsObject.gameObject.SetActive(false);
            }

            if (scrubberObject != null)
            {
                scrubberObject.gameObject.SetActive(false);
            }

            //enure the main content screen matches the viewport
            Rect rect = content.parent.GetComponent<RectTransform>().rect;
            content.sizeDelta = new Vector2(rect.width, rect.height);

            //only use API for video
            player.renderMode = VideoRenderMode.APIOnly;
            player.EnableAudioTrack(0, false);
            player.playOnAwake = false;

            //ensure the main output is hidden
            if (lerpAlpha) output.CrossFadeAlpha(0.0f, 0.0f, true);
            else output.transform.localScale = Vector3.zero;

            //if lock used then set up correctly
            if (deleteLock != null)
            {
                deleteLock.IsNetworked = false;
                deleteLock.IgnoreRaycast = true;
            }
        }

        /// <summary>
        /// Called to upload a content file object
        /// </summary>
        /// <param name="file"></param>
        public void Load(ContentsManager.ContentFileInfo file)
        {
            if (hasLoaded) return;

            //set vars
            m_fileInfo = file;
            m_url = file.url;

            //load
            Load(m_fileInfo.url);
        }

        /// <summary>
        /// Called to open a url video
        /// </summary>
        /// <param name="url"></param>
        public void Load(string url)
        {
            if (hasLoaded) return;

            if (string.IsNullOrEmpty(url)) return;

            hasLoaded = true;
            //prepare video
            gameObject.SetActive(true);
            loader.SetActive(true);
            StartCoroutine(Prepare(url));

            if (!string.IsNullOrEmpty(Owner))
            {
                if (!Owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                {
                    //set all button hidden
                    Button[] all = GetComponentsInChildren<Button>();

                    for(int i = 0; i < all.Length; i++)
                    {
                        all[i].transform.localScale = Vector3.zero;
                    }
                }
            }
        }

        /// <summary>
        /// Called to unload anything that has loaded onto this video screen
        /// </summary>
        public void Unload()
        {
            if (!hasLoaded) return;

            hasLoaded = false;

            //stop
            player.Stop();
            loader.SetActive(false);
            controlsObject.gameObject.SetActive(false);

            //set visuals
            player.clip = null;
            player.EnableAudioTrack(0, false);
            output.texture = null;
            if (lerpAlpha) output.CrossFadeAlpha(0.0f, 0.0f, true);
            else output.transform.localScale = Vector3.zero;

            m_fileInfo = null;

            Flush();

            //set lock vars
            if (deleteLock)
            {
                deleteLock.OnUnlock -= OnUnlocked;
                deleteLock.IgnoreRaycast = true;
            }
        }

        /// <summary>
        /// Called when this screens lock is unlocked
        /// </summary>
        private void OnUnlocked()
        {
            if (!hasLoaded) return;

            if (m_fileInfo != null)
            {
                //need to delete this from the database
                ContentsManager.Instance.WebClientDeleteContent(id, m_fileInfo.url);
            }
            else
            {
                //need to delete this from the database
                ContentsManager.Instance.WebClientDeleteContent(id, m_url);
            }

            //unload
            Unload();
        }

        /// <summary>
        /// Called to perform network change on this video screen
        /// </summary>
        /// <param name="state"></param>
        /// <param name="data"></param>
        public void NetworkStateChange(string state, string data = "")
        {
            if(IsNetworked)
            {
                switch(state)
                {
                    case "SETTINGS":
                        VideoJson settings = JsonUtility.FromJson<VideoJson>(data);

                        if(settings != null)
                        {
                            player.Pause();
                            player.frame = settings.frame;
                            m_elapsedTime = (float)player.time;

                            if (settings.isPlaying)
                            {
                                player.EnableAudioTrack(0, true);
                                player.Play();
                            }

                            if(settings.isPasued)
                            {
                                player.EnableAudioTrack(0, false);
                                player.Pause();
                            }
                        }
                        break;
                    case "SCRUB":
                        if(!string.IsNullOrEmpty(Owner))
                        {
                            if (!Owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                            {
                                var frame = long.Parse(data);
                                player.frame = frame;
                                m_elapsedTime = (float)player.time;
                            }
                        }
                        break;
                    case "PLAY":
                    case "PAUSE":
                        PlayPause(false);
                        break;
                    case "RESTART":
                        Restart(false);
                        break;
                    case "OWNER":
                        Owner = Owner;
                        if (!string.IsNullOrEmpty(Owner))
                        {
                            if (!Owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                            {
                                //set all button hidden
                                Button[] all = GetComponentsInChildren<Button>();

                                for (int i = 0; i < all.Length; i++)
                                {
                                    all[i].transform.localScale = Vector3.zero;
                                }
                            }
                            else
                            {
                                //set all button hidden
                                Button[] all = GetComponentsInChildren<Button>();

                                for (int i = 0; i < all.Length; i++)
                                {
                                    all[i].transform.localScale = Vector3.one;
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }


        /// <summary>
        /// Returns JSON of current video settings
        /// </summary>
        /// <returns></returns>
        public string GetSettings()
        {
            VideoJson json = new VideoJson();
            json.isPasued = player.isPaused;
            json.isPlaying = player.isPlaying;
            json.frame = player.frame;

            return JsonUtility.ToJson(json);
        }

        /// <summary>
        /// Called to network the scrub
        /// </summary>
        public void NetworkScrub()
        {
            if (hasLoaded)
            {
                if (IsNetworked)
                {
                    if (LocalStateChange != null)
                    {
                        LocalStateChange.Invoke("SCRUB", player.frame.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Called to play and pause this video thats playing
        /// </summary>
        /// <param name="localPlayer"></param>
        public void PlayPause(bool localPlayer = true)
        {
            if(hasLoaded)
            {
                string state = "";
                bool flush = currentTime >= durationTime;

                if (scrubber != null)
                {
                    scrubber.value = player.frame;
                }

                if (!player.isPaused && !flush)
                {
                    state = "PAUSE";
                    if (!IsNetworked || !localPlayer)
                    {
                        player.EnableAudioTrack(0, false);
                        player.Pause();
                    }
                }
                else
                {
                    state = "PLAY";

                    if (flush)
                    {
                        Flush();
                    }

                    if (!IsNetworked || !localPlayer)
                    {
                        player.EnableAudioTrack(0, true);
                        player.Play();
                    }
                }

                if (IsNetworked && localPlayer)
                {
                    if (LocalStateChange != null)
                    {
                        LocalStateChange.Invoke(state, "");
                    }
                }
            }
        }

        /// <summary>
        /// Called to restart this video which is playing
        /// </summary>
        /// <param name="localPlayer"></param>
        public void Restart(bool localPlayer = true)
        {
            if (hasLoaded)
            {
                if (!IsNetworked || !localPlayer)
                {
                    if (!player.isPaused)
                    {
                        player.EnableAudioTrack(0, false);
                        player.Pause();
                    }

                    player.frame = 0;
                    Flush();

                    player.EnableAudioTrack(0, true);
                    player.Play();
                }

                if (IsNetworked && localPlayer)
                {
                    if (LocalStateChange != null)
                    {
                        LocalStateChange.Invoke("RESTART", "");
                    }
                }
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

            if (controlsObject != null)
            {
                controlsObject.gameObject.SetActive(showControls ? true : false);
            }

            Flush();

            //send RPC to owner
            if(!string.IsNullOrEmpty(Owner))
            {
                if(IsNetworked && !Owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                {
                    //get current settings from owner
                    MMOManager.Instance.SendRPC("RequestConferenceVideoSettings", (int)MMOManager.RpcTarget.All, PlayerManager.Instance.GetLocalPlayer().ID, Owner, id);
                }
            }

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
            if(autoPlay)
            {
                player.EnableAudioTrack(0, true);
                player.Play();
            }

            if (output.texture != null)
            {
                if (lerpAlpha) output.CrossFadeAlpha(1.0f, 0.5f, true);
                else output.transform.localScale = Vector3.one;
            }

            //set lock state
            if (deleteLock)
            {
                deleteLock.OnUnlock += OnUnlocked;
                deleteLock.IgnoreRaycast = false;
                deleteLock.LockThis();
            }

            if (controlsObject != null)
            {
                controlsObject.gameObject.SetActive(showControls ? true : false);
            }

            if (scrubberObject != null)
            {
                scrubberObject.gameObject.SetActive(showScrubber ? true : false);
            }

            loader.SetActive(false);
        }

        [System.Serializable]
        private class VideoJson
        {
            public bool isPlaying;
            public bool isPasued;
            public long frame;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ContentVideoScreen), true)]
        public class ContentVideoScreen_Editor : UniqueID_Editor
        {
            private ContentVideoScreen videoContentScript;
            private bool isWorldContent;
            private bool isConferenceScreen;
            

            private void OnEnable()
            {
                GetBanner();
                Initialise();

                isConferenceScreen = videoContentScript.gameObject.GetComponentInParent<ConferenceScreen>() != null;
                isWorldContent = videoContentScript.gameObject.GetComponentInParent<WorldContentUpload>() != null;

                if (serializedObject.FindProperty("deleteLock").objectReferenceValue == null &&
                    !isConferenceScreen && !isWorldContent)
                {
                    base.Initialise();
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                if (serializedObject.FindProperty("deleteLock").objectReferenceValue == null &&
                    !isConferenceScreen && !isWorldContent)
                {
                    DisplayID();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Video Content Setup", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("player"), true);

                if(!isWorldContent)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lerpAlpha"), true);
                }
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("content"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("output"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("loader"), true);

                EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

                if(!isWorldContent)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("autoPlay"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("loopVideo"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("showControls"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("showScrubber"), true);
                }
   
                EditorGUILayout.PropertyField(serializedObject.FindProperty("controlsObject"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("scrubberObject"), true);

                EditorGUILayout.LabelField("Scrubbing", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("scrubber"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("currentTimeText"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("durationTimeText"), true);

                EditorGUILayout.Space();

                if (!isConferenceScreen && !isWorldContent)
                {
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Display Controller", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("deleteLock"), true);
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(videoContentScript);
            }

            protected override void Initialise()
            {
                videoContentScript = (ContentVideoScreen)target;
            }
        }
#endif
    }
}
