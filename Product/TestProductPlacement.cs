using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrandLab360;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
public class TestProductPlacement : MonoBehaviour
{
    [SerializeField]
    private string absoluteFolderPath = "";

    private ProductPlacement m_placement;

    private void Start()
    {
        if (!AppManager.IsCreated) return;

        m_placement = GetComponent<ProductPlacement>();

        if(m_placement != null)
        {
            StartCoroutine(CreateAll());
        }
    }

    private IEnumerator CreateAll()
    {
        yield return new WaitForEndOfFrame();

        //read folder
        if(Directory.Exists(absoluteFolderPath))
        {
            string[] files = Directory.GetFiles(absoluteFolderPath);

            for(int i = 0; i < files.Length; i++)
            {
                ProductPlacement.ProductPlacementObject obj = new ProductPlacement.ProductPlacementObject();
                obj.id = i;
                obj.productCode = Path.GetFileName(files[i]);
                obj.textureURL = files[i];

                m_placement.PlaceSingleProduct(obj);
            }
        }
        else
        {
            Debug.Log("Folder path [" + absoluteFolderPath + "] does not exist");
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(HotspotManager), true)]
    public class HotspotManager_Editor : BaseInspectorEditor
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
#endif
