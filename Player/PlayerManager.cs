using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class PlayerManager : Singleton<PlayerManager>
    {
        public static PlayerManager Instance
        {
            get
            {
                return ((PlayerManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("UI")]
        public CameraMenuHandler cameraMenuHandler;
        public GameObject focusUIToogle;
        public GameObject hotspotUIToggle;
        public GameObject topdownUIToggle;


        private IPlayer localPlayer;
        private string PlayerPrefabName = "Player";
        private GameObject m_cameraOrbit;

        [HideInInspector]
        public bool ThirdPersonCameraActive = false;

        [HideInInspector]
        public bool OrbitCameraActive = false;


        private PerspectiveCameraMode perspectiveMode = PerspectiveCameraMode.ThirdPerson;

        /// <summary>
        /// Access to the current remote player this local player is following
        /// </summary>
        public IPlayer CurrentPlayerFollowing
        {
            get;
            private set;
        }

        public PlayerControlSettings MainControlSettings
        {
            get;
            private set;
        }

        public GameObject OrbitCamera
        {
            get
            {
                return m_cameraOrbit;
            }
        }

        public bool IDBSettingsExists
        {
            get;
            private set;
        }


        public System.Action<Hashtable> OnPlayerCustomizationUpdate { get; set; }
        public System.Action<IPlayer, Hashtable> OnRemotePlayerCustomizationUpdate { get; set; }

        public static System.Action OnUpdate { get; set; }
        public static System.Action OnLateUpdate { get; set; }
        public static System.Action OnFixedUpdate { get; set; }

        public bool IsTeleporting
        {
            get;
            set;
        }

        private void OnDisable()
        {
            OnUpdate = null;
            OnLateUpdate = null;
            OnFixedUpdate = null;
        }

        private void Update()
        {
            if (OnUpdate != null)
            {
                if (CoreManager.Instance.CurrentState == state.Running)
                {
                    OnUpdate.Invoke();
                }
            }
        }

        private void FixedUpdate()
        {
            if (OnFixedUpdate != null)
            {
                if (CoreManager.Instance.CurrentState == state.Running)
                {
                    OnFixedUpdate.Invoke();
                }
            }
        }

        private void LateUpdate()
        {
            if (OnLateUpdate != null)
            {
                if (CoreManager.Instance.CurrentState == state.Running)
                {
                    OnLateUpdate.Invoke();
                }
            }
        }

        public string GetPlayerName(string src)
        {
            string str = src;

            if(AppManager.Instance.Settings.projectSettings.checkDuplicatePhotonNames)
            {
                string[] split = src.Split('$');
                str = split[2];
            }

            return str; 
        }

        /// <summary>
        /// Spawn the local player object
        /// </summary>
        /// <param name="online">Whether online (connected to Photon) or not</param>
        public void SpawnLocalPlayer(bool online)
        {
            if (CoreManager.Instance.projectSettings.enableOrbitCamera)
            {
                UnityEngine.Object prefab = string.IsNullOrEmpty(AppManager.Instance.Settings.projectSettings.orbitCameraPrefab) ? Resources.Load("CameraOrbit") : Resources.Load(AppManager.Instance.Settings.projectSettings.orbitCameraPrefab);

                if (prefab != null)
                {
                    m_cameraOrbit = Instantiate(prefab as GameObject, Vector3.zero, Quaternion.identity);
                    m_cameraOrbit.transform.localScale = Vector3.one;
                    m_cameraOrbit.gameObject.SetActive(false);
                }
            }

            Vector3 sPoint = SpawnManager.Instance.GetSpawnPosition();
            Quaternion dRotation = SpawnManager.Instance.GetSpawnRotation();

            //check what type of player to instantiate
            PlayerPrefabName = CoreManager.Instance.playerSettings.playerController;

            if (online)
            {
                GameObject go = MMOManager.Instance.InstantiatePlayer(sPoint, dRotation, PlayerPrefabName);
                localPlayer = go.GetComponentInChildren<IPlayer>(true);
            }
            else
            {
                UnityEngine.Object prefab = Resources.Load(PlayerPrefabName);
                localPlayer = ((GameObject)Instantiate(prefab, sPoint, dRotation)).GetComponentInChildren<IPlayer>(true);
                localPlayer.MainObject.GetComponent<MMOPlayer>().RemovePhotonComponents();
            }

            if (CoreManager.Instance.projectSettings.avatarMode.Equals(AvatarMode.Random) && !AppManager.Instance.URLParamUser)
            {
                //if use indexedDB && !constant
                if(CoreManager.Instance.projectSettings.useIndexedDB && !CoreManager.Instance.projectSettings.alwaysRandomiseAvatar)
                {
                    if(!string.IsNullOrEmpty(AppManager.Instance.Data.CustomiseJson))
                    {
                        AvatarManager.Instance.Customise(localPlayer);
                    }
                    else
                    {
                        AvatarManager.Instance.Randomise(localPlayer);
                    }
                }
                else
                {
                    AvatarManager.Instance.Randomise(localPlayer);
                }
            }
            else
            {
                AvatarManager.Instance.Customise(localPlayer);
            }

            SpawnManager.Instance.EnableSpawnCamera(false);
            SwitchPerspectiveCameraMode(CoreManager.Instance.projectSettings.perspectiveCameraMode);


            //store settings for player here so thet do not effect the core settings
            MainControlSettings = new PlayerControlSettings();
            MainControlSettings.walk = CoreManager.Instance.playerSettings.walkSpeed;
            MainControlSettings.run = CoreManager.Instance.playerSettings.runSpeed;
            MainControlSettings.strife = CoreManager.Instance.playerSettings.strafingSpeed;
            MainControlSettings.mouse = CoreManager.Instance.playerSettings.sensitivity;
            MainControlSettings.invertX = CoreManager.Instance.playerSettings.invertMouseX ? 1 : 0;
            MainControlSettings.invertY = CoreManager.Instance.playerSettings.invertMouseY ? 1 : 0;
            MainControlSettings.highlightOn = CoreManager.Instance.projectSettings.highlightType.Equals(HighlightType.Disabled) ? 0 : 1;
            MainControlSettings.tooltipOn = CoreManager.Instance.HUDSettings.useTooltips ? 1 : 0;
            MainControlSettings.nameOn = CoreManager.Instance.HUDSettings.showHUDUsernameName ? 1 : 0;
            MainControlSettings.controllerType = CoreManager.Instance.playerSettings.mobileControllerType.Equals(BrandLab360.PlayerControlSettings.MobileControlType._Joystick) ? 0 : 1;

            MainControlSettings.controls = new List<int>();

            //need to make fixed controls array for keys
            MainControlSettings.controls.Add((int)AppManager.Instance.Data.fowardDirectionKey);
            MainControlSettings.controls.Add((int)AppManager.Instance.Data.backDirectionKey);
            MainControlSettings.controls.Add((int)AppManager.Instance.Data.leftDirectionKey);
            MainControlSettings.controls.Add((int)AppManager.Instance.Data.rightDirectionKey);
            MainControlSettings.controls.Add((int)AppManager.Instance.Data.sprintKey);
            MainControlSettings.controls.Add((int)AppManager.Instance.Data.strifeLeftDirectionKey);
            MainControlSettings.controls.Add((int)AppManager.Instance.Data.strifeRightDirectionKey);
            MainControlSettings.controls.Add((int)AppManager.Instance.Data.focusKey);
            MainControlSettings.controls.Add((int)AppManager.Instance.Data.interactionKey);

            bool externalData = AppManager.Instance.Settings.projectSettings.loginMode.Equals(LoginMode.Standard) ? CoreManager.Instance.projectSettings.useIndexedDB : 
                !string.IsNullOrEmpty(AppManager.Instance.Data.LoginProfileData.player_settings);

            if (externalData && !AppManager.Instance.URLParamUser)
            {
                if(AppManager.Instance.Settings.projectSettings.loginMode.Equals(LoginMode.Standard))
                {
#if UNITY_EDITOR
                    if (AppManager.Instance.Settings.editorTools.createWebClientSimulator)
                    {
                        IndexedDbManager.Instance.iDbListener += OnIndexedDB;
                        WebClientSimulator.Instance.ProcessIndexedDBRequest("playerControlsData");
                    }
                    else
                    {
                        ApplySettings();
                    }
#else
                    if(AppManager.Instance.Settings.projectSettings.streamingMode != WebClientMode.None)
                    {
                        IndexedDbManager.Instance.iDbListener += OnIndexedDB;
                        IndexedDbManager.Instance.GetEntry("playerControlsData");
                    }
                    else
                    {
                        ApplySettings();
                    }
#endif
                }
                else
                {
                    OnIndexedDB(AppManager.Instance.Data.LoginProfileData.player_settings);
                }
            }
            else
            {
                if (AppManager.Instance.URLParamUser)
                {
                    if(WebClientCommsManager.Instance.UrlParameters.ContainsKey("PlayerControls"))
                    {
                        Debug.Log("Using URL Params player controls");
                        var uploadResponse = new iDbResponse();
                        uploadResponse.iDbEntry = WebClientCommsManager.Instance.UrlParameters["PlayerControls"].ToString();
                        OnIndexedDB(JsonUtility.ToJson(uploadResponse));
                    }
                }
                else
                {
                    ApplySettings();
                }
            }

            Debug.Log("Local Player created");

            localPlayer.ArrowPointer.GetComponentInChildren<UnityEngine.UI.Image>().color = CoreManager.Instance.playerSettings.pointerColor;
            localPlayer.TransformObject.GetComponent<MMOPlayer>().locator.GetComponentInChildren<UnityEngine.UI.Image>().color = CoreManager.Instance.playerSettings.pointerColor;

            if (AppManager.Instance.Settings.playerSettings.pointerDisplayType > 0)
            {
                localPlayer.ArrowPointer.GetComponentInChildren<UnityEngine.UI.Image>(true).gameObject.SetActive(false);
                localPlayer.TransformObject.GetComponent<MMOPlayer>().locator.GetComponentInChildren<UnityEngine.UI.Image>(true).gameObject.SetActive(false);

                Transform arrow = localPlayer.ArrowPointer.GetComponentInChildren<UnityEngine.UI.Image>(true).transform.parent.GetChild(1);
                arrow.gameObject.SetActive(true);
                arrow.GetComponentInChildren<Renderer>(true).material.color = CoreManager.Instance.playerSettings.pointerColor;

                arrow = localPlayer.TransformObject.GetComponent<MMOPlayer>().locator.GetComponentInChildren<UnityEngine.UI.Image>(true).transform.parent.GetChild(1);
                arrow.gameObject.SetActive(true);
                arrow.GetComponentInChildren<Renderer>(true).material.color = CoreManager.Instance.playerSettings.pointerColor;
            }

            localPlayer.TransformObject.forward = SpawnManager.Instance.Forward();

            //required if spawn angles is not vector3.zero
            Vector3 localAngles = SpawnManager.Instance.GetLocalAngles();
            localPlayer.TargetCameraRotation = localAngles;

            PerspectiveCameraMode perspectiveCameraMode = CoreManager.Instance.projectSettings.perspectiveCameraMode;

            if (!CoreManager.Instance.projectSettings.enableOrbitCamera && perspectiveCameraMode.Equals(PerspectiveCameraMode.CameraOrbit))
            {
                perspectiveCameraMode = PerspectiveCameraMode.ThirdPerson;
            }

            if (perspectiveCameraMode.Equals(PerspectiveCameraMode.ThirdPerson))
            {
                if (cameraMenuHandler != null)
                {
                    cameraMenuHandler.SelectTP();
                }

                SwitchToThirdPerson();
            }
            else if(perspectiveCameraMode.Equals(PerspectiveCameraMode.CameraOrbit))
            {
                //instantiate orbit camera 
                Debug.Log("Switching to PerspectiveCameraMode.CameraOrbit");
                SwitchToOrbit(true);

                if (cameraMenuHandler != null)
                {
                    cameraMenuHandler.SelectOrbit();
                }
            }
            else
            {
                if (cameraMenuHandler != null)
                {
                    cameraMenuHandler.SelectFP();
                }
            }

            //set the player custimation data for USERTYPE
            if (AppManager.Instance.Data.AdminRole != null)
            {
                Hashtable hash = new Hashtable();
                hash.Add("USERTYPE", AppManager.Instance.Data.AdminRole.role);
                UpdatePlayerCustomizationData(hash, false);
            }

            MMORoom.Instance.OnRoomReady += OnRoomReady;
            CoreManager.Instance.OnJoinedRoom();

            AppManager.Instance.Data.SceneSpawnLocation = null;
        }

        private void  OnRoomReady()
        {
            MMORoom.Instance.OnRoomReady -= OnRoomReady;

            if (ThirdPersonCameraActive)
            {
                if (AppManager.Instance.Data.SceneSpawnLocation != null)
                {
                    switch (AppManager.Instance.Data.SceneSpawnLocation.view)
                    {
                        case "_Front":
                            localPlayer.TransformObject.GetComponentInChildren<CameraThirdPerson>(true).SetTargetRotation(0, 180);
                            break;
                        case "_Rear":
                            localPlayer.TransformObject.GetComponentInChildren<CameraThirdPerson>(true).SetTargetRotation(0, 0);
                            break;
                        case "_Left":
                            localPlayer.TransformObject.GetComponentInChildren<CameraThirdPerson>(true).SetTargetRotation(0, 90);
                            break;
                        case "_Right":
                            localPlayer.TransformObject.GetComponentInChildren<CameraThirdPerson>(true).SetTargetRotation(0, 270);
                            break;
                    }
                }
                else
                {
                    switch (AppManager.Instance.Settings.playerSettings.startingCameraView)
                    {
                        case CameraStartView._Font:
                            localPlayer.TransformObject.GetComponentInChildren<CameraThirdPerson>(true).SetTargetRotation(0, 180);
                            break;
                        case CameraStartView._Rear:
                            localPlayer.TransformObject.GetComponentInChildren<CameraThirdPerson>(true).SetTargetRotation(0, 0);
                            break;
                        case CameraStartView._Left:
                            localPlayer.TransformObject.GetComponentInChildren<CameraThirdPerson>(true).SetTargetRotation(0, 90);
                            break;
                        case CameraStartView._Right:
                            localPlayer.TransformObject.GetComponentInChildren<CameraThirdPerson>(true).SetTargetRotation(0, 270);
                            break;
                    }
                }
            }
        }

        private void OnIndexedDB(string responce)
        {
            IndexedDbManager.Instance.iDbListener -= OnIndexedDB;
            iDbResponse iDB = JsonUtility.FromJson<iDbResponse>(responce);

            if(iDB != null && !string.IsNullOrEmpty(iDB.iDbEntry))
            {
                string[] dataSplit = iDB.iDbEntry.Split('|');

                for(int i = 0; i < dataSplit.Length; i++)
                {
                    string[] split = dataSplit[i].Split('-');

                    if(split[0].Equals("walk"))
                    {
                        MainControlSettings.walk = float.Parse(split[1]);
                    }
                    else if(split[0].Equals("run"))
                    {
                        MainControlSettings.run = float.Parse(split[1]);
                    }
                    else if (split[0].Equals("strife"))
                    {
                        MainControlSettings.strife = float.Parse(split[1]);
                    }
                    else if (split[0].Equals("mouse"))
                    {
                        MainControlSettings.mouse = float.Parse(split[1]);
                    }
                    else if (split[0].Equals("highlight"))
                    {
                        MainControlSettings.highlightOn = int.Parse(split[1]);
                    }
                    else if (split[0].Equals("invertX"))
                    {
                        MainControlSettings.invertX = int.Parse(split[1]);
                    }
                    else if (split[0].Equals("invertY"))
                    {
                        MainControlSettings.invertY = int.Parse(split[1]);
                    }
                    else if (split[0].Equals("tooltip"))
                    {
                        MainControlSettings.tooltipOn = int.Parse(split[1]);
                    }
                    else if (split[0].Equals("name"))
                    {
                        MainControlSettings.nameOn = int.Parse(split[1]);
                    }
                    else if (split[0].Equals("controllerType"))
                    {
                        MainControlSettings.controllerType = int.Parse(split[1]);
                    }
                    else if (split[0].Equals("controls"))
                    {
                        MainControlSettings.controls = new List<int>();
                        string[] controls = split[1].Split("+");

                        for (int j = 0; j < controls.Length; j++)
                        {
                            MainControlSettings.controls.Add(int.Parse(controls[i]));
                        }
                    }
                }

                AppManager.Instance.Data.CustomiseControls = GetPlayerURLParams();
                IDBSettingsExists = true;
            }
            else
            {
                IDBSettingsExists = false;
            }

            ApplySettings();
        }

        /// <summary>
        /// Get the local player object
        /// </summary>
        /// <returns>The local player controller</returns>
        public IPlayer GetLocalPlayer()
        {
            return localPlayer;
        }

        /// <summary>
        /// Get the any player object
        /// </summary>
        /// <returns>The local player controller</returns>
        public IPlayer GetPlayer(string id)
        {
            foreach (var view in MMOManager.Instance.GetAllPlayers())
            {
                if (view.ID.Equals(id))
                {
                    return view;
                }
            }

            return localPlayer;
        }

        /// <summary>
        /// Get the any player object byt actor number
        /// </summary>
        /// <returns>The local player controller</returns>
        public IPlayer GetPlayer(int actor)
        {
            foreach (var view in MMOManager.Instance.GetAllPlayers())
            {
                if (view.ActorNumber.Equals(actor))
                {
                    return view;
                }
            }

            return null;
        }

        /// <summary>
        /// Teleport the local player to target position and orient to target rotation
        /// </summary>
        public void TeleportLocalPlayer(Vector3 targetDestination, Vector2 targetRotation)
        {
            FreezePlayer(true);

            if (localPlayer.NavMeshAgentScript != null)
            {
                localPlayer.NavMeshAgentScript.enabled = false;
            }

            localPlayer.TransformObject.position = targetDestination;
            localPlayer.TransformObject.rotation = Quaternion.Euler(new Vector3(0, targetRotation.y, 0));

           //if(!ThirdPersonCameraActive)
           // {
                localPlayer.MainCamera.GetComponent<Transform>().localEulerAngles = new Vector3(targetRotation.x, 0.0f, 0.0f);
                Vector2 vec = new Vector3(localPlayer.MainCamera.GetComponent<Transform>().localEulerAngles.x * -1, localPlayer.TransformObject.localEulerAngles.y);
                localPlayer.TargetCameraRotation = vec;
            // }

            StartCoroutine(DelayTeleportUnfreeze());
        }

        private IEnumerator DelayTeleportUnfreeze()
        {
            yield return new WaitForEndOfFrame();

            if (localPlayer.NavMeshAgentScript != null)
            {
                localPlayer.NavMeshAgentScript.enabled = true;
            }

            FreezePlayer(false);
        }

        /// <summary>
        /// Change the perspective players camera mode
        /// </summary>
        /// <param name="mode"></param>
        public void SwitchPerspectiveCameraMode(PerspectiveCameraMode mode)
        {
            perspectiveMode = mode;

            switch (mode)
            {
                case PerspectiveCameraMode.FirstPerson:

                    if (OrbitCameraActive)
                    {
                        SwitchToOrbit(false);
                    }

                    SwitchToFirstPerson();

                    break;
                case PerspectiveCameraMode.ThirdPerson:

                    if (OrbitCameraActive)
                    {
                        SwitchToOrbit(false);
                    }

                    SwitchToThirdPerson();

                    break;

                case PerspectiveCameraMode.CameraOrbit:

                    if(!OrbitCameraActive)
                    {
                        SwitchToOrbit(true);
                    }

                    break;
                default:
                   
                    break;
            }
        }

        /// <summary>
        /// Toggles the state of the menus UI button for the perspective Camera mode
        /// </summary>
        /// <param name="isHidden"></param>
        public void ToggleThirdPersonMenuUIVisibility(bool isHidden)
        {
            ChangeUIVisibility(isHidden);
        }

        public void ToggleFocus(bool focus)
        {
            if(localPlayer != null)
            {
                localPlayer.ToggleFocus(focus);
            }
        }

        /// <summary>
        /// Apply the new local players settings based on the Managers PlayerSettings
        /// </summary>
        public void ApplySettings()
        {
            localPlayer.ApplySettings();
        }

        /// <summary>
        /// Used to show/Hide permission message
        /// </summary>
        public void ShowPermisionMessage(bool show)
        {
            FreezePlayer(show);
            HUDManager.Instance.ToggleHUDMessage("PERMISSION_MESSAGE", show);
        }

        /// <summary>
        /// Locally change the UI menu buttons visibility
        /// </summary>
        /// <param name="isHidden"></param>
        private void ChangeUIVisibility(bool isHidden)
        {
            if (cameraMenuHandler != null)
            {
                if (!cameraMenuHandler.GetComponent<CanvasGroup>())
                {
                    cameraMenuHandler.gameObject.AddComponent<CanvasGroup>();
                }

                cameraMenuHandler.GetComponent<CanvasGroup>().alpha = (isHidden) ? 0.2f : 1;
                cameraMenuHandler.GetComponent<CanvasGroup>().blocksRaycasts = (isHidden) ? false : true;
            }

            if(focusUIToogle != null)
            {
                if (!focusUIToogle.GetComponent<CanvasGroup>())
                {
                    focusUIToogle.gameObject.AddComponent<CanvasGroup>();
                }

                focusUIToogle.GetComponent<CanvasGroup>().alpha = (isHidden) ? 0.2f : 1;
                focusUIToogle.GetComponent<CanvasGroup>().blocksRaycasts = (isHidden) ? false : true;
            }
        }

        /// <summary>
        /// Local switch to First Person
        /// </summary>
        private void SwitchToFirstPerson()
        {
            if(localPlayer != null)
            {
                localPlayer.SwitchToThirdPerson(false);
                localPlayer.ArrowPointer.transform.SetParent(localPlayer.MainCamera.transform);
                localPlayer.ArrowPointer.transform.localPosition = Vector3.zero;

                ThirdPersonCameraActive = false;
            }
        }

        /// <summary>
        /// Local switch to Third Person
        /// </summary>
        private void SwitchToThirdPerson()
        {
            if(localPlayer != null)
            {
                localPlayer.SwitchToThirdPerson(true);

                localPlayer.ArrowPointer.transform.SetParent(localPlayer.TransformObject);
                localPlayer.ArrowPointer.transform.localPosition = new Vector3(0, 1.0f, 0.0f);

                ThirdPersonCameraActive = true;
            }
        }

        /// <summary>
        /// Local switch to Third Person
        /// </summary>
        private void SwitchToOrbit(bool isOn)
        {
            OrbitCameraActive = isOn;

            HUDManager.Instance.ToggleHUDControl("ORBIT_CONTROL", isOn);

            //need to deactive player cameras
            if (localPlayer != null)
            {
                localPlayer.EnableCameras(!isOn);
            }

            //enable orbit
            m_cameraOrbit.SetActive(isOn);

            if (focusUIToogle != null)
            {
                if (!focusUIToogle.GetComponent<CanvasGroup>())
                {
                    focusUIToogle.gameObject.AddComponent<CanvasGroup>();
                }

                focusUIToogle.GetComponent<CanvasGroup>().alpha = (isOn) ? 0.2f : 1;
                focusUIToogle.GetComponent<CanvasGroup>().blocksRaycasts = (isOn) ? false : true;
            }


            if (hotspotUIToggle != null)
            {
                if (!hotspotUIToggle.GetComponent<CanvasGroup>())
                {
                    hotspotUIToggle.gameObject.AddComponent<CanvasGroup>();
                }

                hotspotUIToggle.GetComponent<CanvasGroup>().alpha = (isOn) ? 0.2f : 1;
                hotspotUIToggle.GetComponent<CanvasGroup>().blocksRaycasts = (isOn) ? false : true;
            }


            if (topdownUIToggle != null)
            {
                if (!topdownUIToggle.GetComponent<CanvasGroup>())
                {
                    topdownUIToggle.gameObject.AddComponent<CanvasGroup>();
                }

                topdownUIToggle.GetComponent<CanvasGroup>().alpha = (isOn) ? 0.2f : 1;
                topdownUIToggle.GetComponent<CanvasGroup>().blocksRaycasts = (isOn) ? false : true;
            }
        }

        /// <summary>
        /// Global command to freeze the player
        /// </summary>
        /// <param name="freeze"></param>
        public void FreezePlayer(bool freeze)
        {
            if (!gameObject.scene.isLoaded || localPlayer == null) return;

            if(!freeze && OrbitCameraActive) return;

            if (!freeze && NoticeBoardManager.Instance.ActiveNotice != null) return;

            if (!ChairManager.Instance.HasPlayerOccupiedChair(localPlayer.ID) || !HUDManager.Instance.GetHUDControlObject("NOTICE_CONTROL").activeInHierarchy)
            {
                //freeze third person camera
                localPlayer.TransformObject.GetComponentInChildren<CameraThirdPerson>(true).FreezeControl(freeze);

                localPlayer.FreezePosition = freeze;
                localPlayer.FreezeRotation = freeze;
            }
        }

        /// <summary>
        /// Called to set this player status across the network
        /// </summary>
        /// <param name="status"></param>
        public void SetPlayerStatus(string status)
        {
            Hashtable hash = new Hashtable();
            hash.Add("STATUS", status);
            MMOManager.Instance.SetPlayerProperties(hash);
        }

        /// <summary>
        /// Sets a custom property for this player
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetPlayerProperty(string key, string value)
        {
            Hashtable hash = new Hashtable();
            hash.Add(key, value);
            MMOManager.Instance.SetPlayerProperties(hash);
        }

        /// <summary>
        /// Called to visualy locate the playeer you want to find/follow
        /// </summary>
        /// <param name="player"></param>
        public void FollowRemotePlayer(IPlayer player, bool follow)
        {
            foreach (var view in MMOManager.Instance.GetAllPlayers())
            {
                if (view.ID.Equals(player.ID))
                {
                    localPlayer.ArrowPointer.SetActive(follow);
                    localPlayer.ArrowPointer.GetComponent<LookAtObject>().UpdateFocus(view.TransformObject);

                    view.TransformObject.GetComponent<MMOPlayer>().locator.SetActive(follow);
                    view.TransformObject.GetComponent<MMOPlayer>().locator.GetComponent<LookAtObject>().UpdateFocus(localPlayer.TransformObject);

                    CurrentPlayerFollowing = (follow) ? player : null;
                }
                else
                {
                    view.TransformObject.GetComponent<MMOPlayer>().locator.SetActive(false);
                }
            }
        }

        public void PointToTransforms(bool pointTo, Transform[] trans)
        {
            localPlayer.ArrowPointer.SetActive(pointTo);
            localPlayer.ArrowPointer.GetComponent<LookAtObject>().UpdateFocus(trans);
        }

        /// <summary>
        /// States if the player is moving the camera when button is held down
        /// </summary>
        /// <returns></returns>
        public bool IsPlayerControllerMoving()
        {
            if (localPlayer == null) return false;

            if (AppManager.Instance.Data.SceneSpawnLocation != null) return false;

            if (localPlayer.IsButtonHeldDown || localPlayer.MovementID >= 0 || localPlayer.IsMoving)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Locate the user list you want to attain
        /// </summary>
        /// <param name="GOname"></param>
        /// <returns></returns>
        public UserList GetUserList(string GOname)
        {
            UserList[] all = FindObjectsByType<UserList>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].gameObject.name.Equals(GOname))
                {
                    return all[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Call to show a specific user list in the UI
        /// </summary>
        /// <param name="GOname"></param>
        /// <param name="players"></param>
        public void ShowUserList(string GOname, string listID, bool show, List<IPlayer> players = null)
        {
            UserList[] all = FindObjectsByType<UserList>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].gameObject.name.Equals(GOname))
                {
                    if (show)
                    {
                        all[i].IDSource = listID;

                        if (players != null)
                        {
                            if (all[i].ListSource == null)
                            {
                                all[i].ListSource = new List<IPlayer>();
                            }

                            all[i].ListSource.Clear();
                            all[i].ListSource.AddRange(players);
                        }
                    }
                    else
                    {
                        all[i].IDSource = "";
                    }

                    all[i].gameObject.SetActive(show);

                    break;
                }
            }
        }

        /// <summary>
        /// Returns the status color of this player
        /// </summary>
        /// <returns></returns>
        public Color GetPlayerStatus(string playerID)
        {
            if (!gameObject.scene.isLoaded) return Color.white;

            IPlayer player = GetPlayer(playerID);

            if(player != null)
            {
                if (ChairManager.Instance.HasPlayerOccupiedChair(playerID))
                {
                    return CoreManager.Instance.chatSettings.busy;
                }
                else if (!MMOManager.Instance.GetAllPlayers().Contains(player))
                {
                    return CoreManager.Instance.chatSettings.offline;
                }
                else
                {
                    bool hasStatusProp = MMOManager.Instance.PlayerHasProperty(player, "STATUS");

                    if (hasStatusProp)
                    {
                        if (MMOManager.Instance.GetPlayerProperty(player, "STATUS").Equals("BUSY"))
                        {
                            return CoreManager.Instance.chatSettings.busy;
                        }
                    }

                    bool hasDoNotDisturbProp = MMOManager.Instance.PlayerHasProperty(player, "DONOTDISTURB");

                    if (hasDoNotDisturbProp)
                    {
                        if (MMOManager.Instance.GetPlayerProperty(player, "DONOTDISTURB").Equals("1"))
                        {
                            return CoreManager.Instance.chatSettings.busy;
                        }
                    }
                }
            }

            return CoreManager.Instance.chatSettings.online;
        }

        /// <summary>
        /// Called to return the full string of customizated data on a player
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public string[] GetPlayerCustomizationData(IPlayer player)
        {
            string[] dataHash = new string[2];
            dataHash[0] = "CUSTOMIZATIONDATA";
            int count = 0;
            string str = "";

            foreach (var item in player.CustomizationData.Keys)
            {
                if (count < player.CustomizationData.Keys.Count - 1)
                {
                    str += item + "*" + player.CustomizationData[item] + "|";
                }
                else
                {
                    str += item + "*" + player.CustomizationData[item];
                }

                count++;
            }

            dataHash[1] = str;

            return dataHash;
        }


        /// <summary>
        /// Called to play a emote on local player
        /// </summary>
        /// <param name="emoteID"></param>
        public void PlayEmote(int emoteID)
        {
            if (emoteID < 0) return;

            Emotes emote = localPlayer.TransformObject.GetComponent<Emotes>();
            FreezePlayer(true); // we are unfreezing with the animation event.

            if(emote != null)
            {
                emote.ActivateEmote(emoteID);
            }
        }

        public void PlayEmoji(int emojiID)
        {
            if (emojiID < 0) return;

            Emotes emote = localPlayer.TransformObject.GetComponent<Emotes>();

            if (emote != null)
            {
                emote.ActivateEmoji(emojiID);
            }
        }

        public void SetRigistrationData()
        {
            if(string.IsNullOrEmpty(AppManager.Instance.Data.LoginProfileData.username))
            {
                AppManager.Instance.Data.LoginProfileData.username = AppManager.Instance.Data.NickName;
            }

            if (string.IsNullOrEmpty(AppManager.Instance.Data.LoginProfileData.picture_url) && AppManager.Instance.Data.FixedAvatarUsed)
            {
                AppManager.Instance.Data.LoginProfileData.picture_url = AppManager.Instance.Data.FixedAvatarName;
            }

            Hashtable hashData = new Hashtable();
            hashData.Add("PROFILE_USERNAME", AppManager.Instance.Data.LoginProfileData.username);
            hashData.Add("PROFILE_BIRTHNAME", AppManager.Instance.Data.LoginProfileData.name);
            hashData.Add("PROFILE_ABOUT", AppManager.Instance.Data.LoginProfileData.about);
            hashData.Add("PROFILE_PICTURE", AppManager.Instance.Data.LoginProfileData.picture_url);
            UpdatePlayerCustomizationData(hashData);
        }

        /// <summary>
        /// Function called to update the local player customization data
        /// </summary>
        /// <param name="player"></param>
        /// <param name="hash"></param>
        public Hashtable UpdatePlayerCustomizationData(Hashtable hash, bool send = true)
        {
            string dataHash = "";
            int count = 0;

            Hashtable newHash = new Hashtable();

            foreach (var item in hash.Keys)
            {
                if(localPlayer.CustomizationData.ContainsKey(item))
                {
                    localPlayer.CustomizationData[item] = hash[item];
                }
                else
                {
                    localPlayer.CustomizationData.Add(item, hash[item]);
                }

                if(count < hash.Count - 1)
                {
                    dataHash += item + "*" + hash[item] + "|";
                }
                else
                {
                    dataHash += item + "*" + hash[item];
                }

                newHash.Add(item, hash[item]);

                count++;
            }

            newHash.Add("CUSTOMIZATIONDATA", dataHash);

            //need to update player properties if local
            if (send)
            {
                SetPlayerProperty("CUSTOMIZATIONDATA", dataHash);
            }

            if(OnPlayerCustomizationUpdate != null)
            {
                OnPlayerCustomizationUpdate.Invoke(newHash);
            }

            return newHash;
        }

        /// <summary>
        /// Callback for when player enters a room
        /// </summary>
        /// <param name="player"></param>
        public void OnPlayerEnteredRoom(IPlayer player)
        {
            if (player != null) return;

            //set the customization data
            NetworkPlayerCustomizationData(player, MMOManager.Instance.GetPlayerProperties(player));
        }

        /// <summary>
        /// Called to update a remote players Customization Data
        /// </summary>
        /// <param name="player"></param>
        /// <param name="hash"></param>
        public void NetworkPlayerCustomizationData(IPlayer player, Hashtable hash)
        {
            if (player == null) return;

            IPlayer remotePlayer = player;

            if (remotePlayer != null && remotePlayer.IsLocal) return;

            Debug.Log("Player updated customized data: " + player.NickName + "|" + hash.ToString());

            if (remotePlayer != null)
            {
                if (hash.ContainsKey("CUSTOMIZATIONDATA"))
                {
                    string[] split = hash["CUSTOMIZATIONDATA"].ToString().Split('|');

                    for (int i = 0; i < split.Length; i++)
                    {
                        if (string.IsNullOrEmpty(split[i])) continue;

                        string[] data = split[i].Split('*');

                        if (remotePlayer.CustomizationData.ContainsKey(data[0]))
                        {
                            remotePlayer.CustomizationData[data[0]] = data[1];
                        }
                        else
                        {
                            remotePlayer.CustomizationData.Add(data[0], data[1]);
                        }
                    }
                }

                if (OnRemotePlayerCustomizationUpdate != null)
                {
                    OnRemotePlayerCustomizationUpdate.Invoke(player, hash);
                }
            }
        }

        public string GetPlayerURLParams()
        {
            return "walk-" + MainControlSettings.walk + "|" + "run-" + MainControlSettings.run
                    + "|" + "strife-" + MainControlSettings.strife + "|" + "mouse-" + MainControlSettings.mouse + "|" + "controls-" + MainControlSettings.GetControlKeys() 
                    + "|" + "invertX-" + MainControlSettings.invertX + "|" + "invertY-" + MainControlSettings.invertY + "|" + "controllerType-" + MainControlSettings.controllerType;
        }

        public void AddUserProfileData(string key, string data)
        {
            AppManager.Instance.AddUserProfileData(key, data);
        }

        public enum PlayerType { Simple, Advanced, Custom }

        [System.Serializable]
        public class PlayerControlSettings
        {
            public float walk;
            public float run;
            public float strife;
            public float mouse;

            public int highlightOn = 1;
            public int tooltipOn = 1;
            public int nameOn = 1;

            public int invertX = 0;
            public int invertY = 0;

            public int controllerType = 0;

            public List<int> controls;

            public string GetControlKeys()
            {
                string temp = "";

                if(controls != null)
                {
                    for(int i = 0; i < controls.Count; i++)
                    {
                        if(i < controls.Count - 1)
                        {
                            temp += controls[i].ToString() + "+";
                        }
                        else
                        {
                            temp += controls[i].ToString();
                        }
                    }
                }

                return temp;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PlayerManager), true)]
        public class PlayerManager_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraMenuHandler"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("focusUIToogle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("hotspotUIToggle"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("topdownUIToggle"), true);

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

    public enum AvatarBackwardBehaviour {  None, Rotate }

    /// <summary>
    /// Interface implemented for all player controller types
    /// </summary>
    public interface IPlayer
    {
        void ToggleCanMove(bool canMove);

        void ToggleTopDown(bool isOn);

        void ToggleFocus(bool focus);

        void ApplySettings();

        void UpdateAvatar(CustomiseAvatar.Sex sex, AvatarCustomiseSettings settings);

        void UpdateAvatar(string customAvatar);

        void SwitchToThirdPerson(bool thirdPerson);

        void ToggleVisibility(bool isVisible);

        void RotateAvatar(bool forawrd, bool backward);

        void Move(Vector3 vec);

        void PlayWaterSoundEffect(AudioClip clip, bool loop = false);

        GameObject MainProductHolder { get; }

        GameObject MainCamera { get; }

        GameObject ThirdPerson { get; }

        bool IsLocal { get; set; }

        GameObject MainObject { get; }

        Transform TransformObject { get; }

        Vector2 TargetCameraRotation { get; set; }

        bool IsButtonHeldDown { get; set; }

        bool FreezeRotation { get; set; }

        bool FreezePosition { get; set; }

        bool IsInWater { get; set; }

        WaterHandler WaterHandler { get; set; }

        Animator Animation { get; }

        string InteractionKey { get; }

        string ID { get; }

        string NickName { get; }

        float Speed { get; }

        int ActorNumber { get; }

        GameObject ArrowPointer { get; }

        int RotationInput { get; }

        UnityEngine.AI.NavMeshAgent NavMeshAgentScript { get; }

        bool OverrideAnimationHandler { get; set; }

        Hashtable CustomizationData { get; }

        bool IsSprinting { get; }

        int MovementID { get; }

        bool IsMoving { get; }

        string SittingAnimation { get; set; }

        void EnableCameras(bool enable);

        GameObject Avatar { get; }

        bool OverrideAnimations { get; set; }
    }
}