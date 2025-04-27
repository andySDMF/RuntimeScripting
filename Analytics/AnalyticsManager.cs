using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class AnalyticsManager : Singleton<AnalyticsManager>
    {
        public static AnalyticsManager Instance
        {
            get
            {
                return ((AnalyticsManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private bool m_useAnalytics = false;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Sends an Event Message out via the webclientmanager
        /// </summary>
        /// <param name="msg"></param>
        public void PostAnalyticsEvent(EventMsg msg)
        {
            if (!Application.isPlaying) return;

            m_useAnalytics = AppManager.Instance.Settings.projectSettings.useAnalytics;

            if (!m_useAnalytics) return;

            if (!MMORoom.Instance.RoomReady) return;

            if (msg != null)
            {
                var json = JsonUtility.ToJson(msg);

                Debug.Log("PostAnalyticsEvent: " + json);

                WebclientManager.Instance.Send(json);
            }
        }

        /// <summary>
        /// Create a new Event Message based on string params and send out via webclientmanager
        /// </summary>
        /// <param name="category"></param>
        /// <param name="action"></param>
        /// <param name="label"></param>
        public void PostAnalyticsEvent(string category, string action, string label)
        {
            if (!Application.isPlaying) return;

            EventMsg msg = new EventMsg(category, action, label);

            PostAnalyticsEvent(msg);
        }

        /// <summary>
        /// Create a new Event Message based on enum params and send out via webclientmanager
        /// </summary>
        /// <param name="category"></param>
        /// <param name="action"></param>
        /// <param name="label"></param>
        public void PostAnalyticsEvent(EventCategory category, EventAction action, string label)
        {
            if (!Application.isPlaying) return;

            EventMsg msg = new EventMsg(category, action, label);

            PostAnalyticsEvent(msg);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AnalyticsManager), true)]
        public class AnalyticsManager_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
            }
        }
#endif
    }

    [Serializable]
    public enum EventCategory { Location, Product, Content, UI, Video }

    [Serializable]
    public enum EventAction { Click, Open, Enter }


    [Serializable]
    public class EventMsg
    {
        public string Category;
        public string Action;
        public string Label;

        //constructor for string parans event
        public EventMsg(string cat, string act, string lab)
        {
            Category = cat;
            Action = act;
            Label = lab;
        }

        //constructor for enum params event
        public EventMsg(EventCategory cat, EventAction act, string lab)
        {
            Category = cat.ToString();
            Action = act.ToString();
            Label = lab.ToString();
        }
    }
}
