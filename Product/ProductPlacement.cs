using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Networking;

namespace BrandLab360
{
    public class ProductPlacement : UniqueID
    {
        public ProductPlacementIOObject settings = new ProductPlacementIOObject();

        public Transform placementParent;
        public GameObject placementBounds;
        public GameObject placementTrigger;

        private Dictionary<int, Product> m_products = new Dictionary<int, Product>();
        private List<ProductPlacementObject> m_rawProducts = new List<ProductPlacementObject>();
        private Lock m_lock;

        public List<Product> SelectedProducts = new List<Product>();
        public bool CreatingProductStarted = false;
        public bool DestroyingProductStarted = false;
        private bool m_isVisible = false;
        private Coroutine m_exitTriggerProcess;

        [SerializeField]
        private bool editorAdminOverride = false;

        public Lock AdminLock
        {
            get
            {
                if(m_lock == null)
                {
                    m_lock = GetComponentInChildren<Lock>();
                }

                return m_lock;
            }
        }

        public bool DoesProductHaveTagID(int tagID)
        {
            for (int i = 0; i < m_rawProducts.Count; i++)
            {
                if (!string.IsNullOrEmpty(m_rawProducts[i].description.data))
                {
                    if (m_rawProducts[i].description.id.Equals(tagID))
                    {
                        return true;
                    }
                }

                for (int j = 0; j < m_rawProducts[i].images.Count; j++)
                {
                    if(m_rawProducts[i].images[j].id.Equals(tagID))
                    {
                        return true;
                    }
                }

                for (int j = 0; j < m_rawProducts[i].videos.Count; j++)
                {
                    if (m_rawProducts[i].videos[j].id.Equals(tagID))
                    {
                        return true;
                    }
                }

                for (int j = 0; j < m_rawProducts[i].websites.Count; j++)
                {
                    if (m_rawProducts[i].videos[j].id.Equals(tagID))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public List<int> GetProductPlacementTagIDValues(string productCode)
        {
            List<int> temp = new List<int>();

            //do not delet info tags if more products exists with same product code
            int duplicateProductsCount = m_rawProducts.Count(x => x.productCode.Equals(productCode));

            if(duplicateProductsCount <= 1)
            {
                for (int i = 0; i < m_rawProducts.Count; i++)
                {
                    if (m_rawProducts[i].productCode.Equals(productCode))
                    {
                        if (!string.IsNullOrEmpty(m_rawProducts[i].description.data))
                        {
                            temp.Add(m_rawProducts[i].description.id);
                        }

                        for (int j = 0; j < m_rawProducts[i].images.Count; j++)
                        {
                            temp.Add(m_rawProducts[i].images[j].id);
                        }

                        for (int j = 0; j < m_rawProducts[i].videos.Count; j++)
                        {
                            temp.Add(m_rawProducts[i].videos[j].id);
                        }

                        for (int j = 0; j < m_rawProducts[i].websites.Count; j++)
                        {
                            temp.Add(m_rawProducts[i].websites[j].id);
                        }

                        break;
                    }
                }
            }

            return temp;
        }

        public ProductPlacementObject GetPlacementObject(int productID)
        {
            for (int i = 0; i < m_rawProducts.Count; i++)
            {
                if (m_rawProducts[i].id.Equals(productID))
                {
                    return m_rawProducts[i];
                }
            }

            return null;
        }

        public List<ProductPlacementObject> GetProductRawObjects()
        {
            return m_rawProducts;
        }

        private void OnDestroy()
        {
            DestroyProducts(true);

            if (settings.creationType.Equals(ProductCreationType.PlayerTrigger) && placementTrigger != null)
            {
                //check if trigger has component
                ProductPlacementTrigger trigger = placementTrigger.GetComponent<ProductPlacementTrigger>();

                if (trigger != null)
                {
                    //pass an event to the trigger
                    trigger.OnEnter -= OnTriggerEnterCallcack;
                    trigger.OnExit -= OnTriggerExitCallback;
                }
            }
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if (!AppManager.Instance.Instances.ignoreIObjectSettings)
            {
                //need to get the settings from the instances script then update the settings
                foreach (AppInstances.IOObjectProductPlacementHandler setting in AppManager.Instance.Instances.ioProductPlacementObjects)
                {
                    if (setting.referenceID.Equals(GetRawID()))
                    {
                        ApplySettings(setting.settings);
                        break;
                    }
                }
            }

            bool adminMultipleUser = AppManager.Instance.Settings.projectSettings.useMultipleAdminUsers && AppManager.Instance.Data.AdminRole != null && controlledByUserType ? userTypes.Contains(AppManager.Instance.Data.AdminRole.role) : false;
            bool admin = !adminMultipleUser ? AppManager.Instance.Data.IsAdminUser : true;
            AdminLock.IsNetworked = false;
            AdminLock.OnUnlock += OnAdminUnlocked;
            AdminLock.OverrideSettings(settings.lockSettings.useDataAPIPassword, settings.lockSettings.password, settings.lockSettings.displayType);

#if UNITY_EDITOR
            if(editorAdminOverride && !admin)
            {
                admin = true;
            }
            else
            {
                if(adminMultipleUser)
                {
                    editorAdminOverride = false;
                }
            }
#else
            editorAdminOverride = false;
#endif

            //should check if user is admin
            if (!admin || AppManager.Instance.Data.IsMobile)
            {
                AdminLock.gameObject.SetActive(false);
            }
            else
            {
                if(adminMultipleUser && !editorAdminOverride)
                {
                    if(!string.IsNullOrEmpty(AppManager.Instance.Data.AdminRole.password))
                    {
                        AdminLock.Password = AppManager.Instance.Data.AdminRole.password;
                    }
                }
            }

            if (settings.creationType.Equals(ProductCreationType.PlayerTrigger))
            {
                if(placementTrigger != null)
                {
                    //check if trigger has component
                    ProductPlacementTrigger trigger = placementTrigger.GetComponent<ProductPlacementTrigger>();

                    if (trigger == null)
                    {
                        trigger = placementTrigger.AddComponent<ProductPlacementTrigger>();
                    }

                    //pass an event to the trigger
                    trigger.OnEnter += OnTriggerEnterCallcack;
                    trigger.OnExit += OnTriggerExitCallback;
                }
                else
                {
                    settings.creationType = ProductCreationType.OnAdded;
                }
            }

            m_isVisible = settings.creationType.Equals(ProductCreationType.OnAdded);

        }

        private void OnTriggerEnterCallcack()
        {
            if(m_exitTriggerProcess != null)
            {
                StopCoroutine(m_exitTriggerProcess);
            }

            m_exitTriggerProcess = null;
            MakeProductsVisible(true);
        }

        private void OnTriggerExitCallback()
        {
            m_exitTriggerProcess = StartCoroutine(ProcessOnTriggerExit());
        }

        private IEnumerator ProcessOnTriggerExit()
        {
            yield return new WaitForSeconds(10);

            float distance = Vector3.Distance(transform.position, PlayerManager.Instance.GetLocalPlayer().TransformObject.position);

            if (distance > ProductPlacementManager.Instance.CreationDistance)
            {
                MakeProductsVisible(false);
            }

            m_exitTriggerProcess = null;
        }

        public Product GetProduct(int productID)
        {
            foreach(KeyValuePair<int, Product> prod in m_products)
            {
                if(prod.Key.Equals(productID))
                {
                    return prod.Value;
                }
            }

            return null;
        }

        private void OnAdminUnlocked()
        {
            //dont cast global ray anymore
            RaycastManager.Instance.CastRay = false;

            //need to send to productplacementmanager this script as Live
            ProductPlacementManager.Instance.AdminController = this;

            //HUD
            HUDManager.Instance.ShowHUDNavigationVisibility(false);
            NavigationManager.Instance.ToggleJoystick(false);
            MMORoom.Instance.ToggleLocalProfileInteraction(false);

            //show hud control
            HUDManager.Instance.ToggleHUDControl("PRODUCTPLACEMENT_CONTROL", true);
        }

        public void PlaceSingleProduct(ProductPlacementObject productObject)
        {
            if (productObject == null) return;

            ProductPlacementObject exists = m_rawProducts.FirstOrDefault(x => x.id.Equals(productObject.id));

            if (exists == null)
            {
                m_rawProducts.Add(productObject);

                List<ProductPlacementObject> temp = new List<ProductPlacementObject>();
                temp.Add(productObject);

                if (settings.creationType.Equals(ProductCreationType.OnAdded))
                {
                    CreateProducts(temp);
                }
                else
                {
                    if (m_isVisible)
                    {
                        CreateProducts(temp);
                    }
                }
            }
            else
            {
                exists.productCode = productObject.productCode;
                exists.description = productObject.description;
                exists.textureURL = productObject.textureURL;
                exists.videos = productObject.videos;
                exists.images = productObject.images;
                exists.websites = productObject.websites;
            }
        }

        public void PlaceProductGroup(ProductPlacementGroup productGroup)
        {
            if (productGroup == null) return;

            if (!productGroup.groupID.Equals(ID)) return;

            for(int i = 0; i < productGroup.products.Count; i++)
            {
                ProductPlacementObject exists = m_rawProducts.FirstOrDefault(x => x.productCode.Equals(productGroup.products[i]));

                if (exists == null)
                {
                    m_rawProducts.Add(productGroup.products[i]);
                }
            }

            if (settings.creationType.Equals(ProductCreationType.OnAdded))
            {
                CreateProducts(productGroup.products);
            }
            else
            {
                //check if product is in range
                float distance = Vector3.Distance(transform.position, PlayerManager.Instance.GetLocalPlayer().TransformObject.position);

                if (distance <= ProductPlacementManager.Instance.CreationDistance)
                {
                    CreateProducts(productGroup.products);
                }
            }
        }

        public void RemoveSingleProduct(int productID, bool removeRaw = false)
        {
            Product prod = m_products[productID];

            if (prod != null)
            {
                if(removeRaw)
                {
                    ProductPlacementObject rawObject = m_rawProducts.FirstOrDefault(x => x.id.Equals(productID));

                    if (rawObject != null)
                    {
                        m_rawProducts.Remove(rawObject);
                    }
                }


                //destroy product
                var productMesh = prod.ProductMesh;
                AppManager.Instance.Instances.RemoveUniqueID(prod.GetRawID());
                //need this to release texture from memory
                Destroy(productMesh.GetComponent<Renderer>().material.mainTexture);
                Destroy(productMesh);
                Destroy(prod.gameObject);

                //remove project
                ProductManager.Instance.RemoveExistingProduct(prod);
                m_products.Remove(productID);
            }
        }

        public void RemoveProductGroup(List<int> productCodes, bool removeRaw = false)
        {
            for (int i = 0; i < productCodes.Count; i++)
            {
                RemoveSingleProduct(productCodes[i], removeRaw);
            }
        }

        public void MakeProductsVisible(bool state)
        {
            if (settings.creationType.Equals(ProductCreationType.OnAdded)) return;

            if (state.Equals(m_isVisible)) return;

            if(state)
            {
                if(!m_isVisible)
                {
                    CreateProducts(m_rawProducts);
                }

                m_isVisible = true;
            }
            else
            {
                if(!AppManager.Instance.Settings.playerSettings.maintainProductPlacementObjectWhenCreated)
                {
                    if (m_isVisible)
                    {
                        DestroyProducts();
                    }

                    m_isVisible = false;
                }
            }
        }

        private void DestroyProducts(bool removeRaw = false)
        {
            DestroyingProductStarted = true;
            List<int> temp = new List<int>();

            foreach(KeyValuePair<int, Product> prod in m_products)
            {
                temp.Add(prod.Key);
            }

            RemoveProductGroup(temp, removeRaw);
            DestroyingProductStarted = false;
        }

        private void CreateProducts(List<ProductPlacementObject> products)
        {
            CreatingProductStarted = true;
            Dictionary<int, Product> temp = RequestCreateProductsOperation(products);
            
            foreach(KeyValuePair<int, Product> prod in temp)
            {
                m_products.Add(prod.Key, prod.Value);
            }

            CreatingProductStarted = false;
        }

        private Dictionary<int, Product> RequestCreateProductsOperation(List<ProductPlacementObject> products)
        {
            Dictionary<int, Product> temp = new Dictionary<int, Product>();

            for (int i = 0; i < products.Count; i++)
            {
                Product prod = CreateProductOperation(products[i]);
                temp.Add(products[i].id, prod);
            }

            return temp;
        }

        public Product CreateProductOperation(ProductPlacementObject product)
        {
            //create the product gameobject and scale / apply material
            GameObject productObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            productObj.name = product.productCode;
            productObj.GetComponent<MeshRenderer>().enabled = false;

            //Add the product mesh component to the object
            productObj.AddComponent<ProductMesh>();

            //Destroy mesh collider which it might have if created as a quad
            if (productObj.GetComponent<MeshCollider>())
            {
                Destroy(productObj.GetComponent<MeshCollider>());
            }

            //Add a box collider to the product mesh to generate the bounds to match on the parent root
            var box = productObj.GetComponent<BoxCollider>();

            if (box == null) { box = productObj.AddComponent<BoxCollider>(); }
            box.isTrigger = true;

            //Create the parent root object, of which to add the product mesh as a child, as well as the infotags as childs
            var productRoot = new GameObject(productObj.name);
            productRoot.transform.position = productObj.transform.position;
            productRoot.transform.rotation = productObj.transform.rotation;

            //parent the product mesh to the new root parent
            productObj.transform.parent = productRoot.transform;

            Product prod = productRoot.AddComponent<Product>();
            productObj.GetComponent<ProductMesh>().product = prod;
            prod.settings.ProductCode = product.productCode;
            prod.settings.InfotagText = product.description.data;
            productObj.GetComponent<ProductMesh>().UniqueProductPlacementID = product.id;
            productObj.GetComponent<ProductMesh>().ProductPlacementShop = settings.shop;
            productObj.GetComponent<ProductMesh>().ProductPlacementCollection = ID;
            productObj.GetComponent<ProductMesh>().rawTextureSource = product.textureURL;

            if (product.websites.Count > 0)
            {
                for(int i = 0; i < product.websites.Count; i++)
                {
                    InfotagManager.InfoTagURL iTag = new InfotagManager.InfoTagURL();
                    iTag.title = product.websites[i].title;
                    iTag.url = product.websites[i].data;

                    prod.settings.WebInfotagsUrls.Add(iTag);
                }
            }

            if (product.images.Count > 0)
            {
                for (int i = 0; i < product.images.Count; i++)
                {
                    InfotagManager.InfoTagURL iTag = new InfotagManager.InfoTagURL();
                    iTag.title = product.images[i].title;
                    iTag.url = product.images[i].data;

                    prod.settings.ImageInfotagsUrls.Add(iTag);
                }
            }

            if (product.videos.Count > 0)
            {
                for (int i = 0; i < product.videos.Count; i++)
                {
                    InfotagManager.InfoTagURL iTag = new InfotagManager.InfoTagURL();
                    iTag.title = product.videos[i].title;
                    iTag.url = product.videos[i].data;

                    prod.settings.VideoInfotagsUrls.Add(iTag);
                }
            }

            prod.ProductMesh = productObj;

            productRoot.transform.SetParent((placementParent == null) ? transform : placementParent);
            prod.transform.localPosition = product.position;
            productRoot.transform.localEulerAngles = Vector3.zero;
            ProductManager.Instance.AddNewProduct(prod);

            StartCoroutine(ProcessMaterial(product, productObj));

            return prod;
        }

        private IEnumerator ProcessMaterial(ProductPlacementObject product, GameObject productObj)
        {
            CoreUtilities.GetShaderName();

            //Find the default shader based on render pipeline used
            Shader shader = Shader.Find(CoreUtilities.ShaderName);

            //Create a new material with the selected texture
            bool materialsExists = ProductPlacementManager.Instance.GetMaterial() != null;
            Material material = !materialsExists ? new Material(shader) : new Material(ProductPlacementManager.Instance.GetMaterial());
            yield return StartCoroutine(CoreUtilities.AttainTexture(product.textureURL, material));
            Texture tex = material.mainTexture;

            //Get the dimensions of the texture
            var texWidth = tex.width;
            var texHeight = tex.height;
            float scaledWidth = (float)texWidth / (float)texHeight;

            product.position = productObj.transform.localPosition;
            productObj.GetComponent<Renderer>().material = material;
            productObj.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            productObj.GetComponent<Renderer>().receiveShadows = false;

            //this is where we need to calculate the bounds of the collider based on the opaque pixels and ignore transparent pixels
            if (productObj.GetComponent<MeshFilter>().sharedMesh.name.Contains("Quad"))
            {
                BoxCollider box = productObj.GetComponent<BoxCollider>();
                box.size = new Vector3(1, 1, 0.05f);
                GetTags(box, tex);
                productObj.transform.localPosition = GetOffset(product.position, productObj.transform);
            }

            //set scale based on aspect ratio
            if (product.scale.Equals(Vector3.zero))
            {
                productObj.transform.localScale = new Vector3(scaledWidth, 1, 1);
                product.scale = Vector3.one;
                productObj.transform.parent.localScale = new Vector3(product.scale.x, product.scale.y, 1);
            }
            else
            {
                productObj.transform.localScale = new Vector3(scaledWidth, 1, 1);
                productObj.transform.parent.localScale = new Vector3(product.scale.x, product.scale.y, 1);
            }

            productObj.GetComponent<MeshRenderer>().enabled = true;
        }

        public Vector3 GetOffset(Vector3 origin, Transform t)
        {
            Vector3 offset = Vector3.zero;
            BoxCollider box = t.GetComponent<BoxCollider>();

            if (settings.placementType.Equals(ProductPlacementType.Rail))
            {
                // Position on rail relative to the rail point 
                var yoffset = placementParent.transform.position.y - (t.transform.position.y - box.bounds.extents.y);
                var y = 0 - yoffset + 0.03f;
                offset = new Vector3(origin.x, y, origin.z);
            }
            else if(settings.placementType.Equals(ProductPlacementType.Table))
            {
                //Position on table relative to the table point
                var yoffset = placementParent.transform.position.y - (t.transform.position.y + box.bounds.extents.y);
                var y = 0 - yoffset + 0.03f;
                offset = new Vector3(origin.x, y, origin.z);
            }

            return offset;
        }

        private void GetTags(BoxCollider col, Texture tex, float scaledWidth = 1)
        {
            ProductManager.ProductTagCreator tagCreator = new ProductManager.ProductTagCreator();
            Product prod = col.GetComponentInParent<Product>();
            tagCreator.Create(col.transform.parent.gameObject, col.bounds, prod);

            if(settings.placementType.Equals(ProductPlacementType.Rail))
            {
                prod.RailPoint.transform.localPosition = new Vector2(0, 0);
                prod.TablePoint.transform.localPosition = new Vector2(0, 0 - col.bounds.extents.y * 2);
                prod.HoldPoint.transform.localPosition = new Vector2(0 + col.bounds.extents.x, 0 - col.bounds.extents.y);

                prod.AssortmentCanvas.transform.localPosition = new Vector3(prod.AssortmentCanvas.transform.localPosition.x, 0 - col.bounds.extents.y, prod.AssortmentCanvas.transform.localPosition.z);
                prod.InfotagCanvas.transform.localPosition = new Vector3(prod.InfotagCanvas.transform.localPosition.x, 0 - col.bounds.extents.y / 2, prod.InfotagCanvas.transform.localPosition.z);
                prod.PickupCanvas.transform.localPosition = new Vector3(0 + col.bounds.extents.x / 2, 0, prod.PickupCanvas.transform.localPosition.z);
                prod.DeleteCanvas.transform.localPosition = new Vector3(0 - col.bounds.extents.x / 2, 0, prod.DeleteCanvas.transform.localPosition.z);

            }
            else if(settings.placementType.Equals(ProductPlacementType.Table))
            {
                prod.RailPoint.transform.localPosition = new Vector2(0, 0 + col.bounds.extents.y * 2);
                prod.TablePoint.transform.localPosition = new Vector2(0, 0 - col.bounds.extents.y);
                prod.HoldPoint.transform.localPosition = new Vector2(0 + col.bounds.extents.x, 0 + col.bounds.extents.y);

                prod.AssortmentCanvas.transform.localPosition = new Vector3(prod.AssortmentCanvas.transform.localPosition.x, 0 + col.bounds.extents.y, prod.AssortmentCanvas.transform.localPosition.z);
                prod.InfotagCanvas.transform.localPosition = new Vector3(prod.InfotagCanvas.transform.localPosition.x, 0 + col.bounds.extents.y / 2, prod.InfotagCanvas.transform.localPosition.z);
                prod.PickupCanvas.transform.localPosition = new Vector3(0 + col.bounds.extents.x / 2, 0 + col.bounds.extents.y * 2, prod.PickupCanvas.transform.localPosition.z);
                prod.DeleteCanvas.transform.localPosition = new Vector3(0 - col.bounds.extents.x / 2, 0 + col.bounds.extents.y * 2, prod.DeleteCanvas.transform.localPosition.z);
            }
            else
            {
                prod.RailPoint.transform.localPosition = new Vector2(0, 0 + col.bounds.extents.y);
                prod.TablePoint.transform.localPosition = new Vector2(0, 0 - col.bounds.extents.y);
                prod.HoldPoint.transform.localPosition = new Vector2(0 + col.bounds.extents.x, 0);

                prod.PickupCanvas.transform.localPosition = new Vector3(0 + col.bounds.extents.x / 2, prod.PickupCanvas.transform.localPosition.y, prod.PickupCanvas.transform.localPosition.z);
                prod.DeleteCanvas.transform.localPosition = new Vector3(0 - col.bounds.extents.x / 2, prod.DeleteCanvas.transform.localPosition.y, prod.DeleteCanvas.transform.localPosition.z);

            }

        }

        [System.Serializable]
        public class ProductPlacementIOObject : IObjectSetting
        {
            public string shop = "";
            public ProductCreationType creationType = ProductCreationType.OnAdded;
            public ProductPlacementType placementType = ProductPlacementType.Wall;
            public LockManager.LockSetting lockSettings = new LockManager.LockSetting();
        }

        public override IObjectSetting GetSettings(bool remove = false)
        {
            if (!remove)
            {
                IObjectSetting baseSettings = base.GetSettings();
                settings.adminOnly = baseSettings.adminOnly;
                settings.prefix = baseSettings.prefix;
                settings.controlledByUserType = baseSettings.controlledByUserType;
                settings.userTypes = baseSettings.userTypes;

                settings.GO = gameObject.name;
            }

            settings.ID = id;
            return settings;
        }

        protected override void ApplySettings(IObjectSetting settings)
        {
            if (settings == null) return;

            base.ApplySettings(settings);

            this.settings.shop = ((ProductPlacementIOObject)settings).shop;
            this.settings.creationType = ((ProductPlacementIOObject)settings).creationType;
            this.settings.placementType = ((ProductPlacementIOObject)settings).placementType;
            this.settings.lockSettings = ((ProductPlacementIOObject)settings).lockSettings;
        }

        public class ProductPlacementGroup
        {
            public string groupID = "";
            public List<ProductPlacementObject> products;
        }

        public class ProductPlacementObject
        {
            public int id;

            public string productCode;
            public string textureURL;
            public string shop;

            public ProductPlacementInfoTag description = new ProductPlacementInfoTag();

            public List<ProductPlacementInfoTag> images = new List<ProductPlacementInfoTag>();
            public List<ProductPlacementInfoTag> videos = new List<ProductPlacementInfoTag>();
            public List<ProductPlacementInfoTag> websites = new List<ProductPlacementInfoTag>();

            public Vector3 position = Vector3.zero;
            public Vector3 scale = Vector3.zero;

            public bool InfoTagsUsed
            {
                get
                {
                    return !string.IsNullOrEmpty(description.data) || images.Count <= 0 || videos.Count <= 0 || websites.Count <= 0;
                }
            }
        }

        public class ProductPlacementInfoTag
        {
            public int id;
            public string title;
            public string data;
            public InfotagType type = InfotagType.Text;
        }

        public enum ProductCreationType { OnAdded, PlayerDistance, PlayerTrigger }
        public enum ProductPlacementType { Wall, Rail, Table }

#if UNITY_EDITOR
        [CustomEditor(typeof(ProductPlacement), true), CanEditMultipleObjects]
        public class ProductPlacement_Editor : UniqueID_Editor
        {
            private ProductPlacement placement;
            private int selectedShop = 0;
            private bool addShopMode = false;
            private string shopName = "";
            private bool guichanged = false;

            private void OnEnable()
            {
                base.Initialise();

                placement = (ProductPlacement)target;

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    //need to get the settings from the instances script then update the settings
                    foreach (AppInstances.IOObjectProductPlacementHandler setting in m_instances.ioProductPlacementObjects)
                    {
                        if (setting.referenceID.Equals(placement.ID))
                        {
                            placement.ApplySettings(setting.settings);
                            break;
                        }
                    }

                    m_instances.AddIOObject(placement.ID, placement.GetSettings());

                    if(m_instances.shops.Count > 0 && !string.IsNullOrEmpty(serializedObject.FindProperty("settings").FindPropertyRelative("shop").stringValue))
                    {
                        if(m_instances.shops.Contains(serializedObject.FindProperty("settings").FindPropertyRelative("shop").stringValue))
                        {
                            selectedShop = m_instances.shops.IndexOf(serializedObject.FindProperty("settings").FindPropertyRelative("shop").stringValue);
                        }
                        else
                        {
                            selectedShop = 0;
                            serializedObject.FindProperty("settings").FindPropertyRelative("shop").stringValue = "";
                            guichanged = true;
                        }
                    }
                }
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    m_instances.RemoveIOObject(placement.GetSettings(true));
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                DisplayID();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Lock Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("useDataAPIPassword"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("password"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("lockSettings").FindPropertyRelative("displayType"), true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("creationType"), true);

                if(serializedObject.FindProperty("settings").FindPropertyRelative("creationType").enumValueIndex.Equals(1))
                {
                    EditorGUILayout.LabelField("Distance controlled in App Settings", EditorStyles.miniBoldLabel);
                }
                else if (serializedObject.FindProperty("settings").FindPropertyRelative("creationType").enumValueIndex.Equals(2))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("placementTrigger"), true);
                }

                if(m_instances != null)
                {
                    selectedShop = EditorGUILayout.Popup("Shop", selectedShop, m_instances.shops.ToArray());
                    if(m_instances.shops.Count > 0)
                    {
                        if(string.IsNullOrEmpty(serializedObject.FindProperty("settings").FindPropertyRelative("shop").stringValue))
                        {
                            //update settings
                            guichanged = true;
                        }

                        serializedObject.FindProperty("settings").FindPropertyRelative("shop").stringValue = m_instances.shops[selectedShop];
                    }

                    if(!addShopMode)
                    {
                        if (GUILayout.Button("Create Shop"))
                        {
                            addShopMode = true;
                            shopName = "";
                        }
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        shopName = EditorGUILayout.TextField(shopName);

                        if (GUILayout.Button("Add", GUILayout.Width(75)))
                        {
                            if(!m_instances.shops.Contains(shopName) && string.IsNullOrEmpty(shopName))
                            {
                                m_instances.shops.Add(shopName);
                                serializedObject.FindProperty("settings").FindPropertyRelative("shop").stringValue = shopName;
                                selectedShop = m_instances.shops.IndexOf(shopName);
                            }

                            addShopMode = false;
                            shopName = "";
                        }

                        if (GUILayout.Button("Cancel", GUILayout.Width(75)))
                        {
                            addShopMode = false;
                            shopName = "";
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space();
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("placementType"), true);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("placementParent"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("placementBounds"), true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Editor", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("editorAdminOverride"), true);

                if (placement.placementBounds != null)
                {
                    placement.placementBounds.transform.localScale = EditorGUILayout.Vector3Field("Bounds Area:", placement.placementBounds.transform.localScale);
                }


                if (GUI.changed || GONameChanged() || guichanged)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(script);

                    guichanged = false;

                    if (Application.isPlaying) return;

                    if (m_instances != null)
                    {
                        m_instances.AddIOObject(placement.ID, placement.GetSettings());
                    }
                }
            }
        }
#endif
    }
}
