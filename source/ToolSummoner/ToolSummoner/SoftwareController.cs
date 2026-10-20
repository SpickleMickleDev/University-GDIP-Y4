using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Text;

namespace ToolSummoner
{
    public class SoftwareController
    {
        static SoftwareController instance;
        private RobotArmController robotArmController;

        public SoftwareController()
        {
            instance = new SoftwareController();
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
    }
}
