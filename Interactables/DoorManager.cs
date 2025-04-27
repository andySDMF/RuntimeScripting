using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class DoorManager : Singleton<DoorManager>, IRaycaster
    {
        public static DoorManager Instance
        {
            get
            {
                return ((DoorManager)instance);
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

        public void RaycastHit(RaycastHit hit, out Transform hitObject)
        {
            Door dr = hit.transform.GetComponent<Door>();

            if (dr != null)
            {
                if(!dr.RaycastIgnored)
                {
                    hitObject = hit.transform;
                }
                else
                {
                    hitObject = null;
                }
            }
            else
            {
                hitObject = null;
            }

            if (InputManager.Instance.GetMouseButtonUp(0))
            {
                Door door = hit.transform.GetComponent<Door>();

                if (door)
                {
                    if (!door.RaycastIgnored)
                    {
                        string user = PlayerManager.Instance.GetLocalPlayer().CustomizationData.ContainsKey(m_userKey) ? PlayerManager.Instance.GetLocalPlayer().CustomizationData[m_userKey].ToString() : "";

                        if (door.CanUserControlThis(user))
                        {
                            if (door.IsOpen)
                            {
                                Debug.Log("DOOR: Close " + door.ID);

                                door.Close(true);
                            }
                            else
                            {
                                Debug.Log("DOOR: Open " + door.ID);

                                door.Open(true);
                            }
                        }
                    }
                }
            }
        }

        public void RaycastMiss()
        {

        }

        public void Start()
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
        }

        /// <summary>
        /// Called upon a door being networked
        /// </summary>
        /// <param name="id"></param>
        /// <param name="state"></param>
        public void NetworkDoor(string id, bool state)
        {
            Door[] all = FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Debug.Log("NetworkDoor: " + id + "|" + state);

            for (int i = 0; i < all.Length; i++)
            {
                //match id
                if (all[i].ID.Equals(id))
                {
                    if(!all[i].IsOpen.Equals(state))
                    {
                        all[i].IsOpen = state;
                    }

                    break;
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(DoorManager), true)]
        public class DoorManager_Editor : BaseInspectorEditor
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
