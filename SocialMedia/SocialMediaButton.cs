using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Button))]
    public class SocialMediaButton : MonoBehaviour
    {
        [SerializeField]
        protected string attachedURL = "";

        [SerializeField]
        protected SocialMediaSettings.SocialMediaPlatform socialPlatform = SocialMediaSettings.SocialMediaPlatform.Facebook;

        private bool m_isGlobalMessage = false;

        private void Awake()
        {
            if (!AppManager.IsCreated) return;

            if(IsEnabled())
            {
                GetComponent<Button>().onClick.AddListener(OnClick);
            }
        }

        public virtual void OnClick()
        {
            SocialMediaCanvas smCanvas = GetComponentInParent<SocialMediaCanvas>();
            string comment = "";

            if (smCanvas != null)
            {
                if (string.IsNullOrEmpty(attachedURL))
                {
                    if (string.IsNullOrEmpty(smCanvas.Reference))
                    {
                        attachedURL = smCanvas.Reference;
                    }
                }

                if (!string.IsNullOrEmpty(smCanvas.Comment))
                {
                    comment = smCanvas.Comment;
                }
            }

            SocialMediaManager.PostType postType = SocialMediaManager.PostType.Comment_Only;
            string data = attachedURL;

            m_isGlobalMessage = gameObject.GetComponentInParent<CoreManager>() != null;

            if (!string.IsNullOrEmpty(data))
            {
                if(socialPlatform.Equals(SocialMediaSettings.SocialMediaPlatform.Facebook))
                {
                    if (data.Contains("jpg") || data.Contains("png"))
                    {
                        postType = SocialMediaManager.PostType.Image_Comment;
                    }
                    else if (data.Contains("mp4") || data.Contains("mov") || data.Contains("avi"))
                    {
                        postType = SocialMediaManager.PostType.Video_Comment;
                    }
                    else if (data.Contains("http"))
                    {
                        postType = SocialMediaManager.PostType.Link_Comment;
                    }
                    else
                    {
                        postType = SocialMediaManager.PostType.Link_Comment;
                        data = AppManager.Instance.Settings.socialMediaSettings.mainURL;
                    }
                }
                else
                {
                    postType = SocialMediaManager.PostType.Link_Comment;
                }
            }
            else
            {
                data = AppManager.Instance.Settings.socialMediaSettings.mainURL;
                m_isGlobalMessage = true;
            }

            if(m_isGlobalMessage)
            {
                switch (AppManager.Instance.Settings.socialMediaSettings.commentType)
                {
                    case SocialMediaCanvas.CommentType.Allow:
                        comment = "-2";
                        break;
                    case SocialMediaCanvas.CommentType.Fixed:
                        comment = AppManager.Instance.Settings.socialMediaSettings.fixedComment;
                        break;
                    case SocialMediaCanvas.CommentType.None:
                        comment = "-1";
                        break;
                }
            }

            if (comment.Equals("-1"))
            {
                //just post
                SocialMediaManager.Instance.Post(socialPlatform, postType, "", attachedURL);

                AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, "Posted Social Media " + socialPlatform.ToString());

            }
            else if (!comment.Equals("-2"))
            {
                //just postwith fixed comment
                SocialMediaManager.Instance.Post(socialPlatform, postType, comment, attachedURL);

                AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, "Posted Social Media " + socialPlatform.ToString());
            }
            else
            {
                SocialMediaManager.Instance.OpenSocialMediaPost(socialPlatform, postType, data);
            }

            GetComponentInParent<Toggle>().isOn = false;
        }

        protected bool IsEnabled()
        { 
            switch(socialPlatform)
            {
                case SocialMediaSettings.SocialMediaPlatform.Facebook:

                    if(string.IsNullOrEmpty(SocialMediaManager.Instance.FacebookApp_ID))
                    {
                        gameObject.SetActive(false);
                    }
                    else
                    {
                        gameObject.SetActive(AppManager.Instance.Settings.socialMediaSettings.enableFacebookPost);
                    }
                    break;
                case SocialMediaSettings.SocialMediaPlatform.Instagram:
                    gameObject.SetActive(AppManager.Instance.Settings.socialMediaSettings.enableInstagramPost);
                    break;
                case SocialMediaSettings.SocialMediaPlatform.Twitter:
                    gameObject.SetActive(AppManager.Instance.Settings.socialMediaSettings.enableTwitterPost);
                    break;
                case SocialMediaSettings.SocialMediaPlatform.WhatsApp:
                    gameObject.SetActive(AppManager.Instance.Settings.socialMediaSettings.enableWhatsAppPost);
                    break;
                case SocialMediaSettings.SocialMediaPlatform.TikTok:
                    gameObject.SetActive(AppManager.Instance.Settings.socialMediaSettings.enableTikTokPost);
                    break;
                case SocialMediaSettings.SocialMediaPlatform.LinkedIn:
                    gameObject.SetActive(AppManager.Instance.Settings.socialMediaSettings.enableLinkedInPost);
                    break;
                default:
                    break;
            }

            return gameObject.activeInHierarchy;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SocialMediaButton), true), CanEditMultipleObjects]
        public class SocialMediaButton_Editor : BaseInspectorEditor
        {
            private SocialMediaButton script;
            private bool m_socialMedaiCanvasAttached = false;

            private void OnEnable()
            {
                GetBanner();
                script = (SocialMediaButton)target;

                m_socialMedaiCanvasAttached = script.GetComponentInParent<SocialMediaCanvas>(true) != null;
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                if(!m_socialMedaiCanvasAttached)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("attachedURL"), true);
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("socialPlatform"), true);

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(script);
                }
            }
        }
#endif
        }
}
