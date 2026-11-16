using System;
using System.ComponentModel;

namespace ToolSummoner
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel()
        {
            // Properties exposed to XAML
            // Commands invoked by XAML
            // Calls to application services
        }

        /*
         * SoftwareController softwareController = new SoftwareController();
            Thread softwareControllerThread;

            public MainWindow()
            {
                InitializeComponent();

                ParameterizedThreadStart threadStart = new ParameterizedThreadStart(softwareController.RunSoftware);
                softwareControllerThread = new Thread(new ThreadStart(softwareController.RunSoftware));
                softwareControllerThread.Start();
            }

            public void DisplayError(string message, bool closeApplication)
            {

            }

            public void ItemSelected()
            {

            }

            public void PopulateItemList()
            {

            }
         */

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
