using UnityEngine;

#if BRANDLAB360_INTERNAL
using BrandLab360.Internal;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace BrandLab360
{
    public class MMOView : MonoBehaviour
    {
        private bool hasOwnership = false;
        private IPlayer currentOwner;

#if BRANDLAB360_INTERNAL
        private PhotonViewer m_photon;

        private PhotonViewer M_PhotonViewer
        {
            get
            {
                if (m_photon == null)
                {
                    m_photon = GetComponent<PhotonViewer>();
                }

                return m_photon;
            }
        }
#endif

        private ColyseusViewer m_colyseus;

        private ColyseusViewer M_ColyseusViewer
        {
            get
            {
                if (m_colyseus == null)
                {
                    m_colyseus = GetComponent<ColyseusViewer>();
                }

                return m_colyseus;
            }
        }

        public void TakeOwnership(IPlayer sender)
        {
            if (sender != Owner)
            {
                Owner = sender;
                IsMine = true;
            }
            else
            {
                Owner = sender;
                IsMine = false;
            }
        }

        public int ViewID
        {
            get
            {
                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
#if BRANDLAB360_INTERNAL
                    if (M_PhotonViewer != null)
                    {
                        return M_PhotonViewer.ViewID;
                    }
#endif

                }
                else
                {
                    if (M_ColyseusViewer != null)
                    {
                        return M_ColyseusViewer.ViewID;
                    }
                }

                return -1;
            }
        }

        public string ID
        {
            get
            {
                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
#if BRANDLAB360_INTERNAL
                    if (M_PhotonViewer != null)
                    {
                        return M_PhotonViewer.ID;
                    }
#endif

                }
                else
                {
                    if (M_ColyseusViewer != null)
                    {
                        return M_ColyseusViewer.ID;
                    }
                }

                return "";
            }
        }

        public IPlayer Owner
        {
            get
            {
                return currentOwner;
            }
            set
            {
                currentOwner = value;
            }
        }

        public string Nickname
        {
            get
            {
                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
#if BRANDLAB360_INTERNAL
                    if (M_PhotonViewer != null)
                    {
                        return M_PhotonViewer.Nickname;
                    }
#endif

                }
                else
                {
                    if (M_ColyseusViewer != null)
                    {
                        return M_ColyseusViewer.Nickname;
                    }
                }

                return "";
            }
        }

        public int Actor
        {
            get
            {
                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
#if BRANDLAB360_INTERNAL
                    if (M_PhotonViewer != null)
                    {
                        return M_PhotonViewer.Actor;
                    }
#endif

                }
                else
                {
                    if (M_ColyseusViewer != null)
                    {
                        return M_ColyseusViewer.Actor;
                    }
                }

                return -1;
            }
        }

        public bool IsMine
        {
            get
            {
                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
#if BRANDLAB360_INTERNAL
                    if (M_PhotonViewer != null)
                    {
                        return M_PhotonViewer.IsMine;
                    }
#endif

                }
                else
                {
                    if (M_ColyseusViewer != null)
                    {
                        return M_ColyseusViewer.IsMine;
                    }
                }

                return hasOwnership;
            }
            set
            {
                hasOwnership = value;
            }
        }

        public GameObject GO
        {
            get
            {
                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
#if BRANDLAB360_INTERNAL
                    if (M_PhotonViewer != null)
                    {
                        return M_PhotonViewer.ViewGO;
                    }
#endif

                }
                else
                {
                    if (M_ColyseusViewer != null)
                    {
                        return M_ColyseusViewer.ViewGO;
                    }
                }

                return gameObject;
            }
        }


        public void RemoveNetworkComponents()
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (M_PhotonViewer != null)
                {
                    M_PhotonViewer.RemoveNetworkComponents();
                }
#endif

            }
            else
            {
                if (M_ColyseusViewer != null)
                {
                    M_ColyseusViewer.RemoveNetworkComponents();
                }
            }
        }

        private void Awake()
        {
            if (!AppManager.IsCreated) return;

            if (GetComponent<IPlayer>() != null) currentOwner = GetComponent<IPlayer>();

            if(AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online))
            {
                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
#if BRANDLAB360_INTERNAL
                    m_photon = gameObject.AddComponent<PhotonViewer>();
#endif

                }
                else
                {
                    m_colyseus = gameObject.AddComponent<ColyseusViewer>();
                }
            }
        }

        /// <summary>
        /// Send RPC wrapper
        /// </summary>
        public void RPC(string methodName, MMOManager.RpcTarget target, params object[] parameters)
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (m_photon != null)
                {
                    m_photon.RPC(methodName, (int)target, parameters);
                }
#endif

            }
            else
            {
                if (m_colyseus != null)
                {
                    MMOManager.Instance.ColyseusManager_Ref.RPC(methodName, (int)target, parameters);
                }
            }
        }

        /// <summary>
        /// Send RPC wrapper
        /// </summary>
        public void RPC(string methodName, IPlayer targetPlayer, params object[] parameters)
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (m_photon != null)
                {
                    m_photon.RPC(methodName, targetPlayer.ID, parameters);
                }
#endif

            }
            else
            {
                if (m_colyseus != null)
                {
                    MMOManager.Instance.ColyseusManager_Ref.RPC(methodName, targetPlayer.ID, parameters);
                }
            }
        }

        /// <summary>
        /// Send RPC wrapper
        /// </summary>
        public void RPC(string methodName, int RPCtarget, params object[] parameters)
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (m_photon != null)
                {
                    m_photon.RPC(methodName, RPCtarget, parameters);
                }
#endif

            }
            else
            {
                if (m_colyseus != null)
                {
                    MMOManager.Instance.ColyseusManager_Ref.RPC(methodName, RPCtarget, parameters);
                }
            }
        }

        public void RequestOwnership()
        {
            if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
            {
#if BRANDLAB360_INTERNAL
                if (m_photon != null)
                {
                    m_photon.RequestOwnership();
                }
#endif
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(MMOView), true)]
        public class MMOView_Editor : BaseInspectorEditor
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
