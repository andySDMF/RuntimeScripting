using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class WebInputBox : MonoBehaviour
    {
        [SerializeField]
        protected WebInputData inputData;

        [SerializeField]
        protected bool overrideWebRequest = false;

        protected virtual void OnEnable()
        {
            WebInputManager.WebInputResultListener += InputResult;
            SendWebInputRequest();
        }

        protected virtual void OnDisable()
        {
            WebInputManager.WebInputResultListener -= InputResult;
        }

        protected virtual void InputResult(WebInputResult obj)
        {
            Debug.Log(JsonUtility.ToJson(obj));
        }

        public virtual void SendWebInputRequest()
        {
            if(AppManager.Instance.Data.IsMobile || overrideWebRequest)
            {
                WebInputManager.Instance.SendWebInputRequest(inputData);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(WebInputBox), true)]
        public class WebInputBox_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("inputData"), true);
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
