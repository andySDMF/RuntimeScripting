using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

#if BRANDLAB360_AVATARS_READYPLAYERME
using BrandLab360.ReadyPlayerMe;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class PhotonChatLocalProfile : MonoBehaviour
    {
        [Header("Profile Display")]
        [SerializeField]
        private TMPro.TextMeshProUGUI nameDisplay;

        [SerializeField]
        private Image status;

        [SerializeField]
        private Image doNotDisturbCheckmark;

        [SerializeField]
        private GameObject friendsPending;

        [SerializeField]
        private TMPro.TextMeshProUGUI friendsPendingText;

        [Header("Profile Image")]
        [SerializeField]
        private Transform emptyImage;
        [SerializeField]
        private RawImage profileImage;

        [Header("Settings")]
        [SerializeField]
        private RectTransform settingsPanel;
        [SerializeField]
        private LayoutElement settingsOffsetObject;

        [Header("Friends")]
        [SerializeField]
        private GameObject friendUIToggle;
        [SerializeField]
        private GameObject friendNotifications;
        [SerializeField]
        private Transform friendContainer;
        [SerializeField]
        private GameObject friendEntry;

        private string m_playerID;
        private IPlayer m_player;
        private bool m_loadedTexture;
        private string m_texureName = "";
        private RectTransform m_rectT;

        private float m_rpmTextureCheckTimer = 0.0f;

        public void Update()
        {
            if (!AppManager.IsCreated) return;

            if(settingsPanel != null && settingsOffsetObject != null)
            {
                settingsPanel.anchoredPosition = new Vector2(0, -settingsOffsetObject.minHeight);
            }

            if(PlayerManager.Instance.MainControlSettings != null)
            {
                if (nameDisplay.gameObject.activeInHierarchy)
                {
                    if (PlayerManager.Instance.MainControlSettings.nameOn <= 0)
                    {
                        nameDisplay.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (PlayerManager.Instance.MainControlSettings.nameOn > 0)
                    {
                        nameDisplay.gameObject.SetActive(true);
                    }
                }
            }

            if (friendUIToggle.activeInHierarchy != FriendsManager.Instance.IsEnabled)
            {
                friendUIToggle.SetActive(FriendsManager.Instance.IsEnabled);
            }

            if (FriendsManager.Instance.IsEnabled)
            {
                int pending = FriendsManager.Instance.CountPending;
                friendsPending.SetActive(pending > 0);
                friendsPendingText.text = pending.ToString();

                if (friendNotifications.activeInHierarchy)
                {
                    //only show pending requests
                    foreach (FriendsManager.Friend fr in FriendsManager.Instance.GetFriendsPending())
                    {
                        int friendNots = friendContainer.childCount - 1;
                        bool exists = false;

                        for (int i = friendNots; i >= 1; i--)
                        {
                            if (friendContainer.GetChild(i).GetComponent<FriendsNotification>().Friend_ID.Equals(fr.name))
                            {
                                exists = true;
                            }
                        }

                        if (!exists)
                        {
                            //create new notification
                            GameObject go = Instantiate(friendEntry, Vector3.zero, Quaternion.identity, friendContainer);
                            go.transform.localScale = Vector3.one;
                            go.name = go.name + "_" + fr.name;
                            go.GetComponentInChildren<FriendsNotification>(true).Set(fr);
                            go.SetActive(true);
                        }
                    }

                    CheckFriendsNotifications();
                }
            }
            else
            {
                if (friendsPending.activeInHierarchy)
                {
                    friendsPending.SetActive(false);
                }
            }

            //only do this if RPM and the profile tex is null. this might be a result of an RPM custom avatar and the picture has not DL yet
            if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe))
            {
                if(profileImage.texture == null)
                {
                    if(m_rpmTextureCheckTimer < 5)
                    {
                        m_rpmTextureCheckTimer += Time.deltaTime;
                    }
                    else
                    {
                        m_rpmTextureCheckTimer = 0.0f;
                        StartCoroutine(LoadImage(""));
                    }
                }
            }

            if (AppManager.Instance.Data.CurrentSceneReady)
            {
                status.color = PlayerManager.Instance.GetPlayerStatus(m_playerID);

                if (m_player != null)
                {
                    nameDisplay.text = PlayerManager.Instance.GetPlayerName(m_player.NickName) + " [" + m_player.ActorNumber + "]";
                }
            }

            if(AppManager.Instance.Data.IsMobile)
            {
                if(nameDisplay.gameObject.activeInHierarchy)
                {
                    nameDisplay.gameObject.SetActive(false);
                }
            }
        }

        private void OnEnable()
        {
            if (AppManager.IsCreated)
            {
                if(friendUIToggle != null)
                {
                    friendUIToggle.SetActive(FriendsManager.Instance.IsEnabled);
                }
            }
        }

        private void OnDisable()
        {
            if (m_loadedTexture)
            {
                StopAllCoroutines();

                if (m_loadedTexture)
                {
                    Destroy(profileImage.texture);
                }

                m_loadedTexture = false;
                profileImage.texture = null;
                profileImage.transform.localScale = Vector3.zero;
                emptyImage.localScale = Vector3.one;
            }
        }

        private void PostProfilePicture()
        {
            if (FriendsManager.Instance.IsEnabled && m_player != null)
            {
                if (MMORoom.Instance.Profile != null)
                {
                    MMORoom.Instance.Profile.OnProfileUpdate -= PostProfilePicture;
                }

                if (m_loadedTexture)
                {
                    string filename = CoreUtilities.GetFilename(AppManager.Instance.Data.LoginProfileData.picture_url);
                    if (m_texureName.Equals(filename)) return;

                    Destroy(profileImage.texture);
                }

                m_loadedTexture = false;
                profileImage.texture = null;
                profileImage.transform.localScale = Vector3.zero;
                emptyImage.localScale = Vector3.one;

                if (m_player.IsLocal)
                {
                    if (!gameObject.activeInHierarchy) return;

                    StartCoroutine(LoadImage(AppManager.Instance.Data.LoginProfileData.picture_url));
                }
                else
                {
                    IPlayer iPlayer = PlayerManager.Instance.GetPlayer(m_player.ID);

                    if (iPlayer != null)
                    {
                        if (iPlayer.CustomizationData.ContainsKey("PROFILE_BIRTHNAME"))
                        {
                            StartCoroutine(LoadImage(iPlayer.CustomizationData["PROFILE_BIRTHNAME"].ToString()));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called to set the button display
        /// </summary>
        /// <param name="player"></param>
        /// <param name="listID"></param>
        public void Set(IPlayer player)
        {
            if (player != null)
            {
                m_playerID = player.ID;
                m_player = player;
                nameDisplay.text = PlayerManager.Instance.GetPlayerName(player.NickName) + " [" + player.ActorNumber + "]";
            }
            else
            {
                nameDisplay.text = AppManager.Instance.Data.NickName;
            }

            gameObject.SetActive(true);
            m_rectT = GetComponent<RectTransform>();
           // doNotDisturbCheckmark.color = CoreManager.Instance.chatSettings.busy;

            if (FriendsManager.Instance.IsEnabled)
            {
                PostProfilePicture();
            }
            else
            {
                StartCoroutine(LoadImage(""));
            }
        }

        /// <summary>
        /// Called to toggle this players disturn networked property
        /// </summary>
        public void TogglePlayerDisturbMode()
        {
            if(m_player !=  null)
            {
                //check to see if there is a property for disturb and set the mode
                if (MMOManager.Instance.PlayerHasProperty(m_player, "DONOTDISTURB"))
                {
                    bool currentDisturb = (MMOManager.Instance.GetPlayerProperty(m_player, "DONOTDISTURB").Equals("1")) ? true : false;
                    MMOChat.Instance.SetDisturbedMode(!currentDisturb);
                }
            }
        }

        /// <summary>
        /// Called to enable/disable interaction on this panel
        /// </summary>
        /// <param name="enable"></param>
        public void EnableActions(bool enable)
        {
            GetComponent<CanvasGroup>().alpha = (enable) ? 1.0f : 0.1f;
            GetComponent<CanvasGroup>().interactable = (enable) ? true : false;
        }

        /// <summary>
        /// Called to view this local players profile
        /// </summary>
        public void ViewProfile()
        {
            if (MMORoom.Instance.Profile != null)
            {
                MMORoom.Instance.Profile.OnProfileUpdate += PostProfilePicture;
            }

            MMORoom.Instance.ShowPlayerProfile(m_player);
        }

        public void ViewFriendsNotifications(bool show)
        {
            if (show)
            {
                friendNotifications.SetActive(true);
            }
            else
            {
                friendNotifications.SetActive(false);
                int friendNots = friendContainer.childCount - 1;

                for (int i = friendNots; i >= 1; i--)
                {
                    Destroy(friendContainer.GetChild(i).gameObject);
                }
            }
        }

        private void CheckFriendsNotifications()
        {
            int friendNots = friendContainer.childCount - 1;

            for (int i = friendNots; i >= 1; i--)
            {
                if(!friendContainer.GetChild(i).GetComponent<FriendsNotification>().IsPending)
                {
                    Destroy(friendContainer.GetChild(i).gameObject);
                }
            }
        }

        private IEnumerator LoadImage(string url)
        {
            bool rpmUsed = AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe);

            if (string.IsNullOrEmpty(url) || !url.Contains("http") || rpmUsed)
            {
                if (!string.IsNullOrEmpty(url) && url.Contains("(Clone)"))
                {
                    url = url.Replace("(Clone)", "");
                }

                if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe))
                {
#if BRANDLAB360_AVATARS_READYPLAYERME

                    if (PlayerManager.Instance.GetLocalPlayer() == null) yield break;

                    RPMPlayer rpm = PlayerManager.Instance.GetLocalPlayer().TransformObject.GetComponent<RPMPlayer>();

                    if(rpm != null)
                    {
                        if(rpm.Picture != null)
                        {
                            profileImage.texture = rpm.Picture;
                        }
                        else
                        {
                            profileImage.texture = Resources.Load<Texture>("ProfilePictures/" + AppManager.Instance.Data.FixedAvatarName);
                        }
                    }
                    else
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/" + AppManager.Instance.Data.FixedAvatarName);
                    }
#else
                    profileImage.texture = Resources.Load<Texture>("ProfilePictures/" + AppManager.Instance.Data.FixedAvatarName);
#endif
                }
                else if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Custom))
                {
                    profileImage.texture = Resources.Load<Texture>("ProfilePictures/" + AppManager.Instance.Data.FixedAvatarName);

                    //use default
                    if(profileImage.texture == null)
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/MaleSimple");
                    }
                }
                else if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Simple))
                {
                    if (AppManager.Instance.Data.Sex.Equals(CustomiseAvatar.Sex.Male))
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/MaleSimple");
                    }
                    else
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/FemaleSimple");
                    }
                }
                else
                {
                    if (AppManager.Instance.Data.Sex.Equals(CustomiseAvatar.Sex.Male))
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/MaleStandard");
                    }
                    else
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/FemaleStandard");
                    }
                }
            }
            else
            {
                //webrequest
                UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, true);

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(request.error))
                {
                    profileImage.texture = DownloadHandlerTexture.GetContent(request);
                    m_texureName = CoreUtilities.GetFilename(url);
                    m_loadedTexture = true;
                }

                //dispose the request as not needed anymore
                request.Dispose();
            }

            if (profileImage.texture != null)
            {
                emptyImage.localScale = Vector3.zero;
                profileImage.transform.localScale = Vector2.one;
            }
            else
            {
                emptyImage.localScale = Vector3.one;
                profileImage.transform.localScale = Vector2.zero;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PhotonChatLocalProfile), true)]
        public class PhotonChatLocalProfile_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("nameDisplay"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("status"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("doNotDisturbCheckmark"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("friendsPending"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("friendsPendingText"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("emptyImage"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profileImage"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settingsPanel"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("settingsOffsetObject"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("friendUIToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("friendNotifications"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("friendContainer"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("friendEntry"), true);

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
