using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class FloorplanEntry : MonoBehaviour
    {
        [SerializeField]
        private RawImage tex;

        [SerializeField]
        private TextMeshProUGUI id;

        [SerializeField]
        private AspectRatioFitter aspect;

        [SerializeField]
        private GameObject binButton;

        private string m_img;
        private FloorplanManager.ResourceImageType m_loadType;
        private string m_item;
        private FloorplanGO go;

        public string ItemID
        {
            get
            {
                return m_item;
            }
        }

        public void Set(string img, FloorplanManager.ResourceImageType loadType, string txt, string id = "")
        {
            m_item = id;
            this.id.text = txt;
            m_img = img;
            m_loadType = loadType;

            if(string.IsNullOrEmpty(m_img))
            {
                m_img = "Floorplan/Textures/FBX";
            }

            if (!m_item.Contains(".glb"))
            {
                GameObject temp = FloorplanManager.Instance.GetFloorplanItemGO(ItemID);

                if (temp != null)
                {
                    go = FloorplanManager.Instance.GetFloorplanItemGO(ItemID).GetComponent<FloorplanGO>();
                }
            }

            if(binButton != null)
            {
                binButton.SetActive(false);
            }
        }

        private void Start()
        {
            //need to load image
            if(!string.IsNullOrEmpty(m_img))
            {
                StartCoroutine(ProcessLoad());
            }
        }

        private void Update()
        {
            //only show delete button if player is owner of config object
            if(go != null)
            {
                if(binButton != null)
                {
                    if(go.ConfiguratorRef.IsOwner)
                    {
                        if(!binButton.activeInHierarchy)
                        {
                            binButton.SetActive(true);
                        }
                    }
                    else
                    {
                        if (binButton.activeInHierarchy)
                        {
                            binButton.SetActive(false);
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();

            if(m_loadType.Equals(FloorplanManager.ResourceImageType.URL))
            {
                Destroy(tex.texture);
            }

            tex.texture = null;
        }

        public void Select()
        {
            Configurator config = FloorplanManager.Instance.GetFloorplanItemGO(m_item).GetComponent<FloorplanGO>().ConfiguratorRef;

            //need to deactivte the Flooplan panel

            if (!config.IsActive)
            {
                if (CoreManager.Instance.projectSettings.configTagMode.Equals(TagMode._2D))
                {
                    FloorplanManager.Instance.GetFloorplanItemGO(m_item).GetComponent<WorldContentUpload>().OnClick();
                }
                else
                {
                    //ensure a toggle is on
                    config.GetComponentsInChildren<ConfigureToggle>()[0].GetComponent<Toggle>().isOn = true;
                }
            }
        }

        public void Add()
        {
            FloorplanManager.FloorplanItem item = new FloorplanManager.FloorplanItem();
            item.prefab = (m_item.Contains(".glb")) ? m_item : id.text;
            item.item = "_" + UniqueIDManager.Instance.NewID();
            item.project = CoreManager.Instance.ProjectID;
            item.scale = 0.0f;
            item.rot = 0.0f;

            Vector3 pos = PlayerManager.Instance.GetLocalPlayer().TransformObject.position + Vector3.forward;

            item.pos_x = pos.x;
            item.pos_y = pos.y;
            item.pos_z = pos.z;

            FloorplanAPI.Instance.AddFloorplanItem(item);
        }

        public void Bin()
        {
            //need to make this person the owner of the object
            GameObject go = FloorplanManager.Instance.GetFloorplanItemGO(ItemID);
            //go.GetComponent<FloorplanGO>().AssignOwner();
            FloorplanAPI.Instance.DeleteFloorplanItem(FloorplanManager.Instance.GetFloorplanItem(m_item));
        }

        private IEnumerator ProcessLoad()
        {
            if (m_loadType.Equals(FloorplanManager.ResourceImageType.Resource))
            {
                if (m_img[0].Equals('/'))
                {
                    string temp = m_img.Remove(0, 1);
                    m_img = temp;
                }

                tex.texture = Resources.Load<Texture>(m_img);

                if (tex.texture != null)
                {
                    tex.SetNativeSize();

                    float texWidth = tex.texture.width;
                    float texHeight = tex.texture.height;
                    float aRatio = texWidth / texHeight;
                    aspect.aspectRatio = aRatio;
                }
            }
            else
            {
                UnityWebRequest request = UnityWebRequestTexture.GetTexture(m_img, true);

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(request.error))
                {
                    tex.texture = DownloadHandlerTexture.GetContent(request);
                    tex.SetNativeSize();
                }

                request.Dispose();

                if (tex.texture != null)
                {
                    float texWidth = tex.texture.width;
                    float texHeight = tex.texture.height;
                    float rRatio = texWidth / texHeight;
                    aspect.aspectRatio = rRatio;
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(FloorplanEntry), true)]
        public class FloorplanEntry_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tex"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("id"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("aspect"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("binButton"), true);

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
