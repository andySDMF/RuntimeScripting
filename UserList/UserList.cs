using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class UserList : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField]
        private UserType displayUsers = UserType.All;
        [SerializeField]
        private bool includeLocalPlayer = true;

        [Header("Instantiate")]
        [SerializeField]
        private GameObject prefab;
        [SerializeField]
        private Transform container;

        /// <summary>
        /// Access to the custom list of photon players, defined via script
        /// </summary>
        public List<IPlayer> ListSource
        {
            get;
            set;
        }

        /// <summary>
        /// Access to the ID of this list, defined via script
        /// </summary>
        public string IDSource
        {
            get;
            set;
        }

        public bool IncludeLocalPlayer
        {
            get
            {
                return includeLocalPlayer;
            }
        }

        public bool CurrentExists
        {
            get
            {
                return m_currentActive != null;
            }
        }

        private List<GameObject> m_created = new List<GameObject>();
        private IUserList m_currentActive;


        private void OnEnable()
        {
            //if this list requires modifying if player leaves, connects
            MMORoom.Instance.OnPlayerEnteredRoom += AddPlayer;
            MMORoom.Instance.OnPlayerLeftRoom += RemovePlayer;

            //ignore custom list if all players are required
            if(displayUsers.Equals(UserType.All))
            {
                if(ListSource == null)
                {
                    ListSource = new List<IPlayer>();
                }
                else
                {
                    ListSource.Clear();
                }

                foreach(var player in MMOManager.Instance.GetAllPlayers())
                {
                    ListSource.Add(player);
                }
            }

            if(ListSource.Contains(PlayerManager.Instance.GetLocalPlayer()))
            {
                if (!includeLocalPlayer)
                {
                    ListSource.Remove(PlayerManager.Instance.GetLocalPlayer());
                }
            }

            Create();
        }

        private void OnDisable()
        {
            ///unsubscribe modify actions
            MMORoom.Instance.OnPlayerEnteredRoom -= AddPlayer;
            MMORoom.Instance.OnPlayerLeftRoom -= RemovePlayer;

            Clear();
        }

        private void Update()
        {
            for (int i = 0; i < m_created.Count; i++)
            {
                IUserList iUser = m_created[i].GetComponentInChildren<IUserList>(true);

                if(iUser != null)
                {
                    iUser.Repaint();
                }
            }
        }

        /// <summary>
        /// Action to create a new list
        /// </summary>
        private void Create()
        {
            if (prefab == null || container == null) return;

            foreach(var player in ListSource)
            {
                if (player.Equals(null)) continue;

                //create new owner button
                GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.identity, container);
                obj.name = "User_" + player.ID + " [" + PlayerManager.Instance.GetPlayerName(player.NickName) + "]";

                IUserList iUser = obj.GetComponentInChildren<IUserList>(true);

                if(iUser != null)
                {
                    obj.GetComponentInChildren<IUserList>(true).Set(player, IDSource);
                }

                obj.SetActive(true);
                m_created.Add(obj);
            }
        }

        /// <summary>
        /// Action to clear/destroy the list
        /// </summary>
        private void Clear()
        {
            for(int i = 0; i < m_created.Count; i++)
            {
                Destroy(m_created[i]);
            }

            m_created.Clear();

            if (ListSource == null)
            {
                ListSource = new List<IPlayer>();
            }
            else
            {
                ListSource.Clear();
            }

            m_currentActive = null;
        }

        /// <summary>
        /// Extenal action to remove a player if this script is live/visible
        /// </summary>
        /// <param name="player"></param>
        public void RemovePlayer(IPlayer player)
        {
            if (!gameObject.activeInHierarchy) return;

            if (ListSource.Contains(player))
            {
                int index = 0;
                bool found = false;

                for (int i = 0; i < m_created.Count; i++)
                {
                    if(m_created[i].GetComponent<IUserList>().Owner.Equals(player.ID))
                    {
                        index = i;
                        found = true;
                        Destroy(m_created[i]);
                        break;
                    }
                }

                if(found)
                {
                    m_created.RemoveAt(index);
                    ListSource.Remove(player);
                }
            }
        }

        /// <summary>
        /// Extenal action to add a player if this script is live/visible
        /// </summary>
        /// <param name="player"></param>
        public void AddPlayer(IPlayer player)
        {
            if (!gameObject.activeInHierarchy || displayUsers.Equals(UserType.Custom)) return;

            ListSource.Add(player);

            GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.identity, container);
            obj.name = "User_" + player.ID + " [" + PlayerManager.Instance.GetPlayerName(player.NickName) + "]";
            obj.GetComponentInChildren<IUserList>(true).Set(player, IDSource);
            obj.SetActive(true);
            m_created.Add(obj);
        }

        /// <summary>
        /// Called via the user button to update current active button
        /// </summary>
        /// <param name="current"></param>
        public void SetCurrentInterface(IUserList current)
        {
            if(m_currentActive != null)
            {
                if(m_currentActive != current)
                {
                    m_currentActive.Toggle(false);
                }
                else
                {
                    current = null;
                }
            }

            m_currentActive = current;
        }

        public void SwitchUserType(UserType type, bool refresh = false)
        {
            displayUsers = type;

            if(refresh)
            {
                for (int i = 0; i < m_created.Count; i++)
                {
                    Destroy(m_created[i]);
                }

                m_created.Clear();
                m_currentActive = null;

                Create();
            }
        }

        [System.Serializable]
        public enum UserType {  All, Custom }

#if UNITY_EDITOR
        [CustomEditor(typeof(UserList), true)]
        public class UserList_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("displayUsers"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("includeLocalPlayer"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("prefab"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("container"), true);

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

    public interface IUserList
    {
        void Set(IPlayer player, string listID);

        void Repaint();

        string Owner { get; }

        void Toggle(bool toggle);
    }
}
