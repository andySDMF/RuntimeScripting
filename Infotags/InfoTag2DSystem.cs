using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class InfoTag2DSystem : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField]
        private GameObject assortment;
        [SerializeField]
        private GameObject pickup;
        [SerializeField]
        private GameObject tags;

        [Header("Info Buttons")]
        [SerializeField]
        private GameObject videoButton;
        [SerializeField]
        private GameObject imageButton;
        [SerializeField]
        private GameObject webButton;

        private bool m_visible = false;
        private Product m_product;

        private void Update()
        {
            if(m_product != null)
            {
                transform.position = Camera.main.WorldToScreenPoint(m_product.transform.position);
            }
        }

        public void Show(Product prod)
        {
            if (m_visible) return;

            m_visible = true;

            m_product = prod;

            if(prod.inAssortment)
            {
                tags.SetActive(false);
                pickup.SetActive(false);
                assortment.SetActive(true);
            }
            else
            {
                pickup.SetActive(true);
                assortment.SetActive(false);

                bool showWeb = m_product.settings.WebInfotagsUrls.Count >= 1;
                bool showImage = m_product.settings.ImageInfotagsUrls.Count >= 1;
                bool showVideo = m_product.settings.VideoInfotagsUrls.Count >= 1;

                webButton.SetActive(showWeb);
                imageButton.SetActive(showImage);
                videoButton.SetActive(showVideo);

                if(!showWeb && !showImage && !showVideo)
                {
                    tags.SetActive(false);
                }
                else
                {
                    tags.SetActive(true);
                }
            }

            transform.position = Camera.main.WorldToScreenPoint(m_product.transform.position);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (!m_visible) return;

            m_visible = false;

            gameObject.SetActive(false);

            tags.SetActive(false);
            pickup.SetActive(false);
            assortment.SetActive(false);
        }

        public void Add()
        {
            if(m_product != null)
            {
                m_product.AddToAssortmentAuto();
            }
        }

        public void Pickup()
        {
            if (m_product != null)
            {
                m_product.Pickup();
            }
        }

        public void Delete()
        {
            if (m_product != null)
            {
                m_product.RemoveFromAssortment();
            }
        }

        public void Video()
        {
            if (m_product != null)
            {
                InfotagManager.Instance.ShowInfotag(InfotagType.Video, m_product);
            }
        }

        public void Image()
        {
            if (m_product != null)
            {
                InfotagManager.Instance.ShowInfotag(InfotagType.Image, m_product);
            }
        }

        public void Web()
        {
            if (m_product != null)
            {
                InfotagManager.Instance.ShowInfotag(InfotagType.Web, m_product);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(InfoTag2DSystem), true)]
        public class InfoTag2DSystem_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("assortment"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pickup"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tags"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("videoButton"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("imageButton"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("webButton"), true);

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
