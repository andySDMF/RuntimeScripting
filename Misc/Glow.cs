using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Glow : MonoBehaviour
    {
        [SerializeField]
        private Color glowColor;

        [SerializeField]
        private Material glow;

        [SerializeField]
        private bool beginOnEnable = true;

        private bool m_isOn = false;
        private ObjectMaterial[] m_mats;
        private bool m_isMaterial = true;
        private ColorBlock m_cacheColors;
        private Color m_cacheColor;
        private Button m_button;


        private void Awake()
        {
            if (!AppManager.IsCreated) return;

            Renderer[] rends = GetComponentsInChildren<Renderer>(true);

            if(rends.Length > 0)
            {
                m_isMaterial = true;
            }
            else
            {
                m_isMaterial = false;
            }

            if(m_isMaterial)
            {
                m_mats = new ObjectMaterial[rends.Length];

                //get all materials on each child object
                for (int i = 0; i < rends.Length; i++)
                {
                    m_mats[i] = new ObjectMaterial();
                    m_mats[i].rend = rends[i];
                    m_mats[i].normals = new MaterialRef[rends[i].materials.Length];

                    for (int j = 0; j < rends[i].materials.Length; j++)
                    {
                        Material mat = new Material(Shader.Find("HDRP/Lit"));
                        mat.CopyPropertiesFromMaterial(rends[i].materials[j]);

                        m_mats[i].normals[j] = new MaterialRef(mat);
                    }
                }
            }
            else
            {
                m_button = GetComponentInChildren<Button>();
                m_cacheColors = m_button.colors;
                m_cacheColor = m_cacheColors.normalColor;
            }
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if (glowColor.a <= 0.0f)
            {
                //get color from settings
                Color col = AppManager.Instance.Settings.themeSettings.brandColor;
                glowColor = col;
            }

            if (glow == null)
            {
                glow = Resources.Load<Material>("HighlightMaterial");
            }
        }

        private void OnEnable()
        {
            if (beginOnEnable)
            {
                Begin();
            }
        }

        private void OnDisable()
        {
            End();
        }

        private void Update()
        {
            if (m_isOn)
            {
                float lerp = Mathf.PingPong(Time.time, 1.0f) / 1.0f;

                if (m_isMaterial)
                {
                    foreach (ObjectMaterial cm in m_mats)
                    {
                        for (int i = 0; i < cm.normals.Length; i++)
                        {
                            cm.rend.materials[i].Lerp(cm.normals[i].mat, glow, lerp);
                        }
                    }
                }
                else
                {
                    m_cacheColors.normalColor = Color.Lerp(m_cacheColor, glowColor, lerp);
                    m_button.colors = m_cacheColors;
                }
            }
        }

        public void Begin()
        {
            m_isOn = true;
        }

        public void End()
        {
            m_isOn = false;

            if(m_isMaterial)
            {
                if (m_mats == null) return;

                foreach (ObjectMaterial cm in m_mats)
                {
                    for (int i = 0; i < cm.normals.Length; i++)
                    {
                        cm.rend.materials[i].Lerp(cm.rend.materials[i], cm.normals[i].mat, 1.0f);
                    }
                }
            }
            else
            {
                m_cacheColors.normalColor = m_cacheColor;
                m_button.colors = m_cacheColors;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Glow), true), CanEditMultipleObjects]
        public class GlowEditor : BaseInspectorEditor
        {
            private Glow script;
            private bool isMaterial;

            private void OnEnable()
            {
                GetBanner();

                script = (Glow)target;

                isMaterial = script.GetComponentsInChildren<Renderer>(true).Length > 0;

                if(serializedObject.FindProperty("glowColor").colorValue.a <= 0.0f)
                {
                    //get color from settings
                    Color col = Resources.Load<AppConstReferences>("AppConstReferences").Settings.themeSettings.brandColor;
                    serializedObject.FindProperty("glowColor").colorValue = col;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(script);
                }

                if(serializedObject.FindProperty("glow").objectReferenceValue == null)
                {
                    serializedObject.FindProperty("glow").objectReferenceValue = Resources.Load<Object>("HighlightMaterial");
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("beginOnEnable"), true);

                if (isMaterial)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("glow"), true);
                }
                else
                {
                    EditorGUILayout.LabelField("This will use the first button script found");
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("glowColor"), true);
                }

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(script);
                }
            }
        }
#endif


        [System.Serializable]
        private class ObjectMaterial
        {
            public Renderer rend;
            public MaterialRef[] normals;
        }

        [System.Serializable]
        private class MaterialRef
        {
            public Material mat;

            public MaterialRef(Material mat)
            {
                this.mat = mat;
            }
        }
    }
}
