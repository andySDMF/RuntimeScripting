using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ConfigurePallette : MonoBehaviour
    {
        [SerializeField]
        private GameObject button;

        [SerializeField]
        private GameObject toggle;

        [SerializeField]
        private Sprite[] transformSprites;

        public Vector2 Size
        {
            get
            {
                return GetComponent<RectTransform>().sizeDelta;
            }
        }

        public void CreateColors(List<Color> colors)
        {
            DestroyAll();

            for(int i = 0; i < colors.Count; i++)
            {
                GameObject but = Instantiate(button, Vector3.zero, Quaternion.identity, transform);
                but.transform.localPosition = Vector3.zero;
                but.transform.localRotation = Quaternion.Euler(0, 0, 0);
                but.transform.localScale = Vector3.one;
                but.SetActive(true);
                but.GetComponent<Image>().color = colors[i];
                but.GetComponent<ConfigureButton>().Set(i);
            }

            GetComponent<GridLayoutGroup>().constraint = GridLayoutGroup.Constraint.Flexible;
        }

        public void SetColorAtIndex(int index, Color col)
        {
            for (int i = 2; i < transform.childCount; i++)
            {
                if(i.Equals(index + 2))
                {
                    transform.GetChild(i).GetComponent<Image>().color = col;
                    break;
                }
            }
        }

        public void CreateMaterials(List<Sprite> mats)
        {
            DestroyAll();

            for (int i = 0; i < mats.Count; i++)
            {
                GameObject but = Instantiate(button, Vector3.zero, Quaternion.identity, transform);
                but.transform.localPosition = Vector3.zero;
                but.transform.localRotation = Quaternion.Euler(0, 0, 0);
                but.transform.localScale = Vector3.one;
                but.SetActive(true);
                but.GetComponent<Image>().sprite = mats[i];
                but.GetComponent<Image>().color = Color.white;
                but.GetComponent<ConfigureButton>().Set(i);
            }

            GetComponent<GridLayoutGroup>().constraint = GridLayoutGroup.Constraint.Flexible;
        }

        public void CreateModels(List<Sprite> mods)
        {
            DestroyAll();

            for (int i = 0; i < mods.Count; i++)
            {
                GameObject but = Instantiate(button, Vector3.zero, Quaternion.identity, transform);
                but.transform.localPosition = Vector3.zero;
                but.transform.localRotation = Quaternion.Euler(0, 0, 0);
                but.transform.localScale = Vector3.one;
                but.SetActive(true);
                but.GetComponent<Image>().sprite = mods[i];
                but.GetComponent<Image>().color = Color.white;
                but.GetComponent<ConfigureButton>().Set(i);
            }

            GetComponent<GridLayoutGroup>().constraint = GridLayoutGroup.Constraint.Flexible;
        }

        public void SetSpriteAtIndex(int index, Sprite sp)
        {
            for (int i = 2; i < transform.childCount; i++)
            {
                if (i.Equals(index + 2))
                {
                    transform.GetChild(i).GetComponent<Image>().sprite = sp;
                    transform.GetChild(i).GetComponent<Image>().color = Color.white;
                    break;
                }
            }
        }

        public void CreateTransform(bool moveEnabled, bool scaleEnabled, bool rotateEnabled)
        {
            DestroyAll();

            ToggleGroup tGroup = GetComponent<ToggleGroup>();

            if(tGroup == null)
            {
                tGroup = gameObject.AddComponent<ToggleGroup>();
            }

            tGroup.allowSwitchOff = true;

            for(int i = 0; i < transformSprites.Length; i++)
            {
                GameObject tog = Instantiate(toggle, Vector3.zero, Quaternion.identity, transform);
                tog.transform.localPosition = Vector3.zero;
                tog.transform.localRotation = Quaternion.Euler(0, 0, 0);
                tog.transform.localScale = Vector3.one;
                tog.SetActive(true);

                bool toolEnabled = i == 0 ? moveEnabled : i == 2 ? scaleEnabled : i == 1 ? rotateEnabled : false;
                tog.GetComponent<Toggle>().interactable = toolEnabled;
                tog.GetComponent<Toggle>().isOn = false;
                tog.GetComponent<Toggle>().group = tGroup;
                tog.GetComponent<ConfigureToggle>().Set(i, transformSprites[i]);
            }

            GetComponent<GridLayoutGroup>().constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            GetComponent<GridLayoutGroup>().constraintCount = 3;

            tGroup.SetAllTogglesOff();
        }

        public void DestroyAll()
        {
            if(GetComponent<ToggleGroup>())
            {
                DestroyImmediate(GetComponent<ToggleGroup>());
            }

            List<GameObject> temp = new List<GameObject>();

            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).Equals(button.transform) || transform.GetChild(i).Equals(toggle.transform)) continue;

                temp.Add(transform.GetChild(i).gameObject);
            }

            for (int i = 0; i < temp.Count; i++)
            {
                DestroyImmediate(temp[i]);
            }
        }

        public void DisableToggles()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).Equals(button.transform) || transform.GetChild(i).Equals(toggle.transform)) continue;

                if(transform.GetChild(i).GetComponent<Toggle>())
                {
                    transform.GetChild(i).GetComponent<Toggle>().isOn = false;
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ConfigurePallette), true)]
        public class ConfigurePallette_Editor : BaseInspectorEditor
        {
            private ConfigurePallette configScript;

            private void OnEnable()
            {
                GetBanner();
                Initialise();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("button"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("toggle"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("transformSprites"), true);

                configScript.gameObject.GetComponent<BoxCollider>().size = new Vector3(configScript.Size.x, configScript.Size.y, 10);


                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(configScript);
            }

            protected void Initialise()
            {
                configScript = (ConfigurePallette)target;
            }
        }
#endif
    }
}
