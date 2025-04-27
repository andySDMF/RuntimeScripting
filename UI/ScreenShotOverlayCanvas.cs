using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BrandLab360;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ScreenShotOverlayCanvas : MonoBehaviour
    {
        [SerializeField]
        private GameObject watermark;

        [SerializeField]
        private GameObject comment;

        [SerializeField]
        private TMPro.TextMeshProUGUI commentText;

        [SerializeField]
        private LayoutElement commentLayout;

        [SerializeField]
        private GameObject overlay;

        [SerializeField]
        private Image overlayIcon;

        private RectTransform m_rectT;

        private void Start()
        {
            m_rectT = commentLayout.gameObject.GetComponent<RectTransform>();
        }


        private void Update()
        {
            if (commentLayout != null && m_rectT != null)
            {
                if (m_rectT.sizeDelta.x > 1000)
                {
                    commentLayout.preferredWidth = 1000;
                    commentLayout.minWidth = 1000;
                }
                else
                {
                    commentLayout.preferredWidth = -1;
                    commentLayout.minWidth = -1;
                }
            }
        }

        public void EnableThis(bool show)
        {
            gameObject.SetActive(show);
        }

        public void AddOverlay(bool enable, Sprite overlay)
        {
            overlayIcon.sprite = overlay;

            if (overlayIcon.sprite != null)
            {
                overlayIcon.SetNativeSize();
            }
            else
            {
                overlayIcon.GetComponent<RectTransform>().sizeDelta = Vector3.zero;
            }

            this.overlay.SetActive(enable);
        }

        public void AddComment(bool enable, string comment)
        {
            commentText.text = comment;

            this.comment.SetActive(enable);
        }

        public void AddWatermark(bool enable)
        {
            watermark.SetActive(enable);
        }

        public void HideUI(bool hide)
        {
            //need to hide the brandlab object
            CoreManager core = FindFirstObjectByType<CoreManager>(FindObjectsInactive.Include);

            if (core != null)
            {
                CoreManager.Instance.transform.GetChild(0).GetChild(0).localScale = hide ? Vector3.zero : Vector3.one;
            }
            else
            {
                AppLogin login = FindFirstObjectByType<AppLogin>(FindObjectsInactive.Include);

                if (login != null)
                {
                    login.transform.GetChild(1).transform.localScale = hide ? Vector3.zero : Vector3.one;
                }
                else
                {
                    AppAvatar avatar = FindFirstObjectByType<AppAvatar>(FindObjectsInactive.Include);

                    if (avatar != null)
                    {
                        avatar.transform.GetChild(0).transform.localScale = hide ? Vector3.zero : Vector3.one;
                    }
                }
            }

            if (!hide)
            {
                if (core != null)
                {
                    CoreManager.Instance.transform.GetChild(0).GetChild(0).GetChild(1).GetChild(4).localScale = Vector3.zero;
                }
                else
                {
                    AppLogin login = FindFirstObjectByType<AppLogin>(FindObjectsInactive.Include);

                    if (login != null)
                    {
                        login.SetLogoVisibility(false);
                    }
                    else
                    {
                        AppAvatar avatar = FindFirstObjectByType<AppAvatar>(FindObjectsInactive.Include);

                        if (avatar != null)
                        {
                            avatar.SetLogoVisibility(false);
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ScreenShotOverlayCanvas), true)]
        public class ScreenShotOverlayCanvas_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("watermark"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("comment"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("commentText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("commentLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("overlay"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("overlayIcon"), true);

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