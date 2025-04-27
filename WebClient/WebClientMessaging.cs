using Defective.JSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrandLab360
{
    public static class WebClientMessaging
    {
        public static string Construct(string json)
        {
            string message = "";

            if(WebclientManager.Instance.WebCommsVersion.Equals("v1"))
            {
                message = json;
            }
            else
            {
                MessageV2 v2 = new MessageV2(GetCodeFromJson(json), json);
                message = JsonUtility.ToJson(v2);
            }

            return message;
        }

        public static string Return(string json)
        {
            string message = "";

            if (WebclientManager.Instance.WebCommsVersion.Equals("v1"))
            {
                message = json;
            }
            else
            {
                MessageV2 v2 = JsonUtility.FromJson<MessageV2>(json);

                if(v2 != null)
                {
                    message = v2.Message;
                }
                else
                {
                    message = json;
                }
            }

            return message;
        }

        private static int GetCodeFromJson(string json)
        {
            //this where we need to find out what code to use for the message
            var data = new JSONObject(json);

            // App Popup
            if (data.HasField("title") && data.HasField("description") && data.HasField("button")
                 && data.HasField("inputs") && data.HasField("types"))
            {
                return 1020;
            }

            //GA Tracking
            if (data.HasField("Action") && data.HasField("Category") && data.HasField("Label"))
            {
                return 1019;
            }

            //Re-direct URL 
            if (data.HasField("redirectUrl"))
            {
                return 1022;
            }

            //Copy link 
            if (data.HasField("copyLink"))
            {
                return 1018;
            }

            //IDB 
            if (data.HasField("iDbRequestType") && data.HasField("iDbKey"))
            {
                return 1017;
            }

            //firebase
            if (data.HasField("apiKey") && data.HasField("authDomain") && data.HasField("databaseURL") && data.HasField("measurementId"))
            {
                return 1016;
            }

            //open RPM
            if (data.HasField("showReadyPlayerMe"))
            {
                return 1015;
            }

            //window popp
            if (data.HasField("url") && data.HasField("title"))
            {
                return 1014;
            }

            //upload product
            if (data.HasField("productUploadRequest"))
            {
                return 1013;
            }

            //upload notice
            if (data.HasField("RequestNoticeUpload"))
            {
                return 1012;
            }

            //upload model; GLB
            if (data.HasField("RequestModelUpload"))
            {
                return 1011;
            }

            //upload profile
            if (data.HasField("BeginProfileUpload") && data.HasField("profile_url"))
            {
                return 1010;
            }

            //general upload request
            if (data.HasField("BeginUpload"))
            {
                return 1009;
            }

            //toggle video chat; on/off
            if (data.HasField("ToggleVideoChat"))
            {
                return 1008;
            }

            //close web input
            if (data.HasField("closeWebInput"))
            {
                return 1023;
            }

            //open web input
            if (data.HasField("button") && data.HasField("input") && data.HasField("type"))
            {
                return 1007;
            }

            //info tag popup
            if (data.HasField("isImage"))
            {
                return 1006;
            }

            //enable live stream; on/off
            if (data.HasField("liveStreamEnabled"))
            {
                return 1005;
            }

            //enable audio chat; on/off
            if (data.HasField("ToggleAudioChat"))
            {
                return 1004;
            }

            //enable video chat; on/off
            if (data.HasField("ToggleVideoChat"))
            {
                return 1003;
            }

            //URL param request
            if(data.HasField("urlParamRequest"))
            {
                return 1002;
            }

            //AFK timeout
            if(data.HasField("disconnect"))
            {
                return 1001;
            }

            return 1000;
        }

        [System.Serializable]
        public class MessageV2
        {
            public int MessageCode = 1001;
            public string Message = "";

            public MessageV2(int code, string json)
            {
                MessageCode = code;
                Message = json;
            }
        }
    }
}