using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO;
using System.ComponentModel;
using ReportGenerators;
using My.CanvasApi;
using System.Management.Automation;

namespace WPFCommandPanel
{
    public partial class CommandPanel : Page
    {
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
            try
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
                this.Dispatcher.Invoke(() =>
                {
                    Run run = new Run($"Domain changed to {checkedDomain}\n")
                    {
                        Foreground = System.Windows.Media.Brushes.Green
                    };
                    TerminalOutput.Inlines.Add(run);
                });

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
                    //Just send it in as a string path after running basic find replace on directory
                    this.Dispatcher.Invoke(() =>
                    {
                        Run run = new Run($"Running basic Find Replace on directory\n")
                        {
                            Foreground = System.Windows.Media.Brushes.Green
                        };
                        TerminalOutput.Inlines.Add(run);
                    });
                    var script = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AccessibilityTools\PowerShell\FindReplace.ps1");
                    script = "param($path)process{\n" + script + "\n}";
                    var posh = PowerShell.Create();
                    posh.AddScript(script).AddArgument(text);
                    posh.Invoke();
                    Dispatcher.Invoke(() =>
                    {
                        Run run = new Run($"Find Replace on {text} finished.\nBack up can be found at {Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AccessibilityTools\COURSE_BACKUP"}\n")
                        {
                            Foreground = System.Windows.Media.Brushes.Cyan
                        };
                        TerminalOutput.Inlines.Add(run);
                    });
                    course = new CourseInfo(text);
                    directory = true;
                    ParseForLinks = new LinkParser(course.CourseIdOrPath);
                }
                this.Dispatcher.Invoke(() =>
                {
                    Run run = new Run($"Created course object\n")
                    {
                        Foreground = System.Windows.Media.Brushes.Green
                    };
                    TerminalOutput.Inlines.Add(run);
                });
                if (course == null || course.CourseCode == null)
                {
                    e.Result = "Could not find course. ID was entered wrong or canvas needs time to cool down ...\n";
                    return;
                }
                this.Dispatcher.Invoke(() =>
                {
                    Run run = new Run($"Beginning to parse pages\n")
                    {
                        Foreground = System.Windows.Media.Brushes.Green
                    };
                    TerminalOutput.Inlines.Add(run);
                });
                A11yParser ParseForA11y = new A11yParser();
                MediaParser ParseForMedia = new MediaParser();
                Parallel.ForEach(course.PageHtmlList, page =>
                {
                    ParseForA11y.ProcessContent(page);
                    ParseForMedia.ProcessContent(page);
                    if (directory)
                    {
                        ParseForLinks.ProcessContent(page);
                    }
                });
                this.Dispatcher.Invoke(() =>
                {
                    Run run = new Run($"Finished parsing pages, creating file\n")
                    {
                        Foreground = System.Windows.Media.Brushes.Green
                    };
                    TerminalOutput.Inlines.Add(run);
                });
                var file_name_extention = ((CanvasApi.CurrentDomain == "Directory") ? System.IO.Path.GetPathRoot(text) + "Drive" : CanvasApi.CurrentDomain).Replace(":\\", "");
                CreateExcelReport GenReport = new CreateExcelReport(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + $"\\AccessibilityTools\\ReportGenerators-master\\Reports\\ARC_{course.CourseCode.Replace(",", "").Replace(":", "")}_{file_name_extention}.xlsx");
                GenReport.CreateReport(ParseForA11y.Data, ParseForMedia.Data, ParseForLinks?.Data);
                s.Stop();
                ParseForMedia.Chrome.Quit();
                if (ParseForA11y.Data.Count() > int.Parse(File.ReadAllText(@"M:\DESIGNER\Content EditorsELTA\Accessibility Assistants\HIGHSCORE.txt")))
                {
                    File.WriteAllText(@"M:\DESIGNER\Content EditorsELTA\Accessibility Assistants\HIGHSCORE.txt", ParseForA11y.Data.Count().ToString());
                    Dispatcher.Invoke(() =>
                    {
                        HighScoreBox.Text = "HighScore: " + File.ReadAllText(@"M:\DESIGNER\Content EditorsELTA\Accessibility Assistants\HIGHSCORE.txt");
                    });
                }

                e.Result = $"Report generated.\nTime taken: {s.Elapsed.ToString(@"hh\:mm\:ss")}\n";
            }catch(Exception ex)
            {
                e.Result = ex.Message + '\n' + ex.ToString() + '\n' + ex.StackTrace;
            }
        }
        private void ReviewPage_Click(object sender, EventArgs e)
        {
            if (PageParser == null)
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
    }
}
