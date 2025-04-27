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
    [RequireComponent(typeof(Button))]
    public class SmartphoneUserListButton : MonoBehaviour, IUserList
    {
        [Header("Profile UI")]
        [SerializeField]
        private TMPro.TextMeshProUGUI nameDisplay;

        [SerializeField]
        private GameObject chatNotification;

        [SerializeField]
        private Image status;

        [SerializeField]
        private Image selectBKG;

        [SerializeField]
        private GameObject disturbIndicator;

        [Header("Event")]
        [SerializeField]
        private SmartphoneUserButtonEvent eventType;

        [Header("Profile Image")]
        [SerializeField]
        private Transform emptyImage;
        [SerializeField]
        private RawImage profileImage;

        private string m_playerID;
        private IPlayer m_player;
        private string m_listID;

        private UserList m_handler;
        private bool m_loadedTexture;
        private bool m_isWebRequest = false;

        private RectTransform m_rectT;

        /// <summary>
        /// Access to the active state of this button
        /// </summary>
        public bool IsActive
        {
            get;
            private set;
        }

        /// <summary>
        /// Access to the owner of this button
        /// </summary>
        public string Owner
        {
            get
            {
                return m_playerID;
            }
        }

        private void OnUpdate()
        {
            //need to see if the object is within viewport
            if (m_rectT)
            {
                if (CoreUtilities.IsRectTransformCulled(m_rectT))
                {
                    if (!m_loadedTexture)
                    {
                        m_loadedTexture = true;

                        if (m_player.IsLocal)
                        {
                            if (AppManager.Instance.Data.LoginProfileData != null)
                            {
                                StartCoroutine(LoadImage(AppManager.Instance.Data.LoginProfileData.picture_url));
                            }
                            else
                            {
                                StartCoroutine(LoadImage(AppManager.Instance.Data.FixedAvatarName));
                            }
                        }
                        else
                        {
                            IPlayer iPlayer = PlayerManager.Instance.GetPlayer(m_player.ID);

                            if (iPlayer != null)
                            {
                                if (iPlayer.CustomizationData.ContainsKey("PROFILE_PICTURE"))
                                {
                                    StartCoroutine(LoadImage(iPlayer.CustomizationData["PROFILE_PICTURE"].ToString()));
                                }
                                else if (iPlayer.CustomizationData.ContainsKey("FIXEDAVATAR"))
                                {
                                    StartCoroutine(LoadImage(iPlayer.CustomizationData["FIXEDAVATAR"].ToString()));
                                }
                                else
                                {
                                    if (iPlayer.Avatar != null)
                                    {
                                        StartCoroutine(LoadImage(iPlayer.Avatar.name));
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (m_loadedTexture)
                    {
                        StopAllCoroutines();

                        if (m_loadedTexture && m_isWebRequest)
                        {
                            DestroyImmediate(profileImage.texture);
                        }

                        m_isWebRequest = false;
                        m_loadedTexture = false;
                        profileImage.texture = null;
                        profileImage.transform.localScale = Vector3.zero;
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (m_loadedTexture)
            {
                StopAllCoroutines();

                if (m_loadedTexture && m_isWebRequest)
                {
                    DestroyImmediate(profileImage.texture);
                }

                m_isWebRequest = false;
                m_loadedTexture = false;
                profileImage.texture = null;
                profileImage.transform.localScale = Vector3.zero;
            }
        }

        private void Awake()
        {
           // selectBKG.CrossFadeAlpha(0.0f, 0.0f, true);

            m_rectT = GetComponent<RectTransform>();

            //add button event
            GetComponent<Button>().onClick.AddListener(OnClick);

            PhotonChatPlayerNotification notify = GetComponentInChildren<PhotonChatPlayerNotification>(true);

            if(notify != null)
            {
                notify.ChatID = nameDisplay.text;
            }

            if(disturbIndicator != null)
            {
                disturbIndicator.GetComponentInChildren<Image>(true).color = CoreManager.Instance.chatSettings.busy;
                disturbIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// Called to repaint the UI on the button
        /// </summary>
        public void Repaint()
        {
            OnUpdate();

            if (MMOChat.Instance.PlayerHasUnreadMessage(m_playerID) && chatNotification != null)
            {
                if(chatNotification.activeInHierarchy)
                {
                    chatNotification.SetActive(false);
                }
            }

            status.color = PlayerManager.Instance.GetPlayerStatus(m_playerID);

            //set the disturb icon
            if(disturbIndicator != null)
            {
                if(MMOManager.Instance.PlayerHasProperty(m_player, "STATUS") && MMOManager.Instance.PlayerHasProperty(m_player, "DONOTDISTURB"))
                {
                    disturbIndicator.SetActive(MMOManager.Instance.GetPlayerProperty(m_player, "STATUS").Equals("BUSY") || MMOManager.Instance.GetPlayerProperty(m_player, "DONOTDISTURB").Equals("1"));
                }
                else
                {
                    disturbIndicator.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Called to set the button display
        /// </summary>
        /// <param name="player"></param>
        /// <param name="listID"></param>
        public void Set(IPlayer player, string listID)
        {
            m_playerID = player.ID;
            m_player = player;
            m_listID = listID;
            nameDisplay.text = (m_playerID.Equals(PlayerManager.Instance.GetLocalPlayer().ID)) ? "[You] " + PlayerManager.Instance.GetPlayerName(player.NickName) : PlayerManager.Instance.GetPlayerName(player.NickName) + " [" + player.ActorNumber + "]";

            m_handler = GetComponentInParent<UserList>();
        }

        /// <summary>
        /// Toggle this object on/off
        /// </summary>
        /// <param name="toggle"></param>
        public void Toggle(bool toggle)
        {

        }

        /// <summary>
        /// Called via the button to teleport the local player to this owners position in the VR world
        /// </summary>
        public void OnClick()
        {
            if (PlayerManager.Instance.GetLocalPlayer().ID.Equals(m_playerID)) return;

            /*if (disturbIndicator != null)
            {
                if(disturbIndicator.activeInHierarchy)
                {
                    return;
                }
            }*/

            if (m_handler != null)
            {
               // m_handler.SetCurrentInterface(this);
            }

            if(eventType.Equals(SmartphoneUserButtonEvent.Chat))
            {
                if (chatNotification != null)
                {
                    chatNotification.SetActive(false);
                }

                MMOChat.Instance.SwitchChat(m_playerID);
            }
            else
            {
                if (MMOManager.Instance.PlayerHasProperty(m_player, "STATUS") && MMOManager.Instance.PlayerHasProperty(m_player, "DONOTDISTURB"))
                {
                    if(MMOManager.Instance.GetPlayerProperty(m_player, "STATUS").Equals("BUSY") || MMOManager.Instance.GetPlayerProperty(m_player, "DONOTDISTURB").Equals("1"))
                    {
                        return;
                    }
                }

                OpenCallConfirmation();

                //PhotonChat.Instance.DialVoiceCall(m_playerID);
            }
        }

        private IEnumerator WaitEndFrame()
        {
            yield return new WaitForEndOfFrame();

            if(!m_handler.CurrentExists)
            {
                MMOChat.Instance.SwitchChat("");
            }
        }

        private IEnumerator LoadImage(string url)
        {
            bool rpmUsed = AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe);

            if (string.IsNullOrEmpty(url) || !url.Contains("http") || rpmUsed)
            {
                if(url.Contains("(Clone)"))
                {
                    url = url.Replace("(Clone)", "");
                }

                if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe))
                {
#if BRANDLAB360_AVATARS_READYPLAYERME
                    RPMPlayer rpm = PlayerManager.Instance.GetPlayer(m_player.ID).TransformObject.GetComponentInChildren<RPMPlayer>();

                    if (rpm != null)
                    {
                        if (rpm.Picture != null)
                        {
                            profileImage.texture = rpm.Picture;
                        }
                        else
                        {
                            profileImage.texture = Resources.Load<Texture>("ProfilePictures/" + url);
                        }
                    }
                    else
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/" + url);
                    }
#else
                    profileImage.texture = Resources.Load<Texture>("ProfilePictures/" + url);
#endif
                }
                else if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Custom))
                {
                    if (m_player.IsLocal)
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/" + AppManager.Instance.Data.FixedAvatarName);
                    }
                    else
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/" + url);
                    }

                    //use default
                    if (profileImage.texture == null)
                    {
                        profileImage.texture = Resources.Load<Texture>("ProfilePictures/MaleSimple");
                    }
                }
                else if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Simple))
                {
                    if (url.Contains("Male"))
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
                    if (url.Contains("Male"))
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

                    m_isWebRequest = true;
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

        public void OpenPhoneProfile()
        {
            PhotonChatSmartphone smartphone = GetComponentInParent<PhotonChatSmartphone>();

            if(smartphone != null)
            {
                SmartphoneProfile profile = smartphone.gameObject.GetComponentInChildren<SmartphoneProfile>();

                if(profile != null)
                {
                    profile.Set(m_player);
                }
            }
        }

        public void OpenCallConfirmation()
        {
            PhotonChatSmartphone smartphone = GetComponentInParent<PhotonChatSmartphone>();

            if (smartphone != null)
            {
                SmartphoneConfirmCall callConfirm = smartphone.gameObject.GetComponentInChildren<SmartphoneConfirmCall>();

                if (callConfirm != null)
                {
                    callConfirm.Set(m_player);
                }
            }
        }

        [System.Serializable]
        protected enum SmartphoneUserButtonEvent { Chat, Call }

#if UNITY_EDITOR
        [CustomEditor(typeof(SmartphoneUserListButton), true)]
        public class SmartphoneUserListButton_Editor : BaseInspectorEditor
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
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chatNotification"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("status"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("selectBKG"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("disturbIndicator"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("eventType"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("emptyImage"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profileImage"), true);

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
