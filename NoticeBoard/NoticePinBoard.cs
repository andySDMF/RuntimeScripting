using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class NoticePinBoard : NoticeBoard
    {
        private Vector3 dragOrigin = Vector3.zero;

        public override void Create(NoticeBoardAPI.NoticeJson json)
        {
            //check if the notice already exists
            Notice[] all = GetComponentsInChildren<Notice>();

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].JsonID.Equals(json.id))
                {
                    all[i].OnEditCallback(json);
                    return;
                }
            }

            UnityEngine.Object obj = Resources.Load<UnityEngine.Object>("Noticeboards/Canvas_Notice");

            if (obj != null)
            {
                GameObject go = Instantiate(obj, Vector3.zero, Quaternion.identity, transform.GetChild(0)) as GameObject;

                go.transform.localScale = new Vector3(json.scale, json.scale, json.scale);
                go.transform.rotation = transform.GetChild(0).rotation;
                go.transform.localPosition = new Vector3(json.pos_x, json.pos_y, json.pos_z);

                go.GetComponentInChildren<Notice>(true).Json = json;
                go.name = go.name + "_" + json.id;
                go.SetActive(true);
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(0.1f, 0.1f);
                go.GetComponent<BoxCollider>().size = go.GetComponent<RectTransform>().sizeDelta;
            }

            AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, "Pin Notice Upload " + AnalyticReference);
        }

        public override void OnHover(bool isOver)
        {
            if(isOver && NoticeBoardManager.Instance.HoveredOverNotice != null)
            {
                if(settings.adminOnly && !AppManager.Instance.Data.IsAdminUser)
                {
                    return;
                }

                if(dragOrigin.Equals(Vector3.zero))
                {
                    dragOrigin = InputManager.Instance.GetMousePosition();
                }
      
                if (InputManager.Instance.GetMouseButton(0) && !PlayerManager.Instance.GetLocalPlayer().IsButtonHeldDown)
                {
                    Camera cam = Camera.main;
                    float distance = CoreManager.Instance.playerSettings.interactionDistance;
                    Ray ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMousePosition());
                    RaycastHit[] hits = Physics.RaycastAll(ray, distance);

                    foreach (RaycastHit ht in hits)
                    {
                        if(ht.transform.Equals(NoticeBoardManager.Instance.HoveredOverNotice.ThisNoticeTransform))
                        {
                            if (Vector3.Distance(dragOrigin, InputManager.Instance.GetMousePosition()) > 60f)
                            {
                                //transform the hit point on the bounds into local space for the product pos
                                var transPos = transform.InverseTransformPoint(ht.point);
                                var previousPos = NoticeBoardManager.Instance.HoveredOverNotice.ThisNoticeTransform.localPosition;
                                NoticeBoardManager.Instance.HoveredOverNotice.ThisNoticeTransform.localPosition = new Vector3(transPos.x, transPos.y, NoticeBoardManager.Instance.HoveredOverNotice.ThisNoticeTransform.localPosition.z);

                                if (NoticeBoardManager.Instance.HoveredOverNotice.ThisNoticeTransform.localPosition.x <= -0.5f || NoticeBoardManager.Instance.HoveredOverNotice.ThisNoticeTransform.localPosition.x >= 0.5f)
                                {
                                    NoticeBoardManager.Instance.HoveredOverNotice.ThisNoticeTransform.localPosition = new Vector3(previousPos.x, NoticeBoardManager.Instance.HoveredOverNotice.ThisNoticeTransform.localPosition.y, NoticeBoardManager.Instance.HoveredOverNotice.ThisNoticeTransform.localPosition.z);
                                }

                                if (NoticeBoardManager.Instance.HoveredOverNotice.ThisNoticeTransform.localPosition.y <= -0.5f || NoticeBoardManager.Instance.HoveredOverNotice.ThisNoticeTransform.localPosition.y >= 0.5f)
                                {
                                    NoticeBoardManager.Instance.HoveredOverNotice.ThisNoticeTransform.localPosition = new Vector3(NoticeBoardManager.Instance.HoveredOverNotice.ThisNoticeTransform.localPosition.x, previousPos.y, NoticeBoardManager.Instance.HoveredOverNotice.ThisNoticeTransform.localPosition.z);
                                }
                            }

                            break;
                        }
                    }
                }

                if (InputManager.Instance.GetMouseButtonUp(0))
                {
                    //need to send update on DB and send across network
                    NoticeBoardManager.Instance.HoveredOverNotice.Sync();
                    dragOrigin = Vector3.zero;
                }
            }
        }

        public override void OnClick()
        {
            //check lock status
            if (m_lock != null)
            {
                if (m_lock.IsLocked) return;
            }

            NoticeUploader nu = HUDManager.Instance.GetHUDScreenObject("NOTICE_SCREEN").GetComponentInChildren<NoticeUploader>(true);

            nu.PickerDefaults = settings.pickerDefaults;
            nu.NoticeBoard = ID;
            nu.EnableDisplayPeriod = settings.enableDisplayPeriod;
            
            HUDManager.Instance.ToggleHUDScreen("NOTICE_SCREEN");
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(NoticePinBoard), true)]
        public class NoticePinBoard_Editor : NoticeBoard_Editor
        {

        }
#endif
    }
}
