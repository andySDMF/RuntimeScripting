using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Button))]
    public class ConferenceContentUpload : MonoBehaviour
    {
        [SerializeField]
        private ContentsManager.ContentType type = ContentsManager.ContentType.All;

        [SerializeField]
        [HideInInspector]
        private string uploadID = "";

        [SerializeField]
        private ConferenceContentUploadController controller;

        private ContentsManager.ContentFileInfo m_info;
        private ConferenceChairGroup m_conference;

        /// <summary>
        /// Access to this upload ID
        /// </summary>
        public string ID
        {
            get
            {
                return uploadID;
            }
            set
            {
                uploadID = value;
            }
        }

        /// <summary>
        /// Access to the type of upload 
        /// </summary>
        public int Type
        {
            get
            {
                return (int)type - 1;
            }
        }

        /// <summary>
        /// Access to the current URL that is uploaded on this
        /// </summary>
        public string URL
        {
            get
            {
                if(m_info != null)
                {
                    return m_info.url;
                }

                return "";
            }
        }

        /// <summary>
        /// Access to the File on this
        /// </summary>
        public ContentsManager.ContentFileInfo FileInfo
        {
            get { return m_info; }
            set { m_info = value; }
        }

        /// <summary>
        /// States if this upload is open
        /// </summary>
        public bool IsOpen
        {
            get;
            set;
        }

        private void Awake()
        {
            //add the listerner to the button
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        /// <summary>
        /// Action called when the button is clicked
        /// </summary>
        public void OnClick()
        {
            if (type.Equals(ContentsManager.ContentType.All))
            {
                Debug.Log("Cannot use type All for world canvases [" + uploadID + "]");
                return;
            }

            //subscribe to mamnagers file actions
            ContentsManager.Instance.OnFileUpload += UploadCallback;
            //ContentsManager.Instance.OnFileDelete += DeleteCallback;

            m_conference = (ConferenceChairGroup)ChairManager.Instance.GetChairGroupFromPlayer(PlayerManager.Instance.GetLocalPlayer());

            if(m_conference != null)
            {
                if(m_conference.ContentDisplayType.Equals(ConferenceChairGroup.ScreenContentDisplayType.WorldCanvas))
                {
                    //request upload to web client
                    ContentsManager.Instance.WebClientUploadWorldContent(uploadID, type.ToString());
                    return;
                }
            }

            Debug.Log("ConferenceContentUpload: OnClick Upload Content: " + ID);

            //request upload to web client
            ContentsManager.Instance.WebClient2DUploadContent(uploadID, (int)type - 1);
        }

        /// <summary>
        /// Called to send out local change across the network, called via the IContentLoader
        /// </summary>
        /// <param name="state"></param>
        /// <param name="data"></param>
        public void LocalStateChange(string state, string data)
        {
            if (m_conference == null)
            {
                m_conference = (ConferenceChairGroup)ChairManager.Instance.GetChairGroupFromPlayer(PlayerManager.Instance.GetLocalPlayer());
            }

            if(m_info == null)
            {
                m_info = JsonUtility.FromJson<ContentsManager.ContentFileInfo>(m_conference.CurrentUploadedFile);
            }

            if (state.Equals("CLOSE"))
            {
                DeleteCallback(uploadID, m_info);
                //request upload to web client
                //ContentsManager.Instance.WebClientDeleteContent(uploadID, (int)type - 1, m_info.url);
            }
            else
            {
                //send RCP to all players with the state
                MMOManager.Instance.SendRPC("ConferenceContentControls", (int)MMOManager.RpcTarget.All, PlayerManager.Instance.GetLocalPlayer().ID, GetPlayerWrapper(), uploadID, m_info.extensiontype, state, data);
            }
        }

        /// <summary>
        /// Called on the reciever to perfrom a network change event
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="state"></param>
        /// <param name="data"></param>
        public void NetworkStateChange(IContentLoader loader, string state, string data = "")
        {
            if(loader != null)
            {
                loader.NetworkStateChange(state, data);
            }
        }

        /// <summary>
        /// upload callback when manager get responce
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fileInfo"></param>
        public void UploadCallback(string id, ContentsManager.ContentFileInfo fileInfo)
        {
            if (!uploadID.Equals(id)) return;

            Debug.Log("ConferenceContentUpload: Upload content callback:" + ID);

            if (ContentsManager.Instance.OnFileUpload != null)
            {
                ContentsManager.Instance.OnFileUpload -= UploadCallback;
            }
            
            m_info = fileInfo;
            IContentLoader loader = null;

            if(m_conference == null)
            {
                m_conference = (ConferenceChairGroup)ChairManager.Instance.GetChairGroupFromPlayer(PlayerManager.Instance.GetLocalPlayer());
            }

            //check conference display type
            if (m_conference.ContentDisplayType.Equals(ConferenceChairGroup.ScreenContentDisplayType.WorldCanvas))
            {
                //world content then load directly to canvas
                switch (m_info.extensiontype)
                {
                    case 1:
                        loader = m_conference.ContentLoaders[0].GetComponentInChildren<IContentLoader>(true);
                        loader.LocalStateChange = null;

                        loader.Owner = PlayerManager.Instance.GetLocalPlayer().ID;
                        loader.IsNetworked = true;
                        loader.Unload();
                        loader.Load(fileInfo);
                        loader.LocalStateChange += LocalStateChange;
                        m_conference.ContentLoaders[0].transform.localScale = Vector3.one;

                        m_conference.ContentLoaders[1].GetComponentInChildren<IContentLoader>(true).Unload();
                        m_conference.ContentLoaders[1].transform.localScale = Vector3.zero;

                        controller.Open(ContentsManager.ContentType.Video, this, loader);
                        break;
                    case 2:
                        loader = m_conference.ContentLoaders[1].GetComponentInChildren<IContentLoader>(true);
                        loader.LocalStateChange = null;

                        loader.Owner = PlayerManager.Instance.GetLocalPlayer().ID;
                        loader.IsNetworked = true;
                        loader.Unload();
                        loader.Load(fileInfo);
                        loader.LocalStateChange += LocalStateChange;
                        m_conference.ContentLoaders[1].transform.localScale = Vector3.one;

                        m_conference.ContentLoaders[0].GetComponentInChildren<IContentLoader>(true).Unload();
                        m_conference.ContentLoaders[0].transform.localScale = Vector3.zero;

                        controller.Open(ContentsManager.ContentType.Image, this, loader);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                //2d canvas load
                ContentsManager.Instance.OpenContentFileUsing2DScreen(m_info.url, true, PlayerManager.Instance.GetLocalPlayer().ID, LocalStateChange);
            }

            if (!m_conference.ContentUploadURLs.Contains(m_info.url))
            {
                m_conference.ContentUploadURLs.Add(m_info.url);
            }
            
            if(!ContentsManager.Instance.IsFileConferenceUpload(m_info.id))
            {
                ContentsManager.Instance.AddToConferenceUploads(m_info.id);
            }

            //need to send Room change on this IContentLoader to load file across network
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("EVENT_TYPE", "CONFERENCE");
            data.Add("E", "CE");
            data.Add("I", uploadID);
            data.Add("U", m_conference.ID);
            data.Add("A", "1");
            data.Add("F", fileInfo.id.ToString());
            //data.Add("F", JsonUtility.ToJson(fileInfo));
            data.Add("O", PlayerManager.Instance.GetLocalPlayer().ActorNumber.ToString());
            data.Add("P", GetPlayerWrapper());

            MMOManager.Instance.ChangeRoomProperty(uploadID, data);

            IsOpen = true;
        }

        /// <summary>
        /// delete callback when file is deleted via the manager
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fileInfo"></param>
        public void DeleteCallback(string id, ContentsManager.ContentFileInfo fileInfo)
        {
            if (!uploadID.Equals(id)) return;

            if (!AppManager.IsCreated) return;

            Debug.Log("ConferenceContentUpload: Delete content callback:" + ID);

            //remove this callback so it does not get called again
            //ContentsManager.Instance.OnFileDelete -= DeleteCallback;

            if (m_conference == null && ChairManager.Instance != null)
            {
                m_conference = (ConferenceChairGroup)ChairManager.Instance.GetChairGroupFromPlayer(PlayerManager.Instance.GetLocalPlayer());
            }

            //need to send Room change on this IContentLoader to delete file across network
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("EVENT_TYPE", "CONFERENCE");
            data.Add("E", "CE");
            data.Add("I", uploadID);
            data.Add("U", m_conference.ID);
            data.Add("A", "0");
            data.Add("F", fileInfo.id.ToString());
            //data.Add("F", JsonUtility.ToJson(fileInfo));
            data.Add("O", PlayerManager.Instance.GetLocalPlayer().ID);
            data.Add("P", GetPlayerWrapper());

            MMOManager.Instance.ChangeRoomProperty(uploadID, data);

            for (int i = 0; i < m_conference.ContentLoaders.Length; i++)
            {
                m_conference.ContentLoaders[i].GetComponentInChildren<IContentLoader>(true).Unload();
                m_conference.ContentLoaders[i].transform.localScale = Vector3.zero;
            }

            m_conference.CurrentUploadedFile = "";

            //set interaction as on
            m_info = null;
            IsOpen = false;
        }

        /// <summary>
        /// Called to open the controller
        /// </summary>
        /// <param name="type"></param>
        /// <param name="loader"></param>
        public void OpenController(ContentsManager.ContentType type, IContentLoader loader)
        {
            controller.Open(type, this, loader);
        }

        /// <summary>
        /// Called to turn off the controller
        /// </summary>
        public void TurnOffController()
        {
            controller.gameObject.SetActive(false);
        }

        /// <summary>
        /// Called to Network this upload state 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        public void OnNetworkedUpload(string id, string data)
        {
            if (!uploadID.Equals(id)) return;

            Debug.Log("ConferenceContentUpload: Network Upload content callback:" + ID);

            ContentsManager.ContentFileInfo fileInfo = JsonUtility.FromJson<ContentsManager.ContentFileInfo>(data);
            //ContentsManager.Instance.OnFileDelete += DeleteCallback;

            m_conference = (ConferenceChairGroup)ChairManager.Instance.GetChairGroupFromPlayer(PlayerManager.Instance.GetLocalPlayer());

            if (fileInfo != null)
            {
                switch (type)
                {
                    case ContentsManager.ContentType.Image:
                        m_conference.ContentLoaders[1].GetComponentInChildren<IContentLoader>(true).Unload();
                        m_conference.ContentLoaders[1].GetComponentInChildren<IContentLoader>(true).Load(fileInfo);
                        m_conference.ContentLoaders[1].transform.localScale = Vector3.one;
                        break;
                    case ContentsManager.ContentType.Video:
                        m_conference.ContentLoaders[1].GetComponentInChildren<IContentLoader>(true).Unload();
                        m_conference.ContentLoaders[0].GetComponentInChildren<IContentLoader>(true).Load(fileInfo);
                        m_conference.ContentLoaders[0].transform.localScale = Vector3.one;
                        break;
                    default:
                        break;
                }
            }

            m_info = fileInfo;

            if (!m_conference.ContentUploadURLs.Contains(m_info.url))
            {
                m_conference.ContentUploadURLs.Add(m_info.url);
            }

            if (!ContentsManager.Instance.IsFileConferenceUpload(m_info.id))
            {
                ContentsManager.Instance.AddToConferenceUploads(m_info.id);
            }

            IsOpen = true;
        }

        /// <summary>
        /// Called to network this delete state
        /// </summary>
        /// <param name="id"></param>
        public void OnNetworkedDelete(string id)
        {
            if (!uploadID.Equals(id)) return;

            Debug.Log("ConferenceContentUpload: Network Delete content callback:" + ID);

            //ContentsManager.Instance.OnFileDelete -= DeleteCallback;

            //find icontentloader and unload
            for (int i = 0; i < m_conference.ContentLoaders.Length; i++)
            {
                m_conference.ContentLoaders[i].GetComponentInChildren<IContentLoader>(true).Unload();
                m_conference.ContentLoaders[i].transform.localScale = Vector3.zero;
            }

            //is interactive now
            m_info = null;
            IsOpen = false;
        }

        /// <summary>
        /// Returns the players within this conference
        /// </summary>
        /// <returns></returns>
        private string GetPlayerWrapper()
        {
            //get all players in the conference
            List<IPlayer> players = ChairManager.Instance.GetPlayersInChairGroup(uploadID);

            PlayersConferenceWrapper wrapper = new PlayersConferenceWrapper();
            //wrapper.players.Add(PlayerManager.Instance.GetLocalPlayer().ID);

            if (players != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    wrapper.players.Add(players[i].ID);
                }
            }

            return JsonUtility.ToJson(wrapper);
        }

        [System.Serializable]
        public class PlayersConferenceWrapper
        {
            public List<string> players = new List<string>();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ConferenceContentUpload), true)]
        public class ConferenceContentUpload_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("type"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("controller"), true);

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
