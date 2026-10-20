using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;

namespace ToolSummoner
{
    public class RobotArmController
    {
        private SerialPort serialPort;

        public RobotArmController(string comPort, int baudRate, bool openPort=true) 
        {
            serialPort = new SerialPort(comPort, baudRate);

            if (openPort)
            {
                serialPort.Open();
            }
        }

        public void Setup()
        {
            if (!serialPort.IsOpen)
            {
                serialPort.Open();
            }
        }

        public void ShutDown()
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }
    }
}
