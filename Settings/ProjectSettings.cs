using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;

namespace BrandLab360
{
    /// <summary>
    /// All general settings for the project should be set here
    /// Also this data should be stored to a unity asset file and read from there
    /// </summary>
    [Serializable]
    public class ProjectSettings
    {
        [Header("Intro")]
        public bool skipPassword = false;
        public bool skipName = false;
        public bool showWelcomeScreen = true;
        public bool includeUserName = true;
        public string welcomeMessage = "Welcome";
        public AudioClip welcomeClip;
        public Sprite welcomeIcon;
        public float welcomeScreenDuration = 2.0f;

        public bool showTutorialHint = true;
        public string tutorialHintTitle = "Instruction";
        public string tutorialHintMesage = "Use WASD or the arrow keys to walk around the environment";
        public string tutorialMobileHintMesage = "Use the Joystick or Arrow Pad to walk around the environment";
        public AudioClip tutorialHintAudio;
        public AudioClip tutorialMobileHintAudio;
        public float tutorialHintDuration = 5.0f;

        public bool displayMultiplayerOption = false;
        public bool useAdminUser = false;
        public bool useMultipleAdminUsers = false;
        public List<AdminUser> adminUsers = new List<AdminUser>();
        public string adminUserName = "Admin-User";

        public bool enableFriends = false;
        public string brandlabSuppotEmail = "support@brandlab-360.com";
        public string clientName = "";
        public string clientTAC = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, " +
            "sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, " +
            "quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. " +
            "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
            "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum." + System.Environment.NewLine + System.Environment.NewLine +
"Lorem ipsum dolor sit amet, consectetur adipiscing elit, " +
            "sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, " +
            "quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. " +
            "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
            "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum." + System.Environment.NewLine + System.Environment.NewLine +
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, " +
            "sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, " +
            "quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. " +
            "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
            "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

        public string clientPolicy = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, " +
            "sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, " +
            "quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. " +
            "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
            "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum." + System.Environment.NewLine + System.Environment.NewLine +
"Lorem ipsum dolor sit amet, consectetur adipiscing elit, " +
            "sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, " +
            "quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. " +
            "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
            "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum." + System.Environment.NewLine + System.Environment.NewLine +
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, " +
            "sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, " +
            "quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. " +
            "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
            "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

        public bool skipNameOnIndexedDB = true;
        public bool skipAvatarOnIndexedDB = true;
        [Header("Admin")]
        public bool enableAdminLogin = true;
        public bool displayOnJoinedRoom = false;
        public bool individualOutLogsUsed = false;
        public bool showErrors = true;
        public bool showExceptions = true;
        public bool showWarning = true;
        public bool showLogs = true;
        public bool showAssert = true;

        [Header("Admin Tabs")]
        public bool showSystemTab = false;
        public bool showProfilerTab = false;
        public bool showNetworkTab = false;
        public bool showToolsTab = false;

        [Header("Scenes")]
        public string loginSceneName = "SampleSceneIntro";
        public string mainSceneName = "SampleSceneHDRP";
        public string avatarSceneName = "SampleSceneAvatar";
        public bool overrideAvatarLightingIntensity = false;
        public float avatarLightingIntensity = 2.0f;
        //public List<string> otherScenes = new List<string>();

        [Header("Modes")]
        public LoginMode loginMode = LoginMode.Standard;
        public ReleaseMode releaseMode = ReleaseMode.Development;
        //public bool useStagingHost = true;
        public MultiplayerMode multiplayerMode = MultiplayerMode.Offline;
        public WebClientMode streamingMode = WebClientMode.None;
        public string streamingManagerPrefab = "_BRANDLAB360_STREAMING";
        public bool enableOrbitCamera = false;
        public string orbitCameraPrefab = "CameraOrbit";
        public PerspectiveCameraMode perspectiveCameraMode = PerspectiveCameraMode.ThirdPerson;
        public AvatarMode avatarMode = AvatarMode.Custom;
        public bool alwaysRandomiseAvatar = false;
        public AvatarSetupMode avatarSetupMode = AvatarSetupMode.Simple;
        public ReadyPlayerMeMode readyPlayerMeMode = ReadyPlayerMeMode.Fixed;
        public ReadyPlayerMeSelfieMode readyPlayerMeSelfieMode = ReadyPlayerMeSelfieMode.WebClient;
        public NPCMode npcMode = NPCMode.Network;
        public APILoginMode loginAPIMode = APILoginMode._BrandLab;
        public MMOProtocal mmoProtocal = MMOProtocal.Colyseus;

        [Header("AntiAlias")]
        public bool useOcclusionCulling = true;
        public CameraBrain.AntiAliasingMode antiAliasingMode = CameraBrain.AntiAliasingMode.None;
        public CameraBrain.QualityPresetMode TAAPresetMode = CameraBrain.QualityPresetMode.Medium;
        public CameraBrain.QualityPresetMode SMAAPresetMode = CameraBrain.QualityPresetMode.High;
        public CameraBrain.TAASettings TAASettings = new CameraBrain.TAASettings();

        [Header("Webclient")]
        public bool useStartedMessageStreamTimeout = true;
        public float startedMessageStreamTimeout = 10.0f;
        public bool useAssortmentAPI = true;
        public bool useAssortmentSync = true;
        public bool usePersistentInfotags = false;
        public bool useContentsAPI = true;
        public bool useVideosAPI = true;
        public bool useIndexedDB = false;
        public bool useURLParams = false;
        public bool useDataAPI = true;
        public bool useAnalytics = true;

        [Header("Host")]
        public string DevelopmentHost = "https://api-staging.brandlab360.co.uk";
        public string ProductionHost = "https://api.brandlab360.co.uk";
        public int webCommsVersion = 0;
        public string[] webCommsVersionList = new string[2] { "v1", "v2" };

        [Header("Orientation")]
        public OrientationType orientation = OrientationType.landscape;

        /*  [Header("Vechiles")]
          public bool useVehicles = false;*/

        [Header("Project")]
        public string ProjectID = "brandlab";
        public string Password = "brandlab";

        [Header("Video")]
        public bool useWebClientRoomVariable = true;

        [Header("Content")]
        public bool overrideWorldUploadPassword = false;
        public string worldContentPassword = "brandlab";

        public bool overrideWorldVideoScreensControls = false;
        public bool showControls = true;
        public bool autoPlay = true;
        public bool loopVideos = false;
        public bool showScrubber = true;

        [Header("Raycast")]
        public HighlightType highlightType = HighlightType.Material;
        public Color highlightColor = Color.yellow;
        public string highlightMaterial = "HighlightMaterial";

        public bool useCursor = false;
        public RaycastCursor.CursorType cursorType = RaycastCursor.CursorType.Ring;

        [Header("World Tags Systems")]
        public TagMode infoTagMode = TagMode._3D;
        public TagMode configTagMode = TagMode._3D;

        [Header("Photon")]
        public int maxPlayers = 16;
        public int sendRate = 30;
        public int serializationRate = 100;
        public StartupManager.JoinRoomMode joinRoomMode = StartupManager.JoinRoomMode.Queue;
        public string roomLobbyResourceTexture = "";
        public bool checkDuplicatePhotonNames = false;

        [Header("Offline")]
        public bool syncContentDisplayOffline = false;
        public bool syncContentRequestContinously = true;
        public int syncContentRequestTimer = 30000;

        [Header("Map")]
        public RenderMapMode mapRenderMode = RenderMapMode.Camera;
        public bool useMultipleMaps = false;
        //need to change this to scenes
        public List<MapManager.MapScene> mapSceneImages = new List<MapManager.MapScene>();

        [Header("Invite Types")]
        public InviteManager.InviteSetting videoChatInviteSetting = new InviteManager.InviteSetting();
        public InviteManager.InviteSetting roomInviteSetting = new InviteManager.InviteSetting();
        public string roomInviteMessage = "has invited you to join room";
        public string videoInviteMessage = "has invited you to join video chat";

        [Header("Firebase")]
        public bool useFirebase = false;
        public FirebaseConfig firebaseConfig;

        [Header("Profile API")]
        public GameObject customAPIPrefab;

        public bool defaultsCreated = false;

        public MapManager.MapScene GetMapScene(string sceneName)
        {
            return mapSceneImages.FirstOrDefault(x => x.scene.Equals(sceneName));
        }

        public void AddMapScene(string sceneName)
        {
            MapManager.MapScene mScene = GetMapScene(sceneName);

            if (mScene == null)
            {
                mScene = new MapManager.MapScene();
                mScene.scene = sceneName;
                mapSceneImages.Add(mScene);
            }
        }

#if UNITY_EDITOR
        public void CreateDefaults()
        {
            highlightColor.r = 0.4627450980392157f;
            highlightColor.g = 0.7529411764705882f;
            highlightColor.b = 0.8117647058823529f;
        }
#endif
    }

    public enum state { Inactive, PasswordInput, NameInput, AvatarInput, Running };

    public enum MMOProtocal { Photon, Colyseus };

    public enum MultiplayerMode { Online, Offline };

    public enum ReleaseMode { Production, Development };

    public enum PerspectiveCameraMode { FirstPerson, ThirdPerson, CameraOrbit }

    public enum NavigationMode { Desktop, Mobile }

    public enum AvatarMode { Custom, Random }

    public enum AvatarSetupMode { Simple, Standard, Custom, ReadyPlayerMe }

    public enum ChatSystem { Basic, Smartphone }

    public enum NPCMode { Disabled, Local, Network }

    public enum TagMode { _2D, _3D }

    public enum LoginMode { Standard, Registration }

    public enum ReadyPlayerMeMode { Selfie, Fixed }

    public enum ReadyPlayerMeSelfieMode { Vuplex, WebClient }

    public enum APILoginMode { _BrandLab, _Salesforce, _Hubspot, _Custom }

    public enum CameraStartView { _Font, _Rear, _Left, _Right}
    public enum SimulateDeviceType { _Windows, _IOS, _Android }

    public enum RenderPipeline { _HDRP, _URP }


    [System.Serializable]
    public class AdminUser
    {
        public string user;
        public string password;
        public string role;
    }
}