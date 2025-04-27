using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class DropPoint : UniqueID
    {
        public bool useGlobalPosition = false;
        public float spacing = 0.1f;

        public System.Action<PickupItem> OnDropped { get; set; }

        public bool Occupied
        {
            get
            {
                return isOccupied;
            }
            set
            {
                isOccupied = value;
            }
        }

        private bool isOccupied = false;

        public void Drop(PickupItem item, Transform parent)
        {
            if(item != null)
            {
                if (OnDropped != null)
                {
                    OnDropped.Invoke(item);
                }

                AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, AnalyticReference);

                item.ReparentTo(parent);
            }
        }

        public void Drop(PickupItem item, Transform parent, Vector3 overridePosiiton, Vector3 overrideRotation, Vector3 overrrideScale)
        {
            if (item != null)
            {
                if(OnDropped != null)
                {
                    OnDropped.Invoke(item);
                }

                AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Location, EventAction.Click, AnalyticReference);

                item.ReparentTo(parent, overridePosiiton, overrideRotation, overrrideScale);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(DropPoint), true), CanEditMultipleObjects]
        public class DropPoint_Editor : UniqueID_Editor
        {
            private DropPoint dropScript;

            private void OnEnable()
            {
                GetBanner();
                Initialise();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();
                DisplayID();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Drop Point", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useGlobalPosition"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spacing"), true);

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(dropScript);
            }

            protected override void Initialise()
            {
                base.Initialise();

                dropScript = (DropPoint)target;
            }
        }
#endif
    }
}
