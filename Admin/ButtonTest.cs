using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ButtonTest : Button
    {
        public override void OnPointerClick(PointerEventData eventData)
        {
            Debug.LogError("button OnPointerClick");

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            Debug.LogError("button OnPointerClick not left");

            Press();
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            Debug.LogError("button OnSubmit");

            Press();

            // if we get set disabled during the press
            // don't run the coroutine.
            if (!IsActive() || !IsInteractable())
                return;

            Debug.LogError("button OnSubmit is good");

            DoStateTransition(SelectionState.Pressed, false);
            StartCoroutine(OnFinishSubmit());
        }

        private void Press()
        {
            Debug.LogError("button Press()");

            if (!IsActive() || !IsInteractable())
                return;

            Debug.LogError("button Press() is good");

            UISystemProfilerApi.AddMarker("Button.onClick", this);
            onClick.Invoke();
        }

        private IEnumerator OnFinishSubmit()
        {
            var fadeTime = colors.fadeDuration;
            var elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            DoStateTransition(currentSelectionState, false);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ButtonTest), true)]
        public class ButtonTest_Editor : BaseInspectorEditor
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
