using Defective.JSON;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class NoticeBoardAPI : Singleton<NoticeBoardAPI>
    {
        public static NoticeBoardAPI Instance
        {
            get
            {
                return ((NoticeBoardAPI)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Endpoint")]
        [SerializeField]
        private string noticeEndpoint = "/boards/";

        public void GetNoticeboards()
        {
            if (CoreManager.Instance.IsOffline) return;

            StartCoroutine(ProcessGet());
        }

        public void PostNotice(NoticeJson notice, System.Action<bool> callback = null)
        {
            if (CoreManager.Instance.IsOffline) return;

            StartCoroutine(ProcessUpload(notice, callback));
        }

        public void EditNotice(NoticeJson notice, System.Action<bool> callback = null)
        {
            if (CoreManager.Instance.IsOffline) return;

            StartCoroutine(ProcessEdit(notice, callback));
        }

        public void DeleteNotice(int noticeID, System.Action<bool> callback = null)
        {
            if (CoreManager.Instance.IsOffline) return;

            StartCoroutine(ProcessDelete(noticeID, callback));
        }

        private IEnumerator ProcessGet()
        {
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var project = CoreManager.Instance.ProjectID;

            //we only want to get those notices post expiry date based on todays date
            //back end on request, can it be po=ssible to delete those entries & content if the entries expriy date is < todays date - week??
            var uri = host + APIPATH + noticeEndpoint + "?project=" + project;

            Debug.Log("Request GET notices method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error GET notices: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        ProcessResponse(request.downloadHandler.text);
                    }
                }

                request.Dispose();
            }
        }

        private IEnumerator ProcessUpload(NoticeJson notice, System.Action<bool> callback)
        {
            //data
            var jsonEntry = JsonUtility.ToJson(notice);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + noticeEndpoint;

            //when we send to backend, can yiming work out todays date and expiry date based on diplay period sent in Json?? 1,2,3,4 weeks
            //then add these dates to the entry in DB

            Debug.Log("Request POST notice method: uri= " + uri + "::" + jsonEntry);

            using (UnityWebRequest request = UnityWebRequest.PostWwwForm(uri, UnityWebRequest.kHttpVerbPOST))
            {
                request.SetRequestHeader("Content-Type", "application/json");

                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);

                request.uploadHandler = new UploadHandlerRaw(jsonBytes);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error POST notice: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        //send RPC to others about new notice
                        var nMeta = CreateNoticeJsonFromData(request.downloadHandler.text);
                        NoticeBoardManager.Instance.CreateAllNotices(new List<NoticeJson>() { nMeta });

                        MMOManager.Instance.SendRPC("AddNotice", (int)MMOManager.RpcTarget.Others, request.downloadHandler.text);
                    }
                }

                if(callback != null)
                {
                    callback.Invoke(request.result == UnityWebRequest.Result.Success);
                }

                request.Dispose();
            }
        }

        private IEnumerator ProcessEdit(NoticeJson notice, System.Action<bool> callback)
        {
            //data
            var jsonEntry = JsonUtility.ToJson(notice);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + noticeEndpoint + notice.id.ToString();

            //when we send to backend, can yiming work out todays date and expiry date based on diplay period sent in Json?? 1,2,3,4 weeks
            //then add these dates to the entry in DB

            Debug.Log("Request PUT notice method: uri= " + uri + "::" + jsonEntry);

            using (UnityWebRequest request = UnityWebRequest.Put(uri, UnityWebRequest.kHttpVerbPOST))
            {
                request.SetRequestHeader("Content-Type", "application/json");

                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);

                request.uploadHandler = new UploadHandlerRaw(jsonBytes);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error PUT notice: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        //send RPC to others about new notice
                        var nMeta = CreateNoticeJsonFromData(request.downloadHandler.text);
                        NoticeBoardManager.Instance.CreateAllNotices(new List<NoticeJson>() { nMeta });

                        MMOManager.Instance.SendRPC("EditNotice", (int)MMOManager.RpcTarget.Others, request.downloadHandler.text);
                    }
                }

                if (callback != null)
                {
                    callback.Invoke(request.result == UnityWebRequest.Result.Success);
                }

                request.Dispose();
            }
        }

        private IEnumerator ProcessDelete(int noticeID, System.Action<bool> callback = null)
        {
            //data
            var host = ApiManager.Instance.GetHostURI();
            var APIPATH = ApiManager.Instance.APIPATH;
            var uri = host + APIPATH + noticeEndpoint + noticeID.ToString();

            Debug.Log("Request DELETE notice method: uri= " + uri);

            using (UnityWebRequest request = UnityWebRequest.Delete(uri))
            {
                //requests to the api need to set the access token in header
                var bearer = "Bearer " + ApiManager.Instance.AccessToken;
                request.SetRequestHeader("Authorization", bearer);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error DELETE notice: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        MMOManager.Instance.SendRPC("DeleteNotice", (int)MMOManager.RpcTarget.Others, noticeID);
                    }
                }

                if (callback != null)
                {
                    callback.Invoke(request.result == UnityWebRequest.Result.Success);
                }

                request.Dispose();
            }
        }

        private void ProcessResponse(string json)
        {
            Debug.Log("Processing noticeboards responce: " + json);

            // check for empty json array
            if (json == "[]")
            {
                return;
            }

            //need to break down json and push to noticeboards
            //will also need to look at the the json that comes back for expired

            //need to break down json and push to reports
            var data = new JSONObject(json);
            List<NoticeJson> notices = new List<NoticeJson>();

            foreach (JSONObject obj in data.list)
            {
                var nMeta = new NoticeJson();

                nMeta.id = obj.GetField("id").intValue;
                nMeta.project = obj.GetField("project").stringValue;
                nMeta.noticeboard_id = obj.GetField("noticeboard_id").stringValue;
                nMeta.content_text = obj.GetField("content_text").stringValue;
                nMeta.content_url = obj.GetField("content_url").stringValue;
                nMeta.website_link = obj.GetField("website_link").stringValue;
                nMeta.display_period = obj.GetField("display_period").stringValue;
                nMeta.expire_date = obj.GetField("expire_date").ToString();

                if (obj.HasField("bgk_color"))
                {
                    string[] split = obj.GetField("bgk_color").stringValue.Split('|');

                    if(split.Length > 1)
                    {
                        nMeta.bgk_color = obj.GetField("bgk_color").stringValue;
                    }
                }

                nMeta.pos_x = obj.GetField("pos_x").floatValue;
                nMeta.pos_y = obj.GetField("pos_y").floatValue;
                nMeta.pos_z = obj.GetField("pos_z").floatValue;
                nMeta.scale = obj.GetField("scale").floatValue;

                if(!nMeta.HasExpired)
                {
                    notices.Add(nMeta);
                }
            }

            NoticeBoardManager.Instance.CreateAllNotices(notices);
        }

        public NoticeJson CreateNoticeJsonFromData(string data)
        {
            var nMeta = new NoticeJson();
            var obj = new JSONObject(data);

            nMeta.id = obj.GetField("id").intValue;
            nMeta.project = obj.GetField("project").stringValue;
            nMeta.noticeboard_id = obj.GetField("noticeboard_id").stringValue;
            nMeta.content_text = obj.GetField("content_text").stringValue;
            nMeta.content_url = obj.GetField("content_url").stringValue;
            nMeta.website_link = obj.GetField("website_link").stringValue;
            nMeta.display_period = obj.GetField("display_period").stringValue;
            nMeta.expire_date = obj.GetField("expire_date").ToString();

            if(obj.HasField("bgk_color"))
            {
                nMeta.bgk_color = obj.GetField("bgk_color").ToString();
            }

            nMeta.pos_x = obj.GetField("pos_x").floatValue;
            nMeta.pos_y = obj.GetField("pos_y").floatValue;
            nMeta.pos_z = obj.GetField("pos_z").floatValue;
            nMeta.scale = obj.GetField("scale").floatValue;

            return nMeta;
        }

        [System.Serializable]
        public class NoticeJson
        {
            //DB id
            public int id = -1;

            //required for post
            public string project;
            public string noticeboard_id;
            public string content_text;
            public string content_url;
            public string display_period;

            //optional
            public string website_link;

            //pos
            public float pos_x = 0.0f;
            public float pos_y = 0.0f;
            public float pos_z = 0.0f;
            public float scale = 1.0f;

            public string expire_date = "null";
            public string bgk_color = "#FFFFFF,#000000";

            public bool HasExpired
            { 
                get 
                { 
                    if(expire_date.Equals("null"))
                    {
                        return false;
                    }
                    else
                    {
                        string temp = expire_date.Replace("T", " ").Replace("Z", "").Replace("\"", "");
                        System.DateTime dt = System.Convert.ToDateTime(temp, System.Globalization.CultureInfo.InvariantCulture);

                        if(dt != null)
                        {
                            return dt < System.DateTime.Now;
                        }
                        else
                        {
                            Debug.Log("could not pass date/time");
                        }

                        return false;
                    }
                } 
            }

            public NoticeType Type
            {
                get
                {
                    if (!content_url.Equals("n/a"))
                    {
                        return NoticeType.Image;
                    }
                    else
                    {
                        return NoticeType.Text;
                    }
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(NoticeBoardAPI), true)]
        public class NoticeBoardAPI_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("noticeEndpoint"), true);
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
