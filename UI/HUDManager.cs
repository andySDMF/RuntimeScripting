using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

#if UNITY_PIPELINE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class HUDManager : Singleton<HUDManager>
    {
        public static HUDManager Instance
        {
            get
            {
                return ((HUDManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Menu")]
        [SerializeField]
        private RectTransform menuNavigation;

        [SerializeField]
        private RectTransform menuDropdownIcon;

        [Header("Fade")]
        [SerializeField]
        private FadeOutIn fadeOutInScreen;

        [Header("Panels")]
        [SerializeField]
        private GameObject welcomePanel;
        [SerializeField]
        private GameObject settingsPanel;
        [SerializeField]
        private GameObject lobbyPanel;
        [SerializeField]
        private GameObject avatarSelectionPanel;
        [SerializeField]
        private GameObject rolloverTooltipPanel;
        [SerializeField]
        private GameObject helpPanel;
        [SerializeField]
        private GameObject brandlabPanel;
        [SerializeField]
        private GameObject raycastPanel;

        [Header("HUD")]
        [SerializeField]
        private GameObject[] HUDNavigations;
        [SerializeField]
        private List<HUDDisplay> HUDControls;
        [SerializeField]
        private List<HUDDisplay> HUDMessages;
        [SerializeField]
        private List<HUDDisplay> HUDScreens;

        [Header("Profile")]
        [SerializeField]
        private RectTransform profile;
        [SerializeField]
        private GameObject profileDisturb;
        [SerializeField]
        private GameObject profileAvatar;
        [SerializeField]
        private GameObject profileAdmin;
        [SerializeField]
        private GameObject profileSettings;
        [SerializeField]
        private GameObject profileHelpButton;
        [SerializeField]
        private GameObject profileLogButton;

        [Header("Brandlab")]
        [SerializeField]
        private GameObject[] brandlabLogos;
        [SerializeField]
        private GameObject brandlabInfo;
        [SerializeField]
        private GameObject brandlabRegisterInterest;
        [SerializeField]
        private GameObject brandlabButton;

        [Header("Menu Elements")]
        [SerializeField]
        private Toggle topdownToggle;
        [SerializeField]
        private Toggle hotspotsToggle;
        [SerializeField]
        private Toggle navMeshToggle;
        [SerializeField]
        private Toggle thirdPersonToggle;
        [SerializeField]
        private Toggle focusToggle;
        [SerializeField]
        private Toggle musicToggle;
        [SerializeField]
        private Toggle playerListToggle;
        [SerializeField]
        private Button contentsButton;
        [SerializeField]
        private Toggle smartphoneToggle;
        [SerializeField]
        private Toggle chatToggle;
        [SerializeField]
        private Toggle emotesToggle;
        [SerializeField]
        private Toggle inviteToggle;
        [SerializeField]
        private Toggle floorplanToggle;
        [SerializeField]
        private Toggle shareToggle;
        [SerializeField]
        private Button calendarButton;

        private float m_menuTweenDuration = 0.5f;
        private Coroutine m_menuAnimation;
        private bool m_menuPosition = true;
        private Vector3 m_menuOrigin;
        private Vector2 m_offscreen = new Vector2(0, 5000);
        private Dictionary<string, CustomCanvasClass> m_customCanvases = new Dictionary<string, CustomCanvasClass>();

        public System.Action OnCustomSetupComplete { get; set; }
        public bool NavigationHUDVisibility { get; private set; }

        public Selectable GetMenuFeature(MenuFeature feature)
        {
            switch(feature)
            {
                case MenuFeature._TopDown:
                    return topdownToggle;
                case MenuFeature._Hotspot:
                    return hotspotsToggle;
                case MenuFeature._NavMesh:
                    return navMeshToggle;
                case MenuFeature._Perspective:
                    return thirdPersonToggle;
                case MenuFeature._Focus:
                    return focusToggle;
                case MenuFeature._Players:
                    return playerListToggle;
                case MenuFeature._Contents:
                    return contentsButton;
                case MenuFeature._Phone:
                    return smartphoneToggle;
                case MenuFeature._Chat:
                    return chatToggle;
                case MenuFeature._Emotes:
                    return emotesToggle;
                case MenuFeature._Invite:
                    return inviteToggle;
                case MenuFeature._Floorplan:
                    return floorplanToggle;
                case MenuFeature._Share:
                    return shareToggle;
                case MenuFeature._Music:
                    return musicToggle;
                case MenuFeature._Calendar:
                    return calendarButton;
            }

            return null;
        }

        public Toggle TopdownToggle
        {
            get { return topdownToggle; }
        }

        public GameObject ProfileGO
        {
            get
            {
                return profile.gameObject;
            }
        }

        public Dictionary<string, CustomCanvasClass> CustomCanvasas
        {
            get
            {
                return m_customCanvases;
            }
        }

        public GameObject CustomUIPrefab
        {
            get;
            private set;
        }

        private void OnDestroy()
        {
            if(AppManager.IsCreated)
            {
                OrientationManager.Instance.OnOrientationChanged = null;
            }
        }

        private void Start()
        {
            NavigationHUDVisibility = true;

            m_menuOrigin = new Vector3(menuNavigation.anchoredPosition.x, menuNavigation.anchoredPosition.y, 0);

            CustomCanvas[] cCanvases = FindObjectsByType<CustomCanvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < cCanvases.Length; i++)
            {
                m_customCanvases.Add(cCanvases[i].gameObject.name, new CustomCanvasClass(cCanvases[i].gameObject, null));
            }
        }

        /// <summary>
        /// Called to set the HUD display based on Manager HUD settings
        /// </summary>
        public void SetHUD()
        {
            if (!CoreManager.Instance.HUDSettings.showProfileHUD)
            {
                profile.anchoredPosition = m_offscreen;
            }

            for(int i = 0; i < CoreManager.Instance.HUDSettings.profileOptions.Count; i++)
            {
                if(CoreManager.Instance.HUDSettings.profileOptions[i].optionID.Equals("DISTURB"))
                {
                    if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Offline))
                    {
                        profileDisturb.SetActive(false);
                    }
                    else
                    {
                        profileDisturb.SetActive(CoreManager.Instance.HUDSettings.profileOptions[i].showOption);
                    }
                    
                }
                else if(CoreManager.Instance.HUDSettings.profileOptions[i].optionID.Equals("AVATAR"))
                {
                    profileAvatar.SetActive(CoreManager.Instance.HUDSettings.profileOptions[i].showOption);
                }
                else if (CoreManager.Instance.HUDSettings.profileOptions[i].optionID.Equals("SETTINGS"))
                {
                    profileSettings.SetActive(CoreManager.Instance.HUDSettings.profileOptions[i].showOption);
                }
                else if (CoreManager.Instance.HUDSettings.profileOptions[i].optionID.Equals("LOG"))
                {
                    if(AppManager.Instance.Data.IsMobile)
                    {
                        profileLogButton.SetActive(false);
                    }
                    else
                    {
                        profileLogButton.SetActive(CoreManager.Instance.HUDSettings.profileOptions[i].showOption);
                    }
                }
                else if (CoreManager.Instance.HUDSettings.profileOptions[i].optionID.Equals("HELP"))
                {
                    profileHelpButton.SetActive(CoreManager.Instance.HUDSettings.profileOptions[i].showOption);
                }
                else
                {
                    profileAdmin.SetActive(CoreManager.Instance.HUDSettings.profileOptions[i].showOption);
                }
            }

            for(int i = 0; i < brandlabLogos.Length; i++)
            {
                brandlabLogos[i].SetActive(CoreManager.Instance.HUDSettings.showBrandlabLogo);
            }

            ///brandlabInfo.SetActive(CoreManager.Instance.HUDSettings.showBrandlabInfo);
            //brandlabRegisterInterest.SetActive(CoreManager.Instance.HUDSettings.showBrandlabRegisterInterest);

            //ensure both are off - new UI for brandlab panel
            brandlabInfo.SetActive(false);
            brandlabRegisterInterest.SetActive(false);

            if(brandlabButton != null)
            {
                bool active = CoreManager.Instance.HUDSettings.showBrandlabRegisterInterest || CoreManager.Instance.HUDSettings.showBrandlabInfo ? true : false;
              
                if(AppManager.Instance.Data.IsMobile)
                {
                    //cannot access this if mobile
                    brandlabButton.SetActive(false);
                }
                else
                {
                    brandlabButton.SetActive(active);
                }
            }

            //menu
            topdownToggle.gameObject.SetActive(CoreManager.Instance.HUDSettings.showMapToggle);
            hotspotsToggle.gameObject.SetActive(CoreManager.Instance.HUDSettings.showHotspotsToggle);
            navMeshToggle.gameObject.SetActive(CoreManager.Instance.HUDSettings.showNavigationVisibilityToggle);
            thirdPersonToggle.gameObject.SetActive(CoreManager.Instance.HUDSettings.showThirdPersonToggle);


            if (AppManager.Instance.Data.IsMobile)
            {
                focusToggle.gameObject.SetActive(false);
            }
            else
            {
                focusToggle.gameObject.SetActive(CoreManager.Instance.HUDSettings.showFocusToggle);
            }

            musicToggle.gameObject.SetActive(CoreManager.Instance.HUDSettings.showMusicToggle);

#if BRANDLAB360_INTERNAL
            calendarButton.gameObject.SetActive(CoreManager.Instance.HUDSettings.showCalendarButton);
#else
            calendarButton.gameObject.SetActive(false);
#endif

            if(AppManager.Instance.Data.IsMobile)
            {
                floorplanToggle.gameObject.SetActive(false);
            }
            else
            {
                floorplanToggle.gameObject.SetActive((AppManager.Instance.Data.IsAdminUser || AppManager.Instance.Data.AdminRole != null) ? CoreManager.Instance.HUDSettings.showFloorplanToggle : false);
            }


            if(AppManager.Instance.Settings.socialMediaSettings.socialMediaEnabled)
            {
                shareToggle.gameObject.SetActive(CoreManager.Instance.HUDSettings.showShareToggle);
            }
            else
            {
                shareToggle.gameObject.SetActive(false);
            }

            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online))
            {
                playerListToggle.gameObject.SetActive(CoreManager.Instance.HUDSettings.showPlayersToggle);
                smartphoneToggle.gameObject.SetActive(CoreManager.Instance.HUDSettings.showSmartphoneToggle);
                chatToggle.gameObject.SetActive(CoreManager.Instance.HUDSettings.showchatToggle);
                inviteToggle.gameObject.SetActive(CoreManager.Instance.HUDSettings.showInviteToggle);
            }
            else
            {
                playerListToggle.gameObject.SetActive(false);
                smartphoneToggle.gameObject.SetActive(false);
                chatToggle.gameObject.SetActive(false);
                inviteToggle.gameObject.SetActive(false);
            }

            contentsButton.gameObject.SetActive(CoreManager.Instance.HUDSettings.showContentsFolderToggle);
           
            emotesToggle.gameObject.SetActive(CoreManager.Instance.HUDSettings.showEmotesToggle);

            if(!CoreManager.Instance.HUDSettings.showMainNavBar)
            {
                m_menuPosition = !m_menuPosition;
                m_menuAnimation = StartCoroutine(TweenMenuPosition(m_menuPosition));

                foreach(Transform t in menuNavigation)
                {
                    if(t.name.Equals("Player_Stamina"))
                    {
                        continue;
                    }

                    t.transform.localScale = Vector3.zero;
                }
            }

            SetCustomPrefabs();

            if(CoreManager.Instance.HUDSettings.canvasUI.Equals(UICanvas._BespokeUnity) && CoreManager.Instance.HUDSettings.customUIPrefab != null)
            {
                CoreManager.Instance.MainCanvas.transform.GetChild(0).GetChild(1).transform.localScale = Vector3.zero;

                CustomUIPrefab = Instantiate(CoreManager.Instance.HUDSettings.customUIPrefab);
            }
        }

        private void SetCustomPrefabs()
        {
            /*if(CoreManager.Instance.HUDSettings.customWelcomeOverlay != null)
            {
                GameObject welcome = Instantiate(CoreManager.Instance.HUDSettings.customWelcomeOverlay, Vector3.zero, Quaternion.identity);
                welcome.transform.parent = welcomePanel.transform.parent;
                welcome.transform.SetSiblingIndex(welcomePanel.transform.GetSiblingIndex() + 1);
                welcome.transform.localScale = Vector3.one;
                welcome.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                welcome.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                welcomePanel = welcome;
            }

            if (CoreManager.Instance.HUDSettings.customPlayerSettingsOverlay != null)
            {
                GameObject playerSettings = Instantiate(CoreManager.Instance.HUDSettings.customPlayerSettingsOverlay, Vector3.zero, Quaternion.identity);
                playerSettings.transform.parent = settingsPanel.transform.parent;
                playerSettings.transform.SetSiblingIndex(settingsPanel.transform.GetSiblingIndex() + 1);
                playerSettings.transform.localScale = Vector3.one;
                playerSettings.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                playerSettings.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                settingsPanel = playerSettings;
            }

            if(CoreManager.Instance.HUDSettings.customControlPrefabs != null)
            {
                for (int i = 0; i < CoreManager.Instance.HUDSettings.customControlPrefabs.Length; i++)
                {
                    if (CoreManager.Instance.HUDSettings.customControlPrefabs[i].prefab != null)
                    {
                        //need to instantiate prefab
                        GameObject go = Instantiate(CoreManager.Instance.HUDSettings.customControlPrefabs[i].prefab, Vector3.zero, Quaternion.identity);
                        int childindex = 0;
                        int index = 0;
                        Transform parentObj = null;
                        RectTransform rectT = null;

                        switch (CoreManager.Instance.HUDSettings.customControlPrefabs[i].id)
                        {
                            case "Drop":
                                index = 0;
                                break;
                            case "Leave Chair":
                                index = 1;
                                break;
                            case "Conference Chair":
                                index = 2;
                                break;
                            case "Joystick":
                                index = 3;
                                break;
                            case "Game":
                                index = 4;
                                break;
                            case "Configurator":
                                index = 5;
                                break;
                            case "Info Tag":
                                index = 6;
                                break;
                            case "Vehicle":
                                index = 7;
                                break;
                            case "Map Camera":
                                index = 8;
                                break;
                            case "Product Placement":
                                index = 9;
                                break;
                            case "Notice":
                                index = 10;
                                break;
                            case "Camera Orbit":
                                index = 11;
                                break;
                        }

                        childindex = HUDControls[index].display.transform.GetSiblingIndex();
                        parentObj = HUDControls[index].display.transform.parent;
                        rectT = HUDControls[index].display.GetComponent<RectTransform>();
                        HUDControls[index].display = go;

                        go.transform.SetParent(parentObj);
                        go.transform.localScale = rectT.localScale;
                        go.transform.SetSiblingIndex(childindex + 1);
                        go.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                        go.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                    }
                }
            }

            if(CoreManager.Instance.HUDSettings.customMessagesPrefabs != null)
            {
                for (int i = 0; i < CoreManager.Instance.HUDSettings.customMessagesPrefabs.Length; i++)
                {
                    if (CoreManager.Instance.HUDSettings.customMessagesPrefabs[i].prefab != null)
                    {
                        //need to instantiate prefab
                        GameObject go = Instantiate(CoreManager.Instance.HUDSettings.customMessagesPrefabs[i].prefab, Vector3.zero, Quaternion.identity);
                        int childindex = 0;
                        int index = 0;
                        Transform parentObj = null;
                        RectTransform rectT = null;

                        switch (CoreManager.Instance.HUDSettings.customMessagesPrefabs[i].id)
                        {
                            case "Offline":
                                index = 0;
                                break;
                            case "Password":
                                index = 1;
                                break;
                            case "Permission":
                                index = 2;
                                break;
                            case "Tooltip":
                                index = 3;
                                break;
                            case "Hint":
                                index = 4;
                                break;
                            case "Popup":
                                index = 5;
                                break;
                            case "Product":
                                index = 6;
                                break;
                            case "Invite":
                                index = 7;
                                break;
                            case "OpenAI":
                                index = 8;
                                break;
                        }

                        childindex = HUDMessages[index].display.transform.GetSiblingIndex();
                        parentObj = HUDMessages[index].display.transform.parent;
                        rectT = HUDMessages[index].display.GetComponent<RectTransform>();
                        HUDMessages[index].display = go;

                        go.transform.SetParent(parentObj);
                        go.transform.localScale = rectT.localScale;
                        go.transform.SetSiblingIndex(childindex);
                        go.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                        go.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                    }
                }
            }

            if(CoreManager.Instance.HUDSettings.customScreenPrefabs !=  null)
            {
                for (int i = 0; i < CoreManager.Instance.HUDSettings.customScreenPrefabs.Length; i++)
                {
                    if (CoreManager.Instance.HUDSettings.customScreenPrefabs[i].prefab != null)
                    {
                        //need to instantiate prefab
                        GameObject go = Instantiate(CoreManager.Instance.HUDSettings.customScreenPrefabs[i].prefab, Vector3.zero, Quaternion.identity);
                        int childindex = 0;
                        int index = 0;
                        Transform parentObj = null;
                        RectTransform rectT = null;

                        switch (CoreManager.Instance.HUDSettings.customScreenPrefabs[i].id)
                        {
                            case "Emotes":
                                index = 0;
                                break;
                            case "Hotspots":
                                index = 1;
                                break;
                            case "Players":
                                index = 2;
                                break;
                            case "ChatSystem":
                                index = 3;
                                break;
                            case "Phone Call":
                                index = 4;
                                break;
                            case "Smartphone":
                                index = 5;
                                break;
                            case "Smartphone Toast":
                                index = 6;
                                break;
                            case "User Profile":
                                index = 7;
                                break;
                            case "Contents":
                                index = 8;
                                break;
                            case "Product Placement":
                                index = 9;
                                break;
                            case "Invite":
                                index = 10;
                                break;
                            case "Floorplan":
                                index = 11;
                                break;
                            case "Notice":
                                index = 12;
                                break;
                            case "Report":
                                index = 13;
                                break;
                            case "Social Media":
                                index = 14;
                                break;
                        }

                        childindex = HUDScreens[index].display.transform.GetSiblingIndex();
                        parentObj = HUDScreens[index].display.transform.parent;
                        rectT = HUDScreens[index].display.GetComponent<RectTransform>();
                        HUDScreens[index].display = go;

                        go.transform.SetParent(parentObj);
                        go.transform.localScale = rectT.localScale;
                        go.transform.SetSiblingIndex(childindex);
                        go.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                        go.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                    }
                }
            }*/

            if (OnCustomSetupComplete != null)
            {
                OnCustomSetupComplete.Invoke();
            }
        }

        public void ShowUIRolloverTooltip(bool show, string tooltip)
        {
            if(AppManager.Instance.Settings.HUDSettings.enableUIRollover)
            {
                rolloverTooltipPanel.GetComponentInChildren<HUDRolloverPanel>(true).Set(tooltip);
                rolloverTooltipPanel.SetActive(show);
            }
        }

        public void PlayUISound(AudioClip clip)
        {
            SoundManager.Instance.PlaySound(clip);
        }

        public GameObject GetHUDControlObject(string id)
        {
            for (int i = 0; i < HUDControls.Count; i++)
            {
                if (HUDControls[i].id.Equals(id))
                {
                    return HUDControls[i].display;
                }
            }

            return null;
        }

        public GameObject GetHUDMessageObject(string id)
        {
            for (int i = 0; i < HUDMessages.Count; i++)
            {
                if (HUDMessages[i].id.Equals(id))
                {
                    return HUDMessages[i].display;
                }
            }

            return null;
        }

        public GameObject GetHUDScreenObject(string id)
        {
            for (int i = 0; i < HUDScreens.Count; i++)
            {
                if (HUDScreens[i].id.Equals(id))
                {
                    return HUDScreens[i].display;
                }
            }

            return null;
        }

        public void SetHUDScreenObject(string id, GameObject obj)
        {
            for (int i = 0; i < HUDScreens.Count; i++)
            {
                if (HUDScreens[i].id.Equals(id))
                {
                    HUDScreens[i].display = obj;
                    break;
                }
            }
        }

        public void SetHUDScreenObject(GameObject obj)
        {
            for (int i = 0; i < HUDScreens.Count; i++)
            {
                if (HUDScreens[i].id.Equals(obj.name.Substring(1)))
                {
                    HUDScreens[i].display = obj;
                    break;
                }
            }
        }

        public void ParentToMenu(Transform t)
        {
            if(menuNavigation != null)
            {
                t.SetParent(menuNavigation);
            }
        }

        public Transform GetMenuItem(string item)
        {
            if (menuNavigation != null)
            {
                return menuNavigation.Find(item);
            }

            return null;
        }

        public void ToggleMenuPosition()
        {
            if (!CoreManager.Instance.HUDSettings.showMainNavBar) return;

            if(m_menuAnimation != null)
            {
                StopCoroutine(m_menuAnimation);
            }

            m_menuPosition = !m_menuPosition;
            m_menuAnimation = StartCoroutine(TweenMenuPosition(m_menuPosition));
        }

        private IEnumerator TweenMenuPosition(bool state)
        {
            float runningTime = 0.0f;
            float percentage = 0.0f;

            Vector3 from = menuNavigation.anchoredPosition;
            Vector3 to = (state) ? m_menuOrigin : new Vector3(0, -menuNavigation.sizeDelta.y, 0);

            while (percentage < 1.0f)
            {
                runningTime += Time.deltaTime;
                percentage = runningTime / m_menuTweenDuration;

                menuNavigation.anchoredPosition = new Vector3(0, EaseOutCubic(from.y, to.y, percentage), 0);

                yield return null;
            }

            menuDropdownIcon.localEulerAngles = (state) ? new Vector3(0, 0, 0) : new Vector3(180, 0, 0);
            menuNavigation.anchoredPosition = new Vector3(0, to.y, 0);
        }

        /// <summary>
        /// Fade out/in overlay. Fakes timelapse. Pass in an event that happens after fade out. Add callback when fade in complete. 
        /// Pause the amount of time once faded out
        /// </summary>
        /// /// <param name="fadeType"></param>
        /// <param name="change"></param>
        /// <param name="callback"></param>
        /// <param name="pause"></param>
        public void Fade(FadeOutIn.FadeAction fadeType, System.Action change, System.Action callback, float pause = 0.0f)
        {
            if (fadeOutInScreen)
            {
                fadeOutInScreen.PauseTime = pause;
                fadeOutInScreen.FadeType = fadeType;
                fadeOutInScreen.ImplementChange = change;
                fadeOutInScreen.Callback = callback;

                if(!fadeOutInScreen.gameObject.activeInHierarchy)
                {
                    fadeOutInScreen.gameObject.SetActive(true);
                }
                else
                {
                    fadeOutInScreen.PerformFade();
                }
            }
        }

        /// <summary>
        /// Called to show welcome message panel
        /// </summary>
        /// <param name="show"></param>
        public void ShowWelcomePanel(bool show)
        {
            if(CoreManager.Instance.projectSettings.showWelcomeScreen)
            {
                welcomePanel.SetActive(show);
            }
        }

        public void ShowRoomLobbyPanel(bool show)
        {
            if (CoreManager.Instance.projectSettings.joinRoomMode.Equals(StartupManager.JoinRoomMode.Lobby))
            {
                lobbyPanel.SetActive(show);
            }
        }

        public RaycastInteractionPanel ShowRaycastPanel(bool show)
        {
            if (AppManager.Instance.Settings.playerSettings.raycastType.Equals(PlayerControlSettings.RaycastType._Box) ||
                AppManager.Instance.Settings.playerSettings.raycastType.Equals(PlayerControlSettings.RaycastType._Both))
            {
                if(raycastPanel.activeInHierarchy != show)
                {
                    raycastPanel.SetActive(show);
                }
            }

            return raycastPanel.GetComponentInChildren<RaycastInteractionPanel>(true);
        }

        public void ShowPlayerSettings(bool show)
        {
            PlayerManager.Instance.FreezePlayer(show);
            settingsPanel.SetActive(show);
        }

        public void ShowBrandlab(bool show)
        {
            PlayerManager.Instance.FreezePlayer(show);
            brandlabPanel.SetActive(show);
        }

        public void ShowHelp(bool show)
        {
            PlayerManager.Instance.FreezePlayer(show);
            helpPanel.SetActive(show);
        }

        /// <summary>
        /// Toggles the main HUD UI elements visibility
        /// </summary>
        /// <param name="show"></param>
        public void ShowHUDNavigationVisibility(bool show)
        {
            NavigationHUDVisibility = show;

            for (int i = 0; i < HUDNavigations.Length; i++)
            {
                HUDNavigations[i].transform.localScale = (!show) ? Vector3.zero : Vector3.one;
            }

           // GetHUDControlObject("").transform.localScale = (!show) ? Vector3.zero : Vector3.one;
        }

        /// <summary>
        /// Called to toggle a selected HUD control visibility, if on then turns off, if off turns on
        /// </summary>
        /// <param name="id"></param>
        /// <param name="state"></param>
        public void ToggleHUDScreen(string id)
        {
            for (int i = 0; i < HUDScreens.Count; i++)
            {
                if (HUDScreens[i].id.Equals(id))
                {
                    if (HUDScreens[i].display != null)
                    {
                        HUDScreens[i].display.SetActive(!HUDScreens[i].display.activeInHierarchy);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Called to toggle a selected HUD control visibility by state
        /// </summary>
        /// <param name="id"></param>
        /// <param name="state"></param>
        public void ToggleHUDControl(string id, bool state)
        {
            for(int i = 0; i < HUDControls.Count; i++)
            {
                if(HUDControls[i].id.Equals(id))
                {
                    if (HUDControls[i].display != null)
                    {
                        HUDControls[i].display.SetActive(state);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Called to toggle a HUD Message by state
        /// </summary>
        /// <param name="id"></param>
        /// <param name="state"></param>
        public void ToggleHUDMessage(string id, bool state)
        {
            for (int i = 0; i < HUDMessages.Count; i++)
            {
                if (HUDMessages[i].id.Equals(id))
                {
                    if (HUDMessages[i].display != null)
                    {
                        HUDMessages[i].display.SetActive(state);
                    }

                    break;
                }
            }
        }

        public void OpenCustomizeAvatar(bool useSelectionPanel = true)
        {
            PlayerManager.Instance.FreezePlayer(true);

            if(AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe) || AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Custom))
            {
                if(useSelectionPanel)
                {
                    avatarSelectionPanel.SetActive(true);
                }
                else
                {
                    Fade(FadeOutIn.FadeAction.Out_In, OnCustomizeAvatarOpen, null, 0.0f);
                }
            }
            else
            {
                Fade(FadeOutIn.FadeAction.Out_In, OnCustomizeAvatarOpen, null, 0.0f);
            }
        }

        public void CloseCustmizeAvatar()
        {
            Fade(FadeOutIn.FadeAction.Out_In, OnCustomizeAvatarClose, null, 0.5f);
        }

        private void OnCustomizeAvatarOpen()
        {
            ShowHUDNavigationVisibility(false);
            MMORoom.Instance.ToggleLocalProfileVisibility(false);

            if (CoreManager.Instance.IsMobile)
            {
                NavigationManager.Instance.ToggleJoystick(false);
            }

#if UNITY_PIPELINE_HDRP

            Camera.main.GetComponent<HDAdditionalCameraData>().clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            Camera.main.GetComponent<HDAdditionalCameraData>().backgroundColorHDR = Color.black;

#elif UNITY_PIPELINE_URP

            Camera.main.clearFlags = CameraClearFlags.Color;
            Camera.main.backgroundColor = Color.black;
#endif

            PlayerManager.Instance.GetLocalPlayer().MainCamera.SetActive(false);

            if (CoreManager.Instance.SceneEnvironment != null)
            {
                CoreManager.Instance.SceneEnvironment.Activate(false);
            }

            AppManager.Instance.LoadScene(AppManager.Instance.Settings.projectSettings.avatarSceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }

        private void OnCustomizeAvatarClose()
        {
            AppManager.Instance.UnloadScene(AppManager.Instance.Settings.projectSettings.avatarSceneName);

            ShowHUDNavigationVisibility(true);
            MMORoom.Instance.ToggleLocalProfileVisibility(true);

            if (CoreManager.Instance.IsMobile)
            {
                NavigationManager.Instance.ToggleJoystick(true);
            }

            if (CoreManager.Instance.SceneEnvironment != null)
            {
                CoreManager.Instance.SceneEnvironment.Activate(true);
            }

            PlayerManager.Instance.GetLocalPlayer().MainCamera.SetActive(true);
            PlayerManager.Instance.FreezePlayer(false);

            CameraBrain.Instance.ApplySetting(true);
        }

        public void PreviewCustomCanvas(string canvas, bool show)
        {
            if(m_customCanvases.ContainsKey(canvas))
            {
                m_customCanvases[canvas].canvas.SetActive(show);

                if(m_customCanvases[canvas].eventAction != null)
                {
                    m_customCanvases[canvas].eventAction.Invoke(show);
                }
            }

        }

        [System.Serializable]
        public class CustomCanvasClass
        {
            public GameObject canvas;
            public System.Action<bool> eventAction;

            public CustomCanvasClass(GameObject go, System.Action<bool> evt)
            {
                canvas = go;
                eventAction = evt;
            }
        }


        [System.Serializable]
        private class HUDDisplay
        {
            public string id;
            public GameObject display;
        }

        private float EaseOutCubic(float start, float end, float value)
        {
            value--;
            end -= start;
            return end * (value * value * value + 1) + start;
        }

        public enum MenuFeature { _TopDown, _Hotspot, _NavMesh, _Perspective, _Focus, _Players, _Contents, _Phone, _Chat, _Emotes, _Invite, _Floorplan, _Share, _Music, _Calendar }

#if UNITY_EDITOR
        [CustomEditor(typeof(HUDManager), true)]
        public class HUDManager_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("menuNavigation"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("menuDropdownIcon"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeOutInScreen"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("welcomePanel"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settingsPanel"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lobbyPanel"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("avatarSelectionPanel"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rolloverTooltipPanel"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("helpPanel"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("brandlabPanel"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("raycastPanel"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("HUDNavigations"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("HUDControls"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("HUDMessages"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("HUDScreens"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profile"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profileDisturb"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profileAvatar"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profileAdmin"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profileSettings"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profileHelpButton"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profileLogButton"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("brandlabLogos"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("brandlabInfo"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("brandlabRegisterInterest"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("brandlabButton"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("topdownToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("hotspotsToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("navMeshToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("thirdPersonToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("focusToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("musicToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("playerListToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("contentsButton"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("smartphoneToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chatToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("emotesToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("inviteToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("floorplanToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("shareToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("calendarButton"), true);

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
