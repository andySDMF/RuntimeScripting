using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
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
    public class PhotonPhoneCall : MonoBehaviour
    {
        [Header("Profile Image")]
        [SerializeField]
        private Transform emptyImage;
        [SerializeField]
        private RawImage profileImage;
        [SerializeField]
        private TextMeshProUGUI profileNameText;

        [Header("Buttons")]
        [SerializeField]
        private GameObject recieverDisplay;
        [SerializeField]
        private GameObject callerDisplay;

        [Header("Dialling")]
        [SerializeField]
        [Range(1.0f, 10.0f)]
        private float dialTimeout = 10.0f;

        private IPlayer m_player;
        private bool m_callStarted = false;
        private float m_callDuration = 0.0f;
        private float m_dialTimer = 0.0f;
        private bool m_callTimeOut = false;
        private bool m_loadedTexture = false;


        private OrientationType m_switch = OrientationType.landscape;
        private bool m_loaded = false;
        private float m_playerFontSize;
        private Vector2 m_profileSize;
        private LayoutElement m_profileLayout;

        private RectTransform m_mainLayout;
        private float m_cacheWidth;


        private void Awake()
        {
            m_playerFontSize = profileNameText.fontSize;
            m_profileLayout = profileImage.GetComponentInParent<LayoutElement>(true);

            if (m_profileLayout != null)
            {
                m_profileSize = new Vector2(m_profileLayout.minWidth, m_profileLayout.minHeight);
            }

            m_mainLayout = transform.GetChild(0).GetComponent<RectTransform>();
            m_cacheWidth = m_mainLayout.sizeDelta.x;

            m_loaded = true;
        }

        private void OnEnable()
        {
            m_dialTimer = 0.0f;
            m_callTimeOut = false;

            if (m_player != null)
            {
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

        private void OnDisable()
        {
            if(m_loadedTexture)
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

        private void Update()
        {
            //Update the call duration once on call
            if (m_callStarted && !m_callTimeOut)
            {
                m_dialTimer = 0.0f;
                float minutes = Mathf.FloorToInt(m_callDuration / 60);
                float seconds = Mathf.FloorToInt(m_callDuration % 60);
                string time = string.Format("{0:00}:{1:00}", minutes, seconds);

                profileNameText.text = PlayerManager.Instance.GetPlayerName(m_player.NickName) + " [" + m_player.ActorNumber + "] " + time;
                m_callDuration += Time.deltaTime;
            }
            else
            {
                //if time dialed out, hangup
                if(m_dialTimer < dialTimeout)
                {
                    m_dialTimer += Time.deltaTime;
                }
                else
                {
                    m_callTimeOut = true;
                    m_callStarted = false;
                    EndPhoneCall();
                }
            }

            if (AppManager.Instance.Data.IsMobile && m_loaded && !m_switch.Equals(OrientationManager.Instance.CurrentOrientation))
            {
                m_switch = OrientationManager.Instance.CurrentOrientation;
                float aspect = OrientationManager.Instance.ScreenSize.y / OrientationManager.Instance.ScreenSize.x;

                if (m_switch.Equals(OrientationType.landscape))
                {
                    profileNameText.fontSize = m_playerFontSize;

                    if (m_profileLayout != null)
                    {
                        m_profileLayout.minWidth = m_profileSize.x;
                        m_profileLayout.minHeight = m_profileSize.y;
                    }

                    m_mainLayout.anchorMin = new Vector2(0.5f, 0.5f);
                    m_mainLayout.anchorMax = new Vector2(0.5f, 0.5f);
                    m_mainLayout.anchoredPosition = Vector2.zero;
                    m_mainLayout.sizeDelta = new Vector2(m_cacheWidth, m_mainLayout.sizeDelta.y);
                }
                else
                {
                    profileNameText.fontSize = m_playerFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler * aspect;

                    if (m_profileLayout != null)
                    {
                        m_profileLayout.minWidth = m_profileSize.x * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
                        m_profileLayout.minHeight = m_profileSize.y * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;
                    }

                    m_mainLayout.anchorMin = new Vector2(0f, 0.5f);
                    m_mainLayout.anchorMax = new Vector2(1f, 0.5f);
                    m_mainLayout.offsetMax = new Vector2(-50, 0);
                    m_mainLayout.offsetMin = new Vector2(50, 0);

                    m_mainLayout.anchoredPosition = Vector2.zero;
                }
            }

        }

        /// <summary>
        /// Called to set the UI for the call
        /// </summary>
        /// <param name="callType"></param>
        /// <param name="player"></param>
        public void Set(PhoneCallType callType, IPlayer player)
        {
            PlayerManager.Instance.SetPlayerStatus("BUSY");

            if (callType.Equals(PhoneCallType.Caller))
            {
                m_callDuration = 0.0f;
                profileNameText.text = PlayerManager.Instance.GetPlayerName(player.NickName) + " [" + player.ActorNumber + "]";
                callerDisplay.SetActive(true);
                recieverDisplay.SetActive(false);
            }
            else
            {
                profileNameText.text = PlayerManager.Instance.GetPlayerName(player.NickName) + " [" + player.ActorNumber + "]";
                recieverDisplay.SetActive(true);
                callerDisplay.SetActive(false);
            }

            m_player = player;
        }

        /// <summary>
        /// Action called to accept an incoming call
        /// </summary>
        public void Accept()
        {
            if (m_callTimeOut) return;

            MMOChat.Instance.AcceptVoiceCall(m_player.ID);

            m_callDuration = 0.0f;
            recieverDisplay.SetActive(false);
            profileNameText.text = PlayerManager.Instance.GetPlayerName(m_player.NickName) + " [" + m_player.ActorNumber + "]";
            callerDisplay.SetActive(true);

            StartPhoneCall();
        }

        /// <summary>
        /// Action called to decline incoming call
        /// </summary>
        public void Decline()
        {
            if (m_callTimeOut) return;

            MMOChat.Instance.DeclineVoiceCall(m_player.ID);
            m_callStarted = false;
        }

        /// <summary>
        /// Action called to hangup current phone call
        /// </summary>
        public void HangUp()
        {
            if (m_callTimeOut) return;

            MMOChat.Instance.EndVoiceCall(m_player.ID);
            m_callStarted = false;
        }

        /// <summary>
        /// Action called to start the phone call when both users are on the call
        /// </summary>
        public void StartPhoneCall()
        {
            if(m_player != null)
            {
                m_callStarted = true;
            }
        }

        /// <summary>
        /// Action called to end the call
        /// </summary>
        public void EndPhoneCall()
        {
            if (m_player != null)
            {
                //set the UI text
                m_callStarted = false;
                profileNameText.text = PlayerManager.Instance.GetPlayerName(m_player.NickName).ToUpper() + " [" + m_player.ActorNumber + "]";

                StartCoroutine(DelayEnd());
            }
            else
            {
                PlayerManager.Instance.SetPlayerStatus("AVAILABLE");
            }
        }

        /// <summary>
        /// Delay the end call so user can see that the call has ended
        /// </summary>
        /// <returns></returns>
        private IEnumerator DelayEnd()
        {
            yield return new WaitForSeconds(1.0f);

            m_callStarted = false;
            m_callDuration = 0.0f;

            callerDisplay.SetActive(false);
            recieverDisplay.SetActive(false);
            m_player = null;

            gameObject.SetActive(false);

            PlayerManager.Instance.SetPlayerStatus("AVAILABLE");
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

        public enum PhoneCallType { Caller, Reciever }

#if UNITY_EDITOR
        [CustomEditor(typeof(PhotonPhoneCall), true)]
        public class PhotonPhoneCall_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("emptyImage"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profileImage"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profileNameText"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("recieverDisplay"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("callerDisplay"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dialTimeout"), true);

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
