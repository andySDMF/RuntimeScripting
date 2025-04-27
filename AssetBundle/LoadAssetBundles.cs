using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class LoadAssetBundles : MonoBehaviour
    {
        [SerializeField]
        protected List<AssetBundleObject> assetBundles;

        [SerializeField]
        protected bool loadAsync = false;

        [SerializeField]
        private bool LoadAllOnStart = false;

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if (LoadAllOnStart)
            {
                LoadAllAssets();
            }
        }

        /// <summary>
        /// Called to load all assets within this AssetBundle list
        /// </summary>
        public virtual void LoadAllAssets()
        {
            string[] assets = new string[assetBundles.Count];
            
            for(int i = 0; i < assetBundles.Count; i++)
            {
                assets[i] = assetBundles[i].id;
            }

            StartCoroutine(ProcessAssetLoad(assets));
        }

        /// <summary>
        /// Called to load a range of assets from this Asset Bundle list
        /// </summary>
        /// <param name="assetObjectIDs"></param>
        public virtual void LoadAssets(string[] assetObjectIDs)
        {
            StartCoroutine(ProcessAssetLoad(assetObjectIDs));
        }

        /// <summary>
        /// Called locally to process the asset bundle loading
        /// </summary>
        /// <param name="assetObjectIDs"></param>
        /// <returns></returns>
        protected IEnumerator ProcessAssetLoad(string[] assetObjectIDs)
        {
            for (int i = 0; i < assetObjectIDs.Length; i++)
            {
                AssetBundleObject bundleObj = assetBundles.FirstOrDefault(x => x.id.Equals(assetObjectIDs[i]));

                //if asset bundle exists
                if(bundleObj != null)
                {
                    string relativePath = (bundleObj.urlType.Equals(AssetBundleURLType.StreamingAsset)) ? Application.streamingAssetsPath + "/" : "";

                    //check if web
                    if (bundleObj.url.Contains("http") || bundleObj.url.Contains("https"))
                    {
                        yield return StartCoroutine(AssetBundleManager.Instance.LoadWebAssetBundle(relativePath + bundleObj.url, OnAssetLoaded));
                    }
                    else
                    {
                        //async loading
                        if(loadAsync)
                        {
                            yield return StartCoroutine(AssetBundleManager.Instance.LoadAssetBundleAsync(relativePath + bundleObj.url, OnAssetLoaded));
                        }
                        else
                        {
                            OnAssetLoaded(AssetBundleManager.Instance.LoadAssetBundle(relativePath + bundleObj.url));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Callback function called when the asset has loaded
        /// </summary>
        /// <param name="bundle"></param>
        protected virtual void OnAssetLoaded(AssetBundle bundle)
        {
            //if scene
            if(bundle.GetAllScenePaths().Length > 0)
            {
                string[] scenePath = bundle.GetAllScenePaths();
                StartCoroutine(LoadScene(bundle, scenePath[0]));
                return;
            }

            //if prefab
            foreach (string str in bundle.GetAllAssetNames())
            {
                string[] fileSplit = str.Split('/');
                string file = fileSplit[fileSplit.Length - 1].Replace(GetExtension(str), "");

                if(str.Contains(".prefab"))
                {
                    GameObject asset = bundle.LoadAsset<GameObject>(file);
                    GameObject go = Instantiate(asset);
                    go.transform.SetParent(transform);
                }
                else
                {
                    Debug.Log("cannot create asset bundle");
                }
            }
        }

        /// <summary>
        /// Called to load scene async then unload
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="scene"></param>
        /// <returns></returns>
        private IEnumerator LoadScene(AssetBundle bundle, string scene)
        {
            AsyncOperation async = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            yield return async;

            bundle.Unload(false);
        }

        /// <summary>
        /// Called to return the extension of the asset
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        protected string GetExtension(string source)
        {
            int n = source.Length - 1;
            string extension = "";

            for (int i = n; i > 0; i--)
            {
                if (source[i].Equals('.'))
                {
                    extension += source[i];
                    break;
                }
                else
                {
                    extension += source[i];
                }
            }

            char[] output = extension.ToCharArray();
            System.Array.Reverse(output);

            return new string(output);
        }

        [System.Serializable]
        protected class AssetBundleObject
        {
            public string id;
            public string url;
            public AssetBundleURLType urlType = AssetBundleURLType.StreamingAsset;
            // public AssetBundleLoadType load = AssetBundleLoadType.All;
            // public string[] assetNames;
        }

        [System.Serializable]
        protected enum AssetBundleLoadType { All, Group, Single }

        [System.Serializable]
        protected enum AssetBundleURLType { StreamingAsset, Direct }

#if UNITY_EDITOR
        [CustomEditor(typeof(LoadAssetBundles), true)]
        public class LoadAssetBundles_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("assetBundles"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("loadAsync"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LoadAllOnStart"), true);

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}
