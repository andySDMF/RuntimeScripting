using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Defective.JSON;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// DataAPI is used to store any project specific data for a project. 
    /// - The idea is for some data to be serialized in a json string and stored in the 'data' field 
    ///      So that the db is generic enough for many use cases among different projects.
    /// </summary>
    public class DataAPI : Singleton<DataAPI>
    {
        [Header("Endpoint")]
        public string Endpoint = "/data";

        public Action<List<DataObject>> DataListener;

        public static DataAPI Instance
        {
            get
            {
                return ((DataAPI)instance);
            }
            set
            {
                instance = value;
            }
        }

        private bool m_COREEXISTS = false;

        private void Awake()
        {
            m_COREEXISTS = FindFirstObjectByType<CoreManager>() != null;
        }

        public void Get(string uniqueId)
        {
            if(m_COREEXISTS)
            {
                if (!CoreManager.Instance.projectSettings.useDataAPI) return;
            }

            StartCoroutine(get(uniqueId));
        }

        public void GetAll(string project)
        {
            if (m_COREEXISTS)
            {
                if (!CoreManager.Instance.projectSettings.useDataAPI) return;
            }

            StartCoroutine(getAll(project));
        }

        public void Insert(string project, string uniqueId, string dataType, string data)
        {
            if (m_COREEXISTS)
            {
                if (!CoreManager.Instance.projectSettings.useDataAPI) return;
            }

            StartCoroutine(insert(project, uniqueId, dataType, data));
        }

        public void UpdateData(string project, string data, string dataType, string uniqueId)
        {
            if (m_COREEXISTS)
            {
                if (!CoreManager.Instance.projectSettings.useDataAPI) return;
            }

            StartCoroutine(updateData(project, data, dataType, uniqueId));
        }

        public void Delete(string uniqueId)
        {
            if (m_COREEXISTS)
            {
                if (!CoreManager.Instance.projectSettings.useDataAPI) return;
            }

            StartCoroutine(delete(uniqueId));
        }

        private void processDataResults(string json)
        {
            Debug.Log("Processing data responce: " + json);

            // check for empty json array
            if (json == "[]")
            {
                // invoke callback to be handled per project
                if (DataListener != null)
                {
                    DataListener.Invoke(new List<DataObject>());
                }

                return;
            }

            // collect db entries
            var dataList = new List<DataObject>();
            var data = new JSONObject(json);

            if (data != null)
            {
                foreach (JSONObject obj in data.list)
                {
                    var dataObject = new DataObject();
                    dataObject.uniqueId = obj.GetField("unique_id").stringValue;
                    dataObject.dataType = obj.GetField("data_type").stringValue;
                    dataObject.project = obj.GetField("project").stringValue;
                    dataObject.data = obj.GetField("data").stringValue;
                    dataObject.created_at = obj.GetField("created_at").stringValue;
                    dataObject.updated_at = obj.GetField("updated_at").stringValue;

                    dataList.Add(dataObject);
                }

                // invoke callback to be handled per project
                if (DataListener != null)
                {
                    DataListener.Invoke(dataList);
                }
            }

            if(m_COREEXISTS)
            {
                DataManager.Instance.SetDataObjects(dataList);

            }
        }

        #region Http Requests

        private IEnumerator get(string uniqueId)
        {
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + Endpoint + "/" + uniqueId;

            Debug.Log("Request GET data method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                request.SetRequestHeader("Content-Type", "application/json");
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error GET data: " + request.error);

                    if (DataListener != null)
                    {
                        DataListener.Invoke(new List<DataObject>());
                    }
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        var response = JsonUtility.FromJson<DataObject>(request.downloadHandler.text);
                        var responseList = new List<DataObject>();
                        responseList.Add(response);

                        if(DataListener != null)
                        {
                            DataListener.Invoke(responseList);
                        }
                    }
                }

                request.Dispose();
            }
        }

        private IEnumerator getAll(string project)
        {
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + Endpoint + "?pid=" + project;

            Debug.Log("Request GET data method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                request.SetRequestHeader("Content-Type", "application/json");
                request.downloadHandler = new DownloadHandlerBuffer();

                request.disposeUploadHandlerOnDispose = true;
                request.disposeDownloadHandlerOnDispose = true;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error GET data: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        processDataResults(request.downloadHandler.text);
                    }
                }

                request.Dispose();
            }
        }

        private IEnumerator updateData(string project, string data, string dataType, string uniqueId)
        {
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + Endpoint + "/" + uniqueId;

            var entry = new DataUpdateEntry(uniqueId, project, dataType, data);
            string jsonEntry = JsonUtility.ToJson(entry);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);

            Debug.Log("Request PUT data method: uri= " + uri + ":: JSON= " + jsonEntry);

            using (UnityWebRequest request = UnityWebRequest.Put(uri, jsonBytes))
            {
                request.SetRequestHeader("Content-Type", "application/json");
                request.downloadHandler = new DownloadHandlerBuffer();

                request.disposeUploadHandlerOnDispose = true;
                request.disposeDownloadHandlerOnDispose = true;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error PUT data: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        Debug.Log("PUT data success:" + request.downloadHandler.text);
                    }
                }

                request.Dispose();
            }
        }

        private IEnumerator insert(string project, string uniqueId, string dataType, string data)
        {
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + Endpoint + "/";

            var entry = new DataEntry(project, uniqueId, dataType, data);
            string jsonEntry = JsonUtility.ToJson(entry);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);

            Debug.Log("Request PUT data method: uri= " + uri + ":: JSON= " + jsonEntry);

            using (UnityWebRequest request = UnityWebRequest.PostWwwForm(uri, UnityWebRequest.kHttpVerbPOST))
            {
                request.SetRequestHeader("Content-Type", "application/json");

                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);

                request.uploadHandler = new UploadHandlerRaw(jsonBytes);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.disposeUploadHandlerOnDispose = true;
                request.disposeDownloadHandlerOnDispose = true;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error PUT data: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        Debug.Log("PUT data success:" + request.downloadHandler.text);
                    }
                }

                request.Dispose();
            }
        }

        private IEnumerator delete(string uniqueId)
        {
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + Endpoint + "/" + uniqueId.ToString();

            Debug.Log("Request DELETE data method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Delete(uri))
            {
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.disposeUploadHandlerOnDispose = true;
                request.disposeDownloadHandlerOnDispose = true;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error DELETE data: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        Debug.Log("DELETE data:" + request.downloadHandler.text);
                    }
                }

                request.Dispose();
            }
        }

        #endregion

#if UNITY_EDITOR
        [CustomEditor(typeof(DataAPI), true)]
        public class DataAPI_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Endpoint"), true);

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

#region Data Structures

/// <summary>
/// Anything that uses Data API must implement this to attain its data
/// </summary>
public interface IDataAPICallback
{
    void DataAPICallback(List<DataObject> objs);
}

[System.Serializable]
public class DataObject
{
    public string uniqueId;
    public string project;
    //stores data type
    public string dataType;
    public string data;
    public string created_at;
    public string updated_at;
}

[Serializable]
public class DataEntry
{
    public string unique_id;
    public string project;
    public string data;
    public string data_type;

    public DataEntry(string project, string uniqueId, string dataType, string data)
    {
        this.project = project;
        this.data = data;
        this.unique_id = uniqueId;
        this.data_type = dataType;
    }
}

[Serializable]
public struct DataUpdateEntry
{
    public string project;
    public string uniqueId;
    //stores data type
    public string dataType;
    public string data;

    public DataUpdateEntry(string uniqueId, string project, string dataType, string data)
    {
        this.uniqueId = uniqueId;
        this.project = project;
        this.data = data;
        this.dataType = dataType;
    }
}

#endregion