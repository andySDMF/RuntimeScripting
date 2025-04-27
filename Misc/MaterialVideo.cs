using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(VideoPlayer))]
    [RequireComponent(typeof(AudioSource))]
    public class MaterialVideo : MonoBehaviour
    {
        [SerializeField]
        private VideoClip clip;
        [SerializeField]
        private bool loop = false;
        [SerializeField]
        private bool playOnEnabled = true;
        [SerializeField]
        private bool isSpacialSound = false;

        private AudioSource m_audio;
        private VideoPlayer m_video;

        private void Awake()
        {
            m_audio = GetComponent<AudioSource>();
            m_audio.playOnAwake = false;
            m_audio.loop = loop;
            m_audio.spatialBlend = (isSpacialSound) ? 1 : 0;

            m_video = GetComponent<VideoPlayer>();
            m_video.playOnAwake = false;
            m_video.isLooping = loop;
            m_video.source = VideoSource.VideoClip;
            m_video.renderMode = VideoRenderMode.MaterialOverride;
            m_video.targetMaterialRenderer = GetComponent<Renderer>();
    
            m_video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            m_video.EnableAudioTrack(0, true);
            m_video.SetTargetAudioSource(0, m_audio);
        }

        private void OnEnable()
        {
            if(playOnEnabled)
            {
                Play(clip);
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        public void Play()
        {
            if(!m_video.isPlaying)
            {
                if(m_video.source == VideoSource.VideoClip)
                {
                    if (m_video.clip != null)
                    {
                        m_video.Play();
                    }
                }
            }
        }

        public void Play(VideoClip clip)
        {
            Stop();
            m_video.clip = clip;

            if(clip != null)
            {
                Play();
            }
        }

        public void Stop()
        {
            if (m_video.isPlaying)
            {
                m_video.Stop();
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(MaterialVideo), true)]
        public class MaterialVideo_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("clip"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("loop"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnEnabled"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("isSpacialSound"), true);

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}
