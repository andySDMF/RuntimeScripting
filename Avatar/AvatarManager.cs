using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// Manages calling the customisiton, ransdomisation & networking of avatars
    /// </summary>
    public class AvatarManager : Singleton<AvatarManager>
    {
        public static AvatarManager Instance
        {
            get
            {
                return ((AvatarManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        /// <summary>
        /// Customises the avataor on the local player
        /// </summary>
        /// <param name="player"></param>
        public void Customise(IPlayer player)
        {
            if (player != null)
            {
                if (AppManager.Instance.Data.FixedAvatarUsed)
                {
                    player.UpdateAvatar(AppManager.Instance.Data.FixedAvatarName);

                    //network player
                    StartCoroutine(WaitFrameBeforeNetworking());
                }
                else
                {
                    player.UpdateAvatar(AppManager.Instance.Data.Sex, AppManager.Instance.Data.Avatar);

                    bool isIDB = AppManager.Instance.Settings.projectSettings.loginMode.Equals(LoginMode.Standard) ? AppManager.Instance.Settings.projectSettings.useIndexedDB : true;

#if UNITY_EDITOR
                    if (!AppManager.Instance.Settings.editorTools.createWebClientSimulator)
                    {
                        isIDB = false;
                    }
#endif

                    if (isIDB || !string.IsNullOrEmpty(AppManager.Instance.Data.CustomiseJson))
                    {
                        ICustomAvatar avatar = player.Animation.GetComponent<ICustomAvatar>();

                        if (avatar != null)
                        {
                            if (!string.IsNullOrEmpty(AppManager.Instance.Data.CustomiseJson))
                            {
                                Debug.Log("Applying Avatar UserData customisation");

                                Hashtable hash = CustomiseAvatar.GetAvatarHashFromString(AppManager.Instance.Data.CustomiseJson);
                                string avatarType = "";

                                if (hash.ContainsKey("TYPE"))
                                {
                                    avatarType = hash["TYPE"].ToString();
                                }

                                //check avatar type so that avatar gets set up regardless
                                if (!string.IsNullOrEmpty(avatarType))
                                {
                                    if (avatarType.ToString().Equals(avatar.Type.ToString()))
                                    {
                                        Debug.Log("Customised Json avatar type matches project settings avatarmodetype");

                                        avatar.SetProperties(hash);

                                        Debug.Log("Applying Avatar customisation");

                                        //network player
                                        StartCoroutine(WaitFrameBeforeNetworking());
                                    }
                                    else
                                    {
                                        Debug.Log("Customised Json avatar type does not match project settings avatarmodetype. Randomising Avatar");

                                        //randomise
                                        Randomise(player);
                                    }
                                }
                                else
                                {
                                    Debug.Log("Customised Json avatar type does not exist. Randomising Avatar");

                                    //randomise
                                    Randomise(player);
                                }
                            }
                            else
                            {
                                Debug.Log("No UserData exists for avatar. Randomising Avatar");

                                //randomise
                                Randomise(player);
                            }
                        }
                        else
                        {
                            Debug.Log("Could not find avatar");
                        }
                    }
                    else
                    {
                        //network player
                        StartCoroutine(WaitFrameBeforeNetworking());
                    }
                }
            }
        }

        /// <summary>
        /// Randomises the avatar on the local player
        /// </summary>
        /// <param name="player"></param>
        public void Randomise(IPlayer player)
        {
            if (player != null)
            {
                if (player != null)
                {
                    Debug.Log("Applying Avatar randomisation");

                    if (AppManager.Instance.Data.FixedAvatarUsed)
                    {
                        int rand = 0;
                        bool nonIDB = true;

                        if (CoreManager.Instance.projectSettings.useIndexedDB && !CoreManager.Instance.projectSettings.alwaysRandomiseAvatar)
                        {
                            if(!string.IsNullOrEmpty(AppManager.Instance.Data.CustomiseJson))
                            {
                                rand = CoreManager.Instance.playerSettings.fixedAvatars.IndexOf(AppManager.Instance.Data.CustomiseJson);
                                nonIDB = false;
                            }
                        }
                        
                        if(nonIDB)
                        {
                            rand = Random.Range(0, CoreManager.Instance.playerSettings.fixedAvatars.Count);
                        }

                        //set app data
                        AppManager.Instance.Data.FixedAvatarName = CoreManager.Instance.playerSettings.fixedAvatars[rand];

                        player.UpdateAvatar(CoreManager.Instance.playerSettings.fixedAvatars[rand]);
                    }
                    else
                    {
                        //randomise sex
                        CustomiseAvatar.Sex randomSex = (CustomiseAvatar.Sex)Random.Range(0, System.Enum.GetNames(typeof(CustomiseAvatar.Sex)).Length);
                        ICustomAvatar avatar;
                        string GOname = "";
                        bool nonIDB = true;

                        if (CoreManager.Instance.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Standard))
                        {
                            if(randomSex.Equals(CustomiseAvatar.Sex.Male))
                            {
                                UnityEngine.Object prefab = Resources.Load(CoreManager.Instance.playerSettings.standardMan);
                                avatar = ((GameObject)prefab).GetComponent<ICustomAvatar>();
                                GOname = CoreManager.Instance.playerSettings.standardMan;
                            }
                            else
                            {
                                UnityEngine.Object prefab = Resources.Load(CoreManager.Instance.playerSettings.standardMan);
                                avatar = ((GameObject)prefab).GetComponent<ICustomAvatar>();
                                GOname = CoreManager.Instance.playerSettings.standardWoman;
                            }
                        }
                        else
                        {
                            if (randomSex.Equals(CustomiseAvatar.Sex.Male))
                            {
                                UnityEngine.Object prefab = Resources.Load(CoreManager.Instance.playerSettings.simpleMan);
                                avatar = ((GameObject)prefab).GetComponent<ICustomAvatar>();
                                GOname = CoreManager.Instance.playerSettings.simpleMan;
                            }
                            else
                            {
                                UnityEngine.Object prefab = Resources.Load(CoreManager.Instance.playerSettings.simpleWoman);
                                avatar = ((GameObject)prefab).GetComponent<ICustomAvatar>();
                                GOname = CoreManager.Instance.playerSettings.simpleWoman;
                            }
                        }

                        if (CoreManager.Instance.projectSettings.useIndexedDB && !CoreManager.Instance.projectSettings.alwaysRandomiseAvatar)
                        {
                            if (!string.IsNullOrEmpty(AppManager.Instance.Data.CustomiseJson))
                            {
                                Hashtable hash = CustomiseAvatar.GetAvatarHashFromString(AppManager.Instance.Data.CustomiseJson);
                                AppManager.Instance.Data.Sex = hash["SEX"].ToString() == "Male" ? CustomiseAvatar.Sex.Male : CustomiseAvatar.Sex.Female;
                                AppManager.Instance.Data.Avatar = avatar.Settings;
                                AppManager.Instance.Data.FixedAvatarName = GOname;

                                string avatarType = "";

                                if (hash.ContainsKey("TYPE"))
                                {
                                    avatarType = hash["TYPE"].ToString();
                                }

                                //check avatar type so that avatar gets set up regardless
                                if (string.IsNullOrEmpty(avatarType))
                                {
                                    if (avatarType.ToString().Equals(avatar.Type.ToString()))
                                    {
                                        Debug.Log("Customised Json avatar type matches project settings avatarmodetype");

                                        avatar.SetProperties(hash);
                                        nonIDB = false;
                                    }
                                    else
                                    {
                                        Debug.Log("Customised Json avatar type does not match project settings avatarmodetype. Randomising Avatar");
                                    }
                                }
                                else
                                {
                                    Debug.Log("Customised Json avatar type does not eixts. Randomising Avatar");
                                }
                            }
                        }
                        
                        if(nonIDB)
                        {
                            //set app data
                            AppManager.Instance.Data.Sex = randomSex;
                            AppManager.Instance.Data.Avatar = avatar.Settings.Randomise();
                            AppManager.Instance.Data.FixedAvatarName = GOname;
                        }

                        player.UpdateAvatar(AppManager.Instance.Data.Sex, AppManager.Instance.Data.Avatar);

                        if (nonIDB && CoreManager.Instance.projectSettings.useIndexedDB && !CoreManager.Instance.projectSettings.alwaysRandomiseAvatar)
                        {
                            if (string.IsNullOrEmpty(AppManager.Instance.Data.CustomiseJson))
                            {
                                //set the customisejson and update
                                AppManager.Instance.Data.CustomiseJson = CustomiseAvatar.GetAvatarHashString(avatar.GetProperties());

                                Debug.Log(AppManager.Instance.Data.CustomiseJson);

                                string nName = "User-" + AppManager.Instance.Data.NickName;
                                string admin = "Admin-" + (AppManager.Instance.Data.IsAdminUser ? 1 : 0).ToString();
                                string json = "Json-" + AppManager.Instance.Data.CustomiseJson;
                                string friends = "Friends-" + AppManager.Instance.Data.CustomiseFriends;

                                string prof = "Name*" + AppManager.Instance.Data.LoginProfileData.name + "|About*" + AppManager.Instance.Data.LoginProfileData.about + "|Pic*" + AppManager.Instance.Data.LoginProfileData.picture_url;
                                string profile = "Profile-" + prof;

                                if (AppManager.Instance.UserExists)
                                {
                                    IndexedDbManager.Instance.UpdateEntry("userData", nName + ":" + admin + ":" + json + ":" + friends + ":" + profile);
                                }
                                else
                                {
                                    IndexedDbManager.Instance.InsertEntry("userData", nName + ":" + admin + ":" + json + ":" + friends + ":" + profile);
                                }
                            }
                        }
                    }

                    //network player
                    StartCoroutine(WaitFrameBeforeNetworking());
                }
            }
        }

        private IEnumerator WaitFrameBeforeNetworking()
        {
            //need to wait until player is in the photon player list then send out
            while (PlayerManager.Instance.GetLocalPlayer() == null || !MMORoom.Instance.RoomReady)
            {
                yield return null;
            }

            //network player
            SendAvatarNetworkProperties();
        }

        /// <summary>
        /// Send local players avatar customisation across network
        /// </summary>
        public Hashtable SendAvatarNetworkProperties(bool send = true)
        {
            Hashtable hash;

            //network player
            if (AppManager.Instance.Data.FixedAvatarUsed)
            {
                hash = new Hashtable();
                hash.Add("FIXEDAVATAR", AppManager.Instance.Data.FixedAvatarName);

                if(send)
                {
                    MMOManager.Instance.SetPlayerProperties(hash);
                }
            }
            else
            {
                hash = PlayerManager.Instance.GetLocalPlayer().MainObject.GetComponentInChildren<ICustomAvatar>(true).GetProperties();

                if (send)
                {
                    MMOManager.Instance.SetPlayerProperties(hash);
                }
            }

            return hash;
        }

        /// <summary>
        /// Reciever for networked players avatars
        /// </summary>
        /// <param name="player"></param>
        /// <param name="hash"></param>
        public void CustomiseNetworkPlayer(IPlayer player, Hashtable hash)
        {
            var players = MMOManager.Instance.GetAllPlayers();

            foreach (var view in players)
            {
                //find player which is not local player
                if (view.ID.Equals(player.ID) && !view.IsLocal)
                {
                    Debug.Log("Player updated avatar status: " + player.NickName + "|" + hash.ToString());

                    if (AppManager.Instance.Data.FixedAvatarUsed)
                    {
                        if (hash.ContainsKey("FIXEDAVATAR"))
                        {
                            view.UpdateAvatar(hash["FIXEDAVATAR"].ToString());
                        }
                    }
                    else
                    {
                        if (hash.ContainsKey("SEX"))
                        {
                            CustomiseAvatar.Sex sex = hash["SEX"].Equals("Male") ? CustomiseAvatar.Sex.Male : CustomiseAvatar.Sex.Female;
                            view.UpdateAvatar(sex, null);
                            StartCoroutine(WaitFrameBeforeSyncing(view.TransformObject.gameObject, hash));
                        }
                    }

                    //visual state of the avatar
                    if (hash.ContainsKey("SHOWAVATAR"))
                    {
                        bool show = (int.Parse(hash["SHOWAVATAR"].ToString()) > 0) ? true : false;
                        view.TransformObject.gameObject.GetComponent<MMOPlayer>().ChangeAvatarLayer((show) ? 0 : 7);
                    }

                    break;
                }
            }
        }

        private IEnumerator WaitFrameBeforeSyncing(GameObject go, Hashtable hash)
        {
            yield return new WaitForSeconds(0.5f);

            //sync player
            go.GetComponentInChildren<ICustomAvatar>(true).SetProperties(hash);
            go.GetComponentInChildren<IPlayer>().Animation.transform.parent.transform.localScale = Vector3.one;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AvatarManager), true)]
        public class AvatarManager_Editor : BaseInspectorEditor
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
