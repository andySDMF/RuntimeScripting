using Defective.JSON;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ReportAPI : Singleton<ReportAPI>
    {
        public static ReportAPI Instance
        {
            get
            {
                return ((ReportAPI)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Endpoint")]
        [SerializeField]
        private string reportEndpoint = "/reports/";

        public void GetReports()
        {
            if (CoreManager.Instance.IsOffline) return;

            StartCoroutine(ProcessGet());
        }

        public void PostReport(string id, string subject, string comment, System.Action<bool> callback = null)
        {
            if (CoreManager.Instance.IsOffline) return;

            StartCoroutine(ProcessPost(id, subject, comment, callback));
        }

        public void UpdateReport(int reportID, string id, string subject, string comment, bool resolved, System.Action<bool> callback = null)
        {
            if (CoreManager.Instance.IsOffline) return;

            StartCoroutine(ProcessUpdate(reportID, id, subject, comment, resolved, callback));
        }

        public void DeleteReport(int reportID, System.Action<bool> callback = null)
        {
            if (CoreManager.Instance.IsOffline) return;

            StartCoroutine(ProcessDelete(reportID, callback));
        }

        private IEnumerator ProcessGet()
        {
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var project = CoreManager.Instance.ProjectID;

            var uri = host + APIPATH + reportEndpoint + "?project=" + project;

            Debug.Log("Request GET reports method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error GET reports: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        ProcessResponse(request.downloadHandler.text);
                    }
                }

                request.Dispose();
            }
        }

        private IEnumerator ProcessPost(string id, string subject, string comment, System.Action<bool> callback)
        {
            //data
            ReportJson report = new ReportJson();
            report.project = CoreManager.Instance.ProjectID;
            report.subject = subject;
            report.comment = comment;
            report.unique_id = id;

            var jsonEntry = JsonUtility.ToJson(report);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + reportEndpoint;

            Debug.Log("Request POST report method: uri= " + uri + "::" + jsonEntry);

            using (UnityWebRequest request = UnityWebRequest.PostWwwForm(uri, UnityWebRequest.kHttpVerbPOST))
            {
                request.SetRequestHeader("Content-Type", "application/json");

                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);

                request.uploadHandler = new UploadHandlerRaw(jsonBytes);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error POST report: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        RPCGet();
                    }
                }

                if (callback != null)
                {
                    callback.Invoke(request.result == UnityWebRequest.Result.Success);
                }

                request.Dispose();
            }
        }

        private IEnumerator ProcessUpdate(int reportID, string id, string subject, string comment, bool resolved, System.Action<bool> callback)
        {
            //data
            ReportJson report = new ReportJson();
            report.project = CoreManager.Instance.ProjectID;
            report.id = reportID;
            report.subject = subject;
            report.comment = comment;
            report.unique_id = id;
            report.resolved = resolved;

            var jsonEntry = JsonUtility.ToJson(report);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);

            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + reportEndpoint + reportID.ToString();

            Debug.Log("Request PUT report method: uri= " + uri + "::" + jsonEntry);

            using (UnityWebRequest request = UnityWebRequest.Put(uri, UnityWebRequest.kHttpVerbPOST))
            {
                request.SetRequestHeader("Content-Type", "application/json");

                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);

                request.uploadHandler = new UploadHandlerRaw(jsonBytes);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error POST report: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        RPCGet();
                    }
                }

                if (callback != null)
                {
                    callback.Invoke(request.result == UnityWebRequest.Result.Success);
                }

                request.Dispose();
            }
        }

        private IEnumerator ProcessDelete(int reportID, System.Action<bool> callback = null)
        {
            //data
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + reportEndpoint + reportID.ToString();

            Debug.Log("Request DELETE report method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Delete(uri))
            {
                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error DELETE report: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        RPCGet();
                    }
                }

                if (callback != null)
                {
                    callback.Invoke(request.result == UnityWebRequest.Result.Success);
                }

                request.Dispose();
            }
        }

        private void RPCGet()
        {
            //RPC to everyone to process GET again
            MMOManager.Instance.SendRPC("GetReports", (int)MMOManager.RpcTarget.All);
        }

        private void ProcessResponse(string json)
        {
            Debug.Log("Processing reports responce: " + json);

            // check for empty json array
            if (json == "[]")
            {
                return;
            }

            //need to break down json and push to reports
            var data = new JSONObject(json);
            List<ReportAPI.ReportJson> reports = new List<ReportJson>();

            foreach (JSONObject obj in data.list)
            {
                var rpMeta = new ReportJson();

                rpMeta.id = obj.GetField("id").intValue;
                rpMeta.unique_id = obj.GetField("unique_id").stringValue;
                rpMeta.project = obj.GetField("project").stringValue;
                rpMeta.comment = obj.GetField("comment").stringValue;
                rpMeta.resolved = obj.GetField("resolved").boolValue;

                reports.Add(rpMeta);
            }

            ReportManager.Instance.AddReports(reports);
        }

        [System.Serializable]
        public class ReportJson
        {
            public int id = -1;
            public string unique_id;
            public string project;
            public string subject;
            public string comment;
            public bool resolved = false;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ReportAPI), true)]
        public class ReportAPI_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("reportEndpoint"), true);

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
