using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// WebClient Simulator to simulate responses from the webclient
    /// - use for testing messaging between app/webclient
    /// </summary>
    public class WebClientSimulator : Singleton<WebClientSimulator>
    {
        [Header("Content Upload Simulation")]
        [SerializeField]
        private List<ContentPair> contentSamples = new List<ContentPair>
        {
            new ContentPair("png", "https://www.brandlab360.co.uk/contentExamples/lambo.png"),
            new ContentPair("jpg", "https://www.brandlab360.co.uk/contentExamples/porsche.jpg"),
            new ContentPair("mp4", "https://www.brandlab360.co.uk/contentExamples/contentSampleMp4.mp4"),
            new ContentPair("mov", "https://www.brandlab360.co.uk/contentExamples/contentSampleMov.mov"),
            new ContentPair("avi", "https://www.brandlab360.co.uk/contentExamples/contentSampleAvi.avi"),
            new ContentPair("ProductPlacement", "/com.brandlab360.core/Runtime/Textures/ProductPlacement.png")
        };
        [SerializeField]
        private GameObject uploadPanel;
        [SerializeField]
        private List<UploadButtonPair> uploadButtons = new System.Collections.Generic.List<UploadButtonPair>();

        [Header("Started Message Simulation")]
        [SerializeField]
        private bool simulateIsMobile = false;
        [SerializeField]
        private int simulateRoomNumber = 0;
        [SerializeField]
        private float startedMsgDelaySecs = 2;

        [Header("Orientation")]
        [SerializeField]
        private OrientationType simulateOrientation = OrientationType.landscape;
        [SerializeField]
        private int simulateScreenWidth = 1920;
        [SerializeField]
        private int simulateScreenHeight = 1080;

        [Header("Admin")]
        [SerializeField]
        private bool isAdminUser = false;

        [Header("WebClientComms Simulation")]
        [SerializeField]
        private bool enableURLParamSimulation = true;

        [Header("Indexed DB Simulation")]
        [SerializeField]
        private UserData userData;
        [SerializeField]
        private PlayerManager.PlayerControlSettings playerControlsData;

        [Header("Video Chat Simulation")]
        [SerializeField]
        private GameObject videoChatScreen;
        [SerializeField]
        private TMP_Text videoChatUsername;
        [SerializeField]
        private RawImage webcamImage;

        [Header("Mobile Web Input Simulation")]
        [SerializeField]
        private GameObject mobileWebInputScreen;
        [SerializeField]
        private TMP_InputField mobileWebInputfield;
        [SerializeField]
        private TMP_Text mobileWebInputButtonLabel;
        [SerializeField]
        private TMP_Text mobileWebInputPlaceholder;



        private Dictionary<string, string> contentSamplesDict = new Dictionary<string, string>();
        private WebCamTexture webcamTexture;
        private string m_uploadRequestType = "";

        public static WebClientSimulator Instance
        {
            get
            {
                return ((WebClientSimulator)instance);
            }
            set
            {
                instance = value;
            }
        }

        private void Start()
        {
            // Disable the simulator if running on a build and with a streamingmode set
            // (failsafe incase it gets left in a production build by mistake

            isAdminUser = AppManager.Instance.Settings.editorTools.simulateAdmin;
            simulateIsMobile = AppManager.Instance.Settings.editorTools.simulateMobile;
            simulateOrientation = AppManager.Instance.Settings.editorTools.simulateOrientation;
            enableURLParamSimulation = AppManager.Instance.Settings.editorTools.simulateURLParams;

            simulateScreenWidth = (int)AppManager.Instance.Settings.editorTools.simulateScreenSize.x;
            simulateScreenHeight = (int)AppManager.Instance.Settings.editorTools.simulateScreenSize.y;

            var streamingMode = AppManager.Instance.Settings.projectSettings.streamingMode;
            if (!Application.isEditor && streamingMode != WebClientMode.None)
            {
                gameObject.SetActive(false);
            }
            else
            {
                DontDestroyOnLoad(this.gameObject);
                WebclientManager.WebClientListener += SimulateResponse;

                foreach (var contentPair in contentSamples)
                {
                    contentSamplesDict.Add(contentPair.Key, contentPair.Value);
                }

               // StartCoroutine(simulateStartedMessage(startedMsgDelaySecs));
                //StartCoroutine(simulateURLParamsMessage(startedMsgDelaySecs));
                StartCoroutine(simulateOrientationMessage());
            }
        }

        void OnDestroy()
        {
            if (!gameObject.scene.isLoaded) return;

            WebclientManager.WebClientListener -= SimulateResponse;
        }

        /// <summary>
        /// Simulate the response from the webclient
        /// </summary>
        /// <param name="data">the json data sent from unity app</param>
        public void SimulateResponse(string data)
        {
            if (data.Contains("productUploadRequest"))
            {
                ProductAPI.ProductUploadRequest uploadRpoductMessage = JsonUtility.FromJson<ProductAPI.ProductUploadRequest>(data);
                if (uploadRpoductMessage != null)
                {
                    Debug.Log("Simulating Product UploadMessage Response");
                    m_uploadRequestType = "ProductPlacement";
                    simulateContentUpload(uploadRpoductMessage.format);
                    return;
                }
            }

            //Upload Message
            var uploadMessage = JsonUtility.FromJson<ContentsManager.UploadMessage>(data).OrDefaultWhen(x => x.SupportedFiletypes == null);
            if (uploadMessage != null)
            {
                Debug.Log("Simulating UploadMessage Response");
                m_uploadRequestType = "";
                simulateContentUpload(uploadMessage.SupportedFiletypes);
                return;
            }

            var videoChatMessage = JsonUtility.FromJson<ToggleVideoChatMessage>(data).OrDefaultWhen(x => x.Channel == null && x.Username == null);
            if (videoChatMessage != null)
            {
                Debug.Log("Simulating ToggleVideoChatMessage Response");
                simulateVideoChat(videoChatMessage);
                return;
            }

            var webInputTextMessage = JsonUtility.FromJson<WebInputTextMessage>(data).OrDefaultWhen(x => x.button == null && x.input == null && x.type == null);
            if(webInputTextMessage != null)
            {
                Debug.Log("Simulating WebInputTextMessage");
                simulateWebInputTextMessage(webInputTextMessage);
                return;
            }

            var webIndexedDBMessage = JsonUtility.FromJson<iDbResponse>(data).OrDefaultWhen(x => x.iDbEntry == null);
            if (webIndexedDBMessage != null)
            {
                Debug.Log("Simulating WebIndexedDBMessage");
                SimulateIndexedDBResponce(data);
                return;
            }

            if (data.Contains("closeWebInput"))
            {
                TextEntryCloseMessage textEntryCloseMessage = JsonUtility.FromJson<TextEntryCloseMessage>(data);

                if (textEntryCloseMessage != null)
                {
                    Debug.Log("Simulating TextEntryCloseMessage");
                    simulateWebInputCloseTextMessage();
                    return;
                }
            }
        }

        /// <summary>
        /// Constructs a started response and process by webclient manager
        /// </summary>
        public IEnumerator simulateStartedMessage()
        {
            yield return new WaitForSeconds(startedMsgDelaySecs);

            var startedMessage = new StartedResponse();
            startedMessage.name = AppManager.Instance.Settings.projectSettings.ProjectID;
            startedMessage.room = simulateRoomNumber;
            startedMessage.isMobile = simulateIsMobile;
            var json = JsonUtility.ToJson(startedMessage);

            WebclientManager.Instance.ProcessReceiveJson(json);
        }

        /// <summary>
        /// Constructs a started response and process by webclient manager
        /// </summary>
        public IEnumerator simulateOrientationMessage()
        {
            yield return new WaitForSeconds(startedMsgDelaySecs);

            OnSimulateOrientation(simulateOrientation);
        }

        /// <summary>
        /// Constructs a started response and process by webclient manager
        /// </summary>
        public IEnumerator simulateURLParamsMessage()
        {
            yield return new WaitForSeconds(startedMsgDelaySecs);

            OnSimulateURLResponse();
        }


        /// <summary>
        /// Simulate the video chat by displaying local webcam
        /// </summary>
        /// <param name="isOn"></param>
        private void simulateVideoChat(ToggleVideoChatMessage videoChatMessage)
        {
            if (videoChatMessage.ToggleVideoChat)
            {
                WebCamDevice[] devices = WebCamTexture.devices;

                if (devices.Length > 0 && webcamTexture == null)
                {
                    videoChatScreen.SetActive(true);
                    webcamTexture = new WebCamTexture(devices[0].name);
                    webcamImage.texture = webcamTexture;
                    webcamImage.color = Color.white;

                    if(!string.IsNullOrEmpty(webcamTexture.deviceName))
                    {
                        webcamTexture.Play();
                    }
                   
                    videoChatUsername.text = videoChatMessage.Username;
                }
            }
            else
            {
                if (webcamTexture != null)
                {
                    webcamTexture.Stop();
                    webcamTexture = null;
                    videoChatScreen.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Show the upload panel
        /// </summary>
        /// <param name="supportedFiletypes">supported filetypes requested by unity app</param>
        private void simulateContentUpload(string supportedFiletypes)
        {
            uploadPanel.SetActive(true);

            foreach(var btn in uploadButtons)
            {
                if(supportedFiletypes.Contains(btn.Key))
                {
                    btn.Button.SetActive(true);
                }
                else
                {
                    btn.Button.SetActive(false);
                }
            }
        }

        private void simulateWebInputTextMessage(WebInputTextMessage webInputTextMessage)
        {
            if (webInputTextMessage == null || string.IsNullOrEmpty(webInputTextMessage.type)) return;

            mobileWebInputfield.text = "";

            mobileWebInputPlaceholder.text = webInputTextMessage.input;
            mobileWebInputButtonLabel.text = webInputTextMessage.button;
            
            var inputType = (WebInputType)System.Enum.Parse(typeof(WebInputType), webInputTextMessage.type);
            if(inputType == WebInputType.text)
            {
                mobileWebInputfield.contentType = TMP_InputField.ContentType.Standard;
                
            } else if(inputType == WebInputType.password)
            {
                mobileWebInputfield.contentType = TMP_InputField.ContentType.Password;

            } else if(inputType == WebInputType.number)
            {
                mobileWebInputfield.contentType = TMP_InputField.ContentType.IntegerNumber;

            } else if(inputType == WebInputType.email)
            {
                mobileWebInputfield.contentType = TMP_InputField.ContentType.EmailAddress;
            }

            mobileWebInputScreen.SetActive(true);
        }

        private void simulateWebInputCloseTextMessage()
        {
            mobileWebInputScreen.SetActive(false);
        }

        private void SimulateIndexedDBResponce(string responce)
        {
            //WebclientManager.WebClientListener -= SimulateResponse;
            IndexedDbManager.Instance.receiveWebClientJson(responce);
        }

        private void SimulateURLResponseResponce(string responce)
        {
            //WebclientManager.WebClientListener -= SimulateResponse;
            WebClientCommsManager.Instance.receiveUrlParamResponse(responce);
        }

        public void ProcessIndexedDBRequest(string type)
        {
            Debug.Log("Simulating Webclient response to IndexedDB GetEntry");
            WebclientManager.WebClientListener += IndexedDbManager.Instance.receiveWebClientJson;

            //WebclientManager.WebClientListener += SimulateResponse;
            var uploadResponse = new iDbResponse();

            if (type.Equals("userData"))
            {
                var dbData = new DBUserData();
                dbData.isAdmin = isAdminUser ? 1 : 0;
                dbData.user = userData.user;
                dbData.friends = "Friends-";
                dbData.profile = "Profile-";

                switch (AppManager.Instance.Settings.projectSettings.avatarSetupMode)
                {
                    case AvatarSetupMode.Simple:
                        dbData.json = userData.simpleAvatarJson;
                        break;
                    case AvatarSetupMode.Standard:
                        dbData.json = userData.standardAvatarJson;
                        break;
                    case AvatarSetupMode.Custom:
                        dbData.json = userData.customAvatarName;
                        break;
                    case AvatarSetupMode.ReadyPlayerMe:

                        if(AppManager.Instance.Settings.projectSettings.readyPlayerMeMode.Equals(ReadyPlayerMeMode.Fixed))
                        {
                            dbData.json = userData.RPMAvatar;
                        }
                        else
                        {
                            //need to add stuff for json string of avatar
                            dbData.json = "https://models.readyplayer.me/6400c6c5ce7f75d51cda2223.glb";
                        }
                        
                        break;
                }

                uploadResponse.iDbEntry = JsonUtility.ToJson(dbData);
            }
            else if (type.Equals("playerControlsData"))
            {
                uploadResponse.iDbEntry = "walk-" + playerControlsData.walk + "|" + "run-" + playerControlsData.run
                    + "|" + "strife-" + playerControlsData.strife + "|" + "mouse-" + playerControlsData.mouse +"|" + "invertX-" + playerControlsData.invertX + "|" + "invertY-" + playerControlsData.invertY; ;
            }
           

            var json = JsonUtility.ToJson(uploadResponse);
            WebclientManager.Instance.ProcessReceiveJson(json);
        }


        /// <summary>
        /// On clicking a filetype to upload
        /// </summary>
        /// <param name="fileType">the file extension to upload</param>
        public void OnClickUploadButton(string fileType)
        {
            if(contentSamplesDict.ContainsKey(fileType))
            {
                Debug.Log("Simulating Webclient response to UploadMessage");

                if(m_uploadRequestType.Equals("ProductPlacement"))
                {
                    var uploadResponse = new ProductAPI.ProductUploadResponce();
                    uploadResponse.url = contentSamplesDict[fileType];
                    var json = JsonUtility.ToJson(uploadResponse);
                    WebclientManager.Instance.ProcessReceiveJson(json);
                }
                else
                {
                    var uploadResponse = new ContentsManager.UploadResponce();
                    uploadResponse.url = contentSamplesDict[fileType];
                    var json = JsonUtility.ToJson(uploadResponse);
                    WebclientManager.Instance.ProcessReceiveJson(json);
                }

                uploadPanel.SetActive(false);
            }
        }

        public void OnClickMobileWebInputSubmit()
        {
            var textInput = mobileWebInputfield.text;
            mobileWebInputScreen.SetActive(false);
            var webInputResponse = new WebInputResponse();
            webInputResponse.btn_clicked = mobileWebInputButtonLabel.text;
            webInputResponse.values = mobileWebInputfield.text;
            var json = JsonUtility.ToJson(webInputResponse);

            WebclientManager.Instance.ProcessReceiveJson(json);
        }

        public void OnClickMobileWebInputCancel()
        {
            mobileWebInputScreen.SetActive(false);
        }

        public void OnSimulateURLResponse()
        {
            WebclientManager.WebClientListener += WebClientCommsManager.Instance.receiveUrlParamResponse;

            string str = "{\"urlParams\":{}}";

            if (enableURLParamSimulation)
            {
                str = "{\"urlParams\":{";
                str += "\"Username\":\"" + userData.user + "\",";
                str += "\"IsAdmin\":\"" + (isAdminUser ? "1" : "0") + "\",";
                str += "\"Avatar\":";

                switch (AppManager.Instance.Settings.projectSettings.avatarSetupMode)
                {
                    case AvatarSetupMode.Simple:
                        str += "\"" + userData.simpleAvatarJson + "\"";
                        break;
                    case AvatarSetupMode.Standard:
                        str += "\"" + userData.standardAvatarJson + "\"";
                        break;
                    case AvatarSetupMode.Custom:
                        str += "\"" + userData.customAvatarName + "\"";
                        break;
                    case AvatarSetupMode.ReadyPlayerMe:

                        if (AppManager.Instance.Settings.projectSettings.readyPlayerMeMode.Equals(ReadyPlayerMeMode.Fixed))
                        {
                            str += "\"" + userData.RPMAvatar + "\"";
                        }
                        else
                        {
                            //need to add stuff for json string of avatar
                            str += "\"" + "https://models.readyplayer.me/6400c6c5ce7f75d51cda2223.glb" + "\"";
                        }

                        break;
                }

                str += ",";
                str += "\"PlayerControls\":\"" + "walk-" + playerControlsData.walk + "|" + "run-" + playerControlsData.run
                        + "|" + "strife-" + playerControlsData.strife + "|" + "mouse-" + playerControlsData.mouse +
                        "|" + "invertX-" + playerControlsData.invertX +"|" + "invertY-" + playerControlsData.invertY + "\"" ;

                str += ",";
                str += "\"Friends\":\"" + "";

                str += ",";
                str += "\"Profile\":\"" + "";

                str += "}}";
            }

            WebclientManager.Instance.ProcessReceiveJson(str);
        }

        public void OnSimulateOrientation(OrientationType type)
        {
            simulateOrientation = type;
            var startedMessage = new OrientationResponse();
            startedMessage.orientation = simulateOrientation.ToString();
            startedMessage.screenHeight = (simulateOrientation.Equals(OrientationType.landscape)) ? simulateScreenHeight : simulateScreenWidth;
            startedMessage.screenWidth = (simulateOrientation.Equals(OrientationType.landscape)) ? simulateScreenWidth : simulateScreenHeight;
            var json = JsonUtility.ToJson(startedMessage);

            OrientationManager.Instance.ReceiveWebclientResponse(json);
        }

        [System.Serializable]
        public class URLParamWrapper
        {
            public List<URLParam> urlParams;
        }

        [System.Serializable]
        public class URLParam
        {
            public string key;
            public string data;
        }

        [System.Serializable]
        public class ContentPair
        {
            public string Key;
            public string Value;

            public ContentPair(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }

        [System.Serializable]
        public class UploadButtonPair
        {
            public string Key;
            public GameObject Button;
        }

        [System.Serializable]
        private class UserData
        {
            public string user;
            [HideInInspector]
            public int isAdmin = 0;
            public string simpleAvatarJson;
            public string standardAvatarJson;
            public string customAvatarName;
            public string RPMAvatar;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(WebClientSimulator), true)]
        public class WebClientSimulator_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("contentSamples"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("uploadPanel"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("uploadButtons"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("simulateIsMobile"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("simulateRoomNumber"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("startedMsgDelaySecs"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("simulateOrientation"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("simulateScreenWidth"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("simulateScreenHeight"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("isAdminUser"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("enableURLParamSimulation"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("userData"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("playerControlsData"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("videoChatScreen"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("videoChatUsername"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("webcamImage"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mobileWebInputScreen"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mobileWebInputfield"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mobileWebInputButtonLabel"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mobileWebInputPlaceholder"), true);

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