using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Configurator : UniqueID, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private ConfiguratorIOObject settings = new ConfiguratorIOObject();

        [SerializeField]
        private ConfiguratorManager.ConfiguratorType type = ConfiguratorManager.ConfiguratorType.Color;

        [SerializeField]
        private GameObject options;

        [SerializeField]
        private GameObject closeButton;

        //both colors and mats
        public List<MeshRenderer> TargetRenderers = new List<MeshRenderer>();

        //Model
        public List<GameObject> Models = new List<GameObject>();

        //Transform
        public GameObject Target;

        public Configurator Source;
        private bool m_enableRaycastOnPointerExit = true;


        private Vector3 m_targetPos;
        private Quaternion m_targettRot;
        private Vector3 m_targetSca;

        private bool m_isFloorplanGO = false;

        public bool IsActive
        {
            get;
            private set;
        }

        public ConfiguratorManager.ConfiguratorType Type
        {
            get { return type; }
            set { type = value; }
        }

        public override bool HasParent
        {
            get
            {
                return !GetComponent<Canvas>();
            }
        }

        public bool CanUse
        {
            get;
            private set;
        }

        public bool IsOwner
        {
            get;
            private set;
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if (!AppManager.Instance.Instances.ignoreIObjectSettings)
            {
                //need to get the settings from the instances script then update the settings
                foreach (AppInstances.IOObjectConfiguratorHandler setting in AppManager.Instance.Instances.ioConfiguratorObjects)
                {
                    if (setting.referenceID.Equals(GetRawID()))
                    {
                        ApplySettings(setting.settings);
                        break;
                    }
                }
            }

            //Set up the configurator, memory and options clear up
            switch (type)
            {
                case ConfiguratorManager.ConfiguratorType.Color:
                    if (settings.PropertyNames.Count == 0)
                    {
                        settings.PropertyNames.Add("_BaseColor");
                    }

                    Models.Clear();

                    Target = null;
                    settings.ModSprites.Clear();
                    settings.MatSprites.Clear();
                    settings.Materials.Clear();
                    break;
                case ConfiguratorManager.ConfiguratorType.Material:
                    settings.PropertyNames.Clear();
                    settings.colors.Clear();

                    Models.Clear();
                    settings.ModSprites.Clear();

                    Target = null;
                    break;
                case ConfiguratorManager.ConfiguratorType.Model:
                    settings.colors.Clear();
                    settings.PropertyNames.Clear();

                    Target = null;
                    settings.MatSprites.Clear();

                    TargetRenderers.Clear();
                    settings.Materials.Clear();
                    break;
                default:
                    settings.colors.Clear();
                    settings.PropertyNames.Clear();

                    Models.Clear();
                    settings.ModSprites.Clear();
                    settings.MatSprites.Clear();

                    TargetRenderers.Clear();
                    settings.Materials.Clear();

                    if (Target != null)
                    {
                        GizmosTool gTool = Target.GetComponent<GizmosTool>();
                        
                        if (gTool)
                        {
                            gTool.positionContraint = settings.positionContraint;
                            gTool.scaleContraint = settings.scaleContraint;
                            gTool.rotationContraint = settings.rotationContraint;
                        }

                        transform.SetParent(Target.transform);
                    }

                    m_targetPos = Target.transform.position;
                    m_targettRot = Target.transform.rotation;
                    m_targetSca = Target.transform.localScale;

                    if (Target.GetComponent<FloorplanGO>() != null)
                    {
                        m_isFloorplanGO = true;
                    }

                    //network setup
                    if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Offline) && Target != null)
                    {

                    }
                    else
                    {
                        if(Source == null)
                        {
                            MMORoom.Instance.OnPlayerLeftRoom += OnPlayerLeftRoom;
                        }
                    }
                    break;
            }

            if (!ConfiguratorManager.instance.configurators.Contains(this))
            {
                ConfiguratorManager.instance.configurators.Add(this);
            }

            CanUse = true;
        }

        private void OnEnable()
        {
            if (Source != null)
            {
                ConfigurePallette pallette = GetComponentInChildren<ConfigurePallette>(true);

                CanvasGroup cGroup = GetComponentInChildren<CanvasGroup>(true);

                if (cGroup == null)
                {
                    cGroup = gameObject.AddComponent<CanvasGroup>();
                }

                cGroup.alpha = 0.0f;

                StartCoroutine(Delay(pallette, cGroup));
            }
        }

        private void OnDisable()
        {
            if (Source != null)
            {
                ConfiguratorManager.instance.SetRTEObject(null);
            }

            Source = null;
        }

        private void Update()
        {
            //if the config is Transform
            if(type.Equals(ConfiguratorManager.ConfiguratorType.Transform) && !m_isFloorplanGO)
            {
                if(IsOwner)
                {
                    if(InputManager.Instance.GetMouseButtonUp(0) && ConfiguratorManager.instance.ActiveRTEObject == Target.gameObject)
                    {
                        //need to send rot/pos/scale
                        MMOManager.Instance.SendRPC("UpdateConfiguratorTransform", (int)MMOManager.RpcTarget.Others, ID, Target.transform.position, Target.transform.rotation, Target.transform.localScale);
                    }
                }
                else
                {
                    if(!IsOwner)
                    {
                        if(Target.transform.position != m_targetPos)
                        {
                            Target.transform.position = Vector3.Lerp(Target.transform.position, m_targetPos, Time.deltaTime);
                        }

                        if(Target.transform.rotation != m_targettRot)
                        {
                            Target.transform.rotation = Quaternion.Lerp(Target.transform.rotation, m_targettRot, Time.deltaTime);
                        }

                        if(Target.transform.localScale != m_targetSca)
                        {
                            Target.transform.localScale = Vector3.Lerp(Target.transform.localScale, m_targetSca, Time.deltaTime);
                        }
                    }
                }
            }
        }

        public void NetworkSync(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            m_targetPos = pos;
            m_targettRot = rot;
            m_targetSca = scale;
        }

        public void OverrideForFloorplanItemSettings(bool adminOnly, string newID)
        {
            settings.adminOnly = adminOnly;
            ID = newID;

            GizmosTool gTool = Target.GetComponent<GizmosTool>();

            if(gTool)
            {
                gTool.rotationContraint.axes = RuntimeHandle.HandleAxes.Y;
            }

            ConfigurePallette pallette = CreatePallete();
            pallette.CreateTransform(settings.positionContraint.enabled, settings.scaleContraint.enabled, settings.rotationContraint.enabled);

            if(!AppManager.Instance.Data.IsAdminUser)
            {
                transform.localScale = Vector3.zero;
            }
        }

        /// <summary>
        /// Called to initialise the config for 2D UI
        /// </summary>
        public void InitialiseFor2DSystem()
        {
            switch (type)
            {
                case ConfiguratorManager.ConfiguratorType.Color:
                    for (int i = 0; i < TargetRenderers.Count; i++)
                    {
                        TargetRenderers[i].gameObject.AddComponent<ConfigureWorldItem>().Set(this);
                    }
                    break;
                case ConfiguratorManager.ConfiguratorType.Material:
                    for (int i = 0; i < TargetRenderers.Count; i++)
                    {
                        TargetRenderers[i].gameObject.AddComponent<ConfigureWorldItem>().Set(this);
                    }
                    break;
                case ConfiguratorManager.ConfiguratorType.Model:
                    for (int i = 0; i < Models.Count; i++)
                    {
                        Models[i].AddComponent<ConfigureWorldItem>().Set(this);
                    }
                    break;
                default:
                    Target.AddComponent<ConfigureWorldItem>().Set(this);
                    break;
            }
        }

        /// <summary>
        /// Deactivate this controller if player is using transform config and left
        /// </summary>
        /// <param name="player"></param>
        private void OnPlayerLeftRoom(IPlayer player)
        {
            IsActive = false;
        }

        /// <summary>
        /// Changes the UI appearance of the UI configurator for 3D world config
        /// </summary>
        /// <param name="enabled"></param>
        public void SetUIDoneButton(Configurator config, bool enabled)
        {
            ConfigurePallette pallette = GetComponentInChildren<ConfigurePallette>(true);
            pallette.gameObject.SetActive(false);
            closeButton.SetActive(false);
            options.SetActive(enabled);

            //need to make the the child buttons enabled based on the source
            foreach (Button but in options.GetComponentsInChildren<Button>())
            {
                if (but.gameObject.Equals(closeButton)) continue;

                if (but.name.Contains("Move"))
                {
                    but.interactable = config.settings.positionContraint.enabled;
                }
                else if (but.name.Contains("Scale"))
                {
                    but.interactable = config.settings.scaleContraint.enabled;
                }
                else if (but.name.Contains("Rotate"))
                {
                    but.interactable = config.settings.rotationContraint.enabled;
                }
            }

            gameObject.SetActive(false);

            Source = config;
            type = Source.Type;
        }

        /// <summary>
        /// Called to close the UI configurator for 3D world config
        /// </summary>
        public void CloseUIDoneButton()
        {
            //local movement and use Photon Transform View to sync
            if (type.Equals(ConfiguratorManager.ConfiguratorType.Transform))
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("EVENT_TYPE", "CONFIGURATOR");
                dict.Add("C", Source.ID);
                dict.Add("D", "0");

                MMOManager.Instance.ChangeRoomProperty(Source.ID, dict);

                transform.parent.gameObject.SetActive(false);

                ConfiguratorManager.instance.UseRTE(false);

                DisablePalette();
            }
        }

        /// <summary>
        /// Called to update this 2D UI current config palette
        /// </summary>
        /// <param name="config"></param>
        public void SetUIPalletteFromSource(Configurator config)
        {
            ConfigurePallette pallette = GetComponentInChildren<ConfigurePallette>(true);
            pallette.gameObject.SetActive(true);
            closeButton.SetActive(true);
            Source = config;
            type = Source.Type;

            CanvasGroup cGroup = GetComponentInChildren<CanvasGroup>(true);

            if (cGroup == null)
            {
                cGroup = gameObject.AddComponent<CanvasGroup>();
            }

            cGroup.alpha = 0.0f;

            switch (type)
            {
                case ConfiguratorManager.ConfiguratorType.Color:
                    pallette.CreateColors(config.settings.colors);
                    break;
                case ConfiguratorManager.ConfiguratorType.Material:
                    pallette.CreateMaterials(config.settings.MatSprites);
                    break;
                case ConfiguratorManager.ConfiguratorType.Model:
                    pallette.CreateModels(config.settings.ModSprites);
                    break;
                default:
                    pallette.CreateTransform(config.settings.scaleContraint.enabled, config.settings.scaleContraint.enabled, config.settings.rotationContraint.enabled);
                    break;
            }

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(Delay(pallette, cGroup));
            }

            options.SetActive(false);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Delay used to wait for UI to update before correcting size of palette
        /// </summary>
        /// <param name="pallette"></param>
        /// <param name="cGroup"></param>
        /// <returns></returns>
        private IEnumerator Delay(ConfigurePallette pallette, CanvasGroup cGroup)
        {
            yield return new WaitForEndOfFrame();

            GetComponent<RectTransform>().sizeDelta = pallette.Size;
            pallette.gameObject.GetComponent<BoxCollider>().size = new Vector3(pallette.Size.x, pallette.Size.y, 10);

            cGroup.alpha = 1.0f;
        }

        /// <summary>
        /// Called via a palettes option button/toggle to config this
        /// </summary>
        /// <param name="data"></param>
        /// <param name="networked"></param>
        public void Set(string data, bool networked = true)
        {
            if (!AppManager.IsCreated) return;

            if (CoreManager.Instance.CurrentState != state.Running) return;

            if (!UserCanControl()) return;

            //ROOM CHANGE
            if (networked)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("EVENT_TYPE", "CONFIGURATOR");
                dict.Add("C", ID);

                //local movement and use
                if (type.Equals(ConfiguratorManager.ConfiguratorType.Transform))
                {
                    ConfiguratorManager.instance.UseRTE(true);

                    if(m_isFloorplanGO)
                    {
                        Target.GetComponent<FloorplanGO>().AssignOwner();
                    }

                    IsOwner = true;

                    string[] split = data.Split('|');
                    Set(int.Parse(split[0]));

                    dict.Add("D", "1|" + PlayerManager.Instance.GetLocalPlayer().ActorNumber.ToString());
                    dict.Add("T", JsonUtility.ToJson(new TransformProps(Target.transform.position, Target.transform.rotation.eulerAngles, Target.transform.localScale)));
                }
                else
                {
                    dict.Add("D", data);
                    dict.Add("T", "null");
                }

                MMOManager.Instance.ChangeRoomProperty(ID, dict);
            }
            else
            {
                //networked config
                if (type.Equals(ConfiguratorManager.ConfiguratorType.Transform))
                {
                    string[] dataSplit = data.Split('|');

                    //need to check if i am ower
                    if (!dataSplit[1].Equals(PlayerManager.Instance.GetLocalPlayer().ActorNumber.ToString()))
                    {
                        IsOwner = false;
                        IsActive = (data[0].Equals("1")) ? true : false;
                    }
                    else
                    {
                        IsOwner = true;
                    }
                }
                else
                {
                    IsOwner = false;
                    Set(int.Parse(data));
                }

                AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, AnalyticReference);

            }
        }

        public bool UserCanControl()
        {
            string user = PlayerManager.Instance.GetLocalPlayer().CustomizationData.ContainsKey(ConfiguratorManager.instance.UserCheckKey) ? PlayerManager.Instance.GetLocalPlayer().CustomizationData[ConfiguratorManager.instance.UserCheckKey].ToString() : "";

            return CanUserControlThis(user);
        }

        public void RegisterThis(string data)
        {
            if(!MMORoom.Instance.RoomReady)
            {
                if(type.Equals(ConfiguratorManager.ConfiguratorType.Transform))
                {
                    TransformProps wrapper = JsonUtility.FromJson<TransformProps>(data);

                    string[] array = wrapper.pos.Replace("(", "").Replace(")", "").Split(",");
                    Vector3 pos = new Vector3(float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2]));

                    array = wrapper.rot.Replace("(", "").Replace(")", "").Split(",");
                    Vector3 rot = new Vector3(float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2]));

                    array = wrapper.sca.Replace("(", "").Replace(")", "").Split(",");
                    Vector3 sca = new Vector3(float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2]));

                    Target.transform.position = pos;
                    Target.transform.rotation = Quaternion.Euler(rot);
                    Target.transform.localScale = sca;
                }
            }
        }

        public void Set(int index)
        {
            if (Source != null && !CoreManager.Instance.projectSettings.configTagMode.Equals(TagMode._2D))
            {
                //get palette and then use palette to trigger change
                ConfigurePallette pallette = Source.gameObject.GetComponentInChildren<ConfigurePallette>(true);

                if (pallette != null)
                {
                    for (int i = 2; i < pallette.transform.childCount; i++)
                    {
                        if (i.Equals(index + 2))
                        {
                            ConfigureButton but = pallette.transform.GetChild(i).GetComponent<ConfigureButton>();

                            if (but != null)
                            {
                                but.OnClick();
                                break;
                            }

                            UnityEngine.UI.Toggle tog = pallette.transform.GetChild(i).GetComponent<UnityEngine.UI.Toggle>();

                            if (tog != null)
                            {
                                tog.isOn = true;
                                break;
                            }

                            break;
                        }
                    }
                }
            }
            else
            {
                //config
                switch (type)
                {
                    case ConfiguratorManager.ConfiguratorType.Color:
                        SetMaterialColor(index);
                        break;
                    case ConfiguratorManager.ConfiguratorType.Material:
                        SetMaterial(index);
                        break;
                    case ConfiguratorManager.ConfiguratorType.Model:
                        SetModel(index);
                        break;
                    default:
                        SetTransform(index);
                        break;
                }
            }
        }

        public void DisablePalette()
        {
            ConfigurePallette pallette = GetComponentInChildren<ConfigurePallette>(true);

            if (pallette != null)
            {
                pallette.DisableToggles();
            }
        }

        private ConfigurePallette CreatePallete()
        {
            UnityEngine.Object prefabP = Resources.Load("Config/Config_Pallette");
            GameObject go = (GameObject)GameObject.Instantiate(prefabP, Vector3.zero, Quaternion.identity);
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0, 0, 0);
            go.transform.localScale = Vector3.one;
            go.gameObject.SetActive(true);

            return go.GetComponentInChildren<ConfigurePallette>(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ConfigurePallette pallette = GetComponentInChildren<ConfigurePallette>(true);

            if (!RaycastManager.Instance.UIRaycastOperation(pallette.gameObject, true)) return;

            m_enableRaycastOnPointerExit = true;
            CanUse = true;

            if (!RaycastManager.Instance.CastRay)
            {
                if (type.Equals(ConfiguratorManager.ConfiguratorType.Transform))
                {
                    CanUse = false;
                    return;
                }

                m_enableRaycastOnPointerExit = false;
            }

            Tooltip tTip = GetComponent<Tooltip>();

            if (tTip != null)
            {
                RaycastManager.Instance.CastRay = false;
                TooltipManager.Instance.ShowTooltip(gameObject);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //need to check if tooltip is active
            if (!TooltipManager.Instance.IsVisible)
            {
                RaycastManager.Instance.CastRay = true;
                return;
            }

            TooltipManager.Instance.HideTooltip();

            if (m_enableRaycastOnPointerExit)
            {
                RaycastManager.Instance.CastRay = true;
            }
        }

        #region Color
        /// <summary>
        /// Set the color of the taget material 
        /// </summary>
        /// <param name="col"></param>
        private void SetMaterialColor(int index)
        {
            foreach (string prop in settings.PropertyNames)
            {
                Color col = settings.colors[index];

                foreach (MeshRenderer renderer in TargetRenderers)
                {
                    renderer.material.SetColor(prop, col);
                }
            }
        }
        #endregion

        #region Material
        private void SetMaterial(int index)
        {
            if (index < settings.Materials.Count)
            {
                foreach (MeshRenderer renderer in TargetRenderers)
                {
                    renderer.material = settings.Materials[index];
                }
            }
        }
        #endregion

        #region Model
        /// <summary>
        /// Set the model to show and hide the others
        /// </summary>
        /// <param name="index">index of the model to show</param>
        private void SetModel(int index)
        {
            foreach (GameObject model in Models)
            {
                model.SetActive(false);
            }

            if (index < Models.Count)
            {
                Models[index].SetActive(true);
            }
        }
        #endregion

        #region Transfrom
        private void SetTransform(int index)
        {
            if (!CoreManager.Instance.projectSettings.configTagMode.Equals(TagMode._2D))
            {
                ConfiguratorManager.instance.Set2DConfigPallette(this);
            }

            if (Source != null)
            {
                ConfiguratorManager.instance.SetRTEObject(Source.Target);
            }
            else
            {
                ConfiguratorManager.instance.SetRTEObject(Target);
            }

         

            switch (index)
            {
                case 1:
                    ConfiguratorManager.instance.ChangeRTETool(2);
                    break;
                case 2:
                    ConfiguratorManager.instance.ChangeRTETool(3);
                    break;
                case 3:
                    ConfiguratorManager.instance.ChangeRTETool(0);
                    break;
                default:
                    ConfiguratorManager.instance.ChangeRTETool(1);
                    break;
            }
        }
        #endregion

        [System.Serializable]
        public class TransformContraints
        {
            public bool xAxis = true;
            public bool yAxis = true;
            public bool zAxis = true;
        }

        [System.Serializable]
        private class TransformProps
        {
            public string pos;
            public string rot;
            public string sca;

            public TransformProps(Vector3 p, Vector3 r, Vector3 s)
            {
                pos = p.ToString();
                rot = r.ToString();
                sca = s.ToString();
            }
        }

        [System.Serializable]
        public class ConfiguratorIOObject : IObjectSetting
        {
            //Color
            public List<string> PropertyNames = new List<string>();
            public List<Color> colors = new List<Color>();

            //Material
            //Material
            public List<Material> Materials = new List<Material>();
            public List<Sprite> MatSprites = new List<Sprite>();

            //Model
            public List<Sprite> ModSprites = new List<Sprite>();

            public GizmoToolContraint positionContraint = new GizmoToolContraint();
            public GizmoToolContraint rotationContraint = new GizmoToolContraint();
            public GizmoToolContraint scaleContraint = new GizmoToolContraint();

            public ConfiguratorManager.ConfiguratorType Type { get; set; }
        }

        public override IObjectSetting GetSettings(bool remove = false)
        {
            if (!remove)
            {
                IObjectSetting baseSettings = base.GetSettings();
                settings.adminOnly = baseSettings.adminOnly;
                settings.prefix = baseSettings.prefix;
                settings.controlledByUserType = baseSettings.controlledByUserType;
                settings.userTypes = baseSettings.userTypes;

                settings.GO = gameObject.name;
            }

            settings.Type = type;
            settings.ID = id;
            return settings;
        }

        protected override void ApplySettings(IObjectSetting settings)
        {
            if (settings == null) return;

            base.ApplySettings(settings);

            this.settings.PropertyNames = ((ConfiguratorIOObject)settings).PropertyNames;
            this.settings.colors = ((ConfiguratorIOObject)settings).colors;
            this.settings.Materials = ((ConfiguratorIOObject)settings).Materials;
            this.settings.MatSprites = ((ConfiguratorIOObject)settings).MatSprites;
            this.settings.ModSprites = ((ConfiguratorIOObject)settings).ModSprites;

            this.settings.positionContraint = ((ConfiguratorIOObject)settings).positionContraint;
            this.settings.rotationContraint = ((ConfiguratorIOObject)settings).rotationContraint;
            this.settings.scaleContraint = ((ConfiguratorIOObject)settings).scaleContraint;

            ConfigurePallette pallette = GetComponentInChildren<ConfigurePallette>(true);

            switch (type)
            {
                case ConfiguratorManager.ConfiguratorType.Color:
                    pallette.CreateColors(this.settings.colors);
                    break;
                case ConfiguratorManager.ConfiguratorType.Material:
                    pallette.CreateMaterials(this.settings.MatSprites);
                    break;
                case ConfiguratorManager.ConfiguratorType.Model:
                    pallette.CreateModels(this.settings.ModSprites);
                    break;
                default:
                    pallette.CreateTransform(this.settings.positionContraint.enabled, this.settings.scaleContraint.enabled, this.settings.rotationContraint.enabled);
                    break;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Configurator), true), CanEditMultipleObjects]
        public class Configurator_Editor : UniqueID_Editor
        {
            private Configurator configScript;
            private ConfigurePallette pallette;
            private string buttonName = "";
            private bool renderEditor = true;

           // private PhotonView pView;
           // private PhotonTransformView ptView;
            private GizmosTool m_gizmo;

            private void OnEnable()
            {
                GetBanner();

                Initialise();

                pallette = configScript.GetComponentInChildren<ConfigurePallette>(true);

                Canvas can = configScript.GetComponentInParent<Canvas>();

                if (configScript.Target != null)
                {
                    m_gizmo = configScript.Target.GetComponent<GizmosTool>();

                 //   ptView = configScript.Target.GetComponent<PhotonTransformView>();
                  //  pView = configScript.Target.GetComponent<PhotonView>();
                }

                if (can != null && !can.renderMode.Equals(RenderMode.ScreenSpaceOverlay))
                {
                    base.Initialise();

                    if (Application.isPlaying) return;

                    if (m_instances != null)
                    {
                        //need to get the settings from the instances script then update the settings
                        foreach (AppInstances.IOObjectConfiguratorHandler setting in m_instances.ioConfiguratorObjects)
                        {
                            if (setting.referenceID.Equals(configScript.ID))
                            {
                                configScript.ApplySettings(setting.settings);
                                break;
                            }
                        }

                        m_instances.AddIOObject(configScript.ID, configScript.GetSettings());
                    }
                }
                else
                {
                    renderEditor = false;
                }
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    m_instances.RemoveIOObject(configScript.GetSettings(true));
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                Canvas can = configScript.GetComponentInParent<Canvas>();

                if (can != null && !can.renderMode.Equals(RenderMode.ScreenSpaceOverlay))
                {
                    DisplayID();
                }

                if (renderEditor && !Application.isPlaying)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Config Setup", EditorStyles.boldLabel);

                    int enumVal = serializedObject.FindProperty("type").enumValueIndex;

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("type"), true);
                    bool enumChanged = false;

                    if (enumVal != serializedObject.FindProperty("type").enumValueIndex)
                    {
                        enumChanged = true;

                        if (pallette != null)
                        {
                            pallette.DestroyAll();
                        }
                    }

                    switch (serializedObject.FindProperty("type").enumValueIndex)
                    {
                        case 1:

                            //check sprite count
                            int modCount = serializedObject.FindProperty("settings").FindPropertyRelative("ModSprites").arraySize;

                            EditorGUILayout.LabelField("Models Count Non Editable", EditorStyles.miniBoldLabel);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("Models"), true);

                            EditorGUILayout.LabelField("Sprite Count Determines Model Count", EditorStyles.miniBoldLabel);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("ModSprites"), true);

                            if (modCount != serializedObject.FindProperty("settings").FindPropertyRelative("ModSprites").arraySize)
                            {
                                enumChanged = true;

                                if (configScript.settings.ModSprites.Count != serializedObject.FindProperty("settings").FindPropertyRelative("ModSprites").arraySize)
                                {
                                    List<Sprite> mods = new List<Sprite>();

                                    for (int i = 0; i < serializedObject.FindProperty("settings").FindPropertyRelative("ModSprites").arraySize; i++)
                                    {
                                        mods.Add((Sprite)serializedObject.FindProperty("settings").FindPropertyRelative("ModSprites").GetArrayElementAtIndex(i).objectReferenceValue);
                                    }

                                    configScript.settings.ModSprites = mods;
                                }
                            }

                            serializedObject.FindProperty("Models").arraySize = serializedObject.FindProperty("settings").FindPropertyRelative("ModSprites").arraySize;

                            //button to create pallete
                            if (GUILayout.Button(buttonName) || enumChanged)
                            {
                                if (buttonName.Equals("Create Pallette"))
                                {
                                    CreatePallette();

                                    pallette.CreateModels(configScript.settings.ModSprites);
                                }
                                else
                                {
                                    pallette.CreateModels(configScript.settings.ModSprites);
                                }
                            }

                            if (pallette != null)
                            {
                                for (int i = 0; i < serializedObject.FindProperty("settings").FindPropertyRelative("ModSprites").arraySize; i++)
                                {
                                    pallette.SetSpriteAtIndex(i, (Sprite)serializedObject.FindProperty("settings").FindPropertyRelative("ModSprites").GetArrayElementAtIndex(i).objectReferenceValue);
                                }
                            }

                            if(m_gizmo)
                            {
                                DestroyImmediate(m_gizmo);
                            }

                            break;
                        case 2:
                            //check sprite count
                            int matCount = serializedObject.FindProperty("settings").FindPropertyRelative("MatSprites").arraySize;

                            EditorGUILayout.PropertyField(serializedObject.FindProperty("TargetRenderers"), true);

                            EditorGUILayout.LabelField("Matarials Count Non Editable", EditorStyles.miniBoldLabel);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("Materials"), true);

                            EditorGUILayout.LabelField("Sprite Count Determines Material Count", EditorStyles.miniBoldLabel);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("MatSprites"), true);

                            if (matCount != serializedObject.FindProperty("settings").FindPropertyRelative("MatSprites").arraySize)
                            {
                                enumChanged = true;

                                if (configScript.settings.MatSprites.Count != serializedObject.FindProperty("settings").FindPropertyRelative("MatSprites").arraySize)
                                {
                                    List<Sprite> mats = new List<Sprite>();

                                    for (int i = 0; i < serializedObject.FindProperty("settings").FindPropertyRelative("MatSprites").arraySize; i++)
                                    {
                                        mats.Add((Sprite)serializedObject.FindProperty("settings").FindPropertyRelative("MatSprites").GetArrayElementAtIndex(i).objectReferenceValue);
                                    }

                                    configScript.settings.MatSprites = mats;
                                }
                            }

                            serializedObject.FindProperty("settings").FindPropertyRelative("Materials").arraySize = serializedObject.FindProperty("settings").FindPropertyRelative("MatSprites").arraySize;

                            //button to create pallete
                            if (GUILayout.Button(buttonName) || enumChanged)
                            {
                                if (buttonName.Equals("Create Pallette"))
                                {
                                    CreatePallette();

                                    pallette.CreateMaterials(configScript.settings.MatSprites);
                                }
                                else
                                {
                                    pallette.CreateMaterials(configScript.settings.MatSprites);
                                }
                            }

                            if (pallette != null)
                            {
                                for (int i = 0; i < serializedObject.FindProperty("settings").FindPropertyRelative("MatSprites").arraySize; i++)
                                {
                                    pallette.SetSpriteAtIndex(i, (Sprite)serializedObject.FindProperty("settings").FindPropertyRelative("MatSprites").GetArrayElementAtIndex(i).objectReferenceValue);
                                }
                            }

                            if (m_gizmo)
                            {
                                DestroyImmediate(m_gizmo);
                            }
                            break;
                        case 3:
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("Target"), true);

                            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("positionContraint"), true);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("rotationContraint"), true);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("scaleContraint"), true);

                            //button to create pallete
                            if (GUILayout.Button(buttonName) || enumChanged)
                            {
                                if (buttonName.Equals("Create Pallette"))
                                {
                                    CreatePallette();

                                    pallette.CreateTransform(configScript.settings.positionContraint.enabled, configScript.settings.scaleContraint.enabled, configScript.settings.rotationContraint.enabled);
                                }
                                else
                                {
                                    pallette.CreateTransform(configScript.settings.positionContraint.enabled, configScript.settings.scaleContraint.enabled, configScript.settings.rotationContraint.enabled);
                                }
                            }

                            if (configScript.Target != null)
                            {

                                if (m_gizmo == null)
                                {
                                    m_gizmo = configScript.Target.AddComponent<GizmosTool>();
                                }

                                if(m_gizmo != null)
                                {
                                    m_gizmo.positionContraint = configScript.settings.positionContraint;
                                    m_gizmo.scaleContraint = configScript.settings.scaleContraint;
                                    m_gizmo.rotationContraint = configScript.settings.rotationContraint;
                                }
                            }

                            break;
                        default:


                            //check color count
                            int colCount = serializedObject.FindProperty("settings").FindPropertyRelative("colors").arraySize;

                            EditorGUILayout.PropertyField(serializedObject.FindProperty("TargetRenderers"), true);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("PropertyNames"), true);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings").FindPropertyRelative("colors"), true);

                            if (colCount != serializedObject.FindProperty("settings").FindPropertyRelative("colors").arraySize)
                            {
                                enumChanged = true;

                                if (configScript.settings.colors.Count != serializedObject.FindProperty("settings").FindPropertyRelative("colors").arraySize)
                                {
                                    List<Color> cols = new List<Color>();

                                    for (int i = 0; i < serializedObject.FindProperty("settings").FindPropertyRelative("colors").arraySize; i++)
                                    {
                                        cols.Add(serializedObject.FindProperty("settings").FindPropertyRelative("colors").GetArrayElementAtIndex(i).colorValue);
                                    }

                                    configScript.settings.colors = cols;
                                }
                            }

                            //button to create pallete
                            if (GUILayout.Button(buttonName) || enumChanged)
                            {
                                if (buttonName.Equals("Create Pallette"))
                                {
                                    CreatePallette();

                                    pallette.CreateColors(configScript.settings.colors);
                                }
                                else
                                {
                                    pallette.CreateColors(configScript.settings.colors);
                                }
                            }

                            if (pallette != null)
                            {
                                for (int i = 0; i < serializedObject.FindProperty("settings").FindPropertyRelative("colors").arraySize; i++)
                                {
                                    pallette.SetColorAtIndex(i, serializedObject.FindProperty("settings").FindPropertyRelative("colors").GetArrayElementAtIndex(i).colorValue);
                                }
                            }

                            if (m_gizmo)
                            {
                                DestroyImmediate(m_gizmo);
                            }
                            break;
                    }

                    if (pallette != null)
                    {
                        if (GUILayout.Button("Destroy Pallette"))
                        {
                            DestroyImmediate(pallette.gameObject);
                        }
                    }
                }
                else
                {
                    if (!renderEditor)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("options"), true);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("closeButton"), true);
                    }

                    if (pallette == null)
                    {
                        if (GUILayout.Button("Create Pallette"))
                        {
                            CreatePallette();
                        }
                    }
                }

                if (pallette != null)
                {
                    configScript.gameObject.GetComponent<RectTransform>().sizeDelta = pallette.Size;
                    pallette.gameObject.GetComponent<BoxCollider>().size = new Vector3(pallette.Size.x, pallette.Size.y, 10);

                    buttonName = "Update Pallette";
                }
                else
                {
                    configScript.gameObject.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
                    buttonName = "Create Pallette";
                }

                if (GUI.changed || GONameChanged())
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(configScript);

                    if (Application.isPlaying) return;

                    if (m_instances != null)
                    {
                        m_instances.AddIOObject(configScript.ID, configScript.GetSettings());
                    }
                }
            }

            protected override void Initialise()
            {
                configScript = (Configurator)target;
            }

            private void CreatePallette()
            {
                UnityEngine.Object prefabP = Resources.Load("Config/Config_Pallette");
                GameObject go = (GameObject)GameObject.Instantiate(prefabP, Vector3.zero, Quaternion.identity);
                go.transform.SetParent(configScript.gameObject.transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.Euler(0, 0, 0);
                go.transform.localScale = Vector3.one;
                go.gameObject.SetActive(true);

                pallette = go.GetComponentInChildren<ConfigurePallette>();
            }
        }
#endif
    }
}
