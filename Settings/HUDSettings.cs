using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace BrandLab360
{
    public enum UICanvas { _BrandlabUnity, _BespokeUnity }

    [Serializable]
    public class HUDSettings
    {
        public bool defaultsCreated = false;

        [Header("UI Canvas")]
        public UICanvas canvasUI = UICanvas._BrandlabUnity;
        public GameObject customUIPrefab;

        [Header("Profile")]
        public bool showProfileHUD = true;
        public List<HUDOption> profileOptions = new List<HUDOption>();

        [Header("HUD Rollover")]
        public bool enableUIRollover = false;
        public bool enableUIRolloverSound = false;
        public AudioClip defaultRolloverClip;

        [Header("Toolset")]
        public bool useMobileToolsForDesktop = false;

        [Header("Mobile Configuration")]
        public float mobileButtonScaler = 1.5f;
        public float mobileFontScaler = 1.5f;
        public float mobileIconFontScaler = 1.5f;

        public void DefaultHUDOptions()
        {
            HUDOption opt = new HUDOption("DISTURB");
            AddHUDOption(opt);

            opt = new HUDOption("AVATAR");
            AddHUDOption(opt);

            opt = new HUDOption("SETTINGS", false);
            AddHUDOption(opt);

            opt = new HUDOption("ADMIN", false);
            AddHUDOption(opt);

            opt = new HUDOption("LOG", false);
            AddHUDOption(opt);

            opt = new HUDOption("HELP", false);
            AddHUDOption(opt);
        }

        private void AddHUDOption(HUDOption opt)
        {
            HUDOption hOpt = profileOptions.FirstOrDefault(x => x.optionID.Equals(opt.optionID));

            if (hOpt == null)
            {
                profileOptions.Add(opt);
            }
        }

        [Header("Brandlab")]
        public bool showBrandlabLogo = true;
        public bool showBrandlabInfo = false;
        public bool showBrandlabRegisterInterest = false;

        [Header("Client")]
        public Sprite clientLogo;
        public string clientName;
        public GameObject clientPrefab;
        public ClientNavBar.ClientDisplayType clientDisplayType = ClientNavBar.ClientDisplayType.Off;

        [Header("Menu Navigation")]
        public bool showMainNavBar = true;

        public bool showMapToggle = false;
        public bool showHotspotsToggle = false;
        public bool showNavigationVisibilityToggle = false;
        [Tooltip("Determined by project settings perspective mode")]
        public bool showThirdPersonToggle = true;
        public bool showFocusToggle = true;
        public bool showMusicToggle = false;
        public bool showCalendarButton = false;
        public bool showPlayersToggle = false;
        public bool showContentsFolderToggle = false;
        [Tooltip("Determined by chat settings system mode/use private chat")]
        public bool showSmartphoneToggle = false;
        [Tooltip("Determined by chat settings use global chat")]
        public bool showchatToggle = true;
        public bool showEmotesToggle = true;
        public bool showInviteToggle = false;
        public bool showFloorplanToggle = false;
        public bool showShareToggle = false;
        public bool showHUDUsernameName = true;

        [Header("Tooltip")]
        public bool useTooltips = true;
        public bool tooltipFollowMousePosition = true;
        public float tooltipMouseVerticalOffset = 100.0f;

        [Header("Help")]
        public List<Sprite> helpSprites = new List<Sprite>();

        [Header("Subtitles")]
        public bool useSubtitles = false;
        public bool startWithSubtiles = false;

        [Header("Popup")]
        public Sprite defaultPopupIcon;
        public float popupFadeSpeed = 1;
        public float popupDisplayTime = 5.0f;

        [Header("Custom Prefabs")]
        public GameObject customIntroPassword;
        public GameObject customIntroName;
        public GameObject customAdminPanel;
        public GameObject customRegistrationPanel;

        public GameObject customWelcomeOverlay;
        public GameObject customPlayerSettingsOverlay;

        /*public CustomHUDPrefab[] customControlPrefabs;

        public CustomHUDPrefab[] customScreenPrefabs;

        public CustomHUDPrefab[] customMessagesPrefabs;*/

#if UNITY_EDITOR
        public void CreateDefaults()
        {
            defaultRolloverClip = (AudioClip)GetAsset<AudioClip>("Assets/com.brandlab360.core/Runtime/Audio/Audio Fx/ding.wav");
        }

        private UnityEngine.Object GetAsset<T>(string path)
        {
            UnityEngine.Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(T));

            if (obj == null)
            {
                obj = GetPackageAsset<T>(path);
            }

            return obj;
        }

        private UnityEngine.Object GetPackageAsset<T>(string path)
        {
            string str = path.Replace("Assets", "Packages");
            return UnityEditor.AssetDatabase.LoadAssetAtPath(str, typeof(T));
        }

        public CustomHUDPrefab[] CreateNewCustomPrefabList(int n)
        {
            if (n == 0)
            {
                return new CustomHUDPrefab[12]
                {
                    new CustomHUDPrefab("Joystick"),
                    new CustomHUDPrefab("Drop"),
                    new CustomHUDPrefab("Leave Chair"),
                    new CustomHUDPrefab("Conference Chair"),
                    new CustomHUDPrefab("Game"),
                    new CustomHUDPrefab("Configurator"),
                    new CustomHUDPrefab("Info Tag"),
                    new CustomHUDPrefab("Vehicle"),
                    new CustomHUDPrefab("Map Camera"),
                    new CustomHUDPrefab("Product Placement"),
                    new CustomHUDPrefab("Notice"),
                    new CustomHUDPrefab("Camera Orbit"),
                };
            }
            else if (n == 1)
            {
                return new CustomHUDPrefab[15]
                {
                    new CustomHUDPrefab("Emotes"),
                    new CustomHUDPrefab("Hotspots"),
                    new CustomHUDPrefab("Players"),
                    new CustomHUDPrefab("ChatSystem"),
                    new CustomHUDPrefab("Phone Call"),
                    new CustomHUDPrefab("Smartphone"),
                    new CustomHUDPrefab("Smartphone Toast"),
                    new CustomHUDPrefab("User Profile"),
                    new CustomHUDPrefab("Contents"),
                    new CustomHUDPrefab("Product Placement"),
                    new CustomHUDPrefab("Invite"),
                    new CustomHUDPrefab("Floorplan"),
                    new CustomHUDPrefab("Notice"),
                    new CustomHUDPrefab("Report"),
                    new CustomHUDPrefab("Social Media")
                };
            }
            else
            {
                return new CustomHUDPrefab[9]
                {
                    new CustomHUDPrefab("Password"),
                    new CustomHUDPrefab("Offline"),
                    new CustomHUDPrefab("Permission"),
                    new CustomHUDPrefab("Tooltip"),
                    new CustomHUDPrefab("Hint"),
                    new CustomHUDPrefab("Popup"),
                    new CustomHUDPrefab("Product"),
                    new CustomHUDPrefab("Invite"),
                    new CustomHUDPrefab("OpenAI")
                };
            }
        }
#endif
    }

    [System.Serializable]
    public class HUDOption
    {
        public string optionID = "";
        public bool showOption = true;

        public HUDOption(string id, bool showOpt = true)
        {
            optionID = id;
            showOption = showOpt;
        }
    }

    [System.Serializable]
    public class CustomHUDPrefab
    {
        public string id = "";
        public GameObject prefab;

        public CustomHUDPrefab(string id)
        {
            this.id = id;
        }
    }
}
