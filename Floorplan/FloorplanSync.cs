using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class FloorplanSync : Singleton<FloorplanSync>
    {
        public static FloorplanSync Instance
        {
            get
            {
                return ((FloorplanSync)instance);
            }
            set
            {
                instance = value;
            }
        }

        public void SyncAddFloorplanItem(FloorplanManager.FloorplanItem item)
        {
            FloorplanManager.Instance.InsertFloorplanItem(item);

            MMOManager.Instance.SendRPC("AddFloorplanItem", (int)MMOManager.RpcTarget.Others, JsonUtility.ToJson(item));
        }

        public void SyncUpdateFloorplanItem(string item, Vector3 pos, float rot, float scale)
        {
            FloorplanManager.Instance.UpdateFloorplanItem(item, pos, rot, scale);

            MMOManager.Instance.SendRPC("UpdateFloorplanItem", (int)MMOManager.RpcTarget.Others, item, pos, rot, scale);
        }

        public void SyncRemoveFloorplanItem(string item)
        {
            FloorplanManager.Instance.RemoveFloorplanItem(item);

            MMOManager.Instance.SendRPC("RemoveFloorplanItem", (int)MMOManager.RpcTarget.Others, item);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(FloorplanSync), true)]
        public class FloorplanSync_Editor : BaseInspectorEditor
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
