using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// Navigation Manager handles the type of navigation used in the scene 
    /// </summary>
    public class NavigationManager : Singleton<NavigationManager>, IRaycaster
    {
        [Header("Interaction")]
        private float interactionDistance = 15;
        private bool useLocalDistance = true;
        
        [Header("Navmesh")]
        private TeleportType teleportType = TeleportType.Points;
        private NavMeshContainer[] navMeshContainers;

        private bool enableTeleportOnTopdown = false;
        private Coroutine m_navMeshTeleportProcess;

        public bool OverrideDistance { get { return useLocalDistance; } }

        public string UserCheckKey
        {
            get
            {
                return m_userKey;
            }
        }

        private string m_userKey = "NAV";

        public static NavigationManager Instance
        {
            get
            {
                return ((NavigationManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        public NavigationMode Mode
        {
            get
            {
                return CoreManager.Instance.IsMobile ? NavigationMode.Mobile : NavigationMode.Desktop;
            }
            set
            {
                SwitchMode(value);
            }
        }

        public TeleportType TeleportInteractionType
        {
            get
            {
                return teleportType;
            }
        }

        private TeleportPoint[] teleportPoints;

        private void Awake()
        {
            //subscribe to web client JSON recieved event
            // WebclientManager.Instance.WebClientListener += SetNavigationMode;

            //CastRay = true;
            RaycastManager.OnPointerOutsideOfViewport += ResetCursor;
            RaycastManager.OnPointerOverUI += ResetCursor;
            RaycastManager.Instance.Raycasters.Add(this);

            navMeshContainers = FindObjectsByType<NavMeshContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private void Start()
        {
            PlayerControlSettings.ManagerInteraction mInteration = CoreManager.Instance.playerSettings.GetIRaycasterManager(gameObject.name);

            if (mInteration != null)
            {
                interactionDistance = mInteration.interactionDistance;
                useLocalDistance = mInteration.overrideInteraction;
            }
            else
            {
                useLocalDistance = false;
            }

            PlayerControlSettings.TeleportSetting teleportScene = CoreManager.Instance.playerSettings.GetTeleportScene(gameObject.scene.name);

            if(CoreManager.Instance.playerSettings.teleportConfig.Equals(PlayerControlSettings.TeleportConfig.Scenes))
            {
                if (teleportScene != null)
                {
                    teleportType = teleportScene.teleportType;
                    enableTeleportOnTopdown = teleportScene.enableTeleportOnTopdown;
                }
                else
                {
                    Debug.Log("Teleport Scene could not be found. Setting Teleport Mode to Ignore [" + gameObject.scene.name + "]");
                    teleportType = TeleportType.Ignore;
                }
            }
            else
            {
                teleportType = CoreManager.Instance.playerSettings.teleportType;
                enableTeleportOnTopdown = CoreManager.Instance.playerSettings.enableTeleportOnTopdown;
            }

            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Offline))
            {
                Begin();
            }
        }

        private void OnDestroy()
        {
            RaycastManager.OnPointerOutsideOfViewport -= ResetCursor;
            RaycastManager.OnPointerOverUI -= ResetCursor;
        }

        public float Distance
        {
            get
            {
                float distance = interactionDistance;

                if(MapManager.Instance.TopDownViewActive)
                {
                    distance = 5000;
                }

                return distance;
            }
        }

        public void RaycastHit(RaycastHit hit, out Transform hitObject)
        {
            if(hit.transform.GetComponent<ITeleport>() != null)
            {
                hitObject = hit.transform;
            }
            else
            {
                hitObject = null;
            }
           
            if (teleportType.Equals(TeleportType.Points))
            {
                if (InputManager.Instance.GetMouseButtonUp(0) && InputManager.Instance.CheckWithinViewport())
                {
                    //if hit teleport, send player target position
                    if (hit.transform.GetComponent<ITeleport>() != null && PlayerManager.Instance.GetLocalPlayer().TransformObject != null)
                    {
                        Vector3 target;
                        target = new Vector3(hit.point.x, PlayerManager.Instance.GetLocalPlayer().TransformObject.position.y, hit.point.z);
                        hit.transform.GetComponent<ITeleport>().Teleport(target);
                        return;
                    }
                }
            }
        }

        public void RaycastMiss()
        {

        }

        private void ResetCursor()
        {

        }

        public void Begin()
        {
            teleportPoints = FindObjectsByType<TeleportPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            //if simulator is present we can load normally
            WebClientSimulator webclientSimulator = FindFirstObjectByType<WebClientSimulator>();

            //ensure that the correct mode is set up if not using webclient
            if ((CoreManager.Instance.projectSettings.streamingMode.Equals(WebClientMode.None) || Application.isEditor) && webclientSimulator == null)
            {
                SetNavigationMode(AppManager.Instance.Data.WebClientResponce);
            }
            else
            {
                SetNavigationMode(AppManager.Instance.Data.WebClientResponce);
            }
        }

        /// <summary>
        /// Call back when connected to webclient room
        /// </summary>
        /// <param name="responce"></param>
        public void SetNavigationMode(string json)
        {
            var response = JsonUtility.FromJson<StartedResponse>(json);

            Debug.Log("Setting Navigation Mode:" + json);

            if (response != null)
            {
                //set up mode depending on responce
                //if teleport defined then ignore
                if (response.isMobile)
                {
                    SwitchMode(NavigationMode.Mobile);
                }
                else
                {
                    SwitchMode(NavigationMode.Desktop);
                }
            }
            else
            {
                SwitchMode(NavigationMode.Desktop);
            }

            ToggleTeleport(false);
        }

        /// <summary>
        /// Switches the navagation mode
        /// </summary>
        private void SwitchMode(NavigationMode mode)
        {
            switch(mode)
            {
                case NavigationMode.Mobile:

                    ToggleJoystick(true);

                    break;
                default:

                    ToggleJoystick(false);

                    break;
            }
        }

        /// <summary>
        /// called to teleport the local player on the navmesh
        /// </summary>
        /// <param name="destination"></param>
        public void NavMeshTeleport(Vector3 destination)
        {
            PlayerManager.Instance.IsTeleporting = true;

            if(m_navMeshTeleportProcess != null)
            {
                StopCoroutine(m_navMeshTeleportProcess);
            }

            m_navMeshTeleportProcess = null;

            if(AppManager.Instance.Settings.playerSettings.createNavMeshAgent)
            {
                PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript.isStopped = false;
                PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript.destination = destination;
            }

            //send to other players
            Quaternion lookRotation = Quaternion.LookRotation(destination - PlayerManager.Instance.GetLocalPlayer().TransformObject.position);

            bool jump = CoreManager.Instance.playerSettings.teleportMovement.Equals(TeleportMode.Jump) ? true : false;

            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online))
            {
                PlayerManager.Instance.GetLocalPlayer().TransformObject.GetComponent<MMOPlayerSync>().SendPositionAndRotationAtFrequencyNow(destination, lookRotation, jump, true);
            }

            if(jump)
            {
                JumpToNavMeshPosition(destination);
            }
            else
            {
                m_navMeshTeleportProcess = StartCoroutine(CalculateNavMeshDestination(destination));
            }
        }

        /// <summary>
        /// Called locally to calculate the remaining distance on navmesh
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        private IEnumerator CalculateNavMeshDestination(Vector3 destination)
        {
            PlayerManager.Instance.FreezePlayer(true);
            PlayerManager.Instance.GetLocalPlayer().ThirdPerson.GetComponent<CameraThirdPerson>().ResetActiveCameraControl();
            PlayerManager.Instance.GetLocalPlayer().OverrideAnimationHandler = true;
            PlayerManager.Instance.GetLocalPlayer().Animation.SetBool("Moved", true);
            PlayerManager.Instance.GetLocalPlayer().Animation.SetFloat("MovedVal", 0);

            if (AppManager.Instance.Settings.playerSettings.createNavMeshAgent)
            {
                while (!PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript.hasPath)
                {
                    if (CheckInput())
                    {
                        break;
                    }

                    yield return null;
                }

                while (PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript.remainingDistance >= 0.1f)
                {
                    if (CheckInput())
                    {
                        break;
                    }

                    //if orientation ensure the player rotation matches this transform
                    Quaternion lookRotation = Quaternion.LookRotation(destination - PlayerManager.Instance.GetLocalPlayer().TransformObject.position);
                    PlayerManager.Instance.GetLocalPlayer().TransformObject.rotation = Quaternion.Slerp(PlayerManager.Instance.GetLocalPlayer().TransformObject.rotation, lookRotation, Time.deltaTime);

                    yield return null;
                }

                PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript.isStopped = true;
            }
            else
            {
                Vector3 target;
                target = new Vector3(destination.x, PlayerManager.Instance.GetLocalPlayer().TransformObject.position.y, destination.z);

                while (Vector3.Distance(PlayerManager.Instance.GetLocalPlayer().TransformObject.position, destination) > 0.1f)
                {
                    //if input read then release from loop
                    if (CheckInput())
                    {
                        break;
                    }

                    PlayerManager.Instance.GetLocalPlayer().TransformObject.position = Vector3.MoveTowards(PlayerManager.Instance.GetLocalPlayer().TransformObject.position, target, Time.deltaTime * CoreManager.Instance.playerSettings.walkSpeed);

                    //if orientation ensure the player rotation matches this transform
                    Quaternion lookRotation = Quaternion.LookRotation(target - PlayerManager.Instance.GetLocalPlayer().TransformObject.position);
                    PlayerManager.Instance.GetLocalPlayer().TransformObject.rotation = Quaternion.Slerp(PlayerManager.Instance.GetLocalPlayer().TransformObject.rotation, lookRotation, Time.deltaTime);

                    yield return null;
                }
            }
            

            Vector2 vec = new Vector3(PlayerManager.Instance.GetLocalPlayer().MainCamera.GetComponent<Transform>().localEulerAngles.x * -1, PlayerManager.Instance.GetLocalPlayer().TransformObject.localEulerAngles.y);
            PlayerManager.Instance.GetLocalPlayer().TargetCameraRotation = vec;
            PlayerManager.Instance.GetLocalPlayer().Animation.SetBool("Moved", false);
            PlayerManager.Instance.GetLocalPlayer().OverrideAnimationHandler = false;
            PlayerManager.Instance.FreezePlayer(false);

            PlayerManager.Instance.IsTeleporting = false;
        }

        /// <summary>
        /// Instantly jump player to new position on nav mesh
        /// </summary>
        private void JumpToNavMeshPosition(Vector3 destination)
        {
            Vector2 vec = new Vector3(PlayerManager.Instance.GetLocalPlayer().MainCamera.GetComponent<Transform>().localEulerAngles.x * -1, PlayerManager.Instance.GetLocalPlayer().TransformObject.localEulerAngles.y);
            PlayerManager.Instance.GetLocalPlayer().TargetCameraRotation = vec;
            PlayerManager.Instance.GetLocalPlayer().TransformObject.position = new Vector3(destination.x, PlayerManager.Instance.GetLocalPlayer().TransformObject.position.y, destination.z);
        }

        /// <summary>
        /// Check any input
        /// </summary>
        /// <returns></returns>
        private bool CheckInput()
        {
            //if input read then release from loop
            if (InputManager.Instance.AnyKeyHeldDown() || InputManager.Instance.AnyMouseButtonDown())
            {
                if (PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript != null)
                {
                    PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript.isStopped = true;
                }
     
                PlayerManager.Instance.FreezePlayer(false);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Directly toggle the visibility of the joystick
        /// </summary>
        /// <param name="isOn"></param>
        public void ToggleJoystick(bool isOn)
        {
            if (Mode.Equals(NavigationMode.Desktop)) return;

            HUDManager.Instance.ToggleHUDControl("MOBILE_JOYSTICK", isOn);
        }

        /// <summary>
        /// Toggles the Teleport Nav Mesh Renderer on/off
        /// </summary>
        /// <param name="isOn"></param>
        public void ToggleNavMeshVisibility(bool isOn)
        {
            if (navMeshContainers.Length > 0)
            {
                for(int i = 0; i < navMeshContainers.Length; i++)
                {
                    Renderer[] rend = navMeshContainers[i].GetComponentsInChildren<Renderer>(true);

                    for (int j = 0; j < rend.Length; j++)
                    {
                        rend[j].enabled = isOn;
                    }
                }
            }
        }

        /// <summary>
        /// Toggles the state of the Teleport objects depending on the type of teleport navigation
        /// </summary>
        /// <param name="isOn"></param>
        public void ToggleTeleport(bool isOn)
        {
            bool state = (isOn) ? (teleportType.Equals(TeleportType.Points)) ? enableTeleportOnTopdown : false : teleportType.Equals(TeleportType.Points);
   
            for (int i = 0; i < teleportPoints.Length; i++)
            {
                teleportPoints[i].gameObject.SetActive(state);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(NavigationManager), true)]
        public class NavigationManager_Editor : BaseInspectorEditor
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

    public enum TeleportMode { Jump, Tween }
    public enum TeleportType { Ignore, Points }

    /// <summary>
    /// Intercae used for all Teleport Objects
    /// </summary>
    public interface ITeleport
    {
        void Teleport(Vector3 v);
    }
}