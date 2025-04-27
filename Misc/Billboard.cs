using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360 
{
    public class Billboard : MonoBehaviour
    {
        public bool lockYAxis = true;
        public bool optimise = false;
        public int optimiseLevel = 10;

        private int optimiseCounter = 0;

        void Update()
        {
            if (CoreManager.Instance.CurrentState != state.Running) { return; }

            // If optimising then user a counter so we dont update every frame. Move this to product manager to handle it once for all products
            if (optimise)
            {
                optimiseCounter++;

                if (optimiseCounter < optimiseLevel)
                {
                    return;
                }
                else
                {
                    optimiseCounter = 0;
                }
            }

            // get look vector
            if(PlayerManager.Instance.GetLocalPlayer() != null && PlayerManager.Instance.GetLocalPlayer().MainCamera != null)
            {
                var dir = PlayerManager.Instance.GetLocalPlayer().MainCamera.transform.position - transform.position;

                // allow locking rotation to the Y axis so it rotates on the spot
                if (!lockYAxis)
                {
                    dir.y = 0;
                }

                //perform the rotation
                if (dir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(-dir.normalized);
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Billboard), true)]
        public class Billboard_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Billboard", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lockYAxis"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("optimise"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("optimiseLevel"), true);

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}
