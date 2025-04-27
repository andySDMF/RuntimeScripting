using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class InviteManager : Singleton<InviteManager>
    {
        private InviteSetting m_videoChatSettings;
        private InviteSetting m_roomSetting;

        private List<string> m_cachedInviteList = new List<string>();
        private int m_cachedInviteType = 0;

        private const string devHub = "https://dev.brandlab360.co.uk";
        private const string prodHub = "https://hub.brandlab360.co.uk";

        public static InviteManager Instance
        {
            get
            {
                return ((InviteManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            WebclientManager.WebClientListener += OnWebClientRecieve;

            m_videoChatSettings = AppManager.Instance.Settings.projectSettings.videoChatInviteSetting;
            m_roomSetting = AppManager.Instance.Settings.projectSettings.roomInviteSetting;
        }

        public void Invite(InviteType type, List<string> invited)
        {
            m_cachedInviteList.Clear();
            m_cachedInviteList.AddRange(invited);
            m_cachedInviteType = (int)type;

#if UNITY_EDITOR
            Debug.Log("Feature not included in Editor");
#else
            InviteRequest json = new InviteRequest(type.Equals(InviteType.Video) ? m_videoChatSettings : m_roomSetting);
            WebclientManager.Instance.Send(JsonUtility.ToJson(json));

#endif
        }

        private void OnWebClientRecieve(string json)
        {
            var response = JsonUtility.FromJson<InviteResponse>(json).OrDefaultWhen(x => x.inviteCode == null);

            if(response != null)
            {
                Debug.Log("Response is Invite response ::" + response.inviteCode);

                string sender = AppManager.Instance.Data.NickName + " [" + PlayerManager.Instance.GetLocalPlayer().ActorNumber + "]";

                //need to send a invite RPC to all players in the cached invite list
                for (int i = 0; i < m_cachedInviteList.Count; i++)
                {
                    IPlayer player = MMOManager.Instance.GetPlayerByUserID(m_cachedInviteList[i]);

                    if (player != null)
                    {
                        MMOManager.Instance.SendRPC("SendInvitation", player, sender, m_cachedInviteType, response.inviteCode);
                    }
                }

                //local join
                JoinInvite((InviteType)m_cachedInviteType, response.inviteCode);
            }
        }

        public void JoinInvite(InviteType type, string invitecode)
        {
            AppManager.Instance.Data.inviteCode = invitecode;

            if (type.Equals(InviteType.Room))
            {
                string http = "";

                //need to close current session and reload page
                if(AppManager.Instance.Settings.projectSettings.releaseMode.Equals(ReleaseMode.Development))
                {
                    http = devHub;
                }
                else
                {
                    http = prodHub;
                }

                AppManager.Instance.RedirectURL(http + "/app?name=" + WebClientCommsManager.Instance.URLName);
            }
            else
            {
                //if in chair group
                if(!ChairManager.Instance.HasPlayerOccupiedChair(PlayerManager.Instance.GetLocalPlayer().ID))
                {
                    //need too close the current video chat, and start new global one with new invite code - this will now be the global chat identity
                    StartupManager.Instance.OpenGlobalVideoChat(true);
                }
                
                //otherwise when the player leaves the chair, the global chat will open to include the invite code
            }
        }

        [System.Serializable]
        private class InviteRequest
        {
            public bool toggleInvite = true;
            public string title = "";
            public string description = "";
            public string button = "";

            public InviteRequest(InviteSetting setting)
            {
                title = setting.title;
                description = setting.description;
                button = setting.button;
            }
        }

        [System.Serializable]
        private class InviteResponse
        {
            public string inviteCode;
        }

        [System.Serializable]
        public class InviteSetting
        {
            public string title = "Shop With Friends";
            public string description = "Click copy link to shop with friends.";
            public string button = "Copy Link";
        }

        public enum InviteType { Video, Room }

#if UNITY_EDITOR
        [CustomEditor(typeof(InviteManager), true)]
        public class InviteManager_Editor : BaseInspectorEditor
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
