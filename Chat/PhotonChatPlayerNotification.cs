using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class PhotonChatPlayerNotification : MonoBehaviour
    {
        [SerializeField]
        private GameObject notification;

        public string ChatID { get; set; }

        private void OnEnable()
        {
            //subscrive to the photon chat notification system
            MMOChat.Instance.OnNotify += Notify;

            StartCoroutine(Delay());
        }

        /// <summary>
        /// wait for end frame
        /// </summary>
        /// <returns></returns>
        private IEnumerator Delay()
        {
            yield return new WaitForEndOfFrame();

            if(!string.IsNullOrEmpty(ChatID) && notification != null)
            {
                //if user read recent notification
                notification.SetActive(MMOChat.Instance.PlayerHasUnreadMessage(ChatID));
            }
        }

        private void OnDestroy()
        {
            //unsubscrive from photon chat notification system
            MMOChat.Instance.OnNotify -= Notify;
        }

        /// <summary>
        /// Call back for notifications
        /// </summary>
        /// <param name="id"></param>
        private void Notify(string id)
        {
            if (id.Equals(ChatID) && !MMOChat.Instance.CurrentChatID.Equals(ChatID) && notification != null)
            {
                notification.SetActive(true);
            }
        }

        /// <summary>
        /// Used to hide the notification display
        /// </summary>
        public void Hide()
        {
            if(notification != null)
            {
                notification.SetActive(false);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PhotonChatPlayerNotification), true)]
        public class PhotonChatPlayerNotification_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("notification"), true);

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
