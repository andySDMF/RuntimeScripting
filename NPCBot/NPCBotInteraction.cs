using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(NPCBot))]
    [RequireComponent(typeof(NPCBotFollow))]
    public class NPCBotInteraction : MonoBehaviour
    {
        public BotInteractionType interactionType = BotInteractionType.FollowPlayer;

        [Header("Animations")]
        [SerializeField]
        private BotAnimationEmoteMode emoteMode = BotAnimationEmoteMode.Random;
        [SerializeField]
        [Range(0, 7)]
        private int emoteAnimation = 0;

        [Header("Audio")]
        [SerializeField]
        private AudioClip[] talkAudio;

        [Header("Speech")]
        [SerializeField]
        private TextMeshProUGUI speechText;
        [SerializeField]
        private CanvasGroup speechFader;
        [SerializeField]
        private bool useOpenAI = true;
        [SerializeField]
        private bool fixedAIQuastions = false;
        [SerializeField]
        private NPCBotQuestions.BotQuestion[] openAIQuestions;
        [SerializeField]
        private string[] talkSpeech;

        private NPCBot m_bot;
        private NPCBotFollow m_follow;
        private AudioSource m_audio;
        private Vector3 m_originRotation;
        private float m_clipLength;
        private NPCBotQuestions m_questionList;

#if UNITY_EDITOR
        public TextMeshProUGUI SpeechText
        {
            get
            {
                return speechText;
            }
            set
            {
                speechText = value;
            }
        }

        public CanvasGroup SpeechFader
        {
            get
            {
                return speechFader;
            }
            set
            {
                speechFader = value;
            }
        }
#endif

        private void Start()
        {
            if (m_bot == null)
            {
                m_bot = GetComponent<NPCBot>();
            }

            m_bot.OnInteract += OnInteract;

            m_follow = GetComponent<NPCBotFollow>();

            m_audio = gameObject.AddComponent<AudioSource>();
            m_audio.playOnAwake = false;

            if (speechFader != null)
            {
                speechFader.alpha = 0.0f;
            }

            m_originRotation = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, transform.localEulerAngles.z);
        }

        public virtual void OnInteract(bool state)
        {
            //need to RPC this to other players eventually
            StopAllCoroutines();

            switch (interactionType)
            {
                case BotInteractionType.PlayAnimation:
                    // Play animation on the local client

                    m_follow.ForceLookAt(PlayerManager.Instance.GetLocalPlayer().TransformObject);

                    int randAni = emoteAnimation;

                    if (emoteMode.Equals(BotAnimationEmoteMode.Random))
                    {
                        randAni = Random.Range(0, 6);
                    }

                    StopAllCoroutines();

                    m_bot.Ani.SetBool("Emote", true);
                    m_bot.Ani.SetFloat("EmoteId", randAni);
                    AnimatorClipInfo[] clipInfos = m_bot.Ani.GetCurrentAnimatorClipInfo(0);
                    m_clipLength = clipInfos[0].clip.length;
                    StartCoroutine(Timer());

                    break;
                case BotInteractionType.Audio:
                    m_follow.ForceLookAt(PlayerManager.Instance.GetLocalPlayer().TransformObject);

                    m_audio.Stop();
                    int randAudio = Random.Range(0, talkAudio.Length);
                    m_audio.clip = talkAudio[randAudio];
                    AudioSource.PlayClipAtPoint(m_audio.clip, transform.position);
                    m_clipLength = m_audio.clip.length;
                    StartCoroutine(Timer());

                    break;
                case BotInteractionType.Speech:
                    if (useOpenAI)
                    {
                        m_follow.ForceLookAt(PlayerManager.Instance.GetLocalPlayer().TransformObject, true);

                        if (fixedAIQuastions)
                        {
                            if (m_questionList == null)
                            {
                                UnityEngine.Object prefab = Resources.Load("NPC/NPCCanvas_Questions");

                                if (prefab != null)
                                {
                                    m_questionList = ((GameObject)Instantiate(prefab, Vector3.up, Quaternion.identity, transform)).gameObject.GetComponent<NPCBotQuestions>();
                                    m_questionList.transform.localPosition = Vector3.up;
                                    m_questionList.transform.localScale = Vector3.one;
                                    m_questionList.transform.eulerAngles = Vector3.zero;
                                }
                            }

                            if (m_questionList != null)
                            {
                                m_questionList.SetQuestions(openAIQuestions, AskQuestion);
                            }
                        }
                        else
                        {
                            //need to open the Question pop up
                            HUDManager.Instance.ToggleHUDMessage("OPENAIQUESTION_MESSAGE", true);
                            HUDManager.Instance.GetHUDMessageObject("OPENAIQUESTION_MESSAGE").GetComponent<OpenAIQuestion>().SetCallback(AskQuestion);
                        }
                    }
                    else
                    {
                        m_follow.ForceLookAt(PlayerManager.Instance.GetLocalPlayer().TransformObject);
                        StartCoroutine(ApplySpeech());
                        int randSpeech = Random.Range(0, talkSpeech.Length);
                        speechText.text = talkSpeech[randSpeech];
                    }

                    break;
                default:
                    if (state)
                    {
                        m_follow.Follow(PlayerManager.Instance.GetLocalPlayer().TransformObject, CoreManager.Instance.playerSettings.walkSpeed);
                    }
                    else
                    {
                        m_follow.Stop();
                    }
                    break;
            }
        }

        private void AskQuestion(string question)
        {
            if (string.IsNullOrEmpty(question))
            {
                if (NPCManager.Instance.IsInstantiatedBot(gameObject))
                {
                    m_follow.ForceLookAt(PlayerManager.Instance.GetLocalPlayer().TransformObject, false);
                    m_bot.ReEngageBot();
                }
            }

            OpenAiAPI.Instance.CompletionRequest(question, ProcessQuestionCallback);
        }

        private void AskQuestion(int question)
        {
            List<CompletionMessage> messages = new List<CompletionMessage>();
            messages.Add(new CompletionMessage(OpenAIRole.system.ToString(), "You are a helpful assistant."));
            messages.Add(new CompletionMessage(OpenAIRole.user.ToString(), openAIQuestions[question].question));
            messages.Add(new CompletionMessage(OpenAIRole.assistant.ToString(), openAIQuestions[question].response));

            OpenAiAPI.Instance.CompletionRequest(messages, ProcessQuestionCallback);
        }

        private void ProcessQuestionCallback(List<CompletionChoice> data)
        {
            if (data == null || data.Count <= 0)
            {
                if (NPCManager.Instance.IsInstantiatedBot(gameObject))
                {
                    m_bot.ReEngageBot();
                }

                return;
            }
            StartCoroutine(ApplySpeech());

            //need to set the speech
            int rand = UnityEngine.Random.Range(0, data.Count);
            speechText.text = data[rand].message.content;
        }

        private IEnumerator ApplySpeech()
        {
            while (speechFader.alpha < 1.0f)
            {
                speechFader.alpha += Time.deltaTime;

                yield return null;
            }

            m_clipLength = 5.0f;
            yield return StartCoroutine(Timer());

            while (speechFader.alpha > 0.0f)
            {
                speechFader.alpha -= Time.deltaTime;

                yield return null;
            }

            speechText.text = "";

            if (NPCManager.Instance.IsInstantiatedBot(gameObject))
            {
                m_follow.ForceLookAt(PlayerManager.Instance.GetLocalPlayer().TransformObject, false);
                m_bot.ReEngageBot();
            }
        }

        private IEnumerator Timer()
        {
            float time = 0.0f;

            while (time < m_clipLength)
            {
                time += Time.deltaTime;

                yield return null;
            }

            for (int i = 0; i < m_bot.Ani.parameterCount; i++)
            {
                if (m_bot.Ani.parameters[i].name.Equals("Emote"))
                {
                    m_bot.Ani.SetBool("Emote", false);
                    break;
                }
            }

            transform.localEulerAngles = m_originRotation;
        }

        [System.Serializable]
        public enum BotInteractionType { FollowPlayer, PlayAnimation, Audio, Speech }

        [System.Serializable]
        private enum BotAnimationEmoteMode { Random, Fixed }

#if UNITY_EDITOR
        [CustomEditor(typeof(NPCBotInteraction), true), CanEditMultipleObjects]
        public class NPCBotInteraction_Editor : BaseInspectorEditor
        {
            private NPCBotInteraction interactionScript;

            private void OnEnable()
            {
                GetBanner();
                interactionScript = (NPCBotInteraction)target;
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("interactionType"), true);

                if (interactionScript.interactionType.Equals(BotInteractionType.PlayAnimation))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("emoteMode"), true);

                    if (interactionScript.emoteMode.Equals(BotAnimationEmoteMode.Fixed))
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("emoteAnimation"), true);
                    }

                }
                else if (interactionScript.interactionType.Equals(BotInteractionType.Audio))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("talkAudio"), true);
                }
                else if (interactionScript.interactionType.Equals(BotInteractionType.Speech))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("speechText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("speechFader"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("useOpenAI"), true);

                    if (interactionScript.useOpenAI)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("fixedAIQuastions"), true);

                        if (interactionScript.fixedAIQuastions)
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("openAIQuestions"), true);
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("talkSpeech"), true);
                    }
                }
                else
                {

                }

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(interactionScript);
                }
            }
        }
#endif
    }
}
