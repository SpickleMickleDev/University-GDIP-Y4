using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xaml;

namespace ToolSummoner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
#if DEBUG
        RobotArmController debugMessager;
        Thread robotArmControllerThread;
        private const int MaximumLogMessages = 100;

        int[] currentOffsetValues = new int[6] { 0, 0, 0, 0, 0, 0 };
        int[] currentPosValues = new int[6] { 0, 0, 0, 0, 0, 0 };
        int currentMemoryIndex = 0;
        bool currentUseOffset = false;
        bool liveTracking = false;
        bool cancelLiveTrackingThread = false;
        Thread liveTrackingThread;
        // messages to be sent to: TextBlock MessageLog;
#endif

        public MainWindow()
        {
            InitializeComponent();

            ErrorHandler.RaiseError += DisplayErrorMessage;
        }

        public void DisplayErrorMessage(Error error, EventWaitHandle eventWaiter)
        {
            Dispatcher.Invoke(() => {
                MessageBox.Show($"{(error.critical ? "CRITICAL " : "")}ERROR ({error.errorCode}): {error.errorMessage}");
                eventWaiter.Set();
            });
        }

#if DEBUG
        private void OpenDebugConsoleButton_Click(object sender, RoutedEventArgs e)
        {
            if (PortSelectionComboBox.SelectedItem == null)
            {
                return;
            }

            DebugItems.Visibility = Visibility.Visible;
            MainScreenItems.Visibility = Visibility.Collapsed;

            string selectedPort = PortSelectionComboBox.SelectedItem.ToString();

            /*if (RobotArmController.TestPort(selectedPort))
            {
                Controller_MessageReceived(null, $"Tested port {selectedPort} - PASS");
            }
            else
            {
                Controller_MessageReceived(null, $"Tested port {selectedPort} - FAIL");
            }*/
            
            debugMessager = new RobotArmController(selectedPort, dtrEnable: true, rtsEnable: true);
            debugMessager.MessageReceived += Controller_MessageReceived;

            liveTrackingThread = new Thread(LiveTracking);
            liveTrackingThread.Start();

            robotArmControllerThread = new Thread(debugMessager.Run);
            robotArmControllerThread.Name = "RobotArmController";
            robotArmControllerThread.Start();

        }

        private void Controller_MessageReceived(
            object? sender,
            string message)
        {
            Dispatcher.Invoke(() =>
            {
                MessageLog.Items.Add(message);

                while (MessageLog.Items.Count > MaximumLogMessages)
                {
                    MessageLog.Items.RemoveAt(0);
                }

                MessageLog.ScrollIntoView(
                    MessageLog.Items[MessageLog.Items.Count - 1]);
            });
        }

        private void UpdatePortSelectionOptions(object sender, MouseEventArgs e)
        {
            // Mouse entered the port selection combo box
            ComboBox cb = (ComboBox)sender;
            string[] visiblePorts = SerialPort.GetPortNames();

            cb.Items.Clear();
            foreach (string port in visiblePorts)
            {
                cb.Items.Add(port);
            }
        }

        private void Debug_SendMessage_Calibrate(object sender, RoutedEventArgs e)
        {
            // unchecked((byte)currentOffsetValues[0]) will send a int8_t byte as uint8_t byte and when parsed by the microcontroller, it will hold the same signed value
            byte[] byteArray = new byte[6];
            for (int i = 0; i < 6; i++) byteArray[i] = unchecked((byte)currentOffsetValues[i]);
            debugMessager.SendMessage_Calibrate(byteArray, false);
        }

        private void Debug_SendMessage_SetPosition(object sender, RoutedEventArgs e)
        {
            byte[] byteArray = new byte[6];
            for (int i = 0; i < 6; i++) byteArray[i] = Convert.ToByte(currentPosValues[i]);
            debugMessager.SendMessage_SetPosition(Convert.ToByte(currentMemoryIndex), byteArray, false);
        }

        private void Debug_SendMessage_GoToPosition(object sender, RoutedEventArgs e)
        {
            debugMessager.SendMessage_GoToPosition(Convert.ToByte(currentMemoryIndex), false);
        }

        private void Debug_SendMessage_SetServo(object sender, RoutedEventArgs e)
        {
            byte[] byteArray = new byte[6];
            for (int i = 0; i < 6; i++) byteArray[i] = Convert.ToByte(currentPosValues[i]);
            debugMessager.SendMessage_SetServo(byteArray, currentUseOffset, false);
        }

        private void Debug_SendMessage_Grab(object sender, RoutedEventArgs e)
        {
            debugMessager.SendMessage_Grab(false);
        }

        private void Debug_SendMessage_Drop(object sender, RoutedEventArgs e)
        {
            debugMessager.SendMessage_Drop(false);
        }

        private void Debug_SendMessage_LogPosition(object sender, RoutedEventArgs e)
        { 
            debugMessager.SendMessage_LogPosition(Convert.ToByte(currentMemoryIndex), false);
        }

        private void Debug_SendMessage_RequestVersionInfo(object sender, RoutedEventArgs e)
        {
            debugMessager.SendMessage_RequestVersionInfo(false);
        }

        private void ValueSlider1Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (Slider)sender;
            currentPosValues[0] = Convert.ToInt32(sldr.Value);
            ValueSlider1Previewer.Content = currentPosValues[0].ToString("");
        }

        private void ValueSlider2Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (Slider)sender;
            currentPosValues[1] = Convert.ToInt32(sldr.Value);
            ValueSlider2Previewer.Content = currentPosValues[1].ToString();
        }

        private void ValueSlider3Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (Slider)sender;
            currentPosValues[2] = Convert.ToInt32(sldr.Value);
            ValueSlider3Previewer.Content = currentPosValues[2].ToString();
        }

        private void ValueSlider4Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (Slider)sender;
            currentPosValues[3] = Convert.ToInt32(sldr.Value);
            ValueSlider4Previewer.Content = currentPosValues[3].ToString();
        }

        private void ValueSlider5Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (Slider)sender;
            currentPosValues[4] = Convert.ToInt32(sldr.Value);
            ValueSlider5Previewer.Content = currentPosValues[4].ToString();
        }

        private void ValueSlider6Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (Slider)sender;
            currentPosValues[5] = Convert.ToInt32(sldr.Value);
            ValueSlider6Previewer.Content = currentPosValues[5].ToString();
        }

        private void OffsetSlider1Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (Slider)sender;
            currentOffsetValues[0] = Convert.ToInt32(sldr.Value);
            OffsetSlider1Previewer.Content = currentOffsetValues[0].ToString();
        }

        private void OffsetSlider2Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (Slider)sender;
            currentOffsetValues[1] = Convert.ToInt32(sldr.Value);
            OffsetSlider2Previewer.Content = currentOffsetValues[1].ToString();
        }

        private void OffsetSlider3Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (Slider)sender;
            currentOffsetValues[2] = Convert.ToInt32(sldr.Value);
            OffsetSlider3Previewer.Content = currentOffsetValues[2].ToString();
        }

        private void OffsetSlider4Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (Slider)sender;
            currentOffsetValues[3] = Convert.ToInt32(sldr.Value);
            OffsetSlider4Previewer.Content = currentOffsetValues[3].ToString();
        }

        private void OffsetSlider5Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (Slider)sender;
            currentOffsetValues[4] = Convert.ToInt32(sldr.Value);
            OffsetSlider5Previewer.Content = currentOffsetValues[4].ToString();
        }

        private void OffsetSlider6Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (Slider)sender;
            currentOffsetValues[5] = Convert.ToInt32(sldr.Value);
            OffsetSlider6Previewer.Content = currentOffsetValues[5].ToString();
        }

        private void MemoryIndexSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (Slider)sender;
            currentMemoryIndex = Convert.ToInt32(sldr.Value);
            MemoryIndexSliderPreviewer.Content = currentMemoryIndex.ToString();
        }

        private void UseOffsetSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (Slider)sender;
            currentUseOffset = (sldr.Value == 1);
            UseOffsetSliderPreviewer.Content = currentUseOffset.ToString();
        }

        private void LiveMovementCheckbox_Clicked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            bool? cbChecked = cb.IsChecked;

            liveTracking = (cbChecked == true);
        }

        private void LiveTracking()
        {
            while (!cancelLiveTrackingThread)
            {
                if (liveTracking)
                {
                    Debug_SendMessage_SetServo(null, null); // Send the current state
                }
                Thread.Sleep(250);
            }
        }
#endif
    }
}



// M-V-VM
// Model = Software Controller
// View-Model = MainWindow.xaml.cs
// View = MainWindow.xaml

// List of actions and their directions:
// Populate item list (M->V)
// Item selected (V->M)
// Display error (M->V)
// Admin changes config (V-M)
// AI chat (V<->M)