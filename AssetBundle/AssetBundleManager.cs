using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class AssetBundleManager : Singleton<AssetBundleManager>
    {
        public static AssetBundleManager Instance
        {
            get
            {
                return ((AssetBundleManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Called to return an local Asset Bundle
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public AssetBundle LoadAssetBundle(string url)
        {
            var loadedAssetBundle = AssetBundle.LoadFromFile(url);

            if (loadedAssetBundle == null)
            {
                Debug.Log("Failed to load AssetBundle!");
                return null;
            }

            return loadedAssetBundle;
        }

        /// <summary>
        /// Called to return a local asset bundle object
        /// </summary>
        /// <param name="url"></param>
        /// <param name="asset"></param>
        /// <returns></returns>
        public Object LoadAssetBundleAsync(string url, string asset)
        {
            AssetBundle bundle = LoadAssetBundle(url);

            if (bundle != null)
            {
                return bundle.LoadAsset(asset);
            }

            return null;
        }

        /// <summary>
        /// Called to load async local Asset Bundle
        /// </summary>
        /// <param name="url"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public IEnumerator LoadAssetBundleAsync(string url, System.Action<AssetBundle> callback)
        {
            AssetBundleCreateRequest asyncBundleRequest = AssetBundle.LoadFromFileAsync(url);
            yield return asyncBundleRequest;

            if (asyncBundleRequest.assetBundle == null)
            {
                Debug.Log("Failed to load AssetBundle!");
                yield break;
            }

            if(callback != null)
            {
                callback.Invoke(asyncBundleRequest.assetBundle);
            }

            if (asyncBundleRequest.assetBundle != null && !asyncBundleRequest.assetBundle.isStreamedSceneAssetBundle)
            {
                asyncBundleRequest.assetBundle.Unload(false);
            }

            //fail safe to ensure that any unused memeory is cleared
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Called to async load local asset bundle and return object
        /// </summary>
        /// <param name="url"></param>
        /// <param name="asset"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public IEnumerator LoadAssetBundleAsync(string url, string asset, System.Action<Object> callback)
        {
            AssetBundleCreateRequest asyncBundleRequest = AssetBundle.LoadFromFileAsync(url);
            yield return asyncBundleRequest;

            if (asyncBundleRequest.assetBundle == null)
            {
                Debug.Log("Failed to load AssetBundle!");
                yield break;
            }

            AssetBundleRequest assetRequest = asyncBundleRequest.assetBundle.LoadAssetAsync(asset);
            yield return assetRequest;

            if (callback != null)
            {
                callback.Invoke(assetRequest.asset);
            }

            if(asyncBundleRequest.assetBundle != null && !asyncBundleRequest.assetBundle.isStreamedSceneAssetBundle)
            {
                asyncBundleRequest.assetBundle.Unload(false);
            }

            //fail safe to ensure that any unused memeory is cleared
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Called to load Web asset bundle
        /// </summary>
        /// <param name="url"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public IEnumerator LoadWebAssetBundle(string url, System.Action<AssetBundle> callback)
        {
            using(UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(url))
            {
                yield return request.SendWebRequest();

                if(request.result != UnityWebRequest.Result.Success)
                {
                    if(callback != null)
                    {
                        Debug.Log("Asset bundle web request failed!");
                        callback.Invoke(null);
                    }
                }
                else
                {
                    if (callback != null)
                    {
                        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);

                        if(bundle == null)
                        {
                            Debug.Log("Failed to load AssetBundle!");
                            callback.Invoke(null);
                        }
                        else
                        {
                            callback(bundle);

                            if(bundle != null && !bundle.isStreamedSceneAssetBundle)
                            {
                                bundle.Unload(false);
                            }
                        }
                    }
                }
            }

            //fail safe to ensure that any unused memeory is cleared
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Called to unload asset bundle
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="unloadAllLoadedObjects"></param>
        public void UnloadAssetBundle(AssetBundle bundle, bool unloadAllLoadedObjects = false)
        {
            if(bundle != null)
            {
                bundle.Unload(unloadAllLoadedObjects);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AssetBundleManager), true)]
        public class AssetBundleManager_Editor : BaseInspectorEditor
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
