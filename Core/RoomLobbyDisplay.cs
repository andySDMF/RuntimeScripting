using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class RoomLobbyDisplay : MonoBehaviour
    {
        [Header("Lobby UI")]
        [SerializeField]
        private GameObject prefabLobby;
        [SerializeField]
        private Transform lobbyContainer;

        [Header("Buttons")]
        [SerializeField]
        private GameObject createButton;

        public GameObject Prefab { get { return prefabLobby; } }
        public Transform Container { get { return lobbyContainer; } }

        private void OnEnable()
        {
            createButton.SetActive(RoomManager.instance.allRooms.Count > 0 ? true : false);
        }

        public void CreateNewRoom()
        {
            if (RoomManager.instance.allRooms.Count > 0)
            {
                CoreManager.Instance.AddToProjectID("_" + RoomManager.instance.allRooms.Count.ToString());
                StartupManager.Instance.CreateNewRoom(CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString());
            }
            else
            {
                StartupManager.Instance.CreateNewRoom(CoreManager.Instance.ProjectID + "_" + CoreManager.Instance.RoomID.ToString());
            }

            HUDManager.Instance.ShowRoomLobbyPanel(false);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(RoomLobbyDisplay), true)]
        public class RoomLobbyDisplay_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("prefabLobby"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lobbyContainer"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("createButton"), true);

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
