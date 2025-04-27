using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class SubtitleVideoConverter : MonoBehaviour
    {
        [SerializeField]
        private VideoPlayer video;

        [SerializeField]
        private UnityEngine.Audio.AudioMixerGroup mixer;

        [SerializeField]
        private TextAsset transcript;

        private List<VideoTiming> subtitles = new List<VideoTiming>();

        private VideoTiming m_currentTiming;
        private int m_index = 0;

        private void Awake()
        {
            if(transcript == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (transcript.text[0].ToString().Equals("{"))
            {
                subtitles.Clear();
                SubtitlePanel.SubtitleTimingWrapper m_json = JsonUtility.FromJson<SubtitlePanel.SubtitleTimingWrapper>(transcript.text);

                for (int i = 0; i < m_json.timings.Count; i++)
                {
                    VideoTiming t = new VideoTiming();
                    t.seconds = m_json.timings[i].start;
                    t.subtitle = m_json.timings[i].text;
                    subtitles.Add(t);
                }
            }
            else
            {
                VideoTiming t = new VideoTiming();
                t.seconds = 0.0f;
                t.subtitle = transcript.text;
                subtitles.Add(t);
            }

            video.loopPointReached += End;
        }

        private void Update()
        {
            if (video.isPlaying && subtitles.Count > 0)
            {
                if(m_currentTiming == null)
                {
                    m_currentTiming = subtitles[0];
                    SubtitleManager.Instance.PlayAudioTranscript(null, m_currentTiming.subtitle, null, mixer);
                }

                if (m_index + 1 < subtitles.Count)
                {
                    if (video.time >= subtitles[m_index + 1].seconds)
                    {
                        SubtitleManager.Instance.Stop();
                        m_currentTiming = subtitles[m_index + 1];
                        m_index++;
                        //wait a frame
                        StartCoroutine(Delay());
                    }
                }
            }
        }

        private IEnumerator Delay()
        {
            yield return new WaitForSeconds(0.5f);
            SubtitleManager.Instance.PlayAudioTranscript(null, m_currentTiming.subtitle, null, mixer);
        }

        private void End(VideoPlayer source)
        {
            StartCoroutine(OnEnd());
        }

        private IEnumerator OnEnd()
        {
            yield return new WaitForSeconds(1.0f);

            if(SubtitleManager.Instance.CurrentSubtitle.Equals(m_currentTiming.subtitle))
            {
                SubtitleManager.Instance.Stop();

                m_index = 0;

                if (video.isLooping)
                {
                    m_currentTiming = subtitles[0];
                    SubtitleManager.Instance.PlayAudioTranscript(null, m_currentTiming.subtitle, null, mixer);
                }
                else
                {
                    m_currentTiming = null;
                }
            }
        }


        [System.Serializable]
        private class VideoTiming
        {
            public float seconds;
            public string subtitle;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SubtitleVideoConverter), true)]
        public class SubtitleVideoConverter_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("video"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("mixer"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("transcript"), true);

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
