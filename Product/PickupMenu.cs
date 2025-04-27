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
    public class PickupMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private bool useTooltip = false;

        public void OnclickPickup(bool ignoreRaycast = false)
        {
            if(!ignoreRaycast)
            {
                if (!RaycastManager.Instance.UIRaycastOperation(gameObject, true)) return;

                Selectable[] all = GetComponentsInChildren<Selectable>();

                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].gameObject.name.Contains("Pickup"))
                    {
                        RaycastManager.Instance.UIRaycastSelectablePressed(all[i]);
                        break;
                    }
                }
            }

            var product = GetComponentInParent<Product>();
            
            if (product != null)
            {
                //need to check if in product placement admin mode
                if(ProductPlacementManager.Instance.AdminController != null)
                {
                    ProductPlacementManager.Instance.PickUpProduct(product);
                }
                else
                {
                    product.Pickup();
                }
               
                OnPointerExit(null);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!RaycastManager.Instance.UIRaycastOperation(gameObject)) return;

            useTooltip = GetComponentInParent<Product>().settings.useTooltip;

            if (useTooltip)
            {
                RaycastManager.Instance.CastRay = false;
                TooltipManager.Instance.ShowTooltip(AppManager.Instance.Instances.GetFixedTooltip("PickupMenu"));
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
        [CustomEditor(typeof(PickupMenu), true)]
        public class PickupMenu_Editor : BaseInspectorEditor
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