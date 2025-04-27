using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class InfotagManager : Singleton<InfotagManager>, IRaycaster
    {
        public static InfotagManager Instance
        {
            get
            {
                return ((InfotagManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Interaction")]
        private float interactionDistance = 5;
        private bool useLocalDistance = true;

        public bool OverrideDistance { get { return useLocalDistance; } }

        private WebPopUpType webPopUpType = WebPopUpType.IFrame;

        public List<InfoTagIcon> icons = new List<InfoTagIcon>();

        public Action<List<InfoTagURL>, bool> showInfotagContentListener;

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

            webPopUpType = CoreManager.Instance.playerSettings.webPopUpType;

#if BRANDLAB360_WEBVIEW && !UNITY_EDITOR

            Debug.Log("Webview not available anymore");
#endif
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
            if (hit.transform.GetComponent<PopupTag>())
            {
                hitObject = hit.transform;
            }
            else
            {
                hitObject = null;
            }

            if (InputManager.Instance.GetMouseButtonUp(0))
            {
                //detect components
                var item = hit.transform.GetComponent<PopupTag>();

                //will need to check if user can pick up item eventually once networked ITEM has to have uniqueID implemtation
                string user = PlayerManager.Instance.GetLocalPlayer().CustomizationData.ContainsKey(m_userKey) ? PlayerManager.Instance.GetLocalPlayer().CustomizationData[m_userKey].ToString() : "";

                if (item != null)
                {
                    if (!item.CanUserControlThis(user))
                    {
                        return;
                    }

                    item.OnClick();
                }
            }
        }

        public void RaycastMiss()
        {

        }

        public void OpenURL(InfoTagURL url, bool isImage = false)
        {
            List<InfoTagURL> urls = new List<InfoTagURL>();
            urls.Add(url);

            showInfotagContent(urls, isImage);
        }

        /// <summary>
        /// Show the Infotag content
        /// </summary>
        /// <param name="urls">the list of urls for the content</param>
        /// <param name="isImage">if it is an image it needs to be handled differently on the website</param>
        private void showInfotagContent(List<InfoTagURL> urls, bool isImage)
        {
            Debug.Log("Showing info tag");

            if (urls.Count <= 0) return;

            if (webPopUpType.Equals(WebPopUpType.IFrame))
            {
                var infotagMsg = new InfotagMessage();
                var urlStr = "";

                for (int i = 0; i < urls.Count; i++)
                {
                    if (i > 0)
                    {
                        urlStr += ",";
                    }

                    urlStr += urls[i].url;
                }

                infotagMsg.url = urlStr;
                infotagMsg.isImage = isImage;
                WebclientManager.Instance.Send(JsonUtility.ToJson(infotagMsg));
            }
            else if (webPopUpType.Equals(WebPopUpType.Window))
            {
                var infotagWindow = new InfotagWindow();
                infotagWindow.url = urls[0].url;
                infotagWindow.title = urls[0].title;
                WebclientManager.Instance.Send(JsonUtility.ToJson(infotagWindow));
            }
            else if (webPopUpType.Equals(WebPopUpType.Webview))
            {
#if BRANDLAB360_WEBVIEW && !UNITY_EDITOR
                Debug.Log("Webview not available anymore");
#endif
            }

            if (showInfotagContentListener != null)
            {
                showInfotagContentListener.Invoke(urls, isImage);
            }
        }

        /// <summary>
        /// Returns the icon of the popuptype
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Sprite GetIcon(InfotagType type)
        {
            return icons.FirstOrDefault(x => x.type.Equals(type)).icon;
        }

        /// <summary>
        /// Called to show infotag content
        /// </summary>
        /// <param name="infotagType"></param>
        /// <param name="infoTag"></param>
        public void ShowInfoTag(InfotagType infotagType, InfoTagURL infoTag)
        {
            List<InfoTagURL> temp = new List<InfoTagURL>();
            temp.Add(infoTag);

            if (infotagType.Equals(InfotagType.Image))
            {
                showInfotagContent(temp, true);
            }
            else
            {
                showInfotagContent(temp, false);
            }
        }

        /// <summary>
        /// Show the infotag content for this infotag
        /// </summary>
        /// <param name="infotagType">the infotag type</param>
        /// <param name="product">the product containing the data</param>
        public void ShowInfotag(InfotagType infotagType, Product product)
        {
            if (infotagType == InfotagType.Web)
            {
                showInfotagContent(product.settings.WebInfotagsUrls, false);

            }
            else if (infotagType == InfotagType.Image)
            {
                showInfotagContent(product.settings.ImageInfotagsUrls, true);

            }
            else if (infotagType == InfotagType.Video)
            {
                showInfotagContent(product.settings.VideoInfotagsUrls, false);

            }
            else if (infotagType == InfotagType.Spin360)
            {
                showInfotagContent(product.settings.Spin360InfotagsUrls, false);

            }
            else if (infotagType == InfotagType.Text)
            {
                //shop UI popup
                //show popup message.
                ProductPopup popup = HUDManager.Instance.GetHUDMessageObject("PRODUCT_MESSAGE").GetComponentInChildren<ProductPopup>(true);
                HUDManager.Instance.ToggleHUDMessage("PRODUCT_MESSAGE", true);
                popup.Show(product);
            }
        }

/*#if BRANDLAB360_WEBVIEW

        /// <summary>
        /// Callback for when webviewManager loads the webpage
        /// </summary>
        /// <param name="url">The url that was loaded</param>
        public void OnShowWebpage(string url)
        {
            PlayerManager.Instance.GetLocalPlayer().ToggleCanMove(false);
        }

        /// <summary>
        /// Callback for when webviewManager closes the webpage
        /// </summary>
        public void OnCloseWebpage()
        {
            PlayerManager.Instance.GetLocalPlayer().ToggleCanMove(true);
        }
#endif*/

        [System.Serializable]
        public class InfoTagURL
        {
            public string url;
            public string title;
        }

        [System.Serializable]

        public class InfoTagIcon
        {
            public InfotagType type;
            public Sprite icon;
        }

        [System.Serializable]
        public enum WebPopUpType { IFrame, Window, Webview, None }

#if UNITY_EDITOR
        [CustomEditor(typeof(InfotagManager), true)]
        public class InfotagManager_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("icons"), true);

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