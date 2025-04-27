using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class MobileButtonScaler : MonoBehaviour
    {
        private LayoutElement m_layoutElement;
        private RectTransform m_rectT;

        private float m_Scaler = 0.0f;
        private Vector2 m_cacheSize;

        private Dictionary<RectTransform, Vector2> m_icons = new Dictionary<RectTransform, Vector2>();
        private Dictionary<TextMeshProUGUI, float> m_texts = new Dictionary<TextMeshProUGUI, float>();

        private bool m_init = false;

        private void Awake()
        {
            if (!AppManager.IsCreated) return;

            m_layoutElement = GetComponent<LayoutElement>();
            m_rectT = GetComponent<RectTransform>();

            if (m_layoutElement != null && !m_layoutElement.ignoreLayout)
            {
                m_cacheSize = new Vector2(m_layoutElement.minWidth, m_layoutElement.minHeight);
            }
            else
            {
                m_cacheSize = m_rectT.sizeDelta;
            }

             m_Scaler = AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
        }

        private void OnEnable()
        {
            if (!AppManager.IsCreated) return;

            if(m_init)
            {
                OrientationManager.Instance.OnOrientationChanged += OnOrientation;
                OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            }
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            Image[] images = GetComponentsInChildren<Image>(true);

            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].transform == transform) continue;

                bool ignore = images[i].GetComponentInParent<MobileButtonScaler>(true) != null && images[i].GetComponentInParent<MobileButtonScaler>(true) != this ? true : false;

                if (images[i].GetComponentInChildren<IgnoreParentScaler>(true) || ignore) continue;

                LayoutElement il = images[i].GetComponent<LayoutElement>();

                if (il != null && !il.ignoreLayout)
                {
                    m_icons.Add(images[i].GetComponent<RectTransform>(), new Vector2(il.minWidth, il.minHeight));

                }
                else
                {
                    m_icons.Add(images[i].GetComponent<RectTransform>(), images[i].GetComponent<RectTransform>().sizeDelta);

                }
            }

            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].transform == transform) continue;

                bool ignore = texts[i].GetComponentInParent<MobileButtonScaler>(true) != null && texts[i].GetComponentInParent<MobileButtonScaler>(true) != this ? true : false;


                if (texts[i].GetComponentInChildren<IgnoreParentScaler>(true) || ignore) continue;

                m_texts.Add(texts[i], texts[i].fontSize);
            }


            OrientationManager.Instance.OnOrientationChanged += OnOrientation;
            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            m_init = true;
        }

        private void OnDisable()
        {
            if (!AppManager.IsCreated) return;

            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        private void OnOrientation(OrientationType orientation, int width, int height)
        {
			if (AppManager.Instance.Data.IsMobile)
			{
                if(orientation.Equals(OrientationType.landscape))
                {
                    if (m_layoutElement != null && !m_layoutElement.ignoreLayout)
                    {
                        m_layoutElement.minWidth = m_cacheSize.x * m_Scaler;
                        m_layoutElement.minHeight = m_cacheSize.y * m_Scaler;
                        m_layoutElement.preferredWidth = m_layoutElement.minWidth;
                        m_layoutElement.preferredHeight = m_layoutElement.minHeight;

                        m_rectT.sizeDelta = new Vector2(m_layoutElement.minWidth, m_layoutElement.minHeight);
                    }
                    else
                    {
                        m_rectT.sizeDelta = new Vector2(m_cacheSize.x * m_Scaler, m_cacheSize.y * m_Scaler);
                    }

                    foreach(KeyValuePair<RectTransform, Vector2> img in m_icons)
                    {
                        LayoutElement il = img.Key.GetComponent<LayoutElement>();

                        if (il != null && !il.ignoreLayout)
                        {
                            il.minWidth = img.Value.x;
                            il.minHeight = img.Value.y;
                            il.preferredWidth = il.minWidth;
                            il.preferredHeight = il.minHeight;

                            m_rectT.sizeDelta = new Vector2(il.minWidth, il.minHeight);
                        }
                        else
                        {
                            img.Key.sizeDelta = new Vector2(img.Value.x, img.Value.y);

                        }
                    }

                    foreach (KeyValuePair<TextMeshProUGUI, float> txt in m_texts)
                    {
                        txt.Key.fontSize = txt.Value * m_Scaler;
                    }
                }
                else
                {
                    if (m_layoutElement != null && !m_layoutElement.ignoreLayout)
                    {
                        m_layoutElement.minWidth = (m_cacheSize.x * m_Scaler) * (height / width);
                        m_layoutElement.minHeight = (m_cacheSize.y * m_Scaler) * (height / width);
                        m_layoutElement.preferredWidth = m_layoutElement.minWidth;
                        m_layoutElement.preferredHeight = m_layoutElement.minHeight;

                        m_rectT.sizeDelta = new Vector2(m_layoutElement.minWidth, m_layoutElement.minHeight);
                    }
                    else
                    {
                        m_rectT.sizeDelta = new Vector2((m_cacheSize.x * m_Scaler) * (height / width), (m_cacheSize.y * m_Scaler) * (height / width));
                    }

                    foreach (KeyValuePair<RectTransform, Vector2> img in m_icons)
                    {
                        LayoutElement il = img.Key.GetComponent<LayoutElement>();

                        if (il != null && !il.ignoreLayout)
                        {
                            il.minWidth = (img.Value.x * m_Scaler) * (height / width);
                            il.minHeight = (img.Value.y * m_Scaler) * (height / width);
                            il.preferredWidth = il.minWidth;
                            il.preferredHeight = il.minHeight;

                            m_rectT.sizeDelta = new Vector2(il.minWidth, il.minHeight);
                        }
                        else
                        {
                            img.Key.sizeDelta = new Vector2((img.Value.x * m_Scaler) * (height / width), (img.Value.y * m_Scaler) * (height / width));
                        }
                    }

                    foreach (KeyValuePair<TextMeshProUGUI, float> txt in m_texts)
                    {
                        txt.Key.fontSize = (txt.Value * m_Scaler) * (height / width);
                    }
                }
			}
		}

#if UNITY_EDITOR
        [CustomEditor(typeof(MobileButtonScaler), true)]
        public class MobileButtonScaler_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
            }
        }
#endif
    }
}
