using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ProductManager : Singleton<ProductManager>, IRaycaster
    {
        public static ProductManager Instance
        {
            get
            {
                return ((ProductManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        [HideInInspector]
        public bool isHolding = false;

        [HideInInspector]
        public Product heldProduct;

        [Header("Interaction")]
        private float interactionDistance = 5;
        private bool useLocalDistance = true;

        private InfoTag2DSystem tagSystem;

        public bool OverrideDistance { get { return useLocalDistance; } }

        private bool pickedUpThisFrame = false;
        public ProductMesh PrevProduct { get; set; }
        private Dictionary<string, Product> allProducts = new Dictionary<string, Product>();
        private ProductMesh currenTagOpened = null;

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
            findAllProducts();
            HideAllProductTags();

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
            //bool hitProductThisFrame = false;
            var productMesh = hit.transform.GetComponent<ProductMesh>();

            if (productMesh != null && !productMesh.product.inAssortment)
            {
                hitObject = productMesh.transform;
            }
            else
            {
                hitObject = null;
            }

            if (productMesh != null && !productMesh.product.isHeld && !productMesh.product.inAssortment)
            {
                //check user
                string user = PlayerManager.Instance.GetLocalPlayer().CustomizationData.ContainsKey(m_userKey) ? PlayerManager.Instance.GetLocalPlayer().CustomizationData[m_userKey].ToString() : "";

                if (!productMesh.product.CanUserControlThis(user))
                {
                    return;
                }

                if (!productMesh.product.TagsOpen())
                {
                    AssortmentManager.Instance.HideAllAssortmentTags();
                    HideAllProductTags();
                }

                //hitProduct = true;
                // hitProductThisFrame = true;
                productMesh.product.ShowTags(hit.point);

                if (AssortmentManager.Instance.RayCastProduct != null)
                {
                    AssortmentManager.Instance.RayCastProduct.product.HideTags(false);
                    AssortmentManager.Instance.RayCastProduct = null;
                }

                if (PrevProduct != null && PrevProduct != productMesh)
                {
                    PrevProduct.product.HideTags(false);
                }

                PrevProduct = productMesh;
                currenTagOpened = PrevProduct;
            }

            //If no product hit this frame, clear the infotags on the prev hit product
            //if (hitProduct && !hitProductThisFrame && prevProduct != null)
            //{
            //  hitProduct = false;
            // prevProduct.HideTags(false);
            // }

            if (InputManager.Instance.GetMouseButtonUp(0))
            {
                insertHeldProduct();
            }

            // We stored if it was picked up this frame so it doesnt get reinserted into assortment
            if (pickedUpThisFrame)
            {
                pickedUpThisFrame = false;
            }
        }

        public void RaycastMiss()
        {
            // We stored if it was picked up this frame so it doesnt get reinserted into assortment
            if (pickedUpThisFrame)
            {
                pickedUpThisFrame = false;
            }

            if (currenTagOpened != null)
            {
                currenTagOpened.product.WaitAndHideTags();
                currenTagOpened = null;
            }
        }

        public void Show2DTagSystem(bool show, Product prod = null)
        {
            if (tagSystem == null)
            {
                tagSystem = HUDManager.Instance.GetHUDScreenObject("INFOTAG_CONTROL").GetComponentInChildren<InfoTag2DSystem>(true);
            }

            if (show)
            {
                tagSystem.Show(prod);
            }
            else
            {
                tagSystem.Hide();
            }

            HUDManager.Instance.ToggleHUDControl("INFOTAG_CONTROL", show);
        }

        /// <summary>
        /// Pickup / hold a product
        /// </summary>
        /// <param name="original">The original product to pickup</param>
        public void PickupProduct(Product original)
        {
            Debug.Log("PickupProduct: Product code= " + original.settings.ProductCode);

            if (!isHolding)
            {
                //drop item if player is currently holding item
                if (ItemManager.Instance.IsHolding)
                {
                    ItemManager.Instance.Drop3D();
                }

                original.HideTags(false);

                //instantiate and initialize the new product
                var parent = PlayerManager.Instance.GetLocalPlayer().MainProductHolder.transform;
                ProductMesh mesh = original.ProductMesh.GetComponent<ProductMesh>();

                if (original.IsProductPlacementOrigin)
                {
                    ProductPlacement placement = ProductPlacementManager.Instance.GetProductPlacement(mesh.ProductPlacementCollection, mesh.ProductPlacementShop);

                    if (placement != null)
                    {
                        ProductPlacement.ProductPlacementObject rawObj = placement.GetPlacementObject(mesh.UniqueProductPlacementID);

                        if (rawObj != null)
                        {
                            heldProduct = placement.CreateProductOperation(rawObj);
                            heldProduct.transform.SetParent(parent);

                            //need to check what placement type it is
                            if(placement.settings.placementType.Equals(ProductPlacement.ProductPlacementType.Rail))
                            {
                                heldProduct.transform.localPosition = new Vector3(0, 0 + heldProduct.ProductMesh.GetComponent<BoxCollider>().bounds.extents.y, 0);
                            }
                            else if(placement.settings.placementType.Equals(ProductPlacement.ProductPlacementType.Table))
                            {
                                heldProduct.transform.localPosition = new Vector3(0, 0 - heldProduct.ProductMesh.GetComponent<BoxCollider>().bounds.extents.y, 0);
                            }
                            else
                            {
                                heldProduct.transform.localPosition = Vector3.zero;
                            }
                        }
                    }
                }
                else
                {
                    heldProduct = GameObject.Instantiate(original.gameObject, parent.position, Quaternion.identity, parent).GetComponent<Product>();
                }

                if(heldProduct != null)
                {
                    heldProduct.transform.localRotation = Quaternion.identity;
                    heldProduct.transform.localPosition = Vector3.zero;
                    heldProduct.transform.localScale = original.transform.localScale;
                    heldProduct.isHeld = true;
                    heldProduct.inAssortment = false;
                    heldProduct.HideTags(false);

                    AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Product, EventAction.Click, heldProduct.AnalyticReference);
                }

                isHolding = true;

                if (original.inAssortment)
                {
                    AssortmentManager.Instance.RemoveFromAssortment(original);
                }

                pickedUpThisFrame = true;
                HUDManager.Instance.ShowHUDNavigationVisibility(false);

                DropPanel dropPanel = HUDManager.Instance.GetHUDControlObject("DROP").GetComponentInChildren<DropPanel>(true);
                dropPanel.SetStrings(AppManager.Instance.Settings.playerSettings.showDropInstruction,
                    AppManager.Instance.Settings.playerSettings.defaulDropTitle,
                    AppManager.Instance.Settings.playerSettings.defaultDropMessage,
                    AppManager.Instance.Settings.playerSettings.defaultDropButton);

                HUDManager.Instance.ToggleHUDControl("DROP", true);
            }
        }

        /// <summary>
        /// Drop the held product
        /// </summary>
        public void DropProduct()
        {
            if (isHolding)
            {
                Debug.Log("DropProduct: Product code= " + heldProduct.settings.ProductCode);

                Destroy(heldProduct.gameObject);
                heldProduct = null;
                isHolding = false;
                HUDManager.Instance.ToggleHUDControl("DROP", false);
                HUDManager.Instance.ShowHUDNavigationVisibility(true);
            }

            // Remove this and handle itself in pickup manager
            //PickupManager.Instance.Drop3D();
        }

        /// <summary>
        /// Update product position when it's moved by a remote user
        /// </summary>
        /// <param name="productCode">the target product code</param>
        /// <param name="localPosition">the new poisition to move it to</param>
        /// <param name="assortmentIndex">the target assortment index</param>
        /// <param name="insertID">the unique id of the target product</param>
        public void RemoteUpdateProduct(string productCode, Vector3 localPosition, int assortmentIndex, int insertID)
        {
            Debug.Log("RemoteUpdateProduct: Product code= " + productCode);

            var assortment = AssortmentManager.Instance.GetAssortment(assortmentIndex);
            var product = assortment.FindProduct(productCode, insertID);

            if (assortment != null && product != null && product.inAssortment)
            {
                product.SetMoveTarget(localPosition);
            }
        }

        /// <summary>
        /// Find a product within the list of products
        /// </summary>
        /// <param name="productCode"></param>
        /// <returns></returns>
        public Product FindProduct(string productCode)
        {
            Product result = null;

            if (allProducts.ContainsKey(productCode))
            {
                result = allProducts[productCode];
            }

            return result;
        }

        /// <summary>
        /// Find all products in the scene (ignores duplicates)
        /// </summary>
        private void findAllProducts()
        {
            Product[] products = FindObjectsByType<Product>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (Product product in products)
            {
                product.HideTags(true);

                if (!allProducts.ContainsKey(product.settings.ProductCode))
                {
                    allProducts.Add(product.settings.ProductCode, product);
                }
            }
        }

        public void AddNewProduct(Product product)
        {
            if (!allProducts.ContainsKey(product.settings.ProductCode))
            {
                allProducts.Add(product.settings.ProductCode, product);
            }
        }

        public void RemoveExistingProduct(Product product)
        {
            if (allProducts.ContainsKey(product.settings.ProductCode))
            {
                allProducts.Remove(product.settings.ProductCode);
            }
        }

        public void HideAllProductTags()
        {
            foreach (KeyValuePair<string, Product> p in allProducts)
            {
                p.Value.HideTags(false);

                if (CoreManager.Instance.projectSettings.usePersistentInfotags && !p.Value.inAssortment)
                {
                    p.Value.ShowProductTag(true);
                }
            }
        }

        /// <summary>
        ///  Insert held product into assortment
        /// </summary>
        private void insertHeldProduct()
        {
            if (isHolding && !pickedUpThisFrame)
            {
                var dist = AssortmentManager.Instance.Distance;
                var ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMousePosition());
                var hits = Physics.RaycastAll(ray, dist);

                AssortmentBounds bounds = null;
                float curDist = float.MaxValue;
                Vector3 hitPoint = Vector3.zero;

                //check each bounds that was hit

                Debug.Log("insertHeldProduct: Product code= " + heldProduct.settings.ProductCode);

                foreach (RaycastHit hit in hits)
                {
                    var hitBounds = hit.transform.gameObject.GetComponent<AssortmentBounds>();
                    if (hitBounds != null)
                    {
                        //get the distance to camera

                        float hitDist = Vector3.Distance(PlayerManager.Instance.GetLocalPlayer().MainCamera.transform.position, hit.point);

                        if (hitDist < curDist)
                        {
                            //store if its nearer
                            curDist = hitDist;
                            bounds = hitBounds;
                            hitPoint = hit.point;
                        }
                    }
                }

                if (bounds != null)
                {
                    // if we hit the bounds, transform the hitpoint to the assortment local space and insert the product here

                    var hitAssortment = bounds.ParentAssortment;
                    var transPos = hitAssortment.transform.InverseTransformPoint(hitPoint);


                    //need to identify if its a product placement
                    if(heldProduct.IsProductPlacementOrigin)
                    {
                        ProductMesh mesh = heldProduct.ProductMesh.GetComponent<ProductMesh>();
                        ProductPlacement placement = ProductPlacementManager.Instance.GetProductPlacement(mesh.ProductPlacementCollection, mesh.ProductPlacementShop);

                        if (placement != null)
                        {
                            ProductPlacement.ProductPlacementObject rawObj = placement.GetPlacementObject(mesh.UniqueProductPlacementID);

                            if (rawObj != null)
                            {
                                AssortmentManager.Instance.AddToAssortment(placement, rawObj, hitAssortment.overridingIndex, transPos);
                            }
                        }
                    }
                    else
                    {
                        AssortmentManager.Instance.AddToAssortment(heldProduct, hitAssortment.overridingIndex, transPos);
                    }

                    DropProduct();
                }
            }
        }

        public class ProductTagCreator
        {
            public void Create(GameObject productRoot, Bounds box, Product product, bool useColliderCenter = true)
            {
                //Add the assortment canvas 
                UnityEngine.Object prefabAssortmentCanvas = Resources.Load("Product/AssortmentCanvas");
                GameObject assortmentCanvas = (GameObject)GameObject.Instantiate(prefabAssortmentCanvas, Vector3.zero, Quaternion.identity);
                assortmentCanvas.transform.SetParent(productRoot.transform);
                assortmentCanvas.transform.localRotation = Quaternion.Euler(0, 0, 0);

                if (useColliderCenter)
                {
                    assortmentCanvas.transform.localPosition = new Vector3(box.center.x, box.center.y, -0.05f);
                }
                else
                {
                    assortmentCanvas.transform.localPosition = new Vector3(0, 0, 0 + (box.extents.z + 0.05f));
                }

                assortmentCanvas.gameObject.SetActive(false);
                product.AssortmentCanvas = assortmentCanvas;

                //Add the infotag canvas
                UnityEngine.Object prefab = Resources.Load("Product/InfotagCanvas");
                GameObject canvas = (GameObject)GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);
                canvas.transform.SetParent(productRoot.transform);
                canvas.transform.localRotation = Quaternion.Euler(0, 0, 0);

                if (useColliderCenter)
                {
                    canvas.transform.localPosition = new Vector3(0, box.center.y + 0.225f, -0.05f);
                }
                else
                {
                    canvas.transform.localPosition = new Vector3(0, 0 + 0.225f, 0 + (box.extents.z + 0.05f));
                }

                canvas.gameObject.SetActive(false);
                product.InfotagCanvas = canvas.GetComponent<InfotagMenu>();

                //add the on assortment delete canvas
                UnityEngine.Object prefabD = Resources.Load("Product/DeleteCanvas");
                GameObject deleteCanvas = (GameObject)GameObject.Instantiate(prefabD, Vector3.zero, Quaternion.identity);
                deleteCanvas.transform.SetParent(productRoot.transform);
                deleteCanvas.transform.localRotation = Quaternion.Euler(0, 0, 0);

                var adjustment = 0.0625f;
                if (box.min.x > 0) adjustment = -adjustment;

                if (useColliderCenter)
                {
                    deleteCanvas.transform.localPosition = new Vector3(box.min.x + adjustment, box.max.y - adjustment, 0 - 0.025f);

                }
                else
                {
                    deleteCanvas.transform.localPosition = new Vector3(0 - (box.extents.x), 0 + (box.extents.y), 0 + (box.extents.z + 0.05f));
                }

                deleteCanvas.gameObject.SetActive(false);
                product.DeleteCanvas = deleteCanvas;

                //add the on assortment pickup canvas
                UnityEngine.Object prefabP = Resources.Load("Product/PickupCanvas");
                GameObject pickupCanvas = (GameObject)GameObject.Instantiate(prefabP, Vector3.zero, Quaternion.identity);
                pickupCanvas.transform.SetParent(productRoot.transform);
                pickupCanvas.transform.localRotation = Quaternion.Euler(0, 0, 0);


                adjustment = 0.0625f;
                if (box.min.x > 0) adjustment = -adjustment;

                if (useColliderCenter)
                {
                    pickupCanvas.transform.localPosition = new Vector3(box.max.x - adjustment, box.max.y - adjustment, product.transform.position.z - 0.025f);

                }
                else
                {
                    pickupCanvas.transform.localPosition = new Vector3(0 + (box.extents.x), 0 + (box.extents.y), 0 + (box.extents.z + 0.05f));

                }

                pickupCanvas.gameObject.SetActive(false);
                product.PickupCanvas = pickupCanvas;

                //add the rail point for rail assortment
                GameObject railPoint = new GameObject();
                railPoint.transform.SetParent(productRoot.transform);

                if (useColliderCenter)
                {
                    railPoint.transform.localPosition = new Vector3(0, box.max.y, 0);
                }
                else
                {
                    railPoint.transform.localPosition = new Vector3(0, 0 + box.extents.y, 0);
                }

               
                railPoint.transform.localRotation = Quaternion.Euler(90f, 180f, 0);
                railPoint.name = "RailPoint";
                product.RailPoint = railPoint;

                //add the table point for table assortment
                GameObject tablePoint = new GameObject();
                tablePoint.transform.SetParent(productRoot.transform);
                tablePoint.transform.localRotation = Quaternion.Euler(90f, 180f, 0);

                if (useColliderCenter)
                {
                    tablePoint.transform.localPosition = new Vector3(0, box.min.y, 0);
                }
                else
                {
                    tablePoint.transform.localPosition = new Vector3(0, 0 - box.extents.y, 0);
                }

               
                tablePoint.name = "TablePoint";
                product.TablePoint = tablePoint;

                //add the hold point for avatar holding
                GameObject holdPoint = new GameObject();
                holdPoint.transform.SetParent(productRoot.transform);
                holdPoint.transform.localRotation = Quaternion.Euler(90f, 180f, 0);

                if (useColliderCenter)
                {
                    holdPoint.transform.localPosition = new Vector3(box.max.x, 0, 0);
                }
                else
                {
                    holdPoint.transform.localPosition = new Vector3(0 + box.extents.x, 0, 0);
                }
                
                holdPoint.name = "HoldPoint";
                product.HoldPoint = holdPoint;

                if(product.settings.WebInfotagsUrls.Count > 0 || product.settings.ImageInfotagsUrls.Count > 0
                    || product.settings.VideoInfotagsUrls.Count > 0)
                {
                    bool createSMButton = false;

                    if (Application.isPlaying)
                    {
                        createSMButton = AppManager.Instance.Settings.socialMediaSettings.socialMediaEnabled;
                    }
                    else
                    {
                        //need to get the settings resource
                        AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");

                        if (appReferences != null)
                        {
                            createSMButton = appReferences.Settings.socialMediaSettings.socialMediaEnabled;
                        }
                        else
                        {
                            createSMButton = Resources.Load<AppSettings>("ProjectAppSettings").socialMediaSettings.socialMediaEnabled;
                        }
                    }

                    if(createSMButton)
                    {
                        UnityEngine.Object smPrefab = Resources.Load("Canvas_SocialMediaProduct");

                        if (smPrefab)
                        {
                            GameObject go = Instantiate(smPrefab as GameObject);
                            go.transform.SetParent(productRoot.transform);
                            go.transform.localRotation = Quaternion.Euler(Vector3.zero);

                            if (useColliderCenter)
                            {
                                go.transform.localPosition = new Vector3(0, box.max.y + 0.1f, 0);
                            }
                            else
                            {
                                go.transform.localPosition = new Vector3(0, 0 + box.extents.y + 0.1f, 0);
                            }

                            SocialMediaCanvas smCanvas = go.GetComponentInChildren<SocialMediaCanvas>(true);
                            smCanvas.Reference = product.settings.ProductCode;
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ProductManager), true)]
        public class ProductManager_Editor : BaseInspectorEditor
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