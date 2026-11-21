using ToolSummoner;

namespace ToolSummoner_Testing
{

    [TestClass]
    public sealed class TestSet_Unit
    {
        const string testPort = "COM5";

        [TestMethod]
        public void TestListenForGeneralMessages()
        {
            RobotArmController controller = new RobotArmController(testPort);

            Task.Run(() => controller.Run());
            Task.Run(() =>
            {
                Thread.Sleep(2000);
                controller.SendMessage_Drop(false);
            });
        }


        [TestMethod]
        public void TestMessageReceived_Calibrate()
        {
            RobotArmController controller = new RobotArmController(testPort);

            Task.Run(() => controller.Run());
            Task.Run(() =>
            {
                byte[] offsets = new byte[6] { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60 };
                Thread.Sleep(2000);
                controller.SendMessage_Calibrate(offsets, true);
            });
        }

        [TestMethod]
        public void TestMessageReceived_Drop()
        {
            RobotArmController controller = new RobotArmController(testPort);

            Task.Run(() => controller.Run());
            Task.Run(() =>
            {
                Thread.Sleep(2000);
                controller.SendMessage_Drop(true);
            });
        }

        [TestMethod]
        public void TestMessageReceived_GoToPosition()
        {
            RobotArmController controller = new RobotArmController(testPort);

            Task.Run(() => controller.Run());
            Task.Run(() =>
            {
                byte memoryIndex = 1;
                Thread.Sleep(2000);
                controller.SendMessage_GoToPosition(memoryIndex, true);
            });
        }

        [TestMethod]
        public void TestMessageReceived_Grab()
        {
            RobotArmController controller = new RobotArmController(testPort);

            Task.Run(() => controller.Run());
            Task.Run(() =>
            {
                Thread.Sleep(2000);
                controller.SendMessage_Grab(true);
            });
        }

        [TestMethod]
        public void TestMessageReceived_LogPosition()
        {
            RobotArmController controller = new RobotArmController(testPort);

            Task.Run(() => controller.Run());
            Task.Run(() =>
            {
                byte memoryIndex = 1;
                Thread.Sleep(2000);
                controller.SendMessage_LogPosition(memoryIndex, true);
            });
        }

        [TestMethod]
        public void TestMessageReceived_RequestVersionInfo()
        {
            RobotArmController controller = new RobotArmController(testPort);

            Task.Run(() => controller.Run());
            Task.Run(() =>
            {
                Thread.Sleep(2000);
                controller.SendMessage_RequestVersionInfo(true);
            });
        }

        [TestMethod]
        public void TestMessageReceived_SetPosition()
        {
            RobotArmController controller = new RobotArmController(testPort);

            Task.Run(() => controller.Run());
            Task.Run(() =>
            {
                byte memoryIndex = 1;
                byte[] values = new byte[6] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 };
                Thread.Sleep(2000);
                controller.SendMessage_SetPosition(memoryIndex, values, true);
            });
        }

        [TestMethod]
        public void TestMessageReceived_SetServo()
        {
            RobotArmController controller = new RobotArmController(testPort);

            Task.Run(() => controller.Run());
            Task.Run(() =>
            {
                byte[] values = new byte[6] { 0x1, 0x2, 0x3, 0x4, 0x5, 0x6 };
                bool useOffsets = true;
                Thread.Sleep(2000);
                controller.SendMessage_SetServo(values, useOffsets, true);
            });
        }
    }
}
