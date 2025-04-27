using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrandLab360
{
    [System.Serializable]
    public class AppAssets
    {
        [SerializeField]
        private List<AppAsset> assets = new List<AppAsset>();

        public void Dispose()
        {
            assets.Clear();
        }

        public void Add(string id, Object obj)
        {
            if(Get(id) == null)
            {
                AppAsset ass = new AppAsset();
                ass.name = id;
                ass.asset = obj;

                assets.Add(ass);
            }
            else
            {
                Debug.Log("Asset already exists! [" + id + "]");
            }
        }

        public Object Get(string id)
        {
            for(int i = 0; i < assets.Count; i++)
            {
                if(assets[i].name.Equals(id))
                {
                    return assets[i].asset;
                }
            }

            return null;
        }

        [System.Serializable]
        private class AppAsset
        {
            public string name;
            public Object asset;
        }
    }
}
