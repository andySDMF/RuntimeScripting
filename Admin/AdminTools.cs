using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class AdminTools : MonoBehaviour
    {
        [Header("Tab")]
        [SerializeField]
        private GameObject tabs;

        [Header("Flythrough")]
        [SerializeField]
        private float flythroughSpeed = 5.0f;
        [SerializeField]
        private float flythroughSensitivity = 10.0f;
        [SerializeField]
        private bool inverted = true;
        [SerializeField]
        private bool useSmoothing = true;
        [SerializeField]
        private float acceleration = 0.1f;

        [Header("Compenents")]
        [SerializeField]
        private Slider speedSlider;
        [SerializeField]
        private Slider sensitivitySlider;
        [SerializeField]
        private Slider accelerationSlider;
        [SerializeField]
        private Toggle invertedToggle;
        [SerializeField]
        private Toggle smoothingToggle;

        [Header("Video")]
        [SerializeField]
        private TMP_InputField videoChannel;

        [Header("WebClient")]
        [SerializeField]
        private TMP_InputField webClientJson;
        [SerializeField]
        private TextMeshProUGUI webClientResponce;

        private string m_videoChannel = "";

        private GameObject m_flythroughCamera;
        private Vector3 m_flythroughPosition = new Vector3(0, 10, -10);
        private Vector3 m_flythroughRotation = new Vector3(45, 0, 0);

        private void Awake()
        {
            //set filter toggles
            tabs.GetComponentsInChildren<UnityEngine.UI.Toggle>(true).ToList()[0].isOn = true;
        }

        private void OnEnable()
        {
            WebclientManager.WebClientListener += WebClientResponce;

            speedSlider.value = flythroughSpeed / 10;
            sensitivitySlider.value = flythroughSensitivity / 10;
            accelerationSlider.value = acceleration / 10;
            smoothingToggle.isOn = useSmoothing;
            invertedToggle.isOn = inverted;
        }

        private void OnDisable()
        {
            WebclientManager.WebClientListener -= WebClientResponce;
        }

        private void ToggleUIVisibility(bool visible)
        {
            HUDManager.Instance.ShowHUDNavigationVisibility(visible);
            MMORoom.Instance.ToggleLocalProfileInteraction(visible);
            MMORoom.Instance.ToggleLocalProfileVisibility(visible);
        }

        public void JoinVideoChannel()
        {
            if (string.IsNullOrEmpty(videoChannel.text)) return;

            m_videoChannel = videoChannel.text;

            if (AppManager.Instance.Settings.projectSettings.useWebClientRoomVariable)
            {
                var msg = new ToggleVideoChatMessage();
                msg.Username = "Admin";
                msg.Channel = AppManager.Instance.Data.ProjectID + "_" + AppManager.Instance.Data.RoomID.ToString() + "_" + m_videoChannel;
                msg.ToggleVideoChat = true;
                var json = JsonUtility.ToJson(msg);
                WebclientManager.Instance.Send(json);
            }
        }

        public void LeaveVideoChannel()
        {
            if (string.IsNullOrEmpty(m_videoChannel)) return;

            if (AppManager.Instance.Settings.projectSettings.useWebClientRoomVariable)
            {
                var msg = new ToggleVideoChatMessage();
                msg.Username = "Admin";
                msg.Channel = AppManager.Instance.Data.ProjectID + "_" + AppManager.Instance.Data.RoomID.ToString() + "_" + m_videoChannel;
                msg.ToggleVideoChat = false;
                var json = JsonUtility.ToJson(msg);
                WebclientManager.Instance.Send(json);
            }

            m_videoChannel = "";
            videoChannel.text = "";
        }

        public void SendWebClientJson()
        {
            if (string.IsNullOrEmpty(webClientJson.text)) return;

            WebclientManager.Instance.Send(webClientJson.text);
        }

        public void ClearWebRequestResponce()
        {
            webClientResponce.text = "";
        }

        private void WebClientResponce(string responce)
        {
            webClientResponce.text = responce;
        }

        public void ApplySettings()
        {
            flythroughSensitivity = sensitivitySlider.value * 10;
            flythroughSpeed = speedSlider.value * 10;
            acceleration = accelerationSlider.value * 10;
            inverted = invertedToggle.isOn;
            useSmoothing = smoothingToggle.isOn;

            if(m_flythroughCamera != null)
            {
                m_flythroughCamera.GetComponent<FlythroughControl>().SetControls(inverted, flythroughSpeed, flythroughSensitivity, useSmoothing, acceleration);
            }
        }

        public void SpawnFlythrough()
        {
            if (m_flythroughCamera != null) return;

            if(AppManager.Instance.Data.RoomEstablished)
            {
                if(!MMOManager.Instance.IsPlayerBusy(PlayerManager.Instance.GetLocalPlayer().ID))
                {
                    PlayerManager.Instance.FreezePlayer(true);
                    PlayerManager.Instance.GetLocalPlayer().MainCamera.gameObject.SetActive(false);
                    ToggleUIVisibility(false);
                    AdminManager.Instance.ToggleFlythroughOverlay(true);

                    m_flythroughCamera = Instantiate(Resources.Load<GameObject>("FlythroughCamera"), m_flythroughPosition, Quaternion.identity) as GameObject;
                    m_flythroughCamera.transform.eulerAngles = m_flythroughRotation;
                    m_flythroughCamera.GetComponent<FlythroughControl>().SetControls(inverted, flythroughSpeed, flythroughSensitivity, useSmoothing, acceleration);

                }
            }
        }

        public void ExitFlythrough()
        {
            if (m_flythroughCamera == null) return;

            if (AppManager.Instance.Data.RoomEstablished)
            {
                if (!MMOManager.Instance.IsPlayerBusy(PlayerManager.Instance.GetLocalPlayer().ID))
                {
                    AdminManager.Instance.ToggleFlythroughOverlay(false);
                    AdminManager.Instance.ToggleAdminPanel(true);

                    m_flythroughPosition = m_flythroughCamera.transform.position;
                    m_flythroughRotation = m_flythroughCamera.transform.eulerAngles;
                    Destroy(m_flythroughCamera);

                    PlayerManager.Instance.GetLocalPlayer().MainCamera.gameObject.SetActive(true);
                    PlayerManager.Instance.FreezePlayer(false);
                    ToggleUIVisibility(true);
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AdminTools), true)]
        public class AdminTools_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tabs"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("flythroughSpeed"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("flythroughSensitivity"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("inverted"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("useSmoothing"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("acceleration"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("speedSlider"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sensitivitySlider"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("accelerationSlider"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("invertedToggle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("smoothingToggle"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("videoChannel"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("webClientJson"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("webClientResponce"), true);


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
