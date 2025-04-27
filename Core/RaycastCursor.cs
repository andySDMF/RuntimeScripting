using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class RaycastCursor : MonoBehaviour
    {
        [SerializeField]
        private GameObject diamond;

        [SerializeField]
        private GameObject ring;

        [SerializeField]
        private GameObject hoverCursorWalk;

        private void Awake()
        {
            switch(CoreManager.Instance.projectSettings.cursorType)
            {
                case CursorType.Diamond:
                    diamond.SetActive(true);
                    ring.SetActive(false);
                    hoverCursorWalk.SetActive(false);

                    diamond.GetComponent<Renderer>().material.color = AppManager.Instance.Settings.playerSettings.cursorColor;
                    break;
                case CursorType.Ring:
                    diamond.SetActive(false);
                    ring.SetActive(true);
                    hoverCursorWalk.SetActive(false);

                    ring.GetComponent<Renderer>().material.color = AppManager.Instance.Settings.playerSettings.cursorColor;
                    break;
                case CursorType.CursorWalk:
                    diamond.SetActive(false);
                    ring.SetActive(false);
                    hoverCursorWalk.SetActive(true);

                    hoverCursorWalk.GetComponent<Renderer>().material.color = AppManager.Instance.Settings.playerSettings.cursorColor;
                    break;
                default:
                    break;
            }
        }

        [System.Serializable]
        public enum CursorType { Diamond, Ring, CursorWalk }

#if UNITY_EDITOR
        [CustomEditor(typeof(RaycastCursor), true)]
        public class RaycastCursor_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("diamond"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ring"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("hoverCursorWalk"), true);

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
