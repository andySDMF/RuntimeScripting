using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class VideoManager : Singleton<VideoManager>
    {
        public static VideoManager Instance
        {
            get
            {
                return ((VideoManager)instance);
            }
            set
            {
                instance = value;
            }
        }

 
        //interface for scrubber control, Video screens use this
        public interface IVideoControl
        {
            VideoPlayer VPlayer { get; }

            void FrameUpdate();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(VideoManager), true)]
        public class VideoManager_Editor : BaseInspectorEditor
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
