using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Defective.JSON;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class VideoAPI : Singleton<VideoAPI>
    {
        public static VideoAPI Instance
        {
            get
            {
                return ((VideoAPI)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Endpoint")]
        [SerializeField]
        private string videosEndpoint = "/videos/";

        /// <summary>
        /// Called to get all videos from API based from video screen
        /// </summary>
        /// <param name="vScreen"></param>
        public void GetVideos(VideoScreen vScreen)
        {
            if (CoreManager.Instance.projectSettings.useContentsAPI)
            {
                if (CoreManager.Instance.IsOffline) return;

                StartCoroutine(GetVideosRequest(vScreen));
            }
        }

        /// <summary>
        /// Process the contents web request
        /// </summary>
        /// <returns></returns>
        private IEnumerator GetVideosRequest(VideoScreen vScreen)
        {
            string pID = !Application.isPlaying ? Resources.Load<AppSettings>("ProjectAppSettings").projectSettings.ProjectID : CoreManager.Instance.ProjectID;

            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var project = pID;

            var uri = host + APIPATH + videosEndpoint + "?project=" + project + "?folder=" + vScreen.Folder;

            Debug.Log("Request GET videos method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error GET videos: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        ProcessVideoData(request.downloadHandler.text, vScreen);
                    }
                }

                request.Dispose();
            }
        }

        /// <summary>
        /// Responce called when the GetContents() request is called 
        /// </summary>
        /// <param name="json"></param>
        private void ProcessVideoData(string json, VideoScreen vScreen)
        {
            Debug.Log("Processing videos responce: " + json);

            // check for empty json array
            if (json == "[]")
            {
                vScreen.UpdateVideoFiles(null);
                return;
            }

            // Extract the product data from json array (JsonUtility doesnt support deserializing array so had to manually extract)
            // Would be good to use a library that can actually deserialize it from the productMeta structure. When this is a plugin
            // we could probably use Newtonsoft.json and set it as a package dependancy. But for now I added a json library manually 
            // to the folder to ensure there are no dependancies that have to be manually added for new projects.

            var data = new JSONObject(json);
            VideoEntries wrapper = new VideoEntries();

            foreach (JSONObject obj in data.list)
            {
                var videoMeta = new VideoEntry();
                videoMeta.url = obj.GetField("url").stringValue;
                videoMeta.filename = obj.GetField("filename").stringValue;

                wrapper.entries.Add(videoMeta);
            }

            vScreen.UpdateVideoFiles(wrapper);
        }

        [System.Serializable]
        public class VideoEntry
        {
            public string url = "";
            public string filename = "";
        }

        [System.Serializable]
        public class VideoEntries
        {
            public List<VideoEntry> entries = new List<VideoEntry>();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(VideoAPI), true)]
        public class VideoAPI_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("videosEndpoint"), true);

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