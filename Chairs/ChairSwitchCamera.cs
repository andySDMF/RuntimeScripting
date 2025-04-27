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
    [RequireComponent(typeof(Button))]
    public class ChairSwitchCamera : MonoBehaviour
    {
        private void Awake()
        {
            if(!GetComponent<CanvasGroup>())
            {
                gameObject.AddComponent<CanvasGroup>();
            }

            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            ChairManager.Instance.SwitchCamera();
        }

        private void OnEnable()
        {
            if(ChairManager.Instance.OccupiedChairByPlayer != null)
            {
                CanvasGroup cGroup = GetComponent<CanvasGroup>();
                cGroup.alpha = ChairManager.Instance.OccupiedChairByPlayer.Group.HasAdditionalCameras ? 1.0f : 0.3f;
                cGroup.interactable = ChairManager.Instance.OccupiedChairByPlayer.Group.HasAdditionalCameras ? true : false;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ChairSwitchCamera), true)]
        public class ChairSwitchCamera_Editor : BaseInspectorEditor
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
