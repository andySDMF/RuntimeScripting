using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ProductPlacementSync : Singleton<ProductPlacementSync>
    {
        public static ProductPlacementSync Instance
        {
            get
            {
                return ((ProductPlacementSync)instance);
            }
            set
            {
                instance = value;
            }
        }

        private bool syncEnabled = true;

        private void Start()
        {
            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Offline))
            {
                syncEnabled = false;
            }
        }

        /// <summary>
        /// Admin moves a product, sync this with other players
        /// </summary>
        public void SyncPositionProduct(string placementID, int id, Product product)
        {
            if (syncEnabled && product != null)
            {
                MMOManager.Instance.SendRPC("UpdateProductPositionPlacement", (int)MMOManager.RpcTarget.Others, placementID, id, product.transform.localPosition);
            }
        }

        /// <summary>
        /// Admin deletes a product, sync this with other players
        /// </summary>
        public void SyncScaleProduct(string placementID, int id, Product product)
        {
            if (syncEnabled && product != null)
            {
                MMOManager.Instance.SendRPC("UpdateProductScalePlacement", (int)MMOManager.RpcTarget.Others, placementID, id, product.transform.localScale);
            }
        }

        /// <summary>
        /// Admin deletes a product, sync this with other players
        /// </summary>
        public void SyncRemoveProduct(string placementID, int id)
        {
            if (syncEnabled)
            {
                MMOManager.Instance.SendRPC("RemoveProductPlacement", (int)MMOManager.RpcTarget.Others, placementID, id);
            }
        }

        /// <summary>
        /// Admin adds a product, sync this with other players
        /// </summary>
        public void SyncAddProduct(string placementID, ProductPlacement.ProductPlacementObject product)
        {
            if (syncEnabled && product != null)
            {
                MMOManager.Instance.SendRPC("AddProductPlacement", (int)MMOManager.RpcTarget.Others, placementID, JsonUtility.ToJson(product));
            }
        }

        /// <summary>
        /// Admin request to sync placement, sync this with other players
        /// </summary>
        public void SyncProductPlacment(string placementID)
        {
            if (syncEnabled)
            {
                MMOManager.Instance.SendRPC("GetProductPlacementContents", (int)MMOManager.RpcTarget.Others, placementID);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ProductPlacementSync), true)]
        public class ProductPlacementSync_Editor : BaseInspectorEditor
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
    }
}
