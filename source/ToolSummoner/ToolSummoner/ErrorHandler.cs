using System;
using System.Collections.Generic;
using System.Text;

namespace ToolSummoner
{
    public struct Error
    {
        public int errorCode;
        public string errorMessage;
        // severity? to be implemented if this was more serious than a demo. For now, just a simple boolean critical to determine if we close the application or not
        public bool critical;

        public Error(int errorCode, string errorMessage)
        {
            this.errorCode = errorCode;
            this.errorMessage = errorMessage;
        }
    }

    internal static class ErrorHandler
    {
        private volatile static List<Error> errorList; // volatile to ensure that the list is always up to date across threads and is more threadsafe (in case we incorporate multithreading later)

        public static void AddError(Error error)
        {
            EventWaitHandle threadHalter = new EventWaitHandle(false, EventResetMode.AutoReset);

            Task.Run(() =>
            {
                errorList.Add(error); // keeps a history of all errors that have occurred in the application, for logging purposes
                DisplayError(error);
            });
            threadHalter.WaitOne(); // wait for error message to be closed before continuing. If the error is critical, the application will close and this will never be reached.
        }

        private static void DisplayError(Error error)
        {
            // Prompt view thread to display the error to the user in a WPF window
            // If the error is critical, any action on the wpf window will close the application.
            // final logging of all errors to occur when the application is closed
        }




    }
}
