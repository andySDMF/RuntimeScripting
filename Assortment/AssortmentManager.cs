using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Cinemachine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// Assortment Manager handles assortments to provide functionality for adding
    /// products onto a rail/table/wall and syncinging their positions between users
    /// </summary>
    public class AssortmentManager : Singleton<AssortmentManager>, IRaycaster
    {
        public static AssortmentManager Instance
        {
            get
            {
                return ((AssortmentManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private List<Assortment> Assortments = new List<Assortment>();
        private float interactionDistance = 5;
        private bool useLocalDistance = true;

        public bool OverrideDistance { get { return useLocalDistance; } }

        private float ZSortOffset = 0.0025f;

        private bool draggingItem = false;
        private ProductMesh currentProduct = null;
        private Vector3 boundHitPoint;
        private bool removingFromAssortmentThisFrame = false;
        public ProductMesh RayCastProduct { get; set; }

        private float minXMovement;
        private float minYMovement;
        private float maxXMovement;
        private float maxYMovement;

        public string UserCheckKey
        {
            get
            {
                return m_userKey;
            }
        }

        private string m_userKey = "USERTYPE";

        private void Awake()
        {
            RaycastManager.Instance.Raycasters.Add(this);
        }

        private void Start()
        {
            ZSortOffset = CoreManager.Instance.playerSettings.assortmentZSortOffset;

            initializeAssortments();

            Assortment[] all = FindObjectsByType<Assortment>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assortments.Clear();

            for (int i = 0; i < all.Length; i++)
            {
                Assortments.Add(all[i]);
            }

            PlayerControlSettings.ManagerInteraction mInteration = CoreManager.Instance.playerSettings.GetIRaycasterManager(gameObject.name);

            if (mInteration != null)
            {
                interactionDistance = mInteration.interactionDistance;
                useLocalDistance = mInteration.overrideInteraction;
                m_userKey = mInteration.userCheckKey;
            }
            else
            {
                useLocalDistance = false;
            }
        }

        private void Update()
        {
            //let this manager take over from the RaycastManager - better performance when moving product
            if (draggingItem)
            {
                if (currentProduct != null)
                {
                    Ray ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMousePosition());
                    RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance);

                    foreach (RaycastHit ht in hits)
                    {
                        var hitBounds = ht.transform.gameObject.GetComponentInParent<AssortmentBounds>();

                        if (hitBounds != null && currentProduct.product.currentAssortment == hitBounds.ParentAssortment.overridingIndex)
                        {
                            //transform the hit point on the bounds into local space for the product pos
                            var transPos = currentProduct.product.transform.parent.InverseTransformPoint(ht.point);
                            var curtPos = currentProduct.product.transform.localPosition;
                            var curAssortment = hitBounds.ParentAssortment;
                            var z = -ZSortOffset * currentProduct.product.Sort;

                            if (curAssortment.assortmentType == AssortmentType.Default)
                            {
                                currentProduct.product.transform.localPosition = new Vector3(transPos.x, transPos.y, z);
                            }
                            else if (curAssortment.assortmentType == AssortmentType.Rail)
                            {
                                currentProduct.product.transform.localPosition = new Vector3(transPos.x, curtPos.y, z);
                            }
                            else if (curAssortment.assortmentType == AssortmentType.Table)
                            {
                                //need to look at if user selects product above the assortment table, product wont move
                                currentProduct.product.transform.localPosition = new Vector3(transPos.x, curtPos.y, transPos.z);
                            }

                            //break out so we dont hit an assortment behind
                            break;
                        }
                    }
                }

                if (InputManager.Instance.GetMouseButtonUp(0))
                {
                    if (draggingItem)
                    {
                        if (!removingFromAssortmentThisFrame)
                        {
                            //sync the position
                            AssortmentSync.Instance.SyncUpdateProduct(currentProduct.product);
                        }
                    }

                    currentProduct = null;
                    draggingItem = false;
                    RaycastManager.Instance.CastRay = true;
                }

                removingFromAssortmentThisFrame = false;
            }
        }

        private bool BoundsXIsEncapsulated(float xmin, float xmax)
        {
            if (xmin < minXMovement || xmax > maxXMovement) return false;

            return true;
        }

        private bool BoundsYIsEncapsulated(float ymin, float ymax)
        {
            if (ymin < minYMovement || ymax > maxYMovement) return false;

            return true;
        }

        public float Distance
        {
            get
            {
                float distance = 5000;

                //define camera to use
                if (!MapManager.Instance.TopDownViewActive)
                {
                    if (PlayerManager.Instance.ThirdPersonCameraActive)
                    {
                        distance = interactionDistance + Vector3.Distance(PlayerManager.Instance.GetLocalPlayer().TransformObject.position, PlayerManager.Instance.GetLocalPlayer().MainCamera.transform.position);
                    }
                    else
                    {
                        distance = interactionDistance;
                    }
                }
                else
                {
                    //cannot perform door stuff in topdown view
                    return -1;
                }

                return distance;
            }
        }

        public void RaycastHit(RaycastHit hit, out Transform hitObject)
        {
            var productMesh = hit.transform.GetComponent<ProductMesh>();

            if (productMesh != null && productMesh.product.inAssortment)
            {
                hitObject = productMesh.transform;
            }
            else
            {
                hitObject = null;
            }

            if (InputManager.Instance.GetMouseButtonDown(0) && productMesh != null)
            {
                //check user
                string user = PlayerManager.Instance.GetLocalPlayer().CustomizationData.ContainsKey(m_userKey) ? PlayerManager.Instance.GetLocalPlayer().CustomizationData[m_userKey].ToString() : "";

                if (productMesh.product.CanUserControlThis(user))
                {
                    return;
                }
            }

            if (productMesh != null)
            {
                if (productMesh.product.inAssortment)
                {
                    if (ProductManager.Instance.PrevProduct != null)
                    {
                        ProductManager.Instance.PrevProduct.product.HideTags(false);
                        ProductManager.Instance.PrevProduct = null;
                    }

                    if (RayCastProduct != null)
                    {
                        RayCastProduct.product.HideTags(false);
                    }

                    if (!productMesh.product.TagsOpen())
                    {
                        HideAllAssortmentTags();
                        ProductManager.Instance.HideAllProductTags();
                    }

                    productMesh.product.ShowTags(hit.point);
                    RayCastProduct = productMesh;
                }
            }
            else
            {
                if (RayCastProduct != null)
                {
                    RayCastProduct.product.WaitAndHideTags();
                }

                RayCastProduct = null;
            }


            if (InputManager.Instance.GetMouseButton(0) && !PlayerManager.Instance.GetLocalPlayer().IsButtonHeldDown)
            {
                if (!draggingItem)
                {
                    if (RayCastProduct != null)
                    {
                        currentProduct = RayCastProduct;
                        draggingItem = true;
                        RaycastManager.Instance.CastRay = false;

                        var hitBounds = currentProduct.transform.gameObject.GetComponentInParent<Assortment>();

                        //assign bounds values
                        if (hitBounds != null)
                        {
                            AssignBounds(hitBounds);
                        }
                    }

                }
            }

            removingFromAssortmentThisFrame = false;
        }

        private void AssignBounds(Assortment hitBounds)
        {
            Collider col = hitBounds.AssortmentBounds.GetComponent<Collider>();
            maxXMovement = hitBounds.AssortmentBounds.transform.localPosition.x + Mathf.Abs(col.bounds.extents.x);
            minXMovement = hitBounds.AssortmentBounds.transform.localPosition.x - Mathf.Abs(col.bounds.extents.x);

            if (hitBounds.assortmentType.Equals(AssortmentType.Table))
            {
                minYMovement = hitBounds.AssortmentBounds.transform.localPosition.z - Mathf.Abs(col.bounds.extents.z);
                maxYMovement = hitBounds.AssortmentBounds.transform.localPosition.z + Mathf.Abs(col.bounds.extents.z);
            }
            else
            {
                minYMovement = hitBounds.AssortmentBounds.transform.localPosition.y - Mathf.Abs(col.bounds.extents.y);
                maxYMovement = hitBounds.AssortmentBounds.transform.localPosition.y + Mathf.Abs(col.bounds.extents.y);
            }
        }

        public void RaycastMiss()
        {
            removingFromAssortmentThisFrame = false;

            if (RayCastProduct != null)
            {
                RayCastProduct.product.WaitAndHideTags();
                RayCastProduct = null;
            }
        }

        public void HideAllAssortmentTags()
        {
            foreach (Assortment assort in Assortments)
            {
                assort.HideAllAssortmentTags();
            }
        }

        public void AddToAssortment(ProductPlacement placement, ProductPlacement.ProductPlacementObject obj, int assortmentIndex, Vector3 insertPosition, bool remoteInserted = false, int insertID = -1)
        {
            Product prod = placement.CreateProductOperation(obj);
            StartCoroutine(ProcessProductPlacementAssortment(prod, assortmentIndex, insertPosition, remoteInserted, insertID));
        }

        private IEnumerator ProcessProductPlacementAssortment(Product prod, int assortmentIndex, Vector3 insertPosition, bool remoteInserted = false, int insertID = -1)
        {
            //wait till texture has rendered
            while(prod.RailPoint == null || prod.HoldPoint == null || prod.TablePoint == null)
            {
                yield return null;
            }

            AddToAssortment(prod, assortmentIndex, insertPosition, remoteInserted, insertID, false);
        }

        /// <summary>
        /// Add a product onto the assortment
        /// </summary>
        /// <param name="original">the original product</param>
        /// <param name="assortmentIndex">the index of the assortment to add it to</param>
        /// <param name="insertPosition">the insert position</param>
        /// <param name="remoteInserted">whether it was inserted by a remote user</param>
        /// <param name="insertID">the insert id of the product (the id returned from the server)</param>
        public void AddToAssortment(Product original, int assortmentIndex, Vector3 insertPosition, bool remoteInserted = false, int insertID = -1, bool instantiate = true)
        {
            if (assortmentIndex > Assortments.Count) { return; }

            //Create the new product
            var product = createAssortmentProduct(original, assortmentIndex, insertID, instantiate);

            //Calc the Z sort var to offset the z pos to fix zfighting
            var assortment = GetAssortment(assortmentIndex);
            var parent = assortment.AssortmentParent;
            var sort = parent.childCount - 1;
            product.Sort = sort;
            var z = -ZSortOffset * sort;
            Collider col = product.ProductMesh.GetComponent<Collider>();
            product.ProductMesh.GetComponent<ProductMesh>().rawTextureSource = original.ProductMesh.GetComponent<ProductMesh>().rawTextureSource;

            //position the product in the assortment based on which assortment type it is
            if (assortment.assortmentType == AssortmentType.Default)
            {
                product.transform.localPosition = new Vector3(insertPosition.x, insertPosition.y, z);

                //need to check if the assortment insertpoint plus extents is within the assortment bounds
                AssignBounds(assortment);

                float xminExtents = product.transform.localPosition.x - Mathf.Abs(col.bounds.extents.x);
                float xmaxExtents = product.transform.localPosition.x + Mathf.Abs(col.bounds.extents.x);
                float yminExtents = product.transform.localPosition.y - Mathf.Abs(col.bounds.extents.y);
                float ymaxExtents = product.transform.localPosition.y + Mathf.Abs(col.bounds.extents.y);

                //need to check if the current product bounds exceeds assortmentbounds
                if (!BoundsXIsEncapsulated(xminExtents, xmaxExtents))
                {
                    if (xminExtents < maxXMovement)
                    {
                        product.transform.localPosition = new Vector3(product.transform.localPosition.x + Mathf.Abs(col.bounds.extents.x), product.transform.localPosition.y, product.transform.localPosition.z);
                    }
                    else
                    {
                        product.transform.localPosition = new Vector3(product.transform.localPosition.x - Mathf.Abs(col.bounds.extents.x), product.transform.localPosition.y, product.transform.localPosition.z);
                    }
                }

                if (!BoundsYIsEncapsulated(yminExtents, ymaxExtents))
                {
                    if (yminExtents < minYMovement)
                    {
                        product.transform.localPosition = new Vector3(product.transform.localPosition.x, product.transform.localPosition.y + Mathf.Abs(col.bounds.extents.y), product.transform.localPosition.z);
                    }
                    else
                    {
                        product.transform.localPosition = new Vector3(product.transform.localPosition.x, product.transform.localPosition.y - Mathf.Abs(col.bounds.extents.y), product.transform.localPosition.z);
                    }
                }

                product.IsProductPlacementOrigin = original.IsProductPlacementOrigin;

                if (original.IsProductPlacementOrigin)
                {

                }
            }
            else if (assortment.assortmentType == AssortmentType.Rail)
            {
                // Position on rail relative to the rail point
                var yoffset = (product.RailPoint.transform.position.y - product.transform.position.y);
                var y = 0 - yoffset;
                product.transform.localPosition = new Vector3(insertPosition.x, y, z);

                product.IsProductPlacementOrigin = original.IsProductPlacementOrigin;

                if (original.IsProductPlacementOrigin)
                {
                }
            }
            else if (assortment.assortmentType == AssortmentType.Table)
            {
                //Position on table relative to the table point
                var yoffset = (product.TablePoint.transform.position.y - product.transform.position.y);
                var y = 0 - yoffset;
                product.transform.localPosition = new Vector3(insertPosition.x, y, insertPosition.z);

                product.IsProductPlacementOrigin = original.IsProductPlacementOrigin;

                if (original.IsProductPlacementOrigin)
                {
                }
            }

            //state of social media button
            SocialMediaCanvas smCanvas = product.gameObject.GetComponentInChildren<SocialMediaCanvas>(true);

            if(smCanvas != null)
            {
                smCanvas.gameObject.SetActive(AppManager.Instance.Settings.playerSettings.useAssortmentSocialMedia);
            }

            //Sync product with remote users if we added it ourselves
            if (!remoteInserted)
            {
                // Can refactor this so we dont actually create the product until now 
                Destroy(product.gameObject);

                ProductMesh mesh = product.ProductMesh.GetComponent<ProductMesh>();
                AssortmentSync.Instance.SyncAddProduct(product.settings.ProductCode, product.transform.localPosition, assortmentIndex, mesh.ProductPlacementCollection, mesh.UniqueProductPlacementID < 0 ? "" : mesh.UniqueProductPlacementID.ToString(), mesh.ProductPlacementShop);
            }
        }

        /// <summary>
        /// Add a product to the assortment via remote user or from server
        /// </summary>
        /// <param name="productCode">product to insert</param>
        /// <param name="assortmentIndex">index of assortment to insert into</param>
        /// <param name="insertPosition">position to insert new product</param>
        /// <param name="insertID">the unique id of the product to insert</param>
        public void RemoteAddToAssortment(string productCode, int assortmentIndex, Vector3 insertPosition, int insertID, string collection = "", string productplacementID = "", string shop = "")
        {
            ProductPlacement placement = ProductPlacementManager.Instance.GetProductPlacement(collection, shop);

            if (placement != null)
            {
                ProductPlacement.ProductPlacementObject rawObj = placement.GetPlacementObject(int.Parse(productplacementID));

                if (rawObj != null)
                {
                    AddToAssortment(placement, rawObj, assortmentIndex, insertPosition, true, insertID);
                    return;
                }
            }

            //Insert product by product code, for products added by remote users
            var product = ProductManager.Instance.FindProduct(productCode);

            if (product == null)
            {
                Debug.LogError("Error: trying to insert product code which doesnt exist: " + productCode);

            }
            else
            {
                AddToAssortment(product, assortmentIndex, insertPosition, true, insertID);
            }
        }

        /// <summary>
        /// Remove a product from an assortment
        /// </summary>
        /// <param name="product">the original product to remove</param>
        /// <param name="remoteRemove">whether the remove was triggered by remote user</param>
        public void RemoveFromAssortment(Product product, bool remoteRemove = false)
        {
            //remove a product from the assortment and destroy it

            removingFromAssortmentThisFrame = true;

            var assortmentIndex = product.currentAssortment;

            if (!remoteRemove)
            {
                AssortmentSync.Instance.SyncRemoveProduct(product);
            }

            Destroy(product.gameObject);

            //re sort the products Z offset to account for the removed product
            GetAssortment(assortmentIndex).SortAssortment();
        }

        /// <summary>
        /// Renove product from an assortment via remote user
        /// </summary>
        /// <param name="productCode">original product code to remove</param>
        /// <param name="assortmentIndex">assortment to remove from</param>
        /// <param name="insertID">unique id of the product to remove</param>
        public void RemoteRemoveFromAssortment(string productCode, int assortmentIndex, int insertID)
        {
            //remove product by product code, for products removed by remote users

            var product = GetAssortment(assortmentIndex).FindProduct(productCode, insertID);

            if (product != null)
            {
                RemoveFromAssortment(product, true);

            }
            else
            {
                Debug.Log("Tried to remove product that didnt exist. Cleaned up?");
            }
        }

        /// <summary>
        /// Return an assortment by index
        /// </summary>
        /// <param name="assortmentIndex">index of the assortment to return</param>
        /// <returns></returns>
        public Assortment GetAssortment(int assortmentIndex)
        {
            for(int i = 0; i < Assortments.Count; i++)
            {
                if (Assortments[i].overridingIndex.Equals(assortmentIndex))
                {
                    return Assortments[i];
                }
            }

            return null;
        }

        /// <summary>
        /// intialize the assortments to store their indexes
        /// </summary>
        private void initializeAssortments()
        {
            if (Assortments.Count > 0)
            {
                for (int i = 0; i < Assortments.Count; i++)
                {
                    Assortments[i].overridingIndex = i;
                }
            }
        }

        /// <summary>
        /// Create a new product inside an assortment
        /// </summary>
        /// <param name="original">original product to clone</param>
        /// <param name="assortmentIndex">assortment to create it in</param>
        /// <param name="insertID">unique id of the new product</param>
        /// <returns></returns>
        private Product createAssortmentProduct(Product original, int assortmentIndex, int insertID = -1, bool instantiate = true)
        {
            //Clone and init a product into the given assortment index
            var assortmentTransform = GetAssortment(assortmentIndex).AssortmentParent;

            Product product = null;

            if (instantiate)
            {
                product = GameObject.Instantiate(original.gameObject, assortmentTransform.position, Quaternion.identity, assortmentTransform).GetComponent<Product>();
            }
            else
            {
                product = original;
                product.transform.SetParent(assortmentTransform);
                product.transform.position = assortmentTransform.position;
            }

            product.transform.localScale = original.transform.localScale;
            product.transform.localRotation = Quaternion.identity;
            product.inAssortment = true;
            product.isHeld = false;
            product.currentAssortment = assortmentIndex;
            product.name = product.settings.ProductCode;
            product.HideTags(true);
            product.ProductMesh.layer = LayerMask.NameToLayer("Default");

            if (insertID != -1) { product.InsertID = insertID; }

            return product;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AssortmentManager), true)]
        public class AssortmentManager_Editor : BaseInspectorEditor
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