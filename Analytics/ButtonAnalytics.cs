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
    [RequireComponent(typeof(Button))]
    public class ButtonAnalytics : BaseAnalytics, IPointerEnterHandler, IPointerExitHandler
    {
        public bool useTooltip = false;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(SendAnalytics);
        }

        private void Start()
        {
            if (GetComponent<Tooltip>() != null)
            {
                Debug.Log("Tooltips are all internal for ButtonAnalytics. Destroying tooltip.cs");
                Destroy(GetComponent<Tooltip>());
            }
        }

        /// <summary>
        /// Send analytics event message to manager
        /// </summary>
        public override void SendAnalytics()
        {
            Collider col = GetComponentInParent<Collider>();
            GameObject go = (col != null) ? col.gameObject : gameObject;

            if (!RaycastManager.Instance.UIRaycastOperation(go)) return;

            RaycastManager.Instance.UIRaycastSelectablePressed(GetComponent<Selectable>());

            if (type.Equals(AnaylticsEventType.Predefined))
            {
                AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.UI, EventAction.Click, label);
            }
            else
            {
                AnalyticsManager.Instance.PostAnalyticsEvent(message);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!RaycastManager.Instance.UIRaycastOperation(gameObject)) return;

            if (useTooltip)
            {
                RaycastManager.Instance.CastRay = false;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //need to check if tooltip is active
            if (!TooltipManager.Instance.IsVisible) return;

            if (useTooltip)
            {
                TooltipManager.Instance.HideTooltip();
            }

            RaycastManager.Instance.CastRay = true;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ButtonAnalytics), true)]
        public class ButtonAnalytics_Editor : BaseAnalytics_Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("useTooltip"), true);

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}
