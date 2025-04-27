using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if BRANDLAB360_INTERNAL
using BrandLab360.Internal;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class DataManager : Singleton<DataManager>
    {
        public static DataManager Instance
        {
            get
            {
                return ((DataManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private List<DataObject> dataObjects = new List<DataObject>();

        public System.Action<List<string>> Callback_OnSetData { get; set; }

        /// <summary>
        /// Called to set the data objects - callback for GET method on DataAPI
        /// </summary>
        /// <param name="objs"></param>
        public void SetDataObjects(List<DataObject> objs)
        {
            dataObjects.Clear();
            dataObjects.AddRange(objs);

            //get all API callbacks
            IDataAPICallback[] callbacks = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IDataAPICallback>().ToArray();

            List<string> callbackData = new List<string>();

            foreach(var d in objs)
            {
                callbackData.Add(JsonUtility.ToJson(d));
            }

            for(int i = 0; i < callbacks.Length; i++)
            {
                callbacks[i].DataAPICallback(objs);
            }

            if(Callback_OnSetData != null)
            {
                Callback_OnSetData.Invoke(callbackData);
            }

#if BRANDLAB360_INTERNAL
            CalendarManager.Instance.DataAPICallback(callbackData);
#endif
        }

        /// <summary>
        /// Called to return dataobject
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataObject GetDataObject(string id)
        {
            return dataObjects.FirstOrDefault(x => x.uniqueId.Equals(id));
        }

        /// <summary>
        /// Called to delete data object
        /// </summary>
        /// <param name="id"></param>
        public void DeleteDataObject(string id)
        {
            DataObject obj = GetDataObject(id);

            if (obj != null)
            {
                Debug.Log("Deleting Data Object= " + id);

                //delete from API
                DataAPI.Instance.Delete(obj.uniqueId);

                dataObjects.Remove(obj);
            }
        }

        /// <summary>
        /// Called to insert ne data object
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <param name="dataType"></param>
        public void InsertDataObject(string id, string data, string dataType)
        {
            //create ID if it is null or empty
            if(string.IsNullOrEmpty(id))
            {
                Debug.Log("Creating random ID");
                id = UniqueIDManager.Instance.NewID();
            }

            DataObject obj = GetDataObject(id);

            if (obj == null)
            {
                obj = new DataObject();
                obj.uniqueId = id;
                obj.project = CoreManager.Instance.ProjectID;
                obj.dataType = dataType;
                obj.data = data;
                obj.updated_at = "Just Added";
                obj.created_at = "This Session";

                dataObjects.Add(obj);
                DataAPI.Instance.Insert(CoreManager.Instance.ProjectID, id, dataType, data);
            }
        }

        /// <summary>
        /// Called to update data object
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dataType"></param>
        /// <param name="latestData"></param>
        public void UpdateDataObject(string id, string dataType, string latestData)
        {
            DataObject obj = GetDataObject(id);

            if (obj != null)
            {
                Debug.Log("Updating Data Object= " + id);

                obj.data = latestData;
                DataAPI.Instance.UpdateData(CoreManager.Instance.ProjectID, latestData, dataType, id);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(DataManager), true)]
        public class DataManager_Editor : BaseInspectorEditor
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
}
