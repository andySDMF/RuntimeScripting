using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class MainSpawnPoint : MonoBehaviour
    {
        [SerializeField]
        private GameObject cameraObject;

        public Vector3 Forward
        {
            get
            {
                return transform.forward;
            }
        }

        public Vector3 Position
        {
            get
            {
                return transform.position;
            }
        }

        public Vector3 Euler
        {
            get
            {
                return transform.eulerAngles;
            }
        }

        public Vector3 LocalEuler
        {
            get
            {
                return transform.localEulerAngles;
            }
        }

        public Quaternion Rotation
        {
            get
            {
                return transform.rotation;
            }
        }

        public Collider Col
        {
            get
            {
                return GetComponent<Collider>();
            }
        }

        public GameObject Cam
        {
            get
            {
                return cameraObject;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(MainSpawnPoint), true)]
        public class MainSpawnPoint_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraObject"), true);

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
