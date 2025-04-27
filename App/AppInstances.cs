using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace BrandLab360
{
    [CreateAssetMenu(fileName = "ProjectAppInstances", menuName = "ScriptableObjects/ProjectAppInstances", order = 1)]
    public class AppInstances : ScriptableObject
    {
        [Header("Editor Window")]
        public Sprite brandlabLogo;

        [Header("IO Objects")]
        public bool ignoreIObjectSettings = true;
        public List<IOObjectTeleportHandler> ioTeleportObjects = new List<IOObjectTeleportHandler>();
        public List<IOObjectDoorHandler> ioDoorObjects = new List<IOObjectDoorHandler>();
        public List<IOObjectLockHandler> ioLockObjects = new List<IOObjectLockHandler>();
        public List<IOObjectItemHandler> ioItemObjects = new List<IOObjectItemHandler>();
        public List<IOObjectPopupHandler> ioPopupObjects = new List<IOObjectPopupHandler>();
        public List<IOObjectMascotHandler> ioMascotObjects = new List<IOObjectMascotHandler>();
        public List<IOObjectVideoScreenHandler> ioVideoScreenObjects = new List<IOObjectVideoScreenHandler>();
        public List<IOObjectWorldUploadHandler> ioWorldUploadObjects = new List<IOObjectWorldUploadHandler>();
        public List<IOObjectConfiguratorHandler> ioConfiguratorObjects = new List<IOObjectConfiguratorHandler>();
        public List<IOObjectChairGroupHandler> ioChairGroupObjects = new List<IOObjectChairGroupHandler>();
        public List<IOObjectProductHandler> ioProductObjects = new List<IOObjectProductHandler>();
        public List<IOObjectProductPlacementHandler> ioProductPlacementObjects = new List<IOObjectProductPlacementHandler>();
        public List<IOObjectNPCBotSpawenAreaHandler> ioNPCBotSpawnAreaObjects = new List<IOObjectNPCBotSpawenAreaHandler>();
        public List<IOObjectNoticeboardHandler> ioNoticeboardObjects = new List<IOObjectNoticeboardHandler>();
        public List<IOObjectVehcileSpawnHandler> ioVehicleSpawnObjects = new List<IOObjectVehcileSpawnHandler>();

        [Header("Floorplan")]
        public List<FloorplanManager.FloorplanPrefab> floorplanPrefabs = new List<FloorplanManager.FloorplanPrefab>();

        [Header("Product Shops")]
        public List<string> shops = new List<string>();

        [Header("Tooltips")]
        public Tooltip[] fixedTooltips;

#if UNITY_EDITOR
        public Tooltip[] CreateNewFixedTooltips()
        {
            return new Tooltip[5]
            {
               // new Tooltip("Analytics", "Select to send analytics"), 
                new Tooltip("SwitchSceneTrigger", "Select to enter room"),
                new Tooltip("InfoTag", "Select to view info"),
                new Tooltip("AssortmentMenu", "Select to handle item"),
                new Tooltip("DeleteMenu", "Select to remove item"),
                new Tooltip("PickupMenu", "Select to pickup item")
            };
        }
#endif

        public List<Tooltip> tooltips = new List<Tooltip>();

        [Header("UniqueIDs")]
        public List<UniqueIDObject> uniqueIDs = new List<UniqueIDObject>();

        [Header("Switch Scene Settings")]
        public List<SwitchSceneTriggerID> SwitchSceneReferences = new List<SwitchSceneTriggerID>();


        public void AddIOObject(string referenceID, UniqueID.IObjectSetting obj, bool fullsearch = false)
        {
            if (obj == null) return;

            if (!fullsearch)
            {
#if UNITY_EDITOR
                if (UnityEditor.Selection.activeObject != null && UnityEditor.Selection.activeObject is GameObject)
                {
                    if(!((GameObject)UnityEditor.Selection.activeObject).scene.IsValid())
                    {
                        return;
                    }
                }
#endif
            }

            if(obj is TeleportPoint.TeleportIOObject)
            {
                IOObjectTeleportHandler exists = ioTeleportObjects.FirstOrDefault(x => x.referenceID.Equals(obj.ID));

                if(exists != null)
                {
                    if (fullsearch) return;
                    exists.settings = (TeleportPoint.TeleportIOObject)obj;
                    exists.nameGO = obj.GO;
                    exists.Update(obj);
                }
                else
                {
                    IOObjectTeleportHandler io = new IOObjectTeleportHandler();
                    io.referenceID = obj.ID;
                    io.nameGO = obj.GO;
                    io.settings = (TeleportPoint.TeleportIOObject)obj;
                    io.Update(obj);

                    ioTeleportObjects.Add(io);
                }
            }
            else if(obj is Door.DoorIOObject)
            {
                IOObjectDoorHandler exists = ioDoorObjects.FirstOrDefault(x => x.referenceID.Equals(obj.ID));

                if (exists != null)
                {
                    if (fullsearch) return;

                    exists.settings = (Door.DoorIOObject)obj;
                    exists.nameGO = obj.GO;
                    exists.Update(obj);
                }
                else
                {
                    IOObjectDoorHandler io = new IOObjectDoorHandler();
                    io.referenceID = obj.ID;
                    io.nameGO = obj.GO;
                    io.settings = (Door.DoorIOObject)obj;
                    io.Update(obj);

                    ioDoorObjects.Add(io);
                }
            }
            else if(obj is Lock.LockIOObject)
            {
                IOObjectLockHandler exists = ioLockObjects.FirstOrDefault(x => x.referenceID.Equals(obj.ID));

                if (exists != null)
                {
                    if (fullsearch) return;

                    exists.settings = (Lock.LockIOObject)obj;
                    exists.nameGO = obj.GO;
                    exists.Update(obj);
                }
                else
                {
                    IOObjectLockHandler io = new IOObjectLockHandler();
                    io.referenceID = obj.ID;
                    io.nameGO = obj.GO;
                    io.settings = (Lock.LockIOObject)obj;
                    io.Update(obj);

                    ioLockObjects.Add(io);
                }
            }
            else if (obj is PickupItem.ItemIOObject)
            {
                IOObjectItemHandler exists = ioItemObjects.FirstOrDefault(x => x.referenceID.Equals(obj.ID));

                if (exists != null)
                {
                    if (fullsearch) return;

                    exists.settings = (PickupItem.ItemIOObject)obj;
                    exists.nameGO = obj.GO;
                    exists.Update(obj);
                }
                else
                {
                    IOObjectItemHandler io = new IOObjectItemHandler();
                    io.referenceID = obj.ID;
                    io.nameGO = obj.GO;
                    io.settings = (PickupItem.ItemIOObject)obj;
                    io.Update(obj);

                    ioItemObjects.Add(io);
                }
            }
            else if (obj is PopupTag.PopupTagIOObject)
            {
                IOObjectPopupHandler exists = ioPopupObjects.FirstOrDefault(x => x.referenceID.Equals(obj.ID));

                if (exists != null)
                {
                    if (fullsearch) return;

                    exists.settings = (PopupTag.PopupTagIOObject)obj;
                    exists.nameGO = obj.GO;
                    exists.Update(obj);
                }
                else
                {
                    IOObjectPopupHandler io = new IOObjectPopupHandler();
                    io.referenceID = obj.ID;
                    io.nameGO = obj.GO;
                    io.settings = (PopupTag.PopupTagIOObject)obj;
                    io.Update(obj);

                    ioPopupObjects.Add(io);
                }
            }
            else if (obj is Mascot.MascotIOObject)
            {
                IOObjectMascotHandler exists = ioMascotObjects.FirstOrDefault(x => x.referenceID.Equals(obj.ID));

                if (exists != null)
                {
                    if (fullsearch) return;

                    exists.settings = (Mascot.MascotIOObject)obj;
                    exists.nameGO = obj.GO;
                    exists.Update(obj);
                }
                else
                {
                    IOObjectMascotHandler io = new IOObjectMascotHandler();
                    io.referenceID = obj.ID;
                    io.nameGO = obj.GO;
                    io.settings = (Mascot.MascotIOObject)obj;
                    io.Update(obj);

                    ioMascotObjects.Add(io);
                }
            }
            else if (obj is VideoScreen.VideoScreenIOObject)
            {
                IOObjectVideoScreenHandler exists = ioVideoScreenObjects.FirstOrDefault(x => x.referenceID.Equals(obj.ID));

                if (exists != null)
                {
                    if (fullsearch) return;

                    exists.settings = (VideoScreen.VideoScreenIOObject)obj;
                    exists.nameGO = obj.GO;
                    exists.Update(obj);
                }
                else
                {
                    IOObjectVideoScreenHandler io = new IOObjectVideoScreenHandler();
                    io.referenceID = obj.ID;
                    io.nameGO = obj.GO;
                    io.settings = (VideoScreen.VideoScreenIOObject)obj;
                    io.Update(obj);

                    ioVideoScreenObjects.Add(io);
                }
            }
            else if (obj is WorldContentUpload.WorldUploadIOObject)
            {
                IOObjectWorldUploadHandler exists = ioWorldUploadObjects.FirstOrDefault(x => x.referenceID.Equals(obj.ID));

                if (exists != null)
                {
                    if (fullsearch) return;

                    exists.settings = (WorldContentUpload.WorldUploadIOObject)obj;
                    exists.nameGO = obj.GO;
                    exists.Update(obj);
                }
                else
                {
                    IOObjectWorldUploadHandler io = new IOObjectWorldUploadHandler();
                    io.referenceID = obj.ID;
                    io.nameGO = obj.GO;
                    io.type = ((WorldContentUpload.WorldUploadIOObject)obj).Type;
                    io.settings = (WorldContentUpload.WorldUploadIOObject)obj;
                    io.Update(obj);

                    ioWorldUploadObjects.Add(io);
                }
            }
            else if (obj is Configurator.ConfiguratorIOObject)
            {
                IOObjectConfiguratorHandler exists = ioConfiguratorObjects.FirstOrDefault(x => x.referenceID.Equals(obj.ID));

                if (exists != null)
                {
                    if (fullsearch) return;

                    exists.settings = (Configurator.ConfiguratorIOObject)obj;
                    exists.nameGO = obj.GO;
                    exists.Update(obj);
                }
                else
                {
                    IOObjectConfiguratorHandler io = new IOObjectConfiguratorHandler();
                    io.referenceID = obj.ID;
                    io.nameGO = obj.GO;
                    io.type = ((Configurator.ConfiguratorIOObject)obj).Type;
                    io.settings = (Configurator.ConfiguratorIOObject)obj;
                    io.Update(obj);

                    ioConfiguratorObjects.Add(io);
                }
            }
            else if (obj is ChairGroup.ChairGroupIOObject || obj is ConferenceChairGroup.ConferenceChairGroupIOObject)
            {
                IOObjectChairGroupHandler exists = ioChairGroupObjects.FirstOrDefault(x => x.referenceID.Equals(obj.ID));

                if (exists != null)
                {
                    if (obj is ConferenceChairGroup.ConferenceChairGroupIOObject)
                    {
                        if (fullsearch) return;

                        exists.conferenceSettings = (ConferenceChairGroup.ConferenceChairGroupIOObject)obj;
                        exists.type = ((ConferenceChairGroup.ConferenceChairGroupIOObject)obj).Type;
                    }
                    else
                    {
                        if (fullsearch) return;

                        exists.settings = (ChairGroup.ChairGroupIOObject)obj;
                        exists.type = ((ChairGroup.ChairGroupIOObject)obj).Type;
                    }

                    exists.Update(obj);
                    exists.nameGO = obj.GO;
                }
                else
                {
                    IOObjectChairGroupHandler io = new IOObjectChairGroupHandler();
                    io.referenceID = obj.ID;
                    io.nameGO = obj.GO;

                    if (obj is ConferenceChairGroup.ConferenceChairGroupIOObject)
                    {
                        io.conferenceSettings = (ConferenceChairGroup.ConferenceChairGroupIOObject)obj;
                        io.type = ((ConferenceChairGroup.ConferenceChairGroupIOObject)obj).Type;
                        io.streamCache = ((ConferenceChairGroup.ConferenceChairGroupIOObject)obj).StreamCache;
                    }
                    else
                    {
                        io.settings = (ChairGroup.ChairGroupIOObject)obj;
                        io.type = ((ChairGroup.ChairGroupIOObject)obj).Type;
                        io.streamCache = ((ChairGroup.ChairGroupIOObject)obj).StreamCache;
                    }

                    io.Update(obj);
                    ioChairGroupObjects.Add(io);
                }
            }
            else if (obj is Product.ProductIOObject)
            {
                IOObjectProductHandler exists = ioProductObjects.FirstOrDefault(x => x.referenceID.Equals(obj.ID));

                if (exists != null)
                {
                    if (fullsearch) return;

                    exists.settings = (Product.ProductIOObject)obj;
                    exists.nameGO = obj.GO;
                    exists.Update(obj);
                }
                else
                {
                    IOObjectProductHandler io = new IOObjectProductHandler();
                    io.referenceID = obj.ID;
                    io.nameGO = obj.GO;
                    io.settings = (Product.ProductIOObject)obj;
                    io.Update(obj);

                    ioProductObjects.Add(io);
                }
            }
            else if (obj is ProductPlacement.ProductPlacementIOObject)
            {
                IOObjectProductPlacementHandler exists = ioProductPlacementObjects.FirstOrDefault(x => x.referenceID.Equals(obj.ID));

                if (exists != null)
                {
                    if (fullsearch) return;

                    exists.settings = (ProductPlacement.ProductPlacementIOObject)obj;
                    exists.nameGO = obj.GO;
                    exists.Update(obj);
                }
                else
                {
                    IOObjectProductPlacementHandler io = new IOObjectProductPlacementHandler();
                    io.referenceID = obj.ID;
                    io.nameGO = obj.GO;
                    io.settings = (ProductPlacement.ProductPlacementIOObject)obj;
                    io.Update(obj);

                    ioProductPlacementObjects.Add(io);
                }
            }
            else if (obj is NPCBotSpawnArea.NPCBotSpawnAreaIOObject)
            {
                IOObjectNPCBotSpawenAreaHandler exists = ioNPCBotSpawnAreaObjects.FirstOrDefault(x => x.referenceID.Equals(obj.ID));

                if (exists != null)
                {
                    if (fullsearch) return;

                    exists.settings = (NPCBotSpawnArea.NPCBotSpawnAreaIOObject)obj;
                    exists.nameGO = obj.GO;
                    exists.Update(obj);
                }
                else
                {
                    IOObjectNPCBotSpawenAreaHandler io = new IOObjectNPCBotSpawenAreaHandler();
                    io.referenceID = obj.ID;
                    io.nameGO = obj.GO;
                    io.settings = (NPCBotSpawnArea.NPCBotSpawnAreaIOObject)obj;
                    io.Update(obj);

                    ioNPCBotSpawnAreaObjects.Add(io);
                }
            }
            else if (obj is NoticeBoard.NoticeBoardIOObject)
            {
                IOObjectNoticeboardHandler exists = ioNoticeboardObjects.FirstOrDefault(x => x.referenceID.Equals(obj.ID));

                if (exists != null)
                {
                    if (fullsearch) return;

                    exists.settings = (NoticeBoard.NoticeBoardIOObject)obj;
                    exists.nameGO = obj.GO;
                    exists.Update(obj);
                }
                else
                {
                    IOObjectNoticeboardHandler io = new IOObjectNoticeboardHandler();
                    io.referenceID = obj.ID;
                    io.nameGO = obj.GO;
                    io.settings = (NoticeBoard.NoticeBoardIOObject)obj;
                    io.Update(obj);

                    ioNoticeboardObjects.Add(io);
                }
            }
            else if (obj is VehicleSpawn.VehicleSpawnIOObject)
            {
                IOObjectVehcileSpawnHandler exists = ioVehicleSpawnObjects.FirstOrDefault(x => x.referenceID.Equals(obj.ID));

                if (exists != null)
                {
                    if (fullsearch) return;

                    exists.settings = (VehicleSpawn.VehicleSpawnIOObject)obj;
                    exists.nameGO = obj.GO;
                    exists.Update(obj);
                }
                else
                {
                    IOObjectVehcileSpawnHandler io = new IOObjectVehcileSpawnHandler();
                    io.referenceID = obj.ID;
                    io.nameGO = obj.GO;
                    io.settings = (VehicleSpawn.VehicleSpawnIOObject)obj;
                    io.Update(obj);

                    ioVehicleSpawnObjects.Add(io);
                }
            }

#if UNITY_EDITOR

            if (Application.isPlaying) return;

            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void RemoveIOObject(UniqueID.IObjectSetting obj, bool interfaceWindowDeletion = false)
        {
            bool contains = false;
            string id = obj.ID;

            if (!interfaceWindowDeletion)
            {
                UniqueID[] all = FindObjectsByType<UniqueID>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].ID.Equals(id))
                    {
                        contains = true;
                    }
                }
            }

            if(!contains)
            {
                if (obj is TeleportPoint.TeleportIOObject)
                {
                    IOObjectTeleportHandler exists = ioTeleportObjects.FirstOrDefault(x => x.referenceID.Equals(id));

                    if (exists != null)
                    {
                        ioTeleportObjects.Remove(exists);
                    }
                }
                else if (obj is Door.DoorIOObject)
                {
                    IOObjectDoorHandler exists = ioDoorObjects.FirstOrDefault(x => x.referenceID.Equals(id));

                    if (exists != null)
                    {
                        ioDoorObjects.Remove(exists);
                    }
                }
                else if (obj is Lock.LockIOObject)
                {
                    IOObjectLockHandler exists = ioLockObjects.FirstOrDefault(x => x.referenceID.Equals(id));

                    if (exists != null)
                    {
                        ioLockObjects.Remove(exists);
                    }
                }
                else if (obj is PickupItem.ItemIOObject)
                {
                    IOObjectItemHandler exists = ioItemObjects.FirstOrDefault(x => x.referenceID.Equals(id));

                    if (exists != null)
                    {
                        ioItemObjects.Remove(exists);
                    }
                }
                else if (obj is PopupTag.PopupTagIOObject)
                {
                    IOObjectPopupHandler exists = ioPopupObjects.FirstOrDefault(x => x.referenceID.Equals(id));

                    if (exists != null)
                    {
                        ioPopupObjects.Remove(exists);
                    }
                }
                else if (obj is Mascot.MascotIOObject)
                {
                    IOObjectMascotHandler exists = ioMascotObjects.FirstOrDefault(x => x.referenceID.Equals(id));

                    if (exists != null)
                    {
                        ioMascotObjects.Remove(exists);
                    }
                }
                else if (obj is VideoScreen.VideoScreenIOObject)
                {
                    IOObjectVideoScreenHandler exists = ioVideoScreenObjects.FirstOrDefault(x => x.referenceID.Equals(id));

                    if (exists != null)
                    {
                        ioVideoScreenObjects.Remove(exists);
                    }
                }
                else if (obj is WorldContentUpload.WorldUploadIOObject)
                {
                    IOObjectWorldUploadHandler exists = ioWorldUploadObjects.FirstOrDefault(x => x.referenceID.Equals(id));

                    if (exists != null)
                    {
                        ioWorldUploadObjects.Remove(exists);
                    }
                }
                else if (obj is Configurator.ConfiguratorIOObject)
                {
                    IOObjectConfiguratorHandler exists = ioConfiguratorObjects.FirstOrDefault(x => x.referenceID.Equals(id));

                    if (exists != null)
                    {
                        ioConfiguratorObjects.Remove(exists);
                    }
                }
                else if (obj is ChairGroup.ChairGroupIOObject)
                {
                    IOObjectChairGroupHandler exists = ioChairGroupObjects.FirstOrDefault(x => x.referenceID.Equals(id));

                    if (exists != null)
                    {
                        ioChairGroupObjects.Remove(exists);
                    }
                }
                else if (obj is Product.ProductIOObject)
                {
                    IOObjectProductHandler exists = ioProductObjects.FirstOrDefault(x => x.referenceID.Equals(id));

                    if (exists != null)
                    {
                        ioProductObjects.Remove(exists);
                    }
                }
                else if (obj is ProductPlacement.ProductPlacementIOObject)
                {
                    IOObjectProductPlacementHandler exists = ioProductPlacementObjects.FirstOrDefault(x => x.referenceID.Equals(id));

                    if (exists != null)
                    {
                        ioProductPlacementObjects.Remove(exists);
                    }
                }
                else if (obj is NPCBotSpawnArea.NPCBotSpawnAreaIOObject)
                {
                    IOObjectNPCBotSpawenAreaHandler exists = ioNPCBotSpawnAreaObjects.FirstOrDefault(x => x.referenceID.Equals(id));

                    if (exists != null)
                    {
                        ioNPCBotSpawnAreaObjects.Remove(exists);
                    }
                }
                else if (obj is NoticeBoard.NoticeBoardIOObject)
                {
                    IOObjectNoticeboardHandler exists = ioNoticeboardObjects.FirstOrDefault(x => x.referenceID.Equals(id));

                    if (exists != null)
                    {
                        ioNoticeboardObjects.Remove(exists);
                    }
                }
                else if (obj is VehicleSpawn.VehicleSpawnIOObject)
                {
                    IOObjectVehcileSpawnHandler exists = ioVehicleSpawnObjects.FirstOrDefault(x => x.referenceID.Equals(id));

                    if (exists != null)
                    {
                        ioVehicleSpawnObjects.Remove(exists);
                    }
                }
            }

#if UNITY_EDITOR

            if (Application.isPlaying) return;

            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public string GetFixedTooltip(string id)
        {
            return fixedTooltips.FirstOrDefault(x => x.id.Equals(id)).tooltip;
        }

        public void AddTooltip(string id, string tooltip)
        {
            Tooltip tTip = GetTooltip(id);

            if (tTip != null)
            {
                tTip.tooltip = tooltip;
            }
            else
            {
                tTip = new Tooltip(id, tooltip);
                tooltips.Add(tTip);
            }
        }

        public void RemovedTooltip(string id)
        {
            Tooltip tTip = GetTooltip(id);

            if(tTip != null)
            {
                tooltips.Remove(tTip);
            }
        }

        public Tooltip GetTooltip(string id)
        {
            return tooltips.FirstOrDefault(x => x.id.Equals(id));
        }


        public UniqueIDObject GetUniqueIDObject(string id)
        {
            UniqueIDObject uID = uniqueIDs.FirstOrDefault(x => x.id.Equals(id));

            return uID;
        }

        public bool UniqueIDExists(string id)
        {
            UniqueIDObject uID = uniqueIDs.FirstOrDefault(x => x.id.Equals(id));

            if (uID != null)
            {
                return true;
            }

            return false;
        }

        public void AddUniqueID(string id, GameObject GO, string type)
        {
            UniqueIDObject uID = uniqueIDs.FirstOrDefault(x => x.id.Equals(id));

            if (uID == null)
            {
                uID = new UniqueIDObject();
                uID.id = id;
                uID.sceneName = GO.scene.name;
                uID.type = type;
                uniqueIDs.Add(uID);
            }
            else
            {
                uID.sceneName = GO.scene.name;
                uID.type = type;
            }

#if UNITY_EDITOR

            if (Application.isPlaying) return;

            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void RemoveUniqueID(string id)
        {
            UniqueIDObject uID = uniqueIDs.FirstOrDefault(x => x.id.Equals(id));

            if (uID != null)
            {
                uniqueIDs.Remove(uID);
            }

#if UNITY_EDITOR

            if (Application.isPlaying) return;

            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void UpdateSwitchTrigger(SwitchSceneTriggerID trigger)
        {
            SwitchSceneTriggerID exists = GetSwitchSceneReference(trigger.id);

            if (exists != null)
            {
                exists.load = trigger.load;
                exists.scene = trigger.scene;
                exists.triggerObjectName = trigger.triggerObjectName;
                exists.spawnPoint = trigger.spawnPoint;
                exists.view = trigger.view;

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        public SwitchSceneTriggerID GetSwitchSceneReference(string id)
        {
            return SwitchSceneReferences.FirstOrDefault(x => x.id.Equals(id));
        }

        public void DeleteSwitchSceneReference(string id)
        {
            SwitchSceneTriggerID exists = GetSwitchSceneReference(id);

            if (exists != null)
            {
                SwitchSceneReferences.Remove(exists);
            }
        }

        public List<SwitchSceneTriggerID> GetSwitchSceneReferences(string currentScene)
        {
            List<SwitchSceneTriggerID> all = new List<SwitchSceneTriggerID>();

            for (int i = 0; i < SwitchSceneReferences.Count; i++)
            {
                if (!SwitchSceneReferences[i].scene.Equals(currentScene))
                {
                    SwitchSceneTriggerID exists = all.FirstOrDefault(x => x.scene.Equals(SwitchSceneReferences[i].scene));

                    if (exists == null)
                    {
                        all.Add(SwitchSceneReferences[i]);
                    }
                }
            }

            return all;
        }

        public List<SwitchSceneTriggerID> GetSwitchTriggerSceneReferences(string sceneName)
        {
            List<SwitchSceneTriggerID> all = new List<SwitchSceneTriggerID>();

            for (int i = 0; i < SwitchSceneReferences.Count; i++)
            {
                if (SwitchSceneReferences[i].scene.Equals(sceneName))
                {
                    all.Add(SwitchSceneReferences[i]);
                }
            }

            return all;
        }

        [System.Serializable]
        public class SwitchSceneTriggerID
        {
            public string id;
            public string scene;
            public string load;
            public string spawnPoint;
            public string view = "_Front";
            [HideInInspector]
            public string triggerObjectName;
        }

        [System.Serializable]
        public class UniqueIDObject
        {
            public string id;
            public string sceneName;
            public string type;
        }

        [System.Serializable]
        public class Tooltip
        {
            public string id;
            public string tooltip;

            public Tooltip(string id, string tooltip)
            {
                this.id = id;
                this.tooltip = tooltip;
            }
        }

        [System.Serializable]
        public class IOObjectHandler
        {
            public string referenceID = "";
            public string nameGO = "";

            public string prefix = "";
            public bool controlledByUserType = false;
            public bool adminOnly = false;
            public List<string> userTypes;

            public void Update(UniqueID.IObjectSetting baseSetting)
            {
                adminOnly = baseSetting.adminOnly;
                controlledByUserType = baseSetting.controlledByUserType;
                prefix = baseSetting.prefix;
                userTypes = baseSetting.userTypes;
            }
        }

        [System.Serializable]
        public class IOObjectTeleportHandler : IOObjectHandler
        {
            public TeleportPoint.TeleportIOObject settings;
        }

        [System.Serializable]
        public class IOObjectDoorHandler : IOObjectHandler
        {
            public Door.DoorIOObject settings;
        }

        [System.Serializable]
        public class IOObjectLockHandler : IOObjectHandler
        {
            public Lock.LockIOObject settings;
        }

        [System.Serializable]
        public class IOObjectItemHandler : IOObjectHandler
        {
            public PickupItem.ItemIOObject settings;
        }

        [System.Serializable]
        public class IOObjectPopupHandler : IOObjectHandler
        {
            public PopupTag.PopupTagIOObject settings;
        }

        [System.Serializable]
        public class IOObjectMascotHandler : IOObjectHandler
        {
            public Mascot.MascotIOObject settings;
        }

        [System.Serializable]
        public class IOObjectVideoScreenHandler : IOObjectHandler
        {
            public VideoScreen.VideoScreenIOObject settings;
        }

        [System.Serializable]
        public class IOObjectWorldUploadHandler : IOObjectHandler
        {
            public WorldContentUpload.WorldUploadIOObject settings;
            public ContentsManager.ContentType type;
        }

        [System.Serializable]
        public class IOObjectConfiguratorHandler : IOObjectHandler
        {
            public Configurator.ConfiguratorIOObject settings;
            public ConfiguratorManager.ConfiguratorType type;
        }

        [System.Serializable]
        public class IOObjectChairGroupHandler : IOObjectHandler
        {
            public ChairGroup.ChairGroupIOObject settings;
            public ConferenceChairGroup.ConferenceChairGroupIOObject conferenceSettings;
            public string type = "";
            public List<ChairGroup.ChairStreamCache> streamCache;
        }

        [System.Serializable]
        public class IOObjectProductHandler : IOObjectHandler
        {
            public Product.ProductIOObject settings;
        }

        [System.Serializable]
        public class IOObjectProductPlacementHandler : IOObjectHandler
        {
            public ProductPlacement.ProductPlacementIOObject settings;
        }

        [System.Serializable]
        public class IOObjectNPCBotSpawenAreaHandler : IOObjectHandler
        {
            public NPCBotSpawnArea.NPCBotSpawnAreaIOObject settings;
        }

        [System.Serializable]
        public class IOObjectNoticeboardHandler : IOObjectHandler
        {
            public NoticeBoard.NoticeBoardIOObject settings;
        }

        [System.Serializable]
        public class IOObjectVehcileSpawnHandler : IOObjectHandler
        {
            public VehicleSpawn.VehicleSpawnIOObject settings;
        }
    }
}
