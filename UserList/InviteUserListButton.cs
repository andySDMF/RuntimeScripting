using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

#if BRANDLAB360_AVATARS_READYPLAYERME
using BrandLab360.ReadyPlayerMe;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace BrandLab360
{
    public class InviteUserListButton : MonoBehaviour, IUserList
    {
        [Header("Profile Data")]
        [SerializeField]
        private TMPro.TextMeshProUGUI nameDisplay;

        [SerializeField]
        private Image status;

        [SerializeField]
        private GameObject selectImage;

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

        private OrientationType m_switch = OrientationType.landscape;
        private bool m_loaded = false;
        private float m_playerFontSize;
        private Vector2 m_profileSize;
        private LayoutElement m_profileLayout;

        /// <summary>
        /// Access to the owner of this object
        /// </summary>
        public string Owner
        {
            get
            {
                return m_playerID;
            }
        }

        public bool IsSelected
        {
            get
            {
                return selectImage.activeInHierarchy;
            }
        }

        private void Awake()
        {
            m_rectT = GetComponent<RectTransform>();
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

        /// <summary>
        /// Called to repaint this objects UI
        /// </summary>
        public void Repaint()
        {
            OnUpdate();

            status.color = PlayerManager.Instance.GetPlayerStatus(m_playerID);

            if(status.color.Equals(CoreManager.Instance.chatSettings.offline))
            {
                selectImage.SetActive(false);
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
                }
                else
                {
                    nameDisplay.fontSize = m_playerFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler * aspect;

                    if (m_profileLayout != null)
                    {
                        m_profileLayout.minWidth = m_profileSize.x * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
                        m_profileLayout.minHeight = m_profileSize.y * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
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
                    Destroy(profileImage.texture);
                }

                m_isWebRequest = false;
                m_loadedTexture = false;
                profileImage.texture = null;
                profileImage.transform.localScale = Vector3.zero;
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

            m_playerFontSize = nameDisplay.fontSize;
            m_profileLayout = profileImage.GetComponentInParent<LayoutElement>(true);

            if (m_profileLayout != null)
            {
                m_profileSize = new Vector2(m_profileLayout.minWidth, m_profileLayout.minHeight);
            }

            m_loaded = true;
        }

        /// <summary>
        /// Toggle this object on/off
        /// </summary>
        /// <param name="toggle"></param>
        public void Toggle(bool toggle)
        {
            selectImage.SetActive(toggle);
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

#if UNITY_EDITOR
        [CustomEditor(typeof(InviteUserListButton), true)]
        public class InviteUserListButton_Editor : BaseInspectorEditor
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
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("selectImage"), true);

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

