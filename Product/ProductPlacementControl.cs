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
    public class ProductPlacementControl : MonoBehaviour
    {
        [Header("Main UI Menu")]
        [SerializeField]
        private GameObject deleteButton;

        [SerializeField]
        private Toggle selectModeToggle;

        [SerializeField]
        private GameObject standardDisplay;

        [SerializeField]
        private GameObject dropDisplay;

        private bool m_enabled = false;

        private void OnEnable()
        {
            m_enabled = false;
            selectModeToggle.isOn = false;
            deleteButton.SetActive(false);

            standardDisplay.SetActive(true);
            dropDisplay.SetActive(false);

            StartCoroutine(DelayEnabled());
        }

        private IEnumerator DelayEnabled()
        {
            yield return new WaitForEndOfFrame();

            m_enabled = true;
        }

        public void ToggleAddProductPanel(bool isOn)
        {
            PlayerManager.Instance.FreezePlayer(isOn);
            HUDManager.Instance.ToggleHUDScreen("PRODUCTPLACEMENT_SCREEN");
        }

        public void ToggleDropDisplay(bool show)
        {
            standardDisplay.SetActive(!show);
            dropDisplay.SetActive(show);
        }

        public void MultiAdd()
        {
            string shop = ProductPlacementManager.Instance.AdminController.settings.shop;

#if UNITY_EDITOR
            Debug.Log("Cannot load multi add in Editor");
#else
            ProductAPI.Instance.OpenFile(ProductPlacementManager.Instance.AdminController.ID, shop, "", MultiAddCallback);
#endif
        }

        public void Finish()
        {
            ProductPlacementManager.Instance.FinishPlacementControl();
        }

        public void Drop()
        {
            ProductPlacementManager.Instance.DropProductInPlacement(ProductPlacementManager.Instance.AdminController.ID);
        }

        public void ToggleSelectMode(bool state)
        {
            if(m_enabled)
            {
                ProductPlacementManager.Instance.ToggleSelectMode(state);
            }
          
            deleteButton.SetActive(state);
        }

        public void DeleteSelected()
        {
            ProductPlacementManager.Instance.DeleteSelectedProducts();
        }

        private void MultiAddCallback(string data)
        {
            if(data.Equals("Multi"))
            {
                //need to get all products from API for this collection
                ProductAPI.Instance.GetProductsForPlacement(ProductPlacementManager.Instance.AdminController.ID);

                //then tell all others to do the same
                ProductPlacementSync.Instance.SyncProductPlacment(ProductPlacementManager.Instance.AdminController.ID);
            }
            else
            {
                //need to open the add panel, but in edit mode
                ProductPlacementAddPanel addPanel = HUDManager.Instance.GetHUDScreenObject("PRODUCTPLACEMENT_SCREEN").GetComponentInChildren<ProductPlacementAddPanel>(true);
                addPanel.LoadedTexture = data;
                addPanel.MultiAddResponse = true;

                //need to open the add panel and pass this url to it
                ToggleAddProductPanel(true);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ProductPlacementControl), true)]
        public class ProductPlacementControl_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("deleteButton"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("selectModeToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("standardDisplay"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dropDisplay"), true);

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
