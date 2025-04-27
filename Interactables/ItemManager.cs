using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// Pickup Manager handles 'picking up' 3D objects to view
    /// Want to refactor and change name to 'ItemManager'
    /// </summary>
    public class ItemManager : Singleton<ItemManager>, IRaycaster
    {
        public static ItemManager Instance
        {
            get
            {
                return ((ItemManager)instance);
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

        private bool isHolding = false;
        private PickupItem heldItem;
        private GameObject held3D = null;

        public string UserCheckKey
        {
            get
            {
                return m_userKey;
            }
        }

        private string m_userKey = "USERTYPE";

        /// <summary>
        /// Global access to indicate if player is holding item
        /// </summary>
        public bool IsHolding
        {
            get
            {
                return isHolding;
            }
        }

        private void Awake()
        {
            RaycastManager.Instance.Raycasters.Add(this);
        }

        private void Start()
        {
            MMORoom.Instance.OnPlayerEnteredRoom += OnPlayerJoinedRoom;

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
            if (hit.transform.GetComponent<PickupItem>() || hit.transform.GetComponent<DropPoint>())
            {
                hitObject = hit.transform;
            }
            else
            {
                hitObject = null;
            }

            if (InputManager.Instance.GetMouseButtonUp(0))
            {
                //detect components
                var item = hit.transform.GetComponent<PickupItem>();
                var drop = hit.transform.GetComponent<DropPoint>();

                //will need to check if user can pick up item eventually once networked ITEM has to have uniqueID implemtation

                string user = PlayerManager.Instance.GetLocalPlayer().CustomizationData.ContainsKey(m_userKey) ? PlayerManager.Instance.GetLocalPlayer().CustomizationData[m_userKey].ToString() : "";

                if (item != null)
                {
                    if (!item.CanUserControlThis(user))
                    {
                        return;
                    }

                    if (!isHolding)
                    {
                        //if not holding pick up
                        if (InputManager.Instance.GetMouseButtonUp(0) && !PlayerManager.Instance.GetLocalPlayer().IsButtonHeldDown)
                        {
                            item.Pickup();
                            return;
                        }
                    }
                    else
                    {
                        if (InputManager.Instance.GetMouseButtonUp(0) && !PlayerManager.Instance.GetLocalPlayer().IsButtonHeldDown)
                        {
                            //if holding item, drop item first if not identical, then pickup
                            if (heldItem != null && heldItem != item)
                            {
                                Drop3D();
                            }

                            item.Pickup();
                            return;
                        }
                    }
                }

                if (isHolding && drop != null)
                {
                    //if dropping held item on drop point, make sure the drop point is not occupied by another item
                    if (InputManager.Instance.GetMouseButtonUp(0) && !drop.Occupied && !PlayerManager.Instance.GetLocalPlayer().IsButtonHeldDown)
                    {
                        if (!drop.CanUserControlThis(user))
                        {
                            return;
                        }

                        //place on drop point
                        PlaceCurrent(drop);
                    }
                }
            }
        }

        public void RaycastMiss()
        {

        }

        /// <summary>
        /// Pickup the 3D object (duplicate it and display in front of the camera)
        /// If you want to change the instruction/button name, then add DropPanelOveride.cs on item/product
        /// </summary>
        /// <param name="obj">the item to 'pickup'</param>
        /// <param name="overridePosition">override the position in front of the camera to adjust for large objects etc.</param>
        /// <param name="overrideRotation">override the initial rotaion when picked up</param>
        /// <param name="overrideScale">override the scale of the picked up object</param>
        /// <param name="SpinAxis">the axis on which to spin if it will spin</param>
        /// <param name="shouldSpin">whether the item should spin while held</param>
        public void Pickup3D(GameObject obj)
        {
            if (held3D != null) { return; }

            Debug.Log("Item picked up: " + obj.name);

            //if holding porduct not item, drop product
            if(ProductManager.Instance.isHolding)
            {
                ProductManager.Instance.DropProduct();
            }

            isHolding = true;
            held3D = PickUp(obj);
            heldItem = held3D.GetComponent<PickupItem>();
            PickupItem pItem = held3D.GetComponent<PickupItem>();

            //send this as room event to everyone
            //Photon room change
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("EVENT_TYPE", "ITEMPICKUP");
            dict.Add("I", pItem.ID);
            dict.Add("A", "1");
            dict.Add("P", "");
            dict.Add("H", PlayerManager.Instance.GetLocalPlayer().ActorNumber.ToString());

            MMOManager.Instance.ChangeRoomProperty(pItem.ID, dict);

            DropPanel dropPanel = HUDManager.Instance.GetHUDControlObject("DROP").GetComponentInChildren<DropPanel>(true);
            dropPanel.SetStrings(AppManager.Instance.Settings.playerSettings.showDropInstruction,
                AppManager.Instance.Settings.playerSettings.defaulDropTitle,
                AppManager.Instance.Settings.playerSettings.defaultDropMessage,
                AppManager.Instance.Settings.playerSettings.defaultDropButton);

            //UI
            HUDManager.Instance.ToggleHUDControl("DROP", true);
            HUDManager.Instance.ShowHUDNavigationVisibility(false);
        }

        private GameObject PickUp(GameObject obj)
        {
            //get item script on held item
            var newObj = obj;
            PickupItem pItem = obj.GetComponent<PickupItem>();

            Debug.Log("pickup item" + pItem.ID);

            if (pItem != null)
            {
                //if item was currently on drop point, set local occupied state to false
                if (pItem.CurrentDropPoint != null)
                {
                    pItem.CurrentDropPoint.Occupied = false;
                }

                //physics
                pItem.DestroyRigidbody();

                //check what type if behaviour the picked up item has, or whether the item has already been instantiated
                if (pItem.settings.pickupBehaviour.Equals(PickUpBehaviourType.Instantiate) && !pItem.Instantiated)
                {
                    newObj = Instantiate(obj, GetTransformHolder(pItem.OwnerID));
                    PickupItem tempPItem = newObj.GetComponent<PickupItem>();
                    tempPItem.settings.pickupBehaviour = PickUpBehaviourType.Reparent;
                    tempPItem.Initialise();
                    tempPItem.Instantiated = true;
                }
                else
                {
                    newObj.transform.SetParent(GetTransformHolder(pItem.OwnerID));
                }

                //if third person and local player to not show object
                if (pItem.OwnerID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                {
                    if (PlayerManager.Instance.ThirdPersonCameraActive)
                    {
                        newObj.SetActive(false);
                    }
                }
                else
                {
                    newObj.SetActive(false);
                }
            }

            //set up picked up items transform params
            newObj.transform.localPosition = pItem.OverridePosition;
            newObj.transform.localRotation = Quaternion.Euler(pItem.OverrideRotation);

            float overrideScale = pItem.OverrideScale;
            overrideScale *= obj.transform.localScale.x;
            newObj.transform.localScale = new Vector3(overrideScale, overrideScale, overrideScale);

            //component set up
            if (newObj.GetComponent<Collider>())
            {
                newObj.GetComponent<Collider>().enabled = false;
            }

            if (pItem.settings.spin)
            {
                Spin spinscript = newObj.AddComponent<Spin>();
                spinscript.SpinAxis = pItem.settings.spinAxis;
                spinscript.speed = pItem.settings.spinSpeed;
            }

            return newObj;
        }

        private Transform GetTransformHolder(string owner)
        {
            Transform holder = null;

            if(owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
            {
                holder = PlayerManager.Instance.GetLocalPlayer().MainProductHolder.transform;
            }
            else
            {
                //get player
                IPlayer player = PlayerManager.Instance.GetPlayer(owner);
                holder = player.MainProductHolder.transform;
            }

            return holder;
        }

        /// <summary>
        /// Drop the held item
        /// </summary>
        public void Drop3D()
        {
            if (held3D == null) { return; }

            Debug.Log("Item dropped: " + held3D.name);

            //get item script on held item
            PickupItem pItem = held3D.GetComponent<PickupItem>();

            if(pItem != null)
            {
                Drop(pItem);

                //send this as room event to everyone
                //Photon room change
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("EVENT_TYPE", "ITEMPICKUP");
                dict.Add("I", pItem.ID);
                dict.Add("A", "0");
                dict.Add("P", "");
                dict.Add("H", PlayerManager.Instance.GetLocalPlayer().ActorNumber.ToString());

                MMOManager.Instance.ChangeRoomProperty(pItem.ID, dict);

                pItem.OwnerID = "";
            }

            heldItem = null;
            isHolding = false;

            //UI
            HUDManager.Instance.ToggleHUDControl("DROP", false);
            HUDManager.Instance.ShowHUDNavigationVisibility(true);
            held3D = null;
        }

        public void PlaceItem(PickupItem item, DropPoint drop)
        {
            Place(item, drop);
        }

        public void PlaceCurrent(DropPoint drop)
        {
            if(isHolding && heldItem != null)
            {
                Place(heldItem, drop);

                //send this as room event to everyone
                //Photon room change
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("EVENT_TYPE", "ITEMPICKUP");
                dict.Add("I", heldItem.ID);
                dict.Add("A", "2");
                dict.Add("P", drop.ID);
                dict.Add("H", PlayerManager.Instance.GetLocalPlayer().ActorNumber.ToString());

                MMOManager.Instance.ChangeRoomProperty(heldItem.ID, dict);

                heldItem = null;
                isHolding = false;

                //UI
                HUDManager.Instance.ToggleHUDControl("DROP", false);
                HUDManager.Instance.ShowHUDNavigationVisibility(true);

                held3D = null;
            }
        }

        private void Place(PickupItem item, DropPoint drop)
        {
            //destroy physics
            if (item.GetComponent<Spin>())
            {
                Destroy(item.GetComponent<Spin>());
            }

            item.DestroyRigidbody();

            //get new placement position and drop on point
            if(drop.useGlobalPosition)
            {
                Vector3 newPosition = new Vector3(drop.transform.position.x, drop.transform.position.y + drop.spacing , drop.transform.position.z);
                drop.Drop(item, transform, newPosition, item.OriginRotation, item.OriginScale);

            }
            else
            {
                Vector3 shift = (item.OriginScale / 2);
                Vector3 newPosition = new Vector3(drop.transform.position.x, drop.transform.position.y + shift.y + drop.spacing, drop.transform.position.z);
                drop.Drop(item, transform, newPosition, item.OriginRotation, item.OriginScale);
            }

            drop.Occupied = true;
            item.CurrentDropPoint = drop;

            if (item.GetComponent<Collider>())
            {
                item.GetComponent<Collider>().enabled = true;
            }

            item.gameObject.SetActive(true);
        }

        private void Drop(PickupItem pItem)
        {
            bool dropAtPlayer = false;

            pItem.gameObject.SetActive(true);

            if (pItem != null)
            {
                //set items drop point to null
                pItem.CurrentDropPoint = null;

                //if item has been instantiated drop at player
                if (pItem.Instantiated)
                {
                    dropAtPlayer = true;
                }
                else
                {
                    //drop behaviour is rigidbady then use drop at player
                    if (pItem.settings.dropBehaviour.Equals(DropBehaviourType.UseRigidbody))
                    {
                        dropAtPlayer = true;
                    }
                    else
                    {
                        //else destroy object
                        if (pItem.gameObject.GetComponent<Spin>())
                        {
                            Destroy(pItem.gameObject.GetComponent<Spin>());
                        }

                        pItem.ResetToOrigin();

                        if (pItem.gameObject.GetComponent<Collider>())
                        {
                            pItem.gameObject.GetComponent<Collider>().enabled = true;
                        }
                    }
                }
            }

            if (dropAtPlayer)
            {
                //item drop behaviour is reset, destroy
                if (pItem.settings.dropBehaviour.Equals(DropBehaviourType.Reset))
                {
                    Destroy(pItem.gameObject);
                }
                else
                {
                    //drop item using physics
                    pItem.CurrentDropPoint = null;

                    if (pItem.gameObject.GetComponent<Spin>())
                    {
                        Destroy(pItem.gameObject.GetComponent<Spin>());
                    }

                    pItem.gameObject.transform.SetParent(transform);
                    pItem.AddRigidbody();
                }
            }
        }

        public void NetworkItem(string itemID, string actionID, int actorID, string dropID)
        {
            if (actorID.Equals(PlayerManager.Instance.GetLocalPlayer().ActorNumber)) return;

            Debug.Log("Syncing item [" + itemID + "]");

            //need to get item by ID
            PickupItem[] all = FindObjectsByType<PickupItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            PickupItem item = null;
            string id = itemID.Split('|')[0];

            if(UniqueIDManager.Instance.ReplicatedIDCount.ContainsKey(id.Split('_')[1]))
            {
                //new instantiated item if greater
                if (int.Parse(itemID.Split('|')[1]) <= UniqueIDManager.Instance.ReplicatedIDCount[id.Split('_')[1]])
                {
                    id = itemID;
                }
            }

            for (int i = 0; i < all.Length; i++)
            {
                if(all[i].ID.Equals(id))
                {
                    item = all[i];
                    break;
                }
            }

            if(item != null)
            {
                if (actionID.Equals("1"))
                {
                    item.Initialise();
                    item.OwnerID = PlayerManager.Instance.GetPlayer(actorID).ID;
                    //pickup
                    GameObject newObj = PickUp(item.gameObject);
                    newObj.GetComponentInChildren<PickupItem>(true).ID = itemID;
                    
                }
                else if(actionID.Equals("2"))
                {
                    DropPoint[] allDrops = FindObjectsByType<DropPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    DropPoint drop = null;

                    for (int i = 0; i < allDrops.Length; i++)
                    {
                        if (allDrops[i].ID.Equals(dropID))
                        {
                            drop = allDrops[i];
                            break;
                        }
                    }

                    if(drop != null)
                    {
                        Place(item, drop);
                    }
                }
                else
                {
                    //drop
                    Drop(item);
                }
            }
        }

        public void InstantiateAllRoomItems(string json)
        {
            InstantiatedItemWrapper wrapper = JsonUtility.FromJson<InstantiatedItemWrapper>(json);
            List<PickupItem> all = FindObjectsByType<PickupItem>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
            List<DropPoint> dropPoints = FindObjectsByType<DropPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();

            for(int i = 0; i < wrapper.items.Count; i++)
            {
                if(all.Find(x => x.ID.Equals(wrapper.items[i].id)) == null)
                {
                    //need to get the source item
                    string id = wrapper.items[i].id.Split('|')[0];
                    PickupItem src = all.FirstOrDefault(x => x.ID.Equals(id));

                    if(src != null)
                    {
                        GameObject newObj = Instantiate(src.gameObject, transform);
                        PickupItem tempPItem = newObj.GetComponent<PickupItem>();
                        tempPItem.settings.pickupBehaviour = PickUpBehaviourType.Reparent;
                        tempPItem.Initialise();
                        tempPItem.OwnerID = "";
                        tempPItem.Instantiated = true;

                        DropPoint dPoint = dropPoints.FirstOrDefault(x => x.ID.Equals(wrapper.items[i].dropPointID));
                        tempPItem.CurrentDropPoint = dPoint;

                        if(dPoint)
                        {
                            dPoint.Occupied = true;
                            Vector3 shift = (tempPItem.OriginScale / 2);
                            Vector3 newPosition = new Vector3(dPoint.transform.position.x, dPoint.transform.position.y + shift.y + dPoint.spacing, dPoint.transform.position.z);
                            dPoint.Drop(tempPItem, transform, newPosition, tempPItem.OriginRotation, tempPItem.OriginScale);
                        }
                        else
                        {
                            newObj.transform.localPosition = new Vector3(wrapper.items[i].x, wrapper.items[i].y, wrapper.items[i].z);
                        }

                        newObj.SetActive(true);
                        tempPItem.ID = wrapper.items[i].id;
                    }
                }
            }
        }

        private void OnPlayerJoinedRoom(IPlayer player)
        {
            //if master client send to the new player all instantiated item objects position via RPC
            if(MMOManager.Instance.IsMasterClient())
            {
                InstantiatedItemWrapper wrapper = new InstantiatedItemWrapper();

                PickupItem[] all = FindObjectsByType<PickupItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                for (int i = 0; i < all.Length; i++)
                {
                    string id = all[i].ID.Split('|')[0];

                    if (UniqueIDManager.Instance.ReplicatedIDCount.ContainsKey(id.Split('_')[1]))
                    {
                        if (all[i].GetComponentInParent<IPlayer>() != null)
                        {
                            continue;
                        }

                        InstantiatedItem iItem = new InstantiatedItem();
                        iItem.id = all[i].ID;
                        iItem.x = (float)System.Math.Round(all[i].transform.position.x, 2);
                        iItem.y = (float)System.Math.Round(all[i].transform.position.y, 2);
                        iItem.z = (float)System.Math.Round(all[i].transform.position.z, 2);
                        iItem.dropPointID = (all[i].CurrentDropPoint != null) ? all[i].CurrentDropPoint.ID : "";

                        wrapper.items.Add(iItem);
                    }
                }

                MMOManager.Instance.SendRPC("SetAllInstantiatedItems", (int)MMOManager.RpcTarget.Others, JsonUtility.ToJson(wrapper));
            }
        }

        [System.Serializable]
        private class InstantiatedItemWrapper
        {
            public List<InstantiatedItem> items = new List<InstantiatedItem>();
        }

        [System.Serializable]
        private class InstantiatedItem
        {
            public string id;
            public float x, y, z;
            public string dropPointID;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ItemManager), true)]
        public class ItemManager_Editor : BaseInspectorEditor
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

    public enum PickUpBehaviourType { Instantiate, Reparent }
    public enum DropBehaviourType { Reset , UseRigidbody }
}