using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class FriendsManager : Singleton<FriendsManager>
    {
        public static FriendsManager Instance
        {
            get
            {
                return ((FriendsManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private FriendsJson m_friends = new FriendsJson();
        private bool m_enabled = true;
        private float m_timer = 0.0f;

        public bool IsEnabled
        {
            get
            {
                return m_enabled;
            }
        }

        public int CountPending
        {
            get
            {
                int count = 0;

                for(int i = 0; i < m_friends.friends.Count; i++)
                {
                    if(m_friends.friends[i].source.Equals(FriendRequestSrc.Recieved) && m_friends.friends[i].requestState.Equals(FriendRequestState.Pending))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public Friend GetFriend(string friend)
        {
            for (int i = 0; i < m_friends.friends.Count; i++)
            {
                if (m_friends.friends[i].name.Equals(friend))
                {
                    return m_friends.friends[i];
                }
            }

            return null;
        }

        public List<Friend> GetFriendsPending()
        {
            List<Friend> temp = new List<Friend>();

            for (int i = 0; i < m_friends.friends.Count; i++)
            {
                if (m_friends.friends[i].requestState.Equals(FriendRequestState.Pending))
                {
                    temp.Add(m_friends.friends[i]);
                }
            }

            return temp;
        }

        public List<Friend> GetFriends()
        {
            List<Friend> temp = new List<Friend>();

            for (int i = 0; i < m_friends.friends.Count; i++)
            {
                if (m_friends.friends[i].requestState.Equals(FriendRequestState.Accepted))
                {
                    temp.Add(m_friends.friends[i]);
                }
            }

            return temp;
        }

        private void Start()
        {
            if (AppManager.IsCreated)
            {
#if !UNITY_EDITOR
                m_enabled = AppManager.Instance.Settings.projectSettings.enableFriends ? (AppManager.Instance.Settings.projectSettings.useIndexedDB || AppManager.Instance.Settings.projectSettings.loginMode.Equals(LoginMode.Registration)) ? true : false : false;
#else
                m_enabled = AppManager.Instance.Settings.projectSettings.enableFriends;
#endif

                if (m_enabled)
                {
                    PlayerManager.OnUpdate += OnUpdate;
                    FriendsJson fJson = null;

                    if (!AppManager.Instance.Settings.projectSettings.loginMode.Equals(LoginMode.Registration))
                    {
                        fJson = ConvertIDBFriendsToFriendsStructure(AppManager.Instance.Data.CustomiseFriends);
                    }
                    else
                    {
                        fJson = JsonUtility.FromJson<FriendsJson>(AppManager.Instance.Data.RawFriendsData);
                    }

                    if (fJson != null)
                    {
                        m_friends.friends.AddRange(fJson.friends);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if(m_enabled)
            {
                PlayerManager.OnUpdate -= OnUpdate;
            }
        }

        private void OnUpdate()
        {
            if (!m_enabled) return;

            m_timer += Time.deltaTime;

            //every 30 seconds if there is a friend who is pending, then send request again if this player sent the request
            //this is to check if the friend has already accepted/declined while this player was offline
            if(m_timer > 30.0f)
            {
                m_timer = 0.0f;
                
                for(int i = 0; i < m_friends.friends.Count; i++)
                {
                    if(m_friends.friends[i].requestState.Equals(FriendRequestState.Pending) && m_friends.friends[i].source.Equals(FriendRequestSrc.Sent))
                    {
                        SendFriendRequest(new string[] { m_friends.friends[i].name });
                    }
                }
            }
        }

        public void FriendRequest(string friend)
        {
            if (!m_enabled) return;

            AddFriend(friend, FriendRequestSrc.Sent);

            UpdateFriends();
            SendFriendRequest(new string[1] { friend });
        }

        public void FriendRequests(string[] friends)
        {
            if (!m_enabled) return;

            for (int i = 0; i < friends.Length; i++)
            {
                AddFriend(friends[i], FriendRequestSrc.Sent);
            }

            UpdateFriends();
            SendFriendRequest(friends);
        }

        private bool AddFriend(string friend, FriendRequestSrc src)
        {
            Friend temp = m_friends.friends.FirstOrDefault(x => x.name.Equals(friend));

            if (temp == null)
            {
                temp = new Friend(friend);
                temp.source = src;
                m_friends.friends.Add(temp);
                return true;
            }

            return false;
        }

        private void SendFriendRequest(string[] friends)
        {
            string nickname = AppManager.Instance.Data.NickName;

            for (int i = 0; i < friends.Length; i++)
            {
                IPlayer player = MMOManager.Instance.GetAllPlayers().FirstOrDefault(x => x.NickName.Equals(friends[i]));

                if(player != null)
                {
                    MMOManager.Instance.SendRPC("FriendRequest", (int)MMOManager.RpcTarget.Others, friends[i], nickname);
                }
            }
        }

        public void SendFriendRequestResponse(string friend, FriendRequestState response)
        {
            if (!m_enabled) return;

            Friend temp = m_friends.friends.FirstOrDefault(x => x.name.Equals(friend));

            if(temp != null)
            {
                temp.requestState = response;
                string nickname = AppManager.Instance.Data.NickName;
                IPlayer player = MMOManager.Instance.GetAllPlayers().FirstOrDefault(x => x.NickName.Equals(friend));

                if (player != null)
                {
                    MMOManager.Instance.SendRPC("FriendRequestResponse", (int)MMOManager.RpcTarget.Others, friend, nickname, (int)temp.requestState);
                }
            }

            UpdateFriends();
        }

        public void RecievedFriendsRequest(string friend)
        {
            if (!m_enabled) return;

            //if false then player exists, so reply with responce
            if(!AddFriend(friend, FriendRequestSrc.Recieved))
            {
                //if the state is not pending, this player must have declined or accepted already when friend was offline
                Friend temp = m_friends.friends.FirstOrDefault(x => x.name.Equals(friend));

                if(temp != null && !temp.requestState.Equals(FriendRequestState.Pending))
                {
                    SendFriendRequestResponse(friend, temp.requestState);
                }
            }
            else
            {
                UpdateFriends();
            }
        }

        public void RecievedFriendsRequestResponse(string friend, int state)
        {
            if (!m_enabled) return;

            Friend temp = m_friends.friends.FirstOrDefault(x => x.name.Equals(friend));
            
            if(temp !=  null)
            {
                temp.requestState = (FriendRequestState)state;
                UpdateFriends();
            }
        }

        private void UpdateFriends()
        {
            if (AppManager.Instance.Settings.projectSettings.loginMode.Equals(LoginMode.Standard))
            {
                if (AppManager.Instance.Settings.projectSettings.useIndexedDB)
                {
                    SaveFriendsToIDB();

                    string nName = "User-" + AppManager.Instance.Data.NickName;
                    string admin = "Admin-" + (AppManager.Instance.Data.IsAdminUser ? 1 : 0).ToString();
                    string json = "Json-" + AppManager.Instance.Data.CustomiseJson;
                    string friends = "Friends-" + AppManager.Instance.Data.CustomiseFriends;
                    string games = "Games-" + AppManager.Instance.Data.RawGameData;

                    string prof = "Name*" + AppManager.Instance.Data.LoginProfileData.name + "|About*" + AppManager.Instance.Data.LoginProfileData.about + "|Pic*" + AppManager.Instance.Data.LoginProfileData.picture_url;
                    string profile = "Profile-" + prof;

                    if (AppManager.Instance.UserExists)
                    {
                        IndexedDbManager.Instance.UpdateEntry("userData", nName + ":" + admin + ":" + json + ":" + friends + ":" + profile + ":" + games);
                    }
                    else
                    {
                        IndexedDbManager.Instance.InsertEntry("userData", nName + ":" + admin + ":" + json + ":" + friends + ":" + profile + ":" + games);
                    }
                }
            }
            else
            {
                AppManager.Instance.Data.RawFriendsData = JsonUtility.ToJson(m_friends);
                AppManager.Instance.UpdateLoginsAPI();
                //string projectID = string.IsNullOrEmpty(AppManager.Instance.Settings.projectSettings.clientName) ? AppManager.Instance.Data.ProjectID : AppManager.Instance.Settings.projectSettings.clientName;
                //LoginsAPI.Instance.UpdateUser(AppManager.Instance.Data.NickName, projectID, JsonUtility.ToJson(AppManager.Instance.Data.LoginProfileData), AppManager.Instance.Data.LoginProfileData.password, AppManager.Instance.Data.RawFriendsData, AppManager.Instance.Data.RawGameData);
            }
        }

        private FriendsJson ConvertIDBFriendsToFriendsStructure(string raw)
        {
            FriendsJson json = new FriendsJson();

            //idb struct
            //Name*Andy|src*0|state*0#

           if(string.IsNullOrEmpty(raw))
            {
                return json;
            }

            string[] friends = raw.Split('#');

            for(int i = 0; i < friends.Length; i++)
            {
                Friend fr = new Friend("");
                string[] frSplit = friends[i].Split('|');

                for(int j = 0; j < frSplit.Length; j++)
                {
                    string[] frData = frSplit[j].Split('*');

                    if(frData[0].Equals("Name"))
                    {
                        fr.name = frData[1];
                    }
                    else if(frData[0].Equals("src"))
                    {
                        fr.source = (FriendRequestSrc)int.Parse(frData[1]);
                    }
                    else
                    {
                        fr.requestState = (FriendRequestState)int.Parse(frData[1]);
                    }
                }

                json.friends.Add(fr);
            }

            return json;
        }

        private void SaveFriendsToIDB()
        {
            //idb struct
            //Name*Andy|src*0|state*0#

            string str = "";
            int count = 0;

            foreach(Friend fr in m_friends.friends)
            {
                string frStr = "Name*" + fr.name + "src*" + ((int)fr.source).ToString() + "state*" + ((int)fr.requestState).ToString();

                if(count < m_friends.friends.Count - 1)
                {
                    frStr += "#";
                }

                str += frStr;
            }

            AppManager.Instance.Data.CustomiseFriends = str;
        }

        [System.Serializable]
        public class FriendsJson
        {
            public List<Friend> friends = new List<Friend>();
        }

        [System.Serializable]
        public class Friend
        {
            public string name;
            public FriendRequestSrc source = FriendRequestSrc.Sent;
            public FriendRequestState requestState = FriendRequestState.Pending;

            public Friend(string n)
            {
                name = n;
            }
        }

        [System.Serializable]
        public enum FriendRequestSrc { Sent, Recieved }

        [System.Serializable]
        public enum FriendRequestState { Pending, Accepted, Denied }
    }

    public interface IFriend
    {
        string Friend_ID { get; }

        System.Action OnThisUpdate { get; set; }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(FriendsManager), true)]
    public class FriendsManager_Editor : BaseInspectorEditor
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