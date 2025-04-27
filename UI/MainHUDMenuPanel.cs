using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrandLab360
{
    public class MainHUDMenuPanel : Singleton<MainHUDMenuPanel>
    {
        public static MainHUDMenuPanel Instance
        {
            get
            {
                return ((MainHUDMenuPanel)instance);
            }
            set
            {
                instance = value;
            }
        }

        [SerializeField]
        private RectTransform desktopCenterControl;
        [SerializeField]
        private RectTransform mobileCenterControl;
        [SerializeField]
        private RectTransform mobileCenterControlButton;
        [SerializeField]
        private GameObject toolsPanel;


        public RectTransform MobileButton
        {
            get
            {
                return mobileCenterControlButton;
            }
        }

        private void Update()
        {
            if (AppManager.Instance.Data.IsMobile || AppManager.Instance.Settings.HUDSettings.useMobileToolsForDesktop)
            {
                mobileCenterControlButton.localScale = Vector3.one;

                Transform gPhoneChat = HUDManager.Instance.GetMenuFeature(HUDManager.MenuFeature._Phone).transform;
                gPhoneChat.SetParent(mobileCenterControl);
                gPhoneChat.SetAsFirstSibling();

                Transform gChat = HUDManager.Instance.GetMenuFeature(HUDManager.MenuFeature._Chat).transform;
                gChat.SetParent(mobileCenterControl);
                gChat.SetAsFirstSibling();

                desktopCenterControl.localScale = Vector3.zero;
            }
            else
            {
                mobileCenterControlButton.localScale = Vector3.zero;
            }
        }

        public void TogglePanel(bool isOn)
        {
            toolsPanel.SetActive(isOn);
        }
    }
}
