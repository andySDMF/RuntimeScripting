using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrandLab360
{
    [System.Serializable]
    public class AKFSettings
    {
        public bool enable = true;
        [Range(1, 10)]
        public int displayAfterMinute = 1;

        [Range(0.5f, 1.0f)]
        public float countdownMinutes = 0.5f;

        [Header("Messages")]
        public string textMessage = "Are you still there?";
        public string countdownTimerMessage = "Disconnecting in: ";
        public string sessionEndMessage = "Session Ended!";
    }
}
