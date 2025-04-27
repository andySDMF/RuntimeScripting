using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class HotspotManager : Singleton<HotspotManager>
    {
        public static HotspotManager Instance
        {
            get
            {
                return ((HotspotManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        public UnityEngine.UI.Toggle HotspotToggle
        {
            get
            {
                return HUDManager.Instance.GetMenuItem("Toggle_Hotspots").GetComponentInChildren<UnityEngine.UI.Toggle>(true);
            }
        }

        public void OnClickHotspot(Hotspot hotspotMeta)
        {
            Debug.Log("OnClickHotspot:" + hotspotMeta.hotspotImage);

            var position = new Vector3(hotspotMeta.x, hotspotMeta.y, hotspotMeta.z);
            var rotation = new Vector2(hotspotMeta.cameraRot, hotspotMeta.rot);

            PlayerManager.Instance.TeleportLocalPlayer(position, rotation);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(HotspotManager), true)]
        public class HotspotManager_Editor : BaseInspectorEditor
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