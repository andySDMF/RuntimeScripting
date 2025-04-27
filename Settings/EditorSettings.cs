using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrandLab360
{
    [System.Serializable]
    public class EditorTools
    {
        [Header("Simulator")]
        public string simulator = "_BRANDLAB360_SIMULATOR";
        public bool createWebClientSimulator = false;
        public bool simulateMobile = false;
        public bool simulateAdmin = false;
        public bool simulateURLParams = false;
        public OrientationType simulateOrientation = OrientationType.landscape;
        public Vector2 simulateScreenSize = new Vector2(2340, 1080);

        [Header("OnPlay")]
        public bool ignoreIntroScene = false;

        [Header("Upload")]
        public string pureWebClientID = "1b5ce8ae21d62137fe529e92";
        public string pureWebClientSecret = "247995f7e3fe0fcc79e7dd8d835845bdddb965b02a2bfa72";

        public string furioosAccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VybmFtZSI6Ilh4N3RmSjV5UXN4WHc0Rm9XIiwiYXBpVG9rZW4iOiJleUpoYkdjaU9pSklVekkxTmlJc0luUjVjQ0k2SWtwWFZDSjkuZXlKMWMyVnlTVVFpT2lKRGVubE1ObTFqZFVaMWRIUkhOVlIwU3lJc0luQmhjM04zYjNKa0lqb2lKREppSkRFd0pFbHFXV1JPYms1WlV6WXpVVkJFWW1GeUxsVTNORTlDY2t4bGVHazRURFpvVEZSeVZ6WTBRMk5tWjFscldtRnlSVXhYZEZoTElpd2lhV0YwSWpveE5qWXpOamt6TXpFd2ZRLjltOUdLRGxGQ296RTdtUjZWS3l4cmx3TGd6dzdVNU4zQmxjR0haR2tpanciLCJpYXQiOjE2NjM2OTMzMTB9.zS7Ydm13ertDAG0DEqVMuCzhdcafYb-fdYyBLLlBaDQ";
        public string furioosApplicationName = "";
        public string furioosApplicationDescription = "";
        public string furioosApplicationID = "";
        public string furioosThumbnail = "";
        public int furioosVirtualMachineSelected = 0;
        public int furioosQualitySelected = 1;
        public int furioosRatioModeSelected = 0;
        public int furioosFixedRatioPresetSelected = 0;
        public string furioosRatio = "";
        public bool furioosConvertTouch = true;

#if UNITY_EDITOR
    public bool furioosEditorPreviewThumbnail = false;
    public bool furioosValidation = false;

    public int assortmentIndexAccumulator = 0;

    [Header("Ready Player Me")]
    public string rpmURLSetting = "";
    public string rpmAvatarNameSetting = "";
    public bool rpmSaveAssetSetting = true;
    public bool rpmAddToSceneSetting = true;
#endif
    }
}
