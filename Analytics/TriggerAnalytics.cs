using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrandLab360
{
    [RequireComponent(typeof(Collider))]
    public class TriggerAnalytics : BaseAnalytics
    {
        [SerializeField]
        private bool playerTriggersOnly = true;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (playerTriggersOnly)
            {
                if (other.transform.Equals(PlayerManager.Instance.GetLocalPlayer().TransformObject))
                {
                    SendAnalytics();
                }
            }
            else
            {
                SendAnalytics();
            }
        }

        /// <summary>
        /// Send analytics event message to manager
        /// </summary>
        public override void SendAnalytics()
        {
            if (type.Equals(AnaylticsEventType.Predefined))
            {
                AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Location, EventAction.Enter, label);
            }
            else
            {
                AnalyticsManager.Instance.PostAnalyticsEvent(message);
            }
        }
    }
}
