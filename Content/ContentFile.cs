using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace BrandLab360
{
    /// <summary>
    /// Class to represent a single content file witin the _CONTENTS GO
    /// </summary>
    public class ContentFile : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI uploadedBy;
        [SerializeField]
        private TextMeshProUGUI filename;
        [SerializeField]
        private Image icon;
        [SerializeField]
        private bool isConferenceFile = false;

        private string m_file = "";
        private int m_type = 0;
        private ContentsManager.ContentFileInfo m_fileInfo;

        private bool m_loaded = false;


        private float m_uploadFontSize;
        private float m_filenameFontSize;
        private Vector2 m_iconSize;

        private OrientationType m_switch = OrientationType.landscape;
        private float m_scaler;

        /// <summary>
        /// Called to set the details of this file
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="sp"></param>
        public void Set(ContentsManager.ContentFileInfo fileInfo, Sprite sp)
        {
            m_file = fileInfo.url;
            m_type = fileInfo.extensiontype;

            m_fileInfo = fileInfo;

            uploadedBy.text = fileInfo.uploadedBy;
            filename.text = System.IO.Path.GetFileName(m_file);
            icon.sprite = sp;
            icon.SetNativeSize();

            m_iconSize = icon.GetComponent<RectTransform>().sizeDelta;
            m_uploadFontSize = uploadedBy.fontSize;
            m_filenameFontSize = filename.fontSize;

            m_scaler = AppManager.Instance.Settings.HUDSettings.mobileIconFontScaler;

            m_loaded = true;
        }

        private void Update()
        {
            if(AppManager.Instance.Data.IsMobile && m_loaded && !m_switch.Equals(OrientationManager.Instance.CurrentOrientation))
            {
                m_switch = OrientationManager.Instance.CurrentOrientation;

                if (m_switch.Equals(OrientationType.landscape))
                {
                    icon.SetNativeSize();
                    uploadedBy.fontSize = m_uploadFontSize;
                    filename.fontSize = m_filenameFontSize;
                }
                else
                {
                    icon.GetComponent<RectTransform>().sizeDelta = new Vector2(m_iconSize.x * m_scaler, m_iconSize.y * m_scaler);
                    uploadedBy.fontSize = m_uploadFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    filename.fontSize = m_filenameFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                }
            }
        }

        /// <summary>
        /// Called to open this file on the main 2D UI content screen
        /// </summary>
        public void Open()
        {
            if(ContentsManager.Instance.IsFileConferenceUpload(m_fileInfo.id) || isConferenceFile)
            {
                ConferenceContentUpload[] uploaders = FindObjectsByType<ConferenceContentUpload>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                for (int i = 0; i < uploaders.Length; i++)
                {
                    if(uploaders[i].Type.Equals(m_fileInfo.extensiontype))
                    {
                        Debug.Log("tpye equals extension");
                        uploaders[i].UploadCallback(uploaders[i].ID, m_fileInfo);
                        break;
                    }
                }
            }
            else
            {
                ContentsPanel.Instance.OpenFile(m_file);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ContentFile), true)]
        public class ContentFile_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("uploadedBy"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("filename"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("isConferenceFile"), true);

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
