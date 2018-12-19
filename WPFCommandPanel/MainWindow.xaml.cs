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
        public static MainWindow AppWindow;
        public static CommandPanel CommandPanelObj;
        public MainWindow()
        {
            InitializeComponent();
            AppWindow = this;
            
            if (!(new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\MASTER_CanvasApiCreds.json").Exists))
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
            {
                CommandPanelObj = new CommandPanel();
                ShowPage.Navigate(CommandPanelObj);
            }
        }
    }
}
