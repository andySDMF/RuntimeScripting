using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ConfigureToggle : MonoBehaviour
    {
        [SerializeField]
        private Image img;

        private Configurator m_config;

        [SerializeField]
        private int m_index = 0;


        private void Awake()
        {
            GetComponent<Toggle>().onValueChanged.AddListener(OnValueChanged);

            m_config = GetComponentInParent<Configurator>();

            if (!GetComponent<Toggle>().interactable)
            {
                img.CrossFadeAlpha(0.3f, 0.0f, true);
            }
        }

        private void Start()
        {
            if (GetComponent<Toggle>().isOn)
            {
                GetComponent<Toggle>().isOn = false;
            }
        }

        public void Set(int index, Sprite sp)
        {
            m_index = index;
            img.sprite = sp;
            //img.SetNativeSize();

            if (!GetComponent<Toggle>().interactable)
            {
                img.CrossFadeAlpha(0.3f, 0.0f, true);
            }
            else
            {
                img.CrossFadeAlpha(1.0f, 0.0f, true);
            }
        }

        public void OnValueChanged(bool val)
        {
            if (CoreManager.Instance.projectSettings.configTagMode.Equals(TagMode._3D) && !m_config.CanUse || m_config.IsActive)
            {
                //need to reset the toggle back to the oposite of the val
                GetComponent<Toggle>().onValueChanged.RemoveListener(OnValueChanged);
                GetComponent<Toggle>().isOn = !val;
                GetComponent<Toggle>().onValueChanged.AddListener(OnValueChanged);
                return;
            }

            RaycastManager.Instance.UIRaycastSelectablePressed(GetComponent<Selectable>());

            //send Room room change
            if (m_config != null)
            {
                if (val)
                {
                    switch (m_config.Type)
                    {
                        case ConfiguratorManager.ConfiguratorType.Color:
                            break;
                        case ConfiguratorManager.ConfiguratorType.Material:
                            break;
                        case ConfiguratorManager.ConfiguratorType.Model:
                            break;
                        default:
                            string data = m_index.ToString() + "|" + (val ? "1" : "0");
                            m_config.Set(data);
                            break;
                    }
                }
                else
                {
                    StartCoroutine(WaitFrame());
                }

            }
        }

        private IEnumerator WaitFrame()
        {
            yield return new WaitForEndOfFrame();

            Toggle tog = GetComponent<Toggle>();

            if (tog.group != null)
            {
                if (!tog.group.AnyTogglesOn())
                {
                    //ensure RTE selected is off
                    ConfiguratorManager.instance.SetRTEObject(null);
                    ConfiguratorManager.instance.ChangeRTETool(0);
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ConfigureToggle), true)]
        public class ConfigureToggle_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("img"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_index"), true);

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
