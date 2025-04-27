using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class VehicleManager : Singleton<VehicleManager>, IRaycaster
    {
        public static VehicleManager Instance
        {
            get
            {
                return ((VehicleManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Interaction")]
        private float interactionDistance = 5;
        private bool useLocalDistance = true;

        public bool OverrideDistance { get { return useLocalDistance; } }

        [HideInInspector]
        public string DrivingAnimation = "Driving";
        public GameObject CurrentVehicle { get; private set; }

        private List<OccupiedVehcile> m_occupiedVehciles = new List<OccupiedVehcile>();

        public string UserCheckKey
        {
            get
            {
                return m_userKey;
            }
        }

        private string m_userKey = "USERTYPE";

        private void Awake()
        {
            RaycastManager.Instance.Raycasters.Add(this);

            if(m_userKey != null)
            {

            }
        }

        private void Start()
        {
            PlayerControlSettings.ManagerInteraction mInteration = CoreManager.Instance.playerSettings.GetIRaycasterManager(gameObject.name);
            MMORoom.Instance.OnPlayerLeftRoom += OnPlayerLeftRoom;

            MMORoom.Instance.OnRoomReady += SpawnVehicles;

            if (mInteration != null)
            {
                interactionDistance = mInteration.interactionDistance;
                useLocalDistance = mInteration.overrideInteraction;
            }
            else
            {
                useLocalDistance = false;
            }

#if BRANDLAB360_INTERNAL
            UnityEngine.Object prefab = Resources.Load("VehicleInput");

            if (prefab != null)
            {
                GameObject go = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.transform.localEulerAngles = Vector3.zero;
            }
            else
            {
                Debug.Log("Cannot load vehicle input prefab.");
            }
#endif
        }

        private void SpawnVehicles()
        {
            MMORoom.Instance.OnRoomReady -= SpawnVehicles;

            bool isMasterClient = MMOManager.Instance.IsConnected() ? MMOManager.Instance.IsMasterClient() : true;

            if(isMasterClient)
            {
                VehicleSpawn[] vSpawns = FindObjectsByType<VehicleSpawn>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                for (int i = -0; i < vSpawns.Length; i++)
                {
                    if (vSpawns[i].SpawnOnAwake)
                    {
                        vSpawns[i].Spawn(Random.Range(0, 10001).ToString());
                    }
                }
            }
        }

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
                        distance = interactionDistance + Vector3.Distance(PlayerManager.Instance.GetLocalPlayer().TransformObject.position, PlayerManager.Instance.GetLocalPlayer().MainCamera.transform.position);
                    }
                    else
                    {
                        distance = interactionDistance;
                    }
                }
                else
                {
                    //cannot perform door stuff in topdown view
                    return -1;
                }

                return distance;
            }
        }

        public void AddMMOTransform(GameObject go)
        {
            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online))
            {
                MMOTransform sync = go.GetComponent<MMOTransform>();

                if (sync == null)
                {
                    go.AddComponent<MMOTransform>();
                }
            }
            else
            {
                MMOTransform sync = go.GetComponent<MMOTransform>();

                if (sync != null)
                {
                    Destroy(sync);
                }
            }
        }

        /// <summary>
        /// Set CurrentVehicle without raycasting
        /// </summary>
        /// <param name="vehicle"></param>
        public void SetVehicle(Transform vehicle)
        {
            OccupiedVehcile ov = m_occupiedVehciles.FirstOrDefault(x => x.vehicle.Equals(vehicle.gameObject));

            if (ov == null)
            {
                CurrentVehicle = vehicle.gameObject;
            }
        }

        /// <summary>
        /// For raycaster
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="hitObject"></param>
        public void RaycastHit(RaycastHit hit, out Transform hitObject)
        {
            hitObject = null;

            if (hit.transform != null)
            {
                hitObject = (hit.transform.gameObject.tag == "Vehicle") ? hit.transform : null;

                if (hitObject != null && InputManager.Instance.GetMouseButtonUp(0))
                {
                    OccupiedVehcile ov = m_occupiedVehciles.FirstOrDefault(x => x.vehicle.Equals(hit.transform.gameObject));

                    if(ov == null)
                    {
                        CurrentVehicle = hitObject.gameObject;
                        EnterVehicle();
                    }
                }
            }
        }

        /// <summary>
        /// when raycast missed
        /// </summary>
        public void RaycastMiss()
        {

        }

        /// <summary>
        /// Called to enter a player to a vehicle
        /// </summary>
        public void EnterVehicle()
        {
            if(CurrentVehicle != null)
            {
                //disable ray casts
                RaycastManager.Instance.CastRay = false;
                PlayerManager.Instance.FreezePlayer(true);
                PlayerManager.Instance.GetLocalPlayer().TransformObject.GetComponent<Collider>().enabled = false;

                if (PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript != null)
                {
                    PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript.enabled = false;
                }

                MMOTransform mt = CurrentVehicle.GetComponent<MMOTransform>();

                if (mt != null)
                {
                    mt.RequestOwnership(PlayerManager.Instance.GetLocalPlayer().ID);
                }

                //send room property photon change - enter vehicle
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("EVENT_TYPE", "VEHICLE");
                data.Add("V", CurrentVehicle.gameObject.name.ToString());
                data.Add("O", "1");
                data.Add("P", PlayerManager.Instance.GetLocalPlayer().ActorNumber.ToString());

                string message = "#EVT# " + "Entered vechicle " + CurrentVehicle.name.Replace("(Clone)", "");
                MMOChat.Instance.SendChatMessage("All", message);

                MMOManager.Instance.ChangeRoomProperty(CurrentVehicle.name, data);

                AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Enter, "Vechile Entered " + CurrentVehicle.gameObject.name);

                HUDManager.Instance.Fade(FadeOutIn.FadeAction.Out_In, OnEnterCallback, null, 0.5f);
            }
        }

        /// <summary>
        /// Called to exit the player from a vehicle
        /// </summary>
        public void ExitVehicle()
        {
            if (CurrentVehicle != null)
            {
                HUDManager.Instance.Fade(FadeOutIn.FadeAction.Out_In, OnExitCallback, null, 0.5f);

                MMOTransform mt = CurrentVehicle.GetComponent<MMOTransform>();

                if (mt != null)
                {
                    mt.SyncTransferOwnership("");
                }

                //send room property photon change - exit vehicle
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("EVENT_TYPE", "VEHICLE");
                data.Add("V", CurrentVehicle.gameObject.name.ToString());
                data.Add("O", "0");
                data.Add("P", PlayerManager.Instance.GetLocalPlayer().ActorNumber.ToString());

                string message = "#EVT# " + "Exited vechicle " + CurrentVehicle.name.Replace("(Clone)", "");
                MMOChat.Instance.SendChatMessage("All", message);

                MMOManager.Instance.ChangeRoomProperty(CurrentVehicle.name, data);
            }
        }

        /// <summary>
        /// Called to switch the camera on the vehicle
        /// </summary>
        public void SwitchCamera()
        {
            if(CurrentVehicle && AppManager.IsCreated)
            {
                CurrentVehicle.SendMessage("SwitchCamera");
            }
        }

        /// <summary>
        /// Callback for when entering the vehicle
        /// </summary>
        private void OnEnterCallback()
        {
            if (CurrentVehicle != null)
            {
                m_occupiedVehciles.Add(new OccupiedVehcile(PlayerManager.Instance.GetLocalPlayer(), CurrentVehicle));

                PlayerManager.Instance.GetLocalPlayer().MainCamera.SetActive(false);

                CurrentVehicle.SendMessage("OnClick", PlayerManager.Instance.GetLocalPlayer().TransformObject);

                //UI
                HUDManager.Instance.ShowHUDNavigationVisibility(false);

                if(AppManager.Instance.Data.IsMobile)
                {
                    NavigationManager.Instance.ToggleJoystick(true);
                }
                else
                {
                    NavigationManager.Instance.ToggleJoystick(false);
                }
                
                MMORoom.Instance.ToggleLocalProfileInteraction(false);

                PlayerManager.Instance.GetLocalPlayer().Animation.SetBool("Moved", false);
                PlayerManager.Instance.GetLocalPlayer().Animation.SetBool(DrivingAnimation, true);

                HUDManager.Instance.ToggleHUDControl("VEHICLE_CONTROL", true);
            }
        }

        /// <summary>
        /// Callback for when exiting a vehicle
        /// </summary>
        private void OnExitCallback()
        {
            if (CurrentVehicle != null)
            {
                OccupiedVehcile ov = m_occupiedVehciles.FirstOrDefault(x => x.vehicle.Equals(CurrentVehicle));

                if (ov != null)
                {
                    m_occupiedVehciles.Remove(ov);
                }

                CurrentVehicle.SendMessage("Exit");
                CurrentVehicle = null;

                //enable ray casts
                RaycastManager.Instance.CastRay = true;

                PlayerManager.Instance.GetLocalPlayer().TransformObject.GetComponent<Collider>().enabled = true;
                PlayerManager.Instance.FreezePlayer(false);
                PlayerManager.Instance.GetLocalPlayer().MainCamera.SetActive(true);

                //UI
                HUDManager.Instance.ShowHUDNavigationVisibility(true);
                NavigationManager.Instance.ToggleJoystick(true);
                MMORoom.Instance.ToggleLocalProfileInteraction(true);

                PlayerManager.Instance.GetLocalPlayer().Animation.SetBool(DrivingAnimation, false);
                PlayerManager.Instance.GetLocalPlayer().Animation.SetBool("Moved", true);

                HUDManager.Instance.ToggleHUDControl("VEHICLE_CONTROL", false);

                if (PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript != null)
                {
                    PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript.enabled = true;
                }
            }
        }

        public void NetworkVechicle(string GO, int actorID, string occupied)
        {
            var views = FindObjectsByType<MMOTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var player = PlayerManager.Instance.GetPlayer(actorID);

            if(player != null && !player.IsLocal)
            {
                foreach (var view in views)
                {
                    if(view.gameObject.name.Equals(GO))
                    {
                        if (int.Parse(occupied) < 1)
                        {
                            OccupiedVehcile ov = m_occupiedVehciles.FirstOrDefault(x => x.player.ID.Equals(player.ID));

                            if (ov != null)
                            {
                                m_occupiedVehciles.Remove(ov);

                                //need to network the vehicle with this player
                                view.gameObject.SendMessage("NetworkDriver", player.TransformObject);

                                //set networkplayer animator to play idle
                                MMOPlayer nPlayer = player.MainObject.GetComponent<MMOPlayer>();

                                //ani
                                nPlayer.animator.SetBool(DrivingAnimation, false);
                                nPlayer.animator.SetBool("Moved", true);

                                nPlayer.FreezePosition = false;
                                nPlayer.FreezeRotation = false;

                                if (nPlayer.NavMeshAgentUsed)
                                {
                                    nPlayer.NavMeshAgentUsed.enabled = true;
                                }
                            }
                        }
                        else
                        {
                            OccupiedVehcile ov = m_occupiedVehciles.FirstOrDefault(x => x.vehicle.Equals(CurrentVehicle));

                            if (ov == null)
                            {
                                m_occupiedVehciles.Add(new OccupiedVehcile(player, view.gameObject));

                                MMOPlayer nPlayer = player.MainObject.GetComponent<MMOPlayer>();
                                nPlayer.FreezePosition = true;
                                nPlayer.FreezeRotation = true;

                                if (nPlayer.NavMeshAgentUsed)
                                {
                                    nPlayer.NavMeshAgentUsed.isStopped = true;
                                    nPlayer.NavMeshAgentUsed.enabled = false;
                                }

                                //need to network the vehicle with this player
                                view.gameObject.SendMessage("NetworkDriver", player.TransformObject);

                                nPlayer.animator.SetBool("Moved", false);
                                nPlayer.animator.SetBool(DrivingAnimation, true);
                            }
                        }

                        break;
                    }
                }
            }
        }

        public bool HasPlayerEntertedVehcile(string playerID)
        {
            return m_occupiedVehciles.FirstOrDefault(x => x.player.ID.Equals(playerID)) != null;
        }

        public void MoveCurrentVehicle(Vector3 direction)
        {
            if(CurrentVehicle != null)
            {
                CurrentVehicle.SendMessage("MoveVehicle", direction);
            }
        }

        public void ApplyHandbrakeToCurrentVehicle(bool apply)
        {
            if (CurrentVehicle != null)
            {
                CurrentVehicle.SendMessage("ApplyHandbrake", apply);
            }
        }

        private void OnPlayerLeftRoom(IPlayer player)
        {
            Debug.Log(player.ID);

            OccupiedVehcile ov = m_occupiedVehciles.FirstOrDefault(x => x.player.ID.Equals(player.ID));

            if (ov != null)
            {
                //need to network the vehicle with this player
                ov.vehicle.SendMessage("Exit");
                m_occupiedVehciles.Remove(ov);
            }
        }

        [System.Serializable]
        private class OccupiedVehcile
        {
            public IPlayer player;
            public GameObject vehicle;

            public OccupiedVehcile(IPlayer p, GameObject go)
            {
                player = p;
                vehicle = go;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(VehicleManager), true)]
        public class VehicleManager_Editor : BaseInspectorEditor
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
