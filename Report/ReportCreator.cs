using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace BrandLab360
{
    public class ReportCreator : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField]
        private TMP_InputField subject;
        [SerializeField]
        private CanvasGroup subjectError;

        [SerializeField]
        private TMP_InputField comments;
        [SerializeField]
        private CanvasGroup commentsError;

        [SerializeField]
        private GameObject adminFooter;

        [SerializeField]
        private GameObject reportFooter;

        [SerializeField]
        private TextMeshProUGUI resolveText;

        [SerializeField]
        private TextMeshProUGUI countText;

        [Header("Mobile Layout")]
        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private RectTransform footerLayout;
        [SerializeField]
        private TextMeshProUGUI subjectTitle;
        [SerializeField]
        private TextMeshProUGUI commentstTitle;

        private List<ReportAPI.ReportJson> m_reports = new List<ReportAPI.ReportJson>();
        private int m_index = 0;

        private float m_layoutWidth;
        private RectTransform m_mainLayout;
        private float m_cacheTitleText;

        private float m_subjectTitleHeight;
        private float m_subjectInputHeigth;
        private float m_subjectInputFontSize;

        private float m_detailsTitleHeight;
        private float m_deatilsInputHeigth;
        private float m_detailsInputFontSize;

        public string CurrentObjectID
        {
            get;
            set;
        }

        private void Awake()
        {
            m_mainLayout = transform.GetChild(0).GetComponent<RectTransform>();
            m_layoutWidth = m_mainLayout.sizeDelta.x;

            m_cacheTitleText = titleText.fontSize;

            m_subjectTitleHeight = subjectTitle.fontSize;
            m_subjectInputHeigth = subject.GetComponent<LayoutElement>().minHeight;
            m_subjectInputFontSize = subject.textComponent.fontSize;

            m_detailsTitleHeight = commentstTitle.fontSize;
            m_deatilsInputHeigth = comments.GetComponent<LayoutElement>().minHeight;
            m_detailsInputFontSize = comments.textComponent.fontSize;
        }

        private void OnEnable()
        {
            if (!AppManager.IsCreated) return;
            m_reports.Clear();

            commentsError.alpha = 0.0f;
            subjectError.alpha = 0.0f;

            //if admin show different footer
            if (AppManager.Instance.Data.IsAdminUser)
            {
                m_index = 0;
                m_reports.AddRange(ReportManager.Instance.GetReports(CurrentObjectID));

                reportFooter.SetActive(false);
                adminFooter.SetActive(true);

                //ensure that the first report is open
                if (m_reports.Count > 0)
                {
                    subject.interactable = false;
                    comments.interactable = false;
                    Refresh();
                }
            }
            else
            {
                adminFooter.SetActive(false);
                reportFooter.SetActive(true);

                //user can only post
                subject.text = "";
                subject.interactable = true;

                comments.text = "";
                comments.interactable = true;
            }

            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            SubtitleManager.Instance.ToggleButtonVisibiliy(false);
        }

        private void OnDisable()
        {
            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;

            SubtitleManager.Instance.ToggleButtonVisibiliy(true);
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    m_mainLayout.anchorMin = new Vector2(0.5f, 0.0f);
                    m_mainLayout.anchorMax = new Vector2(0.5f, 1.0f);
                    m_mainLayout.anchoredPosition = Vector2.zero;
                    m_mainLayout.sizeDelta = new Vector2(m_layoutWidth, m_mainLayout.sizeDelta.y);

                    titleText.fontSize = m_cacheTitleText;

                    foreach (Selectable but in footerLayout.GetComponentsInChildren<Selectable>(true))
                    {
                        LayoutElement le = but.GetComponent<LayoutElement>();
                        le.minHeight = 60 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;

                        TextMeshProUGUI txt = but.GetComponentInChildren<TextMeshProUGUI>(true);
                        txt.fontSize = 32;
                    }

                    subjectTitle.fontSize = m_subjectTitleHeight;
                    subject.GetComponent<LayoutElement>().minHeight = m_subjectInputHeigth;

                    foreach (TMP_Text txt in subject.GetComponentsInChildren<TMP_Text>())
                    {
                        txt.fontSize = m_subjectInputFontSize;
                    }

                    commentstTitle.fontSize = m_detailsTitleHeight;
                    comments.GetComponent<LayoutElement>().minHeight = m_deatilsInputHeigth;

                    foreach (TMP_Text txt in comments.GetComponentsInChildren<TMP_Text>())
                    {
                        txt.fontSize = m_detailsInputFontSize;
                    }
                }
                else
                {
                    float aspect = arg2 / arg1;

                    m_mainLayout.anchorMin = new Vector2(0f, 0.0f);
                    m_mainLayout.anchorMax = new Vector2(1f, 1.0f);
                    m_mainLayout.offsetMax = new Vector2(-50, m_mainLayout.offsetMax.y);
                    m_mainLayout.offsetMin = new Vector2(50, m_mainLayout.offsetMin.y);

                    m_mainLayout.anchoredPosition = Vector2.zero;

                    titleText.fontSize = m_cacheTitleText * aspect;

                    foreach (Selectable but in footerLayout.GetComponentsInChildren<Selectable>(true))
                    {
                        LayoutElement le = but.GetComponent<LayoutElement>();
                        le.minHeight = (60 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler) * aspect;

                        TextMeshProUGUI txt = but.GetComponentInChildren<TextMeshProUGUI>(true);
                        txt.fontSize = 32 * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    }

                    subjectTitle.fontSize = m_subjectTitleHeight * aspect;
                    subject.GetComponent<LayoutElement>().minHeight = m_subjectInputHeigth * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;

                    foreach (TMP_Text txt in subject.GetComponentsInChildren<TMP_Text>())
                    {
                        txt.fontSize = m_subjectInputFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    }

                    commentstTitle.fontSize = m_detailsTitleHeight * aspect;
                    comments.GetComponent<LayoutElement>().minHeight = m_deatilsInputHeigth * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;

                    foreach (TMP_Text txt in comments.GetComponentsInChildren<TMP_Text>())
                    {
                        txt.fontSize = m_detailsInputFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    }
                }
            }
        }

        public void Close()
        {
            CurrentObjectID = "";
            PlayerManager.Instance.FreezePlayer(false);
            HUDManager.Instance.ToggleHUDScreen("REPORT_SCREEN");
        }

        public void Report()
        {
            if (!AppManager.Instance.Data.IsAdminUser)
            {
                bool failed = false;

                //need to check to see if the inputs are null
                if(string.IsNullOrEmpty(subject.text))
                {
                    failed = true;
                    subjectError.alpha = 0.5f;
                }
                else
                {
                    subjectError.alpha = 0.0f;
                }

                if (string.IsNullOrEmpty(comments.text))
                {
                    failed = true;
                    commentsError.alpha = 0.5f;
                }
                else
                {
                    commentsError.alpha = 0.0f;
                }

                if (!failed)
                {
                    //send to API
                    ReportAPI.Instance.PostReport(CurrentObjectID, subject.text, comments.text, ReportCallback);
                }
            }
        }

        public void Resolve()
        {
            if (AppManager.Instance.Data.IsAdminUser)
            {
                if (m_reports[m_index].resolved) return;

                //send to API
                ReportAPI.Instance.UpdateReport(m_reports[m_index].id, CurrentObjectID, m_reports[m_index].subject, m_reports[m_index].comment, true, ResolveCallback);
            }
        }

        public void Delete()
        {
            if (AppManager.Instance.Data.IsAdminUser)
            {
                ReportAPI.Instance.DeleteReport(m_reports[m_index].id, DeleteCallback);
            }
        }

        public void Next()
        {
            if (AppManager.Instance.Data.IsAdminUser)
            {
                m_index++;

                if(m_index >= m_reports.Count)
                {
                    m_index = 0;
                }

                Refresh();
            }
        }

        public void Previous()
        {
            if (AppManager.Instance.Data.IsAdminUser)
            {
                m_index--;

                if (m_index < 0)
                {
                    m_index = m_reports.Count - 1;
                }

                Refresh();
            }
        }

        private void Refresh()
        {
            subject.text = m_reports[m_index].subject;
            comments.text = m_reports[m_index].comment;

            resolveText.text = m_reports[m_index].resolved ? "RESOLVED" : "RESOLVE";

            countText.text = (m_index + 1).ToString() + " of " + (m_reports.Count).ToString();
        }

        private void ReportCallback(bool success)
        {
            if(success)
            {
                Close();
            }
        }

        private void DeleteCallback(bool success)
        {
            if (success)
            {
                m_reports.RemoveAt(m_index);


                if(m_reports.Count > 0)
                {
                    Next();
                }
                else
                {
                    Close();
                }
            }
        }

        private void ResolveCallback(bool success)
        {
            if (success)
            {
                m_reports[m_index].resolved = true;
                Refresh();
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ReportCreator), true)]
        public class ReportCreator_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("subject"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("subjectError"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("comments"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("commentsError"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("adminFooter"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("reportFooter"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("resolveText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("countText"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("titleText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("footerLayout"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("subjectTitle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("commentstTitle"), true);

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
