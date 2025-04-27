using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class AvatarSelectionPreview : MonoBehaviour
    {
        [SerializeField]
        private Transform cellContainer;

        [SerializeField]
        private GameObject cellPrefab;

        [SerializeField]
        private GameObject buttonArea;

        [SerializeField]
        private RectTransform scrollArea;

        private List<AvatarSelectionCell> m_cells = new List<AvatarSelectionCell>();

        private GridLayoutGroup m_gridLayout;

        private void OnEnable()
        {
            if(AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe))
            {
                if(AppManager.Instance.Settings.projectSettings.readyPlayerMeMode.Equals(ReadyPlayerMeMode.Fixed))
                {
                    buttonArea.SetActive(false);
                }
                else
                {
                    buttonArea.SetActive(true);
                }
            }

            m_gridLayout = cellContainer.GetComponent<GridLayoutGroup>();

            foreach (string avatar in AppManager.Instance.Settings.playerSettings.fixedAvatars)
            {
                if(AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe))
                {
                    if (!avatar.Contains("RPM_")) continue;
                }

                GameObject go = Instantiate(cellPrefab, Vector3.zero, Quaternion.identity, cellContainer);
                go.transform.localScale = Vector3.one;
                go.gameObject.SetActive(true);
                AvatarSelectionCell cell = go.GetComponent<AvatarSelectionCell>();
                cell.Set(avatar, "ProfilePictures/" + avatar);

                m_cells.Add(cell);
            }
        }

        private void Update()
        {
            if (!AppManager.IsCreated) return;

            if(AppManager.Instance.Data.IsMobile)
            {
                if(OrientationManager.Instance.CurrentOrientation.Equals(OrientationType.landscape))
                {
                    m_gridLayout.constraintCount = 3;
                    float sizeX = (scrollArea.sizeDelta.x / 3) - (m_gridLayout.spacing.x * 1.25f);
                    m_gridLayout.cellSize = new Vector2(sizeX, sizeX);
                }
                else
                {
                    m_gridLayout.constraintCount = 1;
                    float sizeX = (scrollArea.sizeDelta.x / 1) - (m_gridLayout.spacing.x * 1.25f);
                    m_gridLayout.cellSize = new Vector2(sizeX, sizeX);
                }
            }
          
        }

        private void OnDisable()
        {
            foreach(AvatarSelectionCell cell in m_cells)
            {
                Destroy(cell.gameObject);
            }

            m_cells.Clear();


            if(AppManager.Instance.Data.CurrentSceneReady)
            {
                PlayerManager.Instance.FreezePlayer(false);
            }
        }

        public void ChooseAvatar(string avatar)
        {
            AppManager.Instance.Data.CustomiseJson = avatar;
            AppManager.Instance.Data.FixedAvatarName = avatar;

            if (AppManager.Instance.Settings.projectSettings.loginMode.Equals(LoginMode.Standard))
            {
                bool save = AppManager.Instance.Settings.projectSettings.avatarMode.Equals(AvatarMode.Custom) ? true : AppManager.Instance.Settings.projectSettings.alwaysRandomiseAvatar ? false : true;

                if (AppManager.Instance.Settings.projectSettings.useIndexedDB && save)
                {
                    string nName = "User-" + AppManager.Instance.Data.NickName;
                    string admin = "Admin-" + (AppManager.Instance.Data.IsAdminUser ? 1 : 0).ToString();
                    string json = "Json-" + AppManager.Instance.Data.CustomiseJson;
                    string friends = "Friends-" + AppManager.Instance.Data.CustomiseFriends;
                    string games = "Games-" + AppManager.Instance.Data.RawGameData;
                    string prof = "";

                    if (AppManager.Instance.Data.LoginProfileData != null)
                    {
                        prof = "Name*" + AppManager.Instance.Data.LoginProfileData.name + "|About*" + AppManager.Instance.Data.LoginProfileData.about + "|Pic*" + AppManager.Instance.Data.LoginProfileData.picture_url;
                    }
                    else
                    {
                        AppManager.Instance.Data.LoginProfileData = new ProfileData();
                    }

                    string profile = "Profile-" + prof;

                    if (AppManager.Instance.UserExists)
                    {
                        IndexedDbManager.Instance.UpdateEntry("userData", nName + ":" + admin + ":" + json + ":" + friends + ":" + profile + ":" + games);
                    }
                    else
                    {
                        IndexedDbManager.Instance.InsertEntry("userData", nName + ":" + admin + ":" + json + ":" + friends + ":" + profile + ":" + games);
                    }
                }
            }
            else
            {
                AppManager.Instance.Data.LoginProfileData.avatar_data = AppManager.Instance.Data.CustomiseJson;
                AppManager.Instance.UpdateLoginsAPI();

               // string projectID = string.IsNullOrEmpty(AppManager.Instance.Settings.projectSettings.clientName) ? AppManager.Instance.Data.ProjectID : AppManager.Instance.Settings.projectSettings.clientName;
               // LoginsAPI.Instance.UpdateUser(AppManager.Instance.Data.NickName, projectID, JsonUtility.ToJson(AppManager.Instance.Data.LoginProfileData), AppManager.Instance.Data.LoginProfileData.password, AppManager.Instance.Data.RawFriendsData, AppManager.Instance.Data.RawGameData);
            }

            AvatarManager.Instance.Customise(PlayerManager.Instance.GetLocalPlayer());
            PlayerManager.Instance.FreezePlayer(false);

            gameObject.SetActive(false);

            if(!AppManager.Instance.Data.RoomEstablished)
            {
                //load main scene
                AppManager.Instance.OnAvatarApplied();
            }
        }

        public void Edit()
        {
            gameObject.SetActive(false);
            HUDManager.Instance.OpenCustomizeAvatar(false);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AvatarSelectionPreview), true)]
        public class AvatarSelectionPreview_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cellContainer"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cellPrefab"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonArea"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("scrollArea"), true);

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
