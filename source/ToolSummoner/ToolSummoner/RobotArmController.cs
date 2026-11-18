using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace ToolSummoner
{
    public static class RobotArmICD
    {
        public const byte COMMS_START_BYTE_1 = 0xFF;
        public const byte COMMS_START_BYTE_2 = 0xF6;
        public const byte COMMS_END_BYTE= 0x5F;

        public const byte OPERATION_CALIBRATE = 0xD0;
        public const byte OPERATION_SET_POS = 0xD1;
        public const byte OPERATION_GOTO_POS = 0xD2;
        public const byte OPERATION_SET_SERVO = 0xD3;
        public const byte OPERATION_GRAB = 0xD4;
        public const byte OPERATION_DROP = 0xD5;
        public const byte OPERATION_LOG_POS = 0xD6;
        public const byte OPERATION_REQ_VER_INFO = 0xD7;
        // Add more operations as needed. Application -> Robot Arm

        public const byte RESPONSE_ACK = 0xE0;
        public const byte RESPONSE_ERROR = 0xE1;
        public const byte RESPONSE_COMPLETE = 0xE2;
        public const byte RESPONSE_STORED_POS = 0xE3;
        public const byte RESPONSE_VERSION_INFO = 0xE4;
        // Add more responses as needed. Robot Arm -> Application

        public enum ErrorTypes
        {
            ERROR_INVALID_PARAMETER_COUNT = 0x00,
            ERROR_INVALID_PARAMETER_VALUE = 0x01,
            ERROR_INVALID_OPERATION = 0x02,
            ERROR_CONFUSING_MESSAGE = 0x03,
            ERROR_MEMORY_LOCATION_NOT_SET = 0x04,
            ERROR_SERVO_COMMAND_OUT_OF_RANGE = 0x05,
            ERROR_SERVO_W_OFFSET_OUT_OF_RANGE = 0x06
        };
    }

    public class RobotArmController
    {
        public const int DefaultBaudRate = 115200;
        private SerialPort serialPort;

        public RobotArmController(string comPort, bool openPort=true)
        {
            serialPort = new SerialPort(comPort, DefaultBaudRate);

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

        static public bool TestPort(string comPort)
        {
            SerialPort testPort = new SerialPort(comPort, DefaultBaudRate);
            bool portValid = false;
            try
            {
                testPort.Open();

                // send version request message
                byte[] buffer = new byte[] 
                { 
                    RobotArmICD.COMMS_START_BYTE_1, 
                    RobotArmICD.COMMS_START_BYTE_2, 
                    RobotArmICD.OPERATION_REQ_VER_INFO, 
                    RobotArmICD.COMMS_END_BYTE 
                };
                testPort.Write(buffer, 0, buffer.Length);

                // wait for response
                byte[]? response = ReadResponse(testPort, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(2000));

                // check response is valid
                if (response != null)
                {
                    portValid = (response[0] == RobotArmICD.COMMS_START_BYTE_1) &&
                                (response[1] == RobotArmICD.COMMS_START_BYTE_2) &&
                                (response[2] == RobotArmICD.RESPONSE_VERSION_INFO) &&
                              //(response[3] == ????) // major version
                              //(response[4] == ????) // minor version
                                (response[5] == RobotArmICD.COMMS_END_BYTE);

                    if (portValid)
                    {
                        Logger.LogLine($"Robot arm connected on port {comPort}. Embedded device version: {response[3]:X2}.{response[4]:X2}");
                    }
                }
            }
            catch (Exception)
            {
                portValid = false;
            }
            finally
            {
                if (testPort.IsOpen)
                {
                    testPort.Close();
                }
            }

            return portValid;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="port"></param>
        /// <param name="softTimeout">A soft timeout for waiting for data. If data is found within the soft timeout, the function will extend to the hard timeout. If not, the function will return null.</param>
        /// <param name="hardTimeout">A hard/strict timeout for waiting for data. Operation will timeout if no data is received within this time.</param>
        /// <returns></returns>
        private static byte[]? ReadResponse(SerialPort port, TimeSpan softTimeout, TimeSpan hardTimeout)
        {
            var response = new List<byte>();
            var stopwatch = Stopwatch.StartNew();

            int state = 0;

            int prevPortTimeout = port.ReadTimeout;
            port.ReadTimeout = 250; // Set a read timeout of 250ms to avoid blocking indefinitely but allow for waiting for data when a message is unfinished.
            TimeSpan timeout = softTimeout;
            while (stopwatch.Elapsed < timeout)
            {
                try
                {
                    int value = port.ReadByte();

                    switch (state)
                    {
                        case 0:
                            // Looking for start byte 1
                            if (value == RobotArmICD.COMMS_START_BYTE_1)
                            {
                                response.Clear();
                                response.Add(RobotArmICD.COMMS_START_BYTE_1);
                                timeout = hardTimeout; // Extend timeout to hard timeout since we have found the start of a message
                                state = 1;
                            }
                            break;

                        case 1:
                            // Looking for start byte 2
                            if (value == RobotArmICD.COMMS_START_BYTE_2)
                            {
                                response.Add(RobotArmICD.COMMS_START_BYTE_2);
                                state = 2;
                            }
                            else if (value == RobotArmICD.COMMS_START_BYTE_1)
                            {
                                // This may be the beginning of a new header.
                                response.Clear();
                                response.Add(RobotArmICD.COMMS_START_BYTE_1);
                            }
                            else
                            {
                                response.Clear();
                                state = 0;
                            }
                            break;

                        case 2:
                            response.Add((byte)value);

                            if (value == RobotArmICD.COMMS_END_BYTE)
                            {
                                return response.ToArray(); // Only process one message at a time
                            }
                            break;
                    }
                }
                catch (TimeoutException)
                {
                    return null;
                }
            }
            
            port.ReadTimeout = prevPortTimeout;
            return null;
        }
    }
}
