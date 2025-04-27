using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrandLab360
{
    public class SendGridAPI : Singleton<SendGridAPI>
    {
        public static SendGridAPI Instance
        {
            get
            {
                return ((SendGridAPI)instance);
            }
            set
            {
                instance = value;
            }
        }

      //  [SerializeField]
      //  private string host = "https://api.sendgrid.com/v3/";

        private const string APIKey = "";
    }
}
