using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class SocialMediaManager : Singleton<SocialMediaManager>
    {
        public static SocialMediaManager Instance
        {
            get
            {
                return ((SocialMediaManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        [SerializeField]
        private Sprite facebookIcon;
        [SerializeField]
        private Sprite instagramIcon;
        [SerializeField]
        private Sprite twitterIcon;
        [SerializeField]
        private Sprite whatsappIcon;
        [SerializeField]
        private Sprite tiktokIcon;
        [SerializeField]
        private Sprite linkedinIcon;

        private const string BRANDLAB_COPYRIGHT = "\n\n In colaboration with Brandlab360 https://www.brandlab-360.com/";
        private const string BRANDLAB_FACEBOOK_APP_ID = "110698675322257";

        public string FacebookApp_ID
        {
            get
            {
                string appID = "";

                if (AppManager.IsCreated)
                {
                    appID = AppManager.Instance.Settings.socialMediaSettings.facebookAppID;

                    if (string.IsNullOrEmpty(appID))
                    {
                        appID = BRANDLAB_FACEBOOK_APP_ID;
                    }

                }

                return appID;
            }
        }

        public Sprite GetIcon(SocialMediaSettings.SocialMediaPlatform socialPlatform)
        {
            Sprite sp = null;

            switch (socialPlatform)
            {
                case SocialMediaSettings.SocialMediaPlatform.Facebook:
                    sp = facebookIcon;
                    break;
                case SocialMediaSettings.SocialMediaPlatform.Instagram:
                    sp = instagramIcon;
                    break;
                case SocialMediaSettings.SocialMediaPlatform.Twitter:
                    sp = twitterIcon;
                    break;
                case SocialMediaSettings.SocialMediaPlatform.WhatsApp:
                    sp = whatsappIcon;
                    break;
                case SocialMediaSettings.SocialMediaPlatform.TikTok:
                    sp = tiktokIcon;
                    break;
                case SocialMediaSettings.SocialMediaPlatform.LinkedIn:
                    sp = linkedinIcon;
                    break;
                default:
                    break;
            }

            return sp;
        }

        private void Start()
        {
            if(AppManager.IsCreated)
            {
                SocialMediaCanvas[] all = FindObjectsByType<SocialMediaCanvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                bool visible = AppManager.Instance.Settings.socialMediaSettings.socialMediaEnabled;

                for(int i = 0; i < all.Length; i++)
                {
                    //need to ignore if part of main UI
                    if (all[i].gameObject.GetComponentInParent<CoreManager>() != null) continue;

                    all[i].IsVisible(visible);
                }
            }
        }

        public void OpenSocialMediaPost(SocialMediaSettings.SocialMediaPlatform socialPlatform, PostType postType, string data = "")
        {
            if(postType.Equals(PostType.Image_Comment) && string.IsNullOrEmpty(data))
            {
                Debug.Log("Cannot Post Screen Shot Image ATM. Diverting to Comment post type");
                postType = PostType.Comment_Only;
            }

            PlayerManager.Instance.FreezePlayer(true);
            RaycastManager.Instance.CastRay = false;

            //this will open the 
            HUDManager.Instance.ToggleHUDScreen("SOCIALMEDIA_SCREEN");
            SocialMediaPanel socialPanel = HUDManager.Instance.GetHUDScreenObject("SOCIALMEDIA_SCREEN").GetComponent<SocialMediaPanel>();
            socialPanel.Set(socialPlatform, postType, data);
        }

        public void CloseSocialMediaPost()
        {
            PlayerManager.Instance.FreezePlayer(false);
            RaycastManager.Instance.CastRay = true;

            //this will open the 
            HUDManager.Instance.ToggleHUDScreen("SOCIALMEDIA_SCREEN");
        }

        public void Post(SocialMediaSettings.SocialMediaPlatform socialPlatform, PostType postType, string comment, string data, System.Action<bool> callback = null)
        {
            Debug.Log("this will need to post to something. Either using Application.OpenURL or via WebClient Backend");
            string post = comment + BRANDLAB_COPYRIGHT;
            string url = "";
#if !UNITY_EDITOR
            InfotagManager.InfoTagURL tag = null;
#endif

            switch (socialPlatform)
            {
                case SocialMediaSettings.SocialMediaPlatform.Facebook:
                    string facebook = "https://www.facebook.com/dialog/feed";
                    string app_id = FacebookApp_ID;

                    if (postType.Equals(PostType.Comment_Only))
                    {
                        url = facebook + "?app_id=" + app_id + "&description=" + post;
                    }
                    else if(postType.Equals(PostType.Image_Comment))
                    {
                        url = facebook + "?app_id=" + app_id + "&picture=" + data + "&description=" + post;
                    }
                    else if(postType.Equals(PostType.Link_Comment))
                    {
                        url = facebook + "?app_id=" + app_id + "&link=" + data + "&description=" + post;
                    }
                    else
                    {
                        url = facebook + "?app_id=" + app_id + "&video=" + data + "&description=" + post;
                    }

#if UNITY_EDITOR
                    Application.OpenURL(url);
#else
                    tag = new InfotagManager.InfoTagURL();
                    tag.title = "Facebook";
                    tag.url = url;
                    InfotagManager.Instance.ShowInfoTag(InfotagType.Web, tag);
#endif
                    break;
                case SocialMediaSettings.SocialMediaPlatform.Instagram:
                    break;
                case SocialMediaSettings.SocialMediaPlatform.Twitter:
                    string twitter = "http://twitter.com/intent/tweet";
                    string content = (postType.Equals(PostType.Comment_Only)) ? "" : "?media=" + UnityWebRequest.EscapeURL(data);
                    url = twitter + "?text=" + UnityWebRequest.EscapeURL(post) + content + "&amp;lang=" + UnityWebRequest.EscapeURL("en");
#if UNITY_EDITOR
                    Application.OpenURL(url);
#else
                    tag = new InfotagManager.InfoTagURL();
                    tag.title = "Twitter";
                    tag.url = url;
                    InfotagManager.Instance.ShowInfoTag(InfotagType.Web, tag);
#endif
                    break;
                case SocialMediaSettings.SocialMediaPlatform.WhatsApp:
                    string whatsapp = "https://api.whatsapp.com/send/?text=";
                    url = whatsapp + UnityWebRequest.EscapeURL(data) + UnityWebRequest.EscapeURL(post);
#if UNITY_EDITOR
                    Application.OpenURL(url);
#else
                    tag = new InfotagManager.InfoTagURL();
                    tag.title = "WhatsApp";
                    tag.url = url;
                    InfotagManager.Instance.ShowInfoTag(InfotagType.Web, tag);
#endif

                    break;
                case SocialMediaSettings.SocialMediaPlatform.TikTok:
                    break;
                case SocialMediaSettings.SocialMediaPlatform.LinkedIn:
                    string linkedIn = "http://www.linkedin.com/shareArticle?mini=true";
                    string link = (string.IsNullOrEmpty(data)) ? "" : "&url=" + data;
                    string other = "&summary=" + UnityWebRequest.EscapeURL(post);
                    url = linkedIn + UnityWebRequest.EscapeURL(link) + other;
#if UNITY_EDITOR
                    Application.OpenURL(url);
#else
                    tag = new InfotagManager.InfoTagURL();
                    tag.title = "LinkedIn";
                    tag.url = url;
                    InfotagManager.Instance.ShowInfoTag(InfotagType.Web, tag);
#endif
                    break;
                default:
                    break;
            }

            if(callback != null)
            {
                callback.Invoke(true);
            }
        }

        public enum PostType { Comment_Only, Image_Comment, Video_Comment, Link_Comment }

#if UNITY_EDITOR
        [CustomEditor(typeof(SocialMediaManager), true)]
        public class SocialMediaManager_Editor : BaseInspectorEditor
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

                    EditorGUILayout.LabelField("Source Icons", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("facebookIcon"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("instagramIcon"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("twitterIcon"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("whatsappIcon"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tiktokIcon"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("linkedinIcon"), true);

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
