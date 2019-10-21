using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace WPFCommandPanel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Need to referance the main window so that we can navigate to new pages from other elements
        public static MainWindow AppWindow;
        //Not really needed to have this reference but if we want to add more tabs / pages then we can store the old pages so we don't lose any data from them.
        public static CommandPanel CommandPanelObj;
        public static A11yViewer a11YViewer;
        public static A11yRepair a11YRepair;

        public MainWindow()
        {
            InitializeComponent();
            AppWindow = this;
            
            CommandPanelObj = new CommandPanel();
            a11YViewer = new A11yViewer();
            a11YRepair = new A11yRepair();
            ShowPage.Navigate(CommandPanelObj);
        }

        private void SwitchA11yReview(object sender, RoutedEventArgs e)
        {
            ShowPage.Navigate(CommandPanelObj);
        }

        private void SwitchA11yViewer(object sender, RoutedEventArgs e)
        {
            ShowPage.Navigate(a11YViewer);
        }

        private void SwitchA11yRepair(object sender, RoutedEventArgs e)
        {
            ShowPage.Navigate(a11YRepair);
        }
    }
}
