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

        private Thread RobotArmControllerThread;
        private Thread LoggerThread;
        // Also have this thread and the UI thread, both can be seen in MainWindow.xaml.cs

        public SoftwareController()
        {
            instance = this;
            items = new List<ItemListing>();
        }

        public void RunSoftware()
        {
            Initialise();
        }


        public void Initialise()
        {
            // read config, get log folder path, user details path, item details path etc
            todo here

            // initialise item list from files
            ParseErrorCode error = GetItemsFromFiles();

            // initialise user details
            UserLogIn.InitialiseData();

            // initialise robot arm controller
            todo here

            // Initialise threads

        todo:  deal with error handling
            switch (error)
            {
                case ParseErrorCode.FileNotFound:
                    // If logged on as admin, ask the user if they would like to creat a new file and use WPF buttons to populate this file
                    // If logged on as standard user, display an error message and exit the application
                    break;
            }


            // Run other threads
        }

        public void SelectRobotArmPort(string comPort, int baudRate)
        {
            if (CheckValidPortConfig(comPort, baudRate))
            {

            }
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
            var doc = XDocument.Load(@"..\test.xml");
            List<ItemListing> itemList = doc.Root
                .Descendants("Item")
                .Select(node => new ItemListing
                {
                    uniqueID = Convert.ToInt32(node.Element("").Value,
                    imageFilePath = node.Element("").Value,
                    name = node.Element("").Value,
                    description = node.Element("").Value,
                    stockRemaining = node.Element("").Value,
                    location = ItemLocation.ParseFromString(node.Element("").Value) 
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
