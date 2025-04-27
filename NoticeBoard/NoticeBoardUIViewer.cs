using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class NoticeBoardUIViewer : MonoBehaviour
    {
        [Header("Control")]
        [SerializeField]
        private float movementSpeed = 0.1f;
        [SerializeField]
        private float zoomSpeed = 0.5f;

        [Header("Website")]
        [SerializeField]
        private GameObject websiteButton;

        [Header("Mobile Layout")]
        [SerializeField]
        private RectTransform mainLayout;

        private float m_cacheAnchorFromBottom;

        private float minZoom = 1f;
        private float maxZoom = 1f;
        private Coroutine process;

        private GameObject go;
        private bool m_thirdPerson = false;

        private void Awake()
        {
            if (!AppManager.IsCreated) return;

            m_cacheAnchorFromBottom = mainLayout.anchoredPosition.y;
        }

        private void OnEnable()
        {
            Canvas can = new GameObject().AddComponent<Canvas>();
            can.additionalShaderChannels |= AdditionalCanvasShaderChannels.Normal;
            can.additionalShaderChannels |= AdditionalCanvasShaderChannels.Tangent;
            can.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3;

            can.gameObject.name = "NoticeViewer";
            can.GetComponent<RectTransform>().sizeDelta = new Vector2(1, 1);

            if(NoticeBoardManager.Instance.ActiveNotice != null)
            {
                go = Instantiate(NoticeBoardManager.Instance.ActiveNotice.GO, can.transform);

                if (NoticeBoardManager.Instance.ActiveNotice.GetNoticeType.Equals(Notice.NoticeType.Image))
                {
                    go.GetComponent<RawImage>().enabled = true;
                }
                else
                {
                    go.GetComponent<Image>().enabled = true;
                    go.GetComponentInChildren<TMPro.TextMeshProUGUI>().enabled = true;
                }

                RectTransform rectT = go.GetComponent<RectTransform>();

                rectT.anchorMin = new Vector2(0.5f, 0.5f);
                rectT.anchorMax = new Vector2(0.5f, 0.5f);
                rectT.offsetMax = Vector2.zero;
                rectT.offsetMin = Vector2.zero;

                if (NoticeBoardManager.Instance.ActiveNotice.GetNoticeType.Equals(Notice.NoticeType.Image))
                {
                    rectT.sizeDelta = new Vector2(go.GetComponent<RawImage>().texture.width / 10000f, go.GetComponent<RawImage>().texture.height / 10000f);
                }
                else
                {
                    rectT.sizeDelta = new Vector2(0.1f, 0.1f);
                }
            }

            m_thirdPerson = PlayerManager.Instance.ThirdPersonCameraActive;

            if(m_thirdPerson)
            {
                PlayerManager.Instance.SwitchPerspectiveCameraMode(PerspectiveCameraMode.FirstPerson);
            }

            can.transform.SetParent(PlayerManager.Instance.GetLocalPlayer().MainCamera.transform.GetChild(0));
            can.transform.localPosition = new Vector3(0, 0, 0.2f);
            can.transform.localEulerAngles = new Vector3(0, 0, 0);

            if (NoticeBoardManager.Instance.ActiveNotice != null)
            {
                if (NoticeBoardManager.Instance.ActiveNotice.URL.Equals("n/a"))
                {
                    websiteButton.SetActive(false);
                }
                else
                {
                    websiteButton.SetActive(!string.IsNullOrEmpty(NoticeBoardManager.Instance.ActiveNotice.URL));
                }
            }

            if(go != null)
            {
                minZoom = go.transform.localScale.x;
                maxZoom = minZoom * 10;
            }

            if(can.gameObject != null)
            {
                go = can.gameObject;
            }

            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
        }

        private void OnDisable()
        {
            if (!AppManager.IsCreated) return;

            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        private void Update()
        {
            RaycastManager.Instance.CastRay = false;

            if (go != null)
            {
                //this is where we control and move/scale the active notice
                if (InputManager.Instance.GetMouseButton(0))
                {
                    Vector2 delta = InputManager.Instance.GetMouseDelta("Mouse X", "Mouse Y");
                    float x = go.transform.localPosition.x + delta.x;
                    float y = go.transform.localPosition.y + delta.y;
                    float z = go.transform.localPosition.z;
                    Vector3 pos = Vector3.Lerp(go.transform.localPosition, new Vector3(x, y, z), Time.deltaTime * movementSpeed);
                    go.transform.localPosition = pos;
                }

                float scrollWheelInput = InputManager.Instance.GetMouseScrollWheel();

                if (!scrollWheelInput.Equals(0.0f))
                {
                    if(process != null)
                    {
                        StopCoroutine(process);
                        process = null;
                    }

                    Vector3 sca = go.transform.localScale;
                    float amount = scrollWheelInput * 0.1f;
                    float newVal = sca.x + amount;
                    go.transform.localScale = Vector3.Lerp(sca, new Vector3(newVal, newVal, 1), Time.deltaTime * zoomSpeed);

                    Clamp();
                }
            }
        }
        public void Return()
        {
            if(go !=  null)
            {
                Destroy(go);
            }

            if(m_thirdPerson)
            {
                PlayerManager.Instance.SwitchPerspectiveCameraMode(PerspectiveCameraMode.ThirdPerson);
            }

            NoticeBoardManager.Instance.ReturnNotice();
        }

        public void OpenWebURL()
        {
            if(NoticeBoardManager.Instance.ActiveNotice != null)
            {
                if(!string.IsNullOrEmpty(NoticeBoardManager.Instance.ActiveNotice.URL))
                {
                    InfotagManager.InfoTagURL webTag = new InfotagManager.InfoTagURL();
                    webTag.title = "Notice";
                    webTag.url = NoticeBoardManager.Instance.ActiveNotice.URL;

                    InfotagManager.Instance.ShowInfoTag(InfotagType.Web, webTag);
                }
            }
        }

        public void ZoomIn()
        {
            if(process != null)
            {
                StopCoroutine(process);
            }

            process = StartCoroutine(LerpZoom(go.transform.localScale.x + 0.5f));
        }

        public void ZoomOut()
        {
            if (process != null)
            {
                StopCoroutine(process);
            }

            process = StartCoroutine(LerpZoom(go.transform.localScale.x - 0.5f));
        }

        private IEnumerator LerpZoom(float val)
        {
            while(Vector3.Distance(go.transform.localScale, new Vector3(val, val, 1)) > 0)
            {
                go.transform.localScale = Vector3.Lerp(go.transform.localScale, new Vector3(val, val, 1), Time.deltaTime * zoomSpeed);
                Clamp();
                yield return null;
            }

            Clamp();
            process = null;
        }

        private void Clamp()
        {
            if (go.transform.localScale.x < minZoom)
            {
                go.transform.localScale = new Vector3(minZoom, minZoom, 1);
            }

            if (go.transform.localScale.x > maxZoom)
            {
                go.transform.localScale = new Vector3(maxZoom, maxZoom, 1);
            }
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    mainLayout.pivot = new Vector2(0.5f, 0.0f);
                    mainLayout.anchoredPosition = new Vector2(0.0f, m_cacheAnchorFromBottom);
                }
                else
                {
                    float aspect = arg2 / arg1;
                    RectTransform mobileControl = HUDManager.Instance.GetHUDControlObject("MOBILE_JOYSTICK").GetComponent<RectTransform>();

                    mainLayout.pivot = new Vector2(0.0f, 0.0f);

                    if (mobileControl.gameObject.activeInHierarchy)
                    {

                        if (PlayerManager.Instance.MainControlSettings.controllerType == 0)
                        {
                            mainLayout.anchoredPosition = new Vector2(mobileControl.GetChild(0).GetComponent<RectTransform>().anchoredPosition.x, m_cacheAnchorFromBottom + mobileControl.GetChild(0).GetComponent<RectTransform>().sizeDelta.y * aspect);
                        }
                        else
                        {
                            mainLayout.anchoredPosition = new Vector2(mobileControl.GetChild(1).GetComponent<RectTransform>().anchoredPosition.x, m_cacheAnchorFromBottom + mobileControl.GetChild(1).GetComponent<RectTransform>().sizeDelta.y * aspect);
                        }
                    }
                    else
                    {
                        mainLayout.anchoredPosition = new Vector2(25.0f, m_cacheAnchorFromBottom);
                    }

                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(NoticeBoardUIViewer), true)]
        public class NoticeBoardUIViewer_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("movementSpeed"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("zoomSpeed"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("websiteButton"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mainLayout"), true);

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
