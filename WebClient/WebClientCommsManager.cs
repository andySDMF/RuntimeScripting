using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Defective.JSON;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class WebClientCommsManager : Singleton<WebClientCommsManager>
    {
        [HideInInspector]
        public Dictionary<string, object> UrlParameters = new Dictionary<string, object>();

        public static WebClientCommsManager Instance
        {
            get
            {
                return ((WebClientCommsManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        public System.Action OnRecieveParams { get; set; }

        public string URLName
        {
            get
            {
                if(UrlParameters.ContainsKey("name"))
                {
                    return UrlParameters["name"].ToString();
                }

                return "";
            }
        }

        public void Begin()
        {
            RequestUrlParams();
        }

        /// <summary>
        /// Subscribes to webclient response callback
        /// </summary>
        /// <param name="json"></param>
        public void receiveUrlParamResponse(string json)
        {
            Debug.Log("Received UrlParam Response: " + json);

            json = json.Replace("[", "").Replace("]", "");
            var data = new JSONObject(json);
            var urlParams = data.GetField("urlParams");

            WebclientManager.WebClientListener -= receiveUrlParamResponse;

            if (urlParams != null && urlParams.keys != null)
            {
                foreach (var key in urlParams.keys)
                {
                    var field = urlParams.GetField(key);

                    if (field.isString)
                    {
                        var param = urlParams.GetField(key).stringValue;
                        Debug.Log("Storing URL Param: " + key + ":" + param);

                        if(UrlParameters.ContainsKey(key))
                        {
                            UrlParameters[key] = param;
                        }
                        else
                        {
                            UrlParameters.Add(key, param);
                        }
                    }
                    else if (field.isInteger)
                    {
                        var param = urlParams.GetField(key).intValue;
                        Debug.Log("Storing URL Param: " + key + ":" + param);

                        if (UrlParameters.ContainsKey(key))
                        {
                            UrlParameters[key] = urlParams.GetField(key).intValue;
                        }
                        else
                        {
                            UrlParameters.Add(key, urlParams.GetField(key).intValue);
                        }
                    }
                    else if (field.isNumber)
                    {
                        var param = urlParams.GetField(key).floatValue;
                        Debug.Log("Storing URL Param: " + key + ":" + param);
                        UrlParameters.Add(key, urlParams.GetField(key).floatValue);

                        if (UrlParameters.ContainsKey(key))
                        {
                            UrlParameters[key] = urlParams.GetField(key).floatValue;
                        }
                        else
                        {
                            UrlParameters.Add(key, urlParams.GetField(key).floatValue);
                        }
                    }
                }
            }

            if(OnRecieveParams != null)
            {
                OnRecieveParams.Invoke();
            }
        }

        /// <summary>
        /// Request the url params from the webclient
        /// </summary>
        public void RequestUrlParams()
        {
            Debug.Log("Requesting URL params from Web client");

            WebclientManager.WebClientListener += receiveUrlParamResponse;

            var request = new UrlParamRequest();
            request.urlParamRequest = true;
            var json = JsonUtility.ToJson(request);

            WebclientManager.Instance.Send(json);
        }

        /// <summary>
        /// Request the webclient to copy the link to clipboard
        /// </summary>
        public void RequestCopyLink(string URL)
        {
            var request = new CopyLinkRequest();
            request.copyLink = URL;
            var json = JsonUtility.ToJson(request);

            WebclientManager.Instance.Send(json);
        }

        /// <summary>
        /// Request webclient to redirect to a new URL
        /// For example https://dev.brandlab360.co.uk/app?name=PluginAndy
        /// plus CreateURLParamString(keys) to attain other params
        /// </summary>
        public void RequestRedirect(string URL)
        {
            var request = new Redirect();
            request.redirectUrl = URL;
            var json = JsonUtility.ToJson(request);

            WebclientManager.Instance.Send(json);
        }

        public void RequestLivestream(LiveStreamRole role, string channel, bool livestreamEnabled, string hostName)
        {
            var request = new LiveStreamRequest();
            request.livestreamRole = role.ToString();
            request.livestreamChannel = channel;
            request.liveStreamEnabled = livestreamEnabled;
            request.hostName = hostName;
            var json = JsonUtility.ToJson(request);

            WebclientManager.Instance.Send(json);
        }

        public object AttainURLParameter(string key)
        {
            return UrlParameters.ContainsKey(key) ? UrlParameters[key] : null;
        }

        public string CreateURLParamString(List<string> keys)
        {
            string str = "";

            foreach(KeyValuePair<string, object> param in UrlParameters)
            {
                if(keys.Contains(param.Key))
                {
                    str += "&" + param.Key + "=" + param.Value.ToString();
                }
            }

            return str;
        }

        public void TestJoinHost(bool isOn)
        {
            RequestLivestream(LiveStreamRole.host, "testchannel", isOn, PlayerManager.Instance.GetLocalPlayer().NickName);
        }

        public void TestJoinAudience(bool isOn)
        {
            RequestLivestream(LiveStreamRole.audience, "testchannel", isOn, PlayerManager.Instance.GetLocalPlayer().NickName);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(WebClientCommsManager), true)]
        public class WebClientCommsManager_Editor : BaseInspectorEditor
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

    public enum LiveStreamRole { audience, host }
}