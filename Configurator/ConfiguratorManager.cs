using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// Configurator Manager to handle configurators and can handle syncing their states
    /// </summary>
    public class ConfiguratorManager : MonoBehaviour, IRaycaster
    {
        private Configurator configureUIControl;

        [Header("Interaction")]
        private float interactionDistance = 5;
        private bool useLocalDistance = true;
        public bool OverrideDistance { get { return useLocalDistance; } }

        public static ConfiguratorManager instance;

        [HideInInspector]
        public List<Configurator> configurators;

        private ConfigureWorldItem m_configureItem;

        private GameObject m_selected;
        private Configurator m_configSource;

        public string UserCheckKey
        {
            get
            {
                return m_userKey;
            }
        }

        public System.Action<GameObject> OnRTEButtonEvent
        {
            get;
            set;
        }

        public GameObject ActiveRTEObject
        {
            get
            {
                return m_selected;
            }
        }

        private string m_userKey = "USERTYPE";

        public float Distance
        {
            get
            {
                float distance = 5000;

                //define camera to use
                if (!MapManager.Instance.TopDownViewActive)
                {
                    if (PlayerManager.Instance.ThirdPersonCameraActive)
                    {
                        distance = interactionDistance + Vector3.Distance(PlayerManager.Instance.GetLocalPlayer().TransformObject.position, PlayerManager.Instance.GetLocalPlayer().MainCamera.transform.GetChild(0).position);
                    }
                    else
                    {
                        distance = interactionDistance;
                    }
                }
                else
                {
                    //cannot perform chair stuff in topdown view
                    return -1;
                }

                return distance;
            }
        }

        /// <summary>
        /// Handle the raycast for all configurators
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="hitObject"></param>
        public void RaycastHit(RaycastHit hit, out Transform hitObject)
        {
            m_configureItem = hit.transform.GetComponent<ConfigureWorldItem>();

            if (m_configureItem)
            {
                hitObject = m_configureItem.transform;
            }
            else
            {
                hitObject = null;
            }

            if (InputManager.Instance.GetMouseButtonUp(0))
            {
                if (m_configureItem)
                {
                    m_configureItem.OnClick();
                }
            }
        }

        /// <summary>
        /// Raycast missed event
        /// </summary>
        public void RaycastMiss()
        {
            m_configureItem = null;
        }

        /// <summary>
        /// On player moved when RTE is open
        /// </summary>
        public void PlayerMovedOnRTE()
        {
            if(configureUIControl == null)
            {
                configureUIControl = HUDManager.Instance.GetHUDControlObject("CONFIGURATOR_CONTROL").GetComponentInChildren<Configurator>(true);
            }

            if(PlayerManager.Instance.IsPlayerControllerMoving() && GizmosManager.Instance.Target != null)
            {
                m_configureItem = null;
                m_selected = null;

                //hide 2D configurator
                if (configureUIControl != null && configureUIControl.gameObject.activeInHierarchy)
                {
                    configureUIControl.transform.parent.gameObject.SetActive(false);

                    UseRTE(false);
                }
            }
        }

        /// <summary>
        /// On player moved from raycast frame
        /// </summary>
        public void OnPlayerMoved()
        {
            m_configureItem = null;
            m_selected = null;

            if (configureUIControl == null)
            {
                configureUIControl = HUDManager.Instance.GetHUDControlObject("CONFIGURATOR_CONTROL").GetComponentInChildren<Configurator>(true);
            }

            //hide 2D configurator
            if (configureUIControl != null && configureUIControl.gameObject.activeInHierarchy)
            {
                configureUIControl.transform.parent.gameObject.SetActive(false);

                UseRTE(false);
            }
        }

        private void Awake()
        {
            if (!AppManager.IsCreated) return;

            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                instance = this;
            }

            //set raycast up
            RaycastManager.OnPlayerMoved += OnPlayerMoved;
            RaycastManager.OnPointerOverUI += PlayerMovedOnRTE;
            RaycastManager.Instance.Raycasters.Add(this);

            //find all configurators
            Configurator[] all = FindObjectsByType<Configurator>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                if(all[i].GetComponentInParent<CoreManager>())
                {
                    continue;
                }

                configurators.Add(all[i]);
            }
        }

        private void Start()
        {
            PlayerControlSettings.ManagerInteraction mInteration = CoreManager.Instance.playerSettings.GetIRaycasterManager(gameObject.name);

            if (mInteration != null)
            {
                interactionDistance = mInteration.interactionDistance;
                useLocalDistance = mInteration.overrideInteraction;
                m_userKey = mInteration.userCheckKey;
            }
            else
            {
                useLocalDistance = false;
            }

            //initialise 2D UI setup if used 
            if (CoreManager.Instance.projectSettings.configTagMode.Equals(TagMode._2D))
            {
                for (int i = 0; i < configurators.Count; i++)
                {
                    configurators[i].transform.localScale = Vector3.zero;
                    configurators[i].InitialiseFor2DSystem();
                }
            }
        }

        private void OnDestroy()
        {

        }

        private void Update()
        {
            if (CoreManager.Instance.CurrentState != state.Running) return;

            if (configureUIControl == null)
            {
                configureUIControl = HUDManager.Instance.GetHUDControlObject("CONFIGURATOR_CONTROL").GetComponentInChildren<Configurator>(true);
            }

            if (configureUIControl != null && configureUIControl.gameObject.activeInHierarchy)
            {
                if (CoreManager.Instance.projectSettings.configTagMode.Equals(TagMode._2D))
                {
                    if(m_configSource != null)
                    {
                        configureUIControl.transform.position = Camera.main.WorldToScreenPoint(m_configSource.transform.position);
                    }
                }
                //check what tool is used
                if(m_configSource.Type.Equals(ConfiguratorType.Transform))
                {
                    if (GizmosManager.Instance.Target == null)
                    {
                        OnPlayerMoved();
                    }
                }

                return;
            }

            if (!CoreManager.Instance.projectSettings.configTagMode.Equals(TagMode._2D))
            {
                //check what tool is used
                if (m_configSource != null && m_configSource.Type.Equals(ConfiguratorType.Transform) && m_selected != null)
                {
                    GizmosManager.Instance.Target = m_selected.transform;
                }
            }

            if(GizmosManager.Instance.Target != null)
            {
                RaycastManager.Instance.DisplayCursor = false;

                if(InputManager.Instance.GetMouseButtonUp(0))
                {
                    if(OnRTEButtonEvent != null)
                    {
                        OnRTEButtonEvent.Invoke(GizmosManager.Instance.Target.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Called to update the 2D config palette
        /// </summary>
        /// <param name="src"></param>
        public void Set2DConfigPallette(Configurator src)
        {
            if (configureUIControl == null)
            {
                configureUIControl = HUDManager.Instance.GetHUDControlObject("CONFIGURATOR_CONTROL").GetComponentInChildren<Configurator>(true);
            }

            if (configureUIControl != null)
            {
                m_configSource = src;

                if (CoreManager.Instance.projectSettings.configTagMode.Equals(TagMode._2D))
                {
                    configureUIControl.SetUIPalletteFromSource(src);
                }
                else
                {
                    configureUIControl.SetUIDoneButton(src, true);
                }

                configureUIControl.transform.parent.gameObject.SetActive(true);
                RaycastManager.Instance.DisplayCursor = false;
            }
        }

        /// <summary>
        /// Called to network the configurators
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        public void SetNetworkConfig(string id, string data, string tran = "")
        {
            Configurator config = configurators.FirstOrDefault(x => x.ID.Equals(id));

            if(config)
            {
                if(!string.IsNullOrEmpty(tran))
                {
                    config.RegisterThis(tran);
                }

                config.Set(data, false);
            }
        }

        /// <summary>
        /// Called to open the RTE tool
        /// </summary>
        /// <param name="isOn"></param>
        public void UseRTE(bool isOn)
        {
            if(!isOn)
            {
                SetRTEObject(null);

                //ensure the palette toggles are off
                if(m_configSource != null)
                {
                    m_configSource.DisablePalette();
                }

                m_configSource = null;
            }

            //Setr hud accordingly
            RaycastManager.Instance.DisplayCursor = !isOn;

            if (!CoreManager.Instance.projectSettings.configTagMode.Equals(TagMode._2D))
            {
                HUDManager.Instance.ShowHUDNavigationVisibility(!isOn);
                MMORoom.Instance.ToggleLocalProfileVisibility(!isOn);
            }
        }

        /// <summary>
        /// Called to update RTE selected object
        /// </summary>
        /// <param name="obj"></param>
        public void SetRTEObject(GameObject obj)
        {
            if(obj == null)
            {
                GizmosManager.Instance.Target = null;
            }
            else
            {
                GizmosManager.Instance.Target = obj.transform;
            }

            m_selected = obj;
        }

        /// <summary>
        /// Callede tp change the RTE Tool
        /// 1 = position
        /// 2 = rotate
        /// 3 = scale
        /// </summary>
        /// <param name="tool"></param>
        public void ChangeRTETool(int toolID)
        {
            switch(toolID)
            {
                case 0:
                    GizmosManager.Instance.Target = null;
                    break;
                case 1:
                    GizmosManager.Instance.EnablePositionGizmo();
                    break;
                case 2:
                    GizmosManager.Instance.EnableRotationGizmo();
                    break;
                default:
                    GizmosManager.Instance.EnableScaleGizmo();
                    break;
            }

            if (m_selected)
            {
                SetRTEObject(m_selected);
            }
        }

        #region RTE EVENTS
        #endregion

        [System.Serializable]
        public enum ConfiguratorType { Color, Model, Material, Transform }

#if UNITY_EDITOR
        [CustomEditor(typeof(ConfiguratorManager), true)]
        public class ConfiguratorManager_Editor : BaseInspectorEditor
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