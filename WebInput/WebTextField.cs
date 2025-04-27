using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class WebTextField : MonoBehaviour
    {
        [SerializeField]
        private bool sendRequestOnEnable = true;

        [SerializeField]
        private TextEntryMessage webDetails;

        [SerializeField]
        private TMP_InputField inputField;

        [SerializeField]
        private UnityEvent onRecieveResult;

        [SerializeField]
        protected bool overrideWebRequest = false;

        private bool m_isMobile = false;
        private bool m_hasSentRequest = false;

        protected virtual void OnEnable()
        {
            inputField.onSelect.AddListener(SendWebInputRequest);
            inputField.onDeselect.AddListener(SendWebInputHide);

#if UNITY_EDITOR
            WebclientManager.WebClientListener += InputResult;
#else
            WebInputManager.TextEntryResultResultListener += InputResult;
#endif
            if (sendRequestOnEnable)
            {
                SendWebInputRequest();
            }
        }

        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            WebclientManager.WebClientListener -= InputResult;
#else
            WebInputManager.TextEntryResultResultListener -= InputResult;
#endif
            SendWebInputHide();
        }

        protected virtual void InputResult(TextEntryResult obj)
        {
            if(inputField != null && obj != null)
            {
                inputField.text = obj.result;

                if (onRecieveResult != null)
                {
                    onRecieveResult.Invoke();
                }
            }
        }

        protected virtual void InputResult(string str)
        {
            WebInputResponse obj = JsonUtility.FromJson<WebInputResponse>(str).OrDefaultWhen(x=> x.btn_clicked == null && x.values == null);

            if (inputField != null && obj != null)
            {
                inputField.text = obj.values;

                if (onRecieveResult != null)
                {
                    onRecieveResult.Invoke();
                }
            }
        }

        public virtual void SendWebInputRequest()
        {
            m_hasSentRequest = true;
            m_isMobile = AppManager.Instance.Data.IsMobile;

            if (m_isMobile || overrideWebRequest)
            {
                WebInputManager.Instance.SendWebInputFieldRequest(webDetails);
            }
        }

        public virtual void SendWebInputHide()
        {
            m_hasSentRequest = false;
            if (m_isMobile || overrideWebRequest)
            {
                WebInputManager.Instance.SendWebInputFieldCloseRequest();
            }
        }

        private void SendWebInputRequest(string str)
        {
            if (m_hasSentRequest) return;

            SendWebInputRequest();
        }

        private void SendWebInputHide(string str)
        {
            if (!m_hasSentRequest) return;

            SendWebInputHide();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(WebTextField), true)]
        public class WebTextField_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.LabelField("Behaviour", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sendRequestOnEnable"), true);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Readable Input", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("inputField"), true);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Web", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("webDetails"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onRecieveResult"), true);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Editor Tool", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("overrideWebRequest"), true);

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
