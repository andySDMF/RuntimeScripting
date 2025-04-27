using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    public class SmartphoneProfile : MonoBehaviour, IFriend
    {
        [Header("Profile UI")]
        [SerializeField]
        private TextMeshProUGUI username;
        [SerializeField]
        private TextMeshProUGUI birthname;
        [SerializeField]
        private TextMeshProUGUI about;
        [SerializeField]
        private Image status;

        [Header("Profile Image")]
        [SerializeField]
        private Transform emptyImage;
        [SerializeField]
        private RawImage profileImage;

        private bool m_loadedTexture;
        private IPlayer iPlayer = null;
        private bool m_localPlayer = false;
        private string m_playerID = "";

        public string Friend_ID
        {
            get
            {
                return iPlayer.NickName;
            }
        }

        public System.Action OnThisUpdate { get; set; }

        private void OnEnable()
        {
            MMORoom.Instance.OnPlayerLeftRoom += OnPlayerLeftRoom;
            profileImage.GetComponent<RectTransform>().transform.localScale = Vector2.zero;

            if (FriendsManager.Instance.IsEnabled)
            {
                if (m_localPlayer)
                {
                    StartCoroutine(LoadImage(AppManager.Instance.Data.LoginProfileData.picture_url));
                }
                else
                {
                    if (iPlayer.CustomizationData.ContainsKey("PROFILE_PICTURE"))
                    {
                        StartCoroutine(LoadImage(iPlayer.CustomizationData["PROFILE_PICTURE"].ToString()));
                    }
                    else
                    {
                        if (iPlayer.CustomizationData.ContainsKey("FIXEDAVATAR"))
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
            else
            {
                if (m_localPlayer)
                {
                    StartCoroutine(LoadImage(AppManager.Instance.Data.FixedAvatarName));
                }
                else
                {
                    if (iPlayer.CustomizationData.ContainsKey("FIXEDAVATAR"))
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

        private void OnDisable()
        {
            StopAllCoroutines();

            iPlayer = null;
            m_localPlayer = false;

            emptyImage.localScale = Vector3.one;

            if (m_loadedTexture)
            {
                Destroy(profileImage.texture);
                profileImage.texture = null;
            }

            profileImage.GetComponent<RectTransform>().transform.localScale = Vector2.zero;
            m_loadedTexture = false;

            MMORoom.Instance.OnPlayerLeftRoom -= OnPlayerLeftRoom;
        }

        /// <summary>
        /// Called to update the user profiler and open
        /// About now derives form LoginProfileData.about not AppManager.Instance.Data.CustomizedData
        /// Name now derives form LoginProfileData.name and perminatly appears
        /// </summary>
        /// <param name="player"></param>
        /// <param name="data"></param>
        public virtual void Set(IPlayer player)
        {
            iPlayer = player;
            m_playerID = iPlayer.ID;

            if (iPlayer != null)
            {
                username.text = PlayerManager.Instance.GetPlayerName(player.NickName) + " [" + player.ActorNumber + "]";
            }
            else
            {
                username.text = AppManager.Instance.Data.NickName;
            }

            about.text = "";

            if (player != null)
            {
                m_localPlayer = player.ID.Equals(PlayerManager.Instance.GetLocalPlayer().ID);
                iPlayer = PlayerManager.Instance.GetPlayer(player.ID);
            }
            else
            {
                m_localPlayer = true;
                iPlayer = PlayerManager.Instance.GetLocalPlayer();
            }

            //BIRHTNAME - this shold be default now as customised data is irrelvant for this, use LoginProfileData.name 
            birthname.transform.parent.gameObject.SetActive(true);
            birthname.text = m_localPlayer ? AppManager.Instance.Data.LoginProfileData.name : iPlayer.CustomizationData.ContainsKey("PROFILE_BIRTHNAME") ? iPlayer.CustomizationData["PROFILE_BIRTHNAME"].ToString() : "";

            //ABOUT - this shold be default now as customised data is irrelvant for this, use LoginProfileData.name 
            about.text = m_localPlayer ? AppManager.Instance.Data.LoginProfileData.about : iPlayer.CustomizationData.ContainsKey("PROFILE_ABOUT") ? iPlayer.CustomizationData["PROFILE_ABOUT"].ToString() : "";

            gameObject.SetActive(true);
        }

        private void Update()
        {
            status.color = PlayerManager.Instance.GetPlayerStatus(iPlayer.ID);
        }

        private void OnPlayerLeftRoom(IPlayer player)
        {
            if (m_playerID.Equals(player.ID))
            {
                gameObject.SetActive(false);
            }
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void ReportUser()
        {
            ReportCreator rc = HUDManager.Instance.GetHUDScreenObject("REPORT_SCREEN").GetComponentInChildren<ReportCreator>();

            if (rc != null)
            {
                //need to pass this to the reportCreator
                rc.CurrentObjectID = "USERPROFILE:" + iPlayer.NickName;
            }

            HUDManager.Instance.ToggleHUDScreen("REPORT_SCREEN");
        }

        private IEnumerator LoadImage(string url)
        {
            bool rpmUsed = AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe);

            if (string.IsNullOrEmpty(url) || !url.Contains("http") || rpmUsed)
            {
                if (url.Contains("(Clone)"))
                {
                    url = url.Replace("(Clone)", "");
                }

                if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe))
                {
#if BRANDLAB360_AVATARS_READYPLAYERME
                    RPMPlayer rpm = PlayerManager.Instance.GetPlayer(iPlayer.ID).TransformObject.GetComponentInChildren<RPMPlayer>();

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
                    if (m_localPlayer)
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

                    m_loadedTexture = true;
                }

                //dispose the request as not needed anymore
                request.Dispose();
            }

            if (profileImage.texture != null)
            {
                emptyImage.localScale = Vector3.zero;
                profileImage.GetComponent<RectTransform>().transform.localScale = Vector2.one;
            }
            else
            {
                emptyImage.localScale = Vector3.one;
                profileImage.transform.localScale = Vector2.zero;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SmartphoneProfile), true)]
        public class SmartphoneProfile_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("username"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("birthname"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("about"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("status"), true);

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
