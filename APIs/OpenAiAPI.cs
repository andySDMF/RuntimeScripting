using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using Defective.JSON;
using System.Threading.Tasks;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class OpenAiAPI : Singleton<OpenAiAPI>
    {
        public static OpenAiAPI Instance
        {
            get
            {
                return ((OpenAiAPI)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Access")]
        [SerializeField]
        private string ApiKey = "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";

        [Header("API")]
        [SerializeField]
        private string domain = "https://api.openai.com/";
        
        [SerializeField]
        private string version = "v1/chat/";


        [Header("Endpoints")]
        [SerializeField]
        private string completionsEndpoint = "completions";

        [SerializeField]
        private string transcriptEndpoint = "transcriptions";

        /*  [SerializeField]
          private string editsEndpoint = "edits";

          [SerializeField]
          private string transcriptEndpoint = "transcriptions";

          [SerializeField]
          private string imageGenerationEndpoint = "images/generations";

          [SerializeField]
          private string imageEditEndpoint = "images/edits";

          [SerializeField]
          private string imageVariantEndpoint = "images/variations";*/

        public void CompletionRequest(string question, System.Action<List<CompletionChoice>> callback)
        {
            var model = "gpt-3.5-turbo";
            CompletionsData data = new CompletionsData(model, new List<CompletionMessage>() { new CompletionMessage(OpenAIRole.user.ToString(), question) });
            StartCoroutine(ProcessCompletionRequest(data, callback));
        }

        public void CompletionRequest(List<CompletionMessage> messages, System.Action<List<CompletionChoice>> callback)
        {
            var model = "gpt-3.5-turbo";
            CompletionsData data = new CompletionsData(model, messages);
            StartCoroutine(ProcessCompletionRequest(data, callback));
        }

        public Coroutine TranscriptRequest(string absoluteAudioClipPath, System.Action<AudioClip, string> callback)
        {
            TranscriptRequest request = new TranscriptRequest();
            request.file = absoluteAudioClipPath;
            request.model = "whisper-1";

            return StartCoroutine(ProcessTranscriptRequest(request, callback));
        }

        private IEnumerator ProcessCompletionRequest(CompletionsData data, System.Action<List<CompletionChoice>> callback)
        {
            var uri = domain + version + completionsEndpoint;
            string jsonEntry = JsonUtility.ToJson(data);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonEntry);

            Debug.Log("Request OpenAI Completions: uri= " + uri + ":: JSON= " + jsonEntry);
            List<CompletionChoice> output = new List<CompletionChoice>();

            using (UnityWebRequest request = UnityWebRequest.PostWwwForm(uri, UnityWebRequest.kHttpVerbPOST))
            {
                var bearer = "Bearer " + ApiKey;
                request.SetRequestHeader("Authorization", bearer);

                request.SetRequestHeader("Content-Type", "application/json");
                request.uploadHandler = new UploadHandlerRaw(jsonBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 5;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error OpenAI Completions: " + request.error);
                }
                else
                {
                    if (request.responseCode == 200)
                    {
                        Debug.Log("OpenAI Completions success:" + request.downloadHandler.text);

                        var jsonData = new JSONObject(request.downloadHandler.text);
                        var completionResponse = new CompletionsResponse();

                        completionResponse.id = jsonData.GetField("id").stringValue;
                        completionResponse.@object = jsonData.GetField("object").stringValue;
                        completionResponse.model = jsonData.GetField("model").stringValue;

                        foreach (JSONObject choice in jsonData.GetField("choices").list)
                        {
                            var completionsChoice = new CompletionChoice();
                            var messData = new JSONObject(choice.GetField("message").ToString());
                            completionsChoice.message = new CompletionMessage(messData.GetField("role").stringValue, messData.GetField("content").stringValue);

                            completionsChoice.index = choice.GetField("index").intValue;
                            completionsChoice.finish_reason = choice.GetField("finish_reason").stringValue;

                            output.Add(completionsChoice);
                        }
                    }
                }

                if (callback != null)
                {
                    callback.Invoke(output);
                }

                request.Dispose();
            }
        }

        private IEnumerator ProcessTranscriptRequest(TranscriptRequest data, System.Action<AudioClip, string> callback)
        {
            if(!CoreUtilities.GetExtension(data.file).Equals(".mp3"))
            {
                Debug.Log("Can only process MP3 Transcripts at runtime!");

                if (callback != null)
                {
                    callback.Invoke(null, "Faileed. Can only process MP3 Transcripts at runtime!");
                }

                yield break; 
            }

            using (UnityWebRequest DL = UnityWebRequestMultimedia.GetAudioClip(data.file, AudioType.MPEG))
            {
                DL.method = UnityWebRequest.kHttpVerbGET;
                DL.downloadHandler = new DownloadHandlerBuffer();

                yield return DL.SendWebRequest();

                if (DL.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Could not find file for Transcripts");

                    if (callback != null)
                    {
                        callback.Invoke(null, "Faileed. Could not find File for Transcripts");
                    }
                }
                else
                {
                    if (DL.responseCode == 200)
                    {
                        WWWForm form = new WWWForm();

                        byte[] byteArray = File.ReadAllBytes(data.file);

                        form.AddBinaryData("file", DL.downloadHandler.data, data.file, "audio/mp3");
                        form.AddField("model", data.model);
                        var uri = domain + version + transcriptEndpoint;

                        using (UnityWebRequest request = UnityWebRequest.Post(uri, form))
                        {
                            var bearer = "Bearer " + ApiKey;
                            request.SetRequestHeader("Authorization", bearer);
                            request.downloadHandler = new DownloadHandlerBuffer();
                            request.timeout = 1 * 60 * 60;
                            yield return request.SendWebRequest();

                            if (request.result != UnityWebRequest.Result.Success)
                            {
                                Debug.Log("Error OpenAI Transcriptions: " + request.error + request.downloadHandler.text);

                                if (callback != null)
                                {
                                    callback.Invoke(null, request.error);
                                }
                            }
                            else
                            {
                                if (request.responseCode == 200)
                                {
                                    Debug.Log("OpenAI Completions success:" + request.downloadHandler.text);

                                    var jsonData = new JSONObject(request.downloadHandler.text);
                                    string temp = jsonData.GetField("text").stringValue[0].ToString().ToUpper();

                                    if (callback != null)
                                    {
                                        AudioClip clip = DownloadHandlerAudioClip.GetContent(DL);
                                        callback.Invoke(clip, temp + jsonData.GetField("text").stringValue.Substring(1));
                                    }
                                }
                            }

                            request.Dispose();
                        }
                    }
                }

                DL.Dispose();
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OpenAiAPI), true)]
        public class OpenAiAPI_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ApiKey"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("domain"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("version"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("completionsEndpoint"), true);
                   // EditorGUILayout.PropertyField(serializedObject.FindProperty("editsEndpoint"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("transcriptEndpoint"), true);
                  //  EditorGUILayout.PropertyField(serializedObject.FindProperty("imageGenerationEndpoint"), true);
                  //  EditorGUILayout.PropertyField(serializedObject.FindProperty("imageEditEndpoint"), true);
                  //  EditorGUILayout.PropertyField(serializedObject.FindProperty("imageVariantEndpoint"), true);

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


[System.Serializable]
public class TranscriptRequest
{
    public string file;
    public string model;
}

[System.Serializable]
public enum OpenAIRole { system, user, assistant }

[Serializable]
public class CompletionsData
{
    public string model;
    public List<CompletionMessage> messages;

    public CompletionsData(string model, List<CompletionMessage> messages)
    {
        this.model = model;
        this.messages = messages;
    }
}

[Serializable]
public class CompletionMessage
{
    public string role;
    public string content;

    public CompletionMessage()
    {

    }

    public CompletionMessage (string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

public class CompletionsResponse
{
    public string id;
    public string @object;
    public string created;
    public string model;
    public string choices;
    public CompletionsUsage[] usage;
}

public class CompletionChoice
{
    public int index;
    public string finish_reason;
    public CompletionMessage message;
}

public class CompletionsUsage
{
    public int prompt_tokens;
    public int completion_tokens;
    public int total_tokens;
}