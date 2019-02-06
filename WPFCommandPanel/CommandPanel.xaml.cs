﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
using My.CanvasApi;
using My.SeleniumExtentions;
using My.StringExtentions;
using System.Text.RegularExpressions;

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
        //Flag to quit a given opperation. Should add checks for it in various places so it can jsut end the event or funciton.
        public bool QuitThread = false;
        
        public class PageReviewer
        {   //Object to review the current webpage for the user and hold the data. Is reset whenever they click the CreateReport button.
            public PageReviewer()
            {
                A11yReviewer = new A11yParser();
                MediaReviewer = new MediaParser();
                LinkReviewer = new LinkParser("None");
            }
            public A11yParser A11yReviewer { get; set; }
            public MediaParser MediaReviewer { get; set; }
            public LinkParser LinkReviewer { get; set; }
        }
        public PageReviewer PageParser;
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
            HighScoreBox.Text = "HighScore: " + File.ReadAllText(@"M:\DESIGNER\Content EditorsELTA\Accessibility Assistants\HIGHSCORE.txt");
            //PageParser = new PageReviewer();
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
            if(e.Name == e.OldName)
            {
                return;
            }
            if (!e.OldName.Contains(".xlsx"))
            {   //Excel for some reason creates a .tmp file everytime you save the excel document and then renames that to the correct name
                //This causes this event to be run even though it is not needed, so just return in that case
                return;
            }
            try
            {
                file_paths.Remove(file_paths.FirstOrDefault(f => f.FullName == e.OldFullPath));
                file_paths.Add(new FileDisplay(e.FullPath));
            }
            catch
            {
                Console.WriteLine("Failed to register file rename...");
            }
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
            var chromeDriverService = ChromeDriverService.CreateDefaultService(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AccessibilityTools\PowerShell\Modules\SeleniumTest");
            chromeDriverService.HideCommandPromptWindow = true;
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("user-data-dir=" + Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data");
            try
            {
                chrome = new ChromeDriver(chromeDriverService, chromeOptions);
                wait = new WebDriverWait(chrome, new TimeSpan(0, 0, 1));
            }
            catch
            {
                Console.WriteLine("Failed to open browser with profile, check if Chrome is already open. Opening default chrome...");
                chrome = new ChromeDriver(chromeDriverService);
                wait = new WebDriverWait(chrome, new TimeSpan(0, 0, 1));
            }
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
        {   //Runs the ralt function reworked into c#. Will run either the buzz or canvas ones, if not on eithe rof those pages then fails.
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
                MessageBox.Show("Not on a valid page");
                return;
            }
            worker.RunWorkerAsync();
        }
        public class PoshHelper
        {   //Helper object for if I run into the problem again of a popup freezing everything
            public PoshHelper(ChromeDriver d, int s)
            {
                driver = d;
                i = s;
            }
            public ChromeDriver driver;
            public int i;
        }
        public class StoreWebElement
        {
            public string text { get; set; }
            public List<string> class_list { get; set; }
            public string id { get; set; }
            public override string ToString()
            {
                string s;
                s = $"Id: {id}\n";
                s += $"Text: {text}\n";
                s += "Class list: ";
                if(class_list == null)
                {
                    return s;
                }
                foreach(string i in class_list)
                {
                    s += i + " ";
                }
                return s;
            }
        }
        private void CanvasRalt(object sender, DoWorkEventArgs e)
        {
            var s = new System.Diagnostics.Stopwatch();
            s.Start();
            var number_of_modules = chrome.FindElementsByClassName("context_module_item").Count(c => c.Text != "");
            var home_page_url = chrome.Url;
            wait.Timeout = new TimeSpan(0, 0, 3);
            Dispatcher.Invoke(() =>
            {
                Run run = new Run($"RALT estimated time (10 seconds per page, {number_of_modules} pages) : {TimeSpan.FromSeconds(number_of_modules * 10).ToString(@"hh\:mm\:ss")}\n")
                {
                    Foreground = System.Windows.Media.Brushes.White
                };
                TerminalOutput.Inlines.Add(run);
            });
            Dispatcher.Invoke(() =>
            {
                Run run = new Run("0 / {number_of_modules} . . .")
                {
                    Foreground = System.Windows.Media.Brushes.DarkGoldenrod
                };
                TerminalOutput.Inlines.Add(run);
            });
            StoreWebElement store = new StoreWebElement();
            for (int i = 0; i < number_of_modules; i++)
            {
                Dispatcher.Invoke(() =>
                {
                    TerminalOutput.Inlines.Remove(TerminalOutput.Inlines.LastInline);
                    Run run = new Run($"{i} / {number_of_modules} . . .")
                    {
                        Foreground = System.Windows.Media.Brushes.DarkGoldenrod
                    };
                    TerminalOutput.Inlines.Add(run);
                });
                try
                {
                    store = new StoreWebElement();
                    var cur_item = wait.Until(c => c.FindElements(By.CssSelector("li[class*=\"context_module_item\"]")))[i];
                    store.id = cur_item.GetAttribute("id");
                    store.text = cur_item.Text;
                    var class_list = cur_item.GetAttribute("class").CleanSplit(" ").Select(c => Regex.Replace(c, @"\s+", ""));
                    store.class_list = class_list.ToList();
                    if (class_list.Contains("context_module_sub_header"))
                    {
                        continue;
                    }
                    else if (class_list.Contains("external_url"))
                    {
                        continue;
                    }

                    chrome.Url = cur_item.FindElement(By.CssSelector("a.item_link")).GetAttribute("href");
                    var edit_button = wait.Until(c => c.FindElement(By.CssSelector("a[class*=\"edit\"]")));
                    edit_button.Click();

                    if (chrome.Url.Contains("quiz"))
                    {
                        wait.Until(c => c.SwitchTo().Frame(c.FindElement(By.Id("quiz_description_ifr"))));
                        chrome.ExecuteScript("var el = document.querySelector(\"img\"); if(el){el.setAttribute('alt','');}");
                        chrome.SwitchTo().ParentFrame();
                        chrome.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                        wait.Until(c => c.FindElement(By.CssSelector("button.save_quiz_button"))).Click();
                    }
                    else if (chrome.Url.Contains("assignment"))
                    {
                        wait.Until(c => c.SwitchTo().Frame(c.FindElement(By.Id("assignment_description_ifr"))));
                    chrome.ExecuteScript("var el = document.querySelector(\"img\"); if(el){el.setAttribute('alt','');}");
                    chrome.SwitchTo().ParentFrame();
                        chrome.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                        wait.Until(c => c.FindElement(By.CssSelector("button.btn.btn-primary"))).Click();
                    }
                    else if (chrome.Url.Contains("discussion"))
                    {
                        wait.Until(c => c.SwitchTo().Frame(c.FindElement(By.Id("discussion-topic-message9_ifr"))));
                    chrome.ExecuteScript("var el = document.querySelector(\"img\"); if(el){el.setAttribute('alt','');}");
                    chrome.SwitchTo().ParentFrame();
                        chrome.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                        wait.Until(c => c.FindElement(By.CssSelector("[data-text-while-loading]"))).Click();
                    }
                    else if (chrome.Url.Contains("pages"))
                    {
                        wait.Until(c => c.SwitchTo().Frame(c.FindElement(By.Id("wiki_page_body_ifr"))));
                    chrome.ExecuteScript("var el = document.querySelector(\"img\"); if(el){el.setAttribute('alt','');}");
                    chrome.SwitchTo().ParentFrame();
                        chrome.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                        wait.Until(c => c.FindElement(By.CssSelector("button.submit"))).Click();
                    }

                    if (chrome.isAlertPresent())
                    {
                        chrome.SwitchTo().Alert().Dismiss();
                        chrome.SwitchTo().Window(chrome.CurrentWindowHandle);
                    }
                    chrome.SwitchTo().Window(chrome.CurrentWindowHandle);
                    wait.UntilElementIsVisible(By.CssSelector("a[class*='edit']"));
                    chrome.FindElementByCssSelector("a[class='home']").Click();
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error on {i}");
                    Console.WriteLine($"Url: {chrome.Url}");
                    Console.WriteLine($"ID: {store.id}");
                    Console.WriteLine($"Text: {store.text}");
                    Console.WriteLine($"ClassList: {String.Join(", ", store.class_list)}");
                    Console.WriteLine(ex.Message);
                    if (chrome.isAlertPresent())
                    {
                        chrome.SwitchTo().Alert().Dismiss();
                        chrome.SwitchTo().Window(chrome.CurrentWindowHandle);
                    }
                    chrome.Url = home_page_url;
                }
            }
            Dispatcher.Invoke(() =>
            {
                Run run = new Run($"RALT finished.\nTime taken: {s.Elapsed.ToString(@"hh\:mm\:ss")}\n")
                {
                    Foreground = System.Windows.Media.Brushes.White
                };
                TerminalOutput.Inlines.Add(run);
            });
        }
        private void BuzzRalt(object sender, DoWorkEventArgs e)
        {
            if (QuitThread)
            {
                QuitThread = false;
                return;
            }
            //Get number of modules
            var number_of_modules = chrome.FindElementsByCssSelector("button[class*=\"glyphicon-option\"]").Count();
            for(int i = 0; i < number_of_modules; i++)
            {
                if (QuitThread)
                {
                    QuitThread = false;
                    return;
                }
                try
                {
                    //Open moudle options
                    wait.Until(c => c.FindElement(By.CssSelector("button[class*=\"glyphicon-option\"]")).Displayed);
                    chrome.FindElementsByCssSelector("button[class*=\"glyphicon-option\"]")[i].Click();
                    //Wait for edit button to show up and click displayed one
                    wait.Until(c => {
                        var els = c.FindElements(By.LinkText("Edit"));
                        if (els.Select(el => el.Displayed).Count() > 0)
                        {
                            return els.Where(el => el.Displayed).First();
                        } else
                        {
                            return null;
                        }});
                    if (QuitThread)
                    {
                        QuitThread = false;
                        return;
                    }
                    //Get image
                    var image = wait.Until(c => c.FindElement(By.CssSelector("img[class*='fr-draggable']")));
                    //Clear title if it exists
                    if("" != image.GetAttribute("title") && null != image.GetAttribute("title"))
                    {
                        chrome.ExecuteScript("document.querySelector(\"img[class*='fr-draggable']\").setAttribute('title',''));");
                    }
                    //Enter image options
                    image.Click();
                    //Wait for button to edit alt text shows up
                    wait.Until(c =>
                    {
                        var el = c.FindElement(By.CssSelector("button[id*='imageAlt']"));
                        if (el.Displayed)
                        {
                            return el;
                        }
                        else
                        {
                            return null;
                        }
                    }).Click();
                    if (QuitThread)
                    {
                        QuitThread = false;
                        return;
                    }
                    //Clear the text field
                    wait.Until(c =>
                    {
                        var el = c.FindElement(By.CssSelector("input[placeholder*=\"Alternative\"])"));
                        if (el.Displayed)
                        {
                            return el;
                        }
                        else
                        {
                            return null;
                        }
                    }).Clear();

                    //Update alt text
                    chrome.FindElementsByTagName("button").Where(el => el.Text == "Update").First().Click();
                    //Save page
                    chrome.FindElementsByTagName("button").Where(el => el.Text == "Save").First().Click();
                   
                    try
                    {
                        if (QuitThread)
                        {
                            QuitThread = false;
                            return;
                        }
                        //Check for pop up
                        wait.Timeout = new TimeSpan(0, 0, 1);
                        wait.Until(c =>
                        {
                            return c.FindElement(By.CssSelector("mat-dialog-container"));
                        }).FindElements(By.CssSelector("button.mat-button.mat-primary")).First().Click();
                        wait.Timeout = new TimeSpan(0, 0, 3);
                        //Try to then leave page
                        chrome.FindElementsByTagName("button")
                            .Where(el => el.Text.Contains("Clear"))
                            .First()
                            .Click();
                        //May ask "are you sure"
                        chrome.FindElementsByCssSelector("span.mat-button-wrapper")
                            .Where(el => el.Text.Contains("LEAVE"))
                            .First()
                            .Click();
                    }
                    catch
                    {
                        //Silently error since this should mean there is no popup
                    }
                }
                catch
                {
                    Console.WriteLine("Nothing Found");
                    chrome.FindElementsByTagName("button").Where(el => el.Text.Contains("Save")).First().Click();
                    try
                    {
                        if (QuitThread)
                        {
                            QuitThread = false;
                            return;
                        }
                        wait.Timeout = new TimeSpan(0, 0, 1);
                        wait.Until(c =>
                        {
                            return c.FindElement(By.CssSelector("mat-dialog-container"));
                        }).FindElements(By.CssSelector("button.mat-button.mat-primary")).First().Click();
                        wait.Timeout = new TimeSpan(0, 0, 3);
                        //Try to then leave page
                        chrome.FindElementsByTagName("button")
                            .Where(el => el.Text.Contains("Clear"))
                            .First()
                            .Click();
                        //May ask "are you sure"
                        chrome.FindElementsByCssSelector("span.mat-button-wrapper")
                            .Where(el => el.Text.Contains("LEAVE"))
                            .First()
                            .Click();
                    }
                    catch
                    {
                        //Silently error since this should mean there is no popup
                    }
                }
            }
        }
        private void Login_Click(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
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
                wait.Until(c => c.FindElement(By.Id("netid"))).ReturnClear().SendKeys(username);
                wait.Until(c => c.FindElement(By.CssSelector("input#password"))).ReturnClear().SendKeys(password);
                wait.Until(c => c.FindElement(By.CssSelector("input[value*=\"Sign\"]"))).Submit();
            }
            else
            {
                chrome.SwitchTo().Window("Brigham Young University Sign-in Service");
                wait.Until(c => c.FindElement(By.Id("netid"))).ReturnClear().SendKeys(username);
                wait.Until(c => c.FindElement(By.CssSelector("input#password"))).ReturnClear().SendKeys(password);
                wait.Until(c => c.FindElement(By.CssSelector("input[value*=\"Sign\"]"))).Submit();
                chrome.SwitchTo().Window(chrome.CurrentWindowHandle);
            }
        }
        private void A11yHelp_Click(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            worker.DoWork += ShowA11yHelpers;
            worker.RunWorkerAsync();
        }
        private void ShowA11yHelpers(object sender, DoWorkEventArgs e)
        {
            void RecursiveA11y(int num = 0)
            {   //recursively run the javascript within every iframe, so it should work on any webpage.
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
                {   //If that class exists then the function was already ran
                    chrome.SwitchTo().ParentFrame();
                    return;
                }
                //Huge string of javascript to highlight all of the given accessibility things I want. Can just add a new line at the bottom of the string to extend it.
                chrome.ExecuteScript(@"(function() { var e, t, o = document.querySelectorAll(""img""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", /*e.style.clear = ""right"",*/ e.style.cssFloat = o[t].style.cssFloat, e.style.margin = ""-7px -0px -7px -7px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""75px"", e.style.fontSize = ""12px"", e.style.zIndex = ""0"", e.textContent = ""Alt Text:\n"" + o[t].alt, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());javascript:(function() { var e, t, o = document.querySelectorAll(""iframe""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", e.style.clear = ""right"", /*e.style.cssFloat = ""left"",*/ e.style.margin = ""-7px -0px -7px -7px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""300px"", e.style.zIndex = ""0"", e.textContent = ""Iframe title:\n"" + o[t].title, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());javascript:(function() { var e, t, o = document.querySelectorAll(""h1""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", e.style.clear = ""right"",/* e.style.cssFloat = ""left"",*/ e.style.margin = ""-7px 0px 0px 0px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""35px"", e.style.zIndex = ""0"", e.textContent = o[t].tagName, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());(function() { var e, t, o = document.querySelectorAll(""h2""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", e.style.clear = ""right"", /*e.style.cssFloat = ""left"",*/ e.style.margin = ""-7px 0px 0px -7px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""35px"", e.style.zIndex = ""0"", e.textContent = o[t].tagName, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());(function() { var e, t, o = document.querySelectorAll(""h3""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", e.style.clear = ""right"", /*e.style.cssFloat = ""left"",*/ e.style.margin = ""-7px 0px 0px -7px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""35px"", e.style.zIndex = ""0"", e.textContent = o[t].tagName, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());(function() { var e, t, o = document.querySelectorAll(""h4""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", e.style.clear = ""right"", /*e.style.cssFloat = ""left"",*/ e.style.margin = ""-7px 0px 0px -7px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""35px"", e.style.zIndex = ""0"", e.textContent = o[t].tagName, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());(function() { var e, t, o = document.querySelectorAll(""h5""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", e.style.clear = ""right"", /*e.style.cssFloat = ""left"",*/ e.style.margin = ""-7px 0px 0px -7px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""35px"", e.style.zIndex = ""0"", e.textContent = o[t].tagName, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());(function() { var e, t, o = document.querySelectorAll(""h6""); function r(e) { ""use strict""; e.style.backgroundColor = ""#FFE"", e.style.borderColor = ""#393"", e.style.boxShadow = ""1px 2px 5px #CCC"", e.style.zIndex = ""1"" } function s(e) { ""use strict""; e.style.backgroundColor = ""#FFF"", e.style.borderColor = ""#CCC"", e.style.boxShadow = ""none"", e.style.zIndex = ""0"" } function l(e, t, o) { ""use strict""; return function() { e(t), e(o) } } for (t = 0; t < o.length; t++)(e = document.createElement(""div"")).style.backgroundColor = ""#FFF"", e.className = ""AccessibilityHelper"", e.style.border = ""2px solid #CCC"", e.style.borderRadius = ""7px"", e.style.clear = ""right"", /*e.style.cssFloat = ""left"",*/ e.style.margin = ""-7px 0px 0px -7px"", e.style.padding = ""5px"", e.style.position = ""relative"", e.style.textAlign = ""left"", e.style.whiteSpace = ""pre-wrap"", e.style.width = ""35px"", e.style.zIndex = ""0"", e.textContent = o[t].tagName, o[t].style.backgroundColor = ""#FFF"", o[t].className += "" AccessibilityHelper"", o[t].style.border = ""2px solid #CCC"", o[t].style.borderRadius = ""7px"", o[t].style.margin = ""-7px"", o[t].style.padding = ""5px"", o[t].parentNode.insertBefore(e, o[t]), e.addEventListener(""mouseover"", l(r, e, o[t])), o[t].addEventListener(""mouseover"", l(r, e, o[t])), e.addEventListener(""mouseout"", l(s, e, o[t])), o[t].addEventListener(""mouseout"", l(s, e, o[t])); }());
    (function() {
    document.querySelectorAll(""th"").forEach(c => {if(c.scope == ""row""){c.style.backgroundColor = ""green"";}else if(c.scope == ""col""){c.style.backgroundColor = ""blue"";}else{c.style.backgroundColor = ""red""} c.className += "" AccessibilityHelper"";});
    document.querySelectorAll(""i"").forEach(c => {c.style.backgroundColor = ""DeepPink""; c.className += "" AccessibilityHelper"";});
    document.querySelectorAll(""b"").forEach(c => {c.style.backgroundColor = ""DarkViolet""; c.className += "" AccessibilityHelper"";});
    }());");
                chrome.SwitchTo().ParentFrame();
            }
            RecursiveA11y();
        }
        
        private void GenerateReport_Click(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            worker.DoWork += CreateReport;
            worker.RunWorkerCompleted += ReportFinished;
            worker.RunWorkerAsync();
        }
        private void ReportFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Run run = new Run($"{e.Result}")
                {
                    Foreground = System.Windows.Media.Brushes.Cyan
                };
                TerminalOutput.Inlines.Add(run);
            });
        }
        private void CreateReport(object sender, DoWorkEventArgs e)
        {
            var s = new System.Diagnostics.Stopwatch();
            s.Start();
            this.Dispatcher.Invoke(() =>
            {
                Run run = new Run($"Generating Report\n")
                {
                    Foreground = System.Windows.Media.Brushes.Cyan
                };
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
            if (QuitThread)
            {
                QuitThread = false;
                return;
            }
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
            if (QuitThread)
            {
                QuitThread = false;
                return;
            }
            CourseInfo course;
            bool directory = false;
            LinkParser ParseForLinks = null; //Need to declare this early as it is only set if it is a directory
            if (int.TryParse(text, out int id))
            {
                //It is an ID, create course info with new id var
                course = new CourseInfo(id);
            }
            else
            {
                //Just send it in as a string path
                course = new CourseInfo(text);
                directory = true;
                ParseForLinks = new LinkParser(course.CourseIdOrPath);
            }
            if (QuitThread)
            {
                QuitThread = false;
                return;
            }
            A11yParser ParseForA11y = new A11yParser();
            MediaParser ParseForMedia = new MediaParser();

            Parallel.ForEach(course.PageHtmlList, page =>
            {
                if (QuitThread)
                {
                    QuitThread = false;
                    return;
                }
                ParseForA11y.ProcessContent(page);
                ParseForMedia.ProcessContent(page);
                if (directory)
                {
                    ParseForLinks.ProcessContent(page);
                }
            });
            if (QuitThread)
            {
                QuitThread = false;
                return;
            }
            var file_name_extention = ((CanvasApi.CurrentDomain == "Directory") ? System.IO.Path.GetPathRoot(text) + "Drive" : CanvasApi.CurrentDomain).Replace(":\\", "");
            CreateExcelReport GenReport = new CreateExcelReport(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + $"\\AccessibilityTools\\ReportGenerators-master\\Reports\\ARC_{course.CourseCode.Replace(",", "").Replace(":", "")}_{file_name_extention}.xlsx");
            GenReport.CreateReport(ParseForA11y.Data, ParseForMedia.Data, ParseForLinks?.Data);
            s.Stop();
            ParseForMedia.Chrome.Quit();
            if(ParseForA11y.Data.Count() > int.Parse(File.ReadAllText(@"M:\DESIGNER\Content EditorsELTA\Accessibility Assistants\HIGHSCORE.txt")))
            {
                File.WriteAllText(@"M:\DESIGNER\Content EditorsELTA\Accessibility Assistants\HIGHSCORE.txt", ParseForA11y.Data.Count().ToString());
                Dispatcher.Invoke(() =>
                {
                    HighScoreBox.Text = "HighScore: " + File.ReadAllText(@"M:\DESIGNER\Content EditorsELTA\Accessibility Assistants\HIGHSCORE.txt");
                });
            }

            e.Result = $"Report generated.\nTime taken: {s.Elapsed.ToString(@"hh\:mm\:ss")}\n";
           
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
                //I was to lazy to rewrite the function into c# and just import the POSH script.
                string script = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AccessibilityTools\PowerShell\MoveReports.ps1");
                posh.AddScript(script);
                Collection<PSObject> results = posh.Invoke();
                foreach(var obj in results)
                {
                    if (QuitThread)
                    {
                        QuitThread = false;
                        return;
                    }
                    Run run = new Run("Report:\n")
                    {
                        Foreground = System.Windows.Media.Brushes.Cyan
                    };
                    TerminalOutput.Inlines.Add(run);
                    TerminalOutput.Inlines.Add(obj.ToString().Remove(0,2).Replace("; ", "\n").Replace("=", ": ").Replace("}", "") + "\n");
                }
            }
        }
        private void ReviewPage_Click(object sender, EventArgs e)
        {
            if(PageParser == null)
            {
                PageParser = new PageReviewer();
            }
            BackgroundWorker worker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            worker.DoWork += ReviewPage;
            worker.RunWorkerCompleted += PageReviewFinished;
            worker.RunWorkerAsync();
        }
        private void ReviewPage(object sender, DoWorkEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Run run = new Run("Reviewing current page...\n")
                {
                    Foreground = System.Windows.Media.Brushes.Cyan
                };
                TerminalOutput.Inlines.Add(run);
            });
            //Get current page HTML and review it.
            Dictionary<string, string> page = new Dictionary<string, string>
            {
                [chrome.Url] = chrome.FindElementByTagName("body").GetAttribute("outerHTML")
            };
            try
            {
                PageParser.A11yReviewer.ProcessContent(page);
                PageParser.MediaReviewer.ProcessContent(page);
                PageParser.LinkReviewer.ProcessContent(page);
            }
            catch
            {
                return;
            }
           
        }
        private void PageReviewFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TerminalOutput.Inlines.Add("Review of page finished.\n");
            });
        }
        private void CreatePageReport_Click(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            worker.DoWork += CreatePageReport;
            worker.RunWorkerAsync();
        }
        private void CreatePageReport(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("Creating report...");
            CreateExcelReport GenReport = new CreateExcelReport(Environment
                .GetFolderPath(Environment.SpecialFolder.Desktop) + 
                $"\\AccessibilityTools\\ReportGenerators-master\\Reports\\ARC_WebPage.xlsx");
            GenReport.CreateReport(PageParser.A11yReviewer.Data, PageParser.MediaReviewer.Data, null);
            PageParser.MediaReviewer.Chrome.Quit();
            PageParser = null;
        }
        private void ClearButton_Click(object sender, EventArgs e)
        {
            TerminalOutput.Text = "";
        }
        private void TextInput_ScrollDown(object sender, ScrollChangedEventArgs e)
        {
            if(e.ExtentHeightChange > 0)
            {   
                TextBlockScrollBar.ScrollToEnd();
            }
        }
        private void GoToAccessibility_Course(object sender, EventArgs e)
        {
            if(chrome == null)
            {
                //Open the browser to be controlled
                var chromeDriverService = ChromeDriverService.CreateDefaultService(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AccessibilityTools\PowerShell\Modules\SeleniumTest");
                chromeDriverService.HideCommandPromptWindow = true;
                chrome = new ChromeDriver(chromeDriverService);
                wait = new WebDriverWait(chrome, new TimeSpan(0, 0, 5));
            }
            chrome.Url = "https://byu.instructure.com/courses/1026";
        }
    }
}
