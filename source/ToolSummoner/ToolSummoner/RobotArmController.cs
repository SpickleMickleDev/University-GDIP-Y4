using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace ToolSummoner
{
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
            bool portValid = true;
            try
            {
                testPort.Open();

                // send version request message
                byte[] buffer = new byte[] { 0xFF, 0xA2, 0xC7, 0xB3 };
                testPort.Write(buffer, 0, buffer.Length);

                // wait for response
                byte[]? response = ReadResponse(testPort, TimeSpan.FromMilliseconds(1000));
                if (response == null)
                {
                    portValid = false;
                }
                else
                {
                    Logger.LogLine($"Robot arm connected on port {comPort}. Embedded device version: {response[2]:X2}.{response[3]:X2}");
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

        private static byte[]? ReadResponse(SerialPort port, TimeSpan timeout)
        {
            var response = new List<byte>();
            var stopwatch = Stopwatch.StartNew();

            int state = 0;

            while (stopwatch.Elapsed < timeout)
            {
                int remainingMilliseconds =
                    Math.Max(1, (int)(timeout - stopwatch.Elapsed).TotalMilliseconds);

                // ReadTimeout applies to the next read operation.
                port.ReadTimeout = remainingMilliseconds;

                try
                {
                    int value = port.ReadByte();

                    switch (state)
                    {
                        case 0:
                            // Looking for 0xFF
                            if (value == 0xFF)
                            {
                                response.Clear();
                                response.Add(0xFF);
                                state = 1;
                            }
                            break;

                        case 1:
                            // Looking for 0xA2
                            if (value == 0xA2)
                            {
                                response.Add(0xA2);
                                state = 2;
                            }
                            else if (value == 0xFF)
                            {
                                // This may be the beginning of a new header.
                                response.Clear();
                                response.Add(0xFF);
                            }
                            else
                            {
                                response.Clear();
                                state = 0;
                            }
                            break;

                        case 2:
                            response.Add((byte)value);

                            if (value == 0xB3)
                            {
                                return response.ToArray();
                            }
                            break;
                    }
                }
                catch (TimeoutException)
                {
                    return null;
                }
            }

            return null;
        }
    }
}
