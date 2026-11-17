using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ToolSummoner
{
    public class SoftwareController
    {
        private const string DefaultItemsFilePath = @"..\..\..\..\..\Assets\ExampleItemData.xml";

        static SoftwareController instance;
        private RobotArmController robotArmController;
        public List<ItemListing> Items { get; private set; }

        private Thread RobotArmControllerThread;
        private Thread LoggerThread;
        // Also have this thread and the UI thread, both can be seen in MainWindow.xaml.cs

        private bool robotArmConnected = false;

        public SoftwareController()
        {
            instance = this;
            Items = new List<ItemListing>();
        }

        public void RunSoftware()
        {
            Initialise();
        }


        public void Initialise()
        {
            // read config, get log folder path, user details path, item details path etc
            //@todo: add config here

            // start logger
            Logger.Initialise();
            instance.LoggerThread = new Thread(Logger.RunLogger);
            instance.LoggerThread.Start();

            // initialise item list from files
            ParseErrorCode error = GetItemsFromFiles();

            // initialise user details
            UserLogIn.InitialiseData();

            // Get robot arm COM port and initialise robot arm controller
            // -> If could not be found, will not be able to order items from the robot arm, but can still view stock levels and item details
            AttemptToConnectRobotArm();

            // Initialise threads





            //@todo:  deal with error handling?
            switch (error)
            {
                case ParseErrorCode.FileNotFound:
                    // If logged on as admin, ask the user if they would like to creat a new file and use WPF buttons to populate this file
                    // If logged on as standard user, display an error message and exit the application
                    break;
            }
        }

        public void AttemptToConnectRobotArm()
        {
            string? robotArmPort = GetRobotArmPort();

            // initialise robot arm controller
            if (robotArmConnected)
            {
                robotArmController = new RobotArmController(robotArmPort);
            }
        }

        public string? GetRobotArmPort()
        {
            // Repeat x times:
            // - Get list of serial ports available
            // - For each port:
            // - - Attempt to open the port and get a response from the embedded device when requesting version info
            // - - If successful, set the COM port, break out of the loop and log the embedded device version
            // - - If unsuccessful, move on
            // - If no successful connection is made, display message to the user and ask them to check
            // If no successful connection is made still, report an error and exit the application

            string[] serialPortsAvailable;
            SerialPort attemptingPort;
            string successfulPort = string.Empty;

            for (int attemptsLeft = 3; attemptsLeft > 0; attemptsLeft--)
            {
                serialPortsAvailable = SerialPort.GetPortNames();

                foreach (string port in serialPortsAvailable)
                {
                    attemptingPort = new SerialPort(port, RobotArmController.DefaultBaudRate);
                    try
                    {
                        bool portFound = RobotArmController.TestPort(port);
                        if (portFound)
                        {
                            successfulPort = port;
                            attemptsLeft = 0;
                            break;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            if (successfulPort != string.Empty)
            {
                // Do something with the successful port
                instance.robotArmConnected = true;
                return successfulPort;
            }
            else
            {
                // No successful connection was found. Display error message and exit the application
                // @todo: Do we want this to result in exiting the application or entering a kind of view-only mode to just see stock levels and item details?
            }
            return null;
        }



        public enum ParseErrorCode
        {
            None,
            FileNotFound,
            FormatError,

        }
        public ParseErrorCode GetItemsFromFiles()
        {
            return GetItemsFromFiles(DefaultItemsFilePath);
        }

        public ParseErrorCode GetItemsFromFiles(string filePath)
        {
            try
            {
                XDocument document = XDocument.Load(filePath);
                XElement root = document.Root
                    ?? throw new FormatException("The XML document has no root element.");

                Items = root.Elements("Item").Select(ReadItem).ToList();
                return ParseErrorCode.None;
            }
            catch (FileNotFoundException)
            {
                return ParseErrorCode.FileNotFound;
            }
            catch (DirectoryNotFoundException)
            {
                return ParseErrorCode.FileNotFound;
            }
            catch (XmlException)
            {
                return ParseErrorCode.FormatError;
            }
            catch (FormatException)
            {
                return ParseErrorCode.FormatError;
            }
            catch (InvalidOperationException)
            {
                return ParseErrorCode.FormatError;
            }
        }

        public void SaveItemsToFile(string filePath, IEnumerable<ItemListing> itemListings)
        {
            ArgumentNullException.ThrowIfNull(itemListings);

            XElement root = new XElement(
                "Items",
                itemListings.Select(item => new XElement(
                    "Item",
                    new XElement("UniqueId", item.uniqueID),
                    new XElement("ImageFilePath", item.imageFilePath),
                    new XElement("Name", item.name),
                    new XElement("Description", item.description),
                    new XElement("StockRemaining", item.stockRemaining),
                    new XElement(
                        "Location",
                        new XElement("XCoordinate", item.location.xCoordinate.ToString("R", CultureInfo.InvariantCulture)),
                        new XElement("YCoordinate", item.location.yCoordinate.ToString("R", CultureInfo.InvariantCulture))))));

            new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).Save(filePath);
        }

        public void SaveItemsToFile(string filePath)
        {
            SaveItemsToFile(filePath, Items);
        }

        private static ItemListing ReadItem(XElement node)
        {
            XElement location = RequiredElement(node, "Location");

            return new ItemListing
            {
                uniqueID = int.Parse(RequiredValue(node, "UniqueId"), CultureInfo.InvariantCulture),
                imageFilePath = RequiredValue(node, "ImageFilePath"),
                name = RequiredValue(node, "Name"),
                description = RequiredValue(node, "Description"),
                stockRemaining = int.Parse(RequiredValue(node, "StockRemaining"), CultureInfo.InvariantCulture),
                location = new ItemLocation(
                    double.Parse(RequiredValue(location, "XCoordinate"), CultureInfo.InvariantCulture),
                    double.Parse(RequiredValue(location, "YCoordinate"), CultureInfo.InvariantCulture))
            };
        }

        private static XElement RequiredElement(XElement parent, string name)
        {
            return parent.Element(name)
                ?? throw new FormatException($"Missing XML element '{name}'.");
        }

        private static string RequiredValue(XElement parent, string name)
        {
            return RequiredElement(parent, name).Value;
        }



        // AddItem() // call UpdateItemDatabase at end. Also do this for every time an item is taken from stock and lock the file down first
        // need to come up with a threadsafe approach to multiple clients' file reading attempts. If file is taken, wait until available and then attempt action. IF no longer possible, cancel and inform the user

        // RemoveItem() // call UpdateItemDatabase at end

        // UpdateItemDatabase() // add to xml
    }
}
