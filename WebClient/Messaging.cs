using System;

namespace BrandLab360
{
    [Serializable]
    public class StartedResponse
    {
        public string name;
        public int room;
        public bool isMobile;
        public string releaseMode;
    }

    [Serializable]
    public class ToggleVideoChatMessage
    {
        public bool ToggleVideoChat;
        public string Channel;
        public string Username;
    }

    [Serializable]
    public class InfotagMessage
    {
        public string url;
        public bool isImage;
    }

    [Serializable]
    public class InfotagWindow
    {
        public string url;
        public string title;
    }

    [Serializable]
    public class Redirect
    {
        public string redirectUrl;
    }

    [Serializable]
    public class ToggleAudioChatMessage
    {
        public bool ToggleAudioChat;
        public string Channel;
        public string Username;
    }

    [Serializable]
    public class WebInputMessage
    {
        public string title;
        public string description;
        public string button;
        public string inputs;
        public string types;
        public string dropdown;

        public WebInputMessage(string title, string description, string button, string inputs, string types, string dropdown)
        {
            this.title = title;
            this.description = description;
            this.button = button;
            this.inputs = inputs;
            this.types = types;
            this.dropdown = dropdown;
        }
    }

    [Serializable]
    public class WebInputTextMessage
    {
        public string input;
        public string type;
        public string button;

        public WebInputTextMessage(string input, string inputType, string button)
        {
            this.input = input;
            this.type = inputType;
            this.button = button;
        }
    }

    [Serializable]
    public class WebInputResponse
    {
        public string btn_clicked;
        public string values;
    }

    [Serializable]
    public class UrlParamRequest
    {
        public bool urlParamRequest;
    }

    [Serializable]
    public class UrlParamResponse
    {
        public string urlParams;
    }

    [Serializable]
    public class CopyLinkRequest
    {
        public string copyLink;
    }

    [Serializable]
    public class LiveStreamRequest
    {
        public string livestreamRole;
        public string livestreamChannel;
        public bool liveStreamEnabled;
        public string hostName;
    }

    [Serializable]
    public class BrowserLogRequest
    {
        public string consoleLog;
    }

    public class CloseWebInput
    {

    }
}