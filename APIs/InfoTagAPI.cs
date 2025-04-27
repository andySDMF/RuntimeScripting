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
    public class InfoTagAPI : Singleton<InfoTagAPI>
    {
        public static InfoTagAPI Instance
        {
            get
            {
                return ((InfoTagAPI)instance);
            }
            set
            {
                instance = value;
            }
        }
        
        [Header("Endpoint")]
        [SerializeField]
        private string infotagEndpoint = "/infotags/";

        public IEnumerator GetInfoTags(ProductPlacement.ProductPlacementObject prod, System.Action<ProductPlacement.ProductPlacementObject, List<InfotagJson>> callback)
        {
            if (CoreManager.Instance.IsOffline)
            {
                if (callback != null)
                {
                    callback.Invoke(prod, null);
                }

                yield break;
            }

            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var project = CoreManager.Instance.ProjectID;

            var uri = host + APIPATH + infotagEndpoint + "?project=" + project + "&sku=" + prod.productCode + "&shop=" + prod.shop;

            Debug.Log("Request GET infotag method: uri= " + uri);

            List<InfotagJson> temp = new List<InfotagJson>();

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error GET infotags: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        if(request.downloadHandler.text != "[]")
                        {
                            var data = new JSONObject(request.downloadHandler.text);
                            foreach (JSONObject obj in data.list)
                            {
                                var tagMeta = new InfotagJson();
                                tagMeta.id = obj.GetField("id").intValue;
                                tagMeta.sku = obj.GetField("sku").stringValue;
                                tagMeta.infotag_type = obj.GetField("infotag_type").stringValue;
                                tagMeta.name = obj.GetField("name").stringValue;
                                tagMeta.url = obj.GetField("url").stringValue;
                                tagMeta.project = obj.GetField("project").stringValue;

                                if (obj.HasField("shop"))
                                {
                                    if (string.IsNullOrEmpty(obj.GetField("shop").stringValue))
                                    {
                                        tagMeta.shop = "Brandlab";
                                    }
                                    else
                                    {
                                        tagMeta.shop = obj.GetField("shop").stringValue;
                                    }
                                }
                                else
                                {
                                    tagMeta.shop = "Brandlab";
                                }

                                temp.Add(tagMeta);
                            }
                        }
                    }
                }

                request.Dispose();
            }

            if(callback != null)
            {
                callback.Invoke(prod, temp);
            }
        }

        public IEnumerator AddInfoTag(InfotagJson entry)
        {
            if (CoreManager.Instance.IsOffline)
            {
                entry.id = -1;
                yield break;
            }

            //create new data object for info tags
            var jsonEntry = JsonUtility.ToJson(entry);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + infotagEndpoint;

            Debug.Log("Request POST infotag method: uri= " + uri + "::" + jsonEntry);

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
                    Debug.Log("Error POST product infotag: " + request.error);
                    entry.id = -1;
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        var data = new JSONObject(request.downloadHandler.text);

                        if (data != null)
                        {
                            entry.id = data.GetField("id").intValue;
                        }
                    }
                }

                request.Dispose();
            }
        }

        public void DeleteInfoTags(List<int> idvalues)
        {
            if (CoreManager.Instance.IsOffline) return;

            for (int i = 0; i < idvalues.Count; i++)
            {
               StartCoroutine(DeleteInfoTag(idvalues[i]));
            }
        }

        public IEnumerator DeleteInfoTag(int id)
        {
            if (CoreManager.Instance.IsOffline) yield break;

            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var project = CoreManager.Instance.ProjectID;

            var uri = host + APIPATH + infotagEndpoint + id.ToString();

            Debug.Log("Request DELETE infotag method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Delete(uri))
            {
                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error Delete infotag: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {

                    }
                }

                request.Dispose();
            }
        }

        public IEnumerator UpdateInfoTag(InfotagJson entry)
        {
            if (CoreManager.Instance.IsOffline) yield break;

            //create new data object for info tags
            var jsonEntry = JsonUtility.ToJson(entry);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + infotagEndpoint + entry.id.ToString();

            Debug.Log("Request PUT infotag method: uri= " + uri);

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
                    Debug.Log("Error Put infotag: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {

                    }
                }

                request.Dispose();
            }
        }

        [System.Serializable]
        public class InfotagJson
        {
            public int id;
            public string sku;
            public string infotag_type;
            public string url;
            public string name;
            public string project;
            public string shop;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(InfoTagAPI), true)]
        public class InfoTagAPI_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("infotagEndpoint"), true);

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
