using Defective.JSON;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ProductAPI : Singleton<ProductAPI>
    {
        public static ProductAPI Instance
        {
            get
            {
                return ((ProductAPI)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Endpoint")]
        [SerializeField]
        private string productsEndpoint = "/products/";

        private System.Action<string> m_openFileCallback;

        public System.Action OnAPIGETComplete { get; set; }

        public void OpenFile(string placementID, string shop, string fileTypes, System.Action<string> callback)
        {
            m_openFileCallback = callback;

            WebclientManager.WebClientListener += ResponceCallback;
            WebclientManager.Instance.Send(JsonUtility.ToJson(new ProductUploadRequest(fileTypes, CoreManager.Instance.ProjectID, placementID, shop)));
        }

        private void ResponceCallback(string data)
        {
            var webProductUploadResponce = JsonUtility.FromJson<ProductUploadResponce>(data).OrDefaultWhen(x => x.url == null);
            WebclientManager.WebClientListener -= ResponceCallback;

            if (webProductUploadResponce != null)
            {
                if (m_openFileCallback != null)
                {
                    m_openFileCallback.Invoke(webProductUploadResponce.url);
                }
            }
            else
            {
                var webProductUploadZipResponce = JsonUtility.FromJson<ProductUploadZipResponce>(data).OrDefaultWhen(x => x.productUploadRequest == false);

                if(webProductUploadZipResponce != null)
                {
                    if (m_openFileCallback != null)
                    {
                        m_openFileCallback.Invoke("Multi");
                    }
                }
            }

            m_openFileCallback = null;
        }

        public void AddProduct(string placementID, ProductPlacement.ProductPlacementObject prod, System.Action callback = null, int quantity = 1, bool addInfoTags = true)
        {
            if (CoreManager.Instance.IsOffline) return;

            int count = 1;
            bool createInfoTags = addInfoTags;

            while(count <= quantity)
            {
                StartCoroutine(ProcessAddProduct(placementID, prod, (count.Equals(quantity) ? callback : null), createInfoTags));
                createInfoTags = false;
                count++;
            }
        }

        public void DeleteProducts(int[] idvalues)
        {
            if (CoreManager.Instance.IsOffline) return;

            for(int i = 0; i < idvalues.Length; i++)
            {
               StartCoroutine(ProcessDeleteProduct(idvalues[i]));
            }
        }

        public void GetProducts()
        {
            if (CoreManager.Instance.IsOffline) return;

            StartCoroutine(GetProductsRequest());
        }

        public void GetProductsForPlacement(string placementID)
        {
            if (CoreManager.Instance.IsOffline) return;

            StartCoroutine(GetProductsRequest(placementID));
        }

        public void SyncProduct(string placementID, ProductPlacement.ProductPlacementObject prod)
        {
            if (CoreManager.Instance.IsOffline) return;

            StartCoroutine(ProcessSyncProduct(placementID, prod));
        }

        public void UpdateProduct(string placementID, ProductPlacement.ProductPlacementObject prod, ProductPlacement.ProductPlacementInfoTag[] addTags, ProductPlacement.ProductPlacementInfoTag[] updateTags, ProductPlacement.ProductPlacementInfoTag[] deleteTags, System.Action callback = null)
        {
            if (CoreManager.Instance.IsOffline) return;

            StartCoroutine(ProcessUpdateProduct(placementID, prod, addTags, updateTags, deleteTags, callback));
        }

        public void DeleteProductTexture(string textureURL)
        {
            if (CoreManager.Instance.IsOffline) return;

            var deleteTx = new DeleteProductTextureJson();
            deleteTx.url = textureURL;

            StartCoroutine(DeleteProductTextureRequest(deleteTx));
        }

        private IEnumerator DeleteProductTextureRequest(DeleteProductTextureJson pJson)
        {
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;

            var jsonEntry = JsonUtility.ToJson(pJson);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);

            var uri = host + APIPATH + "/content/remove";

            Debug.Log("Request DELETE product texture method: uri= " + uri);

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
                    Debug.Log("Error DELETE contents: " + request.error);
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

        private IEnumerator GetProductsRequest(string placementID = "")
        {
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var project = CoreManager.Instance.ProjectID;

            var uri = "";
            
            if(string.IsNullOrEmpty(placementID))
            {
                uri = host + APIPATH + productsEndpoint + "?project=" + project;
            }
            else
            {
                uri = host + APIPATH + productsEndpoint + "?project=" + project + "&collection=" + placementID;
            }

            Debug.Log("Request GET products method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error GET products: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                       yield return StartCoroutine(ProcessProductData(request.downloadHandler.text));
                    }
                }

                request.Dispose();
            }

            if(OnAPIGETComplete != null)
            {
                OnAPIGETComplete.Invoke();
            }
        }

        private IEnumerator ProcessAddProduct(string placementID, ProductPlacement.ProductPlacementObject prod, System.Action callback = null, bool createInfoTags = true)
        {
            //new product Json
            ProductJson pJson = GetProductJson( placementID, prod);

            //create new data object for product
            var jsonEntry =  JsonUtility.ToJson(pJson);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + productsEndpoint;

            Debug.Log("Request POST product method: uri= " + uri + "::" + jsonEntry);

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
                    Debug.Log("Error POST products: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        var data = new JSONObject(request.downloadHandler.text);

                        if(data != null)
                        {
                            prod.id = data.GetField("id").intValue;
                        }

                        if(createInfoTags)
                        {
                            InfoTagAPI.InfotagJson json = new InfoTagAPI.InfotagJson();
                            json.sku = prod.productCode;
                            json.project = CoreManager.Instance.ProjectID;
                            json.shop = prod.shop;

                            //need to send infotags to its API
                            if (!string.IsNullOrEmpty(prod.description.data))
                            {
                                json.url = prod.description.data;
                                json.name = "Description";
                                json.infotag_type = InfotagType.Text.ToString();

                                yield return StartCoroutine(InfoTagAPI.Instance.AddInfoTag(json));

                                prod.description.id = json.id;
                            }

                            if (prod.images.Count > 0)
                            {
                                for (int i = 0; i < prod.images.Count; i++)
                                {
                                    json.url = prod.images[i].title;
                                    json.name = prod.images[i].data;
                                    json.infotag_type = InfotagType.Image.ToString();

                                    yield return StartCoroutine(InfoTagAPI.Instance.AddInfoTag(json));

                                    prod.images[i].id = json.id;
                                }
                            }

                            if (prod.videos.Count > 0)
                            {
                                for (int i = 0; i < prod.videos.Count; i++)
                                {
                                    json.url = prod.videos[i].title;
                                    json.name = prod.videos[i].data;
                                    json.infotag_type = InfotagType.Video.ToString();

                                    yield return StartCoroutine(InfoTagAPI.Instance.AddInfoTag(json));

                                    prod.videos[i].id = json.id;
                                }
                            }

                            if (prod.websites.Count > 0)
                            {
                                for (int i = 0; i < prod.websites.Count; i++)
                                {
                                    json.url = prod.websites[i].title;
                                    json.name = prod.websites[i].data;
                                    json.infotag_type = InfotagType.Web.ToString();

                                    yield return StartCoroutine(InfoTagAPI.Instance.AddInfoTag(json));

                                    prod.websites[i].id = json.id;
                                }
                            }
                        }
                       
                        //need to send product placement manager
                        ProductPlacementManager.Instance.OnAddedNewProduct(placementID, prod);
                    }
                }

                if (callback != null)
                {
                    callback.Invoke();
                }

                request.Dispose();
            }
        }

        private IEnumerator ProcessDeleteProduct(int id)
        {
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + productsEndpoint + id.ToString();

            Debug.Log("Request DELETE product method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Delete(uri))
            {
                request.SetRequestHeader("Content-Type", "application/json");

                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error Delete products: " + request.error);
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

        private IEnumerator ProcessSyncProduct(string placementID, ProductPlacement.ProductPlacementObject prod)
        {
            //new product Json
            ProductJson pJson = GetProductJson(placementID, prod);

            //create new data object for product
            var jsonEntry = JsonUtility.ToJson(pJson);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + productsEndpoint + prod.id.ToString();

            Debug.Log("Request PUT product method: uri= " + uri);

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
                    Debug.Log("Error PUT product: " + request.error);
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

        private IEnumerator ProcessUpdateProduct(string placementID, ProductPlacement.ProductPlacementObject prod, ProductPlacement.ProductPlacementInfoTag[] addTags, ProductPlacement.ProductPlacementInfoTag[] updateTags, ProductPlacement.ProductPlacementInfoTag[] deleteTags, System.Action callback = null)
        {
            //new product Json
            ProductJson pJson = GetProductJson(placementID, prod);

            //create new data object for product
            var jsonEntry = JsonUtility.ToJson(pJson);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + productsEndpoint + prod.id.ToString();

            Debug.Log("Request PUT product method: uri= " + uri);

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
                    Debug.Log("Error PUT product: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        InfoTagAPI.InfotagJson json = new InfoTagAPI.InfotagJson();
                        json.sku = prod.productCode;
                        json.project = CoreManager.Instance.ProjectID;
                        json.shop = prod.shop;

                        if (addTags != null && addTags.Length > 0)
                        {
                            for(int i = 0; i < addTags.Length; i++)
                            {
                                if(addTags[i].type.Equals(InfotagType.Text))
                                {
                                    json.url = addTags[0].data;
                                    json.name = "Description";
                                    json.infotag_type = InfotagType.Text.ToString();

                                    yield return StartCoroutine(InfoTagAPI.Instance.AddInfoTag(json));
                                    prod.description.id = json.id;
                                }
                                else if (addTags[i].type.Equals(InfotagType.Image))
                                {
                                    json.url = addTags[i].title;
                                    json.name = addTags[i].data;
                                    json.infotag_type = InfotagType.Image.ToString();

                                    yield return StartCoroutine(InfoTagAPI.Instance.AddInfoTag(json));
                                    prod.images[0].id = json.id;
                                }
                                else if (addTags[i].type.Equals(InfotagType.Web))
                                {
                                    json.url = addTags[i].title;
                                    json.name = addTags[i].data;
                                    json.infotag_type = InfotagType.Web.ToString();

                                    yield return StartCoroutine(InfoTagAPI.Instance.AddInfoTag(json));
                                    prod.websites[0].id = json.id;
                                }
                                else if (addTags[i].type.Equals(InfotagType.Video))
                                {
                                    json.url = addTags[i].title;
                                    json.name = addTags[i].data;
                                    json.infotag_type = InfotagType.Video.ToString();

                                    yield return StartCoroutine(InfoTagAPI.Instance.AddInfoTag(json));
                                    prod.videos[0].id = json.id;
                                }
                            }
                        }

                        if (updateTags != null && updateTags.Length > 0)
                        {
                            for (int i = 0; i < updateTags.Length; i++)
                            {
                                if (updateTags[i].type.Equals(InfotagType.Text))
                                {
                                    json.id = updateTags[i].id;
                                    json.url = updateTags[0].data;
                                    json.name = "Description";
                                    json.infotag_type = InfotagType.Text.ToString();

                                    yield return StartCoroutine(InfoTagAPI.Instance.UpdateInfoTag(json));
                                }
                                else if (updateTags[i].type.Equals(InfotagType.Image))
                                {
                                    json.id = updateTags[i].id;
                                    json.url = updateTags[i].data;
                                    json.name = updateTags[i].title;
                                    json.infotag_type = InfotagType.Image.ToString();

                                    yield return StartCoroutine(InfoTagAPI.Instance.UpdateInfoTag(json));
                                }
                                else if (updateTags[i].type.Equals(InfotagType.Web))
                                {
                                    json.id = updateTags[i].id;
                                    json.url = updateTags[i].data;
                                    json.name = updateTags[i].title;
                                    json.infotag_type = InfotagType.Web.ToString();

                                    yield return StartCoroutine(InfoTagAPI.Instance.UpdateInfoTag(json));
                                }
                                else if (updateTags[i].type.Equals(InfotagType.Video))
                                {
                                    json.id = updateTags[i].id;
                                    json.url = updateTags[i].data;
                                    json.name = updateTags[i].title;
                                    json.infotag_type = InfotagType.Video.ToString();

                                    yield return StartCoroutine(InfoTagAPI.Instance.UpdateInfoTag(json));
                                }
                            }
                        }

                        if (deleteTags != null && deleteTags.Length > 0)
                        {
                            for (int i = 0; i < deleteTags.Length; i++)
                            {
                                StartCoroutine(InfoTagAPI.Instance.DeleteInfoTag(deleteTags[i].id));
                            }
                        }

                        //need to send product placement manager
                        ProductPlacementManager.Instance.OnUpdateOldProduct(placementID, prod);
                    }
                }

                if (callback != null)
                {
                    callback.Invoke();
                }

                request.Dispose();
            }
        }

        private IEnumerator ProcessProductData(string json)
        {
            Debug.Log("Processing productAPI responce: " + json);

            // check for empty json array
            if (json == "[]")
            {
                yield break;
            }

            ProductPlacement[] placements = FindObjectsByType<ProductPlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            var data = new JSONObject(json);
            foreach (JSONObject obj in data.list)
            {
                var productMeta = new ProductPlacement.ProductPlacementObject();
                productMeta.id = obj.GetField("id").intValue;
                productMeta.productCode = obj.GetField("sku").stringValue;
                productMeta.textureURL = obj.GetField("content_url").stringValue;
                productMeta.position = new Vector3(obj.GetField("pos_x").floatValue, obj.GetField("pos_y").floatValue, obj.GetField("pos_z").floatValue);
                productMeta.scale = new Vector3(obj.GetField("scale_x").floatValue, obj.GetField("scale_y").floatValue, obj.GetField("scale_z").floatValue);
               
                if(obj.HasField("shop"))
                {
                    if(string.IsNullOrEmpty(obj.GetField("shop").stringValue))
                    {
                        productMeta.shop = "Brandlab";
                    }
                    else
                    {
                        productMeta.shop = obj.GetField("shop").stringValue;
                    }
                }
                else
                {
                    productMeta.shop = "Brandlab";
                }

                if (obj.GetField("infotags").boolValue)
                {
                    //need to get all the info tags for this product
                    yield return StartCoroutine(InfoTagAPI.Instance.GetInfoTags(productMeta, ProcessProductPlacementCallback));
                }

                //post to placement
                for(int i = 0; i < placements.Length; i++)
                {
                    if(placements[i].ID.Equals(obj.GetField("collection").stringValue))
                    {
                        placements[i].PlaceSingleProduct(productMeta);
                        break;
                    }
                }
            }
        }

        private void ProcessProductPlacementCallback(ProductPlacement.ProductPlacementObject productMeta, List<InfoTagAPI.InfotagJson> tags)
        {
            //loop through the tags and assign to corresponding tag list
            for (int i = 0; i < tags.Count; i++)
            {
                ProductPlacement.ProductPlacementInfoTag tag = new ProductPlacement.ProductPlacementInfoTag();
                tag.id = tags[i].id;
                tag.title = tags[i].name;
                tag.data = tags[i].url;

                if (tags[i].infotag_type.Equals(InfotagType.Text.ToString()))
                {
                    productMeta.description = tag;
                }
                else if (tags[i].infotag_type.Equals(InfotagType.Image.ToString()))
                {
                    productMeta.images.Add(tag);
                }
                else if (tags[i].infotag_type.Equals(InfotagType.Video.ToString()))
                {
                    productMeta.videos.Add(tag);
                }
                else if (tags[i].infotag_type.Equals(InfotagType.Web.ToString()))
                {
                    productMeta.websites.Add(tag);
                }
            }
        }



        private ProductJson GetProductJson(string placementID, ProductPlacement.ProductPlacementObject prod)
        {
            //new product Json
            ProductJson pJson = new ProductJson();
            pJson.sku = prod.productCode;
            pJson.collection = placementID;
            pJson.infotags = prod.InfoTagsUsed;
            pJson.project = CoreManager.Instance.ProjectID;
            pJson.content_url = prod.textureURL;
            pJson.ConvertPosition(prod.position);
            pJson.ConvertScale(prod.scale);
            pJson.shop = prod.shop;

            return pJson;
        }

        [System.Serializable]
        public class ProductUploadRequest
        {
            public string format = "";
            public bool productUploadRequest = true;
            public string project;
            public string collection;
            public string shop;

            public ProductUploadRequest(string fileTypes, string proj, string coll, string shop)
            {
                format = fileTypes;
                project = proj;
                collection = coll;
                this.shop = shop;
            }
        }

        [System.Serializable]
        public class ProductUploadResponce
        {
            public string url;
            public string filename;
            public string type;
        }

        [System.Serializable]
        public class ProductUploadZipResponce
        {
            public bool productUploadRequest = true;
            public string project;
            public string collection;
            public string shop;
        }

        [System.Serializable]
        private class DeleteProductTextureJson
        {
            public string url = "";
        }

        [System.Serializable]
        private class ProductJson
        {
            //product code
            public string sku = "";
            //product placementID
            public string collection = "";
            public string project = "";
            public string shop = "";
            //product texture
            public string content_url = "";
            public string content_type = "image";
            public bool infotags = false;
            public float pos_x = 0;
            public float pos_y = 0;
            public float pos_z = 0;
            public float scale_x = 0;
            public float scale_y = 0;
            public float scale_z = 0;
            public float bounds_x = 0;
            public float bounds_y = 0;
            public float bounds_z = 0;

            public void ConvertPosition(Vector3 vec)
            {
                pos_x = vec.x;
                pos_y = vec.y;
                pos_z = vec.z;
            }

            public void ConvertScale(Vector3 vec)
            {
                scale_x = vec.x;
                scale_y = vec.y;
                scale_z = vec.z;
            }

            public void ConvertBounds(Vector3 vec)
            {
                bounds_x = vec.x;
                bounds_y = vec.y;
                bounds_z = vec.z;
            }
        }

        [System.Serializable]
        public enum FormatRestriction { PNG, Any }

#if UNITY_EDITOR
        [CustomEditor(typeof(ProductAPI), true)]
        public class ProductAPI_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("productsEndpoint"), true);

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
