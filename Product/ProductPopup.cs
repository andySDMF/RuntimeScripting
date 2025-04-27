using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ProductPopup : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI title;
        [SerializeField]
        private TextMeshProUGUI message;
        [SerializeField]
        private RawImage picture;

        private RectTransform pictureViewport;
        private bool m_destroyTexture = false;
        private Product m_product;

        private RectTransform m_mainLayout;
        private float m_layoutWidth;

        private float m_titleFontSize;
        private float m_messageFontSize;

        private void Awake()
        {
            m_mainLayout = transform.GetChild(0).GetComponent<RectTransform>();
            m_layoutWidth = m_mainLayout.sizeDelta.x;

            m_titleFontSize = title.fontSize;
            m_messageFontSize = message.fontSize;
        }

        private void OnEnable()
        {
            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            SubtitleManager.Instance.Stop();
            SubtitleManager.Instance.ToggleButtonVisibiliy(false);
        }

        private void OnDisable()
        {
            StopAllCoroutines();

            if(picture != null)
            {
                if(m_destroyTexture && picture.texture != null)
                {
                    DestroyImmediate(picture.texture);
                }

                m_destroyTexture = false;
                picture.texture = null;
                pictureViewport.gameObject.SetActive(false);
            }


            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;

            SubtitleManager.Instance.ToggleButtonVisibiliy(true);
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    m_mainLayout.anchorMin = new Vector2(0.5f, 0.5f);
                    m_mainLayout.anchorMax = new Vector2(0.5f, 0.5f);
                    m_mainLayout.anchoredPosition = Vector2.zero;
                    m_mainLayout.sizeDelta = new Vector2(m_layoutWidth, m_mainLayout.sizeDelta.y);

                    title.fontSize = m_titleFontSize;
                    message.fontSize = m_messageFontSize;

                    foreach (Button but in m_mainLayout.GetComponentsInChildren<Button>())
                    {
                        but.GetComponentInChildren<TextMeshProUGUI>().fontSize = 32;
                        but.GetComponent<LayoutElement>().minHeight = 52 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
                        but.GetComponent<LayoutElement>().preferredHeight = 52 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
                    }
                }
                else
                {
                    float aspect = arg2 / arg1;

                    m_mainLayout.anchorMin = new Vector2(0f, 0.5f);
                    m_mainLayout.anchorMax = new Vector2(1f, 0.5f);
                    m_mainLayout.offsetMax = new Vector2(-50, 0);
                    m_mainLayout.offsetMin = new Vector2(50, 0);

                    title.fontSize = m_titleFontSize * aspect;
                    message.fontSize = m_messageFontSize * aspect;

                    foreach (Button but in m_mainLayout.GetComponentsInChildren<Button>())
                    {
                        but.GetComponentInChildren<TextMeshProUGUI>().fontSize = 32 * AppManager.Instance.Settings.HUDSettings.mobileFontScaler * aspect;
                        but.GetComponent<LayoutElement>().minHeight = 52 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler * aspect;
                        but.GetComponent<LayoutElement>().preferredHeight = 52 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler * aspect;
                    }
                }
            }
        }

        public void Show(Product product)
        {
            m_product = product;
            title.text = product.settings.ProductCode;
            message.text = product.settings.InfotagText;

            if (picture != null)
            {
                if (pictureViewport == null)
                {
                    pictureViewport = picture.transform.parent.GetComponent<RectTransform>();
                }

                pictureViewport.gameObject.SetActive(false);
                //HUDManager.Instance.ShowHUDNavigationVisibility(false);
                PlayerManager.Instance.FreezePlayer(true);

                if(AppManager.Instance.Settings.playerSettings.productPopupInfoShowImage)
                {
                    if (product.settings.InfotagPicture.Equals("Material"))
                    {
                        pictureViewport.gameObject.SetActive(true);
                        picture.texture = product.ProductMesh.GetComponent<MeshRenderer>().material.mainTexture;
                        StartCoroutine(Resize());
                    }
                    else
                    {
                        Texture tex = Resources.Load<Texture>(product.settings.InfotagPicture);

                        if (tex == null)
                        {
                            //try webrequest as it might be a URL
                            StartCoroutine(LoadURL(product.settings.InfotagPicture));
                        }
                        else
                        {
                            pictureViewport.gameObject.SetActive(true);
                            picture.texture = tex;
                            StartCoroutine(Resize());
                        }
                    }
                }
            }
        }

        public void Pickup()
        {
            Close();

            if(m_product != null)
            {
                m_product.Pickup();
            }

            m_product = null;
        }

        public void Close()
        {
            HUDManager.Instance.ToggleHUDMessage("PRODUCT_MESSAGE", false);
           // HUDManager.Instance.ShowHUDNavigationVisibility(true);
            PlayerManager.Instance.FreezePlayer(false);
        }

        private IEnumerator LoadURL(string data)
        {
            bool check = false;
            m_destroyTexture = true;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        check = true;
#elif !UNITY_EDITOR && UNITY_IOS
        check = true;
#endif
            if (check)
            {
                if (!data.Contains("file://"))
                {
                    data = "file://" + data;
                }
            }

            UnityWebRequest request = UnityWebRequestTexture.GetTexture(data, true);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(request.error))
            {
                pictureViewport.gameObject.SetActive(true);
                picture.texture = DownloadHandlerTexture.GetContent(request);
            }

            request.Dispose();

            if (picture.texture != null)
            {
                StartCoroutine(Resize());
            }
        }

        public IEnumerator Resize(float multiplier = 1.0f)
        {
            yield return new WaitForEndOfFrame();

            //ensure image fills viewport
            Vector2 viewport = pictureViewport.sizeDelta;
            RectTransform imageRect = picture.GetComponent<RectTransform>();
            picture.SetNativeSize();

            AspectRatioFitter ratio = null;

            if (GetComponentsInChildren<AspectRatioFitter>(true).Length > 0)
            {
                ratio = GetComponentsInChildren<AspectRatioFitter>(true)[0];
            }

            if (ratio != null)
            {
                float texWidth = picture.texture.width;
                float texHeight = picture.texture.height;
                float aspectRatio = texWidth / texHeight;
                ratio.aspectRatio = aspectRatio;
            }

            /* if (picture.texture.width < viewport.x)
             {
                 float aspect = viewport.x / picture.texture.width;
                 imageRect.sizeDelta = new Vector2(picture.texture.width * aspect, picture.texture.height * aspect);
             }
             else
             {
                 float aspect = picture.texture.width / viewport.x;
                 imageRect.sizeDelta = new Vector2(picture.texture.width / aspect, picture.texture.height / aspect);
             }

             if (imageRect.sizeDelta.y < viewport.y)
             {
                 float aspect = viewport.y / imageRect.sizeDelta.y;
                 imageRect.sizeDelta = new Vector2(imageRect.sizeDelta.x * aspect, imageRect.sizeDelta.y * aspect);
             }*/
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ProductPopup), true)]
        public class ProductPopup_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("title"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("message"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("picture"), true);

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
