using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ToolSummoner
{
    public static class RobotArmICD
    {
        public const byte COMMS_START_BYTE_1 = 0xFF;
        public const byte COMMS_START_BYTE_2 = 0xF6;
        public const byte COMMS_END_BYTE = 0x5F;

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

        private readonly ConcurrentQueue<byte[]> messagesToSendQueue = new();
        private readonly SemaphoreSlim commandLock = new(1, 1);
        private readonly object pendingRequestLock = new();
        private TaskCompletionSource<bool>? pendingCompletion;
        private byte pendingOperation;
        private volatile bool keepListening = true;

        public event EventHandler<string>? MessageReceived;
        private void PublishMessage(string message)
        {
            Debug.WriteLine(message);
            MessageReceived?.Invoke(this, message);
        }

        public RobotArmController(string comPort, bool dtrEnable = true, bool rtsEnable = true, bool openPort = true)
        {
            serialPort = new SerialPort(comPort, DefaultBaudRate)
            {
                DtrEnable = dtrEnable,
                RtsEnable = rtsEnable,
                Handshake = Handshake.None,
                ReadTimeout = 1000
            };

            if (openPort)
            {
                serialPort.Open();
            }
            Thread.Sleep(2000);

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
                if (messagesToSendQueue.TryDequeue(out byte[]? message))
                {
                    SendMessageOverSerial(message);
                }

                ListenForMessages();
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
                        PublishMessage($"{DateTime.Now.ToString("HH:mm:ss")} - ACK message received");
                        break;
                    case RobotArmICD.RESPONSE_ERROR:
                        PublishMessage($"{DateTime.Now.ToString("HH:mm:ss")} - ERROR message received with code {response[3]:X2}");
                        lock (pendingRequestLock)
                        {
                            pendingCompletion?.TrySetResult(false);
                        }
                        break;
                    case RobotArmICD.RESPONSE_COMPLETE:
                        PublishMessage($"{DateTime.Now.ToString("HH:mm:ss")} - COMPLETE message received [0x{response[3]:X2}]");
                        lock (pendingRequestLock)
                        {
                            if (response[3] == pendingOperation)
                            {
                                pendingCompletion?.TrySetResult(true);
                            }
                        }
                        break;
                    case RobotArmICD.RESPONSE_STORED_POS:
                        string positionData = "";
                        for (int i = 3; i < 15; i++)
                        {
                            positionData += $"{response[i]}, ";
                        }
                        PublishMessage($"{DateTime.Now.ToString("HH:mm:ss")} - STORED POSITION message received: {positionData}");
                        break;
                    case RobotArmICD.RESPONSE_VERSION_INFO:
                        PublishMessage($"{DateTime.Now.ToString("HH:mm:ss")} - VERSION INFO message received with version {response[3]:D2}.{response[4]:D2}");

                        break;
                    case RobotArmICD.RESPONSE_GENERAL_MESSAGE:
                        PublishMessage($"{DateTime.Now.ToString("HH:mm:ss")} - GENERAL message received: {DecodeGeneralMessage(response)}");
                        break;
                    default:
                        break;
                }
            }
        }

        private bool QueueMessage(byte[] message, byte operation, bool waitUntilCompletion)
        {
            if (!waitUntilCompletion)
            {
                messagesToSendQueue.Enqueue(message);
                return true;
            }

            commandLock.Wait();

            try
            {
                var completion = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                lock (pendingRequestLock)
                {
                    pendingOperation = operation;
                    pendingCompletion = completion;
                }

                messagesToSendQueue.Enqueue(message);

                return completion.Task.Wait(TimeSpan.FromSeconds(20)) &&
                       completion.Task.Result;
            }
            catch (AggregateException)
            {
                return false;
            }
            finally
            {
                lock (pendingRequestLock)
                {
                    if (ReferenceEquals(pendingCompletion, null) == false)
                    {
                        pendingCompletion = null;
                    }
                }

                commandLock.Release();
            }
        }

        public bool SendMessage_Calibrate(byte[] servoOffsets, bool waitUntilCompletion)
        {
            if (servoOffsets is null || servoOffsets.Length != 6) return false;
            // Will have 1 operation + 6 parameters
            byte[] message = new byte[7];

            message[0] = RobotArmICD.OPERATION_CALIBRATE;
            Array.Copy(servoOffsets, 0, message, 1, servoOffsets.Length);

            return QueueMessage(message, RobotArmICD.OPERATION_CALIBRATE, waitUntilCompletion);
        }

        public bool SendMessage_SetPosition(byte memoryIndex, byte[] servoValues, bool waitUntilCompletion)
        {
            if (servoValues is null || servoValues.Length != 6) return false;
            // Will have 1 operation + 7 parameters
            byte[] message = new byte[8];

            message[0] = RobotArmICD.OPERATION_SET_POS;
            message[1] = memoryIndex;
            Array.Copy(servoValues, 0, message, 2, servoValues.Length);

            return QueueMessage(message, RobotArmICD.OPERATION_SET_POS, waitUntilCompletion);
        }

        public bool SendMessage_GoToPosition(byte memoryIndex, bool waitUntilCompletion)
        {
            // Will have 1 operation + 1 parameter
            byte[] message = { RobotArmICD.OPERATION_GOTO_POS, memoryIndex };

            return QueueMessage(message, RobotArmICD.OPERATION_GOTO_POS, waitUntilCompletion);
        }

        public bool SendMessage_SetServo(byte[] servoValues, bool useOffsets, bool waitUntilCompletion)
        {
            if (servoValues is null || servoValues.Length != 6) return false;
            // Will have 1 operation + 7 parameters
            byte[] message = new byte[8];

            message[0] = RobotArmICD.OPERATION_SET_SERVO;
            message[7] = (byte)((useOffsets) ? 0x01 : 0x00);
            Array.Copy(servoValues, 0, message, 1, servoValues.Length);

            return QueueMessage(message, RobotArmICD.OPERATION_SET_SERVO, waitUntilCompletion);
        }

        public bool SendMessage_Grab(bool waitUntilCompletion)
        {
            // Will have 1 operation + 0 parameters
            byte[] message = { RobotArmICD.OPERATION_GRAB };

            return QueueMessage(message, RobotArmICD.OPERATION_GRAB, waitUntilCompletion);
        }

        public bool SendMessage_Drop(bool waitUntilCompletion)
        {
            // Will have 1 operation + 0 parameters
            byte[] message = { RobotArmICD.OPERATION_DROP };

            return QueueMessage(message, RobotArmICD.OPERATION_DROP, waitUntilCompletion);
        }

        public bool SendMessage_LogPosition(byte memoryIndex, bool waitUntilCompletion)
        {
            // Will have 1 operation + 1 parameter
            byte[] message = { RobotArmICD.OPERATION_LOG_POS, memoryIndex };

            return QueueMessage(message, RobotArmICD.OPERATION_LOG_POS, waitUntilCompletion);
        }

        public bool SendMessage_RequestVersionInfo(bool waitUntilCompletion)
        {
            // Will have 1 operation + 0 parameters
            byte[] message = { RobotArmICD.OPERATION_REQ_VER_INFO };

            return QueueMessage(message, RobotArmICD.OPERATION_REQ_VER_INFO, waitUntilCompletion);
        }

        private void SendMessageOverSerial(byte[] messageContents) // Includes both operation and the parameters
        {
            int messageContentsLength = messageContents.Length;
            byte[] fullMessage = new byte[messageContentsLength + 3];
            fullMessage[0] = RobotArmICD.COMMS_START_BYTE_1;
            fullMessage[1] = RobotArmICD.COMMS_START_BYTE_2;
            Array.Copy(messageContents, 0, fullMessage, 2, messageContentsLength);
            fullMessage[messageContentsLength + 2] = RobotArmICD.COMMS_END_BYTE;

            Debug.WriteLine($"Sending {Convert.ToHexString(fullMessage)}");
            serialPort.Write(fullMessage, 0, fullMessage.Length);
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

        public static bool TestPort(string comPort)
        { // Overload for if we don't care about what the embedded software version is
            return TestPort(comPort, out _);
        }
        public static bool TestPort(string comPort, out string versionInfo)
        {
            SerialPort testPort = new SerialPort(comPort, DefaultBaudRate);
            versionInfo = "?.?";
            testPort.DtrEnable = true;
            testPort.RtsEnable = true;
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

                // wait for a response
                // We may be receiving multiple messages so repeat many times with a short timeout
                byte[]? response;
                int attempts = 0;
                int maxAttempts = 10;

                // Attempt to read a response. If no message found or the message type isn't a version info message then try again. Try a max of 10 times
                do
                {
                    response = ReadResponse(testPort, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(2000));
                    if (response != null && response[2] == RobotArmICD.RESPONSE_GENERAL_MESSAGE) attempts--; // If we are receigin a general message, do not add to our list of attempts.
                } while ((response == null || response[2] != RobotArmICD.RESPONSE_VERSION_INFO) && ++attempts < maxAttempts);


                if (response != null)
                {
                    portValid = (response[0] == RobotArmICD.COMMS_START_BYTE_1) &&
                                (response[1] == RobotArmICD.COMMS_START_BYTE_2) &&
                                (response[2] == RobotArmICD.RESPONSE_VERSION_INFO) &&
                              //(response[3] == ????) // major version
                              //(response[4] == ????) // minor version
                                (response[5] == RobotArmICD.COMMS_END_BYTE);

                    versionInfo = $"{response[3]:X2}.{response[4]:X2}";
                    /*if (portValid)
                    {
                        Logger.LogLine($"Robot arm connected on port {comPort}. Embedded device version: {response[3]:X2}.{response[4]:X2}");
                    }*/
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
                    if (!port.IsOpen) port.Open();
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
                catch (Exception ex)
                {
                    ErrorHandler.AddError(new Error(ex.HResult, ex.Message));
                }
            }
            
            port.ReadTimeout = prevPortTimeout;
            return null;
        }
    }
}
