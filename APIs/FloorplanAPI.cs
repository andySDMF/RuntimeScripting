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
    public class FloorplanAPI : Singleton<FloorplanAPI>
    {
        public static FloorplanAPI Instance
        {
            get
            {
                return ((FloorplanAPI)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Endpoint")]
        [SerializeField]
        private string floorplanEndpoint = "/floorplans";

        public void GetFloorplanItems()
        {
            if (CoreManager.Instance.IsOffline)
            {
                return;
            }

            StartCoroutine(RequestGetFloorplansItems());
            ModelAPI.Instance.GetModels("FloorplanAssets", OnModelCallack);
        }

        private void OnModelCallack(List<ModelAPI.ModelJson> models)
        {
            FloorplanManager.Instance.AddGLBModels(models);
        }

        public void AddFloorplanItem(FloorplanManager.FloorplanItem item)
        {
            if (CoreManager.Instance.IsOffline)
            {
                return;
            }

            StartCoroutine(RequestAddFloorplanItem(item));
        }

        public void UpdateFloorplanItem(FloorplanManager.FloorplanItem item)
        {
            if (CoreManager.Instance.IsOffline)
            {
                return;
            }

            StartCoroutine(RequestUpdateFloorplanItem(item));
        }

        public void DeleteFloorplanItem(FloorplanManager.FloorplanItem item)
        {
            if (CoreManager.Instance.IsOffline)
            {
                return;
            }

            StartCoroutine(RequestDeleteFloorplanItem(item));
        }

        private IEnumerator RequestAddFloorplanItem(FloorplanManager.FloorplanItem item)
        {
            var jsonEntry = JsonUtility.ToJson(item);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);

            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + floorplanEndpoint;

            Debug.Log("Request POST floorplan item: uri= " + uri + "::" + jsonEntry);

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
                    Debug.Log("Error POST floorplan item: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        //need to update the id of the item updated
                        item = JsonUtility.FromJson<FloorplanManager.FloorplanItem>(request.downloadHandler.text);

                        //this will sync with everyone in the room, including the user who inserted and create floorplan item (if RPC recieved)
                        FloorplanSync.Instance.SyncAddFloorplanItem(item);
                    }
                }

                request.Dispose();
            }
        }

        private IEnumerator RequestUpdateFloorplanItem(FloorplanManager.FloorplanItem item)
        {
            var jsonEntry = JsonUtility.ToJson(item);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);

            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + floorplanEndpoint + "/" + item.id.ToString();

            Debug.Log("Request PUT floorplan item method: uri= " + uri);

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
                    Debug.Log("Error PUT floorplan item: " + request.responseCode + " | " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        FloorplanSync.Instance.SyncUpdateFloorplanItem(item.item, new Vector3(item.pos_x, item.pos_y, item.pos_z), item.rot, item.scale);
                    }
                }

                request.Dispose();
            }
        }

        private IEnumerator RequestDeleteFloorplanItem(FloorplanManager.FloorplanItem item)
        {
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;

            var jsonEntry = JsonUtility.ToJson(item);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);

            var uri = host + APIPATH + floorplanEndpoint + "/" + item.id.ToString();

            Debug.Log("Request DELETE floorplan item method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Delete(uri))
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
                    Debug.Log("Error DELETE floorplan item: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        FloorplanSync.Instance.SyncRemoveFloorplanItem(item.item);
                    }
                }

                request.Dispose();
            }
        }

        private IEnumerator RequestGetFloorplansItems()
        {
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var project = CoreManager.Instance.ProjectID;

            var uri = host + APIPATH + floorplanEndpoint + "?project=" + project;

            Debug.Log("Request GET floorplan method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error GET floorplan: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        ProcessProductData(request.downloadHandler.text);
                    }
                }

                request.Dispose();
            }
        }

        private void ProcessProductData(string json)
        {
            Debug.Log("Processing floorplanAPI responce: " + json);

            // check for empty json array
            if (json == "[]")
            {
                return;
            }

            var data = new JSONObject(json);

            foreach (JSONObject obj in data.list)
            {
                var itemMeta = new FloorplanManager.FloorplanItem();

                itemMeta.id = obj.GetField("id").intValue;
                itemMeta.item = obj.GetField("item").stringValue;
                itemMeta.prefab = obj.GetField("prefab").stringValue;
                itemMeta.pos_x = obj.GetField("pos_x").floatValue;
                itemMeta.pos_y = obj.GetField("pos_y").floatValue;
                itemMeta.pos_z = obj.GetField("pos_z").floatValue;
                itemMeta.scale = obj.GetField("scale").floatValue;
                itemMeta.rot = obj.GetField("rot").floatValue;

                FloorplanManager.Instance.InsertFloorplanItem(itemMeta);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(FloorplanAPI), true)]
        public class FloorplanAPI_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("floorplanEndpoint"), true);
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
