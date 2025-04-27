using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class PhotonChatPlayerEntry : MonoBehaviour
    {
        [SerializeField]
        private TMPro.TextMeshProUGUI textDisplay;

        [SerializeField]
        private Image status;

        [SerializeField]
        private GameObject[] disturbIndicators;

        private IPlayer m_player;
        private bool m_donotdistrubMode = false;

        private void Start()
        {
            GetComponentInChildren<PhotonChatPlayerNotification>(true).ChatID = textDisplay.text;

            //status should disappear is chat is global
            if (textDisplay.text.Equals("All"))
            {
                status.gameObject.SetActive(false);
            }

            if(!textDisplay.text.Equals("All"))
            {
                m_player = MMOManager.Instance.GetPlayerByUserID(MMOChat.Instance.GetPlayerIDFromChat(textDisplay.text));
            }

            for (int i = 0; i < disturbIndicators.Length; i++)
            {
                disturbIndicators[i].GetComponentInChildren<Image>(true).color = CoreManager.Instance.chatSettings.busy;
                disturbIndicators[i].SetActive(false);
            }
        }

        private void Update()
        {
            //handles status
            if (!textDisplay.text.Equals("All") && !string.IsNullOrEmpty(textDisplay.text) && status != null)
            {
                status.color = PlayerManager.Instance.GetPlayerStatus(MMOChat.Instance.GetPlayerIDFromChat(textDisplay.text));
            }

            if(m_player != null)
            {
                if (MMOManager.Instance.PlayerHasProperty(m_player, "DONOTDISTURB"))
                {
                    m_donotdistrubMode = (MMOManager.Instance.GetPlayerProperty(m_player, "DONOTDISTURB").Equals("1") ? true : false);

                    for (int i = 0; i < disturbIndicators.Length; i++)
                    {
                        disturbIndicators[i].SetActive(m_donotdistrubMode);
                    }

                    GetComponent<Toggle>().interactable = !m_donotdistrubMode;
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PhotonChatPlayerEntry), true)]
        public class PhotonChatPlayerEntry_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("textDisplay"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("status"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("disturbIndicators"), true);

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
