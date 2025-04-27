using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Image))]
    public class ContentImageScreen : UniqueID, IPointerDownHandler, IPointerUpHandler, IDragHandler, IContentLoader
    {
        [SerializeField]
        private float minZoom = 1.0f;

        [SerializeField]
        private float maxZoom = 10;

        [SerializeField]
        private float zoomSpeed = 10f;

        [SerializeField]
        private float fractionToZoomIn = 0.2f;

        [SerializeField]
        private bool lerpAlpha = false;

        [SerializeField]
        private RectTransform content;
        [SerializeField]
        private RawImage output;
        [SerializeField]
        private GameObject loader;

        [SerializeField]
        private Lock deleteLock;

        private float currentZoom = 1;
        private bool isPinching = false;
        private float startPinchDist;
        private float startPinchZoom;
        private Vector2 startPinchCenterPosition;
        private Vector2 startPinchScreenPosition;
        private float mouseWheelSensitivity = 1;

        private Rect touchArea;
        private bool hasLoaded = false;
        private ContentsManager.ContentFileInfo m_fileInfo;
        private string m_url = "";
        private bool m_isResource = false;

        /// <summary>
        /// Access to the lock object used on this video screen
        /// </summary>
        public Lock LockUsed { get { return deleteLock; } set { deleteLock = value; } }

        /// <summary>
        /// States if this image screen is networked or not
        /// </summary>
        public bool IsNetworked { get; set; }

        /// <summary>
        /// The owner who is controlling this image screen
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Event to subscribe to on this image controls
        /// </summary>
        public System.Action<string, string> LocalStateChange { get; set; }


        private Vector3 m_targetPosition = Vector3.zero;

        /// <summary>
        /// Access to the current Data of this image screen
        /// </summary>
        public string Data { get; private set; }

        /// <summary>
        /// The current URL loaded into this image screen
        /// </summary>
        public string URL { get { return m_url; } }

        /// <summary>
        /// States if this image is loaded
        /// </summary>
        public bool IsLoaded { get { return hasLoaded; } }

        public override bool HasParent
        {
            get
            {
                return GetComponentInParent<ConferenceScreen>() != null || GetComponentInParent<WorldContentUpload>() != null || deleteLock == null;
            }
        }

        private void OnEnable()
        {
            //init
            Initialise();
        }

        private void OnDisable()
        {
            //if conference, sync close
            if (IsNetworked)
            {
                if (LocalStateChange != null)
                {
                    LocalStateChange.Invoke("CLOSE", "");
                }
            }

            m_isResource = false;
            IsNetworked = false;
            Owner = "";
            LocalStateChange = null;

            //unload anything when this object is disabled-memory
            Unload();
        }

        private void Update()
        {
            if (!hasLoaded) return;

            if(IsNetworked && Owner != null && !Owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
            {
                content.localPosition = Vector3.Lerp(content.localPosition, m_targetPosition, Time.deltaTime * 10);
            }

            //used to zoom in/out
            if ((!InputManager.Instance.IsStandardInputUsed() && !Application.isEditor) || (IsNetworked && Owner != null && !Owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID)))
            {
                LerpZoom();
                CheckBelowMinZoom();
                return;
            }

            if (!string.IsNullOrEmpty(Owner) && IsNetworked)
            {
                if (!Owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID)) return;
            }

            //pinch control is using touchscreen - NON web
            if (content != null && output.texture != null)
            {
                //if (Input.touchCount == 2)
                //{
                //    if (!isPinching)
                //    {
                //        isPinching = true;

                //        OnPinchStart();
                //    }

                //    if (touchArea != null)
                //    {
                //        if (touchArea.Contains(Input.touches[0].position) && touchArea.Contains(Input.touches[1].position))
                //        {
                //            OnPinch();
                //        }
                //    }
                //    else
                //    {
                //        OnPinch();
                //    }

                //    return;
                //}
                //else
                //{
                //isPinching = false;
                //}

                //scroll wheel for mous zoom - editor and PC 
                float scrollWheelInput = InputManager.Instance.GetMouseScrollWheel();

                if (!scrollWheelInput.Equals(0.0f))
                {
                    bool scroll;

                    if (touchArea != null)
                    {
                        if (touchArea.Contains(InputManager.Instance.GetMousePosition()))
                        {
                            scroll = true;
                        }
                        else
                        {
                            scroll = false;
                        }
                    }
                    else
                    {
                        scroll = true;
                    }

                    if (scroll)
                    {
                        if (Mathf.Abs(scrollWheelInput) > float.Epsilon)
                        {
                            currentZoom *= 1 + scrollWheelInput * mouseWheelSensitivity;
                            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

                            startPinchScreenPosition = (Vector2)InputManager.Instance.GetMousePosition();
                            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, startPinchScreenPosition, null, out startPinchCenterPosition);
                            Vector2 pivotPosition = new Vector3(content.pivot.x * content.rect.size.x, content.pivot.y * content.rect.size.y);
                            Vector2 posFromBottomLeft = pivotPosition + startPinchCenterPosition;
                            SetPivot(content, new Vector2(posFromBottomLeft.x / content.rect.width, posFromBottomLeft.y / content.rect.height));

                            if (Mathf.Abs(content.localScale.x - currentZoom) > 0.0f)
                                content.localScale = Vector3.Lerp(content.localScale, Vector3.one * currentZoom, zoomSpeed * Time.deltaTime);
                        }
                    }
                }
                else
                {
                    LerpZoom();
                }

                CheckBelowMinZoom();
            }
        }

        public void UpdateSettings(bool lerpAlpha, float minZoom, float maxZoom, float zoomSpeed, float fractionZoomIn)
        {
            this.lerpAlpha = lerpAlpha;
            this.minZoom = minZoom;
            this.maxZoom = maxZoom;
            this.zoomSpeed = zoomSpeed;
            fractionToZoomIn = fractionZoomIn;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        /// <summary>
        /// Called to ensure the current zoom never goes beyond the min zoom
        /// </summary>
        private void CheckBelowMinZoom()
        {
            if (currentZoom <= minZoom)
            {
                if (!content.localPosition.Equals(Vector3.zero))
                {
                    m_targetPosition = Vector3.zero;
                    content.localPosition = Vector3.Lerp(content.localPosition, Vector3.zero, Time.deltaTime * 10);
                }

                if (!content.pivot.Equals(new Vector2(0.5f, 0.5f)))
                {
                    content.pivot = Vector2.Lerp(content.pivot, new Vector2(0.5f, 0.5f), Time.deltaTime * 10);
                }

                if (!content.localScale.Equals(Vector3.one))
                {
                    content.localScale = Vector3.Lerp(content.localScale, Vector3.one, zoomSpeed * Time.deltaTime);
                }

                currentZoom = minZoom;
            }
        }

        /// <summary>
        /// Called to lerp the content based on the zoom
        /// </summary>
        private void LerpZoom()
        {
            if (currentZoom > maxZoom)
            {
                currentZoom = maxZoom;
            }

            if (Mathf.Abs(content.localScale.x - currentZoom) > 0.0f)
                content.localScale = Vector3.Lerp(content.localScale, Vector3.one * currentZoom, zoomSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Called to perform network change on this image screen
        /// </summary>
        /// <param name="state"></param>
        /// <param name="data"></param>
        public void NetworkStateChange(string state, string data = "")
        {
            if (IsNetworked)
            {
                Debug.Log("State Change [" + state + "], " + data);

                switch (state)
                {
                    case "SETTINGS":
                        PanJson settings = JsonUtility.FromJson<PanJson>(data);

                        if (settings != null)
                        {
                            currentZoom = settings.zoom;
                            m_targetPosition = settings.Get();
                        }
                        break;
                    case "ZOOMIN":
                        ZoomIn(false);
                        break;
                    case "ZOOMOUT":
                        ZoomOut(false);
                        break;
                    case "PAN":
                        PanJson pan = JsonUtility.FromJson<PanJson>(data);

                        if (pan != null)
                        {
                            m_targetPosition = pan.Get();
                        }
                        break;
                    case "OWNER":
                        Owner = data;
                        if (!string.IsNullOrEmpty(Owner))
                        {
                            if (!Owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                            {
                                //set all button hidden
                                Button[] all = GetComponentsInChildren<Button>();

                                for (int i = 0; i < all.Length; i++)
                                {
                                    all[i].transform.localScale = Vector3.zero;
                                }
                            }
                            else
                            {
                                //set all button hidden
                                Button[] all = GetComponentsInChildren<Button>();

                                for (int i = 0; i < all.Length; i++)
                                {
                                    all[i].transform.localScale = Vector3.one;
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Returns JSON of this images screen current settings
        /// </summary>
        /// <returns></returns>
        public string GetSettings()
        {
            PanJson json = new PanJson(content.localPosition.x, content.localPosition.y, content.localPosition.z);
            json.zoom = currentZoom;

            return JsonUtility.ToJson(json);
        }

        /// <summary>
        /// Called to zoom in
        /// </summary>
        public void ZoomIn(bool localPlayer = true)
        {
            if (!hasLoaded) return;

            if(!IsNetworked || !localPlayer)
            {
                currentZoom += fractionToZoomIn;
            }

            if (IsNetworked && localPlayer)
            {
                if (LocalStateChange != null)
                {
                    LocalStateChange.Invoke("ZOOMIN", "");
                }
            }
        }

        /// <summary>
        /// Called to zoom out
        /// </summary>
        public void ZoomOut(bool localPlayer = true)
        {
            if (!hasLoaded) return;

            if (!IsNetworked || !localPlayer)
            {
                currentZoom -= fractionToZoomIn;
            }

            if (IsNetworked && localPlayer)
            {
                if (LocalStateChange != null)
                {
                    LocalStateChange.Invoke("ZOOMOUT", "");
                }
            }
        }

        public void ResetControl()
        {
            currentZoom = 1.0f;

            //set scale
            if (content != null)
            {
                content.localScale = Vector3.one;
                content.localPosition = Vector3.zero;
                content.pivot = new Vector2(0.5f, 0.5f);
            }
        }


        /// <summary>
        /// Called to upload a content file object
        /// </summary>
        /// <param name="file"></param>
        public void Load(ContentsManager.ContentFileInfo file)
        {
            if (hasLoaded) return;

            //set vars
            m_fileInfo = file;
            m_url = file.url;

            //load url
            Load(m_fileInfo.url);
        }

        /// <summary>
        /// Called to open a url image
        /// </summary>
        /// <param name="url"></param>
        public void Load(string url)
        {
            if (hasLoaded) return;

            if (string.IsNullOrEmpty(url)) return;

            hasLoaded = true;
            gameObject.SetActive(true);
            loader.SetActive(true);

            if (!string.IsNullOrEmpty(Owner))
            {
                if (!Owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                {
                    //get current settings from owner
                    MMOManager.Instance.SendRPC("RequestConferenceImageSettings", (int)MMOManager.RpcTarget.All, PlayerManager.Instance.GetLocalPlayer().ID, Owner, ID);

                    //set all button hidden
                    Button[] all = GetComponentsInChildren<Button>();

                    for (int i = 0; i < all.Length; i++)
                    {
                        all[i].transform.localScale = Vector3.zero;
                    }
                }
            }

            //request
            StartCoroutine(WebRequest(url));

        }

        /// <summary>
        /// Called to unload anything loaded on this image screen
        /// </summary>
        public void Unload()
        {
            if (!hasLoaded) return;

            loader.SetActive(false);

            //reset output
            if (output != null)
            {
                output.texture = null;

                if(!m_isResource)
                {
                    Destroy(output.texture);
                }
                
                if (lerpAlpha) output.CrossFadeAlpha(0.0f, 0.0f, true);
                else output.transform.localScale = Vector3.zero;
            }

            hasLoaded = false;
            m_fileInfo = null;

            //lock vars
            if (deleteLock)
            {
                deleteLock.OnUnlock -= OnUnlocked;
                deleteLock.IgnoreRaycast = true;
                deleteLock.UnlockThis();
            }
        }

        /// <summary>
        /// Called to request image from url
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private IEnumerator WebRequest(string data)
        {
            if(data.Contains("http"))
            {
                m_isResource = false;
                UnityWebRequest request = UnityWebRequestTexture.GetTexture(data, true);

                yield return request.SendWebRequest();

                Debug.Log(request.result);

                if (request.result != UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(request.error))
                {
                    output.texture = DownloadHandlerTexture.GetContent(request);
                }

                //dispose the request as not needed anymore
                request.Dispose();
            }
            else
            {
                m_isResource = true;
                output.texture = Resources.Load<Texture>(data);
                output.SetNativeSize();
            }

            //set aspect ratio
            AspectRatioFitter ratio = null;

            if (GetComponentsInChildren<AspectRatioFitter>(true).Length > 0)
            {
                ratio = GetComponentsInChildren<AspectRatioFitter>(true)[0];
            }

            if (ratio != null)
            {
                float texWidth = output.texture.width;
                float texHeight = output.texture.height;
                float aspectRatio = texWidth / texHeight;
                ratio.aspectRatio = aspectRatio;
            }

            //visualise output
            if(lerpAlpha) output.CrossFadeAlpha(1.0f, 0.5f, true);
            else output.transform.localScale = Vector3.one;
            loader.SetActive(false);

            //lock vars
            if(deleteLock)
            {
                deleteLock.OnUnlock += OnUnlocked;
                deleteLock.LockThis();
                deleteLock.IgnoreRaycast = false;
            }
        }

        /// <summary>
        /// Called via the lock when trying to delete this content file
        /// </summary>
        private void OnUnlocked()
        {
            if (!hasLoaded) return;

            if (m_fileInfo != null)
            {
                //need to delete this from the database
                ContentsManager.Instance.WebClientDeleteContent(ID, m_fileInfo.url);
            }
            else
            {
                //need to delete this from the database
                ContentsManager.Instance.WebClientDeleteContent(ID, m_url);
            }

            Unload();
        }

        /// <summary>
        /// Called to initialise this video screen
        /// </summary>
        public void Initialise()
        {
            currentZoom = 1.0f;
            loader.SetActive(false);
            hasLoaded = false;

            //set scale
            if (content != null)
            {
                content.localScale = Vector3.one;
                content.localPosition = Vector3.zero;
                content.pivot = new Vector2(0.5f, 0.5f);
            }

            //set output display
            if (output != null)
            {
                if(lerpAlpha) output.CrossFadeAlpha(0.0f, 0.0f, true);
                else output.transform.localScale = Vector3.zero;
                output.texture = null;
            }

            //lock vars
            if(deleteLock != null)
            {
                deleteLock.IsNetworked = false;
                deleteLock.IgnoreRaycast = true;
            }

            //set content size
            Rect rect = content.parent.GetComponent<RectTransform>().rect;
            content.sizeDelta = new Vector2(rect.width, rect.height);

            UpdateTouchArea();
        }

        /// <summary>
        /// Called to update the viewport area for touch input
        /// </summary>
        public void UpdateTouchArea()
        {
            if (content != null)
            {
                RectTransform rect = content.parent.GetComponent<RectTransform>();
                touchArea = new Rect((0 + rect.position.x) - (rect.rect.width / 2), (0 + rect.position.y) - (rect.rect.height / 2), rect.rect.width, rect.rect.height);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
   
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            //send pan data to everyone
            Data = JsonUtility.ToJson(new PanJson(content.localPosition.x, content.localPosition.y, content.localPosition.z));

            if (IsNetworked)
            {
                if (LocalStateChange != null)
                {
                    LocalStateChange.Invoke("PAN", Data);
                }
            }
        }

        /// <summary>
        /// Called when the user drags on the viewport area
        /// </summary>
        /// <param name="eventData"></param>
        public void OnDrag(PointerEventData eventData)
        {
            if(!string.IsNullOrEmpty(Owner) && IsNetworked)
            {
                if (!Owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID)) return;
            }

            if (content != null)
            {
                if (isPinching || content.localScale.x > 1.01f)
                {
                    Pan(new Vector2(eventData.delta.x, eventData.delta.y));
                    return;
                }
            }
        }

        /// <summary>
        /// Called to pan the image within the viewport
        /// </summary>
        /// <param name="pan"></param>
        public void Pan(Vector2 pan)
        {
            content.localPosition += new Vector3(pan.x, pan.y);

            float x = Mathf.Clamp(content.localPosition.x, 0 - (content.rect.width * content.localScale.x / 2), 0 + (content.rect.width * content.localScale.y / 2));
            float y = Mathf.Clamp(content.localPosition.y, 0 - (content.rect.height * content.localScale.x / 2), 0 + (content.rect.height * content.localScale.y / 2));

            content.localPosition = new Vector3(x, y, 0.0f);
        }

        /// <summary>
        /// Called to calculate the pinch touches positions
        /// </summary>
        private void OnPinchStart()
        {
            //Vector2 pos1 = Input.touches[0].position;
            //Vector2 pos2 = Input.touches[1].position;

            //startPinchDist = Distance(pos1, pos2) * content.localScale.x;
            //startPinchZoom = currentZoom;
            //startPinchScreenPosition = (pos1 + pos2) / 2;
            //RectTransformUtility.ScreenPointToLocalPointInRectangle(content, startPinchScreenPosition, null, out startPinchCenterPosition);

            //Vector2 pivotPosition = new Vector3(content.pivot.x * content.rect.size.x, content.pivot.y * content.rect.size.y);
            //Vector2 posFromBottomLeft = pivotPosition + startPinchCenterPosition;

            //SetPivot(content, new Vector2(posFromBottomLeft.x / content.rect.width, posFromBottomLeft.y / content.rect.height));
        }

        /// <summary>
        /// Called when a user is pinching on the screen
        /// </summary>
        private void OnPinch()
        {
            //float currentPinchDist = Distance(Input.touches[0].position, Input.touches[1].position) * content.localScale.x;
            //currentZoom = (currentPinchDist / startPinchDist) * startPinchZoom;
            //currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

            //if (Mathf.Abs(content.localScale.x - currentZoom) > 0.0f)
            //    content.localScale = Vector3.Lerp(content.localScale, Vector3.one * currentZoom, zoomSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Called to calculate the distance between the touch points
        /// </summary>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <returns></returns>
        private float Distance(Vector2 pos1, Vector2 pos2)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, pos1, null, out pos1);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, pos2, null, out pos2);
            return Vector2.Distance(pos1, pos2);
        }

        /// <summary>
        /// Called to set the new pivot of the content rect based on touch positions and scale
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="pivot"></param>
        private void SetPivot(RectTransform rectTransform, Vector2 pivot)
        {
            if (rectTransform == null) return;

            Vector2 size = rectTransform.rect.size;
            Vector2 deltaPivot = rectTransform.pivot - pivot;
            Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y) * rectTransform.localScale.x;
            rectTransform.pivot = pivot;
            rectTransform.localPosition -= deltaPosition;
        }

        [System.Serializable]
        private class PanJson
        {
            public float zoom;
            public float x;
            public float y;
            public float z;

            public PanJson(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public Vector3 Get()
            {
                return new Vector3(x, y, z);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ContentImageScreen), true)]
        public class ContentImageScreen_Editor : UniqueID_Editor
        {
            private ContentImageScreen imageContentScript;
            private bool isWorldContent;
            private bool isConferenceScreen;

            private void OnEnable()
            {
                GetBanner();
                Initialise();

                isConferenceScreen = imageContentScript.gameObject.GetComponentInParent<ConferenceScreen>() != null;
                isWorldContent = imageContentScript.gameObject.GetComponentInParent<WorldContentUpload>() != null;

                if (serializedObject.FindProperty("deleteLock").objectReferenceValue == null &&
                    !isConferenceScreen && !isWorldContent)
                {
                    base.Initialise();
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                if(serializedObject.FindProperty("deleteLock").objectReferenceValue == null &&
                    !isConferenceScreen && !isWorldContent)
                {
                    DisplayID();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Image Content Setup", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                if(!isWorldContent)
                {
                    EditorGUILayout.LabelField("Zooming", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("minZoom"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxZoom"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("zoomSpeed"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fractionToZoomIn"), true);
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Display & Panning", EditorStyles.boldLabel);

                if (!isWorldContent)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lerpAlpha"), true);
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("content"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("output"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("loader"), true);

                if (!isConferenceScreen && !isWorldContent)
                {
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Display Controller", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("deleteLock"), true);
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(imageContentScript);
            }

            protected override void Initialise()
            {
                imageContentScript = (ContentImageScreen)target;
            }
        }
#endif
    }
}
