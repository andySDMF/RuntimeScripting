using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrandLab360
{
    [CreateAssetMenu(fileName = "ProjectAPISettings", menuName = "ScriptableObjects/ProjectAPISettings", order = 2)]

    public class AppLoginAPISettings : ScriptableObject
    {
        [Header("Runtime Settings")]
        public SalesforceAPISettings salesforceSettings;
        public HubspotAPISettings hubspotSettings;
    }

    [System.Serializable]
    public class LoginReference
    {
        public string tableField = "";
        public LoginFormReference formReference;

        public LoginReference(string tf, LoginFormReference formRef)
        {
            tableField = tf;
            formReference = formRef;
        }
    }

    [System.Serializable]
    public class ProfileReference
    {
        public string tableField = "";
        public bool ignore = false;
        public ProfileDataReference profileReference;

        public ProfileReference(string tf, ProfileDataReference profileRef)
        {
            tableField = tf;
            profileReference = profileRef;
        }
    }

    [System.Serializable]
    public enum LoginFormReference { _username, _password, _email, _termsAndConditions }

    [System.Serializable]
    public enum ProfileDataReference { _ID, _username, _name, _about, _password, _email, _picture, _termsAndConditions, _avatar, _playerSettings, _friendsData, _gamesData, _admin, _custom }
}