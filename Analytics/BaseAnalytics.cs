using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class BaseAnalytics : MonoBehaviour
    {
        [Header("Event")]
        [SerializeField]
        protected EventMsg message;

        [SerializeField]
        protected string label;

        [SerializeField]
        protected AnaylticsEventType type = AnaylticsEventType.Predefined;

        public EventMsg Message
        {
            get
            {
                return message;
            }
        }

        /// <summary>
        /// Base function to send analytics event message to manager
        /// </summary>
        public virtual void SendAnalytics()
        {
            //base can only send local event message data
            if(type.Equals(AnaylticsEventType.Predefined))
            {
                Debug.Log("Cannot send predefined analytics using base class. Function must be overriden in inherited class");
            }
            else
            {
                AnalyticsManager.Instance.PostAnalyticsEvent(message);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(BaseAnalytics), true)]
        public class BaseAnalytics_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("message"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("label"), true);

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }

    [Serializable]
    public enum AnaylticsEventType { Predefined, UserDefined }
}
