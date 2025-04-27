using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace BrandLab360
{
    //this will need to be able to display & select/delete current items 

    //also another tab to create an object from a library of assets

    public class FloorplanPanel : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField]
        private GameObject libraryUploadButton;

        [Header("Containers")]
        [SerializeField]
        private GameObject itemsContainer;

        [SerializeField]
        private GameObject libraryContainer;

        [Header("Entries")]
        [SerializeField]
        private GameObject itemEntry;

        [SerializeField]
        private GameObject libraryEntry;

        private List<GameObject> m_itemsCreated = new List<GameObject>();
        private List<GameObject> m_libraryCreated = new List<GameObject>();
        private Coroutine m_itemsProcess;
        private Coroutine m_libraryProcess;

        public void Close()
        {
            HUDManager.Instance.GetMenuItem("Toggle_Floorplan").GetComponent<UnityEngine.UI.Toggle>().isOn = false;
        }

        private void Start()
        {
            StartCoroutine(WaitFrame());
        }

        private IEnumerator WaitFrame()
        {
            yield return new WaitForEndOfFrame();

            Toggle[] togs = GetComponentsInChildren<Toggle>();

            for (int i = 0; i < togs.Length; i++)
            {
                if (togs[i].transform.parent.name != "Layout_Tabs") continue;

                if (togs[i].name.Contains("Items"))
                {
                    togs[i].isOn = true;
                    break;
                }
            }
        }

        public void ToggleItems(bool show)
        {
            if(show)
            {
                m_itemsProcess = StartCoroutine(ProcessItems());
            }
            else
            {
                if(m_itemsProcess != null)
                {
                    StopCoroutine(m_itemsProcess);
                }

                m_itemsProcess = null;

                for(int i =  0; i < m_itemsCreated.Count; i++)
                {
                    Destroy(m_itemsCreated[i]);
                }

                m_itemsCreated.Clear();
            }
        }

        public void ToggleLibrary(bool show)
        {
            if (show)
            {
                m_libraryProcess = StartCoroutine(ProcessLibrary());
                libraryUploadButton.SetActive(true);
            }
            else
            {
                libraryUploadButton.SetActive(false);

                if (m_libraryProcess != null)
                {
                    StopCoroutine(m_libraryProcess);
                }

                m_libraryProcess = null;

                for (int i = 0; i < m_libraryCreated.Count; i++)
                {
                    Destroy(m_libraryCreated[i]);
                }

                m_libraryCreated.Clear();
            }
        }

        public void UploadGLB()
        {
            //this will send a web client request to open GLB

#if UNITY_EDITOR
            ModelAPI.GLBUploadResponse glbResponse = new ModelAPI.GLBUploadResponse();
            glbResponse.ModelFileUrl = "https://api-staging.brandlab360.co.uk/apps/astronaut-pose/source/anstronaut%20animation.glb";

            UploadGLBResponce(JsonUtility.ToJson(glbResponse));
#else
            WebclientManager.WebClientListener += UploadGLBResponce;
            ModelAPI.GLBUploadRequest glbRequest = new ModelAPI.GLBUploadRequest();
            glbRequest.project = AppManager.Instance.Settings.projectSettings.ProjectID;
            glbRequest.collection = "FloorplanAssets";
            WebclientManager.Instance.Send(JsonUtility.ToJson(glbRequest));
#endif
        }

        private void UploadGLBResponce(string obj)
        {
            ModelAPI.GLBUploadResponse responce = JsonUtility.FromJson<ModelAPI.GLBUploadResponse>(obj).OrDefaultWhen(x => x.ModelFileUrl == null);

            if (responce != null)
            {
                WebclientManager.WebClientListener -= UploadGLBResponce;

                ModelAPI.ModelJson model = new ModelAPI.ModelJson();
                model.project = CoreManager.Instance.ProjectID;
                model.collection = "FloorplanAssets";
                model.url = responce.ModelFileUrl;
                model.filename = CoreUtilities.GetFilename(model.url);

                ModelAPI.Instance.PostModel(model, OnModelAPIUploadCallback);
            }
        }

        private void OnModelAPIUploadCallback(bool success, ModelAPI.ModelJson model)
        {
            if(success)
            {
                FloorplanManager.Instance.AddGLBModels(new List<ModelAPI.ModelJson>() { model });

                FloorplanManager.FloorplanItem item = new FloorplanManager.FloorplanItem();
                item.prefab = model.url;
                item.item = "_" + UniqueIDManager.Instance.NewID();
                item.project = CoreManager.Instance.ProjectID;
                item.scale = 1.0f;
                item.rot = 0.0f;

                Vector3 pos = PlayerManager.Instance.GetLocalPlayer().TransformObject.position + Vector3.forward;

                item.pos_x = pos.x;
                item.pos_y = pos.y;
                item.pos_z = pos.z;

                FloorplanAPI.Instance.AddFloorplanItem(item);
            }
        }

        public void OnLibraryAdded(ModelAPI.ModelJson model)
        {
            if (libraryContainer.activeInHierarchy)
            {
                GameObject go = Instantiate(libraryEntry, Vector3.zero, Quaternion.identity, libraryContainer.transform);
                go.transform.localScale = Vector3.one;
                go.name = "Entry_LibraryItem_" + model.id + "_"+ model.filename;
                go.GetComponentInChildren<FloorplanEntry>(true).Set("", FloorplanManager.ResourceImageType.Resource, model.filename, model.url);
                go.SetActive(true);

                m_libraryCreated.Add(go);
            }
        }

        public void OnItemAdded(FloorplanManager.FloorplanItem item)
        {
            if (itemsContainer.activeInHierarchy)
            {
                if(item.prefab.Contains(".glb"))
                {
                    GameObject go = Instantiate(itemEntry, Vector3.zero, Quaternion.identity, itemsContainer.transform);
                    go.transform.localScale = Vector3.one;
                    go.name = "Entry_FloorplanItem_" + CoreUtilities.GetFilename(item.prefab);
                    go.GetComponentInChildren<FloorplanEntry>(true).Set("", FloorplanManager.ResourceImageType.Resource, CoreUtilities.GetFilename(item.prefab), item.item);
                    go.SetActive(true);

                    m_itemsCreated.Add(go);
                }
                else
                {
                    FloorplanManager.FloorplanPrefab prefab = AppManager.Instance.Instances.floorplanPrefabs.FirstOrDefault(x => x.id.Equals(item.prefab));
                    CreateItem(prefab, item.item);
                }
            }
        }

        public void OnItemDelete(string item)
        {
            if(itemsContainer.activeInHierarchy)
            {
                int remove = -1;
                bool removeItem = false;

                for (int i = 0; i < m_itemsCreated.Count; i++)
                {
                    if(m_itemsCreated[i].GetComponent<FloorplanEntry>().ItemID.Equals(item))
                    {
                        removeItem = true;
                        remove = i;
                        Destroy(m_itemsCreated[i]);
                        break;
                    }
                }

                if(removeItem)
                {
                    m_itemsCreated.RemoveAt(remove);
                }
            }
        }

        private IEnumerator ProcessItems()
        {
            WaitForEndOfFrame wait = new WaitForEndOfFrame();

            foreach (FloorplanManager.FloorplanItem item in FloorplanManager.Instance.GetItems())
            {
                FloorplanManager.FloorplanPrefab prefab = AppManager.Instance.Instances.floorplanPrefabs.FirstOrDefault(x => x.id.Equals(item.prefab));

                if(item != null)
                {
                    if(prefab == null)
                    {
                        if (item.prefab.Contains(".glb"))
                        {
                            GameObject go = Instantiate(itemEntry, Vector3.zero, Quaternion.identity, itemsContainer.transform);
                            go.transform.localScale = Vector3.one;
                            go.name = "Entry_FloorplanItem_" + CoreUtilities.GetFilename(item.prefab);
                            go.GetComponentInChildren<FloorplanEntry>(true).Set("", FloorplanManager.ResourceImageType.Resource, CoreUtilities.GetFilename(item.prefab), item.item);
                            go.SetActive(true);

                            m_itemsCreated.Add(go);
                        }
                    }
                    else
                    {
                        CreateItem(prefab, item.item);
                    }
                }

                yield return wait;
            }
        }

        private void CreateItem(FloorplanManager.FloorplanPrefab prefab, string item)
        {
            if (prefab != null)
            {
                GameObject go = Instantiate(itemEntry, Vector3.zero, Quaternion.identity, itemsContainer.transform);
                go.transform.localScale = Vector3.one;
                go.name = "Entry_FloorplanItem_" + prefab.id + "_" + item;
                go.GetComponentInChildren<FloorplanEntry>(true).Set(prefab.imageURL, prefab.imageType, prefab.id, item);
                go.SetActive(true);

                m_itemsCreated.Add(go);
            }
        }

        private IEnumerator ProcessLibrary()
        {
            WaitForEndOfFrame wait = new WaitForEndOfFrame();

            foreach (FloorplanManager.FloorplanPrefab prefab in AppManager.Instance.Instances.floorplanPrefabs)
            {
                GameObject go = Instantiate(libraryEntry, Vector3.zero, Quaternion.identity, libraryContainer.transform);
                go.transform.localScale = Vector3.one;
                go.name = "Entry_LibraryItem_" + prefab.id;
                go.GetComponentInChildren<FloorplanEntry>(true).Set(prefab.imageURL, prefab.imageType, prefab.id);
                go.SetActive(true);

                m_libraryCreated.Add(go);

                yield return wait;
            }

            foreach (KeyValuePair<int, ModelAPI.ModelJson> glb in FloorplanManager.Instance.GLBModels)
            {
                GameObject go = Instantiate(libraryEntry, Vector3.zero, Quaternion.identity, libraryContainer.transform);
                go.transform.localScale = Vector3.one;
                go.name = "Entry_LibraryItem_" + glb.Key + "_" + glb.Value.filename;
                go.GetComponentInChildren<FloorplanEntry>(true).Set("", FloorplanManager.ResourceImageType.Resource, glb.Value.filename, glb.Value.url);
                go.SetActive(true);

                m_libraryCreated.Add(go);

                yield return wait;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(FloorplanPanel), true)]
        public class FloorplanPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("libraryUploadButton"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("itemsContainer"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("libraryContainer"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("itemEntry"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("libraryEntry"), true);

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
