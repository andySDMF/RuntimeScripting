using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ProductPlacementAddPanel : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField]
        private TMP_InputField productCodeInput;
        [SerializeField]
        private CanvasGroup productCodeError;
        [SerializeField]
        private TMP_InputField textureInput;
        [SerializeField]
        private CanvasGroup textureError;
        [SerializeField]
        private TMP_InputField descriptionInput;

        [SerializeField]
        private TMP_InputField websiteInput;
        [SerializeField]
        private TMP_InputField imageInput;
        [SerializeField]
        private TMP_InputField videoInput;
        [SerializeField]
        private TMP_InputField quantityInput;

        [SerializeField]
        private CanvasGroup[] inputCanvases;

        [Header("Events")]
        [SerializeField]
        private bool autoCloseOnAdd = true;
        [SerializeField]
        private GameObject uploadProgress;

        [Header("Dropdown")]
        [SerializeField]
        private GameObject dropdownButton;
        [SerializeField]
        private GameObject dropdownObject;
        [SerializeField]
        private GameObject productCodePrefab;
        [SerializeField]
        private Transform dropdownContainer;

        [Header("Regions")]
        [SerializeField]
        private GameObject addRegion;
        [SerializeField]
        private GameObject editRegion;

        private TMP_InputField m_apiOpenFileObject;
        private Coroutine m_dropDownProcess;
        private List<GameObject> m_createdProductCodes = new List<GameObject>();
        private bool m_imageLoaded = false;

        public bool IsEditModeOn
        {
            get;
            set;
        }

        public bool MultiAddResponse
        {
            get;
            set;
        }

        public int EditableProductID
        {
            get;
            set;
        }

        public string LoadedTexture
        {
            get;
            set;
        }

        private void OnEnable()
        {
            productCodeError.alpha = 0;
            textureError.alpha = 0;

            string pCode = "";

            if (MultiAddResponse && !string.IsNullOrEmpty(LoadedTexture))
            {
                string[] stringArray = LoadedTexture.Split('/');
                pCode = stringArray[stringArray.Length - 1].Replace(CoreUtilities.GetExtension(stringArray[stringArray.Length - 1]), "");
            }

            productCodeInput.text = pCode;
            textureInput.text = LoadedTexture;
            descriptionInput.text = "";

            websiteInput.text = "";

            imageInput.text = "";

            videoInput.text = "";

            if (uploadProgress != null)
            {
                uploadProgress.SetActive(false);
            }

            quantityInput.text = "1";

            productCodeInput.onEndEdit.AddListener(OnProductCodeEndEdit);
            quantityInput.onEndEdit.AddListener(OnQuantityEndEdit);

            //need to replace the add button region with edit region
            if(IsEditModeOn)
            {
                OnProductCodeClick(EditableProductID);

                dropdownButton.SetActive(false);
                addRegion.SetActive(false);
                editRegion.SetActive(true);
            }
            else
            {
                //need to hide the file buttons
                for (int i = 0; i < inputCanvases.Length; i++)
                {
                    if(i == 0 && MultiAddResponse)
                    {
                        inputCanvases[i].alpha = 0.3f;
                        inputCanvases[i].interactable = false;
                    }
                    else
                    {
                        inputCanvases[i].alpha = 1f;
                        inputCanvases[i].interactable = true;
                    }
                }

                dropdownButton.SetActive(!MultiAddResponse);
                addRegion.SetActive(true);
                editRegion.SetActive(false);
            }
        }

        private void OnDisable()
        {
            productCodeInput.onEndEdit.RemoveListener(OnProductCodeEndEdit);
            quantityInput.onEndEdit.RemoveListener(OnQuantityEndEdit);

            IsEditModeOn = false;
            EditableProductID = -1;
            LoadedTexture = "";
            MultiAddResponse = false;
            m_imageLoaded = false;
        }

        public void AddNewProduct()
        {
            if (CheckInputs()) return;

            //if good, create send product to API
            ProductPlacement.ProductPlacementObject prod = CreateProduct();

            if (uploadProgress != null)
            {
                uploadProgress.SetActive(true);
            }

            OnQuantityEndEdit("");
            bool createInfoTags = true;

            //need to loop through all products in the shop to see if the code exists
            foreach (ProductPlacement.ProductPlacementObject obj in ProductPlacementManager.Instance.GetAllProductsFromShop(ProductPlacementManager.Instance.AdminController.settings.shop))
            {
                if (obj.productCode.Equals(productCodeInput.text))
                {
                    createInfoTags = false;
                    break;
                }
            }

            ProductAPI.Instance.AddProduct(ProductPlacementManager.Instance.AdminController.ID, prod, AddCallback, int.Parse(quantityInput.text), createInfoTags);
        }

        private bool CheckInputs()
        {
            bool failed = false;

            //check if product code is empty
            if (string.IsNullOrEmpty(productCodeInput.text))
            {
                productCodeError.alpha = 0.5f;
                failed = true;
            }
            else
            {
                productCodeError.alpha = 0;
            }

            //check if image texture is empty
            if (string.IsNullOrEmpty(textureInput.text))
            {
                textureError.alpha = 0.5f;
                failed = true;
            }
            else
            {
                textureError.alpha = 0;
            }

            return failed;
        }

        private ProductPlacement.ProductPlacementObject CreateProduct()
        {
            ProductPlacement.ProductPlacementObject prod = new ProductPlacement.ProductPlacementObject();
            prod.productCode = productCodeInput.text;
            prod.description.type = InfotagType.Text;
            prod.description.data = descriptionInput.text;
            prod.textureURL = textureInput.text;
            prod.shop = ProductPlacementManager.Instance.AdminController.settings.shop;

            if (!string.IsNullOrEmpty(websiteInput.text))
            {
                ProductPlacement.ProductPlacementInfoTag iTag = new ProductPlacement.ProductPlacementInfoTag();
                iTag.title = "Webiste";
                iTag.data = websiteInput.text;
                iTag.type = InfotagType.Web;
                prod.websites.Add(iTag);
            }

            if (!string.IsNullOrEmpty(imageInput.text))
            {
                ProductPlacement.ProductPlacementInfoTag iTag = new ProductPlacement.ProductPlacementInfoTag();
                iTag.title = "Image";
                iTag.data = imageInput.text;
                iTag.type = InfotagType.Image;
                prod.images.Add(iTag);
            }

            if (!string.IsNullOrEmpty(videoInput.text))
            {
                ProductPlacement.ProductPlacementInfoTag iTag = new ProductPlacement.ProductPlacementInfoTag();
                iTag.title = "Video";
                iTag.data = videoInput.text;
                iTag.type = InfotagType.Video;
                prod.videos.Add(iTag);
            }

            return prod;
        }

        private void AddCallback()
        {
            if (uploadProgress != null)
            {
                uploadProgress.SetActive(false);
            }

            if(autoCloseOnAdd)
            {
                ClosePanel();
            }
        }

        public void ClosePanel()
        {
            ProductPlacementControl controller = FindFirstObjectByType<ProductPlacementControl>();

            if(MultiAddResponse || (!IsEditModeOn && m_imageLoaded))
            {
                if(!string.IsNullOrEmpty(textureInput.text))
                {
                    ProductPlacementManager.Instance.DeleteProductTexture(textureInput.text);
                }
            }

            if (controller != null)
            {
                controller.ToggleAddProductPanel(false);
            }
        }

        public void OpenFile(string id)
        {
            string[] filetypes;

            switch (id)
            {
                case "ImageURL":
                    filetypes = new string[2] { "FileType", "png,jpg,jpeg" };
                    m_apiOpenFileObject = imageInput;
                    imageInput.text = OpenFileUpload(filetypes);
                    break;
                case "VideoURL":
                    filetypes = new string[2] { "FileType", "mp4,mov,avi" };
                    m_apiOpenFileObject = videoInput;
                    videoInput.text = OpenFileUpload(filetypes);
                    break;
                default:

#if UNITY_EDITOR
                    if (AppManager.Instance.Settings.editorTools.createWebClientSimulator)
                    {
                        filetypes = new string[2] { "FileType", "ProductPlacement" };
                    }
                    else
                    {
                        filetypes = new string[2] { "FileType", "png" };
                    }
#else
                    
                    if(AppManager.Instance.Settings.playerSettings.productFormat.Equals(ProductAPI.FormatRestriction.PNG))
                    {
                        filetypes = new string[1] { "png" };
                    }
                    else
                    {
                      filetypes = new string[1] { "" };
                    }
#endif
                    m_apiOpenFileObject = textureInput;
                    textureInput.text = OpenFileUpload(filetypes);
                    break;
            }
        }

        private string OpenFileUpload(string[] fileTypes)
        {
            string path = "";
            string shop = ProductPlacementManager.Instance.AdminController.settings.shop;

#if UNITY_EDITOR
            if (AppManager.Instance.Settings.editorTools.createWebClientSimulator)
            {
                ProductAPI.Instance.OpenFile(ProductPlacementManager.Instance.AdminController.ID, shop, fileTypes[1], ProductAPIOpenFileCallback);
            }
            else
            {
                path = UnityEditor.EditorUtility.OpenFilePanelWithFilters("Upload File", Application.dataPath, fileTypes);
            }
#else
            ProductAPI.Instance.OpenFile(ProductPlacementManager.Instance.AdminController.ID, shop, fileTypes[0], ProductAPIOpenFileCallback);
#endif
            return path;
        }

        private void ProductAPIOpenFileCallback(string str)
        {
            if (str.Equals("Multi"))
            {
                ClosePanel();

                //need to get all products from API for this collection
                ProductAPI.Instance.GetProductsForPlacement(ProductPlacementManager.Instance.AdminController.ID);

                //then tell all others to do the same
                ProductPlacementSync.Instance.SyncProductPlacment(ProductPlacementManager.Instance.AdminController.ID);
            }
            else
            {
                m_imageLoaded = true;

                if (m_apiOpenFileObject != null)
                {
                    m_apiOpenFileObject.text = str;
                }

                m_apiOpenFileObject = null;

                inputCanvases[0].alpha = 0.3f;
                inputCanvases[0].interactable = false;

                if(string.IsNullOrEmpty(productCodeInput.text))
                {
                    string[] stringArray = LoadedTexture.Split('/');
                    productCodeInput.text = stringArray[stringArray.Length - 1].Replace(CoreUtilities.GetExtension(stringArray[stringArray.Length - 1]), "");
                }
            }
        }

        public void ToggleProductCodeDropDown(bool isOpen)
        {
            if(!isOpen)
            {
                if(m_dropDownProcess != null)
                {
                    StopCoroutine(m_dropDownProcess);
                }

                m_dropDownProcess = null;

                for(int i = 0; i < m_createdProductCodes.Count; i++)
                {
                    Destroy(m_createdProductCodes[i]);
                }

                m_createdProductCodes.Clear();
            }
            else
            {
                m_dropDownProcess = StartCoroutine(CreateProductCodes());
            }

            dropdownObject.SetActive(isOpen);
        }

        private IEnumerator CreateProductCodes()
        {
            List<ProductPlacement.ProductPlacementObject> objs = ProductPlacementManager.Instance.GetAllProductsFromShop(ProductPlacementManager.Instance.AdminController.settings.shop);
            int count = 0;

            while(count < objs.Count)
            {
                //need to check if the product code has already been created 
                GameObject go = m_createdProductCodes.FirstOrDefault(x => x.name.Contains(objs[count].productCode));

                if(go != null)
                {
                    count++;
                }
                else
                {
                    go = Instantiate(productCodePrefab, Vector3.zero, Quaternion.identity, dropdownContainer) as GameObject;
                    go.name = "ProductCode_" + objs[count].productCode;
                    go.GetComponentInChildren<TextMeshProUGUI>(true).text = objs[count].productCode;
                    go.transform.localScale = Vector3.one;
                    go.SetActive(true);
                    m_createdProductCodes.Add(go);

                    Button button = go.GetComponentInChildren<Button>(true);
                    int n = objs[count].id;
                    button.onClick.AddListener(() => { OnProductCodeClick(n); });
                    count++;

                    yield return null;
                }
            }
        }

        private void OnProductCodeEndEdit(string str)
        {
            if (MultiAddResponse) return;

            bool foundExisting = false;

            //need to loop through all products in the shop to see if the code exists
            foreach(ProductPlacement.ProductPlacementObject obj in ProductPlacementManager.Instance.GetAllProductsFromShop(ProductPlacementManager.Instance.AdminController.settings.shop))
            {
                if(obj.productCode.Equals(productCodeInput.text))
                {
                    OnProductCodeClick(obj.id);
                    foundExisting = true;
                    EditableProductID = obj.id;
                    break;
                }
            }

            if(!foundExisting)
            {
                if(!IsEditModeOn)
                {
                    textureError.alpha = 0;
                    productCodeError.alpha = 0;
                    textureInput.text = "";
                    descriptionInput.text = "";

                    websiteInput.text = "";

                    imageInput.text = "";

                    videoInput.text = "";

                    //need to hide the file buttons
                    for (int i = 0; i < inputCanvases.Length; i++)
                    {
                        inputCanvases[i].alpha = 1f;
                        inputCanvases[i].interactable = true;
                    }

                    addRegion.SetActive(true);
                    editRegion.SetActive(false);
                }
            }
            else
            {
                if(IsEditModeOn)
                {
                    addRegion.SetActive(false);
                    editRegion.SetActive(true);
                }
                else
                {
                    addRegion.SetActive(true);
                    editRegion.SetActive(false);
                }
            }

            quantityInput.text = "1";
        }

        private void OnProductCodeClick(int id)
        {
            //set all relevant fields base don selected product code
            foreach (ProductPlacement.ProductPlacementObject obj in ProductPlacementManager.Instance.GetAllProductsFromShop(ProductPlacementManager.Instance.AdminController.settings.shop))
            {
                if(obj.id.Equals(id))
                {
                    productCodeInput.text = obj.productCode;
                    productCodeError.alpha = 0;
                    textureInput.text = obj.textureURL;
                    textureError.alpha = 0;

                    descriptionInput.text = obj.description.data;

                    websiteInput.text = (obj.websites.Count > 0) ? obj.websites[0].data : "";
                    imageInput.text = (obj.images.Count > 0) ? obj.images[0].data : "";
                    videoInput.text = (obj.videos.Count > 0) ? obj.videos[0].data : "";

                    //need to hide the file buttons
                    for (int i = 0; i < inputCanvases.Length; i++)
                    {
                        //cannot edit the image
                        if (i == 0)
                        {
                            inputCanvases[i].alpha = 0.3f;
                            inputCanvases[i].interactable = false;
                        }
                        else
                        {
                            if(IsEditModeOn)
                            {
                                inputCanvases[i].alpha = 1f;
                                inputCanvases[i].interactable = true;
                            }
                            else
                            {
                                inputCanvases[i].alpha = 0.3f;
                                inputCanvases[i].interactable = false;
                            }
                        }
                    }

                    break;
                }
            }

            quantityInput.text = "1";
            ToggleProductCodeDropDown(false);
        }


        private void OnQuantityEndEdit(string str)
        {
            if(int.Parse(quantityInput.text) < 1)
            {
                quantityInput.text = "1";
            }
        }

        public void DeleteProduct()
        {
            if(IsEditModeOn)
            {
                ProductPlacementManager.Instance.DeleteSingleProduct(EditableProductID);
                ClosePanel();
            }
        }

        public void EditAndSaveProduct()
        {
            if (IsEditModeOn)
            {
                if (CheckInputs()) return;

                if (uploadProgress != null)
                {
                    uploadProgress.SetActive(true);
                }

                //if good, create send product to API
                ProductPlacement.ProductPlacementObject original = ProductPlacementManager.Instance.AdminController.GetPlacementObject(EditableProductID);
                ProductPlacement.ProductPlacementObject prod = CreateProduct();
                prod.id = original.id;
                prod.scale = original.scale;
                prod.position = original.position;

                List<ProductPlacement.ProductPlacementInfoTag> addTags = new List<ProductPlacement.ProductPlacementInfoTag>();
                List<ProductPlacement.ProductPlacementInfoTag> updateTags = new List<ProductPlacement.ProductPlacementInfoTag>();
                List<ProductPlacement.ProductPlacementInfoTag> deleteTags = new List<ProductPlacement.ProductPlacementInfoTag>();

                //check description update
                if(!prod.description.data.Equals(original.description.data))
                {
                    if(string.IsNullOrEmpty(prod.description.data) && !string.IsNullOrEmpty(original.description.data))
                    {
                        //add to delete tags
                        deleteTags.Add(original.description);
                    }
                    else if (!string.IsNullOrEmpty(prod.description.data) && string.IsNullOrEmpty(original.description.data))
                    {
                        //add to addtags
                        addTags.Add(prod.description);
                    }
                    else
                    {
                        //add to update tags
                        prod.description.id = original.description.id;
                        updateTags.Add(prod.description);
                    }
                }

                if (prod.websites.Count.Equals(original.websites.Count))
                {
                    if (prod.websites.Count > 0)
                    {
                        if (!prod.websites[0].data.Equals(original.websites[0].data) || !prod.websites[0].title.Equals(original.websites[0].title))
                        {
                            //add to update tags
                            prod.websites[0].id = original.websites[0].id;
                            updateTags.Add(prod.websites[0]);
                        }
                    }
                }
                else
                {
                    if (prod.websites.Count > 0 && original.websites.Count <= 0)
                    {
                        //add to addtags
                        addTags.Add(prod.websites[0]);
                    }
                    else if(prod.websites.Count <= 0 && original.websites.Count > 0)
                    {
                        //add to delete tags
                        deleteTags.Add(original.websites[0]);
                    }
                }

                if (prod.images.Count.Equals(original.images.Count))
                {
                    if (prod.images.Count > 0)
                    {
                        if (!prod.images[0].data.Equals(original.images[0].data) || !prod.images[0].title.Equals(original.images[0].title))
                        {
                            //add to update tags
                            prod.images[0].id = original.images[0].id;
                            updateTags.Add(prod.images[0]);
                        }
                    }
                }
                else
                {
                    if (prod.images.Count > 0 && original.images.Count <= 0)
                    {
                        //add to addtags
                        addTags.Add(prod.images[0]);
                    }
                    else if (prod.images.Count <= 0 && original.images.Count > 0)
                    {
                        //add to delete tags
                        deleteTags.Add(original.images[0]);
                    }
                }

                if (prod.videos.Count.Equals(original.videos.Count))
                {
                    if (prod.videos.Count > 0)
                    {
                        if (!prod.videos[0].data.Equals(original.videos[0].data) || !prod.videos[0].title.Equals(original.videos[0].title))
                        {
                            //add to update tags
                            prod.videos[0].id = original.videos[0].id;
                            updateTags.Add(prod.videos[0]);
                        }
                    }
                }
                else
                {
                    if (prod.videos.Count > 0 && original.videos.Count <= 0)
                    {
                        //add to addtags
                        addTags.Add(prod.videos[0]);
                    }
                    else if (prod.videos.Count <= 0 && original.videos.Count > 0)
                    {
                        //add to delete tags
                        deleteTags.Add(original.videos[0]);
                    }
                }

                ProductAPI.Instance.UpdateProduct(ProductPlacementManager.Instance.AdminController.ID, prod, addTags.ToArray(), updateTags.ToArray(), deleteTags.ToArray(), AddCallback);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ProductPlacementAddPanel), true)]
        public class ProductPlacementAddPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("productCodeInput"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("productCodeError"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("textureInput"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("textureError"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("descriptionInput"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("websiteInput"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("imageInput"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("videoInput"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("quantityInput"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("inputCanvases"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("autoCloseOnAdd"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("uploadProgress"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dropdownButton"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dropdownObject"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("productCodePrefab"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dropdownContainer"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("addRegion"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("editRegion"), true);

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
