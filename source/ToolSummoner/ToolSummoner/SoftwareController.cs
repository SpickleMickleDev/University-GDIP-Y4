using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Text;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;

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
            UserLogIn.InitialiseData();
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
            // Read list of all items from a .xml file

            // temporary
            //XmlDocument xmlDoc = new XmlDocument(); // Create xml object
            //xmlDoc.Load("test.xml"); // Load the XML document from the specified file
            ItemLocation
            var doc = XDocument.Load(@"..\test.xml");
            List<ItemListing> itemList = doc.Root
                .Descendants("Item")
                .Select(node => new ItemListing
                {
                    uniqueID = node.Element("").Value,
                    imageFilePAth = node.Element("").Value,
                    name = node.Element("").Value,
                    description = node.Element("").Value,
                    stockRemaining = node.Element("").Value,
                    ItemLocation location = ItemLocation.ParseFromString(node.Element("").Value)
                })
                .ToList();


            // display error if the file is not found
            // ask user if they would like to create a new file



            return ParseErrorCode.None;
        }

        // AddItem() // call UpdateItemDatabase at end. Also do this for every time an item is taken from stock and lock the file down first
        // need to come up with a threadsafe approach to multiple clients' file reading attempts. If file is taken, wait until available and then attempt action. IF no longer possible, cancel and inform the user

        // RemoveItem() // call UpdateItemDatabase at end

        // UpdateItemDatabase() // add to xml
    }
}
