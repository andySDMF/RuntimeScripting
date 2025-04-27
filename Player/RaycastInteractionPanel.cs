using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class RaycastInteractionPanel : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI displayMessage;

        public GameObject InteractiveObject
        {

            get
            {
                return m_current;
            }
            set
            {
                if(value != null)
                {
                    if (m_current != null)
                    {
                        if (m_current != value && m_indicator != null)
                        {
                            m_indicator.GetComponentInChildren<AudioSource>().Play();
                        }
                    }
                }

                m_current = value;
            }
        }

        private GameObject m_indicator;
        private GameObject indicator;
        private GameObject m_current;

        private OrientationType m_switch = OrientationType.landscape;
        private float m_scaler;
        private float m_fontSize;

        private void Awake()
        {
            m_fontSize = displayMessage.fontSize;
            m_scaler = AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
        }

        private void OnEnable()
        {
            displayMessage.text = "Select object OR press " + PlayerManager.Instance.GetLocalPlayer().InteractionKey + " to interact withh object";
            indicator = AppManager.Instance.Settings.playerSettings.raycastInteractionIndicator;

            if(indicator == null)
            {
                indicator = Resources.Load<GameObject>("RaycastInteractionIndicator");
            }
        }

        private void OnDisable()
        {
            if(m_indicator != null)
            {
                Destroy(m_indicator);
            }

            InteractiveObject = null;
        }

        private void Update()
        {
            if(InteractiveObject != null)
            {
                if(m_indicator == null)
                {
                    //need to create indicator
                    m_indicator = Instantiate(indicator);
                    m_indicator.transform.position = InteractiveObject.transform.position;
                }

                float yPos = 0.0f;

                //ensure indicator sits over the item
                if (InteractiveObject.GetComponent<Collider>() != null)
                {
                    //need to get the collider bounds
                    yPos = InteractiveObject.GetComponent<Collider>().bounds.extents.y + 0.1f;
                }
                else
                {
                    yPos = 1.0f;
                }

                m_indicator.transform.position = new Vector3(InteractiveObject.transform.position.x, InteractiveObject.transform.position.y + yPos, InteractiveObject.transform.position.z);
           
            
                if(InputManager.Instance.GetKeyUp(PlayerManager.Instance.GetLocalPlayer().InteractionKey))
                {
                    //need to check what script the object has on it to determine action
                    if (InteractiveObject.transform.GetComponentInChildren<DropPoint>() != null)
                    {
                        ItemManager.Instance.PlaceCurrent(InteractiveObject.transform.GetComponentInChildren<DropPoint>());
                    }
                    else
                    {
                        //if above are all false
                        //for now we can just to the button action
                        Button[] buts = InteractiveObject.GetComponentsInChildren<Button>();

                        for (int i = 0; i < buts.Length; i++)
                        {
                            if (!buts[i].enabled || !buts[i].interactable)
                            {
                                continue;
                            }

                            buts[i].onClick.Invoke();
                        }
                    }
                }

                if (AppManager.Instance.Data.IsMobile && !m_switch.Equals(OrientationManager.Instance.CurrentOrientation))
                {
                    m_switch = OrientationManager.Instance.CurrentOrientation;

                    if (m_switch.Equals(OrientationType.landscape))
                    {
                        displayMessage.fontSize = m_fontSize;
                    }
                    else
                    {
                        displayMessage.fontSize = m_fontSize * m_scaler;
                    }
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(RaycastInteractionPanel), true)]
        public class RaycastInteractionPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("displayMessage"), true);

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
