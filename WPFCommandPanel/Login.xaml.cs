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

namespace WPFCommandPanel
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Page
    {
        public Login()
        {
            InitializeComponent();
        }
        private void SubmitLoginInfo(object sender, RoutedEventArgs e)
        {
            var UserName = UsernameBox.Text;
            var PassWord = PasswordBox.SecurePassword;
            //Save the passwords
            WPFCommandPanel.MainWindow.AppWindow.ShowPage.Navigate(new CommandPanel());
        }
    }
}
