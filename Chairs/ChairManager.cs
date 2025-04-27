using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ChairManager : Singleton<ChairManager>, IRaycaster
    {
        public static ChairManager Instance
        {
            get
            {
                return ((ChairManager)instance);
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

        [Header("Conference")]
        private ConferenceContentUpload[] conferenceUploaders;
        private ConferenceContentUploadList conferenceUploadList;

        private bool fadeOutIn = true;
        private float fadePauseTime = 0.5f;

        private List<IChairObject> m_allIChairObjects = new List<IChairObject>();

        public List<IChairObject> AllIChairObjects
        {
            get
            {
                return m_allIChairObjects;
            }
        }


        public void AddIChairObject(IChairObject obj)
        {
            m_allIChairObjects.Add(obj);
        }

        public string UserCheckKey
        {
            get
            {
                return m_userKey;
            }
        }

        private string m_userKey = "USERTYPE";

        /// <summary>
        /// Action called if chair group is full (could be UI pop up)
        /// </summary>
        public System.Action ChairGroupOccupiedEvent { get; set; }
        /// <summary>
        /// Action called if chair is occupied (could be UI pop up)
        /// </summary>
        public System.Action ChairOccupiedEvent { get; set; }

        private IChairObject occupiedChair;
        private List<string> m_playersOccupyingChairs = new List<string>();
        private IChairObject chairFound;

        public IChairObject OccupiedChairByPlayer
        {
            get
            {
                return occupiedChair;
            }
        }

        private void Awake()
        {
            RaycastManager.OnPointerOutsideOfViewport += ResetHighlight;
            RaycastManager.OnPointerOverUI += ResetHighlight;
            RaycastManager.Instance.Raycasters.Add(this);

            HUDManager.Instance.OnCustomSetupComplete += GetUIReferences;
        }

        private void Start()
        {
            fadeOutIn = CoreManager.Instance.playerSettings.chairFadeOutIn;
            fadePauseTime = CoreManager.Instance.playerSettings.chairFadePauseTime;

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

        private void OnDestroy()
        {
            RaycastManager.OnPointerOutsideOfViewport -= ResetHighlight;
            RaycastManager.OnPointerOverUI -= ResetHighlight;
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

        public void RaycastHit(RaycastHit hit, out Transform hitObject)
        {
            chairFound = hit.transform.GetComponent<IChairObject>();

            //check parent as collider could be on child objects
            if (chairFound == null)
            {
                if (hit.transform.GetComponent<Lock>() == null)
                {
                    chairFound = hit.transform.GetComponentInParent<IChairObject>();
                }
            }

            if (chairFound != null)
            {
                hitObject = chairFound.GO.transform;
            }
            else
            {
                hitObject = null;
            }

            if (InputManager.Instance.GetMouseButtonUp(0))
            {
                if (chairFound != null)
                {
                    string user = PlayerManager.Instance.GetLocalPlayer().CustomizationData.ContainsKey(m_userKey) ? PlayerManager.Instance.GetLocalPlayer().CustomizationData[m_userKey].ToString() : "";

                    if (chairFound.CanUserControl(user))
                    {
                        //first check chair group is not full
                        if (!chairFound.Group.IsOccupied)
                        {
                            //is chair occupied
                            if (!chairFound.ChairOccupied)
                            {
                                if (!chairFound.Group.useTrigger)
                                    OccupyChiar(chairFound, true);
                            }
                            else
                            {
                                //chair occupied perform action
                                if (ChairOccupiedEvent != null)
                                {
                                    ChairOccupiedEvent.Invoke();
                                }
                            }
                        }
                        else
                        {
                            //group is full perfrom action
                            if (ChairGroupOccupiedEvent != null)
                            {
                                ChairGroupOccupiedEvent.Invoke();
                            }
                        }
                    }
                }
            }
        }

        public void RaycastMiss()
        {
            chairFound = null;
        }

        private void ResetHighlight()
        {
            chairFound = null;
        }

        /// <summary>
        /// External call to occupy a chair on this local player
        /// </summary>
        /// <param name="chair"></param>
        /// <param name="occupy"></param>
        public void OccupyChiar(IChairObject chair, bool occupy)
        {
            if (occupy)
            {
                if (CoreManager.Instance.IsOffline && chair.Group is ConferenceChairGroup)
                {
                    OfflineManager.Instance.ShowOfflineMessage();
                    return;
                }

                if (chair.ChairLock != null && chair.ChairLock.gameObject.activeInHierarchy)
                {
                    if (chair.Group != null)
                    {
                        if (chair.Group.chairLock == null)
                        {
                            if (chair.ChairLock != null && chair.ChairLock.IsLocked)
                            {
                                return;
                            }
                        }
                        else
                        {
                            if (chair.Group.chairLock.IsLocked)
                            {
                                return;
                            }
                        }
                    }
                }
                else
                {
                    if (chair.Group != null)
                    {
                        if (chair.Group.chairLock != null)
                        {
                            if (chair.Group.chairLock.IsLocked)
                            {
                                return;
                            }
                        }
                    }
                }

                //check if player is on call, if so disconnect
                if (MMOChat.Instance.OnCall)
                {
                    MMOChat.Instance.EndVoiceCall(MMOChat.Instance.CurrentCallID, true);
                }

                //turn off phone
                MMOChat.Instance.HideChat();

                //disable ray casts
                RaycastManager.Instance.CastRay = false;

                //freeze player
                PlayerManager.Instance.FreezePlayer(true);

                //occupy chair
                occupiedChair = chair;

                //perform chair join action
                if (fadeOutIn)
                {
                    HUDManager.Instance.Fade(FadeOutIn.FadeAction.Out_In, JoinChair, OnJoinedChairGroup, fadePauseTime);
                }
                else
                {
                    JoinChair();
                }
            }
            else
            {
                LeaveChairGroup();
            }
        }

        /// <summary>
        /// Add/Remove player ID to the global cache of all players occupying chairs
        /// </summary>
        /// <param name="add"></param>
        /// <param name="playerID"></param>
        public void AmendPlayerToGlobalChairOccupancy(bool add, string playerID)
        {
            if (add)
            {
                if (!m_playersOccupyingChairs.Contains(playerID))
                {
                    m_playersOccupyingChairs.Add(playerID);
                }
            }
            else
            {
                if (m_playersOccupyingChairs.Contains(playerID))
                {
                    m_playersOccupyingChairs.Remove(playerID);
                }
            }
        }

        /// <summary>
        /// Returns true if player is occupying a chair
        /// </summary>
        /// <param name="playerID"></param>
        /// <returns></returns>
        public bool HasPlayerOccupiedChair(string playerID)
        {
            return m_playersOccupyingChairs.Contains(playerID);
        }

        public IChairObject GetChairFromOccupiedPlayer(string playerID)
        {
            if(HasPlayerOccupiedChair(playerID))
            {
                for(int i = 0; i < m_allIChairObjects.Count; i++)
                {
                    if(!string.IsNullOrEmpty(m_allIChairObjects[i].OccupantID))
                    {
                        if (m_allIChairObjects[i].OccupantID.Contains(playerID))
                        {
                            return m_allIChairObjects[i];
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Action used during joing chair in the the fade out if applicable
        /// </summary>
        private void JoinChair()
        {
            //UI
            HUDManager.Instance.ShowHUDNavigationVisibility(false);
            HotspotManager.Instance.HotspotToggle.isOn = false;
            NavigationManager.Instance.ToggleJoystick(false);
            MMORoom.Instance.ToggleLocalProfileInteraction(false);

            Debug.Log("Joining Chair: " + occupiedChair.IDRef);

            RaycastManager.Instance.DisplayCursor = false;

            occupiedChair.MainInteraface.Join(PlayerManager.Instance.GetLocalPlayer());

            string chairGroupName = string.IsNullOrEmpty(occupiedChair.Group.GroupName) ? occupiedChair.Group.ID : occupiedChair.Group.GroupName;
            string message = occupiedChair.Group is ConferenceChairGroup ? " Joined conference " : " Joined ";
            MMOChat.Instance.SendChatMessage("All", "#EVT# " + message + chairGroupName);

            if (occupiedChair.Group is ConferenceChairGroup)
            {
                conferenceUploadList.ID = ((ConferenceChairGroup)occupiedChair.Group).ID;

                for (int i = 0; i < conferenceUploaders.Length; i++)
                {
                    conferenceUploaders[i].ID = ((ConferenceChairGroup)occupiedChair.Group).ID;

                    if (!string.IsNullOrEmpty(((ConferenceChairGroup)occupiedChair.Group).CurrentUploadedFile))
                    {
                        ContentsManager.Instance.NetworkConferenceScreen(occupiedChair.Group.ID, ((ConferenceChairGroup)occupiedChair.Group).Owner.ID, "1", ((ConferenceChairGroup)occupiedChair.Group).CurrentUploadedFile);
                    }
                }

                if (((ConferenceChairGroup)occupiedChair.Group).Owner != null && ((ConferenceChairGroup)occupiedChair.Group).Owner.ID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                {
                    HUDManager.Instance.ToggleHUDControl("CONFERENCE_OWNER", true);
                }
                else
                {
                    HUDManager.Instance.ToggleHUDControl("LEAVE_CHAIR", true);
                }
            }
            else
            {
                HUDManager.Instance.ToggleHUDControl("LEAVE_CHAIR", true);
            }
        }

        /// <summary>
        /// Action used during leaving chair in the fade out if applicable
        /// </summary>
        private void LeaveChair()
        {
            IChairObject temp = occupiedChair;

            Debug.Log("Leaving Chair: " + occupiedChair.IDRef);

            occupiedChair.MainInteraface.Leave(PlayerManager.Instance.GetLocalPlayer());

            string chairGroupName = string.IsNullOrEmpty(temp.Group.GroupName) ? temp.Group.ID : temp.Group.GroupName;
            string message = temp.Group is ConferenceChairGroup ? " Left conference " : " Left ";
            MMOChat.Instance.SendChatMessage("All", "#EVT# " + message + chairGroupName);

            RaycastManager.Instance.DisplayCursor = true;

            if (temp.Group is ConferenceChairGroup)
            {
                ContentsManager.Instance.CloseAllContentFileUsing2DScreen();

                if (((ConferenceChairGroup)temp.Group).Owner != null)
                {
                    if (((ConferenceChairGroup)temp.Group).Owner.ID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                    {
                        ((ConferenceChairGroup)temp.Group).UnClaim();

                        if (((ConferenceChairGroup)temp.Group).ContentUploadURLs.Count > 0)
                        {
                            ContentsManager.Instance.WebClientDeleteContentGroup(((ConferenceChairGroup)temp.Group).ID, ((ConferenceChairGroup)temp.Group).ContentUploadURLs.ToArray());
                        }
                    }
                    else
                    {
                        if (!((ConferenceChairGroup)temp.Group).IsClaimed)
                        {
                            ((ConferenceChairGroup)temp.Group).ResetOnUnClaim = true;
                            ((ConferenceChairGroup)temp.Group).UnClaim();
                        }
                    }

                    bool unloadContents = false;

                    if (((ConferenceChairGroup)temp.Group).IsClaimed)
                    {
                        if (((ConferenceChairGroup)temp.Group).ContentDisplayMode.Equals(ConferenceChairGroup.ScreenContentPrivacy.Private))
                        {
                            unloadContents = true;
                        }
                    }
                    else
                    {
                        unloadContents = true;
                    }

                    if (unloadContents)
                    {
                        for (int i = 0; i < ((ConferenceChairGroup)temp.Group).ContentLoaders.Length; i++)
                        {
                            IContentLoader loader = ((ConferenceChairGroup)temp.Group).ContentLoaders[i].GetComponent<IContentLoader>();

                            loader.LocalStateChange = null;
                            loader.Owner = "";
                            loader.Unload();

                            ((ConferenceChairGroup)temp.Group).ContentLoaders[i].transform.localScale = Vector3.zero;
                        }
                    }
                }
                else
                {
                    ((ConferenceChairGroup)temp.Group).ResetOnUnClaim = true;
                    ((ConferenceChairGroup)temp.Group).UnClaim();
                }

                ((ConferenceChairGroup)temp.Group).CurrentUploadedFile = "";
                ((ConferenceChairGroup)temp.Group).ContentUploadURLs.Clear();
            }
        }

        /// <summary>
        /// Callback after player has joined the chair/group
        /// </summary>
        private void OnJoinedChairGroup()
        {
            if(occupiedChair.OccupantID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
            {
                occupiedChair.VideoChat(true);
            }
            else
            {
                //need to ensure the HUD is all set up correctly
                AmendPlayerToGlobalChairOccupancy(false, PlayerManager.Instance.GetLocalPlayer().ID);
                ExitChair();
            }
        }

        /// <summary>
        /// Called to change the local camera on the chairs group (locol only)
        /// </summary>
        public void SwitchCamera()
        {
            if (occupiedChair != null)
            {
                occupiedChair.ChangeGroupCamera();
            }
        }

        /// <summary>
        /// Called when player leaves the chair
        /// </summary>
        public void ExitChair()
        {
            HUDManager.Instance.ToggleHUDControl("LEAVE_CHAIR", false);
            HUDManager.Instance.ToggleHUDControl("CONFERENCE_OWNER", false);

            //unfreeze player
            PlayerManager.Instance.FreezePlayer(false);

            MMORoom.Instance.ToggleLocalProfileInteraction(true);

            //UI
            HUDManager.Instance.ShowHUDNavigationVisibility(true);
            NavigationManager.Instance.ToggleJoystick(true);

            if(PlayerManager.Instance.ThirdPersonCameraActive)
            {
                PlayerManager.Instance.GetLocalPlayer().ThirdPerson.GetComponentInChildren<CameraThirdPerson>().ResetActiveCameraControl();
            }

            RaycastManager.Instance.CastRay = true;
            occupiedChair = null;
        }

        /// <summary>
        /// Action called via the UI Leave button to leave the chair/group
        /// </summary>
        public void LeaveChairGroup(bool closeVideoChat = false)
        {
            if (occupiedChair != null)
            {
                //close video
                if(occupiedChair.OccupantID.Contains(PlayerManager.Instance.GetLocalPlayer().ID) || closeVideoChat)
                {
                    occupiedChair.VideoChat(false);
                }

                //leave
                if (fadeOutIn)
                {
                    HUDManager.Instance.Fade(FadeOutIn.FadeAction.Out_In, LeaveChair, null, fadePauseTime);
                }
                else
                {
                    LeaveChair();
                }
            }
        }

        /// <summary>
        /// Action called upon a chair being networked
        /// </summary>
        /// <param name="player"></param>
        /// <param name="groupID"></param>
        /// <param name="chairID"></param>
        /// <param name="isOccupied"></param>
        public void NetworkChiar(IPlayer player, string groupID, string chairID, bool isOccupied)
        {
            Debug.Log("NetworkChiar: " + chairID + "|Ocuupied: " + isOccupied);

            //find chair and update occupancy
            foreach (IChairObject c in m_allIChairObjects)
            {
                if (c.IDRef.Equals(chairID))
                {
                    if (isOccupied)
                    {
                        c.MainInteraface.Join(player);
                    }
                    else
                    {
                        c.MainInteraface.Leave(player);
                    }

                    break;
                }
            }
        }

        public void NetworkBench(IPlayer player, string groupID, string chairID, bool isOccupied, int sp)
        {
            Debug.Log("NetworkBench: " + chairID + "|Ocuupied: " + isOccupied);

            //find chair and update occupancy
            foreach (IChairObject c in m_allIChairObjects)
            {
                if (c.IDRef.Equals(chairID))
                {
                    if (isOccupied)
                    {
                        c.GO.GetComponent<Bench>().SetSittinPoint(sp, player.ID);
                        c.MainInteraface.Join(player);
                    }
                    else
                    {
                        c.MainInteraface.Leave(player);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Get all players in a chair group
        /// </summary>
        /// <param name="groupID"></param>
        /// <returns></returns>
        public List<IPlayer> GetPlayersInChairGroup(string groupID)
        {
            ChairGroup[] all = FindObjectsByType<ChairGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                //match group id
                if (all[i].ID.Equals(groupID))
                {
                    return all[i].Occupancies;
                }
            }

            return null;
        }

        public ChairGroup GetChairGroupFromPlayer(IPlayer player)
        {
            ChairGroup[] all = FindObjectsByType<ChairGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                //match group id
                if (all[i].Occupancies != null)
                {
                    if (all[i].Occupancies.Contains(player))
                    {
                        return all[i];
                    }
                }
            }

            return null;
        }

        public ChairGroup GetChairGroupFromID(string id)
        {
            ChairGroup[] all = FindObjectsByType<ChairGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                //match group id
                if (all[i].ID.Equals(id))
                {
                    return all[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Action called upon a conference being networked
        /// </summary>
        /// <param name="player"></param>
        /// <param name="conferenceID"></param>
        /// <param name="password"></param>
        /// <param name="isClaimed"></param>
        public void NetworkConference(IPlayer player, string conferenceID, string password, bool isClaimed, PlayerVectorWrapper endpoints)
        {
            ChairGroup[] all = FindObjectsByType<ChairGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Debug.Log("NetworkConference: " + conferenceID + "|Claimed: " + isClaimed);

            for (int i = 0; i < all.Length; i++)
            {
                //match group id
                if (all[i].ID.Equals(conferenceID))
                {
                    //claim/uncliam
                    if (isClaimed)
                    {
                        if (((ConferenceChairGroup)all[i]).IsClaimed) return;

                        //set conference vars
                        ((ConferenceChairGroup)all[i]).Owner = player;
                        ((ConferenceChairGroup)all[i]).DoorLock.Password = password;

                        ((ConferenceChairGroup)all[i]).Claim();
                    }
                    else
                    {
                        //if unclaimed ensure the local player leaves the group/conference
                        foreach (IChairObject ch in all[i].AllChairs)
                        {
                            PlayerVector pos = endpoints.GetVector(PlayerManager.Instance.GetLocalPlayer().ID);

                            if (pos != null)
                            {
                                all[i].AllChairs[i].SetOccupantsPreviousPosition(pos.Get(), player.ID);
                                break;
                            }
                        }

                        if (occupiedChair != null && occupiedChair.Group.ID == all[i].ID)
                        {
                            LeaveChairGroup(true);
                        }

                        ((ConferenceChairGroup)all[i]).ResetOnUnClaim = true;
                        ((ConferenceChairGroup)all[i]).UnClaim();
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Called to toggle the conference user list
        /// </summary>
        /// <param name="show"></param>
        public void ShowConferenceUserList(bool show)
        {
            if (show)
            {
                List<IPlayer> all = new List<IPlayer>();

                foreach (IPlayer player in occupiedChair.Group.Occupancies)
                {
                    all.Add(MMOManager.Instance.GetPlayerByUserID(player.ID));
                }

                PlayerManager.Instance.ShowUserList("Layout_Occupants", occupiedChair.Group.ID, show, all);
            }
            else
            {
                PlayerManager.Instance.ShowUserList("Layout_Occupants", "", show, null);
            }
        }

        /// <summary>
        /// Called to change the owner of the conference to another member in the conference
        /// </summary>
        /// <param name="conferenceID"></param>
        /// <param name="newOwner"></param>
        public void SwitchConferenceOwner(string conferenceID, string newOwner)
        {
            ChairGroup[] all = FindObjectsByType<ChairGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            IPlayer tempPlayer = PlayerManager.Instance.GetPlayer(int.Parse(newOwner));

            Debug.Log("SwitchConferenceOwner: " + conferenceID + "|New Owner: " + newOwner);

            for (int i = 0; i < all.Length; i++)
            {
                //match group id
                if (all[i].ID.Equals(conferenceID))
                {
                    if (all[i] is ConferenceChairGroup)
                    {     
                        if (!((ConferenceChairGroup)all[i]).OccupanciesContainsID(tempPlayer.ID))
                        {
                            //need to just leave the group (this will ensure there are no network issues)
                            LeaveChairGroup();
                            return;
                        }

                        foreach (var view in MMOManager.Instance.GetAllPlayers())
                        {
                            if (view.ID.Equals(tempPlayer.ID))
                            {
                                ((ConferenceChairGroup)all[i]).UpdateOwner(view);

                                //need to switch the button UI
                                if (tempPlayer.ID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                                {
                                    HUDManager.Instance.ToggleHUDControl("LEAVE_CHAIR", false);
                                    HUDManager.Instance.ToggleHUDControl("CONFERENCE_OWNER", true);
                                }
                                else
                                {
                                    HUDManager.Instance.ToggleHUDControl("LEAVE_CHAIR", true);
                                    HUDManager.Instance.ToggleHUDControl("CONFERENCE_OWNER", false);
                                }

                                //check conference display type and upload new content
                                if (((ConferenceChairGroup)all[i]).ContentDisplayType.Equals(ConferenceChairGroup.ScreenContentDisplayType.UICanvas))
                                {
                                    ContentsManager.Instance.UpdateOwnerOf2DScreens(tempPlayer.ID);
                                }
                                else
                                {
                                    for (int j = 0; j < ((ConferenceChairGroup)all[i]).ContentLoaders.Length; j++)
                                    {
                                        IContentLoader loader = ((ConferenceChairGroup)all[i]).ContentLoaders[j].GetComponent<IContentLoader>();
                                        loader.LocalStateChange = null;

                                        loader.Owner = tempPlayer.ID;
                                        loader.IsNetworked = true;
                                        bool identifiedLoader = false;

                                        if (loader is ContentImageScreen)
                                        {
                                            for (int k = 0; k < conferenceUploaders.Length; k++)
                                            {
                                                if (((ContentsManager.ContentType)conferenceUploaders[k].Type + 1).Equals(ContentsManager.ContentType.Image))
                                                {
                                                    if (tempPlayer.ID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                                                    {
                                                        if (loader.IsLoaded)
                                                        {
                                                            loader.LocalStateChange += conferenceUploaders[k].LocalStateChange;
                                                            conferenceUploaders[k].OpenController(ContentsManager.ContentType.Image, loader);
                                                            identifiedLoader = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        conferenceUploaders[k].TurnOffController();
                                                    }

                                                    break;
                                                }
                                            }

                                            if (identifiedLoader)
                                            {
                                                break;
                                            }
                                        }
                                        else if (loader is ContentVideoScreen)
                                        {
                                            for (int k = 0; k < conferenceUploaders.Length; k++)
                                            {
                                                if (((ContentsManager.ContentType)conferenceUploaders[k].Type + 1).Equals(ContentsManager.ContentType.Video))
                                                {
                                                    if (tempPlayer.ID.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                                                    {
                                                        if (loader.IsLoaded)
                                                        {
                                                            loader.LocalStateChange += conferenceUploaders[k].LocalStateChange;
                                                            conferenceUploaders[k].OpenController(ContentsManager.ContentType.Video, loader);
                                                            identifiedLoader = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        conferenceUploaders[k].TurnOffController();
                                                    }

                                                    break;

                                                }
                                            }

                                            if (identifiedLoader)
                                            {
                                                break;
                                            }
                                        }
                                        else
                                        {

                                        }
                                    }
                                }

                                return;
                            }
                        }
                    }
                }
            }
        }

        private void GetUIReferences()
        {
            HUDManager.Instance.OnCustomSetupComplete -= GetUIReferences;

            //get the conferenceuploaders + list from the hud manager conference control object
            GameObject hudGO = HUDManager.Instance.GetHUDControlObject("CONFERENCE_OWNER");

            if (conferenceUploadList == null)
            {
                conferenceUploadList = hudGO.GetComponentInChildren<ConferenceContentUploadList>(true);
            }

            ConferenceContentUpload[] all = hudGO.GetComponentsInChildren<ConferenceContentUpload>(true);
            conferenceUploaders = new ConferenceContentUpload[all.Length];

            for (int i = 0; i < all.Length; i++)
            {
                conferenceUploaders[i] = all[i];
            }
        }

        [System.Serializable]
        public enum ChairHighlightType { Color, Material }

#if UNITY_EDITOR
        [CustomEditor(typeof(ChairManager), true)]
        public class ChairManager_Editor : BaseInspectorEditor
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

    /// <summary>
    /// Interface that a chair and chairgroup must implement
    /// </summary>
    public interface IChair
    {
        string ID { get; }

        bool IsOccupied { get; }

        List<IPlayer> Occupancies { get; }

        void Join(IPlayer player);

        void Leave(IPlayer player);

        void OnPlayerDisconnect(string id);
    }

    public interface IChairObject
    {
        Chair.LiveStreamMode StreamMode { get; set; }

        bool ChairOccupied { get; }

        GameObject GO { get; }

        Lock ChairLock { get; set; }

        string IDRef { get; set; }

        ChairGroup Group { get; }

        bool CanUserControl(string user);

        void VideoChat(bool val);

        void ChangeGroupCamera();

        string OccupantID { get; }

        void SetOccupantsPreviousPosition(Vector3 vec, string playerID = "");

        Vector3 GetOccupantsPreviousPosition(string playerID = "");

        void UpdateLiveStreamRole();

        IChair MainInteraface { get; }

        Vector3 SittingPosition(string playerID = "");

        Vector3 SittingDirection(string playerID = "");

        bool HasSittingSpot { get; }
    }

    [System.Serializable]
    public class ChiarJson
    {
        public string player;
        public string chairGroupID;
        public string chairID;
        public bool isOccupied;
    }
}

