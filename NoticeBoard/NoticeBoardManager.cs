using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class NoticeBoardManager : Singleton<NoticeBoardManager>, IRaycaster
    {
        public static NoticeBoardManager Instance
        {
            get
            {
                return ((NoticeBoardManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        private string m_userKey = "USERTYPE";
        private float interactionDistance = 5;
        private bool useLocalDistance = true;
        private INotice m_currentNotice = null;

        public bool OverrideDistance { get { return useLocalDistance; } }

        public string UserCheckKey
        {
            get
            {
                return m_userKey;
            }
        }

        private Notice m_activeNotice;

        public Notice ActiveNotice
        {
            get
            {
                return m_activeNotice;
            }
        }

        public INotice HoveredOverNotice
        {
            get
            {
                return m_currentNotice;
            }
        }

        public float Distance
        {
            get
            {
                float distance = 5000;

                //define camera to use
                if (!MapManager.Instance.TopDownViewActive)
                {
                    if (PlayerManager.Instance.ThirdPersonCameraActive)
                    {
                        distance = interactionDistance + Vector3.Distance(PlayerManager.Instance.GetLocalPlayer().TransformObject.position, PlayerManager.Instance.GetLocalPlayer().MainCamera.transform.GetChild(0).position);
                    }
                    else
                    {
                        distance = interactionDistance;
                    }
                }
                else
                {
                    //cannot perform chair stuff in topdown view
                    return -1;
                }

                return distance;
            }
        }

        private void Awake()
        {
            RaycastManager.Instance.Raycasters.Add(this);
        }

        public void RaycastHit(RaycastHit hit, out Transform hitObject)
        {
            INotice notice = hit.transform.GetComponent<INotice>();

            if(notice != null)
            {
                hitObject = hit.transform;
            }
            else
            {
                hitObject = null;
            }

            if (notice != null)
            {
                string user = PlayerManager.Instance.GetLocalPlayer().CustomizationData.ContainsKey(m_userKey) ? PlayerManager.Instance.GetLocalPlayer().CustomizationData[m_userKey].ToString() : "";

                //need to check if the user can continue
                if (notice.UserCanUse(user))
                {
                    if(m_currentNotice != null && !m_currentNotice.Equals(notice))
                    {
                        m_currentNotice.OnHover(false);
                    }

                    notice.OnHover(true);
                    m_currentNotice = notice;
                }
            }
            else
            {
                if(m_currentNotice != null)
                {
                    m_currentNotice.OnHover(false);
                }

                m_currentNotice = null;
                hitObject = null;
            }
        }

        public void RaycastMiss()
        {
            if (m_currentNotice != null)
            {
                m_currentNotice.OnHover(false);
            }

            m_currentNotice = null;
        }

        private void ResetHighlight()
        {
            
        }

        public void CreateAllNotices(List<NoticeBoardAPI.NoticeJson> notices)
        {
            List<IBoard> boards = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IBoard>().ToList();

            foreach(NoticeBoardAPI.NoticeJson n in notices)
            {
                IBoard br = boards.FirstOrDefault(x => x.GetBoardID.Equals(n.noticeboard_id));
                
                if(br != null)
                {
                    br.Create(n);
                }
            }
        }

        public void DeleteNotice(int noticeID)
        {
            m_currentNotice = null;

            List<Notice> notices = FindObjectsByType<Notice>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();

            for(int i = 0; i < notices.Count; i++)
            {
                if(notices[i].JsonID.Equals(noticeID))
                {
                    Destroy(notices[i].gameObject);
                    break;
                }
            }
        }

        public void RemoteSyncNoticeTransform(int noticeID, Vector3 localPosition, float scale)
        {
            Debug.Log("RemoteSyncNoticeTransform: noticeID = " + noticeID);

            Notice notice = FindObjectsByType<Notice>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList().FirstOrDefault(x => x.ID.Equals(noticeID));

            if (notice != null)
            {
                notice.RemoteSync(localPosition, scale);
            }
        }

        public void OpenNotice(Notice notice)
        {
            m_activeNotice = notice;

            PlayerManager.Instance.FreezePlayer(true);
            RaycastManager.Instance.CastRay = false;

            HUDManager.Instance.ShowHUDNavigationVisibility(false);
            HotspotManager.Instance.HotspotToggle.isOn = false;
            NavigationManager.Instance.ToggleJoystick(false);
            MMORoom.Instance.ToggleLocalProfileInteraction(false);

            HUDManager.Instance.ToggleHUDControl("NOTICE_CONTROL", true);
        }

        public void ReturnNotice()
        {
            if(m_activeNotice != null)
            {
                HUDManager.Instance.ToggleHUDControl("NOTICE_CONTROL", false);
                m_activeNotice.Return();

                HUDManager.Instance.ShowHUDNavigationVisibility(true);
                NavigationManager.Instance.ToggleJoystick(true);
                MMORoom.Instance.ToggleLocalProfileInteraction(true);

                m_activeNotice = null;

                PlayerManager.Instance.FreezePlayer(false);
                RaycastManager.Instance.CastRay = true;

            }

            m_activeNotice = null;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(NoticeBoardManager), true)]
        public class NoticeBoardManager_Editor : BaseInspectorEditor
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

    public interface INotice
    {
        void OnHover(bool isOver);
        void OnClick();

        bool UserCanUse(string user);

        GameObject GO { get; }

        Transform ThisNoticeTransform { get; }

        void Sync();
    }

    public interface IBoard
    {
        NoticeType Type { get; }

        string GetBoardID { get; }

        void Remove(NoticeBoardAPI.NoticeJson json);

        void Create(NoticeBoardAPI.NoticeJson json);
    }

    public enum NoticeType { Image, Text, Both }
}
