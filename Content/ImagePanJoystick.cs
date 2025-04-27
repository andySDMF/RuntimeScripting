using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ImagePanJoystick : JoystickController
    {
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            GetComponentInParent<ConferenceContentUploadController>().SendPan();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ImagePanJoystick), true)]
        public class ImagePanJoystick_Editor : JoystickController_Editor
        {

        }
#endif
    }
}
