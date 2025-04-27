using System.Collections;
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
    /// The AssortmentAPI stores the positions and data of assortments so that it can be persisted and 
    /// synced between users
    /// </summary>
    public class AssortmentAPI : Singleton<AssortmentAPI>
    {
        public string EntriesEndpoint = "/entries";

        public static AssortmentAPI Instance
        {
            get
            {
                return ((AssortmentAPI)instance);
            }
            set
            {
                instance = value;
            }
        }

        /// <summary>
        /// Get the product entries from the database
        /// </summary>
        public void GetProducts()
        {
            ProductAPI.Instance.OnAPIGETComplete -= GetProducts;

            if(CoreManager.Instance.projectSettings.useAssortmentAPI)
            {
                if (CoreManager.Instance.IsOffline) return;

                StartCoroutine(getProducts());
            }
        }

        /// <summary>
        /// Insert a product into the database table
        /// </summary>
        public void InsertProduct(string productCode, Vector3 localPosition, int assortmentIndex, string collection = "", string productplacementID = "", string shop = "")
        {
            if (CoreManager.Instance.projectSettings.useAssortmentAPI)
            {
                if (CoreManager.Instance.IsOffline) return;

                StartCoroutine(insert(productCode, localPosition, assortmentIndex, collection, productplacementID, shop));
            }
        }

        /// <summary>
        /// Update a product entry within the database table
        /// </summary>
        public void UpdateProduct(string productCode, Vector3 localPosition, int assortmentIndex, int insertID, string collection = "", string productplacementID = "", string shop = "")
        {
            if (CoreManager.Instance.projectSettings.useAssortmentAPI)
            {
                if (CoreManager.Instance.IsOffline) return;

                StartCoroutine(updateEntry(productCode, localPosition, assortmentIndex, insertID, collection, productplacementID, shop));
            }
        }

        /// <summary>
        /// Delete a product from the database table
        /// </summary>
        public void DeleteProduct(string productCode, int assortmentIndex, int insertID)
        {
            if (CoreManager.Instance.projectSettings.useAssortmentAPI)
            {
                if (CoreManager.Instance.IsOffline) return;

                StartCoroutine(deleteEntry(productCode, assortmentIndex, insertID));
            }
        }


        #region HTTP Requests

        private IEnumerator getProducts()
        {
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + EntriesEndpoint + "?project_id=" + CoreManager.Instance.ProjectID + "&room_id=" + CoreManager.Instance.RoomID;

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
                        processAssortmentData(request.downloadHandler.text);
                    }
                }

                request.Dispose();
            }
        }

        private IEnumerator insert(string productCode, Vector3 localPosition, int assortmentIndex, string collection = "", string productplacementID = "", string shop = "")
        {
            //Api insert product

            Entry entry = new Entry();
            entry.product_id = productCode;
            entry.position_x = localPosition.x;
            entry.position_y = localPosition.y;
            entry.room_id = CoreManager.Instance.RoomID;
            entry.assortment_index = assortmentIndex;
            entry.project_id = CoreManager.Instance.ProjectID;

            entry.collection = collection;
            entry.shop = shop;
            entry.placement_id = productplacementID;

            var jsonEntry = JsonUtility.ToJson(entry);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);

            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + EntriesEndpoint;

            Debug.Log("Request POST products method: uri= " + uri + "::" + jsonEntry);

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
                    if(request.responseCode == 200)
                    {
                        //Store the db id of the product entry on the product object (to use for later requests)
                        var response = JsonUtility.FromJson<InsertResponse>(request.downloadHandler.text);

                        MMOManager.Instance.SendRPC("AddProduct", (int)MMOManager.RpcTarget.All, productCode, localPosition, assortmentIndex, response.id, collection, productplacementID, shop);
                    }
                }

                request.Dispose();
            }
        }

        private IEnumerator updateEntry(string productCode, Vector3 localPosition, int assortmentIndex, int insertID, string collection = "", string productplacementID = "", string shop = "")
        {
            Entry entry = new Entry();
            entry.product_id = productCode;
            entry.position_x = localPosition.x;
            entry.position_y = localPosition.y;
            entry.room_id = CoreManager.Instance.RoomID;
            entry.assortment_index = assortmentIndex;
            entry.project_id = CoreManager.Instance.ProjectID;

            entry.collection = collection;
            entry.shop = shop;
            entry.placement_id = productplacementID;

            var jsonEntry = JsonUtility.ToJson(entry);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);

            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + EntriesEndpoint + "/" + insertID.ToString();

            Debug.Log("Request PUT products method: uri= " + uri);

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
                    Debug.Log("Error PUT products: " + request.responseCode + " | " + request.error);

                    // if the product doesnt exist in the backend, it needs to be removed

                    if (request.responseCode == 404)
                    {
                        Debug.LogWarning("Warning: trying to update a product that doesnt exist in the database. Deleting product");

                        // remove this locally

                        AssortmentManager.Instance.RemoteRemoveFromAssortment(productCode, assortmentIndex, insertID);

                        // tell the others to remove it

                        AssortmentSync.Instance.MasterCleanupProduct(productCode, assortmentIndex, insertID);
                    }
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

        private IEnumerator deleteEntry(string productCode, int assortmentIndex, int insertID)
        {
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + EntriesEndpoint+"/"+ insertID.ToString();

            Debug.Log("Request DELETE products method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Delete(uri))
            {
                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error DELETE products: " + request.error);

                    if (request.responseCode == 404)
                    {
                        // if the product doesnt exist in the backend, need to make sure it definitely was removed by everyone

                        Debug.LogWarning("Warning: trying to update a product that doesnt exist in the database. removing");

                        // remove this locally

                        AssortmentManager.Instance.RemoteRemoveFromAssortment(productCode, assortmentIndex, insertID);

                        // tell the others to remove it

                        AssortmentSync.Instance.MasterCleanupProduct(productCode, assortmentIndex, insertID);
                    }
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

        #endregion

        /// <summary>
        /// Process the data received from the getProduct API request
        /// </summary>
        /// <param name="json">JSON object containing the data - this will be an array</param>
        private void processAssortmentData(string json)
        {
            Debug.Log("Processing products responce: " + json);

            // check for empty json array
            if (json == "[]")
            {
                return;
            }

            // Extract the product data from json array (JsonUtility doesnt support deserializing array so had to manually extract)
            // Would be good to use a library that can actually deserialize it from the productMeta structure. When this is a plugin
            // we could probably use Newtonsoft.json and set it as a package dependancy. But for now I added a json library manually 
            // to the folder to ensure there are no dependancies that have to be manually added for new projects.

            var data = new JSONObject(json);

            foreach(JSONObject obj in data.list)
            {
                var productMeta = new ProductMeta();
                productMeta.id = obj.GetField("id").intValue;
                productMeta.product_id = obj.GetField("product_id").stringValue;
                productMeta.project_id = obj.GetField("project_id").stringValue;
                productMeta.room_id = obj.GetField("room_id").intValue;
                productMeta.assortment_index = obj.GetField("assortment_index").intValue;
                productMeta.position_x = float.Parse(obj.GetField("position_x").stringValue);
                productMeta.position_y = float.Parse(obj.GetField("position_y").stringValue);

                productMeta.placement_id = obj.GetField("placement_id").stringValue;
                productMeta.shop = obj.GetField("shop").stringValue;
                productMeta.collection = obj.GetField("collection").stringValue;

                ProductPlacement placement = ProductPlacementManager.Instance.GetProductPlacement(productMeta.collection, productMeta.shop);

                if(placement != null)
                {
                    ProductPlacement.ProductPlacementObject rawObj = placement.GetPlacementObject(int.Parse(productMeta.placement_id));

                    if(rawObj != null)
                    {
                        var insertPosition = new Vector3(productMeta.position_x, productMeta.position_y, 0);
                        AssortmentManager.Instance.AddToAssortment(placement, rawObj, productMeta.assortment_index, insertPosition, true, productMeta.id);
                        continue;
                    }
                }
 
                //Find the product in the scene based on productId;
                var product = ProductManager.Instance.FindProduct(productMeta.product_id);

                if (product != null)
                {
                    var insertPosition = new Vector3(productMeta.position_x, productMeta.position_y, 0);

                    AssortmentManager.Instance.AddToAssortment(product, productMeta.assortment_index, insertPosition, true, productMeta.id);
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AssortmentAPI), true)]
        public class AssortmentAPI_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("EntriesEndpoint"), true);

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



    #region Data Structures

    [Serializable]
    public class Entry
    {
        public string product_id;
        public float position_x;
        public float position_y;
        public int room_id;
        public int assortment_index;
        public string project_id;

        public string placement_id;
        public string shop;
        public string collection;
    }

    [Serializable]
    public class InsertResponse
    {
        public int id;
        public string product_id;
        public string room_id;
        public int assortment_index;
        public float position_x;
        public float position_y;
        public string created_at;
        public string updated_at;

        public string placement_id;
        public string shop;
        public string collection;
    }


    [Serializable]
    public class ProductMeta
    {
        public int id;
        public string product_id;
        public string project_id;
        public int room_id;
        public int assortment_index;
        public float position_x;
        public float position_y;
        public string created_at;
        public string updated_at;

        public string placement_id;
        public string shop;
        public string collection;
    }

    #endregion
}