using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Defective.JSON;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ContentsAPI : Singleton<ContentsAPI>
    {
        public static ContentsAPI Instance
        {
            get
            {
                return ((ContentsAPI)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Endpoint")]
        [SerializeField]
        private string contentsEndpoint = "/contents/";

        public System.Action OnGetContentsCallback { get; set; }

        /// <summary>
        /// Action called to request all contents within the DB
        /// </summary>
        public void GetContents()
        {
            if (CoreManager.Instance.projectSettings.useContentsAPI)
            {
                if (CoreManager.Instance.IsOffline && !CoreManager.Instance.projectSettings.syncContentDisplayOffline) return;

                StartCoroutine(GetContentsRequest());
            }
        }

        /// <summary>
        /// Action called to upload content to DB
        /// </summary>
        /// <param name="url"></param>
        /// <param name="filetype"></param>
        /// <param name="content_index"></param>
        /// <param name="uploadedBy"></param>
        public void PostContents(string url, string filetype, string content_index, string uploadedBy)
        {
            if (CoreManager.Instance.projectSettings.useContentsAPI)
            {
                if (CoreManager.Instance.IsOffline) return;

                //create entry
                ContentEntry entry = new ContentEntry();
                entry.url = url;
                entry.filetype = filetype;
                entry.content_index = content_index;
                entry.project = CoreManager.Instance.ProjectID;
                entry.uploaded_by = uploadedBy;

                StartCoroutine(PostContentsRequest(entry));
            }
        }

        /// <summary>
        /// Action called to delete content files from the DB/AWS
        /// </summary>
        /// <param name="contentsID"></param>
        public void DeleteContents(int[] contentsID)
        {
            if (CoreManager.Instance.projectSettings.useContentsAPI)
            {
                if (CoreManager.Instance.IsOffline) return;

                for (int i = 0; i < contentsID.Length; i++)
                {
                    if (i < contentsID.Length - 1)
                    {
                        StartCoroutine(DeleteContentsRequest(contentsID[i], false));
                    }
                    else
                    {
                        StartCoroutine(DeleteContentsRequest(contentsID[i]));
                    }
                }
            }
        }

        /// <summary>
        /// Action called to update a current file on DB/AWS
        /// </summary>
        /// <param name="url"></param>
        /// <param name="filetype"></param>
        /// <param name="content_index"></param>
        /// <param name="uploadedBy"></param>
        /// <param name="contentID"></param>
        public void UpdateContents(string url, string filetype, string content_index, string uploadedBy, int contentID)
        {
            if (CoreManager.Instance.projectSettings.useContentsAPI)
            {
                if (CoreManager.Instance.IsOffline) return;

                //create entry
                ContentEntry entry = new ContentEntry();
                entry.url = url;
                entry.filetype = filetype;
                entry.content_index = content_index;
                entry.project = CoreManager.Instance.ProjectID;
                entry.uploaded_by = uploadedBy;

                StartCoroutine(UpdateContentRequest(entry, contentID));
            }
        }

        /// <summary>
        /// Process the contents web request
        /// </summary>
        /// <returns></returns>
        private IEnumerator GetContentsRequest()
        {
            //data
            if (!AppManager.IsCreated) yield break;

            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var project = CoreManager.Instance.ProjectID;

            var uri = host + APIPATH + contentsEndpoint + "?project=" + project;

            Debug.Log("Request GET contents method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error GET contents: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        ProcessContentData(request.downloadHandler.text);
                    }
                }

                request.Dispose();
            }
        }

        /// <summary>
        /// Process the upload content request
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        private IEnumerator PostContentsRequest(ContentEntry entry)
        {
            //data
            var jsonEntry = JsonUtility.ToJson(entry);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + contentsEndpoint;

            Debug.Log("Request POST contents method: uri= " + uri + "::" + jsonEntry);

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
                    Debug.Log("Error POST contents: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        SendRPCGetUpdates();
                    }
                }

                request.Dispose();
            }
        }

        /// <summary>
        /// Process the delete content request
        /// </summary>
        /// <param name="contentID"></param>
        /// <returns></returns>
        private IEnumerator DeleteContentsRequest(int contentID, bool invokeAction = true)
        {
            //data
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + contentsEndpoint + contentID.ToString();

            Debug.Log("Request DELETE contents method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Delete(uri))
            {
                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error DELETE contents: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        //send RPC to all to getupdated contents
                        if(invokeAction) SendRPCGetUpdates();
                    }
                }

                request.Dispose();
            }
        }

        /// <summary>
        /// Process the update content request
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="contentID"></param>
        /// <returns></returns>
        private IEnumerator UpdateContentRequest(ContentEntry entry, int contentID)
        {
            //data
            var jsonEntry = JsonUtility.ToJson(entry);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + contentsEndpoint + contentID.ToString();

            Debug.Log("Request PUT contents method: uri= " + uri);

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
                    Debug.Log("Error PUT contents: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        //send RPC to all to getupdated contents
                        SendRPCGetUpdates();
                    }
                }

                request.Dispose();
            }
        }

        /// <summary>
        /// Send RPC to everyone to ensure their local contents is updated on UPLOAD/DELETE/UPDATE
        /// </summary>
        private void SendRPCGetUpdates()
        {
            MMOManager.Instance.SendRPC("GetContentUpdates", (int)MMOManager.RpcTarget.All);
        }

        /// <summary>
        /// Responce called when the GetContents() request is called 
        /// </summary>
        /// <param name="json"></param>
        private void ProcessContentData(string json)
        {
            Debug.Log("Processing contents responce: " + json);

            // check for empty json array
            if (json == "[]")
            {
                ContentsManager.Instance.UpdateContents(null);
                return;
            }

            // Extract the product data from json array (JsonUtility doesnt support deserializing array so had to manually extract)
            // Would be good to use a library that can actually deserialize it from the productMeta structure. When this is a plugin
            // we could probably use Newtonsoft.json and set it as a package dependancy. But for now I added a json library manually 
            // to the folder to ensure there are no dependancies that have to be manually added for new projects.

            var data = new JSONObject(json);
            ContentEntries wrapper = new ContentEntries();

            foreach (JSONObject obj in data.list)
            {
                var contentMeta = new ContentEntry();
                contentMeta.ID = obj.GetField("id").intValue;
                contentMeta.url = obj.GetField("url").stringValue;
                contentMeta.filetype = obj.GetField("filetype").stringValue;
                contentMeta.project = obj.GetField("project").stringValue;
                contentMeta.content_index = obj.GetField("content_index").stringValue;
                contentMeta.uploaded_by = obj.GetField("uploaded_by").stringValue;

                wrapper.entries.Add(contentMeta);
            }

            ContentsManager.Instance.UpdateContents(wrapper);

            if(OnGetContentsCallback != null)
            {
                OnGetContentsCallback.Invoke();
            }
        }

        [System.Serializable]
        public class ContentEntry
        {
            public string url = "";
            public string filetype = "";
            public string project = "";
            public string content_index = "";
            public string uploaded_by = "";

            public int ID { get; set; }
        }

        [System.Serializable]
        public class ContentEntries
        {
            public List<ContentEntry> entries = new List<ContentEntry>();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ContentsAPI), true)]
        public class ContentsAPI_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("contentsEndpoint"), true);

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
