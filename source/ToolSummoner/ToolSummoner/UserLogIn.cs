using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ToolSummoner
{
    public static class UserLogIn
    {
        private static string UserDetailsFilePath = "..\\..\\Assets\\ExampleUserDetails.csv";
        private static List<UserDetails> userDetailsList;
        private static bool initialised = false;

        public static void InitialiseData()
        {
            initialised = true;

            StreamReader sr = new StreamReader(UserDetailsFilePath);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                UserDetails user;
                string[] segments = line.Split(',');
                user.Username = segments[0].Trim();
                user.Password = segments[1].Trim();
                user.Type = UserTypeFromString(segments[2].Trim());
                if (user.Type != UserType.Invalid)
                {
                    userDetailsList.Add(user);
                }
            }
            if (userDetailsList.Count <= 0)
            {
                throw new Exception("No user details found in the file.");
            }
        }

        public static bool ValidateUser(string username, string password, out UserType privileges)
        {
            bool userValid = false;
            privileges = UserType.StandardUser; // Default value

            if (!initialised)
            {
                // todo:
                // if exists, read and populate list
                // if does not exist, prompt the user to make a new one and give them admin perms?
                // - Require hard-coded backup code to do this. Adds a security failsafe, can give this code to customer on delivery of the product
                InitialiseData();
            }

            // Look through all of user details list and check if the details match
            foreach (UserDetails user in userDetailsList)
            {
                if (user.Username == username && user.Password == password)
                {
                    privileges = user.Type;
                    userValid = true;
                    break;
                }
            }
            
            return userValid;
        }


        public enum UserType
        {
            Admin,
            StandardUser,
            Invalid
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
                    return UserType.Invalid;
            }
        }
    }
}




// user details file
// if exists, read and populate list
// if does not exist, prompt the user to make a new one and give them admin perms?
// - Require hard-coded backup code to do this. Adds a security failsafe, can give this code to customer on delivery of the product

/*
Larger system design:

M-V-VM pattern
    Form .xaml (View)
    Form .cs (View-model)
    SoftwareController (Model)

Class diagram
    SoftwareController (Class) - The module responsible for handling the overall software logic and calling the other modules
    RobotArmController (Class) - The module responsible for communicating with the Robot Arm, handling the serial port and sending commands
    UserLogIn (Class)
    ItemListing (struct)
    ItemLocation (struct)

Lower-Level designs:


*/