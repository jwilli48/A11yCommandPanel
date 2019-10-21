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
                openFileDialog.InitialDirectory = @"M:\DESIGNER\Content EditorsELTA\Accessibility Assistants\JSON_DATA\Accessibility";
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
        private void IssueGrid_Selected(object sender, SelectedCellsChangedEventArgs e)
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
            browser.Url = url;
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
                            curNode = curPage.Doc.DocumentNode.SelectSingleNode($"//a[contains(text(),'{row.Notes}')]");
                            break;
                        case "JavaScript Link":
                            var list = curPage.Doc.DocumentNode.SelectNodes("//a").Where(el => el.OuterHtml == row.Notes);
                            if (list.Count() > 1)
                            {
                                System.Windows.MessageBox.Show("Found more then one match, fix one then reselect issue to do next");
                            }
                            curNode = list.FirstOrDefault();
                            break;
                        default:
                            curNode = curPage.Doc.DocumentNode.SelectSingleNode("//body");
                            break;
                    }                   
                    break;
                case "Semantics":
                    curNode = curPage.Doc.DocumentNode.SelectSingleNode("//body");
                    break;
                case "Image":
                    curNode = curPage.Doc.DocumentNode.SelectSingleNode("//body");
                    break;
                case "Media":
                    curNode = curPage.Doc.DocumentNode.SelectSingleNode("//body");
                    break;
                case "Table":
                    int tableIndex = int.Parse(row.Notes.Split(':')[0].Split(' ').LastOrDefault()) - 1;
                    curNode = curPage.Doc.DocumentNode.SelectNodes("//table")[tableIndex];
                    break;
                case "Misc":
                    curNode = curPage.Doc.DocumentNode.SelectSingleNode("//body");
                    break;
                case "Color":
                    curNode = curPage.Doc.DocumentNode.SelectSingleNode("//body");
                    break;
                case "Keyboard":
                    curNode = curPage.Doc.DocumentNode.SelectSingleNode("//body");
                    break;
                default:
                    curNode = curPage.Doc.DocumentNode.SelectSingleNode("//body");
                    break;
            }
            using (TidyManaged.Document my_doc = Document.FromString(curNode.OuterHtml))
            {
                my_doc.ShowWarnings = false;
                my_doc.Quiet = true;
                my_doc.OutputXhtml = true;
                my_doc.OutputXml = true;
                my_doc.IndentBlockElements = AutoBool.Yes;
                my_doc.IndentAttributes = false;
                my_doc.IndentCdata = true;
                my_doc.AddVerticalSpace = false;
                my_doc.WrapAt = 200;
                my_doc.OutputBodyOnly = AutoBool.Yes;
                my_doc.IndentWithTabs = true;
                my_doc.CleanAndRepair();           
                editor.Text = my_doc.Save();
            }            
        }

        private void editor_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if(e.Key == Key.S)
                {
                    var newNode = HtmlNode.CreateNode(editor.Text);
                    curNode.ParentNode.ReplaceChild(newNode, curNode);
                    curNode = newNode;
                    browser.LoadHtml(curPage.Doc.DocumentNode.OuterHtml);
                    e.Handled = true;                    
                }
                if(e.Key == Key.Enter)
                {
                    if (curPage == null)
                    {
                        e.Handled = true;
                        return;
                    }
                    int index = IssueGrid.SelectedIndex;
                    data[index].Completed = true;
                    curPage.Doc.Save(curPage.Location);
                    ViewSource.View.Refresh();            
                    e.Handled = true;
                }
            }
        }
        private void Search_Button(object sender, RoutedEventArgs e)
        {
            string[] array = directory.Text.Split('\\');
            string CourseName = array.Take(array.Length - 1).LastOrDefault();
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = @"M:\DESIGNER\Content EditorsELTA\Accessibility Assistants\JSON_DATA\Accessibility";
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
            if(curPage == null)
            {
                e.Handled = true;
                return;
            }
            int index = IssueGrid.SelectedIndex;
            data[index].Completed = true;
            curPage.Doc.Save(curPage.Location);
            ViewSource.View.Refresh();
            e.Handled = true;
        }
    }
}
