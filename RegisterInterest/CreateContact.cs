using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class CreateContact : MonoBehaviour
    {
        private RegisterInputStage m_inputStage = RegisterInputStage.firtsname;

        private void Start()
        {
            ChangeInput("Firstname");
        }

        private void Update()
        {
            if (InputManager.Instance.GetKeyUp("Enter"))
            {
                if (m_inputStage == RegisterInputStage.firtsname)
                {
                    ChangeInput("Lastname");
                    return;
                }

                if (m_inputStage == RegisterInputStage.lastname)
                {
                    ChangeInput("Email");
                    return;
                }

                if (m_inputStage == RegisterInputStage.email)
                {
                    ChangeInput("Phone");
                    return;
                }

                if (m_inputStage == RegisterInputStage.phone)
                {
                    ChangeInput("Company");
                    return;
                }

                if (m_inputStage == RegisterInputStage.company)
                {
                    ChangeInput("Website");
                    return;
                }
            }
        }

        private void OnDisable()
        {
            TMP_InputField[] all = GetComponentsInChildren<TMP_InputField>();

            for(int i = 0; i < all.Length; i++)
            {
                all[i].text = "";
                all[i].transform.Find("Error").GetComponent<CanvasGroup>().alpha = 0.0f;
            }
        }

        public void Register()
        {
            Debug.Log("Need to implement the Salesforce API contact");

            /*HubspotAPI.HubspotContact contact = new HubspotAPI.HubspotContact();
            TMP_InputField[] all = GetComponentsInChildren<TMP_InputField>();

            bool failed = false;

            for (int i = 0; i < all.Length; i++)
            {
                if (string.IsNullOrEmpty(all[i].text))
                {
                    failed = true;
                    all[i].transform.Find("Error").GetComponent<CanvasGroup>().alpha = 0.5f;
                }

                if(all[i].name.Contains("Firstname"))
                {
                    contact.firstname = all[i].text;
                }
                else if(all[i].name.Contains("Lastname"))
                {
                    contact.lastname = all[i].text;
                }
                else if (all[i].name.Contains("Telephone"))
                {
                    contact.phone = all[i].text;
                }
                else if (all[i].name.Contains("Company"))
                {
                    contact.company = all[i].text;
                }
                else if (all[i].name.Contains("Website"))
                {
                    contact.website = all[i].text;
                }
                else if (all[i].name.Contains("Email"))
                {
                    if(!IsValidEmail(all[i].text))
                    {
                        failed = true;
                        all[i].transform.Find("Error").GetComponent<CanvasGroup>().alpha = 0.5f;
                    }
                    else
                    {
                        contact.email = all[i].text;
                    }
                }
            }

            if(!failed)
            {
                Create(contact);
            }*/
        }

        private void ChangeInput(string id)
        {
            TMP_InputField[] all = GetComponentsInChildren<TMP_InputField>();

            for (int i = 0; i < all.Length; i++)
            {
                if(all[i].name.Contains(id))
                {
                    all[i].Select();
                    break;
                }
            }
        }

        public void UpdateStage(int stage)
        {
            switch(stage)
            {
                case 1:
                    m_inputStage = RegisterInputStage.lastname;
                    break;
                case 2:
                    m_inputStage = RegisterInputStage.email;
                    break;
                case 3:
                    m_inputStage = RegisterInputStage.phone;
                    break;
                case 4:
                    m_inputStage = RegisterInputStage.company;
                    break;
                case 5:
                    m_inputStage = RegisterInputStage.website;
                    break;
                default:
                    m_inputStage = RegisterInputStage.firtsname;
                    break;
            }
        }


        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private enum RegisterInputStage { firtsname, lastname, email, phone, company, website }

#if UNITY_EDITOR
        [CustomEditor(typeof(CreateContact), true)]
        public class CreateContact_Editor : BaseInspectorEditor
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
