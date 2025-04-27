using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrandLab360
{
    [System.Serializable]
    public class ChatSettings
    {
        [Header("Private Messageing System")]
        [HideInInspector]
        public ChatSystem privateChat = ChatSystem.Smartphone;

        [Header("Chat Messages")]
        public bool useGlobalChat = true;
        public bool usePrivateChat = true;

        [Header("Player Object")]
        public bool usePlayerChat = true;

        [Header("User Statuses")]
        public Color online = Color.green;
        public Color busy = Color.yellow;
        public Color offline = Color.red;

        [Header("Message Format")]
        public bool displayDate = false;
        public bool displayTime = true;
    }
}
