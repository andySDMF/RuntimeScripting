using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ContentsPanel : Singleton<ContentsPanel>
    {
        public static ContentsPanel Instance
        {
            get
            {
                return ((ContentsPanel)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Files")]
        [SerializeField]
        private Transform container;
        [SerializeField]
        private GameObject filePrefab;
        [SerializeField]
        private GameObject tabFiles;

        [Header("Pages")]
        [SerializeField]
        [Range(10, 40)]
        private int messagesPerPage = 20;
        [SerializeField]
        private TextMeshProUGUI countDisplayMin;
        [SerializeField]
        private TextMeshProUGUI countDisplayMax;
        [SerializeField]
        private TextMeshProUGUI countDisplayTotal;
        [SerializeField]
        private TextMeshProUGUI pageDisplayTotal;

        [Header("Mobile Layout")]
        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private LayoutElement toggleHolderLayout;
        [SerializeField]
        private LayoutElement footerHolderLayout;
        [SerializeField]
        private LayoutElement pageTotalHolderLayout;

        private ContentsManager.ContentType m_currentType = ContentsManager.ContentType.All;
        private List<GameObject> m_filesCreated = new List<GameObject>();
        private List<ContentsManager.ContentFileInfo> m_filteredFiles = new List<ContentsManager.ContentFileInfo>();

        private int m_firstIndex = 0;
        private int m_lastIndex = 0;
        private int m_page = 1;

        private Coroutine m_process;
        private bool m_ascendingOrder = true;
        private bool m_disablePageOperations = false;

        private float m_layoutWidth;
        private RectTransform m_mainLayout;
        private float m_cacheTitleText;
        private float m_togleHolderCaheHeight;
        private float m_footerLayoutCache;
        private float m_toggleTextHeight;
        private float m_pageTotalCacheWidth;


        public GameObject TabFiles
        {
            get
            {
                return tabFiles;
            }
        }

        private void Awake()
        {
            m_mainLayout = transform.GetChild(0).GetComponent<RectTransform>();
            m_layoutWidth = m_mainLayout.sizeDelta.x;
            m_footerLayoutCache = footerHolderLayout.minHeight;

            m_cacheTitleText = titleText.fontSize;
            m_togleHolderCaheHeight = toggleHolderLayout.minHeight;
            m_toggleTextHeight = toggleHolderLayout.GetComponentInChildren<TextMeshProUGUI>(true).fontSize;

            m_pageTotalCacheWidth = pageTotalHolderLayout.minWidth;



            //display
            m_lastIndex = messagesPerPage;
            UpdatePageDisplay();
        }

        private void Start()
        {
            StartCoroutine(WaitFrame());
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
                    toggleHolderLayout.minHeight = m_togleHolderCaheHeight;
                    toggleHolderLayout.preferredHeight = toggleHolderLayout.minHeight;

                    footerHolderLayout.minHeight = m_footerLayoutCache * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;

                    foreach (TextMeshProUGUI ft in toggleHolderLayout.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        ft.fontSize = m_toggleTextHeight;
                    }

                    foreach (TextMeshProUGUI txt in footerHolderLayout.GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        txt.fontSize = 24;
                    }

                    pageTotalHolderLayout.minWidth = m_pageTotalCacheWidth;
                }
                else
                {
                    float aspect = arg2 / arg1;

                    m_mainLayout.anchorMin = new Vector2(0f, 0.0f);
                    m_mainLayout.anchorMax = new Vector2(1f, 1.0f);
                    m_mainLayout.offsetMax = new Vector2(-50, m_mainLayout.offsetMax.y);
                    m_mainLayout.offsetMin = new Vector2(50, m_mainLayout.offsetMin.y);

                    m_mainLayout.anchoredPosition = Vector2.zero;

                    titleText.fontSize = m_cacheTitleText * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    toggleHolderLayout.minHeight = m_togleHolderCaheHeight * aspect;
                    toggleHolderLayout.preferredHeight = toggleHolderLayout.minHeight;

                    footerHolderLayout.minHeight = (m_footerLayoutCache * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler) * aspect;

                    foreach (TextMeshProUGUI ft in toggleHolderLayout.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        ft.fontSize = m_toggleTextHeight * aspect;
                    }

                    foreach (TextMeshProUGUI txt in footerHolderLayout.GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        txt.fontSize = 24 * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    }

                    pageTotalHolderLayout.minWidth = m_pageTotalCacheWidth * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                }
            }
        }

        private IEnumerator WaitFrame()
        {
            yield return new WaitForEndOfFrame();

            Toggle[] togs = GetComponentsInChildren<Toggle>();

            for (int i = 0; i < togs.Length; i++)
            {
                if (togs[i].transform.parent.name != "Layout_Tabs") continue;

                if (togs[i].name.Contains("All"))
                {
                    togs[i].isOn = true;
                    break;
                }
            }
        }

        private void OnEnable()
        {
            //create
            CreateDisplay();

            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            SubtitleManager.Instance.ToggleButtonVisibiliy(false);
        }

        private void OnDisable()
        {
            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;

            SubtitleManager.Instance.ToggleButtonVisibiliy(true);

            //clear
            if (m_process != null)
            {
                StopCoroutine(m_process);
            }

            m_process = null;

            ClearDisplay();
        }

        public void Close()
        {
            ContentsManager.Instance.ToggleContentsScreen();
        }

        /// <summary>
        /// Change current content on the panel
        /// </summary>
        /// <param name="type"></param>
        public void ToggleCurrentContentIndex(ContentsManager.ContentType type)
        {
            if (m_currentType.Equals(type)) return;

            m_currentType = type;
            CreateDisplay();
        }

        /// <summary>
        /// Opens a file onto the main 2D UI panel
        /// </summary>
        /// <param name="contentIndex"></param>
        /// <param name="file"></param>
        public void OpenFile(string file)
        {
            ContentsManager.Instance.OpenContentFileUsing2DScreen(file);
        }

        /// <summary>
        /// Clear current display
        /// </summary>
        private void ClearDisplay()
        {
            for(int i = 0; i < m_filesCreated.Count; i++)
            {
                Destroy(m_filesCreated[i]);
            }

            m_filesCreated.Clear();
        }

        //create new display
        public void CreateDisplay()
        {
            if (!gameObject.activeInHierarchy) return;

            if (m_process != null)
            {
                StopCoroutine(m_process);
            }

            ClearDisplay();

            m_disablePageOperations = true;
            m_process = StartCoroutine(ProcessDisplay());
        }

        /// <summary>
        /// Process the display request
        /// </summary>
        /// <returns></returns>
        private IEnumerator ProcessDisplay()
        {
            yield return new WaitForEndOfFrame();
            m_filteredFiles.Clear();

            switch (m_currentType)
            {
                case ContentsManager.ContentType.All:
                    m_filteredFiles.AddRange(ContentsManager.Instance.GetFileInfo("All"));
                    break;
                default:
                    m_filteredFiles.AddRange(ContentsManager.Instance.GetFileInfo(m_currentType.ToString()));
                    break;
            }

            //need to check the page count is correct
            if (m_firstIndex + messagesPerPage < m_filteredFiles.Count)
            {
                m_lastIndex = m_firstIndex + messagesPerPage;
            }
            else
            {
                m_lastIndex = m_filteredFiles.Count;
            }

            if (m_firstIndex >= m_lastIndex)
            {
                if (m_lastIndex - messagesPerPage >= 0)
                {
                    m_firstIndex = m_lastIndex - messagesPerPage;
                }
                else
                {
                    m_firstIndex = 0;
                }
            }

            //create the display
            if (m_ascendingOrder)
            {
                for (int i = m_firstIndex; i <= m_lastIndex; i++)
                {
                    if (i < m_filteredFiles.Count)
                    {
                        m_filesCreated.Add(CreateContentFile(m_filteredFiles[i]));
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                for (int i = m_lastIndex; i >= m_firstIndex; i++)
                {
                    if (i < m_filteredFiles.Count)
                    {
                        m_filesCreated.Add(CreateContentFile(m_filteredFiles[i]));
                    }
                    else
                    {
                        break;
                    }
                }
            }

            //update page display
            UpdatePageDisplay();
            m_disablePageOperations = false;

            yield return null;
        }

        /// <summary>
        /// Action called to instantiate the message GO
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private GameObject CreateContentFile(ContentsManager.ContentFileInfo fileInfo)
        {
            GameObject go = Instantiate(filePrefab, Vector3.zero, Quaternion.identity, container);
            go.transform.localScale = Vector3.one;
            go.SetActive(true);
            go.name = "ContentFileInfo_" + m_filteredFiles.IndexOf(fileInfo).ToString();

            ContentsManager.ContentType contentEnum = (ContentsManager.ContentType)fileInfo.extensiontype + 1;
            go.GetComponentInChildren<ContentFile>(true).Set(fileInfo, ContentsManager.Instance.GetLogTypeIcon(contentEnum));

            return go;
        }

        /// <summary>
        /// Called to refresh the display
        /// </summary>
        public void Refresh()
        {
            ContentsAPI.Instance.GetContents();
        }

        /// <summary>
        /// Called to create the next pool of logs
        /// </summary>
        public void NextPage()
        {
            if (m_lastIndex >= m_filteredFiles.Count || m_disablePageOperations) return;

            m_firstIndex += messagesPerPage;

            if (m_lastIndex + messagesPerPage < m_filteredFiles.Count)
            {
                m_lastIndex += messagesPerPage;
            }
            else
            {
                m_lastIndex = m_filteredFiles.Count;
            }

            m_page++;

            CreateDisplay();
        }

        /// <summary>
        /// Called to create the previous pool of logs
        /// </summary>
        public void PreviousPage()
        {
            if (m_firstIndex <= 0 || m_disablePageOperations) return;

            if (m_firstIndex - messagesPerPage > 0)
            {
                m_firstIndex -= messagesPerPage;
            }
            else
            {
                m_firstIndex = 0;
            }

            m_lastIndex = m_firstIndex + messagesPerPage;
            m_page--;

            CreateDisplay();
        }

        /// <summary>
        /// Called to update the page display
        /// </summary>
        private void UpdatePageDisplay()
        {
            countDisplayMin.text = (m_firstIndex <= 0) ? 1.ToString() : m_firstIndex.ToString();
            countDisplayMax.text = m_lastIndex.ToString();

            countDisplayTotal.text = m_filteredFiles.Count.ToString();
            pageDisplayTotal.text = m_page.ToString();
        }

        public void SortAcending()
        {

        }

        public void SortDescending()
        {

        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ContentsPanel), true)]
        public class ContentsPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("container"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("filePrefab"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tabFiles"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messagesPerPage"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("countDisplayMin"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("countDisplayMax"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("countDisplayTotal"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pageDisplayTotal"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("titleText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("toggleHolderLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("footerHolderLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pageTotalHolderLayout"), true);

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
