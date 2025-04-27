using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Report : MonoBehaviour
    {
        [SerializeField]
        private Image buttonImage;

        private Color m_cacheColor;

        private void Awake()
        {
            m_cacheColor = buttonImage.color;
        }

        private void OnEnable()
        {
            if (!AppManager.IsCreated) return;

            OnReportsChanged();
        }

        public void Open()
        {
            if(AppManager.Instance.Data.IsAdminUser)
            {
                if (ReportManager.Instance.GetReports(GetID()).Count <= 0) return;
            }

            //open the report overlay
            PlayerManager.Instance.FreezePlayer(true);
            ReportCreator rc = HUDManager.Instance.GetHUDScreenObject("REPORT_SCREEN").GetComponentInChildren<ReportCreator>();

            if(rc != null)
            { 
                //need to pass this to the reportCreator
                rc.CurrentObjectID = GetID();
            }

            HUDManager.Instance.ToggleHUDScreen("REPORT_SCREEN");
        }

        public void OnReportsChanged()
        {
            if(AppManager.Instance.Data.IsAdminUser)
            {
                //need to highlight this report canvas as having a report/s
                buttonImage.color = ReportManager.Instance.HasBeenReported(GetID()) ? Color.red : m_cacheColor;
            }
        }

        private string GetID()
        {
            UniqueID uID = GetComponentInParent<UniqueID>();
            string id = "";

            if (uID != null)
            {
                id = uID.ID;
            }
            else
            {
                //must be user profile
                UserProfile profile = GetComponentInParent<UserProfile>();

                if (profile != null)
                {
                    id = profile.CurrentPlayerID;
                }
            }

            return id;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Report), true), CanEditMultipleObjects]
        public class Report_Editor : BaseInspectorEditor
        {
            private Report script;

            private void OnEnable()
            {
                GetBanner();
                script = (Report)target;
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonImage"), true);

                if (GUILayout.Button("Back To Parent"))
                {
                    Selection.activeTransform = script.transform.parent;
                }

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(script);
                }
            }
        }
#endif
    }
}
