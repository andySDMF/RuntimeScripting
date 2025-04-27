using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// Infotag is a UI button that displays on a product to link content / media
    /// </summary>
    public class Infotag : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private bool useTooltip = false;

        public InfotagType infotagType;

        /// <summary>
        /// Callback when the infotag was clicked on
        /// </summary>
        public void OnClick()
        {
            Collider col = GetComponentInParent<Collider>();
            GameObject go = (col != null) ? col.gameObject : gameObject;

            if (!RaycastManager.Instance.UIRaycastOperation(go)) return;

            RaycastManager.Instance.UIRaycastSelectablePressed(GetComponent<Selectable>());

            var product = GetComponentInParent<Product>();

            if(product != null)
            {
                InfotagManager.Instance.ShowInfotag(infotagType, product);

                var label = infotagType.ToString() + " " + product.settings.ProductCode;
                AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Product, EventAction.Click, label);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!RaycastManager.Instance.UIRaycastOperation(gameObject)) return;

            useTooltip = GetComponentInParent<Product>().settings.useTooltip;

            if (useTooltip)
            {
                RaycastManager.Instance.CastRay = false;
                TooltipManager.Instance.ShowTooltip(AppManager.Instance.Instances.GetFixedTooltip("InfoTag"));
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //need to check if tooltip is active
            if (!TooltipManager.Instance.IsVisible) return;

            useTooltip = GetComponentInParent<Product>().settings.useTooltip;

            if (useTooltip)
            {
                TooltipManager.Instance.HideTooltip();
            }
            
            RaycastManager.Instance.CastRay = true;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Infotag), true)]
        public class Infotag_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();


                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("infotagType"), true);


                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }

    public enum InfotagType { Video, Web, Image, Spin360, Text }
}