using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// AssortmentSync is used to synchronise assortments so that products are updated between users
    /// This can optionally use the AssortmentAPI to sync and persist the data on the backend
    /// </summary>
    public class AssortmentSync : Singleton<AssortmentSync>
    {
        public static AssortmentSync Instance
        {
            get
            {
                return ((AssortmentSync)instance);
            }
            set
            {
                instance = value;
            }
        }

        private bool useAssortmentAPI;
        private bool syncEnabled;
        private int localInsertID = -1;

        private void Start()
        {
            useAssortmentAPI = CoreManager.Instance.projectSettings.useAssortmentAPI;
            syncEnabled = CoreManager.Instance.projectSettings.useAssortmentSync;

            if(AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Offline))
            {
                syncEnabled = false;
            }
        }

        /// <summary>
        /// Player adds a product to assortment, sync this with other players
        /// </summary>
        public void SyncAddProduct(string productCode, Vector3 localPosition, int assortmentIndex, string collection = "", string productplacementID = "", string shop = "")
        {
            if (syncEnabled)
            {
                // If we are the master client: add product to api, get insertID, sync details to others via photon
                if (MMOManager.Instance.IsMasterClient())
                {
                    MasterAddProduct(productCode, localPosition, assortmentIndex, collection, productplacementID, shop);

                }
                else
                {
                    // If we are not the master client, send our update to the masterclient only. He can then insert it into the api, he can then tell the others

                    MMOManager.Instance.SendRPC("RequestMasterAddProduct", (int)MMOManager.RpcTarget.MasterClient, productCode, localPosition, assortmentIndex, collection, productplacementID, shop);
                }

            } else
            {
                localInsertID++;
                AssortmentManager.Instance.RemoteAddToAssortment(productCode, assortmentIndex, localPosition, localInsertID, collection, productplacementID, shop);
            }
        }

        /// <summary>
        /// Players moves a product, sync this with other players
        /// </summary>
        public void SyncUpdateProduct(Product product)
        {
            if (syncEnabled && product != null)
            {
                var userID = PlayerManager.Instance.GetLocalPlayer().ID;
                ProductMesh mesh = product.ProductMesh.GetComponent<ProductMesh>();

                MMOManager.Instance.SendRPC("UpdateProduct", (int)MMOManager.RpcTarget.Others, product.settings.ProductCode, product.transform.localPosition, product.currentAssortment, product.InsertID, mesh.ProductPlacementCollection, mesh.UniqueProductPlacementID < 0 ? "" : mesh.UniqueProductPlacementID.ToString(), mesh.ProductPlacementShop);

                // if we are the master client, update in the API 
                if (MMOManager.Instance.IsMasterClient())
                {
                    //AssortmentAPI.instance.UpdateProduct(product.ProductCode, product.transform.localPosition, product.currentAssortment, product.InsertID);
                    MasterUpdateProduct(product.settings.ProductCode, product.transform.localPosition, product.currentAssortment, product.InsertID, mesh.ProductPlacementCollection, mesh.UniqueProductPlacementID.ToString(), mesh.ProductPlacementShop);
                }
            }
        }

        /// <summary>
        /// Player removes a product, sync this with other players
        /// </summary>
        /// <param name="product"></param>
        public void SyncRemoveProduct(Product product)
        {
            if (syncEnabled)
            {
                MMOManager.Instance.SendRPC("RemoveProduct", (int)MMOManager.RpcTarget.Others, product.settings.ProductCode, product.currentAssortment, product.InsertID);

                // if we are the master client, delete from the API 
                if (MMOManager.Instance.IsMasterClient())
                {
                    MasterRemoveProduct(product.settings.ProductCode, product.currentAssortment, product.InsertID);
                }
            }
        }

        /// <summary>
        ///  Store the local insert ID so that all users have the latest value (for if master client transfers to another user) [for when not using API]
        /// </summary>
        /// <param name="insertID">the current local insert ID value</param>
        public void StoreLocalInsertID(int insertID)
        {
            if (syncEnabled && !useAssortmentAPI)
            {
                localInsertID = insertID;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AssortmentSync), true)]
        public class AssortmentSync_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
            }
        }
#endif

        #region Master Client Functions
        /// <summary>
        /// Master client add product to API, tell other users to add
        /// </summary>
        public void MasterAddProduct(string productCode, Vector3 localPosition, int assortmentIndex, string collection = "", string productplacementID = "", string shop = "")
        {
            if (syncEnabled)
            {
                if (useAssortmentAPI)
                {
                    AssortmentAPI.Instance.InsertProduct(productCode, localPosition, assortmentIndex, collection, productplacementID, shop);
                }
                else
                {
                    // if not using API and only syncing via Photon, we just increment localInsertID for each new product (And sync this too so other users
                    // have it for when master client transfer)

                    localInsertID++;
                    MMOManager.Instance.SendRPC("AddProduct", (int)MMOManager.RpcTarget.All, productCode, localPosition, assortmentIndex, localInsertID, collection, productplacementID, shop);
                }
            }
        }

        /// <summary>
        /// Master client update the product position on API
        /// </summary>
        public void MasterUpdateProduct(string productCode, Vector3 localPosition, int assortmentIndex, int insertID, string collection = "", string productplacementID = "", string shop = "")
        {
            if (syncEnabled && useAssortmentAPI)
            {
                AssortmentAPI.Instance.UpdateProduct(productCode, localPosition, assortmentIndex, insertID, collection, productplacementID, shop);
            }
        }
        
        /// <summary>
        /// Master client delete product from API
        /// </summary>
        public void MasterRemoveProduct(string productCode, int assortmentIndex, int insertID)
        {
            if (syncEnabled && useAssortmentAPI)
            {
                AssortmentAPI.Instance.DeleteProduct(productCode, assortmentIndex, insertID);
            }
        }

        /// <summary>
        ///  In case the master had tried to update a product which wasnt found in the database, we make sure everyone deletes it
        /// </summary>
        public void MasterCleanupProduct(string productCode, int assortmentIndex, int insertID)
        {
            if (syncEnabled && useAssortmentAPI && MMOManager.Instance.IsMasterClient())
            {
                MMOManager.Instance.SendRPC("RemoveProduct", (int)MMOManager.RpcTarget.Others, productCode, assortmentIndex, insertID);
            }
        }
        #endregion
    }
}