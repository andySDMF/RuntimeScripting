using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class TooltipManager : Singleton<TooltipManager>
    {
        public static TooltipManager Instance
        {
            get
            {
                return ((TooltipManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private GameObject tooltipObject;
        private TextMeshProUGUI tooltipText;


        private bool followMousePosition = false;
        private float tooltipOffset = 100.0f;
        private bool m_visible = false;
        private RectTransform m_recT;
        private GameObject m_current;

        public bool IsVisible
        {
            get
            {
                return m_visible;
            }
        }

        private void Start()
        {
            //get project settings
            followMousePosition = CoreManager.Instance.HUDSettings.tooltipFollowMousePosition;
            tooltipOffset = CoreManager.Instance.HUDSettings.tooltipMouseVerticalOffset;

            HideTooltip(true);
        }

        private void Update()
        {
            //follow mouse if true
            if(m_visible)
            {
                UpdatePosition();
            }
        }

        /// <summary>
        /// Called to show tooltip by GO
        /// </summary>
        /// <param name="go"></param>
        public void ShowTooltip(GameObject go)
        {
            bool useTooltip = (CoreManager.Instance.HUDSettings.useTooltips) ?  PlayerManager.Instance.MainControlSettings.tooltipOn.Equals(1) : CoreManager.Instance.HUDSettings.useTooltips;

            if (!useTooltip) return;

            if (m_visible) return;

            m_current = go;
            m_visible = true;

            AppInstances.Tooltip tTip = AppManager.Instance.Instances.GetTooltip(go.GetComponent<Tooltip>().ID);

            if (tTip != null)
            {
                if (tooltipText == null)
                {
                    tooltipText = HUDManager.Instance.GetHUDMessageObject("TOOLTIP_MESSAGE").GetComponentInChildren<TextMeshProUGUI>(true);
                }

                if (tooltipText != null)
                {
                    tooltipText.text = tTip.tooltip;
                }

                UpdatePosition();
                HUDManager.Instance.ToggleHUDMessage("TOOLTIP_MESSAGE", true);
            }
        }

        /// <summary>
        /// Called to show the tooltip by ID
        /// </summary>
        /// <param name="id"></param>
        public void ShowTooltip(string id)
        {
            bool useTooltip = (CoreManager.Instance.HUDSettings.useTooltips) ? PlayerManager.Instance.MainControlSettings.tooltipOn.Equals(1) : CoreManager.Instance.HUDSettings.useTooltips;

            if (!useTooltip) return;

            if (m_visible) return;

            m_visible = true;
            m_current = null;

            AppInstances.Tooltip tTip = AppManager.Instance.Instances.GetTooltip(id);

            if(tTip != null)
            {
                if (tooltipText == null)
                {
                    tooltipText = HUDManager.Instance.GetHUDMessageObject("TOOLTIP_MESSAGE").GetComponentInChildren<TextMeshProUGUI>(true);
                }

                if (tooltipText != null)
                {
                    tooltipText.text = tTip.tooltip;
                }

                UpdatePosition();
                HUDManager.Instance.ToggleHUDMessage("TOOLTIP_MESSAGE", true);
            }
        }

        /// <summary>
        /// Called to hide the tooltip
        /// </summary>
        /// <param name="forceHide"></param>
        public void HideTooltip(bool forceHide = false)
        {
            if (!m_visible && !forceHide) return;

            m_visible = false;
            m_current = null;

            HUDManager.Instance.ToggleHUDMessage("TOOLTIP_MESSAGE", false);

            if (tooltipText != null)
            {
                tooltipText.text = "";
            }
        }

        private void UpdatePosition()
        {
            if (m_recT == null)
            {
                m_recT = HUDManager.Instance.GetHUDMessageObject("TOOLTIP_MESSAGE").GetComponent<RectTransform>();
            }

            if (followMousePosition)
            {
                Vector3 pos = InputManager.Instance.GetMousePosition();
                m_recT.position = new Vector3(pos.x, pos.y + tooltipOffset, pos.z);
            }
            else
            {
                if (m_current != null)
                {
                    m_recT.position = Camera.main.WorldToScreenPoint(m_current.transform.position);
                }
            }
        }
    }

    public static class RendererExtensions
    {
        /// <summary>
        /// Counts the bounding box corners of the given RectTransform that are visible from the given Camera in screen space.
        /// </summary>
        /// <returns>The amount of bounding box corners that are visible from the Camera.</returns>
        /// <param name="rectTransform">Rect transform.</param>
        /// <param name="camera">Camera.</param>
        private static int CountCornersVisibleFrom(this RectTransform rectTransform, Camera camera)
        {
            Rect screenBounds = new Rect(0f, 0f, Screen.width, Screen.height); // Screen space bounds (assumes camera renders across the entire screen)
            Vector3[] objectCorners = new Vector3[4];
            rectTransform.GetWorldCorners(objectCorners);

            int visibleCorners = 0;
            Vector3 tempScreenSpaceCorner; // Cached
            for (var i = 0; i < objectCorners.Length; i++) // For each corner in rectTransform
            {
                tempScreenSpaceCorner = camera.WorldToScreenPoint(objectCorners[i]); // Transform world space position of corner to screen space
                if (screenBounds.Contains(tempScreenSpaceCorner)) // If the corner is inside the screen
                {
                    visibleCorners++;
                }
            }
            return visibleCorners;
        }

        /// <summary>
        /// Determines if this RectTransform is fully visible from the specified camera.
        /// Works by checking if each bounding box corner of this RectTransform is inside the cameras screen space view frustrum.
        /// </summary>
        /// <returns><c>true</c> if is fully visible from the specified camera; otherwise, <c>false</c>.</returns>
        /// <param name="rectTransform">Rect transform.</param>
        /// <param name="camera">Camera.</param>
        public static bool IsFullyVisibleFrom(this RectTransform rectTransform, Camera camera)
        {
            return CountCornersVisibleFrom(rectTransform, camera) == 4; // True if all 4 corners are visible
        }

        /// <summary>
        /// Determines if this RectTransform is at least partially visible from the specified camera.
        /// Works by checking if any bounding box corner of this RectTransform is inside the cameras screen space view frustrum.
        /// </summary>
        /// <returns><c>true</c> if is at least partially visible from the specified camera; otherwise, <c>false</c>.</returns>
        /// <param name="rectTransform">Rect transform.</param>
        /// <param name="camera">Camera.</param>
        public static bool IsVisibleFrom(this RectTransform rectTransform, Camera camera)
        {
            return CountCornersVisibleFrom(rectTransform, camera) > 0; // True if any corners are visible
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(TooltipManager), true)]
        public class TooltipManager_Editor : BaseInspectorEditor
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
