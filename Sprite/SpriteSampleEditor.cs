using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UI;
#endif

namespace BrandLab360
{
    public class SpriteSampleEditor : MonoBehaviour
    {
        [SerializeField]
        private string resourcePath = "";

        private void Awake()
        {
            if(string.IsNullOrEmpty(resourcePath))
            {

            }

            Destroy(this);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SpriteSampleEditor), true)]
        public class SpriteSampleEditor_Editor : BaseInspectorEditor
        {
            private SpriteSampleEditor script;

            private void OnEnable()
            {
                GetBanner();

                script = (SpriteSampleEditor)target;
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                if(Application.isPlaying == false && Application.productName.Equals("BL360 Plugin"))
                {
                    serializedObject.Update();

                    if (GUILayout.Button("Apply Scripts"))
                    {
                        Image[] all = script.GetComponentsInChildren<Image>(true);

                        for (int i = 0; i < all.Length; i++)
                        {
                            SpriteSample sample = all[i].GetComponentInChildren<SpriteSample>(true);

                            if (sample == null)
                            {
                                sample = all[i].gameObject.AddComponent<SpriteSample>();
                            }

                            sample.AttainSampleSprite();
                        }
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Resource");

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("resourcePath"), true);


                    if (GUILayout.Button("Add Resource Path"))
                    {
                        Image[] all = script.GetComponentsInChildren<Image>(true);

                        for (int i = 0; i < all.Length; i++)
                        {
                            SpriteSample sample = all[i].GetComponentInChildren<SpriteSample>(true);

                            if (sample != null)
                            {
                                sample.AddRouteResource(script.resourcePath);
                            }
                        }
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Display");

                    if (GUILayout.Button("Visualize"))
                    {
                        Image[] all = script.GetComponentsInChildren<Image>(true);

                        for (int i = 0; i < all.Length; i++)
                        {
                            SpriteSample sample = all[i].GetComponentInChildren<SpriteSample>(true);

                            if (sample != null)
                            {
                                sample.Visualzie();
                            }
                        }
                    }


                    if (GUILayout.Button("Clear"))
                    {
                        Image[] all = script.GetComponentsInChildren<Image>(true);

                        for (int i = 0; i < all.Length; i++)
                        {
                            SpriteSample sample = all[i].GetComponentInChildren<SpriteSample>(true);

                            if (sample != null)
                            {
                                sample.Clear();
                            }
                        }
                    }

                    if (GUI.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(script);
                    }
                }
            }
        }
#endif
    }
}
