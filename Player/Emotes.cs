using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Emotes : MonoBehaviour
    {
        [Header("Components")]
        public Animator playerAnimator;
        public MMOPlayer networkPlayer;

        [Header("Animation Notifications")]
        public string[] animationNames = new string[8]
        {"Waving",
        "Shrugging",
        "Laughing",
        "Drinking",
        "Dancing",
        "Clapping",
        "Shaking Hands",
        "Hand Raising"};

        private ParticleSystem m_emojiParticles;

        private void Awake()
        {
            PlayerManager.OnLateUpdate += OnThisLateUpdate;
        }

        private void OnDestroy()
        {
            PlayerManager.OnLateUpdate -= OnThisLateUpdate;
        }

        private void OnThisLateUpdate()
        {
            if (playerAnimator != networkPlayer.animator)
                playerAnimator = networkPlayer.animator;
        }

        public void ActivateEmote(float id)
        {
            if (playerAnimator.GetComponent<EndAnimation>() == null)
            {
                playerAnimator.gameObject.AddComponent<EndAnimation>();
            }

            // Play animation on the local client
            playerAnimator.SetBool("Emote", false);

            StopAllCoroutines();
            StartCoroutine(PlayEmote(id));
        }

        public void ActivateEmoji(int id)
        {
            PlayEmoji(id, true);
        }

        private void PlayEmoji(int id, bool isLocal = true)
        {
            if (m_emojiParticles == null)
            {
                m_emojiParticles = GetComponentInChildren<ParticleSystem>();
            }

            //need to open the particles system on player
            if (m_emojiParticles != null)
            {
                if (m_emojiParticles.isPlaying)
                {
                    m_emojiParticles.Stop();
                }

                //update the material texture
                var croppedTexture = new Texture2D((int)AppManager.Instance.Settings.playerSettings.emoteIcons[id].icon.rect.width, (int)AppManager.Instance.Settings.playerSettings.emoteIcons[id].icon.rect.height, TextureFormat.RGBA32, false);

                var pixels = AppManager.Instance.Settings.playerSettings.emoteIcons[id].icon.texture.GetPixels((int)AppManager.Instance.Settings.playerSettings.emoteIcons[id].icon.textureRect.x,
                                                        (int)AppManager.Instance.Settings.playerSettings.emoteIcons[id].icon.textureRect.y,
                                                        (int)AppManager.Instance.Settings.playerSettings.emoteIcons[id].icon.textureRect.width,
                                                        (int)AppManager.Instance.Settings.playerSettings.emoteIcons[id].icon.textureRect.height);

                croppedTexture.SetPixels(pixels);
                croppedTexture.Apply();

                m_emojiParticles.GetComponent<ParticleSystemRenderer>().material.mainTexture = croppedTexture;
            }

            // Play animation on the local client
            playerAnimator.SetBool("Emote", false);
            StopAllCoroutines();

            //need to open the particles system on player
            if (m_emojiParticles != null)
            {
                m_emojiParticles.Play();
            }

            if (isLocal)
            {
                MMOManager.Instance.SendRPC("SendEmoji", (int)MMOManager.RpcTarget.Others, id, networkPlayer.ID);
            }
        }

        private IEnumerator PlayEmote(float id, bool isLocal = true)
        {
            yield return new WaitForSeconds(0.5f);

            playerAnimator.SetBool("Emote", true);
            playerAnimator.SetFloat("EmoteId", id);

            AnimatorClipInfo[] clipInfos = playerAnimator.GetCurrentAnimatorClipInfo(0);
            string aniName = Regex.Replace(clipInfos[0].clip.name, "([a-z])_?([A-Z])", "$1 $2");

            if(isLocal)
            {
                if((int)id < animationNames.Length)
                {
                    MMOChat.Instance.SendChatMessage("All", "#EVT# " + animationNames[(int)id]);
                }
                else
                {
                    MMOChat.Instance.SendChatMessage("All", "#EVT# " + "Emote Playing");
                }

                MMOManager.Instance.SendRPC("SendEmote", (int)MMOManager.RpcTarget.Others, id, networkPlayer.ID);
            }

            float clipDuration = 0.0f;

            while (clipDuration < playerAnimator.GetCurrentAnimatorStateInfo(0).length)
            {
                clipDuration += Time.deltaTime;

                if(playerAnimator.GetBool("Emote") == false)
                {
                    yield break;
                }

                yield return null;
            }

            playerAnimator.SetBool("Emote", false);
        }

        public void NetworkEmote(float id)
        {
            if (playerAnimator.GetComponent<EndAnimation>() == null)
            {
                playerAnimator.gameObject.AddComponent<EndAnimation>();
            }

            // Play animation on the local client
            playerAnimator.SetBool("Emote", false);

            StopAllCoroutines();
            StartCoroutine(PlayEmote(id, false));
        }
        public void NetworkEmoji(int id)
        {
            PlayEmoji(id, false);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Emotes), true)]
        public class Emotes_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                if (Application.productName.Equals("BL360 Plugin"))
                {
                    serializedObject.Update();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("playerAnimator"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("networkPlayer"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("animationNames"), true);

                    if (GUI.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(this);
                    }
                }
            }
        }
#endif
    }
}
