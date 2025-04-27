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
    public class HotspotButton : MonoBehaviour
    {
        private Hotspot m_meta;
        private RectTransform rectT;
        private RawImage img;

        public void Set(Hotspot meta)
        {
            m_meta = meta;
            GetComponentInChildren<Button>(true).onClick.AddListener(OnClick);
        }

        private void OnEnable()
        {
            StartCoroutine(WaitFrame());
        } 

        private IEnumerator WaitFrame()
        {
            yield return new WaitForEndOfFrame();

            rectT = GetComponent<RectTransform>();
            img = transform.GetChild(0).GetComponentInChildren<RawImage>(true);

            if (img != null)
            {
                var path = "Hotspots/" + m_meta.hotspotImage;
                var texture = (Texture2D)Resources.Load(path);
                img.texture = texture;
                Resize();
            }
        }

        public void Resize(float multiplier = 1.0f)
        {
            if (!gameObject.activeInHierarchy) return;

            //ensure image fills viewport
            Vector2 viewport = rectT.sizeDelta;
            RectTransform imageRect = img.GetComponent<RectTransform>();
            img.SetNativeSize();

            if (img.texture.width < viewport.x)
            {
                float aspect = viewport.x / img.texture.width;
                imageRect.sizeDelta = new Vector2(img.texture.width * aspect, img.texture.height * aspect);
            }
            else
            {
                float aspect = img.texture.width / viewport.x;
                imageRect.sizeDelta = new Vector2(img.texture.width / aspect, img.texture.height / aspect);
            }

            if (imageRect.sizeDelta.y < viewport.y)
            {
                float aspect = viewport.y / imageRect.sizeDelta.y;
                imageRect.sizeDelta = new Vector2(imageRect.sizeDelta.x * aspect, imageRect.sizeDelta.y * aspect);
            }
        }

        private void OnClick()
        {
            HotspotManager.Instance.OnClickHotspot(m_meta);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(HotspotButton), true)]
        public class HotspotButton_Editor : BaseInspectorEditor
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
