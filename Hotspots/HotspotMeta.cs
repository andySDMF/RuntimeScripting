using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    /// <summary>
    /// HotspotMeta stores the metadata for the hotspot
    /// </summary>
    public class HotspotMeta : MonoBehaviour
    {
        public string sceneName = "";
        public Texture2D HotspotImage;
        public Vector3 Position;
        public Vector2 Rotation;

#if UNITY_EDITOR
        [CustomEditor(typeof(HotspotMeta), true)]
        public class HotspotPanel_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("sceneName"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("HotspotImage"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Position"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Rotation"), true);

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }

    public class Hotspot
    {
        public string sceneName;
        public string hotspotImage;
        public float x;
        public float y;
        public float z;
        public float rot;
        public float cameraRot;
    }
}