using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Button))]
    public class VideoListButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
            string vid = GetComponentInParent<VideoScreen>().GetVideoByIndex(transform.GetSiblingIndex() - 1);

            //get video name not path
            string[] split = vid.Split('/');

            GetComponentInChildren<TextMeshProUGUI>().text = split[split.Length - 1].Replace(GetExtension(vid), "");
        }

        /// <summary>
        /// Called by the button
        /// </summary>
        private void OnClick()
        {
            GetComponentInParent<VideoScreen>().Load(transform.GetSiblingIndex() - 1);
        }

        /// <summary>
        /// Function called to find file extension - replaces System.IO.Path.GetExtension as this wont work on WebGL
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private string GetExtension(string source)
        {
            int n = source.Length - 1;
            string extension = "";

            for (int i = n; i > 0; i--)
            {
                if (source[i].Equals('.'))
                {
                    extension += source[i];
                    break;
                }
                else
                {
                    extension += source[i];
                }
            }

            char[] output = extension.ToCharArray();
            System.Array.Reverse(output);

            return new string(output);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(VideoListButton), true)]
        public class VideoListButton_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
            }
        }
#endif
    }
}
