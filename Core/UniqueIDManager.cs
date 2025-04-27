using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class UniqueIDManager : Singleton<UniqueIDManager>
    {
        public static UniqueIDManager Instance
        {
            get
            {
                return ((UniqueIDManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        public Dictionary<string, int> ReplicatedIDCount = new Dictionary<string, int>();

        public bool Exists(string id, UniqueID obj)
        {
            if (string.IsNullOrEmpty(id) || obj == null) return true;

            UniqueID[] all = FindObjectsByType<UniqueID>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            UniqueID found = null;

            for (int i = 0; i < all.Length; i++)
            {
                if (string.IsNullOrEmpty(all[i].ID)) continue;

                if (!all[i].Equals(obj) && all[i].ID.Equals(id))
                {
                    return true;
                }

                if(all[i].Equals(obj))
                {
                    found = all[i];
                }
            }

            AppInstances settings;
            AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");

            if (appReferences != null)
            {
                settings = appReferences.Instances;
            }
            else
            {
                settings = Resources.Load<AppInstances>("ProjectAppInstances");
            }

            if (settings != null)
            {
                if (settings.UniqueIDExists(id) && found == null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool Exists(string id)
        {
            UniqueID[] all = FindObjectsByType<UniqueID>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].ID.Equals(id))
                {
                    return true;
                }
            }

            AppInstances settings;
            AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");

            if (appReferences != null)
            {
                settings = appReferences.Instances;
            }
            else
            {
                settings = Resources.Load<AppInstances>("ProjectAppInstances");
            }

            if (settings != null)
            {
                if (settings.UniqueIDExists(id))
                {
                    return true;
                }
            }

            return false;
        }

        public string NewID(GameObject GO)
        {
            string str = "";

            int randValue;
            char letter;

            for (int i = 0; i < 9; i++)
            {
                // Generating a random number.
                randValue = Random.Range(0, 27);

                // Generating random character by converting
                // the random number into character.
                letter = System.Convert.ToChar(randValue + 65);

                // Appending the letter to string.
                str = str + letter;
            }

            if(GO.scene != null)
            {
                Add(str, GO);
            }

            return str;
        }

        public string NewID()
        {
            string str = "";

            int randValue;
            char letter;

            for (int i = 0; i < 9; i++)
            {
                // Generating a random number.
                randValue = Random.Range(0, 27);

                // Generating random character by converting
                // the random number into character.
                letter = System.Convert.ToChar(randValue + 65);

                // Appending the letter to string.
                str = str + letter;
            }

            return str;
        }

        public void Add(string id, GameObject GO)
        {
            if (!GO.scene.IsValid()) return;

            AppInstances settings;
            AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");

            if (appReferences != null)
            {
                settings = appReferences.Instances;
            }
            else
            {
                settings = Resources.Load<AppInstances>("ProjectAppInstances");
            }

            if (settings != null)
            {
                settings.AddUniqueID(id, GO, GO.GetComponent<UniqueID>().MonoType);
            }
        }

        public void Clear(UniqueID id, string rawID)
        {
            if (string.IsNullOrEmpty(rawID)) return;

            UniqueID[] all = FindObjectsByType<UniqueID>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            bool contains = false;
            string idString = rawID;

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Equals(id))
                {
                    contains = true;
                }
            }

            if (!contains || id == null)
            {
                AppInstances settings;
                AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");

                if (appReferences != null)
                {
                    settings = appReferences.Instances;
                }
                else
                {
                    settings = Resources.Load<AppInstances>("ProjectAppInstances");
                }

                if (settings != null)
                {
                    settings.RemoveUniqueID(idString);
                }
            }
        }

        private IEnumerator ProcessClear(UniqueID id)
        {
            yield return new WaitForSeconds(0.5f);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(UniqueIDManager), true)]
        public class UniqueIDManager_Editor : BaseInspectorEditor
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
