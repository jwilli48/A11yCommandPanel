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
using System.ComponentModel;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Collections.ObjectModel;
using System.Threading;
using System.Collections.Specialized;
using ReportGenerators;
using System.Management.Automation;

namespace WPFCommandPanel
{
    /// <summary>
    /// Interaction logic for CommandPanel.xaml
    /// </summary>
    public partial class CommandPanel : Page
    {
        public FileSystemWatcher FileWatcher = new FileSystemWatcher(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AccessibilityTools\ReportGenerators-master\Reports");
        public ObservableCollection<FileDisplay> file_paths = new AsyncObservableCollection<FileDisplay>();
        public ChromeDriver chrome;
        public WebDriverWait wait;
        public class AsyncObservableCollection<T> : ObservableCollection<T>
        {
            //Solution to :
            //This type of CollectionView does not support changes to its SourceCollection from a thread different from the Dispatcher thread 
            //error. Copied from: https://www.thomaslevesque.com/2009/04/17/wpf-binding-to-an-asynchronous-collection/
            private SynchronizationContext _synchronizationContext = SynchronizationContext.Current;

            public AsyncObservableCollection()
            {
            }

            public AsyncObservableCollection(IEnumerable<T> list)
                : base(list)
            {
            }

            protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
            {
                if (SynchronizationContext.Current == _synchronizationContext)
                {
                    // Execute the CollectionChanged event on the current thread
                    RaiseCollectionChanged(e);
                }
                else
                {
                    // Raises the CollectionChanged event on the creator thread
                    _synchronizationContext.Send(RaiseCollectionChanged, e);
                }
            }

            private void RaiseCollectionChanged(object param)
            {
                // We are in the creator thread, call the base implementation directly
                base.OnCollectionChanged((NotifyCollectionChangedEventArgs)param);
            }

            protected override void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                if (SynchronizationContext.Current == _synchronizationContext)
                {
                    // Execute the PropertyChanged event on the current thread
                    RaisePropertyChanged(e);
                }
                else
                {
                    // Raises the PropertyChanged event on the creator thread
                    _synchronizationContext.Send(RaisePropertyChanged, e);
                }
            }

            private void RaisePropertyChanged(object param)
            {
                // We are in the creator thread, call the base implementation directly
                base.OnPropertyChanged((PropertyChangedEventArgs)param);
            }
        }
        

        public class FileDisplay
        {
            public FileDisplay(string path)
            {
                DisplayName = path.Split('\\').Last();
                FullName = path;
            }
            public string DisplayName { get; set; }
            public string FullName { get; set; }

        }
        public class ControlWriter : TextWriter
        {
            private TextBlock terminal;
            public ControlWriter(TextBlock send_here)
            {
                this.terminal = send_here;
            }

            public override void Write(char value)
            {
                if(value == '\n')
                {
                    return;
                }
                base.Write(value);
                terminal.Dispatcher.Invoke(() =>
                {
                    terminal.Inlines.Add(value.ToString());
                });
            }

            public override void Write(string value)
            {
                base.Write(value);
                terminal.Dispatcher.Invoke(() =>
                {
                    terminal.Inlines.Add(value);
                });
            }

            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }
        }
        public CommandPanel()
        {
            InitializeComponent();

            Console.SetOut(new ControlWriter(TerminalOutput));
            this.FileWatcher.EnableRaisingEvents = true;
            this.FileWatcher.Created += new System.IO.FileSystemEventHandler(this.FileWatcher_Created);
            this.FileWatcher.Deleted += new System.IO.FileSystemEventHandler(this.FileWatcher_Deleted);
            this.FileWatcher.Renamed += new System.IO.RenamedEventHandler(this.FileWatcher_Renamed);

            foreach (var d in new DirectoryInfo(FileWatcher.Path).GetFiles("*.xlsx"))
            {
                file_paths.Add(new FileDisplay(d.FullName));
            }
            ReportList.ItemsSource = file_paths;
            ReportList.DisplayMemberPath = "DisplayName";
            ReportList.SelectedValuePath = "FullName";
        }
        private void FileWatcher_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            if (!e.FullPath.Contains(".xlsx"))
            {
                return;
            }
            this.file_paths.Add(new FileDisplay(e.FullPath));
        }
        public void FileWatcher_Deleted(object sender, System.IO.FileSystemEventArgs e)
        {
            if (!e.FullPath.Contains(".xlsx"))
            {
                return;
            }
            this.file_paths.Remove(file_paths.First(f => f.FullName == e.FullPath));
        }
        public void FileWatcher_Renamed(object sender, System.IO.RenamedEventArgs e)
        {
            if (!e.FullPath.Contains(".xlsx"))
            {
                return;
            }
            this.file_paths.Remove(file_paths.First(f => f.FullName == e.OldFullPath));
            this.file_paths.Add(new FileDisplay(e.FullPath));
        }
        public void OpenBrowserButton(object sender, EventArgs e)
        {
            chrome = new ChromeDriver(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AccessibilityTools\PowerShell\Modules\SeleniumTest");
            wait = new WebDriverWait(chrome, new TimeSpan(0, 0, 5));
        }
        private void Canvas_Click(object sender, EventArgs e)
        {
            chrome.Url = "https://byu.instructure.com";
        }

        private void MasterCanvas_Click(object sender, EventArgs e)
        {
            chrome.Url = "https://byuismastercourses.instructure.com";
        }

        private void TestCanvas_Click(object sender, EventArgs e)
        {
            chrome.Url = "https://byuistest.instructure.com";
        }
        private void Ralt_Click(object sender, EventArgs e)
        {
            using (PowerShell posh = PowerShell.Create())
            {
                string path;
                if (chrome?.Url?.Contains("instruct") == true)
                {
                    path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AccessibilityTools\PowerShell\QuickScripts\canvasReplaceAlt.ps1";
                }else if (chrome?.Url?.Contains("agilix") == true)
                {
                    path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AccessibilityTools\PowerShell\QuickScripts\buzzReplaceAlt.ps1";
                }
                else
                {
                    MessageBox.Show("Not on a valid URL");
                    return;
                }
                string script = File.ReadAllText(path);
                posh.AddScript(script);
                Collection<PSObject> results = posh.Invoke();
            }
        }

        private void Login_Click(object sender, EventArgs e)
        {    
            string username = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AccessibilityTools\Credentials\MyByuUserName.txt").Replace("\n", "").Replace("\r", "");
            
            var posh = PowerShell.Create();
            posh.AddScript("process{$c = Get-Content \"$HOME\\Desktop\\AccessibilityTools\\Credentials\\MyByuPassword.txt\"; $s = $c | ConvertTo-SecureString; Write-Host (New-Object System.Management.Automation.PSCredential -ArgumentList 'asdf', $s).GetNetworkCredential().Password}"
                );
            posh.Invoke();
            var password = posh.Streams.Information[0].ToString();
            if (chrome.Url.Contains("instructure") || chrome.Url.Contains("cas"))
            {
                wait.Until(c => c.FindElement(By.Id("netid"))).SendKeys(username);
                wait.Until(c => c.FindElement(By.CssSelector("input#password"))).SendKeys(password);
                wait.Until(c => c.FindElement(By.CssSelector("input[value*=\"Sign\"]"))).Submit();
            }
            else
            {
                chrome.SwitchTo().Window("Brigham Young University Sign-in Service");
                wait.Until(c => c.FindElement(By.Id("netid"))).SendKeys(username);
                wait.Until(c => c.FindElement(By.CssSelector("input#password"))).SendKeys(password);
                wait.Until(c => c.FindElement(By.CssSelector("input[value*=\"Sign\"]"))).Submit();
                chrome.SwitchTo().Window(chrome.CurrentWindowHandle);
            }
        }
        
        private void GenerateReport_Click(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += CreateReport;
            worker.RunWorkerCompleted += ReportFinished;
            worker.RunWorkerAsync();
        }
        private void ReportFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"{e.Result}");
            });
        }
        private void CreateReport(object sender, DoWorkEventArgs e)
        {
            var s = new System.Diagnostics.Stopwatch();
            s.Start();
            this.Dispatcher.Invoke(() =>
            {
                Run run = new Run($"Generating Report\n");
                run.Foreground = System.Windows.Media.Brushes.Cyan;
                TerminalOutput.Inlines.Add(run);
            });

            var checkedDomain = this.Dispatcher.Invoke(() =>
            {
                var domain = RadioButtonGroup?.Children?.OfType<RadioButton>()
                ?.FirstOrDefault(r => r.IsChecked == true)
                ?.Content
                ?.ToString();
                return domain;
            });
            if (checkedDomain == null)
            {
                this.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("No domain chosen.");
                });
                return;
            }
            var text = this.Dispatcher.Invoke(() =>
            {
                return CourseID.Text;
            });
            if (text == null)
            {
                this.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("No course ID found.");
                });
                return;
            }
            CanvasApi.ChangeDomain(checkedDomain);
            CourseInfo course;
            if (int.TryParse(text, out int id))
            {
                //It is an ID, create course info with new id var
                course = new CourseInfo(id);
            }
            else
            {
                //Just send it in as a string path
                course = new CourseInfo(text);
            }
            
            //Code to run report... (need to copy / add ReportGenerator code or move this project to ReportGenerator solution and use this as the executable.
            A11yParser ParseForA11y = new A11yParser();
            foreach (var page in course.PageHtmlList)
            {
                ParseForA11y.ProcessContent(page);
            }
            CreateExcelReport GenReport = new CreateExcelReport(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + $"\\AccessibilityTools\\ReportGenerators-master\\Reports\\ARC_{course.CourseCode}.xlsx");
            GenReport.CreateReport(ParseForA11y.Data, null, null);
            s.Stop();
            e.Result = $"Report generated.\nTime taken: {s.Elapsed.ToString(@"hh\:mm\:ss")}";
        }
        private void ReportList_DoubleClick(object sender, EventArgs e)
        {
            if (new FileInfo(ReportList.SelectedValue.ToString()).Exists)
            {
                System.Diagnostics.Process.Start(ReportList.SelectedValue.ToString());
            }
            else
            {
                MessageBox.Show("File no longer exists");
            }
        }
        private void mReportsButton_Click(object sender, EventArgs e)
        {
            //Insert code for mReports. May be easiest to create a POSH terminal and just copy paste the script over
            using(PowerShell posh = PowerShell.Create())
            {
                string script = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AccessibilityTools\PowerShell\MoveReports.ps1");
                posh.AddScript(script);
                Collection<PSObject> results = posh.Invoke();
                foreach(var obj in results)
                {
                    Run run = new Run("Report:\n");
                    run.Foreground = System.Windows.Media.Brushes.Cyan;
                    TerminalOutput.Inlines.Add(run);
                    TerminalOutput.Inlines.Add(obj.ToString().Remove(0,2).Replace("; ", "\n").Replace("=", ": "));
                }
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            TerminalOutput.Text = "";
        }
    }
}
