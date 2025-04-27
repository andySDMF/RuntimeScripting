using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class CameraMenuHandler : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField]
        private Image display;
        [SerializeField]
        private Image displayDual;
        [SerializeField]
        private GameObject orbitButton;

        [Header("Sprites")]
        [SerializeField]
        private Sprite firstPerson;
        [SerializeField]
        private Sprite thirdPerson;
        [SerializeField]
        private Sprite orbit;

        private void Awake()
        {
            if(AppManager .IsCreated)
            {
                orbitButton.SetActive(AppManager.Instance.Settings.projectSettings.enableOrbitCamera);
            }
        }

        public void SelectTP()
        {
            display.sprite = thirdPerson;
            displayDual.sprite = display.sprite;
            GetComponent<Toggle>().isOn = false;
            PlayerManager.Instance.SwitchPerspectiveCameraMode(PerspectiveCameraMode.ThirdPerson);
        }

        public void SelectFP()
        {
            display.sprite = firstPerson;
            displayDual.sprite = display.sprite;
            GetComponent<Toggle>().isOn = false;
            PlayerManager.Instance.SwitchPerspectiveCameraMode(PerspectiveCameraMode.FirstPerson);
        }

        public void SelectOrbit()
        {
            display.sprite = orbit;
            displayDual.sprite = display.sprite;
            GetComponent<Toggle>().isOn = false;
            PlayerManager.Instance.SwitchPerspectiveCameraMode(PerspectiveCameraMode.CameraOrbit);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(CameraMenuHandler), true)]
        public class CameraMenuHandler_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("display"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("displayDual"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("orbitButton"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("firstPerson"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("thirdPerson"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("orbit"), true);

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
