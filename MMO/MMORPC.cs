using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class MMORPC : MonoBehaviour
    {
        public static List<RPCMethod> RPCMethods = new List<RPCMethod>();

        private void Awake()
        {
            #region Test
            RPCMethods.Add(new RPCMethod("Test", new string[6] { "vector", "float", "int", "string", "bool", "array" }));
            RPCMethods.Add(new RPCMethod("Test2", new string[5] { "float", "int", "string", "bool", "vector" }));
            #endregion

            #region TranformSync
            RPCMethods.Add(new RPCMethod("MMOTransformSync", new string[6] { "transformID", "owner", "pos", "rot", "scale", "data" }));
            RPCMethods.Add(new RPCMethod("MMOTransformRigidbodySync", new string[3] { "transformID", "pos", "rot" }));
            RPCMethods.Add(new RPCMethod("MMOTransformOwnership", new string[2] { "transformID", "owner" }));
            #endregion

            #region Products
            RPCMethods.Add(new RPCMethod("AddProduct", new string[6] { "localPosition", "assortmentIndex", "insertID", "collection", "ppID", "shop" }));
            RPCMethods.Add(new RPCMethod("UpdateProduct", new string[6] { "localPosition", "assortmentIndex", "insertID", "collection", "ppID", "shop" }));
            RPCMethods.Add(new RPCMethod("RemoveProduct", new string[3] { "productCode", "assortmentIndex", "insertID" }));
            RPCMethods.Add(new RPCMethod("RequestMasterAddProduct", new string[6] { "productCode", "localPosition", "assortmentIndex", "collection", "ppID", "shop" }));
            #endregion

            #region ProductPlacement
            RPCMethods.Add(new RPCMethod("UpdateProductPositionPlacement", new string[3] { "placementID", "productCode", "localPosition" }));
            RPCMethods.Add(new RPCMethod("UpdateProductScalePlacement", new string[3] { "placementID", "productCode", "localScale" }));
            RPCMethods.Add(new RPCMethod("RemoveProductPlacement", new string[2] { "placementID", "productCode" }));
            RPCMethods.Add(new RPCMethod("AddProductPlacement", new string[2] { "placementID", "product" }));
            RPCMethods.Add(new RPCMethod("GetProductPlacementContents", new string[1] { "placementID" }));
            #endregion

            #region Chat
            RPCMethods.Add(new RPCMethod("SendVoiceCallRequest", new string[2] { "from", "to" }));
            RPCMethods.Add(new RPCMethod("VoiceCallAccepted", new string[2] { "from", "to" }));
            RPCMethods.Add(new RPCMethod("VoiceCallDeclined", new string[2] { "from", "to" }));
            RPCMethods.Add(new RPCMethod("VoiceCallEnded", new string[2] { "from", "to" }));
            #endregion

            #region Bots
            RPCMethods.Add(new RPCMethod("BotControl", new string[3] { "state", "bot", "path" }));
            #endregion

            #region Contents
            RPCMethods.Add(new RPCMethod("GetContentUpdates", new string[0] { }));
            #endregion

            #region Configurator
            RPCMethods.Add(new RPCMethod("UpdateConfiguratorTransform", new string[4] { "id", "pos", "rot", "scale" }));
            #endregion

            #region Conference
            RPCMethods.Add(new RPCMethod("ConferenceContentControls", new string[6] { "from", "to", "conferenceID", "contentType", "state", "data" }));
            RPCMethods.Add(new RPCMethod("RequestConferenceImageSettings", new string[3] { "from", "to", "contentID" }));
            RPCMethods.Add(new RPCMethod("AttainConferenceImageSettings", new string[4] { "from", "to", "contentID", "data" }));
            RPCMethods.Add(new RPCMethod("RequestConferenceVideoSettings", new string[3] { "from", "to", "contentID" }));
            RPCMethods.Add(new RPCMethod("AttainConferenceVideoSettings", new string[4] { "from", "to", "contentID", "data" }));
            #endregion

            #region Items
            RPCMethods.Add(new RPCMethod("SetAllInstantiatedItems", new string[1] { "json" }));
            #endregion

            #region Invites
            RPCMethods.Add(new RPCMethod("SendInvitation", new string[3] { "sender", "type", "inviteCode" }));
            #endregion

            #region Floorplan Items
            RPCMethods.Add(new RPCMethod("AddFloorplanItem", new string[1] { "json" }));
            RPCMethods.Add(new RPCMethod("UpdateFloorplanItem", new string[4] { "item", "pos", "rot", "scale" }));
            RPCMethods.Add(new RPCMethod("RemoveFloorplanItem", new string[1] { "item" }));
            #endregion

            #region Reports
            RPCMethods.Add(new RPCMethod("GetReports", new string[0] { }));
            #endregion

            #region Likes
            RPCMethods.Add(new RPCMethod("PostLike", new string[0] { }));
            #endregion

            #region Freinds
            RPCMethods.Add(new RPCMethod("FriendRequest", new string[2] { "to", "friend" }));
            RPCMethods.Add(new RPCMethod("FriendRequestResponse", new string[3] { "to", "friend", "state" }));
            #endregion

            #region NoticeBoards
            RPCMethods.Add(new RPCMethod("AddNotice", new string[1] { "json" }));
            RPCMethods.Add(new RPCMethod("DeleteNotice", new string[1] { "noticeID" }));
            RPCMethods.Add(new RPCMethod("EditNotice", new string[1] { "json" }));
            RPCMethods.Add(new RPCMethod("SyncNoticeTransform", new string[3] { "id", "localPosition", "scale" }));
            #endregion

            #region Emotes
            RPCMethods.Add(new RPCMethod("SendEmote", new string[2] { "id", "playerID" }));
            RPCMethods.Add(new RPCMethod("SendEmoji", new string[2] { "id", "playerID" }));
            #endregion

            #region Games
            RPCMethods.Add(new RPCMethod("NetworkGameSettings", new string[3] { "id", "playerID", "state" }));
            RPCMethods.Add(new RPCMethod("SetUpNetwork", new string[2] { "gameId", "isNetwork" }));
            RPCMethods.Add(new RPCMethod("PlayerJoinGame", new string[1] { "gameId" }));
            RPCMethods.Add(new RPCMethod("PlayerLeaveGame", new string[1] { "gameId" }));
            RPCMethods.Add(new RPCMethod("SendMessageOnActiveGame", new string[3] { "gameId", "method", "args" }));
            RPCMethods.Add(new RPCMethod("TakeOwnership", new string[2] { "senderId", "viewId" }));
            #endregion
        }

        public static void OnRPCRecieved(Hashtable data)
        {
            IPlayer iPlayer = null;

            switch (data["RPC"].ToString())
            {
                #region TranformSync
                case "MMOTransformOwnership":

                    for (int i = 0; i < MMOManager.Instance.TransformSyncObjects.Count; i++)
                    {
                        if (MMOManager.Instance.TransformSyncObjects[i].ID.Equals(data["transformID"].ToString()))
                        {
                            MMOManager.Instance.TransformSyncObjects[i].SyncTransferOwnership(data["owner"].ToString());
                            break;
                        }
                    }
                    break;
                case "MMOTransformSync":

                    for (int i = 0; i < MMOManager.Instance.TransformSyncObjects.Count; i++)
                    {
                        if (MMOManager.Instance.TransformSyncObjects[i].ID.Equals(data["transformID"].ToString()))
                        {
                            MMOManager.Instance.TransformSyncObjects[i].SyncTransferOwnership(data["owner"].ToString());
                            MMOManager.Instance.TransformSyncObjects[i].Sync((Vector3)data["pos"], (Vector3)data["rot"], (Vector3)data["scale"], data["data"].ToString());
                            break;
                        }
                    }
                    break;
                case "MMOTransformRigidbodySync":

                    for (int i = 0; i < MMOManager.Instance.TransformSyncObjects.Count; i++)
                    {
                        if (MMOManager.Instance.TransformSyncObjects[i].ID.Equals(data["transformID"].ToString()))
                        {
                            MMOManager.Instance.TransformSyncObjects[i].SyncRigidbody((Vector3)data["pos"], (Vector3)data["rot"]);
                            break;
                        }
                    }
                    break;
                #endregion

                #region Products
                case "AddProduct":

                    AssortmentManager.Instance.RemoteAddToAssortment(data["productCode"].ToString(), (int)data["assortmentIndex"], (Vector3)data["localPosition"], (int)data["insertID"], data["collection"].ToString(), data["ppID"].ToString(), data["shop"].ToString());
                    AssortmentSync.Instance.StoreLocalInsertID((int)data["insertID"]);

                    break;
                case "UpdateProduct":

                    ProductManager.Instance.RemoteUpdateProduct(data["productCode"].ToString(), (Vector3)data["localPosition"], (int)data["assortmentIndex"], (int)data["insertID"]);

                    if (MMOManager.Instance.IsMasterClient())
                    {
                        AssortmentSync.Instance.MasterUpdateProduct(data["productCode"].ToString(), (Vector3)data["localPosition"], (int)data["assortmentIndex"], (int)data["insertID"], data["collection"].ToString(), data["ppID"].ToString(), data["shop"].ToString());
                    }

                    break;
                case "RemoveProduct":

                    AssortmentManager.Instance.RemoteRemoveFromAssortment(data["productCode"].ToString(), (int)data["assortmentIndex"], (int)data["insertID"]);

                    if (MMOManager.Instance.IsMasterClient())
                    {
                        AssortmentSync.Instance.MasterRemoveProduct(data["productCode"].ToString(), (int)data["assortmentIndex"], (int)data["insertID"]);
                    }

                    break;
                case "RequestMasterAddProduct":

                    if (MMOManager.Instance.IsMasterClient())
                    {
                        AssortmentSync.Instance.SyncAddProduct(data["productCode"].ToString(), (Vector3)data["localPosition"], (int)data["assortmentIndex"], data["collection"].ToString(), data["ppID"].ToString(), data["shop"].ToString());
                    }

                    break;
                #endregion

                #region Product Placement
                case "UpdateProductPositionPlacement":

                    ProductPlacementManager.Instance.RemotePositionProductPlacement(data["placementID"].ToString(), (int)data["productCode"], (Vector3)data["localPosition"]);

                    break;
                case "UpdateProductScalePlacement":

                    ProductPlacementManager.Instance.RemoteScaleProductPlacement(data["placementID"].ToString(), (int)data["productCode"], (Vector3)data["localScale"]);

                    break;
                case "RemoveProductPlacement":

                    ProductPlacementManager.Instance.RemoteRemoveProductPlacement(data["placementID"].ToString(), (int)data["productCode"]);

                    break;
                case "AddProductPlacement":

                    ProductPlacementManager.Instance.RemoteAddProductPlacement(data["placementID"].ToString(), data["product"].ToString());

                    break;
                case "GetProductPlacementContents":

                    ProductAPI.Instance.GetProductsForPlacement(data["placementID"].ToString());

                    break;
                #endregion

                #region Chat
                case "SendVoiceCallRequest":

                    if (data["to"].ToString().Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                    {
                        MMOChat.Instance.SwitchCall(data["from"].ToString(), true);
                    }

                    break;
                case "VoiceCallAccepted":

                    if (data["to"].ToString().Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                    {
                        MMOChat.Instance.AcceptVoiceCall(data["from"].ToString(), false);
                    }

                    break;
                case "VoiceCallDeclined":

                    if (data["to"].ToString().Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                    {
                        MMOChat.Instance.DeclineVoiceCall(data["from"].ToString(), false);
                    }

                    break;
                case "VoiceCallEnded":

                    if (data["to"].ToString().Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                    {
                        MMOChat.Instance.EndVoiceCall(data["from"].ToString(), false);
                    }

                    break;
                #endregion

                #region BOTS
                case "BotControl":

                    if (!MMOManager.Instance.IsMasterClient())
                    {
                        NPCManager.Instance.SyncBot(data["bot"].ToString(), data["state"].ToString(), data["path"].ToString());
                    }

                    break;
                #endregion

                #region Contents
                case "GetContentUpdates":

                    ContentsAPI.Instance.GetContents();

                    break;
                #endregion

                #region Configurator
                case "UpdateConfiguratorTransform":

                    Configurator[] objs = FindObjectsByType<Configurator>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                    for (int i = 0; i < objs.Length; i++)
                    {
                        if (objs[i].ID.Equals(data["id"].ToString()))
                        {
                            objs[i].NetworkSync((Vector3)data["pos"], (Quaternion)data["rot"], (Vector3)data["scale"]);
                            break;
                        }
                    }

                    break;
                #endregion

                #region Conference
                case "ConferenceContentControls":

                    ConferenceContentUpload.PlayersConferenceWrapper wrapper = JsonUtility.FromJson<ConferenceContentUpload.PlayersConferenceWrapper>(data["to"].ToString());

                    if (wrapper != null)
                    {
                        if (wrapper.players.Contains(PlayerManager.Instance.GetLocalPlayer().ID) || data["from"].ToString().Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                        {
                            ContentsManager.Instance.NetworkScreenEvent(data["conferenceID"].ToString(), (int)data["contentType"], data["state"].ToString(), data["data"].ToString());
                        }
                    }

                    break;
                case "RequestConferenceImageSettings":

                    if (data["to"].ToString().Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                    {
                        ContentImageScreen[] cLoaders = FindObjectsByType<ContentImageScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                        for (int i = 0; i < cLoaders.Length; i++)
                        {
                            if (cLoaders[i].ID.Equals(data["contentID"].ToString()))
                            {
                                MMOManager.Instance.SendRPC("AttainConferenceImageSettings", (int)MMOManager.RpcTarget.All, data["to"].ToString(), data["from"].ToString(), cLoaders[i].ID, cLoaders[i].GetSettings());
                                break;
                            }
                        }
                    }

                    break;
                case "AttainConferenceImageSettings":

                    if (data["to"].ToString().Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                    {
                        ContentImageScreen[] cLoaders = FindObjectsByType<ContentImageScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                        for (int i = 0; i < cLoaders.Length; i++)
                        {
                            if (cLoaders[i].ID.Equals(data["contentID"].ToString()))
                            {
                                cLoaders[i].LocalStateChange("SETTINGS", data["data"].ToString());
                                break;
                            }
                        }
                    }

                    break;
                case "RequestConferenceVideoSettings":

                    if (data["to"].ToString().Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                    {
                        ContentVideoScreen[] cLoaders = FindObjectsByType<ContentVideoScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                        for (int i = 0; i < cLoaders.Length; i++)
                        {
                            if (cLoaders[i].ID.Equals(data["contentID"].ToString()))
                            {
                                MMOManager.Instance.SendRPC("AttainConferenceVideoSettings", (int)MMOManager.RpcTarget.All, data["to"].ToString(), data["from"].ToString(), cLoaders[i].ID, cLoaders[i].GetSettings());
                                break;
                            }
                        }
                    }

                    break;
                case "AttainConferenceVideoSettings":

                    if (data["to"].ToString().Equals(PlayerManager.Instance.GetLocalPlayer().ID))
                    {
                        ContentImageScreen[] cLoaders = FindObjectsByType<ContentImageScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                        for (int i = 0; i < cLoaders.Length; i++)
                        {
                            if (cLoaders[i].ID.Equals(data["contentID"].ToString()))
                            {
                                cLoaders[i].LocalStateChange("SETTINGS", data["data"].ToString());
                                break;
                            }
                        }
                    }

                    break;
                #endregion

                #region Items
                case "SetAllInstantiatedItems":

                    ItemManager.Instance.InstantiateAllRoomItems(data["json"].ToString());

                    break;
                #endregion

                #region Invites
                case "SendInvitation":

                    InviteAcceptance iAcceptance = HUDManager.Instance.GetHUDMessageObject("INVITE_MESSAGE").GetComponentInChildren<InviteAcceptance>(true);
                    iAcceptance.Set(data["sender"].ToString(), (int)data["type"], data["inviteCode"].ToString());

                    HUDManager.Instance.ToggleHUDMessage("INVITE_MESSAGE", true);

                    break;
                #endregion

                #region Floorplan
                case "AddFloorplanItem":

                    FloorplanManager.FloorplanItem item = JsonUtility.FromJson<FloorplanManager.FloorplanItem>(data["json"].ToString());

                    if (item != null)
                    {
                        FloorplanManager.Instance.InsertFloorplanItem(item);
                    }

                    break;
                case "UpdateFloorplanItem":

                    FloorplanManager.Instance.UpdateFloorplanItem(data["item"].ToString(), (Vector3)data["pos"], (float)data["rot"], (float)data["scale"]);

                    break;
                case "RemoveFloorplanItem":

                    FloorplanManager.Instance.RemoveFloorplanItem(data["item"].ToString());

                    break;
                #endregion

                #region Reports
                case "GetReports":

                    ReportAPI.Instance.GetReports();

                    break;
                #endregion

                #region Reports
                case "PostLike":

                    DataAPI.Instance.GetAll(CoreManager.Instance.ProjectID);

                    break;
                #endregion

                #region Friends
                case "FriendRequest":

                    if (!data["to"].ToString().Equals(AppManager.Instance.Data.NickName)) return;

                    FriendsManager.Instance.RecievedFriendsRequest(data["friend"].ToString());

                    break;
                case "FriendRequestResponse":

                    if (!data["to"].ToString().Equals(AppManager.Instance.Data.NickName)) return;

                    FriendsManager.Instance.RecievedFriendsRequestResponse(data["friend"].ToString(), (int)data["state"]);

                    break;
                #endregion

                #region Notices
                case "AddNotice":

                    var aMeta = NoticeBoardAPI.Instance.CreateNoticeJsonFromData(data["json"].ToString());
                    NoticeBoardManager.Instance.CreateAllNotices(new List<NoticeBoardAPI.NoticeJson>() { aMeta });

                    break;
                case "DeleteNotice":

                    NoticeBoardManager.Instance.DeleteNotice((int)data["noticeID"]);

                    break;
                case "EditNotice":

                    var eMeta = NoticeBoardAPI.Instance.CreateNoticeJsonFromData(data["json"].ToString());
                    NoticeBoardManager.Instance.CreateAllNotices(new List<NoticeBoardAPI.NoticeJson>() { eMeta });

                    break;
                case "SyncNoticeTransform":

                    NoticeBoardManager.Instance.RemoteSyncNoticeTransform((int)data["id"], (Vector3)data["localPosition"], (float)data["scale"]);

                    break;
                #endregion

                #region Games
                case "NetworkGameSettings":

                    GamesManager.Instance.NetworkGameSettings(data["id"].ToString(), data["playerID"].ToString(), (bool)data["state"]);

                    break;

                case "SendMessageOnActiveGame":

                    GamesManager.Instance.SendMessageOnActiveGame(data["gameId"].ToString(), data["method"].ToString(), (string[])data["args"]);

                    break;

                case "SetUpNetwork":
                    GamesManager.Instance.ToggleNetworkOnActiveGame(data["gameId"].ToString(), (bool)data["isNetwork"]);
                    break;

                case "TakeOwnership":
                    GamesManager.Instance.TakeOwnershipOfGameView(data["senderId"].ToString(), data["viewId"].ToString());
                    break;

                case "PlayerJoinGame":
                    GamesManager.Instance.PlayerJoinGame(data["gameId"].ToString());
                    break;

                case "PlayerLeaveGame":
                    GamesManager.Instance.PlayerLeaveGame(data["gameId"].ToString());
                    break;

                #endregion

                #region Emotes
                case "SendEmote":

                    iPlayer = MMOManager.Instance.GetPlayerByUserID(data["playerID"].ToString());

                    if (iPlayer != null)
                    {
                        iPlayer.MainObject.GetComponent<Emotes>().NetworkEmote((float)data["id"]);
                    }

                    break;
                case "SendEmoji":

                    iPlayer = MMOManager.Instance.GetPlayerByUserID(data["playerID"].ToString());

                    if (iPlayer != null)
                    {
                        iPlayer.MainObject.GetComponent<Emotes>().NetworkEmoji((int)data["id"]);
                    }

                    break;
                #endregion
            }
        }

        [System.Serializable]
        public class RPCMethod
        {
            public string method = "";
            public List<string> parameters = new List<string>();

            public RPCMethod(string name, string[] paramNames)
            {
                method = name;

                for (int i = 0; i < paramNames.Length; i++)
                {
                    parameters.Add(paramNames[i]);
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(MMORPC), true)]
        public class MMORPC_Editor : BaseInspectorEditor
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
