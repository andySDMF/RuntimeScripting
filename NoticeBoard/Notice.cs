using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Notice : UniqueID, INotice
    {
        [SerializeField]
        private NoticeType noticeType = NoticeType.Image;

        [SerializeField]
        private string content = "";

        [SerializeField]
        private string websiteLink = "";

        [SerializeField]
        private RawImage imageScript;

        [SerializeField]
        private TextMeshProUGUI textScript;

        [SerializeField]
        private GameObject layoutButtons;

        private bool m_isImageStreamed = false;

        public GameObject GO 
        { 
            get 
            {
                if (noticeType.Equals(NoticeType.Image))
                {
                    return imageScript.gameObject;
                }
                else
                {
                    return textScript.transform.parent.gameObject;
                }
            } 
        }

        public NoticeType GetNoticeType
        {
            get
            {
                return noticeType;
            }
        }

        public string URL {  get { return websiteLink; } }

        public NoticeBoardAPI.NoticeJson Json { get; set; }

        public int JsonID { get { return Json.id; } }

        public Transform ThisNoticeTransform { get { return transform; } }

        private bool Moving = false;
        private Vector3 targetPosition;

        private void Start()
        {
            if(AppManager.IsCreated)
            {
                bool show = AppManager.Instance.Data.IsAdminUser;

                if (GetComponentInParent<NoticePinBoard>() != null)
                {
                    show = GetComponentInParent<NoticePinBoard>().GetSettings().adminOnly ? AppManager.Instance.Data.IsAdminUser : true;
                }
                
                layoutButtons.transform.GetChild(1).gameObject.SetActive(show);
                layoutButtons.transform.GetChild(2).gameObject.SetActive(show);

                if(GetComponentInParent<NoticePinBoard>())
                {
                    layoutButtons.GetComponent<HorizontalLayoutGroup>().spacing = 0.002f;

                    for (int i = 0; i < layoutButtons.transform.childCount; i++)
                    {
                        LayoutElement le = layoutButtons.transform.GetChild(i).GetComponent<LayoutElement>();
                        le.minHeight = 0.03f;
                        le.minWidth = 0.02f;
                        le.preferredHeight = le.minHeight;
                        le.preferredWidth = le.minWidth;
                    }
                }
            }
        }

        private void OnEnable()
        {
            if(Json != null)
            {
                OnEditCallback(Json);
                targetPosition = this.transform.localPosition;
            }
        }

        private void OnDisable()
        {
            if(imageScript.texture != null)
            {
                if(m_isImageStreamed)
                {
                    Destroy(imageScript.texture);
                }
                
                imageScript.texture = null;
            }

            m_isImageStreamed = false;
        }

        public void Update()
        {
            //If product was set moving, move it to the target
            if (Moving && this.transform.localPosition != targetPosition)
            {
                if ((targetPosition - this.transform.localPosition).magnitude > 0.1f)
                {
                    this.transform.localPosition = Vector3.MoveTowards(this.transform.localPosition, targetPosition, 0.1f);
                }
                else
                {
                    this.transform.localPosition = targetPosition;
                    Moving = false;
                }

                Json.pos_x = transform.localPosition.x;
                Json.pos_y = transform.localPosition.y;
                Json.pos_z = transform.localPosition.z;
                Json.scale = transform.localScale.x;
            }
        }

        public void RemoteSync(Vector3 target, float scale)
        {
            Moving = true;
            targetPosition = target;
        }

        public void Sync()
        {
            Json.pos_x = transform.localPosition.x;
            Json.pos_y = transform.localPosition.y;
            Json.pos_z = transform.localPosition.z;
            Json.scale = transform.localScale.x;

            MMOManager.Instance.SendRPC("SyncNoticeTransform", (int)MMOManager.RpcTarget.Others, Json.id, transform.localPosition, Json.scale);
            NoticeBoardAPI.Instance.EditNotice(Json);
        }

        public void OnEditCallback(NoticeBoardAPI.NoticeJson json)
        {
            OnDisable();

            Json = json;

            if (Json != null)
            {
                json.bgk_color = json.bgk_color.Replace("\"", "");
                content = Json.content_url;
                websiteLink = Json.website_link;
                noticeType = Json.Type.Equals(BrandLab360.NoticeType.Image) ? NoticeType.Image : NoticeType.Text;
            }

            if (noticeType.Equals(NoticeType.PDF))
            {
                Debug.Log("PDF not supported. GO [" + gameObject.name + "] setting to inactive");
                gameObject.SetActive(false);
            }
            else if (noticeType.Equals(NoticeType.Image))
            {
                imageScript.gameObject.SetActive(true);
                textScript.transform.parent.gameObject.SetActive(false);
            }
            else
            {
                textScript.transform.parent.gameObject.SetActive(true);
                imageScript.gameObject.SetActive(false);

                ///need to get the color of the Json
                string[] split = json.bgk_color.Split('|');

                Color col;
                if (ColorUtility.TryParseHtmlString(split[1].Replace("\"", ""), out col))
                {
                    textScript.color = new Color(col.r, col.g, col.b, 255);
                }
                else
                {
                    textScript.color = Color.black;
                }

                if (ColorUtility.TryParseHtmlString(split[0].Replace("\"", ""), out col))
                {
                    textScript.transform.parent.GetComponent<Image>().color = new Color(col.r, col.g, col.b, 255);
                }
                else
                {
                    textScript.transform.parent.GetComponent<Image>().color = Color.white;
                }
            }

            layoutButtons.SetActive(false);

            //load content
            if (noticeType.Equals(NoticeType.Image))
            {
                if(!string.IsNullOrEmpty(content))
                {
                    StartCoroutine(LoadImage(content));
                }
            }
            else
            {
                textScript.text = Json.content_text;
                imageScript.texture = null;
            }
        }

        public void OnHover(bool isOver)
        {
            if(layoutButtons.activeInHierarchy != isOver)
            {
                layoutButtons.SetActive(isOver);
            }

            //need to call the parent on hover over
            GetComponentInParent<NoticeBoard>().OnHover(isOver);
        }

        public void OnClick()
        {
            if (noticeType.Equals(NoticeType.Image))
            {
                imageScript.enabled = false;
            }
            else
            {
                textScript.transform.parent.GetComponent<Image>().enabled = false;
                textScript.enabled = false;
            }

            AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Open, "Open Notice " + AnalyticReference);

            NoticeBoardManager.Instance.OpenNotice(this);
            OnHover(false);
        }

        public void Edit()
        {
            NoticeUploader nu = HUDManager.Instance.GetHUDScreenObject("NOTICE_SCREEN").GetComponentInChildren<NoticeUploader>(true);

            nu.EditableJson = Json;
            nu.EditMode = true;
            nu.NoticeBoard = Json.noticeboard_id;
            nu.PickerDefaults = GetComponentInParent<NoticeBoard>().PickerDefaults;
            HUDManager.Instance.ToggleHUDScreen("NOTICE_SCREEN");
        }

        public void Bin()
        {
            NoticeBoardAPI.Instance.DeleteNotice(Json.id, OnNoticeDeletedCallback);
        }

        public void Return()
        {
            if (noticeType.Equals(NoticeType.Image))
            {
                imageScript.enabled = true;
            }
            else
            {
                textScript.transform.parent.GetComponent<Image>().enabled = true;
                textScript.enabled = true;
            }
        }

        public bool UserCanUse(string user)
        {
            return CanUserControlThis(user);
        }

        private void OnNoticeDeletedCallback(bool success)
        {
            if(success)
            {
                NoticeBoardManager.Instance.DeleteNotice(Json.id);
            }
        }

        private IEnumerator LoadImage(string url)
        {
            if(url.Contains("http"))
            {
                m_isImageStreamed = true;

                //webrequest
                UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, true);

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(request.error))
                {
                    imageScript.texture = DownloadHandlerTexture.GetContent(request);
                }

                //dispose the request as not needed anymore
                request.Dispose();
            }
            else
            {
                m_isImageStreamed = false;
                imageScript.texture = Resources.Load<Texture>(url);
            }

            if(imageScript.texture != null)
            {
                RectTransform rectT = imageScript.GetComponent<RectTransform>();
                float texWidth = imageScript.texture.width;
                float texHeight = imageScript.texture.height;
                float aspectRatio = texWidth / texHeight;

                if (imageScript.texture.width > imageScript.texture.height)
                {
                    //need to expand width to match
                    rectT.anchorMin = new Vector2(0, 0.5f);
                    rectT.anchorMax = new Vector2(1, 0.5f);
                    rectT.offsetMax = Vector2.zero;
                    rectT.offsetMin = Vector2.zero;
                    float height = transform.GetComponent<BoxCollider>().size.y / aspectRatio;
                    rectT.sizeDelta = new Vector2(0, height);
                }
                else
                {
                    //need to expand height to match
                    rectT.anchorMin = new Vector2(0.5f, 0f);
                    rectT.anchorMax = new Vector2(0.5f, 1f);
                    rectT.offsetMax = Vector2.zero;
                    rectT.offsetMin = Vector2.zero;

                    float width = transform.GetComponent<BoxCollider>().size.x * aspectRatio;
                    rectT.sizeDelta = new Vector2(width, 0);
                }
            }

            LayoutGroup lGroup = GetComponentInChildren<LayoutGroup>();
            StartCoroutine(CalculateCollider(lGroup));

            if (imageScript.texture == null)
            {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator CalculateCollider(LayoutGroup lGroup)
        {
            yield return new WaitForEndOfFrame();

            if (lGroup != null)
            {
                RectTransform rectT = lGroup.GetComponent<RectTransform>();
                GetComponent<BoxCollider>().size = new Vector3(rectT.sizeDelta.x / 100, rectT.sizeDelta.y / 100, 0.01f);
            }
        }

        public enum NoticeType { Image, PDF, Text }

#if UNITY_EDITOR
        [CustomEditor(typeof(Notice), true), CanEditMultipleObjects]
        public class Notice_Editor : UniqueID_Editor
        {
            private Notice noticeScript;

            private void OnEnable()
            {
                GetBanner();
                Initialise();
            }
            public override void OnInspectorGUI()
            {
                DisplayBanner();

                if (noticeScript.GetComponentInParent<NoticeBoard>() == null)
                {
                    DisplayID();
                }

                serializedObject.Update();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Notice Setup", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("noticeType"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("content"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("websiteLink"), true);

                EditorGUILayout.LabelField("UI", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("layoutButtons"), true);

                if (noticeScript.noticeType.Equals(NoticeType.Image))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("imageScript"), true);
                }
                else if(noticeScript.noticeType.Equals(NoticeType.Text))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("textScript"), true);
                }
                else
                {
                    EditorGUILayout.LabelField("PDF not supported");
                }


                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(noticeScript);

                    if (m_instances != null)
                    {
                        m_instances.AddIOObject(noticeScript.ID, noticeScript.GetSettings());
                    }
                }
            }

            protected override void Initialise()
            {
                noticeScript = (Notice)target;

                if (noticeScript.GetComponentInParent<NoticeBoard>() == null)
                {
                    base.Initialise();
                }
            }
        }
#endif
    }
}
