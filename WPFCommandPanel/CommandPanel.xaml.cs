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
        //Will watch the folder that will contain the reports
        public FileSystemWatcher FileWatcher = new FileSystemWatcher(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AccessibilityTools\ReportGenerators-master\Reports");
        //Collection of FileDisplay objects that will be displayed in the panel
        public ObservableCollection<FileDisplay> file_paths = new AsyncObservableCollection<FileDisplay>();
        //Allows the program to control a single browser through multiple events and commands
        public ChromeDriver chrome;
        public WebDriverWait wait;
        public bool QuitThread = false;
        //Class to work best with the Listbox and FileSystemWatcher together.
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

        //Container for file info to be displayed
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
        //Object to control console output / put it into the terminal on the command panel
        public class ControlWriter : TextWriter
        {
            private TextBlock terminal;
            public ControlWriter(TextBlock send_here)
            {
                this.terminal = send_here;
            }

            public override void Write(char value)
            {
                if (value == '\n')
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
        //Init
        public CommandPanel()
        {
            InitializeComponent();
            //Set all console output to our own writer
            Console.SetOut(new ControlWriter(TerminalOutput));
            //Setup the events for the filewatcher
            this.FileWatcher.EnableRaisingEvents = true;
            this.FileWatcher.Created += new System.IO.FileSystemEventHandler(this.FileWatcher_Created);
            this.FileWatcher.Deleted += new System.IO.FileSystemEventHandler(this.FileWatcher_Deleted);
            this.FileWatcher.Renamed += new System.IO.RenamedEventHandler(this.FileWatcher_Renamed);
            //Init the listbox / file_paths container
            foreach (var d in new DirectoryInfo(FileWatcher.Path).GetFiles("*.xlsx"))
            {
                file_paths.Add(new FileDisplay(d.FullName));
            }
            //Setup the listbox
            ReportList.ItemsSource = file_paths;
            ReportList.DisplayMemberPath = "DisplayName";
            ReportList.SelectedValuePath = "FullName";
        }
        private void FileWatcher_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            //If a file is created that is an excel doc we want to display it
            if (!e.FullPath.Contains(".xlsx"))
            {
                return;
            }
            this.file_paths.Add(new FileDisplay(e.FullPath));
        }
        public void FileWatcher_Deleted(object sender, System.IO.FileSystemEventArgs e)
        {
            //Remove any excel docs from the list if they were deleted
            if (!e.FullPath.Contains(".xlsx"))
            {
                return;
            }
            this.file_paths.Remove(file_paths.First(f => f.FullName == e.FullPath));
        }
        public void FileWatcher_Renamed(object sender, System.IO.RenamedEventArgs e)
        {
            //If it was renamed we need to delete old one and create new one with new path
            if (!e.FullPath.Contains(".xlsx"))
            {
                return;
            }
            this.file_paths.Remove(file_paths.First(f => f.FullName == e.OldFullPath));
            this.file_paths.Add(new FileDisplay(e.FullPath));
        }
        public void OpenBrowserButton(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += OpenBrowser;
            worker.RunWorkerAsync();

        }
        private void OpenBrowser(object sender, DoWorkEventArgs e)
        {
            //Open the browser to be controlled
            this.chrome = new ChromeDriver(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AccessibilityTools\PowerShell\Modules\SeleniumTest");
            this.wait = new WebDriverWait(chrome, new TimeSpan(0, 0, 5));
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
        private void QuitProcess(object sender, EventArgs e)
        {
            QuitThread = true;
        }
        private void Ralt_Click(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;


            if (chrome?.Url?.Contains("instruct") == true)
            {
                worker.DoWork += CanvasRalt;
            } else if (chrome?.Url?.Contains("agilix") == true)
            {
                worker.DoWork += BuzzRalt;
            }
            else
            {
                MessageBox.Show("Not on a valid URL");
                return;
            }
            worker.RunWorkerAsync();
        }
        public class PoshHelper
        {
            public PoshHelper(ChromeDriver d, int s)
            {
                driver = d;
                i = s;
            }
            public ChromeDriver driver;
            public int i;
        }
        private void CanvasRalt(object sender, DoWorkEventArgs e)
        {
            var number_of_modules = chrome.FindElementsByClassName("item_name").Count(c => c.Text != "");
            for (int i = 0; i < number_of_modules; i++)
            {
                if (QuitThread)
                {
                    QuitThread = false;
                    return;
                }
                try
                {
                    wait.Until(c => c.FindElement(By.ClassName("item_name")));
                    chrome.FindElementsByClassName("item_name")[i].Click();
                    /*
                    PowerShell helper = PowerShell.Create();
                    helper.AddScript("param($c)\n" +
                        "process{" +
                        "($c.Driver.FindElementsByClassname(\"item_name\") | Select-Object -Index $c.i).click()" +
                        "}").AddArgument(new PoshHelper(this.chrome, i));
                    var job = helper.BeginInvoke();
                    for(var j = 0; j < 500; j++)
                    {
                        if (job.IsCompleted)
                        {
                            break;
                        }
                        if(j >= 450)
                        {
                            
                        }
                    }*/
                    wait.Until(c => c.FindElement(By.CssSelector("a[class*=\"edit\""))).Click();
                    if (chrome.Url.Contains("quiz"))
                    {
                        wait.Until(c => c.SwitchTo().Frame(c.FindElement(By.Id("quiz_description_ifr"))));
                        chrome.ExecuteScript("document.querySelector(\"img\").setAttribute('alt','')");
                        chrome.SwitchTo().ParentFrame();
                        chrome.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                        wait.Until(c => c.FindElement(By.CssSelector("button.save_quiz_button"))).Click();
                    }
                    else if (chrome.Url.Contains("assignment"))
                    {
                        wait.Until(c => c.SwitchTo().Frame(c.FindElement(By.Id("assignment_description_ifr"))));
                        chrome.ExecuteScript("document.querySelector(\"img\").setAttribute('alt','')");
                        chrome.SwitchTo().ParentFrame();
                        chrome.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                        wait.Until(c => c.FindElement(By.CssSelector("button.btn.btn-primary"))).Click();
                    }
                    else if (chrome.Url.Contains("discussion"))
                    {
                        wait.Until(c => c.SwitchTo().Frame(c.FindElement(By.Id("discussion-topic-message9_ifr"))));
                        chrome.ExecuteScript("document.querySelector(\"img\").setAttribute('alt','')");
                        chrome.SwitchTo().ParentFrame();
                        chrome.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                        wait.Until(c => c.FindElement(By.CssSelector("[data-text-while-loading]"))).Click();
                    }
                    else
                    {
                        wait.Until(c => c.SwitchTo().Frame(c.FindElement(By.Id("wiki_page_body_ifr"))));
                        chrome.ExecuteScript("document.querySelector(\"img\").setAttribute('alt','')");
                        chrome.SwitchTo().ParentFrame();
                        chrome.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                        wait.Until(c => c.FindElement(By.CssSelector("button.submit"))).Click();
                    }

                    wait.Until(c => c.FindElement(By.CssSelector("a[class*='edit']")));
                    chrome.FindElementByCssSelector("a[class='home']").Click();
                }
                catch
                {
                    try
                    {
                        chrome.SwitchTo().Window(chrome.CurrentWindowHandle);
                        if (chrome.Url.Contains("quiz"))
                        {
                            chrome.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                            wait.Until(c => c.FindElement(By.CssSelector("button.save_quiz_button"))).Click();
                        }
                        else if (chrome.Url.Contains("assignment"))
                        {
                            chrome.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                            wait.Until(c => c.FindElement(By.CssSelector("button.btn.btn-primary"))).Click();
                        }
                        else if (chrome.Url.Contains("discussion"))
                        {
                            chrome.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                            wait.Until(c => c.FindElement(By.CssSelector("[data-text-while-loading]"))).Click();
                        }
                        else
                        {
                            chrome.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                            wait.Until(c => c.FindElement(By.CssSelector("button.submit"))).Click();
                        }
                        wait.Until(c => c.FindElement(By.CssSelector("a[class*='edit']")));
                        chrome.FindElementByCssSelector("a[class='home']").Click();
                    }
                    catch
                    {
                        try
                        {
                            chrome.SwitchTo().Window(chrome.CurrentWindowHandle);
                            Console.WriteLine($"Failed to save page: {chrome.Url}");
                            chrome.FindElementsByCssSelector("a[class='home']")[0].Click();
                        }
                        catch
                        {
                            try
                            {
                                chrome.FindElementsByCssSelector("span.ui-icon-closethick").First(c => c.Text == "close").Click();

                                chrome.FindElementsByCssSelector("a[class='home']")[0].Click();
                            }
                            catch
                            {
                                Console.WriteLine("Probably at home page...");
                                try
                                {
                                    wait.Until(c => c.SwitchTo().Alert()).Accept();
                                }
                                catch
                                {
                                    Console.WriteLine("Its broken, sorry");
                                }
                            }
                        }
                    }
                }
            }
        }
        private void BuzzRalt(object sender, DoWorkEventArgs e)
        {

        }
        private void Login_Click(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += LoginToByu;
            worker.RunWorkerAsync();
        }
        private void LoginToByu(object sender, DoWorkEventArgs e)
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
        private void A11yHelp_Click(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += ShowA11yHelpers;
            worker.RunWorkerAsync();
        }
        private void ShowA11yHelpers(object sender, DoWorkEventArgs e)
        {
            void RecursiveA11y(int num = 0)
            {
                var num_frames = chrome.FindElementsByTagName("iframe").Count();
                if (num > 20){
                    //Fail safe to not get stuck
                    return;
                }
                for(int i = 0; i < num_frames; i++)
                {
                    chrome.SwitchTo().Frame(i);
                    RecursiveA11y((num + 1));
                }
                if(chrome.FindElementsByCssSelector(".AccessibilityHelper").Count() > 0)
                {
                    chrome.SwitchTo().ParentFrame();
                    return;
                }
                chrome.ExecuteScript(@"(function() { var e, t, o = document.querySelectorAll(""img""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", /*e.style.clear = ""right"",*/ e.style.cssFloat = o[t].style.cssFloat, e.style.margin = ""-7px -0px -7px -7px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""75px"", e.style.fontSize = ""12px"", e.style.zIndex = ""0"", e.textContent = ""Alt Text:\n"" + o[t].alt, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());javascript:(function() { var e, t, o = document.querySelectorAll(""iframe""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", e.style.clear = ""right"", /*e.style.cssFloat = ""left"",*/ e.style.margin = ""-7px -0px -7px -7px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""300px"", e.style.zIndex = ""0"", e.textContent = ""Iframe title:\n"" + o[t].title, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());javascript:(function() { var e, t, o = document.querySelectorAll(""h1""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", e.style.clear = ""right"",/* e.style.cssFloat = ""left"",*/ e.style.margin = ""-7px 0px 0px 0px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""35px"", e.style.zIndex = ""0"", e.textContent = o[t].tagName, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());(function() { var e, t, o = document.querySelectorAll(""h2""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", e.style.clear = ""right"", /*e.style.cssFloat = ""left"",*/ e.style.margin = ""-7px 0px 0px -7px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""35px"", e.style.zIndex = ""0"", e.textContent = o[t].tagName, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());(function() { var e, t, o = document.querySelectorAll(""h3""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", e.style.clear = ""right"", /*e.style.cssFloat = ""left"",*/ e.style.margin = ""-7px 0px 0px -7px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""35px"", e.style.zIndex = ""0"", e.textContent = o[t].tagName, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());(function() { var e, t, o = document.querySelectorAll(""h4""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", e.style.clear = ""right"", /*e.style.cssFloat = ""left"",*/ e.style.margin = ""-7px 0px 0px -7px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""35px"", e.style.zIndex = ""0"", e.textContent = o[t].tagName, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());(function() { var e, t, o = document.querySelectorAll(""h5""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", e.style.clear = ""right"", /*e.style.cssFloat = ""left"",*/ e.style.margin = ""-7px 0px 0px -7px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""35px"", e.style.zIndex = ""0"", e.textContent = o[t].tagName, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());(function() { var e, t, o = document.querySelectorAll(""h6""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", e.style.clear = ""right"", /*e.style.cssFloat = ""left"",*/ e.style.margin = ""-7px 0px 0px -7px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""35px"", e.style.zIndex = ""0"", e.textContent = o[t].tagName, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());
    (function() {
    document.querySelectorAll(""th"").forEach(c => {if(c.scope == ""row""){c.style.backgroundColor = ""green"";}else if(c.scope == ""col""){c.style.backgroundColor = ""blue"";}else{c.style.backgroundColor = ""red""}});
    document.querySelectorAll(""i"").forEach(c => c.style.backgroundColor = ""DeepPink"");
    document.querySelectorAll(""b"").forEach(c => c.style.backgroundColor = ""DarkViolet"");
    }());");
                chrome.SwitchTo().ParentFrame();
            }
            RecursiveA11y();
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
