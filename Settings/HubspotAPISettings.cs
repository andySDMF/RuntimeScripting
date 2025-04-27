using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Defective.JSON;

namespace BrandLab360
{
    [System.Serializable]
    public class HubspotAPISettings
    {
        [Header("Access")]
        public string accessToken = "";
        public string accessEndpoint = "https://api.hubapi.com/crm/v3/objects/contacts/";
        public string sessionIDField = "username/";

        [Header("Regsiter")]
        public LoginReference username = new LoginReference("username", LoginFormReference._username);
        public LoginReference email = new LoginReference("email", LoginFormReference._email);
        public LoginReference password = new LoginReference("password", LoginFormReference._password);
        public LoginReference termsAndCondition = new LoginReference("termsandcondition", LoginFormReference._termsAndConditions);

        [Header("Contact Properties")]
        public ProfileReference[] properties = new ProfileReference[] {
                new ProfileReference("id", ProfileDataReference._ID),
                new ProfileReference("username", ProfileDataReference._username),
                new ProfileReference("password", ProfileDataReference._password),
                new ProfileReference("name", ProfileDataReference._name),
                new ProfileReference("email", ProfileDataReference._email),
                new ProfileReference("about", ProfileDataReference._about),
                new ProfileReference("termsandcondition", ProfileDataReference._termsAndConditions),
                new ProfileReference("playersettings", ProfileDataReference._playerSettings),
                new ProfileReference("picture", ProfileDataReference._picture),
                new ProfileReference("isAdmin", ProfileDataReference._admin),
                new ProfileReference("friendsData", ProfileDataReference._friendsData),
                new ProfileReference("gamesData", ProfileDataReference._gamesData)
            };

#if UNITY_EDITOR
        [HideInInspector]
        public string[] _DISPLAYTYPE = new string[3] { "Access", "Register", "Properties" };
        [HideInInspector]
        public int _DISPLAYVALUE = 0;

        [HideInInspector]
        public int FixedDataCount { get { return 12; } }
#endif

        public static UnityWebRequest CreateWebRequest(string url, RequestType requestType = RequestType.Get, string form = "")
        {
            UnityWebRequest www = null;

            switch (requestType)
            {
                case RequestType.Get:
                    www = UnityWebRequest.Get(url);
                    break;
                case RequestType.Post:
                    www = UnityWebRequest.PostWwwForm(url, form);
                    break;
                case RequestType.Put:
                    www = UnityWebRequest.PostWwwForm(url, form);
                    www.method = "PUT";
                    break;
                case RequestType.Delete:
                    www = UnityWebRequest.Delete(url);
                    break;
            }

            return www;
        }

        public async Task<string> GetContact(string username, string password)
        {
            using var request = CreateWebRequest(accessEndpoint + sessionIDField + username + "/profile?", RequestType.Get, "");

            request.SetRequestHeader("Content-Type", "application/json");
            var bearer = "Bearer " + accessToken;
            request.SetRequestHeader("Authorization", bearer);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 5;
            string rawdata = "";

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(request.error))
            {
                Debug.Log("Hubspot GET request error " + request.responseCode.ToString() + "[" + request.error + "]");
                rawdata = "";
            }
            else
            {
                Debug.Log("Hubspot GET request text [" + request.downloadHandler.text + "]");

                Encoding enc = new UTF8Encoding(true, true);
                byte[] bytes = enc.GetBytes(request.downloadHandler.text);

                rawdata = enc.GetString(bytes);
                var data = new JSONObject(rawdata);

                if(data != null)
                { 
                    if(data.HasField("password"))
                    {
                        if(!data.GetField("password").stringValue.Equals(password))
                        {
                            rawdata = "";
                        }
                    }
                }
                else
                {
                    rawdata = "";
                }
            }

            return rawdata;
        }

        public async Task<string> CreateContact(string username, string password, Dictionary<string, string> data)
        {
            //need to get contact first to check if the contact exists
            string contact = await GetContact(username, password);

            if (!string.IsNullOrEmpty(contact)) return "";

            string jsonEntry = "{\"properties\":{";
            int count = 0;

            foreach (KeyValuePair<string, string> props in data)
            {
                jsonEntry += "\"" + props.Key + "\":\"" + props.Value + "\"" + (count < data.Count ? "," : "");
            }

            jsonEntry += "}}";
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);

            using var request = CreateWebRequest(accessEndpoint + "?", RequestType.Post, "");
            request.SetRequestHeader("Content-Type", "application/json");
            var bearer = "Bearer " + accessToken;
            request.SetRequestHeader("Authorization", bearer);
            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 5;

            await request.SendWebRequest();

            string rawdata = "";

            if (request.result == UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(request.error))
            {
                Debug.Log("Hubspot POST request error " + request.responseCode.ToString() + "[" + request.error + "]");
                rawdata = "";
            }
            else
            {
                Debug.Log("Hubspot POST request text [" + request.downloadHandler.text + "]");

                Encoding enc = new UTF8Encoding(true, true);
                byte[] bytes = enc.GetBytes(request.downloadHandler.text);

                rawdata = enc.GetString(bytes);
            }

            return rawdata;
        }

        public async Task<bool> PushContact(string username, Dictionary<string, string> data)
        {
            bool success = false;

            string jsonEntry = "{\"properties\":{";
            int count = 0;

            foreach (KeyValuePair<string, string> props in data)
            {
                jsonEntry += "\"" + props.Key + "\":\"" + props.Value + "\"" + (count < data.Count ? "," : "");
            }

            jsonEntry += "}}";
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);

            using var request = CreateWebRequest(accessEndpoint + sessionIDField + username + "/profile?", RequestType.Post, "");
            request.SetRequestHeader("Content-Type", "application/json");
            var bearer = "Bearer " + accessToken;
            request.SetRequestHeader("Authorization", bearer);
            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 5;

            await request.SendWebRequest();

            string rawdata = "";

            if (request.result == UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(request.error))
            {
                Debug.Log("Hubspot POST request error " + request.responseCode.ToString() + "[" + request.error + "]");
                rawdata = "";
                success = false;
            }
            else
            {
                Debug.Log("Hubspot POST request text [" + request.downloadHandler.text + "]");

                Encoding enc = new UTF8Encoding(true, true);
                byte[] bytes = enc.GetBytes(request.downloadHandler.text);

                rawdata = enc.GetString(bytes);
                success = true;
            }

            return success;
        }

        public enum RequestType
        {
            Get,
            Post,
            Put,
            Delete
        }
    }
}
