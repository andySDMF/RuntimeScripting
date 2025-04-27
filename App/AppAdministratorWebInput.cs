using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class AppAdministratorWebInput : MonoBehaviour
    {
        [SerializeField]
        private TextEntryMessage webDetails;

        [SerializeField]
        private TMP_InputField inputField;

        private void Awake()
        {
            if (inputField != null)
            {
                inputField.onSelect.AddListener(OnInputSelected);
                inputField.onDeselect.AddListener(OnInputDeslected);
            }
        }

        public void OnInputSelected(string str)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
#if UNITY_EDITOR
                WebclientManager.WebClientListener += InputResult;
#else
                WebInputManager.TextEntryResultResultListener += InputResult;
#endif

                WebInputManager.Instance.SendWebInputFieldRequest(webDetails);
            }
        }

        public void OnInputDeslected(string str)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
#if UNITY_EDITOR
                WebclientManager.WebClientListener -= InputResult;
#else
                WebInputManager.TextEntryResultResultListener -= InputResult;
#endif

                WebInputManager.Instance.SendWebInputFieldCloseRequest();
            }
        }

        protected virtual void InputResult(TextEntryResult obj)
        {
            if (inputField != null)
            {
                inputField.text = obj.result;
            }
        }

        protected virtual void InputResult(string str)
        {
            WebInputResponse obj = JsonUtility.FromJson<WebInputResponse>(str).OrDefaultWhen(x => x.btn_clicked == null && x.values == null);

            if (inputField != null)
            {
                inputField.text = obj.values;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AppAdministratorWebInput), true)]
        public class AppAdministratorWebInput_Editor : BaseInspectorEditor
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

                    EditorGUILayout.LabelField("Web Comms", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("webDetails"), true);

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Input Component", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("inputField"), true);

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
