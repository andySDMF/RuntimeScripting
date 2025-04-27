using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace BrandLab360
{
    [System.Serializable]
    public class NPCSettings
    {
        [Header("NPC Global Control")]
        public float speed = 2.0f;
        public float angularSpeed = 500.0f;
        public float aceleration = 8;
        public float stoppingDistance = 0;
        public float obsticalAvoidanceRadius = 0.3f;
        public int obsticalAvoidancePriority = 1;

        [Header("AI")]
        [Range(1.0f, 10.0f)]
        public float stationaryMaxTime = 5.0f;
        public float maxWalkingDistance = 100.0f;

        [Header("NPC Per Scene")]
        public List<NPCManager.NPCScene> sceneNPC = new List<NPCManager.NPCScene>();

        public NPCManager.NPCScene GetScene(string scene)
        {
            return sceneNPC.FirstOrDefault(x => x.sceneName.Equals(scene));
        }

        public void AddScene(string scene)
        {
            NPCManager.NPCScene npcScene = GetScene(scene);

            if (npcScene == null)
            {
                npcScene = new NPCManager.NPCScene();
                npcScene.sceneName = scene;
                sceneNPC.Add(npcScene);
            }
        }
    }
}
