using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class VideoScrubber : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private bool m_paused = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            VideoManager.IVideoControl vScreen = GetComponentInParent<VideoManager.IVideoControl>();

            if(vScreen != null)
            {
                m_paused = !vScreen.VPlayer.isPlaying;

                StartCoroutine(Wait(vScreen, true));
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            VideoManager.IVideoControl vScreen = GetComponentInParent<VideoManager.IVideoControl>();

            if(vScreen != null)
            {
                StartCoroutine(Wait(vScreen, false));
            }
        }

        private IEnumerator Wait(VideoManager.IVideoControl vScreen, bool state)
        {
            yield return new WaitForEndOfFrame();

            if(state)
            {
                if (!m_paused)
                {
                    vScreen.VPlayer.Pause();
                }
            }
            else
            {
                vScreen.FrameUpdate();

                if (!m_paused)
                {
                    vScreen.VPlayer.Play();
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(VideoScrubber), true)]
        public class VideoScrubber_Editor : BaseInspectorEditor
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
