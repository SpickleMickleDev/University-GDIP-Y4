using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Text;
using System.Xml;

namespace ToolSummoner
{
    public class SoftwareController
    {
        static SoftwareController instance;
        private RobotArmController robotArmController;
        private List<ItemListing> items;

        public SoftwareController()
        {
            instance = new SoftwareController();
            items = new List<ItemListing>();
        }

        public void Initialise()
        {
            ParseErrorCode error = GetItemsFromFiles();
            switch (error)
            {
                case ParseErrorCode.FileNotFound:
                    // If logged on as admin, ask the user if they would like to creat a new file and use WPF buttons to populate this file
                    // If logged on as standard user, display an error message and exit the application
                    break;
            }
        }

        public void SelectRobotArmPort(string comPort, int baudRate)
        {
            if (CheckValidPortConfig(comPort, baudRate))
            {

            }
        }

        public bool CheckValidPortConfig(string comPort, int baudRate)
        {
            if (!SerialPort.GetPortNames().Contains(comPort)) return false;

            // static supportedBaudRates = new List<int> { 9600, 19200, ... };
            // if (!supportedBaudRates.Contains(baudRate)) return false;
            return true;
        }

        public enum ParseErrorCode
        {
            None,
            FileNotFound,
            FormatError,

        }
        public ParseErrorCode GetItemsFromFiles()
        {
            // Read from a file or database to populate the items list
            // Xml file is to be used for this
            XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
            xmlDoc.Load("yourXMLFile.xml"); // Load the XML document from the specified file

            // display error if the file is not found
            // ask user if they would like to create a new file



            return ParseErrorCode.None;
        }
    }
}
