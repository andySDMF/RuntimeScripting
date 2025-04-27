using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class RoomLobby : MonoBehaviour
    {
        [SerializeField]
        private RawImage background;

        [SerializeField]
        private TextMeshProUGUI room;

        [SerializeField]
        private TextMeshProUGUI particpants;

        [SerializeField]
        private TextMeshProUGUI status;

        [SerializeField]
        private GameObject joinButton;

        private RoomInfo m_room;
        private RectTransform rectT;

        public string Room { get { return m_room.Name; } }

        private void Awake()
        {
            Texture tex = Resources.Load<Texture>(AppManager.Instance.Settings.projectSettings.roomLobbyResourceTexture);

            if(tex == null)
            {
                tex = Resources.Load<Texture>("Lobby/roomLobby_BKG");
            }

            background.texture = tex;
        }

        private void Start()
        {
            StartCoroutine(WaitFrame());
        }

        private IEnumerator WaitFrame()
        {
            yield return new WaitForEndOfFrame();

            rectT = GetComponent<RectTransform>();
            Resize();
        }

        public void Set(RoomInfo info)
        {
            m_room = info;
            room.text = "Room: " + info.Name;
            particpants.text = "Participants: " + info.PlayerCount + "/" + info.MaxPlayers;
            status.text = "Status: " + ((info.PlayerCount >= info.MaxPlayers) ? "Full" : "Open");

            if (info.PlayerCount < info.MaxPlayers)
            {
                joinButton.SetActive(true);
            }
            else
            {
                joinButton.SetActive(false);
            }
        }

        public void OnClick()
        {
            //need to tell startupmanager to connect to this room
            HUDManager.Instance.ShowRoomLobbyPanel(false);
            StartupManager.Instance.ConnectToRoom(Room);
        }

        private void Resize()
        {
            if (!gameObject.activeInHierarchy) return;

            //ensure image fills viewport
            Vector2 viewport = rectT.sizeDelta;
            RectTransform imageRect = background.GetComponent<RectTransform>();
            background.SetNativeSize();

            if (background.texture.width < viewport.x)
            {
                float aspect = viewport.x / background.texture.width;
                imageRect.sizeDelta = new Vector2(background.texture.width * aspect, background.texture.height * aspect);
            }
            else
            {
                float aspect = background.texture.width / viewport.x;
                imageRect.sizeDelta = new Vector2(background.texture.width / aspect, background.texture.height / aspect);
            }

            if (imageRect.sizeDelta.y < viewport.y)
            {
                float aspect = viewport.y / imageRect.sizeDelta.y;
                imageRect.sizeDelta = new Vector2(imageRect.sizeDelta.x * aspect, imageRect.sizeDelta.y * aspect);
            }
        }

        [System.Serializable]
        public class RoomInfoWrapper
        {
            public List<RoomInfo> rooms = new List<RoomInfo>();
        }

        [System.Serializable]
        public class RoomInfo
        {
            public string id;
            public string Name;
            public int PlayerCount;
            public int MaxPlayers;
            public bool RemovedFromList = false;

            public Hashtable CustomProperties = new Hashtable();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(RoomLobby), true)]
        public class RoomLobby_Editor : BaseInspectorEditor
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

                    EditorGUILayout.LabelField("Elements", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("background"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("joinButton"), true);

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Text Display", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("room"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("particpants"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("status"), true);
                   

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
