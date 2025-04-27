using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class FloorplanManager : Singleton<FloorplanManager>
    {
        public static FloorplanManager Instance
        {
            get
            {
                return ((FloorplanManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private Dictionary<FloorplanItem, GameObject> m_createdItems = new Dictionary<FloorplanItem, GameObject>();
        private Dictionary<int, ModelAPI.ModelJson> m_models = new Dictionary<int, ModelAPI.ModelJson>();

        public Dictionary<int, ModelAPI.ModelJson> GLBModels
        {
            get
            {
                return m_models;
            }
        }

        public void AddGLBModels(List<ModelAPI.ModelJson> models)
        {
            models.ForEach(x => m_models.Add(x.id, x));

            //check if the floorplan panel is active
            if (HUDManager.Instance.GetHUDScreenObject("FLOORPLAN_SCREEN").activeInHierarchy)
            {
                for(int i = 0; i < models.Count; i++)
                {
                    HUDManager.Instance.GetHUDScreenObject("FLOORPLAN_SCREEN").GetComponent<FloorplanPanel>().OnLibraryAdded(models[i]);
                }
            }
        }

        public GameObject GetFloorplanItemGO(string item)
        {
            if (!string.IsNullOrEmpty(item))
            {
                foreach (KeyValuePair<FloorplanItem, GameObject> i in m_createdItems)
                {
                    if (i.Key.item.Equals(item))
                    {
                        return i.Value;
                    }
                }
            }

            return null;
        }

        public FloorplanItem GetFloorplanItem(string item)
        {
            if(!string.IsNullOrEmpty(item))
            {
                foreach (KeyValuePair<FloorplanItem, GameObject> i in m_createdItems)
                {
                    if (i.Key.item.Equals(item))
                    {
                        return i.Key;
                    }
                }
            }

            return null;
        }

        public void PostFloorplanItemGO(string item, GameObject go)
        {
            if (!string.IsNullOrEmpty(item))
            {
                foreach (KeyValuePair<FloorplanItem, GameObject> i in m_createdItems)
                {
                    if (i.Key.item.Equals(item))
                    {
                        if(i.Value == null)
                        {
                            m_createdItems[i.Key] = go;
                            break;
                        }
                    }
                }
            }
        }

        public List<FloorplanItem> GetItems()
        {
            return m_createdItems.Keys.ToList();
        }

        public void InsertFloorplanItem(FloorplanItem item)
        {
            if(item != null)
            {
#if UNITY_EDITOR
                if (!gameObject.scene.name.Contains("SampleSceneHDRP")) return;
#endif

                //best to do coroutine to stop any lag if multiple items being created at once
                StartCoroutine(ProcessInsert(item));
            }
        }

        public void UpdateFloorplanItem(string item, Vector3 pos, float rot, float scale)
        {
            //best to do coroutine to stop any lag if multiple items being updated at once
            StartCoroutine(ProcessUpdate(GetFloorplanItem(item), pos, rot, scale));
        }

        public void RemoveFloorplanItem(string item)
        {
            //best to do coroutine to stop any lag if multiple items being deleted at once
            StartCoroutine(ProcessDelete(GetFloorplanItem(item)));
        }

        private IEnumerator ProcessInsert(FloorplanItem item, bool isRemote = true)
        {
            //check to see if parent holder exists
            GameObject container = GameObject.Find("_FLOORPLANITEMS");

            if(container == null)
            {
                container = new GameObject();
                container.name = "_FLOORPLANITEMS";
                container.transform.position = Vector3.zero;
                container.transform.localScale = Vector3.one;
                container.transform.eulerAngles = Vector3.zero;
            }

            //need to check if it is a GLB
            if(item.prefab.Contains(".glb"))
            {
                LoadGLTF(item);
                yield break;
            }

            //need to instantiate new GO for item
            FloorplanPrefab prefab = AppManager.Instance.Instances.floorplanPrefabs.FirstOrDefault(x => x.id.Equals(item.prefab));

            if(prefab != null)
            {
                GameObject go = null;
                Vector3 pos = new Vector3(item.pos_x, item.pos_y, item.pos_z);
                Vector3 scale = new Vector3(item.scale, item.scale, item.scale);
                Vector3 rot = new Vector3(0.0f, item.rot, 0.0f);

                bool itemCreated = false;

                switch (prefab.type)
                {
                    case ResourceType.AssetBundle:
                        AssetBundle bundle = AssetBundleManager.Instance.LoadAssetBundle(prefab.resourceURL);

                        if(bundle != null)
                        {
                            foreach (string str in bundle.GetAllAssetNames())
                            {
                                string[] fileSplit = str.Split('/');
                                string file = fileSplit[fileSplit.Length - 1].Replace(CoreUtilities.GetExtension(str), "");

                                if (str.Contains(".prefab"))
                                {
                                    GameObject asset = bundle.LoadAsset<GameObject>(file);

                                    if(asset != null)
                                    { 
                                        go = Instantiate(asset);
                                        go.transform.SetParent(container.transform);
                                        go.transform.position = pos;
                                        itemCreated = true;
                                    }
                                }
                                else
                                {
                                    Debug.Log("cannot create asset bundle");
                                }
                            }
                        }
                        else
                        {
                            Debug.Log("AssetBundle [" + prefab.id + "] does not exists");
                        }
                        break;
                    default:
                        string path = prefab.resourceURL;

                        if(prefab.resourceURL[0].Equals('/'))
                        {
                            path = prefab.resourceURL.Remove(0, 1);
                        }

                        UnityEngine.Object obj = Resources.Load(path);

                        if(obj != null)
                        {
                            go = (GameObject)Instantiate(obj, pos, Quaternion.identity, container.transform);
                            itemCreated = true;
                        }
                        else
                        {
                            Debug.Log("Resource [" + prefab.id + "] does not exists");
                        }
                        
                        break;

                }

                if (itemCreated)
                {
                    if(item.scale <= 0.0f)
                    {
                        item.scale = go.transform.localScale.x;
                        FloorplanAPI.Instance.UpdateFloorplanItem(item);
                    }

                    go.GetComponent<FloorplanGO>().ItemID = item.item;
                    m_createdItems.Add(item, go);
                }
                else
                {
                    m_createdItems.Add(item, null);
                }

                //check if the floorplan panel is active
                if (HUDManager.Instance.GetHUDScreenObject("FLOORPLAN_SCREEN").activeInHierarchy)
                {
                    HUDManager.Instance.GetHUDScreenObject("FLOORPLAN_SCREEN").GetComponent<FloorplanPanel>().OnItemAdded(item);
                }
            }

            yield return null;
        }

        private async void LoadGLTF(FloorplanItem item)
        {
            //check to see if parent holder exists
            GameObject container = GameObject.Find("_FLOORPLANITEMS");
            GameObject GO = await GLTFManager.Instance.LoadAsync(container.transform, item.prefab);

            if (GO == null) return;

            GO.SetActive(false);
            GO.name = CoreUtilities.GetFilename(item.prefab);

            //need to get the original scale
            if(item.scale <= 0.0f)
            {
                item.scale = GO.transform.localScale.x;
                FloorplanAPI.Instance.UpdateFloorplanItem(item);
            }

            FloorplanGO fGO = GO.AddComponent<FloorplanGO>();
            fGO.ItemID = item.item;
            fGO.IsGLB = true;
            m_createdItems.Add(item, GO);
            GO.SetActive(true);

            if(GO.GetComponent<Animation>() != null)
            {
                await System.Threading.Tasks.Task.Delay(1000);
                GO.GetComponent<Animation>().Play();
            }
        }

        private IEnumerator ProcessUpdate(FloorplanItem item, Vector3 pos, float rot, float scale)
        {
            if (item != null)
            {
                item.pos_x = pos.x;
                item.pos_y = pos.y;
                item.pos_z = pos.z;
                item.rot = rot;
                item.scale = scale;

                Vector3 scaleV = new Vector3(item.scale, item.scale, item.scale);
                Vector3 rotV = new Vector3(0.0f, item.rot, 0.0f);

                //need to update go with new values
                GameObject go = GetFloorplanItemGO(item.item);

                if(go != null)
                {
                    FloorplanGO fpScript = go.GetComponentInChildren<FloorplanGO>(true);

                    if(fpScript != null)
                    {
                        fpScript.UpdateTransform(pos, rotV, scaleV);
                    }
                }
            }

            yield return null;
        }

        private IEnumerator ProcessDelete(FloorplanItem item, bool isRemote = true)
        {
            if (item != null)
            {
                GameObject go = GetFloorplanItemGO(item.item);

                if (ConfiguratorManager.instance.ActiveRTEObject != null && ConfiguratorManager.instance.ActiveRTEObject.Equals(go))
                {
                    ConfiguratorManager.instance.SetRTEObject(null);
                }

                if (item.prefab.Contains(".glb"))
                {
                    Destroy(go);
                }
                else
                {
                    if (go != null)
                    {
                        Destroy(go);
                    }
                }

                m_createdItems.Remove(item);

                //check if the floorplan panel is active
                if(HUDManager.Instance.GetHUDScreenObject("FLOORPLAN_SCREEN").activeInHierarchy)
                {
                    HUDManager.Instance.GetHUDScreenObject("FLOORPLAN_SCREEN").GetComponent<FloorplanPanel>().OnItemDelete(item.item);
                }
            }

            yield return null;
        }

        private GameObject CreateBaseObject(string id)
        {
            RectTransform rectT = null;
            Canvas goCanvas = new GameObject().AddComponent<Canvas>();
            CanvasScaler goLockCanvasScaler = goCanvas.gameObject.AddComponent<CanvasScaler>();
            GraphicRaycaster goLockRaycaster = goCanvas.gameObject.AddComponent<GraphicRaycaster>();

            goCanvas.name = "Config_" + id;
            rectT = goCanvas.GetComponent<RectTransform>();
            rectT.anchorMin = Vector2.zero;
            rectT.anchorMax = Vector2.zero;
            rectT.localScale = new Vector3(0.0004526008f, 0.0004526008f, 0.0004526008f);
            goLockRaycaster.ignoreReversedGraphics = false;

            goCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Normal;
            goCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            goCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Tangent;

            return goCanvas.gameObject;
        }

        [System.Serializable]
        public class FloorplanItem
        {
            public int id;
            public string project;
            public string item;
            public string prefab;
            public float pos_x;
            public float pos_y;
            public float pos_z;
            public float rot;
            public float scale;
        }

        [System.Serializable]
        public class FloorplanPrefab
        {
            public string id;
            public ResourceType type = ResourceType.Resource;
            public string resourceURL;

            public ResourceImageType imageType = ResourceImageType.Resource;
            public string imageURL;
        }

        [System.Serializable]
        public enum ResourceType { Resource, AssetBundle }

        [System.Serializable]
        public enum ResourceImageType { Resource, URL }

#if UNITY_EDITOR
        [CustomEditor(typeof(FloorplanManager), true)]
        public class FloorplanManager_Editor : BaseInspectorEditor
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
