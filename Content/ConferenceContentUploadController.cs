using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ConferenceContentUploadController : MonoBehaviour
    {
        [Header("Video Components")]
        [SerializeField]
        private GameObject[] videoLayouts;
        [SerializeField]
        private Image videoPlayPauseButton;
        [SerializeField]
        private Sprite playIcon;
        [SerializeField]
        private Sprite pauseIcon;

        [Header("Image Components")]
        [SerializeField]
        private GameObject[] imageLayouts;
        [SerializeField]
        private float panSpeed = 10.0f;

        [Header("PDF Components")]
        [SerializeField]
        private GameObject[] pdfLayouts;

        private ConferenceContentUpload m_uploader;
        private IContentLoader m_display;
        private ImagePanJoystick m_panJoystick;
        private bool m_usePan = false;
        private bool m_rewind = false;
        private bool m_fastForward = false;
        private bool m_playing = false;

        private void OnDisable()
        {
            for(int i = 0; i < videoLayouts.Length; i++)
            {
                videoLayouts[i].SetActive(false);
            }

            for (int i = 0; i < imageLayouts.Length; i++)
            {
                imageLayouts[i].SetActive(false);
            }

            for (int i = 0; i < pdfLayouts.Length; i++)
            {
                pdfLayouts[i].SetActive(false);
            }

            m_uploader = null;
            m_display = null;
            m_usePan = false;
        }

        public void Update()
        {
            if(m_rewind)
            {
                if (((ContentVideoScreen)m_display).VPlayer.time > 0)
                {
                    --((ContentVideoScreen)m_display).VPlayer.time;
                }
            }

            if(m_fastForward)
            {
                if (((ContentVideoScreen)m_display).VPlayer.frame < (long)((ContentVideoScreen)m_display).VPlayer.frameCount)
                {
                    ++((ContentVideoScreen)m_display).VPlayer.time;
                }
            }
        }

        private void OnPanUpdate(Vector2 vec)
        {
            if (m_panJoystick != null && m_usePan)
            {
                if (InputManager.Instance.GetMouseButton(0))
                {
                    Vector3 direction = Vector3.up * vec.y + Vector3.right * vec.x;
                    direction *= panSpeed;

                    ((ContentImageScreen)m_display).Pan(direction);
                }
            }
        }

        /// <summary>
        /// Called to open the controller based on the content type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="uploader"></param>
        /// <param name="display"></param>
        public void Open(ContentsManager.ContentType type, ConferenceContentUpload uploader, IContentLoader display)
        {
            OnDisable();

            m_uploader = uploader;
            m_display = display;

            switch(type)
            {
                case ContentsManager.ContentType.Image:
                    for (int i = 0; i < imageLayouts.Length; i++)
                    {
                        imageLayouts[i].SetActive(true);
                    }

                    m_usePan = true;
                    break;
                case ContentsManager.ContentType.Video:
                    for (int i = 0; i < videoLayouts.Length; i++)
                    {
                        videoLayouts[i].SetActive(true);
                    }

                    videoPlayPauseButton.sprite = ((ContentVideoScreen)m_display).VPlayer.isPlaying ? pauseIcon : playIcon;
                    videoPlayPauseButton.SetNativeSize();

                    break;
                case ContentsManager.ContentType.PDF:
                    for (int i = 0; i < pdfLayouts.Length; i++)
                    {
                        pdfLayouts[i].SetActive(true);
                    }
                    break;
                default:
                    break;
            }

            m_panJoystick = GetComponentInChildren<ImagePanJoystick>(true);
            m_panJoystick.onJoyStickMoved += OnPanUpdate;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Called to unload content (turn off screen)
        /// </summary>
        public void Standby()
        {
            //unload current object
            m_uploader.DeleteCallback(m_uploader.ID, m_uploader.FileInfo);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Called to perform play/pause video
        /// </summary>
        public void PlayPauseVideo()
        {
            videoPlayPauseButton.sprite = ((ContentVideoScreen)m_display).VPlayer.isPlaying ? pauseIcon : playIcon;
            videoPlayPauseButton.SetNativeSize();

            ((ContentVideoScreen)m_display).PlayPause();
        }

        /// <summary>
        /// Called to restart the video
        /// </summary>
        public void RestartVideo()
        {
            ((ContentVideoScreen)m_display).Restart();
        }

        /// <summary>
        /// Called to rewind the video
        /// </summary>
        /// <param name="trigger"></param>
        public void Rewind(bool trigger)
        {
            if(trigger)
            {
                if(!m_rewind)
                {
                    m_playing = ((ContentVideoScreen)m_display).VPlayer.isPlaying;
                    ((ContentVideoScreen)m_display).VPlayer.Pause();
                }

                m_rewind = true;
            }
            else
            {
                m_rewind = false;
                ((ContentVideoScreen)m_display).NetworkScrub();

                if (m_playing)
                {
                    ((ContentVideoScreen)m_display).VPlayer.Play();
                }

                m_playing = false;
            }
        }

        /// <summary>
        /// Called to fast forward the video
        /// </summary>
        /// <param name="trigger"></param>
        public void FastForward(bool trigger)
        {
            if (trigger)
            {
                if (!m_fastForward)
                {
                    m_playing = ((ContentVideoScreen)m_display).VPlayer.isPlaying;
                    ((ContentVideoScreen)m_display).VPlayer.Pause();
                }

                m_fastForward = true;
            }
            else
            {
                m_fastForward = false;
                ((ContentVideoScreen)m_display).NetworkScrub();

                if (m_playing)
                {
                    ((ContentVideoScreen)m_display).VPlayer.Play();
                }

                m_playing = false;
            }
        }

        /// <summary>
        /// Called to zoom into an image
        /// </summary>
        public void ZoomInImage()
        {
            ((ContentImageScreen)m_display).ZoomIn();
        }

        /// <summary>
        /// Called to zoom out an image
        /// </summary>
        public void ZoomOutImage()
        {
            ((ContentImageScreen)m_display).ZoomOut();
        }

        /// <summary>
        /// Called to send pan out to all players
        /// </summary>
        public void SendPan()
        {
            ((ContentImageScreen)m_display).OnPointerUp(null);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ConferenceContentUploadController), true)]
        public class ConferenceContentUploadController_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("videoLayouts"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("videoPlayPauseButton"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("playIcon"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pauseIcon"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("imageLayouts"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("panSpeed"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pdfLayouts"), true);

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
