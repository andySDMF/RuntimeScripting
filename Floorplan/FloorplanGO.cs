using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class FloorplanGO : MonoBehaviour
    {
        private GizmosTool m_gizmo;

        public bool IsGLB
        {
            get;
            set;
        }

        public string Owner
        {
            get;
            set;
        }

        public Configurator ConfiguratorRef
        {
            get;
            set;
        }

        public string ItemID
        {
            get;
            set;
        }

        private float glbSyncTimer = 0.0f;

        private void Awake()
        {
            if (!AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Offline))
            {
                if (m_gizmo == null)
                {
                    m_gizmo = gameObject.AddComponent<GizmosTool>();
                }
            }
        }

        private void Start()
        {
            //need to add the navmesh obsticle script to the object
            gameObject.AddComponent<NavMeshObstacle>();
            ConfiguratorManager.instance.OnRTEButtonEvent += SyncAPI;

            StartCoroutine(AssignConfig());
        }

        private void Update()
        {
            //need to send RPC
            if(!AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Offline))
            {
                //send RPC to others to update pos/rot/sca if local player controlling
                if(!string.IsNullOrEmpty(Owner))
                {
                    if(Owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                    {
                        if(ConfiguratorManager.instance.ActiveRTEObject != null && ConfiguratorManager.instance.ActiveRTEObject.Equals(this.gameObject))
                        {
                            if (glbSyncTimer < 1.0f)
                            {
                                glbSyncTimer += Time.deltaTime;
                            }
                            else
                            {
                                UpdateTransform(transform.position, new Vector3(0.0f, transform.eulerAngles.y, 0.0f), transform.localScale);
                                glbSyncTimer = 0.0f;
                            }
                        }
                    }
                }
            }
        }

        public void UpdateTransform(Vector3 pos, Vector3 rot, Vector3 scale)
        {
            transform.position = pos;
            transform.localScale = (scale.x <= 0f) ? Vector3.one : scale;
            transform.eulerAngles = rot;
        }

        public void SyncAPI(GameObject go)
        {
            if (go == gameObject)
            {
                FloorplanManager.FloorplanItem item = FloorplanManager.Instance.GetFloorplanItem(ItemID);

                if(item != null)
                {
                    item.pos_x = go.transform.position.x;
                    item.pos_y = go.transform.position.y;
                    item.pos_z = go.transform.position.z;
                    item.rot = go.transform.eulerAngles.y;
                    item.scale = go.transform.localScale.x;
                }

                FloorplanAPI.Instance.UpdateFloorplanItem(item);
            }
        }

        public void AssignOwner()
        {
            Owner = PlayerManager.Instance.GetLocalPlayer().ID;
        }

        private IEnumerator AssignConfig()
        {
            //wait for bit so that manager can recieve RPC;
            yield return new WaitForSeconds(0.5f);

            Vector3 pos = Vector3.zero;
            Vector3 scale = Vector3.one;
            Vector3 rot = Vector3.zero;

            //need to get item somehow from manager
            FloorplanManager.FloorplanItem item = FloorplanManager.Instance.GetFloorplanItem(ItemID);

            if (item != null || IsGLB)
            {
                pos = new Vector3(item.pos_x, item.pos_y, item.pos_z);
                scale = new Vector3(item.scale, item.scale, item.scale);
                rot = new Vector3(0.0f, item.rot, 0.0f);
                UpdateTransform(pos, rot, scale);
            }
            else
            {
                while (item == null)
                {
                    //send rpc to master server for item ID
                 //   pView.rp("RPC_RequestID", (int)MMOManager.RpcTarget.MasterClient, pView.ViewID);
                    yield return new WaitForSeconds(1.0f);
                    item = FloorplanManager.Instance.GetFloorplanItem(ItemID);
                }

                pos = new Vector3(item.pos_x, item.pos_y, item.pos_z);
                scale = new Vector3(item.scale, item.scale, item.scale);
                rot = new Vector3(0.0f, item.rot, 0.0f);
                UpdateTransform(pos, rot, scale);

                FloorplanManager.Instance.PostFloorplanItemGO(item.item, gameObject);
            }

            if (item != null)
            {
                //need to check if it has a configurator script
                GameObject configGO = CreateBaseObject(item.prefab + "_" + item.item);
                Configurator config = configGO.AddComponent<Configurator>();
                config.Type = ConfiguratorManager.ConfiguratorType.Transform;
                config.Target = gameObject;
                configGO.transform.SetParent(gameObject.transform, true);
                configGO.transform.eulerAngles = gameObject.transform.eulerAngles;

                //need to get bounds of object
                BoxCollider col = GetComponent<BoxCollider>();

                if (col != null)
                {
                    float height = col.bounds.extents.y;
                    configGO.transform.localPosition = new Vector3(0, height + 0.5f, 0);
                }
                else
                {
                    Bounds bounds = new Bounds();
                    NavMeshObstacle nmo = GetComponent<NavMeshObstacle>();
                    int count = 0;
                    float scaleMultiplier = 1 / transform.localScale.x;

                    foreach (Renderer rend in GetComponentsInChildren<Renderer>(true))
                    {
                        bounds.center += rend.bounds.center * scaleMultiplier;
                        bounds.size += rend.bounds.size * scaleMultiplier;
                        count++;
                    }

                    nmo.center = new Vector3(0, (bounds.center.y / count) / 2, 0);
                    nmo.size = new Vector3(bounds.size.x / count, bounds.center.y / count, bounds.size.z/ count);

                    configGO.transform.localPosition = new Vector3(0, (nmo.center.y + (nmo.size.y / 2)) + 0.5f, 0);
                }

                yield return new WaitForEndOfFrame();

                config.OverrideForFloorplanItemSettings(true, item.item);
                ConfiguratorRef = config;

                if (CoreManager.Instance.projectSettings.configTagMode.Equals(TagMode._2D))
                {
                    config.InitialiseFor2DSystem();
                }
            }
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

#if UNITY_EDITOR
        [CustomEditor(typeof(FloorplanGO), true)]
        public class FloorplanGO_Editor : BaseInspectorEditor
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
