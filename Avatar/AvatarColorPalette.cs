using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class AvatarColorPalette : MonoBehaviour
    {
        [SerializeField]
        private GameObject colorButtonPrefab;

        public void OnEnable()
        {
            CreatePalette();
        }

        private void CreatePalette()
        {
            if (AppManager.Instance.Settings.playerSettings.overrideClothingColors)
            {
                if (colorButtonPrefab != null)
                {
                    for(int i = transform.childCount - 1; i > 0; i--)
                    {
                        if (transform.GetChild(i).Equals(colorButtonPrefab)) continue;

                        Destroy(transform.GetChild(i).gameObject);
                    }

                    colorButtonPrefab.GetComponent<Button>().onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
                    colorButtonPrefab.gameObject.SetActive(false);

                    for(int i = 0; i < AppManager.Instance.Settings.playerSettings.simpleClothingColors.Length; i++)
                    {
                        GameObject go = Instantiate(colorButtonPrefab, Vector3.zero, Quaternion.identity, transform);
                        go.transform.localScale = Vector3.one;
                        go.transform.GetChild(0).GetComponentInChildren<Image>().color = AppManager.Instance.Settings.playerSettings.simpleClothingColors[i];
                        int n = i;
                        go.GetComponentInChildren<Button>().onClick.AddListener(() => { OnClicked(n); });
                        go.SetActive(true);
                    }
                }
            }
            else
            {
                //create the buttons based on avatarcustomise object
                CustomiseAvatar appAvatar = FindFirstObjectByType<CustomiseAvatar>();

                if (appAvatar.CustomAvatar.FixedColors != null)
                {
                    if (colorButtonPrefab != null)
                    {
                        for (int i = transform.childCount - 1; i > 0; i--)
                        {
                            if (transform.GetChild(i).Equals(colorButtonPrefab)) continue;

                            Destroy(transform.GetChild(i).gameObject);
                        }

                        colorButtonPrefab.GetComponent<Button>().onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
                        colorButtonPrefab.gameObject.SetActive(false);

                        for (int i = 0; i < appAvatar.CustomAvatar.FixedColors.Length; i++)
                        {
                            GameObject go = Instantiate(colorButtonPrefab, Vector3.zero, Quaternion.identity, transform);
                            go.transform.localScale = Vector3.one;
                            go.transform.GetChild(0).GetComponentInChildren<Image>().color = appAvatar.CustomAvatar.FixedColors[i];
                            int n = i;
                            go.GetComponentInChildren<Button>().onClick.AddListener(() => { OnClicked(n); });
                            go.SetActive(true);
                        }
                    }
                }
            }
        }

        private void OnClicked(int index)
        {
            GetComponentInParent<CustomiseAvatar>().ChangeFixedColor(index);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AvatarColorPalette), true)]
        public class AvatarColorPalette_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("colorButtonPrefab"), true);

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
