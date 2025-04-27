using System;
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
    public class ProductPlacementEditButton : MonoBehaviour
    {
        private int m_productID = -1;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        public void Set(int productID)
        {
            m_productID = productID;
        }

        private void OnClick()
        {
            //need to open the add panel, but in edit mode
            ProductPlacementAddPanel addPanel = HUDManager.Instance.GetHUDScreenObject("PRODUCTPLACEMENT_SCREEN").GetComponentInChildren<ProductPlacementAddPanel>(true);
            addPanel.IsEditModeOn = true;
            addPanel.EditableProductID = m_productID;

            PlayerManager.Instance.FreezePlayer(true);
            HUDManager.Instance.ToggleHUDScreen("PRODUCTPLACEMENT_SCREEN");
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ProductPlacementEditButton), true)]
        public class ProductPlacementEditButton_Editor : BaseInspectorEditor
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