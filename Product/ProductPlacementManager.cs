using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ProductPlacementManager : Singleton<ProductPlacementManager>
    {
        [Header("Source")]
        [SerializeField]
        private Material quadMeshMaterial;

        private float productCreationDistance = 10;

        public static ProductPlacementManager Instance
        {
            get
            {
                return ((ProductPlacementManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        public float CreationDistance { get { return productCreationDistance; } }

        public Material GetMaterial()
        {
            return quadMeshMaterial;
        }

        public ProductPlacement AdminController
        {
            get;
            set;
        }

        private bool draggingItem = false;
        private Vector3 dragOrigin = Vector3.zero;
        private bool hasMoved = false;
        private ProductMesh currentProduct;
        private float minXMovement;
        private float minYMovement;
        private float minZMovement;
        private float maxXMovement;
        private float maxYMovement;
        private float maxZMovement;
        private float ZSortOffset = 0.0025f;
        private float minScale = 0.5f;

        private bool selectModeOn = false;
        private ProductPlacement[] m_placements;

        private Vector2 prevMousePosition = Vector2.zero;
        private bool m_scalingObject = false;
        private ProductMesh m_scalingMesh;
        private Product m_pickupProduct;

        private bool m_movingObjectOrigin = false;
        private Product heldProduct;
        private ProductPlacement.ProductPlacementObject m_cacheObject;

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            m_placements = FindObjectsByType<ProductPlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            productCreationDistance = AppManager.Instance.Settings.playerSettings.productPlacementPlayerDistance;
        }


        private void Update()
        {
            //check if all productplacements are in range of distance, if true make products visible
            if(AppManager.Instance.Data.RoomEstablished && AppManager.Instance.Data.SceneSpawnLocation == null)
            {
                for (int i = 0; i < m_placements.Length; i++)
                {
                    float distance = 0;

                    if (PlayerManager.Instance.GetLocalPlayer() != null && PlayerManager.Instance.GetLocalPlayer().TransformObject != null)
                    {
                        distance = Vector3.Distance(m_placements[i].transform.position, PlayerManager.Instance.GetLocalPlayer().TransformObject.position);
                    }

                    if (distance <= CreationDistance)
                    {
                        if (!m_placements[i].CreatingProductStarted && !m_placements[i].DestroyingProductStarted)
                        {
                            if(m_placements[i].settings.creationType.Equals(ProductPlacement.ProductCreationType.PlayerDistance))
                            {
                                m_placements[i].MakeProductsVisible(true);
                            }
                        }
                    }
                    else
                    {
                        if (m_placements[i].settings.creationType.Equals(ProductPlacement.ProductCreationType.PlayerDistance))
                        {
                            if (!m_placements[i].CreatingProductStarted && !m_placements[i].DestroyingProductStarted)
                            {
                                m_placements[i].MakeProductsVisible(false);
                            }
                        }

                        if (AdminController != null && AdminController.Equals(m_placements[i]))
                        {
                            FinishPlacementControl();
                        }
                    }
                }
            }

            if(AdminController != null)
            {
                //admin can now control the placement of the prodct on the current controller
                if(RaycastManager.Instance.CastRay)
                {
                    RaycastManager.Instance.CastRay = false;
                }

                if (EventSystem.current.IsPointerOverGameObject())
                {
                    bool cancel = false;
                    GameObject hoveredObject = InputManager.Instance.HoveredObject(InputManager.Instance.GetMousePosition());

                    if (hoveredObject)
                    {
                        Canvas canvas = hoveredObject.GetComponent<Canvas>();

                        if (canvas == null)
                        {
                            //check if there is one in the parent
                            canvas = hoveredObject.GetComponentInParent<Canvas>();
                        }

                        if (canvas != null)
                        {
                            if (canvas.renderMode.Equals(RenderMode.WorldSpace))
                            {
                                cancel = true;
                            }
                        }
                    }

                    if(cancel)
                    { return; }
                }

                if (!InputManager.Instance.CheckWithinViewport())
                {
                    return;
                }

                Camera cam = Camera.main;
                Ray ray = cam.ScreenPointToRay(InputManager.Instance.GetMousePosition());
                RaycastHit hit;

                if(m_movingObjectOrigin)
                {
                    float distance = CoreManager.Instance.playerSettings.interactionDistance;

                    if (Physics.Raycast(ray, out hit, distance))
                    {
                        //admin can now control the placement of the prodct on the current controller
                        if (InputManager.Instance.GetMouseButton(0) && !PlayerManager.Instance.GetLocalPlayer().IsButtonHeldDown)
                        {
                            var hitBounds = hit.transform.GetComponentInParent<ProductPlacement>();

                            if(hitBounds)
                            {
                                DropProductInPlacement(hitBounds.ID);
                            }
                        }
                    }

                    return;
                }

                if (!draggingItem)
                {
                    float distance = CoreManager.Instance.playerSettings.interactionDistance;

                    if (Physics.Raycast(ray, out hit, distance))
                    {
                        //admin can now control the placement of the prodct on the current controller
                        if (InputManager.Instance.GetMouseButton(0) && !PlayerManager.Instance.GetLocalPlayer().IsButtonHeldDown)
                        {
                            if(m_pickupProduct != null)
                            {
                                m_pickupProduct.ShowPickuptags(false);
                                m_pickupProduct = null;
                            }

                            if (selectModeOn)
                            {
                                //meaning it could scale the product
                                if (hit.transform.name.Equals("ScalingPoint") && !m_scalingObject)
                                {
                                    Product mesh = hit.transform.GetComponentInParent<Product>();

                                    if (mesh != null)
                                    {
                                        var hitBounds = mesh.gameObject.GetComponentInParent<ProductPlacement>();
                                        AssignBounds(hitBounds);
                                        m_scalingObject = true;
                                        m_scalingMesh = mesh.ProductMesh.GetComponent<ProductMesh>();
                                    }

                                    return;
                                }
                                else
                                {
                                    if (m_scalingObject && m_scalingMesh != null)
                                    {
                                        ScaleProduct(m_scalingMesh);
                                        return;
                                    }
                                }
                            }

                            if (currentProduct == null)
                            {
                                currentProduct = hit.transform.GetComponent<ProductMesh>();
                                
                                if(currentProduct != null)
                                {
                                    draggingItem = true;
                                    dragOrigin = InputManager.Instance.GetMousePosition();
                                    var hitBounds = currentProduct.gameObject.GetComponentInParent<ProductPlacement>();

                                    //assign bounds values
                                    if (hitBounds != null)
                                    {
                                        AssignBounds(hitBounds);
                                    }
                                    return;
                                }
                            }
                        }
                        else
                        {
                            //need to show the pickup tag;
                            ProductMesh mesh = hit.transform.GetComponent<ProductMesh>();

                            if(mesh != null && !selectModeOn)
                            {
                                m_pickupProduct = mesh.product;
                                m_pickupProduct.ShowPickuptags(true);
                            }
                            else
                            {
                                if (m_pickupProduct != null)
                                {
                                    m_pickupProduct.ShowPickuptags(false);
                                    m_pickupProduct = null;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (m_pickupProduct != null)
                        {
                            m_pickupProduct.ShowPickuptags(false);
                            m_pickupProduct = null;
                        }
                    }
                }
                else
                {
                    if (currentProduct != null)
                    {
                        float distance = CoreManager.Instance.playerSettings.interactionDistance;

                        ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMousePosition());
                        RaycastHit[] hits = Physics.RaycastAll(ray, distance);

                        foreach (RaycastHit ht in hits)
                        {
                            var hitBounds = ht.transform.gameObject.GetComponentInParent<ProductPlacement>();

                            if (hitBounds != null)
                            {
                                if (ht.transform.GetComponent<Lock>() != null || !ht.transform.Equals(AdminController.placementBounds.transform)) continue;

                                //transform the hit point on the bounds into local space for the product pos
                                var transPos = currentProduct.product.transform.parent.InverseTransformPoint(ht.point);
                                var z = -ZSortOffset * currentProduct.product.Sort;
                                var previousPosition = currentProduct.product.transform.localPosition;

                                if (Vector3.Distance(dragOrigin, InputManager.Instance.GetMousePosition()) > 0.01f)
                                {
                                    hasMoved = true;

                                    if (AdminController.settings.placementType.Equals(ProductPlacement.ProductPlacementType.Table))
                                    {
                                        currentProduct.product.transform.localPosition = new Vector3(transPos.x, previousPosition.y, transPos.z);
                                    }
                                    else
                                    {
                                        currentProduct.product.transform.localPosition = new Vector3(transPos.x, transPos.y, z);
                                    }

                                    //break out so we dont hit an product behind
                                    Collider col = currentProduct.GetComponent<Collider>();
                                    float xminExtents = currentProduct.product.transform.localPosition.x - Mathf.Abs(col.bounds.extents.x);
                                    float xmaxExtents = currentProduct.product.transform.localPosition.x + Mathf.Abs(col.bounds.extents.x);
                                    float yminExtents = currentProduct.product.transform.localPosition.y - Mathf.Abs(col.bounds.extents.y);
                                    float ymaxExtents = currentProduct.product.transform.localPosition.y + Mathf.Abs(col.bounds.extents.y);
                                    float zminExtents = currentProduct.product.transform.localPosition.z - Mathf.Abs(col.bounds.extents.z);
                                    float zmaxExtents = currentProduct.product.transform.localPosition.z + Mathf.Abs(col.bounds.extents.z);

                                    if(AdminController.settings.placementType.Equals(ProductPlacement.ProductPlacementType.Wall))
                                    {
                                        //need to check if the current product bounds exceeds assortmentbounds
                                        if (!BoundsXIsEncapsulated(xminExtents, xmaxExtents))
                                        {
                                            currentProduct.product.transform.localPosition = new Vector3(previousPosition.x, currentProduct.product.transform.localPosition.y, currentProduct.product.transform.localPosition.z);
                                        }

                                        if (!BoundsYIsEncapsulated(yminExtents, ymaxExtents))
                                        {
                                            currentProduct.product.transform.localPosition = new Vector3(currentProduct.product.transform.localPosition.x, previousPosition.y, currentProduct.product.transform.localPosition.z);
                                        }
                                    }
                                    else if(AdminController.settings.placementType.Equals(ProductPlacement.ProductPlacementType.Rail))
                                    {
                                        //need to check if the current product bounds exceeds assortmentbounds
                                        if (!BoundsXIsEncapsulated(xminExtents, xmaxExtents))
                                        {
                                            currentProduct.product.transform.localPosition = new Vector3(previousPosition.x, previousPosition.y, previousPosition.z);
                                        }
                                        else
                                        {
                                            currentProduct.product.transform.localPosition = new Vector3(transPos.x, previousPosition.y, previousPosition.z);
                                        }
                                    }
                                    else
                                    {
                                        //need to check if the current product bounds exceeds assortmentbounds
                                        if (!BoundsXIsEncapsulated(xminExtents, xmaxExtents))
                                        {
                                            currentProduct.product.transform.localPosition = new Vector3(previousPosition.x, currentProduct.product.transform.localPosition.y, currentProduct.product.transform.localPosition.z);
                                        }

                                        if (!BoundsZIsEncapsulated(zminExtents, zmaxExtents))
                                        {
                                            currentProduct.product.transform.localPosition = new Vector3(currentProduct.product.transform.localPosition.x, currentProduct.product.transform.localPosition.y, previousPosition.z);
                                        }
                                    }

                                }

                                //break out so we dont hit an product behind
                                break;
                            }
                        }
                    }
                }

                if (InputManager.Instance.GetMouseButtonUp(0))
                {
                    if(selectModeOn)
                    {
                        float distance = CoreManager.Instance.playerSettings.interactionDistance;

                        if (Physics.Raycast(ray, out hit, distance))
                        {
                            if (!m_scalingObject)
                            {
                                ProductMesh mesh = hit.transform.GetComponent<ProductMesh>();

                                if (mesh != null)
                                {
                                    if (!hasMoved)
                                    {
                                        //add this to the productplacement
                                        if (AdminController.SelectedProducts.Contains(mesh.product))
                                        {
                                            CreateVisibleBoundingBox(-1, mesh.GetComponent<BoxCollider>(), false);
                                            AdminController.SelectedProducts.Remove(mesh.product);
                                        }
                                        else
                                        {
                                            CreateVisibleBoundingBox(mesh.UniqueProductPlacementID, mesh.GetComponent<BoxCollider>(), true);
                                            AdminController.SelectedProducts.Add(mesh.product);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                ProductPlacement.ProductPlacementObject pObject = AdminController.GetPlacementObject(m_scalingMesh.UniqueProductPlacementID);
                                
                                if (pObject != null)
                                {
                                    ProductPlacementSync.Instance.SyncScaleProduct(AdminController.ID, pObject.id, m_scalingMesh.product);

                                    pObject.scale = m_scalingMesh.product.transform.localScale;
                                    pObject.position = m_scalingMesh.product.transform.localPosition;

                                    //update API
                                    ProductAPI.Instance.SyncProduct(AdminController.ID, pObject);
                                }

                                m_scalingObject = false;
                                m_scalingMesh = null;
                                prevMousePosition = Vector2.zero;
                            }
                        }
                    }

                    if (draggingItem)
                    {
                        ProductPlacement.ProductPlacementObject pObject = AdminController.GetPlacementObject(currentProduct.UniqueProductPlacementID);

                        if (pObject != null)
                        {
                            ProductPlacementSync.Instance.SyncPositionProduct(AdminController.ID, pObject.id, currentProduct.product);

                            pObject.scale = currentProduct.product.transform.localScale;
                            pObject.position = currentProduct.product.transform.localPosition;

                            //update API
                            ProductAPI.Instance.SyncProduct(AdminController.ID, pObject);
                        }
                    }

                    currentProduct = null;
                    draggingItem = false;
                    dragOrigin = Vector3.zero;
                    hasMoved = false;
                }
            }
        }

        private void ScaleProduct(ProductMesh mesh)
        {
            Vector2 mousePosition = InputManager.Instance.GetMousePosition();

            if (prevMousePosition.Equals(Vector3.zero))
            {
                prevMousePosition = mousePosition;
            }

            if (Vector2.Distance(mousePosition, prevMousePosition) > 0.1f)
            {
                float scaleFactor = 0.03f;

                //need to check if scale is out of the bounds
                Collider col = mesh.GetComponent<Collider>();
                Vector3 previousPosition = mesh.product.transform.localPosition;

                float xminExtents = mesh.product.transform.localPosition.x - Mathf.Abs(col.bounds.extents.x + scaleFactor);
                float xmaxExtents = mesh.product.transform.localPosition.x + Mathf.Abs(col.bounds.extents.x + scaleFactor);
                float yminExtents = mesh.product.transform.localPosition.y - Mathf.Abs(col.bounds.extents.y + scaleFactor);
                float ymaxExtents = mesh.product.transform.localPosition.y + Mathf.Abs(col.bounds.extents.y + scaleFactor);

                //need to check if the current product bounds exceeds assortmentbounds

                bool Yencapsulated = (AdminController.settings.placementType.Equals(ProductPlacement.ProductPlacementType.Table)) ? true : BoundsYIsEncapsulated(yminExtents, ymaxExtents);

                if (BoundsXIsEncapsulated(xminExtents, xmaxExtents) && Yencapsulated)
                {
                    // Change the scale of mainObject by comparing previous frame mousePosition with t$$anonymous$$s frame's position, modified by sizingFactor.
                    Vector3 scale = mesh.product.transform.localScale;
                    scale.x = scale.x + (mousePosition.x - prevMousePosition.x) * scaleFactor;
                    scale.y = scale.y + (mousePosition.x - prevMousePosition.x) * scaleFactor;

                    if (Vector2.Distance(scale, Vector2.zero) >= minScale)
                    {
                        mesh.product.transform.localScale = scale;
                    }
                    else
                    {
                        mesh.product.transform.localScale = new Vector3(minScale, minScale, minScale);
                    }
                }
                else
                {
                    Vector3 scale = mesh.product.transform.localScale;
                    scale.x = scale.x - scaleFactor;
                    scale.y = scale.y - scaleFactor;
                    mesh.product.transform.localScale = scale;
                    mesh.product.transform.localPosition = previousPosition;
                }
            }
            else
            {
                if (Vector2.Distance(mesh.product.transform.localScale, Vector2.zero) < minScale)
                {
                    mesh.product.transform.localScale = new Vector3(minScale, minScale, minScale);
                }
            }

            prevMousePosition = mousePosition;
        }

        public void PickUpProduct(Product prod)
        {
            //need to duplicate the product and then delete this product from admin controller (product placement)

            if (AdminController != null && !m_movingObjectOrigin)
            {
                //turn off all deselected
                int count = AdminController.SelectedProducts.Count;
                for (int i = 0; i < count; i++)
                {
                    CreateVisibleBoundingBox(-1, AdminController.SelectedProducts[i].ProductMesh.GetComponent<BoxCollider>(), false);
                }

                AdminController.SelectedProducts.Clear();

                m_movingObjectOrigin = true;
                m_cacheObject = AdminController.GetPlacementObject(prod.ProductMesh.GetComponent<ProductMesh>().UniqueProductPlacementID);

                //instantiate and initialize the new product
                var parent = PlayerManager.Instance.GetLocalPlayer().MainProductHolder.transform;
                heldProduct = GameObject.Instantiate(prod.gameObject, parent.position, Quaternion.identity, parent).GetComponent<Product>();
                heldProduct.transform.SetParent(parent);
                heldProduct.transform.localRotation = Quaternion.identity;
                heldProduct.transform.localScale = prod.transform.localScale;
                heldProduct.isHeld = true;
                heldProduct.inAssortment = false;
                heldProduct.HideTags(false);

                bool materialsExists = GetMaterial() != null;
                CoreUtilities.GetShaderName();
                Shader shader = Shader.Find(CoreUtilities.ShaderName);
                Material material = !materialsExists ? new Material(shader) : new Material(GetMaterial());
                StartCoroutine(CoreUtilities.AttainTexture(m_cacheObject.textureURL, material));
                prod.ProductMesh.GetComponent<Renderer>().material = material;

                //delete from admincontroller
                AdminController.RemoveSingleProduct(m_cacheObject.id, true);

                //RPC to everyone
                ProductPlacementSync.Instance.SyncRemoveProduct(AdminController.ID, m_cacheObject.id);

                ProductPlacementControl controlPanel = HUDManager.Instance.GetHUDControlObject("PRODUCTPLACEMENT_CONTROL").GetComponentInChildren<ProductPlacementControl>(true);
                controlPanel.ToggleDropDisplay(true);
            }
        }

        public void DropProductInPlacement(string placementID)
        {
            if(AdminController != null)
            {
                bool sendRPC = false;

                if (AdminController.ID.Equals(placementID))
                {
                    //dropping product in previous placement
                    AdminController.PlaceSingleProduct(m_cacheObject);
                    sendRPC = true;
                }
                else
                {
                    ProductPlacement[] temp = FindObjectsByType<ProductPlacement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

                    for (int i = 0; i < temp.Length; i++)
                    {
                        if(temp[i].ID.Equals(placementID))
                        {
                            if(temp[i].settings.shop.Equals(AdminController.settings.shop))
                            {
                                m_cacheObject.position = Vector3.zero;
                                temp[i].PlaceSingleProduct(m_cacheObject);
                                ProductAPI.Instance.SyncProduct(placementID, m_cacheObject);
                                sendRPC = true;
                            }

                            break;
                        }
                    }
                }

                if(sendRPC)
                {
                    //RPC to everyone
                    ProductPlacementSync.Instance.SyncAddProduct(placementID, m_cacheObject);

                    m_movingObjectOrigin = false;
                    m_cacheObject = null;

                    if (heldProduct != null)
                    {
                        Destroy(heldProduct.gameObject);
                    }

                    heldProduct = null;

                    ProductPlacementControl controlPanel = HUDManager.Instance.GetHUDControlObject("PRODUCTPLACEMENT_CONTROL").GetComponentInChildren<ProductPlacementControl>(true);
                    controlPanel.ToggleDropDisplay(false);
                }
            }
        }

        public void ToggleSelectMode(bool state)
        {
            selectModeOn = state;

            if(!selectModeOn)
            {
                //loop through all the selected products on the placement and deselect
                if(AdminController != null)
                {
                    int count = AdminController.SelectedProducts.Count;
                    for (int i = 0; i < count; i++)
                    {
                        CreateVisibleBoundingBox(-1, AdminController.SelectedProducts[i].ProductMesh.GetComponent<BoxCollider>(), false);
                    }

                    AdminController.SelectedProducts.Clear();
                }
            }
        }

        public void DeleteSingleProduct(int productID)
        {
            if (AdminController != null)
            {
                List<int> tagValues = new List<int>();
                int[] values = new int[1] { productID };
                Product prod = AdminController.GetProduct(productID);
                tagValues.AddRange(AdminController.GetProductPlacementTagIDValues(prod.settings.ProductCode));

                ProductPlacement.ProductPlacementObject pObject = AdminController.GetPlacementObject(productID);
                ProductPlacementSync.Instance.SyncRemoveProduct(AdminController.ID, pObject.id);
                AdminController.RemoveSingleProduct(pObject.id, true);
                AdminController.SelectedProducts.Remove(prod);

                List<int> tagValuesAPI = CheckForDuplicateTags(tagValues);

                DeleteProductTexture(pObject.textureURL);

                //API call
                ProductAPI.Instance.DeleteProducts(values);
                //API to delete info tags
                InfoTagAPI.Instance.DeleteInfoTags(tagValuesAPI);
            }
        }

        public List<int> CheckForDuplicateTags(List<int> tags)
        {
            ProductPlacement[] temp = FindObjectsByType<ProductPlacement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<int> tagValuesAPI = new List<int>();
            //for each tagvalue, need to check if any other productplacement product is using the same tag, if true exclude from API call
            for (int i = 0; i < tags.Count; i++)
            {
                bool exclude = temp.ToList().TrueForAll(x => x.DoesProductHaveTagID(tags[i]));

                if (!exclude)
                {
                    tagValuesAPI.Add(tags[i]);
                }
            }

            return tagValuesAPI;
        }

        public void DeleteProductTexture(string textureURL)
        {
            //need to identify if any product is using this url, if false then delete
            ProductPlacement[] placements = FindObjectsByType<ProductPlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool delete = true;

            for(int i = 0; i < placements.Length; i++)
            {
                foreach(ProductPlacement.ProductPlacementObject obj in placements[i].GetProductRawObjects())
                {
                    if (obj.textureURL.Equals(textureURL))
                    {
                        delete = false;
                        break;
                    }
                }

                if(!delete)
                {
                    break;
                }
            }

            //not too sure if we need to check for products that have been duplicated and within assortments

            if(delete)
            {
                //call API to delete texture
                ProductAPI.Instance.DeleteProductTexture(textureURL);
            }
        }

        public void DeleteSelectedProducts()
        {
            if(AdminController != null)
            {
                int[] values = new int[AdminController.SelectedProducts.Count];
                List<int> tagValues = new List<int>();
                int count = AdminController.SelectedProducts.Count;
                for (int i = 0; i < count; i++)
                {
                    values[i] = AdminController.SelectedProducts[i].ProductMesh.GetComponent<ProductMesh>().UniqueProductPlacementID;
                    tagValues.AddRange(AdminController.GetProductPlacementTagIDValues(AdminController.SelectedProducts[i].settings.ProductCode));
                    ProductPlacement.ProductPlacementObject pObject = AdminController.GetPlacementObject(AdminController.SelectedProducts[i].ProductMesh.GetComponent<ProductMesh>().UniqueProductPlacementID);
                    ProductPlacementSync.Instance.SyncRemoveProduct(AdminController.ID, pObject.id);

                    DeleteProductTexture(pObject.textureURL);

                    AdminController.RemoveSingleProduct(pObject.id, true);
                    Destroy(AdminController.SelectedProducts[i].gameObject);
                }

                ProductPlacement[] temp = FindObjectsByType<ProductPlacement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                List<int> tagValuesAPI = new List<int>();
                //for each tagvalue, need to check if any other productplacement product is using the same tag, if true exclude from API call
                for (int i = 0;  i < tagValues.Count; i++)
                {
                    bool exclude = temp.ToList().TrueForAll(x => x.DoesProductHaveTagID(tagValues[i]));

                    if(!exclude)
                    {
                        tagValuesAPI.Add(tagValues[i]);
                    }
                }


                //API call
                ProductAPI.Instance.DeleteProducts(values);
                //API to delete info tags
                InfoTagAPI.Instance.DeleteInfoTags(tagValuesAPI);

                AdminController.SelectedProducts.Clear();
            }
        }

        public void FinishPlacementControl()
        {
            if(selectModeOn)
            {
                ToggleSelectMode(false);
            }

            if(m_movingObjectOrigin)
            {
                DropProductInPlacement(AdminController.ID);
            }

            AdminController.AdminLock.LockThis();
            AdminController = null;
            RaycastManager.Instance.CastRay = true;
            HUDManager.Instance.ToggleHUDControl("PRODUCTPLACEMENT_CONTROL", false);

            //HUD
            HUDManager.Instance.ShowHUDNavigationVisibility(true);
            NavigationManager.Instance.ToggleJoystick(true);
            MMORoom.Instance.ToggleLocalProfileInteraction(true);
        }

        private void CreateVisibleBoundingBox(int id, BoxCollider col, bool create)
        {
            //create a bounding box around the for corners of the col.
            if(create)
            {
                Transform[] temp = new Transform[6];

                GameObject go = new GameObject();
                go.name = "SelectedBoundingBox";
                go.transform.SetParent(col.transform);
                go.transform.localPosition = col.center;
                go.transform.localScale = Vector3.one;

                GameObject center = GameObject.CreatePrimitive(PrimitiveType.Cube);
                center.name = "ScalingPoint";
                center.GetComponent<Renderer>().material.color = Color.green;
                center.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                center.GetComponent<Renderer>().receiveShadows = false;
                center.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                center.transform.SetParent(go.transform);
                center.transform.localPosition = Vector3.zero;

                temp[0] = center.transform;

                for (int i = 0; i < 4; i++)
                {
                    GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    corner.GetComponent<Renderer>().material.color = Color.green;
                    corner.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    corner.GetComponent<Renderer>().receiveShadows = false;
                    corner.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                    corner.transform.SetParent(go.transform);

                    if( i == 0)
                    {
                        corner.transform.localPosition = new Vector3(0 - col.transform.localScale.x / 2, 0 + col.transform.localScale.y / 2, 0);
                        temp[1] = corner.transform;
                    }
                    else if(i == 1)
                    {
                        corner.transform.localPosition = new Vector3(0 + col.transform.localScale.x / 2, 0 + col.transform.localScale.y / 2, 0);

                        //create the edit button
                        UnityEngine.Object prefab = Resources.Load("Product/Canvas_ProductEdit");
                        ProductPlacementEditButton editButton = Instantiate(prefab as GameObject).GetComponentInChildren<ProductPlacementEditButton>(true);
                        editButton.transform.parent.SetParent(go.transform);
                        editButton.transform.parent.transform.localPosition = new Vector3(corner.transform.localPosition.x, corner.transform.localPosition.y, corner.transform.localPosition.z - 0.01f);
                        editButton.transform.parent.transform.localEulerAngles = Vector3.zero;
                        editButton.transform.parent.localScale = new Vector3(1, 1, 1);
                        editButton.Set(id);
                        temp[2] = corner.transform;
                        temp[3] = corner.transform;

                    }
                    else if(i == 2)
                    {
                        corner.transform.localPosition = new Vector3(0 + col.transform.localScale.x / 2, 0 - col.transform.localScale.y / 2, 0);
                        temp[4] = corner.transform;
                    }
                    else
                    {
                        corner.transform.localPosition = new Vector3(0 - col.transform.localScale.x / 2, 0 - col.transform.localScale.y / 2, 0);
                        temp[5] = corner.transform;
                    }
                }

                go.transform.SetParent(col.transform.parent);
                go.transform.localScale = Vector3.one;

                for(int i = 0; i < temp.Length; i++)
                {
                    temp[i].localScale = new Vector3(0.05f, 0.05f, 0.05f);
                }
            }
            else
            {
                Transform t = col.transform.parent.Find("SelectedBoundingBox");

                if(t != null)
                {
                    Destroy(t.gameObject);
                }
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

        private bool BoundsZIsEncapsulated(float zmin, float zmax)
        {
            if (zmin < minZMovement || zmax > maxZMovement) return false;

            return true;
        }

        private void AssignBounds(ProductPlacement hitBounds)
        {
            Collider col = hitBounds.placementBounds.GetComponent<Collider>();
            maxXMovement = hitBounds.placementBounds.transform.localPosition.x + Mathf.Abs(col.bounds.extents.x);
            minXMovement = hitBounds.placementBounds.transform.localPosition.x - Mathf.Abs(col.bounds.extents.x);

            minYMovement = hitBounds.placementBounds.transform.localPosition.y - Mathf.Abs(col.bounds.extents.y);
            maxYMovement = hitBounds.placementBounds.transform.localPosition.y + Mathf.Abs(col.bounds.extents.y);

            minZMovement = hitBounds.placementBounds.transform.localPosition.z - Mathf.Abs(col.bounds.extents.z);
            maxZMovement = hitBounds.placementBounds.transform.localPosition.z + Mathf.Abs(col.bounds.extents.z);
        }

        public List<ProductPlacement.ProductPlacementObject> GetAllProductsFromShop(string shop)
        {
            List<ProductPlacement.ProductPlacementObject> temp = new List<ProductPlacement.ProductPlacementObject>();
            ProductPlacement[] all = FindObjectsByType<ProductPlacement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                if(all[i].settings.shop.Equals(shop))
                {
                    temp.AddRange(all[i].GetProductRawObjects());
                }
            }

            return temp;
        }

        public ProductPlacement GetProductPlacement(string collection, string shop)
        {
            ProductPlacement temp = null;

            ProductPlacement[] all = FindObjectsByType<ProductPlacement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].settings.shop.Equals(shop) && all[i].ID.Equals(collection))
                {
                    temp = all[i];
                    break;
                }
            }

            return temp;
        }

        public void OnAddedNewProduct(string placementID, ProductPlacement.ProductPlacementObject product)
        {
            ProductPlacement pPlacement = FindObjectsByType<ProductPlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList().FirstOrDefault(x => x.ID.Equals(placementID));

            if (pPlacement != null)
            {
                pPlacement.PlaceSingleProduct(product);
            }

            ProductPlacementSync.Instance.SyncAddProduct(placementID, product);
        }

        public void OnUpdateOldProduct(string placementID, ProductPlacement.ProductPlacementObject product)
        {
            ProductPlacement pPlacement = FindObjectsByType<ProductPlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList().FirstOrDefault(x => x.ID.Equals(placementID));

            if (pPlacement != null)
            {
                pPlacement.PlaceSingleProduct(product);
            }

            PushUpdateToAllProducts(product);

            ProductPlacementSync.Instance.SyncAddProduct(placementID, product);
        }

        public void RemotePositionProductPlacement(string placementID, int productID, Vector3 localPosition)
        {
            Debug.Log("RemotePositionProductPlacement: Product code= " + productID);

            ProductPlacement pPlacement = FindObjectsByType<ProductPlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList().FirstOrDefault(x => x.ID.Equals(placementID));

            if(pPlacement != null)
            {
                var product = pPlacement.GetProduct(productID);

                if (product != null)
                {
                    product.SetMoveTarget(localPosition);

                    ProductPlacement.ProductPlacementObject pObject = pPlacement.GetPlacementObject(productID);

                    if (pObject != null)
                    {
                        pObject.scale = m_scalingMesh.product.transform.localScale;
                        pObject.position = m_scalingMesh.product.transform.localPosition;
                    }
                }
            }
        }

        public void RemoteScaleProductPlacement(string placementID, int productID, Vector3 localScale)
        {
            Debug.Log("RemoteScaleProductPlacement: Product code= " + productID);

            ProductPlacement pPlacement = FindObjectsByType<ProductPlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList().FirstOrDefault(x => x.ID.Equals(placementID));

            if (pPlacement != null)
            {
                var product = pPlacement.GetProduct(productID);

                if (product != null)
                {
                    product.SetScaleTarget(localScale);

                    ProductPlacement.ProductPlacementObject pObject = pPlacement.GetPlacementObject(productID);

                    if (pObject != null)
                    {
                        pObject.scale = m_scalingMesh.product.transform.localScale;
                        pObject.position = m_scalingMesh.product.transform.localPosition;
                    }
                }
            }
        }

        public void RemoteRemoveProductPlacement(string placementID, int productID)
        {
            Debug.Log("RemoteUpdateProductPlacement: Product code= " + productID);

            ProductPlacement pPlacement = FindObjectsByType<ProductPlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList().FirstOrDefault(x => x.ID.Equals(placementID));

            if (pPlacement != null)
            {
                pPlacement.RemoveSingleProduct(productID, true);
            }
        }

        public void RemoteAddProductPlacement(string placementID, string productJson)
        {
            ProductPlacement.ProductPlacementObject obj = JsonUtility.FromJson<ProductPlacement.ProductPlacementObject>(productJson);

            Debug.Log("RemoteUpdateProductPlacement: Product code= " + obj.productCode);

            ProductPlacement pPlacement = FindObjectsByType<ProductPlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList().FirstOrDefault(x => x.ID.Equals(placementID));

            if (pPlacement != null)
            {
                pPlacement.PlaceSingleProduct(obj);
            }

            PushUpdateToAllProducts(obj);
        }

        private void PushUpdateToAllProducts(ProductPlacement.ProductPlacementObject obj)
        {
            //update all product placement raw objects that match
            ProductPlacement[] allPlacements = FindObjectsByType<ProductPlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < allPlacements.Length; i++)
            {
                if(allPlacements[i].settings.shop.Equals(obj.shop))
                {
                    foreach(ProductPlacement.ProductPlacementObject o in allPlacements[i].GetProductRawObjects())
                    {
                        if(o.shop.Equals(obj.shop) && o.productCode.Equals(obj.productCode))
                        {
                            o.productCode = obj.productCode;
                            o.description = obj.description;
                            o.textureURL = obj.textureURL;
                            o.videos = obj.videos;
                            o.images = obj.images;
                            o.websites = obj.websites;
                        }
                    }
                }
            }

            //need to update to all products in the room whos product code is this product and shop
            Product[] allProducts = FindObjectsByType<Product>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < allProducts.Length; i++)
            {
                if (allProducts[i].settings.ProductCode.Equals(obj.productCode) && allProducts[i].ProductMesh.GetComponent<ProductMesh>().ProductPlacementShop.Equals(obj.shop))
                {
                    allProducts[i].settings.InfotagText = obj.description.data;
                    allProducts[i].settings.WebInfotagsUrls = new List<InfotagManager.InfoTagURL>();
                    allProducts[i].settings.ImageInfotagsUrls = new List<InfotagManager.InfoTagURL>();
                    allProducts[i].settings.VideoInfotagsUrls = new List<InfotagManager.InfoTagURL>();

                    if (obj.websites.Count > 0)
                    {
                        for (int j = 0; j < obj.websites.Count; j++)
                        {
                            InfotagManager.InfoTagURL iTag = new InfotagManager.InfoTagURL();
                            iTag.title = obj.websites[j].title;
                            iTag.url = obj.websites[j].data;

                            allProducts[i].settings.WebInfotagsUrls.Add(iTag);
                        }
                    }

                    if (obj.images.Count > 0)
                    {
                        for (int j = 0; j < obj.images.Count; j++)
                        {
                            InfotagManager.InfoTagURL iTag = new InfotagManager.InfoTagURL();
                            iTag.title = obj.images[j].title;
                            iTag.url = obj.images[j].data;

                            allProducts[i].settings.ImageInfotagsUrls.Add(iTag);
                        }
                    }

                    if (obj.videos.Count > 0)
                    {
                        for (int j = 0; j < obj.videos.Count; j++)
                        {
                            InfotagManager.InfoTagURL iTag = new InfotagManager.InfoTagURL();
                            iTag.title = obj.videos[j].title;
                            iTag.url = obj.videos[j].data;

                            allProducts[i].settings.VideoInfotagsUrls.Add(iTag);
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ProductPlacementManager), true)]
        public class ProductPlacementManager_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("quadMeshMaterial"), true);

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
