using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace BrandLab360
{
    public class BaseCustomAPI : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Base Task to authenticate comms
        /// This might not be needed so will return true via base class
        /// </summary>
        /// <returns></returns>
        public virtual async Task<bool> Authenticate()
        {
            await Task.Delay(100);

            return true;
        }

        /// <summary>
        /// Base Task to Login user
        /// Will return false by default
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public virtual async Task<bool> Login(string username, string password)
        {
            bool success = false;

            //if login is successful 
            //create new Profile Data and apply data to it
            //ProfileData Profile = new ProfileData();

            //you then need to apply the following data

            /*
             *  AppManager.Instance.Data.LoginProfileData = JsonUtility.FromJson<ProfileData>(data);
                AppManager.Instance.Data.LoginProfileData.username = username;
                AppManager.Instance.Data.LoginProfileData.password = password;
                AppManager.Instance.Data.NickName = username;
                AppManager.Instance.Data.CustomiseJson = AppManager.Instance.Data.LoginProfileData.avatar_data;
                AppManager.Instance.Data.IsAdminUser = isAdmin;
                AppManager.Instance.Data.RawFriendsData = freinds;
                AppManager.Instance.Data.RawGameData = games;

                AppLogin login = FindObjectOfType<AppLogin>();
                login.ShowLoadingOverlay(true);

                AppManager.Instance.LoginComplete();
             * 
             */

            await Task.Delay(100);

            return success;
        }

        /// <summary>
        /// Base class to register new user
        /// Will return false by default
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="email"></param>
        /// <param name="termsAndConditions"></param>
        /// <param name="isAdmin"></param>
        /// <returns></returns>
        public virtual async Task<bool> Register(string username, string password, string email, bool termsAndConditions = true, bool isAdmin = false)
        {
            bool success = false;

            await Task.Delay(100);

            return success;
        }

        /// <summary>
        /// Base class to push user data
        /// Will return false by default
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="friendsData"></param>
        /// <param name="gamesData"></param>
        /// <param name="isAdmin"></param>
        /// <returns></returns>
        public virtual async Task<bool> Push(ProfileData profile, string friendsData, string gamesData, bool isAdmin = false)
        {
            bool success = false;

            await Task.Delay(100);

            return success;
        }
    }
}
