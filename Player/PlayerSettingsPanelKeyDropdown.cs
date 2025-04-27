using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class PlayerSettingsPanelKeyDropdown : MonoBehaviour
    {
        [SerializeField]
        private InputKeyCode defaultKey = InputKeyCode.W;

        [SerializeField]
        private SettingName setting = SettingName._Forward;

        public SettingName ThisSettings { get { return setting; } }

        public int Key
        {
            get
            {
                string[] enums = System.Enum.GetNames(typeof(InputKeyCode));
                int index = 0;

                for (int i = 0; i < enums.Length; i++)
                {
                    if (enums[i].Equals(m_dropDown.captionText.text))
                    {
                        index = i;
                        break;
                    }
                }

                return index;
            }
        }

        private TMP_Dropdown m_dropDown;

        private void Awake()
        {
            if(AppManager.IsCreated)
            {
                m_dropDown = GetComponent<TMP_Dropdown>();
                m_dropDown.options = new List<TMP_Dropdown.OptionData>();

                //need to construct all the values from Keycode
                string[] enums = System.Enum.GetNames(typeof(InputKeyCode));
                int index = 0;

                for (int i = 0; i < enums.Length; i++)
                {
                    TMP_Dropdown.OptionData data = new TMP_Dropdown.OptionData();
                    data.text = enums[i];
                    m_dropDown.options.Add(data);

                    if(enums[i].Equals(defaultKey.ToString()))
                    {
                        index = i;
                    }
                }

                m_dropDown.value = index;
            }
        }

        private void OnEnable()
        {
            if (!AppManager.IsCreated) return;

            TMP_Dropdown.OptionData option = null;

            //need to apply the player controls setting to it
            switch (setting)
            {
                case SettingName._Forward:
                    //might need to find value based on index

                    option = m_dropDown.options.FirstOrDefault(x => x.text.Equals(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[0]).ToString()));

                    if(option != null)
                    {
                        m_dropDown.value = m_dropDown.options.IndexOf(option);
                    }
                    break;
                case SettingName._Back:
                    option = m_dropDown.options.FirstOrDefault(x => x.text.Equals(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[1]).ToString()));

                    if (option != null)
                    {
                        m_dropDown.value = m_dropDown.options.IndexOf(option);
                    }
                    break;
                case SettingName._Left:
                    option = m_dropDown.options.FirstOrDefault(x => x.text.Equals(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[2]).ToString()));
                    if (option != null)
                    {
                        m_dropDown.value = m_dropDown.options.IndexOf(option);
                    }
                    break;
                case SettingName._Right:
                    option = m_dropDown.options.FirstOrDefault(x => x.text.Equals(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[3]).ToString()));
                    if (option != null)
                    {
                        m_dropDown.value = m_dropDown.options.IndexOf(option);
                    }
                    break;
                case SettingName._Sprint:
                    option = m_dropDown.options.FirstOrDefault(x => x.text.Equals(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[4]).ToString()));
                    if (option != null)
                    {
                        m_dropDown.value = m_dropDown.options.IndexOf(option);
                    }
                    break;
                case SettingName._StrifeLeft:
                    option = m_dropDown.options.FirstOrDefault(x => x.text.Equals(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[5]).ToString()));
                    if (option != null)
                    {
                        m_dropDown.value = m_dropDown.options.IndexOf(option);
                    }
                    break;
                case SettingName._StrifeRight:
                    option = m_dropDown.options.FirstOrDefault(x => x.text.Equals(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[6]).ToString()));
                    if (option != null)
                    {
                        m_dropDown.value = m_dropDown.options.IndexOf(option);
                    }
                    break;
                case SettingName._Focus:
                    option = m_dropDown.options.FirstOrDefault(x => x.text.Equals(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[7]).ToString()));
                    if (option != null)
                    {
                        m_dropDown.value = m_dropDown.options.IndexOf(option);
                    }
                    break;
                case SettingName._Interact:
                    option = m_dropDown.options.FirstOrDefault(x => x.text.Equals(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[8]).ToString()));
                    if (option != null)
                    {
                        m_dropDown.value = m_dropDown.options.IndexOf(option);
                    }
                    break;
            }
        }

        [System.Serializable]
        public enum SettingName {_Forward, _Back, _Left, _Right, _Sprint, _StrifeLeft, _StrifeRight, _Focus, _Interact}

#if UNITY_EDITOR
        [CustomEditor(typeof(PlayerSettingsPanelKeyDropdown), true)]
        public class PlayerSettingsPanelKeyDropdown_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultKey"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("setting"), true);

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
