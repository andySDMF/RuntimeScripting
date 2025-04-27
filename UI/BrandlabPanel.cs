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
    public class BrandlabPanel : MonoBehaviour
    {
        [Header("Objects")]
        [SerializeField]
        private GameObject registerTab;

        [SerializeField]
        private GameObject registerContent;

        [SerializeField]
        private GameObject registerButtons;

        [SerializeField]
        private GameObject informationTab;

        [SerializeField]
        private GameObject informationContent;


        [Header("Support")]
        [SerializeField]
        private TextMeshProUGUI textSupport;

        [Header("TAC")]
        [SerializeField]
        private TextMeshProUGUI textTAC;

        [Header("Policy")]
        [SerializeField]
        private TextMeshProUGUI textPolicy;

        private void Awake()
        {
            if (!CoreManager.Instance.HUDSettings.showBrandlabRegisterInterest)
            {
                registerTab.SetActive(false);
                registerContent.SetActive(false);
                registerButtons.SetActive(false);
            }

            if (!CoreManager.Instance.HUDSettings.showBrandlabInfo)
            {
                informationTab.SetActive(false);
                informationContent.SetActive(false);
            }

            textTAC.text = AppManager.Instance.Settings.projectSettings.clientTAC;
            textPolicy.text = AppManager.Instance.Settings.projectSettings.clientPolicy;
            textSupport.text = AppManager.Instance.Settings.projectSettings.brandlabSuppotEmail;

            StartCoroutine(WaitFrame());
        }

        private IEnumerator WaitFrame()
        {
            yield return new WaitForEndOfFrame();

            Toggle[] togs = GetComponentsInChildren<Toggle>();

            for (int i = 0; i < togs.Length; i++)
            {
                if (togs[i].name.Contains("Register") && CoreManager.Instance.HUDSettings.showBrandlabRegisterInterest)
                {
                    togs[i].isOn = true;
                    break;
                }
                else if (togs[i].name.Contains("Information") && CoreManager.Instance.HUDSettings.showBrandlabInfo)
                {
                    togs[i].isOn = true;
                    break;
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(BrandlabPanel), true)]
        public class BrandlabPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("registerTab"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("registerContent"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("registerButtons"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("informationTab"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("informationContent"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("textSupport"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("textTAC"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("textPolicy"), true);

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