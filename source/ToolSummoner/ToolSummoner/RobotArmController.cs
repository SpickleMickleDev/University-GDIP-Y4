using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;

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
        public const byte RESPONSE_GENERAL_MESSAGE = 0xE5;

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

        volatile static private Queue<byte[]> messagesToSendQueue;

        volatile bool keepListening = true;

        public RobotArmController(string comPort, bool openPort=true)
        {
            serialPort = new SerialPort(comPort, DefaultBaudRate)
            {
                DtrEnable = true,
                RtsEnable = true,
                Handshake = Handshake.None,
                ReadTimeout = 1000
            };

            if (openPort)
            {
                serialPort.Open();
            }
            Thread.Sleep(2000);

            messagesToSendQueue = new Queue<byte[]>();
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
            keepListening = false;
        }

        public void Run()
        {
            Setup();

            while (keepListening)
            {
                ListenForMessages();

                if (messagesToSendQueue.Count > 0)
                {
                    SendMessageOverSerial(messagesToSendQueue.Dequeue());
                }
            }
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }

            ShutDown();
        }

        public void ListenForMessages()
        {
            byte[]? response = ReadResponse(serialPort, TimeSpan.FromMilliseconds(2500), TimeSpan.FromMilliseconds(20000));

            if (response != null)
            {
                byte operation = response[2];

                switch (operation)
                {
                    case RobotArmICD.RESPONSE_ACK:
                        break;
                    case RobotArmICD.RESPONSE_ERROR:
                        break;
                    case RobotArmICD.RESPONSE_COMPLETE:
                        break;
                    case RobotArmICD.RESPONSE_STORED_POS:
                        break;
                    case RobotArmICD.RESPONSE_VERSION_INFO:
                        break;
                    case RobotArmICD.RESPONSE_GENERAL_MESSAGE:
                        Debug.WriteLine($"Received message: {DecodeGeneralMessage(response)}");
                        break;
                    default:
                        break;
                }
            }
        }

        public bool SendMessage_Calibrate(byte[] servoOffsets, bool waitUntilCompletion)
        {
            if (servoOffsets is null || servoOffsets.Length != 6) return false;
            // Will have 1 operation + 6 parameters
            byte[] message = new byte[7];
            
            message[0] = RobotArmICD.OPERATION_CALIBRATE;
            Array.Copy(servoOffsets, 0, message, 1, servoOffsets.Length);

            bool ackReceived = false;
            bool completeReceived = false;



            messagesToSendQueue.Enqueue(message);

            // wait for message ack and message complete
            return ackReceived && (!waitUntilCompletion || completeReceived);
        }

        public bool SendMessage_SetPosition(byte memoryIndex, byte[] servoValues, bool waitUntilCompletion)
        {
            if (servoValues is null || servoValues.Length != 5) return false;
            // Will have 1 operation + 7 parameters
            byte[] message = new byte[8];

            message[0] = RobotArmICD.OPERATION_SET_POS;
            message[1] = memoryIndex;
            Array.Copy(servoValues, 0, message, 2, servoValues.Length);

            bool ackReceived = false;
            bool completeReceived = false;



            messagesToSendQueue.Enqueue(message);

            // wait for message ack and message complete
            return ackReceived && (!waitUntilCompletion || completeReceived);
        }

        public bool SendMessage_GoToPosition(byte memoryIndex, bool waitUntilCompletion)
        {
            // Will have 1 operation + 1 parameter
            byte[] message = { RobotArmICD.OPERATION_GOTO_POS, memoryIndex };

            bool ackReceived = false;
            bool completeReceived = false;
            messagesToSendQueue.Enqueue(message);

            // wait for message ack and message complete
            return ackReceived && (!waitUntilCompletion || completeReceived);
        }

        public bool SendMessage_SetServo(byte[] servoValues, bool useOffsets, bool waitUntilCompletion)
        {
            if (servoValues is null || servoValues.Length != 6) return false;
            // Will have 1 operation + 7 parameters
            byte[] message = new byte[8];

            message[0] = RobotArmICD.OPERATION_SET_SERVO;
            message[7] = (byte)((useOffsets) ? 0x01 : 0x00);
            Array.Copy(servoValues, 0, message, 1, servoValues.Length);

            bool ackReceived = false;
            bool completeReceived = false;
            messagesToSendQueue.Enqueue(message);

            // wait for message ack and message complete
            return ackReceived && (!waitUntilCompletion || completeReceived);
        }

        public bool SendMessage_Grab(bool waitUntilCompletion)
        {
            // Will have 1 operation + 0 parameters
            byte[] message = { RobotArmICD.OPERATION_GRAB };
            
            bool ackReceived = false;
            bool completeReceived = false;
            messagesToSendQueue.Enqueue(message);

            // wait for message ack and message complete
            return ackReceived && (!waitUntilCompletion || completeReceived);
        }

        public bool SendMessage_Drop(bool waitUntilCompletion)
        {
            // Will have 1 operation + 0 parameters
            byte[] message = { RobotArmICD.OPERATION_DROP };

            bool ackReceived = false;
            bool completeReceived = false;
            messagesToSendQueue.Enqueue(message);

            // wait for message ack and message complete
            return ackReceived && (!waitUntilCompletion || completeReceived);
        }

        public bool SendMessage_LogPosition(byte memoryIndex, bool waitUntilCompletion)
        {
            // Will have 1 operation + 1 parameter
            byte[] message = { RobotArmICD.OPERATION_LOG_POS, memoryIndex };

            bool ackReceived = false;
            bool completeReceived = false;
            messagesToSendQueue.Enqueue(message);

            // wait for message ack and message complete
            return ackReceived && (!waitUntilCompletion || completeReceived);
        }

        public bool SendMessage_RequestVersionInfo(bool waitUntilCompletion)
        {
            // Will have 1 operation + 0 parameters
            byte[] message = { RobotArmICD.OPERATION_REQ_VER_INFO };

            bool ackReceived = false;
            bool completeReceived = false;
            messagesToSendQueue.Enqueue(message);

            // wait for message ack and message complete
            return ackReceived && (!waitUntilCompletion || completeReceived);
        }



        private void SendMessageOverSerial(byte[] messageContents) // Includes both operation and the parameters
        {
            int messageContentsLength = messageContents.Length;
            byte[] fullMessage = new byte[messageContentsLength+ 3];
            fullMessage[0] = RobotArmICD.COMMS_START_BYTE_1;
            fullMessage[1] = RobotArmICD.COMMS_START_BYTE_2;
            Array.Copy(messageContents, 0, fullMessage, 2, messageContentsLength);
            fullMessage[messageContentsLength + 2] = RobotArmICD.COMMS_START_BYTE_2;

            serialPort.Write(fullMessage, 0, fullMessage.Length);
        }

        public void SendMessage(byte operation, byte[]? parameters = null)
        {
            int parameterCount = parameters?.Length ?? 0;
            byte[] message = new byte[parameterCount + 1];

            message[0] = operation;

            if (parameters is not null)
            {
                Array.Copy(parameters, 0, message, 1, parameterCount);
            }

            messagesToSendQueue.Enqueue(message);
        }

        private static string DecodeGeneralMessage(byte[] originalBuffer)
        {
            int count = originalBuffer.Length - 4;
            var output = new StringBuilder(count);
            
            for (int i = 3; i < count + 3; i++)
            {
                output.Append((char)originalBuffer[i]);
            }
            return output.ToString();
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
