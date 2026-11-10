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

namespace ToolSummoner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SoftwareController softwareController = new SoftwareController();
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