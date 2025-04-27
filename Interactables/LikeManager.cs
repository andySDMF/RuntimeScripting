using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrandLab360
{
    public class LikeManager : Singleton<LikeManager>
    {
        public static LikeManager Instance
        {
            get
            {
                return ((LikeManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private Like[] m_likes;

        private void Start()
        {
            m_likes = FindObjectsByType<Like>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        public void PostLike(string id, int count)
        {
            //need to send this to the data API
            //check if it exists first
            if(DataManager.Instance.GetDataObject(id) == null)
            {
                DataAPI.Instance.Insert(CoreManager.Instance.ProjectID, id, "text", count.ToString());
            }
            else
            {
                DataAPI.Instance.UpdateData(CoreManager.Instance.ProjectID, count.ToString(), "text", id);
            }

            //then RPC to everyone
            MMOManager.Instance.SendRPC("PostLike", (int)MMOManager.RpcTarget.Others);
        }
    }
}
