using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class FriendsStatusPanel : MonoBehaviour
    {
        [SerializeField]
        private GameObject reqestedDisplay;

        [SerializeField]
        private Button button;

        [SerializeField]
        private GameObject accepting;

        [SerializeField]
        private GameObject removeButton;

        private IFriend friend;

        private void Awake()
        {
            if(!FriendsManager.Instance.IsEnabled)
            {
                gameObject.SetActive(false);
                return;
            }

            button.onClick.AddListener(SendRequest);

            friend = GetComponentInParent<IFriend>();

            if(friend != null)
            {
                friend.OnThisUpdate += OnUpdate;
            }
        }

        private void OnUpdate()
        {
            //need to get the player name
            if(friend != null)
            {
                if(friend.Friend_ID.Equals(PlayerManager.Instance.GetLocalPlayer().NickName))
                {
                    gameObject.SetActive(false);
                    return;
                }
                else
                {
                    gameObject.SetActive(true);
                }

                SetUI();
            }
        }

        public void AcceptRequest()
        {
            FriendsManager.Instance.SendFriendRequestResponse(friend.Friend_ID, FriendsManager.FriendRequestState.Accepted);

            SetUI();
        }

        public void DenyRequest()
        {
            FriendsManager.Instance.SendFriendRequestResponse(friend.Friend_ID, FriendsManager.FriendRequestState.Denied);

            SetUI();
        }

        private void SetUI()
        {
            FriendsManager.Friend temp = FriendsManager.Instance.GetFriend(friend.Friend_ID);

            if (temp != null)
            {
                if (temp.requestState.Equals(FriendsManager.FriendRequestState.Denied))
                {
                    reqestedDisplay.SetActive(false);
                    button.gameObject.SetActive(true);
                    accepting.SetActive(false);
                    removeButton.SetActive(false);
                }
                else if (temp.requestState.Equals(FriendsManager.FriendRequestState.Pending))
                {
                    if (temp.source.Equals(FriendsManager.FriendRequestSrc.Sent))
                    {
                        reqestedDisplay.SetActive(true);
                        button.gameObject.SetActive(false);
                        accepting.SetActive(false);
                        removeButton.SetActive(false);
                    }
                    else
                    {
                        accepting.SetActive(true);
                        removeButton.SetActive(false);
                        reqestedDisplay.SetActive(false);
                        button.gameObject.SetActive(false);
                    }
                }
                else
                {
                    removeButton.SetActive(true);
                    button.gameObject.SetActive(false);
                    accepting.SetActive(false);
                    reqestedDisplay.SetActive(false);
                }
            }
            else
            {
                button.gameObject.SetActive(true);
                removeButton.SetActive(false);
                reqestedDisplay.SetActive(false);
                accepting.SetActive(false);
            }
        }

        private void SendRequest()
        {
            FriendsManager.Instance.FriendRequest(friend.Friend_ID);

            SetUI();
        }

        private string Status(FriendsManager.FriendRequestState state)
        {
            string str = "";

            switch(state)
            {
                case FriendsManager.FriendRequestState.Accepted:
                    str = "FRIENDS";
                    break;
                case FriendsManager.FriendRequestState.Pending:
                    str = "FRIENDS REQUEST PENDING";
                    break;
                default:
                    break;
            }

            return str;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(FriendsStatusPanel), true)]
        public class FriendsStatusPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("reqestedDisplay"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("button"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("accepting"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("removeButton"), true);

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