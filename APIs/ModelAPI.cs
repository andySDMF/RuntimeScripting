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
    public class ModelAPI : Singleton<ModelAPI>
    {
        public static ModelAPI Instance
        {
            get
            {
                return ((ModelAPI)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Endpoint")]
        [SerializeField]
        private string modelEndpoint = "/model_files/";

        public void GetModels(string collection = "", System.Action<List<ModelJson>> callback = null)
        {
            if (CoreManager.Instance.IsOffline) return;

            StartCoroutine(ProcessGet(collection, callback));
        }

        public void PostModel(ModelJson model, System.Action<bool, ModelJson> callback = null)
        {
            if (CoreManager.Instance.IsOffline) return;

            StartCoroutine(ProcessUpload(model, callback));
        }

        public void DeleteModel(int modelID, System.Action<bool> callback = null)
        {
            if (CoreManager.Instance.IsOffline) return;

            StartCoroutine(ProcessDelete(modelID, callback));
        }

        private IEnumerator ProcessGet(string collection, System.Action<List<ModelJson>> callback = null)
        {
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var project = CoreManager.Instance.ProjectID;

            var uri = host + APIPATH + modelEndpoint + "?project=" + project;

            Debug.Log("Request GET models method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error GET models: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        ProcessResponse(request.downloadHandler.text, collection, callback);
                    }
                }

                request.Dispose();
            }
        }

        private IEnumerator ProcessUpload(ModelJson json, System.Action<bool, ModelJson> callback)
        {
            //data
            var jsonEntry = JsonUtility.ToJson(json);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + modelEndpoint;

            Debug.Log("Request POST model method: uri= " + uri + "::" + jsonEntry);

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
                    Debug.Log("Error POST model: " + request.error);

                    if (callback != null)
                    {
                        callback.Invoke(request.result == UnityWebRequest.Result.Success, null);
                    }
                }
                else
                {
                    if (request.responseCode == 200)
                    {

                        ModelJson model = CreateModelJsonFromData(request.downloadHandler.text);

                        if (callback != null)
                        {
                            callback.Invoke(request.result == UnityWebRequest.Result.Success, model);
                        }
                    }
                    else
                    {
                        if (callback != null)
                        {
                            callback.Invoke(true, null);
                        }
                    }
                }

                request.Dispose();
            }
        }

        private IEnumerator ProcessDelete(int modelID, System.Action<bool> callback = null)
        {
            //data
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + modelEndpoint + modelID.ToString();

            Debug.Log("Request DELETE model method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Delete(uri))
            {
                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error DELETE model: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {

                    }
                }

                if (callback != null)
                {
                    callback.Invoke(request.result == UnityWebRequest.Result.Success);
                }

                request.Dispose();
            }
        }

        private void ProcessResponse(string json, string collection, System.Action<List<ModelJson>> callback = null)
        {
            Debug.Log("Processing models responce: " + json);

            // check for empty json array
            if (json == "[]")
            {
                return;
            }

            //need to break down json and push to noticeboards
            //will also need to look at the the json that comes back for expired

            //need to break down json and push to reports
            var data = new JSONObject(json);
            List<ModelJson> models = new List<ModelJson>();

            foreach (JSONObject obj in data.list)
            {
                if(!string.IsNullOrEmpty(collection))
                {
                    if (!obj.GetField("collection").stringValue.Equals(collection))
                    {
                        continue;
                    }
                }

                var nMeta = new ModelJson();

                nMeta.id = obj.GetField("id").intValue;
                nMeta.project = obj.GetField("project").stringValue;
                nMeta.collection = obj.GetField("collection").stringValue;
                nMeta.filename = obj.GetField("filename").stringValue;
                nMeta.url = obj.GetField("url").stringValue;

                models.Add(nMeta);
            }

            if (callback != null)
            {
                callback.Invoke(models);
            }
        }

        public ModelJson CreateModelJsonFromData(string data)
        {
            var nMeta = new ModelJson();
            var obj = new JSONObject(data);

            nMeta.id = obj.GetField("id").intValue;
            nMeta.project = obj.GetField("project").stringValue;
            nMeta.collection = obj.GetField("collection").stringValue;
            nMeta.filename = obj.GetField("filename").stringValue;
            nMeta.url = obj.GetField("url").stringValue;

            return nMeta;
        }

        [System.Serializable]
        public class ModelJson
        {
            public int id = -1;

            public string project;
            public string collection;
            public string filename;
            public string url;
        }

        [System.Serializable]
        public class GLBUploadRequest
        {
            public bool RequestModelUpload = true;
            public string project = "";
            public string collection = "";
        }

        [System.Serializable]
        public class GLBUploadResponse
        {
            public string ModelFileUrl;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ModelAPI), true)]
        public class ModelAPI_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("modelEndpoint"), true);

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
