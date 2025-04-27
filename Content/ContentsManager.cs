using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ContentsManager : Singleton<ContentsManager>
    {
        public static ContentsManager Instance
        {
            get
            {
                return ((ContentsManager)instance);
            }
            set
            {
                instance = value;
            }
        }

       
        private GameObject contentsPanel;
        private Transform tabFiles;

        [Header("UI Screens")]
        [SerializeField]
        private GameObject rootScreenHolder;
        [SerializeField]
        private List<ContentScreen> contentScreens = new List<ContentScreen>();

        [Header("Source Icons")]
        [SerializeField]
        private List<ContentTypeIcon> icons;

        /// <summary>
        /// Action to subscribe to when a file is uploaded (use in the WorldContentUpload.cs)
        /// </summary>
        public System.Action<string, ContentFileInfo> OnFileUpload { get; set; }
        /// <summary>
        /// Action to subscribe to when a file is deleted (use in the WorldContentUpload.cs)
        /// </summary>
        public System.Action<string, ContentFileInfo> OnFileDelete { get; set; }

        private string m_currentUpload = "";
        private List<int> m_conferenceUploads = new List<int>();
        private List<ContentFileInfo> m_allFiles = new List<ContentFileInfo>();

        private void Awake()
        {
            HUDManager.Instance.OnCustomSetupComplete += GetUIReferences;
            ContentsAPI.Instance.OnGetContentsCallback += SetWorldContentScreens;
        }

        /// <summary>
        /// Called once to set the world convases up based on contents of DB download
        /// </summary>  
        private void SetWorldContentScreens()
        {
            ContentsAPI.Instance.OnGetContentsCallback -= SetWorldContentScreens;

            WorldContentUpload[] worldContentUploaders = FindObjectsByType<WorldContentUpload>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            ConferenceChairGroup[] conferences = FindObjectsByType<ConferenceChairGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            bool firstPersonInRoom = false;
            List<int> filesToDelete = new List<int>();

            if (MMOManager.Instance.GetAllPlayers().Count <= 1)
            {
                firstPersonInRoom = true;
            }

            //need to ensure that all world content loaders currently loaded is in the files, if not delete
            if (!CoreManager.Instance.IsOffline)
            {
                for (int i = 0; i < worldContentUploaders.Length; i++)
                {
                    ContentFileInfo file = m_allFiles.FirstOrDefault(x => x.contentType.Equals(worldContentUploaders[i].ID));

                    if(file == null)
                    {
                        worldContentUploaders[i].OnNetworkedDelete(worldContentUploaders[i].ID);
                    }
                }
            }

            foreach (ContentFileInfo file in m_allFiles)
            {
                //need to apply all files to correct world uploader
                for(int i = 0; i < worldContentUploaders.Length; i++)
                {
                    if(worldContentUploaders[i].ID.Equals(file.contentType))
                    {
                        if (!CoreManager.Instance.IsOffline)
                        {
                            //check if contents URL has changed
                            if(!worldContentUploaders[i].URL.Equals(file.url))
                            {
                                worldContentUploaders[i].OnNetworkedDelete(worldContentUploaders[i].ID);
                            }
                        }

                        worldContentUploaders[i].OnNetworkedUpload(worldContentUploaders[i].ID, JsonUtility.ToJson(file));

                        break;
                    }
                }

                //need to apply all files to correct conference
                if(!CoreManager.Instance.IsOffline)
                {
                    for (int i = 0; i < conferences.Length; i++)
                    {
                        if (conferences[i].ID.Equals(file.contentType))
                        {
                            //ensure if fist person in room and there are conference uploads (meaning owner of conference disconnected before ending meeting)
                            if (firstPersonInRoom)
                            {
                                filesToDelete.Add(file.id);
                                continue;
                            }

                            AddToConferenceUploads(file.id);
                            conferences[i].ContentUploadURLs.Add(file.url);

                            if (conferences[i].ContentDisplayMode.Equals(ConferenceChairGroup.ScreenContentPrivacy.Global))
                            {
                                for (int j = 0; j < conferences[i].ContentLoaders.Length; j++)
                                {
                                    IContentLoader loader = conferences[i].ContentLoaders[j].GetComponent<IContentLoader>();
                                    ContentFileInfo temp = null;

                                    loader.Owner = "";
                                    loader.IsNetworked = true;

                                    //need to check if loaded or not
                                    if (!string.IsNullOrEmpty(conferences[i].CurrentUploadedFile))
                                    {
                                        temp = JsonUtility.FromJson<ContentFileInfo>(conferences[i].CurrentUploadedFile).OrDefaultWhen(x => x.uploadedBy == null && x.url == null);

                                        if (temp != null && temp.url.Equals(file.url))
                                        {
                                            loader.Load(file);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //delete all files that have been uploaded to a conference if this player is the first player in room
            if(firstPersonInRoom && filesToDelete.Count > 0)
            {
                ContentsAPI.Instance.DeleteContents(filesToDelete.ToArray());
            }
        }

        /// <summary>
        /// Called to add file if it has been loaded as a conference upload
        /// </summary>
        /// <param name="fileID"></param>
        public void AddToConferenceUploads(int fileID)
        {
            m_conferenceUploads.Add(fileID);
        }

        /// <summary>
        /// Check if file is conference upload
        /// </summary>
        /// <param name="fileID"></param>
        /// <returns></returns>
        public bool IsFileConferenceUpload(int fileID)
        {
            return m_conferenceUploads.Contains(fileID);
        }

        /// <summary>
        /// Called to taggle the visibility of the contents panel
        /// </summary>
        public void ToggleContentsScreen()
        {
            contentsPanel.SetActive(!contentsPanel.activeInHierarchy);
        }

        /// <summary>
        /// Returs the list of files within a screen type
        /// </summary>
        /// <param name="screenIndex"></param>
        /// <returns></returns>
        public List<ContentFileInfo> GetFileInfo(string type)
        {
            var files = new List<ContentFileInfo>();

            foreach (ContentFileInfo file in m_allFiles)
            {
                if (IsFileConferenceUpload(file.id)) continue;

                string extension = GetExtension(file.url);

                if(type.Equals("Video"))
                {
                    if(contentScreens[1].supportedFiles.Contains(extension))
                    {
                        files.Add(file);
                    }
                }
                else if(type.Equals("Image"))
                {
                    if (contentScreens[2].supportedFiles.Contains(extension))
                    {
                        files.Add(file);
                    }
                }
                else if(type.Equals("PDF"))
                {
                    if (contentScreens[0].supportedFiles.Contains(extension))
                    {
                        files.Add(file);
                    }
                }
                else
                {
                    files.Add(file);
                }
            }

            return files;
        }

        public ContentFileInfo GetFileInfo(int id)
        {
            return m_allFiles.FirstOrDefault(x => x.id.Equals(id));
        }

        /// <summary>
        /// Called to open a file onto the main UI 2D screen - non World
        /// </summary>
        /// <param name="screenIndex"></param>
        /// <param name="file"></param>
        public void OpenContentFileUsing2DScreen(string file, bool isNetworked = false, string owner = "", System.Action<string, string> eventChange = null)
        {
            string extension = GetExtension(file);
            var screen = contentScreens.FirstOrDefault(x => x.supportedFiles.Contains(extension));

            if (screen != null)
            {
                screen.display.SetActive(true);
                IContentLoader iLoader = screen.display.GetComponent<IContentLoader>();

                iLoader.Owner = owner;
                iLoader.IsNetworked = isNetworked;
                iLoader.LocalStateChange += eventChange;
                iLoader.Load(file);
            }
        }

        /// <summary>
        /// Called to close a 2D screen if its been opened
        /// </summary>
        /// <param name="screenIndex"></param>
        public void CloseContentFileUsing2DScreen()
        {
            contentScreens.ForEach(x => x.display.gameObject.SetActive(false));
        }

        /// <summary>
        /// Called to update the owner of a 2D screen that has been opened
        /// </summary>
        /// <param name="owner"></param>
        public void UpdateOwnerOf2DScreens(string owner)
        {
            for (int i = 0; i < contentScreens.Count; i++)
            {
                if (contentScreens[i].display == null) continue;

                if (contentScreens[i].display.activeInHierarchy)
                {
                    IContentLoader cLoader = contentScreens[i].display.GetComponent<IContentLoader>();
                    cLoader.LocalStateChange("OWNER", owner);
                }
            }
        }

        /// <summary>
        /// Called to close all 2D screens if opened
        /// </summary>
        public void CloseAllContentFileUsing2DScreen()
        {
            for(int i = 0; i < contentScreens.Count; i++)
            {
                if (contentScreens[i].display == null) continue;

                if(contentScreens[i].display.activeInHierarchy)
                {
                    IContentLoader cLoader = contentScreens[i].display.GetComponent<IContentLoader>();
                    WebClientDeleteContent("", cLoader.URL);
                }

                contentScreens[i].display.SetActive(false);
            }
        }

        /// <summary>
        /// Called to update the local contents based on the DL request 
        /// </summary>
        /// <param name="contents"></param>
        public void UpdateContents(ContentsAPI.ContentEntries contents)
        {
            //this needs to change to all files
            m_allFiles.Clear();

            if (contents == null) return;

            //create all files to the correct screen
            foreach (ContentsAPI.ContentEntry entry in contents.entries)
            {
                ContentFileInfo info = new ContentFileInfo();
                info.id = entry.ID;
                info.url = entry.url;
                info.uploadedBy = entry.uploaded_by;
                info.contentType = entry.content_index;

                string extension = GetExtension(entry.url);
                info.extensiontype = GetContentEnumFromExtension(extension);

                m_allFiles.Add(info);
            }

            //contents panel open, update
            if (contentsPanel.activeInHierarchy)
            {
                contentsPanel.GetComponent<ContentsPanel>().CreateDisplay();
            }

            if(CoreManager.Instance.IsOffline && CoreManager.Instance.projectSettings.syncContentDisplayOffline)
            {
                SetWorldContentScreens();
            }
        }

        private void GetUIReferences()
        {
            if (contentsPanel != null) return;

            HUDManager.Instance.OnCustomSetupComplete -= GetUIReferences;

            contentsPanel = HUDManager.Instance.GetHUDScreenObject("CONTENTS_SCREEN");
            tabFiles = contentsPanel.GetComponentInChildren<ContentsPanel>().TabFiles.transform;

            for (int i = 0; i < contentScreens.Count; i++)
            {
                if (contentScreens[i].type == "Presentation") continue;

                if(contentScreens[i].type == "Image")
                {
                    contentScreens[i].display = rootScreenHolder.GetComponentInChildren<ContentImageScreen>(true).gameObject;
                }
                else if(contentScreens[i].type == "Video")
                {
                    contentScreens[i].display = rootScreenHolder.GetComponentInChildren<ContentVideoScreen>(true).gameObject;
                }
            }

            //esnure the fist toggle found on the tabs is on
            contentsPanel.SetActive(false);
        }

        /// <summary>
        /// Returns the enum from an file extension type
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        private int GetContentEnumFromExtension(string extension)
        {
            foreach(ContentScreen screen in contentScreens)
            {
                if(screen.supportedFiles.Contains(extension))
                {
                    return screen.index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the icon used for a message
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Sprite GetLogTypeIcon(ContentType type)
        {
            return icons.FirstOrDefault(x => x.contentType.Equals(type)).icon;
        }

        /// <summary>
        /// Returns the actual file based on the URL param
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public ContentFileInfo GetContentFromURL(string url)
        {
            return m_allFiles.FirstOrDefault(x => x.url.Equals(url));
        }

        /// <summary>
        /// Request to delete an uploaded file on a world screen
        /// </summary>
        /// <param name="uploadID"></param>
        /// <param name="screenIndex"></param>
        /// <param name="urlRef"></param>
        public void WebClientDeleteContent(string uploadID, string urlRef)
        {
            //locate the file based on url
            ContentFileInfo file = m_allFiles.FirstOrDefault(x => x.url.Equals(urlRef));

            if (file != null)
            {
                //delete
                int[] contents = new int[] { file.id };

                if (OnFileDelete != null)
                {
                    OnFileDelete.Invoke(uploadID, file);
                }

                ContentsAPI.Instance.DeleteContents(contents);
            }
        }

        /// <summary>
        /// Request to delete a number of contents on the world screens
        /// </summary>
        /// <param name="uploadID"></param>
        /// <param name="urlRefs"></param>
        public void WebClientDeleteContentGroup(string uploadID, string[] urlRefs)
        {
            int[] contents = new int[urlRefs.Length];

            for (int i = 0; i < urlRefs.Length; i++)
            {
                ContentFileInfo file = m_allFiles.FirstOrDefault(x => x.url.Equals(urlRefs[i]));

                if (file != null)
                {
                    contents[i] = file.id;

                    if (m_conferenceUploads.Contains(file.id))
                    {
                        m_conferenceUploads.Remove(file.id);
                    }
                }
            }

            ContentsAPI.Instance.DeleteContents(contents);
        }

        /// <summary>
        /// Request to open the webclient upload based on uniqueIndex
        /// </summary>
        /// <param name="uploadID"></param>
        /// <param name="screenIndex"></param>
        public void WebClientUploadWorldContent(string uploadID, string type)
        {
            string fileTypes = "";

            if (type.Equals("Video"))
            {
                fileTypes = contentScreens[1].supportedFiles;
            }
            else if (type.Equals("Image"))
            {
                fileTypes = contentScreens[2].supportedFiles;
            }
            else if (type.Equals("PDF"))
            {
                fileTypes = contentScreens[0].supportedFiles;
            }
            else
            {
                fileTypes = /*contentScreens[0].supportedFiles + "," + */contentScreens[1].supportedFiles + "," + contentScreens[2].supportedFiles;
            }

            var message = new UploadMessage(true, fileTypes);
            var json = JsonUtility.ToJson(message);

            Debug.Log("WebClientUploadWorldContent: " + json);

            m_currentUpload = uploadID;

            //add responce listener and send
            WebclientManager.WebClientListener += ResponceCallback;
            WebclientManager.Instance.Send(json);
        }

        /// <summary>
        /// Request to open the webclient upload display based on 2D screen type
        /// </summary>
        /// <param name="uploadID"></param>
        /// <param name="screenType"></param>
        public void WebClient2DUploadContent(string uploadID, string screenType)
        {
            //get screen
            var screen = contentScreens.FirstOrDefault(x => x.type.Equals(screenType));

            if (screen != null)
            {
                WebClient2DUploadContent(uploadID, screen);
            }
        }

        /// <summary>
        /// Request to open the webclient upload display based on 2D screen index
        /// </summary>
        /// <param name="uploadID"></param>
        /// <param name="screenIndex"></param>
        public void WebClient2DUploadContent(string uploadID, int screenIndex)
        {
            //get screen
            var screen = contentScreens.FirstOrDefault(x => x.index.Equals(screenIndex));

            if (screen != null)
            {
                WebClient2DUploadContent(uploadID, screen);
            }
        }

        /// <summary>
        /// Process the qeb client request
        /// </summary>
        /// <param name="uploadID"></param>
        /// <param name="content"></param>
        private void WebClient2DUploadContent(string uploadID, ContentScreen content)
        {
            string fileTypes = content.supportedFiles;

            var message = new UploadMessage(true, fileTypes);
            var json = JsonUtility.ToJson(message);

            Debug.Log("WebClient2DUploadContent: " + json);

            m_currentUpload = uploadID;

            //add responce listener and send
            WebclientManager.WebClientListener += ResponceCallback;
            WebclientManager.Instance.Send(json);
        }

        /// <summary>
        /// Responce listener for the web client upon upload
        /// </summary>
        /// <param name="obj"></param>
        private void ResponceCallback(string obj)
        {
            //ensure reponce data is tpye responce
            UploadResponce responce = JsonUtility.FromJson<UploadResponce>(obj).OrDefaultWhen(x => x.url == null);

            Debug.Log("WebClientUploadContentResponce: " + obj);

            //if responce
            if (responce != null)
            {
                //remove listener
                WebclientManager.WebClientListener -= ResponceCallback;

                string fileExtension = GetExtension(responce.url);

                //send DB request to post new content
                IPlayer player = PlayerManager.Instance.GetLocalPlayer();
                string uploadedBy = PlayerManager.Instance.GetPlayerName(player.NickName) + " [" + player.ActorNumber + "]";
                ContentsAPI.Instance.PostContents(responce.url, fileExtension, m_currentUpload, uploadedBy);

                //local file upload callbck
                if(OnFileUpload != null)
                {
                    ContentFileInfo info = new ContentFileInfo();
                    info.id = 0;
                    info.url = responce.url;
                    info.uploadedBy = uploadedBy;
                    info.contentType = m_currentUpload;
                    info.extensiontype = GetContentEnumFromExtension(fileExtension);
                    OnFileUpload.Invoke(m_currentUpload, info);
                    m_currentUpload = "";
                }
            }
        }

        /// <summary>
        /// Called to networked any world content screen via the WorldContentUpload.cs script
        /// </summary>
        /// <param name="id"></param>
        /// <param name="actionName"></param>
        /// <param name="data"></param>
        /// <param name="command"></param>
        public void NetworkWorldScreen(string id, string actionName, string data, string command)
        {
            WorldContentUpload[] allUploads = FindObjectsByType<WorldContentUpload>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Debug.Log("NetworkWorldScreen: " + id + "|" + actionName);

            for (int i = 0; i < allUploads.Length; i++)
            {
                if(actionName.Equals("1"))
                {
                    allUploads[i].OnNetworkedUpload(id, data);
                }
                else if(actionName.Equals("0"))
                {
                    allUploads[i].OnNetworkedDelete(id);
                }
            }
        }

        /// <summary>
        /// Called to network a 2D screen
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="actionName"></param>
        /// <param name="rawData"></param>
        public void NetworkConferenceScreen(string conferenceID, string owner, string actionName, string rawData)
        {
            ConferenceContentUpload[] all = FindObjectsByType<ConferenceContentUpload>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            ContentFileInfo fileInfo = GetFileInfo(int.Parse(rawData));
            //ContentFileInfo fileInfo = JsonUtility.FromJson<ContentFileInfo>(rawData);

            ConferenceChairGroup conGroup = (ConferenceChairGroup)ChairManager.Instance.GetChairGroupFromID(conferenceID);

            Debug.Log("NetworkConferenceScreen: " + conferenceID + "|" + owner + "|" + actionName + "|" + rawData);

            //if player is not part of the network group
            if (owner.Equals("OTHERS"))
            {
                if (actionName.Equals("1"))
                {
                    if (!m_conferenceUploads.Contains(fileInfo.id))
                    {
                        AddToConferenceUploads(fileInfo.id);
                    }

                    if (!conGroup.ContentUploadURLs.Contains(fileInfo.url))
                    {
                        conGroup.ContentUploadURLs.Add(fileInfo.url);
                    }
                }

                //check conference display type
                if (conGroup.ContentDisplayType.Equals(ConferenceChairGroup.ScreenContentDisplayType.WorldCanvas))
                {
                    for (int j = 0; j < conGroup.ContentLoaders.Length; j++)
                    {
                        IContentLoader loader = conGroup.ContentLoaders[j].GetComponent<IContentLoader>();
                        loader.Unload();
                        conGroup.ContentLoaders[j].transform.localScale = Vector3.zero;
                        
                        //check loader type against file extension
                        if (loader is ContentImageScreen && fileInfo.extensiontype.Equals(2))
                        {
                            loader.Owner = owner;
                            loader.IsNetworked = true;

                            if(actionName.Equals("1"))
                            {
                                //if global display then load content
                                if (conGroup.ContentDisplayMode.Equals(ConferenceChairGroup.ScreenContentPrivacy.Global))
                                {
                                    loader.Load(fileInfo);
                                    conGroup.ContentLoaders[j].transform.localScale = Vector3.one;
                                }
                            }
                        }
                        else if (loader is ContentVideoScreen && fileInfo.extensiontype.Equals(1))
                        {
                            loader.Owner = owner;
                            loader.IsNetworked = true;

                            if (actionName.Equals("1"))
                            {
                                //if global display then load content
                                if (conGroup.ContentDisplayMode.Equals(ConferenceChairGroup.ScreenContentPrivacy.Global))
                                {
                                    loader.Unload();
                                    loader.Load(fileInfo);

                                    conGroup.ContentLoaders[j].transform.localScale = Vector3.one;
                                }
                            }
                        }
                        else
                        {

                        }
                    }
                }

                return;
            }
            IPlayer tempPlayer = PlayerManager.Instance.GetPlayer(int.Parse(owner));


            //check conference display type
            if (conGroup.ContentDisplayType.Equals(ConferenceChairGroup.ScreenContentDisplayType.WorldCanvas))
            {
                for (int j = 0; j < conGroup.ContentLoaders.Length; j++)
                {
                    IContentLoader loader = conGroup.ContentLoaders[j].GetComponent<IContentLoader>();
                    loader.Unload();
                    conGroup.ContentLoaders[j].transform.localScale = Vector3.zero;

                    //check loader type against file extension, if true then load content as player is in conference
                    if (loader is ContentImageScreen && fileInfo.extensiontype.Equals(2))
                    {
                        loader.Owner = tempPlayer.ID;
                        loader.IsNetworked = true;

                        if (actionName.Equals("1"))
                        {
                            if (!m_conferenceUploads.Contains(fileInfo.id))
                            {
                                AddToConferenceUploads(fileInfo.id);
                            }

                            if (!conGroup.ContentUploadURLs.Contains(fileInfo.url))
                            {
                                conGroup.ContentUploadURLs.Add(fileInfo.url);
                            }

                            loader.Unload();
                            loader.Load(fileInfo);
                            conGroup.ContentLoaders[j].transform.localScale = Vector3.one;
                        }
                    }
                    else if (loader is ContentVideoScreen && fileInfo.extensiontype.Equals(1))
                    {
                        loader.Owner = tempPlayer.ID;
                        loader.IsNetworked = true;

                        if (actionName.Equals("1"))
                        {
                            if (!m_conferenceUploads.Contains(fileInfo.id))
                            {
                                AddToConferenceUploads(fileInfo.id);
                            }

                            if (!conGroup.ContentUploadURLs.Contains(fileInfo.url))
                            {
                                conGroup.ContentUploadURLs.Add(fileInfo.url);
                            }

                            loader.Unload();
                            loader.Load(fileInfo);

                            conGroup.ContentLoaders[j].transform.localScale = Vector3.one;
                        }
                    }
                    else
                    {

                    }
                }
            }
            else
            {
                //open and update screen content
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].Type.Equals(fileInfo.contentType))
                    {
                        all[i].FileInfo = fileInfo;
                        all[i].IsOpen = true;

                        if (fileInfo != null)
                        {
                            if (actionName.Equals("1"))
                            {
                                OpenContentFileUsing2DScreen(fileInfo.url, true, tempPlayer.ID, all[i].LocalStateChange);

                                if(!m_conferenceUploads.Contains(fileInfo.id))
                                {
                                    AddToConferenceUploads(fileInfo.id);
                                }
                            
                                if(!conGroup.ContentUploadURLs.Contains(fileInfo.url))
                                {
                                    conGroup.ContentUploadURLs.Add(fileInfo.url);
                                }
                            }
                            else
                            {
                                CloseContentFileUsing2DScreen();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called to appluy network event on a 2D screen
        /// </summary>
        /// <param name="uploadID"></param>
        /// <param name="screenIndex"></param>
        /// <param name="state"></param>
        /// <param name="data"></param>
        public void NetworkScreenEvent(string uploadID, int screenIndex, string state, string data = "")
        {
            ConferenceContentUpload[] all = FindObjectsByType<ConferenceContentUpload>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            ConferenceChairGroup conGroup = (ConferenceChairGroup)ChairManager.Instance.GetChairGroupFromID(uploadID);

            Debug.Log("NetworkScreenEvent: " + uploadID + "|" + data);

            if (conGroup != null && conGroup.ContentDisplayType.Equals(ConferenceChairGroup.ScreenContentDisplayType.WorldCanvas))
            {
                for (int j = 0; j < conGroup.ContentLoaders.Length; j++)
                {
                    IContentLoader loader = conGroup.ContentLoaders[j].GetComponent<IContentLoader>();

                    if (loader is ContentImageScreen && screenIndex.Equals(2))
                    {
                        loader.NetworkStateChange(state, data);
                        break;
                    }
                    else if (loader is ContentVideoScreen && screenIndex.Equals(1))
                    {
                        loader.NetworkStateChange(state, data);
                        break;
                    }
                    else
                    {
                        
                    }
                }
                return;
            }

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].ID.Equals(uploadID))
                {
                    ContentScreen screen = contentScreens.FirstOrDefault(x => x.index.Equals(screenIndex));

                    if (screen != null)
                    {
                        all[i].NetworkStateChange(screen.display.GetComponent<IContentLoader>(), state, data);
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// Function called to find file extension - replaces System.IO.Path.GetExtension as this wont work on WebGL
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private string GetExtension(string source)
        {
            int n = source.Length - 1;
            string extension = "";

            for(int i = n; i > 0; i--)
            {
                if(source[i].Equals('.'))
                {
                    extension += source[i];
                    break;
                }
                else
                {
                    extension += source[i];
                }
            }

            char[] output = extension.ToCharArray();
            System.Array.Reverse(output);

            return new string(output);
        }

        [System.Serializable]
        public class UploadMessage
        {
            public bool BeginUpload;
            public string SupportedFiletypes;

            public UploadMessage(bool upload, string filetypes)
            {
                BeginUpload = upload;
                SupportedFiletypes = filetypes;
            }
        }

        [System.Serializable]
        public class UploadResponce
        {
            public string url;
        }

        [System.Serializable]
        private class ContentScreen
        {
            public string type;
            public int index;
            public GameObject display;
            public string supportedFiles;

            public List<ContentFileInfo> files = new List<ContentFileInfo>();
        }

        [System.Serializable]
        public class ContentFileInfo
        {
            public int id;
            public string url;
            public string uploadedBy;
            public string contentType;

            public int extensiontype;
        }

        [System.Serializable]
        public enum ContentType { All, PDF, Video, Image }

        [System.Serializable]
        private class ContentTypeIcon
        {
            public ContentType contentType;
            public Sprite icon;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ContentsManager), true)]
        public class ContentsManager_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rootScreenHolder"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("contentScreens"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("icons"), true);

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

    /// <summary>
    /// interface for the types of content screens
    /// </summary>
    public interface IContentLoader
    {
        void Load(string file);
        void Load(ContentsManager.ContentFileInfo file);

        void Unload();

        string ID { get; set; }

        string URL { get; }

        Lock LockUsed { get; set; }

        bool IsNetworked { get; set; }

        bool IsLoaded { get; }

        string Owner { get; set; }

        System.Action<string, string> LocalStateChange { get; set; }

        void NetworkStateChange(string state, string data = "");

        string Data { get; }
    }
}
