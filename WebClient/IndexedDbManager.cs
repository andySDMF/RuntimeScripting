using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// Class to handle data storage in the web browser indexedDB via the webclient
    /// </summary>
    public class IndexedDbManager : Singleton<IndexedDbManager>
    {
        public static IndexedDbManager Instance
        {
            get
            {
                return ((IndexedDbManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        public System.Action<string> iDbListener;

        private IDbRequestType sentType = IDbRequestType.get;

        public IDbRequestType SentType
        {
            get { return sentType; }
        }

        /// <summary>
        /// Subscribes to webclient response callback
        /// </summary>
        /// <param name="json"></param>
        public void receiveWebClientJson(string json)
        {
            iDbResponse response = JsonUtility.FromJson<iDbResponse>(json).OrDefaultWhen(x => x.iDbEntry == null);

            if (response != null)
            {
                WebclientManager.WebClientListener -= receiveWebClientJson;

                Debug.Log("Received IndexedDB Response: " + json);

                if (sentType == IDbRequestType.get)
                {
                    // response is a json string that we had stored there
                    // this can then be decoded and used 
                    if (iDbListener != null)
                    {
                        iDbListener.Invoke(json);
                    }
                }
            }           
        }

        /// <summary>
        /// The GET request to the indexedDB
        /// </summary>
        /// <param name="key">key to lookup in indexedDB</param>
        public void GetEntry(string key)
        {
            var request = new iDbRequest(IDbRequestType.get.ToString(), key, "null");
            var json = JsonUtility.ToJson(request);

            WebclientManager.WebClientListener += receiveWebClientJson;

            WebclientManager.Instance.Send(json);
            sentType = IDbRequestType.get;
        }

        /// <summary>
        /// The INSERT request to the indexedDB
        /// </summary>
        /// <param name="key">key to store in db</param>
        /// <param name="value">value to store in db</param>
        public void InsertEntry(string key, string value)
        {
            var request = new iDbRequest(IDbRequestType.insert.ToString(), key, value);
            var json = JsonUtility.ToJson(request);

            WebclientManager.WebClientListener += receiveWebClientJson;

            WebclientManager.Instance.Send(json);
            sentType = IDbRequestType.insert;
        }

        /// <summary>
        /// The UPDATE request to the indexedDB
        /// </summary>
        /// <param name="key">key to lookup in the indexedDB</param>
        /// <param name="value">value to update for that key</param>
        public void UpdateEntry(string key, string value)
        {
            var request = new iDbRequest(IDbRequestType.update.ToString(), key, value);
            var json = JsonUtility.ToJson(request);

            WebclientManager.WebClientListener += receiveWebClientJson;

            WebclientManager.Instance.Send(json);
            sentType = IDbRequestType.update;
        }

        /// <summary>
        /// The DELETE request to the indexedDB
        /// </summary>
        /// <param name="key">the key to lookup in the indexedDB</param>
        public void DeleteEntry(string key)
        {
            var request = new iDbRequest(IDbRequestType.delete.ToString(), key, "");
            var json = JsonUtility.ToJson(request);

            WebclientManager.WebClientListener += receiveWebClientJson;

            WebclientManager.Instance.Send(json);
            sentType = IDbRequestType.delete;
        }


        /*
        public void Test(string method)
        {
            switch(method)
            {
                case "INSERT":
                    InsertEntry("userData", "TestData");
                    break;
                case "UPDATE":
                    UpdateEntry("userData", "TestData" + System.Guid.NewGuid());
                    break;
                case "DELETE":
                    DeleteEntry("userData");
                    break;
                default:
                    GetEntry("userData");
                    break;
            }
        }*/

#if UNITY_EDITOR
        [CustomEditor(typeof(IndexedDbManager), true)]
        public class IndexedDbManager_Editor : BaseInspectorEditor
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

    [Serializable]
    public class iDbRequest
    {
        public string iDbRequestType;
        public string iDbKey;
        public string iDbValue;

        public iDbRequest(string requestType, string key, string value)
        {
            iDbRequestType = requestType;
            iDbKey = key;

            if(!string.IsNullOrEmpty(value))
            {
                iDbValue = value;
            }
            else
            {
                iDbValue = "null";
            }
        }
    }

    [Serializable]
    public class iDbResponse
    {
        public string iDbEntry;
    }

    [Serializable]
    public class DBUserData
    {
        public string user;
        public int isAdmin;
        public string json;
        public string friends = "";
        public string profile = "";
        public string games = "";
    }

    public enum IDbRequestType { insert, update, get, delete };
}