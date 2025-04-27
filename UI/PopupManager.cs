using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class PopupManager : MonoBehaviour
    {
        public static PopupManager instance;

        private Sprite defaultIcon;
        private float fadeSpeed = 3;
        private float displayTime = 5.0f;

        private TMP_Text popupText;
        private TMP_Text popupTitle;
        private TMP_Text buttonText;
        private TMP_Text popupURL;
        private Image popupImage;
        private AudioSource popupAudio;

        private CanvasGroup hintCanvas;
        private TMP_Text hintText;
        private TMP_Text hintTitle;
        private AudioSource hintAudio;

        private void Awake()
        {
            if (instance == null)
                instance = this;
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if(AppManager.Instance.Settings.HUDSettings.defaultPopupIcon != null)
            {
                defaultIcon = AppManager.Instance.Settings.HUDSettings.defaultPopupIcon;
            }
            
            fadeSpeed = AppManager.Instance.Settings.HUDSettings.popupFadeSpeed;
            displayTime = AppManager.Instance.Settings.HUDSettings.popupDisplayTime;
        }

        /// <summary>
        /// Display a box with a custom message.
        /// </summary>
        /// <param name="message"></param>
        public void ShowPopUp(string title, string message, string buttonMessage, Sprite icon, AudioClip clip = null, string url = "")
        {
            //freeze player
            PlayerManager.Instance.FreezePlayer(true);
            RaycastManager.Instance.CastRay = false;

            if(popupText == null)
            {
                TMP_Text[] all = HUDManager.Instance.GetHUDMessageObject("POPUP_MESSAGE").transform.GetChild(0).GetComponentsInChildren<TMP_Text>(true);
                popupImage = HUDManager.Instance.GetHUDMessageObject("POPUP_MESSAGE").transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>();

                if(defaultIcon == null)
                {
                    defaultIcon = popupImage.sprite;
                }

                if (popupAudio == null)
                {
                    if (HUDManager.Instance.GetHUDMessageObject("POPUP_MESSAGE").GetComponentInChildren<AudioSource>(true) == null)
                    {
                        popupAudio = HUDManager.Instance.GetHUDMessageObject("POPUP_MESSAGE").AddComponent<AudioSource>();
                    }
                    else
                    {
                        popupAudio = HUDManager.Instance.GetHUDMessageObject("POPUP_MESSAGE").GetComponentInChildren<AudioSource>(true);
                    }

                    popupAudio.playOnAwake = false;
                    popupAudio.loop = false;
                }

                for (int i = 0; i < all.Length; i++)
                {
                    if(all[i].name.Contains("Message"))
                    {
                        popupText = all[i];
                    }

                    if (all[i].name.Contains("Title"))
                    {
                        popupTitle = all[i];
                    }

                    if (all[i].name.Contains("button"))
                    {
                        buttonText = all[i];
                    }

                    if (all[i].name.Contains("URLLink"))
                    {
                        popupURL = all[i];
                    }
                }
            }

            if (icon == null)
                popupImage.sprite = defaultIcon;
            else
                popupImage.sprite = icon;

            buttonText.text = string.IsNullOrEmpty(buttonMessage) ? "OK" : buttonMessage;
            popupTitle.text = title;
            popupText.text = message;
            popupAudio.clip = clip;
            popupURL.text = url;

            HUDManager.Instance.ToggleHUDMessage("POPUP_MESSAGE", true);

            if (popupAudio.clip != null)
            {
                popupAudio.Play();
            }

        }


        /// <summary>
        /// Display a hint with a custom message over period of time (icon will be default if null).
        /// if localdisplaytime = 0.0f, global is used
        /// </summary>
        /// <param name="message"></param>
        /// <param name="icon"></param>
        public void ShowHint(string title, string message, float localDisplayTime = 0.0f, AudioClip clip = null)
        {
            //need to get components
            HUDManager.Instance.ToggleHUDMessage("HINT_MESSAGE", true);

            GetHintComponenets();

            hintCanvas.alpha = 1.0f;

            hintTitle.text = title;
            hintText.text = message;
            hintAudio.clip = clip;

            StopAllCoroutines();
            StartCoroutine(PopUpTimer(localDisplayTime));

            if (hintAudio.clip != null)
            {
                hintAudio.Play();
            }
        }

        /// <summary>
        /// Display a constant hint with a custom message  (icon will be default if null)
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="icon"></param>
        public void ShowHint(string title, string message, AudioClip clip = null)
        {
            //need to get components
            HUDManager.Instance.ToggleHUDMessage("HINT_MESSAGE", true);

            GetHintComponenets();

            hintCanvas.alpha = 1.0f;

            hintTitle.text = title;
            hintText.text = message;
            hintAudio.clip = clip;

            if(hintAudio.clip != null)
            {
                hintAudio.Play();
            }
        }

        /// <summary>
        /// Called to hide hint
        /// </summary>
        public void HideHint()
        {
            StopAllCoroutines();
            GetHintComponenets();

            if (hintAudio.isPlaying)
            {
                hintAudio.Stop();
            }

            hintCanvas.alpha = 0.0f;

            //need to get components
            HUDManager.Instance.ToggleHUDMessage("HINT_MESSAGE", false);

        }

        private void GetHintComponenets()
        {
            if (hintText == null)
            {
                hintText = HUDManager.Instance.GetHUDMessageObject("HINT_MESSAGE").transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>();
            }
            if (hintTitle == null)
            {
                hintTitle = HUDManager.Instance.GetHUDMessageObject("HINT_MESSAGE").transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<TMP_Text>();
            }

            if(hintCanvas == null)
            {
                hintCanvas = HUDManager.Instance.GetHUDMessageObject("HINT_MESSAGE").transform.GetChild(0).GetComponent<CanvasGroup>();
            }

            if(hintAudio == null)
            {
                if(HUDManager.Instance.GetHUDMessageObject("HINT_MESSAGE").GetComponentInChildren<AudioSource>(true) == null)
                {
                    hintAudio = HUDManager.Instance.GetHUDMessageObject("HINT_MESSAGE").AddComponent<AudioSource>();
                }
                else
                {
                    hintAudio = HUDManager.Instance.GetHUDMessageObject("HINT_MESSAGE").GetComponentInChildren<AudioSource>(true);
                }

                hintAudio.playOnAwake = false;
                hintAudio.loop = false;
            }
        }

        IEnumerator PopUpTimer(float localDisplayTime = 5.0f)
        {
            hintCanvas.alpha = 0.0f;
            float time = 0.0f;
            float percentage = 0.0f;

            while (percentage < 1)
            {
                time += Time.deltaTime;
                percentage = time / fadeSpeed;
                hintCanvas.alpha = Mathf.Lerp(0, 1, percentage);

                yield return null;
            }

            hintCanvas.alpha = 1.0f;
            float delay = (localDisplayTime <= 0.0f) ? displayTime : localDisplayTime;
            yield return new WaitForSeconds(delay);

            time = 0.0f;
            percentage = 0.0f;

            while (percentage < 1)
            {
                time += Time.deltaTime;
                percentage = time / fadeSpeed;
                hintCanvas.alpha = Mathf.Lerp(1, 0, percentage);

                yield return null;
            }

            hintCanvas.alpha = 0.0f;

            //need to get components
            HUDManager.Instance.ToggleHUDMessage("HINT_MESSAGE", false);

        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PopupManager), true)]
        public class PopupManager_Editor : BaseInspectorEditor
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