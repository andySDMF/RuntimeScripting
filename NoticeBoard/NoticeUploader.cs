using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace BrandLab360
{
    public class NoticeUploader : MonoBehaviour
    {
        [Header("Layouts")]
        [SerializeField]
        private GameObject imageLayout;
        [SerializeField]
        private GameObject textLayout;
        [SerializeField]
        private ColorPicker colorPicker;

        [Header("Data")]
        [SerializeField]
        private TMP_Dropdown typeDropdown;
        [SerializeField]
        private RawImage output;
        [SerializeField]
        private TMP_InputField textScript;
        [SerializeField]
        private TMP_InputField website;
        [SerializeField]
        private TMP_Dropdown displayPeriod;
        [SerializeField]
        private GameObject[] colorPickerButtons;

        [Header("Processing")]
        [SerializeField]
        private GameObject progress;

        [Header("Mode")]
        [SerializeField]
        private GameObject addButton;
        [SerializeField]
        private GameObject editButton;
        [SerializeField]
        private GameObject editDisplayPeriodButton;

        private NoticeBoardAPI.NoticeJson m_temp;

        public string NoticeBoard
        {
            get;
            set;
        }

        public bool EnableDisplayPeriod
        {
            get;
            set;
        }

        public bool EditMode
        {
            get;
            set;
        }

        public NoticeBoardAPI.NoticeJson EditableJson
        {
            get { return m_temp; }
            set
            {
                m_temp = value;
            }
        }

        public ColorPicker.PickerDefaults PickerDefaults
        {
            get;
            set;
        }

        private void OnEnable()
        {
            PlayerManager.Instance.FreezePlayer(true);
            RaycastManager.Instance.CastRay = false;

            if(EditMode)
            {
                addButton.SetActive(false);
                editButton.SetActive(true);
                editDisplayPeriodButton.SetActive(EnableDisplayPeriod);

                if (m_temp.Type.Equals(BrandLab360.NoticeType.Image))
                {
                    typeDropdown.value = 0;
                    StartCoroutine(LoadImage(m_temp.content_url));
                }
                else
                {
                    typeDropdown.value = 1;
                    textScript.text = m_temp.content_text;
                }

                if(!m_temp.website_link.Equals("n/a"))
                {
                    website.text = m_temp.website_link;
                }

                int index = 0; 
                for(int i = 0; i < displayPeriod.options.Count; i++)
                {
                    if(displayPeriod.options[i].text.Equals(m_temp.display_period))
                    {
                        index = i;
                        break;
                    }
                }

                displayPeriod.interactable = false;
                displayPeriod.value = index;
                typeDropdown.interactable = false;

                ///need to get the color of the Json
                string[] split = EditableJson.bgk_color.Split('|');

                Color col;
                if(ColorUtility.TryParseHtmlString(split[0], out col))
                {
                    textScript.GetComponent<Image>().color = col;
                }
                else
                {
                    textScript.GetComponent<Image>().color = Color.white;
                }

                if (ColorUtility.TryParseHtmlString(split[1], out col))
                {
                    textScript.textComponent.color = col;
                }
                else
                {
                    textScript.textComponent.color = Color.black;
                }
            }
            else
            {
                m_temp = new NoticeBoardAPI.NoticeJson();
                typeDropdown.value = 0;
                addButton.SetActive(true);
                editButton.SetActive(false);
                editDisplayPeriodButton.SetActive(false);
                typeDropdown.interactable = true;
                displayPeriod.interactable = true;
            }

            for(int i = 0; i < colorPickerButtons.Length; i++)
            {
                colorPickerButtons[i].SetActive(!PickerDefaults.type.Equals(ColorPicker.PickerType.Disabled));
            }

            if(EnableDisplayPeriod)
            {
                displayPeriod.transform.localScale = Vector3.one;
            }
            else
            {
                displayPeriod.transform.localScale = Vector3.zero;
            }
        }

        public void OpenColorPicker(MaskableGraphic graphic)
        {
            if (PickerDefaults.type.Equals(ColorPicker.PickerType.Disabled)) return;

            colorPicker.SetGraphic(graphic, PickerDefaults);
            colorPicker.gameObject.SetActive(true);
        }

        public void SwitchType(int index)
        {
            if(index > 0)
            {
                imageLayout.SetActive(false);
                textLayout.SetActive(true);
            }
            else
            {
                imageLayout.SetActive(true);
                textLayout.SetActive(false);
            }
        }

        public void OnNoticeTextEdit(string val)
        {
            m_temp.content_text = textScript.text;
        }

        public void CreateNotice()
        {
            if (m_temp == null) return;

            string cType = typeDropdown.value > 0 ? m_temp.content_text : m_temp.content_url;

            if (!string.IsNullOrEmpty(cType))
            {
                progress.SetActive(true);

                m_temp.project = CoreManager.Instance.ProjectID;
                m_temp.noticeboard_id = NoticeBoard;

                m_temp.content_url = typeDropdown.value > 0 ? "n/a" : m_temp.content_url;
                m_temp.content_text = typeDropdown.value > 0 ? m_temp.content_text : "n/a";

                m_temp.website_link = string.IsNullOrEmpty(website.text) ? "n/a" : website.text;
                m_temp.display_period = EnableDisplayPeriod ? displayPeriod.options[displayPeriod.value].text : "Infinate";

                string imgHex = ColorUtility.ToHtmlStringRGB(textScript.GetComponent<Image>().color);
                string txtHex = ColorUtility.ToHtmlStringRGB(textScript.textComponent.color);

                m_temp.bgk_color = "#" + imgHex + "|#" + txtHex;

                NoticeBoardAPI.Instance.PostNotice(m_temp, PostCallback);
            }
        }

        public void EnableEditOnDisplayPeriod()
        {
            displayPeriod.interactable = true;
        }

        public void Edit()
        {
            if (m_temp == null) return;

            string cType = typeDropdown.value > 0 ? m_temp.content_text : m_temp.content_url;

            if (!string.IsNullOrEmpty(cType))
            {
                progress.SetActive(true);

                m_temp.project = CoreManager.Instance.ProjectID;
                m_temp.noticeboard_id = NoticeBoard;

                m_temp.content_url = typeDropdown.value > 0 ? "n/a" : m_temp.content_url;
                m_temp.content_text = typeDropdown.value > 0 ? m_temp.content_text : "n/a";

                m_temp.website_link = string.IsNullOrEmpty(website.text) ? "n/a" : website.text;
                m_temp.display_period = displayPeriod.options[displayPeriod.value].text;

                string imgHex = ColorUtility.ToHtmlStringRGB(textScript.GetComponent<Image>().color);
                string txtHex = ColorUtility.ToHtmlStringRGB(textScript.textComponent.color);

                m_temp.bgk_color = "#" + imgHex.Replace("\"", "") + "|#" + txtHex.Replace("\"", "");

                NoticeBoardAPI.Instance.EditNotice(m_temp, PostCallback);
            }
        }

        public void Close()
        {
            HUDManager.Instance.ToggleHUDScreen("NOTICE_SCREEN");

            PlayerManager.Instance.FreezePlayer(false);
            RaycastManager.Instance.CastRay = true;

            if(output.texture != null)
            {
                Destroy(output.texture);
            }

            output.texture = null;
            output.CrossFadeAlpha(0.0f, 0.0f, true);
            output.raycastTarget = false;
            website.text = "";
            displayPeriod.value = displayPeriod.options.Count - 1;

            NoticeBoard = "";
            EditMode = false;
            EditableJson = null;
            EnableDisplayPeriod = false;

            textScript.GetComponent<Image>().color = Color.white;
            textScript.textComponent.color = Color.black;
        }

        public void Upload()
        {
            string fileTypes = ".png,.jpg";
            var message = new ContentsManager.UploadMessage(true, fileTypes);
            var json = JsonUtility.ToJson(message);

            Debug.Log("WebClientUpload Notice: " + json);

            //add responce listener and send
            WebclientManager.WebClientListener += UploadResponse;
            WebclientManager.Instance.Send(json);
        }

        private void UploadResponse(string obj)
        {
            //ensure reponce data is tpye responce
            ContentsManager.UploadResponce responce = JsonUtility.FromJson<ContentsManager.UploadResponce>(obj).OrDefaultWhen(x => x.url == null);

            if (responce != null)
            {
                //remove listener
                WebclientManager.WebClientListener -= UploadResponse;

                m_temp.content_url = responce.url;

                //load preview of image
                StartCoroutine(LoadImage(m_temp.content_url));
            }
        }

        private void PostCallback(bool success)
        {
            progress.SetActive(false);

            if (success)
            {


                Close();
            }
        }

        private IEnumerator LoadImage(string url)
        {
            //webrequest
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, true);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(request.error))
            {
                output.texture = DownloadHandlerTexture.GetContent(request);
            }

            //dispose the request as not needed anymore
            request.Dispose();

            if (output.texture != null)
            {
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

                output.CrossFadeAlpha(1.0f, 0.0f, true);
                output.raycastTarget = true;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(NoticeUploader), true)]
        public class NoticeUploader_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("imageLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("textLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("colorPicker"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("typeDropdown"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("output"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("textScript"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("website"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("displayPeriod"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("colorPickerButtons"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("progress"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("addButton"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("editButton"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("editDisplayPeriodButton"), true);

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