using System;
using System.Collections.Generic;
using System.Text;

namespace ToolSummoner
{
    public static class UserLogIn
    {
        private static string UserDetailsFilePath = "..\\..\\Assets\\ExampleUserDetails.csv";
        private static List<UserDetails> userDetailsList;
        private static bool initialised = false;

        public void InitialiseData()
        {
            initialised = true;
        }

        public bool ValidateUser(string username, string password, out UserType privileges)
        {
            bool userValid = false;
            privileges = UserType.StandardUser; // Default value

            if (!initialised)
            {
                InitialiseData();
            }

            // todo: look through all of user details list and check if details match
        }


        public enum UserType
        {
            Admin,
            StandardUser
        }
        private struct UserDetails
        {
            public string Username;
            public string Password;
            public UserType Type;
            public UserDetails(string username, string password, UserType type)
            {
                Username = username;
                Password = password;
                Type = type;
            }
        }

        private static UserType UserTypeFromString(string userType)
        {
            switch(userType)
            {
                case "Admin":
                    return UserType.Admin;
                case "StandardUser":
                    return UserType.StandardUser;
                default:
                    throw new ArgumentException("Invalid user type");
            }
        }
    }
}
