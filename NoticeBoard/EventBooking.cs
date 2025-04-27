using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if BRANDLAB360_INTERNAL
using BrandLab360.Internal;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class EventBooking : UniqueID
    {
        private void Start()
        {
            Button button = GetComponentInChildren<Button>();

            if(button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        /// <summary>
        /// Used when you click button
        /// </summary>
        private void OnClick()
        {
#if BRANDLAB360_INTERNAL
            CalendarManager.Instance.OpenCalendar(ID);
#endif
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(EventBooking), true)]
    public class EventBooking_Editor : UniqueID.UniqueID_Editor
    {
        private EventBooking eventScript;

        protected override void Initialise()
        {
            GetBanner();
            base.Initialise();

            eventScript = (EventBooking)target;
        }

        public override void OnInspectorGUI()
        {
            DisplayBanner();
            serializedObject.Update();

            DrawID();

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(eventScript);
            }
        }
    }
#endif
}
