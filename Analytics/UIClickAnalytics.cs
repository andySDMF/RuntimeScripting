using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class UIClickAnalytics : BaseAnalytics, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            SendAnalytics();
        }

        /// <summary>
        /// Send analytics event message to manager
        /// </summary>
        public override void SendAnalytics()
        {
            if (type.Equals(AnaylticsEventType.Predefined))
            {
                AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.UI, EventAction.Click, label);
            }
            else
            {
                AnalyticsManager.Instance.PostAnalyticsEvent(message);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(UIClickAnalytics), true)]
        public class UIClickAnalytics_Editor : BaseAnalytics_Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
            }
        }
#endif
    }
}
