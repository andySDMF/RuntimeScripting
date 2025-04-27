using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ConsoleLog : MonoBehaviour
    {
        [Header("Messages")]
        [SerializeField]
        private bool displayDate = false;
        [SerializeField]
        private bool displayTime = true;
        [SerializeField]
        private Transform container;
        [SerializeField]
        private GameObject logPrefab;

        [Header("Pages")]
        [SerializeField]
        [Range(10, 40)]
        private int messagesPerPage = 20;
        [SerializeField]
        private TextMeshProUGUI logCountDisplayMin;
        [SerializeField]
        private TextMeshProUGUI logCountDisplayMax;
        [SerializeField]
        private TextMeshProUGUI logCountDisplayTotal;
        [SerializeField]
        private TextMeshProUGUI pageDisplayTotal;

        [Header("Log Icons")]
        [SerializeField]
        private List<DebugLogTypeIcon> icons;

        private List<DebugMessage> m_logs = new List<DebugMessage>();
        private List<GameObject> m_logsCreated = new List<GameObject>();
        private List<LogType> m_logsOpen = new List<LogType>();

        private List<DebugMessage> m_filteredLogs = new List<DebugMessage>();

        private int m_firstIndex = 0;
        private int m_lastIndex = 0;
        private int m_page = 1;

        private Coroutine m_process;
        private bool m_ascendingOrder = true;
        private bool m_disablePageOperations = false;

        public List<DebugMessage> AllLogs
        {
            get
            {
                return m_logs;
            }
        }

        private void Awake()
        {
            //add logtype filters
           /* m_logsOpen.Add(LogType.Assert);
            m_logsOpen.Add(LogType.Error);
            m_logsOpen.Add(LogType.Exception);
            m_logsOpen.Add(LogType.Log);
            m_logsOpen.Add(LogType.Warning);*/

            //set filter toggles
            foreach(ConsoleLogType logType in GetComponentsInChildren<ConsoleLogType>(true))
            {
                switch(logType.Type)
                {
                    case LogType.Assert:

                        if(AppManager.Instance.Settings.projectSettings.showAssert)
                        {
                            m_logsOpen.Add(LogType.Assert);
                            logType.Set(true);
                        }
                        else
                        {
                            logType.Set(false);
                        }
                        break;
                    case LogType.Error:

                        if (AppManager.Instance.Settings.projectSettings.showErrors)
                        {
                            m_logsOpen.Add(LogType.Error);
                            logType.Set(true);
                        }
                        else
                        {
                            logType.Set(false);
                        }
                        break;
                    case LogType.Exception:

                        if (AppManager.Instance.Settings.projectSettings.showExceptions)
                        {
                            m_logsOpen.Add(LogType.Exception);
                            logType.Set(true);
                        }
                        else
                        {
                            logType.Set(false);
                        }
                        break;
                    case LogType.Log:

                        if (AppManager.Instance.Settings.projectSettings.showLogs)
                        {
                            m_logsOpen.Add(LogType.Log);
                            logType.Set(true);
                        }
                        else
                        {
                            logType.Set(false);
                        }
                        break;
                    case LogType.Warning:

                        if (AppManager.Instance.Settings.projectSettings.showWarning)
                        {
                            m_logsOpen.Add(LogType.Warning);
                            logType.Set(true);
                        }
                        else
                        {
                            logType.Set(false);
                        }
                        break;
                    default:
                        break;


                }
            }

           // GetComponentsInChildren<ConsoleLogType>(true).ToList().ForEach(x => x.Set());

            //display
            m_lastIndex = messagesPerPage;
            UpdatePageDisplay();

#if UNITY_EDITOR
            //add logs for editor testing
          //  StartCoroutine(AdminTest());
#endif
        }

        /// <summary>
        /// Editor action to create test logging
        /// </summary>
        /// <returns></returns>
        private IEnumerator AdminTest()
        {
            yield return new WaitForSeconds(1.0f);

            Debug.LogError("This is a test error");
            Debug.LogError("This is a test error");
            Debug.LogError("This is a test error");
            Debug.LogWarning("This is a test warning");
            Debug.LogWarning("This is a test warning");
            Debug.LogWarning("This is a test warning");
            Debug.LogWarning("This is a test warning");
        }

        private void OnEnable()
        {
            //create
            CreateDisplay();
        }

        private void OnDisable()
        {
            //clear
            if (m_process != null)
            {
                StopCoroutine(m_process);
            }

            m_process = null;

            ClearDisplay();
        }

        /// <summary>
        /// Action called to create the display of log messages
        /// </summary>
        private void CreateDisplay()
        {
            if(m_process != null)
            {
                StopCoroutine(m_process);
            }

            ClearDisplay();

            m_disablePageOperations = true;
            m_process = StartCoroutine(ProcessDisplay());
        }

        /// <summary>
        /// Action called to clear the current display of log messages
        /// </summary>
        private void ClearDisplay()
        {
            for(int i = 0; i < m_logsCreated.Count; i++)
            {
                Destroy(m_logsCreated[i]);
            }

            m_logsCreated.Clear();
        }

        /// <summary>
        /// Coroutine to create the messages based on the filtered logs
        /// </summary>
        /// <returns></returns>
        private IEnumerator ProcessDisplay()
        {
            yield return new WaitForEndOfFrame();

            m_filteredLogs.Clear();

            //need to find all the logs that match the m_logsOpen list
            for (int i = 0; i < m_logs.Count; i++)
            {
                if (m_logsOpen.Contains(m_logs[i].logType))
                {
                    m_filteredLogs.Add(m_logs[i]);
                }
            }

            //need to check the page count is correct
            if (m_firstIndex + messagesPerPage < m_filteredLogs.Count)
            {
                m_lastIndex = m_firstIndex + messagesPerPage;
            }
            else
            {
                m_lastIndex = m_filteredLogs.Count;
            }

            if(m_firstIndex >= m_lastIndex)
            {
                if(m_lastIndex - messagesPerPage >= 0)
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
                    if (i < m_filteredLogs.Count)
                    {
                        m_logsCreated.Add(CreateLogMessage(m_filteredLogs[i]));
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
                    if (i < m_filteredLogs.Count)
                    {
                        m_logsCreated.Add(CreateLogMessage(m_filteredLogs[i]));
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
        private GameObject CreateLogMessage(DebugMessage message)
        {
            GameObject go = Instantiate(logPrefab, Vector3.zero, Quaternion.identity, container);
            go.transform.localScale = Vector3.one;
            go.SetActive(true);
            go.name = "DebugMessage_" + m_logs.IndexOf(message).ToString();

            go.GetComponentInChildren<LogMessage>(true).Set(GetLogTypeIcon(message.logType), message.date + message.time + message.logType, message.log, message.stackTrace);

            return go;
        }

        /// <summary>
        /// Returns the icon used for a message
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private Sprite GetLogTypeIcon(LogType type)
        {
            return icons.FirstOrDefault(x => x.logType.Equals(type)).icon;
        }

        /// <summary>
        /// Called to add log type to the filters
        /// </summary>
        /// <param name="type"></param>
        public void AddLogTypeFilter(LogType type)
        {
            if(!m_logsOpen.Contains(type))
            {
                m_logsOpen.Add(type);
            }

            m_page = 1;
            m_lastIndex = messagesPerPage;
            m_firstIndex = 0;
            CreateDisplay();
        }

        /// <summary>
        /// Called to remove log type from the filters
        /// </summary>
        /// <param name="type"></param>
        public void RemoveLogTypeFilter(LogType type)
        {
            if (m_logsOpen.Contains(type))
            {
                m_logsOpen.Remove(type);
            }

            m_page = 1;
            m_lastIndex = messagesPerPage;
            m_firstIndex = 0;
            CreateDisplay();
        }

        /// <summary>
        /// Called to create the next pool of logs
        /// </summary>
        public void NextPage()
        {
            if (m_lastIndex >= m_filteredLogs.Count || m_disablePageOperations) return;

            m_firstIndex += messagesPerPage;

            if (m_lastIndex + messagesPerPage < m_filteredLogs.Count)
            {
                m_lastIndex += messagesPerPage;
            }
            else
            {
                m_lastIndex = m_filteredLogs.Count;
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
            logCountDisplayMin.text = (m_firstIndex <= 0) ? 1.ToString() : m_firstIndex.ToString();
            logCountDisplayMax.text = m_lastIndex.ToString();

            logCountDisplayTotal.text = m_filteredLogs.Count.ToString();
            pageDisplayTotal.text = m_page.ToString();
        }

        public void SortAcending()
        {

        }

        public void SortDescending()
        {

        }

        /// <summary>
        /// Action called to add a internal log to the console log display
        /// </summary>
        /// <param name="logString"></param>
        /// <param name="stackTrace"></param>
        /// <param name="type"></param>
        public void HandleDebugLog(string logString, string stackTrace, LogType type)
        {
            if (this == null) return;

            string date = "";
            string time = "";

            if (displayDate)
            {
                date = System.DateTime.Now.ToString("MM/dd/yyyy") + " ";
            }

            if (displayTime)
            {
                time = System.DateTime.Now.ToString("HH:mm") + " ";
            }

            string log = "";

            if (logString.Length > 1000)
            {
                log = logString.Substring(0, 1000) + "........";
            }
            else
            {
                log = logString;
            }

            DebugMessage message = new DebugMessage(type, date, time, log, "<color=#BCBCBC>" + stackTrace + "</color>");
            m_logs.Add(message);

            if (AppManager.Instance.Settings.projectSettings.individualOutLogsUsed && AppManager.IsCreated)
            {
                RequestCreateBrowserLog(log + " | Stack: " + stackTrace);
            }

            if (Application.isPlaying)
            {
                //need to check if gameobject is open
                if (gameObject != null && gameObject.activeInHierarchy)
                {
                    if (m_logsOpen.Contains(type))
                    {
                        if (m_filteredLogs.Count < messagesPerPage)
                        {
                            m_logsCreated.Add(CreateLogMessage(message));
                        }

                        m_filteredLogs.Add(message);
                        UpdatePageDisplay();
                    }
                }
            }
        }

        public void RequestCreateBrowserLog(string logString)
        {
            var request = new BrowserLogRequest();
            request.consoleLog = logString;
            var json = JsonUtility.ToJson(request);

            if(AppManager.IsCreated)
            {
                WebclientManager.Instance.Send(json);
            }
        }

        [System.Serializable]
        private class DebugLogTypeIcon
        {
            public LogType logType;
            public Sprite icon;
        }

        [System.Serializable]
        public class DebugMessage
        {
            public LogType logType;
            public string date = "";
            public string time = "";
            public string log;
            public string stackTrace;

            public DebugMessage(LogType type, string date, string time, string log, string stackTrace)
            {
                logType = type;
                this.date = date;
                this.time = time;
                this.log = log;
                this.stackTrace = stackTrace;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ConsoleLog), true)]
        public class ConsoleLog_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("displayDate"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("displayTime"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("container"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("logPrefab"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messagesPerPage"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("logCountDisplayMin"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("logCountDisplayMax"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("logCountDisplayTotal"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pageDisplayTotal"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("icons"), true);

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
