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
    public class SocialMediaPanel : MonoBehaviour
    {
        [Header("Social Platform")]
        [SerializeField]
        private Image socialIcon;

        [Header("Data")]
        [SerializeField]
        private TMP_InputField comment;
        [SerializeField]
        private CanvasGroup commentError;
        [SerializeField]
        private TMP_InputField link;
        [SerializeField]
        private RawImage picture;
        [SerializeField]
        private GameObject screenshotButton;

        [Header("Progress")]
        [SerializeField]
        private GameObject progress;

        [Header("Mobile Layout")]
        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private TextMeshProUGUI commentstTitle;
        [SerializeField]
        private TextMeshProUGUI urlTitle;
        [SerializeField]
        private RectTransform footerLayout;

        private string m_data;
        private bool m_streaming = false;
        private SocialMediaManager.PostType m_type;
        private SocialMediaSettings.SocialMediaPlatform m_platform;
        private Texture2D m_tx;


        private float m_layoutWidth;
        private RectTransform m_mainLayout;
        private float m_cacheTitleText;

        private float m_detailsTitleHeight;
        private float m_detailsInputFontSize;

        private float m_urlTitleHeight;
        private float m_urlInputFontSize;
        private float m_urlInputHeight;

        private void Awake()
        {
            m_mainLayout = transform.GetChild(0).GetComponent<RectTransform>();
            m_layoutWidth = m_mainLayout.sizeDelta.x;

            m_cacheTitleText = titleText.fontSize;

            m_detailsTitleHeight = commentstTitle.fontSize;
            m_detailsInputFontSize = comment.textComponent.fontSize;

            m_urlTitleHeight = urlTitle.fontSize;
            m_urlInputFontSize = link.textComponent.fontSize;
            m_urlInputHeight = link.GetComponent<LayoutElement>().minHeight;
        }

        private void OnEnable()
        {
            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            SubtitleManager.Instance.ToggleButtonVisibiliy(false);
        }

        private void OnDisable()
        {
            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;

            SubtitleManager.Instance.ToggleButtonVisibiliy(true);

            if (picture.texture != null)
            {
                if (m_streaming)
                {
                    Destroy(picture.texture);
                }

                picture.texture = null;
            }

            if (m_tx != null)
            {
                Destroy(m_tx);
            }

            m_streaming = false;
            m_data = "";
            comment.text = "";
            RectTransform rectT = picture.GetComponent<RectTransform>();
            rectT.gameObject.GetComponentInChildren<AspectRatioFitter>(true).aspectRatio = 0;
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    m_mainLayout.anchorMin = new Vector2(0.5f, 0.0f);
                    m_mainLayout.anchorMax = new Vector2(0.5f, 1.0f);
                    m_mainLayout.anchoredPosition = Vector2.zero;
                    m_mainLayout.sizeDelta = new Vector2(m_layoutWidth, m_mainLayout.sizeDelta.y);

                    titleText.fontSize = m_cacheTitleText;

                    socialIcon.SetNativeSize();

                    commentstTitle.fontSize = m_detailsTitleHeight;

                    foreach (TMP_Text txt in comment.GetComponentsInChildren<TMP_Text>())
                    {
                        txt.fontSize = m_detailsInputFontSize;
                    }

                    urlTitle.fontSize = m_urlTitleHeight;

                    foreach (TMP_Text txt in link.GetComponentsInChildren<TMP_Text>())
                    {
                        txt.fontSize = m_urlInputFontSize;
                    }

                    foreach (Selectable but in footerLayout.GetComponentsInChildren<Selectable>(true))
                    {
                        LayoutElement le = but.GetComponent<LayoutElement>();
                        le.minHeight = 60 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;

                        TextMeshProUGUI txt = but.GetComponentInChildren<TextMeshProUGUI>(true);
                        txt.fontSize = 32;
                    }

                    link.GetComponent<LayoutElement>().minHeight = m_urlInputHeight;
                }
                else
                {
                    float aspect = arg2 / arg1;

                    m_mainLayout.anchorMin = new Vector2(0f, 0.0f);
                    m_mainLayout.anchorMax = new Vector2(1f, 1.0f);
                    m_mainLayout.offsetMax = new Vector2(-50, m_mainLayout.offsetMax.y);
                    m_mainLayout.offsetMin = new Vector2(50, m_mainLayout.offsetMin.y);

                    m_mainLayout.anchoredPosition = Vector2.zero;

                    titleText.fontSize = m_cacheTitleText * aspect;

                    RectTransform iconRect = socialIcon.GetComponent<RectTransform>();
                    iconRect.sizeDelta = new Vector2(iconRect.sizeDelta.x * aspect, iconRect.sizeDelta.y * aspect);

                    commentstTitle.fontSize = m_detailsTitleHeight * aspect;

                    foreach (TMP_Text txt in comment.GetComponentsInChildren<TMP_Text>())
                    {
                        txt.fontSize = m_detailsInputFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    }

                    urlTitle.fontSize = m_urlTitleHeight * aspect;

                    foreach (TMP_Text txt in link.GetComponentsInChildren<TMP_Text>())
                    {
                        txt.fontSize = m_urlInputFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    }

                    foreach (Selectable but in footerLayout.GetComponentsInChildren<Selectable>(true))
                    {
                        LayoutElement le = but.GetComponent<LayoutElement>();
                        le.minHeight = (60 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler) * aspect;

                        TextMeshProUGUI txt = but.GetComponentInChildren<TextMeshProUGUI>(true);
                        txt.fontSize = 32 * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    }

                    link.GetComponent<LayoutElement>().minHeight = m_urlInputHeight * aspect;
                }
            }
        }

        public void Set(SocialMediaSettings.SocialMediaPlatform platform, SocialMediaManager.PostType type, string data = "")
        {
            m_data = data;
            m_type = type;
            m_platform = platform;
            commentError.alpha = 0.0f;

            socialIcon.sprite = SocialMediaManager.Instance.GetIcon(platform);
            socialIcon.SetNativeSize();

            switch (type)
            {
                case SocialMediaManager.PostType.Comment_Only:
                    picture.transform.parent.gameObject.SetActive(false);
                    link.transform.parent.gameObject.SetActive(false);
                    screenshotButton.SetActive(false);
                    break;
                case SocialMediaManager.PostType.Image_Comment:
                    picture.transform.parent.gameObject.SetActive(true);
                    link.transform.parent.gameObject.SetActive(false);
                    comment.text = "";

                    //load image
                    if (!string.IsNullOrEmpty(data))
                    {
                       // StartCoroutine(LoadImage(data));
                        screenshotButton.SetActive(false);
                    }
                    else
                    {
                        screenshotButton.SetActive(false);
                    }

                    break;
                case SocialMediaManager.PostType.Video_Comment:
                    picture.transform.parent.gameObject.SetActive(false);
                    link.transform.parent.gameObject.SetActive(false);
                    comment.text = "";
                    screenshotButton.SetActive(false);

                    break;
                case SocialMediaManager.PostType.Link_Comment:
                    picture.transform.parent.gameObject.SetActive(false);
                    link.transform.parent.gameObject.SetActive(true);

                    comment.text = "";
                    link.text = data;
                    screenshotButton.SetActive(false);
                    break;
                default:
                    break;
            }
        }

        public void Close()
        {
            StopAllCoroutines();
            SocialMediaManager.Instance.CloseSocialMediaPost();
        }

        public void TakeScreenshot()
        {
            CanvasGroup cGroup = null;

            if (CoreManager.Instance.MainCanvas != null)
            {
                cGroup = CoreManager.Instance.MainCanvas.GetComponent<CanvasGroup>();

                if (cGroup == null)
                {
                    cGroup = CoreManager.Instance.MainCanvas.gameObject.AddComponent<CanvasGroup>();
                }

                cGroup.alpha = 0.0f;
                cGroup.interactable = false;
            }

            if(m_tx != null)
            {
                Destroy(m_tx);
            }

            StartCoroutine(ProcessScreenshot(cGroup));
        }

        private IEnumerator ProcessScreenshot(CanvasGroup cGroup)
        {
            yield return new WaitForEndOfFrame();

            m_tx = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            m_tx.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            m_tx.Apply();

            string path = Application.temporaryCachePath + "/sharedimage.jpg";
            System.IO.File.WriteAllBytes(path, m_tx.EncodeToJPG());

            m_data = path;

            picture.texture = m_tx;
            UpdateTextureSize();

            if (cGroup != null)
            {
                cGroup.alpha = 1.0f;
                cGroup.interactable = true;
            }
        }

        public void Post()
        {
            if(string.IsNullOrEmpty(comment.text))
            {
                commentError.alpha = 0.5f;
            }
            else
            {
                commentError.alpha = 0.0f;
                StartCoroutine(DelayPost());
            }
        }

        private IEnumerator DelayPost()
        {
            progress.SetActive(true);
            yield return new WaitForSeconds(2.0f);
            SocialMediaManager.Instance.Post(m_platform, m_type, comment.text, m_data, PostCallback);
        }

        private void PostCallback(bool success)
        {
            progress.SetActive(false);

            if (success)
            {
                Close();
            }
        }

        private IEnumerator LoadImage(string url)
        {
            if (url.Contains("http"))
            {
                m_streaming = true;

                //webrequest
                UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, true);

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(request.error))
                {
                    picture.texture = DownloadHandlerTexture.GetContent(request);
                }

                //dispose the request as not needed anymore
                request.Dispose();
            }
            else
            {
                m_streaming = false;
                picture.texture = Resources.Load<Texture>(url);
            }

            UpdateTextureSize();
        }

        private void UpdateTextureSize()
        {
            if (picture.texture != null)
            {
                RectTransform rectT = picture.GetComponent<RectTransform>();
                float texWidth = picture.texture.width;
                float texHeight = picture.texture.height;
                float aspectRatio = texWidth / texHeight;
                rectT.gameObject.GetComponentInChildren<AspectRatioFitter>(true).aspectRatio = aspectRatio;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SocialMediaPanel), true)]
        public class SocialMediaPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("socialIcon"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("comment"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("commentError"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("link"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("picture"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("screenshotButton"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("progress"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("titleText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("commentstTitle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("urlTitle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("footerLayout"), true);

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
