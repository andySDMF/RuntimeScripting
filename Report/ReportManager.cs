using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ReportManager : Singleton<ReportManager>
    {
        public static ReportManager Instance
        {
            get
            {
                return ((ReportManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private List<ReportAPI.ReportJson> m_reports = new List<ReportAPI.ReportJson>();

        public void AddReports(List<ReportAPI.ReportJson> reports)
        {
            m_reports.Clear();
            m_reports.AddRange(reports);

            Report[] all = FindObjectsByType<Report>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                all[i].OnReportsChanged();
            }
        }

        public List<ReportAPI.ReportJson> GetReports(string unique_id)
        {
            List<ReportAPI.ReportJson> temp = new List<ReportAPI.ReportJson>();

            for(int i = 0; i < m_reports.Count; i++)
            {
                if(m_reports[i].unique_id.Equals(unique_id))
                {
                    temp.Add(m_reports[i]);
                }
            }

            return temp;
        }

        public bool HasBeenReported(string unique_id)
        {
            for (int i = 0; i < m_reports.Count; i++)
            {
                if (m_reports[i].unique_id.Equals(unique_id) && !m_reports[i].resolved)
                {
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ReportManager), true)]
        public class ReportManager_Editor : BaseInspectorEditor
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
}
