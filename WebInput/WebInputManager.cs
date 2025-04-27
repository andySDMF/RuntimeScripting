using System;
using System.Collections.Generic;
using UnityEngine;
using Defective.JSON;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class WebInputManager : Singleton<WebInputManager>
    {
        private WebInputData currentWebInputData;
        private bool awaitingWebInputResponse = false;

        public static System.Action<WebInputResult> WebInputResultListener;
        public static System.Action<TextEntryResult> TextEntryResultResultListener;

        public static WebInputManager Instance
        {
            get
            {
                return ((WebInputManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        void Awake()
        {
            WebclientManager.WebClientListener += webClientResponse;
        }

        private void OnDestroy()
        {
            WebclientManager.WebClientListener -= webClientResponse;
        }

        /// <summary>
        /// Send the web input request message to the webclient to display the input popup
        /// </summary>
        /// <param name="webInputData"></param>
        public void SendWebInputRequest(WebInputData webInputData)
        {
            var webInputMessage = createWebInputMessage(webInputData);
            var json = JsonUtility.ToJson(webInputMessage);

            awaitingWebInputResponse = true;
            currentWebInputData = webInputData;

            WebclientManager.Instance.Send(json);
        }

        /// <summary>
        /// Send web input field request to webclient to display web input field popup
        /// </summary>
        /// <param name="webInputMessage"></param>
        public void SendWebInputFieldRequest(TextEntryMessage webInputMessage)
        {
            var webInputTextMessage = createWebInputTextMessage(webInputMessage);
            var json = JsonUtility.ToJson(webInputTextMessage);
            awaitingWebInputResponse = true;

            Debug.Log("Web request input field show json: " + json);
            WebclientManager.Instance.Send(json);
        }

        /// <summary>
        /// Send web request to close the web input field pop up
        /// </summary>
        /// <param name="webInputMessage"></param>
        public void SendWebInputFieldCloseRequest()
        {
            var json = JsonUtility.ToJson(new TextEntryCloseMessage());
            awaitingWebInputResponse = true;

            Debug.Log("Web request input field hide json: " + json);
            WebclientManager.Instance.Send(json);
        }

        /// <summary>
        /// Web client listener function, processes the returned json data
        ///  - The result data is dynamic so we detect the "btn_clicked" key which is consistent
        /// </summary>
        /// <param name="json">the json data returned from the webclient</param>
        private void webClientResponse(string json)
        {
            if (!awaitingWebInputResponse) return;

            Debug.Log("webInputResponse json: " + json);

            TextEntryResult textResult = JsonUtility.FromJson<TextEntryResult>(json).OrDefaultWhen(x => x.result == null);

            if (textResult != null)
            {
                if (TextEntryResultResultListener != null)
                {
                    TextEntryResultResultListener.Invoke(textResult);
                }

                return;
            }

            if (!awaitingWebInputResponse || currentWebInputData == null) { return; }

            string outBtnClicked = "";
            List<string> outInputValues = new List<string>();
            string outDropdownValue = "";
            var data = new JSONObject(json);

            if (data != null)
            {
                // detect the webinput response based on json contents
                if (data.GetField("btn_clicked").stringValue == null)
                {
                    return;
                }

                // detect the clicked button
                if (data.GetField("btn_clicked") != null)
                {
                    outBtnClicked = data.GetField("btn_clicked").stringValue;
                }

                // detect the values array
                if (data.GetField("values") != null)
                {
                    var values = data.GetField("values");

                    // search for results in the json based on the inputs that were sent
                    for (int i = 0; i < currentWebInputData.inputTypes.Length; i++)
                    {
                        // extract the input value
                        var key = "input_" + currentWebInputData.inputTypes[i].ToString() + "_" + (i + 1).ToString();

                        if (values.GetField(key) != null)
                        {
                            outInputValues.Add(values.GetField(key).ToString());
                        }
                    }

                    // extract the dropdown value
                    if (values.GetField("select_dropdown_1") != null)
                    {
                        outDropdownValue = values.GetField("select_dropdown_1").ToString();
                    }
                }

                WebInputResult result = new WebInputResult(outBtnClicked, outInputValues.ToArray(), outDropdownValue);
                awaitingWebInputResponse = false;

                if (WebInputResultListener != null)
                {
                    WebInputResultListener.Invoke(result);
                }
            }
        }

        /// <summary>
        /// Leaving this example input here temporarily to demonstrate usage
        /// </summary>
        private void exampleInput()
        {
            string[] buttons = { "OK", "Cancel" };
            string[] inputs = { "email1", "password1", "email2" };
            WebInputType[] inputTypes = { WebInputType.email, WebInputType.password, WebInputType.email };
            string[] dropdownOptions = { "optionA" };

            var webInputData = new WebInputData("title", "description", buttons, inputs, inputTypes, dropdownOptions);

            SendWebInputRequest(webInputData);
        }

        /// <summary>
        /// Creates the web input request message, structures the array data in comma separated strings
        /// </summary>
        /// <param name="webInputData">the webinput data</param>
        private WebInputMessage createWebInputMessage(WebInputData webInputData)
        {
            //convert the arrays into comma separated strings

            string buttonsString = "";
            for (int i = 0; i < webInputData.buttons.Length; i++)
            {
                buttonsString += webInputData.buttons[i];

                if (i < (webInputData.buttons.Length - 1) && webInputData.buttons.Length != 1) { buttonsString += ","; }
            }

            string InputsString = "";
            for (int i = 0; i < webInputData.inputs.Length; i++)
            {
                InputsString += webInputData.inputs[i];

                if (i < (webInputData.inputs.Length - 1) && webInputData.inputs.Length != 1) { InputsString += ","; }
            }

            string InputTypesString = "";
            for (int i = 0; i < webInputData.inputTypes.Length; i++)
            {
                InputTypesString += webInputData.inputTypes[i].ToString();

                if (i < (webInputData.inputTypes.Length - 1) && webInputData.inputTypes.Length != 1) { InputTypesString += ","; }
            }

            string dropdownOptionsString = "";
            for (int i = 0; i < webInputData.dropdownOptions.Length; i++)
            {
                dropdownOptionsString += webInputData.dropdownOptions[i];

                if (i < (webInputData.dropdownOptions.Length - 1) && webInputData.dropdownOptions.Length != 1) { dropdownOptionsString += ","; }
            }

            return new WebInputMessage(webInputData.title, webInputData.description, buttonsString, InputsString, InputTypesString, dropdownOptionsString);
        }

        private WebInputTextMessage createWebInputTextMessage(TextEntryMessage textMessage)
        {
            string input = textMessage.input;
            string inputType = textMessage.inputType.ToString();
            string button = textMessage.button;

            return new WebInputTextMessage(input, inputType, button);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(WebInputManager), true)]
        public class WebInputManager_Editor : BaseInspectorEditor
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

    #region Data Structures

    [System.Serializable]
    public enum WebInputType { text, email, password, number }

    [System.Serializable]
    public class WebInputData
    {
        public string title;
        public string description;
        public string[] buttons;
        public string[] inputs;
        public WebInputType[] inputTypes;
        public string[] dropdownOptions;

        public WebInputData(string title, string description, string[] buttons, string[] inputs,
                            WebInputType[] inputTypes, string[] dropdownOptions)
        {
            this.title = title;
            this.description = description;
            this.buttons = buttons;
            this.inputs = inputs;
            this.inputTypes = inputTypes;
            this.dropdownOptions = dropdownOptions;
        }
    }

    public class WebInputResult
    {
        public string btnClicked;
        public string[] inputValues;
        public string dropdownValue;

        public WebInputResult(string btnClicked, string[] inputValues, string dropdownValue)
        {
            this.btnClicked = btnClicked;
            this.inputValues = inputValues;
            this.dropdownValue = dropdownValue;
        }
    }

    [System.Serializable]
    public class TextEntryCloseMessage
    {
        public bool closeWebInput = true;
    }

    [System.Serializable]
    public class TextEntryMessage
    {
        public string input;
        public WebInputType inputType;
        public string button;
    }

    [System.Serializable]
    public class TextEntryResult
    {
        public string result;
    }
    #endregion


}