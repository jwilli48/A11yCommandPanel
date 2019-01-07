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
using System.Web.Script.Serialization;
using System.Management.Automation;

namespace WPFCommandPanel
{
    /// <summary>
    /// Interaction logic for LoginTestCanvas.xaml
    /// </summary>
    public partial class LoginTestCanvas : Page
    {
        public LoginTestCanvas()
        {
            InitializeComponent();
        }
        private class InfoToSave
        {
            public InfoToSave(string token, string url)
            {
                Token = token;
                BaseUrl = url;
            }
            string Token { get; set; }
            string BaseUrl { get; set; }
        }
        private void SubmitLoginInfo(object sender, RoutedEventArgs e)
        {
            var TokenInfo = Token.Text;
            var BaseUrlInfo = BaseUrl.Text;
            var FilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\TEST_CanvasApiCreds.json";
            if (File.Exists(FilePath))
            {
                goto SkipFileCreation;
            }
            using (var posh = PowerShell.Create())
            {
                posh.AddScript(@"param($t, $b, $p)
                                    $ApiInfo = [ordered]@{
                                        Token = $t
                                        BaseUri = $b
                                    }
                                    $ApiInfo | ConvertTo-Json | Out-File -FilePath $p")
                                    .AddArgument(TokenInfo)
                                    .AddArgument(BaseUrlInfo)
                                    .AddArgument(FilePath)
                                    .Invoke();
            }
            SkipFileCreation:
            if (!(new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\MASTER_CanvasApiCreds.json").Exists))
            {
                WPFCommandPanel.MainWindow.AppWindow.ShowPage.Navigate(new LoginMasterCourses());
            }
            else if (!(new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\BYU_CanvasApiCreds.json").Exists))
            {
                WPFCommandPanel.MainWindow.AppWindow.ShowPage.Navigate(new LoginBYUOnlineCanvas());
            }
            else
            {
                WPFCommandPanel.MainWindow.AppWindow.ShowPage.Navigate(new CommandPanel());
            }
        }

    }
}
