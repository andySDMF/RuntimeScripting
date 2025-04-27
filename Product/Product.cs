using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Product : UniqueID
    {
        public ProductIOObject settings = new ProductIOObject();

        public GameObject ProductMesh;
        public InfotagMenu InfotagCanvas;
        public GameObject AssortmentCanvas;
        public GameObject DeleteCanvas;
        public GameObject PickupCanvas;
        public GameObject RailPoint;
        public GameObject TablePoint;
        public GameObject HoldPoint;

        [HideInInspector]
        public bool inAssortment = false;

        [HideInInspector]
        public int currentAssortment;

        [HideInInspector]
        public bool isHeld = false;

        [HideInInspector]
        public int Sort = 0;

        [HideInInspector]
        public int InsertID = -1;

        [HideInInspector]
        public bool Moving = false;

        private Vector3 targetPosition;
        private bool enabledProductTags = false;

        public bool IsProductPlacementOrigin
        {
            get; set;
        }

        private Vector3 m_cacheInfoTagPosition;
        private Vector3 m_cacheAssrtmentTagPosition;
        private Vector3 m_cachePickupTagPosition;
        private Vector3 m_cacheDeleteTagPosition;

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if(GetComponentInParent<ProductPlacement>())
            {
                IsProductPlacementOrigin = true;
            }

            if (!AppManager.Instance.Instances.ignoreIObjectSettings)
            {
                //need to get the settings from the instances script then update the settings
                foreach (AppInstances.IOObjectProductHandler setting in AppManager.Instance.Instances.ioProductObjects)
                {
                    if (setting.referenceID.Equals(GetRawID()))
                    {
                        ApplySettings(setting.settings);
                        break;
                    }
                }
            }

            m_cacheInfoTagPosition = InfotagCanvas.transform.localPosition;

            if(m_cacheInfoTagPosition.z >= 0.0f)
            {
                m_cacheInfoTagPosition.z = m_cacheInfoTagPosition.z * -1;
            }

            m_cacheAssrtmentTagPosition = AssortmentCanvas.transform.localPosition;
            if (m_cacheAssrtmentTagPosition.z >= 0.0f)
            {
                m_cacheAssrtmentTagPosition.z = m_cacheAssrtmentTagPosition.z * -1;
            }

            m_cachePickupTagPosition = PickupCanvas.transform.localPosition;
            if (m_cachePickupTagPosition.z >= 0.0f)
            {
                m_cachePickupTagPosition.z = m_cachePickupTagPosition.z * -1;
            }

            m_cacheDeleteTagPosition = DeleteCanvas.transform.localPosition;
            if (m_cacheDeleteTagPosition.z >= 0.0f)
            {
                m_cacheDeleteTagPosition.z = m_cacheDeleteTagPosition.z * -1;
            }

            if (string.IsNullOrEmpty(settings.InfotagPicture))
            {
                settings.InfotagPicture = "Material";
            }

            if (GetComponent<Tooltip>() != null)
            {
                Debug.Log("Tooltips are all internal for products. Destroying tooltip.cs");
                Destroy(GetComponent<Tooltip>());
            }

            Renderer rend = ProductMesh.GetComponent<Renderer>();

            if (rend != null)
            {
                if (ProductMesh.GetComponent<MeshFilter>().mesh.name.Contains("Quad"))
                {
                    rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    rend.receiveShadows = false;
                }
            }
        }

        private void OnDestroy()
        {
            //only destroy texture if it has been loaded from URL
            if(ProductMesh != null)
            {
                if(ProductMesh.GetComponent<ProductMesh>().UniqueProductPlacementID >= 0)
                {
                    Destroy(ProductMesh.GetComponent<Renderer>().material.mainTexture);
                }
            }
        }

        public void Update()
        {
            //If product was set moving, move it to the target
            if (Moving && this.transform.localPosition != targetPosition)
            {
                if ((targetPosition - this.transform.localPosition).magnitude > 0.1f)
                {
                    this.transform.localPosition = Vector3.MoveTowards(this.transform.localPosition, targetPosition, 0.1f);
                }
                else
                {
                    this.transform.localPosition = targetPosition;
                    Moving = false;
                }
            }
        }

        /// <summary>
        /// Pickup product 
        /// </summary>
        public void Pickup()
        {
            if (!Moving)
            {
                ProductManager.Instance.PickupProduct(this);
            }
        }

        /// <summary>
        /// Add product to assortment automatically
        /// </summary>
        public void AddToAssortmentAuto()
        {
            AssortmentManager.Instance.AddToAssortment(this, settings.DefaultAssortment, Vector3.zero);
        }

        /// <summary>
        /// Remove product from assortment if in one
        /// </summary>
        public void RemoveFromAssortment()
        {
            if (!Moving && inAssortment)
            {
                AssortmentManager.Instance.RemoveFromAssortment(this);
            }
        }

        public bool TagsOpen()
        {
            return (InfotagCanvas.gameObject.activeInHierarchy || AssortmentCanvas.activeInHierarchy || DeleteCanvas.activeInHierarchy
                || PickupCanvas.activeInHierarchy) ? true : false;
        }

        public void ShowProductTag(bool show)
        {
            InfotagCanvas.gameObject.SetActive(show);

            // When showing the tags we can enable the infotags first time only

            if (show && !enabledProductTags)
            {
                enabledProductTags = true;
                InfotagCanvas.ToggleInfotag(InfotagType.Image, settings.ImageInfotagsUrls.Count > 0);
                InfotagCanvas.ToggleInfotag(InfotagType.Spin360, settings.Spin360InfotagsUrls.Count > 0);
                InfotagCanvas.ToggleInfotag(InfotagType.Video, settings.VideoInfotagsUrls.Count > 0);
                InfotagCanvas.ToggleInfotag(InfotagType.Web, settings.WebInfotagsUrls.Count > 0);
                InfotagCanvas.ToggleInfotag(InfotagType.Text, !string.IsNullOrWhiteSpace(settings.InfotagText));
            }
        }

        /// <summary>
        /// Hide tags that show when hovering on the product
        /// </summary>
        /// <param name="overridePersistent">if we need to override persistant infotags on a product</param>
        public void HideTags(bool overridePersistent)
        {
            StopAllCoroutines();

            //need to check if the 2D UI is used
            if(CoreManager.Instance.projectSettings.configTagMode.Equals(TagMode._2D))
            {
                ProductManager.Instance.Show2DTagSystem(false);
                return;
            }

            if ((!settings.persistentInfotags && !CoreManager.Instance.projectSettings.usePersistentInfotags) || overridePersistent)
            {
                if(InfotagCanvas != null)
                {
                    InfotagCanvas.gameObject.SetActive(false);
                }
            }

            if (AssortmentCanvas != null) AssortmentCanvas.SetActive(false);
            if (DeleteCanvas != null) DeleteCanvas.SetActive(false);
            if (PickupCanvas != null) PickupCanvas.SetActive(false);
        }

        public void WaitAndHideTags()
        {
            if (CoreManager.Instance.projectSettings.configTagMode.Equals(TagMode._2D))
            {
                ProductManager.Instance.Show2DTagSystem(false);

                return;
            }

            StopAllCoroutines();
            StartCoroutine(WaitHide());
        }

        private IEnumerator WaitHide()
        {
            yield return new WaitForSeconds(1.0f);

            HideTags(false);
        }

        /// <summary>
        /// Show tags when hovering mouse
        /// </summary>
        public void ShowTags(Vector3 hitPoint)
        {
            //need to check if the 2D UI is used
            if (CoreManager.Instance.projectSettings.configTagMode.Equals(TagMode._2D))
            {
                ProductManager.Instance.Show2DTagSystem(true, this);
                return;
            }

            Reposition(hitPoint);

            StopAllCoroutines();

            if (inAssortment)
            {
                ShowAssortmentTags();

                if(AppManager.Instance.Settings.playerSettings.enableAssortmentTags)
                {
                    ShowProductTag(true);
                }
            }
            else
            {
                ShowInfotags();
            }
        }

        /// <summary>
        /// Set the target to move the product towards
        /// </summary>
        /// <param name="target">target position</param>
        public void SetMoveTarget(Vector3 target)
        {
            Moving = true;
            targetPosition = target;
        }

        /// <summary>
        /// Set the new scale of the product
        /// </summary>
        /// <param name="target">target position</param>
        public void SetScaleTarget(Vector3 target)
        {
            transform.localScale = target;
        }

        /// <summary>
        /// Click the infotag
        /// </summary>
        /// <param name="infotagType"></param>
        public void OnClickInfotag(InfotagType infotagType)
        {
            InfotagManager.Instance.ShowInfotag(infotagType, this);
        }

        private void Reposition(Vector3 hitPoint)
        {
            Vector3 hitLocal = transform.InverseTransformPoint(hitPoint);

            if(hitLocal.z < 0.0f)
            {
                InfotagCanvas.transform.localPosition = new Vector3(m_cacheInfoTagPosition.x, m_cacheInfoTagPosition.y, m_cacheInfoTagPosition.z);
                AssortmentCanvas.transform.localPosition = new Vector3(m_cacheAssrtmentTagPosition.x, m_cacheAssrtmentTagPosition.y, m_cacheAssrtmentTagPosition.z);
                PickupCanvas.transform.localPosition = new Vector3(m_cachePickupTagPosition.x, m_cachePickupTagPosition.y, m_cachePickupTagPosition.z);
                DeleteCanvas.transform.localPosition = new Vector3(m_cacheDeleteTagPosition.x, m_cacheDeleteTagPosition.y, m_cacheDeleteTagPosition.z);

            }
            else
            {
                InfotagCanvas.transform.localPosition = new Vector3(m_cacheInfoTagPosition.x, m_cacheInfoTagPosition.y, m_cacheInfoTagPosition.z * -1);
                AssortmentCanvas.transform.localPosition = new Vector3(m_cacheAssrtmentTagPosition.x, m_cacheAssrtmentTagPosition.y, m_cacheAssrtmentTagPosition.z * -1);
                PickupCanvas.transform.localPosition = new Vector3(m_cachePickupTagPosition.x, m_cachePickupTagPosition.y, m_cachePickupTagPosition.z * -1);
                DeleteCanvas.transform.localPosition = new Vector3(m_cacheDeleteTagPosition.x, m_cacheDeleteTagPosition.y, m_cacheDeleteTagPosition.z * -1);
            }
        }

        /// <summary>
        /// Show Infotags
        /// </summary>
        private void ShowInfotags()
        {
            if (settings.useInfotags)
            {
                ShowProductTag(true);

                if (!settings.hideAssortmentMenu)
                {
                    AssortmentCanvas.SetActive(true);

                    var assortmentMenu = AssortmentCanvas.GetComponent<AssortmentMenu>();

                    if (assortmentMenu != null)
                    {
                        if (AppManager.Instance.Settings.playerSettings.useAutoAdd)
                        {
                            assortmentMenu.AutoAddButton.SetActive(true);
                        }

                        if (AppManager.Instance.Settings.playerSettings.usePickup)
                        {
                            assortmentMenu.PickupButton.SetActive(true);
                        }
                    }
                }
            }
        }

        public void ShowPickuptags(bool show)
        {
            AssortmentCanvas.SetActive(show);

            var assortmentMenu = AssortmentCanvas.GetComponent<AssortmentMenu>();

            if (assortmentMenu != null)
            {
                assortmentMenu.AutoAddButton.SetActive(false);
                assortmentMenu.PickupButton.SetActive(true);
            }
        }

        /// <summary>
        /// Show Assortment Tags
        /// </summary>
        private void ShowAssortmentTags()
        {
            DeleteCanvas.SetActive(true);
            PickupCanvas.SetActive(true);
        }


        [System.Serializable]
        public class ProductIOObject : IObjectSetting
        {
            public bool useTooltip = false;
            public int DefaultAssortment = 0;
            public string ProductCode;

            [TextArea]
            public string InfotagText;
            public string InfotagPicture;
            public bool useInfotags = true;
            public bool hideAssortmentMenu = false;
            public bool persistentInfotags = false;

            public List<InfotagManager.InfoTagURL> ImageInfotagsUrls = new List<InfotagManager.InfoTagURL>();
            public List<InfotagManager.InfoTagURL> VideoInfotagsUrls = new List<InfotagManager.InfoTagURL>();
            public List<InfotagManager.InfoTagURL> WebInfotagsUrls = new List<InfotagManager.InfoTagURL>();
            public List<InfotagManager.InfoTagURL> Spin360InfotagsUrls = new List<InfotagManager.InfoTagURL>();
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

            this.settings.useTooltip = ((ProductIOObject)settings).useTooltip;
            this.settings.DefaultAssortment = ((ProductIOObject)settings).DefaultAssortment;
            this.settings.ProductCode = ((ProductIOObject)settings).ProductCode;

           // this.settings.InfotagPicture = ((ProductIOObject)settings).InfotagPicture;
            this.settings.InfotagText = ((ProductIOObject)settings).InfotagText;
            this.settings.useInfotags = ((ProductIOObject)settings).useInfotags;
            this.settings.hideAssortmentMenu = ((ProductIOObject)settings).hideAssortmentMenu;
            this.settings.persistentInfotags = ((ProductIOObject)settings).persistentInfotags;

            this.settings.ImageInfotagsUrls = ((ProductIOObject)settings).ImageInfotagsUrls;
            this.settings.VideoInfotagsUrls = ((ProductIOObject)settings).VideoInfotagsUrls;
            this.settings.WebInfotagsUrls = ((ProductIOObject)settings).WebInfotagsUrls;
            this.settings.Spin360InfotagsUrls = ((ProductIOObject)settings).Spin360InfotagsUrls;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Product), true), CanEditMultipleObjects]
        public class Product_Editor : UniqueID_Editor
        {
            private Product product;

            private void OnEnable()
            {
                GetBanner();
                Initialise();
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    m_instances.RemoveIOObject(product.GetSettings(true));
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                DisplayID();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Product Name", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("DefaultAssortment"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("ProductCode"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("InfotagText"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("InfotagPicture"), true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Product Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("useTooltip"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("useInfotags"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("hideAssortmentMenu"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("persistentInfotags"), true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Product URLs", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("ImageInfotagsUrls"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("VideoInfotagsUrls"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("WebInfotagsUrls"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("Spin360InfotagsUrls"), true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Product Objects", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ProductMesh"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("InfotagCanvas"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DeleteCanvas"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("PickupCanvas"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("RailPoint"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("TablePoint"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("HoldPoint"), true);

                if (GUI.changed || GONameChanged())
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(product);

                    if (Application.isPlaying) return;

                    if (m_instances != null)
                    {
                        m_instances.AddIOObject(product.ID, product.GetSettings());
                    }
                }
            }

            protected override void Initialise()
            {
                base.Initialise();

                product = (Product)target;

                if (Application.isPlaying) return;

                if (product.transform.localScale != Vector3.one)
                {
                    product.transform.localScale = Vector3.one;
                }

                if (m_instances != null)
                {
                    //need to get the settings from the instances script then update the settings
                    foreach (AppInstances.IOObjectProductHandler setting in m_instances.ioProductObjects)
                    {
                        if (setting.referenceID.Equals(product.ID))
                        {
                            product.ApplySettings(setting.settings);
                            break;
                        }
                    }

                    m_instances.AddIOObject(product.ID, product.GetSettings());
                }
            }
        }
#endif
    }
}
