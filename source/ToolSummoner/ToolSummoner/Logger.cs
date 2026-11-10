using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace ToolSummoner
{
    internal static class Logger
    {
        public static string logFolder; // get from config file
        public static string logFilePath; // get from logFolder + startup timestamp etc
        public static string startupTimestamp;
        private static bool Initialised = false;
        private static Queue<string> logQueue;
        private static EventWaitHandle taskWaiter = new EventWaitHandle(false, EventResetMode.ManualReset);
        private static StreamWriter fileWriter;
        private static bool keepLogging = true; // set to false when application is closing to stop logging and close down the file


        public static void Initialise(string folderPath = "..\\..\\Logs\\") // Default folder path in case Initialise() is not called by software controller or is not set in any config yet
        {
            logFolder = folderPath;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            startupTimestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
            logFilePath = logFolder + "\\ToolSummonerLog-" + startupTimestamp + ".csv";
            logQueue = new Queue<string>(100);
            fileWriter = new StreamWriter(logFilePath, true); // locks down file for writing

            // write an initial log entry to the file
            LogLine($"");

            Initialised = true;
        }

        public static void LogError(Error error)
        {
            string criticalMessage = error.critical ? "CRITICAL" : "NON-CRITICAL";
            LogLine($"Error {error.errorCode} [{criticalMessage}]: {error.errorMessage}");
        }

        public static void LogLine(string log)
        {
            // Add item to log queue
            logQueue.Enqueue(log);
            taskWaiter.Set(); // Signal the logger thread to process the queue
        }

        public static void RunLogger() // To be run by a separate thread
        {
            while (keepLogging)
            {
                if (logQueue.Count == 0) // Keep logging unless the queue is empty
                {
                    taskWaiter.Reset();
                }
                taskWaiter.WaitOne(); // Wait for a log line to be added to the queue
                if (!keepLogging) break;
                
                if (!Initialised)
                {
                    Initialise(); // Initialise with default folder path if not already initialised
                }


                string log = logQueue.Dequeue();
                WriteLogToFile(log);
            }

            fileWriter.Close(); // Close the file when logging is stopped
        }

        public static void StopLogging()
        {
            keepLogging = false;
            taskWaiter.Reset();
        }

        private static void WriteLogToFile(string log)
        {
            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"); // timestamp in ISO 8601 format
            fileWriter.WriteLine($"{currentTime},{log}");
        }
    }
}
