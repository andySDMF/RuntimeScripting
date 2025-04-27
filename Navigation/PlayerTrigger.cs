using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Collider))]
    public class PlayerTrigger : MonoBehaviour
    {
        [SerializeField]
        protected TriggerActivationMode activationMode;

        public UnityEvent TriggerEnter;
        public UnityEvent TriggerStay;
        public UnityEvent TriggerExit;

        private bool m_performedAction = false;
        private bool m_processExit = false;

        private void Awake()
        {
            var collider = this.GetComponent<Collider>();
            if (!collider.isTrigger)
            {
                Debug.LogWarning("Warning: PlayerTrigger requires collider set as Trigger. Setting automatically on: " + gameObject.name);
                collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var playerController = other.GetComponent<IPlayer>();

            if(playerController == null) { return; }

            if (activationMode == TriggerActivationMode.Local && !m_processExit)
            {
                if (m_performedAction) return;

                m_performedAction = true;

                if (playerController.IsLocal && TriggerEnter != null)
                {
                    TriggerEnter.Invoke();
                }
            } 
            else
            {
                if (!playerController.IsLocal && TriggerEnter != null)
                {
                    TriggerEnter.Invoke();
                }
            }
        }

        public void OnTriggerStay(Collider other)
        {
            var playerController = other.GetComponent<IPlayer>();

            if (playerController == null) { return; }

            if (activationMode == TriggerActivationMode.Local)
            {
                if (playerController.IsLocal && TriggerStay != null)
                {
                    TriggerStay.Invoke();
                }
            }
            else
            {
                if (!playerController.IsLocal && TriggerStay != null)
                {
                    TriggerStay.Invoke();
                }
            }
        }

        public void OnTriggerExit(Collider other)
        {
            var playerController = other.GetComponent<IPlayer>();

            if (playerController == null) { return; }

            if (activationMode == TriggerActivationMode.Local && !PlayerManager.Instance.GetLocalPlayer().FreezePosition)
            {
                if (!m_processExit)
                {
                    m_processExit = true;
                    StartCoroutine(Delay());
                }

                if (playerController.IsLocal && TriggerExit != null)
                {
                    TriggerExit.Invoke();
                }
            }
            else
            {
                if (!playerController.IsLocal && TriggerExit != null)
                {
                    TriggerExit.Invoke();
                }
            }
        }

        private IEnumerator Delay()
        {
            yield return new WaitForSeconds(2.0f);

            m_processExit = false;
            m_performedAction = false;
        }
    }

    public enum TriggerActivationMode { Local, Remote };

#if UNITY_EDITOR
    [CustomEditor(typeof(Bounce), true)]
    public class Bounce_Editor : BaseInspectorEditor
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("activationMode"), true);

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(this);
            }
        }
    }
#endif
}