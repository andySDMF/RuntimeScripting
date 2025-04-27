using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

#if BRANDLAB360_AVATARS_READYPLAYERME
using BrandLab360.ReadyPlayerMe;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class FriendsNotification : MonoBehaviour, IFriend
    {
        [Header("Profile Display")]
        [SerializeField]
        private TextMeshProUGUI nameDisplay;

        [Header("Friend Handler")]
        [SerializeField]
        private GameObject friendsOption;

        [Header("Profile Image")]
        [SerializeField]
        private Transform emptyImage;
        [SerializeField]
        private RawImage profileImage;

        [Header("Mobile Layout")]
        [SerializeField]
        private RectTransform friendsLayout;

        public string Friend_ID
        {
            get
            {
                return m_friend.name;
            }
        }

        public bool IsPending
        {
            get
            {
                return m_friend.requestState.Equals(FriendsManager.FriendRequestState.Pending);
            }
        }

        public System.Action OnThisUpdate { get; set; }

        private FriendsManager.Friend m_friend;
        private bool m_loadedTexture;
        private bool m_isWebRequest = false;
        private string m_texureName = "";
        private IPlayer m_player;
        private RectTransform m_rectT;

        private OrientationType m_switch = OrientationType.landscape;
        private bool m_loaded = false;
        private float m_playerFontSize;
        private Vector2 m_profileSize;
        private LayoutElement m_profileLayout;

        private void OnEnable()
        {
            if (AppManager.IsCreated)
            {
                friendsOption.SetActive(false);

                if(m_rectT == null)
                {
                    m_rectT = GetComponent<RectTransform>();
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
                    DestroyImmediate(profileImage.texture);
                }

                m_loadedTexture = false;
                profileImage.texture = null;
                profileImage.transform.localScale = Vector3.zero;
                emptyImage.localScale = Vector3.one;
            }

            friendsOption.SetActive(false);
        }

        private void Update()
        {
            //need to see if the object is within viewport
            if (m_rectT && m_player != null)
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

            if (AppManager.Instance.Data.IsMobile && m_loaded && !m_switch.Equals(OrientationManager.Instance.CurrentOrientation))
            {
                m_switch = OrientationManager.Instance.CurrentOrientation;
                float aspect = OrientationManager.Instance.ScreenSize.y / OrientationManager.Instance.ScreenSize.x;

                if (m_switch.Equals(OrientationType.landscape))
                {
                    nameDisplay.fontSize = m_playerFontSize;

                    if (m_profileLayout != null)
                    {
                        m_profileLayout.minWidth = m_profileSize.x;
                        m_profileLayout.minHeight = m_profileSize.y;
                    }

                    friendsLayout.GetComponent<LayoutElement>().minHeight = 60;

                    foreach (Selectable but in friendsLayout.GetComponentsInChildren<Selectable>(true))
                    {
                        Image[] imgs = but.GetComponentsInChildren<Image>(true);

                        for (int i = 0; i < imgs.Length; i++)
                        {
                            if (imgs[i].transform.Equals(but.transform))
                            {
                                continue;
                            }

                            if (imgs[i].transform.name.Contains("Notification")) continue;

                            Destroy(imgs[i].GetComponent<LayoutElement>());
                            imgs[i].SetNativeSize();
                        }

                        TextMeshProUGUI txt = but.GetComponentInChildren<TextMeshProUGUI>(true);

                        if (txt != null)
                        {
                            txt.fontSize = 16;
                        }

                    }
                }
                else
                {
                    nameDisplay.fontSize = m_playerFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler * aspect;

                    if (m_profileLayout != null)
                    {
                        m_profileLayout.minWidth = m_profileSize.x * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
                        m_profileLayout.minHeight = m_profileSize.y * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
                    }

                    friendsLayout.GetComponent<LayoutElement>().minHeight = 60 * aspect;

                    foreach (Selectable but in friendsLayout.GetComponentsInChildren<Selectable>(true))
                    {
                        Image[] imgs = but.GetComponentsInChildren<Image>(true);

                        for (int i = 0; i < imgs.Length; i++)
                        {
                            if (imgs[i].transform.Equals(but.transform))
                            {
                                continue;
                            }

                            if (imgs[i].transform.name.Contains("Notification")) continue;

                            Vector2 size = imgs[i].GetComponent<RectTransform>().sizeDelta;
                            LayoutElement le = imgs[i].gameObject.AddComponent<LayoutElement>();
                            le.minWidth = size.x * aspect;
                            le.minHeight = size.y * aspect;
                        }

                        TextMeshProUGUI txt = but.GetComponentInChildren<TextMeshProUGUI>(true);

                        if (txt != null)
                        {
                            txt.fontSize = 16 * aspect;
                        }

                    }
                }
            }
        }

        public void ToggleFriendsOption()
        {
            friendsOption.SetActive(!friendsOption.activeInHierarchy);
        }

        public void Set(FriendsManager.Friend fr)
        {
            m_friend = fr;
            nameDisplay.text = m_friend.name;

            string playerID = "";
            foreach (var pl in MMOManager.Instance.GetAllPlayers())
            {
                if (pl.NickName.Equals(m_friend.name))
                {
                    m_player = pl;
                    playerID = pl.ID;
                    break;
                }
            }

            m_playerFontSize = nameDisplay.fontSize;
            m_profileLayout = profileImage.GetComponentInParent<LayoutElement>(true);

            if (m_profileLayout != null)
            {
                m_profileSize = new Vector2(m_profileLayout.minWidth, m_profileLayout.minHeight);
            }

            m_loaded = true;

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Called to view this local players profile
        /// </summary>
        public void ViewProfile()
        {
            if(m_player != null)
            {
                MMORoom.Instance.ShowPlayerProfile(m_player);
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
                    RPMPlayer rpm = PlayerManager.Instance.GetLocalPlayer().TransformObject.GetComponent<RPMPlayer>();

                    if (rpm != null)
                    {
                        if (rpm.Picture != null)
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
                    if (profileImage.texture == null)
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
        [CustomEditor(typeof(FriendsNotification), true)]
        public class FriendsNotification_Editor : BaseInspectorEditor
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
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("friendsOption"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("emptyImage"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profileImage"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("friendsLayout"), true);

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
