using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WPFCommandPanel
{
    class PanelOptions : INotifyPropertyChanged
    {
        private string reportPath;
        public string ReportPath
        {
            get { return reportPath; }
            set {
                if (value != reportPath)
                {
                    reportPath = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string highscorePath;
        public string HighscorePath
        {
            get { return highscorePath; }
            set
            {
                if (value != highscorePath)
                {
                    highscorePath = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string powerShellScriptDir;
        public string PowerShellScriptDir
        {
            get { return powerShellScriptDir; }
            set
            {
                if (value != powerShellScriptDir)
                {
                    powerShellScriptDir = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string courseBackupDir;
        public string CourseBackupDir
        {
            get { return courseBackupDir; }
            set
            {
                if (value != courseBackupDir)
                {
                    courseBackupDir = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string chromeDriverPath;
        public string ChromeDriverPath
        {
            get { return chromeDriverPath; }
            set
            {
                if (value != chromeDriverPath)
                {
                    chromeDriverPath = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string firefoxDriverPath;
        public string FirefoxDriverPath
        {
            get { return firefoxDriverPath; }
            set
            {
                if (value != firefoxDriverPath)
                {
                    firefoxDriverPath = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string[] filesToIgnore;
        public string[] FilesToIgnore
        {
            get { return filesToIgnore; }
            set
            {
                if (value != filesToIgnore)
                {
                    filesToIgnore = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private Dictionary<string, string> byuOnlineCreds;
        public Dictionary<string, string> BYUOnlineCreds
        {
            get { return byuOnlineCreds; }
            set
            {
                if (value != byuOnlineCreds)
                {
                    byuOnlineCreds = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private Dictionary<string, string> byuIsTestCreds;
        public Dictionary<string, string> BYUISTestCreds
        {
            get { return byuIsTestCreds; }
            set
            {
                if (value != byuIsTestCreds)
                {
                    byuIsTestCreds = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private Dictionary<string, string> byuMasterCoursesCreds;
        public Dictionary<string, string> BYUMasterCoursesCreds
        {
            get { return byuMasterCoursesCreds; }
            set
            {
                if (value != byuMasterCoursesCreds)
                {
                    byuMasterCoursesCreds = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string iDriveContentUrl;
        public string IDriveContentUrl
        {
            get { return iDriveContentUrl; }
            set
            {
                if (value != iDriveContentUrl)
                {
                    iDriveContentUrl = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string qDriveContentUrl;
        public string QDriveContentUrl
        {
            get { return qDriveContentUrl; }
            set
            {
                if (value != qDriveContentUrl)
                {
                    qDriveContentUrl = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string excelTemplatePath;
        public string ExcelTemplatePath
        {
            get { return excelTemplatePath; }
            set
            {
                if (value != excelTemplatePath)
                {
                    excelTemplatePath = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string jsonDataDir;
        public string JsonDataDir
        {
            get { return jsonDataDir; }
            set
            {
                if (value != jsonDataDir)
                {
                    jsonDataDir = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private Dictionary<string, string> brightCoveCred;
        public Dictionary<string, string> BrightCoveCred
        {
            get { return brightCoveCred; }
            set
            {
                if (value != brightCoveCred)
                {
                    brightCoveCred = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string googleApi;
        public string GoogleApi
        {
            get { return googleApi; }
            set
            {
                if (value != googleApi)
                {
                    googleApi = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private Dictionary<string, string> byuCred;
        public Dictionary<string, string> ByuCred
        {
            get { return byuCred; }
            set
            {
                if (value != byuCred)
                {
                    byuCred = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
