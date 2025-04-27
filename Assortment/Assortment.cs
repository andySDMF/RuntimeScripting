using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public enum AssortmentType { Default, Rail, Table };

    /// <summary>
    /// Assortment is a dynamic collection of products e.g. a rail of clothes or a table of shoes
    /// The products within can be moved around and synced.
    /// </summary>
    public class Assortment : UniqueID
    {
        public int overridingIndex = -1;

        public Transform AssortmentParent;
        public GameObject AssortmentBounds;
        public AssortmentType assortmentType;

        private new void Awake()
        {
            base.Awake();

            //not good as devs might not group assotments
            //overridingIndex = transform.GetSiblingIndex();
        }

        /// <summary>
        /// Sort the assortment to store and update the Z offset to fix zfighting issues
        /// </summary>
        public void SortAssortment()
        {
            for (int i = 0; i < AssortmentParent.transform.childCount; i++)
            {
                AssortmentParent.GetChild(i).GetComponent<Product>().Sort = i;
            }
        }

        /// <summary>
        ///  Find a product in the assortment index if it exists
        /// </summary>
        /// <param name="productCode">product code to find</param>
        /// <param name="insertID">the insert ID of the product when it was added to database</param>
        /// <returns></returns>
        public Product FindProduct(string productCode, int insertID)
        {
            Product product = null;
            bool found = false;

            foreach (Transform child in AssortmentParent)
            {
                var childProduct = child.GetComponent<Product>();

                if (childProduct.settings.ProductCode == productCode && childProduct.InsertID == insertID)
                {
                    return child.GetComponent<Product>();
                }
            }

            if (!found) { Debug.LogError("Error: couldnt find requested product: " + productCode + " in assortment index: " + overridingIndex.ToString()); }

            return product;
        }

        public void HideAllAssortmentTags()
        {
            foreach (Transform child in AssortmentParent)
            {
                var childProduct = child.GetComponent<Product>();

                if (childProduct != null)
                {
                    childProduct.HideTags(false);
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Assortment), true), CanEditMultipleObjects]
        public class Assortment_Editor : UniqueID_Editor
        {
            private Assortment assortment;
            private AppSettings m_settings;
            private SerializedObject m_asset;

            private void OnEnable()
            {
                GetBanner();
                assortment = (Assortment)target;

                AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");

                if (appReferences != null)
                {
                    m_settings = appReferences.Settings;
                }
                else
                {
                    m_settings = Resources.Load<AppSettings>("ProjectAppSettings");
                }

                m_asset = new SerializedObject(m_settings);
                bool noIndex = false;

                if (!Application.isPlaying)
                {
                    if (assortment.gameObject.scene.IsValid() && assortment.gameObject.scene.name != null)
                    {
                        if (string.IsNullOrEmpty(assortment.ID) || UniqueIDManager.Instance.Exists(assortment.ID, assortment))
                        {
                            noIndex = true;
                        }
                    }

                    if (noIndex && m_settings != null)
                    {
                        //this will ensure that this assortment will have an index based app settings assortment index accumalator
                        m_settings.editorTools.assortmentIndexAccumulator++;
                        assortment.overridingIndex = m_settings.editorTools.assortmentIndexAccumulator;

                        if (m_asset != null) m_asset.ApplyModifiedProperties();

                        EditorUtility.SetDirty(m_settings);
                    }
                }

                base.Initialise();

            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                DrawID();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Assortment", EditorStyles.boldLabel);
                //EditorGUILayout.LabelField("Index: " + serializedObject.FindProperty("overridingIndex").intValue, EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("overridingIndex"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("AssortmentParent"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("AssortmentBounds"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("assortmentType"), true);

                if (assortment.AssortmentBounds != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Editor", EditorStyles.boldLabel);
                    assortment.AssortmentBounds.transform.localScale = EditorGUILayout.Vector3Field("Bounds Area:", assortment.AssortmentBounds.transform.localScale);
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(script);
            }

            private void OnSceneGUI()
            {
                if (assortment.transform.localScale != Vector3.one)
                {
                    assortment.transform.localScale = Vector3.one;
                }
            }
        }
#endif
    }
}