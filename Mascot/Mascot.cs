using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Mascot : UniqueID
    {
        [SerializeField]
        protected MascotIOObject settings = new MascotIOObject();

    
        [SerializeField]
        protected GameObject prefabClipButton;
        [SerializeField]
        protected Transform clipButtonContainer;

        [SerializeField]
        private VideoPlayer m_videoPlayer;
        [SerializeField]
        private Renderer videoBuffer;
        [SerializeField]
        private RawImage m_outputImage;

        //general
        [SerializeField]
        protected AudioSource m_audioSource;

#if UNITY_EDITOR
        public void EditorDisplayTexture()
        {
            if (gameObject.scene.IsValid()) return;

            LoadBaseTexture();
        }

        public void EditorClearTexture()
        {
            m_outputImage.texture = null;
        }
#endif

        private void OnDisable()
        {
            Stop();
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if (!AppManager.Instance.Instances.ignoreIObjectSettings)
            {
                //need to get the settings from the instances script then update the settings
                foreach (AppInstances.IOObjectMascotHandler setting in AppManager.Instance.Instances.ioMascotObjects)
                {
                    if (setting.referenceID.Equals(GetRawID()))
                    {
                        ApplySettings(setting.settings);
                        break;
                    }
                }
            }

            Initialise();

            //create all the UI clip buttons if there are more than 1
            if (settings.clips.Count > 1)
            {
                for (int i = 0; i < settings.clips.Count; i++)
                {
                    GameObject go = Instantiate(prefabClipButton, Vector3.zero, Quaternion.identity, clipButtonContainer);
                    go.transform.localScale = Vector3.one;
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localEulerAngles = Vector3.zero;
                    go.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text = settings.clips[i].title;
                    Button button = go.GetComponentInChildren<Button>(true);
                    int n = i;
                    button.onClick.AddListener(() => { Play(n); });
                    go.SetActive(true);
                }

                //show UI clip list
                if (settings.persistantClipVisibility)
                {
                    clipButtonContainer.parent.gameObject.SetActive(true);
                }
            }
        }

        private void OnEnable()
        {
            if (!AppManager.IsCreated) return;

            //load base texture is there is one
            if (!LoadBaseTexture())
            {
                if (m_outputImage != null)
                {
                    m_outputImage.CrossFadeAlpha(0.0f, 0.0f, true);
                    m_outputImage.enabled = false;
                }
            }

            //play on awake
            if (settings.playOnAwake)
            {
                if (settings.clips.Count > 0)
                {
                    StartCoroutine(WaitForAppReady());
                }
            }
        }

        /// <summary>
        /// Called to wait for the room to be ready until playing, this avoids it playing when loading
        /// </summary>
        /// <returns></returns>
        protected IEnumerator WaitForAppReady()
        {
            while (!AppManager.Instance.Data.RoomEstablished)
            {
                yield return null;
            }

            Play(0);
        }

        /// <summary>
        /// Called to initialise this mascot
        /// </summary>
        protected virtual void Initialise()
        {
            //only use API for video
            if (m_videoPlayer != null)
            {
                if(settings.outputMode.Equals(MascotOuputMode.RawTexture))
                {
                    m_videoPlayer.renderMode = VideoRenderMode.APIOnly;
                }
                else
                {
                    m_videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
                    m_videoPlayer.targetMaterialRenderer = videoBuffer.GetComponent<Renderer>();
                }

                m_videoPlayer.playOnAwake = false;
                m_videoPlayer.loopPointReached += OnStopCallback;
            }

            if (m_audioSource != null)
            {
                m_audioSource.playOnAwake = false;
                m_audioSource.spatialBlend = 1.0f;
            }

            //ensure the main output is hidden
            if (m_outputImage != null)
            {
                if (m_outputImage.mainTexture == null)
                {

                    m_outputImage.CrossFadeAlpha(0.0f, 0.0f, true);
                    m_outputImage.enabled = false;
                }
            }

            if (videoBuffer != null)
            {
                videoBuffer.gameObject.SetActive(false);
            }

            if (settings.persistantClipVisibility)
            {
                //Get info button
                Button[] all = gameObject.GetComponentsInChildren<Button>(true);

                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].name.Equals("Button_Info"))
                    {
                        all[i].gameObject.SetActive(false);
                        break;
                    }
                }
            }

            //add billboard
            if(settings.billboard)
            {
                Billboard bFollow = gameObject.AddComponent<Billboard>();
            }
        }

        /// <summary>
        /// Callback for when the video is reached loop point/ended
        /// </summary>
        /// <param name="source"></param>
        private void OnStopCallback(VideoPlayer source)
        {
            if(!LoadBaseTexture())
            {
                if (m_outputImage != null)
                {
                    m_outputImage.CrossFadeAlpha(0.0f, 0.0f, true);
                    m_outputImage.enabled = false;
                }
            }

            m_videoPlayer.Stop();
            m_videoPlayer.clip = null;
        }

        /// <summary>
        /// called to load base texture
        /// </summary>
        /// <returns></returns>
        private bool LoadBaseTexture()
        {
            if (videoBuffer != null)
            {
                videoBuffer.gameObject.SetActive(false);
            }

            //load base texture is there is one
            if (!string.IsNullOrEmpty(settings.mascotBaseTexture))
            {
                if (m_outputImage != null)
                {
                    m_outputImage.texture = Resources.Load<Texture>(settings.mascotBaseTexture);
                    ApplyTexture();

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Event called when the information button ic clicked
        /// </summary>
        public virtual void OnInformationClick()
        {
            //UI raycast check
            if (!RaycastManager.Instance.UIRaycastOperation(gameObject, true)) return;

            Selectable[] all = GetComponentsInChildren<Selectable>();

            AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, AnalyticReference);

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].gameObject.name.Contains("Info"))
                {
                    RaycastManager.Instance.UIRaycastSelectablePressed(all[i]);
                    break;
                }
            }

            //check clip count
            if (settings.clips.Count >= 0)
            {
                if(settings.clips.Count.Equals(1))
                {
                    Play(0);
                }
                else
                {
                    //show clip list
                    if(!settings.persistantClipVisibility)
                    {
                        clipButtonContainer.parent.gameObject.SetActive(!clipButtonContainer.parent.gameObject.activeInHierarchy);
                    }
                }
            }
        }

        /// <summary>
        /// Called to play a clip
        /// </summary>
        /// <param name="clip"></param>
        public virtual void Play(int clip)
        {
            //close the clip list if both are true
            if(clipButtonContainer.gameObject.activeInHierarchy && !settings.persistantClipVisibility)
            {
                clipButtonContainer.parent.gameObject.SetActive(false);

                //ui raycast check
                if (!RaycastManager.Instance.UIRaycastOperation(clipButtonContainer.gameObject, true)) return;

                Selectable[] all = clipButtonContainer.GetComponentsInChildren<Selectable>();

                for (int i = 0; i < all.Length; i++)
                {
                    if (i.Equals(clip + 1))
                    {
                        RaycastManager.Instance.UIRaycastSelectablePressed(all[i]);
                        break;
                    }
                }
            }

            //play video or sound or both
            if (m_videoPlayer != null && m_audioSource != null)
            {
                if(settings.clips[clip].isAudioOnly)
                {
                    if (settings.clips[clip].type.Equals(MascotClipType.Resource))
                    {
                        AudioClip aClip = Resources.Load<AudioClip>(settings.clips[clip].clip);

                        if (aClip != null)
                        {
                            m_audioSource.clip = aClip;
                            m_audioSource.Play();
                        }
                        else
                        {
                            Debug.Log("VideoClip [" + settings.clips[clip].clip + "] does not exist in Resources");
                            return;
                        }
                    }
                    else
                    {
                        Debug.Log("AudioClip [" + settings.clips[clip].clip + "] cannot be loaded using URL. Must be from resources");
                        return;
                    }
                }

                Stop();

                if (clip >= 0 && clip < settings.clips.Count)
                {
                    Debug.Log("Playing Mascot [" + gameObject.name + "] clip= " + clip);

                    StartCoroutine(PrepareVideo(settings.clips[clip]));
                }
            }
        }

        /// <summary>
        /// Called to stop the playback
        /// </summary>
        public virtual void Stop()
        {
            StopAllCoroutines();

            if (m_videoPlayer != null)
            {
                m_videoPlayer.Stop();
                m_videoPlayer.clip = null;
            }

            if(m_audioSource != null)
            {
                m_audioSource.Stop();
                m_audioSource.clip = null;
            }

            if (!LoadBaseTexture())
            {
                if (m_outputImage != null)
                {
                    m_outputImage.CrossFadeAlpha(0.0f, 0.0f, true);
                    m_outputImage.enabled = false;
                }
            }

            if(videoBuffer != null)
            {
                videoBuffer.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Called to prepare the video display with the correct loaded url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private IEnumerator PrepareVideo(MascotClip clip)
        {
            //prepare and wait until true
            if (clip.type.Equals(MascotClipType.Resource))
            {
                m_videoPlayer.source = VideoSource.Url;
                VideoClip vClip = Resources.Load<VideoClip>(clip.clip);

                if (vClip != null)
                {
                    m_videoPlayer.clip = vClip;
                }
                else
                {
                    Debug.Log("VideoClip [" + clip.clip + "] does not exist in Resources");
                    yield break;
                }
            }
            else
            {
                m_videoPlayer.source = VideoSource.Url;
                m_videoPlayer.url = clip.clip;
            }

            m_videoPlayer.Prepare();

            while (!m_videoPlayer.isPrepared)
            {
                yield return null;
            }

            Debug.Log("VideoClip [" + clip.clip + "] is playing");

            //play and visualise the output
            m_videoPlayer.Play();

            if(clip.startingFrame <= 0)
            {
                clip.startingFrame = 30;
            }

            while(m_videoPlayer.frame < clip.startingFrame)
            {
                yield return null;
            }

            if(settings.outputMode.Equals(MascotOuputMode.RawTexture))
            {
                m_outputImage.texture = m_videoPlayer.texture;
                ApplyTexture();
            }
            else
            {
                m_outputImage.CrossFadeAlpha(0.0f, 0.0f, true);
                videoBuffer.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Called to apply the base texture to the mascot
        /// </summary>
        private void ApplyTexture()
        {
            m_outputImage.SetNativeSize();

            //apply aspect ratio if true to fit within content display
            AspectRatioFitter ratio = null;

            if (GetComponentsInChildren<AspectRatioFitter>(true).Length > 0)
            {
                ratio = GetComponentsInChildren<AspectRatioFitter>(true)[0];
            }

            if (ratio != null)
            {
                float texWidth = m_outputImage.texture.width;
                float texHeight = m_outputImage.texture.height;
                float aspectRatio = texWidth / texHeight;
                ratio.aspectRatio = aspectRatio;
            }

            if (m_outputImage.texture != null)
            {
                m_outputImage.enabled = true;
                m_outputImage.CrossFadeAlpha(1.0f, 0.0f, true);
            }
        }

        [System.Serializable]
        public class MascotClip
        {
            public MascotClipType type = MascotClipType.Resource;
            public string title = "Mascot 1";
            public string clip = "Mascot/Mascot_01";
            public bool isAudioOnly = false;
            public float startingFrame = 30;
        }

        public enum MascotClipType { URL, Resource }
        public enum MascotOuputMode { Material, RawTexture }

        [System.Serializable]
        public class MascotIOObject : IObjectSetting
        {
            public List<MascotClip> clips;
            public bool playOnAwake = false;
            public bool persistantClipVisibility = true;
            public bool billboard = false;
            public string mascotBaseTexture = "Mascot/baseMascotTexture";
            public MascotOuputMode outputMode = MascotOuputMode.RawTexture;
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

            this.settings.clips = ((MascotIOObject)settings).clips;
            this.settings.playOnAwake = ((MascotIOObject)settings).playOnAwake;
            this.settings.persistantClipVisibility = ((MascotIOObject)settings).persistantClipVisibility;
            this.settings.billboard = ((MascotIOObject)settings).billboard;
            this.settings.mascotBaseTexture = ((MascotIOObject)settings).mascotBaseTexture;
            this.settings.outputMode = ((MascotIOObject)settings).outputMode;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Mascot), true)]
        public class Mascot_Editor : UniqueID_Editor
        {
            private Mascot mascotScript;

            private void OnEnable()
            {
                GetBanner();
                Initialise();
            }

            private void OnDisable()
            {
                if (!Application.isPlaying)
                {
                    mascotScript.EditorClearTexture();
                }
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    m_instances.RemoveIOObject(mascotScript.GetSettings(true));
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();
                DrawID();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Mascot Clips", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("clips"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("playOnAwake"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("persistantClipVisibility"), true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Mascot UI", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("prefabClipButton"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("clipButtonContainer"), true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Mascot Output", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("billboard"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("mascotBaseTexture"), true);


                if (!string.IsNullOrEmpty(serializedObject.FindProperty("settings").FindPropertyRelative("mascotBaseTexture").stringValue))
                {
                    if (GUILayout.Button("Display Base Texture"))
                    {
                        mascotScript.EditorDisplayTexture();
                    }

                    EditorGUILayout.Space();
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_videoPlayer"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("outputMode"), true);

                if (serializedObject.FindProperty("settings").FindPropertyRelative("outputMode").enumValueIndex == 0)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("videoBuffer"), true);
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_outputImage"), true);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Mascot Sound", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_audioSource"), true);

                if (GUI.changed || GONameChanged())
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(mascotScript);

                    if (Application.isPlaying) return;

                    if (m_instances != null)
                    {
                        m_instances.AddIOObject(mascotScript.ID, mascotScript.GetSettings());
                    }
                }
            }

            protected override void Initialise()
            {
                base.Initialise();

                mascotScript = (Mascot)target;

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    //need to get the settings from the instances script then update the settings
                    foreach (AppInstances.IOObjectMascotHandler setting in m_instances.ioMascotObjects)
                    {
                        if (setting.referenceID.Equals(mascotScript.ID))
                        {
                            mascotScript.ApplySettings(setting.settings);
                            break;
                        }
                    }

                    m_instances.AddIOObject(mascotScript.ID, mascotScript.GetSettings());
                }
            }
        }
#endif
    }
}
