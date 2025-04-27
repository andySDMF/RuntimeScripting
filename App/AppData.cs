using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace BrandLab360
{
    [System.Serializable]
    public class AppData
    {
        //Web Client Responce
        public string ProjectID = "";
        public bool IsMobile = false;
        public string WebClientResponce = "";
        public string releaseMode = "development";

        //Startup Data
        public bool StartupCompleted = false;
        public MultiplayerMode Mode = MultiplayerMode.Online;

        //Room
        public bool RoomEstablished = false;
        public int RoomID = 0;
        public string inviteCode = "";
        public bool CurrentSceneReady = false;

        //Player Data
        public bool IsAdminUser = false;
        public string NickName = "";

        //for IDB
        public string CustomiseControls = "";
        public string CustomiseJson = "";
        public string CustomiseFriends = "";
        public string CustomiseProfile = "";

        public Hashtable CustomizationData;
        public CustomiseAvatar.Sex Sex;
        public AvatarCustomiseSettings Avatar;
        public string FixedAvatarName = "";
        public AdminUser AdminRole;
        public bool FixedAvatarUsed = false;
        public ProfileData LoginProfileData = new ProfileData();
        public string RawFriendsData = "";
        public string RawGameData = "";

        //Scene data
        public AppInstances.SwitchSceneTriggerID SceneSpawnLocation;
        //music
        public bool EnvironmentalSoundOn = true;

        //web cam
        public bool WebCamActive = false;
        public string videoChannel = "";
        public bool GlobalVideoChatUsed = false;

        //control keys
        public InputKeyCode fowardDirectionKey = InputKeyCode.W;
        public InputKeyCode backDirectionKey = InputKeyCode.S;
        public InputKeyCode leftDirectionKey = InputKeyCode.A;
        public InputKeyCode rightDirectionKey = InputKeyCode.D;
        public InputKeyCode sprintKey = InputKeyCode.LeftShift;
        public InputKeyCode focusKey = InputKeyCode.F;
        public InputKeyCode strifeRightDirectionKey = InputKeyCode.X;
        public InputKeyCode strifeLeftDirectionKey = InputKeyCode.Z;
        public InputKeyCode interactionKey = InputKeyCode.Enter;

        public AssetBundle CurrentAssetBundleScene { get; set; }

        public void UpdatePlayerData(IPlayer player)
        {
            if(MMOManager.Instance.IsConnected())
            {
                NickName = PlayerManager.Instance.GetPlayerName(player.TransformObject.GetComponent<MMOView>().Nickname);
            }

            CustomizationData = player.CustomizationData;

            if(player.Animation.GetComponent<ICustomAvatar>() != null)
            {
                Avatar = player.Animation.GetComponent<ICustomAvatar>().Settings;
            }
        }

        public void UpdatePlayerSceneData(string id)
        {
            string[] split = id.Split('_');

            SceneSpawnLocation = AppManager.Instance.Instances.SwitchSceneReferences.FirstOrDefault(x => x.id.Equals(split[split.Length - 1]));
        }

        public void UnloadCurrentAssetBundleScene()
        {
            if(CurrentAssetBundleScene != null)
            {
                CurrentAssetBundleScene.Unload(false);
            }

            CurrentAssetBundleScene = null;
            Resources.UnloadUnusedAssets();
        }
    }
}
