using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using Microsoft.Win32;
using My;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Linq;
using System;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using HtmlAgilityPack;
using System.Windows;
using My.DatagridExtensions;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Data;
using TidyManaged;
using My.StringExtentions;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;

namespace WPFCommandPanel
{
    /// <summary>
    /// Interaction logic for A11yRepair.xaml
    /// </summary>
    public partial class A11yRepair : Page
    {
        public CollectionViewSource ViewSource { get; set; }
        public ObservableCollection<A11yData> data { get; set; }
        public String curReportFile { get; set; }
        public A11yRepair()
        {
            InitializeComponent();
            data = new ObservableCollection<A11yData>();
            this.ViewSource = new CollectionViewSource();
            ViewSource.Source = this.data;
            IssueGrid.ItemsSource = ViewSource.View;
        }
        
        private void Submit_TextBox(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {                
                string[] array = directory.Text.Split('\\');
                string CourseName = array.Take(array.Length - 1).LastOrDefault();
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.InitialDirectory = MainWindow.panelOptions.JsonDataDir;
                openFileDialog.FileName = "*" + CourseName + "*";
                openFileDialog.Filter = "Json Files|*.json";
                if(openFileDialog.ShowDialog() == true)
                {
                    string json = "";
                    using (StreamReader r = new StreamReader(openFileDialog.FileName))
                    {
                        json = r.ReadToEnd();
                    }
                    curReportFile = openFileDialog.FileName;
                    var tempdata = JsonConvert.DeserializeObject<ObservableCollection<A11yData>>(json);
                    data.Clear();
                    for(int i = tempdata.Count-1; i >= 0; i--)
                    {      
                        if(tempdata[i].Location == null || tempdata[i].Location == "")
                        {
                            continue;
                        }
                        data.Add(tempdata[i]);
                    }
                    ViewSource.View.Refresh();
                }
            }
        }
        public class DataToParse
        {   //Object stored in the ReportParser objects that turns the html string from the CourseInfo object into a live HTML dom to be used by the parsers.
            public DataToParse(string location)
            {
                Location = location;
                Doc = new HtmlAgilityPack.HtmlDocument();
                Doc.Load(location);
            }
            public DataToParse(string location, HtmlAgilityPack.HtmlDocument doc)
            {
                Location = location;
                Doc = doc;
            }
            public string Location;
            public HtmlAgilityPack.HtmlDocument Doc;
        }
        private DataToParse curPage;
        private HtmlNode curNode;
        private void SetCurrentNode()
        {
            A11yData row = (A11yData)IssueGrid.SelectedItem;
            if (row == null)
            {
                browser.Url = "";
                curPage = null;
                curNode = null;
                editor.Clear();
                return;
            }
            // TODO: Save on issue change
            var url = (directory.Text + "\\" + row.Location.Split('/').LastOrDefault());
            string html;
            using (StreamReader r = new StreamReader(url))
            {
                html = r.ReadToEnd();
            }
            curPage = new DataToParse(url);

            switch (row.IssueType)
            {
                case "Link":
                    switch (row.DescriptiveError)
                    {
                        case "Non-Descriptive Link":
                            curNode = curPage.Doc.DocumentNode.SelectSingleNode(row.html);
                            break;
                        case "JavaScript Link":
                            curNode = curPage.Doc.DocumentNode.SelectSingleNode(row.html);
                            break;
                        case "Broken Link":
                            curNode = curPage.Doc.DocumentNode.SelectSingleNode(row.html);
                            break;
                        default:
                            curNode = curPage.Doc.DocumentNode.SelectSingleNode("//body");
                            break;
                    }
                    break;
                case "Semantics":
                    switch (row.DescriptiveError)
                    {
                        case "Missing title/label":
                            ;
                            curNode = curPage.Doc.DocumentNode.SelectSingleNode(row.html);
                            break;
                        case "Improper Headings":
                            curNode = curPage.Doc.DocumentNode.SelectSingleNode(row.html);
                            break;
                        case "Bad use of <i> and/or <b>":
                            var ibList = curPage.Doc.DocumentNode.SelectNodes("//i | //b");
                            if (ibList.Count() > 1)
                            {
                                System.Windows.MessageBox.Show("Found more then one match, fix one then reselect issue to do next");
                            }
                            curNode = ibList.FirstOrDefault();
                            break;
                        default:
                            curNode = curPage.Doc.DocumentNode.SelectSingleNode("//body");
                            break;
                    }
                    break;
                case "Image":
                    switch (row.DescriptiveError)
                    {
                        case "No Alt Attribute":
                            curNode = curPage.Doc.DocumentNode.SelectSingleNode(row.html);
                            break;
                        case "Non-Descriptive alt tags":
                            curNode = curPage.Doc.DocumentNode.SelectSingleNode(row.html);
                            break;
                        default:
                            curNode = curPage.Doc.DocumentNode.SelectSingleNode("//body");
                            break;
                    }
                    break;
                case "Media":
                    switch (row.DescriptiveError)
                    {
                        case "Transcript Needed":
                            if (row.Notes.Contains("Video number"))
                            {
                                curNode = curPage.Doc.DocumentNode.SelectSingleNode(row.html);
                            }
                            else if (row.Notes.Contains("BrightCove video with id"))
                            {
                                curNode = curPage.Doc.DocumentNode.SelectSingleNode(row.html);
                            }
                            break;
                        default:
                            curNode = curPage.Doc.DocumentNode.SelectSingleNode("//body");
                            break;
                    }

                    break;
                case "Table":
                    int tableIndex = int.Parse(row.Notes.Split(':')[0].Split(' ').LastOrDefault()) - 1;
                    curNode = curPage.Doc.DocumentNode.SelectNodes("//table")[tableIndex];
                    break;
                case "Misc":
                    curNode = curPage.Doc.DocumentNode.SelectSingleNode("//body");
                    break;
                case "Color":
                    curNode = curPage.Doc.DocumentNode.SelectSingleNode(row.html);
                    break;
                case "Keyboard":
                    curNode = curPage.Doc.DocumentNode.SelectSingleNode(row.html);
                    break;
                default:
                    curNode = curPage.Doc.DocumentNode.SelectSingleNode("//body");
                    break;
            }
            if(curNode == null)
            {
                System.Windows.MessageBox.Show("Issue was not found, report data is old. You probably want to generate a new report.");
                return;
            }
            MemoryStream str = new MemoryStream(Encoding.UTF8.GetBytes(curNode.OuterHtml));
            using (TidyManaged.Document my_doc = Document.FromStream(str))
            {
                my_doc.ShowWarnings = false;
                my_doc.Quiet = true;
                my_doc.OutputXhtml = true;
                my_doc.OutputXml = true;
                my_doc.IndentBlockElements = AutoBool.Yes;
                my_doc.IndentAttributes = false;
                my_doc.IndentCdata = true;
                my_doc.AddVerticalSpace = false;
                my_doc.WrapAt = 0;
                my_doc.OutputBodyOnly = AutoBool.Yes;
                my_doc.IndentWithTabs = true;
                my_doc.CleanAndRepair();
                editor.Text = my_doc.Save();
                editor.ScrollToHome();
            }
            var style = curNode.GetAttributeValue("style", "");
            if (style == "")
            {
                style = "border: 5px solid red";
            }
            else
            {
                style = "; border: 5px solid red;";
            }
            curNode.Id = "focus_this";
            curNode.SetAttributeValue("style", style);
            browser.LoadHtmlAndWait(curPage.Doc.DocumentNode.OuterHtml);
            browser.QueueScriptCall($"var el = document.getElementById('focus_this'); el.scrollIntoView({{behavior: 'smooth' , block: 'center', inline: 'center'}});");
        }
        private void IssueGrid_Selected(object sender, SelectedCellsChangedEventArgs e)
        {
            SetCurrentNode();
        }
        private void SaveFile()
        {
            if (curPage == null)
            {               
                return;
            }
            var newNode = HtmlNode.CreateNode(editor.Text);
            curNode.ParentNode.ReplaceChild(newNode, curNode);
            curNode = newNode;
            A11yData selectedItem = (A11yData)IssueGrid.SelectedItem;
            var index = data.IndexOf(selectedItem);
            data[index].Completed = true;
            curPage.Doc.Save(curPage.Location);
            ViewSource.View.Refresh();
            browser.LoadHtmlAndWait(curPage.Doc.DocumentNode.OuterHtml);
            browser.QueueScriptCall($"var el = document.getElementById('focus_this'); el.scrollIntoView({{behavior: 'smooth' , block: 'center', inline: 'center'}});");           
        }
        private void editor_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if(e.Key == Key.S)
                {
                    if(curNode == null)
                    {
                        e.Handled = true;
                        return;
                    }
                    var newNode = HtmlNode.CreateNode(editor.Text);
                    var style = newNode.GetAttributeValue("style", "");
                    if (style == "")
                    {
                        style = "border: 5px solid red";
                    }
                    else
                    {
                        style = "; border: 5px solid red;";
                    }
                    newNode.Id = "focus_this";
                    newNode.SetAttributeValue("style", style);
                    curNode.ParentNode.ReplaceChild(newNode, curNode);
                    curNode = newNode;
                    browser.LoadHtmlAndWait(curPage.Doc.DocumentNode.OuterHtml);
                    browser.QueueScriptCall($"var el = document.getElementById('focus_this'); el.scrollIntoView({{behavior: 'smooth' , block: 'center', inline: 'center'}});");

                    e.Handled = true;                    
                }
                else if(e.Key == Key.Enter)
                {
                    SaveFile();
                    e.Handled = true;
                }
                else if(e.Key == Key.Down)
                {
                    if(data.Count <= IssueGrid.SelectedIndex + 1)
                    {
                        e.Handled = true;
                    }else
                    {
                        IssueGrid.SelectedIndex = IssueGrid.SelectedIndex + 1;
                    }
                }else if(e.Key == Key.Up)
                {
                    if(IssueGrid.SelectedIndex == 0)
                    {
                        e.Handled = true;
                    }else
                    {
                        IssueGrid.SelectedIndex = IssueGrid.SelectedIndex - 1;
                    }
                }
            }
        }
        private void Search_Button(object sender, RoutedEventArgs e)
        {
            string[] array = directory.Text.Split('\\');
            string CourseName = array.Take(array.Length - 1).LastOrDefault();
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = MainWindow.panelOptions.JsonDataDir;
            openFileDialog.FileName = "*" + CourseName + "*";
            openFileDialog.Filter = "Json Files|*.json";
            if (openFileDialog.ShowDialog() == true)
            {
                string json = "";
                using (StreamReader r = new StreamReader(openFileDialog.FileName))
                {
                    json = r.ReadToEnd();
                }
                curReportFile = openFileDialog.FileName;
                var tempdata = JsonConvert.DeserializeObject<ObservableCollection<A11yData>>(json);
                data.Clear();
                for (int i = tempdata.Count - 1; i >= 0; i--)
                {
                    if (tempdata[i].Location == null || tempdata[i].Location == "")
                    {
                        continue;
                    }
                    data.Add(tempdata[i]);
                }
                ViewSource.View.Refresh();
            }
        }
        private void Save_Button(object sender, RoutedEventArgs e)
        {         
            if(curReportFile == null)
            {
                e.Handled = true;                
                return;
            }
            using (StreamWriter file = new StreamWriter(System.IO.Path.Combine(curReportFile), false))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, data);
            }
        }
        private void SaveIssue_Button(object sender, RoutedEventArgs e)
        {
            SaveFile();
            e.Handled = true;
        }
        private void Preview_Button(object sender, RoutedEventArgs e)
        {
            if (curNode == null)
            {
                e.Handled = true;
                return;
            }
            var newNode = HtmlNode.CreateNode(editor.Text);
            var style = newNode.GetAttributeValue("style", "");
            if (style == "")
            {
                style = "border: 5px solid red";
            }
            else
            {
                style = "; border: 5px solid red;";
            }
            newNode.Id = "focus_this";
            newNode.SetAttributeValue("style", style);
            curNode.ParentNode.ReplaceChild(newNode, curNode);
            curNode = newNode;
            browser.LoadHtmlAndWait(curPage.Doc.DocumentNode.OuterHtml);
            browser.QueueScriptCall($"var el = document.getElementById('focus_this'); el.scrollIntoView({{behavior: 'smooth' , block: 'center', inline: 'center'}});");
            e.Handled = true;
        }

        private void RefreshNode_Button(object sender, RoutedEventArgs e)
        {
            SetCurrentNode();
        }

        
        private void OpenImage_Button(object sender, RoutedEventArgs e)
        {
            if(curNode?.Name != "img")
            {
                return;
            }            
            var imagePath = Path.Combine(Path.GetDirectoryName(curPage.Location), curNode.GetAttributeValue("src", null));
            if(imagePath == null)
            {
                System.Windows.MessageBox.Show($"Image source seems to be null");
            }
            ImagePopUp.IsOpen = true;
            try
            {
                var bitmap = new BitmapImage(new Uri(imagePath));
                ImagePopUp.Width = bitmap.Width + 6;
                ImagePopUp.Height = bitmap.Height + 6;
                ImagePopUp.DisplayImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error opening the image at {imagePath}");
            }
        }

        private void OpenHTML_Button(object sender, RoutedEventArgs e)
        {
            if (curPage == null)
            {
                e.Handled = true;
                return;
            }
            editor.WordWrap = false;
            var newNode = HtmlNode.CreateNode(editor.Text);
            var textToMatch = Array.ConvertAll(editor.Text.CleanSplit("\r\n"), s => s.Replace("\t",""));
            curNode.ParentNode.ReplaceChild(newNode, curNode);
            curNode = newNode;
            MemoryStream str = new MemoryStream(Encoding.UTF8.GetBytes(curPage.Doc.DocumentNode.OuterHtml));            
            using (TidyManaged.Document my_doc = Document.FromStream(str))
            {
                my_doc.CharacterEncoding = EncodingType.Utf8;
                my_doc.InputCharacterEncoding = EncodingType.Utf8;
                my_doc.OutputCharacterEncoding = EncodingType.Utf8;
                my_doc.ShowWarnings = false;
                my_doc.Quiet = true;
                my_doc.OutputXhtml = true;
                my_doc.OutputXml = true;
                my_doc.IndentBlockElements = AutoBool.Yes;
                my_doc.IndentAttributes = false;
                my_doc.IndentCdata = true;
                my_doc.AddVerticalSpace = false;                
                my_doc.OutputBodyOnly = AutoBool.Yes;
                my_doc.IndentWithTabs = true;
                my_doc.WrapAt = 0;
                my_doc.CleanAndRepair();              
                editor.Text = "<body>\r\n" + my_doc.Save() + "\r\n</body>";
            }
            str.Close();
            var compareText = editor.Text.CleanSplit("\r\n");
            int lineNum = 0;
            for(int i = 0; i < compareText.Length - textToMatch.Length; i++)
            {
                bool foundMatch = false;
                if(compareText[i].Contains(textToMatch[0]))
                {
                    foundMatch = true;
                    for (int j = 0; j < textToMatch.Length; j++)
                    {
                        if (!compareText[i+j].Contains(textToMatch[j]))
                        {
                            foundMatch = false;
                        }
                    }
                }
                if(foundMatch)
                {
                    lineNum = i;
                    break;
                }
            }
            double vertOffset = (editor.TextArea.TextView.DefaultLineHeight) * lineNum;
            editor.ScrollToVerticalOffset(vertOffset);
            editor.WordWrap = true;
            curNode = curPage.Doc.DocumentNode.SelectSingleNode("//body");
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (curReportFile == null)
            {
                e.Handled = true;
                return;
            }
            using (StreamWriter file = new StreamWriter(System.IO.Path.Combine(curReportFile), false))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, data);
            }
        }
    }
}
