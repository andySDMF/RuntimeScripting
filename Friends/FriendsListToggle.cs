using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class FriendsListToggle : MonoBehaviour
    {
        [SerializeField]
        private UserList list;

        private bool m_isOn = false;

        private void Start()
        {
            if(!FriendsManager.Instance.IsEnabled)
            {
                gameObject.SetActive(false);
            }
        }

        public void Toggle(bool val)
        {
            if (!m_isOn) return;

            if (val)
            {
                list.ListSource.Clear();

                foreach (IPlayer pl in MMOManager.Instance.GetAllPlayers())
                {
                    if(FriendsManager.Instance.GetFriend(pl.NickName) != null)
                    {
                        list.ListSource.Add(pl);
                    }
                }

                list.SwitchUserType(UserList.UserType.Custom, true);
            }
            else
            {
                list.ListSource.Clear();

                foreach (var player in MMOManager.Instance.GetAllPlayers())
                {
                    list.ListSource.Add(player);
                }

                if (list.ListSource.Contains(PlayerManager.Instance.GetLocalPlayer()))
                {
                    if (!list.IncludeLocalPlayer)
                    {
                        list.ListSource.Remove(PlayerManager.Instance.GetLocalPlayer());
                    }
                }

                list.SwitchUserType(UserList.UserType.All, true);
            }    
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(FriendsListToggle), true)]
        public class FriendsListToggle_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("list"), true);

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
