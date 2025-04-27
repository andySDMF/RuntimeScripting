using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Highlight : MonoBehaviour
    {
        [SerializeField]
        private string[] alternativeColorProperty;

        [SerializeField]
        private bool overrideSettingsHighlightOption = false;

        [SerializeField]
        private HighlightType highlightOption = HighlightType.Color;

        public bool isEnabled = true;

        private bool m_isHighlighted = false;
        private ObjectMaterial[] m_mats;

        private void GetMats()
        {
            Renderer[] rends = GetComponentsInChildren<Renderer>(true);
            m_mats = new ObjectMaterial[rends.Length];

            //get all materials on each child object
            for (int i = 0; i < rends.Length; i++)
            {
                m_mats[i] = new ObjectMaterial();
                m_mats[i].rend = rends[i];
                m_mats[i].normals = new MaterialRef[rends[i].materials.Length];

                for (int j = 0; j < rends[i].materials.Length; j++)
                {
                    bool useColor = overrideSettingsHighlightOption ? highlightOption.Equals(HighlightType.Color) :  CoreManager.Instance.projectSettings.highlightType.Equals(HighlightType.Color);

                    if (!rends[i].materials[j].shader.name.Contains("HDRP"))
                    {
                        useColor = true;
                    }

                    if (useColor)
                    {
                        if (rends[i].materials[j].HasProperty("_BaseColor"))
                        {
                            m_mats[i].normals[j] = new MaterialRef(new Color(rends[i].materials[j].color.r, rends[i].materials[j].color.g, rends[i].materials[j].color.b, rends[i].materials[j].color.a), rends[i].materials[j].shader.name);
                        }
                        else
                        {
                            bool found = false;

                            for(int k = 0; k < alternativeColorProperty.Length; k++)
                            {
                                if (rends[i].materials[j].HasProperty(alternativeColorProperty[k]))
                                {
                                    found = true;
                                    m_mats[i].normals[j] = new MaterialRef(new Color(rends[i].materials[j].GetColor(alternativeColorProperty[k]).r, rends[i].materials[j].GetColor(alternativeColorProperty[k]).g, rends[i].materials[j].GetColor(alternativeColorProperty[k]).b, rends[i].materials[j].GetColor(alternativeColorProperty[k]).a), rends[i].materials[j].shader.name);
                                    break;
                                }
                            }

                            if (!found)
                            { 
                                m_mats[i].normals[j] = new MaterialRef(new Color(rends[i].materials[j].color.r, rends[i].materials[j].color.g, rends[i].materials[j].color.b, rends[i].materials[j].color.a), rends[i].materials[j].shader.name);
                            }
                        }
                    }
                    else
                    {
                        Material mat = new Material(Shader.Find("HDRP/Lit"));
                        mat.CopyPropertiesFromMaterial(rends[i].materials[j]);

                        m_mats[i].normals[j] = new MaterialRef(mat, rends[i].materials[j].shader.name);
                    }
                }
            }
        }

        public void HighlightObject(bool highlight)
        {
            bool useHighlight = CoreManager.Instance.projectSettings.highlightType.Equals(HighlightType.Disabled) ? false : PlayerManager.Instance.MainControlSettings.highlightOn.Equals(1);

            if (!isEnabled || !useHighlight) return;

            if (m_isHighlighted.Equals(highlight)) return;

            m_isHighlighted = highlight;

            if (m_isHighlighted)
            {
                GetMats();
            }

            foreach (ObjectMaterial cm in m_mats)
            {
                for (int i = 0; i < cm.normals.Length; i++)
                {
                    bool useColor = overrideSettingsHighlightOption ? highlightOption.Equals(HighlightType.Color) : CoreManager.Instance.projectSettings.highlightType.Equals(HighlightType.Color);

                    if (!cm.normals[i].shader.Contains("HDRP"))
                    {
                        useColor = true;
                    }

                    if (useColor)
                    {
                        cm.rend.materials[i].shader = (m_isHighlighted) ? Shader.Find("HDRP/Lit") : Shader.Find(cm.normals[i].shader);

                        if (cm.rend.materials[i].HasProperty("_BaseColor"))
                        {
                            cm.rend.materials[i].SetColor("_BaseColor", (m_isHighlighted) ? CoreManager.Instance.projectSettings.highlightColor : cm.normals[i].col);
                        }
                        else
                        {
                            for (int k = 0; k < alternativeColorProperty.Length; k++)
                            {
                                if (cm.rend.materials[i].HasProperty(alternativeColorProperty[k]))
                                {
                                    cm.rend.materials[i].SetColor(alternativeColorProperty[k], (m_isHighlighted) ? CoreManager.Instance.projectSettings.highlightColor : cm.normals[i].col);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (m_isHighlighted)
                        {
                            cm.rend.materials[i].Lerp(cm.rend.materials[i], RaycastManager.Instance.HighlightMaterial, 1.0f);
                        }
                        else
                        {
                            cm.rend.materials[i].Lerp(cm.rend.materials[i], cm.normals[i].mat, 1.0f);
                        }
                    }
                }
            }

            if (!m_isHighlighted)
            {
                m_mats = new ObjectMaterial[0];
            }
        }

        [System.Serializable]
        private class ObjectMaterial
        {
            public Renderer rend;
            public MaterialRef[] normals;
        }

        [System.Serializable]
        private class MaterialRef
        {
            public Color col;
            public string shader;
            public Material mat;

            public MaterialRef(Color col, string shader)
            {
                this.col = col;
                this.shader = shader;
            }

            public MaterialRef(Material mat, string shader)
            {
                this.mat = mat;
                this.shader = shader;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Highlight), true)]
        public class Highlight_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("alternativeColorProperty"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("overrideSettingsHighlightOption"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("highlightOption"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("isEnabled"), true);


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
