using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Poster : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField]
        private GameObject trigger;

        [SerializeField]
        private TextMeshProUGUI textScript;

        [Header("Sources")]
        [SerializeField]
        private string caption = "";

        [SerializeField]
        private AudioClip audioClip;

        [SerializeField]
        private TextAsset transcript;

        [Header("Visibility")]
        [SerializeField]
        private bool hideMesh = true;
        [SerializeField]
        private bool hideCaption = false;

        private ColliderTriggerEvent m_triggerScript;
        private AudioSource m_audioSource;

        private void Start()
        {
            hideCaption = true;

            if (trigger != null)
            {
                if (trigger.GetComponent<ColliderTriggerEvent>() == null)
                {
                    m_triggerScript = trigger.AddComponent<ColliderTriggerEvent>();
                }
                else
                {
                    m_triggerScript = trigger.GetComponent<ColliderTriggerEvent>();
                }

                m_triggerScript.OnTriggerEvent += OnTrigger;

                m_triggerScript.GetComponent<MeshRenderer>().enabled = !hideMesh;
            }

            if (GetComponent<AudioSource>() == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }
            else
            {
                m_audioSource = GetComponent<AudioSource>();
            }

            m_audioSource.playOnAwake = false;
            m_audioSource.spatialBlend = 1.0f;
            m_audioSource.maxDistance = trigger != null ? trigger.transform.localScale.x * trigger.transform.localScale.z : 10;
            m_audioSource.clip = audioClip;
            m_audioSource.loop = true;

            if (audioClip != null && trigger == null)
            {
                m_audioSource.Play();
            
            }



            SubtitleManager.Instance.PlayAudioTranscript(audioClip, transcript.text);

            if (textScript != null)
            {
                textScript.text = caption;
                textScript.transform.parent.gameObject.SetActive(!hideCaption);
            }
        }

        private void OnTrigger(bool entered)
        {
            if (entered)
            {
                if (audioClip != null)
                {
                    m_audioSource.Play();
                }
            }
            else
            {
                m_audioSource.Stop();
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Poster), true)]
        public class Poster_Editor : BaseInspectorEditor
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
                EditorGUILayout.PropertyField(serializedObject.FindProperty("trigger"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textScript"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("caption"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("audioClip"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("transcript"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("hideMesh"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hideCaption"), true);

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
