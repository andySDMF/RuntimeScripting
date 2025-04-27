using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ConferenceContentUploadList : MonoBehaviour
    {
        [SerializeField]
        private Transform container;

        [SerializeField]
        private GameObject prefab;

        public string ID
        {
            get;
            set;   
        }

        private List<GameObject> m_files = new List<GameObject>();
        private List<ContentsManager.ContentFileInfo> m_filteredFiles = new List<ContentsManager.ContentFileInfo>();

        private void OnEnable()
        {
            ConferenceChairGroup conference = (ConferenceChairGroup)ChairManager.Instance.GetChairGroupFromPlayer(PlayerManager.Instance.GetLocalPlayer());

            if(conference != null)
            {
                m_filteredFiles.Clear();
               
                for(int i = 0; i < conference.ContentUploadURLs.Count; i++)
                {
                    ContentsManager.ContentFileInfo file = ContentsManager.Instance.GetContentFromURL(conference.ContentUploadURLs[i]);

                    if(file != null)
                    {
                        m_filteredFiles.Add(file);
                    }
                }

                for (int i = 0; i < m_filteredFiles.Count; i++)
                {
                    m_files.Add(CreateContentFile(m_filteredFiles[i]));
                }
            }
        }

        private void OnDisable()
        {
            for(int i = 0; i < m_files.Count; i++)
            {
                Destroy(m_files[i]);
            }

            m_files.Clear();
            m_filteredFiles.Clear();
        }

        /// <summary>
        /// Action called to instantiate the message GO
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private GameObject CreateContentFile(ContentsManager.ContentFileInfo fileInfo)
        {
            GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity, container);
            go.transform.localScale = Vector3.one;
            go.SetActive(true);
            go.name = "ContentFileInfo_" + m_filteredFiles.IndexOf(fileInfo).ToString();

            ContentsManager.ContentType contentEnum = (ContentsManager.ContentType)fileInfo.extensiontype + 1;
            go.GetComponentInChildren<ContentFile>(true).Set(fileInfo, ContentsManager.Instance.GetLogTypeIcon(contentEnum));

            return go;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ConferenceContentUploadList), true)]
        public class ConferenceContentUploadList_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("container"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("prefab"), true);

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
