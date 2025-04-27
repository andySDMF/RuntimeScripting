using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrandLab360
{
    public class SocialMediaProductButton : SocialMediaButton
    {
        private string productCode;

        public override void OnClick()
        {
            SocialMediaCanvas smCanvas = GetComponentInParent<SocialMediaCanvas>();
            string comment = "";

            if (smCanvas != null)
            {
                if (string.IsNullOrEmpty(productCode))
                {
                    if (string.IsNullOrEmpty(smCanvas.Reference))
                    {
                        productCode = smCanvas.Reference;
                    }
                }

                if (!string.IsNullOrEmpty(smCanvas.Comment))
                {
                    comment = smCanvas.Comment;
                }
            }

            //get product in parent
            if(string.IsNullOrEmpty(productCode))
            {
                productCode = GetComponentInParent<Product>().settings.ProductCode;
            }

            Product product = ProductManager.Instance.FindProduct(productCode);

            if (product != null)
            {
                attachedURL = (product.settings.WebInfotagsUrls.Count > 0) ? product.settings.WebInfotagsUrls[0].url : "";
                SocialMediaManager.PostType postType = SocialMediaManager.PostType.Link_Comment;

                AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, "Posted Social Media " + socialPlatform.ToString());

                //if web info tag is null, use image tag
                if (string.IsNullOrEmpty(attachedURL))
                {
                    attachedURL = (product.settings.ImageInfotagsUrls.Count > 0) ? product.settings.ImageInfotagsUrls[0].url : "";
                    postType = SocialMediaManager.PostType.Image_Comment;
                }
                //if image info tag is null, use video tag
                if (string.IsNullOrEmpty(attachedURL))
                {
                    attachedURL = (product.settings.VideoInfotagsUrls.Count > 0) ? product.settings.VideoInfotagsUrls[0].url : "";
                    postType = postType = SocialMediaManager.PostType.Video_Comment;
                }

                //if data is null, then just use the product texture
                if (string.IsNullOrEmpty(attachedURL))
                {
                    attachedURL = product.ProductMesh.GetComponent<ProductMesh>().rawTextureSource;
                    postType = SocialMediaManager.PostType.Image_Comment;
                }

                //if data is still null then just comment only
                if(string.IsNullOrEmpty(attachedURL))
                {
                    postType = SocialMediaManager.PostType.Comment_Only;
                }

                if (comment.Equals("-1"))
                {
                    //just post
                    SocialMediaManager.Instance.Post(socialPlatform, postType, "", attachedURL);
                }
                else if (!comment.Equals("-2"))
                {
                    //just postwith fixed comment
                    SocialMediaManager.Instance.Post(socialPlatform, postType, comment, attachedURL);
                }
                else
                {
                    SocialMediaManager.Instance.OpenSocialMediaPost(socialPlatform, postType, attachedURL);
                }

                GetComponentInParent<Toggle>().isOn = false;
            }
        }
    }
}
