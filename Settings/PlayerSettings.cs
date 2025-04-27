using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace BrandLab360
{
    /// <summary>
    /// Global settings for player control
    /// </summary>
    [Serializable]
    public class PlayerControlSettings
    {
        [Header("Avatar")]
        public string playerController = "BLPlayer";
        public AvatarBackwardBehaviour avatarBackwardsBehaviour = AvatarBackwardBehaviour.None;
        public bool freezePlayerOnStart = false;
        public bool interactOnStart = true;

        public string standardMan = "MaleStandard";
        public Vector3 scaleStandardMan = Vector3.one;
        public string standardWoman = "FemaleStandard";
        public Vector3 scaleStandardWoman = Vector3.one;

        public string simpleMan = "MaleSimple";
        public Vector3 scaleSimpleMan = new Vector3(0.65f, 0.65f, 0.65f);
        public string simpleWoman = "FemaleSimple";
        public Vector3 scaleSimpleWoman = new Vector3(0.65f, 0.65f, 0.65f);
        public bool overrideClothingColors = false;
        public Color[] simpleClothingColors = new Color[9]
            {
                Color.white,
                Color.gray,
                Color.yellow,
                Color.green,
                Color.red,
                Color.magenta,
                Color.blue,
                Color.cyan,
                Color.black
            };

        public List<string> fixedAvatars = new List<string>() { "Dan", "Charlotte" };
        public List<Vector3> fixedAvatarScales = new List<Vector3>() { Vector3.one, Vector3.one };

        public Vector3 RPMAvatarScale = Vector3.one;

        [Header("Follow Remote Player")]
        public Color pointerColor = Color.magenta;
        public Color cursorColor = Color.magenta;

        [Header("Third Person")]
        public CameraThirdPerson.ResetCameraWhen resetCameraWhen = CameraThirdPerson.ResetCameraWhen._PlayerMove;

        public float Smoothing = 4f;
        public float distanceOffset = 0.5f;
        public Vector3 followMinOffset = new Vector3(0, 0.3f, 0.8f);
        public Vector3 followMaxOffset = new Vector3(0, 3, 8);
        public CameraStartView startingCameraView = CameraStartView._Rear;


        [Header("Control")]
        public MouseInputButton inputButton = MouseInputButton._1;
        public float walkSpeed = 4.0f;
        public float runSpeed = 8.0f;
        public float sensitivity = 4.0f;
        public float strafingSpeed = 4.0f;
        public float maxSlopeAngle = 70;
        public float maxStepHeight = 0.3f;
        public bool jumpEnabled = false;
        public float jumpPower = 5f;
        public MobileControlType mobileControllerType = MobileControlType._Arrows;

        public float footstepVolume = 1.0f;

        public bool invertMouseX = false;
        public bool invertMouseY = false;

        public bool createNavMeshAgent = false;
        public bool useStaminaBar = false;
        public bool enableNavMeshMovement = false;
        public bool invertArrowPadLook = true;
        public bool canRunInJoystick = false;

        [Header("Control Keys")]
        public InputKeyCode fowardDirectionKey = InputKeyCode.W;
        public InputKeyCode backDirectionKey = InputKeyCode.S;
        public InputKeyCode leftDirectionKey = InputKeyCode.A;
        public InputKeyCode rightDirectionKey = InputKeyCode.D;
        public InputKeyCode sprintKey = InputKeyCode.LeftShift;
        public InputKeyCode focusKey = InputKeyCode.F;
        public InputKeyCode strifeRightDirectionKey = InputKeyCode.X;
        public InputKeyCode strifeLeftDirectionKey = InputKeyCode.Z;
        public InputKeyCode interactionKey = InputKeyCode.Enter;

        [Header("Display")]
        public bool overrideFPLayerMask = false;
        public LayerMask FPMask = ~0;
        public bool overrideThirdPersonLayerMask = false;
        public LayerMask TPMask = ~0;

        [Header("Interaction")]
        public RaycastType raycastType = RaycastType._Mouse;
        public GameObject raycastInteractionIndicator;
        public Vector3 RaycastBoxExtents = new Vector3(0.5f, 1, 0.5f);
        public float RaycastFOVAngleToloerance = 20.0f;
        public float interactionDistance = 15.0f;
        public float worldCanvasUIInteractionDistance = 5.0f;
        public Color worldCanvasUIPressedColor = Color.grey;
        public List<ManagerInteraction> managerInteraction = new List<ManagerInteraction>();

        public enum RaycastType { _Mouse, _Box, _Both }

        //With these settings
        //Service is called 10 times per second on localplayer, this means high frequency updates are every 2 seconds and low frequency updates are every 20 seconds
        [Header("Photon Player")]
        public int maximumNumberOfServiceFrames = 1000;
        public int highFrequencyEveryNthFrame = 20;
        public int lowFrequencyEveryNthFrame = 200;
        public int lowFrequencyFrameOffset = 10;
        [Range(0.5f, 1.0f)]
        public float cooldown = 1.0f;

        public bool createRemoteNavMeshAgent = false;
        public bool defaultsCreated = false;

        [Header("Teleport")]
        public TeleportMode teleportMovement = TeleportMode.Tween;
        public bool enableTeleportOnTopdown = false;
        public TeleportType teleportType = TeleportType.Points;
        public PointerDisplayType pointerDisplayType = PointerDisplayType._2D;

        public TeleportConfig teleportConfig = TeleportConfig.Global;
        public List<TeleportSetting> teleportScenes = new List<TeleportSetting>();

        [Header("Chair")]
        public bool chairFadeOutIn = true;
        public float chairFadePauseTime = 0.5f;
        public bool enableGlobalChatWhilstInChair = false;
        public bool addHightLightToChairs = true;

        [Header("ChairGroup")]
        public Color conferenceAvailableColor = Color.green;
        public Color conferenceUnavailableColor = Color.red;

        [Header("Info Tag")]
        public InfotagManager.WebPopUpType webPopUpType = InfotagManager.WebPopUpType.Window;

        [Header("Assortment Tag")]
        public float assortmentZSortOffset = 0.0025f;
        public bool usePickup = true;
        public bool useAutoAdd = true;
        public bool useBillboards = false;
        public bool useAssortmentSocialMedia = false;

        public bool enableAssortmentTags = false;

        [Header("Product")]
        public bool productPopupInfoShowImage = false;
        public bool maintainProductPlacementObjectWhenCreated = true;
        public float productPlacementPlayerDistance = 10;
        public ProductAPI.FormatRestriction productFormat = ProductAPI.FormatRestriction.Any;

        [Header("Emotes")]
        public bool emotesCreated = false;
        public List<EmoteEntry> emoteActions = new List<EmoteEntry>();
        public List<EmoteEntry> emoteIcons = new List<EmoteEntry>();

        [Header("Drop Control")]
        public bool showDropInstruction = true;
        public string defaulDropTitle = "Instruction";
        public string defaultDropMessage = "Place item on a interactive area or click 'Drop' to drop item.";
        public string defaultDropButton = "Drop";

        [Header("3D Buttons")]
        public bool apply3DButtonAppearenceAtRuntime = false;
        public ButtonAppearance.Appearance buttonAppearance = ButtonAppearance.Appearance._Round;

        public enum MouseInputButton { _0, _1 }

        public enum PointerDisplayType { _2D, _3D }

        public enum MobileControlType { _Joystick, _Arrows }

        public enum TeleportConfig { Global, Scenes }

        [System.Serializable]
        public class TeleportSetting
        {
            public string sceneName = "";
            public bool enableTeleportOnTopdown = false;
            public TeleportType teleportType = TeleportType.Points;

#if UNITY_EDITOR
            public bool foldout = false;
#endif

        }

        [System.Serializable]
        public class ManagerInteraction
        {
            public string managerName;
            public bool overrideInteraction = false;
            public float interactionDistance = 20.0f;
            public string userCheckKey = "";
        }

        public TeleportSetting GetTeleportScene(string sceneName)
        {
            return teleportScenes.FirstOrDefault(x => x.sceneName.Equals(sceneName));
        }

        public void AddScene(string sceneName)
        {
            TeleportSetting tScene = GetTeleportScene(sceneName);

            if (tScene == null)
            {
                tScene = new TeleportSetting();
                tScene.sceneName = sceneName;
                teleportScenes.Add(tScene);
            }
        }

        public ManagerInteraction GetIRaycasterManager(string managerName)
        {
            return managerInteraction.FirstOrDefault(x => x.managerName.Equals(managerName));
        }

        [Serializable]
        public class EmoteEntry
        {
            public string name;
            public int id;
            public Sprite icon;
        }

#if UNITY_EDITOR
        public void CreateDefaults()
        {
            pointerColor.r = 0.4627450980392157f;
            pointerColor.g = 0.7529411764705882f;
            pointerColor.b = 0.8117647058823529f;

            cursorColor.r = 0.4627450980392157f;
            cursorColor.g = 0.7529411764705882f;
            cursorColor.b = 0.8117647058823529f;

            raycastInteractionIndicator = Resources.Load<GameObject>("RaycastInteractionIndicator");
        }

        public void CreateDefaultEmotes()
        {
            emoteActions.Clear();
            emoteIcons.Clear();

            //need to change this to the samples directory instead
            UnityEngine.Object[] atlas = Resources.LoadAll<UnityEngine.Object>("HUD/HUD_General/"); //GetAssets("Assets/com.brandlab360.core/Runtime/Sprites/HUD/HUD_General.png");
            UnityEngine.Object[] emojis = Resources.LoadAll<UnityEngine.Object>("HUD/HUD_Emojis/");//GetAssets("Assets/com.brandlab360.core/Runtime/Sprites/HUD/HUD_Emojis.png");

            //actions
            EmoteEntry ea = new EmoteEntry();
            ea.name = "Raised Hand";
            ea.id = 7;
            ea.icon = GetSpriteFromAtlas(atlas, "HUD_RaisedHand");
            if(ea.icon != null)
            {
                emoteActions.Add(ea);
            }

            ea = new EmoteEntry();
            ea.name = "Waving";
            ea.id = 0;
            ea.icon = GetSpriteFromAtlas(atlas, "HUD_Wave");
            if (ea.icon != null)
            {
                emoteActions.Add(ea);
            }

            ea = new EmoteEntry();
            ea.name = "Claping";
            ea.id = 5;
            ea.icon = GetSpriteFromAtlas(atlas, "HUD_Clap");
            if (ea.icon != null)
            {
                emoteActions.Add(ea);
            }

            ea = new EmoteEntry();
            ea.name = "Shaking hands";
            ea.id = 6;
            ea.icon = GetSpriteFromAtlas(atlas, "HUD_Handshake");
            if (ea.icon != null)
            {
                emoteActions.Add(ea);
            }

            ea = new EmoteEntry();
            ea.name = "Dancing";
            ea.id = 4;
            ea.icon = GetSpriteFromAtlas(atlas, "HUD_Dance");
            if (ea.icon != null)
            {
                emoteActions.Add(ea);
            }

            ea = new EmoteEntry();
            ea.name = "Laughing";
            ea.id = 2;
            ea.icon = GetSpriteFromAtlas(atlas, "HUD_Laugh");
            if (ea.icon != null)
            {
                emoteActions.Add(ea);
            }

            ea = new EmoteEntry();
            ea.name = "Drinking";
            ea.id = 3;
            ea.icon = GetSpriteFromAtlas(atlas, "HUD_Coffee");
            if (ea.icon != null)
            {
                emoteActions.Add(ea);
            }

            ea = new EmoteEntry();
            ea.name = "Confused";
            ea.id = 1;
            ea.icon = GetSpriteFromAtlas(atlas, "HUD_Shrug");
            if (ea.icon != null)
            {
                emoteActions.Add(ea);
            }

            //icons
            for (int i = 0; i < emojis.Length; i++)
            {
                EmoteEntry ei = new EmoteEntry();
                ei.name = "HUD_Emojis_" + i.ToString();
                ei.id = i;
                ei.icon = GetSpriteFromAtlas(emojis, "HUD_Emojis_" + i.ToString());

                if (ei.icon != null)
                {
                    emoteIcons.Add(ei);
                }
            }
        }

        private Sprite GetSpriteFromAtlas(UnityEngine.Object[] atlas, string name)
        {
            for (int i = 0; i < atlas.Length; i++)
            {
                if (atlas[i].name.Equals(name))
                {
                    return (Sprite)atlas[i];
                }
            }

            return null;
        }

        private UnityEngine.Object[] GetAssets(string path)
        {
            UnityEngine.Object[] obj = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);

            if (obj == null || obj.Length <= 0)
            {
                obj = GetPackageAssets(path);
            }

            return obj;
        }

        private UnityEngine.Object[] GetPackageAssets(string path)
        {
            string str = path.Replace("Assets", "Packages");
            return UnityEditor.AssetDatabase.LoadAllAssetsAtPath(str);
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
#endif
    }
}
