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
        public MainWindow()
        {
            InitializeComponent();
            AppWindow = this;
            
            /*if (!(new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\MASTER_CanvasApiCreds.json").Exists))
            {
                ShowPage.Navigate(new LoginMasterCourses());
            }
            else if(!(new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\BYU_CanvasApiCreds.json").Exists))
            {
                ShowPage.Navigate(new LoginTestCanvas());
            }
            else if(!(new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\TEST_CanvasApiCreds.json").Exists))
            {
                ShowPage.Navigate(new LoginBYUOnlineCanvas());
            }
            else
            {*/
                CommandPanelObj = new CommandPanel();
                ShowPage.Navigate(CommandPanelObj);
            //}
        }
    }
}
