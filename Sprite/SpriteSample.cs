using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Image))]
    public class SpriteSample : MonoBehaviour
    {
        [SerializeField]
        private bool showWhenEmpty = true;

        [SerializeField]
        private string sampleSprite = "";

        [SerializeField]
        private string relativeResourcePath = "";

        [SerializeField]
        private bool isAtlas = false;

        [SerializeField]
        private string atlasTexturePath = "";

        private void Awake()
        {
            Image img = GetComponent<Image>();

            if(!img.enabled)
            {
                showWhenEmpty = false;
            }

            if (!string.IsNullOrEmpty(sampleSprite))
            {
                if(img.sprite != null)
                {
                    if(img.sprite.name != sampleSprite)
                    {
                        return;
                    }
                }

                if(isAtlas)
                {
                    Sprite[] sprites = Resources.LoadAll<Sprite>(relativeResourcePath + atlasTexturePath);

                    for(int i = 0; i < sprites.Length; i++)
                    {
                        if(sprites[i].name.Equals(sampleSprite))
                        {
                            img.sprite = sprites[i];
                            break;
                        }
                    }
                }
                else
                {
                    Sprite sp = Resources.Load<Sprite>(relativeResourcePath + sampleSprite);

                    if(sp != null)
                    {
                        img.sprite = sp;
                    }
                }
            }

            img.enabled = Application.isPlaying ? img.sprite != null ? true : showWhenEmpty : true;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SpriteSample), true)]
        public class SpriteSample_Editor : BaseInspectorEditor
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

                    EditorGUILayout.LabelField("Editor Data", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("showWhenEmpty"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sampleSprite"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("relativeResourcePath"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("isAtlas"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("atlasTexturePath"), true);

                    if (GUI.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(this);
                    }
                }
            }
        }
#endif

#if UNITY_EDITOR
        public void AddRouteResource(string res)
        {
            relativeResourcePath = res;
        }

        public void AttainSampleSprite()
        {
            Image img = GetComponentInChildren<Image>(true);

            if (img.sprite == null) return;

            if (string.IsNullOrEmpty(sampleSprite))
            {
                relativeResourcePath = "";
                sampleSprite = img.sprite != null ? img.sprite.name : "";

                if(img.sprite.texture.name != sampleSprite)
                {
                    isAtlas = true;
                    atlasTexturePath = img.sprite.texture.name;
                }

                string relativeAssetPath = AssetDatabase.GetAssetPath(img.sprite);
                string[] folders = relativeAssetPath.Split("/");
                bool start = false;

                for(int i = 0; i < folders.Length; i++)
                {
                    if(folders[i].Equals("Resources"))
                    {
                        start = true;
                        continue;
                    }

                    if(start)
                    {
                        if(System.IO.Path.HasExtension(folders[i]))
                        {
                            break;
                        }

                        relativeResourcePath += folders[i] + "/";
                    }
                }
            }

            //need to replace the current sprite with the default icon which is null
            img.sprite = null;

            img.enabled = showWhenEmpty;

            EditorUtility.SetDirty(this);
        }

        public void Visualzie()
        {
            Awake();
            EditorUtility.SetDirty(this);
        }

        public void Clear()
        {
            Image img = GetComponentInChildren<Image>(true);
            img.sprite = null;
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
