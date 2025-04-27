using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class OfflineManager : Singleton<OfflineManager>
    {
        public static OfflineManager Instance
        {
            get
            {
                return ((OfflineManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private GameObject offlineMessage;

        private void Start()
        {
            if(CoreManager.Instance.projectSettings.syncContentDisplayOffline && CoreManager.Instance.IsOffline)
            {
                if(CoreManager.Instance.projectSettings.syncContentRequestContinously)
                {
                    Sync();
                }
            }
        }

        public void ShowOfflineMessage()
        {
            if(offlineMessage == null)
            {
                offlineMessage = HUDManager.Instance.GetHUDMessageObject("OFFLINE_MESSAGE");
            }

            if(offlineMessage != null)
            {
                PlayerManager.Instance.FreezePlayer(true);
                offlineMessage.SetActive(true);
            }
        }

        public void HideOfflineMessage()
        {
            if (offlineMessage != null)
            {
                offlineMessage.SetActive(false);
                PlayerManager.Instance.FreezePlayer(false);
            }
        }

        private async void Sync()
        {
            await TrySync();
        }

        private async Task TrySync()
        {
            while (Application.isPlaying && AppManager.IsCreated)
            {
                if (Application.isPlaying && AppManager.IsCreated)
                {
                    await Task.Delay(CoreManager.Instance.projectSettings.syncContentRequestTimer);
                }
                else
                {
                    break;
                }

                Debug.Log("Syncing Contents");

                ContentsAPI.Instance.GetContents();
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OfflineManager), true)]
        public class OfflineManager_Editor : BaseInspectorEditor
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
